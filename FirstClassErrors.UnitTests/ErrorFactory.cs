namespace FirstClassErrors.UnitTests;

/// <summary>
///     Test helper that builds fully-formed errors through the staged builder without spelling out
///     <c>Create(...).WithPublicMessage(...)</c> at every call site. For concise fixtures the supplied message is reused as
///     both the diagnostic message and the public short message (so <c>ToException().Message</c> equals that message).
///     Tests that specifically exercise the public creation API or a distinct message field call
///     <see cref="DomainError.Create(ErrorCode, string, System.Action{ErrorContextBuilder})" /> and friends directly instead.
/// </summary>
internal static class ErrorFactory {

    #region Statics members declarations

    public static DomainError Domain(ErrorCode code, string message) {
        return DomainError.Create(code, message).WithPublicMessage(message);
    }

    public static DomainError Domain(ErrorCode code, string message, DomainError innerError) {
        return DomainError.Create(code, message, innerError).WithPublicMessage(message);
    }

    public static DomainError Domain(ErrorCode code, string message, IEnumerable<DomainError> innerErrors) {
        return DomainError.Create(code, message, innerErrors).WithPublicMessage(message);
    }

    public static InfrastructureError Infrastructure(ErrorCode code, string message, InteractionDirection direction, Transience transience) {
        return InfrastructureError.Create(code, message, direction, transience).WithPublicMessage(message);
    }

    public static PrimaryPortError Primary(ErrorCode code, string message, Transience transience) {
        return PrimaryPortError.Create(code, message, transience).WithPublicMessage(message);
    }

    public static PrimaryPortError Primary(ErrorCode code, string message, PrimaryPortInnerErrors innerErrors) {
        return PrimaryPortError.Create(code, message, innerErrors).WithPublicMessage(message);
    }

    public static SecondaryPortError Secondary(ErrorCode code, string message, Transience transience) {
        return SecondaryPortError.Create(code, message, transience).WithPublicMessage(message);
    }

    public static SecondaryPortError Secondary(ErrorCode code, string message, SecondaryPortInnerErrors innerErrors) {
        return SecondaryPortError.Create(code, message, innerErrors).WithPublicMessage(message);
    }

    #endregion

}
