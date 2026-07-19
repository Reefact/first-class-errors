// Compiler-required marker behind C# `init` accessors and records. The .NET Framework 4.7.2 BCL does not
// ship it, so this polyfill is compiled ONLY into net472 test builds (see build/Net472TestFloor.props). It is
// internal, so it never leaves the test assembly and each net472 test assembly gets its own. The shipped
// FirstClassErrors libraries use neither `init` nor records, so nothing in the product relies on this.

// ReSharper disable once CheckNamespace -- the compiler resolves this type by its exact fully-qualified name.
namespace System.Runtime.CompilerServices {

    internal static class IsExternalInit { }

}
