# Tool JustDummies (`dum`) — spécification v1.0

🌍 🇫🇷 Français (ce fichier) · 🇬🇧 [English](justdummies-tool.md)

**Statut :** spécification, prête à implémenter. Rien n'est encore construit.
**Remplace :** la pré-spécification de travail 0.1 (jamais commitée)

---

## 0. Comment lire ce document

Cette spécification est **autonome à dessein**. JustDummies a vocation à rejoindre son propre
dépôt avant que le tool soit construit, donc rien ici ne peut dépendre d'une lecture à l'intérieur
de `Reefact/first-class-errors`.

* **§1–§9, c'est le produit.** Ce que le tool fait, ce qu'il émet, et pourquoi. Lire le §2
  d'abord : onze décisions portent tout le reste. Le §5 est la partie difficile et la seule qui
  comporte un vrai risque de conception.
* **§10–§12, c'est la construction.** Deux projets, le contrat entre eux, et le plan de tests.
* **§13, c'est le contrat de portabilité.** Tout ce dont le tool a besoin *de son dépôt hôte*,
  énoncé en exigences plutôt qu'en chemins. Si JustDummies a déménagé, commencer ici.
* **§14, c'est la référence.** Chaque fait sur la bibliothèque JustDummies dont dépend cette
  spécification, inliné, avec la commande pour le redériver. Rien dans les §1–§12 n'exige de lire
  la source de la bibliothèque pour être vérifié.
* **§17, ce sont les preuves.** Le squelette émis du §4.1 a été compilé et exécuté contre la vraie
  bibliothèque, et les deux affirmations contestées ont été mesurées. Le §17.2 dit comment tout
  rejouer.

Tout dans ce document est **décidé**, sauf ce qui figure au §16 (reporté) ou ce qui est
explicitement marqué ouvert. Aucune question ouverte ne bloque l'implémentation.

---

## 1. Ce qu'est `dum`

`dum` est un **scaffolder**, pas un générateur de code.

À partir d'un type du code du développeur, il écrit **un fichier C#, une fois**, contenant un
generator nommé et composable pour ce type. Dès que le fichier est écrit il appartient au
développeur : il le lit, le modifie, le commite, et ne relance jamais le tool dessus.

```console
$ cd Shop.Tests
$ dum generate Order
✓ AnyOrder.cs
```

```csharp
Order order = new AnyOrder()
    .WithStatus(OrderStatus.Pending)
    .Generate();
```

La distinction avec un *générateur* est toute la position produit, et elle règle d'un coup
l'essentiel de la conception :

* il n'y a pas de dérive, parce qu'il n'y a rien à maintenir synchronisé — le fichier est celui du
  développeur, pas celui du tool ;
* il n'y a donc **ni verbe `check`, ni source generator, ni scénario de régénération** ;
* le tool a le droit de laisser le fichier **inachevé**, parce que l'achever est la moitié du
  marché qui revient au développeur.

La proposition de valeur reste distincte de celle de la bibliothèque : la **bibliothèque** rend les
valeurs valides ; le **tool** rend le test concis.

### 1.1 Les règles de conception auxquelles ce document répond

1. **Extrêmement simple à utiliser.** L'invocation nominale tient en un verbe et un nom de type,
   depuis le répertoire où le fichier atterrira, sans fichier de configuration et sans option.
2. **Bon marché aux deux bouts.** Rien à configurer avant le premier usage ; rien à configurer à
   chaque usage.
3. **Générer tout ce qui peut l'être, et rien de plus.** Là où le tool ne peut pas savoir, il le
   dit dans le fichier et dans la console, et rend la main sur le squelette.
4. **Le nommage est figé en v1.0.** `Order` devient `AnyOrder`, point. Le renommage
   (`OrderFactory`, un préfixe personnalisé) est pour la v1.1+ et le §16 en réserve la forme pour
   que la v1.0 ne la bloque pas.

---

## 2. Décisions

Ce sont les décisions porteuses. Le §15 liste les ADR à rédiger pour chacune.

| # | Décision | Pourquoi, en une ligne |
|---|---|---|
| **D1** | Scaffolder une fois ; le fichier appartient au développeur. | Supprime d'un coup la dérive, le `check` et la question du source generator. |
| **D2** | Le type émis implémente `IAny<T>` et est **immuable**. | Composabilité, et réarmement des analyzers `JustDummies.Usage` sur le type émis. |
| **D3** | Le fichier émis n'est **pas** marqué comme code généré. | Les 27 analyzers exemptent le code généré ; le marquer rendrait le fichier aveugle. |
| **D4** | Ne jamais émettre un membre non résolu dans la compilation cible. | Une règle couvre le clivage de TFM, la baseline d'API publique, l'écart de version et l'arithmétique non signée. |
| **D5** | Lire les clauses de garde du constructeur pour amorcer chaque generator. | Sans cela le code émis produit des valeurs que le constructeur rejette. |
| **D6** | Un paramètre non résolu est émis comme **erreur de compilation**. | Le développeur est déjà dans le fichier ; un soulignement rouge est le signal le moins cher. |
| **D7** | Le generator émis tire du contexte **ambiant** uniquement. | Supporter `AnyContext` coûte de la surface pour un cas que `.WithX(IAny<T>)` couvre déjà. |
| **D8** | Le type émis vit dans le **namespace du type cible**. | Le test a déjà ce `using` ; `new AnyOrder()` fonctionne tel quel. |
| **D9** | Le tool ne prend **aucune dépendance sur le package JustDummies**. | Résolution par nom de métadonnée, comme les analyzers — l'écart de version devient structurellement impossible. |
| **D10** | Ne jamais émettre `.OrNull()`. | Un dummy aléatoirement `null` est précisément l'instabilité que la bibliothèque existe pour supprimer. |
| **D11** | Le **moteur de scaffolding est une bibliothèque séparée** au plancher Roslyn ; la CLI est une coquille. | Le second consommateur plausible du moteur est un refactoring IDE, qui n'est pas une CLI et ne peut pas charger un assembly `net8.0`. |

### 2.1 D3 en détail — pourquoi le fichier n'est pas marqué comme généré

Chacun des 27 analyzers du package d'analyzers JustDummies appelle
`ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None)` (§14.6). Un fichier portant un
en-tête `<auto-generated/>`, ou nommé `*.g.cs` / `*.generated.cs`, est donc **entièrement exempté
de tout diagnostic JD**.

Cela a été mesuré, pas déduit. Un fichier contenant exactement deux violations — un avertissement
`JD006` (une contrainte dont le résultat est jeté) et une **erreur** `JD005` (un generator rendu
comme texte) — a été compilé deux fois contre les analyzers construits, en ne changeant que sa
première ligne :

| Première ligne du fichier | Diagnostics remontés | Build |
|---|---|---|
| *(aucune)* | avertissement `JD006`, erreur `JD005` | échoue |
| `// <auto-generated/>` | **aucun** | réussit |

Une erreur de compilation est devenue du silence. Marquer le fichier scaffoldé créerait le seul
fichier du projet de test que le filet de sécurité JustDummies ne couvre pas — et c'est le fichier
le plus susceptible de contenir une erreur JustDummies, puisque le développeur est sur le point de
l'éditer.

Donc : un simple commentaire d'en-tête, un nom de fichier `.cs` ordinaire, pas de
`GeneratedCodeAttribute`. Le fichier est analysé comme n'importe quel autre. Le bénéfice est
direct : une chaîne dérivée d'une garde (§5.3) que le moteur aurait mal formée remonte en
`JD015`/`JD023` dans l'IDE du développeur au lieu d'une `ConflictingAnyConstraintException` à
l'exécution.

### 2.2 D2 en détail — pourquoi `IAny<T>` n'est pas optionnel

Les analyzers reconnaissent un generator comme *l'interface `IAny<T>` elle-même, ou tout type qui
l'implémente* (§14.6). Faire de `AnyOrder` un `IAny<Order>` produit donc deux effets :

* il devient consommable par `Any.Combine`, `.As(...)`, `Any.ListOf(...)`, `Any.OneOf(...)` et
  tous les scopes de reproductibilité, sans code supplémentaire — les agrégats imbriqués se
  composent gratuitement ;
* toute la catégorie `JustDummies.Usage` (`JD005`, `JD006`, `JD012`, `JD013`) se met à reconnaître
  `AnyOrder` comme une recette, si bien que `Assert.Equal("x", new AnyOrder())` ou un
  `new AnyOrder().Generate()` dont le résultat est jeté est signalé exactement comme pour
  `Any.String()`.

L'immuabilité n'est pas une préférence de style : `IAny<T>` est documentée comme *« an immutable
recipe […] each fluent constraint returns a new generator »*, et tous les generators de la
bibliothèque l'honorent (§14.5). Un `.WithX()` retournant `this` ferait de `AnyOrder` le seul
generator mutable de l'écosystème et casserait ceci :

