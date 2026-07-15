# Premiers pas

🌍 **Langues :**  
🇬🇧 [English](./GettingStarted.en.md) | 🇫🇷 Français (ce fichier)

Ce guide vous mène d’un projet vide à votre premier catalogue d’erreurs généré.

Vous allez :

1. installer la bibliothèque et le CLI ;
2. activer la génération pour un projet ;
3. définir une erreur documentée ;
4. l’utiliser dans le code ;
5. générer le catalogue.

## 1. Installer le package et le CLI

Dans le projet applicatif :

```bash
dotnet add package FirstClassErrors
```

Installez une fois le CLI de documentation sur votre machine :

```bash
dotnet tool install --global FirstClassErrors.Cli
```

## 2. Activer la génération pour le projet

Ajoutez cette propriété directement dans le fichier du projet qui contient vos erreurs :

```xml
<PropertyGroup>
  <GenerateErrorDocumentation>true</GenerateErrorDocumentation>
</PropertyGroup>
```

Le marqueur doit être présent dans le `.csproj` lui-même. Les projets qui ne le déclarent pas sont ignorés lorsque le CLI analyse une solution.

## 3. Définir une situation d’erreur

Créez une classe factory statique. Chaque méthode factory représente une situation précise reconnue par le système. Les exemples utilisent un petit type valeur `Amount` doté d’une `Currency` ; remplacez-le par n’importe quel type de votre propre domaine.

```csharp
using FirstClassErrors;

[ProvidesErrorsFor(nameof(Amount))]
public static class InvalidAmountOperationError {

    [DocumentedBy(nameof(CurrencyMismatchDocumentation))]
    internal static DomainError CurrencyMismatch(Amount left, Amount right) {
        return DomainError.Create(
                Code.CurrencyMismatch,
                diagnosticMessage: $"Impossible d’additionner {left} et {right} car leurs devises diffèrent.")
            .WithPublicMessage(
                shortMessage: "Les montants utilisent des devises différentes.",
                detailedMessage: "Les deux montants doivent utiliser la même devise.");
    }

    private static class Code {
        public static readonly ErrorCode CurrencyMismatch =
            ErrorCode.Create("AMOUNT_CURRENCY_MISMATCH");
    }
}
```

Quatre éléments sont essentiels :

- `[ProvidesErrorsFor]` relie la classe factory au concept dont elle déclare les erreurs ; le générateur s’en sert pour les découvrir et les regrouper ;
- le nom de la factory exprime la situation dans le code ;
- le code d’erreur constitue son identité stable et lisible par machine ;
- le message de diagnostic est interne, tandis que les messages publics sont sûrs pour les appelants.

## 4. Ajouter la documentation structurée

L’attribut `[DocumentedBy]` relie la factory à une méthode de documentation située dans la même classe :

```csharp
private static ErrorDocumentation CurrencyMismatchDocumentation() {
    return DescribeError.WithTitle("Incohérence de devise")
                        .WithDescription(
                            "Cette erreur survient lorsqu’une opération combine des montants exprimés dans des devises différentes.")
                        .WithRule(
                            "Une opération monétaire doit utiliser une devise commune.")
                        .WithDiagnostic(
                            "Les montants ont atteint l’opération sans avoir été convertis dans une devise commune.",
                            ErrorOrigin.Internal,
                            "Vérifiez à quel endroit les montants auraient dû être convertis avant cette opération.")
                        .AndDiagnostic(
                            "L’appelant a fourni des montants dans des devises qui ne peuvent pas être combinées directement.",
                            ErrorOrigin.External,
                            "Vérifiez les devises fournies par l’appelant.")
                        .WithExamples(() => CurrencyMismatch(
                            new Amount(10m, Currency.EUR),
                            new Amount(12m, Currency.USD)));
}
```

Il s’agit de connaissance structurée, pas d’un commentaire : le générateur peut extraire le titre, l’explication, la règle, les hypothèses de diagnostic et l’exemple réellement produit par la factory.

Chaque diagnostic est une hypothèse : une cause plausible, une origine (le soupçon porte-t-il sur l’intérieur du système ou sur un appelant externe ?) et une piste d’investigation.

## 5. Utiliser l’erreur

Lorsque l’échec est exceptionnel, transformez l’erreur en son exception associée :

```csharp
public Amount Add(Amount other) {
    if (Currency != other.Currency) {
        throw InvalidAmountOperationError.CurrencyMismatch(this, other).ToException();
    }

    return new Amount(Value + other.Value, Currency);
}
```

Le code métier nomme la situation sans répéter les codes ni les messages.

Lorsque l’échec fait normalement partie du flux, transportez la même `Error` sans la lever :

```csharp
public static Outcome<Amount> TryAdd(Amount left, Amount right) {
    if (left.Currency != right.Currency) {
        return Outcome<Amount>.Failure(
            InvalidAmountOperationError.CurrencyMismatch(left, right));
    }

    return Outcome<Amount>.Success(
        new Amount(left.Value + right.Value, left.Currency));
}
```

Ces deux usages ne définissent pas deux erreurs différentes. Ils transportent la même situation d’erreur de deux manières différentes.

## 6. Générer le catalogue

Construisez la solution, puis générez un catalogue Markdown :

```bash
dotnet build MyApp.sln -c Release

fce generate \
  --solution MyApp.sln \
  --configuration Release \
  --no-build \
  --format markdown \
  --service-name my-api \
  --output artifacts/errors.md
```

`--configuration Release` correspond au build ci-dessus : avec `--no-build`, le générateur lit les assemblies produits par cette configuration précise (à défaut, il utilise `Debug`).

Le document généré contient une entrée pour `AMOUNT_CURRENCY_MISMATCH`, avec sa description, sa règle, ses hypothèses de diagnostic et les messages produits par l’exemple.

Un résultat abrégé ressemble à ceci :

```markdown
## Incohérence de devise

**Code :** `AMOUNT_CURRENCY_MISMATCH`

Cette erreur survient lorsqu’une opération combine des montants exprimés dans des devises différentes.

**Règle :** Une opération monétaire doit utiliser une devise commune.
```

Commitez ou publiez ce catalogue selon votre workflow de livraison. Pour automatiser sa génération en CI, consultez [Intégration CI/CD et exploitation](OperationalIntegration.fr.md).

## Étapes suivantes facultatives

- Ajoutez des faits propres à chaque occurrence avec le [contexte d’erreur](ErrorContext.fr.md).
- Découvrez les catégories disponibles dans [Taxonomie et composition des erreurs](ErrorTaxonomy.fr.md).
- Apprenez quand lever ou retourner un `Outcome<T>` dans les [cas d’usage](UsagePatterns.fr.md).
- Protégez les codes et clés de contexte stables avec le [versionnage du catalogue](CatalogVersioning.fr.md).

---

<div align="center">
<a href="README.fr.md#-documentation">↑ Table des matières</a> · <a href="DesignPrinciples.fr.md">Principes de conception →</a>
</div>

---
