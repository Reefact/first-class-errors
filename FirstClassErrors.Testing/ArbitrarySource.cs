namespace FirstClassErrors.Testing;

/// <summary>
///     The generic, error-agnostic engine behind <see cref="Any" />: a seedable, context-local source of arbitrary
///     primitive values (numbers, booleans, identifiers, instants, tokens, enum members). It carries no knowledge of
///     FirstClassErrors — <see cref="Any" /> layers the error-specific helpers on top, and the clock and instance-id
///     seams draw their arbitrary values from here — so it stays a self-contained unit that could be extracted as a
///     standalone utility unchanged.
/// </summary>
/// <remarks>
///     The source is stored in an <see cref="AsyncLocal{T}" />, so it flows with the current execution context and
///     never leaks across tests running in parallel. Outside a <see cref="UseSeed" /> scope it is unseeded and every
///     run differs; inside one it is deterministic.
/// </remarks>
internal static class ArbitrarySource {

    #region Fields declarations

    private static readonly DateTimeOffset      Origin = new(2000, 1, 1, 0, 0, 0, TimeSpan.Zero);
    private static readonly AsyncLocal<Random?> Seeded = new();

    #endregion

    internal static Random Current {
        get {
            Random? random = Seeded.Value;
            if (random is null) {
                random       = new Random(NewSeed());
                Seeded.Value = random;
            }

            return random;
        }
    }

    internal static int NewSeed() {
        return System.Guid.NewGuid().GetHashCode();
    }

    internal static IDisposable UseSeed(int seed) {
        Random? previous = Seeded.Value;
        Seeded.Value = new Random(seed);

        return new SeedScope(previous);
    }

    internal static int Int() {
        return Current.Next(int.MinValue, int.MaxValue);
    }

    internal static bool Bool() {
        return Current.Next(2) == 0;
    }

    internal static Guid Guid() {
        return NewGuid(Current);
    }

    internal static DateTimeOffset Instant() {
        return NewInstant(Current);
    }

    internal static string Token(string alphabet, int length) {
        Random random = Current;
        char[] chars  = new char[length];
        for (int i = 0; i < length; i++) {
            chars[i] = alphabet[random.Next(alphabet.Length)];
        }

        return new string(chars);
    }

    internal static TEnum Enum<TEnum>()
        where TEnum : struct, System.Enum {
        TEnum[] values = (TEnum[])System.Enum.GetValues(typeof(TEnum));

        return values[Current.Next(values.Length)];
    }

    internal static TEnum EnumExcluding<TEnum>(params TEnum[] excluded)
        where TEnum : struct, System.Enum {
        List<TEnum> pool = new();
        foreach (TEnum value in (TEnum[])System.Enum.GetValues(typeof(TEnum))) {
            if (Array.IndexOf(excluded, value) < 0) { pool.Add(value); }
        }

        return pool[Current.Next(pool.Count)];
    }

    private static Guid NewGuid(Random random) {
        byte[] bytes = new byte[16];
        random.NextBytes(bytes);

        return new Guid(bytes);
    }

    private static DateTimeOffset NewInstant(Random random) {
        return Origin.AddSeconds(random.Next());
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
