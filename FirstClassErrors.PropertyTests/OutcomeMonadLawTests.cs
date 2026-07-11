#region Usings declarations

using FsCheck;
using FsCheck.Fluent;

using JetBrains.Annotations;

#endregion

namespace FirstClassErrors.PropertyTests;

/// <summary>
///     Property-based tests asserting that <see cref="Outcome{T}" /> obeys the monad laws and that a failure
///     propagates the original <see cref="Error" /> instance unchanged. These are the richest behavioural
///     invariants in the library, and the ones most worth checking against a wide range of randomly generated
///     pipelines rather than a few hand-written examples.
/// </summary>
/// <remarks>
///     Two outcomes are compared with <see cref="Equivalent" />: successes by their carried value, failures by
///     <b>reference identity</b> of their error. Reference identity is deliberate — the library propagates the
///     very same <see cref="Error" /> instance through <c>Then</c>/<c>To</c>/<c>Recover</c>, and these tests
///     lock that in.
/// </remarks>
[TestSubject(typeof(Outcome<>))]
public sealed class OutcomeMonadLawTests {

    [Fact(DisplayName = "Left identity: Success(a).Then(f) is equivalent to f(a).")]
    public void LeftIdentity() {
        var inputs = (from value in Ints()
                      from step in Steps()
                      select (value, step)).ToArbitrary();

        Prop.ForAll(inputs,
                    input => {
                        Func<int, Outcome<int>> f = ToFunction(input.step);

                        return Equivalent(Outcome<int>.Success(input.value).Then(f), f(input.value));
                    })
            .QuickCheckThrowOnFailure();
    }

    [Fact(DisplayName = "Right identity: m.Then(Success) is equivalent to m.")]
    public void RightIdentity() {
        Prop.ForAll(Outcomes().ToArbitrary(), outcome => Equivalent(outcome.Then(value => Outcome<int>.Success(value)), outcome))
            .QuickCheckThrowOnFailure();
    }

    [Fact(DisplayName = "Associativity: m.Then(f).Then(g) is equivalent to m.Then(x => f(x).Then(g)).")]
    public void Associativity() {
        var inputs = (from outcome in Outcomes()
                      from first in Steps()
                      from second in Steps()
                      select (outcome, first, second)).ToArbitrary();

        Prop.ForAll(inputs,
                    input => {
                        Func<int, Outcome<int>> f = ToFunction(input.first);
                        Func<int, Outcome<int>> g = ToFunction(input.second);

                        return Equivalent(input.outcome.Then(f).Then(g), input.outcome.Then(value => f(value).Then(g)));
                    })
            .QuickCheckThrowOnFailure();
    }

    [Fact(DisplayName = "Then on a failure short-circuits and propagates the original error instance.")]
    public void ThenPropagatesTheOriginalErrorOnFailure() {
        Prop.ForAll(Steps().ToArbitrary(),
                    step => {
                        Error                   error = AnError("origin");
                        Func<int, Outcome<int>> f     = ToFunction(step);
                        Outcome<int>            result = Outcome<int>.Failure(error).Then(f);

                        return result.IsFailure && ReferenceEquals(result.Error, error);
                    })
            .QuickCheckThrowOnFailure();
    }

    [Fact(DisplayName = "Functor identity: m.To(x => x) is equivalent to m.")]
    public void FunctorIdentity() {
        Prop.ForAll(Outcomes().ToArbitrary(), outcome => Equivalent(outcome.To(value => value), outcome))
            .QuickCheckThrowOnFailure();
    }

    [Fact(DisplayName = "To is a special case of Then: m.To(f) equals m.Then(x => Success(f(x))).")]
    public void MapIsBindWithSuccess() {
        var inputs = (from outcome in Outcomes()
                      from delta in Ints()
                      select (outcome, delta)).ToArbitrary();

        Prop.ForAll(inputs,
                    input => Equivalent(input.outcome.To(value => value + input.delta),
                                        input.outcome.Then(value => Outcome<int>.Success(value + input.delta))))
            .QuickCheckThrowOnFailure();
    }

