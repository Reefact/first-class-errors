# Référence des workflows CI/CD

🌍 🇬🇧 [English](README.md) · 🇫🇷 Français (ce fichier)

↑ Fait partie de la [documentation mainteneur](../README.fr.md).

> Documentation mainteneur. Elle décrit les workflows GitHub Actions qui
> construisent, vérifient et publient FirstClassErrors. Elle ne fait **pas**
> partie de la documentation utilisateur de la bibliothèque, sous `doc/`.

## De quoi il s'agit

Chaque workflow sous [`.github/workflows/`](../../../../.github/workflows/) porte une
intention qu'il est facile de casser en « faisant le ménage » : une permission
volontairement étroite, un ordre d'étapes qui protège d'une panne précise, une
version gelée pour une raison produit. Les fichiers de workflow eux-mêmes
contiennent le raisonnement ligne à ligne dans leurs commentaires — ces
commentaires sont la source de vérité la plus proche du code. **Ces pages sont la
couche pédagogique au-dessus :** à quoi sert chaque workflow, quand et comment il
s'exécute, et les quelques points qu'il ne faut pas modifier sans en comprendre
la raison.

Lisez la page d'un workflow avant d'y toucher. Si la page et le YAML se
contredisent, c'est le YAML qui fait foi — et la page doit être corrigée.

## Les conventions transverses

Quelques décisions sont partagées par (presque) tous les workflows. Elles sont
documentées une seule fois ici plutôt que répétées sur chaque page.

- **Les actions sont épinglées par SHA de commit, pas par tag.** Un tag comme
  `@v4` peut être déplacé par son propriétaire vers du nouveau code ; un SHA de
  40 caractères hexadécimaux, non. Chaque `uses:` épingle donc un SHA avec le tag
  lisible en commentaire de fin (`# v4`). Quand vous montez une action, changez
  **les deux**. L'écosystème `github-actions` de Dependabot propose ces montées.
- **Les `permissions:` partent en lecture seule et s'élargissent par job.** Le
  bloc au niveau du workflow est le moindre privilège nécessaire (en général
  `contents: read`) ; un job qui doit écrire quelque chose (uploader un SARIF,
  publier une release, activer l'auto-merge) redéclare un bloc `permissions:` qui
  ajoute *uniquement* ce périmètre. N'élargissez jamais le bloc de haut niveau
  pour satisfaire un seul job.
- **Chaque job fixe `timeout-minutes`.** Le défaut GitHub est de six heures ; une
  étape bloquée retiendrait sinon un runner tout ce temps. Chaque plafond est
  fixé à quelques fois le temps observé, noté en commentaire à côté.
- **`concurrency` annule les runs remplacés.** Pousser deux fois sur la même
  branche ou PR annule le run en cours. La seule exception est `release`, qui met
  `cancel-in-progress: false` — on ne veut jamais annuler une publication à
  moitié faite.
- **Les scanners de sécurité tournent aussi chaque semaine via `schedule`.**
  `codeql` et `scorecard` se relancent sur du code inchangé pour que les
  requêtes/checks nouvellement livrés soient appliqués même sans push.
- **Les forks ne peuvent pas lire les secrets.** Les workflows qui ont besoin
  d'un secret (p. ex. `sonar`) détectent une PR issue d'un fork et s'abstiennent
  au lieu d'échouer ; GitHub n'expose pas les secrets du dépôt à une PR ouverte
  depuis un fork.
- **Ce sont les checks *required* qui font barrage.** Plusieurs workflows
  (`dependency-review`, `dependabot-automerge`) ne font que *signaler* ou
  *activer* — ils ne mergent rien seuls. Ce qui bloque réellement un mauvais
  merge, c'est la configuration de protection de branche / ruleset sur `main` qui
  marque ces checks comme **required**. C'est un réglage de dépôt, pas quelque
  chose qu'un workflow peut imposer pour lui-même.

## Les workflows

### Build & qualité

| Workflow | Rôle |
| --- | --- |
| [`ci`](ci.fr.md) | Construit et teste toute la solution sous Linux et Windows, avec couverture. Le barrage principal. |
| [`sonar`](sonar.fr.md) | Analyse SonarQube Cloud — quality gate et remontée de couverture. |
| [`analyzers`](analyzers.fr.md) | Dogfood des analyzers Roslyn embarqués, y compris sur le plus vieux compilateur supporté (le floor Roslyn). |
| [`commit-lint`](commit-lint.fr.md) | Impose la convention Conventional Commits sur chaque commit de PR, via le même script que le hook local. |
| [`adr-check`](adr-check.fr.md) | Consultatif, dispatch manuel : confronte une branche à la base d'ADR (nouvelle décision / remplacement / conflit). Le repli pour les contributeurs sans Claude Code ; ne bloque jamais. |

### Sécurité & chaîne d'approvisionnement

| Workflow | Rôle |
| --- | --- |
| [`codeql`](codeql.fr.md) | Analyse statique GitHub CodeQL pour C#, résultats sur le tableau de bord code-scanning. |
| [`dependency-review`](dependency-review.fr.md) | Bloque une PR qui introduit une dépendance vulnérable connue. |
| [`scorecard`](scorecard.fr.md) | OpenSSF Scorecard — note la posture de sécurité du dépôt et alimente le badge du README. |

### Release

| Workflow | Rôle |
| --- | --- |
| [`release`](release.fr.md) | Construit, atteste et publie les packages NuGet sur un tag de version (avec un dry run manuel). |
| [`release-dryrun`](release-dryrun.fr.md) | Répète en continu la partie sans effet de bord de la release (pack + SBOM) sur chaque PR et push. |
| [`changelog`](changelog.fr.md) | Rédige la section `[Unreleased]` du changelog d'un train à partir des PR mergées, sur déclenchement manuel, et ouvre une PR de relecture. |

### Maintenance des dépendances

| Workflow | Rôle |
| --- | --- |
| [`dependabot-automerge`](dependabot-automerge.fr.md) | Active l'auto-merge sur les mises à jour patch/minor de Dependabot ; laisse les majeures à un humain. |

## Docs mainteneur en rapport

- [Répétition de release à blanc (« dry run » manuel)](../ReleaseDryRun.fr.md) —
  le guide opérationnel du dry run manuel via le dispatch `release`.
- [ADR 0001 — Verrouiller le floor Roslyn de l'analyzer](../adr/0001-lock-the-analyzer-roslyn-floor.md)
  — pourquoi la version de Roslyn de l'analyzer est gelée, ce que le workflow
  `analyzers` fait respecter. *(Rédigé en anglais.)*
- [`CONTRIBUTING.fr.md`](../../for-users/CONTRIBUTING.fr.md) — les conventions de commit et de PR
  que le workflow `commit-lint` vérifie.
