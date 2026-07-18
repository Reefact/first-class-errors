# ADR-0019 | Documenter les erreurs de binder surchargées dans le catalogue du consommateur

🌍 🇬🇧 [English](0019-document-overridden-binder-errors-in-the-consumers-catalog.md) · 🇫🇷 Français (ce fichier)

**Statut :** Proposé
**Date :** 2026-07-18
**Décideurs :** Reefact

## Contexte

* L’ADR-0018 (qui a remplacé l’ADR-0016) a rendu les deux erreurs structurelles du binder
  configurables comme un `BinderErrorDefinition` — code et messages publics ensemble — sur
  `RequestBinderOptions`. Les deux ADR ont différé une question vers l’issue #140 : comment le
  catalogue généré d’un consommateur fait apparaître les codes du binder — éventuellement
  surchargés — sans dériver de ce qu’il émet au runtime.
* Le générateur de documentation documente une erreur à partir d’une fabrique statique portant
  `[DocumentedBy]` dans un type `[ProvidesErrorsFor]` : il exécute la méthode de documentation
  nommée (la prose) et l’exemple qu’elle porte (une erreur vivante), en lisant le code, les
  messages et le contexte sur l’erreur construite. Il découvre ces types en scannant les
  projets opt-in de la solution ; il ne scanne pas les paquets référencés.
* Le binder fabrique lui-même ses deux erreurs structurelles — leurs fabriques sont internes —
  et documente déjà ses propres défauts dans le catalogue de son paquet.
* Quand un consommateur surcharge une définition, il possède le code et les messages effectifs :
  ils vivent dans son propre assembly (le `BinderErrorDefinition` injecté dans les options) et
  sont de la configuration runtime, non découvrable statiquement depuis le binaire du binder.
* La prose des erreurs structurelles du binder (titre, règle, diagnostics) est indépendante du
  code : elle décrit le sens de « un argument requis manquait » quel que soit le code qui le
  porte.
* Le modèle d’erreurs codées de la bibliothèque documente une erreur là où elle est définie, via
  `[ProvidesErrorsFor]` / `[DocumentedBy]`, et interdit de référencer une erreur par une chaîne
  magique.
* Un seul paquet livre aujourd’hui des codes documentés et émissibles
  (`FirstClassErrors.RequestBinder`), et la bibliothèque est en pré-version sans consommateur
  externe.

## Décision

Un consommateur fait apparaître ses erreurs structurelles de binder surchargées dans son propre
catalogue généré en les documentant dans son propre type `[ProvidesErrorsFor]` — bâti à partir
de seams publics du binder qui réutilisent la prose indépendante du code et construisent un
exemple fidèle depuis la définition du consommateur — plutôt que par une découverte automatique
des codes d’un paquet référencé par le générateur.

## Justification

* Le consommateur possède les codes effectifs (ADR-0018) : les documenter dans son propre
  catalogue — là où le modèle d’erreurs codées documente toute autre erreur possédée — garde une
  règle unique plutôt qu’un chemin cross-paquet spécial.
* Réutiliser la prose indépendante du code via des seams publics fait que le consommateur n’écrit
  aucun texte de description et ne construit aucune erreur à la main : les faits mécaniques (code,
  messages, transience, clé de contexte du chemin d’argument, câblage de l’erreur interne)
  viennent du binder qui bâtit l’exemple comme à la liaison, si bien que l’entrée documentée ne
  peut pas dériver de ce qui est émis — fermant le risque même que #140 nommait.
* Le générateur n’a pas besoin de changer : le catalogue du consommateur est un type
  `[ProvidesErrorsFor]` / `[DocumentedBy]` ordinaire dans un projet déjà scanné. Cela évite la
  machinerie qu’exigerait une étape de découverte automatique — résoudre la clôture de références
  d’un projet, un pré-check métadonnée pour écarter les assemblys non documentants, exécuter le
  worker sur des binaires tiers, une garde de version de contrat de documentation et une
  politique de collision de codes — et ses pièges de justesse, puisqu’un paquet référencé n’est
  pas toujours émis sur la surface publique et que ses défauts sont faux pour un consommateur qui
  les a surchargés.
