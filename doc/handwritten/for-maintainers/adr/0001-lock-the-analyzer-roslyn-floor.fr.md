# ADR-0001 | Verrouiller le floor Roslyn de l'analyzer

🌍 🇬🇧 [English](0001-lock-the-analyzer-roslyn-floor.md) · 🇫🇷 Français (ce fichier)

**Statut :** Accepté
**Date :** 2026-07-10
**Décideurs :** Reefact

## Contexte

`FirstClassErrors.Analyzers` est un analyzer Roslyn livré **intégré dans le
package NuGet `FirstClassErrors`** (à `analyzers/dotnet/cs/`), de sorte que les
consommateurs qui référencent le package obtiennent automatiquement les
diagnostics `FCExxx`, sans installation supplémentaire.

Un analyzer intégré est chargé par **le compilateur hôte de chaque
consommateur** — le Roslyn fourni avec leur SDK .NET ou leur IDE. La version de
`Microsoft.CodeAnalysis.*` contre laquelle l'analyzer est *compilé* devient donc
le Roslyn **minimum** capable de le charger :

* si l'analyzer référence un Roslyn **plus récent** que l'hôte, l'hôte refuse de
  le charger et émet **`CS8032`** (et l'analyzer ne fait silencieusement rien) ;
* si l'analyzer lève une exception lors du chargement, l'hôte émet **`AD0001`**.

Une montée de version de routine de la dépendance `Microsoft.CodeAnalysis.*`
élève donc silencieusement le SDK/IDE minimum que chaque consommateur doit
posséder. Cette régression précise s'est produite une fois : l'analyzer a dérivé
jusqu'à exiger Roslyn 5.6. Dependabot propose de telles montées de version
automatiquement, comme n'importe quelle autre mise à jour de dépendance.

Le contrat de chargement est invisible sur les chaînes d'outils modernes — la CI
sur le dernier SDK, ainsi que l'IDE du mainteneur lui-même, satisfont l'un comme
l'autre n'importe quel floor — de sorte qu'il peut régresser sans aucun signal
rouge.

L'hôte le plus ancien que FirstClassErrors prend en charge est le **SDK .NET
8.0.100 / Visual Studio 2022 17.8**, dont le compilateur est **Roslyn 4.8**.

`CS8032` n'est émis que lorsqu'un chargement est *tenté* : un analyzer qui n'est
jamais raccordé à une compilation laisse la build au vert. Et l'analyzer atteint
les consommateurs via un chemin de packaging (`analyzers/dotnet/cs/`) qui peut se
casser indépendamment des références propres de l'analyzer.

`Microsoft.CodeAnalysis.Analyzers` (5.6.0) est un analyzer d'authoring à la
compilation (`PrivateAssets="all"`), et non une référence à l'exécution de
l'assembly livré, de sorte qu'il n'affecte pas le contrat de chargement.

## Décision

Le floor Roslyn de l'analyzer est fixé à **4.8.0** — le Roslyn de l'hôte pris en
charge le plus ancien — déclaré une seule fois comme `RoslynFloorVersion` dans
`Directory.Build.props` et appliqué par quatre garde-fous indépendants.

## Justification

La valeur du floor est dictée par le contexte : 4.8.0 est exactement le Roslyn
livré avec l'hôte le plus ancien que nous prétendons prendre en charge (SDK .NET
8.0.100 / VS 2022 17.8). Un floor plus bas n'apporte rien ; un floor plus haut
abandonne silencieusement des hôtes pris en charge.

Le contrat nécessite plus qu'un simple épinglage de version, car il régresse sans
aucun signal rouge sur les chaînes d'outils modernes. Un épinglage seul
*ressemble* encore à de la maintenance de routine, et une référence trop récente
peut se glisser de trois manières distinctes qu'aucune vérification unique
n'attrape : la *version de référence* peut dériver, l'analyzer livré peut échouer
à se *charger* sur un hôte ancien, et il peut échouer à être *packagé* là où les
consommateurs le cherchent. La décision superpose donc une **défense en
profondeur** — une source unique de vérité et quatre garde-fous, chacun fermant
une brèche que les autres ne peuvent fermer :

* le floor est déclaré **une seule fois**, comme `RoslynFloorVersion` dans
  `Directory.Build.props`, de sorte que l'épinglage, le test et le job CI suivent
  une valeur unique et ne peuvent jamais diverger ;
* **l'épinglage** compile l'analyzer contre exactement ce Roslyn, fixant le floor
  à sa source ;
* **le test unitaire** (`RoslynFloorTests`) relit le floor depuis les métadonnées
  de l'assembly et échoue, rapidement et in-process, si un quelconque assembly
  `Microsoft.CodeAnalysis*` référencé le dépasse — attrapant la dérive de
  *référence* ;
* **le job CI de floor-check** empaquette la librairie et reconstruit
  l'exemple contre l'analyzer empaqueté sous le SDK du floor, prouvant que
  l'**artefact livré** à la fois se **charge** et est **packagé** correctement sur
  le **compilateur pris en charge le plus ancien** — les deux brèches que le test
  unitaire ne peut atteindre ;
* **l'ignore Dependabot** empêche une PR automatisée de jamais proposer la montée
  de version, de sorte que relever le floor reste un acte conscient plutôt qu'une
  mise à jour entérinée sans examen.

