#region Usings declarations

using System.Globalization;
using System.Runtime.CompilerServices;

#endregion

namespace FirstClassErrors.GenDoc.UnitTests;

internal static class ModuleInitializer {

    #region Statics members declarations

    // Pin the whole test assembly to the invariant culture so every snapshot is deterministic regardless of the
    // machine or thread culture the tests run on. This covers not only our own rendering but also Verify's
    // culture-sensitive scrubbers (e.g. its inline date detection, which parses candidate values with the ambient
    // culture). Individual tests still override CurrentUICulture to exercise the localized resources.
    [ModuleInitializer]
    internal static void Initialize() {
        CultureInfo.DefaultThreadCurrentCulture   = CultureInfo.InvariantCulture;
        CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentCulture                = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture              = CultureInfo.InvariantCulture;
    }

    #endregion

}