* Garder le lien entre une erreur et sa description dans le code, via `nameof` / `typeof`, le
  laisse sous l’œil du compilateur — cohérent avec la position de la bibliothèque contre les
  chaînes magiques — là où un lien exprimé en configuration de build les réintroduirait.
* Avec un seul paquet à codes documentés et aucun consommateur, la petite glue par consommateur
  est un prix acceptable, et la surface des options comme les seams de documentation sont arrêtés
  avant de s’engager sur une découverte automatique.

## Alternatives considérées

### Découvrir automatiquement les codes documentés d’un paquet référencé

Considérée parce qu’elle est sans boilerplate : un consommateur référence le paquet et ses codes
apparaissent dans son catalogue.

Rejetée pour l’instant parce qu’elle est une machinerie disproportionnée pour un paquet et aucun
consommateur ; elle ne peut pas représenter les codes effectifs d’un consommateur qui surcharge,
qui sont de la configuration runtime absente du binaire du paquet ; et elle risque de documenter
un paquet référencé mais non émis, ou les défauts d’un paquet qu’un consommateur a surchargés.
Elle reste ouverte comme amélioration ergonomique future si l’écosystème grandit.

### Un lien en configuration de build reliant une erreur à une méthode de description

Considérée parce qu’elle sort le câblage de documentation du code vers la configuration du projet.

Rejetée parce qu’elle référencerait des membres par chaîne dans les fichiers projet —
réintroduisant les chaînes magiques que le modèle d’erreurs codées existe pour éliminer, sans
contrôle à la compilation, ni navigation, ni sûreté au renommage. Un attribut d’assembly
compile-safe serait le repli si un lien déclaratif était un jour souhaité.

## Conséquences

### Positives

* La moitié « surcharge » de #140 est fermée : un consommateur documente exactement ce qu’il émet,
  fidèle au runtime, avec la prose du binder et sans changer le générateur.
* Le lien entre une erreur et sa documentation reste compile-safe et dans le code, cohérent avec
  la position de la bibliothèque sur les chaînes magiques.

### Négatives

* Un consommateur fait entrer les codes du binder dans son catalogue avec un petit type de glue
  par erreur (un type `[ProvidesErrorsFor]` déléguant aux seams publics) ; il n’y a pas de
  découverte automatique sans boilerplate.
* Le binder gagne quatre membres publics (un seam de description et un d’exemple par erreur
  structurelle). Les deux seams d’exemple renvoient une erreur sans `[DocumentedBy]` et sont
  supprimés contre FCE009 comme helpers délibérément hors catalogue.

### Risques

* Un consommateur pourrait appeler un seam d’exemple pour fabriquer une erreur structurelle hors
  documentation. Il produit la même forme que ce que le binder émet et n’est injecté nulle part :
  il est donc inerte — pas différent de bâtir n’importe quelle erreur via le `PrimaryPortError.Create`
  public.

## Actions de suivi

* La découverte automatique des codes des paquets référencés (la direction plus large de #140)
  reste ouverte, à revisiter si plusieurs paquets à codes documentés ou des consommateurs externes
  apparaissent.

## Références

* ADR-0018 — regrouper le code et les messages d’une erreur structurelle du binder dans une seule
  définition ; l’appropriation sur laquelle cette décision s’appuie.
* ADR-0016 — rendre configurables les codes d’erreur structurels du binder (remplacée) ; elle a
  d’abord différé ce sujet vers #140.
* Issue #140 — documenter les codes d’erreur des paquets FirstClassErrors référencés dans le
  catalogue d’un consommateur.
* FCE009 — `ErrorFactoryNotDocumented`, l’analyseur contre lequel les seams d’exemple sont
  supprimés.
