# Workflow `analyzers`

🌍 🇬🇧 [English](analyzers.en.md) · 🇫🇷 Français (ce fichier)

> Documentation mainteneur — fait partie de la [référence des workflows](README.fr.md).
> Ne fait pas partie de la documentation utilisateur sous `doc/`.

**Fichier du workflow :** [`.github/workflows/analyzers.yml`](../../.github/workflows/analyzers.yml)

## À quoi il sert

La librairie livre des analyzers Roslyn (`FCExxx`) **embarqués dans le package
NuGet**. Ce workflow prouve deux choses que la CI ordinaire ne prouve pas :

1. **Dogfood** — les analyzers tournent réellement et ne signalent rien
   d'indu quand on construit le projet d'exemple qui les référence.
2. **Floor check** — l'analyzer *tel que livré*, packagé exactement comme un
   consommateur le reçoit, **se charge et s'exécute sous le plus vieux
   compilateur supporté** (Roslyn 4.8.0 == SDK .NET 8 / Visual Studio 2022 17.8).

Le floor check existe parce qu'un analyzer embarqué compilé contre un Roslyn plus
récent ne se charge pas (`CS8032`) sur les SDK/IDE plus anciens — dégradant
silencieusement le produit pour ces utilisateurs. Voir
[ADR 0001 — Verrouiller le floor Roslyn de l'analyzer](../adr/0001-lock-the-analyzer-roslyn-floor.md)
pour le raisonnement complet *(rédigé en anglais)* ; ce workflow en est la mise
en application côté CI.

## Quand il s'exécute

- À chaque **push sur `main`**, **pull request visant `main`**, et à la demande
  via **`workflow_dispatch`**.

## Comment il s'exécute

Deux jobs :

- **`dogfood`** — construit `FirstClassErrors.Usage` avec les analyzers branchés
  (`OutputItemType=Analyzer`) ; échoue sur tout diagnostic `FCExxx` de sévérité
  `Error`. (Les *tests unitaires* de l'analyzer tournent dans [`ci`](ci.fr.md),
  qui build et teste toute la solution.)
- **`floor`** — le vrai test de contrat :
  1. Installer **deux SDK** : le SDK de release (10.0.x, celui avec lequel
     `release` packe) et le SDK floor (8.0.100, Roslyn 4.8).
  2. **Packer** `FirstClassErrors` sous le SDK *de release* — l'artefact exact
     qu'un consommateur restaure.
  3. **Consommer** ce package depuis `tools/floor-check/`, dont le `global.json`
     imbriqué épingle le build au SDK *floor*.
  4. **Prouver que l'analyzer s'est chargé** en grep-ant le log `ReportAnalyzer`
     à la recherche d'un *type* d'analyzer pleinement qualifié.

## Permissions & sécurité

`contents: read` seulement — il build et packe localement, ne publie rien.

## À manipuler avec précaution

Ce job est dense parce que chaque ligne bouche un trou précis. Avant de
l'éditer, lisez les commentaires du YAML — ils font foi. Les pièges :

- **Le pack tourne sous le SDK de release, la consommation sous le SDK floor.**
  Packer sous le SDK floor testerait un analyzer que personne ne livre et
  épinglerait la librairie à C# 12. Ce découpage est tout l'intérêt.
- **`FLOORCHECK_VERSION` porte un suffixe `run_number.run_attempt`** pour que
  chaque run produise une version que NuGet n'a jamais mise en cache, forçant
  l'étape de consommation à restaurer le `.nupkg` fraîchement packé *de ce run*
  plutôt qu'une copie périmée. `FloorCheck.csproj` épingle cette version
  **exacte** (pas un flottant), donc il ne peut jamais résoudre silencieusement
  un futur `FirstClassErrors` stable depuis nuget.org.
- **La sélection du SDK dépend du répertoire courant.** L'étape de consommation
  choisit le SDK floor *parce qu*'elle tourne depuis `tools/floor-check/` avec un
  `global.json` imbriqué (`rollForward: disable`). Sortez l'étape de ce
  répertoire et elle build silencieusement sur le mauvais SDK.
- **`ReportAnalyzer=true` + `-v detailed` + `--no-incremental` sont tous
  requis** pour faire remonter la table par-analyzer de Roslyn dans le log.
  Retirez-en un et le grep « prouve qu'il s'est chargé » n'a plus rien à
  matcher.
- **Le grep matche un *type* d'analyzer (`…QuelqueChoseAnalyzer`), pas le nom de
  l'assembly.** Le nom de l'assembly apparaît dans des lignes de build ordinaires
  même si l'analyzer ne s'est jamais chargé ; seul le type apparaît dans la table
  `ReportAnalyzer`. Un analyzer jamais chargé laisserait sinon le build vert.

**Pour relever le floor délibérément**, suivez la procédure de l'ADR 0001 —
c'est une décision produit, pas une montée de routine (raison pour laquelle
Dependabot est configuré pour ignorer `Microsoft.CodeAnalysis.*`).

## En rapport

- [ADR 0001 — Verrouiller le floor Roslyn de l'analyzer](../adr/0001-lock-the-analyzer-roslyn-floor.md)
- [`ci`](ci.fr.md) — exécute la suite de tests unitaires de l'analyzer dans le
  cadre de la solution complète.
