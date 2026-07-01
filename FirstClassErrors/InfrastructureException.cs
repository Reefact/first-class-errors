namespace FirstClassErrors;

/// <summary>
///     Represents an application exception that originates from technical concerns outside the domain model, including
///     infrastructure, integration, and adaptation logic.
/// </summary>
/// <remarks>
///     <para>
///         An <see cref="InfrastructureException" /> indicates that the system could not complete an operation due to a
///         technical failure unrelated to domain rules or business logic. These exceptions arise from the system’s
///         interaction with its technical environment or from transformation layers that connect the domain to that
///         environment.
///     </para>
///     <para>
///         This type includes failures related to:
///     </para>
///     <list type="bullet">
///         <item>External dependencies (databases, filesystems, network services, APIs)</item>
///         <item>Messaging and transport mechanisms</item>
///         <item>Serialization, parsing, or format handling</item>
///         <item>Mapping or transformation between technical and domain models</item>
///         <item>Configuration or environment setup</item>
///     </list>
///     <para>
///         Unlike <see cref="DomainException" />, infrastructure exceptions represent technical or environmental
///         conditions rather than incorrect business state. They may be transient or permanent depending on the failure.
///     </para>
///     <para>
///         <b>Authoring guidance for derived exceptions:</b>
///     </para>
///     <list type="bullet">
///         <item>The message should describe the technical operation that failed.</item>
///         <item>Do not encode domain rule violations in this type.</item>
///         <item>Use this class to clearly separate technical failures from domain errors.</item>
///     </list>
/// </remarks>
public class InfrastructureException : DiagnosableException {

    #region Constructors declarations

    /// <summary>
    ///     Initializes a new instance of the <see cref="InfrastructureException" /> class with the specified
    ///     <see cref="InfrastructureError" />.
    /// </summary>
    /// <param name="error">The <see cref="InfrastructureError" /> that describes the error.</param>
    public InfrastructureException(InfrastructureError error) : base(error) { }

    #endregion

}