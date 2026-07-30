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
* **§15, c'est le raisonnement.** Huit enregistrements de décision au format ADR de ce dépôt, tenus
  dans la spécification parce que le dépôt qui devrait les accueillir n'existe pas encore. À lire
  quand on veut savoir *pourquoi*, ou quand on est tenté de revenir sur une décision du §2.
* **§16, c'est la frontière de la v1.0.** Ce qui est reporté, et ce qui a été abandonné net.
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

Ce sont les décisions porteuses. Neuf d'entre elles sont couvertes par les huit enregistrements de
décision du §15 — contexte, argument, alternatives écartées, conséquences ; D5 et D6 en partagent un. Cette table en est l'index ; elle
ne porte aucun argument propre.

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

/// <summary>
///     A generator of arbitrary <see cref="Order" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins it; to draw from an isolated
///     <c>Any.WithSeed(...)</c> context, pass that context's generators through the
///     <c>With…</c> overloads.
/// </summary>
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
| `p <= 0` ; ou `p < 1` sur un type **intégral** | `.Positive()` |
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

Les contraintes reconnues **se composent quand elles bornent des choses différentes, et sont
abandonnées quand elles se heurtent**. Deux gardes posant une borne inférieure et une borne
supérieure sont complémentaires — `.NonEmpty()` avec `.WithMaxLength(10)`, ou
`.GreaterThanOrEqualTo(0)` avec `.LessThanOrEqualTo(100)` — et les deux sont conservées. C'est
l'idiome d'intervalle borné ordinaire, écrit en deux gardes consécutives ; l'écarter rendrait la
lecture des gardes inutile pour le cas qu'elle rencontre le plus souvent. Les deux compositions ont
été vérifiées contre la bibliothèque (§17).

Deux gardes posant *la même* borne, ou un jeu de caractères contre un autre, sont inconciliables :
les deux sont abandonnées et le paramètre est signalé `guards not combined`. Une borne inférieure
au-dessus d'une borne supérieure aussi — la bibliothèque rejette cette chaîne par
`ConflictingAnyConstraintException`, et `JD023` la signale à la compilation (§17), mais le moteur ne
doit pas l'émettre pour autant.

Une **garde de motif reconnue est exclusive**. `Any.StringMatching(...)` retourne un generator
n'exposant que `DifferentFrom` et `Except` (§14.3), donc aucune contrainte de longueur ou de jeu de
caractères ne peut y être chaînée — une telle émission ne compile tout simplement pas. Quand une
garde de motif est reconnue, elle remplace le generator de base et toute autre contrainte de chaîne
sur ce paramètre est abandonnée.

Quand deux lignes apparient une même condition, **la plus spécifique gagne**. `p < 1` sur un type
intégral relève de la ligne `.Positive()` ; sur `decimal`, `double` ou `float`, de la ligne
`.GreaterThanOrEqualTo(N)`, parce que `.Positive()` admettrait les valeurs entre zéro et un que la
garde rejette. C'est un tirage rare pour un `decimal` par ailleurs non contraint — mesuré à un sur
cinq mille — et fréquent dès que le paramètre porte une autre borne (§17). Exactement le profil d'un
défaut qui survit à un test superficiel.

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

L'exécution ci-dessous porte sur le même `Order` qu'au §4.1, mais *avant* que `AnyCustomer` ne soit
scaffoldé — d'où l'unique paramètre resté ouvert. Scaffolder `Customer` puis relancer avec `--force`
le referme, et ce deux-temps est la façon prévue de traverser un graphe d'agrégats.

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
corps du constructeur était indisponible et qu'aucune garde n'a pu être lue, `unread guards` quand
le corps lève d'une façon que l'ensemble reconnu n'a pas appariée, et `unavailable` quand le
generator existe dans la bibliothèque mais pas dans l'asset que ce projet résout.

