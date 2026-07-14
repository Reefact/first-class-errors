# Générer et publier le catalogue d’erreurs

🌍 **Langues :**  
🇬🇧 [English](./OperationalIntegration.en.md) | 🇫🇷 Français (ce fichier)

FirstClassErrors devient réellement utile pour l’exploitation lorsque le catalogue est généré depuis le code exact en cours de build et publié à un endroit accessible aux développeurs, au support et aux opérateurs.

Ce guide couvre la chaîne de livraison. Pour le logging structuré et les diagnostics de production, voir [Logging et intégration opérationnelle](LoggingIntegration.fr.md).

## Le flux de livraison

```mermaid
flowchart LR
    A[Compiler l’application] --> B[Extraire la connaissance d’erreur]
    B --> C[Rendre le catalogue]
    C --> D[Publier l’artefact ou le site]
    B --> E[Comparer la baseline de contrat]
```

Un pipeline fiable doit :

1. compiler l’application ;
2. générer le catalogue depuis le code compilé ;
3. publier les fichiers générés ;
4. éventuellement comparer le contrat courant à une baseline commitée.

## Activer les projets

La génération au niveau solution est opt-in. Ajoutez le marqueur directement dans chaque `.csproj` qui définit des erreurs applicatives documentées :

```xml
<PropertyGroup>
  <GenerateErrorDocumentation>true</GenerateErrorDocumentation>
</PropertyGroup>
```

Le marqueur est lu dans le fichier projet lui-même. Une valeur héritée de `Directory.Build.props` n’est pas détectée.

Lorsqu’aucun projet n’a opté, le générateur émet un avertissement plutôt que de présenter silencieusement un catalogue vide comme un résultat valide.

Pour les déclarations ambiguës, la découverte de projets et le fonctionnement des workers, voir [Architecture du pipeline de documentation](ArchitectureOfTheDocumentationPipeline.fr.md).

## Générer le catalogue en local

Installez le CLI, compilez, puis générez depuis les binaires existants :

```bash
dotnet tool install --global FirstClassErrors.Cli
dotnet build MyApp.sln -c Release
fce generate \
  --solution MyApp.sln \
  --configuration Release \
  --no-build \
  --format markdown \
  --output artifacts/errors.md \
  --service-name my-api
```

`--service-name` est requis pour Markdown et HTML car leurs exemples RFC 9457 utilisent des types de problème comme :

```text
urn:problem:my-api:payment-declined
```

La sortie JSON ne nécessite pas de nom de service.

## Workflow GitHub Actions recommandé

```yaml
name: error-documentation

on:
  pull_request:
  push:
    branches: [main]

jobs:
  generate-error-catalog:
    runs-on: ubuntu-latest

    steps:
      - uses: actions/checkout@v4

      - uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '10.0.x'

      - name: Install FirstClassErrors CLI
        run: dotnet tool install --global FirstClassErrors.Cli

      - name: Build
        run: dotnet build MyApp.sln -c Release

      - name: Generate catalog
        run: |
          fce generate \
            --solution MyApp.sln \
            --configuration Release \
            --no-build \
            --format html \
            --layout split \
            --output artifacts/error-catalog \
            --service-name my-api

      - name: Publish catalog artifact
        uses: actions/upload-artifact@v4
        with:
          name: error-catalog
          path: artifacts/error-catalog
```

Le build et la génération utilisent la même configuration. `--no-build` évite que le générateur reconstruise un autre ensemble de binaires.

## Générer plusieurs langues

Exécutez une génération par locale :

```yaml
strategy:
  matrix:
    language: [en, fr]

steps:
  # checkout, setup, installation et build omis

  - name: Generate ${{ matrix.language }} catalog
    run: |
      fce generate \
        --solution MyApp.sln \
        --configuration Release \
        --no-build \
        --format html \
        --layout split \
        --language "${{ matrix.language }}" \
        --output "artifacts/error-catalog-${{ matrix.language }}" \
        --service-name my-api
```

Les noms de fichiers et les ancres restent stables d’une langue à l’autre. Voir [Internationalisation](Internationalisation.fr.md) pour la localisation du contenu et des templates de renderer.

## Choisir une cible de publication

Le catalogue généré peut être :

- conservé comme artefact de pipeline ;
- attaché à une release ;
- déployé comme site statique ;
- copié dans un portail documentaire interne ;
- publié à côté de la documentation opérationnelle du service.

La plateforme importe moins que l’accessibilité : le catalogue correspondant à une version déployée doit être atteignable par les personnes qui investiguent cette version.

## Conserver des catalogues versionnés

Un site « latest » est pratique au quotidien, mais n’explique plus correctement une ancienne release de production après l’évolution du contrat.

Pour les systèmes durables ou critiques pour le support, publiez au moins une forme immuable par release :

```text
/errors/latest/
/errors/releases/2.4.0/
/errors/releases/2.3.1/
```

Un `InstanceId`, une version de déploiement ou un tag de release peut alors mener le support vers la documentation correspondante.

## Protéger le contrat d’erreurs

La génération répond à « que documente cette version ? ». Le versionnage répond à « cette version casse-t-elle un contrat déjà accepté ? ».

```bash
fce catalog diff --solution MyApp.sln --configuration Release --no-build
```

Conservez la baseline acceptée dans le dépôt et exécutez la comparaison dans les pull requests.

Voir :

- [Versionnage du catalogue](CatalogVersioning.fr.md) pour le modèle mental et le workflow quotidien ;
- [Intégration CI/CD du versionnage](CatalogVersioningCI.fr.md) pour les exemples complets GitHub Actions et GitLab.

## Politique d’échec

Traitez séparément les situations suivantes :

| Situation | Signification dans le pipeline |
| --- | --- |
| le build applicatif échoue | le produit ne peut pas être construit |
| l’extraction ou le rendu échoue | le catalogue n’est pas fiable |
| aucun projet n’a opté | la configuration est incomplète ou la solution ne contient volontairement aucun projet documenté |
| le contrat du catalogue casse | une revue humaine est nécessaire avant acceptation |
| la publication échoue | la documentation reste indisponible malgré une génération réussie |

Ne masquez pas les erreurs d’extraction ou de publication avec `continue-on-error` dans un pipeline qui promet une documentation opérationnelle.

## Checklist de revue

Avant d’approuver un pipeline de livraison du catalogue, vérifiez que :

- les projets activés déclarent le marqueur dans leur propre `.csproj` ;
- la génération utilise les mêmes binaires et la même configuration que le build applicatif ;
- `--service-name` est fourni pour Markdown ou HTML ;
- les fichiers générés sont publiés à un endroit accessible ;
- les sorties par langue ne s’écrasent pas ;
- les releases peuvent conserver une documentation immuable ;
- la comparaison de contrat reste distincte du rendu du catalogue ;
- les échecs de génération et de publication sont visibles.

---

<div align="center">
<a href="DeterministicTesting.fr.md">← Tests d’erreur déterministes</a> · <a href="README.fr.md#-étapes-suivantes">↑ Table des matières</a> · <a href="LoggingIntegration.fr.md">Logging et intégration opérationnelle →</a>
</div>

---