```csharp
AnyOrder baseOrder = new AnyOrder().WithCustomer(customer);
AnyOrder pending   = baseOrder.WithStatus(OrderStatus.Pending);
AnyOrder shipped   = baseOrder.WithStatus(OrderStatus.Shipped);  // ne doit pas perturber `pending`
```

### 2.3 D4 en détail — résoudre, puis émettre

Le moteur détient une `Compilation` du projet du développeur. Avant d'émettre le moindre membre
JustDummies, il **cherche ce membre dans cette compilation**
(`GetTypeByMetadataName("JustDummies.Any")`, puis une recherche de membre). Si la recherche échoue,
le membre n'est pas émis et le paramètre bascule sur le chemin non résolu (§5.5).

Une règle, cinq problèmes réglés :

* **Le clivage de TFM.** `DateOnly`, `TimeOnly`, `Int128`, `UInt128` et `Half` n'existent que sur
  l'asset `net8.0` (§14.1). Un projet de test qui résout l'asset `netstandard2.0` — `net472`,
  `netstandard2.0`, tout consommateur sous `net8.0` — n'a pas de `Any.DateOnly()`. Le moteur n'a
  pas besoin de le savoir : la recherche échoue et le paramètre devient un TODO.
* **L'arithmétique non signée.** `Positive()` et `Negative()` n'existent pas sur `AnyByte`,
  `AnyUInt16`, `AnyUInt32` ni `AnyUInt64` — un type non signé ne peut pas les exprimer (§14.3).
  Une garde `p <= 0` sur un paramètre `uint` ne résout donc rien et est silencieusement ignorée,
  plutôt que d'émettre un appel qui ne compile pas.
* **La baseline d'API publique.** Tout ce qui est résoluble dans la compilation fait, par
  construction, partie de la surface publiée.
* **L'écart de version.** Un vieux `dum` contre une bibliothèque récente émet moins ; un `dum`
  récent contre une vieille bibliothèque émet moins. Aucun des deux n'émet quelque chose qui ne
  compile pas.
* **Les generators du développeur.** La même recherche trouve `AnyCustomer` dans son code (§5.4).

### 2.4 D11 en détail — pourquoi le moteur est sa propre bibliothèque

L'argument naïf pour séparer — *« la CLI aura peut-être d'autres verbes »* — est faible. Des verbes
en plus sont des fichiers en plus dans le projet CLI, tous assis sur le même moteur ; cela ne
justifie aucune frontière. Après D1 la liste de verbes plausibles est de toute façon quasi vide,
puisque `check`, `init` et `list` sont morts avec la décision scaffolder.

Le vrai argument va dans l'autre sens : **le moteur a un consommateur plausible qui n'est pas une
CLI.** Un `CodeRefactoringProvider` Roslyn — clic droit sur un type, *« Scaffold a generator »* —
est la seconde surface naturelle pour une bibliothèque qui publie déjà des analyzers, et émettre un
document est exactement ce qu'un code refactoring fait bien. Ce consommateur veut
`(Compilation, ITypeSymbol) → source` et rien d'autre : ni Spectre, ni MSBuild, ni console, ni
système de fichiers.

Garder cette porte ouverte a un coût qui se paie **maintenant ou jamais** : un assembly hébergé par
un analyzer doit cibler `netstandard2.0` et compiler contre le plancher Roslyn (§13.2), tandis que
la CLI cible `net8.0` et a besoin de `MSBuildWorkspace`, qui n'est ni l'un ni l'autre. Un moteur né
`net8.0` ferme la voie IDE, et la rouvrir plus tard signifie re-vérifier chaque API contre le
plancher. Le coût est asymétrique — bon marché maintenant, cher plus tard — et le moteur n'est que
du Roslyn plus de la construction de chaînes, donc `netstandard2.0` ne lui coûte presque rien.
C'est une exception au YAGNI, délibérée et énoncée comme telle.

Deux bénéfices immédiats suivent, indépendants de tout consommateur futur :

* **Les tests.** Le plan de tests (§12) est écrasé par le moteur — comportement du résolveur sur
  une `CSharpCompilation` en mémoire, émission à fichiers de référence, compilation de la sortie
  avec analyzers branchés. Aucun ne veut de console ni de parseur d'arguments.
* **Les tests de mutation.** Un projet unique mettrait la plomberie de commandes Spectre sous le
  même budget de mutation que le résolveur. Deux projets donnent une cible à forte valeur et une à
  faible valeur, configurables à part.

---

## 3. Surface de commande

Le tool est distribué comme .NET tool dont la commande est **`dum`**.

```console
dotnet tool install --global JustDummies.Cli
dum generate <Type> [<Type>...] [options]
```

`generate` est le seul verbe de la v1.0.

| Option | Défaut | Signification |
|---|---|---|
| `--project <path>` | l'unique `*.csproj` du répertoire courant | Projet dont la compilation est analysée. |
| `--output <dir>` | le répertoire courant | Où le fichier est écrit. |
| `--namespace <ns>` | le namespace du type cible (D8) | Namespace du type émis. |
| `--force` | inactif | Écrase un fichier existant. |
| `--dry-run` | inactif | Affiche le fichier sur stdout ; n'écrit rien. |

C'est toute la surface. Pas de fichier de configuration, pas de `init`, pas de `list`, pas de
`--all`, et — par D1 — pas de `check`. Le §16 liste ce qui est délibérément reporté.

### 3.1 Où le tool est lancé

Depuis le **projet de test**, parce que c'est là que le fichier va. Le projet de test référence le
projet de production, donc `Order` est atteignable depuis sa compilation, et le défaut de
`--output` place `AnyOrder.cs` à côté des tests qui l'utilisent.

Résolution de `--project` : si exactement un `*.csproj` se trouve dans le répertoire courant, il
est retenu ; s'il n'y en a aucun ou plusieurs, échec avec un message nommant les candidats et
pointant `--project`.

### 3.2 Résolution du type cible

`Order` est cherché, dans l'ordre :

1. par nom de métadonnée complet, si l'argument contient un `.` (`Shop.Domain.Order`) ;
2. par nom simple parmi les types source de la compilation et les assemblies référencées.

Zéro correspondance → erreur, avec les noms les plus proches par distance d'édition. Plus d'une →
erreur, avec les noms complets, en demandant lequel. Les deux sortent en `1`.

---

## 4. Le fichier émis

### 4.1 Exemple complet

Cet exemple n'est pas une esquisse : il a été compilé et exécuté contre la vraie bibliothèque
(§17).

Source analysée :

```csharp
namespace Shop.Domain;

public sealed class Order {

    public Order(OrderReference reference, Customer customer, int quantity,
                 OrderStatus status, IReadOnlyList<string> tags, DateTime placedAt) {
        if (reference is null) { throw new ArgumentNullException(nameof(reference)); }
        if (quantity <= 0) { throw new ArgumentOutOfRangeException(nameof(quantity)); }
        ...
    }

}

public sealed class OrderReference {

    public static OrderReference Create(string value) {
        if (string.IsNullOrWhiteSpace(value)) { throw new ArgumentException(...); }
        ...
    }

}
```

`dum generate Order`, avec `AnyCustomer` déjà scaffoldé dans le projet, émet :

```csharp
// Scaffolded by dum (JustDummies). This file is yours: read it, edit it, commit it.
// `dum generate Order --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;
using System.Collections.Generic;

using JustDummies;

namespace Shop.Domain;

/// <summary>A generator of arbitrary <see cref="Order" /> values.</summary>
public sealed partial class AnyOrder : IAny<Order> {

    private readonly IAny<OrderReference>        _reference;
    private readonly IAny<Customer>              _customer;
    private readonly IAny<int>                   _quantity;
    private readonly IAny<OrderStatus>           _status;
    private readonly IAny<IReadOnlyList<string>> _tags;
    private readonly IAny<DateTime>              _placedAt;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyOrder()
        : this(reference: Any.String().NonEmpty().As(OrderReference.Create),
               customer:  new AnyCustomer(),
               quantity:  Any.Int32().Positive(),
               status:    Any.Enum<OrderStatus>(),
               tags:      Any.ListOf(Any.String().NonEmpty()),
               placedAt:  Any.DateTime()) { }

    private AnyOrder(IAny<OrderReference>        reference,
                     IAny<Customer>              customer,
                     IAny<int>                   quantity,
                     IAny<OrderStatus>           status,
                     IAny<IReadOnlyList<string>> tags,
                     IAny<DateTime>              placedAt) {
        _reference = reference;
        _customer  = customer;
        _quantity  = quantity;
        _status    = status;
        _tags      = tags;
        _placedAt  = placedAt;
    }

    /// <summary>Pins <c>reference</c> to a fixed value.</summary>
    public AnyOrder WithReference(OrderReference value) {
        return WithReference(new FixedValue<OrderReference>(value));
    }

