#region Usings declarations

using System.Reflection;

using NFluent;

#endregion

namespace JustDummies.UnitTests;

/// <summary>
///     The size-guard convention, enforced by reflection over every generator the library exposes: a size a generator
///     must actually <b>produce</b> — an exact or minimum length or count — is refused above the ceiling, while a size
///     that only <b>caps</b> a draw accepts any non-negative value (ADR-0050).
/// </summary>
/// <remarks>
///     <para>
///         The rule this pins is a rule about how the API reads, so it is stated the way a reader states it: a method
///         whose name announces a maximum, or a parameter named <c>maximum</c>, declares a cap; everything else that
///         takes a length or a count declares something the generator has to materialize.
///     </para>
///     <para>
///         It is written as a convention rather than as one test per method for the reason ADR-0045 gives for the
///         null-argument guard: the defect it prevents is a <i>new</i> builder forgetting the rule, and only
///         reflection holds a member that does not exist yet. The overflow behind the original defect existed because
///         the same arithmetic had been made safe one line away and not at the second site — a rule applied by hand
///         is a rule applied unevenly. Discovery starts from the generator factories on <see cref="Any" />, so a
///         collection shape added later is covered with nothing to add here.
///     </para>
/// </remarks>
public sealed class SizeGuardConventionTests {

    /// <summary>The largest size a generator will be asked to produce; mirrored from the library, which keeps it internal.</summary>
    private const int MaxProducibleSize = 1_000_000;

    /// <summary>The parameter names the size constraints use across both surfaces.</summary>
    private static readonly string[] SizeParameterNames = ["length", "count", "minimum", "maximum"];

    [Fact(DisplayName = "Every size constraint ceilings what it must produce and leaves a pure cap uncapped.")]
    public void EverySizeConstraintGuardsItsArgumentsToTheConvention() {
        List<string> violations = [];
        List<object> generators = Generators().ToList();

        // An empty harvest would make every assertion below vacuously true — the classic way a convention test goes
        // green by testing nothing.
        Check.WithCustomMessage("No generator was harvested from Any's factories; the convention would assert nothing.")
             .That(generators)
             .Not.IsEmpty();

        foreach (object generator in generators) {
            foreach (MethodInfo method in SizeMethodsOf(generator.GetType())) {
                foreach (ParameterInfo parameter in method.GetParameters()) {
                    Verify(generator, method, parameter, violations);
                }
            }
        }

        Check.WithCustomMessage($"Size-guard convention — {violations.Count} deviation(s):{Environment.NewLine}"
                              + string.Join(Environment.NewLine, violations.OrderBy(line => line, StringComparer.Ordinal)))
             .That(violations)
             .IsEmpty();
    }

    #region Statics members declarations

    /// <summary>
    ///     One generator per shape that carries size constraints, obtained from <see cref="Any" />'s own factories so
    ///     that a shape added later is picked up here automatically. Generic factories are closed over
    ///     <see cref="int" />, and the item generators a collection factory needs are supplied from the same source.
    /// </summary>
    private static IEnumerable<object> Generators() {
        List<object> generators = [Any.String()];

        foreach (MethodInfo factory in typeof(Any).GetMethods(BindingFlags.Public | BindingFlags.Static)) {
            if (!factory.IsGenericMethodDefinition) { continue; }
            if (factory.GetGenericArguments().Length is not (1 or 2)) { continue; }
            if (!TryClose(factory, out MethodInfo closed)) { continue; }
            if (!TryBuildArguments(closed, out object[] arguments)) { continue; }

            object? generator = closed.Invoke(null, arguments);
            if (generator is not null && SizeMethodsOf(generator.GetType()).Any()) { generators.Add(generator); }
        }

        return generators;
    }

    /// <summary>
    ///     Closes a generic factory over <see cref="int" />, or reports that it cannot be: a factory constrained to
    ///     something else (<c>Any.Enum&lt;TEnum&gt;</c>) carries no size constraint, so skipping it costs no coverage.
    /// </summary>
    private static bool TryClose(MethodInfo factory, out MethodInfo closed) {
        try {
            closed = factory.MakeGenericMethod(Enumerable.Repeat(typeof(int), factory.GetGenericArguments().Length).ToArray());

            return true;
        } catch (ArgumentException) {
            closed = null!;

            return false;
        }
    }

