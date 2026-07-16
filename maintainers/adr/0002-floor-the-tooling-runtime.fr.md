# ADR-0002 | Fixer le floor du runtime de l'outillage à la plus ancienne LTS supportée

🌍 🇬🇧 [English](0002-floor-the-tooling-runtime.md) · 🇫🇷 Français (ce fichier)

**Statut :** Accepté
**Date :** 2026-07-13
**Décideurs :** Reefact

## Contexte

FirstClassErrors livre deux types d'artefacts très différents :

* la **librairie** (`FirstClassErrors`, `FirstClassErrors.Testing`) cible
  **`netstandard2.0`**. Une librairie netstandard est consommée par
  *n'importe quel* runtime qui implémente le standard — .NET Framework 4.6.1+,
  .NET Core 2.0+, .NET 5–10+, Mono/Unity — de sorte que la question
  « fonctionne presque partout » est déjà résolue, une seule fois, par le TFM,
  et ne nécessite rien ici.
* l'**outillage** (`FirstClassErrors.Cli` — l'outil .NET `fce` — plus
  `FirstClassErrors.GenDoc` et `FirstClassErrors.GenDoc.Worker`, qu'il charge
  in-process et lance comme processus enfant) est une **application exécutable
  dépendante du framework**. Son TFM est un **minimum strict** : une application
  dépendante du framework ne peut jamais s'exécuter sur un runtime **plus
  ancien** que son TFM, et le **roll-forward ne va jamais que vers le haut,
  jamais vers le bas**.

Le TFM de l'outillage décide donc *quels consommateurs peuvent exécuter `fce`
tout court*. Il était à `net10.0`, ce qui signifiait qu'un atelier dont le
runtime installé le plus récent est .NET 8 pouvait référencer la librairie
mais **ne pouvait pas exécuter le générateur de documentation** — alors même que
la librairie qu'il documente est `netstandard2.0`.

Il existe une seconde contrainte, plus subtile. Le worker charge l'assembly
**cible** via `Assembly.LoadFrom` (voir
`FirstClassErrors.GenDoc.Worker/Program.cs`). Cette cible peut être compilée
pour n'importe quel runtime choisi par le consommateur, de sorte que le
**processus** worker doit s'exécuter sur un runtime `>=` à celui de la cible.
C'est un problème de *roll-forward*, pas un problème de *nombre de TFM*, et les
deux sont faciles à confondre.

Les politiques de roll-forward se comportent comme suit : la politique `Minor`
par défaut ne franchit jamais une version majeure ; `Major` monte à la version
majeure suivante uniquement lorsque la majeure demandée est *absente* ;
`LatestMajor` se lie toujours à la **plus haute** majeure **installée**.

.NET 8 est le plus ancien .NET encore supporté par Microsoft (sa date d'EOL est
le 2026-11-10), et c'est le floor que l'analyzer énonce déjà :
[ADR-0001](0001-lock-the-analyzer-roslyn-floor.fr.md) épingle Roslyn 4.8, le
compilateur du SDK .NET 8.0.100.

La CI compile sur le dernier SDK .NET publié (actuellement .NET 10), et les
runners GitHub embarquent plusieurs runtimes côte à côte.

## Décision

L'outillage (`FirstClassErrors.Cli`, `FirstClassErrors.GenDoc`,
`FirstClassErrors.GenDoc.Worker`) cible uniquement **`net8.0`** — le plus ancien
.NET encore supporté par Microsoft — et couvre tous les runtimes plus récents
avec le roll-forward, et non une matrice de cibles.

## Justification

Le floor réduit l'histoire du support à une seule phrase : *FirstClassErrors
supporte .NET 8 et au-delà pour son outillage et son analyzer ; la librairie
elle-même est `netstandard2.0` et descend jusqu'à .NET Framework 4.6.1.* Le
floor de l'outillage et le floor de l'analyzer (ADR-0001) énoncent le **même**
minimum, de sorte que le produit énonce **un seul** chiffre de support.

Le roll-forward couvre tous les runtimes plus récents, ajusté par processus :

