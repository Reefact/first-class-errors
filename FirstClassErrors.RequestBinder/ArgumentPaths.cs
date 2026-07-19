#region Usings declarations

using System.Reflection;

#endregion

namespace FirstClassErrors.RequestBinder;

/// <summary>
///     Resolves a converter's deferred argument path. A converter stage stores its path as a single
///     <see cref="object" /> field holding either the resolved <see cref="string" /> (an out-of-DTO argument's
///     verbatim name, or a path already built) or the selected <see cref="PropertyInfo" /> (a DTO property, whose
///     path — name provider, then prefix — is built <b>only when a path is first needed</b>: a recorded failure, a
///     complex property's nested prefix, or the misconfigured-fallback exception. An all-valid bind of scalar and
///     list-of-scalar properties never materializes one).
/// </summary>
/// <remarks>
///     The resolved string is written back into the field, so the provider is consulted at most once per bound
///     property — the same upper bound as when the path was built eagerly. Bindings are single-threaded by contract,
///     so the unsynchronized write-back is safe.
/// </remarks>
internal static class ArgumentPaths {

    #region Statics members declarations

    /// <summary>Resolves (and caches back) the path held by <paramref name="argumentPathOrProperty" />.</summary>
    /// <param name="argumentPathOrProperty">The converter's path field: a resolved <see cref="string" /> or a deferred <see cref="PropertyInfo" />.</param>
    /// <param name="binding">The binding whose options and prefix the path is resolved with.</param>
    /// <returns>The full argument path.</returns>
    internal static string Resolve(ref object argumentPathOrProperty, RequestBinding binding) {
        if (argumentPathOrProperty is string resolved) { return resolved; }

        string path = binding.PathOfProperty((PropertyInfo)argumentPathOrProperty);
        argumentPathOrProperty = path;

        return path;
    }

    #endregion

}
