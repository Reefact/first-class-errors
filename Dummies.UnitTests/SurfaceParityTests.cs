#region Usings declarations

using System.Reflection;
using System.Threading.Tasks;

using NFluent;

#endregion

namespace Dummies.UnitTests;

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
    // SetOf, DictionaryOf, PairOf, ...), as well as WithSeed (returns AnyContext) and Reproducibly (returns
    // void/Task) — none of which AnyContext is meant to mirror.
    private static bool IsScalarFactory(MethodInfo method) {
        if (method.GetParameters().Any(parameter => IsAny(parameter.ParameterType))) { return false; }

        Type returnType = method.ReturnType;

        return returnType != typeof(AnyContext)
            && returnType != typeof(void)
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
    // test compares it against what each builder actually exposes through reflection.
    private static readonly string[] SignedNumericAlgebra = [
        "Positive", "Negative", "Zero", "NonZero",
        "GreaterThan", "GreaterThanOrEqualTo", "LessThan", "LessThanOrEqualTo",
        "Between", "OneOf", "Except", "DifferentFrom"
    ];

    // Unsigned integers drop Positive/Negative (meaningless there — NonZero carries the intent).
    private static readonly string[] UnsignedNumericAlgebra = [
        "Zero", "NonZero",
        "GreaterThan", "GreaterThanOrEqualTo", "LessThan", "LessThanOrEqualTo",
        "Between", "OneOf", "Except", "DifferentFrom"
    ];

    // Instant-like builders rename the bound family to domain vocabulary, with identical inclusive/exclusive
    // semantics, and carry no Positive/Negative/Zero (an instant has no sign).
    private static readonly string[] InstantAlgebra = [
        "After", "AfterOrEqualTo", "Before", "BeforeOrEqualTo",
        "Between", "OneOf", "Except", "DifferentFrom"
    ];

    public static IEnumerable<object[]> Builders() {
        // Signed integers, the continuous/decimal builders, and TimeSpan (a signed magnitude) share the full algebra.
        yield return [typeof(AnyInt32), SignedNumericAlgebra];
        yield return [typeof(AnySByte), SignedNumericAlgebra];
        yield return [typeof(AnyInt16), SignedNumericAlgebra];
        yield return [typeof(AnyInt64), SignedNumericAlgebra];
        yield return [typeof(AnyDouble), SignedNumericAlgebra];
        yield return [typeof(AnySingle), SignedNumericAlgebra];
        yield return [typeof(AnyDecimal), SignedNumericAlgebra];
        yield return [typeof(AnyTimeSpan), SignedNumericAlgebra];

        yield return [typeof(AnyByte), UnsignedNumericAlgebra];
        yield return [typeof(AnyUInt16), UnsignedNumericAlgebra];
        yield return [typeof(AnyUInt32), UnsignedNumericAlgebra];
        yield return [typeof(AnyUInt64), UnsignedNumericAlgebra];

        yield return [typeof(AnyDateTime), InstantAlgebra];
        yield return [typeof(AnyDateTimeOffset), InstantAlgebra];

        // The remaining scalar builders each carry their own deliberate set.
        yield return [typeof(AnyBoolean), new[] { "True", "False", "DifferentFrom" }];
        yield return [typeof(AnyGuid), new[] { "NonEmpty", "Empty", "OneOf", "Except", "DifferentFrom" }];
        yield return [typeof(AnyEnum<DayOfWeek>), new[] { "OneOf", "Except", "DifferentFrom" }];
        yield return [typeof(AnyChar), new[] { "Alpha", "AlphaNumeric", "Numeric", "UpperCase", "LowerCase", "OneOf", "Except", "DifferentFrom" }];

        // AnyString carries the exclusion pair Except/DifferentFrom (met by a bounded redraw, since strings are not
        // ordinal-mapped). Its OneOf is terminal — it returns AnyStringOneOf, a different type, so it is not a
        // self-returning constraint and does not appear in this fluent-method set.
        yield return [typeof(AnyString), new[] {
            "NonEmpty", "WithLength", "WithMinLength", "WithMaxLength", "WithLengthBetween",
            "StartingWith", "EndingWith", "Containing", "Alpha", "AlphaNumeric", "Numeric", "UpperCase", "LowerCase",
            "Except", "DifferentFrom"
        }];

#if NET8_0_OR_GREATER
        yield return [typeof(AnyInt128), SignedNumericAlgebra];
        yield return [typeof(AnyHalf), SignedNumericAlgebra];
        yield return [typeof(AnyUInt128), UnsignedNumericAlgebra];
        yield return [typeof(AnyDateOnly), InstantAlgebra];
        yield return [typeof(AnyTimeOnly), InstantAlgebra];
#endif
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
