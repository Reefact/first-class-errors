using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace JustDummies.Analyzers;

/// <summary>
///     Suppresses a diagnostic on the shape a test uses to assert that a call fails: the offending expression is the
///     <b>entire</b> body of an expression-bodied lambda handed to another call — <c>Assert.Throws(() =&gt; illegal)</c>,
///     <c>Check.ThatCode(() =&gt; illegal)</c>, <c>Should.Throw(() =&gt; illegal)</c>.
/// </summary>
/// <remarks>
///     Deliberately framework-agnostic: it names no assertion library, so it covers the ones this repository uses and
///     the ones a consumer brings. It is also deliberately narrow — the expression must be the whole lambda body, so
///     arrange code inside <c>Any.Reproducibly(() =&gt; { ... })</c> stays reported, the call there being one statement
///     of a block rather than the body itself.
/// </remarks>
internal static class NegativeTestGuard {

    public static bool IsSoleBodyOfLambdaArgument(SyntaxNode expression) {
        if (expression.Parent is not LambdaExpressionSyntax lambda) { return false; }
        if (lambda.Body != expression) { return false; }

        return lambda.Parent is ArgumentSyntax;
    }

}
