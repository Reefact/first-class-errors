#region Usings declarations

using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;

#endregion

namespace FirstClassErrors.RequestBinder;

/// <summary>
///     Process-wide cache of compiled, typed property getters — one per bound (DTO type, property). The reflection
///     cost of reading a DTO property (a <see cref="PropertyInfo.GetValue(object)" /> invoke, and its boxing of
///     value-type values) is paid once, at the first bind of a property, and every later bind of the same property
///     reuses the compiled delegate: a direct, allocation-free property read.
/// </summary>
/// <remarks>
///     <para>
///         The cache is keyed by the closed generic type (one dictionary per (DTO type, selected value type) pair) and
///         by <see cref="PropertyInfo" /> within it, so two DTO types sharing a property name, or one property selected
///         under two different static types, can never collide. It is deliberately <b>options-independent</b>: a getter
///         depends only on the property, never on a binder's <see cref="RequestBinderOptions" /> — argument names and
///         paths stay per-binding (see ADR-0012: options are fixed before binding begins, per binder).
///     </para>
///     <para>
///         The mis-declaration guard (a non-nullable value-type property, whose absence the binder cannot detect)
///         lives in the compile step: an invalid property is never cached, so the guard throws on <b>every</b> call —
///         eagerly, at selector time, exactly as before this cache existed. A cache hit implies a valid property.
///     </para>
///     <para>
///         Thread-safety: bindings are single-threaded by contract, but this cache is process-wide and safe — a
///         concurrent first bind of the same property may compile twice, and either delegate wins, both correct.
///         Statics of a generic instantiation live with the DTO type's loader context, so an unloadable consumer
///         assembly is not pinned by the cache.
///     </para>
/// </remarks>
/// <typeparam name="TDto">The type of the request DTO the property is read from.</typeparam>
/// <typeparam name="TValue">The static type the selector yields (the property type, or a base type of it).</typeparam>
internal static class PropertyGetters<TDto, TValue> {

    #region Statics members declarations

    // A static of THIS closed generic, not a single map shared across DTO types: the CLR keeps a closed generic's
    // statics in the DTO type's own loader context, so a collectible consumer assembly unloads together with its
    // cache entries. Replacing this with one binder-wide dictionary would pin every consumer assembly for the
    // process lifetime — the per-closed-generic layout is load-bearing, not stylistic.
    private static readonly ConcurrentDictionary<PropertyInfo, Func<TDto, TValue>> Cache = new();

    /// <summary>
    ///     The compiled getter of <paramref name="property" /> — from the cache, or compiled and cached on first
    ///     use.
    /// </summary>
    /// <param name="property">The DTO property to read.</param>
    /// <returns>The compiled, typed getter.</returns>
    /// <exception cref="ArgumentException">
    ///     Thrown — on every call, an invalid property is never cached — when <paramref name="property" /> is a
    ///     non-nullable value type: a missing value would be indistinguishable from its default.
    /// </exception>
    internal static Func<TDto, TValue> For(PropertyInfo property) {
        if (Cache.TryGetValue(property, out Func<TDto, TValue>? getter)) { return getter; }

        return Cache.GetOrAdd(property, CompileValidated);
    }

    private static Func<TDto, TValue> CompileValidated(PropertyInfo property) {
        // A non-nullable value-type property can never be null, so a missing argument (deserialized to default(T) —
        // 0, false, ...) is indistinguishable from a legitimately-sent default: absence would be silently lost. The
        // information does not exist at runtime, so reject the mis-declaration loudly (the binder's programming-error
        // channel) — the DTO property must be declared nullable so that an absent argument arrives as null. The
        // parameter name is the selector every PropertySource method receives.
        if (property.PropertyType.IsValueType && Nullable.GetUnderlyingType(property.PropertyType) is null) {
            throw new ArgumentException(
                $"The request property '{property.Name}' is a non-nullable value type ({property.PropertyType.Name}); a missing value would be indistinguishable from its default. Declare it as {property.PropertyType.Name}? so the binder can detect an absent argument.",
                "selector");
        }

        // A canonical tree is compiled from the PropertyInfo instead of compiling the caller's selector: the caller's
        // tree may carry a Convert node and cannot be shared across call sites anyway. When the static selector type
        // differs from the property type, the value is routed THROUGH OBJECT because that is exactly the conversion
        // the reflection-based reader applied (a cast on the boxed result of GetValue): a widening the compiler emits
        // (List<T> selected as IEnumerable<T>) still succeeds, and a hand-built selector over an incompatible value
        // type still fails with the same InvalidCastException, instead of silently gaining C#'s numeric/user-defined
        // conversions. For a nullable value-type selector the boxed value is unboxed to the UNDERLYING type before
        // the lift — `(TValue?)(TValue)(object)value` — mirroring the pre-cache cast, whose unbox accepted the CLR's
        // boxed-enum/underlying compatibility that a direct unbox to Nullable<> rejects.
        ParameterExpression dto  = Expression.Parameter(typeof(TDto), "dto");
        Expression          body = Expression.Property(dto, property);
        if (body.Type != typeof(TValue)) {
            Type? liftedUnderlying = Nullable.GetUnderlyingType(typeof(TValue));
            if (liftedUnderlying is null) {
                body = Expression.Convert(Expression.Convert(body, typeof(object)), typeof(TValue));
            } else {
                ParameterExpression boxed = Expression.Variable(typeof(object), "boxed");
                body = Expression.Block(
                    new[] { boxed },
                    Expression.Assign(boxed, Expression.Convert(body, typeof(object))),
                    Expression.Condition(
                        Expression.ReferenceEqual(boxed, Expression.Constant(null, typeof(object))),
                        Expression.Default(typeof(TValue)),
                        Expression.Convert(Expression.Convert(boxed, liftedUnderlying), typeof(TValue))));
            }
        }

        try {
            return Expression.Lambda<Func<TDto, TValue>>(body, dto).Compile();
        } catch (Exception exception) when (exception is NotSupportedException or PlatformNotSupportedException) {
            // Ahead-of-time targets without IL emission fall back to the interpreter inside Compile(); the rare
            // environment where even that fails keeps the pre-cache behavior: a reflection read whose result is
            // cast, boxing included. Known, accepted divergence in this last-resort path only: a coercion selector
            // between an enum and its underlying type unbox-fails here, because the generic cast targets TValue
            // directly.
            return dtoInstance => (TValue)property.GetValue(dtoInstance)!;
        }
    }

    #endregion

}