Cette dernière valeur compte plus qu'il n'y paraît. Sans elle, la dégradation de D4 est
indiscernable d'une simple ignorance du tool : un paramètre `DateOnly` sur un projet downlevel se
lirait « non inféré », alors que la vérité est « inféré, mais `Any.DateOnly()` n'existe pas ici —
change de cible, ou écris-le toi-même ». Un mot transforme une impasse en instruction.

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
  Là où le constructeur lève d'une façon que l'ensemble n'apparie pas — règle inter-paramètres,
  condition arithmétique — le paramètre obtient le generator neutre et le récapitulatif le marque
  `unread guards`, pour que le développeur sache où regarder. Là où la validation est entièrement
  déléguée à un helper (`Guard.Against.Null(p)`), il n'y a aucun `throw` à voir dans le corps, et le
  tool ne peut pas distinguer ce paramètre d'un paramètre non contraint. Dans aucun des deux cas il
  ne devine.
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
  * un indicateur « contient au moins un TODO » ;
  * **l'échec comme donnée, pas comme exception** — un type cible qui ne résout vers rien ou vers
    plusieurs candidats revient comme un résultat portant cette liste de candidats, de sorte que la
    CLI le projette sur les codes de sortie du §7 sans rien attraper. Le §11.1 place la résolution
    du type dans le moteur, donc le modèle doit porter cela ou la frontière fuit des exceptions.

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
4. Résoudre `JustDummies.Any`, ``JustDummies.IAny`1`` et `JustDummies.AnyExtensions` par nom de
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
  §5.4 et le repli du §5.5. Inclure le cas non signé (`p <= 0` sur un `uint`), le cas nullable de
  type valeur, les deux issues de composition du §5.3 (bornes complémentaires conservées, même borne
  abandonnée), l'exclusivité du motif, et `p < 1` sur un paramètre intégral puis sur un `decimal` —
  les deux lignes qui ne diffèrent que par le type du paramètre.
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
  premier produit un TODO **marqué `unavailable`** — pas seulement un TODO — et le second
  `Any.DateOnly()`. C'est la preuve exécutable de D4 (§13.8).

**Coquille — `JustDummies.Cli.UnitTests`** : découverte de projet, gestion des options, codes de
sortie du §7, et rendu du récapitulatif depuis un modèle de résultat figé.

---

## 13. Ce que le dépôt hôte doit fournir

JustDummies a vocation à rejoindre son propre dépôt avant que ce tool soit construit. Cette section
énonce chaque dépendance envers l'hôte comme une **exigence**, avec la réalisation actuelle en
exemple. Si la bibliothèque a déménagé, rétablir tout cela là-bas ; ne pas construire le tool contre
l'infrastructure d'un autre dépôt.

### 13.1 Versions de packages épinglées

Pour les dépendances du tool. Nouvelles pour le tool :
`Microsoft.CodeAnalysis.Workspaces.MSBuild` et `Microsoft.Build.Locator` (CLI uniquement). Déjà
présentes pour la bibliothèque et ses analyzers : `Microsoft.CodeAnalysis.CSharp` et
`Spectre.Console.Cli`. *Réalisation actuelle : gestion centralisée des packages dans
`Directory.Packages.props`.*

### 13.2 Une propriété de plancher Roslyn

`JustDummies.GenAny` doit compiler contre la **même
version minimale de Roslyn que le package d'analyzers**, et ne pas flotter au-dessus — un assembly
chargé par le compilateur d'un consommateur échoue silencieusement (`CS8032`) sur un hôte plus
ancien s'il a été construit contre un Roslyn plus récent. *Réalisation actuelle :
`RoslynFloorVersion` = `4.8.0`, posée une fois dans `Directory.Build.props` et appliquée avec
`VersionOverride`.* La CLI n'est **pas** liée par cela : elle héberge son propre compilateur.

### 13.3 Imbrication dans la solution

Si l'hôte utilise un `.sln`, ajouter les deux projets et les
deux projets de test à son `GlobalSection(NestedProjects)`, sous les dossiers de solution source et
tests. Un projet absent de cette section apparaît en vrac à la racine de la solution au lieu d'être
groupé avec ses frères. Cela a été manqué puis corrigé après coup à plusieurs reprises ; le vérifier
à chaque ajout de `.csproj`.

### 13.4 Exclusion de la baseline d'API publique

Ni `JustDummies.GenAny` ni `JustDummies.Cli`
n'adhèrent à la baseline d'API publique : les outils ne portent aucune promesse de compatibilité, et
l'analyzer signalerait toute leur surface comme non déclarée. *Réalisation actuelle : seules les
bibliothèques publiées importent `build/PublicApiBaseline.props`.*

### 13.5 Tests de mutation

Si l'hôte mesure la mutation sur les projets dont le code est publié ou
s'exécute, les deux projets qualifient. Donner à chacun sa propre configuration — le moteur est la
cible à forte valeur, la coquille non — et les enregistrer avec les autres. *Réalisation actuelle :
un JSON par projet sous `build/stryker/`, piloté par un flux dédié, consultatif par pull request et
imposé par un balayage hebdomadaire.*

### 13.6 Un train de publication pour le tool

Distinct de celui de la bibliothèque. Le tool ne
versionne pas en lockstep avec la bibliothèque (D9), donc il ne doit pas monter sur son train.
L'étape de packaging du train doit asserter que le `.nupkg` produit ne déclare **aucune dépendance
`JustDummies`** — la forme exécutable de D9. *Réalisation actuelle : `tools/packaging/pack.sh` avec
un train par famille de packages et une assertion « standalone » déjà écrite pour le train de la
bibliothèque.*

### 13.7 Les analyzers doivent pouvoir tourner sur le code de l'hôte

Pour que le test sur le code
du dépôt (§12) puisse exister. *Réalisation actuelle : le projet d'analyzers est branché sur les
suites du dépôt lui-même, décision prise après avoir constaté que la suite unitaire des analyzers
n'attrapait pas cinq règles fausses que le passage sur du vrai code a attrapées immédiatement.*

### 13.8 Deux TFM consommateurs pour la bibliothèque packagée

Pour que le
test de sélection d'asset (§12) puisse exister : un consommateur en `net8.0` (résout l'asset
`net8.0`) et un en dessous (résout `netstandard2.0`). *Réalisation actuelle : un projet isolé hors
solution, multi-ciblé, consommant le `.nupkg` packagé depuis un flux local.*

### 13.9 Framework de tests

*Réalisation actuelle : `xunit.v3`, `NFluent`, `Verify.XunitV3` pour
les fichiers de référence, `NSubstitute`.* Tout équivalent convient ; les tests à fichier de
référence ont besoin d'une bibliothèque de snapshots.

### 13.10 Conventions de commit, de branche et de pull request

Et un processus ADR pour le §15.
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

## 15. Enregistrements de décision

Neuf des onze décisions du §2 sont architecturales : un mainteneur futur remettrait chacune en
question, et chacune tiendrait inchangée même si l'implémentation était entièrement réécrite. Dans le cours
normal des choses, elles entreraient dans la base ADR d'un dépôt en `Proposed`, y recevraient un
numéro, et seraient acceptées par le mainteneur.

**Elles sont tenues à l'intérieur de cette spécification, parce que le dépôt qui devrait les
accueillir n'existe pas encore.** JustDummies a vocation à quitter `Reefact/first-class-errors`
avant que ce tool soit construit, et ces enregistrements décrivent un outil qui vivra dans ce
nouveau dépôt. Les faire entrer dans la base actuelle leur attribuerait des numéros — les poignées
stables sur lesquelles toute la base est bâtie — qu'il faudrait abandonner ou réécrire au
déménagement, et laisserait le journal de ce dépôt porter des décisions sur du code qu'il ne
contient plus.

Les garder ici ne coûte rien et rapporte deux choses. Le raisonnement reste attaché à la
spécification qu'il justifie, donc l'historique de décision voyage comme un seul artefact plutôt
que comme un document plus huit fichiers que quelqu'un doit penser à emporter. Et chaque
enregistrement suit le format ADR de ce dépôt, section par section, de sorte que l'admission est
mécanique : soulever l'enregistrement dans la base ADR du dépôt de destination, lui y attribuer son
numéro, conserver sa date `Proposed:`, et remplacer l'enregistrement ici par un lien.

D'ici là ce sont des brouillons. Aucun statut n'est basculé dans ce document ; le mainteneur les
accepte dans la base qui les portera.

Deux décisions du §2 ne portent délibérément aucun enregistrement. **D7** (contexte ambiant
uniquement) est une frontière de périmètre déjà listée comme reportée au §16 — une décision
programmée pour être revisitée n'est pas une décision durable, et implémenter le support
d'`AnyContext` plus tard serait un ajout, pas une supersession. **D8** (le namespace du type cible)
est un défaut dont `--namespace` est l'échappatoire, et un défaut surchargeable à chaque invocation
ne décide rien de durable. Les deux échouent au test qui tranche la question : *si l'implémentation
changeait mais que la décision tenait, l'enregistrement aurait-il besoin d'être édité ?* Pour
celles-là, l'enregistrement ne serait que l'implémentation redite.

**D10 a été déplacée dans cette section plutôt que laissée dehors.** C'est en apparence la plus
petite décision ici — une règle sur une méthode de la bibliothèque — et la taille est justement la
mauvaise mesure. Elle passe le test sur les trois plans : elle survivrait à une réécriture de
l'émetteur dans n'importe quel langage ; c'est le genre de règle qu'un mainteneur remettrait en
question, parce qu'émettre `OrNull` pour un paramètre déclaré `string?` est la lecture d'apparence
fidèle et serait signalée comme une correction de bug ; et elle a une conséquence visible au §5.2 —
la conversion explicite pour les nullables de type valeur — qui se lit comme de la complexité
accidentelle pour qui ignore pourquoi elle est là. Un enregistrement qui empêche un « nettoyage »
plausible de réintroduire des échecs intermittents mérite sa place.

---

### D1 — Scaffolder le generator une fois et confier le fichier au développeur

**Statut :** Proposed
**Proposé :** 2026-07-30
**Décideurs :** Reefact

#### Contexte

Le tool écrit un fichier C#, contenant un generator pour un type du code du développeur, dans le
projet du développeur lui-même. Trois formes existent pour un tel outil, toutes utilisées par des
outils réels : un source generator Roslyn produisant le fichier dans la sortie intermédiaire du
build ; un fichier écrit une fois dans l'arbre des sources ; et un fichier écrit dans l'arbre des
sources accompagné d'une commande de vérification qui échoue quand il ne correspond plus à ce que
l'outil produirait aujourd'hui.

Un fichier dans l'arbre des sources peut se désynchroniser, silencieusement, du type dont il a été
dérivé quand le constructeur de ce type change.

La bibliothèque que le tool sert affiche l'absence de magie dans son positionnement : pas de
réflexion, pas de remplissage de graphe d'objets, et sa propre description est « small,
deterministic, explicit ».

Le tool ne peut pas inférer tous les paramètres de constructeur. Certains portent des invariants
exprimés d'une façon qu'aucun ensemble clos de règles ne peut lire (§9), donc un fichier scaffoldé
est censé être incomplet pour certains types.

La sortie d'un source generator n'est pas éditable par le développeur et n'apparaît pas en revue de
code. Un fichier dans l'arbre des sources est les deux.

#### Décision

Le tool écrit chaque fichier de generator une fois et en transfère la propriété au développeur, qui
peut l'éditer librement et à qui il n'est jamais demandé de le régénérer.

#### Justification

La dérive est la seule objection sérieuse à l'écriture dans l'arbre des sources, et elle n'existe
que tant que le tool revendique la propriété du fichier. Une fois la propriété transférée, « le
fichier ne correspond plus à ce que le tool produirait » cesse d'être un défaut et devient l'état
attendu d'un fichier que le développeur a édité — ce que le tool lui demande précisément de faire.
L'objection se dissout au lieu d'être atténuée.

Ce transfert est aussi ce qui rend un fichier incomplet acceptable. Un outil qui possède sa sortie
doit produire quelque chose de complet ou échouer ; un outil qui remet un squelette peut s'arrêter
où sa connaissance s'arrête et le dire, ce qui est la position honnête étant donné que certains
invariants sont illisibles. D5 et D6 dépendent de ce point réglé d'abord.

L'éditabilité et la visibilité en revue servent une bibliothèque dont l'argument de vente est que
rien ne se passe dans le dos du développeur. Un generator qu'il peut lire, parcourir au débogueur
et modifier est cohérent avec ce positionnement ; un generator matérialisé par le compilateur ne
l'est pas.

Retirer la propriété retire avec elle toute une classe de machinerie : pas de verbe de
vérification, pas de protocole de régénération, pas de détection de dérive, pas de règles sur les
régions éditables à la main. Pour un outil dont la première règle de conception est d'être trivial
à adopter, la machinerie non construite vaut plus que les garanties qu'elle aurait offertes.

#### Alternatives considérées

##### Un source generator Roslyn

Considéré parce qu'il rend la dérive structurellement impossible : il rejoue à chaque build, donc
sa sortie ne peut pas retarder sur le type.

Écarté parce qu'il abandonne tout ce que l'existence réelle du fichier apporte. Le développeur ne
peut pas l'éditer, ne peut pas compléter les paramètres que le tool n'a pas su inférer, et les
relecteurs ne le voient jamais. Il n'a par ailleurs aucun moyen utile de laisser du travail
inachevé, donc le cas du paramètre non résolu devrait faire échouer le build sans offrir au
développeur d'endroit où agir.

##### Un fichier écrit plus un verbe de vérification

Considéré parce que c'est la réponse standard à la dérive pour les artefacts générés commités, et
qu'elle s'intègre proprement en intégration continue.

Écarté parce que vérification et édition s'excluent. Une commande qui échoue dès que le fichier
diffère d'une génération fraîche interdit exactement l'édition que ce tool existe pour inviter.
Garder les deux supposerait d'encoder quelles régions appartiennent au tool et lesquelles au
développeur — plus de machinerie que la fonctionnalité entière n'en vaut.

#### Conséquences

**Positives.** Le tool a un verbe et aucun protocole. Le fichier scaffoldé est du code ordinaire :
relisible, débogable, éditable. Le chemin du paramètre non résolu de D6 devient disponible.

**Négatives.** Un generator peut retarder sur son type. Ajouter un paramètre de constructeur casse
la compilation du generator, ce qui fait remonter le problème ; changer l'invariant d'un paramètre,
non — le generator continue de produire des valeurs que le constructeur rejette désormais, et seul
un test en échec le révèle.

**Risques.** Un développeur peut s'attendre à ce que la régénération préserve ses éditions. Atténué
par l'en-tête émis, qui indique que la régénération écrase et que le type est `partial` donc que
les fichiers voisins survivent, et par `--force` exigé pour écraser tout court.

#### Actions de suivi

* Énoncer la position « ce fichier est le tien » en évidence dans la documentation utilisateur du
  tool : elle inverse l'attente installée par la plupart des outils de scaffolding.

#### Références

* §1, §3, §4.3 de cette spécification.

---

### D2 — Faire du generator émis un `IAny<T>` de plein droit

**Statut :** Proposed
**Proposé :** 2026-07-30
**Décideurs :** Reefact

#### Contexte

`IAny<T>` est le seam de composition de la bibliothèque : `As`, `Combine`, les generators de
collection et ceux de choix le consomment et le produisent tous (§14.4).

L'interface est documentée comme une recette immuable, et tous les generators de la bibliothèque
l'honorent — chaque contrainte fluide retourne une nouvelle instance (§14.5).

La catégorie `Usage` des analyzers reconnaît un generator comme l'interface `IAny<T>` elle-même ou
tout type qui l'implémente, plutôt que comme une liste fixe de types intégrés (§14.6).

Le type émis expose une méthode fluide par paramètre de constructeur, ce qui lui donne la forme
d'un builder. Les builders de l'écosystème mutent conventionnellement et retournent `this`.

#### Décision

Le type émis implémente `IAny<T>` et est immuable, chaque méthode `With` retournant une nouvelle
instance.

#### Justification

Implémenter le seam est ce qui fait fonctionner les agrégats imbriqués sans code supplémentaire. Un
generator émis est directement utilisable comme generator d'élément, comme opérande de `Combine` ou
comme source de `As` ; sans l'interface, soit le tool émettrait des adaptateurs, soit le
développeur les écrirait.

Le second bénéfice est moins évident et vaut autant : les analyzers `Usage` s'appuient sur
l'interface, donc un type émis qui l'implémente est couvert par eux exactement comme un generator
intégré. Cette couverture compte plus ici qu'ailleurs, parce que le fichier émis est celui que le
développeur édite (D3), souvent en découvrant cette API.

L'immuabilité n'est pas une préférence de style mais le contrat documenté du seam. Un `With` mutant
ferait du type émis le seul generator mutable de l'écosystème, et se comporterait de façon
surprenante : deux generators dérivés d'une base partagée interféreraient. Le coût est une
allocation par appel à `With`, sur un chemin de code qui n'est pas chaud.

#### Alternatives considérées

##### Un builder mutant retournant `this`

Considéré parce que c'est la forme conventionnelle du builder et qu'il alloue moins.

Écarté parce qu'il contredit le contrat documenté de l'interface qu'il implémenterait, et parce que
dériver deux generators d'une base partagée les corromprait silencieusement tous les deux.

##### Un type ordinaire exposant `Generate`, n'implémentant pas `IAny<T>`

Considéré parce qu'il garde le fichier émis exempt de toute interface de bibliothèque.

Écarté parce qu'il abandonne les deux bénéfices d'un coup : aucune composition avec les seams de la
bibliothèque, et aucune couverture d'analyzer sur le fichier qui en a le plus besoin.

#### Conséquences

**Positives.** La composition avec tous les seams de la bibliothèque est gratuite. Quatre règles
d'analyzer s'étendent au type émis sans rien coûter.

**Négatives.** Une allocation par appel à `With`. Le constructeur privé complet grossit avec le
nombre de paramètres, donc le fichier émis est verbeux pour les constructeurs larges.

**Risques.** Si la bibliothèque relâchait un jour le contrat d'immuabilité, la forme émise serait
plus stricte que nécessaire — inoffensif, et aucune action ne serait requise.

#### Actions de suivi

* Aucune.

#### Références

* §4.2, §14.4, §14.5, §14.6 de cette spécification.

---

### D3 — Laisser le fichier scaffoldé ouvert aux analyzers JustDummies

**Statut :** Proposed
**Proposé :** 2026-07-30
**Décideurs :** Reefact

#### Contexte

Les analyzers voyagent dans le package de la bibliothèque, donc tout consommateur de celle-ci les
reçoit automatiquement (§14.1).

Les 27 exemptent le code généré (§14.6). Roslyn classe un fichier comme généré quand il se nomme
`*.g.cs` ou `*.generated.cs`, ou quand il s'ouvre sur un commentaire d'en-tête auto-generated.

L'exemption a été mesurée. Un fichier contenant exactement deux violations — un avertissement
`JD006` et une erreur `JD005` — a été compilé deux fois, en ne changeant que sa première ligne :
sans l'en-tête, les deux ont été remontées et le build a échoué ; avec `// <auto-generated/>`,
aucune n'a été remontée et le build a réussi (§17).

