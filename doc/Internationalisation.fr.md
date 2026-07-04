# Internationalisation

FirstClassErrors peut produire le catalogue d’erreurs en plusieurs langues. L’internationalisation est **optionnelle et granulaire** : sans aucune configuration, la documentation est en anglais, et vous ne localisez que ce que vous choisissez.

Deux choses peuvent être localisées, à deux étapes différentes du pipeline :

| Quoi | Localisé quand | Comment |
| --- | --- | --- |
| **Contenu des erreurs** — titres, explications, règles, diagnostics, les messages publics (court et détaillé), descriptions de source et de contexte | à l’**extraction** | vos fabriques lisent des ressources localisées sous la culture UI courante |
| **Gabarits des renderers** — titres, libellés, en-têtes de tableau | au **rendu** | le renderer lit son propre texte fixe pour `RenderRequest.Culture` |

Tout le reste demeure **indépendant de la culture**, pour que les liens ne cassent jamais d’une langue à l’autre — et pour que les diagnostics restent dans une langue unique et cohérente pour les logs et le support : codes d’erreur, noms de source (`nameof(...)`), valeurs d’`ErrorOrigin`, le **message de diagnostic interne** de chaque erreur, ainsi que les noms de fichiers et les ancres générés.

### Les messages publics sont localisés, le message de diagnostic ne l’est pas

Une erreur porte trois messages, qui se localisent différemment :

* **`ShortMessage`** et **`DetailedMessage`** sont du contenu public : ils sont localisés à l’extraction comme n’importe quelle autre prose — lisez-les depuis des ressources sous la culture UI courante.
* **`DiagnosticMessage`** est délibérément **conservé dans la langue de l’auteur (indépendant de la culture)**. Il est destiné aux logs, au support et aux développeurs, et un texte de diagnostic est le plus utile lorsqu’il se lit toujours dans une langue unique et cohérente, quelle que soit la locale de l’appelant — c’est une bonne pratique assumée.

Ainsi, dans la documentation générée, les messages publics sont rendus localisés tandis que le message de diagnostic est rendu dans la langue invariante (celle de l’auteur).

L’exemple `.Usage` fournit cinq langues — anglais, français, espagnol, allemand et suédois (`en`, `fr`, `es`, `de`, `sv`).

## Choisir la langue

Passez `--language` (alias `-l`) à `fce generate`, ou définissez une valeur `language` par défaut dans `fce.json` ; une valeur en ligne de commande écrase la configuration, exactement comme les autres options. Le défaut est l’anglais.

```bash
fce generate --solution ./MyApp.sln --format markdown --language sv --output ./docs/errors
```

```json
{
  "solution": "./MyApp.sln",
  "language": "sv"
}
```

## Niveau 1 — localiser le contenu des erreurs

Le contenu des erreurs est localisé à l’**extraction**. Le générateur lance le worker de chaque assembly avec `CultureInfo.CurrentUICulture` réglé sur la langue demandée, de sorte que toute fabrique qui lit des ressources localisées produit cette langue. Dans l’exemple, la prose est lue depuis un petit wrapper de `ResourceManager` (`UsageErrorMessages`) adossé à un `.resx` par langue :

```csharp
private static ErrorDocumentation BelowAbsoluteZeroDocumentation() {
    return DescribeError.WithTitle(UsageErrorMessages.Get("Temperature_BelowAbsoluteZero_Title"))
                        .WithDescription(UsageErrorMessages.Get("Temperature_BelowAbsoluteZero_Description"))
                        // …règles, diagnostics, exemples lus de la même façon
                        ;
}
```