    /// <summary>
    ///     The arguments a closed factory needs, when every one of them is an item generator this test can supply.
    ///     A factory asking for anything else is skipped rather than guessed at — it carries no size constraint the
    ///     convention would reach.
    /// </summary>
    private static bool TryBuildArguments(MethodInfo factory, out object[] arguments) {
        List<object> built = [];
        foreach (ParameterInfo parameter in factory.GetParameters()) {
            if (parameter.ParameterType != typeof(IAny<int>)) {
                arguments = [];

                return false;
            }

            built.Add(Any.Int32());
        }

        arguments = built.ToArray();

        return true;
    }

    /// <summary>
    ///     Every public method of a generator whose parameters are all sizes — the constraints this convention
    ///     governs. Inherited members are deliberately included: the collection shapes take
    ///     <c>WithCount</c>/<c>WithMinCount</c>/<c>WithMaxCount</c> from their shared base, so excluding them would
    ///     leave every shape but <see cref="AnyDictionary{TKey,TValue}" /> unchecked.
    /// </summary>
    private static IEnumerable<MethodInfo> SizeMethodsOf(Type type) {
        return type.GetMethods(BindingFlags.Public | BindingFlags.Instance)
                   .Where(method => method.GetParameters().Length > 0
                                 && method.GetParameters().All(IsSizeParameter));
    }

    private static bool IsSizeParameter(ParameterInfo parameter) {
        return parameter.ParameterType == typeof(int)
            && parameter.Name is not null
            && SizeParameterNames.Contains(parameter.Name, StringComparer.Ordinal);
    }

    /// <summary>
    ///     Whether <paramref name="parameter" /> declares a pure cap: the parameter explicitly named <c>maximum</c>,
    ///     or the single size of a method that announces a maximum in its name.
    /// </summary>
    private static bool IsCap(MethodInfo method, ParameterInfo parameter) {
        return parameter.Name == "maximum" || method.Name.StartsWith("WithMax", StringComparison.Ordinal);
    }

    private static void Verify(object generator, MethodInfo method, ParameterInfo parameter, List<string> violations) {
        string member = $"{generator.GetType().Name}.{method.Name}(...) [param '{parameter.Name}']";

        // A negative size is a caller mistake on every parameter, cap or not — the rule this one extends.
        if (!Throws<ArgumentOutOfRangeException>(generator, method, parameter, -1)) {
            violations.Add($"{member}: a negative size was accepted; expected ArgumentOutOfRangeException.");
        }

        if (IsCap(method, parameter)) {
            // A cap costs nothing to honour, so it must stay declarable above the ceiling: refusing it here would
            // reject a legitimate bound mirroring a storage limit.
            if (Throws<ArgumentOutOfRangeException>(generator, method, parameter, MaxProducibleSize + 1)) {
                violations.Add($"{member}: a cap above the ceiling was refused; a maximum only narrows the draw and must accept any non-negative value.");
            }

            return;
        }

        if (!Throws<ArgumentOutOfRangeException>(generator, method, parameter, MaxProducibleSize + 1)) {
            violations.Add($"{member}: a produced size above the ceiling was accepted; expected ArgumentOutOfRangeException.");
        }

        if (Throws<ArgumentOutOfRangeException>(generator, method, parameter, MaxProducibleSize)) {
            violations.Add($"{member}: the ceiling itself was refused; it is an inclusive bound.");
        }
    }

    /// <summary>
    ///     Invokes <paramref name="method" /> with <paramref name="probe" /> on <paramref name="parameter" /> and the
    ///     neutral <c>0</c> everywhere else, reporting whether it threw <typeparamref name="TException" />. Zero is
    ///     what makes a two-bound call reach its own guards: the ordering check that would otherwise reject a crossed
    ///     pair runs after them, so the probe is the argument being judged.
    /// </summary>
    private static bool Throws<TException>(object generator, MethodInfo method, ParameterInfo probed, int probe)
        where TException : Exception {
        object[] arguments = method.GetParameters()
                                   .Select(parameter => (object)(parameter.Position == probed.Position ? probe : 0))
                                   .ToArray();

        try {
            method.Invoke(generator, arguments);

            return false;
        } catch (TargetInvocationException invocation) {
            return invocation.InnerException is TException;
        }
    }

    #endregion

}
