#region Usings declarations

using BenchmarkDotNet.Running;

#endregion

namespace FirstClassErrors.RequestBinder.Benchmarks;

internal static class Program {

    private static void Main(string[] args) {
        BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
    }

}
