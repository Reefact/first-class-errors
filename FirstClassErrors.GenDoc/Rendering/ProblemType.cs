#region Usings declarations

using System.Text;

#endregion

namespace FirstClassErrors.GenDoc.Rendering;

/// <summary>
///     Builds the RFC 9457 problem <c>type</c> for a documented error, as a stable <c>urn:problem</c> URN whose
///     segments are the service name and the error code, each slugified to lowercase kebab-case
///     (for example <c>urn:problem:temperature-simulator:temperature-below-absolute-zero</c>).
/// </summary>
/// <remarks>
///     The service name is chosen by the application and supplied through the <c>--service-name</c> option (or the
///     <c>serviceName</c> configuration value). A type is only built when both a service name and a code are present;
///     the CLI rejects a missing service name up front for the formats that emit examples, so the generated
///     documentation always carries a full <c>urn:problem:{service}:{code}</c>.
/// </remarks>
internal static class ProblemType {

    #region Statics members declarations

    /// <summary>
    ///     Builds the problem <c>type</c> URN from a <paramref name="serviceName" /> and an error <paramref name="code" />.
    /// </summary>
    /// <param name="serviceName">The service name the error is exposed under.</param>
    /// <param name="code">The error code.</param>
    /// <returns>
    ///     The <c>urn:problem:{service}:{code}</c> type, or <c>null</c> when either the service name or the code is
    ///     missing (no meaningful type can be built).
    /// </returns>
    public static string? For(string? serviceName, string? code) {
        string service    = Slugify(serviceName);
        string identifier = Slugify(code);

        // Both segments are required: a problem type is only meaningful with a service (chosen by the application) and a
        // code. The CLI rejects a missing service name for the formats that emit examples, so in practice the generated
        // documentation always carries a full type; here a missing segment simply yields no type.
        if (service.Length == 0 || identifier.Length == 0) { return null; }

        return $"urn:problem:{service}:{identifier}";
    }

    /// <summary>
    ///     Reduces a value to lowercase kebab-case: ASCII letters and digits are kept, every other run collapses to a
    ///     single hyphen, and leading/trailing hyphens are trimmed. Returns an empty string for a <c>null</c>/blank value.
    /// </summary>
    private static string Slugify(string? value) {
        if (string.IsNullOrWhiteSpace(value)) { return string.Empty; }

        StringBuilder builder  = new(value!.Length);
        bool          lastDash = false;
        foreach (char character in value.Trim().ToLowerInvariant()) {
            if (character is (>= 'a' and <= 'z') or (>= '0' and <= '9')) {
                builder.Append(character);
                lastDash = false;
            } else if (lastDash is false && builder.Length > 0) {
                builder.Append('-');
                lastDash = true;
            }
        }

        while (builder.Length > 0 && builder[^1] == '-') { builder.Length--; }

        return builder.ToString();
    }

    #endregion

}
