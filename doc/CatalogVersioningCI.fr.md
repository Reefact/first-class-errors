# Intégrer le versionnage du catalogue en CI/CD

🌍 **Langues :**  
🇬🇧 [English](./CatalogVersioningCI.en.md) | 🇫🇷 Français (ce fichier)

Ce guide part du principe que la baseline a déjà été créée et commitée :

```bash
fce catalog update --solution MyApp.sln
git add errors-baseline.json
git commit -m "chore: add error catalog baseline"
```

La CI n'a alors besoin que d'une opération en lecture : comparer le catalogue courant au contrat accepté.

```bash
fce catalog diff --solution MyApp.sln
```

## 🚦 Comportement attendu dans une pull request

Le workflow recommandé est le suivant :

1. la pull request exécute `fce catalog diff` contre `errors-baseline.json` ;
2. aucun changement ne fait échouer le job ;
3. les ajouts compatibles sont signalés sans bloquer le job avec la politique par défaut ;
4. un changement cassant retourne `2` et bloque la pull request ;
5. l'auteur corrige le changement accidentel ou met volontairement à jour la baseline.

La dérive doit être visible à l'endroit où le changement est relu : idéalement dans la pull request elle-même, pas uniquement dans un log de pipeline.

## GitHub Actions

L'exemple suivant conserve le rapport, le publie uniquement lorsqu'un changement atteint le seuil configuré, puis restitue le véritable code de sortie de `fce`.

```yaml
name: error-catalog

on:
  pull_request:
    branches: [main]

jobs:
  catalog-diff:
    runs-on: ubuntu-latest
    permissions:
      contents: read
      pull-requests: write

    steps:
      - uses: actions/checkout@v4

      - uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '10.0.x'

      # Rendez fce disponible sur le runner : dotnet tool, build mis en cache
      # ou mécanisme de distribution interne.

      - name: Compare the catalog with the baseline
        id: catalog
        shell: bash
        run: |
          set +e
          fce catalog diff \
            --solution MyApp.sln \
            --report markdown > catalog-diff.md
          exit_code=$?
          echo "exit_code=$exit_code" >> "$GITHUB_OUTPUT"
          exit 0

      - name: Post the breaking-change report
        if: steps.catalog.outputs.exit_code == '2'
        run: gh pr comment "${{ github.event.pull_request.number }}" --body-file catalog-diff.md
        env:
          GH_TOKEN: ${{ github.token }}

      - name: Propagate the catalog result
        if: steps.catalog.outputs.exit_code != '0'
        run: exit "${{ steps.catalog.outputs.exit_code }}"
```

Cette séparation évite de confondre deux situations :

- `2` signifie que le contrat a changé selon la politique choisie ;
- `1` signifie que la commande n'a pas pu s'exécuter correctement.

Avec `--fail-on breaking`, seuls les changements cassants font échouer le job. Pour faire échouer la CI sur tout changement, utilisez `--fail-on any`.

## GitLab CI

```yaml
error-catalog:
  stage: test
  image: mcr.microsoft.com/dotnet/sdk:10.0
  script:
    # Rendez fce disponible sur le runner avant cette commande.
    - fce catalog diff --solution MyApp.sln --report markdown > catalog-diff.md
  artifacts:
    when: always
    paths:
      - catalog-diff.md
```

Le code de sortie de `fce` pilote directement le résultat du job. `artifacts: when: always` conserve le rapport même lorsque le job échoue.

Vous pouvez ensuite publier `catalog-diff.md` dans la merge request au moyen de l'[API Notes de GitLab](https://docs.gitlab.com/ee/api/notes.html).

## ✍️ Accepter un changement

Le flux le plus simple et le plus explicite reste local :

```bash
fce catalog update --solution MyApp.sln
git diff -- errors-baseline.json
git add errors-baseline.json
git commit -m "chore: update error catalog baseline"
```

Le relecteur voit alors le changement de contrat dans le diff Git de la pull request.

> N'exécutez pas `catalog update` automatiquement après l'échec du diff. Cela transformerait toute rupture accidentelle en contrat accepté sans décision humaine.

## ⚙️ Initialiser automatiquement une baseline absente

L'initialisation locale est recommandée, car elle permet de garder la CI en lecture seule. Une initialisation automatique peut néanmoins être utile lors du déploiement progressif de l'outil sur de nombreux dépôts.

Exécutez cette opération uniquement sur la branche par défaut, avec une permission d'écriture explicite :

```yaml
permissions:
  contents: write

steps:
  - uses: actions/checkout@v4

  - name: Seed the error-catalog baseline if missing
    run: |
      if [ ! -f errors-baseline.json ]; then
        fce catalog update --solution MyApp.sln
        git add errors-baseline.json
        git -c user.name='github-actions[bot]' \
            -c user.email='41898282+github-actions[bot]@users.noreply.github.com' \
            commit -m 'chore: seed the error-catalog baseline'
        git push
      fi
```

Cette étape ne doit créer le fichier que s'il est absent. Elle ne doit pas rafraîchir silencieusement une baseline existante.

## 🏷️ Accepter un changement avec un label GitHub

Une équipe peut transformer l'acceptation en action explicite sur la pull request. Par exemple, l'ajout du label `accept-contract-change` peut régénérer puis commiter la baseline sur la branche de la PR.

```yaml
name: accept-error-catalog-change

on:
  pull_request:
    types: [labeled]

jobs:
  accept:
    if: github.event.label.name == 'accept-contract-change'
    runs-on: ubuntu-latest
    permissions:
      contents: write

    steps:
      - uses: actions/checkout@v4
        with:
          ref: ${{ github.event.pull_request.head.ref }}

      - uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '10.0.x'

      # Rendez fce disponible sur le runner avant cette étape.

      - name: Regenerate and commit the baseline
        run: |
          fce catalog update --solution MyApp.sln
          git add errors-baseline.json

          if git diff --cached --quiet; then
            echo 'Baseline already up to date — nothing to accept.'
          else
            git -c user.name='github-actions[bot]' \
                -c user.email='41898282+github-actions[bot]@users.noreply.github.com' \
                commit -m 'chore: accept error-catalog contract change'
            git push
          fi
```

Ce workflow fonctionne directement pour les branches du même dépôt. Pour une pull request provenant d'un fork, l'auteur doit généralement exécuter `fce catalog update` localement puis pousser le commit sur son fork.

## 📦 Vérifier la branche principale et les releases

Exécuter également `fce catalog diff` sur la branche principale permet de détecter une modification qui aurait contourné le flux normal de pull request.

Pour comparer un artefact de release à la baseline sans reconstruire la solution :

```bash
fce catalog diff --against path/to/release-snapshot.json
```

Cette forme peut servir à produire des notes de version du contrat d'erreurs ou à comparer le snapshot de la release précédente à celui d'un candidat.

## 📚 Documents associés

- [Comprendre et utiliser le versionnage du catalogue](CatalogVersioning.fr.md)
- [Référence des commandes, formats et codes de sortie](CatalogVersioningReference.fr.md)

---

<div align="center">
<a href="CatalogVersioningReference.fr.md">← Référence des commandes</a> · <a href="README.fr.md#-étapes-suivantes">↑ Table des matières</a> · <a href="ArchitectureOfTheDocumentationPipeline.fr.md">Architecture du pipeline de documentation →</a>
</div>

---
