# Workflow `scorecard`

🌍 🇬🇧 [English](scorecard.en.md) · 🇫🇷 Français (ce fichier)

> Documentation mainteneur — fait partie de la [référence des workflows](README.fr.md).
> Ne fait pas partie de la documentation utilisateur sous `doc/`.

**Fichier du workflow :** [`.github/workflows/scorecard.yml`](../../.github/workflows/scorecard.yml)

## À quoi il sert

`scorecard` exécute [OpenSSF Scorecard](https://securityscorecards.dev), qui note
la **posture de sécurité** du dépôt selon un ensemble de checks automatisés —
actions épinglées, permissions des tokens, protection de branche, releases
signées, outillage de mise à jour des dépendances, et plus. Il remonte les
résultats au tableau de bord code-scanning et **publie le score sur
securityscorecards.dev**, ce qui alimente le badge OpenSSF Scorecard du README.

Là où `codeql` note le *code* et `dependency-review` note les *dépendances*,
Scorecard note les *pratiques du projet* — dont plusieurs des conventions mêmes
que les autres workflows mettent en œuvre.

## Quand il s'exécute

Il tourne sur la **branche par défaut uniquement** — il évalue le dépôt, pas un
diff de PR, donc il n'y a **pas de trigger `pull_request`** :

- Sur **`branch_protection_rule`** — re-note quand une règle de protection de
  branche change (cela alimente le check Branch-Protection de Scorecard).
- **Chaque semaine** via `schedule` (`cron: 23 5 * * 1`).
- Sur **push vers `main`**.

## Comment il s'exécute

Un seul job, `analysis` : checkout sans credentials → exécution de
`ossf/scorecard-action` avec `publish_results: true` → upload du SARIF à la fois
en artefact de build et vers le tableau de bord code-scanning.

## Permissions & sécurité

`permissions: read-all` au niveau supérieur ; le job n'élargit que deux
périmètres en écriture :

- `security-events: write` — uploader le SARIF vers code-scanning.
- `id-token: write` — publier les résultats vers l'API publique OpenSSF via OIDC ;
  **c'est ce qui active le badge**.

Le checkout utilise **`persist-credentials: false`** — un checkout sans
credentials que le propre check Token-Permissions de Scorecard récompense.

## À manipuler avec précaution

- **Il ne tourne pas sur les pull requests, à dessein.** N'ajoutez pas de trigger
  `pull_request` pour « le tester sur une PR » — Scorecard évalue le dépôt entier,
  et il a besoin du contexte de la branche par défaut et de l'identité OIDC pour
  publier. Un run de PR serait dénué de sens et ne pourrait pas publier. Pour
  exercer un changement, mergez-le et observez le premier run push-sur-`main`.
- **Le badge affiche « no data » jusqu'au premier run sur `main` qui publie.**
  C'est attendu lors de la mise en place initiale, pas un échec.
- **`publish_results: true` convient à un dépôt public.** Il publie le score sur
  securityscorecards.dev par transparence et pour le badge. Laissez-le tel quel
  pour un dépôt public ; si le dépôt passait un jour en privé, retirez-le (et le
  badge).
- **Le commentaire sur les permissions porte du sens.** Ce dépôt est public, donc
  `contents: read` / `actions: read` ne sont pas nécessaires dans le token. Les
  lignes commentées dans le YAML expliquent ce qu'il faut **décommenter pour un
  dépôt privé** — ne les supprimez pas.
- **On améliore le score dans les *autres* workflows et dans les réglages du
  dépôt**, pas ici. Un sous-score Branch-Protection faible, par exemple, se
  corrige en marquant des checks required sur `main`, pas en éditant ce fichier.

## Check Fuzzing

Le check **Fuzzing** de Scorecard est satisfait ici par les tests property-based
de `FirstClassErrors.PropertyTests` (FsCheck). Scorecard crédite le property-based
testing C# en cherchant `using FsCheck;` dans l'arborescence des sources : les
fuzzers vivent donc dans ce projet de tests, pas dans ce workflow.

- **Attention à la version de l'outillage.** La détection property-based C# est
  arrivée dans **scorecard v5.5.0** ; `scorecard-action@v2.4.3` — le pin du
  workflow — embarque encore **v5.3.0**, antérieure. Le sous-check ne passe donc
  au vert qu'une fois le scan exécuté en ≥ v5.5.0 : quand une version plus récente
  de `scorecard-action` (embarquant ≥ v5.5.0) sort et que le pin est relevé.
  D'ici là les tests tournent et protègent le code — seul le sous-score publié est
  en retard.

## En rapport

- [`codeql`](codeql.fr.md) — partage l'action `github/codeql-action/upload-sarif`
  utilisée pour pousser les résultats vers code-scanning.
- Le badge du README pointe vers le visualiseur public sur `securityscorecards.dev`.
