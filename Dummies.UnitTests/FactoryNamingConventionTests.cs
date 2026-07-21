#region Usings declarations

using System.Reflection;

using NFluent;

#endregion

namespace Dummies.UnitTests;

/// <summary>
///     Locks the factory-naming rule recorded in ADR-0031: every parameterless, type-named scalar factory
///     on <see cref="Any" /> is named after the CLR type it produces — which is also the name of its
///     <c>Any{ClrType}</c> builder. This is the guard that would have caught the <c>Bool</c>/<c>AnyBool</c>
///     deviation before release. The <see cref="Any" />↔<see cref="AnyContext" /> mirror itself is guarded
///     separately by <c>SurfaceParityTests</c>.
/// </summary>
public sealed class FactoryNamingConventionTests {

    // The type-named scalar factories are exactly Any's public, static, non-generic, parameterless methods
    // whose return type is a builder (implements IAny<T>). StringMatching (parameters), Enum<T> (generic),
    // the collection/composition factories (generic, parameterized) and WithSeed/Reproducibly (not builders)
    // fall out by construction, so no hand-maintained allow-list can drift out of sync with the surface.
    private static IEnumerable<MethodInfo> ScalarFactories() {
        return typeof(Any).GetMethods(BindingFlags.Public | BindingFlags.Static)
                          .Where(method => !method.IsGenericMethod
                                        && method.GetParameters().Length == 0
                                        && ElementTypeOf(method.ReturnType) is not null);
    }

    // The T of the single IAny<T> a builder implements, or null when the type is not a builder.
    private static Type? ElementTypeOf(Type builder) {
        return builder.GetInterfaces()
                      .FirstOrDefault(candidate => candidate.IsGenericType && candidate.GetGenericTypeDefinition() == typeof(IAny<>))
                      ?.GetGenericArguments()[0];
    }

    [Fact(DisplayName = "Every type-named scalar factory, and its builder, is named after the CLR type it produces.")]
    public void FactoriesAreNamedAfterTheirClrType() {
        List<MethodInfo> factories = ScalarFactories().ToList();

        // Guards the reflection itself: were the query ever to match nothing, every assertion below would pass vacuously.
        Check.That(factories.Count).IsStrictlyGreaterThan(15);

        foreach (MethodInfo factory in factories) {
            Type   builder = factory.ReturnType;
            string clrName = ElementTypeOf(builder)!.Name;

            Check.WithCustomMessage($"Any.{factory.Name}() returns {builder.Name} (IAny<{clrName}>); the factory must be named '{clrName}', after the CLR type it produces.")
                 .That(factory.Name).IsEqualTo(clrName);
            Check.WithCustomMessage($"The builder for {clrName} is named '{builder.Name}'; it must be 'Any{clrName}' to match the CLR type.")
                 .That(builder.Name).IsEqualTo("Any" + clrName);
        }
    }

}
