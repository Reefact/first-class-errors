// Compiler-required marker behind C# `init` accessors and records. The .NET Framework 4.7.2 BCL does not
// ship it, so this polyfill is compiled ONLY into net472 test builds (see build/Net472TestFloor.props). It is
// internal, so it never leaves the test assembly and each net472 test assembly gets its own. The shipped
// FirstClassErrors libraries use neither `init` nor records, so nothing in the product relies on this.

// ReSharper disable once CheckNamespace -- the compiler resolves this type by its exact fully-qualified name.
namespace System.Runtime.CompilerServices {

    // Empty BY CONTRACT. The compiler looks this type up by name and never reads a member, so there is
    // nothing to write and it cannot become an interface: it must be a class the compiler can bind to.
    // Sonar's S2094 only fires on the net472 inner build, which is the only one that compiles this file.
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Minor Code Smell", "S2094:Classes should not be empty",
                                                     Justification =
                                                         "A compiler-recognised marker type. Its emptiness is the specification: the compiler binds " +
                                                         "System.Runtime.CompilerServices.IsExternalInit by name to enable `init` accessors and records, " +
                                                         "and reads nothing from it. It cannot be an interface, and giving it a member would be noise.")]
    internal static class IsExternalInit { }

}
