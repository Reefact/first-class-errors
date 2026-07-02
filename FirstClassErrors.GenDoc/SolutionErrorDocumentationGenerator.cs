#region Usings declarations

using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;

#endregion

namespace FirstClassErrors.GenDoc;

/// <summary>
///     Generates ErrorDocumentation from a solution by building (optional), collecting target assemblies,
///     and extracting documentation using the provided extractor.
/// </summary>
public static class SolutionErrorDocumentationGenerator {

    #region Statics members declarations

    public static IEnumerable<ErrorDocumentation> GetErrorDocumentationFrom(string solutionPath) {
        ArgumentNullException.ThrowIfNull(solutionPath);

        return GetErrorDocumentationFrom(solutionPath, new SolutionGenerationOptions());
    }

    public static IEnumerable<ErrorDocumentation> GetErrorDocumentationFrom(string solutionPath, SolutionGenerationOptions options) {
        ArgumentNullException.ThrowIfNull(solutionPath);
        ArgumentNullException.ThrowIfNull(options);

        options.Logger.Info($"Starting documentation generation for solution '{solutionPath}'");

        string fullSolutionPath = Path.GetFullPath(solutionPath);
        if (File.Exists(fullSolutionPath) is false) { throw new FileNotFoundException($"Solution file not found: '{fullSolutionPath}'", fullSolutionPath); }
        if (string.Equals(Path.GetExtension(fullSolutionPath), ".sln", StringComparison.OrdinalIgnoreCase) is false) { throw new ArgumentException($"Expected a .sln file path, got: '{fullSolutionPath}'", nameof(solutionPath)); }

        if (options.BuildSolution) {
            DotNetBuild(fullSolutionPath, options);
        }

        IReadOnlyList<ProjectInfo> projects = ReadSolutionProjects(fullSolutionPath, options);
        options.Logger.Debug($"Found {projects.Count} MSBuild projects in solution.");

        IReadOnlyList<ProjectInfo> includedProjects = FilterProjects(projects, options);
        options.Logger.Info($"{includedProjects.Count} projects opted-in for error documentation.");

        List<string> assemblyPaths = new(includedProjects.Count);
        foreach (ProjectInfo project in includedProjects) {
            options.Logger.Debug($"Resolving TargetPath for project '{project.ProjectPath}'");
            try {
                string? targetPath = ResolveTargetPath(project.ProjectPath, options);
                if (string.IsNullOrWhiteSpace(targetPath)) { continue; }

                if (!File.Exists(targetPath)) {
                    HandleFailure(options, $"Target assembly not found for project '{project.ProjectPath}'. Resolved TargetPath='{targetPath}'.");

                    continue;
                }

                assemblyPaths.Add(targetPath);
            } catch (Exception ex) {
                HandleFailure(options, $"Failed to resolve target path for project '{project.ProjectPath}'.", ex);
            }
        }

        assemblyPaths = assemblyPaths
                       .Select(Path.GetFullPath)
                       .Distinct(StringComparer.OrdinalIgnoreCase)
                       .ToList();

        if (assemblyPaths.Count == 0) { return []; }

        IEnumerable<ErrorDocumentation> errorDocumentation = ExtractFromAssemblies(assemblyPaths, options);
        options.Logger.Info("Documentation generation completed.");

        return errorDocumentation;
    }

    public static IEnumerable<ErrorDocumentation> GetErrorDocumentationFromAssemblies(IReadOnlyList<string> assemblyPaths, SolutionGenerationOptions options) {
        ArgumentNullException.ThrowIfNull(assemblyPaths);
        ArgumentNullException.ThrowIfNull(options);

        options.Logger.Info($"Starting documentation generation from {assemblyPaths.Count} assembly path(s).");

        List<string> resolved = new();
        foreach (string assemblyPath in assemblyPaths) {
            string fullPath = Path.GetFullPath(assemblyPath);
            if (File.Exists(fullPath) is false) {
                HandleFailure(options, $"Assembly not found: '{fullPath}'.");

                continue;
            }

            resolved.Add(fullPath);
        }

        resolved = resolved.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        if (resolved.Count == 0) { return []; }

        IEnumerable<ErrorDocumentation> errorDocumentation = ExtractFromAssemblies(resolved, options);
        options.Logger.Info("Documentation generation completed.");

        return errorDocumentation;
    }

