namespace JustDummies;

/// <summary>
///     Marks a type that exists only to build one of this library's exceptions, and is therefore constructed while a
///     failure is being reported.
/// </summary>
/// <remarks>
///     Such a type is exempt from the null-argument guard convention, for the same reason exception types themselves
///     are: a guard on this path throws while a failure is being reported, replacing that failure with a failure about
///     reporting it and losing the original. The exemption is declared here rather than inferred, so it applies only
///     where someone has said it should — the marker is the decision, and
///     <c>NullArgumentGuardConventionTests</c> reads it (ADR-0064, which widened ADR-0045's exemption from exception
///     types to this path).
///     <para>
///         Nothing is given up by it: every argument on this path is non-nullable, so a caller that cannot prove a
///         value is <c>CS8604</c> at build time. The contract moves from a runtime guard to the compiler, which is
///         where it belongs for a path that must never throw.
///     </para>
/// </remarks>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
internal sealed class BuiltOnTheFailurePathAttribute : Attribute { }
