# Workflow `changelog`

🌍 🇬🇧 [English](changelog.en.md) · 🇫🇷 Français (ce fichier)

> Documentation mainteneur — fait partie de la [référence des workflows](README.fr.md).
> Ne fait pas partie de la documentation utilisateur sous `doc/`.

**Fichier du workflow :** [`.github/workflows/changelog.yml`](../../.github/workflows/changelog.yml)

## À quoi il sert

`changelog` rédige la section `[Unreleased]` du changelog d'un train de release à
partir des pull requests mergées depuis le dernier tag de ce train, puis ouvre une
pull request avec le résultat. Il ne publie jamais rien et n'écrit jamais dans une
section déjà publiée — il produit un **brouillon qu'un humain relit et merge**.

Il comble un manque que le chemin [`release`](release.fr.md) laisse ouvert.
[`tools/packaging/release-notes.sh`](../../tools/packaging/release-notes.sh)
génère déjà le corps de la GitHub Release, mais ces notes sont **orientées commits
et brutes** (« `- feat(core): … (abc1234)` »), cantonnées à un train. Ce workflow
produit l'artefact complémentaire : un **changelog narratif, orienté utilisateur**,
au format [Keep a Changelog](https://keepachangelog.com/), groupé par nature de
changement (Breaking / Added / Changed / Fixed / Deprecated), écrit pour le
développeur qui consomme le package depuis NuGet.

Les deux trains sont versionnés indépendamment et gardent des fichiers de
changelog **distincts** :

| Train | Scopes | Fichier de changelog |
| --- | --- | --- |
| `lib` | `core`, `analyzers`, `testing` | [`CHANGELOG.md`](../../CHANGELOG.md) |
| `cli` | `cli`, `gendoc` | [`FirstClassErrors.Cli/CHANGELOG.md`](../../FirstClassErrors.Cli/CHANGELOG.md) |

## Quand il s'exécute

- Sur **`workflow_dispatch`** uniquement. Il doit être déclenché à la main depuis
  l'onglet Actions — il n'y a aucun déclencheur automatique, parce que réécrire un
  changelog est un acte éditorial relu par un humain, pas quelque chose à lancer à
  chaque merge.

Deux entrées :

- **`component`** (`lib` | `cli`, requis) — pour quel train rédiger.
- **`from_ref`** (optionnel) — le tag précédent à partir duquel différ. Vide, le
  dernier tag du train est détecté automatiquement ; si le train n'a pas encore de
  tag, tout l'historique est pris.

## Comment il s'exécute

Un seul job, `draft-changelog` :

1. Checkout avec **`fetch-depth: 0`** — le tag précédent du train (et l'horodatage
   de son commit, la borne basse de la plage de pull requests) doit se résoudre.
2. **Collecter** les pull requests du train avec
   [`tools/changelog/collect-prs.sh`](../../tools/changelog/collect-prs.sh) :
   `gh pr list` rassemble les candidates mergées dans `main` après l'heure du
   commit taggé, puis chaque candidate n'est conservée que si l'un des **scopes
   Conventional Commit de ses commits** tombe dans l'ensemble du train. C'est la
   **même partition que `release-notes.sh`** — le changelog et les notes de
   release décrivent donc le même ensemble de changements, et les PR d'infra sans
   scope (bare `ci:` / `chore:` / `docs:`) n'appartiennent à aucun train et sont
   écartées des deux.
3. **Rédiger** l'entrée : les PR mergées (numéro, titre, corps, labels, auteur)
   sont envoyées à l'API Anthropic sous
   [`.github/changelog-prompt.md`](../../.github/changelog-prompt.md), qui demande
   au modèle de grouper les changements et de **n'inventer rien**.
4. **Fusionner** le bloc rédigé dans le fichier de changelog du train avec
   [`tools/changelog/merge-unreleased.sh`](../../tools/changelog/merge-unreleased.sh),
   qui **remplace** la section `[Unreleased]` sur place.
5. **Ouvrir** (ou rafraîchir) une pull request depuis
   `chore/changelog-<component>-draft` via `gh`, pour relecture.

Si aucune PR du train n'est trouvée, le job s'arrête après l'étape 2 sans rien
ouvrir.

## Permissions & sécurité

Le token de haut niveau est en lecture seule (`contents: read`). Le job
`draft-changelog` n'ajoute que `contents: write` (pousser la branche brouillon) et
`pull-requests: write` (ouvrir la PR de relecture) — le moindre privilège que
récompense OpenSSF Scorecard. La pull request est ouverte avec `gh` (préinstallé
sur le runner), donc aucune action tierce n'est épinglée pour cela.

**Secret :** l'étape de rédaction a besoin de **`ANTHROPIC_API_KEY`** (secret du
dépôt). L'étape échoue avec un message explicite s'il manque. Comme le seul
déclencheur est `workflow_dispatch` — disponible pour les comptes ayant l'accès en
écriture, jamais pour une pull request issue d'un fork — la clé n'est jamais
exposée aux PR de forks.

