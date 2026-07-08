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

        // Accept both the classic (.sln) and the XML (.slnx) solution formats: "dotnet sln list", used below to
        // enumerate the projects, handles the two uniformly. Solution filters (.slnf) are intentionally excluded —
        // the "dotnet sln" subcommand does not process them.
        string extension = Path.GetExtension(fullSolutionPath);
        bool isSolution = string.Equals(extension, ".sln", StringComparison.OrdinalIgnoreCase)
                       || string.Equals(extension, ".slnx", StringComparison.OrdinalIgnoreCase);
        if (isSolution is false) { throw new ArgumentException($"Expected a .sln or .slnx file path, got: '{fullSolutionPath}'", nameof(solutionPath)); }

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

        ProcessResult result = RunProcess("dotnet", ["sln", solutionPath, "list"], solutionDirectory, options.Logger, options.SdkQueryTimeout, options.CancellationToken);
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

        return DotNetGetProperty(projectPath, options.Configuration, tfm, "TargetPath", options);
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
        List<string> args = ["build", solutionPath, "-c", options.Configuration];

        if (string.IsNullOrWhiteSpace(options.TargetFramework) is false) {
            args.Add("-f");
            args.Add(options.TargetFramework);
        }

        foreach (string additionalArgument in options.DotNetBuildAdditionalArguments) {
            if (string.IsNullOrWhiteSpace(additionalArgument) is false) { args.Add(additionalArgument); }
        }

        ProcessResult result = RunProcess("dotnet", args, Path.GetDirectoryName(solutionPath)!, options.Logger, options.BuildTimeout, options.CancellationToken);

        if (result.ExitCode != 0) {
            throw new SolutionDocumentationGenerationException(
                $"dotnet build failed (exit code {result.ExitCode}).\n{result.StandardOutput}\n{result.StandardError}");
        }
    }

    private static string? DotNetGetProperty(string projectPath, string configuration, string targetFramework, string propertyName, SolutionGenerationOptions options) {
        // dotnet msbuild <proj> -getProperty:TargetPath -property:Configuration=Release -property:TargetFramework=net8.0 -nologo
        List<string> args = ["msbuild", projectPath, $"-getProperty:{propertyName}", $"-property:Configuration={configuration}"];

        if (string.IsNullOrWhiteSpace(targetFramework) is false) {
            args.Add($"-property:TargetFramework={targetFramework}");
        }

        args.Add("-nologo");

        ProcessResult result = RunProcess("dotnet", args, Path.GetDirectoryName(projectPath)!, options.Logger, options.SdkQueryTimeout, options.CancellationToken);

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

    private static ProcessResult RunProcess(string fileName, IReadOnlyList<string> arguments, string workingDirectory, IGenerationLogger logger, TimeSpan? timeout = null, CancellationToken cancellationToken = default) {
        ProcessStartInfo psi = new() {
            FileName               = fileName,
            WorkingDirectory       = workingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError  = true,
            UseShellExecute        = false,
            CreateNoWindow         = true
        };

        // Pass arguments one token at a time so the runtime applies the platform-correct escaping. Composing the command
        // line by hand breaks as soon as a token (typically a path) contains a space or a quote.
        foreach (string argument in arguments) {
            psi.ArgumentList.Add(argument);
        }

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

        // Cancellation (e.g. a Ctrl+C forwarded by the CLI host) kills the whole child process tree, so a long-running
        // build/worker does not outlive the request. The registration is declared after the process so it is disposed
        // first (reverse declaration order), guaranteeing the callback never fires against the disposed Process handle.
        // A default (None) token yields a no-op registration.
        using CancellationTokenRegistration registration = cancellationToken.Register(() => {
            try {
                process.Kill(entireProcessTree: true);
            } catch {
                // The process may already have exited between the cancellation and the kill.
            }
        });

        if (timeout is { } limit) {
            if (process.WaitForExit((int)limit.TotalMilliseconds) is false) {
                try {
                    process.Kill(entireProcessTree: true);
                } catch {
                    // The process may already have exited between the timeout and the kill.
                }

                process.WaitForExit();

                // A cancellation that raced the timeout is reported as cancellation, not as a spurious timeout.
                cancellationToken.ThrowIfCancellationRequested();
                logger.Error($"Process '{fileName}' timed out after {limit} and was killed.");

                return new ProcessResult(-1, stdout.ToString(), stderr.ToString());
            }
        }

        // A final no-argument wait blocks until the async stdout/stderr handlers have flushed.
        process.WaitForExit();

        // When cancellation killed the process, surface it as cancellation rather than as the killed process's exit code.
        cancellationToken.ThrowIfCancellationRequested();

        return new ProcessResult(process.ExitCode, stdout.ToString(), stderr.ToString());
    }

    private static void HandleFailure(SolutionGenerationOptions options, string message, Exception? ex = null) {
        if (options.FailureBehavior == FailureBehavior.Continue) {
            // In Continue mode the failure is swallowed so the generation proceeds with the remaining assemblies, but it
            // must never be silent: log it as a warning (with the exception detail when present) so the skipped assembly
            // leaves a trace the caller can diagnose.
            options.Logger.Warning(ex is not null ? $"{message} {ex}" : message);

            return;
        }

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
            // Stop launching new workers as soon as cancellation is requested; the running one (if any) is already
            // killed through the token registration inside RunProcess.
            options.CancellationToken.ThrowIfCancellationRequested();

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
        return DeduplicateAcrossAssemblies(results, options.Logger);
    }

    internal static IReadOnlyList<ErrorDocumentation> DeduplicateAcrossAssemblies(IReadOnlyList<ErrorDocumentation> documentation, IGenerationLogger logger) {
        List<ErrorDocumentation> catalog = new();

        foreach (IGrouping<string?, ErrorDocumentation> group in documentation.GroupBy(doc => doc.Code, StringComparer.OrdinalIgnoreCase)) {
            ErrorDocumentation survivor = group.First();
            catalog.Add(survivor);

            List<ErrorDocumentation> dropped = group.Skip(1).ToList();
            if (dropped.Count == 0) { continue; }

            // Each source assembly is deduplicated in its own worker, so a code that still collides here is declared by
            // distinct assemblies — exactly the cross-assembly case the compile-time analyzers cannot see. Warn instead
            // of dropping it silently.
            string code           = group.Key      ?? "<null>";
            string survivorSource = survivor.Source ?? "<unknown source>";
            string droppedSources = string.Join(", ", dropped.Select(doc => $"'{doc.Source ?? "<unknown source>"}'"));

            logger.Warning(
                $"Duplicate error code '{code}' declared in more than one assembly: keeping the entry from source " +
                $"'{survivorSource}' and dropping {dropped.Count} other(s) from source(s) {droppedSources}. Cross-assembly " +
                "duplicate codes are not caught at compile time (see FCE001/FCE011); give each documented error a unique code.");
        }

        return catalog
              .OrderBy(doc => doc.Code, StringComparer.OrdinalIgnoreCase)
              .ToList();
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
            List<string> args = ["exec"];
            if (File.Exists(depsFile)) {
                args.Add("--depsfile");
                args.Add(depsFile);
            }

            args.Add("--additionalprobingpath");
            args.Add(workerDirectory);
            args.Add(workerAssemblyPath);
            args.Add(targetAssemblyPath);
            args.Add(outputPath);

            // Run the extraction under the requested culture so localized documentation resources resolve to that
            // language. The worker treats it as an option, keeping the two positional paths above intact.
            if (options.Culture is not null) {
                args.Add("--culture");
                args.Add(options.Culture.Name);
            }

            ProcessResult result = RunProcess("dotnet", args, workerDirectory, options.Logger, options.WorkerTimeout, options.CancellationToken);

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
        } catch (Exception ex) when (ex is not SolutionDocumentationGenerationException and not OperationCanceledException) {
            // A cancellation must abandon the whole generation, so it is allowed to propagate rather than being recorded
            // as a per-assembly failure and swallowed under FailureBehavior.Continue.
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