    /// <summary>Draws <c>reference</c> from <paramref name="generator" />.</summary>
    public AnyOrder WithReference(IAny<OrderReference> generator) {
        return new AnyOrder(generator, _customer, _quantity, _status, _tags, _placedAt);
    }

    // ... une telle paire par paramètre ...

    /// <summary>Produces one arbitrary <see cref="Order" />.</summary>
    public Order Generate() {
        return new Order(_reference.Generate(),
                         _customer.Generate(),
                         _quantity.Generate(),
                         _status.Generate(),
                         _tags.Generate(),
                         _placedAt.Generate());
    }

    private sealed class FixedValue<TValue> : IAny<TValue> {

        private readonly TValue _value;

        public FixedValue(TValue value) {
            _value = value;
        }

        public TValue Generate() {
            return _value;
        }

    }

}
```

### 4.2 Règles de forme

* `public sealed partial class Any{Type} : IAny<{Type}>`. `partial` pour que les membres propres du
  développeur vivent dans un fichier voisin et survivent à un `--force`.
* Un `private readonly IAny<TParam> _param;` par paramètre du constructeur, dans l'ordre de
  déclaration.
* Un **constructeur public sans paramètre** portant la recette inférée, écrit avec des arguments
  nommés pour que le lecteur associe chaque expression à son paramètre sans compter.
* Un **constructeur privé complet** réalisant la copie.
* Par paramètre, **deux** surcharges `With{Param}` retournant une nouvelle instance :
  `With{Param}(TParam value)` et `With{Param}(IAny<TParam> generator)`.
  La surcharge par valeur est l'ergonomique ; la surcharge par generator est ce qui maintient la
  composition possible, et c'est pourquoi passer `Any.String().StartingWith("ORD-")` ne devient pas
  une erreur `JD011`/`JD012`.
* `public {Type} Generate()` appelant le constructeur avec le `Generate()` de chaque champ.
* Le helper privé imbriqué `FixedValue<TValue>`. Justification : il accepte `null` (ce que
  `Any.OneOf(value)` refuse) et ne consomme aucun tirage de la source ambiante, donc épingler un
  paramètre ne décale pas les valeurs tirées pour les autres (§14.5). Il est imbriqué et privé,
  donc un nombre quelconque de fichiers scaffoldés coexistent. *(Si `Any.Fixed<T>(value)` est un
  jour ajouté à la bibliothèque, le helper pourra disparaître — voir §15.)*
* Casse de `With{Param}` : le nom du paramètre, première lettre en majuscule, culture invariante.
  Un paramètre nommé `_id` ou `@class` est normalisé en retirant le `_`/`@` de tête.

### 4.3 Règles d'en-tête

Exactement trois lignes de commentaire, comme ci-dessus. **Aucun horodatage et aucune version du
tool** : les deux feraient dépendre le contenu d'autre chose que du type analysé, si bien que tout
scaffold suivant une montée de version produirait un diff parasite. Le déterminisme est une
exigence dure (§8.1).

### 4.4 Niveau de langage

Le code émis n'utilise aucune construction plus récente que **C# 7.3** : pas de `var` (cela se lit
mieux dans un squelette), pas de `new` typé par la cible, pas de records, pas d'expressions
`switch`, pas de namespace à portée de fichier sauf si le fichier du type cible en utilise déjà un.
Le fichier atterrit dans le projet du développeur et doit compiler au `LangVersion` de ce projet.

La seule exception est la forme du namespace, copiée sur le style de déclaration du type cible pour
que le fichier émis ressemble à ses voisins.

---

## 5. Résolution — comment un paramètre devient un generator

Pour chaque paramètre, le moteur produit une expression de type `IAny<TParam>`, ou échoue et marque
le paramètre non résolu.

### 5.1 Choix du constructeur

1. Constructeurs d'instance publics, le plus de paramètres d'abord ; égalité départagée par l'ordre
   source. La signature retenue est toujours affichée (§6).
2. Si le type n'a **aucun** constructeur accessible mais expose une fabrique statique reconnue
   (§5.4) retournant lui-même, cette fabrique est utilisée et `Generate()` l'appelle.
3. Un constructeur sans paramètre donne un `AnyOrder` valide et trivial, sans méthode `With`.
4. Les records positionnels fonctionnent sans traitement particulier — leur constructeur primaire
   est un constructeur public ordinaire. Les membres `init` et `required` sont **hors périmètre**
   (§16).

### 5.2 La table de base

Chaque entrée est soumise à D4 : le membre n'est émis que s'il se résout dans la compilation.

| Type du paramètre | Émission |
|---|---|
| `string` | `Any.String().NonEmpty()` |
| `bool` | `Any.Boolean()` |
| `sbyte` `byte` `short` `ushort` `int` `uint` `long` `ulong` | `Any.SByte()` … `Any.UInt64()` |
| `float` `double` `decimal` | `Any.Single()` / `Any.Double()` / `Any.Decimal()` |
| `char` | `Any.Char()` |
| `Guid` | `Any.Guid().NonEmpty()` |
| `DateTime` `DateTimeOffset` `TimeSpan` | `Any.DateTime()` / `Any.DateTimeOffset()` / `Any.TimeSpan()` |
| `DateOnly` `TimeOnly` `Int128` `UInt128` `Half` | la fabrique correspondante — **asset `net8.0` uniquement**, D4 tranche |
| tout `enum E` | `Any.Enum<E>()` |
| `Uri` | `Any.Uri().Web()` |
| `T[]` | `Any.ArrayOf(<T>)` |
| `List<T>` `IReadOnlyList<T>` `IList<T>` `ICollection<T>` `IReadOnlyCollection<T>` | `Any.ListOf(<T>)` |
| `IEnumerable<T>` | `Any.SequenceOf(<T>)` |
| `HashSet<T>` `ISet<T>` | `Any.SetOf(<T>)` |
| `Dictionary<K,V>` `IDictionary<K,V>` `IReadOnlyDictionary<K,V>` | `Any.DictionaryOf(<K>, <V>)` |
| `T?` où `T` est un type référence | le generator de `T` inchangé — **jamais** `.OrNull()` (D10) |
| `T?` où `T` est un type valeur | `<generator de T>.As(value => (T?)value)` — **jamais** `.OrNull()` (D10) |
| un type ayant un `AnyT` scaffoldé dans la compilation | `new AnyT()` (§5.4) |
| un type ayant une fabrique statique reconnue à un paramètre | `<generator du paramètre>.As(T.Create)` (§5.4) |
| tout le reste | non résolu (§5.5) |

Trois remarques sur la table.

**`Any.String().NonEmpty()`, pas `Any.String()`.** Sans contrainte, `Any.String()` produit *0 à 16*
lettres et chiffres ASCII (§14.5) — il peut retourner la chaîne vide. Un paramètre de constructeur
de type `string` dans un type métier est massivement requis non vide, et un défaut qui échoue
environ une fois sur seize (mesuré : §17) est exactement l'instabilité que la bibliothèque existe
pour supprimer. Même raisonnement pour `Any.Guid().NonEmpty()`.

**Les collections reposent sur la covariance — les types valeur, non.** `IAny<out T>` est
covariante, donc `Any.ListOf(...)`, de type `IAny<List<T>>`, est directement affectable à un champ
de type `IAny<IReadOnlyList<T>>` ; aucun adaptateur n'est nécessaire pour les lignes d'interface, et
il en va de même pour `HashSet<T>`/`ISet<T>` et `Dictionary<K,V>`/`IReadOnlyDictionary<K,V>`.

La variance en C# ne s'applique qu'aux conversions de **référence**, d'où la différence entre les
deux lignes nullables. `IAny<string>` est un `IAny<string?>` et ne demande rien ; `IAny<int>`
n'est **pas** un `IAny<int?>`, donc un paramètre `int?` exige le saut explicite
`.As(value => (int?)value)`. S'y tromper est la façon la plus probable de produire une table qui ne
compile pas — les lignes réservées à `net8.0` sont elles aussi des types valeur.

**Les generators d'éléments récursent.** `IReadOnlyList<OrderLine>` résout son élément par cette
même table, et devient donc `Any.ListOf(new AnyOrderLine())` quand `AnyOrderLine` existe. La
récursion est limitée à une profondeur de 3 et protégée contre les cycles ; dépasser l'une ou
l'autre rend le paramètre non résolu.

### 5.3 Clauses de garde

C'est la fonctionnalité qui justifie de construire le tool plutôt que de faire un template.

Quand le corps du constructeur (ou de la fabrique) est **disponible en source** — ce qui est le cas
pour tout type de la solution du développeur, et ne l'est pas pour un type venant d'un package
NuGet — le moteur lit ses clauses de garde de tête et resserre le generator en conséquence.

Une instruction n'est une garde que si **toutes** les conditions suivantes tiennent. La règle est
délibérément conservatrice, à l'image des analyzers de la bibliothèque, qui préfèrent sous-signaler
plutôt que se tromper :

* c'est un `if` dont le corps lève inconditionnellement, sans `else` ;
* elle apparaît avant la première affectation à un champ ou une propriété ;
* sa condition mentionne **exactement un** paramètre et ne contient ni `&&` ni `||` ;
* tout autre opérande est une constante de compilation.

L'ensemble reconnu est clos :

| Condition qui lève | Contrainte ajoutée |
|---|---|
| `p is null`, `p == null` | aucune — le generator ne retourne jamais `null` de toute façon |
| `string.IsNullOrEmpty(p)`, `string.IsNullOrWhiteSpace(p)`, `p.Length == 0`, `p.Length < 1` | `.NonEmpty()` |
| `p.Length > N` | `.WithMaxLength(N)` |
| `p.Length < N` | `.WithMinLength(N)` |
| `p <= 0`, `p < 1` | `.Positive()` |
| `p < 0` | `.GreaterThanOrEqualTo(0)` |
| `p >= 0` | `.Negative()` |
| `p == 0` | `.NonZero()` |
| `p > N` | `.LessThanOrEqualTo(N)` |
| `p < N` | `.GreaterThanOrEqualTo(N)` |
| `p == Guid.Empty` | `.NonEmpty()` |
| `!Regex.IsMatch(p, "littéral")` | le generator de base est remplacé par `Any.StringMatching("littéral")` |
| `!Enum.IsDefined(typeof(E), p)` | aucune — `Any.Enum<E>()` ne tire déjà que des membres déclarés |

`.NonEmpty()` couvre `IsNullOrWhiteSpace` aussi bien que `IsNullOrEmpty`, parce qu'un
`Any.String()` non contraint ne tire que des lettres et chiffres ASCII : un tirage non vide ne peut
jamais être blanc (§14.5).

Les contraintes sont regroupées par **axe** — longueur, intervalle, jeu de caractères, motif. Si
deux gardes reconnues atterrissent sur le même axe, **les deux sont abandonnées** et le paramètre
est signalé `guards not combined` ; le développeur voit le generator neutre et la console lui dit
d'aller voir. C'est le seul endroit où le moteur pourrait émettre une chaîne que la bibliothèque
rejette à l'exécution par `ConflictingAnyConstraintException`, et cette règle l'élimine.

Chaque contrainte ci-dessus reste soumise à D4. `.Positive()` sur un paramètre `uint` ne se résout
pas (§14.3) et est ignorée.

La lecture des gardes est aussi ce qui rend la composition par fabrique correcte plutôt que
nominale : `OrderReference.Create` garde sur `IsNullOrWhiteSpace`, donc le tool émet
`Any.String().NonEmpty().As(OrderReference.Create)` — une chaîne qui fonctionne — au lieu de
`Any.String().As(OrderReference.Create)`, mesurée levant `AnyGenerationException` **594 fois sur
10 000 tirages**, environ une fois sur seize (§17).

Cette seule mesure est l'argument de toute cette section. Un tool qui émet la seconde chaîne ne fait
pas que passer à côté : il fabrique, dans la suite de tests du développeur, exactement l'échec
intermittent que la bibliothèque a été construite pour éliminer.

### 5.4 Composition

**Un generator scaffoldé l'emporte.** Si la compilation contient un type nommé `Any{T}` implémentant
`IAny<T>` avec un constructeur public sans paramètre, le moteur émet `new Any{T}()`. C'est ainsi que
les agrégats se composent en cascade, et cela fonctionne que ce type ait été scaffoldé plus tôt ou
écrit à la main.

**Sinon, une fabrique statique.** Une méthode qualifie si elle est `public static`, retourne le type
du paramètre, prend exactement un paramètre, et se nomme `Create`, `From`, `Of` ou `Parse`. Si
plusieurs qualifient, `Create` gagne ; s'il en reste plusieurs, le paramètre est non résolu et la
console nomme les candidates. L'émission est `<generator du paramètre de la fabrique>.As(T.Create)`,
avec le §5.3 appliqué au corps de la fabrique elle-même.

Convention, pas attribut, pas configuration : un attribut supposerait de toucher au code de
production du développeur pour plaire à un outil de test, et un fichier de configuration casserait
la règle de conception 2.

### 5.5 Paramètres non résolus

L'argument du paramètre dans le constructeur public devient un identifiant qui n'existe pas :

```csharp
    public AnyOrder()
        : this(reference: Any.String().NonEmpty().As(OrderReference.Create),
               // TODO(dum): no generator inferred for 'Customer customer'.
               //   Scaffold one:  dum generate Customer
               //   or write one here, or delete this argument and always pass .WithCustomer(...).
               customer:  TODO_supply_a_generator_for_customer,
               quantity:  Any.Int32().Positive(),
               ...
```

Le fichier ne compile pas tant que le développeur n'a pas agi. C'est le but (D6). Le message du
compilateur lui-même — *« The name 'TODO_supply_a_generator_for_customer' does not exist in the
current context »* — est l'instruction, et il apparaît dans l'IDE, dans la liste d'erreurs et en
intégration continue.

Les deux alternatives ont été écartées : une expression `throw` compile et reporte l'échec au premier
run de test, et omettre le paramètre rend `AnyOrder` silencieusement inutilisable. Le développeur
lance le tool et ouvre le fichier dans la même minute ; un soulignement rouge à la ligne exacte lui
coûte dix secondes, un échec à l'exécution une semaine plus tard lui coûte bien davantage.

---

## 6. Sortie console

Le récapitulatif console n'est pas décoratif : c'est le mécanisme qui maintient le tool honnête sur
ce qu'il a inféré et ce qu'il a deviné.

```console
$ dum generate Order

Analyzing Shop.Domain.Order
  constructor Order(OrderReference, Customer, int, OrderStatus, IReadOnlyList<string>, DateTime)

  reference  OrderReference         Any.String().NonEmpty().As(OrderReference.Create)  factory, guard
  customer   Customer               —                                                  TODO
  quantity   int                    Any.Int32().Positive()                             guard
  status     OrderStatus            Any.Enum<OrderStatus>()
  tags       IReadOnlyList<string>  Any.ListOf(Any.String().NonEmpty())
  placedAt   DateTime               Any.DateTime()

✓ AnyOrder.cs — 5 of 6 parameters inferred, 1 TODO.
  The file will not compile until you resolve it. That is deliberate.
```

La colonne de droite porte la provenance de chaque expression : vide pour la table de base, `guard`
quand le §5.3 l'a resserrée, `factory` quand le §5.4 l'a composée, `AnyX` quand un generator
scaffoldé a été réutilisé, `guards not combined` pour le cas de conflit du §5.3, `no source` quand le
corps du constructeur était indisponible et qu'aucune garde n'a pu être lue.

**La provenance est une donnée, pas une sortie.** Le moteur la retourne dans son modèle de résultat
(§10.3) ; la CLI la rend. C'est ce qui rend le récapitulatif testable sans console.

`--dry-run` affiche le même récapitulatif sur stderr et le fichier sur stdout.

---

## 7. Modes d'échec et codes de sortie

| Situation | Sortie | Comportement |
|---|---|---|
| Fichier écrit, tout inféré | `0` | — |
| Fichier écrit, un ou plusieurs TODO | `0` | L'écriture a réussi ; le build du développeur signale le reste. |
| `--dry-run` | `0` | Rien n'est écrit. |
| Type introuvable / ambigu | `1` | Candidats listés. |
| Fichier de sortie existant, sans `--force` | `1` | Nomme le fichier, suggère `--force`, avertit que les éditions seront perdues. |
| Aucun / plusieurs projets trouvés | `1` | Candidats listés, `--project` suggéré. |
| Le projet ne charge pas ou n'est pas restauré | `1` | Le diagnostic MSBuild, tel quel. |
| Le projet ne référence pas JustDummies | `1` | Rien ne peut être résolu (D4) ; le dit et suggère le package. |
| `Any{Type}` masque un type `JustDummies.Any*` | `0` | **Avertissement**, puis génération. |

Cette dernière ligne mérite sa note. La bibliothèque possède 39 noms de types publics `Any*`
(§14.2) — `AnyList`, `AnySet`, `AnyArray`, `AnySequence`, `AnyPattern`, `AnyUri`, `AnyChar`,
`AnyString`, … Un type métier nommé `Set`, `List`, `Sequence` ou `Pattern` scaffolde vers un nom
qui, dans son propre namespace, **masque silencieusement le type de la bibliothèque** pour tous les
fichiers de ce namespace : C# résout le namespace englobant avant tout `using`. Cela compile ; c'est
simplement faux plus tard. Le tool avertit, nomme les deux types, et génère quand même — sous la
règle de conception 4, le renommage est l'affaire du développeur, et la v1.1 lui en donne le levier.

Plusieurs arguments de type (`dum generate Order Customer Invoice`) sont traités indépendamment ; le
code de sortie est le pire d'entre eux, et un échec n'empêche pas l'écriture des autres.

---

## 8. Garanties

### 8.1 Déterminisme

Le même type analysé contre la même compilation produit un fichier **identique à l'octet près**, sur
n'importe quelle machine, sous n'importe quelle version du tool qui résout les mêmes membres. Rien
qui dépende du temps, du chemin, de la culture ou d'un ordre de hachage n'entre dans la sortie :
aucun horodatage, aucune version de tool, aucun chemin absolu, et toute énumération parcourue par
l'émetteur est ordonnée par déclaration.

Cela compte même sans verbe `check` : c'est ce qui rend un nouveau scaffold relisible comme un diff.

### 8.2 Reproductibilité

Le generator émis tire du contexte **ambiant**, parce que toutes les expressions qu'il émet viennent
de la façade statique `Any`, et que la source ambiante résout la frame `AsyncLocal` courante **au
moment du tirage**, pas à la construction (§14.5). Donc :

```csharp
AnyOrder recipe = new AnyOrder();          // construit hors du scope
Any.Reproducibly(() => {
    Order order = recipe.Generate();       // toujours épinglé par le seed du scope
});
```

est reproductible, tout comme le cas ordinaire où les deux se produisent dans le scope. Cela a été
vérifié (§17).

**`Any.WithSeed(seed)` est hors périmètre par décision (D7).** Un `AnyContext` porte sa propre
source aléatoire fixe et n'est pas affecté par le scope ambiant, donc un generator construit à
partir de `Any.*` ne peut pas y tirer. Le supporter impliquerait un constructeur
`AnyOrder(AnyContext)` et un second chemin de recette. Cela ne vaut pas la surface : la surcharge
`.With{Param}(IAny<TParam>)` permet déjà à un développeur sur `WithSeed` de fournir
`context.String()` par paramètre. La doc XML émise le dit en une phrase.

L'émetteur ne produit jamais d'état statique, donc `JD009` et `JD020` n'ont rien sur quoi se
déclencher.

### 8.3 Aucune réflexion dans le code émis

Le fichier émis ne contient aucune réflexion — ce sont des appels de constructeur et des chaînes
fluides. La revendication *« no reflection »* de la bibliothèque porte sur ce qui s'exécute dans le
test du développeur, et elle tient.

Le **tool lui-même** est un programme de build et n'est soumis à aucune contrainte de ce genre ; il
utilise Roslyn, qui n'est de toute façon pas de la réflexion. Les deux questions sont indépendantes.

---

## 9. Non-objectifs de la v1.0

Nommés explicitement pour ne pas être pris pour des oublis.

* **Données réalistes.** Le tool hérite du périmètre de la bibliothèque : arbitraire-mais-valide,
  jamais plausible. Ni noms, ni emails, ni adresses.
* **Remplissage automatique de graphe d'objets.** La composition est d'un saut, via `Any{T}` ou une
  fabrique à un paramètre, limitée à une profondeur de 3. Au-delà, le développeur l'écrit.
* **Les invariants que le tool ne peut pas voir.** Le §5.3 lit un ensemble clos d'idiomes de garde.
  Un constructeur qui valide via une méthode auxiliaire, une bibliothèque `Guard.Against` ou une
  règle inter-paramètres obtient le generator neutre et une ligne de console. Il n'obtient pas une
  supposition fausse.
* **L'aller-retour.** Le tool ne relit jamais un fichier qu'il a écrit.
* **Membres `init` / `required`, construction par propriétés.** Constructeur et fabrique statique
  uniquement.
* **Tout ce qui relève de `--all`.** Arguments de type explicites seulement.

---

## 10. Architecture

### 10.1 Deux projets

| Projet | TFM | Rôle |
|---|---|---|
| `JustDummies.GenAny` | `netstandard2.0`, épinglé au plancher Roslyn (§13.2) | Le moteur. Résolution, lecture des gardes, composition, émission. |
| `JustDummies.Cli` | `net8.0`, `RollForward=Major` | La coquille. Commandes, chargement du projet, IO fichier, console. |

Sur le nom : le moteur existant du tool frère de ce dépôt s'appelle `GenDoc` — un nom de
**fonction**, pas un nom de pattern (`GenDoc` génère de la documentation). `GenAny` le suit
exactement : il génère les types `AnyX`, et `Any` est le nom central de la bibliothèque
(`Any.String()`, `IAny<T>`, `AnyOrder`). « Scaffolder » a été écarté comme nom de projet — il
nomme un rôle générique plutôt qu'un produit, et tous les frameworks en ont un. Le mot survit dans
la prose, où il décrit un **comportement** (§1) ; le projet est nommé d'après ce qu'il **produit**.

### 10.2 La frontière

**`JustDummies.GenAny` possède** la table de résolution (§5.2), la lecture des gardes (§5.3), la
composition et la reconnaissance de fabriques (§5.4), l'émetteur (§11.2) et la fonction de nommage
(§11.3).
Il dépend de `Microsoft.CodeAnalysis.CSharp` **uniquement** — pas de `Workspaces`, dont il n'a pas
besoin : la lecture des gardes veut un arbre syntaxique et un modèle sémantique, et l'émission est
de la construction de chaînes.

**Il ne fait aucune IO, n'écrit sur aucune console, et ne touche jamais MSBuild.** Ces trois
contraintes sont ce qui le maintient chargeable dans un hôte Roslyn.

**`JustDummies.Cli` possède** les définitions de commandes et de settings Spectre, la découverte de
projet, `MSBuildLocator` / `MSBuildWorkspace`, l'écriture de fichiers, la gestion de `--force` /
`--dry-run`, le rendu du récapitulatif console, et les codes de sortie du §7.

### 10.3 Le contrat entre les deux

Un point d'entrée, taillé pour que le futur consommateur IDE puisse l'appeler tel quel :

* **Entrée** — une `Compilation`, l'`ITypeSymbol` cible, et un enregistrement d'options portant la
  surcharge de namespace et le motif de nommage du type (§16).
* **Sortie** — un modèle de résultat, jamais une chaîne nue :
  * le nom de fichier et le texte source complet ;
  * une ligne par paramètre : nom, type affiché, expression émise (ou aucune), et provenance (§6) ;
  * les avertissements, comme le cas de masquage `Any*` du §7 ;
  * un indicateur « contient au moins un TODO ».

La CLI rend ce modèle ; un code refactoring appliquerait le texte source et ignorerait le reste.
Rien dans le modèle n'est une chaîne destinée à une console.

### 10.4 Packaging

`JustDummies.Cli` est packagé comme le .NET tool (`PackAsTool`, `ToolCommandName=dum`,
`PackageId=JustDummies.Cli`). `JustDummies.GenAny` n'est **pas publié comme package propre** en
v1.0 : il voyage dans le package du tool comme dépendance managée ordinaire, exactement comme le
dépôt frère publie son moteur `GenDoc`. Le publier plus tard, quand un consommateur IDE existera,
est une décision purement additive.

Conséquence : aucun des deux projets ne porte de promesse de compatibilité d'API publique, donc
aucun ne prend de baseline d'API publique (§13.4).

**D9 s'applique aux deux.** Aucun des deux ne référence le package ni le projet `JustDummies`.
Chaque symbole JustDummies est résolu par nom de métadonnée contre la compilation du développeur,
exactement comme le font les analyzers de la bibliothèque. L'écart de version entre le tool et la
bibliothèque est donc structurellement impossible, et le package du tool ne doit déclarer aucune
dépendance `JustDummies` (§13.6).

---

## 11. Notes d'implémentation

### 11.1 Chaîne de traitement

1. `MSBuildLocator.RegisterDefaults()` — **avant de toucher au moindre type de workspace Roslyn**.
   Charger `MSBuildWorkspace` d'abord est la façon classique dont cela échoue, avec une
   `FileNotFoundException` sur `Microsoft.Build` qui ne nomme rien d'utile. (CLI uniquement.)
2. `MSBuildWorkspace.Create()`, ouvrir le projet, prendre sa `Compilation`. Les diagnostics du
   workspace sont remontés, pas avalés. (CLI uniquement.)
3. Passer la `Compilation` au moteur. Tout ce qui suit est `JustDummies.GenAny`.
4. Résoudre `JustDummies.Any`, `JustDummies.IAny\`1` et `JustDummies.AnyExtensions` par nom de
   métadonnée. Absents → le moteur le signale et la CLI sort en `1` (§7).
5. Résoudre le type cible (§3.2), choisir le constructeur (§5.1).
6. Par paramètre : table de base (§5.2) → gardes (§5.3) → composition (§5.4) → non résolu (§5.5).
   Tout membre candidat est cherché dans la compilation avant d'être retenu (D4).
7. Émettre dans le modèle de résultat (§10.3).
8. La CLI écrit le fichier et rend le récapitulatif.

### 11.2 Émetteur

Un simple constructeur de chaînes sur un modèle ordonné, pas `SyntaxFactory`. La sortie doit être
lisible et correspondre à une mise en page écrite à la main — déclarations de champs alignées, types
explicites, accolades — et l'espacement normalisé par `SyntaxFactory` ne produit pas cela.
L'émetteur étant couvert par des tests à fichier de référence (§12), l'argument de fragilité en
faveur d'une API syntaxique ne s'applique pas.

### 11.3 Nommage

Faire passer le nom du type émis par **une seule** fonction,
`TypeNaming.GeneratorNameFor(ITypeSymbol, NamingOptions)`. La v1.1 (§16) devient alors une
modification de cette fonction plus une liaison d'options, pas un balayage. En v1.0
`NamingOptions` ne porte qu'un motif fixe, `Any{Type}`.

---

## 12. Plan de tests

**Moteur — `JustDummies.GenAny.UnitTests`** (le gros) :

* **Tests unitaires du résolveur.** Construire une `CSharpCompilation` en mémoire avec une référence
  sur le `JustDummies.dll` construit, et asserter la chaîne d'expression émise par paramètre.
  Rapide, sans MSBuild. Couvrir chaque ligne du §5.2, chaque ligne du §5.3, les deux chemins du
  §5.4 et le repli du §5.5. Inclure le cas non signé (`p <= 0` sur un `uint`) et le cas nullable de
  type valeur.
* **Fichiers de référence de l'émetteur.** Un fichier approuvé par forme représentative : aucun
  paramètre, un paramètre, six paramètres, un TODO, une collision de nom, un record positionnel,
  une cible à fabrique statique.
* **Tests de compilation de la sortie.** Chaque fichier de référence est compilé contre
  `JustDummies.dll` **avec les analyzers JustDummies branchés**, et la compilation ne doit produire
  aucune erreur `CS*` ni aucun diagnostic `JD*`. C'est le contrôle que D3 rend possible : le fichier
  n'étant pas marqué comme code généré, les analyzers tournent réellement dessus. Le harnais doit
  inclure un **fichier de contrôle avec une violation connue**, dont on asserte qu'elle se
  déclenche — sinon « aucun diagnostic » ne se distingue pas de « analyzers non chargés » (§17.2).
* **Le test sur le code du dépôt.** Scaffolder les **vrais types du dépôt hôte**, compiler les
  résultats et générer une valeur depuis chacun. Le raisonnement est consigné dans la décision
  « faire tourner les analyzers sur notre propre code » (§13.7) : une règle et le snippet qui la
  teste, écrits par le même auteur, partagent la même idée fausse et passent ensemble ; du code
  écrit pour d'autres raisons, non. `ErrorCode.Create` du dépôt actuel est le cas canonique — il
  garde sur `IsNullOrWhiteSpace`, donc sans le §5.3 le code scaffoldé échoue environ une fois sur
  seize, ce qu'aucun fichier de référence ne révélerait. Dans un dépôt dépourvu de tels types,
  prendre n'importe quel value object validant doté d'une fabrique statique.
* **Test de sélection d'asset.** Scaffolder contre un consommateur de l'asset `netstandard2.0` et un
  consommateur de l'asset `net8.0` pour un type ayant un paramètre `DateOnly`, et asserter que le
  premier produit un TODO et le second `Any.DateOnly()`. C'est la preuve exécutable de D4 (§13.8).

**Coquille — `JustDummies.Cli.UnitTests`** : découverte de projet, gestion des options, codes de
sortie du §7, et rendu du récapitulatif depuis un modèle de résultat figé.

---

## 13. Ce que le dépôt hôte doit fournir

JustDummies a vocation à rejoindre son propre dépôt avant que ce tool soit construit. Cette section
énonce chaque dépendance envers l'hôte comme une **exigence**, avec la réalisation actuelle en
exemple. Si la bibliothèque a déménagé, rétablir tout cela là-bas ; ne pas construire le tool contre
l'infrastructure d'un autre dépôt.

**13.1 Versions de packages épinglées** pour les dépendances du tool. Nouvelles pour le tool :
`Microsoft.CodeAnalysis.Workspaces.MSBuild` et `Microsoft.Build.Locator` (CLI uniquement). Déjà
présentes pour la bibliothèque et ses analyzers : `Microsoft.CodeAnalysis.CSharp` et
`Spectre.Console.Cli`. *Réalisation actuelle : gestion centralisée des packages dans
`Directory.Packages.props`.*

**13.2 Une propriété de plancher Roslyn.** `JustDummies.GenAny` doit compiler contre la **même
version minimale de Roslyn que le package d'analyzers**, et ne pas flotter au-dessus — un assembly
chargé par le compilateur d'un consommateur échoue silencieusement (`CS8032`) sur un hôte plus
ancien s'il a été construit contre un Roslyn plus récent. *Réalisation actuelle :
`RoslynFloorVersion` = `4.8.0`, posée une fois dans `Directory.Build.props` et appliquée avec
`VersionOverride`.* La CLI n'est **pas** liée par cela : elle héberge son propre compilateur.

**13.3 Imbrication dans la solution.** Si l'hôte utilise un `.sln`, ajouter les deux projets et les
deux projets de test à son `GlobalSection(NestedProjects)`, sous les dossiers de solution source et
tests. Un projet absent de cette section apparaît en vrac à la racine de la solution au lieu d'être
groupé avec ses frères. Cela a été manqué puis corrigé après coup à plusieurs reprises ; le vérifier
à chaque ajout de `.csproj`.

**13.4 Exclusion de la baseline d'API publique.** Ni `JustDummies.GenAny` ni `JustDummies.Cli`
n'adhèrent à la baseline d'API publique : les outils ne portent aucune promesse de compatibilité, et
l'analyzer signalerait toute leur surface comme non déclarée. *Réalisation actuelle : seules les
bibliothèques publiées importent `build/PublicApiBaseline.props`.*

**13.5 Tests de mutation.** Si l'hôte mesure la mutation sur les projets dont le code est publié ou
s'exécute, les deux projets qualifient. Donner à chacun sa propre configuration — le moteur est la
cible à forte valeur, la coquille non — et les enregistrer avec les autres. *Réalisation actuelle :
un JSON par projet sous `build/stryker/`, piloté par un flux dédié, consultatif par pull request et
imposé par un balayage hebdomadaire.*

**13.6 Un train de publication pour le tool,** distinct de celui de la bibliothèque. Le tool ne
versionne pas en lockstep avec la bibliothèque (D9), donc il ne doit pas monter sur son train.
L'étape de packaging du train doit asserter que le `.nupkg` produit ne déclare **aucune dépendance
`JustDummies`** — la forme exécutable de D9. *Réalisation actuelle : `tools/packaging/pack.sh` avec
un train par famille de packages et une assertion « standalone » déjà écrite pour le train de la
bibliothèque.*

**13.7 Les analyzers doivent pouvoir tourner sur le code de l'hôte,** pour que le test sur le code
du dépôt (§12) puisse exister. *Réalisation actuelle : le projet d'analyzers est branché sur les
suites du dépôt lui-même, décision prise après avoir constaté que la suite unitaire des analyzers
n'attrapait pas cinq règles fausses que le passage sur du vrai code a attrapées immédiatement.*

**13.8 Un moyen de consommer la bibliothèque packagée depuis deux TFM consommateurs,** pour que le
test de sélection d'asset (§12) puisse exister : un consommateur en `net8.0` (résout l'asset
`net8.0`) et un en dessous (résout `netstandard2.0`). *Réalisation actuelle : un projet isolé hors
solution, multi-ciblé, consommant le `.nupkg` packagé depuis un flux local.*

