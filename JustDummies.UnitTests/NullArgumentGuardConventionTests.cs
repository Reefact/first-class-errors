#region Usings declarations

using System.Collections;
using System.Linq.Expressions;
using System.Reflection;

using NFluent;

#endregion

namespace JustDummies.UnitTests;

/// <summary>
///     The null-argument guard convention, enforced by reflection over the whole library surface: <b>every</b>
///     <c>public</c> or <c>internal</c> member (constructor or method) that takes a non-nullable reference-type
///     argument must reject <c>null</c> with an <see cref="ArgumentNullException" /> naming the offending parameter.
///     A caller — even another class of this assembly — is outside the class it calls, so the boundary validates
///     what crosses it; only what a validating member has already accepted is trusted inside.
/// </summary>
/// <remarks>
///     <para>
///         This is a single self-maintaining guard: it discovers members through reflection, so a new generator,
///         factory, or fluent method is held to the convention automatically, with nothing to add here. Value-type
///         and nullable (<c>?</c>) parameters are excluded by design — the former cannot be <c>null</c>, the latter
///         are deliberately optional. Exception types are excluded too: constructing an exception must never itself
///         throw while an error is being handled or logged — as are the types that exist only to build one, which
///         declare themselves with <c>[BuiltOnTheFailurePath]</c> (ADR-0064).
///     </para>
///     <para>
///         Testing the internal boundary (the <c>Create</c> factories and internal constructors the public API can
///         never route a <c>null</c> through) requires reaching internals, which is why JustDummies opens them to
///         this suite — see ADR-0045. The test uses .NET 6+ reflection nullability metadata, so it runs on the
///         modern leg only and is excluded from the net472 support-floor build.
///     </para>
/// </remarks>
public sealed class NullArgumentGuardConventionTests {

    private static readonly Assembly              LibraryAssembly = typeof(Any).Assembly;
    private static readonly NullabilityInfoContext Nullability     = new();
    private static readonly List<object>          Samples         = HarvestSamples();

    [Fact(DisplayName = "Every public/internal member rejects a null non-nullable reference argument with ArgumentNullException.")]
    public void EveryPublicOrInternalMemberGuardsItsNonNullableReferenceArguments() {
        List<string> violations = [];
        List<string> uncovered  = [];

        foreach (Type declared in LibraryAssembly.GetTypes()) {
            if (!IsInScope(declared)) { continue; }

            Type type = declared;
            if (declared.IsGenericTypeDefinition) {
                if (declared.IsAbstract) { continue; } // an abstract base (e.g. AnyCollection`3): covered through its concrete subclasses
                if (!TryClose(declared, out type!)) {
                    uncovered.Add($"type {declared.Name}: could not close generic definition");
                    continue;
                }
            }

            foreach (MemberDescriptor member in MembersOf(type)) {
                ProcessMember(member, violations, uncovered);
            }
        }

        // A parameter the harness could not exercise is a hole in the convention's coverage, not a pass: it is
        // reported alongside the missing guards so it is either given a sample here or covered by an explicit test,
        // never silently skipped.
        List<string> failures = uncovered.Select(entry => $"[uncovered]     {entry}")
                                         .Concat(violations.Select(entry => $"[missing-guard] {entry}"))
                                         .OrderBy(line => line, StringComparer.Ordinal)
                                         .ToList();

        Check.WithCustomMessage(
                  $"Null-argument guard convention — {violations.Count} member(s) missing a guard, "
                + $"{uncovered.Count} parameter(s) the harness could not exercise:{Environment.NewLine}"
                + string.Join(Environment.NewLine, failures))
             .That(failures)
             .IsEmpty();
    }

    #region Member enumeration

    private readonly struct MemberDescriptor(MethodBase invoke, MethodBase classify, Type receiver) {

        public MethodBase Invoke   { get; } = invoke;   // closed and callable
        public MethodBase Classify { get; } = classify; // open enough to read annotations and generic constraints
        public Type       Receiver { get; } = receiver; // the (closed) type an instance member is called on

    }

    private static IEnumerable<MemberDescriptor> MembersOf(Type type) {
        return ConstructorsOf(type).Concat(MethodsOf(type));
    }

    private static IEnumerable<MemberDescriptor> ConstructorsOf(Type type) {
        if (type is { IsAbstract: true } or { IsInterface: true }) { yield break; }

        foreach (ConstructorInfo ctor in type.GetConstructors(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)) {
            if (IsAccessible(ctor)) { yield return new MemberDescriptor(ctor, ctor, type); }
        }
    }

