namespace FirstClassErrors.RequestBinder;

/// <summary>
///     Assembles the bound command inside <see cref="RequestBinder.New{TCommand}" />, reading each bound value
///     from the supplied <see cref="BindingScope" /> — the only channel through which a bound value is reachable — and
///     returning the command directly (a total <c>new</c> that cannot fail). For a command produced by a validating
///     factory that returns an <see cref="Outcome{T}" />, use <see cref="ValidatingAssembler{TCommand}" /> instead.
/// </summary>
/// <remarks>
///     A dedicated delegate — rather than <c>Func&lt;BindingScope, TCommand&gt;</c> — is required because
///     <see cref="BindingScope" /> is a <c>ref struct</c> and cannot be used as a generic type argument.
/// </remarks>
/// <typeparam name="TCommand">The type of the assembled command or query.</typeparam>
/// <param name="scope">The scope through which bound values are read.</param>
/// <returns>The assembled command.</returns>
public delegate TCommand BindingAssembler<TCommand>(BindingScope scope);
