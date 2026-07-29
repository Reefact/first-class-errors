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
  `github.event.pull_request.user.login == 'dependabot[bot]'` — l'**auteur** de
  la pull request — donc il n'agit que sur les PR de Dependabot. Ce n'est
  délibérément pas `github.actor` ; voir *À manipuler avec précaution*.

## Comment il s'exécute

Un seul job, `automerge` :

1. `dependabot/fetch-metadata` lit le type de mise à jour (patch / minor / major).
2. Le **commit de tête de cet événement** est inspecté et classé : commit signé
   par GitHub appartenant à Dependabot, commit de Dependabot non signé, ou
   étranger.
3. Pour une tête **signée** et une mise à jour **patch ou minor**,
   `gh pr merge --auto` active l'auto-merge. Les majeures ne passent pas la
   condition et restent ouvertes.
4. Pour une tête **étrangère**, l'auto-merge est **retiré** (`--disable-auto`).
   Une tête de Dependabot non signée — ce que laisse `dependabot-autofix` après
   une réécriture ou un rebase — est laissée telle quelle.

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
- **Le garde-fou porte sur l'AUTEUR de la pull request, et ne doit pas revenir à
  `github.actor`.** Les deux empêchent le chemin élevé `contents: write` /
  `pull-requests: write` de tourner sur des PR humaines, mais `github.actor`
  nomme celui qui a déclenché le run : un push par quelqu'un d'autre faisait donc
  *sauter* le job. Or l'auto-merge survit aux pushes suivants sur la branche de
  tête ; sauter le laissait donc armé sur une tête que personne ne revérifiait.
  L'auteur d'une pull request ne change jamais, si bien que le job tourne
  désormais à chaque événement d'une PR Dependabot — c'est-à-dire précisément
  quand il doit agir.
- **Les deux gardes sur la tête sont asymétriques à dessein.** *Armer* exige le
  commit signé par GitHub appartenant à Dependabot : les noms d'auteur de commit
  sont des valeurs `git config` et se forgent librement, la signature de GitHub
  non. *Retirer* se déclenche sur le signal faible — un auteur qui n'est pas
  Dependabot — parce que retirer est le sens sûr ; au pire un humain merge à la
  main. Ne « rangez » pas cela en une vérification symétrique : conditionner le
  retrait à la signature entrerait en conflit avec
  [`dependabot-autofix`](dependabot-autofix.fr.md), dont les `--amend` et
  `rebase` conservent Dependabot comme auteur mais perdent la signature, et qui
  garde délibérément l'auto-merge après un correctif trivial.
- **`dependabot/fetch-metadata` est une seconde barrière, mais pas celle-ci.**
  Elle revérifie l'auteur de la PR, l'auteur du **premier** commit et la
  signature de ce commit, ne consulte jamais `github.actor`, et échoue fermé en
  n'émettant aucune sortie (ses deux entrées `skip-*-verification` valent `false`
  par défaut). Ce qu'elle ne vérifie pas, c'est la **tête** — et c'est la tête que
  l'auto-merge merge.

## En rapport

- [`dependabot-autofix`](dependabot-autofix.fr.md) — le compagnon de diagnostic :
  quand une PR Dependabot reste rouge, il trie pourquoi et commente un correctif
  prêt à appliquer.
- [`.github/dependabot.yml`](../../../../.github/dependabot.yml) — ce que Dependabot met
  à jour et ce qu'il ignore (p. ex. les `Microsoft.CodeAnalysis.*` gelés ; voir
  [`analyzers`](analyzers.fr.md)).
- [`dependency-review`](dependency-review.fr.md) — le barrage de vulnérabilité au
  moment de la PR, par lequel une PR Dependabot passe aussi.
