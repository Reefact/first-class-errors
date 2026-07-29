#region Usings declarations

using System.Reflection;
using System.Threading.Tasks;

using NFluent;

#endregion

namespace JustDummies.UnitTests;

/// <summary>
///     Structural guards over the library's two hand-mirrored surfaces. Both are pure reflection, so they add no
///     per-builder maintenance beyond the expectation table encoded here:
///     <list type="number">
///         <item>
///             <b>Mirror parity.</b> Every scalar factory on the static <see cref="Any" /> entry point has an
///             identical instance counterpart on <see cref="AnyContext" />. A scalar factory added to one surface and
///             forgotten on the other would compile and pass every behavioral test, silently shipping a hole in the
///             deterministic surface.
///         </item>
///         <item>
///             <b>Algebra parity.</b> Each builder exposes exactly the constraint method set its family declares. A
///             renamed or missing constraint on one of the cloned numeric or temporal builders would otherwise slip
///             past the copy-paste discipline that keeps the duplication safe.
///         </item>
///     </list>
///     Composition and collection factories (<c>Combine</c>, <c>ListOf</c>, <c>DictionaryOf</c>, ...) are deliberately
///     <b>not</b> mirrored onto <see cref="AnyContext" />: they inherit the context through their operand sources, so
///     the mirror guard excludes them by construction (they take an <see cref="IAny{T}" /> operand).
/// </summary>
public sealed class SurfaceParityTests {

    #region Mirror parity: Any <-> AnyContext

    [Fact(DisplayName = "Every Any scalar factory has an identical AnyContext counterpart.")]
    public void AnyAndAnyContextExposeTheSameScalarFactories() {
        HashSet<string> onAny = typeof(Any)
                                .GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly)
                                .Where(IsScalarFactory)
                                .Select(Signature)
                                .ToHashSet();

        HashSet<string> onContext = typeof(AnyContext)
                                    .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                                    .Where(method => !method.IsSpecialName) // drops the Seed property getter
                                    .Select(Signature)
                                    .ToHashSet();

        string[] onlyOnAny     = onAny.Except(onContext).OrderBy(signature => signature, StringComparer.Ordinal).ToArray();
        string[] onlyOnContext = onContext.Except(onAny).OrderBy(signature => signature, StringComparer.Ordinal).ToArray();