**13.9 Framework de tests.** *Réalisation actuelle : `xunit.v3`, `NFluent`, `Verify.XunitV3` pour
les fichiers de référence, `NSubstitute`.* Tout équivalent convient ; les tests à fichier de
référence ont besoin d'une bibliothèque de snapshots.

**13.10 Conventions de commit, de branche et de pull request,** et un processus ADR pour le §15.
*Réalisation actuelle : Conventional Commits avec une liste close de types et de scopes, imposée par
un hook et par la CI ; ADR sous `doc/handwritten/for-maintainers/adr/` où un agent rédige en
`Proposed` et le mainteneur accepte.*

---

## 14. Faits sur la bibliothèque dont dépend cette spécification

Tout ce qui suit a été lu dans la source de la bibliothèque. C'est inliné pour que ce document
puisse être implémenté sans ouvrir la bibliothèque, et pour qu'un lecteur futur puisse repérer
quelles affirmations sont porteuses. Le §14.7 donne la commande pour redériver chaque bloc.

### 14.1 Identité du package et frameworks cibles

* `PackageId` **`JustDummies`**, `TargetFrameworks` **`netstandard2.0;net8.0`**, `Nullable` activé,
  `LangVersion` latest.
* Les deux assets divergent : la branche `net8.0` porte en plus `DateOnly`, `TimeOnly`, `Int128`,
  `UInt128` et `Half`, gardés par `#if NET8_0_OR_GREATER`. Un consommateur sous `net8.0` résout
  l'asset `netstandard2.0` et ne les voit pas. C'est le fait que D4 existe pour absorber.
