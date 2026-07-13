# Workflow `commit-lint`

🌍 🇬🇧 [English](commit-lint.en.md) · 🇫🇷 Français (ce fichier)

> Documentation mainteneur — fait partie de la [référence des workflows](README.fr.md).
> Ne fait pas partie de la documentation utilisateur sous `doc/`.

**Fichier du workflow :** [`.github/workflows/commit-lint.yml`](../../.github/workflows/commit-lint.yml)

## À quoi il sert

Il impose la convention de message de commit du dépôt (Conventional Commits,
telle que spécifiée dans [`CONTRIBUTING.fr.md`](../../doc/CONTRIBUTING.fr.md)) sur **chaque
commit non-merge d'une pull request**. C'est le filet de sécurité côté serveur du
hook local `commit-msg` : un contributeur qui contourne le hook avec
`git commit --no-verify` est quand même rattrapé ici.

Le hook et ce workflow appellent le **même script**,
[`tools/commit-lint/lint-commit-message.sh`](../../tools/commit-lint/lint-commit-message.sh),
donc il existe exactement une définition de « message de commit valide » et la CI
ne peut jamais diverger du contrôle local.

## Quand il s'exécute

- À chaque **pull request** (quelle que soit la branche de base).

## Comment il s'exécute

Un seul job, `Conventional commits` :

1. Checkout avec **`fetch-depth: 0`** — l'historique complet est nécessaire pour
   énumérer les commits de la PR.
2. Énumérer les commits non-merge de `base..head` (`git rev-list --no-merges`) et
   passer chaque message au script de lint avec `--ci`. Les commits de merge sont
   générés par GitHub et sont exemptés.
3. Si un commit échoue, le job affiche lesquels et sort en non-zéro, en orientant
   le contributeur vers un rebase interactif pour les corriger.

## Permissions & sécurité

`contents: read` seulement.

## À manipuler avec précaution

- **Le script est partagé avec le hook local — changez la règle à un seul
  endroit.** Si vous ajustez les types/scopes acceptés, éditez
  `tools/commit-lint/lint-commit-message.sh` ; le hook comme ce workflow le
  reprennent. N'encodez pas une seconde règle divergente dans le workflow.
- **`--no-merges` est intentionnel.** Les commits de merge de GitHub ne suivent
  pas la convention et doivent rester exemptés.
- **`fetch-depth: 0` est requis.** Un checkout superficiel ne contiendrait pas la
  plage de commits de la PR ; le lint passerait alors silencieusement sur rien.
- **Ce check n'aide que s'il est *required*.** Comme les autres checks de
  qualité, il ne bloque un merge que lorsque la protection de branche sur `main`
  le marque **required**.

## En rapport

- [`CONTRIBUTING.fr.md`](../../doc/CONTRIBUTING.fr.md) — les conventions de commit et de PR
  que ce workflow impose. Activez le hook local une fois par clone avec
  `git config core.hooksPath .githooks`.
