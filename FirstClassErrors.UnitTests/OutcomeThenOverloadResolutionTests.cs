#region Usings declarations

using System;
using System.Threading;
using System.Threading.Tasks;

using JetBrains.Annotations;

using NFluent;

#endregion

namespace FirstClassErrors.UnitTests;

/// <summary>
///     Locks the overload resolution of the unified <see cref="Outcome{T}.Then{TResult}(System.Func{T, TResult})" />
///     (value mapping) versus <see cref="Outcome{T}.Then{TResult}(System.Func{T, Outcome{TResult}})" /> (binding).
///     A regression that picked the value-mapping overload for an <c>Outcome</c>-returning function would either fail to
///     compile (the explicitly typed locals below) or change the runtime result type asserted here, turning a silent
///     <c>Outcome&lt;Outcome&lt;T&gt;&gt;</c> nesting into a red test.
/// </summary>
[TestSubject(typeof(Outcome<>))]
public sealed class OutcomeThenOverloadResolutionTests {

    #region Statics members declarations

    private static string          Map(int value)      => "v" + value;                          // Func<int, string>          -> value mapping
    private static Outcome<string> Bind(int value)     => Outcome<string>.Success("v" + value); // Func<int, Outcome<string>> -> binding (flatten)
    private static Outcome         BindUnit(int value) => Outcome.Success;                       // Func<int, Outcome>         -> non-generic binding

    #endregion

    [Fact(DisplayName = "Then binds to the value-mapping overload for a value-returning method group.")]
    public void ThenMapsWithAValueReturningMethodGroup() {
        Outcome<string> result = Outcome<int>.Success(3).Then(Map);

        Check.That(result.GetType()).IsEqualTo(typeof(Outcome<string>));
        Check.That(result.GetResultOrThrow()).IsEqualTo("v3");
    }

    [Fact(DisplayName = "Then flattens an Outcome-returning method group; it does not nest into Outcome<Outcome<T>>.")]
    public void ThenFlattensWithAnOutcomeReturningMethodGroup() {
        Outcome<string> result = Outcome<int>.Success(3).Then(Bind);

        Check.That(result.GetType()).IsEqualTo(typeof(Outcome<string>));
        Check.That(result.GetType()).IsNotEqualTo(typeof(Outcome<Outcome<string>>));
        Check.That(result.GetResultOrThrow()).IsEqualTo("v3");
    }

    [Fact(DisplayName = "Then maps with a value-returning lambda.")]
    public void ThenMapsWithAValueReturningLambda() {
        Outcome<string> result = Outcome<int>.Success(3).Then(value => "v" + value);

        Check.That(result.GetType()).IsEqualTo(typeof(Outcome<string>));
        Check.That(result.GetResultOrThrow()).IsEqualTo("v3");
    }

    [Fact(DisplayName = "Then flattens with an Outcome-returning lambda.")]
    public void ThenFlattensWithAnOutcomeReturningLambda() {
        Outcome<string> result = Outcome<int>.Success(3).Then(value => Outcome<string>.Success("v" + value));

        Check.That(result.GetType()).IsEqualTo(typeof(Outcome<string>));
        Check.That(result.GetResultOrThrow()).IsEqualTo("v3");
    }

    [Fact(DisplayName = "Then resolves to the non-generic bind for a function returning a non-generic Outcome.")]
    public void ThenResolvesToTheNonGenericBind() {
        Outcome result = Outcome<int>.Success(3).Then(BindUnit);

        Check.That(result.GetType()).IsEqualTo(typeof(Outcome));
        Check.That(result.IsSuccess).IsTrue();
    }

    [Fact(DisplayName = "Then chains a bind then a map into a single flattened outcome.")]
    public void ThenChainsBindThenMap() {
        Outcome<string> result = Outcome<int>.Success(3)
                                             .Then(Bind)
                                             .Then(value => value + "!");

        Check.That(result.GetType()).IsEqualTo(typeof(Outcome<string>));
        Check.That(result.GetResultOrThrow()).IsEqualTo("v3!");
    }

    [Fact(DisplayName = "Async Then maps with a value-returning asynchronous function.")]
    public async Task AsyncThenMapsWithAValueReturningFunction() {
        Outcome<string> result = await Outcome<int>.Success(3)
                                                   .Then(async (value, _) => {
                                                       await Task.Yield();

                                                       return "v" + value;
                                                   }, TestContext.Current.CancellationToken);

        Check.That(result.GetType()).IsEqualTo(typeof(Outcome<string>));
        Check.That(result.GetResultOrThrow()).IsEqualTo("v3");
    }

    [Fact(DisplayName = "Async Then flattens with an Outcome-returning asynchronous function.")]
    public async Task AsyncThenFlattensWithAnOutcomeReturningFunction() {
        Outcome<string> result = await Outcome<int>.Success(3)
                                                   .Then(async (value, _) => {
                                                       await Task.Yield();

                                                       return Outcome<string>.Success("v" + value);
                                                   }, TestContext.Current.CancellationToken);

        Check.That(result.GetType()).IsEqualTo(typeof(Outcome<string>));
        Check.That(result.GetResultOrThrow()).IsEqualTo("v3");
    }
}
