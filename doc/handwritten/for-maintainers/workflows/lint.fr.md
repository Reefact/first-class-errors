# Workflow `lint`

🌍 🇫🇷 Français (ce fichier) · 🇬🇧 [English](lint.en.md)

> Documentation mainteneur — fait partie de la [référence des workflows](README.fr.md).
> Ne fait pas partie de la documentation utilisateur sous `doc/`.

**Fichier du workflow :** [`.github/workflows/lint.yml`](../../../../.github/workflows/lint.yml)

## À quoi il sert

Il analyse statiquement les fichiers que le compilateur C# ne voit jamais : les
scripts shell POSIX sous `tools/` et `.claude/hooks/`, et les définitions de
workflow de `.github/workflows/` elles-mêmes.

Toute autre analyse de ce dépôt tourne **dans une compilation** — les analyseurs
Roslyn, la règle du type explicite redite dans `.editorconfig` par l'ADR-0055, le
ratchet de warnings de `Directory.Build.props` — si bien qu'un contributeur la
rencontre au moment où il écrit le code. Le shell et le YAML n'avaient pas ce
moment. La seule chose qui les lisait était l'analyse [`sonar`](sonar.fr.md), qui
rapporte **après** le merge et n'applique rien : son job est vert dès que
l'analyse est téléversée, quoi que dise le Quality Gate. Deux constats typés
VULNERABILITY ont atteint `main` par ce chemin, et 21 constats shell s'y sont
accumulés sans être vus.

Ce workflow referme ce trou avec des outils qui tournent sur nos propres
*runners* : le signal arrive avant le merge et ne dépend pas de la disponibilité
d'un service tiers.

## Quand il tourne

- À chaque **push sur `main`**.
- À chaque **pull request visant `main`**.
- À la demande via **`workflow_dispatch`**.

## Comment il tourne

Un job, `Lint scripts and workflows`, sous Linux :

1. **shellcheck** sur chaque `*.sh` du dépôt. Il est préinstallé sur l'image du
   *runner* : rien à télécharger, aucune action tierce dans la chaîne
   d'approvisionnement.
2. **actionlint** sur `.github/workflows/`. Il vérifie ce que le YAML seul ne
   peut pas : le typage des expressions `${{ }}`, les entrées d'actions face au
   schéma de chaque action, les références `needs` et matrice, la syntaxe cron
   et — via un shellcheck embarqué — le shell de chaque bloc `run:`.

## Permissions & sécurité

`contents: read`, déclaré **sur le job** plutôt qu'au niveau du workflow, pour
qu'un job ajouté plus tard n'hérite de rien qu'il n'ait demandé (c'est la règle
Sonar `githubactions:S8264`, et la raison pour laquelle les deux workflows de
mutation ont été modifiés de la même façon).

actionlint est récupéré comme **archive de version épinglée et vérifiée par
SHA-256**, et non exécuté via une action tierce : une action non épinglée est
précisément ce que le contrôle Pinned-Dependencies d'OpenSSF Scorecard retient
contre ce dépôt. La version et l'empreinte se suivent dans le workflow et se
mettent à jour ensemble.

## À manier avec précaution

- **La barre est à zéro constat, `info` compris.** L'arbre est propre à cette
  barre : tout nouveau constat est donc réellement nouveau. Une barre plus basse
  laisserait les `info` s'accumuler exactement comme dans le rapport Sonar — ce
  que ce workflow existe pour empêcher, non pour reproduire.
- **Les faux positifs sont annotés sur place, jamais désactivés globalement.**
  Trois motifs sont tus par un `# shellcheck disable=` en ligne portant sa
  raison : `SC2016` là où un format `printf` contient des *backticks* Markdown
  (lus comme une substitution de commande), et `SC2317` sur les deux fonctions de
  *hook* atteintes par la répartition `"rule_${rule}"` que shellcheck ne sait pas
  suivre. Un `.shellcheckrc` à l'échelle du dépôt aveuglerait ces règles partout,
  y compris là où elles ont raison.
- **Les scripts sont en `#!/bin/sh`, et shellcheck applique le dialecte POSIX.**
  C'est délibéré : `local`, les tableaux et `[[` ne sont pas disponibles sur les
  shells qui les exécutent, et les règles POSIX auxquelles ces scripts sont tenus
  sont une décision consignée (ADR-0060).
- **actionlint audite la correction, pas la posture de sécurité.** Il ne signale
  ni permissions trop larges, ni vérification d'acteur usurpable, ni déclencheur
  dangereux — la classe même qui a produit les deux constats VULNERABILITY de ce
  dépôt. Un auditeur dédié (`zizmor`) couvre cela et relève d'une décision à
  part, que ce workflow ne fournit pas en douce.
- **Ce contrôle ne sert que s'il est requis.** Comme les autres contrôles de
  qualité, il ne bloque un merge que si la protection de branche de `main` le
  marque **required**.

## Voir aussi

- [`sonar`](sonar.fr.md) — l'analyse que ce workflow ramène en amont. Elle reste
  la vue de rapport et de couverture ; elle n'est pas, et n'a jamais été, un
  garde-fou.
- [`ci`](ci.fr.md) — là où le ratchet de warnings applique la barre équivalente
  côté C#.