    private static IEnumerable<MemberDescriptor> MethodsOf(Type type) {
        BindingFlags methodFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance;
        if (type.IsAbstract) { methodFlags |= BindingFlags.DeclaredOnly; } // instance methods of an abstract base are reached through its concrete subclasses

        foreach (MethodInfo method in type.GetMethods(methodFlags)) {
            if (!IsReachableMethod(method)) { continue; }

            if (method.IsGenericMethodDefinition) {
                if (!TryClose(method, out MethodInfo? closed)) { continue; }
                yield return new MemberDescriptor(closed!, method, type);
            } else {
                yield return new MemberDescriptor(method, method, type);
            }
        }
    }

    // The methods the harness can actually call: not object's own, not an accessor or operator, not compiler-generated,
    // accessible, and not abstract.
    private static bool IsReachableMethod(MethodInfo method) {
        if (method.DeclaringType == typeof(object)) { return false; }
        if (method.IsSpecialName) { return false; }      // property/event accessors, operators
        if (method.Name.Contains('<')) { return false; } // compiler-generated
        if (!IsAccessible(method)) { return false; }

        return method is not { IsAbstract: true }; // no body to reach directly
    }

    private static bool IsInScope(Type type) {
        if (type.IsInterface || type.IsEnum) { return false; }
        if (typeof(Delegate).IsAssignableFrom(type)) { return false; }
        if (typeof(Exception).IsAssignableFrom(type)) { return false; }
        // Types that exist only to build one of the library's exceptions are exempt for the same reason exception
        // types are, and say so with the marker rather than being inferred (ADR-0064, widening ADR-0045).
        if (type.GetCustomAttribute<BuiltOnTheFailurePathAttribute>() is not null) { return false; }
        if (type.Name.StartsWith('<')) { return false; }
        if (type.GetCustomAttribute<System.Runtime.CompilerServices.CompilerGeneratedAttribute>() is not null) { return false; }

        if (type.IsNested) {
            return type.IsNestedPublic || type.IsNestedAssembly || type.IsNestedFamORAssem;
        }

        return type.IsPublic || type.IsNotPublic; // top-level public or internal
    }

    private static bool IsAccessible(MethodBase member) {
        return member.IsPublic || member.IsAssembly || member.IsFamilyOrAssembly; // public / internal / protected internal
    }

    #endregion

    #region Per-member verification

    private static void ProcessMember(MemberDescriptor member, List<string> violations, List<string> uncovered) {
        ParameterInfo[] invokeParams   = member.Invoke.GetParameters();
        ParameterInfo[] classifyParams = ClassificationParameters(member);

        // A validation helper that carries a caller-supplied name (RequireHost(host, parameterName)) reports that
        // forwarded name, not its own — so for such members any ArgumentNullException, whatever its ParamName, honours
        // the convention. Whether an exception is thrown at all is still checked strictly.
        bool forwardsName = invokeParams.Any(parameter => parameter.Name is "parameterName" or "paramName");

        for (int index = 0; index < invokeParams.Length; index++) {
            ParameterInfo classify = classifyParams[index];
            if (!IsGuardTarget(classify)) { continue; }

            VerifyParameterGuard(member, invokeParams, index, classify, forwardsName, violations, uncovered);
        }
    }

    // One parameter, exercised on its own: every other argument is supplied, so whatever the member throws is a
    // verdict about THIS parameter's guard and nothing else.
    private static void VerifyParameterGuard(MemberDescriptor member, ParameterInfo[] invokeParams, int index, ParameterInfo classify,
                                             bool forwardsName, List<string> violations, List<string> uncovered) {
        string description = Describe(member, classify);

        if (!TryBuildInvocation(member, invokeParams, index, out object? instance, out object?[] arguments, out string? failure)) {
            uncovered.Add($"{description}: could not build arguments — {failure}");

            return;
        }

        try {
            if (member.Invoke is ConstructorInfo ctor) { ctor.Invoke(arguments); } else { member.Invoke.Invoke(instance, arguments); }
            violations.Add($"{description}: expected ArgumentNullException, nothing was thrown");
        } catch (TargetInvocationException invocation) {
            Exception? thrown = invocation.InnerException;
            if (thrown is ArgumentNullException guard && (guard.ParamName == classify.Name || forwardsName)) { return; }

            string got = thrown is ArgumentNullException other
                             ? $"ArgumentNullException(ParamName=\"{other.ParamName}\")"
                             : thrown?.GetType().Name ?? "null";
            violations.Add($"{description}: expected ArgumentNullException(\"{classify.Name}\") but got {got}");
        } catch (Exception unexpected) {
            uncovered.Add($"{description}: invocation failed — {Root(unexpected).GetType().Name}: {Root(unexpected).Message}");
        }
    }

