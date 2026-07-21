# Analyseurs FirstClassErrors

🌍 **Langues:**  
🇬🇧 [English](./README.md) | 🇫🇷 Français (ce fichier)

Les analyseurs FirstClassErrors sont des règles Roslyn qui s'exécutent pendant la compilation de votre projet. Ils transforment en diagnostics de compilation des erreurs que le runtime et le pipeline de documentation de FirstClassErrors ne signaleraient sinon que tardivement — voire jamais. Ces règles sont **incluses dans le package NuGet `FirstClassErrors`** : tout projet qui le référence en bénéficie automatiquement, sans installation supplémentaire.

Chaque règle a un identifiant stable `FCExxx`. Les erreurs sont des défauts durs ; les avertissements signalent des fautes probables ; les règles d'info sont des conventions, et plusieurs sont opt-in (voir chaque page pour les activer).

## Codes d'erreur

| Règle | Sévérité | Défaut | Description |
|------|----------|---------|-------------|
| [FCE001 DuplicateErrorCode](FCE001.fr.md) | 🔴 Error | activée | Le même code d'erreur littéral est créé par plus d'un ErrorCode.Create("...") dans la compilation. |
| [FCE002 EmptyErrorCode](FCE002.fr.md) | 🔴 Error | activée | ErrorCode.Create est appelé avec un littéral vide, composé d'espaces, ou null. |
| [FCE003 NonLiteralErrorCode](FCE003.fr.md) | 🔵 Info | opt-in | ErrorCode.Create est appelé avec un argument qui n'est pas une constante de compilation. |
| [FCE004 InvalidErrorCodeFormat](FCE004.fr.md) | 🔵 Info | opt-in | Un code d'erreur littéral ne respecte pas la convention UPPER_SNAKE_CASE. |
| [FCE005 TooGenericErrorCode](FCE005.fr.md) | 🔵 Info | opt-in | Un code d'erreur littéral fait partie d'un petit ensemble de mots fourre-tout (ERROR, INVALID, FAILED, …) sans valeur diagnostique. |

## Câblage de la documentation

| Règle | Sévérité | Défaut | Description |
|------|----------|---------|-------------|
| [FCE006 DocumentedByTargetNotFound](FCE006.fr.md) | 🔴 Error | activée | Un [DocumentedBy("...")] désigne une méthode de documentation qui n'existe pas sur le type contenant. |
| [FCE007 DocumentedByInvalidSignature](FCE007.fr.md) | 🔴 Error | activée | La méthode référencée par [DocumentedBy] existe mais ne peut pas servir de factory de documentation. |
| [FCE008 DocumentedByWithoutProvidesErrorsFor](FCE008.fr.md) | 🔴 Error | activée | Un type déclare des factories [DocumentedBy] mais n'a pas [ProvidesErrorsFor]. |
| [FCE009 ErrorFactoryNotDocumented](FCE009.fr.md) | 🟠 Warning | activée | Une factory statique non privée qui retourne une Error dans un type [ProvidesErrorsFor] ne porte pas [DocumentedBy]. |
| [FCE010 MultipleFactoriesShareDocumentation](FCE010.fr.md) | 🟠 Warning | activée | Deux factories (ou plus) du même type pointent leur [DocumentedBy] vers la même méthode de documentation. |

## Contenu de la documentation

| Règle | Sévérité | Défaut | Description |
|------|----------|---------|-------------|
| [FCE011 DuplicateDocumentedCode](FCE011.fr.md) | 🔴 Error | activée | Plus d'une factory documentée produit le même code d'erreur en référençant le même champ ErrorCode. |
| [FCE012 EmptyExamples](FCE012.fr.md) | 🟠 Warning | activée | L'appel terminal WithExamples() du DSL de documentation ne reçoit aucune factory d'exemple. |
| [FCE013 ExampleDoesNotCallDocumentedFactory](FCE013.fr.md) | 🟠 Warning | activée | Un exemple passé à WithExamples(...) n'appelle aucune factory du type qui déclare la documentation. |
| [FCE014 ShortMessageSameAsDetailedMessage](FCE014.fr.md) | 🔵 Info | activée | WithPublicMessage(short, detailed) est appelé avec deux messages littéraux identiques. |
| [FCE015 DocumentationTitleTooGeneric](FCE015.fr.md) | 🔵 Info | opt-in | Un WithTitle("...") utilise un titre qui ne décrit rien (Error, Invalid value, Failure, …). |

## Usage

| Règle | Sévérité | Défaut | Description |
|------|----------|---------|-------------|
| [FCE016 UnusedToExceptionResult](FCE016.fr.md) | 🟠 Warning | activée | Error.ToException() est appelé comme instruction isolée, ou son résultat est explicitement ignoré avec `_ =`. |
| [FCE017 SensitiveDataInErrorContext](FCE017.fr.md) | 🟠 Warning | opt-in | Le nom d'une ErrorContextKey désigne un secret, un identifiant d'authentification ou une donnée personnelle (mot de passe, token, secret, chaîne de connexion, carte bancaire, …). |
| [FCE018 OversizedErrorContextValue](FCE018.fr.md) | 🔵 Info | opt-in | Le type de valeur d'une ErrorContextKey est un gros payload (tableau d'octets, Stream ou FileInfo) qui n'a pas sa place dans un contexte destiné aux logs. |
| [FCE019 TryCatchesTooBroadly](FCE019.fr.md) | 🟠 Warning | activée | Outcome.Try attrape System.Exception, transformant des bugs inattendus en erreurs anticipées au lieu de la seule exception que l'opération est censée lever. |
| [FCE020 TryCatchesRichProtocolException](FCE020.fr.md) | 🟠 Warning | opt-in | Outcome.Try attrape un échec de protocole (HttpRequestException, DbException, SocketException, …) dont la donnée de statut ou de résultat est perdue une fois réduite à une levée. |
| [FCE021 PreferNonThrowingAlternativeToTry](FCE021.fr.md) | 🟠 Warning | activée | Outcome.Try enveloppe un appel qui a déjà une contrepartie non-levante TryXxx / TryCreate disponible pour le framework cible ; envisagez de mapper son résultat (conseil — à supprimer là où la contrepartie n'est pas un vrai inverse). |
| [FCE022 TryCatchesCancellation](FCE022.fr.md) | 🟠 Warning | activée | Outcome.Try lie TException à OperationCanceledException (ou un sous-type) ; Try laisse toujours l'annulation se propager, donc le catch est inatteignable et le mapper ne s'exécute jamais. |

## Configuration

La sévérité de chaque règle se règle dans `.editorconfig`, par exemple :

```ini
# activer une règle opt-in
dotnet_diagnostic.FCE004.severity = warning

# ou faire taire une règle
dotnet_diagnostic.FCE014.severity = none
```

> `FCE001` et `FCE011` sont des vérifications sur toute la compilation : elles apparaissent au build / à l'analyse de la solution entière, pas à la frappe dans un seul fichier.
