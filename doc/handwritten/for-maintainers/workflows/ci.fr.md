# Workflow `ci`

🌍 🇬🇧 [English](ci.en.md) · 🇫🇷 Français (ce fichier)

> Documentation mainteneur — fait partie de la [référence des workflows](README.fr.md).
> Ne fait pas partie de la documentation utilisateur sous `doc/`.

**Fichier du workflow :** [`.github/workflows/ci.yml`](../../../../.github/workflows/ci.yml)

## À quoi il sert

`ci` est le barrage principal : il construit toute la solution et exécute toute
la suite de tests, sous **Linux et Windows**, et collecte la couverture de code.
Si un changement casse le build ou un test sur l'une des deux plateformes, c'est
ici que ça se voit.

La matrice multiplateforme n'est pas décorative. Le générateur de documentation
lance son worker dans un **processus séparé** et manipule des chemins de système
de fichiers ; la solution se comporte donc réellement différemment sous Windows
et Linux — les deux branches sont exercées à dessein.

Un second job, `floor`, prouve l'*autre* bout de la plage supportée : que l'outil
de documentation `fce` et son worker — livrés en ciblant `net8.0`, le floor de
l'outillage — **s'exécutent réellement sur le runtime .NET 8**, et pas seulement
sur le plus récent avec lequel la CI build. Voir
[ADR 0002 — Fixer le floor du runtime de l'outillage](../adr/0002-floor-the-tooling-runtime.md)
pour la décision *(rédigée en anglais)* ; ce job en est la mise en application
côté runtime.

## Quand il s'exécute

- À chaque **push sur `main`**.
- À chaque **pull request visant `main`**.
- À la demande via **`workflow_dispatch`**.

## Comment il s'exécute

### `build-test` — toute la solution sur le .NET le plus récent

Sur une matrice `[ubuntu-latest, windows-latest]` :

1. Checkout, puis installation du SDK .NET.
2. `dotnet restore` → `dotnet build -c Release` → `dotnet test -c Release`.
3. Les tests collectent la couverture OpenCover via `coverlet.collector`,
   configuré par [`coverage.runsettings`](../../../../coverage.runsettings), dans
   `artifacts/coverage/<guid>/coverage.opencover.xml`.
4. Le rapport de couverture est uploadé en artefact, un par OS.

### `floor` — l'outillage sur son runtime minimum

`build-test` tourne sur le .NET le plus récent ; `floor` fait tourner l'outillage
*tel que livré* sur le plus ancien supporté. Sur `ubuntu-latest` :

1. Installer **deux SDK** : `10.0.x` (satisfait le `global.json` du dépôt, pour
   que le build tourne normalement) et `8.0.x` (apporte le **runtime .NET 8**, le
   floor contre lequel l'outillage est livré).
2. Construire l'outil `fce` net8 avec son worker, et un **build net8 de l'exemple
   `Usage`** comme vraie cible à documenter. (`Usage` est multi-ciblé
   `net8.0;net10.0` précisément pour que ce job ait une cible sur le floor.)
3. Lancer `fce generate` contre cette cible avec `DOTNET_ROLL_FORWARD=LatestPatch`
   dans l'environnement, puis vérifier que le catalogue généré contient bien une
   erreur documentée — une preuve positive, pas seulement un exit 0.

## Permissions & sécurité

`contents: read` seulement — le workflow ne fait que checkout et build. Il ne
stocke aucun secret et n'a besoin d'aucun périmètre en écriture.

## À manipuler avec précaution

- **`fail-fast: false` est volontaire.** Il force les deux branches de la matrice
  à aller au bout, pour qu'une panne propre à une plateforme soit toujours
  visible ; ne le retirez pas pour « gagner des minutes ».
- **Le nom de l'artefact de couverture est par OS** (`coverage-${{ matrix.os }}`).
  Uploader les deux branches sous le même nom d'artefact provoquerait une
  collision — gardez le nom paramétré.
- **`if-no-files-found: error`** est intentionnel : un « aucune couverture
  produite » silencieux laisserait passer une configuration de couverture cassée.
  Ça doit échouer.
- **C'est ici que le cliquet de warnings est imposé.** La promotion
  `TreatWarningsAsErrors` / `MSBuildTreatWarningsAsErrors` (voir
  [`Directory.Build.props`](../../../../Directory.Build.props)) est cantonnée à la CI et
  *imposée ici*, sur les deux branches OS. Le workflow `sonar` la désactive
  volontairement pour son propre build d'analyse — c'est donc `ci`, et non
  `sonar`, le barrage qui doit rester vert sur les warnings.
- **`DOTNET_ROLL_FORWARD=LatestPatch` sur le run `floor` est porteur.** Il
  surcharge le roll-forward inscrit dans chaque runtimeconfig (`Major` pour la
  CLI, `LatestMajor` pour le worker) et reste dans le major demandé, pour que les
  deux processus se lient au plus haut patch **.NET 8** présent et ne puissent
  jamais basculer sur le .NET 10 que le runner porte aussi. Retirez-le et
  l'outillage tournerait silencieusement sur .NET 10 — et le job ne prouverait
  plus rien sur le floor.
- **Le job `floor` grep le catalogue généré à la recherche d'une erreur
  documentée, pas seulement un exit 0.** Un outil qui a démarré mais n'a rien
  chargé sortirait proprement ; exiger une erreur extraite prouve que le worker a
  réellement chargé la cible net8 via `Assembly.LoadFrom`.
- **`Usage` doit garder une cible `net8.0`.** Le job documente le build net8 de
  `Usage` ; si `Usage` retirait net8 de ses `<TargetFrameworks>`, le job n'aurait
  plus de cible sur le floor à documenter.

## En rapport

- [`sonar`](sonar.fr.md) réutilise le même `coverage.runsettings` pour que son
  rapport de couverture corresponde à celui-ci.
- [`analyzers`](analyzers.fr.md) couvre le dogfood des analyzers que `ci` ne fait
  pas.
- [`canary.yml`](../../../../.github/workflows/canary.yml) *(pas encore de page de
  référence)* fait tourner le même outillage net8 sur la **preview** .NET
  suivante, chaque semaine, pour attraper une régression de roll-forward vers un
  major pas encore publié avant qu'il ne sorte — la seule surface que le job
  `floor` ne peut pas couvrir (voir ADR 0002).
- [ADR 0002 — Fixer le floor du runtime de l'outillage](../adr/0002-floor-the-tooling-runtime.md)
  — la décision que le job `floor` fait respecter.