## À manipuler avec précaution

- **La partition par train vit à un seul endroit : [`tools/trains.sh`](../../tools/trains.sh).**
  `collect-prs.sh`, `release-notes.sh` et ce workflow le *sourcent* tous, donc le
  changelog et les notes de release GitHub ne peuvent jamais diverger sur quels
  scopes appartiennent à quel train (`lib` → core/analyzers/testing, `cli` →
  cli/gendoc). Ajoutez un train, ou déplacez un scope entre trains, **là** — pas
  dans les scripts. Un train entièrement nouveau demande aussi les édits statiques
  que GitHub impose (trigger de tag, options du choix, scopes du commit-lint) :
  suivez [Ajouter un train de release](../AddingAReleaseTrain.fr.md).
- **Le bloc `[Unreleased]` est *remplacé*, jamais préfixé.** `merge-unreleased.sh`
  est propriétaire du bloc : à chaque run il échange toute la section
  `## [Unreleased]` contre celle fraîchement rédigée et laisse intactes les
  sections publiées `## [x.y.z]`. C'est ce qui rend un re-run idempotent — préfixer
  au lieu de remplacer empilerait des titres `[Unreleased]` en double. L'entrée
  rédigée doit commencer par `## [Unreleased]` ; le workflow coupe tout ce que le
  modèle émettrait avant, pour que le remplacement ne puisse jamais accumuler de
  texte parasite.
- **La relecture humaine *est* le mécanisme de sûreté.** Le prompt reçoit la
  consigne de n'inventer rien, mais un modèle peut quand même déduire un bénéfice
  qu'une PR n'a jamais énoncé. C'est pourquoi le workflow ouvre une PR plutôt que
  de committer sur `main` : relisez l'entrée face aux PR réelles avant de merger,
  et supprimez tout ce qui a été déduit plutôt que trouvé. Ne branchez pas ceci
  sur l'auto-merge.
- **Un brouillon tronqué ou refusé fait échouer le run — il n'ouvre pas de PR
  partielle.** L'étape de rédaction inspecte la réponse de l'API et sort en
  non-zéro sur une erreur API, un `refusal` ou une troncature `max_tokens`, plutôt
  que de fusionner un demi-changelog. Si vous atteignez `max_tokens`, augmentez-le
  ou resserrez la plage avec `from_ref`.
- **Le texte non-fiable des PR est traité comme de la donnée.** Titres et corps de
  PR viennent de contributeurs. Ils sont échappés en JSON avec `jq --arg`,
  enveloppés dans des délimiteurs `<context>` / `<pull_requests>`, et le prompt
  reçoit la consigne de les traiter comme des données, pas des instructions.
  L'entrée libre `from_ref` transite par l'environnement et n'est jamais remise
  qu'à `git log`, jamais interpolée dans une commande shell.
- **`claude-sonnet-5` est un alias flottant.** Il se résout vers le snapshot
  Sonnet 5 courant — souhaitable pour un rédacteur (on veut le plus récent), mais
  cela signifie que la sortie peut évoluer dans le temps. La relecture humaine
  absorbe cela ; ne considérez pas le brouillon comme reproductible.
- **Il n'apparaît dans l'onglet Actions qu'une fois sur la branche par défaut.**
  GitHub ne liste un workflow `workflow_dispatch` que depuis la branche par
  défaut, donc une modification de ce fichier n'est déclenchable qu'après son
  merge sur `main`.

## En rapport

- [`release`](release.fr.md) — la publication pilotée par tag, dont le
  `release-notes.sh` produit le corps de GitHub Release brut et orienté commits que
  ce changelog complète.
- [`CONTRIBUTING.fr.md`](../../doc/CONTRIBUTING.fr.md) — les **scopes** Conventional
  Commit sur lesquels repose la partition par train.
- Les deux fichiers de changelog : [`CHANGELOG.md`](../../CHANGELOG.md) (lib) et
  [`FirstClassErrors.Cli/CHANGELOG.md`](../../FirstClassErrors.Cli/CHANGELOG.md)
  (cli).
