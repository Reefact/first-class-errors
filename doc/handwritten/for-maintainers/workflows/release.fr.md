# Workflow `release`

🌍 🇬🇧 [English](release.en.md) · 🇫🇷 Français (ce fichier)

> Documentation mainteneur — fait partie de la [référence des workflows](README.fr.md).
> Ne fait pas partie de la documentation utilisateur sous `doc/`.

**Fichier du workflow :** [`.github/workflows/release.yml`](../../../../.github/workflows/release.yml)

## À quoi il sert

`release` construit, atteste et publie les packages NuGet. C'est le seul workflow
dont le chemin complet n'est autrement **jamais exercé avant un vrai tag** —
résolution de version, pack, SBOM, OIDC et permissions d'attestation ne tournent
pour la première fois qu'en conditions de production. Pour rendre tout cela
testable, il joue aussi le rôle de **dry run manuel** qui exécute tout jusqu'à
l'attestation incluse tout en sautant les étapes de publication.

> Pour le versant opérationnel du dry run manuel — comment le lancer, ce qu'il
> touche et quand l'utiliser — voir le guide dédié :
> **[Répétition de release à blanc (« dry run » manuel)](../ReleaseDryRun.fr.md)**.
> Cette page couvre ce que le workflow *est* et les pièges de sa structure.

## Quand il s'exécute

- Sur **push d'un tag de version préfixé par un train** — chaque train de
  release se versionne et se publie indépendamment sur son propre préfixe :
  `lib-v*.*.*` (FirstClassErrors + FirstClassErrors.Testing +
  FirstClassErrors.RequestBinder, en lockstep), `cli-v*.*.*` (l'outil .NET
  `fce`), `dum-v*.*.*` (Dummies). P. ex. `lib-v1.2.3`, `cli-v1.2.3-beta.1`. Un
  push de tag publie. Le mapping des trains vit dans
  [`tools/trains.sh`](../../../../tools/trains.sh) ; le câblage complet est le
  runbook [Ajouter un train de release](../AddingAReleaseTrain.fr.md).
- Sur **`workflow_dispatch`** avec trois entrées : `component` (quel train
  packer/publier : `lib`, `cli` ou `dum`), `version` et `dry_run` (**défaut
  `true`**). Un run manuel ne publie que si `dry_run` est explicitement décoché.

## Étiquettes de pré-version

La version est une chaîne SemVer (le préfixe de train du tag — `lib-v`,
`cli-v`, `dum-v` — est retiré). Une release **stable**
n'a pas d'étiquette (`1.4.2`) ; tout ce qui suit un `-` est une étiquette de
**pré-version**, que le workflow marque comme pre-release sur GitHub — et que nuget.org
liste de la même façon. Étiquettes courantes, du moins mûr au plus mûr :

| Étiquette | Signification |
| - | - |
| `alpha` | Toute première phase — fonctionnalités incomplètes, instable, API pouvant changer du tout au tout. Test interne / adopteurs très précoces ; cassures attendues. |
| `beta` | Périmètre gelé mais encore en stabilisation ; l'API peut bouger à la marge. Ouverte à un public plus large pour retours. |
| `preview` | Le terme « à la .NET » (≈ beta / accès anticipé). L'étiquette que ce projet utilise pour ses previews, p. ex. `0.1.0-preview.1`. |
| `rc` | Release candidate — la version finale sauf blocage ; correctifs critiques uniquement, aucune nouvelle fonctionnalité. Promue stable telle quelle. |
| `nightly` / `dev` / `canary` | Builds automatiques « bleeding edge » (chaque nuit ou chaque commit), non curés. Ne font pas partie du flux de release piloté par tag de ce projet — listés pour référence. |
| `dry` | Pas une vraie release : la convention de ce dépôt pour le placeholder du dry-run (`0.0.0-dry.1`, l'exemple de l'input `version`). Jamais publiée. |

Précédence SemVer : une pré-version se classe **toujours sous** son stable
(`1.0.0-rc.1` < `1.0.0`), et les étiquettes se comparent alphanumériquement, donc
`-preview.1` < `-preview.2`. Sur nuget.org, elles ne s'installent jamais par défaut —
un consommateur doit passer `--prerelease`, d'où le badge README en `nuget/vpre`.

## Comment il s'exécute

Un seul job, `pack-push` : checkout → setup .NET → **résoudre & valider la
version** (le train et le SemVer, depuis le tag ou les entrées) → restore →
build → test → **exiger un bump majeur pour les breaking changes de GenDoc**
(train cli uniquement) → **packer uniquement le train résolu** (via
`tools/packaging/pack.sh`, qui embarque le SBOM SPDX et vérifie les gardes
propres à chaque train) → upload des artefacts → **attester la provenance de
build** → **login NuGet (OIDC)** → **push vers NuGet** → **publier la GitHub
Release** (avec des notes scopées au train via
`tools/packaging/release-notes.sh`, pour qu'une release lib ne liste jamais du
travail cli ou dum). Les deux dernières étapes (et elles seules) sont
désactivées en dry run.

