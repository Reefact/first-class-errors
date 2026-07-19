namespace FirstClassErrors.RequestBinder;

/// <summary>
///     Assembles the bound command inside <see cref="RequestBinder.Create{TCommand}" />, reading each bound
///     value from the supplied <see cref="BindingScope" /> and returning an <see cref="Outcome{T}" /> — so the assembly
///     step itself may still fail, typically a validating factory (<c>Command.Create(...)</c>) enforcing a cross-field
///     rule (<c>CheckOut &gt; CheckIn</c>) that no single field could check on its own.
/// </summary>
/// <remarks>
///     A dedicated delegate — rather than <c>Func&lt;BindingScope, Outcome&lt;TCommand&gt;&gt;</c> — is required because
///     <see cref="BindingScope" /> is a <c>ref struct</c> and cannot be used as a generic type argument.
/// </remarks>
/// <typeparam name="TCommand">The type of the assembled command or query.</typeparam>
/// <param name="scope">The scope through which bound values are read.</param>
/// <returns>The assembled command, or the failure of a cross-field rule.</returns>
public delegate Outcome<TCommand> ValidatingAssembler<TCommand>(BindingScope scope) where TCommand : notnull;
