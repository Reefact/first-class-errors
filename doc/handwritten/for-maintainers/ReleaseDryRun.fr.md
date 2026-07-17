# Répétition de release à blanc (« dry run » manuel)

🌍 🇬🇧 [English](ReleaseDryRun.en.md) · 🇫🇷 Français (ce fichier)

↑ Fait partie de la [documentation mainteneur](README.fr.md) · voir aussi la
référence des workflows pour [`release`](workflows/release.fr.md) et
[`release-dryrun`](workflows/release-dryrun.fr.md).

> Documentation technique / mainteneur. Elle ne fait **pas** partie de la
> documentation utilisateur de la bibliothèque, sous `doc/`.

## De quoi s'agit-il

Le workflow `release` (`.github/workflows/release.yml`) publie les packages
NuGet. Il ne s'exécute normalement **que lorsqu'un tag de version est poussé**
(`v1.2.3`) : tout son pipeline — analyse de la version, build, tests, pack,
génération du SBOM, OIDC, attestation de provenance et étapes de publication —
ne tourne donc *pour la première fois qu'en production, sur un tag, une seule
fois*.

Le **dry run manuel** permet de lancer ce même pipeline **à la demande**,
jusqu'à l'attestation de provenance et au login OIDC NuGet inclus, **sans rien
publier**. C'est une répétition : on vérifie que la machinerie de release est
saine avant que ça ne compte.

## Ce qu'il fait — et ne fait pas

| Étape | Release réelle (push de tag) | Dry run |
| --- | --- | --- |
| Résoudre & valider la version | ✅ | ✅ |
| Restore, build, tests | ✅ | ✅ |
| Packer les trains publiés | ✅ | ✅ |
| Embarquer le SBOM SPDX | ✅ | ✅ |
| Uploader les packages en artefacts de run | ✅ | ✅ |
| **Signer l'attestation de provenance** | ✅ | ✅ (voir *Impacts*) |
| **Login NuGet (OIDC)** | ✅ | ✅ (voir *Impacts*) |
| **Push vers nuget.org** | ✅ | ⛔ sauté |
| **Créer la GitHub Release** | ✅ | ⛔ sauté |

Les trois étapes de publication sont conditionnées par
`github.event_name == 'push' || inputs.dry_run == false`, donc :

- un **push de tag publie toujours** — le chemin de release normal est
  inchangé ;
- un **lancement manuel ne publie que si tu décoches explicitement `dry_run`**.

## Comment le lancer

1. Sur GitHub, ouvre l'onglet **Actions**.
2. Dans la barre latérale de gauche, sélectionne le workflow **release**.
3. Clique **Run workflow** (en haut à droite).
4. Renseigne les entrées :
   - **version** — n'importe quel SemVer valide ; utilise une version
     manifestement factice comme `0.0.0-dry.1` (rien n'est publié, la valeur
     doit seulement être valide).
   - **dry run** — **déjà cochée par défaut**. Laisse-la cochée.
5. Clique **Run workflow** et observe le run.

Si le run est vert jusqu'à *Attest build provenance*, le pipeline de release
est sain.

## Impacts

Un dry run est *presque* sans effet de bord, avec deux points à connaître :