## Permissions & sécurité

Le workflow a besoin de trois périmètres en écriture : `contents: write` (créer la
Release et uploader les assets), `id-token: write` (générer la clé NuGet
éphémère via trusted publishing) et `attestations: write` (stocker la provenance
signée). Ils sont accordés uniquement sur le job `pack-push` ; le token top-level
reste en lecture seule (`contents: read`) — la forme de moindre privilège que
récompense le check Token-Permissions d'OpenSSF Scorecard. Aucun `NUGET_API_KEY`
durable n'est stocké.

## À manipuler avec précaution

Ce workflow encode plusieurs corrections durement acquises. Chacun des points
suivants est délibéré :

- **L'entrée de version est validée contre une allowlist SemVer stricte, lue via
  l'environnement.** Le tag/entrée est contrôlable par un attaquant (un tag comme
  `lib-v1.2.3;id` est une ref valide qui matche le trigger). Il passe par `env:`
  plutôt que d'être interpolé dans le shell, et il est rejeté s'il ne matche pas
  la regex — sinon il pourrait injecter des commandes dans chaque étape qui
  l'utilise.
- **Les métadonnées de build (`+…`) sont rejetées bien que SemVer les autorise.**
  NuGet retire `+build` de l'identité du package, donc `lib-v1.2.3+build5`
  packerait en `1.2.3` ; combiné à `--skip-duplicate` au push, un `1.2.3` déjà publié
  transformerait la release en no-op vert qui ne publie rien. Échouer bruyamment
  est le but.
- **La garde de bump majeur GenDoc tourne avant le pack, sur le train cli
  uniquement — et en dry run aussi.** GenDoc est embarqué dans l'outil fce, donc
  un breaking change de son catalogue d'erreurs est un breaking change du train
  cli : la release refuse de publier tant que la version ne bumpe pas la
  composante majeure par rapport au précédent tag `cli-v`. Répéter la garde en
  dry run attrape un bump majeur oublié avant une vraie tentative de release,
  pas pendant.
- **`Attest build provenance` tourne *avant* les deux publications, et tourne
  même en dry run.** Seuls des artefacts attestés sont jamais publiés ou poussés ;
  et les échecs d'OIDC / de permission d'attestation sont exactement ce que le dry
  run sert à détecter.
- **L'attestation ne correspond volontairement pas à la copie de nuget.org.**
  nuget.org re-signe chaque upload côté dépôt (ajoute un `.signature.p7s` dans le
  `.nupkg`), changeant le checksum. Les octets attestés sont donc publiés comme
  **assets de GitHub Release**, et les consommateurs vérifient la provenance
  contre *ceux-là* avec `gh attestation verify` — la copie de nuget.org se
  vérifie avec `dotnet nuget verify`. Ne « simplifiez » pas en attestant
  seulement la copie de nuget.org.
- **`NuGet login (OIDC)` tourne à chaque trigger, dry run inclus — seuls le push
  et la Release sont conditionnés.** L'échange de token est ce qui valide la
  policy trusted-publishing, donc un dry run échoue (rouge) quand la policy ou
  `NUGET_USER` est absent. Il génère une clé à usage unique que le dry run ne
  dépense jamais. Nécessite une policy trusted-publishing sur nuget.org et le
  secret `NUGET_USER` (le **nom d'utilisateur** du profil, pas l'e-mail).
- **L'étape Release épingle `--target "$GITHUB_SHA"`.** Sur `workflow_dispatch` le
  tag n'existe pas encore et `gh` le créerait sinon depuis le dernier état de la
  branche par défaut ; épingler le SHA lie le tag, l'archive source et les
  packages attestés au commit que ce job a réellement construit. Le repli
  `|| … upload --clobber` garde un re-run idempotent.
- **`concurrency` met `cancel-in-progress: false`.** Ne jamais annuler une
  publication à moitié faite.
- **La GitHub Release est marquée `--prerelease` quand la version porte une
  étiquette de pré-version SemVer** (tout `-…`, p. ex. `-preview.1`, `-beta.1`,
  `-rc.1`), pour qu'une preview n'apparaisse jamais comme la release « Latest »
  du dépôt. Les métadonnées de build (`+…`) étant rejetées en amont, un `-` est
  sans ambiguïté le marqueur de pré-version — c'est ainsi que nuget.org liste
  le même package.

## En rapport

- [Répétition de release à blanc (« dry run » manuel)](../ReleaseDryRun.fr.md) —
  le guide opérationnel.
- [`release-dryrun`](release-dryrun.fr.md) — la répétition automatique et sans
  effet de bord qui tourne sur chaque PR et push, partageant le même `pack.sh`.
- [Ajouter un train de release](../AddingAReleaseTrain.fr.md) — comment les
  préfixes de tag, les scopes et les branches de pack des trains sont câblés.
- La section **Supply chain** du README documente comment un consommateur vérifie
  la provenance et le SBOM que ce workflow produit.
