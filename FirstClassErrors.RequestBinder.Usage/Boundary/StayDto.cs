namespace FirstClassErrors.RequestBinder.Usage.Boundary;

/// <summary>
///     The wire shape of a nested stay: two nullable date strings. Bound by a nested binder into a
///     <see cref="Model.Stay" />, with each field reported under a path prefixed by <c>Stay.</c>.
/// </summary>
/// <param name="CheckIn">The check-in date as an ISO string (required once bound).</param>
/// <param name="CheckOut">The check-out date as an ISO string (required once bound).</param>
public sealed record StayDto(string? CheckIn, string? CheckOut);