- **Il crée une vraie attestation de provenance.** L'étape `Attest build
  provenance` s'exécute pendant un dry run (volontairement — les échecs d'OIDC
  ou de permission d'attestation sont précisément ce qu'elle sert à détecter).
  Cette attestation est écrite dans le magasin d'attestations du dépôt et dans
  le journal de transparence public Sigstore : elle est **permanente et
  publique**, et référence la version jetable. C'est inoffensif mais pas rien,
  donc :
  - utilise une version clairement factice (`0.0.0-dry.N`) pour qu'une
    attestation jetable ne soit jamais confondue avec une vraie release ;
  - lance le dry run manuel de façon délibérée (avant une vraie release, ou
    après avoir modifié `release.yml`), pas en boucle par réflexe.
- **Il effectue le vrai login OIDC NuGet.** L'échange de jeton du trusted
  publishing s'exécute pendant un dry run — c'est le but : il valide la policy
  nuget.org, donc un dry run **échoue (rouge)** si la policy trusted-publishing
  ou le secret `NUGET_USER` est absent ou mal configuré. Il génère une clé API
  éphémère à usage unique que le dry run ne dépense jamais (le push est sauté),
  donc rien n'est publié.
- **Rien n'est publié.** Aucun package n'atteint nuget.org, et aucune GitHub
  Release ni aucun tag Git n'est créé.
- **Les `.nupkg` / `.snupkg` produits sont uploadés en artefacts de run**, que
  tu peux télécharger depuis la page du run pour les inspecter, et qui expirent
  selon la rétention d'artefacts normale du dépôt.

## Quand l'utiliser

- Avant de sortir une release importante, comme test de fumée final du
  pipeline.
- Après avoir modifié `release.yml`, les `.csproj` empaquetés ou la
  configuration de packaging (`Directory.Build.props`), puisque ces changements
  restent sinon non vérifiés jusqu'à un vrai tag.

## En rapport : le dry run automatique

Pour la partie **sans effet de bord** du pipeline — build, pack et
embarquement du SBOM — il n'y a rien à déclencher à la main : le workflow
`release-dryrun` (`.github/workflows/release-dryrun.yml`) l'exécute
automatiquement à **chaque pull request et push sur `main`**, et échoue si le
SBOM cesse d'être embarqué. Il n'a ni attestation ni publication, donc il tourne
en continu sans effet de bord.

Utilise le dry run **manuel** documenté ici quand tu veux en plus répéter le
chemin **attestation / OIDC** que l'automatique laisse volontairement de côté.

## Ce qu'aucun dry run ne peut tester

Le **push réel vers nuget.org** et les **octets re-signés par le dépôt** que
nuget.org sert ne peuvent pas être exercés sans publier — nuget.org n'a pas de
« push à blanc ». Ce dernier maillon n'est jamais validé que par une vraie
release.

Aucun dry run ne prouve non plus que l'**outil `fce` installé fonctionne
réellement**. `fce generate` ne fait pas tout le travail en process : il lance
`FirstClassErrors.GenDoc.Worker` dans un process enfant et le résout à côté de
l'exécutable installé (`AppContext.BaseDirectory`). Le worker ne voyage dans le
package du tool que parce que `PackAsTool` empaquette la sortie *publish* du CLI
et que la cible `_PublishDocumentationWorker` y dépose la clôture du worker — un
mécanisme que `dotnet build` et `dotnet publish` n'exercent pas (ils déposent des
fichiers à côté d'un binaire de build, jamais dans le `.nupkg`). Voir le test de
tool-install ci-dessous.

## Le test de fumée tool-install (le worker `fce`)

`tools/packaging/pack.sh` vérifie, pour le train `cli`, que le **fichier** du
worker (`FirstClassErrors.GenDoc.Worker.dll`) est présent sous `tools/<tfm>/any/`
dans le `.nupkg`. Ce garde est réel et tourne sur chaque train `cli` packé — mais
un fichier présent n'est pas un tool qui marche : la clôture du worker peut rester
incomplète (une dépendance manquante, un mauvais `.runtimeconfig.json`), ce que le
contrôle de présence ne voit pas. Le seul oracle qui prouve que la clôture
empaquetée s'exécute vraiment est une installation réelle suivie d'un
`fce generate`.

Lance-le **au moins une fois avant le premier tag `cli-v…`**, et de nouveau après
tout changement de l'empaquetage du CLI (les cibles worker du `.csproj`,
`PackAsTool`, ou les dépendances du worker) :

```sh
# 1. Builder et packer le train cli exactement comme la release.
dotnet build FirstClassErrors.sln -c Release
tools/packaging/pack.sh 0.0.0-workercheck.1 cli   # -> artifacts/FirstClassErrors.Cli.0.0.0-workercheck.1.nupkg

# 2. Installer le tool packé en global. Installer par IDENTIFIANT DE PACKAGE
#    (FirstClassErrors.Cli) ; ce n'est pas le nom de commande (fce).
dotnet tool install --global --add-source ./artifacts FirstClassErrors.Cli --version 0.0.0-workercheck.1

# 3. Générer un catalogue. Soit en pointant une assembly buildée et opt-in...
fce generate --assemblies chemin/vers/VotreProjet.dll --format markdown --output ./out/catalog.md --service-name demo
#    ...soit une solution avec au moins un projet opt-in (GenerateErrorDocumentation=true) :
fce generate --solution chemin/vers/Votre.sln --format markdown --output ./out --service-name demo
#    Attendu : un catalogue NON VIDE et ZÉRO « documentation worker could not be located ».

# 4. Nettoyer.
dotnet tool uninstall --global FirstClassErrors.Cli
```

Une cible pratique du dépôt pour l'étape 3 est `FirstClassErrors.Usage` : elle
opte-in (`GenerateErrorDocumentation=true`) et définit des erreurs documentées,
donc après un build Release son assembly, à
`FirstClassErrors.Usage/bin/Release/net10.0/FirstClassErrors.Usage.dll`, produit
un catalogue non vide. Deux pièges de nommage à éviter : le fichier de package est
`FirstClassErrors.Cli.<version>.nupkg` (le `PackageId`), pas
`fce.<version>.nupkg`, et `dotnet tool install` prend ce même identifiant de
package, pas le nom de commande `fce`.

## En rapport

- [`release`](workflows/release.fr.md) — le workflow que ceci répète, décrit
  structurellement (déclencheurs, jobs, pièges de sa conception).
- [`release-dryrun`](workflows/release-dryrun.fr.md) — le dry run automatique et
  sans effet de bord qui tourne sur chaque PR et push.
- [Documentation mainteneur](README.fr.md) — l'index de toutes les docs
  mainteneur.
