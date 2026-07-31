namespace JustDummies;

/// <summary>
///     Marks a type whose instances are values: two of them holding the same thing are the same one, and nothing
///     about which instance you hold matters.
/// </summary>
/// <remarks>
///     A reference type answers "is this the same one?" by identity unless somebody writes another answer, and it
///     answers silently — no compiler warning, no failing test, just a comparison that quietly means something else.
///     The marker turns that into a contract <c>ValueObjectConventionTests</c> enforces by reflection: a marked type
///     is sealed, immutable, and carries the full set — <see cref="System.IEquatable{T}" />, both
///     <c>Equals</c> overloads, <c>GetHashCode</c>, and the <c>==</c>/<c>!=</c> pair, whose absence is the silent
///     case since it degrades to reference comparison rather than failing to compile.
///     <para>
///         It is a declaration, not a detection. Immutability alone does not make a value: the generators and the
///         specifications are immutable too, yet two identically constrained generators are two recipes, not one
///         value, and comparing them would claim a meaning they do not have. Only a type that says it is a value is
///         held to the contract.
///     </para>
///     <para>
///         Marking a struct is rejected by the same convention rather than by usage rules alone: a struct exposes a
///         parameterless constructor that yields an instance bypassing every validating factory, which is why a value
///         enforcing an invariant is a class throughout this repository.
///     </para>
/// </remarks>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
internal sealed class ValueObjectAttribute : Attribute { }
