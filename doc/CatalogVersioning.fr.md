# Versionnage du catalogue

🌍 **Langues:**  
🇬🇧 [English](./CatalogVersioning.en.md) | 🇫🇷 Français (ce fichier)

Un code d'erreur ne reste pas à l'intérieur du système qui l'émet. Des applications clientes branchent leur logique dessus, des tableaux de bord déclenchent des alertes dessus, des procédures de support y font référence. Supprimer ou renommer un code est donc un **changement cassant** — de même nature que la suppression d'un membre d'API publique — et mérite le même garde-fou : une référence commitée, et une étape de CI qui échoue quand le contrat dérive par accident.

FirstClassErrors fournit ce garde-fou avec deux commandes : `fce catalog update` et `fce catalog diff`.

## 🧾 Le snapshot de contrat

L'unité de comparaison est le **snapshot canonique** : une petite projection JSON déterministe du catalogue, contenant uniquement ce qui constitue le contrat.

| Suivi | Rôle |
| --- | --- |
| `code` | L'identité de l'erreur. Sa suppression est cassante. |
| `context` (nom de clé + type de valeur) | Les données structurées attachées aux occurrences. Les pipelines de logs et les tableaux de bord les lisent par leur nom ; une suppression ou un changement de type est cassant. |
| `title`, `source` | Identité documentaire — les changements sont signalés à titre informatif, et les titres identiques servent d'indice de renommage probable. |

Les messages, explications, règles métier et diagnostics ne sont délibérément **pas** suivis : c'est de la documentation, extraite d'exemples vivants, libre d'évoluer sans toucher au contrat.

Le snapshot est indépendant du renderer : que le catalogue destiné aux humains soit publié en HTML, Markdown, JSON ou dans un format personnalisé, c'est le même fichier de contrat qui pilote le versionnage. Il est déterministe — erreurs triées par code, clés de contexte par nom, fins de ligne fixées — si bien que le fichier commité ne dépend jamais de la machine qui l'a produit. Les commandes `fce catalog` l'extraient toujours sous la culture `en`, de sorte que la baseline reste indépendante de la langue même quand votre catalogue est localisé (voir [Internationalisation](Internationalisation.fr.md)).

## 📌 Créer la baseline

```bash
fce catalog update --solution MyApp.sln
```

Cette commande extrait le catalogue, le projette en snapshot et écrit `errors-baseline.json` (modifiable avec `--baseline`, ou via `baseline` dans `fce.json`). **Commitez ce fichier** : c'est le contrat accepté, et chaque modification passe en revue de code comme n'importe quel autre changement de contrat.

## 🔍 Détecter la dérive en CI

```bash
fce catalog diff --solution MyApp.sln
```

La commande extrait le catalogue courant, le compare à la baseline et écrit un rapport sur la sortie standard. Son code de sortie est conçu pour les pipelines :

| Code de sortie | Signification |
| --- | --- |
| `0` | Aucun changement au niveau du seuil `--fail-on` ou au-dessus. |
| `2` | Le contrat a dérivé : au moins un changement atteint le seuil. |
| `1` | Erreur d'exécution — baseline manquante, extraction en échec, ou baseline écrite par un schéma plus récent (voir plus bas). |
| `130` | Interrompu avant la fin (Ctrl+C). |

