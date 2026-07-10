# Workflow `dependency-review`

🌍 🇬🇧 [English](dependency-review.en.md) · 🇫🇷 Français (ce fichier)

> Documentation mainteneur — fait partie de la [référence des workflows](README.fr.md).
> Ne fait pas partie de la documentation utilisateur sous `doc/`.

**Fichier du workflow :** [`.github/workflows/dependency-review.yml`](../../.github/workflows/dependency-review.yml)

## À quoi il sert

`dependency-review` bloque une pull request qui **introduit une dépendance
vulnérable connue**. Il compare le graphe de dépendances base-vs-head et fait
échouer la PR si un changement tire un package portant un avis de sécurité à la
sévérité configurée ou au-dessus.

C'est le complément **au moment de la PR** de Dependabot : Dependabot ne réagit
qu'*après* qu'une dépendance vulnérable est déjà sur `main`, alors que celui-ci
attrape le problème au moment où une PR l'ajouterait — exactement quand il est le
moins coûteux à corriger.

## Quand il s'exécute

- À chaque **pull request visant `main`**.

## Comment il s'exécute

Un seul job, `review` : checkout, puis exécution de
`actions/dependency-review-action` avec `fail-on-severity: moderate`.

## Permissions & sécurité

`contents: read` seulement — l'action lit le graphe de dépendances du dépôt. Elle
ne poste **aucun** commentaire de PR (cela demanderait `pull-requests: write`) ;
le check en échec est le signal.

## À manipuler avec précaution

- **Il exige que le Dependency graph du dépôt soit activé.** C'est un réglage de
  dépôt GitHub, pas quelque chose que le workflow peut activer. S'il est éteint,
  l'action échoue avec *« Dependency review is not supported on this repository…
  ensure that Dependency graph is enabled »* — c'est une erreur de configuration,
  pas un bug de workflow. (Sur un dépôt privé, il faut aussi Advanced Security.)
- **Il ne voit que les changements apportés par la PR.** Une CVE publiée pendant
  la nuit contre une dépendance *existante* ne fait pas échouer ce check — cela
  reste un warning via l'audit NuGet (voir
  [`Directory.Build.props`](../../Directory.Build.props)). Ce workflow ne bloque
  qu'au point d'introduction, à dessein.
- **`fail-on-severity: moderate` est le bouton de réglage.** Abaissez-le à `low`
  pour être plus strict, montez-le à `high` pour être plus laxiste. Comme il
  n'inspecte que les changements de dépendances de la PR, `moderate` est un vrai
  barrage plutôt que du bruit.
- **Il ne fait barrage que s'il est *required*.** Comme les autres checks, il
  doit être marqué **required** dans la protection de branche sur `main` pour
  réellement bloquer un merge.

## En rapport

- [`codeql`](codeql.fr.md) — le scanner de sécurité côté code.
- [`dependabot-automerge`](dependabot-automerge.fr.md) — le versant mise à jour de
  dépendances après merge.
