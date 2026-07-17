namespace Dummies.UnitTests;

/// <summary>
///     A DDD-style value object with a format invariant, used to exercise the primitive-to-value-object bridge
///     (<c>As</c>): its factory is the single gatekeeper, exactly as in production code.
/// </summary>
public sealed class OrderReference {

    #region Statics members declarations

    public static OrderReference Create(string value) {
        if (value is null) { throw new ArgumentNullException(nameof(value)); }
        if (!value.StartsWith("ORD-", StringComparison.Ordinal)) { throw new ArgumentException("An order reference starts with 'ORD-'.", nameof(value)); }
        if (value.Length != 12) { throw new ArgumentException("An order reference is exactly 12 characters long.", nameof(value)); }

        return new OrderReference(value);
    }

    #endregion

    private OrderReference(string value) {
        Value = value;
    }

    public string Value { get; }

}

/// <summary>A numeric value object with a range invariant, for the numeric side of the bridge.</summary>
public sealed class Percentage {

    #region Statics members declarations

    public static Percentage Create(int value) {
        if (value is < 0 or > 100) { throw new ArgumentOutOfRangeException(nameof(value), value, "A percentage lies between 0 and 100."); }

        return new Percentage(value);
    }

    #endregion

    private Percentage(int value) {
        Value = value;
    }

    public int Value { get; }

}

/// <summary>A small aggregate assembled from constrained parts, for <c>Any.Combine</c>.</summary>
public sealed class Customer {

    public Customer(string name, OrderReference lastOrder) {
        Name      = name ?? throw new ArgumentNullException(nameof(name));
        LastOrder = lastOrder ?? throw new ArgumentNullException(nameof(lastOrder));
    }

    public string         Name      { get; }
    public OrderReference LastOrder { get; }

}
