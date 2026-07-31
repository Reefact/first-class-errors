#region Usings declarations

using System.Diagnostics;
using System.Reflection;

using NFluent;

#endregion

namespace JustDummies.UnitTests;

/// <summary>
///     The value-object convention, enforced by reflection over the whole library: a type marked
///     <c>[ValueObject]</c> is sealed, immutable, renders itself, and carries the full identity set —
///     <see cref="IEquatable{T}" />, both <c>Equals</c> overloads, <c>GetHashCode</c>, <c>ToString</c> behind a
///     <see cref="DebuggerDisplayAttribute" />, and <c>==</c>/<c>!=</c>.
/// </summary>
/// <remarks>
///     <para>
///         This exists because the gap it closes is silent. A reference type compares by identity when nobody writes
///         another answer, and nothing complains: not the compiler, not a test, not a reviewer reading a type that
///         calls itself a value in its own remarks. Two of this library's values shipped that way, and only the third
///         had its equality — because code happened to compare it with <c>==</c>, which forced the question. Nothing
///         forced it for the others.
///     </para>
///     <para>
///         The marker is what makes the rule enforceable without guessing: immutability alone would sweep in the
///         generators and the specifications, which are immutable recipes rather than values. Declaring a value is
///         therefore the decision, and this test is what holds it to it.
///     </para>
///     <para>
///         Structure is all reflection can settle, and it is the part that goes missing: whether two equal instances
///         really hash alike belongs to each type's own tests. What is checked here cannot be satisfied by accident.
///     </para>
/// </remarks>
public sealed class ValueObjectConventionTests {

    private static readonly Assembly LibraryAssembly = typeof(Any).Assembly;

    [Fact(DisplayName = "Every type declared a value object carries a full value identity.")]
    public void EveryDeclaredValueObjectCarriesAValueIdentity() {
        List<Type> values = LibraryAssembly.GetTypes()
                                           .Where(type => type.GetCustomAttribute<ValueObjectAttribute>() is not null)
                                           .OrderBy(type => type.Name, StringComparer.Ordinal)
                                           .ToList();

        // Guards the scan itself: a renamed attribute or a moved assembly would leave the enumeration empty and every
        // assertion below would pass vacuously. Emptiness is the failure mode; the exact count is not pinned, so
        // retiring a value never trips this instead of saying what really changed.
        Check.WithCustomMessage("No type is marked [ValueObject]; the scan lost its target.")
             .That(values).Not.IsEmpty();

        List<string> violations = [];
        foreach (Type value in values) {
            violations.AddRange(MissingFrom(value).Select(missing => $"{value.Name}: {missing}"));
        }

        Check.WithCustomMessage(
                  $"Value-object convention — {violations.Count} missing member(s) or property(ies):{Environment.NewLine}"
                + string.Join(Environment.NewLine, violations))
             .That(violations)
             .IsEmpty();
    }

    #region Per-type verification

    private static IEnumerable<string> MissingFrom(Type value) {
        // A struct yields a zero-initialized instance through its parameterless constructor, bypassing every
        // validating factory — which is why a value enforcing an invariant is a class in this repository.
        if (value.IsValueType) { yield return "is a struct; a value enforcing an invariant is a class here"; }

        // An unsealed value cannot keep equality symmetric: a subclass compares unequal to its base under one
        // direction of the comparison and equal under the other.
        if (!value.IsValueType && !value.IsSealed) { yield return "is not sealed"; }

        if (!typeof(IEquatable<>).MakeGenericType(value).IsAssignableFrom(value)) {
            yield return $"does not implement IEquatable<{value.Name}>";
        }

        if (!DeclaresMethod(value, nameof(Equals), typeof(object))) { yield return "does not override Equals(object)"; }
        if (!DeclaresMethod(value, nameof(GetHashCode))) { yield return "does not override GetHashCode()"; }

        // A value that does not render itself shows a debugger its type name, which is the one thing the reader
        // already knows. The attribute is what puts that rendering in front of them without expanding the instance.
        if (!DeclaresMethod(value, nameof(ToString))) { yield return "does not override ToString()"; }

        DebuggerDisplayAttribute? display = value.GetCustomAttribute<DebuggerDisplayAttribute>();
        if (display is null) {
            yield return "does not carry [DebuggerDisplay]";
        } else if (display.Value?.Contains(nameof(ToString)) != true) {
            yield return $"carries [DebuggerDisplay(\"{display.Value}\")] rather than forwarding to ToString()";
        }

        // The operator pair is the silent half of the contract: without it `a == b` compiles and compares references,
        // where a missing Equals would at least be visible to anyone reading the type.
        if (!DeclaresOperator(value, "op_Equality")) { yield return "does not define operator =="; }
        if (!DeclaresOperator(value, "op_Inequality")) { yield return "does not define operator !="; }

        foreach (string mutable in MutableStateOf(value)) { yield return mutable; }
    }

    private static IEnumerable<string> MutableStateOf(Type value) {
        foreach (FieldInfo field in value.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)) {
            if (!field.IsInitOnly) { yield return $"field '{field.Name}' is not readonly"; }
        }

        foreach (PropertyInfo property in value.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)) {
            if (property.SetMethod is not null) { yield return $"property '{property.Name}' has a setter"; }
        }
    }

    private static bool DeclaresMethod(Type value, string name, params Type[] parameters) {
        MethodInfo? declared = value.GetMethod(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance, null, parameters, null);

        return declared is not null && declared.DeclaringType == value;
    }

    private static bool DeclaresOperator(Type value, string name) {
        return value.GetMethods(BindingFlags.Public | BindingFlags.Static)
                    .Any(method => method.Name == name && method.GetParameters().Length == 2);
    }

    #endregion

}