* Les analyzers voyagent **dans** ce package, sous `analyzers/dotnet/cs`, donc tout consommateur les
  reçoit automatiquement. C'est pour cela que le fichier émis est analysé du tout (D3).
* Un package compagnon adapte la bibliothèque à xUnit v3 (`[Reproducible]`) ; le tool n'interagit
  pas avec lui.

### 14.2 Points d'entrée

`JustDummies.Any` est une façade statique, répartie en fichiers partiels par famille. L'ensemble
complet des fabriques, toutes tirant du contexte aléatoire ambiant :

* **Primitifs** — `String()`, `Boolean()`, `Char()`, `Guid()`,
  `SByte()`, `Byte()`, `Int16()`, `UInt16()`, `Int32()`, `UInt32()`, `Int64()`, `UInt64()`,
  `Single()`, `Double()`, `Decimal()`,
  `TimeSpan()`, `DateTime()`, `DateTimeOffset()`,
  `Enum<TEnum>() where TEnum : struct, Enum`.
* **Asset `net8.0` uniquement** — `DateOnly()`, `TimeOnly()`, `Int128()`, `UInt128()`, `Half()`.
* **Motif** — `StringMatching(string)`, `StringMatching(Regex)`.
* **URI** — `Uri()`, puis un sélecteur de famille : `.Web()`, `.Ftp()`, `.Mailto()`, `.Relative()`,
  `.WebSocket()`.