Deux des garde-fous échouent **bruyamment** (le test unitaire et le job CI) ;
l'épinglage et l'ignore Dependabot fonctionnent silencieusement par construction.
Ensemble, ils satisfont l'exigence posée par le contexte : un garde-fou qui
échoue sur l'hôte le **plus ancien**, sur l'**artefact exact que nous livrons**,
avant qu'un consommateur ne voie jamais `CS8032`. Le compromis accepté est
l'entretien d'un job CI délibérément complexe et d'un projet `tools/floor-check/`
hors-solution ; les mécanismes de ce job — la séparation en deux SDK, la preuve
de chargement et les pièges NuGet qu'il ferme — sont documentés dans la
[référence du workflow `analyzers`](../workflows/analyzers.fr.md), et l'épinglage
et les métadonnées résident dans `FirstClassErrors.Analyzers.csproj`.

## Alternatives envisagées

### Suivre le Roslyn courant et laisser passer les montées de version de dépendances (statu quo)

Envisagée parce que c'est le comportement par défaut, sans effort : les montées
de version de Roslyn arrivent comme des mises à jour de dépendances de routine,
et rien dans la chaîne d'outils ne s'y oppose.

Rejetée parce que chaque montée de version élève silencieusement le SDK/IDE
minimum que chaque consommateur doit posséder — la dérive vers Roslyn 5.6 s'est
produite exactement de cette manière, sans aucun signal rouge nulle part.

### Épingler la version de Roslyn, sans garde-fous supplémentaires

Envisagée comme le correctif minimal : un épinglage d'une ligne arrête la dérive.

Rejetée parce qu'un simple épinglage *ressemble* encore à de la maintenance de
routine — Dependabot continue de proposer la montée de version, et en accepter
une ne fait rien échouer : aucun test ne relit le floor, et aucune build ne
s'exécute sur l'hôte le plus ancien. La régression reviendrait avec la prochaine
mise à jour bien intentionnée.

### S'appuyer sur le seul test unitaire (pas de job CI de floor-check)

Envisagée parce que le test est rapide, in-process, et s'exécute dans le
`dotnet test` ordinaire.

Rejetée parce qu'elle n'attrape que la dérive de version de *référence*. Elle ne
peut pas prouver que l'artefact livré se **charge** réellement sur l'hôte pris en
charge le plus ancien, ni que l'analyzer est réellement **packagé** à
`analyzers/dotnet/cs/` — deux choses qui peuvent se casser alors que chaque
version de référence reste au floor.

## Conséquences

### Positives

* Le contrat de chargement ne peut pas régresser silencieusement : une référence
  Roslyn trop récente fait échouer le test unitaire (rapide) **et** le job de
  floor-check (authentique), et un chemin de packaging cassé fait échouer le job
  de floor-check.
* Le job de floor-check teste l'*artefact livré* sur l'*hôte pris en charge le
  plus ancien*, et non un substitut.
* Le floor est une décision d'une seule ligne, auto-documentée
  (`RoslynFloorVersion`).

### Négatives

* Deux garde-fous supplémentaires à maintenir au vert, et un projet
  `tools/floor-check/` hors-solution avec une configuration NuGet
  intentionnellement complexe (documentée dans la [référence du workflow
  `analyzers`](../workflows/analyzers.fr.md)).
* Le job de floor-check télécharge le SDK 8.0.100 à chaque exécution (~quelques
  secondes).
* Relever le floor est un acte délibéré et en plusieurs étapes (à dessein).

### Risques

* La configuration NuGet du floor-check paraît sur-conçue à un lecteur qui ne
  connaît pas les pièges qu'elle ferme ; une future « simplification » en
  réintroduirait un comme un bug silencieux. Atténuation : chaque subtilité est
  consignée dans la [référence du workflow `analyzers`](../workflows/analyzers.fr.md)
  et dans les commentaires YAML du workflow lui-même.
* Lorsque le floor est relevé, le SDK du floor épinglé dans `analyzers.yml` et
  dans `tools/floor-check/global.json` doit être déplacé à la main ; les oublier
  laisse le job valider l'ancien floor. Atténuation : la procédure de relèvement
  ci-dessous les liste explicitement.

## Actions de suivi

* Aucune dans l'immédiat : les quatre garde-fous (épinglage, `RoslynFloorTests`,
  le job de floor-check `analyzers.yml`, l'ignore Dependabot) ont été livrés avec
  cette décision.
* Si le floor est un jour relevé :
  1. Monter `<RoslynFloorVersion>` dans `Directory.Build.props`.
  2. Mettre à jour le SDK du floor dans `analyzers.yml` (`dotnet-version` et le
     `tools/floor-check/global.json` imbriqué) vers le SDK dont le Roslyn
     correspond au nouveau floor.
  3. Mettre à jour la note d'exigence de compilateur du README /
     `doc/handwritten/for-users/README.fr.md`.
  4. Remplacer cet ADR (nouveau floor, nouveau SDK/IDE minimum).

  L'épinglage, le test unitaire et l'ignore Dependabot ne nécessitent aucun
  changement — ils suivent tous `$(RoslynFloorVersion)` ou les identifiants de
  package.

## Références

* Façonné par #69 (verrouillage initial) et #74 / #75 / #77 (durcissement du
  floor-check).
* [ADR-0002](0002-floor-the-tooling-runtime.fr.md) — le floor du runtime
  d'outillage, le pendant à l'exécution de cette décision à la compilation.
* [référence du workflow `analyzers`](../workflows/analyzers.fr.md) — le job de
  floor-check, structurellement.
