#region Usings declarations

using System.Reflection;
using System.Resources;

#endregion

namespace FirstClassErrors.GenDoc;

/// <summary>
///     Reads documented errors from an already-loaded assembly and exposes the extracted documentation model.
/// </summary>
/// <remarks>
///     <para>
///         This reader is intentionally infrastructure-free: it operates on an <see cref="Assembly" /> that the
///         caller has already loaded (in whatever <c>AssemblyLoadContext</c> or process it sees fit) and depends only
///         on reflection and the FirstClassErrors documentation model. Deciding how to <b>locate and load</b>
///         assemblies safely — dependency resolution, isolation, unloading — is the responsibility of the tooling
///         layer, not of this method.
///     </para>
///     <para>
///         Extraction is resilient: a single type or documentation factory that fails to load, resolve or execute is
///         recorded as an <see cref="ErrorDocumentationExtractionFailure" /> and skipped, rather than aborting the
///         whole extraction. This matters for "living" documentation, where one broken example must not silence the
///         entire catalog — the failure is surfaced as data for the caller to report or escalate.
///     </para>
/// </remarks>
public static class AssemblyErrorDocumentationReader {

    #region Statics members declarations

    /// <summary>
    ///     Extracts all documented errors from the provided (already-loaded) assembly.
    /// </summary>
    /// <param name="assembly">The assembly to scan for documented errors.</param>
    /// <returns>
    ///     An <see cref="ErrorDocumentationExtractionResult" /> carrying the deduplicated documentation and every
    ///     per-type / per-factory failure encountered along the way.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="assembly" /> is <c>null</c>.</exception>
    public static ErrorDocumentationExtractionResult GetErrorDocumentationFrom(Assembly assembly) {
        if (assembly is null) { throw new ArgumentNullException(nameof(assembly)); }

        List<ErrorDocumentation>                  documentation = new();
        List<ErrorDocumentationExtractionFailure> failures      = new();

        foreach (Type type in GetLoadableTypes(assembly, failures)) {
            if (IsExtractableType(type) is false) { continue; }

            ExtractFromType(type, documentation, failures);
        }

        List<ErrorDocumentation> deduplicated = new();

        // Order before grouping so that, when several factories share the same Code, the surviving documentation is
        // chosen deterministically (reflection ordering is not guaranteed).
        IEnumerable<IGrouping<string?, ErrorDocumentation>> groups = documentation
                                                                    .OrderBy(doc => doc.Code, StringComparer.OrdinalIgnoreCase)
                                                                    .ThenBy(doc => doc.Source, StringComparer.Ordinal)
                                                                    .GroupBy(doc => doc.Code, StringComparer.OrdinalIgnoreCase);

        foreach (IGrouping<string?, ErrorDocumentation> group in groups) {
            ErrorDocumentation survivor = group.First();
            deduplicated.Add(survivor);

            // A code shared by several factories collapses to a single catalog entry. The drop is deterministic, but it
            // must not be silent: record it as a failure so the caller can report or escalate a code collision that the
            // FCE001/FCE011 analyzers cannot always catch at compile time.
            List<ErrorDocumentation> dropped = group.Skip(1).ToList();
            if (dropped.Count > 0) {
                failures.Add(BuildDuplicateCodeFailure(survivor, dropped));
            }
        }

        deduplicated = deduplicated
                      .OrderBy(doc => doc.Code, StringComparer.OrdinalIgnoreCase)
                      .ToList();

        return new ErrorDocumentationExtractionResult(deduplicated, failures);
    }

    private static ErrorDocumentationExtractionFailure BuildDuplicateCodeFailure(ErrorDocumentation survivor, IReadOnlyList<ErrorDocumentation> dropped) {
        string code           = survivor.Code   ?? "<null>";
        string survivorSource = survivor.Source ?? "<unknown source>";
        string droppedSources = string.Join(", ", dropped.Select(doc => $"'{doc.Source ?? "<unknown source>"}'"));

        string message =
            $"Duplicate error code '{code}': {dropped.Count + 1} documentation factories declare it. " +
            $"Keeping the entry from source '{survivorSource}' and dropping {dropped.Count} other(s) from source(s) {droppedSources}. " +
            "Give each documented error a unique code so no documentation is silently lost.";

        return new ErrorDocumentationExtractionFailure(survivorSource, null, message, null);
    }

    private static IEnumerable<Type> GetLoadableTypes(Assembly assembly, List<ErrorDocumentationExtractionFailure> failures) {
        // A documented assembly may reference types that cannot be loaded (e.g. a missing or version-mismatched
        // dependency). GetTypes() then throws ReflectionTypeLoadException; we still want the loadable, attributed
        // types rather than aborting extraction for the whole assembly.
        try {
            return assembly.GetTypes();
        } catch (ReflectionTypeLoadException ex) {
            string assemblyName = assembly.GetName().Name ?? "<assembly>";

            foreach (Exception? loaderException in ex.LoaderExceptions ?? []) {
                if (loaderException is null) { continue; }

                failures.Add(new ErrorDocumentationExtractionFailure(assemblyName, null, "A referenced type could not be loaded.", loaderException.ToString()));
            }

            return ex.Types.Where(type => type is not null).Select(type => type!);
        }
    }

