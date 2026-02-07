#region Usings declarations

using System.Diagnostics;
using System.Reflection;
using System.Text;

using Microsoft.Build.Construction;

#endregion

namespace DiagnosableExceptions.GenDoc;

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

        IReadOnlyList<ProjectInfo> projects = ReadSolutionProjects(fullSolutionPath);
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

        IEnumerable<ErrorDocumentation> errorDocumentation = ExtractFromAssemblies(assemblyPaths);
        options.Logger.Info("Documentation generation completed.");

        return errorDocumentation;
    }

    private static List<ProjectInfo> ReadSolutionProjects(string solutionPath) {
        SolutionFile sln = SolutionFile.Parse(solutionPath);

        List<ProjectInfo> projects = new();

        foreach (ProjectInSolution p in sln.ProjectsInOrder) {
            if (p.ProjectType != SolutionProjectType.KnownToBeMSBuildFormat) { continue; }
            if (string.IsNullOrWhiteSpace(p.AbsolutePath)) { continue; }

            string projectPath = Path.GetFullPath(p.AbsolutePath);

            if (!File.Exists(projectPath)) { continue; }

            projects.Add(new ProjectInfo(projectPath));
        }

        return projects;
    }

    private static IReadOnlyList<ProjectInfo> FilterProjects(IReadOnlyList<ProjectInfo> projects, SolutionGenerationOptions options) {
        List<ProjectInfo> included = new();

        foreach (ProjectInfo project in projects) {
            bool optedIn = TryReadOptInFromProjectFile(project.ProjectPath, options.OptInPropertyName);
            if (optedIn) {
                included.Add(project);

                continue;
            }

            if (options.IncludeProjectsWithoutOptIn) {
                included.Add(project);
            }
        }

        return included;
    }

    private static bool TryReadOptInFromProjectFile(string projectPath, string optInPropertyName) {
        try {
            ProjectRootElement? root = ProjectRootElement.Open(projectPath);
            if (root is null) { return false; }

            foreach (ProjectPropertyGroupElement group in root.PropertyGroups) {
                foreach (ProjectPropertyElement prop in group.Properties) {
                    if (string.Equals(prop.Name, optInPropertyName, StringComparison.OrdinalIgnoreCase)) {
                        return IsTrue(prop.Value);
                    }
                }
            }

            return false;
        } catch {
            return false;
        }
    }

    private static string? ResolveTargetPath(string projectPath, SolutionGenerationOptions options) {
        // We purposely avoid MSBuild object model evaluation here to keep dependencies minimal and robust.
        // We read from the project file:
        // - TargetFramework or TargetFrameworks
        // Then compute the expected TargetPath through standard output conventions is risky.
        // So we rely on MSBuild property "TargetPath" via "dotnet msbuild -getProperty:TargetPath".
        // This stays SDK-driven and avoids referencing Microsoft.Build assemblies in this lib.
        string tfm = ResolveTargetFrameworkMoniker(projectPath, options);

        return DotNetGetProperty(projectPath, options.Configuration, tfm, "TargetPath", options.Logger);
    }

    private static string ResolveTargetFrameworkMoniker(string projectPath, SolutionGenerationOptions options) {
        if (string.IsNullOrWhiteSpace(options.TargetFramework) is false) { return options.TargetFramework!; }

        ProjectRootElement? root = ProjectRootElement.Open(projectPath);
        if (root is null) { throw new SolutionDocumentationGenerationException($"Unable to open project file '{projectPath}'."); }

        string? single = ReadMsBuildProperty(root, "TargetFramework");
        if (string.IsNullOrWhiteSpace(single) is false) { return single; }

        string? multi = ReadMsBuildProperty(root, "TargetFrameworks");
        if (string.IsNullOrWhiteSpace(multi)) {
            // If missing, it's often legacy full framework projects. Let MSBuild decide.
            // In that case, we pass an empty TFM and let dotnet msbuild resolve it.
            return string.Empty;
        }

        // Take first framework as default policy for V1.
        string first = multi
                      .Split([';'], StringSplitOptions.RemoveEmptyEntries)
                      .Select(x => x.Trim())
                      .FirstOrDefault() ?? string.Empty;

        return first;
    }

    private static string? ReadMsBuildProperty(ProjectRootElement root, string propertyName) {
        foreach (ProjectPropertyGroupElement group in root.PropertyGroups) {
            foreach (ProjectPropertyElement prop in group.Properties) {
                if (string.Equals(prop.Name, propertyName, StringComparison.OrdinalIgnoreCase)) {
                    return prop.Value;
                }
            }
        }

        return null;
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

    private static ProcessResult RunProcess(string fileName, string arguments, string workingDirectory, IGenerationLogger logger) {
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

    private static IEnumerable<ErrorDocumentation> ExtractFromAssemblies(IReadOnlyList<string> assemblyPaths) {
        List<ErrorDocumentation> results = new();

        foreach (string assemblyPath in assemblyPaths) {
            Assembly                        assembly = Assembly.LoadFrom(assemblyPath);
            IEnumerable<ErrorDocumentation> docs     = AssemblyErrorDocumentationReader.GetErrorDocumentationFrom(assembly);
            results.AddRange(docs);
        }

        return results.OrderBy(x => x.Code, StringComparer.OrdinalIgnoreCase);
    }

    #endregion

    #region Nested types declarations

    private sealed record ProjectInfo(string ProjectPath);

    private sealed record ProcessResult(int ExitCode, string StandardOutput, string StandardError);

    #endregion

}