* **Choix** — `OneOf<T>(params T[])`, `ElementOf<T>(IReadOnlyList<T>)`,
  `ElementOf<T>(IEnumerable<T>)`.
* **Collections** — `ListOf<T>`, `ArrayOf<T>`, `SequenceOf<T>`, `SetOf<T>` (avec comparateur
  optionnel), `DictionaryOf<TKey,TValue>` (avec comparateur de clés optionnel).
* **Composition** — `Combine` en arités 2 à 8, `PairOf`, `TripleOf`.
* **Reproductibilité** — `WithSeed(int)`, `UseSeed(int)`, `UseSeed(int, string)`,
  `Reproducibly(...)`, `ReproduciblyAsync(...)`.

Attention aux pièges de nommage : c'est **`Any.Boolean()`**, pas `Any.Bool()` ; et `double` se
projette sur **`Any.Double()`**, pas `Any.Decimal()`.

`AnyContext`, retourné par `Any.WithSeed(int)`, reflète les primitifs, le motif, l'URI et les points
d'entrée de choix comme méthodes **d'instance** tirant de sa propre source fixe. Il ne reflète
**pas** les points d'entrée de collection ni de composition. D7 le met hors périmètre.

La bibliothèque déclare **39 noms de types publics `Any*`** (37 generators plus `AnyContext` et
`AnyGenerationException`). C'est cet ensemble que l'avertissement de masquage du §7 interroge.