Le fichier scaffoldé est celui que le développeur édite (D1), et il peut sortir du tool incomplet
(D6).

La seule façon pour l'émetteur de produire une chaîne que la bibliothèque rejette à l'exécution est
deux contraintes dérivées de gardes atterrissant sur le même axe (§5.3). `JD015` et `JD023`
détectent exactement cette classe de chaîne insatisfiable.

La convention de l'écosystème est de marquer les fichiers générés, principalement pour que les
analyzers de style ne se déclenchent pas sur du code écrit par une machine.

#### Décision

Le fichier scaffoldé ne porte aucun marqueur de code généré, de sorte que les analyzers JustDummies
l'analysent comme ils analysent du code écrit à la main.

#### Justification

L'exemption est totale, et la mesure montre à quel point elle s'applique discrètement : une erreur
de compilation est devenue du silence sur un changement d'une ligne. Marquer le fichier en ferait
le seul fichier du projet de test du développeur hors du filet de sécurité de la bibliothèque.

Ce serait aussi le pire fichier à exempter. C'est celui que le développeur va éditer, avec une API
qu'il découvre peut-être, dans un fichier que le tool vient de lui demander de compléter.

La couverture sert en outre de filet aux erreurs de l'émetteur lui-même. La règle du même axe du
§5.3 élimine le cas de la chaîne conflictuelle par construction, mais un défaut dans cette règle ne
remonterait autrement que comme une exception à l'exécution ; avec le fichier analysé, il remonte
dans l'éditeur.

