namespace Reefact.DiagnosableExceptions.Analyzer {

    public static class DiagnosticId {

        /// <summary>
        ///     Represents the diagnostic ID for the "Unused private member" rule in Roslyn analyzers.
        /// </summary>
        /// <remarks>
        ///     This diagnostic ID corresponds to the Roslyn rule <c>IDE0051</c>, which identifies unused private members in the
        ///     code.
        /// </remarks>
        public const string IDE0051 = "IDE0051";
        /// <summary>
        ///     Represents the diagnostic ID for the "Unused private members" rule in SonarQube analyzers.
        /// </summary>
        /// <remarks>
        ///     This diagnostic ID corresponds to the SonarQube rule <c>S1144</c>, which identifies unused private members in the
        ///     code.
        /// </remarks>
        public const string S1144 = "S1144";
        /// <summary>
        ///     Represents the diagnostic ID for the "Avoid uncalled private code" rule in Code Analysis.
        /// </summary>
        /// <remarks>
        ///     This diagnostic ID corresponds to the Code Analysis rule <c>CA1811</c>, which identifies private code that is never
        ///     called.
        /// </remarks>
        public const string CA1811 = "CA1811";

    }

}