### 14.3 Surfaces de contraintes utilisées par l'émetteur

| Famille de generator | Contraintes utilisées par les §5.2 et §5.3 |
|---|---|
| `AnyString` | `NonEmpty`, `WithMinLength`, `WithMaxLength`, `WithLength`, `WithLengthBetween`, `StartingWith`, `EndingWith`, `Containing`, `Alpha`, `Numeric`, `AlphaNumeric`, `UpperCase`, `LowerCase`, `WithChars`, `OneOf`, `Except`, `DifferentFrom` |
| Entiers signés (`SByte`, `Int16`, `Int32`, `Int64`) | `Positive`, `Negative`, `NonZero`, `Zero`, `Between`, `GreaterThan(OrEqualTo)`, `LessThan(OrEqualTo)`, `MultipleOf`, `OneOf`, `Except`, `DifferentFrom` |
| **Entiers non signés** (`Byte`, `UInt16`, `UInt32`, `UInt64`) | les mêmes **moins `Positive` et `Negative`**, qu'un type non signé ne peut pas exprimer |
| `AnyDouble`, `AnySingle` | comme les entiers signés, moins `MultipleOf` |
| `AnyDecimal` | comme les entiers signés, moins `MultipleOf`, plus `WithScale` |
| `AnyGuid` | `NonEmpty`, `Empty`, `OneOf`, `Except`, `DifferentFrom` |
| `AnyBoolean` | `True`, `False`, `DifferentFrom` |
| `AnyEnum` | `AllowingCombinations`, `OneOf`, `Except`, `DifferentFrom` |
| Temporels (`DateTime`, `DateTimeOffset`, `DateOnly`, `TimeOnly`) | `After(OrEqualTo)`, `Before(OrEqualTo)`, `Between`, `WithGranularity`, `OneOf`, `Except`, `DifferentFrom` |
| `AnyTimeSpan` | style temporel plus `Positive`, `Negative`, `NonZero`, `Zero` |
| Collections | `Empty`, `NonEmpty`, `WithCount`, `WithCountBetween`, `WithMinCount`, `WithMaxCount`, `Containing`, `ContainingAny` |

La ligne non signée est celle qui mord : c'est pour elle que D4 doit filtrer `.Positive()` plutôt que
laisser l'émetteur supposer une algèbre numérique uniforme.

### 14.4 Seams de composition

* `AnyExtensions.As<TSource,TResult>(this IAny<TSource>, Func<TSource,TResult>)` → `IAny<TResult>`.
  Un groupe de méthodes comme `OrderReference.Create` s'y lie directement. Quand la fabrique rejette
  la valeur générée, l'appel lève `AnyGenerationException`.
* `Any.Combine` (arités 2 à 8) → `IAny<TResult>`.
* Les generators de collection dérivent d'une base commune implémentant `IAny<TCollection>` :
  `ListOf` → `List<T>`, `ArrayOf` → `T[]`, `SequenceOf` → `IEnumerable<T>`, `SetOf` → `HashSet<T>`,
  `DictionaryOf` → `Dictionary<TKey,TValue>`.
* `NullableExtensions.OrNull<T>()` existe en deux formes, une pour les types valeur et une pour les
  types référence annotés. **D10 interdit d'émettre l'une ou l'autre.**

### 14.5 Invariants sémantiques dont dépend le code émis

Ces cinq-là sont ceux qui casseraient silencieusement le code émis s'ils changeaient. Chacun est
exercé au §17.

1. **La source ambiante se résout au moment du tirage.** Chaque fabrique `Any.*` capture une source
   ambiante singleton, et cette source lit la frame `AsyncLocal` courante à l'intérieur de
   `Generate()`, pas à la construction. C'est pour cela qu'une recette construite hors d'un scope de
   reproductibilité y rejoue quand même (§8.2).
2. **`IAny<out T>` est covariante.** D'où l'absence d'adaptateur pour les lignes d'interface de
   collection du §5.2 — et la nécessité d'un pour la ligne nullable de type valeur.
3. **Les generators sont des recettes immuables.** Chaque contrainte fluide retourne une nouvelle
   instance. D2 en hérite.
4. **`Any.String()` non contraint tire 0 à 16 lettres et chiffres ASCII.** Il peut retourner la
   chaîne vide ; il ne peut jamais retourner du blanc. Les deux moitiés comptent pour les §5.2 et
   §5.3.
5. **`Any.OneOf(value)` exige au moins une valeur, rejette les éléments `null`, et consomme un
   tirage.** Ces trois raisons sont pourquoi le §4.2 émet un `FixedValue<TValue>` privé à la place.

### 14.6 Inventaire des analyzers

28 identifiants de diagnostic sur 27 classes d'analyzer — `JD023` et `JD024` en partagent une.

| Plage | Catégorie | Sévérités |
|---|---|---|
| `JD001`–`JD004` | Reproducibility | toutes **Error** |
| `JD005` | Usage | **Error** |
| `JD006` | Usage | Warning |
| `JD007`–`JD010` | Reproducibility | Warning |
| `JD011` | Usage | **Désactivé par défaut** |
| `JD012`–`JD013` | Usage | Warning |
| `JD014`–`JD017` | Constraints | Warning |
| `JD018` | Reproducibility | Warning |
| `JD019` | Reproducibility | **Désactivé par défaut** |
| `JD020` | Reproducibility | Info |
| `JD021` | Reproducibility | Warning |
| `JD022` | Reproducibility | Info |
| `JD023` | Constraints | Warning |
| `JD024` | Constraints | Info |
| `JD025`–`JD026` | Constraints | Warning |
| `JD027`–`JD028` | Composition | Warning |

Trois faits à leur sujet pilotent des décisions de ce document :

* **Les 27 appellent `ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None)`** — d'où D3.
* **Les règles `Usage` matchent tout type implémentant `IAny<T>`**, pas une liste de generators
  intégrés — d'où le second bénéfice de D2.