La raison conventionnelle du marquage — épargner les règles de style au code écrit par une machine
— ne s'applique pas à un fichier qui, par D1, n'appartient pas à une machine. C'est le code du
développeur dès l'instant où il est écrit, et il doit répondre des mêmes règles que ses voisins.

#### Alternatives considérées

##### Marquer le fichier d'un en-tête auto-generated

Considéré parce que c'est la convention de l'écosystème, et parce que cela épargnerait à un premier
scaffold les analyzers de style propres au développeur.

Écarté parce que cela désactive tout diagnostic JustDummies sur ce fichier, ce qui est l'inverse de
ce dont a besoin un fichier sur le point d'être édité à la main contre une API peu familière. La
mesure rend le coût concret : un diagnostic de sévérité erreur disparaît sans laisser de trace.

##### Nommer le fichier `*.g.cs`

Considéré comme une variante plus légère de la même idée.

Écarté pour la même raison, plus une autre : le nom affirme une propriété machine que D1 nie.

#### Conséquences

**Positives.** Le fichier scaffoldé est couvert par les mêmes diagnostics que le code qui l'entoure,
et les erreurs de l'émetteur remontent à l'édition plutôt qu'à l'exécution.

**Négatives.** Les analyzers et règles de style propres au développeur se déclenchent aussi dessus,
donc un premier scaffold peut demander une passe de formatage pour rejoindre le style maison.
L'émetteur limite cela en écrivant des types explicites et une mise en page conventionnelle, mais
il ne peut pas coller à toutes les configurations.

