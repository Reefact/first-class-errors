# ADR-0045 | Garder contre le null les arguments publics et internes, imposé par une convention par réflexion

🌍 🇫🇷 Français (ce fichier) · 🇬🇧 [English](0045-guard-public-and-internal-arguments-against-null.md)

**Statut :** Remplacé par [ADR-0064](0064-exempt-the-whole-failure-reporting-path-from-the-null-guard-convention.fr.md)
**Proposé :** 2026-07-27
**Accepté :** 2026-07-27
**Décideurs :** Reefact

## Contexte

* `JustDummies` fait des invariants la raison d'être de la bibliothèque : un arrangement erroné doit *échouer*, au
  plus près de sa cause. Ses objets-valeurs et ses résultats sont des classes validantes (la règle « class, jamais
  struct » du dépôt), dont toute la garantie est qu'aucune instance n'existe sans avoir franchi un point d'entrée
  validant.
* Les types référence nullables ne sont qu'une annotation **à la compilation**. Un appelant dont l'analyse nullable
  est désactivée, un `null!`, la réflexion ou un `default` peuvent toujours faire passer un `null` par un paramètre
  typé non-nullable à l'exécution. La bibliothèque raisonne déjà à partir de ce fait — c'est la raison affichée pour
  laquelle les objets-valeurs sont des classes, pas des structs.
* Avant ce changement, beaucoup de membres ne validaient pas leurs arguments référence. Le manque était le plus large
  à la **frontière interne** : les fabriques `Create(RandomSource)` et les constructeurs internes, là où les
  dépendances d'une classe (la source aléatoire, les specs d'intervalle/de chaîne/d'URI) entrent en elle pour la
  première fois. L'API publique ne peut jamais y router un `null` ; un `null` qui les atteindrait ne pourrait venir que
  d'une erreur de câblage interne — et surgirait plus tard en `NullReferenceException`, loin de sa cause.
* La suite de contrats était strictement **boîte noire** : aucun `InternalsVisibleTo` n'existait, si bien que chaque
  test n'exerçait que la surface publique. L'audit d'architecture du 2026-07-20 (§9.3) l'a consigné comme un choix
  délibéré — il prouve que l'API publique suffit à spécifier la bibliothèque, et rend les refactors du moteur
  transparents aux tests.
* Construire une exception se produit sur le chemin de gestion d'erreur et de journalisation. `System.Exception`
  tolère un message et une exception interne `null`.
* La bibliothèque a pour plancher **.NET Standard 2.0** (donc `ArgumentNullException.ThrowIfNull`, une API .NET 6+,
  est indisponible), et les suites de contrats tournent en plus sur le plancher de support .NET Framework 4.7.2. Les
  métadonnées de nullabilité par réflexion (`NullabilityInfoContext`) sont une API .NET 6+.

## Décision

Tout membre `public` ou `internal` de `JustDummies` — constructeur ou méthode — rejette un argument de type référence
non-nullable `null` par une `ArgumentNullException` nommant le paramètre, à l'exception des constructeurs de types
d'exception ; un test-convention piloté par réflexion l'impose sur toute la surface, et les internes de la bibliothèque
sont ouverts à la suite de contrats pour qu'il le puisse.

## Rationale

* **La classe, pas l'assembly, est la frontière de confiance.** Un membre ne peut pas supposer ses appelants
  corrects, et « appelant » inclut une autre classe du même assembly. Valider ce qui franchit la frontière — et là
  seulement, en faisant confiance à ce qu'un membre validant a déjà accepté — est ce qui empêche un `null` de voyager
  loin de l'erreur qui l'a produit, sans re-vérification redondante à l'intérieur de la classe.
* **Les annotations nullables ne sont pas une application.** Comme elles disparaissent à l'exécution, le seul mécanisme
  qui rejette réellement un `null` est une garde à l'exécution. C'est le raisonnement que le dépôt accepte déjà pour
  faire des objets-valeurs des classes plutôt que des structs ; l'appliquer à la validation d'arguments est cohérent,
  pas nouveau.
* **La frontière interne est la garde la plus utile et la plus difficile à tester.** C'est là que les dépendances
  entrent dans une classe, donc là qu'une erreur de câblage interne est attrapée — pourtant l'API publique ne peut
  jamais y conduire un `null`, si bien qu'une convention limitée au public laisserait justement cette garde non
  vérifiée. La vérifier est ce qui rend l'ouverture des internes rentable.
* **Seule la réflexion rend la convention auto-entretenue.** La convention doit valoir pour chaque membre existant et
  chaque membre ajouté ensuite ; un test qui découvre les membres par réflexion y soumet automatiquement un nouveau
  générateur, une nouvelle fabrique ou une nouvelle méthode fluide, sans rien à ajouter. Des tests écrits à la main,
  un par paramètre, oublient précisément le nouveau membre que la convention existe pour attraper.