    private static List<ProjectInfo> ReadSolutionProjects(string solutionPath, SolutionGenerationOptions options) {
        // Enumerate projects via the SDK ("dotnet sln list") rather than the in-process MSBuild object model. This keeps
        // the generator free of the heavy Microsoft.Build dependency and handles both .sln and .slnx uniformly.
        string solutionDirectory = Path.GetDirectoryName(solutionPath) ?? ".";

        ProcessResult result = RunProcess("dotnet", $"sln \"{solutionPath}\" list", solutionDirectory, options.Logger);
        if (result.ExitCode != 0) {
            throw new SolutionDocumentationGenerationException(
                $"Failed to list the projects of solution '{solutionPath}' (exit code {result.ExitCode}).\n{result.StandardError}");
        }

        List<ProjectInfo> projects = new();

        foreach (string rawLine in result.StandardOutput.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries)) {
            string line = rawLine.Trim();

            // The command prints a (localized) header followed by the project paths, relative to the solution. Keep
            // only lines that resolve to an existing project file — this is robust to the header and to localization.
            if (line.EndsWith("proj", StringComparison.OrdinalIgnoreCase) is false) { continue; }

            string projectPath = Path.GetFullPath(Path.Combine(solutionDirectory, line));
            if (File.Exists(projectPath) is false) { continue; }

            projects.Add(new ProjectInfo(projectPath));
        }

