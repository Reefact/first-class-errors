# Workflow `codeql`

🌍 🇬🇧 [English](codeql.en.md) · 🇫🇷 Français (ce fichier)

> Documentation mainteneur — fait partie de la [référence des workflows](README.fr.md).
> Ne fait pas partie de la documentation utilisateur sous `doc/`.

**Fichier du workflow :** [`.github/workflows/codeql.yml`](../../../../.github/workflows/codeql.yml)

## À quoi il sert

`codeql` exécute l'analyse statique CodeQL de GitHub sur le code C# et remonte
les résultats au **tableau de bord code-scanning** du dépôt (l'onglet Security).
C'est le scanner de sécurité sémantique : il cherche des motifs de vulnérabilité
(injection, désérialisation non sûre, etc.) plutôt que des problèmes de style. Il
alimente le badge `codeql` du README.

## Quand il s'exécute

- À chaque **push sur `main`** et **pull request visant `main`**.
- **Chaque semaine** via `schedule` (`cron: 17 6 * * 1`), pour que les requêtes
  CodeQL nouvellement livrées tournent sur du code inchangé.
- À la demande via **`workflow_dispatch`**.

## Comment il s'exécute

Un seul job, `analyze` :

1. Checkout.
2. **Initialize CodeQL** pour `csharp` avec **`build-mode: none`** (extraction
   sans build).
3. **Perform CodeQL analysis** et upload des résultats.

## Permissions & sécurité

Le workflow est par défaut en `contents: read` ; le job `analyze` ajoute
`security-events: write` (pour uploader les résultats au tableau de bord) et
`actions: read` (nécessaire à l'action sur les dépôts privés, inoffensif sur les
publics).

## À manipuler avec précaution

- **`build-mode: none` est un choix délibéré.** L'extraction sans build ne
  demande aucun SDK .NET ni étape de build, et elle contourne les problèmes de
  traçage du compilateur sur un SDK très récent. Si un jour vous voulez une
  analyse de flux de données plus poussée, passez à `manual` et ajoutez une étape
  `dotnet build` explicite — ne comptez pas sur `autobuild` comme sur une
  amélioration gratuite.
- **Les deux étapes CodeQL doivent rester sur le même SHA d'action.** `init` et
  `analyze` viennent de `github/codeql-action` ; montez-les ensemble (ainsi que
  la référence `upload-sarif` dans [`scorecard`](scorecard.fr.md), qui utilise la
  même famille d'action).
- **Le `schedule` hebdomadaire n'est pas redondant.** Il applique les mises à
  jour de requêtes à du code qui n'a pas changé ; le retirer signifie que les
  nouvelles classes de requêtes ne tournent qu'au prochain push.

## En rapport

- [`scorecard`](scorecard.fr.md) — uploade aussi du SARIF vers code-scanning, via
  la même action `github/codeql-action/upload-sarif`.
- [`dependency-review`](dependency-review.fr.md) — le barrage de sécurité côté
  dépendances, complémentaire de celui-ci côté code.
