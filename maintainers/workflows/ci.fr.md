# Workflow `ci`

🌍 🇬🇧 [English](ci.en.md) · 🇫🇷 Français (ce fichier)

> Documentation mainteneur — fait partie de la [référence des workflows](README.fr.md).
> Ne fait pas partie de la documentation utilisateur sous `doc/`.

**Fichier du workflow :** [`.github/workflows/ci.yml`](../../.github/workflows/ci.yml)

## À quoi il sert

`ci` est le barrage principal : il construit toute la solution et exécute toute
la suite de tests, sous **Linux et Windows**, et collecte la couverture de code.
Si un changement casse le build ou un test sur l'une des deux plateformes, c'est
ici que ça se voit.

La matrice multiplateforme n'est pas décorative. Le générateur de documentation
lance son worker dans un **processus séparé** et manipule des chemins de système
de fichiers ; la solution se comporte donc réellement différemment sous Windows
et Linux — les deux branches sont exercées à dessein.

## Quand il s'exécute

- À chaque **push sur `main`**.
- À chaque **pull request visant `main`**.
- À la demande via **`workflow_dispatch`**.

## Comment il s'exécute

Un seul job, `build-test`, sur une matrice `[ubuntu-latest, windows-latest]` :

1. Checkout, puis installation du SDK .NET.
2. `dotnet restore` → `dotnet build -c Release` → `dotnet test -c Release`.
3. Les tests collectent la couverture OpenCover via `coverlet.collector`,
   configuré par [`coverage.runsettings`](../../coverage.runsettings), dans
   `artifacts/coverage/<guid>/coverage.opencover.xml`.
4. Le rapport de couverture est uploadé en artefact, un par OS.

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
  [`Directory.Build.props`](../../Directory.Build.props)) est cantonnée à la CI et
  *imposée ici*, sur les deux branches OS. Le workflow `sonar` la désactive
  volontairement pour son propre build d'analyse — c'est donc `ci`, et non
  `sonar`, le barrage qui doit rester vert sur les warnings.

## En rapport

- [`sonar`](sonar.fr.md) réutilise le même `coverage.runsettings` pour que son
  rapport de couverture corresponde à celui-ci.
- [`analyzers`](analyzers.fr.md) couvre le dogfood des analyzers que `ci` ne fait
  pas.
