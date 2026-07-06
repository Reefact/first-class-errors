#region Usings declarations

using System.Diagnostics;

#endregion

namespace FirstClassErrors;

/// <summary>
///     Represents a collection of errors that occurred within a secondary port.
/// </summary>
/// <remarks>
///     This class is designed to aggregate and manage errors related to secondary port operations.
///     It provides methods to add specific types of errors and retrieve them as a read-only list.
/// </remarks>
[DebuggerDisplay("{ToString()}")]
public sealed class SecondaryPortInnerErrors {

    #region Fields declarations

    private readonly List<Error> _errors = new();

    #endregion

    /// <summary>
    ///     Adds a <see cref="DomainError" /> to the collection of errors.
    /// </summary>
    /// <param name="error">
    ///     The <see cref="DomainError" /> instance to add. If <c>null</c>, the call is ignored.
    /// </param>
    /// <returns>
    ///     The current <see cref="SecondaryPortInnerErrors" /> instance, allowing for method chaining.
    /// </returns>
    /// <remarks>
    ///     This method appends a domain-specific error to the internal collection of errors.
    ///     A <c>null</c> <paramref name="error" /> is ignored rather than rejected (manufacturing an error never throws).
    /// </remarks>
    public SecondaryPortInnerErrors Add(DomainError error) {
        // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
        if (error != null) {
            _errors.Add(error);
        }

        return this;
    }

    /// <summary>
    ///     Adds a <see cref="SecondaryPortError" /> to the collection of errors.
    /// </summary>
    /// <param name="error">
    ///     The <see cref="SecondaryPortError" /> instance to add. If <c>null</c>, the call is ignored.
    /// </param>
    /// <returns>
    ///     The current <see cref="SecondaryPortInnerErrors" /> instance, allowing for method chaining.
    /// </returns>
    /// <remarks>
    ///     This method appends a secondary port error to the internal collection of errors.
    ///     A <c>null</c> <paramref name="error" /> is ignored rather than rejected (manufacturing an error never throws).
    /// </remarks>
    public SecondaryPortInnerErrors Add(SecondaryPortError error) {
        // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
        if (error != null) {
            _errors.Add(error);
        }

        return this;
    }

    /// <summary>
    ///     Retrieves the list of errors that have been added to the collection.
    /// </summary>
    /// <returns>
    ///     A read-only list of <see cref="Error" /> instances representing the errors in the collection.
    /// </returns>
    /// <remarks>
    ///     This method provides access to the internal collection of errors in a read-only format,
    ///     ensuring that the original collection remains unmodifiable.
    /// </remarks>
    internal IReadOnlyList<Error> ToList() {
        return _errors;
    }

    /// <summary>
    ///     Computes the overall transience of the errors contained within the current instance.
    /// </summary>
    /// <returns>
    ///     A <see cref="Transience" /> value that represents the transience of the errors:
    ///     <list type="bullet">
    ///         <item>
    ///             <description><see cref="Transience.NonTransient" /> if any error is explicitly marked as non-transient.</description>
    ///         </item>
    ///         <item>
    ///             <description>
    ///                 <see cref="Transience.Transient" /> if at least one error is marked as transient and none are
    ///                 non-transient.
    ///             </description>
    ///         </item>
    ///         <item>
    ///             <description><see cref="Transience.Unknown" /> if no errors are marked as transient or non-transient.</description>
    ///         </item>
    ///     </list>
    /// </returns>
    /// <remarks>
    ///     This method evaluates the transience of errors by iterating through the collection of
    ///     <see cref="InfrastructureError" /> instances. The transience is determined based on the
    ///     <see cref="InfrastructureError.Transience" /> property of each error.
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

    /// <inheritdoc />
    public override string ToString() {
        return _errors.Count.ToString();
    }

}