    [Fact(DisplayName = "Recover on a success returns the same instance and never runs the fallback.")]
    public void RecoverOnSuccessIsIdentity() {
        Prop.ForAll(Ints().ToArbitrary(),
                    value => {
                        Outcome<int> success   = Outcome<int>.Success(value);
                        Outcome<int> recovered = success.Recover(_ => Outcome<int>.Failure(AnError("fallback")));

                        return ReferenceEquals(recovered, success);
                    })
            .QuickCheckThrowOnFailure();
    }

    [Fact(DisplayName = "Recover on a failure runs the fallback with the original error.")]
    public void RecoverOnFailureAppliesTheFallback() {
        Prop.ForAll(Ints().ToArbitrary(),
                    fallbackValue => {
                        Error  error    = AnError("origin");
                        Error? observed = null;

                        Outcome<int> recovered = Outcome<int>.Failure(error)
                                                            .Recover(caught => {
                                                                observed = caught;

                                                                return Outcome<int>.Success(fallbackValue);
                                                            });

                        return recovered.IsSuccess && recovered.GetResultOrThrow() == fallbackValue && ReferenceEquals(observed, error);
                    })
            .QuickCheckThrowOnFailure();
    }

    [Fact(DisplayName = "Finally folds a success to onSuccess(value) and a failure to onFailure(error).")]
    public void FinallyFoldsBothCases() {
        Prop.ForAll(Outcomes().ToArbitrary(),
                    outcome => {
                        (bool handledSuccess, int value) folded = outcome.Finally(value => (true, value), _ => (false, 0));

                        return outcome.IsSuccess ? folded == (true, outcome.GetResultOrThrow()) : folded == (false, 0);
                    })
            .QuickCheckThrowOnFailure();
    }

    #region Statics members declarations

    /// <summary>
    ///     Bounded integers, kept well inside <see cref="int" /> range so that the additions performed by the
    ///     generated steps never overflow.
    /// </summary>
    private static Gen<int> Ints() {
        return Gen.Choose(-1_000_000, 1_000_000);
    }

    /// <summary>
    ///     Generates the specification of a monadic step: how much it adds, whether it fails, and a seed used to
    ///     build a distinct error instance when it does.
    /// </summary>
    private static Gen<(int Add, bool Fail, int Seed)> Steps() {
        return from add in Ints()
               from fail in ArbMap.Default.GeneratorFor<bool>()
               from seed in Ints()
               select (Add: add, Fail: fail, Seed: seed);
    }

    /// <summary>
    ///     Generates an <see cref="Outcome{T}" /> that is either a success carrying a value or a failure carrying
    ///     a distinct error instance.
    /// </summary>
    private static Gen<Outcome<int>> Outcomes() {
        return Gen.OneOf(Ints().Select(value => Outcome<int>.Success(value)),
                         Ints().Select(seed => Outcome<int>.Failure(AnError("outcome" + seed))));
    }

    /// <summary>
    ///     Materializes a step specification into a function. A failing step always returns the same captured
    ///     error instance, so reference-identity propagation can be asserted.
    /// </summary>
    private static Func<int, Outcome<int>> ToFunction((int Add, bool Fail, int Seed) step) {
        Error error = AnError("step" + step.Seed);

        return value => step.Fail ? Outcome<int>.Failure(error) : Outcome<int>.Success(value + step.Add);
    }

    /// <summary>
    ///     Builds a distinct <see cref="DomainError" /> whose code embeds <paramref name="tag" />.
    /// </summary>
    private static DomainError AnError(string tag) {
        return DomainError.Create(ErrorCode.Create("ERR." + tag), "diagnostic " + tag).WithPublicMessage("summary " + tag);
    }

    /// <summary>
    ///     Structural equivalence for two outcomes: successes compare by value, failures by reference identity of
    ///     their error.
    /// </summary>
    private static bool Equivalent(Outcome<int> left, Outcome<int> right) {
        if (left.IsSuccess && right.IsSuccess) { return left.GetResultOrThrow() == right.GetResultOrThrow(); }
        if (left.IsFailure && right.IsFailure) { return ReferenceEquals(left.Error, right.Error); }

        return false;
    }

    #endregion

}
