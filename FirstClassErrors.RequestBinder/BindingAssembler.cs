namespace FirstClassErrors.RequestBinder;

/// <summary>
///     Assembles the bound command inside <see cref="RequestBinder{TRequest}.Build{TCommand}" />, reading each bound
///     value from the supplied <see cref="BindingScope" /> — the only channel through which a bound value is reachable.
/// </summary>
/// <remarks>
///     A dedicated delegate — rather than <c>Func&lt;BindingScope, TCommand&gt;</c> — is required because
///     <see cref="BindingScope" /> is a <c>ref struct</c> and cannot be used as a generic type argument.
/// </remarks>
/// <typeparam name="TCommand">The type of the assembled command or query.</typeparam>
/// <param name="read">The scope through which bound values are read.</param>
/// <returns>The assembled command.</returns>
public delegate TCommand BindingAssembler<TCommand>(BindingScope read);
