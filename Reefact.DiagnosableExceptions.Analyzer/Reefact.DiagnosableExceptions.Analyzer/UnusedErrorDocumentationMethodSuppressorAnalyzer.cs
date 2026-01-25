#region Usings declarations

using System.Collections.Immutable;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

#endregion

namespace Reefact.DiagnosableExceptions.Analyzer;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class UnusedErrorDocumentationMethodSuppressorAnalyzer : DiagnosticSuppressor {

    #region Static members

    // 1. Définition des descripteurs de suppression pour chaque diagnostic ciblé.
    private static readonly SuppressionDescriptor SuppressUnusedPrivateMemberIDE0051 =
        new(
            "SPR0001",                                 // Identifiant unique de la suppression
            DiagnosticId.IDE0051,                      // ID du diagnostic à supprimer (Unused private member)
            "Méthode marquée par [ErrorDocumentation]" // Justification affichée
        );

    private static readonly SuppressionDescriptor SuppressUnusedPrivateMemberSonarS1144 =
        new(
            "SPR0002",
            DiagnosticId.S1144, // SonarQube: Unused private members
            "Méthode marquée par [ErrorDocumentation]"
        );

    private static readonly SuppressionDescriptor SuppressUnusedPrivateMemberCA1811 =
        new(
            "SPR0003",
            DiagnosticId.CA1811, // Code Analysis: Avoid uncalled private code
            "Méthode marquée par [ErrorDocumentation]"
        );

    #endregion

    // 2. Indiquer la liste des suppressions supportées par cet analyseur.
    public override ImmutableArray<SuppressionDescriptor> SupportedSuppressions => ImmutableArray.Create(
        SuppressUnusedPrivateMemberIDE0051,
        SuppressUnusedPrivateMemberSonarS1144,
        SuppressUnusedPrivateMemberCA1811
    );

    // 3. Logique de suppression des diagnostics.
    public override void ReportSuppressions(SuppressionAnalysisContext context) {
        // On parcourt tous les diagnostics rapportés dans la compilation analysée.
        foreach (Diagnostic diagnostic in context.ReportedDiagnostics) {
            // Filtrer rapidement les diagnostics qui ne sont pas ceux que l'on gère.
            string diagId = diagnostic.Id;
            if (diagId != DiagnosticId.IDE0051 && diagId != DiagnosticId.S1144 && diagId != DiagnosticId.CA1811) {
                continue; // on ne traite pas d'autres diagnostics ici
            }

            // Récupérer le nœud de syntaxe ayant déclenché le diagnostic.
            Location   location   = diagnostic.Location;
            SyntaxTree syntaxTree = location.SourceTree;
            if (syntaxTree == null) {
                continue;
            }
            // Trouver le nœud dans l'arbre syntaxique correspondant à l'emplacement du diagnostic.
            SyntaxNode root = syntaxTree.GetRoot(context.CancellationToken);
            SyntaxNode node = root.FindNode(location.SourceSpan);

            // Obtenir le symbole (sémantique) associé à ce nœud de déclaration.
            SemanticModel semanticModel = context.GetSemanticModel(syntaxTree);
            ISymbol       symbol        = semanticModel.GetDeclaredSymbol(node, context.CancellationToken);
            if (symbol is IMethodSymbol methodSymbol) {
                // Vérifier si la méthode est décorée par l'attribut [ErrorDocumentation].
                foreach (AttributeData attr in methodSymbol.GetAttributes()) {
                    string attrClassName = attr.AttributeClass?.Name;
                    if (attrClassName == "ErrorDocumentationAttribute" || attrClassName == "ErrorDocumentation") {
                        // 4. Si oui, on crée la suppression du diagnostic correspondant.
                        SuppressionDescriptor descriptor = diagId switch {
                            DiagnosticId.IDE0051 => SuppressUnusedPrivateMemberIDE0051,
                            DiagnosticId.S1144   => SuppressUnusedPrivateMemberSonarS1144,
                            DiagnosticId.CA1811  => SuppressUnusedPrivateMemberCA1811,
                            _                    => null
                        };
                        if (descriptor != null) {
                            context.ReportSuppression(Suppression.Create(descriptor, diagnostic));
                        }

                        break; // on peut sortir dès qu'on a trouvé l'attribut
                    }
                }
            }
        }
    }

}