        return projects;
    }

    private static IReadOnlyList<ProjectInfo> FilterProjects(IReadOnlyList<ProjectInfo> projects, SolutionGenerationOptions options) {
        List<ProjectInfo> included = new();

        foreach (ProjectInfo project in projects) {
            bool? optedIn = TryReadOptInFromProjectFile(project.ProjectPath, options.OptInPropertyName);

            if (optedIn == true) {
                included.Add(project);

                continue;
            }

            // An explicit opt-out (property present and set to a falsy value) is always honored, even when
            // IncludeProjectsWithoutOptIn is true.
            if (optedIn == false) { continue; }

            // The opt-in property is absent: fall back to the global policy.
            if (options.IncludeProjectsWithoutOptIn) {
                included.Add(project);
            }
        }

        return included;
    }

    private static bool? TryReadOptInFromProjectFile(string projectPath, string optInPropertyName) {
        string? value = ReadMsBuildProperty(projectPath, optInPropertyName);

        // Absent property -> no opinion (null); present (even empty) -> its truthiness.
        return value is null ? null : IsTrue(value);
    }

    private static string? ResolveTargetPath(string projectPath, SolutionGenerationOptions options) {
        // We resolve TargetPath by invoking the SDK ("dotnet msbuild -getProperty:TargetPath") rather than evaluating
        // the project through the MSBuild object model. Computing the output path ourselves from output conventions
        // would be fragile, and a full evaluation would require resolving the project's full import graph; delegating
        // to the installed SDK keeps the result authoritative across project styles.
        string tfm = ResolveTargetFrameworkMoniker(projectPath, options);

        return DotNetGetProperty(projectPath, options.Configuration, tfm, "TargetPath", options.Logger);
    }

    private static string ResolveTargetFrameworkMoniker(string projectPath, SolutionGenerationOptions options) {
        if (string.IsNullOrWhiteSpace(options.TargetFramework) is false) { return options.TargetFramework!; }

        string? single = ReadMsBuildProperty(projectPath, "TargetFramework");
        if (string.IsNullOrWhiteSpace(single) is false) { return single; }

        string? multi = ReadMsBuildProperty(projectPath, "TargetFrameworks");
        if (string.IsNullOrWhiteSpace(multi)) {
            // Missing on legacy full-framework projects; pass an empty TFM and let "dotnet msbuild" resolve it.
            return string.Empty;
        }

        // Take the first framework as the default policy for V1.
        return multi
              .Split([';'], StringSplitOptions.RemoveEmptyEntries)
              .Select(x => x.Trim())
              .FirstOrDefault() ?? string.Empty;
    }

    private static string? ReadMsBuildProperty(string projectPath, string propertyName) {
        // Read a raw, unevaluated property from the project XML. This mirrors what the previous MSBuild-object-model
        // reader saw (ProjectRootElement.Open does not evaluate imports/conditions either), without the dependency.
        // Matching on the element's local name handles both SDK-style (no namespace) and legacy MSBuild-namespaced files.
        try {
            XDocument document = XDocument.Load(projectPath);

            return document
                  .Descendants()
                  .Where(element => string.Equals(element.Name.LocalName, "PropertyGroup", StringComparison.OrdinalIgnoreCase))
                  .SelectMany(propertyGroup => propertyGroup.Elements())
                  .Where(property => string.Equals(property.Name.LocalName, propertyName, StringComparison.OrdinalIgnoreCase))
                  .Select(property => property.Value)
                  .FirstOrDefault();
        } catch {
            return null;
        }
    }

    private static void DotNetBuild(string solutionPath, SolutionGenerationOptions options) {
        StringBuilder args = new();
        args.Append("build ");
        args.Append('"').Append(solutionPath).Append('"');
        args.Append(" -c ").Append(options.Configuration);

        if (string.IsNullOrWhiteSpace(options.TargetFramework) is false) {
            args.Append(" -f ").Append(options.TargetFramework);
        }

        if (string.IsNullOrWhiteSpace(options.DotNetBuildAdditionalArguments) is false) {
            args.Append(' ').Append(options.DotNetBuildAdditionalArguments);
        }

        ProcessResult result = RunProcess("dotnet", args.ToString(), Path.GetDirectoryName(solutionPath)!, options.Logger);

        if (result.ExitCode != 0) {
            throw new SolutionDocumentationGenerationException(
                $"dotnet build failed (exit code {result.ExitCode}).\n{result.StandardOutput}\n{result.StandardError}");
        }
    }

    private static string? DotNetGetProperty(string projectPath, string configuration, string targetFramework, string propertyName, IGenerationLogger logger) {
        // dotnet msbuild <proj> -getProperty:TargetPath -property:Configuration=Release -property:TargetFramework=net8.0 -nologo
        StringBuilder args = new();
        args.Append("msbuild ");
        args.Append('"').Append(projectPath).Append('"');
        args.Append(" -getProperty:").Append(propertyName);
        args.Append(" -property:Configuration=").Append(configuration);

        if (string.IsNullOrWhiteSpace(targetFramework) is false) {
            args.Append(" -property:TargetFramework=").Append(targetFramework);
        }

        args.Append(" -nologo");

        ProcessResult result = RunProcess("dotnet", args.ToString(), Path.GetDirectoryName(projectPath)!, logger);

        if (result.ExitCode != 0) {
            return null;
        }

        // Output format is usually "TargetPath = ...", but can vary.
        // We handle common cases robustly.
        string output = result.StandardOutput.Trim();
        string line = output
                     .Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries)
                     .FirstOrDefault(l => l.IndexOf(propertyName, StringComparison.OrdinalIgnoreCase) >= 0)
                   ?? output.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries).LastOrDefault()
                   ?? string.Empty;

        string value = ParseMsBuildGetPropertyLine(line, propertyName);
        if (string.IsNullOrWhiteSpace(value)) { return null; }

        return value.Trim();
    }

    private static string ParseMsBuildGetPropertyLine(string line, string propertyName) {
        // Examples:
        // "TargetPath = C:\...\bin\Release\net8.0\My.dll"
        // "TargetPath: C:\...\My.dll"
        // "C:\...\My.dll"
        if (string.IsNullOrWhiteSpace(line)) { return string.Empty; }

        int idx = line.IndexOf(propertyName, StringComparison.OrdinalIgnoreCase);
        if (idx < 0) { return line.Trim(); }

        string afterName = line.Substring(idx + propertyName.Length).Trim();

        if (afterName.StartsWith("=", StringComparison.Ordinal)) {
            return afterName.Substring(1).Trim();
        }

        if (afterName.StartsWith(":", StringComparison.Ordinal)) {
            return afterName.Substring(1).Trim();
        }

        return afterName.Trim();
    }

    private static ProcessResult RunProcess(string fileName, string arguments, string workingDirectory, IGenerationLogger logger, TimeSpan? timeout = null) {
        ProcessStartInfo psi = new() {
            FileName               = fileName,
            Arguments              = arguments,
            WorkingDirectory       = workingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError  = true,
            UseShellExecute        = false,
            CreateNoWindow         = true
        };

        using Process process = new();
        process.StartInfo = psi;

        StringBuilder stdout = new();
        StringBuilder stderr = new();

        process.OutputDataReceived += (_, e) => {
            if (e.Data is null) { return; }

            stdout.AppendLine(e.Data);
            logger.Info(e.Data);
        };

        process.ErrorDataReceived += (_, e) => {
            if (e.Data is null) { return; }

            stderr.AppendLine(e.Data);
            logger.Error(e.Data);
        };

        bool started = process.Start();
        if (started is false) { throw new SolutionDocumentationGenerationException($"Failed to start process '{fileName}'."); }

        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        if (timeout is { } limit) {
            if (process.WaitForExit((int)limit.TotalMilliseconds) is false) {
                try {
                    process.Kill(entireProcessTree: true);
                } catch {
                    // The process may already have exited between the timeout and the kill.
                }

                process.WaitForExit();
                logger.Error($"Process '{fileName}' timed out after {limit} and was killed.");

                return new ProcessResult(-1, stdout.ToString(), stderr.ToString());
            }
        }

        // A final no-argument wait blocks until the async stdout/stderr handlers have flushed.
        process.WaitForExit();

        return new ProcessResult(process.ExitCode, stdout.ToString(), stderr.ToString());
    }

    private static void HandleFailure(SolutionGenerationOptions options, string message, Exception? ex = null) {
        if (options.FailureBehavior == FailureBehavior.Continue) { return; }
        if (ex is not null) { throw new SolutionDocumentationGenerationException(message, ex); }

        throw new SolutionDocumentationGenerationException(message);
    }

    private static bool IsTrue(string? value) {
        if (value is null) { return false; }

        return value.Trim().Equals("true", StringComparison.OrdinalIgnoreCase)
            || value.Trim().Equals("1", StringComparison.OrdinalIgnoreCase)
            || value.Trim().Equals("yes", StringComparison.OrdinalIgnoreCase)
            || value.Trim().Equals("y", StringComparison.OrdinalIgnoreCase);
    }

    private static IEnumerable<ErrorDocumentation> ExtractFromAssemblies(IReadOnlyList<string> assemblyPaths, SolutionGenerationOptions options) {
        string workerAssemblyPath = ResolveWorkerAssemblyPath(options);

        List<ErrorDocumentation> results = new();

        foreach (string assemblyPath in assemblyPaths) {
            // Each assembly is documented in a dedicated worker process, launched against that assembly's own
            // dependency closure (dotnet exec --depsfile ...). This gives a fresh static registry per assembly, binds to
            // the target's own FirstClassErrors version, and isolates a crashing or hanging factory from the generator.
            ErrorDocumentationExtractionResult extraction = RunWorker(workerAssemblyPath, assemblyPath, options);

            foreach (ErrorDocumentationExtractionFailure failure in extraction.Failures) {
                options.Logger.Error($"Documentation extraction issue in '{assemblyPath}': {failure}");
            }

            results.AddRange(extraction.Documentation);
        }

        // Deduplicate across assemblies as well: the same error Code declared in two assemblies must collapse to a
        // single catalog entry, mirroring the per-assembly deduplication performed by the reader.
        return results
              .GroupBy(x => x.Code, StringComparer.OrdinalIgnoreCase)
              .Select(g => g.First())
              .OrderBy(x => x.Code, StringComparer.OrdinalIgnoreCase);
    }

    private static string ResolveWorkerAssemblyPath(SolutionGenerationOptions options) {
        if (string.IsNullOrWhiteSpace(options.WorkerAssemblyPath) is false) {
            if (File.Exists(options.WorkerAssemblyPath) is false) {
                throw new SolutionDocumentationGenerationException($"The configured documentation worker was not found at '{options.WorkerAssemblyPath}'.");
            }

            return options.WorkerAssemblyPath!;
        }

        // Convention: the worker is deployed next to the tool (copied into its output directory).
        string candidate = Path.Combine(AppContext.BaseDirectory, "FirstClassErrors.GenDoc.Worker.dll");
        if (File.Exists(candidate)) { return candidate; }

        throw new SolutionDocumentationGenerationException(
            "The documentation worker 'FirstClassErrors.GenDoc.Worker.dll' could not be located. Set " +
            $"{nameof(SolutionGenerationOptions)}.{nameof(SolutionGenerationOptions.WorkerAssemblyPath)}, or ensure the worker is deployed next to the tool.");
    }

    private static ErrorDocumentationExtractionResult RunWorker(string workerAssemblyPath, string targetAssemblyPath, SolutionGenerationOptions options) {
        string outputPath      = Path.Combine(Path.GetTempPath(), $"first-class-errors-doc-{Guid.NewGuid():N}.json");
        string workerDirectory = Path.GetDirectoryName(workerAssemblyPath) ?? AppContext.BaseDirectory;

        try {
            string depsFile = Path.ChangeExtension(targetAssemblyPath, ".deps.json");

            // Run the worker on its own runtimeconfig (RollForward=Major) but against the TARGET's dependency graph, so
            // the target and its own FirstClassErrors resolve from the target's deps.json. The worker's own directory is
            // added as a fallback probing path (consulted only for assemblies the target's deps.json does not provide).
            StringBuilder args = new();
            args.Append("exec ");
            if (File.Exists(depsFile)) {
                args.Append("--depsfile \"").Append(depsFile).Append("\" ");
            }

            args.Append("--additionalprobingpath \"").Append(workerDirectory).Append("\" ");
            args.Append('"').Append(workerAssemblyPath).Append("\" ");
            args.Append('"').Append(targetAssemblyPath).Append("\" ");
            args.Append('"').Append(outputPath).Append('"');

            ProcessResult result = RunProcess("dotnet", args.ToString(), workerDirectory, options.Logger, options.WorkerTimeout);

            if (result.ExitCode != 0) {
                HandleFailure(options, $"The documentation worker failed (exit code {result.ExitCode}) for '{targetAssemblyPath}'.\n{result.StandardError}");

                return new ErrorDocumentationExtractionResult([], []);
            }

            if (File.Exists(outputPath) is false) {
                HandleFailure(options, $"The documentation worker produced no output for '{targetAssemblyPath}'.");

                return new ErrorDocumentationExtractionResult([], []);
            }

            string                              json       = File.ReadAllText(outputPath);
            ErrorDocumentationExtractionResult? extraction = JsonSerializer.Deserialize<ErrorDocumentationExtractionResult>(json);

            if (extraction is null) {
                HandleFailure(options, $"The documentation worker produced unreadable output for '{targetAssemblyPath}'.");

                return new ErrorDocumentationExtractionResult([], []);
            }

            return extraction;
        } catch (Exception ex) when (ex is not SolutionDocumentationGenerationException) {
            HandleFailure(options, $"Failed to run the documentation worker for '{targetAssemblyPath}'.", ex);

            return new ErrorDocumentationExtractionResult([], []);
        } finally {
            try {
                if (File.Exists(outputPath)) { File.Delete(outputPath); }
            } catch {
                // Best-effort cleanup of the temporary catalog file.
            }
        }
    }

    #endregion

    #region Nested types declarations

    private sealed record ProjectInfo(string ProjectPath);

    private sealed record ProcessResult(int ExitCode, string StandardOutput, string StandardError);

    #endregion

}