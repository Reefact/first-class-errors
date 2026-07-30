# FirstClassErrors — Analyse de la couverture de code

🌍 **Langues :**  
🇬🇧 [English](./2026-07-30-coverage-analysis.md) | 🇫🇷 Français (ce fichier)

**Date :** 2026-07-30
**Révision analysée :** `da6e7ee` (tête de `main` au moment de l'analyse ; `main` a avancé depuis)
**Source :** projet SonarCloud `reefact_first-class-errors`, analyse du 2026-07-30 07:51 UTC
**Périmètre :** tous les composants que SonarCloud rapporte pour la solution — 614 composants,
20 668 ncloc, dont 182 fichiers portent un trou de couverture.
**Statut :** consultatif. Conformément à la convention du dépôt (ADR-0004), cette analyse produit des
recommandations, jamais des blocages ; les deux ADR candidates qu'elle nomme sont des propositions que
`@reefact` accepte ou rejette.

**Méthode.** Les mesures par fichier ont été tirées de `api/measures/component_tree` de SonarCloud (les
deux pages, 614 composants), et les compteurs de passages et de branchements par ligne de
`api/sources/lines` pour les 182 fichiers portant un trou. Les totaux reconstruits correspondent
**exactement** aux chiffres publiés par Sonar — 840 lignes et 1 303 conditions non couvertes — la
classification ci-dessous porte donc sur la population entière et non sur un échantillon. Chaque ligne
non couverte a été rattachée au membre qui la contient par analyse des sources locales ; chaque
branchement manquant a été classé d'après la forme de sa propre expression.

**Non vérifié localement.** `dotnet build` et `dotnet test` **n'ont pas** été exécutés pour cette
analyse. Tous les chiffres proviennent de l'analyse SonarCloud du 2026-07-30, elle-même produite par
`dotnet test … --settings coverage.runsettings` dans [`sonar.yml`](../workflows/sonar.fr.md).

> **Terminologie.** Dans tout ce document, un *branchement* (que SonarCloud appelle *condition*, et la
> littérature anglophone *branch*) est un chemin conditionnel dans le code — chaque issue possible d'un
> `if`, d'un `&&`, d'un `||`, d'un `??`, d'un `case` ou d'un ternaire. Une ligne
> `if (x is null) { throw new ArgumentNullException(nameof(x)); }` compte pour une ligne et deux
> branchements ; un test qui ne passe jamais `null` couvre la ligne sans couvrir le branchement. Rien
> ici ne concerne les branches Git.

---

## 1. Résumé

La solution est à **86,6 % de couverture globale** — 91,1 % en lignes, 80,2 % en branchements — avec
**2 143 unités non couvertes** : 840 lignes et 1 303 branchements. Ces 2 143 unités ne sont pas
2 143 tests manquants. Elles se répartissent en cinq catégories disjointes, et une seule d'entre elles
se règle en écrivant davantage de tests.

Trois constats commandent toutes les recommandations ci-dessous.

1. **Le déficit est un déficit de branchements, et il n'est pas là où le pourcentage le suggère.** La
   couverture en lignes est déjà à 91,1 % ; les branchements représentent 61 % des unités non
   couvertes. **JustDummies et les deux projets d'analyzers détiennent 1 084 des 2 143 unités**, dont
   859 branchements. Pendant ce temps, les deux projets aux pires pourcentages —
   `FirstClassErrors.Cli` à 53,9 % et `FirstClassErrors.GenDoc.Worker` à 0 % — sont majoritairement du
   code que la CI exerce réellement, simplement pas sous l'instrument de couverture. Prioriser par le
   pourcentage commencerait exactement au mauvais endroit.

2. **`JustDummies.Xunit` n'est pas dans le dénominateur du tout.** Un package NuGet publié est classé
   par Sonar comme du code de test et sa couverture ne compte jamais, dans aucun sens. Tout objectif
   « 100 % de la solution » l'exclut silencieusement aujourd'hui. Voir
   [§5](#5-un-angle-mort-de-mesure--justdummiesxunit).

3. **Le levier le moins cher est déjà écrit dans ce dépôt.** JustDummies possède un
   `NullArgumentGuardConventionTests` réflexif qui invoque chaque membre avec `null` et vérifie
   l'`ArgumentNullException` produite. Aucun autre projet n'a d'équivalent — d'où les 30 gardes null
   parmi les 49 trous de branchement de la bibliothèque cœur, dont 24 dans le seul
   `OutcomeTaskExtensions.cs`.

Le quality gate est **vert** et ne mesure que le code *neuf* (88,3 % pour une barre à 80 %). Rien
n'échoue. Chaque chiffre de ce document est donc un choix, pas une exigence.

## 2. Chiffres clés

| Métrique | Valeur |
|---|---|
| Couverture globale | **86,6 %** |
| Couverture en lignes | 91,1 % — 840 non couvertes sur 9 449 lignes à couvrir |
| Couverture en branchements | 80,2 % — 1 303 non couverts sur 6 572 conditions à couvrir |
| Total des unités manquantes | **2 143** (840 lignes + 1 303 branchements) |
| Quality gate | Vert — `new_coverage` 88,3 % pour un seuil à 80 % |
| Taille analysée | 20 668 ncloc sur 614 composants ; 182 fichiers portent un trou |

Évolution de la couverture sur la période, d'après l'historique des métriques SonarCloud :

| Date | 09/07 | 12/07 | 19/07 | 26/07 | 27/07 | 28/07 | 29/07 | 30/07 |
|---|---|---|---|---|---|---|---|---|
| Couverture | 78,1 % | 79,3 % | 79,3 % | 82,8 % | 84,2 % | 86,1 % | 86,6 % | 86,6 % |

## 3. Où se situe le déficit

Par projet, par taille de déficit décroissante. `LTC` = lignes à couvrir, `CTC` = conditions à couvrir.

| Projet | Couverture | LTC | CTC | Lignes n.c. | Branch. n.c. | Unités |
|---|---:|---:|---:|---:|---:|---:|
| `JustDummies` | 90,4 % | 3 229 | 2 745 | 167 | 404 | **571** |
| `JustDummies.Analyzers` | 88,0 % | 1 451 | 1 671 | 50 | 324 | **374** |
| `FirstClassErrors.Cli` | 53,9 % | 502 | 248 | 227 | 119 | **346** |
| `FirstClassErrors.GenDoc` | 84,2 % | 1 414 | 542 | 161 | 148 | **309** |
| `FirstClassErrors.Usage` | 67,1 % | 351 | 72 | 81 | 58 | 139 |
| `FirstClassErrors.Analyzers` | 89,7 % | 858 | 494 | 8 | 131 | 139 |
| `FirstClassErrors.RequestBinder.Usage` | 75,1 % | 352 | 98 | 55 | 57 | 112 |
| `FirstClassErrors` | 94,7 % | 701 | 482 | 14 | 49 | 63 |
| `FirstClassErrors.RequestBinder` | 93,2 % | 483 | 176 | 33 | 12 | 45 |
| `FirstClassErrors.GenDoc.Worker` | 0,0 % | 43 | 0 | 43 | 0 | 43 |
| `FirstClassErrors.Testing` | 98,2 % | 65 | 44 | 1 | 1 | 2 |

`FirstClassErrors.RequestBinder.Benchmarks` est absent parce qu'il est déjà exclu de la couverture par
`sonar.coverage.exclusions` dans [`sonar.yml`](../workflows/sonar.fr.md) — un banc de mesure jamais
publié et jamais testé unitairement. Son code passe malgré tout sous SonarAnalyzer.

Les douze fichiers portant les plus gros déficits :

| # | Fichier | Lignes n.c. | Branch. n.c. | Unités | Couverture |
|---:|---|---:|---:|---:|---:|
| 1 | `FirstClassErrors.GenDoc/SolutionErrorDocumentationGenerator.cs` | 143 | 83 | 226 | 50,0 % |
| 2 | `JustDummies/Any.Combine.cs` | 0 | 75 | 75 | 76,6 % |
| 3 | `JustDummies/WideIntervalSpec.cs` | 16 | 46 | 62 | 80,9 % |
| 4 | `JustDummies/DecimalIntervalSpec.cs` | 19 | 42 | 61 | 83,1 % |
| 5 | `JustDummies/RegexParser.cs` | 17 | 41 | 58 | 90,8 % |
| 6 | `FirstClassErrors.Usage/Model/Temperature.cs` | 35 | 22 | 57 | 0,0 % |
| 7 | `JustDummies.Analyzers/ScalarConstraintState.cs` | 1 | 56 | 57 | 74,3 % |
| 8 | `FirstClassErrors.Cli/RendererLoader.cs` | 27 | 21 | 48 | 7,7 % |
| 9 | `FirstClassErrors.GenDoc.Worker/Program.cs` | 43 | 0 | 43 | 0,0 % |
| 10 | `FirstClassErrors.Usage/Utils/DocumentationFormatter.cs` | 21 | 19 | 40 | 46,7 % |
| 11 | `JustDummies.Analyzers/RejectedConstantArgumentAnalyzer.cs` | 14 | 26 | 40 | 86,5 % |
| 12 | `FirstClassErrors.Cli/CatalogSnapshotSource.cs` | 26 | 12 | 38 | 0,0 % |

## 4. Les cinq natures de déficit

Chacune des 2 143 unités a été classée d'après sa propre ligne de source. Les catégories sont
disjointes et leur somme fait le total.

| Nature | Lignes n.c. | Branch. n.c. | Unités | Part |
|---|---:|---:|---:|---:|
| **V1** — exercé par la CI, invisible à l'instrument | 186 | 83 | 269 | 12,6 % |
| **V2** — code d'exemple et de démonstration | 136 | 115 | 251 | 11,7 % |
| **V3** — un test le ferme aujourd'hui | 340 | 993 | **1 333** | **62,2 %** |
| **V4** — exige une couture avant qu'un test puisse l'atteindre | 176 | 88 | 264 | 12,3 % |
| **V5** — pratiquement inatteignable | 2 | 24 | 26 | 1,2 % |
| **Total** | 840 | 1 303 | 2 143 | 100 % |

### V1 — exercé par la CI, invisible à l'instrument (269 unités)

Le lancement de processus MSBuild dans `SolutionErrorDocumentationGenerator` (226 unités) et le point
d'entrée de `GenDoc.Worker` (43 unités). Les zones non couvertes sont précisément les chemins qui
lancent des processus : `DotNetBuild`, `DotNetGetProperty`, les branchements de timeout et de kill de
`RunProcess`, et l'invocation du sous-processus dans `RunWorker`.

Ce code n'est pas non testé. [`canary.yml`](../../../../.github/workflows/canary.yml) lance le vrai
`fce.dll generate` sur un vrai projet, capture les diagnostics du worker et *vérifie* deux choses : que
le catalogue émis contient des codes d'erreur, et que la bannière du worker annonce bien le runtime le
plus récent (`Documenting … on .NET <n>.`).
[`gendoc-docs.yml`](../../../../.github/workflows/gendoc-docs.yml) lance le même binaire pour
régénérer le catalogue commité. C'est une *meilleure* vérification du comportement MSBuild et du
roll-forward que n'importe quel mock. Ces chemins sont non couverts parce que `dotnet test` ne les
lance jamais — une propriété de l'instrument, pas du banc de test.

Une réserve : le canary ne s'exécute que lorsqu'une préversion de .NET est disponible, et passe son
tour sinon ; ce n'est donc pas une garantie à chaque commit. `gendoc-docs` n'a pas cette condition.

### V2 — code d'exemple et de démonstration (251 unités)

`FirstClassErrors.Usage` et `FirstClassErrors.RequestBinder.Usage`. L'intention est déjà consignée dans
le code : `Usage/Model/Amount.cs` porte un `SuppressMessage` justifiant que les opérateurs de
comparaison sont hors périmètre parce qu'ils « ajouteraient de la surface non testée à un type que les
tests n'exercent qu'indirectement ». Le périmètre de couverture n'a simplement jamais été aligné sur
cette intention affichée.

### V3 — un test le ferme aujourd'hui (1 333 unités)

Aucun refactoring, aucune couture, aucune décision de politique — seulement des tests qui n'existent
pas encore. 993 de ces unités sont des branchements. C'est la seule catégorie où écrire des tests est
la réponse, et elle est détaillée en [§6](#6-de-quoi-sont-faites-les-1-333-unités-actionnables).

### V4 — exige une couture avant qu'un test puisse l'atteindre (264 unités)

La branche de commandes `renderer` et `config` de la CLI écrit directement dans `Console.Out` et
appelle `Assembly.LoadFrom`, tandis que les commandes `generate` et `catalog` passent par `IOutputSink`,
`IErrorDocumentationGenerator` et `ICatalogSnapshotSource` et se situent entre 80 % et 97 %.

Les commandes non testées sont exactement celles qui n'ont jamais adopté la couture que le projet
possède déjà. La liste : `RendererLoader`, `RendererListCommand`, `RendererAddCommand`,
`RendererRemoveCommand`, `ConfigShowCommand`, `ConsoleGenerationLogger`, `CatalogSnapshotSource`,
`CatalogSourceResolver`, `RendererCatalog`, `SolutionErrorDocumentationGeneratorAdapter` — ce dernier
étant le côté production de la couture même que les tests pilotent avec des doublures — plus
`Cli/Program.cs`, qui est du câblage Spectre.

### V5 — pratiquement inatteignable (26 unités)

Gardes défensives contre des états que le compilateur ne peut pas produire : vérifications Roslyn
`is not <forme>` sur des types d'opération et de symbole, et bras `default:` de `switch` exhaustifs.
**Ce chiffre est un plancher, pas un total** — c'est seulement ce qui était démontrable par la syntaxe
seule ; le nombre réel de branchements défensifs inatteignables est plus élevé. Les poursuivre coûte de
la correction, car la seule façon de « couvrir » une telle garde est de supprimer une garde qui est là
volontairement.

## 5. Un angle mort de mesure : `JustDummies.Xunit`

`JustDummies.Xunit/ReproducibleAttribute.cs` — 60 lignes de code d'un **package NuGet publié** — est
classé par SonarCloud sous le qualifieur `UTS` (source de test unitaire), et non comme du code
principal. Le SonarScanner pour .NET considère un projet comme un projet de test dès qu'il référence un
framework de test, et ce package référence `xunit.v3.extensibility.core` parce que c'est exactement ce
qu'il est : l'adaptateur xUnit.

La conséquence n'est *pas* qu'il est non testé — `JustDummies.Xunit.UnitTests` existe et l'exerce, y
compris via une couture `InternalsVisibleTo` ajoutée délibérément pour que la règle « ne rapporter
qu'en cas d'échec » puisse être prouvée sans qu'un test doive échouer pour de vrai. La conséquence est
que sa couverture **ne compte jamais**, dans aucun sens : une régression qui la ferait tomber à zéro
déplacerait le chiffre publié de 0,0 point, et le travail déjà fait pour le tester ne rapporte rien.

Tout objectif « 100 % de la solution » laisse ce package silencieusement dehors. Le corriger suppose de
forcer la classification — `sonar.test.exclusions`, ou un `SonarQubeTestProject=false` explicite sur ce
seul projet.

## 6. De quoi sont faites les 1 333 unités actionnables

Voici la catégorie **V3** ouverte — les unités qu'un test peut fermer aujourd'hui.

| Motif | Lignes n.c. | Branch. n.c. | Unités | Forme du correctif |
|---|---:|---:|---:|---|
| Chaînes de gardes et dispatch des analyzers | 56 | 431 | **487** | Extraits de code négatifs via l'`AnalyzerTestHarness` existant |
| Moteurs de spec JustDummies (intervalle, chaîne, regex) | 73 | 205 | **278** | Cas limites et d'épuisement ; `DescribeExhaustion` et `Cardinality` ne sont jamais atteints |
| Reste de la surface `Any<T>` | 34 | 68 | 102 | Constructeurs de contrainte morts sur certains types scalaires — `MultipleOf` sur `AnySByte`, `LessThan` sur `AnyUInt16`, … |
| Renderers et versioning GenDoc | 18 | 65 | 83 | Cas limites des renderers et libellés de diff de catalogue ; la couture existe et est déjà testée |
| CLI, la partie déjà cousue | 51 | 31 | 82 | Davantage de cas via les doublures qu'utilisent déjà `GenerateCommand` et les commandes de catalogue |
| Interfaces d'introspection `Any<T>` jamais appelées | 55 | 9 | 64 | Une théorie réflexive sur chaque `Any<T>` — 26 fichiers fermés d'un coup |
| Gardes de domaine et d'intervalle jamais violées | 4 | 57 | 61 | Un test de convention qui passe à chaque garde sa valeur illégale |
| Gardes null jamais nourries d'un `null` | 4 | 52 | 56 | Porter le `NullArgumentGuardConventionTests` de JustDummies aux autres projets |
| Chaînes `??` d'`Any.Combine` (matrice des positions d'opérande) | 1 | 51 | 52 | Une théorie faisant varier l'opérande porteur du `RandomSource` |
| Bibliothèque cœur `FirstClassErrors` | 14 | 19 | 33 | Chemins d'échec de chargement et de nom null dans `AssemblyErrorDocumentationReader` |
| `FirstClassErrors.RequestBinder` | 29 | 4 | 33 | `BindingScope.Get` et le chemin du convertisseur de propriétés simples |
| Autre (`FirstClassErrors.Testing`) | 1 | 1 | 2 | — |
| **Total** | **340** | **993** | **1 333** | |

### Les quatre motifs répliqués

Plusieurs entrées ci-dessus sont un même motif répété sur de nombreux fichiers, ce qui les rend
intéressantes à attaquer : un seul harnais ferme des dizaines d'unités d'un coup.

**La matrice d'introspection `Any<T>` — 64 unités sur 26 fichiers.** `IHasRandomSource.Source`,
`ICardinalityHint<T>.DistinctCardinality` et `ICardinalityHint<T>.Contains` sont des implémentations
explicites d'interface, et pour la plupart des types scalaires rien dans la suite ne passe jamais par
elles. Une seule théorie pilotée par réflexion sur chaque `Any<T>` ferme les 26 fichiers d'un coup — et
le dépôt possède déjà cette forme de harnais dans `SurfaceParityTests`, `FactoryNamingConventionTests`
et `NullArgumentGuardConventionTests`.

**Des gardes qui existent mais ne sont jamais violées — 117 unités.** Réparties par type d'exception et
par projet :

| Projet | `ArgumentNullException` | `ArgumentOutOfRangeException` | `ArgumentException` | Total |
|---|---:|---:|---:|---:|
| `JustDummies` | 14 | 10 | 47 | 71 |
| `FirstClassErrors` | 30 | 0 | 0 | 30 |
| `FirstClassErrors.RequestBinder` | 8 | 0 | 0 | 8 |
| `FirstClassErrors.Usage` | 0 | 0 | 2 | 2 |
| **Total** | **52** | **10** | **49** | **111** |

Le `NullArgumentGuardConventionTests` de JustDummies invoque par réflexion chaque membre avec `null` et
vérifie l'`ArgumentNullException` — c'est pourquoi sa colonne null est la plus faible alors qu'il s'agit
du plus gros projet. **`FirstClassErrors` n'a pas d'équivalent**, et ses 30 gardes null non couvertes en
sont la conséquence directe ; 24 d'entre elles sont dans `OutcomeTaskExtensions.cs`, une par garde
`is null` sur `next`, `fallback`, `onSuccess` et `onFailure`. Porter ce seul test de convention est le
mouvement le moins cher de ce document. Le même manque existe pour les **gardes de domaine et
d'intervalle**, qu'aucun test de convention ne couvre dans aucun projet.

**La matrice des positions d'opérande d'`Any.Combine` — 52 unités dans un seul fichier.** Chaque
surcharge d'arité enchaîne `SourceOf(first) ?? SourceOf(second) ?? …` : une surcharge à *N* opérandes
émet donc 2*N* branchements, et les tests ne placent jamais la source ailleurs qu'en première position.
`Any.Combine.cs` a 0 *ligne* non couverte et 75 *branchements* non couverts — chaque ligne s'exécute, la
moitié des chemins jamais. Une théorie faisant varier l'opérande porteur de la source parcourt toute la
chaîne.

**Les chaînes de gardes des analyzers — 487 unités, la plus grosse catégorie.** Par forme d'expression,
sur les deux projets d'analyzers (455 unités de branchement classées) :

| Forme | Unités | Part |
|---|---:|---:|
| garde null / null-conditionnelle sur un symbole Roslyn | 150 | 33,0 % |
| `if` simple couvert d'un seul côté | 109 | 24,0 % |
| autre | 71 | 15,6 % |
| dispatch `switch` / `case` | 48 | 10,5 % |
| court-circuit `&&` / `\|\|` | 35 | 7,7 % |
| garde `is not <forme Roslyn>` (catégorie V5) | 24 | 5,3 % |
| boucle sans chemin à zéro itération | 12 | 2,6 % |
| coalescence `??` | 6 | 1,3 % |

La plupart sont de véritables chemins d'analyzer — syntaxe malformée ou inhabituelle à laquelle
l'analyzer doit survivre — atteignables via l'`AnalyzerTestHarness` existant avec des extraits de code
négatifs. À noter, le contraste avec les tests de mutation sur lesquels ce dépôt s'appuie déjà
(ADR-0043, ADR-0046) : beaucoup de ces branchements sont *exécutés* mais jamais *vérifiés*, ils sont
donc probablement aussi des mutants survivants.

## 7. Ce que chaque décision rapporte

Deux leviers distincts déplacent le chiffre et ne doivent pas être confondus. Les **exclusions**
changent le dénominateur et ne coûtent qu'une décision documentée. Les **tests** changent le numérateur
et coûtent du travail. Les chiffres sont cumulatifs, calculés avec la formule de Sonar
`((LTC − ln.c.) + (CTC − br.n.c.)) / (LTC + CTC)` sur les mesures par fichier.

| Étape | Levier | Unités | Couverture |
|---|---|---:|---:|
| Aujourd'hui | — | — | 86,62 % |
| Exclure les deux projets d'exemple `Usage` | dénominateur | −251 | 87,51 % |
| … et `GenDoc.Worker`, le point d'entrée du worker | dénominateur | −43 | 87,76 % |
| … et `Cli/Program.cs`, le câblage Spectre | dénominateur | −17 | 87,86 % |
| … et `SolutionErrorDocumentationGenerator.cs`, le lancement MSBuild | dénominateur | −226 | 89,03 % |
| … puis couvrir les dix fichiers CLI non cousus | numérateur (après couture) | −247 | **90,71 %** |

Après tout cela, **1 359 unités subsistent et 90,7 % est le plafond des mouvements peu coûteux** — dont
1 084 dans JustDummies et les analyzers, très majoritairement des branchements. Il n'y a pas de
raccourci pour éviter cette catégorie : c'est le vrai travail, et c'est aussi le code où la correction
compte le plus.

## 8. Recommandation

1. **Corriger d'abord l'angle mort — c'est un défaut de mesure, pas un trou de couverture.** Forcer
   l'analyse de `JustDummies.Xunit` comme code principal. Tant que ce n'est pas fait, aucun objectif de
   couverture ne couvre réellement la solution, et on ne peut pas se fier au chiffre pour bouger quand
   ce package régresse.

2. **Trancher le périmètre explicitement, une fois, dans une ADR.** Les exemples, les points d'entrée
   de processus et le lancement MSBuild représentent 520 unités — un quart du total — pour lesquelles
   aucun test unitaire ne devrait jamais être écrit. `Benchmarks` est déjà exclu pour exactement cette
   raison et le raisonnement est déjà écrit dans `sonar.yml` ; il s'agit d'étendre la même règle au même
   type de code. C'est une décision durable qu'un futur mainteneur remettrait en question : elle appelle
   une ADR plutôt qu'un commentaire.

3. **Faire compter l'exercice de la CI, au lieu d'écrire des tests unitaires pour l'imiter.** `canary`
   et `gendoc-docs` lancent déjà le vrai `fce generate`, démarrent le vrai worker et vérifient le
   résultat. Soit on collecte la couverture de ces exécutions, soit on exclut le chemin en disant
   pourquoi — mais on n'écrit pas un faux `IProcessRunner` pour faire bouger un chiffre. En cas
   d'exclusion, noter que le canary est conditionné à une préversion : c'est `gendoc-docs` qui s'exécute
   réellement à chaque poussée concernée.

4. **Porter le test de convention des gardes null hors de JustDummies.** Un seul harnais, déjà écrit et
   éprouvé dans ce dépôt, appliqué à `FirstClassErrors`, `RequestBinder` et `GenDoc`. Ferme 56 unités,
   supprime une catégorie entière définitivement, et chaque garde future est couverte le jour où elle
   est écrite. Étendre ensuite le même harnais aux gardes d'intervalle (+61).

5. **Ensuite, les deux matrices réflexives de JustDummies.** Les interfaces d'introspection `Any<T>`
   (64) et les positions d'opérande d'`Any.Combine` (52). Ce sont deux théories uniques sur une liste de
   types existante. D'après
   [l'ADR-0040](../adr/0040-split-the-justdummies-test-bed-between-example-and-property-suites.fr.md),
   ce sont des invariants qui tiennent pour tout argument légal : ils relèvent donc de
   `JustDummies.PropertyTests`, pas de la suite unitaire — voir
   [Écrire des tests JustDummies](../WritingJustDummiesTests.fr.md).

6. **Seulement après, les analyzers — et les piloter par la mutation, pas par la couverture.** 487
   unités, majoritairement des branchements déjà *exécutés* mais non *vérifiés*. La couverture les
   déclarera fermés dès qu'un extrait les atteindra ; seule la campagne de mutation dira si le test a
   réellement épinglé le comportement. Le dépôt lance déjà cette campagne — qu'elle choisisse les cibles
   ici, plutôt que le pourcentage de couverture.

### ADR candidates

Deux décisions ci-dessus sont durables et qu'un futur mainteneur remettrait en question ; elles sont
proposées comme brouillons :

- **La politique de périmètre de couverture** — quelles catégories de code sont délibérément hors du
  dénominateur (exemples, points d'entrée de processus, lancements de processus) et pourquoi.
  Recommandation 2.
- **Où les chemins de niveau processus sont vérifiés** — acter que les exercices `canary` et
  `gendoc-docs` constituent la vérification retenue pour les chemins MSBuild et worker, plutôt que des
  tests unitaires sur mocks. Recommandation 3.

Aucune n'est rédigée ici. Conformément à la convention du dépôt, un agent propose et n'accepte jamais.

## 9. Ce que cette analyse ne prétend pas

**Que 100 % soit le bon objectif.** Le quality gate porte sur le code neuf et il est vert à 88,3 %.
Rien n'échoue ici. Le plafond atteignable après tous les mouvements raisonnables est de l'ordre de
96–97 %, parce que la catégorie V5 est réelle et que ses 26 unités ne sont que celles démontrables par
la syntaxe.

**Que la couverture mesure la qualité des tests.** Ce dépôt le sait déjà : il s'appuie sur le score de
mutation précisément parce qu'un test peut exécuter une ligne sans rien vérifier à son sujet (ADR-0043,
ADR-0046). Plusieurs catégories ci-dessus passeraient au vert sous la couverture tout en restant rouges
sous Stryker. Quand les deux divergent, c'est la campagne de mutation qui dit vrai.

**Que les chiffres soient à jour.** Ils décrivent `da6e7ee`. `main` a avancé depuis, y compris des
refactorings à l'intérieur de `JustDummies` : les chiffres par fichier auront donc bougé. C'est la
structure de l'analyse — les cinq natures, les motifs répliqués, les deux leviers — qui est censée
survivre à l'instantané.

## 10. Reproduire ces chiffres

Toutes les données sont publiques ; le projet SonarCloud est lisible sans jeton.

```sh
# Chiffres clés
curl -s "https://sonarcloud.io/api/measures/component?component=reefact_first-class-errors\
&metricKeys=coverage,line_coverage,branch_coverage,uncovered_lines,uncovered_conditions,\
lines_to_cover,conditions_to_cover,ncloc"

# Mesures par fichier (paginer : ps=500, p=1 puis p=2)
curl -s "https://sonarcloud.io/api/measures/component_tree?component=reefact_first-class-errors\
&metricKeys=coverage,uncovered_lines,uncovered_conditions,lines_to_cover,conditions_to_cover\
&strategy=leaves&ps=500&p=1&s=metric&metricSort=uncovered_lines&asc=false"

# Passages et branchements ligne à ligne pour un fichier
curl -s "https://sonarcloud.io/api/sources/lines?key=reefact_first-class-errors%3A<chemin-url-encodé>"
```

Une ligne est non couverte quand `lineHits == 0` ; une ligne a des branchements manquants quand
`coveredConditions < conditions`. La somme de `uncovered_lines` et de `(conditions − coveredConditions)`
sur tous les fichiers doit reproduire les totaux du projet — c'est cette réconciliation qui rend la
classification exhaustive plutôt qu'indicative.

## Voir aussi

- [Workflow `sonar`](../workflows/sonar.fr.md) — comment l'analyse et son rapport de couverture sont
  produits.
- [Workflow `sonar-gate`](../workflows/sonar-gate.fr.md) — comment le quality gate est relu.
- [Workflow `ci`](../workflows/ci.fr.md) — produit le même format OpenCover via `coverage.runsettings`.
- [`mutation`](../workflows/mutation.fr.md) et
  [`justdummies-mutation`](../workflows/justdummies-mutation.fr.md) — les contrôles qui mesurent si une
  ligne couverte est réellement vérifiée.
- [Écrire des tests JustDummies](../WritingJustDummiesTests.fr.md) — à quelle suite appartient un
  nouveau test JustDummies.
