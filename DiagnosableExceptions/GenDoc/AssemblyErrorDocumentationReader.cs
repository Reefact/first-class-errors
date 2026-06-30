#region Usings declarations

using System.Reflection;

#endregion

namespace DiagnosableExceptions.GenDoc;

/// <summary>
///     Reads documented errors from an assembly and exposes enriched documentation metadata.
/// </summary>
public static class AssemblyErrorDocumentationReader {

    #region Statics members declarations

    /// <summary>
    ///     Extracts all documented errors from the provided assembly.
    /// </summary>
    /// <param name="assembly">The assembly to scan for documented errors.</param>
    /// <returns>An enumeration of <see cref="ErrorDocumentation" /> instances.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="assembly" /> is <c>null</c>.</exception>
    public static IEnumerable<ErrorDocumentation> GetErrorDocumentationFrom(Assembly assembly) {
        if (assembly is null) { throw new ArgumentNullException(nameof(assembly)); }

        return GetLoadableTypes(assembly)
              .Where(type => type is { IsClass: true })
              .SelectMany(BuildFromExceptionType)
               // Order before grouping so that, when several factories share the same Code, the surviving
               // documentation is chosen deterministically (reflection ordering is not guaranteed).
              .OrderBy(x => x.Code, StringComparer.OrdinalIgnoreCase)
              .ThenBy(x => x.Source, StringComparer.Ordinal)
              .GroupBy(x => x.Code, StringComparer.OrdinalIgnoreCase)
              .Select(g => g.First())
              .OrderBy(x => x.Code, StringComparer.OrdinalIgnoreCase);
    }

    private static IEnumerable<Type> GetLoadableTypes(Assembly assembly) {
        // A documented assembly may reference types that cannot be loaded (e.g. a missing or version-mismatched
        // dependency). GetTypes() then throws ReflectionTypeLoadException; we still want the loadable, attributed
        // types rather than aborting extraction for the whole assembly.
        try {
            return assembly.GetTypes();
        } catch (ReflectionTypeLoadException ex) {
            return ex.Types.Where(type => type is not null).Select(type => type!);
        }
    }

    private static IEnumerable<ErrorDocumentation> BuildFromExceptionType(Type exceptionType) {
        ProvidesErrorsForAttribute? providesErrorsFor = exceptionType.GetCustomAttribute<ProvidesErrorsForAttribute>();
        if (providesErrorsFor == null) { yield break; }

        foreach (MethodInfo factoryMethod in exceptionType.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)) {
            DocumentedByAttribute? documentedBy = factoryMethod.GetCustomAttribute<DocumentedByAttribute>();
            if (documentedBy is null) { continue; }

            MethodInfo? documentationMethod = exceptionType.GetMethod(documentedBy.MethodName, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            if (documentationMethod is null) { continue; }

            object? documentation = documentationMethod.Invoke(null, []);
            if (documentation is not ErrorDocumentation errorDocumentation) { continue; }

            errorDocumentation.Source = providesErrorsFor.Source;

            yield return errorDocumentation;
        }
    }

    #endregion

}