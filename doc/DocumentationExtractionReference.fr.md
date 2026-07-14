# Référence de l’extraction et de la découverte des projets

🌍 **Langues :**  
🇬🇧 [English](./DocumentationExtractionReference.en.md) | 🇫🇷 Français (ce fichier)

Cette page est la référence opérationnelle pour sélectionner les projets et assemblies, exécuter les workers d’extraction et traiter les échecs. Pour commencer par le modèle mental, lisez [Architecture du pipeline de documentation](ArchitectureOfTheDocumentationPipeline.fr.md).

## Mode solution

Le parcours CLI courant part d’une solution :

```bash
fce generate --solution ./MyApp.sln --format markdown --service-name my-api --output ./docs/errors
```

Le mode solution :

1. compile toute la solution, sauf avec `--no-build` ;
2. liste les projets via `dotnet sln list` ;
3. les sélectionne selon le marqueur d’opt-in ;
4. localise leurs assemblies de sortie ;
5. lance un worker d’extraction par assembly ;
6. agrège la documentation et les échecs.

Le build porte sur la solution elle-même, avant la sélection des projets : une erreur de compilation dans un projet qui n’a jamais opté fait quand même échouer l’exécution.

## Opt-in des projets

Un projet participe lorsque son propre `.csproj` contient :

```xml
<PropertyGroup>
  <GenerateErrorDocumentation>true</GenerateErrorDocumentation>
</PropertyGroup>
```

Le marqueur est lu directement dans le XML du projet. Il n’est pas évalué comme une propriété MSBuild normale.

| Déclaration | Résultat |
| --- | --- |
| `true` une fois, sans condition | projet inclus |
| absente | projet ignoré |
| `false` | projet ignoré |
| déclarée plusieurs fois | ambiguïté signalée |
| déclarée sous `Condition` | ambiguïté signalée |

Conséquences importantes :

- une valeur uniquement définie dans `Directory.Build.props` n’active pas le projet ;
- une propriété importée depuis un autre fichier n’active pas le projet ;
- `-p:GenerateErrorDocumentation=true` passé à `dotnet build` n’active pas le projet ;
- le marqueur doit être littéral et non ambigu dans le `.csproj` lui-même.

Avec une politique de poursuite, les projets ambigus sont signalés puis ignorés. Avec une politique stricte, ils font échouer la génération.

## Options d’opt-in programmatiques

`SolutionGenerationOptions` permet de modifier les valeurs par défaut :

- `OptInPropertyName` change le nom du marqueur ;
- `IncludeProjectsWithoutOptIn` inclut les projets sans marqueur.

La CLI `fce` utilise `GenerateErrorDocumentation` et le comportement d’opt-in décrit ci-dessus.

## Mode assemblies

Utilisez des assemblies déjà compilés lorsque la découverte de solution ou le build ne doivent pas faire partie de l’exécution :

```bash
fce generate \
  --assemblies ./artifacts/MyApp.Domain.dll \
  --assemblies ./artifacts/MyApp.Application.dll \
  --format json \
  --output ./artifacts/errors.json
```

`--assemblies` accepte un chemin par occurrence ; répétez l’option pour chaque assembly.

Le mode assemblies documente exactement les binaires fournis. Il n’applique pas le filtre d’opt-in du `.csproj`.

Il est adapté lorsque :

- une étape précédente a déjà compilé l’application ;
- les assemblies proviennent de plusieurs solutions ;
- l’appelant veut sélectionner exactement les binaires ;
- la découverte de projets n’est pas pertinente.

L’appelant reste responsable de fournir les fichiers de dépendances et assets runtime compatibles à côté de l’assembly cible.

## Extraction d’un assembly unique

`AssemblyErrorDocumentationReader.GetErrorDocumentationFrom(assembly)` effectue une extraction en processus pour un assembly déjà chargé.

Cette API :

- trouve les classes `[ProvidesErrorsFor]` ;
- résout les méthodes référencées par `[DocumentedBy]` ;
- invoque les méthodes de documentation et factories d’exemples ;
- renvoie un `ErrorDocumentationExtractionResult` contenant documentation et échecs ;
- déduplique et ordonne la documentation par code d’erreur.

Elle est utile pour des outils contrôlés et des tests. La génération au niveau solution utilise normalement des workers isolés.

## Exécution des workers

Chaque assembly sélectionné est extrait dans un processus worker éphémère. Le générateur le lance avec le contexte de dépendances de la cible afin d’isoler les dépendances applicatives et la version de FirstClassErrors.

Le worker :

1. charge l’assembly cible ;
2. exécute l’extraction ;
3. sérialise le résultat complet en JSON ;
4. se termine.

