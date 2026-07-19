# ADR-0001 | Verrouiller le floor Roslyn de l'analyzer

🌍 🇬🇧 [English](0001-lock-the-analyzer-roslyn-floor.md) · 🇫🇷 Français (ce fichier)

**Statut :** Accepté
**Date :** 2026-07-10
**Décideurs :** Reefact

## Contexte

`FirstClassErrors.Analyzers` est intégré au package NuGet `FirstClassErrors` : il
est donc chargé par le compilateur de chaque consommateur, pas par un runtime
contrôlé par le projet. La version de Roslyn référencée par l'analyzer devient le
compilateur minimum capable de le charger ; une référence plus récente peut faire
émettre `CS8032` et supprimer silencieusement les diagnostics.

Cette frontière régresse facilement parce qu'une CI et un IDE modernes satisfont
naturellement un floor plus récent. Le projet a déjà dérivé vers une version de
Roslyn supérieure à celle de l'hôte supporté le plus ancien, et une mise à jour de
dépendance ordinaire peut reproduire l'échec. Le packaging peut aussi casser
indépendamment : un assembly correct absent de `analyzers/dotnet/cs/` n'est jamais
chargé.

L'hôte analyzer pris en charge le plus ancien est le SDK .NET 8.0.100 / Visual
Studio 2022 17.8, dont le compilateur est Roslyn 4.8.

## Décision

Le floor de compatibilité Roslyn de l'analyzer est fixé à **4.8.0**, correspondant au compilateur pris en charge le plus ancien, et protégé comme frontière explicite de compatibilité du produit.

## Justification

Un floor inférieur n'ajoute aucun hôte pris en charge ; un floor supérieur en
abandonne un annoncé par le produit. La frontière doit être défendue sur ses modes
d'échec distincts : dérive des références, chargement par l'hôte, placement dans
le package et mises à jour automatiques. Ces garde-fous partagent une source de
vérité et exercent l'artefact empaqueté sur l'hôte du floor ; leur implémentation
courante est maintenue dans la
[spécification de compatibilité](../specifications/platform-compatibility.fr.md)
et la [référence du workflow `analyzers`](../workflows/analyzers.fr.md).

Sortir les détails d'implémentation de cet ADR permet d'améliorer les garde-fous
sans réécrire la décision.

## Alternatives envisagées

### Suivre le Roslyn courant

Envisagé parce que c'est le comportement par défaut sans maintenance. Rejeté car
chaque bump élève silencieusement le SDK ou l'IDE minimum, exactement comme lors
de la régression initiale.

### Épingler la version sans vérification indépendante

Envisagé comme correctif minimal. Rejeté car l'épinglage peut encore être modifié
comme maintenance ordinaire et ne prouve pas le chargement du package sur l'hôte
le plus ancien.

### Vérifier uniquement les versions d'assemblies référencées

Envisagé parce qu'un test in-process est rapide. Rejeté car il ne prouve ni le
placement dans le package ni le chargement authentique par le compilateur du floor.

## Conséquences

### Positives

* Le compilateur minimum de l'analyzer est explicite et gardé mécaniquement.
* Le package livré, pas seulement les références du projet, est exercé sur le floor.
* Relever le floor devient une décision consciente de compatibilité.

### Négatives

* Les garde-fous et un build sur l'hôte du floor doivent être maintenus.
* Les mises à jour Roslyn exigent une revue délibérée.

### Risques

* Un mainteneur pourrait simplifier un garde-fou sans comprendre le mode d'échec
  qu'il couvre. Atténuation : la spécification évolutive et la référence de
  workflow documentent les responsabilités courantes.

## Actions de suivi

* Un futur changement de floor doit remplacer cet ADR et mettre à jour la
  spécification, la documentation du package et la validation du floor.

## Références

* [Spécification de compatibilité des plateformes](../specifications/platform-compatibility.fr.md).
* [Référence du workflow `analyzers`](../workflows/analyzers.fr.md).
* [ADR-0002](0002-floor-the-tooling-runtime.fr.md) — le pendant runtime de l'outillage.
* Issues #69, #74, #75 et #77 — verrouillage initial et durcissements.
