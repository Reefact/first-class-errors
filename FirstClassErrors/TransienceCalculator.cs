namespace FirstClassErrors;

/// <summary>
///     Computes the overall <see cref="Transience" /> of a collection of errors.
/// </summary>
/// <remarks>
///     This helper centralizes the transience resolution rules so that the various inner-error collections
///     (for instance <see cref="PrimaryPortInnerErrors" /> and <see cref="SecondaryPortInnerErrors" />) share a
///     single, authoritative implementation rather than duplicating the logic.
/// </remarks>
internal static class TransienceCalculator {

    #region Statics members declarations

    /// <summary>
    ///     Computes the overall transience of the specified errors.
    /// </summary>
    /// <param name="errors">The errors to evaluate.</param>
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
    ///     Only errors of type <see cref="InfrastructureError" /> are taken into account; the transience is determined
    ///     based on their <see cref="InfrastructureError.Transience" /> property.
    /// </remarks>
    public static Transience Compute(IEnumerable<Error> errors) {
        IEnumerable<InfrastructureError> infraErrors = errors.OfType<InfrastructureError>();

        bool hasTransient = false;

        foreach (InfrastructureError error in infraErrors) {
            switch (error.Transience) {
                case Transience.NonTransient: return Transience.NonTransient;
                case Transience.Transient:    hasTransient = true; break;
            }
        }

        return hasTransient
            ? Transience.Transient
            : Transience.Unknown;
    }

    #endregion

}
