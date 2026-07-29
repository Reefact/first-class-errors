# Analyseurs

🌍 **Langues:**  
🇬🇧 [English](./README.md) | 🇫🇷 Français (ce fichier)

Ce dépôt fournit des règles Roslyn avec deux packages. Elles s'exécutent pendant la compilation et transforment en diagnostics de compilation des erreurs que le runtime et le pipeline de documentation ne signaleraient sinon que tardivement — voire jamais. Les règles **FirstClassErrors** (`FCExxx`) sont incluses dans le package `FirstClassErrors` ; les règles **JustDummies** (`JDxxx`) sont incluses dans le package `JustDummies`. Tout projet qui référence un package en bénéficie automatiquement, sans installation supplémentaire.

Chaque règle a un identifiant stable (`FCExxx` ou `JDxxx`). Les erreurs sont des défauts durs ; les avertissements signalent des fautes probables ; les règles d'info sont des conventions, et plusieurs sont opt-in (voir chaque page pour les activer).

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

## JustDummies — Reproductibilité

Ces règles sont incluses dans le package **`JustDummies`** (pas FirstClassErrors) et empêchent un corps de test asynchrone d'avaler silencieusement ses propres échecs.

| Règle | Sévérité | Défaut | Description |
|-------|----------|--------|-------------|
| [JD001 AsyncBodyPassedToReproducibly](JD001.fr.md) | 🔴 Erreur | on | Une lambda async est passée à `Any.Reproducibly(Action)` synchrone ; liée à une Action elle devient async void et ses échecs ne font jamais échouer le test. Utilisez `Any.ReproduciblyAsync` et faites `await`. |
| [JD002 DiscardedReproduciblyAsyncResult](JD002.fr.md) | 🔴 Erreur | on | Le `Task` retourné par `Any.ReproduciblyAsync` est jeté (instruction isolée ou `_ =`) ; les échecs du corps sont perdus. Faites `await`. |
| [JD003 AwaitableBodyPassedToReproducibly](JD003.fr.md) | 🔴 Erreur | on | Une lambda synchrone dont le corps abandonne une tâche, ou un groupe de méthodes `async void`, atteint `Any.Reproducibly` ; la portée retourne avant l'exécution des assertions, et `CS4014` ne se déclenche pas. |
| [JD004 DiscardedSeedingResult](JD004.fr.md) | 🔴 Erreur | on | La poignée retournée par `Any.UseSeed` est jetée, laissant la graine épinglée pour la suite — ou `Any.WithSeed` est appelé pour son effet, alors qu'il n'épingle rien. |
| [JD007 DrawOutsideThePinnedScope](JD007.fr.md) | 🟠 Avertissement | on | Une valeur est tirée pendant la construction d'une classe de test `[Reproducible]`, qu'xUnit exécute avant l'ouverture de la portée de graine ; la graine rapportée ne la rejoue pas. |
| [JD008 ArbitraryValueInTheoryData](JD008.fr.md) | 🟠 Avertissement | on | Le fournisseur de données d'une théorie tire une valeur à la découverte, avant tout épinglage ; tous les cas partagent cette unique valeur. |
| [JD009 DrawInStaticInitializer](JD009.fr.md) | 🟠 Avertissement | on | Un initialiseur statique tire une seule fois pour toute la suite, sous le premier test exécuté, rendant les tests dépendants de l'ordre et rejouables depuis aucune graine. |
| [JD010 ReproducibleOnNonTestMethod](JD010.fr.md) | 🟠 Avertissement | on | `[Reproducible]` sur une méthode qu'xUnit ne traite jamais comme un test ; il n'épingle rien, et ressemble exactement à la forme active. |

## JustDummies — Usage

Un générateur est une *recette* immuable, et `Generate()` est la seule chose qui en matérialise une valeur. Ces règles ferment les deux façons dont cette distinction se perd silencieusement.

| Règle | Sévérité | Défaut | Description |
|-------|----------|--------|-------------|
| [JD005 GeneratorRenderedAsText](JD005.fr.md) | 🔴 Erreur | on | Un générateur est interpolé, concaténé ou passé à `ToString()` au lieu d'être généré ; aucun générateur ne surcharge `ToString()`, donc le texte obtenu est le nom de type du constructeur. |
| [JD006 DiscardedGeneratorResult](JD006.fr.md) | 🟠 Avertissement | on | Le générateur retourné par une contrainte est jeté en instruction isolée ; les générateurs étant immuables, l'invariant déclaré est silencieusement perdu. |
| [JD011 GeneratorWhereValueExpected](JD011.fr.md) | 🟠 Avertissement | opt-in | Un générateur atteint une position `object`, `dynamic` ou `params object[]` : c'est la recette qui est stockée, comparée ou assérée, pas la valeur. |
| [JD012 GeneratorPooledAsValue](JD012.fr.md) | 🟠 Avertissement | on | `Any.OneOf` reçoit des générateurs et infère un ensemble de recettes ; y tirer produit une recette plutôt qu'une valeur. |
| [JD013 HeldCollectionPassedToOneOf](JD013.fr.md) | 🟠 Avertissement | on | Une collection tenue passée à `Any.OneOf` lie `T` au type de la collection, formant un ensemble d'un seul élément ; `Any.ElementOf` tire parmi ses éléments. |

## JustDummies — Contraintes

Ces règles anticipent, à la compilation, le sous-ensemble des vérifications de contraintes de la bibliothèque qui est décidable depuis des constantes. Les vérifications d'exécution demeurent : elles couvrent tous les arguments que celles-ci ne peuvent pas voir.

| Règle | Sévérité | Défaut | Description |
|-------|----------|--------|-------------|
| [JD014 RejectedConstantArgument](JD014.fr.md) | 🟠 Avertissement | on | Un argument de contrainte est une constante que la garde du générateur refuse : l'appel lève à chaque exécution. |
| [JD015 StringConstraintsAdmitNoValue](JD015.fr.md) | 🟠 Avertissement | on | Les contraintes constantes d'une chaîne `AnyString` n'admettent aucune valeur — un fragment hors de la famille de caractères ou de la casse déclarée, ou des fragments qui ne peuvent pas tenir dans la longueur déclarée. |
| [JD016 CollectionConstraintsAdmitNoValue](JD016.fr.md) | 🟠 Avertissement | on | Les contraintes de cardinal d'une chaîne de collection ne peuvent pas toutes tenir, ou elle réclame plus d'éléments distincts que son générateur d'éléments ne peut en produire. |
| [JD017 EnumUniverseViolation](JD017.fr.md) | 🟠 Avertissement | on | Une contrainte d'enum sort des membres déclarés — une combinaison de drapeaux sans `AllowingCombinations()`, ou une exclusion qui vide l'univers. |

## Configuration

La sévérité de chaque règle se règle dans `.editorconfig`, par exemple :

```ini
# activer une règle opt-in
dotnet_diagnostic.FCE004.severity = warning

# ou faire taire une règle
dotnet_diagnostic.FCE014.severity = none
```

> `FCE001` et `FCE011` sont des vérifications sur toute la compilation : elles apparaissent au build / à l'analyse de la solution entière, pas à la frappe dans un seul fichier.
