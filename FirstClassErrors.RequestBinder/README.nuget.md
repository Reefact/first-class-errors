# FirstClassErrors.RequestBinder

Fluent, framework-agnostic request binding for [FirstClassErrors](https://github.com/Reefact/first-class-errors): convert an incoming request DTO (body + route) into a typed command or query of value objects at the primary-adapter boundary.

* **Collect-all**: every failing field is reported at once — no fix-one-resubmit loop.
* **First-class errors**: failures are coded, documented `PrimaryPortError` trees (code + argument path + public/diagnostic messages), not flat strings.
* **No throw on the invalid-input path**: converters return `Outcome<T>`; the binder returns `Outcome<TCommand>`. Exceptions are reserved for genuine bugs.
* **Framework-agnostic**: works the same for HTTP controllers, message consumers, CLIs and gRPC handlers.

```csharp
var bind = Bind.PropertiesOf(request).FailWith(InvalidBookingCommandError.Invalid);

var email = bind.SimpleProperty(r => r.GuestEmail).AsRequired(EmailAddress.Parse);
var stay  = bind.ComplexProperty(r => r.Stay).FailWith(InvalidStayError.Invalid).AsRequired(BindStay);

Outcome<PlaceBookingCommand> command =
    bind.New(s => new PlaceBookingCommand(s.Get(email), s.Get(stay)));
```

See the [request-binder guide](https://github.com/Reefact/first-class-errors/blob/main/doc/handwritten/for-users/RequestBinder.en.md) for the full walkthrough, and the [repository documentation](https://github.com/Reefact/first-class-errors) for the rest of FirstClassErrors.