* **Relâcher la boîte noire est le prix de la vérification de la frontière interne, et il est borné.** Puisqu'un
  `null` ne peut pas atteindre les membres internes via l'API publique, vérifier leurs gardes exige un accès interne.
  Les suites comportementales gardent leur posture boîte noire et ses bénéfices ; l'unique test qui a besoin des
  internes ne nomme aucun membre — il est générique par réflexion — donc il reste transparent aux refactors. Ce à quoi
  on renonce, c'est seulement la propriété que *tous* les tests ne touchent que la surface publique.
* **Les exceptions sont exemptées car une garde y irait contre son propre but.** Leurs constructeurs s'exécutent
  pendant qu'une erreur est gérée ou journalisée ; lever une `ArgumentNullException` sur un message `null` masquerait
  l'échec d'origine, et le type de base le tolère déjà.

## Alternatives considérées

### Garder la posture boîte noire : imposer la convention sur la seule surface publique

Considérée parce qu'elle préserve la posture délibérée que l'audit consigne, sans ouvrir aucun interne. Rejetée parce
qu'elle laisse non vérifiées les gardes de la frontière interne — les fabriques `Create` et les constructeurs internes
où l'API publique ne peut jamais router un `null` —, c'est-à-dire la couverture dont la convention a le plus besoin ; et
elle ne correspond pas à la portée de la décision elle-même, qui est *public ou internal*.

### Exercer les internes par réflexion sans `InternalsVisibleTo`

Considérée parce qu'elle garde vrai le fait littéral « aucun `InternalsVisibleTo` ». Rejetée parce qu'elle exerce
malgré tout les internes — donc relâche exactement la même posture — tout en forçant le test à atteindre, par la seule
réflexion, des types qu'il n'a pas le droit de nommer ; si l'on relâche la posture, le faire explicitement est plus
clair et pas davantage un écart.

### Tests écrits à la main, un par paramètre

Considérée comme l'option la plus compatible avec la boîte noire. Rejetée parce qu'elle ne s'auto-entretient pas :
chaque nouveau membre exige un nouveau test, et une garde oubliée sur un nouveau membre — le défaut même que la
convention existe pour empêcher — est justement ce qu'une suite écrite à la main oublie aussi.

### S'appuyer sur les annotations de référence nullable, ou un analyseur, plutôt que sur des gardes à l'exécution

Considérée parce que les annotations documentent l'intention à la compilation et qu'un analyseur pourrait signaler les
gardes manquantes. Rejetée parce que ni l'un ni l'autre ne rejette un `null` à l'exécution, qui est la garantie
recherchée ; les consommateurs en aval peuvent compiler avec l'analyse nullable désactivée, et la propre règle
« class, jamais struct » de la bibliothèque repose déjà sur le fait que l'application à l'exécution est la seule
réelle.

## Conséquences

### Positives

* Un argument `null` échoue vite à la frontière, en `ArgumentNullException` nommant le paramètre, au lieu de surgir
  plus tard en `NullReferenceException` loin de la cause.
* La convention s'auto-impose : un nouveau membre `public`/`internal` y est soumis automatiquement, sans test à écrire.
* La frontière interne — jusqu'ici hors d'atteinte de tout test — est désormais vérifiée.

### Négatives

* La posture de test boîte noire délibérée est relâchée : les internes de la bibliothèque sont visibles pour la suite
  de contrats.
* Un petit volume permanent de code de garde est réparti sur la surface publique et interne.
* Le test-convention utilise des métadonnées de nullabilité par réflexion .NET 6+, donc il ne tourne que sur la patte
  moderne et est exclu du build du plancher net472 ; les gardes qu'il impose sont, elles, en netstandard2.0.

### Risques

* Le test-convention ne peut vérifier qu'un membre pour lequel il sait construire des arguments valides. Atténuation :
  un membre qu'il ne peut pas exercer est signalé comme *non couvert* et fait échouer le test (échec bruyant), jamais
  ignoré en silence — un trou de couverture apparaît en test rouge, à combler par un échantillon ou un test explicite.
* Des internes ouverts pourraient tenter de futurs tests vers un couplage boîte blanche. Atténuation : le
  test-convention ne nomme aucun membre, et les suites comportementales restent boîte noire.

## Actions de suivi

* Aucune nécessaire pour que la convention tienne : les membres futurs sont couverts automatiquement. Garder le
  test-convention au vert.
* L'observation §9.3 de l'audit du 2026-07-20 selon laquelle « aucun `InternalsVisibleTo` n'existe » cesse
  délibérément d'être vraie à partir de cette décision.

## Références

* [ADR-0011](0011-host-dummies-as-a-standalone-package.md) — JustDummies est un paquet autonome et agnostique aux erreurs.
* [ADR-0026](0026-rebase-testing-arbitrary-values-on-dummies.md) — le paquet Testing rebase ses valeurs arbitraires sur JustDummies.
* Audit d'architecture et de conception JustDummies du 2026-07-20, §9.3 (stratégie de test — la posture boîte noire).
* `CLAUDE.md` — la règle « class, jamais struct » des objets-valeurs (application des invariants à l'exécution).
