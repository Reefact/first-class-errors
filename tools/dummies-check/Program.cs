#region Usings declarations

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

using Dummies;

#endregion

namespace DummiesCheck;

// Packaged-asset compatibility consumer. Run once per consumer TFM by .github/workflows/dummies.yml.
// The consumer's compile target dictates which packaged asset NuGet restored, and therefore what this
// program must observe:
//   net8.0 consumer -> lib/net8.0 asset         -> modern generators present
//   net6.0 consumer -> lib/netstandard2.0 asset -> modern generators absent
// Any mismatch is a regression in packaging or conditional compilation; the program prints the offending
// asset moniker and exits non-zero so the workflow step turns red against the right asset.
internal static class Program {

#if NET8_0_OR_GREATER
    private const  bool   ExpectModernTypes = true;
    private const  string ConsumerTfm       = "net8.0";
    private const  string ExpectedFamily    = ".NETCoreApp";
#else
    private const  bool   ExpectModernTypes = false;
    private const  string ConsumerTfm       = "net6.0";
    private const  string ExpectedFamily    = ".NETStandard";
#endif

    // The net8.0-only generators, guarded by #if NET8_0_OR_GREATER in Dummies (Any.cs). Present on the net8.0
    // asset, absent on the netstandard2.0 asset — the exact conditional surface the acceptance criteria name.
    private static readonly string[] ModernEntryPoints = { "DateOnly", "TimeOnly", "Int128", "UInt128", "Half" };

    private static int Main() {
        Assembly dummies      = typeof(Any).Assembly;
        string   assetMoniker = dummies.GetCustomAttribute<TargetFrameworkAttribute>()?.FrameworkName ?? "(none)";

        // Machine-readable banner the workflow greps to prove which asset actually loaded — a program that
        // silently did nothing would otherwise exit 0. RESULT= is emitted last, once the checks have run.
        Console.WriteLine($"CONSUMER_TFM={ConsumerTfm}");
        Console.WriteLine($"ASSET={assetMoniker}");
        Console.WriteLine($"RUNTIME={RuntimeInformation.FrameworkDescription}");

        List<string> failures = new();

        // 1. The restored asset is the one this consumer TFM is meant to force.
        bool assetIsNetStandard = assetMoniker.IndexOf("NETStandard", StringComparison.OrdinalIgnoreCase) >= 0;
        if (assetIsNetStandard == ExpectModernTypes) {
            failures.Add($"wrong asset: consumer {ConsumerTfm} loaded '{assetMoniker}', expected a {ExpectedFamily} asset");
        }

        // 2. The conditional net8.0-only surface is present exactly on the net8.0 asset and absent otherwise.
        foreach (string name in ModernEntryPoints) {
            bool present = typeof(Any).GetMethod(name, BindingFlags.Public | BindingFlags.Static, binder: null, types: Type.EmptyTypes, modifiers: null) is not null;
            if (present != ExpectModernTypes) {
                failures.Add($"Any.{name}() is {(present ? "present" : "absent")} on '{assetMoniker}', expected {(ExpectModernTypes ? "present" : "absent")}");
            }
        }

        // 3. Smoke: the common public surface actually works when consumed from the package.
        RunSmoke(failures);

        if (failures.Count == 0) {
            Console.WriteLine($"RESULT=PASS asset={assetMoniker}");

            return 0;
        }

        Console.Error.WriteLine($"RESULT=FAIL asset={assetMoniker} failures={failures.Count}");
        foreach (string failure in failures) {
            Console.Error.WriteLine($"  - [asset={assetMoniker}] {failure}");
        }

        return 1;
    }

    private static void RunSmoke(List<string> failures) {
        // Scalars + constraints.
        int roll = Any.Int32().Between(1, 6).Generate();
        Require(failures, roll is >= 1 and <= 6, $"Int32().Between(1,6) produced {roll}");

        int positive = Any.Int32().Positive().Generate();
        Require(failures, positive > 0, $"Int32().Positive() produced {positive}");

        string capped = Any.String().NonEmpty().WithMaxLength(50).Generate();
        Require(failures, capped.Length is >= 1 and <= 50, $"String().NonEmpty().WithMaxLength(50) produced length {capped.Length}");

        double real = Any.Double().Between(0d, 1000d).Generate();
        Require(failures, real is >= 0d and <= 1000d, $"Double().Between(0,1000) produced {real.ToString("R", CultureInfo.InvariantCulture)}");

        // A contradiction in the Arrange must fail fast at declaration time (part of the library's contract):
        // the prefix alone requires 4 characters, so WithLength(3) cannot be satisfied.
        bool threw = false;
        try { Any.String().WithLength(3).StartingWith("ORD-"); } catch (ConflictingAnyConstraintException) { threw = true; }
        Require(failures, threw, "a contradictory String constraint did not throw ConflictingAnyConstraintException");

        // Composition through a factory (.As).
        string composed = Any.Int32().Between(1, 999).As(n => "ID-" + n.ToString(CultureInfo.InvariantCulture)).Generate();
        Require(failures, composed.StartsWith("ID-", StringComparison.Ordinal), $"As(...) produced '{composed}'");

        // Collections.
        List<int> list = Any.ListOf(Any.Int32().Between(0, 9)).WithCount(4).Generate();
        Require(failures, list.Count == 4 && list.All(value => value is >= 0 and <= 9), $"ListOf(...).WithCount(4) produced [{string.Join(",", list)}]");

        HashSet<int> set = Any.SetOf(Any.Int32().Between(0, 99)).WithCount(3).Generate();
        Require(failures, set.Count == 3, $"SetOf(...).WithCount(3) produced {set.Count} elements");

        // Seeded reproducibility: two contexts with the same seed replay an identical mixed sequence, and a
        // different seed diverges. This is the library's crown-jewel guarantee — verified here on each asset.
        string first  = SeedBatch(Any.WithSeed(20260719));
        string second = SeedBatch(Any.WithSeed(20260719));
        Require(failures, first == second, "same-seed contexts diverged");

        string other = SeedBatch(Any.WithSeed(987654321));
        Require(failures, first != other, "different-seed contexts produced identical sequences");
    }

    // Draws a fixed mixed sequence from the COMMON surface only (no modern types), so it compiles and runs
    // on both assets. Rendered with InvariantCulture to match the library's own culture-invariant rendering.
    private static string SeedBatch(AnyContext any) {
        List<string> parts = new() {
            any.Int32().Generate().ToString(CultureInfo.InvariantCulture),
            any.Int32().Between(1, 1000).Generate().ToString(CultureInfo.InvariantCulture),
            any.String().NonEmpty().WithMaxLength(50).Generate(),
            any.Int64().Generate().ToString(CultureInfo.InvariantCulture),
            any.UInt64().Generate().ToString(CultureInfo.InvariantCulture),
            any.Double().Between(0d, 1000d).Generate().ToString("R", CultureInfo.InvariantCulture),
            any.Decimal().Between(0m, 1000m).Generate().ToString(CultureInfo.InvariantCulture),
            any.Boolean().Generate().ToString(),
            any.Guid().Generate().ToString(),
            any.Char().Generate().ToString(),
            any.TimeSpan().Generate().Ticks.ToString(CultureInfo.InvariantCulture),
            any.DateTime().Generate().Ticks.ToString(CultureInfo.InvariantCulture)
        };

        return string.Join("|", parts);
    }

    private static void Require(List<string> failures, bool condition, string message) {
        if (!condition) { failures.Add(message); }
    }

}