`--fail-on` choisit la politique : `breaking` (défaut), `any` (tout changement fait échouer, y compris les ajouts) ou `none` (rapport seul). `--report` choisit la sortie : `text` (défaut), `markdown` (prêt à poster en commentaire de pull request) ou `json` (pour l'outillage).

## 🧮 Classification des changements

| Changement | Impact |
| --- | --- |
| Code d'erreur supprimé | 💥 Cassant |
| Clé de contexte supprimée | 💥 Cassant |
| Type de valeur d'une clé de contexte modifié | 💥 Cassant |
| Code d'erreur ajouté | ✅ Compatible |
| Clé de contexte ajoutée | ✅ Compatible |
| Titre ou source modifié | ℹ️ Informatif |

Un **renommage** est une suppression plus un ajout — et reste cassant, car les consommateurs connaissent l'ancien code. Quand exactement une erreur ajoutée porte le même titre que l'erreur supprimée, le rapport ajoute un indice : *« possibly renamed to 'NEW_CODE', which has the same title »*.

## ✍️ Accepter un changement délibérément

Quand un changement de contrat est intentionnel, rafraîchissez la baseline et commitez-la :

```bash
fce catalog update --solution MyApp.sln
```

La commande résume ce qu'elle absorbe (`1 breaking, 2 compatible and 0 documentation change(s) accepted`), et la pull request montre alors le diff de la baseline — un code supprimé apparaît comme une ligne supprimée. L'accident devient impossible ; le changement délibéré devient visible et relisible. C'est la même discipline qu'un fichier de baseline d'API publique, appliquée au catalogue d'erreurs.

## 🛡️ Résilience de la baseline & versionnage du schéma

La baseline est un fichier versionné : au fil de la vie d'un projet, elle peut être corrompue par un merge malheureux ou produite par une version différente de l'outil. `fce catalog update` traite chaque cas de façon délibérée plutôt que silencieuse :

* **Une baseline corrompue ou illisible est régénérée, jamais fatale.** Mettre à jour, c'est précisément la façon de (re)construire une baseline : un fichier existant impossible à parser est réécrit à partir du catalogue courant, avec un avertissement — une baseline cassée ne vous bloque jamais.
* **Une baseline écrite par un schéma _plus récent_ est refusée, jamais rétrogradée.** Chaque snapshot porte une version de `schema`. Si un collègue a commité une baseline avec un outil plus récent, un outil plus ancien ne l'écrasera pas avec un schéma plus ancien — cela supprimerait silencieusement de l'information — : il s'arrête avec une erreur vous invitant à mettre à jour. `fce catalog diff` la refuse de la même manière. Mettre à jour l'outil, ou aligner les versions dans l'équipe, résout le problème.

Ainsi `fce catalog update` sort `0` quand la baseline est créée, déjà à jour ou rafraîchie (y compris auto-réparée) ; `1` sur une erreur d'exécution ou une baseline de schéma trop récent ; et `130` en cas d'interruption.

## ⚙️ Des snapshots sans baseline

Deux autres façons de produire et comparer des snapshots :

* `fce generate --snapshot <chemin>` écrit aussi le snapshot canonique à côté du format rendu, quel qu'il soit — une seule génération produit à la fois le catalogue pour les humains et le fichier de contrat. Il reflète la langue de rendu `--language` ; lorsqu'elle n'est pas l'anglais, la commande émet un avertissement, car une baseline commitée doit rester indépendante de la langue — utilisez `fce catalog update` (ou `--language en`) pour cela.
* `fce catalog diff --against <chemin>` compare la baseline à un **fichier** de snapshot au lieu d'extraire depuis la source — utile pour comparer deux artefacts de release.

`baseline` et `snapshot` peuvent être définis dans `fce.json` pour ne pas répéter les chemins à chaque exécution.

## 🚦 Intégration CI/CD : un exemple complet

L'objectif de l'intégration CI est simple : **la dérive du contrat doit être visible là où le changement est relu** — dans la pull request elle-même, pas dans un log que personne ne lit. La boucle est la suivante :

1. Chaque pull request exécute `fce catalog diff` contre la baseline commitée.
2. **Aucun changement** → le job passe silencieusement.
3. **Changements compatibles ou documentaires** → le job passe quand même (avec le `--fail-on breaking` par défaut) ; le rapport peut être posté pour information.
4. **Changement cassant** → le code de sortie `2` fait échouer le job, et le rapport Markdown atterrit en commentaire de la pull request. L'auteur n'a alors que deux issues honnêtes : corriger la suppression accidentelle, ou l'accepter délibérément avec `fce catalog update` — auquel cas le relecteur voit le diff de la baseline (le code supprimé apparaît comme une ligne supprimée) et approuve un changement cassant *en connaissance de cause*.

Déroulé sur un scénario concret : un développeur renomme `PAYMENT.DECLINED` en `PAYMENT.REFUSED` au cours d'un refactoring. Sans le garde-fou, le renommage part silencieusement et chaque tableau de bord ou client branché sur l'ancien code casse en production. Avec lui, la pull request échoue avec :

```
Breaking changes (1):
  - [removed] PAYMENT.DECLINED — error removed (possibly renamed to 'PAYMENT.REFUSED', which has the same title)
Compatible changes (1):
  - [added] PAYMENT.REFUSED — new error 'Payment declined' (source: Payment)
```

Si le renommage était accidentel, le développeur le corrige. S'il était délibéré, il exécute `fce catalog update`, committe `errors-baseline.json`, et le changement de contrat devient une partie explicite et relisible de la pull request.

### GitHub Actions

```yaml
name: error-catalog

on:
  pull_request:
    branches: [main]

jobs:
  catalog-diff:
    runs-on: ubuntu-latest
    permissions:
      pull-requests: write # needed to post the report as a comment
    steps:
      - uses: actions/checkout@v4

      - uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '10.0.x'

      # Make fce available on the runner (dotnet tool install, a cached
      # build from source, or your internal distribution).

      - name: Compare the catalog against the baseline
        run: fce catalog diff --solution MyApp.sln --report markdown > catalog-diff.md
        # Exit code 2 (breaking change) fails this step, and therefore the job.

      - name: Post the report on the pull request
        if: failure() # comment only when the contract drifted
        run: gh pr comment ${{ github.event.pull_request.number }} --body-file catalog-diff.md
        env:
          GH_TOKEN: ${{ github.token }}
```

Deux comportements à noter : le rapport est généré *avant* que l'étape n'échoue (la redirection capture la sortie standard, puis le code de sortie fait échouer l'étape), et l'étape de commentaire ne s'exécute que sur `if: failure()` — un pipeline sain reste silencieux.

### GitLab CI

```yaml
error-catalog:
  stage: test
  image: mcr.microsoft.com/dotnet/sdk:10.0
  script:
    # Make fce available on the runner (dotnet tool install, a cached
    # build from source, or your internal distribution).
    - fce catalog diff --solution MyApp.sln --report markdown > catalog-diff.md
  artifacts:
    when: always # keep the report even when the job fails on a breaking change
    paths:
      - catalog-diff.md
```

Le code de sortie pilote le résultat du job exactement comme sur GitHub ; `artifacts: when: always` garde le rapport téléchargeable depuis le pipeline en échec, et il peut être posté sur la merge request via l'[API notes](https://docs.gitlab.com/ee/api/notes.html) si vous souhaitez aussi automatiser le commentaire.

### Au-delà des pull requests

Exécuter le même `fce catalog diff` sur la branche principale (à chaque push ou de façon planifiée) attrape les dérives qui auraient contourné le flux de pull request, et `fce catalog diff --against` permet à un pipeline de release de comparer deux snapshots publiés — par exemple celui livré avec la release précédente contre celui du candidat — pour générer des notes de version du contrat d'erreurs.

---

<div align="center">
<a href="OperationalIntegration.fr.md">← CI/CD et intégration opérationnelle</a> · <a href="README.fr.md#-étapes-suivantes">↑ Table des matières</a> · <a href="ArchitectureOfTheDocumentationPipeline.fr.md">Architecture du pipeline de documentation →</a>
</div>

---
