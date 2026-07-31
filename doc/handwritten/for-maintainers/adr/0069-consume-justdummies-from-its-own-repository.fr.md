# ADR-0069 | Consommer JustDummies depuis son propre dépôt

🌍 🇫🇷 Français (ce fichier) · 🇬🇧 [English](0069-consume-justdummies-from-its-own-repository.md)

**Statut :** Proposé
**Proposé :** 2026-07-31
**Décideurs :** Reefact
**Supersède :** [ADR-0011](0011-host-dummies-as-a-standalone-package.fr.md) (son volet colocation), ainsi que
le contournement d'embarquement accepté par [ADR-0026](0026-rebase-testing-arbitrary-values-on-dummies.fr.md)

## Contexte

L'[ADR-0011](0011-host-dummies-as-a-standalone-package.fr.md) décidait que JustDummies est un package
indépendant qui ne doit jamais référencer un projet FirstClassErrors, **et** qu'il vit dans ce dépôt, pour
réutiliser son infrastructure CI, packaging, release, SBOM, SourceLink et gouvernance. Il actait que la règle
de non-référence existe pour qu'« une extraction ultérieure reste mécanique plutôt qu'architecturale », et il
écartait un dépôt séparé immédiat pour des raisons de coût, non de principe.

Cette extraction a eu lieu. Le produit — la bibliothèque, ses 28 analyseurs, son adaptateur xUnit v3, ses deux
suites de tests, sa documentation, ses ADR et son scaffolder `dum` spécifié — a été filtré hors de
l'historique de ce dépôt vers **`Reefact/just-dummies`** au
`fbf523b86acebdd34ba0bbfd437683864be3cb9c`, en préservant auteurs, dates, messages et le renommage de
`Dummies` en `JustDummies`. Rien n'a été supprimé ici.

Ce dépôt dépend encore de JustDummies en quatre points, et l'un d'eux le **livre** :

| Projet | Nature de la dépendance |
| --- | --- |
| `FirstClassErrors.Testing` | `ProjectReference` privé **plus** une cible de pack qui embarque `JustDummies.dll` dans son propre `lib/` |
| `FirstClassErrors.UnitTests` | `ProjectReference`, tests uniquement |
| `FirstClassErrors.RequestBinder.UnitTests` | `ProjectReference`, tests uniquement |
| `FirstClassErrors.Testing.UnitTests` | `ProjectReference`, tests uniquement |

Tous les projets du dépôt chargent en outre les analyseurs JustDummies au build (ADR-0061).

L'[ADR-0026](0026-rebase-testing-arbitrary-values-on-dummies.fr.md) acceptait l'embarquement comme
explicitement temporaire : JustDummies « n'est pas encore sur NuGet (ADR-0011), donc le référencer en privé et
embarquer son assembly dans ce package […] ; basculer vers un `PackageReference` NuGet une fois JustDummies
publié ».

**JustDummies n'a jamais été publié.** Aucun tag `dum-v*` n'a jamais été poussé depuis ce dépôt, et
`Reefact/just-dummies` n'a pas publié non plus — sa politique de *trusted publishing* nuget.org n'existe pas
encore.

## Décision

Ce dépôt devient **consommateur** des packages `JustDummies` et de ses analyseurs publiés depuis
`Reefact/just-dummies`, et cesse d'en être la source.

La bascule est **conditionnée à la première publication** et n'est délibérément pas exécutée par
l'extraction. Tant qu'aucun package `JustDummies` restaurable n'existe sur nuget.org, la source reste ici
telle quelle : remplacer un `ProjectReference` par un `PackageReference` vers une version que personne ne peut
restaurer casserait le build pour chaque contributeur et chaque exécution CI, sans bénéfice.

Lorsque cette version existera, en une seule pull request :

1. ajouter `<PackageVersion Include="JustDummies" Version="X.Y.Z" />` à `Directory.Packages.props` ;
2. remplacer les quatre `ProjectReference` vers `JustDummies` par des `PackageReference` ;
3. remplacer les `ProjectReference` vers les analyseurs — ils sont livrés dans le package de la bibliothèque
   sous `analyzers/dotnet/cs`, donc un simple `PackageReference` les délivre et la plomberie
   `OutputItemType="Analyzer"` disparaît ;
