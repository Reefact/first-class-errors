#region Usings declarations

using System.Reflection;

#endregion

namespace DiagnosableExceptions.GenDoc;

/// <summary>
///     Reads documented errors from an assembly and exposes enriched documentation metadata.
/// </summary>
public sealed class AssemblyErrorDocumentationReader {

    #region Statics members declarations

    /// <summary>
    ///     Extracts all documented errors from the provided assembly.
    /// </summary>
    /// <param name="assembly">The assembly to scan for documented errors.</param>
    /// <returns>An enumeration of <see cref="ErrorDocumentation" /> instances.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="assembly" /> is <c>null</c>.</exception>
    public static IEnumerable<ErrorDocumentation> GetErrorDocumentationFrom(Assembly assembly) {
        if (assembly is null) { throw new ArgumentNullException(nameof(assembly)); }

        return assembly.GetTypes()
                       .Where(type => type is { IsClass: true, IsAbstract: false } && typeof(DiagnosableException).IsAssignableFrom(type))
                       .SelectMany(BuildFromExceptionType)
                       .OrderBy(x => x.Code, StringComparer.OrdinalIgnoreCase);
    }

    private static IEnumerable<ErrorDocumentation> BuildFromExceptionType(Type exceptionType) {
        var   providesErrorsFor = exceptionType.GetCustomAttribute<ProvidesErrorsForAttribute>();
        Type? providedType      = providesErrorsFor?.OwnerType;

        foreach (MethodInfo factoryMethod in exceptionType.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)) {
            var documentedBy = factoryMethod.GetCustomAttribute<DocumentedByAttribute>();
            if (documentedBy is null) { continue; }

            MethodInfo? documentationMethod = exceptionType.GetMethod(documentedBy.MethodName, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            if (documentationMethod is null) { continue; }

            object? documentation = documentationMethod.Invoke(null, []);
            if (documentation is not ErrorDocumentation errorDocumentation) { continue; }

            errorDocumentation.Exception         = exceptionType;
            errorDocumentation.ErrorSource       = providedType;
            errorDocumentation.FactoryMethodName = factoryMethod.Name;

            yield return errorDocumentation;
        }
    }

    #endregion

}