* **Les règles `Reproducibility` matchent les chaînes enracinées sur la façade statique `Any`**, et
  répondent délibérément « non » pour un generator atteint par un local, un champ ou un paramètre.
  `new AnyOrder().Generate()` leur est donc invisible ; c'est une limite connue et acceptée, pas un
  défaut que le tool peut corriger.

### 14.7 Comment redériver ces faits

Depuis la racine du dépôt de la bibliothèque :

```console
# 14.1  identité du package et clivage de TFM
grep -n "TargetFrameworks\|PackageId\|analyzers/dotnet/cs" JustDummies/JustDummies.csproj
grep -n "#if NET8_0_OR_GREATER" JustDummies/Any.Primitive.cs

# 14.2  points d'entrée, et le miroir AnyContext
grep -hn "public static" JustDummies/Any.*.cs
grep -n "public " JustDummies/AnyContext.cs
grep -rhoP "^public (sealed )?class \KAny\w+" JustDummies/*.cs | sort

# 14.3  surfaces de contraintes
grep -oP "public AnyInt32 \K\w+(?=\()" JustDummies/AnyInt32.cs | sort -u
grep -oP "public AnyUInt32 \K\w+(?=\()" JustDummies/AnyUInt32.cs | sort -u   # noter : ni Positive ni Negative

# 14.4  seams de composition
grep -n "public static" JustDummies/AnyExtensions.cs JustDummies/NullableExtensions.cs

# 14.5  invariants — les docs XML les énoncent tous les cinq
sed -n '1,60p' JustDummies/IAny.cs
grep -n "AmbientRandomSource.Instance" JustDummies/Any.Primitive.cs | head -3

# 14.6  inventaire des analyzers et exemption du code généré
cat JustDummies.Analyzers/AnalyzerReleases.Unshipped.md
grep -rlc "ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None)" JustDummies.Analyzers/*.cs | wc -l
```

Les chemins sont ceux du dépôt actuel ; les ajuster si la bibliothèque a déménagé.

---

## 15. ADR à rédiger

Rédigés en `Status: Proposed`, un par décision ; le mainteneur accepte. Aucun n'entre en conflit
avec une décision acceptée : D2 renforce la décision sur les analyzers recette-contre-valeur, et D9
respecte la règle d'indépendance de la bibliothèque.

| Décision | Titre proposé |
|---|---|
| D1 | Scaffold the generator once and hand the file to the developer |
| D3 | Leave the scaffolded file open to the JustDummies analyzers |
| D4 | Emit only members resolved in the target compilation |
| D5 + D6 | Seed generators from constructor guards, and leave the rest as a compile error |
| D9 | Give the scaffolder no dependency on the JustDummies package |
| D11 | Keep the scaffolding engine loadable by a Roslyn host |

Un suivi côté bibliothèque mérite une proposition séparée et n'est **pas** requis pour la v1.0 :
`Any.Fixed<T>(value)`, un `IAny<T>` retournant une constante. `Any.OneOf(value)` remplit presque le
rôle mais refuse `null` et consomme un tirage (§14.5). L'ajouter permettrait à l'émetteur
d'abandonner le helper imbriqué `FixedValue<TValue>`. Ajout d'API publique, donc : décision du
mainteneur.

---

## 16. Réservé pour la v1.1+

La v1.0 ne doit pas s'interdire ces évolutions ; le §11.3 est la contrainte qui garde la première
bon marché.

**Nommage.** `AnyOrder` → `OrderFactory`, ou tout autre motif. Forme :

```console
dum generate Order --name OrderFactory        # ce type uniquement
dum generate Order --pattern "{Type}Factory"  # cette exécution
```

plus un `dum.json` optionnel à la racine du projet pour un défaut à l'échelle du projet :

```json
{ "naming": { "pattern": "Any{Type}" } }
```

`{Type}` est le seul emplacement. Le motif par défaut reste `Any{Type}`, donc un projet existant ne
voit aucun changement. C'est aussi la réponse à l'avertissement de masquage du §7.

**Autres éléments reportés.** `--all` ; les membres `init` / `required` et la construction par
propriétés ; le support d'`AnyContext` (D7) ; un sélecteur `--ctor` quand plusieurs constructeurs se
disputent ; l'extension du §5.3 à une bibliothèque auxiliaire de type `Guard.Against` ; la
publication de `JustDummies.GenAny` comme package propre une fois qu'un consommateur IDE existe ; le
code refactoring IDE lui-même.

Délibérément **non** reportés — abandonnés : un verbe `check`, un mode source generator, et toute
forme de régénération ou de détection de dérive. D1 supprime le problème qu'ils résoudraient.

---

## 17. Vérifications

### 17.1 Ce qui a été contrôlé

Le fichier émis du §4.1 a été écrit à la main exactement comme spécifié — paramètre `int?`, helper
`FixedValue<TValue>` et `AnyCustomer` composé compris — puis compilé et exécuté contre le
`JustDummies.dll` construit depuis la source (asset `net8.0`), avec les analyzers JustDummies
branchés. Les résultats ci-dessous sont ce que le harnais a affiché.

| Affirmation | Où | Résultat |
|---|---|---|
| Le squelette spécifié compile tel quel | §4.1 | compile, 0 avertissement |
| Le chaînage `.WithX` fonctionne et ne perturbe pas une base partagée | D2, §4.2 | deux `.WithStatus` sur une même base restent indépendants |
| `AnyOrder` est accepté par les seams de composition de la bibliothèque | D2, §2.2 | `Any.ListOf`, `Any.PairOf` et `.As` l'acceptent tous |
| `.WithX(IAny<T>)` maintient la composition contrainte ouverte | §4.2 | `.WithReference(Any.String().StartingWith("ORD-").As(...))` donne `ORD-x9vDEd2` |
| Une recette construite **hors** d'un scope rejoue dedans | §8.2, §14.5 | deux exécutions `Any.Reproducibly(20260730, …)` ont produit des valeurs identiques |
| La chaîne dérivée des gardes ne lève jamais | §5.3 | 500 tirages à travers `OrderReference.Create`, aucune `AnyGenerationException` |
| La chaîne **sans** lecture des gardes lève par intermittence | §5.3 | **594 / 10 000** tirages ont levé — environ 1 sur 16 |
| La covariance des collections ne demande aucun adaptateur | §5.2, §14.5 | `Any.ListOf(...)` affecté à `IAny<IReadOnlyList<string>>` |
| Un nullable de type valeur **exige** bien le saut `.As` | §5.2 | `IAny<int>` n'est pas un `IAny<int?>` ; `.As(value => (int?)value)` compile |
| La sortie scaffoldée ne lève aucun diagnostic JD | D3, §12 | 0 diagnostic sur les fichiers émis |
| Les analyzers étaient réellement chargés | D3 | un fichier de contrôle a levé `JD006` et `JD005` dans le même build |
| `<auto-generated/>` les éteint | D3, §2.1 | le même fichier de contrôle, ainsi marqué, en a levé **0** — l'erreur `JD005` comprise |

### 17.2 Comment le rejouer

Rien du harnais n'est exotique ; il vaut la peine d'être recréé chaque fois que la bibliothèque
déménage ou change de version.

1. Construire la bibliothèque et les analyzers en `Release` (branche `net8.0` pour la bibliothèque).
2. Créer un projet console `net8.0` jetable **hors** du dépôt, pour qu'aucune propriété de build à
   l'échelle du dépôt ne s'applique. Référencer le `JustDummies.dll` construit par un
   `<Reference>` / `<HintPath>`, et l'analyzer construit par
   `<Analyzer Include="…/JustDummies.Analyzers.dll" />`.
3. Ajouter le domaine du §4.1 (`Order`, `OrderReference` avec son `Create` gardé, `Customer`,
   `OrderStatus`) et les `AnyOrder.cs` / `AnyCustomer.cs` scaffoldés exactement comme le §4.1 les
   spécifie.
4. Ajouter un **fichier de contrôle** avec deux violations connues — une contrainte dont le résultat
   est jeté (`Any.String().NonEmpty();` comme instruction, `JD006`) et un generator dans une chaîne
   interpolée (`$"{Any.Int32()}"`, `JD005`). Compiler, et confirmer que **les deux se déclenchent**.
   Sans cette étape, « aucun diagnostic sur le fichier scaffoldé » ne se distingue pas de
   « l'analyzer n'a jamais été chargé » — piège dans lequel cette vérification est tombée au premier
   essai.
5. Préfixer ce même fichier de contrôle par `// <auto-generated/>` et recompiler : les deux
   diagnostics disparaissent et le build réussit. C'est la preuve de D3.
6. Lancer les assertions du §17.1. Pour la mesure, boucler
   `Any.String().As(OrderReference.Create).Generate()` 10 000 fois en comptant les
   `AnyGenerationException`.

Note d'exécution : si seul un runtime .NET plus récent est installé, la sortie `net8.0` s'exécute
quand même sous `DOTNET_ROLL_FORWARD=LatestMajor`.