    // The receiver and the argument array to invoke with, `index` left null. Answers false when the harness has no
    // sample for some other parameter — which is a coverage hole to report, not a missing guard.
    private static bool TryBuildInvocation(MemberDescriptor member, ParameterInfo[] invokeParams, int index,
                                           out object? instance, out object?[] arguments, out string? failure) {
        try {
            instance  = member.Invoke.IsStatic || member.Invoke is ConstructorInfo ? null : Sample(member.Receiver);
            arguments = new object?[invokeParams.Length];
            for (int j = 0; j < invokeParams.Length; j++) {
                arguments[j] = j == index ? null : ArgumentFor(invokeParams[j]);
            }

            failure = null;

            return true;
        } catch (Exception build) {
            instance  = null;
            arguments = [];
            failure   = Root(build).Message;

            return false;
        }
    }

    // Parameters to read annotations and generic constraints from: for a generic method, the open definition (which
    // still carries `T` and its constraints); for a member of a constructed generic type, the definition member
    // (whose parameters carry the nullable annotations the constructed copy elides).
    private static ParameterInfo[] ClassificationParameters(MemberDescriptor member) {
        if (member.Classify is MethodInfo { IsGenericMethodDefinition: true } definition) { return definition.GetParameters(); }

        Type? declaring = member.Invoke.DeclaringType;
        if (declaring is { IsGenericType: true, IsGenericTypeDefinition: false }) {
            try {
                Type openDeclaring = declaring.GetGenericTypeDefinition();
                if (openDeclaring.Module.ResolveMember(member.Invoke.MetadataToken) is MethodBase open) { return open.GetParameters(); }
            } catch {
                // fall through to the constructed parameters
            }
        }

        return member.Invoke.GetParameters();
    }

    private static bool IsGuardTarget(ParameterInfo parameter) {
        Type type = parameter.ParameterType;
        if (type.IsByRef) { return false; } // out/ref/in

        if (type.IsGenericParameter) {
            return (type.GenericParameterAttributes & GenericParameterAttributes.ReferenceTypeConstraint) != 0;
        }

        if (type.IsValueType || type.IsPointer) { return false; } // includes Nullable<T>, enums, structs

        try {
            return Nullability.Create(parameter).ReadState == NullabilityState.NotNull;
        } catch {
            return true; // unknown annotation → treat as a target so any gap shows up as a visible violation
        }
    }

    private static string Describe(MemberDescriptor member, ParameterInfo parameter) {
        string kind      = member.Invoke is ConstructorInfo ? "ctor" : member.Invoke.Name;
        string signature = string.Join(", ", member.Invoke.GetParameters().Select(p => $"{Readable(p.ParameterType)} {p.Name}"));

        return $"{Readable(member.Receiver)}.{kind}({signature}) [param '{parameter.Name}']";
    }

    #endregion

    #region Sample values

    private static object? ArgumentFor(ParameterInfo parameter) {
        Type type = parameter.ParameterType;
        if (type.IsByRef) { type = type.GetElementType()!; }

        if (type.IsValueType) {
            return Nullable.GetUnderlyingType(type) is not null ? null : Activator.CreateInstance(type);
        }

        // A nullable non-target reference can simply be null; a non-nullable one needs a real, valid value so the
        // member reaches (and only reaches) the guard of the parameter under test.
        try {
            if (Nullability.Create(parameter).ReadState == NullabilityState.Nullable) { return null; }
        } catch {
            // fall through and build a value
        }

        return Sample(type);
    }

    private static object Sample(Type type) {
        foreach (object candidate in Samples) {
            if (type.IsInstanceOfType(candidate)) { return candidate; }
        }

        if (type == typeof(string)) { return "sample"; }

        if (type.IsArray) {
            Type element = type.GetElementType()!;
            Array array  = Array.CreateInstance(element, 1);
            array.SetValue(Element(element), 0);

            return array;
        }

