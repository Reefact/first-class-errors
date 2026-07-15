namespace FirstClassErrors.Testing;

/// <summary>
///     Supplies arbitrary, valid values for the parts of a test that are <b>not</b> under assertion — the "any" a
///     test needs so its <c>Arrange</c> stops advertising values it never checks. Reach for it when a test says "give
///     me <i>some</i> error code / message / instant" and the exact value is irrelevant: an explicit
///     <see cref="Any" /> call reads as "this is arbitrary" where a hand-picked literal reads as "this matters".
/// </summary>
/// <remarks>
///     <para>
///         Every value is drawn from a pseudo-random source. By default that source is unseeded, so each run produces
///         fresh values; wrap the code in <see cref="UseSeed" /> to make the sequence deterministic and reproducible.
///         The source is stored in an <see cref="AsyncLocal{T}" />, so it flows with the current execution context and
///         never leaks across tests running in parallel — the same contract the rest of this package keeps.
///     </para>
///     <para>
///         The values are <b>valid</b> (an <see cref="Any.ErrorCode" /> is never blank, an <see cref="Any.Instant" />
///         is a real UTC instant) but deliberately <b>recognizable</b> as arbitrary (codes look like
///         <c>ANY_CODE_7F3A9C</c>), so an arbitrary value that surfaces in a failure message is easy to spot.
///     </para>
///     <example>
///         <code>
///         // The order id is what the test asserts on; the message is not — so it is Any.
///         Outcome&lt;Order&gt; outcome = Outcome&lt;Order&gt;.Failure(
///             DomainError.Create(ErrorCode.Create("ORDER_NOT_FOUND"), Any.DiagnosticMessage())
///                        .WithPublicMessage(Any.ShortMessage()));
///
///         // Reproduce a run by pinning the seed:
///         using (Any.UseSeed(1234)) {
///             ErrorCode code = Any.ErrorCode(); // same value on every run
///         }
///         </code>
///     </example>
/// </remarks>
public static class Any {

    #region Fields declarations

    private const string CodeAlphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
    private const string TextAlphabet = "abcdefghijklmnopqrstuvwxyz0123456789";

    private static readonly DateTimeOffset      Origin = new(2000, 1, 1, 0, 0, 0, TimeSpan.Zero);
    private static readonly AsyncLocal<Random?> Seeded = new();

    #endregion

    /// <summary>
    ///     Pins the arbitrary-value source to a deterministic sequence seeded with <paramref name="seed" /> until the
    ///     returned scope is disposed, so a test (or a whole run) using <see cref="Any" /> becomes reproducible.
    /// </summary>
    /// <remarks>
    ///     Scopes nest: an inner <see cref="UseSeed" /> uses its own source and restores the outer one when disposed,
    ///     without disturbing the outer sequence. Always scope the override with <c>using</c>. Outside any scope the
    ///     source is unseeded and every run differs.
    /// </remarks>
    /// <param name="seed">The seed that makes the sequence of arbitrary values reproducible.</param>
    /// <returns>A scope that restores the previous source when disposed.</returns>
    public static IDisposable UseSeed(int seed) {
        Random? previous = Seeded.Value;
        Seeded.Value = new Random(seed);

        return new SeedScope(previous);
    }

    /// <summary>
    ///     Returns an arbitrary <see cref="int" /> drawn from the full range of the type.
    /// </summary>
    /// <returns>An arbitrary integer, possibly negative.</returns>
    public static int Int() {
        return CurrentRandom.Next(int.MinValue, int.MaxValue);
    }

    /// <summary>
    ///     Returns an arbitrary <see cref="bool" />.
    /// </summary>
    /// <returns><c>true</c> or <c>false</c>, with even probability.</returns>
    public static bool Bool() {
        return CurrentRandom.Next(2) == 0;
    }

    /// <summary>
    ///     Returns an arbitrary <see cref="System.Guid" />. Unlike <see cref="System.Guid.NewGuid" />, it is drawn from
    ///     the seedable source, so it is reproducible inside a <see cref="UseSeed" /> scope.
    /// </summary>
    /// <returns>An arbitrary identifier.</returns>
    public static Guid Guid() {
        return NewGuid(CurrentRandom);
    }

    /// <summary>
    ///     Returns an arbitrary UTC <see cref="DateTimeOffset" /> (offset <see cref="TimeSpan.Zero" />).
    /// </summary>
    /// <remarks>
    ///     To make an error's <c>OccurredAt</c> arbitrary rather than a real wall-clock reading, prefer
    ///     <c>Clock.UseAny()</c>, which freezes the ambient clock to an arbitrary instant for a scope.
    /// </remarks>
    /// <returns>An arbitrary instant, in UTC.</returns>
    public static DateTimeOffset Instant() {
        return NewInstant(CurrentRandom);
    }

    /// <summary>
    ///     Returns an arbitrary, non-blank <see cref="string" />, prefixed with <c>any-</c> so it reads as arbitrary.
    /// </summary>
    /// <returns>An arbitrary string such as <c>any-4f2a9c1b</c>.</returns>
    public static string String() {
        return "any-" + NextToken(TextAlphabet, 8);
    }

    /// <summary>
    ///     Returns an arbitrary, valid <see cref="FirstClassErrors.ErrorCode" /> — never blank, and shaped like a real
    ///     code (for example <c>ANY_CODE_7F3A9C</c>) so it is recognizable as arbitrary.
    /// </summary>
    /// <returns>An arbitrary error code.</returns>
    public static ErrorCode ErrorCode() {
        return FirstClassErrors.ErrorCode.Create("ANY_CODE_" + NextToken(CodeAlphabet, 6));
    }

