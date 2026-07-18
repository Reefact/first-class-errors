namespace FirstClassErrors.RequestBinder.Usage.Boundary;

/// <summary>
///     The wire shape of one guest: a nullable first name and a nullable e-mail. Bound by a per-element nested binder
///     into a <see cref="Model.Guest" />, with each field reported under an indexed path such as
///     <c>Guests[1].FirstName</c>.
/// </summary>
/// <param name="FirstName">The guest's first name (required once bound).</param>
/// <param name="Email">The guest's e-mail address (optional).</param>
public sealed record GuestDto(string? FirstName, string? Email);