**Risques.** Un changement futur de l'émetteur pourrait introduire un diagnostic dans tous les
fichiers scaffoldés d'un coup. Atténué par les tests de compilation de la sortie (§12), qui
échouent sur tout diagnostic `JD`.

#### Actions de suivi

* Conserver le fichier de contrôle dans le test de compilation de la sortie. Sans une violation
  connue dont on asserte le déclenchement, le test ne distingue pas « aucun diagnostic » de « les
  analyzers n'ont jamais été chargés » et devient silencieusement inopérant — le piège dans lequel
  la vérification de cette spécification est tombée au premier essai (§17.2).

#### Références

* §2, §5.3, §14.6, §17 de cette spécification.

---

### D4 — N'émettre que des membres résolus dans la compilation cible

**Statut :** Proposed
**Proposé :** 2026-07-30
**Décideurs :** Reefact

#### Contexte

La bibliothèque publie deux assets divergents. Le moderne porte cinq points d'entrée de generator
qui n'existent pas sur celui de bas niveau, parce que les types de framework sous-jacents n'y
existent pas (§14.1).

Les generators d'entiers non signés n'exposent ni contrainte `Positive` ni `Negative`, un type non
signé ne pouvant exprimer ni l'une ni l'autre (§14.3).

Le tool ne détient aucune référence sur la bibliothèque (D9), donc il ne peut pas voir l'API de
celle-ci à sa propre compilation.

La compilation du développeur fait autorité sur ce qui est réellement disponible dans son projet :
son framework cible choisit l'asset, et sa version de package choisit la surface.

Un membre émis mais absent est une erreur de compilation dans le projet du développeur, imputée au
tool.

#### Décision

Le moteur n'émet un membre JustDummies qu'après avoir résolu ce membre dans la compilation du
développeur.

#### Justification

L'alternative est une table, à l'intérieur du tool, de ce qui existe par version de bibliothèque et
par framework cible. Elle demanderait un entretien à chaque publication de la bibliothèque, serait
fausse pour toute version antérieure au tool, et encoderait des faits que la compilation connaît
déjà exactement.

La résolution remplace quatre cas particuliers indépendants par une règle : le clivage d'assets, la
surface numérique non signée, le tool plus ancien ou plus récent que la bibliothèque, et la
découverte des generators du développeur. Aucun n'a à être nommé où que ce soit dans l'émetteur.

Le mode d'échec qu'elle produit est le bon. Un membre non résoluble transforme le paramètre en
paramètre non résolu (D6) — un état que le tool traite et signale déjà — plutôt qu'en une émission
que le développeur rencontre comme une erreur de compilation qu'il n'a pas causée et ne peut pas
interpréter.

Elle rend aussi gratuite la garantie d'API publique au lieu d'en faire une contrainte à imposer :
tout ce qui est résoluble dans la compilation fait par construction partie de la surface publique
publiée, donc le tool ne peut pas émettre contre un membre interne ni hors de la baseline de
compatibilité.

#### Alternatives considérées

##### Une table de membres codée en dur par version de bibliothèque

Considérée parce qu'elle est plus simple, ne demande aucune recherche de symbole, et rend la
connaissance de l'émetteur explicite et relisible.

Écartée parce qu'elle est inmaintenable au fil des versions et tout simplement fausse pour toute
version publiée après le tool.

##### Référencer la bibliothèque et émettre contre ses types de compilation

Considérée parce qu'elle laisserait le compilateur vérifier l'usage que l'émetteur fait de l'API,
supprimant le mode d'échec « faute de frappe silencieuse » que D9 accepte.

Écartée parce qu'elle contredit D9, et parce qu'elle répondrait de toute façon à la mauvaise
question : la version que le tool référence n'est pas celle du projet du développeur.

#### Conséquences

**Positives.** Le tool est correct contre n'importe quelle version de bibliothèque et n'importe quel
framework cible, sans détenir la moindre connaissance par version.

**Négatives.** La dégradation est discrète par nature : un membre qui ne se résout pas n'apparaît
simplement pas dans l'émission, et sans un signalement délibéré le développeur ne peut pas
distinguer un paramètre que le tool n'a pas su inférer d'un paramètre dont le generator existe mais
n'est pas disponible ici.

**Risques.** Un défaut de résolution — chercher un mauvais nom de métadonnée — dégraderait tout en
TODO d'un coup, ce qui se lit comme un tool qui ne marche pas plutôt que comme un bug. Atténué par
le test de sélection d'asset (§12), qui asserte le cas présent et le cas absent.

#### Actions de suivi

* Le §6 porte la valeur de provenance `unavailable` pour cette raison. Conserver un test qui
  l'asserte : sans lui, la dégradation que cette décision accepte redevient invisible et l'exigence
  se dégrade en commentaire.

#### Références

* §5.2, §5.3, §6, §14.1, §14.3 de cette spécification.

---

### D5 + D6 — Amorcer les generators sur les gardes du constructeur, et laisser le reste en erreur de compilation

**Statut :** Proposed
**Proposé :** 2026-07-30
**Décideurs :** Reefact

#### Contexte

Les generators non contraints tirent tout leur domaine : celui des chaînes produit de zéro à seize
caractères, donc il peut retourner la chaîne vide, et celui des entiers tire tout l'intervalle,
négatifs compris (§14.5).

Les constructeurs métier rejettent couramment une partie de ce domaine.

Cela a été mesuré sur une vraie fabrique validante de ce dépôt : un generator de chaînes non
contraint composé dessus a levé 594 fois sur 10 000 tirages, environ une fois sur seize (§17).

Les clauses de garde en tête de constructeur sont l'idiome de validation dominant dans le code que
ce tool vise.

Le tool dispose du corps du constructeur en source pour tout type de la solution du développeur, et
n'en dispose pas pour un type venant d'un package.

Certains invariants ne sont pas exprimés comme des gardes du tout — validation déléguée à une
méthode auxiliaire, à une bibliothèque de gardes, ou règle portant sur deux paramètres.

Le développeur lance le tool et ouvre le fichier obtenu dans la même minute.

#### Décision

Le moteur dérive les contraintes d'un ensemble clos de clauses de garde de constructeur reconnues,
et émet un identifiant inexistant pour tout paramètre dont il ne peut pas inférer le generator.

#### Justification

Sans lecture des gardes, la sortie par défaut du tool n'est pas seulement imprécise, elle est
nuisible : elle fabrique, dans la suite de tests du développeur, l'échec intermittent que la
bibliothèque existe pour éliminer. Un échec sur seize est pire que pas d'outil du tout, parce qu'il
discrédite la bibliothèque à l'instant du premier usage.

