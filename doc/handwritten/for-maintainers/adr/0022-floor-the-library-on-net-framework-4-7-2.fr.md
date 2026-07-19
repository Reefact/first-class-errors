# ADR-0022 | Fixer le plancher .NET Framework de la bibliothèque à 4.7.2

🌍 🇬🇧 [English](0022-floor-the-library-on-net-framework-4-7-2.md) · 🇫🇷 Français (ce fichier)

**Statut :** Accepté
**Date :** 2026-07-19
**Décideurs :** Reefact

## Contexte

Les bibliothèques livrées ciblent `netstandard2.0`, dont le minimum formel sur .NET Framework est 4.6.1.

Sur les versions antérieures à .NET Framework 4.7.2, la prise en charge de `netstandard2.0` dépend de façades ajoutées a posteriori, d'assets de packages supplémentaires et de redirects de binding côté consommateur. .NET Framework 4.7.2 est la première version qui fournit les façades nécessaires nativement et constitue le minimum pratique recommandé pour une consommation fiable.

Le dépôt annonçait auparavant une prise en charge de .NET Framework 4.6.1 sans exécuter les bibliothèques sur ce runtime. Une promesse de compatibilité qui n'est pas exercée ne peut pas constituer une frontière de support fiable.

La pile de tests actuelle peut s'exécuter sur .NET Framework 4.7.2 mais pas sur les versions antérieures. L'outillage possède un plancher distinct défini par l'ADR-0002.

## Décision

Le plancher .NET Framework pris en charge pour les bibliothèques `netstandard2.0` livrées est **4.7.2**.

## Justification

4.7.2 est la version la plus basse sur laquelle les bibliothèques peuvent être consommées sans la plomberie de compatibilité fragile exigée par les versions antérieures.

C'est également la plus basse version que le dépôt peut exercer avec sa pile de tests prise en charge. Aligner le plancher documenté sur un runtime vérifié en continu transforme une déclaration de compatibilité théorique en contrat imposable.

La décision choisit volontairement la frontière pratique et testable plutôt que le minimum théorique de `netstandard2.0`. Les versions inférieures exigeraient une seconde pile de tests et des comportements de binding spécifiques à l'environnement pour une valeur utilisateur désormais limitée.

Cet ADR raffine la mention incidente de .NET Framework 4.6.1 auparavant présente dans l'ADR-0002 ; il ne remplace pas l'ADR-0002, car cette décision concerne l'outillage exécutable et non les bibliothèques.

Le job Windows exact, les cibles de tests conditionnées, les polyfills, les exclusions de projets et la couverture des previews sont documentés dans la [référence d'implémentation des ADR](../specifications/adr-implementation-reference.fr.md#plancher-dexécution-des-outils) et la référence du workflow CI.

## Alternatives envisagées

### Continuer à annoncer .NET Framework 4.6.1

Envisagé parce qu'il s'agit du minimum formel de `netstandard2.0`. Rejeté parce que cette déclaration n'était pas vérifiée et dépend d'une plomberie fragile côté consommateur sur des runtimes largement obsolètes.

### Fixer le plancher à .NET Framework 4.6.2

Envisagé car cette version est restée maintenue plus longtemps que 4.6.1. Rejeté parce qu'elle présente les mêmes contraintes de façades et de redirects de binding et ne peut pas être vérifiée avec la pile de tests prise en charge.

### Tester chaque version majeure moderne de .NET dans une matrice bloquante

Envisagé pour une assurance large. Rejeté parce que la frontière de compatibilité utile est celle entre .NET Framework et .NET moderne, tandis que le dernier runtime et la preview couvrent l'autre extrémité sans recréer une maintenance à chaque release.

## Conséquences

### Positives

* La prise en charge de .NET Framework est vérifiée en continu plutôt que simplement affirmée.
* Le plancher pratique évite la fragilité des redirects de binding côté consommateur.
* Les frontières de runtime de la bibliothèque et de l'outillage sont énoncées séparément et précisément.
* Le plancher .NET Framework est stable puisque la plateforme n'ajoute plus de nouvelles versions majeures.

### Négatives

* Les consommateurs sur .NET Framework 4.6.1 à 4.7.1 sortent de la plage prise en charge.
* Une couverture de compatibilité Windows et une plomberie de cibles de tests dédiées doivent être maintenues.

### Risques

* Certains scénarios du Request Binder utilisent des types réservés au .NET moderne et ne peuvent pas s'exécuter sur le plancher framework. Mesure : couvrir l'assembly livré du binder par des suites compatibles et conserver les exclusions explicites dans la référence d'implémentation.
* Un job de plancher peut exister sans être imposé par la protection de branche. Mesure : maintenir ce job comme statut obligatoire lorsque les réglages du dépôt le permettent.

## Actions de suivi

* Maintenir la déclaration utilisateur à .NET Framework 4.7.2 ou supérieur.
* Maintenir le contrôle du plancher framework comme condition obligatoire de fusion.

## Références

* [Référence d'implémentation des ADR — Plancher d'exécution des outils](../specifications/adr-implementation-reference.fr.md#plancher-dexécution-des-outils)
* [ADR-0002](0002-floor-the-tooling-runtime.fr.md) — raffiné par cet ADR pour le plancher .NET Framework de la bibliothèque.
* [ADR-0001](0001-lock-the-analyzer-roslyn-floor.fr.md)
* `FirstClassErrors/README.nuget.md` et la référence du workflow CI.
* [ADR-0023](0023-allow-a-one-time-editorial-refactoring-of-accepted-adrs.fr.md) — autorise cette extraction éditoriale.