        if (type.IsGenericType) {
            Type   definition = type.GetGenericTypeDefinition();
            Type[] arguments  = type.GetGenericArguments();

            if (definition == typeof(IAny<>)) { return CreateAny(arguments[0]); }

            if (definition == typeof(IEnumerable<>) || definition == typeof(IReadOnlyList<>) || definition == typeof(IReadOnlyCollection<>)
             || definition == typeof(IList<>) || definition == typeof(ICollection<>) || definition == typeof(List<>)) {
                Type  listType = typeof(List<>).MakeGenericType(arguments[0]);
                IList list     = (IList)Activator.CreateInstance(listType)!;
                list.Add(Element(arguments[0]));

                return list;
            }

            if (definition == typeof(IReadOnlyDictionary<,>) || definition == typeof(IDictionary<,>) || definition == typeof(Dictionary<,>)) {
                Type dictionaryType = typeof(Dictionary<,>).MakeGenericType(arguments);
                IDictionary dictionary     = (IDictionary)Activator.CreateInstance(dictionaryType)!;
                dictionary[Element(arguments[0])!] = Element(arguments[1]);

                return dictionary;
            }
        }

        if (typeof(Delegate).IsAssignableFrom(type)) { return CreateDelegate(type); }

        throw new NotSupportedException($"no sample available for {Readable(type)}");
    }

    private static object? Element(Type type) {
        if (type == typeof(string)) { return "x"; }
        if (type.IsValueType) { return Nullable.GetUnderlyingType(type) is not null ? null : Activator.CreateInstance(type); }

        return Sample(type);
    }

    private static object CreateAny(Type valueType) {
        MethodInfo oneOf = typeof(Any).GetMethods(BindingFlags.Public | BindingFlags.Static)
                                      .First(method => method is { Name: "OneOf", IsGenericMethodDefinition: true }
                                                    && method.GetParameters() is [{ ParameterType.IsArray: true }])
                                      .MakeGenericMethod(valueType);

        Array pool = Array.CreateInstance(valueType, 1);
        pool.SetValue(Element(valueType), 0);

        return oneOf.Invoke(null, [pool])!;
    }

    private static object CreateDelegate(Type delegateType) {
        MethodInfo         signature  = delegateType.GetMethod("Invoke")!;
        ParameterExpression[] inputs  = signature.GetParameters().Select(p => Expression.Parameter(p.ParameterType)).ToArray();
        Type               returnType = signature.ReturnType;

        Expression body = returnType == typeof(void)
                              ? Expression.Empty()
                              : Expression.Constant(Element(returnType), returnType);

        return Expression.Lambda(delegateType, body, inputs).Compile();
    }

    // A pool of live, valid internal instances (random sources, interval/string/uri specs, collection state, regex
    // nodes, ...) harvested by walking the object graph of a handful of real generators. Reusing what the library
    // itself builds is what lets the harness supply a valid `spec` when testing a `source` guard, without wiring up
    // each engine type by hand.
    private static List<object> HarvestSamples() {
        HashSet<object> visited  = new(ReferenceEqualityComparer.Instance);
        Queue<object>   frontier = new();

        void Seed(object? root) {
            if (root is null or string) { return; }
            if (root.GetType().IsValueType) { return; } // primitives, enums, structs are never harvested samples
            if (visited.Add(root)) { frontier.Enqueue(root); }
        }

        SeedContextRoots(Seed);
        SeedRepresentativeGenerators(Seed);

        return WalkReachableObjects(frontier, Seed);
    }

    // The context itself, the two random sources, and every parameterless generator AnyContext exposes.
    private static void SeedContextRoots(Action<object?> seed) {
        AnyContext context = new(0);
        seed(context);
        seed(new FixedRandomSource(0));
        seed(new SeededRandom(0));

        foreach (MethodInfo factory in typeof(AnyContext).GetMethods(BindingFlags.Public | BindingFlags.Instance)) {
            if (factory.DeclaringType == typeof(object)) { continue; }
            if (factory.IsSpecialName || factory.GetParameters().Length != 0) { continue; }
            if (factory.ReturnType == typeof(void) || factory.ReturnType == typeof(AnyContext) || factory.IsGenericMethodDefinition) { continue; }
            try { seed(factory.Invoke(context, null)); } catch { /* best effort */ }
        }
    }

