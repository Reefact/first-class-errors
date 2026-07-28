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
[System.Diagnostics.CodeAnalysis.SuppressMessage("Major Code Smell", "S3246:Generic type parameters should be co-variant where possible",
                                                 Justification =
                                                     "Marking TCommand as out would widen the public API surface and move the committed public-API baseline, which this change is not " +
                                                     "asking for. The variance is worth having and is flagged for the maintainer rather than taken here.")]
public delegate TCommand BindingAssembler<TCommand>(BindingScope scope);
