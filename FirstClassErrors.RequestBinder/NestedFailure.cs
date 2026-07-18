namespace FirstClassErrors.RequestBinder;

/// <summary>
///     Groups the failure of a nested binding (a complex property, or one element of a complex list) under its
///     argument path — the single decision shared by <see cref="ComplexPropertyConverter{TRequest, TArgument}" /> and
///     <see cref="ListOfComplexPropertiesConverter{TRequest, TArgument}" />.
/// </summary>
internal static class NestedFailure {

    #region Statics members declarations

    /// <summary>
    ///     Decides how a nested binding's failure joins the parent envelope. The nested binder's <b>own</b> failure
    ///     envelope — <paramref name="nestedEnvelope" />, self-describing because its inner errors already carry the
    ///     prefixed paths — is recorded as-is. Anything else, <b>including a bare <see cref="PrimaryPortError" /> leaf a
    ///     converter returned directly</b>, is wrapped in <c>REQUEST_ARGUMENT_INVALID</c> so the argument path survives
    ///     — exactly as a <see cref="DomainError" /> would be. The test is by <b>reference</b>, not by type: a leaf that
    ///     merely happens to be a <see cref="PrimaryPortError" /> is not this binder's envelope and must not skip the
    ///     path.
    /// </summary>
    /// <param name="error">The failure the nested binding returned.</param>
    /// <param name="nestedEnvelope">The envelope the nested binder's build terminal produced, or <c>null</c> when it built none.</param>
    /// <param name="argumentPath">The argument path to attach when wrapping.</param>
    /// <param name="argumentInvalid">The structural-error definition to wrap under — the parent binder's configured <see cref="RequestBinderOptions.ArgumentInvalid" />.</param>
    /// <returns>The error to record on the parent binder.</returns>
    internal static PrimaryPortError Group(Error error, PrimaryPortError? nestedEnvelope, string argumentPath, BinderErrorDefinition argumentInvalid) {
        return ReferenceEquals(error, nestedEnvelope)
                   ? (PrimaryPortError)error
                   : RequestBindingError.ArgumentInvalid(argumentInvalid, argumentPath, error);
    }

    #endregion

}
