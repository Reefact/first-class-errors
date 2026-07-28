#region Usings declarations

using System.Text;

#endregion

namespace JustDummies;

/// <summary>
///     The carrier of a single generation: the seeded random generator to draw from, and the buffer the nodes write
///     into. <see cref="Append" /> enforces a hard length ceiling so no pattern can expand the buffer without bound —
///     whether through a nested unbounded quantifier (<c>(a+)+</c> and the like) or through bounded quantifiers whose
///     product is very large (<c>(a{1000}){1000}</c>). The value is built directly, never generated then retried, but
///     the buffer is still guarded.
/// </summary>
internal sealed class RegexGenerationContext {

    #region Fields declarations

    private readonly StringBuilder _builder = new();
    private readonly int           _limit;

    #endregion

    internal RegexGenerationContext(SeededRandom random, int limit) {
        if (random is null) { throw new ArgumentNullException(nameof(random)); }
        Random = random;
        _limit = limit;
    }

    internal SeededRandom Random { get; }

    internal void Append(char character) {
        if (_builder.Length >= _limit) {
            throw new AnyGenerationException($"The pattern produced a string longer than the {_limit}-character generation limit. This ceiling guards against runaway expansion; a pattern can reach it either through a nested unbounded quantifier (such as \"(a+)+\") or through bounded quantifiers whose product is very large (such as \"(a{{1000}}){{1000}}\").");
        }

        _builder.Append(character);
    }

    internal string Result() {
        return _builder.ToString();
    }

}

/// <summary>
///     A node of the parsed pattern tree. Generation is a direct recursive descent: each node writes the characters it
///     stands for into the <see cref="RegexGenerationContext" />, drawing counts and choices from the seeded random
///     generator — so the whole tree yields exactly one string that matches the pattern, in one pass.
/// </summary>
[System.Diagnostics.CodeAnalysis.SuppressMessage("Minor Code Smell", "S1694:An abstract class should have both abstract and concrete methods",
                                                 Justification =
                                                     "The abstract class is the root of a closed, internal hierarchy and is deliberately not an interface: a class cannot be " +
                                                     "implemented from outside the assembly, keeps the option of adding shared state without breaking every subtype, and on the " +
                                                     "netstandard2.0 leg cannot be replaced by an interface with non-public members.")]
internal abstract class RegexNode {

    internal abstract void Append(RegexGenerationContext context);

}

/// <summary>A terminal: one character drawn uniformly from a fixed set (a literal is the singleton case).</summary>
internal sealed class RegexCharacters : RegexNode {

    #region Fields declarations

    private readonly char[] _choices;

    #endregion

    internal RegexCharacters(char[] choices) {
        if (choices is null) { throw new ArgumentNullException(nameof(choices)); }
        _choices = choices;
    }

    /// <summary>The characters this terminal can emit — empty when a class excludes the whole universe.</summary>
    internal int Count => _choices.Length;

    internal override void Append(RegexGenerationContext context) {
        if (context is null) { throw new ArgumentNullException(nameof(context)); }
        context.Append(_choices[context.Random.Next(_choices.Length)]);
    }

}

/// <summary>A concatenation: its children in order.</summary>
internal sealed class RegexSequence : RegexNode {

    #region Fields declarations

    private readonly RegexNode[] _parts;

    #endregion

    internal RegexSequence(RegexNode[] parts) {
        if (parts is null) { throw new ArgumentNullException(nameof(parts)); }
        _parts = parts;
    }

    internal override void Append(RegexGenerationContext context) {
        if (context is null) { throw new ArgumentNullException(nameof(context)); }
        foreach (RegexNode part in _parts) { part.Append(context); }
    }

}

/// <summary>An alternation: one branch, chosen uniformly.</summary>
internal sealed class RegexAlternation : RegexNode {

    #region Fields declarations

    private readonly RegexNode[] _branches;

    #endregion

    internal RegexAlternation(RegexNode[] branches) {
        if (branches is null) { throw new ArgumentNullException(nameof(branches)); }
        _branches = branches;
    }

    internal override void Append(RegexGenerationContext context) {
        if (context is null) { throw new ArgumentNullException(nameof(context)); }
        _branches[context.Random.Next(_branches.Length)].Append(context);
    }

}

/// <summary>
///     A quantifier: the child repeated between <c>min</c> and <c>max</c> times. An unbounded quantifier
///     (<c>*</c>, <c>+</c>, <c>{n,}</c>) has no <c>max</c>; generation then draws <c>min</c> plus 0 to
///     <see cref="UnboundedExtra" /> extra repetitions, the same bounded-spread default the rest of the library uses.
/// </summary>
internal sealed class RegexRepeat : RegexNode {

    #region Statics members declarations

    /// <summary>How many repetitions above the minimum an unbounded quantifier may add.</summary>
    internal const int UnboundedExtra = 8;

    #endregion

    #region Fields declarations

    private readonly RegexNode _child;
    private readonly int?      _max;
    private readonly int       _min;

    #endregion

    internal RegexRepeat(RegexNode child, int min, int? max) {
        if (child is null) { throw new ArgumentNullException(nameof(child)); }
        _child = child;
        _min   = min;
        _max   = max;
    }

    internal override void Append(RegexGenerationContext context) {
        if (context is null) { throw new ArgumentNullException(nameof(context)); }
        // The unbounded count is widened to long before the extra repetitions are added: a minimum within
        // UnboundedExtra of int.MaxValue would otherwise wrap negative, and a negative count writes nothing at all —
        // silently yielding a value the pattern does not match, which is the one outcome generation must never
        // produce. Widened, such a count simply walks until the generation ceiling reports the overrun, exactly as
        // any other minimum too large to fit does.
        long count = _max is int max
                         ? context.Random.NextInt32Inclusive(_min, max)
                         : (long)_min + context.Random.Next(0, UnboundedExtra + 1);

        for (long repetition = 0; repetition < count; repetition++) { _child.Append(context); }
    }

}
