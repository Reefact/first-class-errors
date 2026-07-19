# FirstClassErrors.RequestBinder

Fluent, framework-agnostic request binding for [FirstClassErrors](https://github.com/Reefact/first-class-errors): build a typed command or query of value objects from an incoming request — a DTO body, route/query/header arguments, or both — at the primary-adapter boundary.

* **Collect-all**: every failing field is reported at once — no fix-one-resubmit loop.
* **First-class errors**: failures are coded, documented `PrimaryPortError` trees (code + argument path + public/diagnostic messages), not flat strings.
* **Source-agnostic**: the binder builds the command; a DTO's properties and out-of-DTO arguments are attached as peers, into one envelope with one set of paths.
* **No throw on the invalid-input path**: converters return `Outcome<T>`; the binder returns `Outcome<TCommand>`. Exceptions are reserved for genuine bugs.
* **Framework-agnostic**: works the same for HTTP controllers, message consumers, CLIs and gRPC handlers.

```csharp
var bind = Bind.Request(InvalidBookingCommandError.Invalid);

var body  = bind.PropertiesOf(request);
var email = body.SimpleProperty(r => r.GuestEmail).AsRequired(EmailAddress.Parse);
var stay  = body.ComplexProperty(r => r.Stay).FailWith(InvalidStayError.Invalid).AsRequired(BindStay);
var id    = bind.Argument("bookingId").FromRoute(routeBookingId).AsRequired(BookingId.From);

Outcome<PlaceBookingCommand> command =
    bind.New(s => new PlaceBookingCommand(s.Get(id), s.Get(email), s.Get(stay)));
```

See the [request-binder guide](https://github.com/Reefact/first-class-errors/blob/main/doc/handwritten/for-users/RequestBinder.en.md) for the full walkthrough, and the [repository documentation](https://github.com/Reefact/first-class-errors) for the rest of FirstClassErrors.
