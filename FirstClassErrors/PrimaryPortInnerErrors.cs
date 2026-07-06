#region Usings declarations

using System.Diagnostics;

#endregion

namespace FirstClassErrors;

/// <summary>
///     Represents a collection of inner errors specific to primary port operations.
/// </summary>
/// <remarks>
///     This class provides methods to add domain-specific and primary port-specific errors to the collection.
///     A <c>null</c> error is ignored rather than rejected (manufacturing an error never throws), and the methods support
///     method chaining for convenience.
/// </remarks>
[DebuggerDisplay("{ToString()}")]
public sealed class PrimaryPortInnerErrors {

    #region Fields declarations

    private readonly List<Error> _errors = new();

    #endregion

    /// <summary>
    ///     Adds a domain-specific error to the collection of inner errors.
    /// </summary>
    /// <param name="error">
    ///     The <see cref="DomainError" /> instance representing the domain-specific error to add. If <c>null</c>, the call is
    ///     ignored.
    /// </param>
    /// <returns>
    ///     The current instance of <see cref="PrimaryPortInnerErrors" />, allowing for method chaining.
    /// </returns>
    /// <remarks>
    ///     A <c>null</c> <paramref name="error" /> is ignored rather than rejected (manufacturing an error never throws).
    /// </remarks>
    public PrimaryPortInnerErrors Add(DomainError error) {
        // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
        if (error != null) {
            _errors.Add(error);
        }

        return this;
    }

    /// <summary>
    ///     Adds a primary port-specific error to the collection of inner errors.
    /// </summary>
    /// <param name="error">
    ///     The <see cref="PrimaryPortError" /> instance representing the primary port-specific error to add. If <c>null</c>,
    ///     the call is ignored.
    /// </param>
    /// <returns>
    ///     The current instance of <see cref="PrimaryPortInnerErrors" />, allowing for method chaining.
    /// </returns>
    /// <remarks>
    ///     A <c>null</c> <paramref name="error" /> is ignored rather than rejected (manufacturing an error never throws).
    /// </remarks>
    public PrimaryPortInnerErrors Add(PrimaryPortError error) {
        // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
        if (error != null) {
            _errors.Add(error);
        }

        return this;
    }

    /// <summary>
    ///     Computes the transience of the current collection of errors.
    /// </summary>
    /// <returns>
    ///     A <see cref="Transience" /> value indicating the overall transience of the errors:
    ///     <list type="bullet">
    ///         <item>
    ///             <description><see cref="Transience.NonTransient" /> if any error is explicitly non-transient.</description>
    ///         </item>
    ///         <item>
    ///             <description>
    ///                 <see cref="Transience.Transient" /> if at least one error is transient and none are
    ///                 non-transient.
    ///             </description>
    ///         </item>
    ///         <item>
    ///             <description><see cref="Transience.Unknown" /> if no errors are classified as transient or non-transient.</description>
    ///         </item>
    ///     </list>
    /// </returns>
    /// <remarks>
    ///     This method evaluates all errors of type <see cref="InfrastructureError" /> in the collection.
    ///     It determines the transience based on the <see cref="InfrastructureError.Transience" /> property.
    /// </remarks>
    public Transience ComputeTransience() {
        IEnumerable<InfrastructureError> infraErrors = _errors.OfType<InfrastructureError>();

        bool hasTransient = false;

        foreach (InfrastructureError? error in infraErrors) {
            switch (error.Transience) {
                case Transience.NonTransient: return Transience.NonTransient;
                case Transience.Transient:    hasTransient = true; break;
            }
        }

        return hasTransient
            ? Transience.Transient
            : Transience.Unknown;
    }

    internal IReadOnlyList<Error> ToList() {
        return _errors;
    }

    /// <inheritdoc />
    public override string ToString() {
        return _errors.Count.ToString();
    }

}