Vous êtes libre d’écrire des chaînes littérales à la place — l’erreur est alors simplement toujours dans cette langue (voir [Opt-in et localisation partielle](#opt-in-et-localisation-partielle)).

### La description du groupe de source

`[ProvidesErrorsFor]` accepte un `DescriptionResourceType`. Lorsqu’il est renseigné, l’extracteur traite `Description` comme une **clé de ressource** résolue via ce type — le même patron que `[Display(ResourceType = …)]` de DataAnnotations. En son absence, `Description` est un texte littéral.

```csharp
[ProvidesErrorsFor(nameof(Amount),
                   Description = "Amount_Source",                        // une clé de ressource…
                   DescriptionResourceType = typeof(UsageErrorMessages))] // …résolue via ces ressources
```

### Les descriptions de clés de contexte

Une `ErrorContextKey` est enregistrée une seule fois par son nom, mais sa description peut être résolue paresseusement pour suivre la culture courante. Utilisez la surcharge `Func<string?>` de `Create` :

```csharp
public static readonly ErrorContextKey<DateOnly> TransactionDate =
    ErrorContextKey.Create<DateOnly>("TRANSACTION_DATE", () => UsageErrorMessages.Get("Bank_TransactionDate_Context"));
```

L’identité de la clé (son nom) reste figée ; seul le texte de la description est différé et lu sous la culture en vigueur au moment de l’extraction.

## Niveau 2 — localiser les gabarits des renderers

Le texte fixe propre à un renderer (titres, libellés, en-têtes de tableau) est localisé au **rendu**, depuis `RenderRequest.Culture`. Le renderer Markdown intégré lit ses chaînes depuis un jeu de `.resx` pour cette culture ; le renderer JSON n’a aucun texte fixe à traduire — ses noms de champs sont un schéma machine, pas de la prose.

Un renderer personnalisé localise son gabarit de la même façon — voir [Écrire son propre renderer](WritingACustomRenderer.fr.md). Le *contenu* des erreurs qu’il reçoit est déjà localisé en amont : un renderer ne localise donc jamais que son propre texte.

## Opt-in et localisation partielle

L’internationalisation n’est jamais imposée :

* Une erreur dont le `[ProvidesErrorsFor]` n’a pas de `DescriptionResourceType` conserve sa `Description` littérale.
* Une fabrique qui écrit des chaînes en dur (plutôt que de lire des ressources) reste toujours dans cette langue.
* Sans `--language`, tout est rendu en anglais (la culture invariante), à l’octet près comme avant l’existence de l’i18n.

Un projet ne s’internationalise donc que là où il le souhaite. L’exemple `.Usage` montre les deux extrêmes : `Temperature` est un exemple simple, non localisé, tandis qu’`Amount` et `BankTransactionFileValidator` sont entièrement localisés dans les cinq langues.

## L’utiliser sans la CLI

Lorsque vous pilotez le pipeline vous-même, réglez la **même** culture sur les deux étapes pour que le contenu et les gabarits concordent :

```csharp
CultureInfo culture = CultureInfo.GetCultureInfo("sv");

IEnumerable<ErrorDocumentation> catalog =
    SolutionErrorDocumentationGenerator.GetErrorDocumentationFrom(
        "MyApp.sln", new SolutionGenerationOptions { Culture = culture });

RenderRequest request = new(RenderLayouts.Single, culture);
IReadOnlyList<RenderedDocument> documents = new MarkdownErrorDocumentationRenderer().Render(catalog, request);
```

## Comment la culture traverse le pipeline

| Étape | Source de la culture | Ce qu’elle localise |
| --- | --- | --- |
| Worker / extraction | `CultureInfo.CurrentUICulture` (réglée depuis `--language`) | le contenu des erreurs (titres, explications, règles, diagnostics, les messages publics court et détaillé, descriptions de source et de contexte) |
| Renderer | `RenderRequest.Culture` | le texte fixe propre au renderer (titres, libellés, en-têtes de tableau) |

Le contenu est localisé à l’extraction ; le texte fixe au rendu. Les noms de fichiers, les ancres et le message de diagnostic interne de chaque erreur restent indépendants de la culture.

---

Section précédente: [Écrire son propre renderer](WritingACustomRenderer.fr.md) | Section suivante: [FAQ](FAQ.fr.md)

---