| Projet | `RollForward` | Pourquoi |
|---|---|---|
| `FirstClassErrors.Cli` (`fce`) | `Major` | Le front-end a seulement besoin de *s'exécuter*. `Major` monte le build net8 à la majeure suivante lorsque .NET 8 est absent, de sorte qu'une machine qui n'a que .NET 10 l'exécute (monte 8→10). Sans lui, la politique `Minor` par défaut ne franchit jamais une majeure et `fce` échouerait à démarrer sur la machine courante « .NET plus récent, pas de .NET 8 ». |
| `FirstClassErrors.GenDoc.Worker` | `LatestMajor` | Le worker doit **surclasser la cible qu'il charge**. `Major` ne monte que lorsque la majeure demandée est *absente*, de sorte que sur une machine qui embarque **à la fois** .NET 8 et .NET 10, un worker net8 se lierait à 8 et échouerait à charger une cible net10. `LatestMajor` se lie toujours à la **plus haute** majeure **installée**, de sorte que le worker peut documenter une cible compilée pour n'importe quel runtime présent. |
| `FirstClassErrors.GenDoc` | — | Chargé in-process par `fce` ; le runtime est choisi par le runtimeconfig de la CLI, de sorte qu'une librairie ne fixe aucune politique. |

`<LangVersion>latest</LangVersion>` reste sur les trois projets afin que le floor
net8 ne borne que la **surface BCL et le runtime cible**, et non le C# que la
source peut utiliser (les projets `netstandard2.0` font déjà exactement cela).

Ce design évite aussi un cycle répétitif à chaque version : une nouvelle version
.NET (net11, net12, …) ne requiert **aucun rebuild, aucun changement de code,
aucune re-publication** — le roll-forward exécute dessus les binaires `net8.0`
existants — et un runtime *au-dessus* du floor qui atteint son EOL ne requiert
rien, parce que nous ne le ciblons pas. Seul le floor LTS lui-même atteignant
son EOL appelle un bump, une ligne par projet, selon une cadence à peu près
biennale (voir Actions de suivi).

La décision est sûre à maintenir parce que le floor peut être imposé sur les
deux axes sur lesquels il peut régresser, et chaque axe dispose d'un garde-fou :

* **La surface d'API dérive au moment du build.** Parce que les projets
  *ciblent* `net8.0`, chaque build de CI (sur le SDK .NET 10) les compile contre
  le reference pack net8, de sorte qu'une API propre à `net10` ne peut pas se
  glisser silencieusement — elle casse le build ordinaire, sans qu'un job dédié
  soit nécessaire. C'est pourquoi le floor de l'outillage est moins coûteux à
  garder que le floor Roslyn de l'analyzer, qui est invisible sur une CI moderne
  et nécessite `tools/floor-check` (ADR-0001).
* **L'exécution runtime régresse au moment de l'exécution.** Le job `floor` dans
  `ci.yml` exécute l'outillage net8 livré sur le runtime .NET 8 lui-même,
  prouvant que la CLI et le worker démarrent et documentent effectivement une
  vraie cible net8 à cet endroit — la garantie que le build ne peut pas donner.
  La seule surface qu'il ne peut pas couvrir, le roll-forward vers une majeure
  **pas encore publiée**, est surveillée à l'avance par le `canary.yml`
  hebdomadaire, qui exécute le même outillage sur la prochaine preview .NET et
  avertit le mainteneur avant que cette majeure ne sorte.

Les mécaniques des deux jobs — les overrides de roll-forward qui épinglent
l'exécution au runtime voulu, et pourquoi `Usage` est multi-ciblé pour leur
fournir une cible — sont documentées dans la
[référence du workflow `ci`](../workflows/ci.fr.md) ; les réglages `RollForward`
par projet vivent dans les trois csprojs de l'outillage.

## Alternatives envisagées

### Garder l'outillage sur `net10.0` (statu quo)

Envisagé parce que c'était l'état existant : cibler le runtime le plus récent
est la voie de moindre résistance et ne nécessite aucun ajustement de
roll-forward.

Rejeté parce que le TFM est un minimum strict pour une application dépendante du
framework : un atelier dont le runtime installé le plus récent est .NET 8
pourrait référencer la librairie `netstandard2.0` mais ne pourrait pas
exécuter le générateur de documentation qui la documente.

### Multi-cibler l'outillage (`net8.0;net10.0`)

Envisagé comme la façon conventionnelle de servir plusieurs runtimes à la fois.

Rejeté parce que :

* le roll-forward permet déjà à un seul build `net8.0` de s'exécuter sur
  8 / 9 / 10 / 11+, de sorte qu'un second TFM achète une portée que nous avons
  déjà ;
* un générateur de documentation n'a aucun besoin d'API BCL propres à `net10` ;
* une matrice met l'outillage sur une cadence « ajouter en haut, retirer en
  bas » à chaque version, et **réintroduit le piège du worker** : le build bas
  de la matrice est précisément celui qui ne peut pas charger une cible au TFM
  plus élevé.

Un seul build floor + les deux réglages de roll-forward est strictement plus
simple et a la même portée.

## Conséquences

### Positives

* Tout consommateur sur **.NET 8 ou plus récent** peut exécuter `fce`, pas
  seulement ceux sur le runtime le plus récent.