4. supprimer la cible `IncludeJustDummiesInPackage` et son accroche `TargetsForTfmSpecificBuildOutput` de
   `FirstClassErrors.Testing.csproj`, et retirer `PrivateAssets="all"` pour que le package déclare une
   dépendance `JustDummies` honnête ;
5. retirer les sept projets `JustDummies.*` de `FirstClassErrors.sln`, le train `dum` de `tools/trains.sh`,
   `pack.sh` et `release.yml`, le scope `justdummies` de `tools/commit-lint/lint-commit-message.sh`, les
   trois configurations `build/stryker/justdummies*.json`, et les workflows
   `.github/workflows/justdummies.yml` et `justdummies-mutation.yml` ;
6. supprimer les répertoires source et `tools/justdummies-check/` en dernier, une fois que plus rien ne les
   référence ;
7. exécuter le build et la suite de tests complets.

L'ordre compte : supprimer les répertoires en premier transforme chaque autre étape en séance de débogage de
build cassé.

## Conséquences

### `FirstClassErrors.Testing` acquiert une vraie dépendance

Aujourd'hui le package embarque silencieusement une copie de `JustDummies.dll` sans entrée `<dependency>` :
un consommateur qui référence aussi `JustDummies` directement peut se retrouver avec deux copies à des
versions différentes, sans aucun diagnostic. Après la bascule, le package déclare sa dépendance, NuGet résout
un seul assembly, et la version devient visible et relisible. C'est l'objet du changement, pas un effet de
bord.

### La documentation qui reste change de sujet, pas de propriétaire

Les ADR-0011, ADR-0026 et ADR-0061 ne sont **pas** supprimés, ni l'ADR-0006. Ils actent des décisions que ce
dépôt a réellement prises, et la forme actuelle de `FirstClassErrors.Testing` est illisible sans eux. Les
ADR-0011 et ADR-0022 existent aussi dans `Reefact/just-dummies`, car ils lient les deux produits.

`doc/handwritten/for-users/ArbitraryTestValues.{en,fr}.md` documente le package de test de ce dépôt et
mentionne JustDummies comme son moteur ; il reste, et gagne un lien vers le nouveau dépôt.

### Les références d'issues ne fonctionnent que dans un sens

Les messages de commit de `Reefact/just-dummies` antérieurs au 2026-07-31 citent des numéros d'issues et de
pull requests de **ce** dépôt. Rien là-bas ne peut être renuméroté : ces références se résolvent ici et
doivent continuer de se résoudre. Les issues de ce dépôt ne doivent donc pas être supprimées, seulement
fermées.

### Jusqu'à la publication, les deux dépôts portent la source

Cette duplication est réelle et constitue le prix de ne pas livrer un build cassé. Elle prend fin à la
première release de `JustDummies`. Le risque de divergence entre-temps est faible — ce dépôt doit traiter sa
copie comme gelée et faire atterrir les changements JustDummies dans `Reefact/just-dummies` — mais il n'est
pas nul, et c'est la raison pour laquelle la bascule ne doit pas trop attendre.

## Alternatives considérées

### Faire la bascule maintenant, contre une version non publiée

Rejetée : `dotnet restore` échouerait en NU1102 pour chaque contributeur et chaque exécution CI tant qu'aucun
package n'existe. Un dépôt qui ne construit pas est pire qu'un dépôt qui porte un doublon temporaire.

### Continuer de consommer JustDummies via un sous-module Git, ou par URL Git

Rejetée. Les deux réintroduisent le couplage que l'extraction a supprimé, sous une forme plus difficile à
raisonner que le `ProjectReference` actuel : un sous-module épingle un commit et non une version, et ni l'un
ni l'autre n'est exprimable dans le graphe de dépendances du package publié — `FirstClassErrors.Testing`
devrait donc toujours embarquer l'assembly qu'il ne peut pas déclarer.

### Publier JustDummies depuis ce dépôt une dernière fois, puis basculer

Considérée parce qu'elle débloquerait la bascule immédiatement. Rejetée parce que la première version publiée
d'un package fixe l'endroit d'où il est publié : la politique de *trusted publishing*, l'URL de dépôt dans ses
métadonnées et les commits SourceLink pointeraient tous ici, et la release suivante devrait les contredire.
