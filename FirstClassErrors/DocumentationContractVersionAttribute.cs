[assembly: FirstClassErrors.DocumentationContractVersion(FirstClassErrors.DocumentationContractVersionAttribute.CurrentVersion)]

namespace FirstClassErrors;

/// <summary>
///     Stamps the assembly with the version of the error-documentation <em>contract</em> it produces — the shape the
///     documentation generator (<c>fce</c>) reads by reflection: the <c>[ProvidesErrorsFor]</c> / <c>[DocumentedBy]</c>
///     attributes, the <c>DescribeError</c> DSL, and the resulting documentation model.
/// </summary>
/// <remarks>
///     Increment <see cref="CurrentVersion" /> ONLY when a change breaks that contract, independently of the library's
///     SemVer: a breaking contract change need not coincide with a major library release, and a major library release
///     need not break the contract. The generator reads this value from the target assembly to detect a contract it was
///     not built to extract; an assembly published without the attribute is treated as contract version 1, the original.
///     The attribute is internal and consumer-invisible — it is diagnostic metadata for the tooling, not public API.
/// </remarks>
[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = false)]
internal sealed class DocumentationContractVersionAttribute : Attribute {

    /// <summary>The documentation-contract version this build of the library produces.</summary>
    internal const int CurrentVersion = 1;

    public DocumentationContractVersionAttribute(int version) {
        Version = version;
    }

    /// <summary>The declared documentation-contract version.</summary>
    public int Version { get; }

}
