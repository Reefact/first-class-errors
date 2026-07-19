# ADR-0023 | Autoriser un refactoring éditorial unique des ADR acceptés

🌍 🇫🇷 Français (ce fichier) · 🇬🇧 [English](0023-allow-a-one-time-editorial-refactoring-of-accepted-adrs.md)

**Statut :** Accepté
**Date :** 2026-07-19
**Décideurs :** Reefact

## Contexte

Le corpus d'ADR définit les décisions acceptées comme des archives historiques immuables et précise également qu'un ADR est un relevé de décision, pas une spécification d'implémentation.

Plusieurs ADR acceptés sont antérieurs à cette séparation ou ne l'appliquent qu'imparfaitement. Ils contiennent des chemins de projets exacts, des propriétés de configuration, des étapes de workflows, des séquences de commandes, des signatures d'API, des détails algorithmiques ou des procédures de maintenance susceptibles d'évoluer alors que la décision architecturale demeure valide.

Conserver ces détails crée deux règles contradictoires : les ADR acceptés ne peuvent pas être modifiés, mais les détails d'implémentation qu'ils contiennent finissent inévitablement par devenir obsolètes. Cela rend aussi plus difficile la distinction entre la décision durable et sa réalisation technique actuelle.

Le mainteneur a examiné le corpus et autorisé une migration éditoriale unique à condition qu'elle ne modifie aucune décision, justification, alternative, conséquence, statut historique ni attribution.

## Décision

Le dépôt autorisera un refactoring éditorial traçable et unique des ADR acceptés existants afin de déplacer les spécifications d'implémentation vers une documentation de référence dédiée sans modifier leur sens architectural.

## Justification

Cette migration résout une contradiction du modèle de gouvernance actuel tout en préservant la valeur historique des enregistrements. La décision durable et son raisonnement restent dans chaque ADR ; les mécanismes volatils sont déplacés vers une documentation appelée à évoluer avec l'implémentation.

Formaliser la migration comme une décision architecturale rend l'exception visible et limitée. Cela évite qu'elle devienne un précédent informel permettant de réécrire silencieusement des décisions acceptées.

Une référence thématique unique est préférable à une multiplication d'ADR de remplacement, car ces détails décrivent des contrats et procédures actuels plutôt que de nouvelles décisions architecturales.

## Alternatives envisagées

### Laisser les ADR acceptés inchangés

Cette option préserverait une immutabilité stricte, mais conserverait également des éléments d'implémentation obsolètes ou excessivement détaillés et continuerait de contredire la distinction entre décisions et spécifications.

### Remplacer chaque ADR concerné par un nouvel ADR

Cette option préserverait l'immutabilité historique, mais créerait de nombreux successeurs artificiels alors qu'aucune décision n'a changé. L'historique donnerait l'impression de revirements architecturaux là où seul un travail éditorial a eu lieu.

### Retirer les détails sans enregistrer d'exception

Cette option serait plus simple, mais rendrait la gouvernance du dépôt incohérente et créerait un précédent non documenté de réécriture des ADR acceptés.

## Conséquences

### Positives

* Les ADR existants deviennent plus courts, plus durables et plus faciles à relire comme décisions architecturales.
* Les détails d'implémentation disposent d'un emplacement maintenable qui peut évoluer sans réécrire l'historique.
* La politique ADR du dépôt devient cohérente avec elle-même.
* Les liens croisés peuvent rendre explicites les raffinements et décisions ultérieures sans modifier le sens initial.

### Négatives

* Le texte historique des ADR concernés change une fois, même si leurs décisions ne changent pas.
* Les relecteurs doivent vérifier qu'aucun sens architectural n'a été perdu pendant l'extraction.
* La migration crée et impose de maintenir un document de référence supplémentaire.

### Risques

* Une réécriture éditoriale pourrait modifier involontairement la portée ou la force d'une décision. Mesure : conserver la phrase de décision, la justification, les alternatives, les conséquences, le statut, la date et les décideurs sauf correction de gouvernance autorisée séparément.
* L'exception pourrait être réutilisée plus tard pour justifier la réécriture de décisions acceptées. Mesure : cet ADR n'autorise que la migration identifiée dans ses références ; toute modification future d'une décision exige toujours un ADR qui la remplace.

## Actions de suivi

* Extraire les éléments spécifiques à l'implémentation des ADR concernés vers la référence bilingue d'implémentation des ADR.
* Ajouter des liens explicites entre les ADR qui raffinent, réexaminent ou mettent à jour la forme d'API de décisions antérieures.
* Corriger le statut des décisions déjà implémentées et approuvées par le mainteneur.
* Relire le diff final spécifiquement pour détecter toute modification sémantique d'une décision acceptée.

## Références

* [Référence d'implémentation des ADR](../specifications/adr-implementation-reference.fr.md)
* [Corpus et conventions des ADR](README.fr.md)