* **Un seul** artefact d'outillage livré ; aucune matrice de TFM par version à
  maintenir.
* Le floor de l'outillage et le floor de l'analyzer énoncent le **même** minimum
  (.NET 8), de sorte que l'histoire du support tient en une seule phrase.
* Vérifié de bout en bout : un `fce` `net8.0` documente un assembly cible
  **`net10`** sur une machine qui n'a **que** le runtime .NET 10 — `fce` monte
  8→10 (`Major`) et le worker se lie à la plus haute majeure (`LatestMajor`)
  pour charger la cible net10.
* Gardé en CI sur toute la plage : `build-test` exécute la suite sur le dernier
  .NET publié (10) ; le job `floor` exécute l'outillage livré sur le runtime
  .NET 8 ; et `canary.yml` l'exécute sur la prochaine preview .NET (voir
  Justification, et la [référence du workflow `ci`](../workflows/ci.fr.md)).
* Aucun remaniement de code dû au mouvement des versions .NET ; tout au plus un
  bump de TFM d'une ligne environ une fois tous les deux ans.

### Négatives

* `fce` ne peut pas s'exécuter sur une machine dont le runtime le plus récent
  est antérieur à .NET 8 (par exemple un .NET 6/7 en EOL, ou uniquement
  .NET Framework). Accepté : ces consommateurs **utilisent** toujours la
  librairie `netstandard2.0` dans leur application ; exécuter un *outil* de
  dev/CI sur un runtime actuellement supporté est un prérequis raisonnable (un
  SDK .NET moderne est déjà présent partout où l'on compile du .NET moderne).

### Risques

* `LatestMajor` sur le worker va, sur une machine où une **preview** de la
  prochaine majeure est installée, se lier à cette preview. Ce n'est un risque
  que pour les machines qui optent pour les previews, et `canary.yml` est
  précisément l'alerte précoce que cette liaison fonctionne toujours avant que
  cette majeure ne sorte.
* Une régression de roll-forward contre une majeure pas encore publiée est
  attrapée par le canary hebdomadaire, pas par une barrière de pull request —
  par conception, puisqu'une preview peut être non publiée ou instable.

## Actions de suivi

* **Quand le floor LTS atteint son EOL** (.NET 8 → 2026-11-10 ; hygiène plutôt
  que fonction — le roll-forward maintient le build net8 fonctionnel sur les
  runtimes plus récents après l'EOL — selon une cadence à peu près biennale) :
  1. Changer `<TargetFramework>` de `net8.0` vers le nouveau floor dans les trois
     csprojs de l'outillage : `FirstClassErrors.Cli`, `FirstClassErrors.GenDoc`,
     `FirstClassErrors.GenDoc.Worker`. Laisser les réglages `RollForward` tels
     quels.
  2. Monter le nouveau floor dans le `<TargetFrameworks>` de l'exemple `Usage`
     (afin que le job floor de la CI ait toujours une cible sur le nouveau floor)
     et dans le job `floor` de `ci.yml` (`dotnet-version` `8.0.x` → le runtime du
     nouveau floor).
  3. Mettre à jour la note de runtime dans `FirstClassErrors.Cli/README.nuget.md`.
  4. Remplacer cet ADR (nouveau floor, nouveau runtime minimum).
  5. Optionnellement, retirer les overrides `<LangVersion>latest</LangVersion>`
     si le C# par défaut du nouveau floor est déjà la version que vous voulez.

  Garder ceci aligné avec le floor de l'analyzer (ADR-0001) maintient vrai
  l'énoncé de support unique « .NET N et au-delà » du produit.
* **Quand la majeure preview du canary atteint la GA :** `canary.yml` épingle la
  majeure preview qu'il cible (`dotnet-version: 11.0.x`, qualité `preview`) ;
  montez-la à la suivante (`12.0.x`, …) pour que le canary continue de regarder
  une version en avant. Rien ne casse si vous oubliez : `build-test` récupère la
  majeure nouvellement publiée comme « latest », et le canary cesse simplement de
  trouver une preview plus récente que le SDK de build et termine ses exécutions
  en neutre jusqu'au bump.

## Références

* [ADR-0001](0001-lock-the-analyzer-roslyn-floor.fr.md) — le floor Roslyn de
  l'analyzer, le pendant build-time de cette décision run-time.
* [référence du workflow `ci`](../workflows/ci.fr.md) — le job `floor`,
  structurellement.
* `FirstClassErrors.GenDoc.Worker/Program.cs` — l'appel `Assembly.LoadFrom`
  derrière la contrainte de roll-forward du worker.
