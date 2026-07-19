#region Usings declarations

using FsCheck;
using FsCheck.Fluent;

using JetBrains.Annotations;

#endregion

namespace FirstClassErrors.RequestBinder.PropertyTests;

/// <summary>
///     Property-based (fuzzing-style) invariants for the request binder's list binding. Where the example-based
///     unit tests pin a handful of hand-picked cases, these assert the binder's defining guarantees across a wide
///     range of randomly generated lists: the collect-all contract — one recorded error per failing element, never
///     fewer, never more — and the positional stability of the indexed error paths, so that reordering the elements
///     reorders their paths accordingly and an element's path never depends on its neighbours.
/// </summary>
[TestSubject(typeof(ListOfSimplePropertiesConverter<>))]
public sealed class ListBindingInvariantTests {

    [Fact(DisplayName = "Collect-all: a required list records exactly one error per failing element, and none for a valid one.")]
    public void CollectAllRecordsExactlyOneErrorPerFailingElement() {
        Prop.ForAll(BinderGen.Slots().ToArbitrary(),
                    slots => {
                        int                           expectedFailures = slots.Count(slot => slot.Fails);
                        Outcome<IReadOnlyList<Token>> outcome          = BindTokens(slots);

                        return expectedFailures == 0
                                   ? outcome.IsSuccess && outcome.GetResultOrThrow().Count == slots.Length
                                   : outcome.IsFailure && outcome.Error!.InnerErrors.Count == expectedFailures;
                    })
            .QuickCheckThrowOnFailure();
    }

    [Fact(DisplayName = "Indexed paths are exactly the positions of the failing elements — nothing more, nothing less.")]
    public void ErrorPathsAreExactlyThePositionsOfFailingElements() {
        Prop.ForAll(BinderGen.Slots().ToArbitrary(),
                    slots => FailingPaths(BindTokens(slots)).SetEquals(PositionsOfFailures(slots)))
            .QuickCheckThrowOnFailure();
    }

    [Fact(DisplayName = "Reordering the elements reorders their error paths accordingly: a path tracks position, never content or neighbours.")]
    public void ReorderingElementsReordersTheirErrorPathsAccordingly() {
        Prop.ForAll(BinderGen.Slots().ToArbitrary(),
                    slots => {
                        // AsEnumerable() pins the LINQ Reverse (a reversed copy). A bare slots.Reverse() binds instead
                        // to the void, in-place MemoryExtensions.Reverse(Span<T>) on the net472 floor, where
                        // System.Memory is in the graph (see build/Net472TestFloor.props).
                        (string? Raw, bool Fails)[] reversed = slots.AsEnumerable().Reverse().ToArray();

                        return FailingPaths(BindTokens(slots)).SetEquals(PositionsOfFailures(slots))
                            && FailingPaths(BindTokens(reversed)).SetEquals(PositionsOfFailures(reversed));
                    })
            .QuickCheckThrowOnFailure();
    }

    #region Statics members declarations

    /// <summary>Binds the generated tokens as a <b>required</b> list and returns the whole outcome.</summary>
    private static Outcome<IReadOnlyList<Token>> BindTokens((string? Raw, bool Fails)[] slots) {
        TokenListRequest request = new(slots.Select(slot => slot.Raw).ToArray());
        var              binder  = Bind.Request(CommandError.Invalid);
        var              body    = binder.PropertiesOf(request);

        RequiredField<IReadOnlyList<Token>> tokens = body.ListOfSimpleProperties(r => r.Tokens).AsRequired(Token.Parse);

        return binder.New(scope => scope.Get(tokens));
    }

    /// <summary>The indexed paths a correct binder must record — one per failing slot, at that slot's position.</summary>
    private static ISet<string> PositionsOfFailures((string? Raw, bool Fails)[] slots) {
        return slots.Select((slot, index) => (slot, index))
                    .Where(entry => entry.slot.Fails)
                    .Select(entry => $"Tokens[{entry.index}]")
                    .ToHashSet();
    }

    /// <summary>The set of indexed argument paths actually recorded by a binding outcome (empty on success).</summary>
    private static ISet<string> FailingPaths(Outcome<IReadOnlyList<Token>> outcome) {
        if (outcome.IsSuccess) { return new HashSet<string>(); }

        return outcome.Error!.InnerErrors
                      .Select(ArgumentPathOf)
                      .Where(path => path is not null)
                      .Select(path => path!)
                      .ToHashSet();
    }

    private static string? ArgumentPathOf(Error error) {
        error.Context.ToNameDictionary().TryGetValue("RequestArgument", out object? path);

        return path as string;
    }

    #endregion

}
