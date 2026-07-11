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

    /// <summary>The .NET CLI executable used for every solution/project SDK query and for the build.</summary>
    private const string DotNetCli = "dotnet";

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

        List<string> projects = ReadSolutionProjects(fullSolutionPath, options);
        options.Logger.Debug($"Found {projects.Count} MSBuild projects in solution.");

        IReadOnlyList<string> includedProjects = FilterProjects(projects, options);
        options.Logger.Info($"{includedProjects.Count} projects opted-in for error documentation.");

        List<string> assemblyPaths = new(includedProjects.Count);
        foreach (string projectPath in includedProjects) {
            options.Logger.Debug($"Resolving TargetPath for project '{projectPath}'");
            try {
                string? targetPath = ResolveTargetPath(projectPath, options);
                if (string.IsNullOrWhiteSpace(targetPath)) { continue; }

                if (!File.Exists(targetPath)) {
                    HandleFailure(options, $"Target assembly not found for project '{projectPath}'. Resolved TargetPath='{targetPath}'.");

                    continue;
                }

                assemblyPaths.Add(targetPath);
            } catch (Exception ex) {
                HandleFailure(options, $"Failed to resolve target path for project '{projectPath}'.", ex);
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

    private static List<string> ReadSolutionProjects(string solutionPath, SolutionGenerationOptions options) {
        // Enumerate projects via the SDK ("dotnet sln list") rather than the in-process MSBuild object model. This keeps
        // the generator free of the heavy Microsoft.Build dependency and handles both .sln and .slnx uniformly.
        string solutionDirectory = Path.GetDirectoryName(solutionPath) ?? ".";

        ProcessResult result = RunProcess(DotNetCli, ["sln", solutionPath, "list"], solutionDirectory, options.Logger, options.SdkQueryTimeout, options.CancellationToken);
        if (result.ExitCode != 0) {
            throw new SolutionDocumentationGenerationException(
                $"Failed to list the projects of solution '{solutionPath}' (exit code {result.ExitCode}).\n{result.StandardError}");
        }

        List<string> projects = new();

        foreach (string rawLine in result.StandardOutput.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries)) {
            string line = rawLine.Trim();

            // The command prints a (localized) header followed by the project paths, relative to the solution. Keep
            // only lines that resolve to an existing project file — this is robust to the header and to localization.
            if (line.EndsWith("proj", StringComparison.OrdinalIgnoreCase) is false) { continue; }

            string projectPath = Path.GetFullPath(Path.Combine(solutionDirectory, line));
            if (File.Exists(projectPath) is false) { continue; }

            projects.Add(projectPath);
        }

        return projects;
    }

    internal static IReadOnlyList<string> FilterProjects(IReadOnlyList<string> projectPaths, SolutionGenerationOptions options) {
        List<string> included = new();

        included.AddRange(projectPaths.Where(projectPath => ShouldIncludeProject(projectPath, options)));

        // An opt-in declared only in a shared build file (Directory.Build.props, an import) reads as absent in every
        // .csproj — per project, that is indistinguishable from a genuine absence, so it cannot be diagnosed above. The
        // one signature visible at this level is a solution where nothing opted in while the filter was active: name the
        // most likely cause instead of handing back an empty catalog in silence. A warning, not a failure — an empty
        // catalog is legitimate for a solution that documents no errors.
        if (included.Count == 0 && projectPaths.Count > 0 && options.IncludeProjectsWithoutOptIn is false) {
            options.Logger.Warning(
                $"No project opted in to error documentation: '{options.OptInPropertyName}' was not set to a truthy value " +
                "in any project file. GenDoc reads this property literally from the .csproj, without MSBuild evaluation: a " +
                "value declared in a shared Directory.Build.props or another imported build file is not seen. Declare it " +
                "in the .csproj of each project to document, or document assemblies explicitly with --assemblies.");
        }

        return included;
    }

    internal static bool ShouldIncludeProject(string projectPath, SolutionGenerationOptions options) {
        OptInReadResult optIn = ReadOptIn(projectPath, options.OptInPropertyName);

        // The opt-in is a marker read straight from the project XML, without MSBuild evaluation. When it is defined more
        // than once, or gated behind a Condition, its effective value cannot be known here — so we refuse to guess it.
        // Surfacing it through the normal failure path (a warning under Continue, a hard stop otherwise) rather than
        // silently picking one occurrence keeps a library about first-class errors honest about its own opt-in: the
        // project is left out with a trace instead of being documented on a coin toss.
        if (optIn.IsAmbiguous) {
            HandleFailure(
                options,
                $"Cannot determine the opt-in for project '{projectPath}': the '{options.OptInPropertyName}' property is " +
                $"{optIn.AmbiguityReason} in the project XML, which GenDoc reads without MSBuild evaluation. Declare it once, " +
                "literally and unconditionally in the .csproj, or document the assembly explicitly with --assemblies. The project is skipped.");

            return false;
        }

        if (optIn.OptedIn == true) { return true; }

        // An explicit opt-out (property present and set to a falsy value) is always honored, even when
        // IncludeProjectsWithoutOptIn is true.
        if (optIn.OptedIn == false) { return false; }

        // The opt-in property is absent: fall back to the global policy.
        return options.IncludeProjectsWithoutOptIn;
    }

    private static OptInReadResult ReadOptIn(string projectPath, string optInPropertyName) {
        MsBuildPropertyRead read = ReadMsBuildPropertyDetailed(projectPath, optInPropertyName);

        return read.Kind switch {
            MsBuildPropertyReadKind.Absent    => new OptInReadResult(null, null),
            MsBuildPropertyReadKind.Ambiguous => new OptInReadResult(null, read.AmbiguityReason),
            // Present (even empty) -> its truthiness.
            _                                 => new OptInReadResult(IsTrue(read.Value), null)
        };
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
        // Raw, unevaluated read used for TargetFramework(s) resolution, where an ambiguous read is harmless: the caller
        // falls back to an authoritative "dotnet msbuild" evaluation when the value is missing or unusable. Preserves the
        // historical "first occurrence" behavior by collapsing an ambiguous read to its first matched value.
        MsBuildPropertyRead read = ReadMsBuildPropertyDetailed(projectPath, propertyName);

        return read.Kind == MsBuildPropertyReadKind.Absent ? null : read.Value;
    }

    private static MsBuildPropertyRead ReadMsBuildPropertyDetailed(string projectPath, string propertyName) {
        // Read a raw, unevaluated property from the project XML. This mirrors what the previous MSBuild-object-model
        // reader saw (ProjectRootElement.Open does not evaluate imports/conditions either), without the dependency.
        // Matching on the element's local name handles both SDK-style (no namespace) and legacy MSBuild-namespaced files.
        try {
            XDocument document = XDocument.Load(projectPath);

            List<XElement> matches = document
                                    .Descendants()
                                    .Where(element => string.Equals(element.Name.LocalName, "PropertyGroup", StringComparison.OrdinalIgnoreCase))
                                    .Where(propertyGroup => IsInsideTarget(propertyGroup) is false)
                                    .SelectMany(propertyGroup => propertyGroup.Elements())
                                    .Where(property => string.Equals(property.Name.LocalName, propertyName, StringComparison.OrdinalIgnoreCase))
                                    .ToList();

            if (matches.Count == 0) { return MsBuildPropertyRead.Absent(); }

            // The XML is taken verbatim, so a property defined more than once, or gated behind a Condition (on the
            // element itself, on any ancestor, or via a Choose/When/Otherwise branch), has an effective value we cannot
            // compute without evaluating MSBuild. Report it as ambiguous and let the caller decide, rather than picking
            // one at random.
            if (matches.Count > 1) {
                return MsBuildPropertyRead.Ambiguous(matches[0].Value, $"defined {matches.Count} times");
            }

            if (IsConditioned(matches[0])) {
                return MsBuildPropertyRead.Ambiguous(matches[0].Value, "defined under an MSBuild Condition");
            }

            return MsBuildPropertyRead.Single(matches[0].Value);
        } catch {
            return MsBuildPropertyRead.Absent();
        }
    }

    private static bool IsConditioned(XElement property) {
        // MSBuild Condition attributes are unqualified even in legacy namespaced files, so a no-namespace lookup fits both.
        // The condition can sit on the property itself or on ANY ancestor, not only its PropertyGroup: a
        // <Choose>/<When> block carries it two levels up. And a <When>/<Otherwise> branch is conditional by
        // construction — <Otherwise> without bearing any Condition attribute of its own — so those ancestor names count
        // as conditions too.
        if (property.Attribute("Condition") is not null) { return true; }

        return property
              .Ancestors()
              .Any(ancestor => ancestor.Attribute("Condition") is not null
                            || string.Equals(ancestor.Name.LocalName, "When",      StringComparison.OrdinalIgnoreCase)
                            || string.Equals(ancestor.Name.LocalName, "Otherwise", StringComparison.OrdinalIgnoreCase));
    }

    private static bool IsInsideTarget(XElement propertyGroup) {
        // A PropertyGroup nested inside a <Target> assigns its properties when the target RUNS, not when the project is
        // evaluated: for an evaluation-time read it neither provides a value nor counts toward a duplicate.
        return propertyGroup
              .Ancestors()
              .Any(ancestor => string.Equals(ancestor.Name.LocalName, "Target", StringComparison.OrdinalIgnoreCase));
    }

    private static void DotNetBuild(string solutionPath, SolutionGenerationOptions options) {
        List<string> args = ["build", solutionPath, "-c", options.Configuration];

        if (string.IsNullOrWhiteSpace(options.TargetFramework) is false) {
            args.Add("-f");
            args.Add(options.TargetFramework);
        }

        args.AddRange(options.DotNetBuildAdditionalArguments.Where(additionalArgument => string.IsNullOrWhiteSpace(additionalArgument) is false));

        ProcessResult result = RunProcess(DotNetCli, args, Path.GetDirectoryName(solutionPath)!, options.Logger, options.BuildTimeout, options.CancellationToken);

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

        ProcessResult result = RunProcess(DotNetCli, args, Path.GetDirectoryName(projectPath)!, options.Logger, options.SdkQueryTimeout, options.CancellationToken);

        if (result.ExitCode != 0) {
            return null;
        }

        // Output format is usually "TargetPath = ...", but can vary.
        // We handle common cases robustly.
        string output = result.StandardOutput.Trim();
        string line = output
                     .Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries)
                     .FirstOrDefault(l => l.Contains(propertyName, StringComparison.OrdinalIgnoreCase))
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

        if (afterName.StartsWith('=')) {
            return afterName.Substring(1).Trim();
        }

        if (afterName.StartsWith(':')) {
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

        if (timeout is { } limit && process.WaitForExit((int)limit.TotalMilliseconds) is false) {
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

            ProcessResult result = RunProcess(DotNetCli, args, workerDirectory, options.Logger, options.WorkerTimeout, options.CancellationToken);

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

    private sealed record ProcessResult(int ExitCode, string StandardOutput, string StandardError);

    private enum MsBuildPropertyReadKind {

        Absent,
        Value,
        Ambiguous

    }

    private sealed record MsBuildPropertyRead(MsBuildPropertyReadKind Kind, string? Value, string? AmbiguityReason) {

        public static MsBuildPropertyRead Absent()                                => new(MsBuildPropertyReadKind.Absent, null, null);
        public static MsBuildPropertyRead Single(string?    value)                => new(MsBuildPropertyReadKind.Value, value, null);
        public static MsBuildPropertyRead Ambiguous(string? value, string reason) => new(MsBuildPropertyReadKind.Ambiguous, value, reason);

    }

    private sealed record OptInReadResult(bool? OptedIn, string? AmbiguityReason) {

        public bool IsAmbiguous => AmbiguityReason is not null;

    }

    #endregion

}