Un ensemble clos et syntaxique borne le risque. Lire des gardes n'est pas inférer une intention ;
chaque forme reconnue se projette sur exactement une contrainte, et tout ce qui est hors de
l'ensemble est ignoré. L'appariement conservateur — un paramètre, aucune composition booléenne, des
opérandes constants — sous-signale plutôt qu'il ne se trompe, ce qui est le bon biais ici : une
contrainte manquante donne une valeur que le constructeur peut rejeter et un échec visible, tandis
qu'une contrainte fausse donne une valeur qui exerce mal le test en silence.

Pour les paramètres qui restent non résolus, une erreur de compilation est le signal le moins cher
disponible. Le développeur est dans le fichier, venant de lancer le tool ; le compilateur nomme le
paramètre dans son propre message, et ce message atteint aussi bien l'éditeur, la liste d'erreurs
que l'intégration continue. Un signal délivré plus tard coûte plus, et un signal jamais délivré
coûte le plus.

Publier un fichier qui ne compile pas n'est défendable qu'à cause de D1. Un outil qui possède sa
sortie ne le pourrait pas ; un outil qui remet un squelette le peut, et énoncer le manque
franchement est plus honnête qu'un fichier qui compile et échoue plus tard.

#### Alternatives considérées

##### Des generators neutres, tout le resserrement laissé au développeur

Considérée parce qu'elle fait que le tool n'affirme rien qu'il ne puisse prouver, ce qui est
séduisant pour une bibliothèque bâtie sur la précision.

Écartée sur la mesure. La sortie par défaut échouerait par intermittence pour la plupart des
constructeurs validants, ce qui est le mode d'échec le plus coûteux disponible et celui que la
bibliothèque a été construite pour supprimer.

##### Une exception à l'exécution pour les paramètres non résolus

Considérée parce que le fichier compile alors, ce qui est plus avenant à première vue.

Écartée parce qu'elle reporte le signal au-delà du moment où le développeur regarde le fichier, et
convertit un manque de scaffolding en un test en échec dont la cause est une ligne qu'il n'a jamais
lue.

##### Omettre du recipe le paramètre non résolu

Considérée parce que c'est la plus élégante des trois : le generator exigerait simplement du
développeur qu'il fournisse ce paramètre.

Écartée parce qu'elle est silencieuse. Le generator devient partiellement utilisable sans le dire,
et le manque remonte comme un null ou un défaut au fond d'un test.

##### Un fichier de déclaration associant des types à leur construction

Considérée parce qu'elle permettrait au développeur d'enseigner le tool une fois pour toutes,
couvrant des invariants qu'aucune garde n'exprime, et rendrait la composition correcte pour les
value objects en général plutôt que pour les seuls gardés.

Écartée pour la première version parce qu'elle convertit le tool en système de conventions, ce qui
contredit la règle de conception voulant que rien ne soit configuré avant le premier usage. Laissée
ouverte au §16.

#### Conséquences

**Positives.** Le défaut émis fonctionne pour l'idiome de validation dominant. Les paramètres non
résolus sont impossibles à manquer.

**Négatives.** Un fichier scaffoldé peut ne pas compiler tant qu'il n'est pas édité, ce qui
surprendra quiconque attend d'un scaffolding qu'il produise du code fonctionnel. Les invariants hors
de l'ensemble reconnu donnent toujours des valeurs que le constructeur rejette.

**Risques.** L'ensemble reconnu peut apparier une garde dont il se méprend sur le sens, produisant
une contrainte fausse plutôt qu'absente — le seul résultat pire que de ne rien inférer. Atténué par
les conditions d'appariement conservatrices et la règle de conflit sur le même axe ; le test sur le
code du dépôt (§12) est le contrôle le plus susceptible de l'attraper, parce qu'il fait tourner
l'émetteur sur du code écrit pour d'autres raisons.

#### Actions de suivi

* Tout ajout à l'ensemble de gardes reconnues demande un cas dans la suite du résolveur et, quand
  c'est possible, une occurrence dans le test sur le code du dépôt.

#### Références

* §5.3, §5.5, §9, §14.5, §17 de cette spécification.

---

### D9 — Ne donner au scaffolder aucune dépendance sur le package JustDummies

**Statut :** Proposed
**Proposé :** 2026-07-30
**Décideurs :** Reefact

#### Contexte

Le tool émet du code qui appelle l'API de la bibliothèque, mais n'appelle jamais cette API
lui-même.

Si le tool référençait la bibliothèque, le projet du développeur en détiendrait deux versions :
celle contre laquelle le tool a été construit et celle que le projet référence réellement.

Les analyzers de la bibliothèque résolvent déjà chaque symbole de celle-ci par nom de métadonnée
contre la compilation du consommateur, sans référencer aucun assembly de la bibliothèque ; une
règle dont le type est absent de la compilation se tait simplement.

Le dépôt hôte publie des familles de packages sur des trains de publication, chaque train publiant
ses membres à une version unique.

#### Décision

Ni le moteur ni la CLI ne référencent le package ou le projet JustDummies ; chaque symbole
JustDummies est résolu par nom de métadonnée contre la compilation du développeur.

#### Justification

La question de correction du tool n'est jamais « qu'offre la version de bibliothèque contre
laquelle j'ai été construit » mais « qu'offre la version de bibliothèque de ce projet ». Une
référence répond à la première en laissant croire qu'elle répond à la seconde, ce qui est
exactement ainsi qu'un outil se met à émettre du code qui ne compile pas chez quelqu'un d'autre.

Conjuguée à D4, l'absence de référence rend l'écart de version structurellement impossible plutôt
que seulement testé. Il n'y a aucun couple de versions à tester, parce que le tool ne détient
aucune version de la bibliothèque.

Les analyzers de la bibliothèque fonctionnent déjà ainsi, ce qui démontre que le motif suffit pour
exactement ce travail : des symboles résolus par nom, un silence gracieux quand un type est absent.

Cela découple aussi les trains de publication. Le tool sort quand le tool change et la bibliothèque
quand la bibliothèque change, et aucun ne force la publication de l'autre.

#### Alternatives considérées

##### Référencer la bibliothèque et versionner les deux en lockstep

Considérée parce qu'elle laisse le compilateur vérifier l'usage que l'émetteur fait de l'API, et
parce qu'un numéro de version identique est une histoire de compatibilité évidente à présenter aux
utilisateurs.

Écartée parce que le lockstep ne garantit que la correspondance du tool avec la bibliothèque publiée
en même temps que lui, pas avec celle du projet du développeur — le seul cas qui compte — et parce
qu'elle forcerait une publication du tool à chaque publication de la bibliothèque.

#### Conséquences