    // The shapes AnyContext's parameterless factories cannot reach: everything that needs an element generator, a
    // type argument or a pattern to exist at all.
    private static void SeedRepresentativeGenerators(Action<object?> seed) {
        IAny<string> strings = Any.String();
        void Try(Func<object?> build) {
            try { seed(build()); } catch { /* best effort */ }
        }

        // Collections closed on the representative element type the harness uses (string), plus a dictionary and an enum.
        Try(() => Any.ListOf(strings));
        Try(() => Any.SetOf(strings));
        Try(() => Any.SequenceOf(strings));
        Try(() => Any.ArrayOf(strings));
        Try(() => Any.DictionaryOf(strings, strings));
        Try(() => Any.OneOf("a", "b", "c"));
        Try(() => Any.Enum<DayOfWeek>());
        Try(() => strings.As(value => value.Length));
        Try(() => strings.OrNull());

        // One pattern per regex-node shape, so every RegexNode subtype is reachable through AnyPattern's tree.
        Try(() => Any.StringMatching("abc"));    // sequence
        Try(() => Any.StringMatching("a|b|c"));  // alternation
        Try(() => Any.StringMatching("a{2,3}")); // repeat
        Try(() => Any.StringMatching("[a-z]"));  // characters
    }

    // Breadth-first over what the seeded roots can reach, bounded by a budget so a cyclic or unexpectedly wide graph
    // cannot hang the suite.
    private static List<object> WalkReachableObjects(Queue<object> frontier, Action<object?> seed) {
        List<object> pool   = [];
        int          budget = 0;

        while (frontier.Count > 0 && budget++ < 5_000) {
            object current = frontier.Dequeue();
            pool.Add(current);

            SeedFields(current, seed);
            SeedParameterlessSteps(current, seed);
        }

        return pool;
    }

    private static void SeedFields(object current, Action<object?> seed) {
        for (Type? level = current.GetType(); level is not null && level != typeof(object); level = level.BaseType) {
            foreach (FieldInfo field in level.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)) {
                seed(field.GetValue(current));
            }
        }
    }

    // Follow parameterless generator-producing steps too, so a sibling generator reachable only through a
    // fluent call (e.g. the concrete URI generators returned by AnyUri) still enters the pool.
    private static void SeedParameterlessSteps(object current, Action<object?> seed) {
        if (current.GetType().Assembly != LibraryAssembly) { return; }

        foreach (MethodInfo step in current.GetType().GetMethods(BindingFlags.Public | BindingFlags.Instance)) {
            if (step.DeclaringType == typeof(object) || step.IsSpecialName || step.IsGenericMethodDefinition) { continue; }
            if (step.GetParameters().Length != 0 || step.ReturnType.Assembly != LibraryAssembly) { continue; }
            try { seed(step.Invoke(current, null)); } catch { /* best effort */ }
        }
    }

    #endregion

    #region Helpers

    private static bool TryClose(Type definition, out Type? closed) {
        try {
            closed = definition.MakeGenericType(definition.GetGenericArguments().Select(Representative).ToArray());

            return true;
        } catch {
            closed = null;

            return false;
        }
    }

    private static bool TryClose(MethodInfo definition, out MethodInfo? closed) {
        try {
            closed = definition.MakeGenericMethod(definition.GetGenericArguments().Select(Representative).ToArray());

            return true;
        } catch {
            closed = null;

            return false;
        }
    }

    private static Type Representative(Type parameter) {
        GenericParameterAttributes attributes  = parameter.GenericParameterAttributes & GenericParameterAttributes.SpecialConstraintMask;
        Type[]                     constraints = parameter.GetGenericParameterConstraints();

        if ((attributes & GenericParameterAttributes.NotNullableValueTypeConstraint) != 0) {
            return constraints.Any(constraint => constraint == typeof(Enum) || constraint.BaseType == typeof(Enum)) ? typeof(DayOfWeek) : typeof(int);
        }

        Type? baseConstraint = constraints.FirstOrDefault(constraint => constraint is { IsClass: true } && constraint != typeof(object));
        if (baseConstraint is not null) { return baseConstraint; }

        return constraints.All(constraint => !constraint.IsInterface || constraint.IsAssignableFrom(typeof(string))) ? typeof(string) : typeof(int);
    }

    private static Exception Root(Exception exception) {
        return exception is TargetInvocationException { InnerException: { } inner } ? inner : exception;
    }

    private static string Readable(Type type) {
        if (!type.IsGenericType) { return type.Name; }

        string name = type.Name;
        int    tick = name.IndexOf('`');
        if (tick >= 0) { name = name[..tick]; }

        return $"{name}<{string.Join(", ", type.GetGenericArguments().Select(Readable))}>";
    }

    #endregion

}
