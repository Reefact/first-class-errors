namespace FirstClassErrors.RequestBinder.Usage.Model;

/// <summary>
///     A guest on the booking: a required first name and an optional e-mail address. Built by a total constructor
///     (the binder's <c>New</c> terminal) inside the nested per-element binder of the guests list.
/// </summary>
/// <param name="FirstName">The guest's first name (bound raw, presence-only).</param>
/// <param name="Email">The guest's e-mail address, or <c>null</c> when the request omitted it.</param>
public sealed record Guest(string FirstName, EmailAddress? Email);
