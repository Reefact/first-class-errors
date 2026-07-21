# Workflow `dependabot-automerge`

🌍 🇬🇧 [English](dependabot-automerge.en.md) · 🇫🇷 Français (ce fichier)

> Documentation mainteneur — fait partie de la [référence des workflows](README.fr.md).
> Ne fait pas partie de la documentation utilisateur sous `doc/`.

**Fichier du workflow :** [`.github/workflows/dependabot-automerge.yml`](../../../../.github/workflows/dependabot-automerge.yml)

## À quoi il sert

Pour les pull requests de Dependabot, ce workflow **active l'auto-merge de GitHub
sur les mises à jour patch et minor**, pour qu'elles se mergent d'elles-mêmes une
fois les checks required passés. Les mises à jour **majeures** sont
volontairement laissées telles quelles, en attente d'une revue humaine. C'est la
voie à faible friction de la politique de mise à jour des dépendances : les
montées de routine n'ont pas besoin d'un humain, les risquées si.

La configuration de Dependabot elle-même (quels écosystèmes, planning, packages
ignorés) vit dans [`.github/dependabot.yml`](../../../../.github/dependabot.yml), pas
ici.

## Quand il s'exécute

- À chaque **pull request visant `main`**, mais le job est conditionné à
  `github.actor == 'dependabot[bot]'`, donc il n'agit que sur les PR de
  Dependabot.

## Comment il s'exécute

Un seul job, `automerge` :

1. `dependabot/fetch-metadata` lit le type de mise à jour (patch / minor / major).
2. Pour les mises à jour **patch ou minor**, `gh pr merge --auto` active
   l'auto-merge. Les majeures ne passent pas la condition et restent ouvertes.

## Permissions & sécurité

Défaut du workflow `contents: read` ; le job élargit à `contents: write` et
`pull-requests: write` — les périmètres nécessaires pour activer l'auto-merge sur
la PR.

## À manipuler avec précaution

- **Ce workflow ne fait qu'*activer* l'auto-merge ; il ne décide pas quand
  merger.** GitHub ne merge la PR qu'une fois les checks de statut **required**
  de la branche passés. **Sans une règle de protection de branche sur `main` qui
  marque les checks CI comme required, l'auto-merge mergerait immédiatement** —
  avant la CI. Les checks required sont le garde-fou de sécurité, pas ce workflow.
  C'est le point le plus important à comprendre avant de s'y fier.
- **L'exclusion des `major` est intentionnelle.** Seuls `semver-patch` et
  `semver-minor` obtiennent l'auto-merge ; les majeures sont laissées à un humain
  parce que ce sont elles qui risquent le plus de casser. N'élargissez pas la
  condition aux majeures.
- **Le garde-fou sur l'acteur compte.** `if: github.actor == 'dependabot[bot]'`
  empêche le chemin élevé `contents: write` / `pull-requests: write` de tourner
  sur des PR humaines.

## En rapport

- [`dependabot-autofix`](dependabot-autofix.fr.md) — le compagnon de diagnostic :
  quand une PR Dependabot reste rouge, il trie pourquoi et commente un correctif
  prêt à appliquer.
- [`.github/dependabot.yml`](../../../../.github/dependabot.yml) — ce que Dependabot met
  à jour et ce qu'il ignore (p. ex. les `Microsoft.CodeAnalysis.*` gelés ; voir
  [`analyzers`](analyzers.fr.md)).
- [`dependency-review`](dependency-review.fr.md) — le barrage de vulnérabilité au
  moment de la PR, par lequel une PR Dependabot passe aussi.
