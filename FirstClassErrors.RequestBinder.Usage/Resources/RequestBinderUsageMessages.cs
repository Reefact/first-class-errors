#region Usings declarations

using System.Resources;

#endregion

namespace FirstClassErrors.RequestBinder.Usage.Resources;

/// <summary>
///     The localized text of the sample's errors (titles, explanations, rules, diagnostics and public messages).
///     Values are read from the <c>RequestBinderUsageMessages</c> resources for the current UI culture — which the
///     documentation worker sets per run — falling back to the neutral (English) resources, and finally to the key
///     itself. Mirrors <c>UsageErrorMessages</c> in <c>FirstClassErrors.Usage</c>.
/// </summary>
/// <remarks>
///     This type doubles as the <c>DescriptionResourceType</c> passed to <c>[ProvidesErrorsFor]</c>: the extractor
///     resolves a source group's <c>Description</c> key against these same resources.
/// </remarks>
internal static class RequestBinderUsageMessages {

    #region Statics members declarations

    private static readonly ResourceManager Resources = new(typeof(RequestBinderUsageMessages));

    /// <summary>Returns the localized string for <paramref name="key" /> in the current UI culture.</summary>
    public static string Get(string key) {
        return Resources.GetString(key) ?? key;
    }

    #endregion

}