**Positives.** Aucune matrice de versions, aucune question de compatibilité à gérer, et des cadences
de publication indépendantes.

**Négatives.** La connaissance que l'émetteur a de l'API s'exprime en chaînes, donc un nom de membre
mal orthographié n'est pas une erreur de compilation dans le tool. Il remonte comme un membre non
résolu, que D4 transforme en TODO — une sortie fausse mais silencieuse.

**Risques.** Ce mode d'échec silencieux est le vrai coût de cette décision. Atténué par les tests de
compilation de la sortie et le test sur le code du dépôt (§12), qui exercent les expressions émises
contre une vraie compilation, où un membre mal orthographié apparaît en TODO à une place qui aurait
dû porter une valeur.

#### Actions de suivi

* Le package du tool doit asserter au moment du packaging qu'il ne déclare aucune dépendance
  JustDummies (§13.6) — la forme exécutable de cette décision.

#### Références

* §10.4, §13.6, §14.2 de cette spécification.

---

### D10 — Ne jamais tirer null pour un paramètre nullable

**Statut :** Proposed
**Proposé :** 2026-07-30
**Décideurs :** Reefact

#### Contexte

La bibliothèque expose `OrNull` sous deux formes — une pour les types valeur, une pour les types
référence annotés — chacune retournant un generator qui produit `null` une partie du temps (§14.4).

Un paramètre de constructeur déclaré `string?` ou `int?` énonce que null est *permis*. Il n'énonce
pas qu'un test particulier a l'intention d'exercer le chemin null.

Le principe affiché de la bibliothèque est que les contraintes expriment les invariants qu'une
valeur doit satisfaire, jamais ce que le test asserte.

Le type émis porte une surcharge `With{Param}(IAny<TParam>)` pour chaque paramètre (D2), donc un
développeur peut fournir n'importe quel generator, y compris nullable, sur un paramètre choisi dans
un test choisi.

La variance en C# ne franchit pas les types valeur, donc un paramètre nullable de type valeur exige
une conversion explicite quand le generator sous-jacent est utilisé. `OrNull` n'en exigerait
aucune, puisqu'il retourne déjà le type de generator nullable (§5.2).

Un test qui n'échoue que sur certaines exécutions est le mode d'échec que la bibliothèque existe
pour supprimer.

#### Décision

L'émetteur n'applique jamais `OrNull`, de sorte qu'un paramètre nullable tire une valeur de son
type sous-jacent et que le développeur consent à null explicitement.

#### Justification

La nullabilité dans une signature est une permission, pas une intention. La lire comme une
intention fait décider au tool, à la place du développeur et au hasard, quelles exécutions
exercent le chemin null — si bien qu'un test écrit pour le chemin ordinaire échoue sur les
exécutions qui tirent null, pour une raison étrangère à tout ce qu'il asserte. C'est l'échec
intermittent que D5 existe pour empêcher, atteint par l'autre bout.

Le consentement est déjà bon marché et précis. La surcharge par generator de D2 permet au
développeur de demander null au paramètre exact et dans le test exact où cela compte, c'est-à-dire
là où cette décision appartient : le test qui veut le chemin null le dit, et aucun autre test n'en
souffre.

Refuser ici applique aussi à un défaut la règle propre à la bibliothèque sur les contraintes.
Émettre `OrNull` encoderait ce qu'un test pourrait asserter plutôt que ce que la valeur doit
satisfaire, ce qui est la distinction sur laquelle la bibliothèque est bâtie.

#### Alternatives considérées

##### Émettre `OrNull` pour tout paramètre nullable

Considérée parce que c'est la lecture fidèle du type déclaré, qu'elle ne demande aucun cas
particulier, et que — pour les nullables de type valeur — elle est plus courte que la conversion que
cette décision impose.

Écartée parce que la fidélité à la signature coûte le déterminisme : environ la moitié des valeurs
générées seraient null sans que le test l'ait choisi. L'émission plus courte achète la brièveté au
prix de la propriété que la bibliothèque vend.

##### Émettre `OrNull` seulement là où le constructeur tolère visiblement null

Considérée parce qu'elle réutiliserait la lecture des gardes que D5 effectue déjà, n'appliquant la
nullabilité que là où le code l'accepte démontrablement.

Écartée parce que l'absence de garde null n'est pas une preuve d'intention — elle est tout aussi
compatible avec un oubli — et parce qu'elle ferait dépendre la stabilité d'un test de l'écriture ou
non d'une garde sans rapport. C'est pire qu'une règle uniforme, dans un sens comme dans l'autre.

#### Conséquences

**Positives.** Un generator scaffoldé produit la même forme de valeur à chaque exécution. Rien dans
le défaut émis ne peut rendre un test intermittent par la nullabilité.

**Négatives.** La branche null d'un constructeur, ou du code sous test, n'est jamais exercée par un
generator scaffoldé à moins que le développeur ne le demande. Un paramètre typé `string?` pour une
raison reçoit un generator qui n'explore jamais cette raison.

Visiblement négatif aussi : pour un nullable de type valeur l'émetteur doit convertir explicitement,
donc le §5.2 porte un saut qui se lit comme gratuit tant que cette décision n'est pas connue.

**Risques.** Ce saut est la partie de l'émetteur la plus susceptible d'être « simplifiée » en
défaut — `OrNull` est plus court, retourne exactement le type voulu, et ressemble au nettoyage
évident. Le réintroduire restaurerait l'instabilité en silence. Atténué par cet enregistrement et
par le cas de résolveur nommé ci-dessous.

#### Actions de suivi

* Conserver un cas de résolveur pour un paramètre nullable de type valeur assertant la conversion
  explicite, et nommer cet enregistrement là où l'émetteur l'effectue, pour que le saut ne soit pas
  simplifié.

#### Références

* §5.2, §14.4 de cette spécification ; D2 et D5 de cette section.

---

### D11 — Garder le moteur de scaffolding chargeable par un hôte Roslyn

**Statut :** Proposed
**Proposé :** 2026-07-30
**Décideurs :** Reefact

#### Contexte

La CLI doit ouvrir un projet sur disque, ce qui exige un workspace conscient de MSBuild ; celui-ci
n'est disponible que sur .NET moderne, pas sur la cible de bas niveau.

Un assembly chargé par le compilateur d'un consommateur — analyzer, code fix, code refactoring —
doit cibler le framework de bas niveau et être compilé contre la version minimale de Roslyn sous
laquelle il doit se charger. Construit contre une plus récente, il échoue à se charger, et il échoue
silencieusement.