    private static bool IsExtractableType(Type type) {
        // Only concrete, closed classes can host the static documentation factories we invoke. Open generic type
        // definitions would throw on invocation, so they are skipped.
        return type is { IsClass: true, IsGenericTypeDefinition: false };
    }

    private static void ExtractFromType(Type type, List<ErrorDocumentation> documentation, List<ErrorDocumentationExtractionFailure> failures) {
        ProvidesErrorsForAttribute? providesErrorsFor;
        try {
            providesErrorsFor = type.GetCustomAttribute<ProvidesErrorsForAttribute>();
        } catch (Exception ex) {
            failures.Add(new ErrorDocumentationExtractionFailure(TypeName(type), null, "Failed to read the [ProvidesErrorsFor] attribute.", ex.ToString()));

            return;
        }

        if (providesErrorsFor is null) { return; }

        foreach (MethodInfo factoryMethod in type.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)) {
            DocumentedByAttribute? documentedBy;
            try {
                documentedBy = factoryMethod.GetCustomAttribute<DocumentedByAttribute>();
            } catch (Exception ex) {
                failures.Add(new ErrorDocumentationExtractionFailure(TypeName(type), factoryMethod.Name, "Failed to read the [DocumentedBy] attribute.", ex.ToString()));

                continue;
            }

            if (documentedBy is null) { continue; }

            MethodInfo? documentationMethod = ResolveDocumentationMethod(type, documentedBy.MethodName, failures);
            if (documentationMethod is null) { continue; }

            object? produced;
            try {
                produced = documentationMethod.Invoke(null, []);
            } catch (Exception ex) {
                // Unwrap the reflection wrapper so the reported failure points at the real cause.
                Exception cause = (ex as TargetInvocationException)?.InnerException ?? ex;
                failures.Add(new ErrorDocumentationExtractionFailure(TypeName(type), documentationMethod.Name, "The documentation factory threw while being executed.", cause.ToString()));

                continue;
            }

            if (produced is not ErrorDocumentation errorDocumentation) {
                failures.Add(new ErrorDocumentationExtractionFailure(TypeName(type), documentationMethod.Name, "The documentation factory did not return an ErrorDocumentation instance.", null));

                continue;
            }

            errorDocumentation.Source            = providesErrorsFor.Source;
            errorDocumentation.SourceDescription = ResolveSourceDescription(providesErrorsFor);

            documentation.Add(errorDocumentation);
        }
    }

    private static MethodInfo? ResolveDocumentationMethod(Type type, string methodName, List<ErrorDocumentationExtractionFailure> failures) {
        // Resolve by hand rather than Type.GetMethod(name, flags), which throws AmbiguousMatchException when the name
        // is overloaded. We only ever invoke a parameterless static method with no type arguments, so we filter to that
        // shape. Open generic definitions are excluded: `Foo()` and `Foo<T>()` are distinct, legal overloads that both
        // look parameterless via reflection, yet an open generic definition cannot be invoked without a type argument
        // and could never serve as a documentation factory — so it is never a candidate.
        List<MethodInfo> candidates = type
                                     .GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
                                     .Where(method => string.Equals(method.Name, methodName, StringComparison.Ordinal))
                                     .Where(method => method.GetParameters().Length == 0)
                                     .Where(method => !method.IsGenericMethodDefinition)
                                     .ToList();

        if (candidates.Count == 1) { return candidates[0]; }

        string reason = candidates.Count == 0
            ? $"No parameterless static method named '{methodName}' (referenced by [DocumentedBy]) was found."
            : $"Several parameterless methods named '{methodName}' were found; the [DocumentedBy] reference is ambiguous.";

        failures.Add(new ErrorDocumentationExtractionFailure(TypeName(type), methodName, reason, null));

        return null;
    }

    private static string? ResolveSourceDescription(ProvidesErrorsForAttribute providesErrorsFor) {
        string? description = providesErrorsFor.Description;

        // Without a resource type the description is literal text. With one, it is a resource key resolved (under the
        // current UI culture, which the worker sets per run) against that type's resources — the data-annotations
        // pattern. A missing resource or an unreadable manager falls back to the key text rather than failing.
        if (string.IsNullOrEmpty(description) || providesErrorsFor.DescriptionResourceType is null) {
            return description;
        }

        try {
            ResourceManager resources = new(providesErrorsFor.DescriptionResourceType);

            return resources.GetString(description) ?? description;
        } catch {
            return description;
        }
    }

    private static string TypeName(Type type) {
        return type.FullName ?? type.Name;
    }

    #endregion

}