Le générateur lit ce résultat puis passe à l’assembly suivant.

## Pourquoi les workers sont nécessaires

Les méthodes de documentation et factories d’exemples sont du code exécutable. Elles peuvent :

- initialiser de l’état statique ;
- charger des dépendances applicatives ;
- utiliser une autre version de FirstClassErrors ;
- lever une exception ;
- faire planter le processus ;
- rester bloquées.

Les workers par assembly isolent ces risques et rattachent l’échec à l’assembly qui l’a produit.

## Échecs et poursuite

Les échecs d’extraction sont des données et ne provoquent pas nécessairement l’arrêt immédiat.

Les échecs rapportés par un worker qui se termine normalement sont toujours enregistrés et journalisés, et la génération poursuit avec les assemblies restants quelle que soit la politique d’échec configurée :

- cible `[DocumentedBy]` introuvable ;
- signature invalide ;
- méthode de documentation qui lève ;
- factory d’exemple qui lève.

Les échecs de processus honorent la politique d’échec configurée, qui décide si le générateur enregistre le problème et poursuit avec les autres assemblies, ou le considère comme fatal :

- assembly impossible à charger ;
- arrêt inattendu du worker ;
- timeout du worker.

Une exécution poursuivie peut donc produire un catalogue partiel avec des échecs explicites. La présence d’un fichier généré ne prouve pas que tous les assemblies ont été documentés correctement.

## Timeouts et crashs

Un worker qui dépasse son timeout est arrêté et enregistré comme échoué. Un crash est également enregistré avec les informations disponibles.

Pour analyser un timeout :

1. exécutez directement la factory documentée ou l’exemple dans un test ;
2. cherchez les I/O bloquantes, deadlocks ou initialisations dépendantes de l’environnement ;
3. vérifiez la présence des fichiers runtime et de dépendances ;
4. évitez tout accès réseau ou service de production dans les factories de documentation ;
5. gardez les factories d’exemples petites et déterministes.

Le code de documentation doit construire des erreurs représentatives, pas exécuter de véritables workflows applicatifs.

## Build et `--no-build`

En mode solution, le générateur compile la solution par défaut. Utilisez `--no-build` uniquement lorsque les sorties attendues existent déjà et correspondent au code courant.

```bash
fce generate --solution ./MyApp.sln --no-build --format markdown --service-name my-api --output ./docs/errors
```

Une séquence CI sûre est :

```bash
dotnet build MyApp.sln -c Release
fce generate --solution MyApp.sln --configuration Release --no-build --format markdown --service-name my-api --output artifacts/errors
```

Des sorties obsolètes ou absentes peuvent documenter un ancien code ou provoquer un échec de localisation.

## Configuration et framework

La configuration et le framework sélectionnés doivent identifier une sortie réelle pour chaque projet participant. Les projets multi-cibles peuvent nécessiter un framework explicite.

Gardez la configuration de la CLI alignée sur le build qui a produit les assemblies :

```bash
fce generate \
  --solution ./MyApp.sln \
  --configuration Release \
  --framework net8.0 \
  --no-build \
  --output ./artifacts/errors
```

## Factories de documentation sûres

Une méthode de documentation doit être :

- déterministe ;
- rapide ;
- sans I/O externe ;
- indépendante des secrets d’environnement ;
- sûre à exécuter plusieurs fois ;
- limitée à la construction de documentation et d’erreurs représentatives.

Évitez :

- les appels de base de données ;
- les appels HTTP ;
- la lecture de configuration de production mutable ;
- la dépendance à l’heure courante ou à l’aléatoire lorsqu’elle affecte la sortie ;
- le démarrage de tâches de fond ;
- la modification d’état global de l’application.

## Checklist de dépannage

Lorsque des erreurs attendues manquent, vérifiez :

- la présence du marqueur littéral `<GenerateErrorDocumentation>true</GenerateErrorDocumentation>` dans le `.csproj` du projet ;
- `[ProvidesErrorsFor]` sur la classe factory ;
- `[DocumentedBy]` sur la factory ;
- l’existence et la signature de la méthode référencée ;
- le succès des méthodes de documentation et exemples ;
- la configuration et le framework compilés ;
- l’absence de sorties obsolètes avec `--no-build` ;
- les avertissements et échecs des workers ;
- les chemins exacts en mode assemblies.

---

<div align="center">
<a href="ArchitectureOfTheDocumentationPipeline.fr.md">← Architecture du pipeline de documentation</a> · <a href="README.fr.md#-étapes-suivantes">↑ Table des matières</a> · <a href="WritingACustomRenderer.fr.md">Écrire son propre renderer →</a>
</div>

---