Un code refactoring Roslyn est une seconde surface plausible pour le moteur : la bibliothèque publie
déjà des analyzers, donc le chemin de packaging et de chargement existe, et appliquer un document
est l'opération naturelle d'un refactoring.

Le travail du moteur est de l'inspection de symboles, de la lecture de syntaxe et de la construction
de chaînes. Il n'a besoin ni de système de fichiers, ni de console, ni de MSBuild.

La surface de tests décrite au §12 est dominée par le comportement du moteur plutôt que par la
plomberie de commandes.

Le dépôt hôte mesure la mutation sur tout projet dont le code est publié ou s'exécute (§13.5).

#### Décision

Le moteur de scaffolding est une bibliothèque séparée ciblant le framework de bas niveau et compilée
contre le plancher Roslyn de l'analyzer, ne faisant aucune entrée-sortie, la CLI étant une coquille
par-dessus.

#### Justification

La contrainte est asymétrique dans le temps. Cibler le plancher ne coûte presque rien au moteur
aujourd'hui, parce qu'aucune partie de son travail n'a besoin d'une API moderne. Découvrir plus tard
qu'il doit être chargeable par un compilateur signifie re-vérifier chaque API qu'il utilise contre ce
plancher, dans un code écrit sans cette contrainte à l'esprit. Payer maintenant est bon marché,
payer plus tard ne l'est pas, et c'est ce qui justifie de construire pour un consommateur qui
n'existe pas encore.

La frontière qu'exige le consommateur futur est celle-là même que veut le code présent. Un moteur
qui prend une compilation et retourne un modèle, sans sortie propre, est la forme testable : le
résolveur et l'émetteur s'exercent sur une compilation en mémoire, sans projet sur disque ni analyse
d'arguments dans le chemin.

Les séparer sépare aussi le budget de mutation. La plomberie de commandes et les règles de
résolution ne méritent pas la même attention, et un projet unique ne peut pas exprimer cette
différence.

L'argument selon lequel la CLI pourrait gagner d'autres verbes ne justifie rien de tout cela. Des
verbes en plus sont des fichiers en plus au-dessus du même moteur, et après D1 la liste plausible
est de toute façon quasi vide.

#### Alternatives considérées

##### Un projet CLI unique contenant tout

Considéré parce que c'est la plus petite chose qui fonctionne pour un outil à un seul verbe, et que
cela évite deux projets et deux suites de tests.

Écarté parce qu'il ferme la voie de l'hôte Roslyn à l'instant de sa création, et parce qu'il force
chaque test du moteur à passer par les dépendances de la CLI.

##### Un moteur séparé ciblant .NET moderne

Considéré parce qu'il garde la frontière, et avec elle les bénéfices de test et de mutation, sans
accepter la contrainte de bas niveau.

Écarté parce que la raison d'être principale de la frontière est le consommateur que cette variante
exclut.

#### Conséquences

**Positives.** Le moteur est chargeable tel quel par un hôte compilateur. Ses tests n'ont besoin
d'aucun projet sur disque. La mesure de mutation peut être visée là où elle paie.

**Négatives.** Deux projets et deux suites de tests pour un verbe. Le moteur est écrit contre le
framework de bas niveau, donc les API de confort modernes lui sont indisponibles.

**Risques.** L'épinglage au plancher Roslyn peut dériver si la référence de package du moteur est
laissée flottante, et l'échec de chargement qui en résulte est silencieux. Atténué par un épinglage
sur la même propriété de plancher que celle du package d'analyzers (§13.2).

#### Actions de suivi

* Si un code refactoring est un jour construit, le moteur devra être publié comme package propre
  (§16).

#### Références

* §10, §12, §13.2, §13.5, §16 de cette spécification.

---

### Un suivi côté bibliothèque, pas un enregistrement de décision

`Any.Fixed<T>(value)` — un `IAny<T>` retournant une constante — permettrait à l'émetteur
d'abandonner le helper imbriqué `FixedValue<TValue>` du §4.2. `Any.OneOf(value)` remplit presque le
rôle mais refuse `null` et consomme un tirage (§14.5). C'est un ajout à l'API publique de la
bibliothèque plutôt qu'une décision sur le tool, donc cela relève de la base de décision de la
bibliothèque et n'est **pas** requis pour la v1.0.

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
| `AnyOrder` est accepté par les seams de composition de la bibliothèque | D2, §15 | `Any.ListOf`, `Any.PairOf` et `.As` l'acceptent tous |
| `.WithX(IAny<T>)` maintient la composition contrainte ouverte | §4.2 | `.WithReference(Any.String().StartingWith("ORD-").As(...))` donne `ORD-x9vDEd2` |
| Une recette construite **hors** d'un scope rejoue dedans | §8.2, §14.5 | deux exécutions `Any.Reproducibly(20260730, …)` ont produit des valeurs identiques |
| La chaîne dérivée des gardes ne lève jamais | §5.3 | 500 tirages à travers `OrderReference.Create`, aucune `AnyGenerationException` |
| La chaîne **sans** lecture des gardes lève par intermittence | §5.3 | **594 / 10 000** tirages ont levé — environ 1 sur 16 |
| La covariance des collections ne demande aucun adaptateur | §5.2, §14.5 | `Any.ListOf(...)` affecté à `IAny<IReadOnlyList<string>>` |
| Un nullable de type valeur **exige** bien le saut `.As` | §5.2 | `IAny<int>` n'est pas un `IAny<int?>` ; `.As(value => (int?)value)` compile |
| Les bornes complémentaires se composent | §5.3 | `.GreaterThanOrEqualTo(0).LessThanOrEqualTo(100)` et `.NonEmpty().WithMaxLength(10)` tirent tous deux |
| Les bornes contradictoires sont rejetées deux fois | §5.3 | `ConflictingAnyConstraintException` à l'exécution, et `JD023` à la **compilation** |
| Un generator de motif n'admet aucune autre contrainte de chaîne | §5.3 | `Any.StringMatching(...).NonEmpty()` ne compile pas — `CS1061`, `AnyPattern` n'a que `DifferentFrom`/`Except` |
| `.Positive()` est incorrect pour une garde `p < 1` sur un decimal | §5.3 | 1 tirage sur 5 000 est passé sous 1 sans contrainte ; ~1 sur 5 dès qu'une autre borne resserre |
| La sortie scaffoldée ne lève aucun diagnostic JD | D3, §12 | 0 diagnostic sur les fichiers émis |
| Les analyzers étaient réellement chargés | D3 | un fichier de contrôle a levé `JD006` et `JD005` dans le même build |
| `<auto-generated/>` les éteint | D3, §15 | le même fichier de contrôle, ainsi marqué, en a levé **0** — l'erreur `JD005` comprise |

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
