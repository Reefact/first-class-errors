# Spécification de compatibilité des plateformes

🌍 🇬🇧 [English](platform-compatibility.en.md) · 🇫🇷 Français (ce fichier)

Cette page est la référence évolutive des frontières de compatibilité décidées
par les [ADR-0001](../adr/0001-lock-the-analyzer-roslyn-floor.fr.md),
[ADR-0002](../adr/0002-floor-the-tooling-runtime.fr.md) et
[ADR-0022](../adr/0022-floor-the-library-on-net-framework-4-7-2.fr.md).

## Frontières prises en charge

| Artefact | Cible / floor de compilation | Frontière d'hôte prise en charge |
|---|---|---|
| `FirstClassErrors`, `FirstClassErrors.Testing`, `FirstClassErrors.RequestBinder` | `netstandard2.0` | .NET Framework 4.7.2 ou plus récent ; implémentations modernes compatibles .NET Standard 2.0 |
| `FirstClassErrors.Analyzers` | `netstandard2.0`, compilé contre Roslyn 4.8.0 | SDK .NET 8.0.100 / Visual Studio 2022 17.8 ou hôtes Roslyn compatibles plus récents |
| `FirstClassErrors.Cli`, `FirstClassErrors.GenDoc`, `FirstClassErrors.GenDoc.Worker` | `net8.0` | .NET 8 ou plus récent par roll-forward |

La spécification .NET Standard nomme .NET Framework 4.6.1 comme consommateur
théorique, mais ce projet prend en charge **4.7.2+** : 4.7.2 est la première
version qui livre les façades nécessaires in-box et la plus ancienne que la pile
de tests xUnit v3 actuelle puisse exercer.

## Implémentation du floor de l'analyzer

L'analyzer est livré dans le package NuGet principal sous
`analyzers/dotnet/cs/`, puis chargé par le compilateur du consommateur. Les
propriétés suivantes doivent rester vraies :

1. `RoslynFloorVersion` dans `Directory.Build.props` est l'unique source de version.
2. Les références du projet analyzer compilent contre ce floor.
3. `RoslynFloorTests` refuse tout assembly `Microsoft.CodeAnalysis*` référencé
   au-dessus du floor.
4. Le job de floor empaquette le véritable artefact NuGet et charge l'analyzer
   empaqueté sous le SDK du floor, prouvant à la fois le chargement et le chemin
   de packaging.
5. Dependabot ne doit pas relever les références runtime Roslyn comme une mise à
   jour ordinaire.

Le câblage exact du job et l'isolation NuGet sont décrits dans la
[référence du workflow `analyzers`](../workflows/analyzers.fr.md).

## Implémentation du runtime de l'outillage

Les trois projets d'outillage ciblent uniquement `net8.0` :

| Projet | Politique de roll-forward | Contrat |
|---|---|---|
| `FirstClassErrors.Cli` | `Major` | S'exécute sur la majeure installée suivante lorsque .NET 8 est absent. |
| `FirstClassErrors.GenDoc.Worker` | `LatestMajor` | S'exécute sur la plus haute majeure installée afin de charger une cible compilée pour ce runtime présent. |
| `FirstClassErrors.GenDoc` | aucune | Chargé in-process ; la configuration runtime de la CLI pilote l'exécution. |

Le build ordinaire garde la surface d'API `net8.0`. Le job `floor` exécute
l'outillage livré sur .NET 8, tandis que `canary.yml` exerce le roll-forward sur
la prochaine preview. Les commandes et overrides exacts vivent dans la
[référence du workflow `ci`](../workflows/ci.fr.md).

## Implémentation du floor .NET Framework

Le job `framework-floor` exécute sous Windows la surface de tests des
bibliothèques en `net472`. Les projets activent cette cible via
`build/Net472TestFloor.props` et `EnableNet472Floor` ; le build local et la CI
ordinaires conservent leur cible habituelle. Le job exclut volontairement les
tests dont les fixtures requièrent des API absentes de .NET Framework, tout en
exerçant chaque bibliothèque livrée par un projet de test adapté.

La documentation des packages doit continuer d'annoncer **.NET Framework
4.7.2+**. Lorsque les réglages du dépôt le permettent, la protection de branche
doit rendre le statut framework-floor obligatoire.

## Changer un floor

Un changement de floor est une décision d'architecture, pas un simple édit de
spécification. Il doit :

1. introduire un ADR de remplacement ;
2. modifier la version ou le TFM source de vérité ;
3. mettre à jour les jobs de floor et de canary correspondants ;
4. mettre à jour la documentation des packages et des utilisateurs ;
5. conserver un test qui exécute l'artefact livré sur la nouvelle frontière annoncée.