        Check.WithCustomMessage($"Scalar factories only on Any: [{string.Join(", ", onlyOnAny)}]; only on AnyContext: [{string.Join(", ", onlyOnContext)}].")
             .That(onlyOnAny.Length + onlyOnContext.Length)
             .IsEqualTo(0);
    }

    // A scalar factory produces a generator from the context's own source: it returns a builder and takes no
    // IAny<> operand. That excludes the composition/collection factories that live only on Any (Combine, ListOf,
    // SetOf, DictionaryOf, PairOf, ...), as well as the three ways to control seeding — WithSeed (returns
    // AnyContext), Reproducibly (returns void/Task) and UseSeed (returns IDisposable). None of those is a
    // generator factory, and AnyContext is not meant to mirror them: it already *is* an explicit deterministic
    // context, so pinning a seed on one would be meaningless.
    private static bool IsScalarFactory(MethodInfo method) {
        if (method.GetParameters().Any(parameter => IsAny(parameter.ParameterType))) { return false; }

        Type returnType = method.ReturnType;

        return returnType != typeof(AnyContext)
            && returnType != typeof(void)
            && returnType != typeof(IDisposable)
            && !typeof(Task).IsAssignableFrom(returnType);
    }

    private static bool IsAny(Type type) {
        return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IAny<>);
    }

    // Name + generic arity + parameter types + return type, ignoring the static/instance distinction so the two
    // surfaces line up. A drift in any of those four dimensions moves the signature and fails the guard.
    private static string Signature(MethodInfo method) {
        string parameters = string.Join(", ", method.GetParameters().Select(parameter => parameter.ParameterType.Name));

        return $"{method.Name}`{method.GetGenericArguments().Length}({parameters}) -> {method.ReturnType.Name}";
    }

    #endregion

    #region Algebra parity: per-family constraint sets

    // The constraint vocabulary each family declares, encoded once as data. This table is the specification; the
    // test compares it against what each builder actually exposes through reflection. Ordering here is irrelevant —
    // the test compares sets. The lattice constraint splits what was once one signed-numeric family: only the
    // integers carry MultipleOf, only Decimal carries WithScale, only the temporals carry WithGranularity.

    // Signed integers: the bound/sign vocabulary plus the integer lattice MultipleOf.
    private static readonly string[] SignedIntegerAlgebra = [
        "Positive", "Negative", "Zero", "NonZero",
        "GreaterThan", "GreaterThanOrEqualTo", "LessThan", "LessThanOrEqualTo",
        "Between", "MultipleOf", "OneOf", "Except", "DifferentFrom"
    ];

    // Unsigned integers drop Positive/Negative (meaningless there — NonZero carries the intent); they keep MultipleOf.
    private static readonly string[] UnsignedIntegerAlgebra = [
        "Zero", "NonZero",
        "GreaterThan", "GreaterThanOrEqualTo", "LessThan", "LessThanOrEqualTo",
        "Between", "MultipleOf", "OneOf", "Except", "DifferentFrom"
    ];

    // Binary floating-point carries the full signed vocabulary but no lattice: a grid of 10^-n over binary floats is a
    // footgun (0.1 is not representable), so MultipleOf/WithScale are deliberately withheld.
    private static readonly string[] FloatingPointAlgebra = [
        "Positive", "Negative", "Zero", "NonZero",
        "GreaterThan", "GreaterThanOrEqualTo", "LessThan", "LessThanOrEqualTo",
        "Between", "OneOf", "Except", "DifferentFrom"
    ];

    // Decimal is the signed vocabulary plus the decimal scale lattice WithScale.
    private static readonly string[] DecimalAlgebra = [
        "Positive", "Negative", "Zero", "NonZero",
        "GreaterThan", "GreaterThanOrEqualTo", "LessThan", "LessThanOrEqualTo",
        "Between", "OneOf", "Except", "DifferentFrom", "WithScale"
    ];

    // TimeSpan is a signed magnitude with a temporal granularity lattice WithGranularity.
    private static readonly string[] TimeSpanAlgebra = [
        "Positive", "Negative", "Zero", "NonZero",
        "GreaterThan", "GreaterThanOrEqualTo", "LessThan", "LessThanOrEqualTo",
        "Between", "OneOf", "Except", "DifferentFrom", "WithGranularity"
    ];

    // Instant-like builders rename the bound family to domain vocabulary, with identical inclusive/exclusive
    // semantics, and carry no Positive/Negative/Zero (an instant has no sign).
    private static readonly string[] InstantAlgebra = [
        "After", "AfterOrEqualTo", "Before", "BeforeOrEqualTo",
        "Between", "OneOf", "Except", "DifferentFrom"
    ];

    // Instants with sub-day tick precision also carry the temporal granularity lattice WithGranularity (DateOnly,
    // already day-resolution, keeps the plain InstantAlgebra).
    private static readonly string[] InstantWithGranularityAlgebra = [
        "After", "AfterOrEqualTo", "Before", "BeforeOrEqualTo",
        "Between", "OneOf", "Except", "DifferentFrom", "WithGranularity"
    ];

    // AnyDateTimeOffset additionally exposes the offset dimension (WithOffset/WithOffsetBetween) — the only instant
    // type carrying a second, offset dimension on top of the instant.
    private static readonly string[] InstantWithGranularityAndOffsetAlgebra = [
        "After", "AfterOrEqualTo", "Before", "BeforeOrEqualTo",
        "Between", "OneOf", "Except", "DifferentFrom", "WithGranularity", "WithOffset", "WithOffsetBetween"
    ];

    public static TheoryData<Type, string[]> Builders() {
        TheoryData<Type, string[]> data = new();

        // Signed integers carry MultipleOf; the binary floats do not; Decimal carries WithScale; TimeSpan (a signed
        // magnitude) carries WithGranularity — the lattice constraint is what forks the former shared signed family.
        data.Add(typeof(AnyInt32), SignedIntegerAlgebra);
        data.Add(typeof(AnySByte), SignedIntegerAlgebra);
        data.Add(typeof(AnyInt16), SignedIntegerAlgebra);
        data.Add(typeof(AnyInt64), SignedIntegerAlgebra);
        data.Add(typeof(AnyDouble), FloatingPointAlgebra);
        data.Add(typeof(AnySingle), FloatingPointAlgebra);
        data.Add(typeof(AnyDecimal), DecimalAlgebra);
        data.Add(typeof(AnyTimeSpan), TimeSpanAlgebra);

        data.Add(typeof(AnyByte), UnsignedIntegerAlgebra);
        data.Add(typeof(AnyUInt16), UnsignedIntegerAlgebra);
        data.Add(typeof(AnyUInt32), UnsignedIntegerAlgebra);
        data.Add(typeof(AnyUInt64), UnsignedIntegerAlgebra);

        data.Add(typeof(AnyDateTime), InstantWithGranularityAlgebra);
        data.Add(typeof(AnyDateTimeOffset), InstantWithGranularityAndOffsetAlgebra);

        // The remaining scalar builders each carry their own deliberate set.
        data.Add(typeof(AnyBoolean), new[] { "True", "False", "DifferentFrom" });
        data.Add(typeof(AnyGuid), new[] { "NonEmpty", "Empty", "OneOf", "Except", "DifferentFrom" });
        // AnyEnum adds AllowingCombinations, the opt-in widening the draw from the declared members to their
        // combinations — meaningful only for a [Flags] enum, hence a constraint rather than a second factory.
        data.Add(typeof(AnyEnum<DayOfWeek>), new[] { "AllowingCombinations", "OneOf", "Except", "DifferentFrom" });
        data.Add(typeof(AnyChar), new[] { "Alpha", "AlphaNumeric", "Numeric", "UpperCase", "LowerCase", "OneOf", "Except", "DifferentFrom" });

        // AnyString carries the exclusion pair Except/DifferentFrom (met by a bounded redraw, since strings are not
        // ordinal-mapped) and, like every other family, a composable OneOf that returns the builder itself.
        data.Add(typeof(AnyString), new[] {
            "NonEmpty", "WithLength", "WithMinLength", "WithMaxLength", "WithLengthBetween",
            "StartingWith", "EndingWith", "Containing", "Alpha", "AlphaNumeric", "Numeric", "WithChars", "UpperCase", "LowerCase",
            "OneOf", "Except", "DifferentFrom"
        });

#if NET8_0_OR_GREATER
        data.Add(typeof(AnyInt128), SignedIntegerAlgebra);
        data.Add(typeof(AnyHalf), FloatingPointAlgebra);
        data.Add(typeof(AnyUInt128), UnsignedIntegerAlgebra);
        data.Add(typeof(AnyDateOnly), InstantAlgebra);
        data.Add(typeof(AnyTimeOnly), InstantWithGranularityAlgebra);
#endif

        return data;
    }

    [Theory(DisplayName = "Each builder exposes exactly its family's constraint method set.")]
    [MemberData(nameof(Builders))]
    public void BuilderExposesExactlyItsFamilyAlgebra(Type builder, string[] expected) {
        // A constraint method is fluent — it returns the builder itself. Generate() (returns the value) and the
        // explicit interface members (not public) are excluded automatically.
        HashSet<string> actual = builder
                                 .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                                 .Where(method => method.ReturnType == builder && !method.IsSpecialName)
                                 .Select(method => method.Name)
                                 .ToHashSet();

        string[] missing    = expected.Except(actual).OrderBy(name => name, StringComparer.Ordinal).ToArray();
        string[] unexpected = actual.Except(expected).OrderBy(name => name, StringComparer.Ordinal).ToArray();

        Check.WithCustomMessage($"{builder.Name} — missing: [{string.Join(", ", missing)}]; unexpected: [{string.Join(", ", unexpected)}].")
             .That(missing.Length + unexpected.Length)
             .IsEqualTo(0);
    }

    #endregion

}
