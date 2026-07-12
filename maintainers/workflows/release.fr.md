# Workflow `release`

🌍 🇬🇧 [English](release.en.md) · 🇫🇷 Français (ce fichier)

> Documentation mainteneur — fait partie de la [référence des workflows](README.fr.md).
> Ne fait pas partie de la documentation utilisateur sous `doc/`.

**Fichier du workflow :** [`.github/workflows/release.yml`](../../.github/workflows/release.yml)

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

- Sur **push d'un tag de version** `v*.*.*` (p. ex. `v1.2.3`, `v1.2.3-beta.1`) —
  cela publie.
- Sur **`workflow_dispatch`** avec deux entrées : `version` et `dry_run` (**défaut
  `true`**). Un run manuel ne publie que si `dry_run` est explicitement décoché.

## Comment il s'exécute

Un seul job, `pack-push` : checkout → setup .NET → **résoudre & valider la
version** → restore → build → test → **pack** (via `tools/packaging/pack.sh`, qui
embarque le SBOM SPDX) → upload des artefacts → **attester la provenance de
build** → **login NuGet (OIDC)** → **push vers NuGet** → **publier la GitHub
Release**. Les deux dernières étapes (et elles seules) sont désactivées en dry
run.

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
  `v1.2.3;id` est une ref valide qui matche le trigger). Il passe par `env:`
  plutôt que d'être interpolé dans le shell, et il est rejeté s'il ne matche pas
  la regex — sinon il pourrait injecter des commandes dans chaque étape qui
  l'utilise.
- **Les métadonnées de build (`+…`) sont rejetées bien que SemVer les autorise.**
  NuGet retire `+build` de l'identité du package, donc `v1.2.3+build5` packerait
  en `1.2.3` ; combiné à `--skip-duplicate` au push, un `1.2.3` déjà publié
  transformerait la release en no-op vert qui ne publie rien. Échouer bruyamment
  est le but.
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
- La section **Supply chain** du README documente comment un consommateur vérifie
  la provenance et le SBOM que ce workflow produit.