    /// <summary>
    ///     Returns an arbitrary, non-blank internal diagnostic message, suitable wherever a test needs a
    ///     <c>diagnosticMessage</c> it does not assert on.
    /// </summary>
    /// <returns>An arbitrary diagnostic message.</returns>
    public static string DiagnosticMessage() {
        return "Any diagnostic message " + NextToken(CodeAlphabet, 6) + ".";
    }

    /// <summary>
    ///     Returns an arbitrary, non-blank public short message, suitable wherever a test needs a <c>shortMessage</c>
    ///     it does not assert on.
    /// </summary>
    /// <returns>An arbitrary short message.</returns>
    public static string ShortMessage() {
        return "Any short message " + NextToken(CodeAlphabet, 6) + ".";
    }

    /// <summary>
    ///     Returns an arbitrary, non-blank public detailed message, suitable wherever a test needs a
    ///     <c>detailedMessage</c> it does not assert on.
    /// </summary>
    /// <returns>An arbitrary detailed message.</returns>
    public static string DetailedMessage() {
        return "Any detailed message " + NextToken(CodeAlphabet, 6) + ".";
    }

    /// <summary>
    ///     Returns an arbitrary value of the enum <typeparamref name="TEnum" />, uniformly across <b>all</b> its
    ///     declared members.
    /// </summary>
    /// <remarks>
    ///     This can return a sentinel such as <c>Unknown</c>. When a test needs a <i>meaningful</i> value, prefer the
    ///     dedicated helpers (<see cref="Transience" />, <see cref="InteractionDirection" />), which exclude the
    ///     <c>Unknown</c> sentinel.
    /// </remarks>
    /// <typeparam name="TEnum">The enum type to draw a value from.</typeparam>
    /// <returns>An arbitrary member of <typeparamref name="TEnum" />.</returns>
    public static TEnum Enum<TEnum>()
        where TEnum : struct, System.Enum {
        TEnum[] values = (TEnum[])System.Enum.GetValues(typeof(TEnum));

        return values[CurrentRandom.Next(values.Length)];
    }

    /// <summary>
    ///     Returns an arbitrary <b>meaningful</b> <see cref="FirstClassErrors.Transience" /> —
    ///     <see cref="FirstClassErrors.Transience.Transient" /> or
    ///     <see cref="FirstClassErrors.Transience.NonTransient" />, never
    ///     <see cref="FirstClassErrors.Transience.Unknown" />.
    /// </summary>
    /// <returns>An arbitrary transience classification other than <c>Unknown</c>.</returns>
    public static Transience Transience() {
        return NextEnumExcluding(FirstClassErrors.Transience.Unknown);
    }

    /// <summary>
    ///     Returns an arbitrary <see cref="FirstClassErrors.ErrorOrigin" />, uniformly across all of its members.
    /// </summary>
    /// <returns>An arbitrary error origin.</returns>
    public static ErrorOrigin ErrorOrigin() {
        return Enum<ErrorOrigin>();
    }

    /// <summary>
    ///     Returns an arbitrary <b>meaningful</b> <see cref="FirstClassErrors.InteractionDirection" /> —
    ///     <see cref="FirstClassErrors.InteractionDirection.Incoming" /> or
    ///     <see cref="FirstClassErrors.InteractionDirection.Outgoing" />, never
    ///     <see cref="FirstClassErrors.InteractionDirection.Unknown" />.
    /// </summary>
    /// <returns>An arbitrary interaction direction other than <c>Unknown</c>.</returns>
    public static InteractionDirection InteractionDirection() {
        return NextEnumExcluding(FirstClassErrors.InteractionDirection.Unknown);
    }

    internal static Guid NewGuid(Random random) {
        byte[] bytes = new byte[16];
        random.NextBytes(bytes);

        return new Guid(bytes);
    }

    internal static DateTimeOffset NewInstant(Random random) {
        return Origin.AddSeconds(random.Next());
    }

    private static Random CurrentRandom {
        get {
            Random? random = Seeded.Value;
            if (random is null) {
                random       = new Random(System.Guid.NewGuid().GetHashCode());
                Seeded.Value = random;
            }

            return random;
        }
    }

    private static string NextToken(string alphabet, int length) {
        Random random = CurrentRandom;
        char[] chars  = new char[length];
        for (int i = 0; i < length; i++) {
            chars[i] = alphabet[random.Next(alphabet.Length)];
        }

        return new string(chars);
    }

    private static TEnum NextEnumExcluding<TEnum>(params TEnum[] excluded)
        where TEnum : struct, System.Enum {
        List<TEnum> pool = new();
        foreach (TEnum value in (TEnum[])System.Enum.GetValues(typeof(TEnum))) {
            if (Array.IndexOf(excluded, value) < 0) { pool.Add(value); }
        }

        return pool[CurrentRandom.Next(pool.Count)];
    }

    #region Nested types

    private sealed class SeedScope : IDisposable {

        private readonly Random? _previous;
        private          bool    _disposed;

        internal SeedScope(Random? previous) {
            _previous = previous;
        }

        public void Dispose() {
            if (_disposed) { return; }

            _disposed    = true;
            Seeded.Value = _previous;
        }

    }

    #endregion

}
