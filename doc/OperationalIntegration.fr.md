# CI/CD et intégration opérationnelle

DiagnosableExceptions révèle toute sa valeur lorsqu’il est intégré dans la chaîne de livraison et les outils opérationnels. L’objectif n’est pas seulement de définir la connaissance liée aux erreurs, mais de la rendre automatiquement accessible aux personnes qui en ont besoin : développeurs, équipes de support et opérateurs.

## 📦 La documentation comme artefact de build

La documentation des erreurs doit être générée automatiquement pendant la CI.

Étape typique dans un pipeline :

1. Compiler la solution  
2. Exécuter l’outil d’extraction de la documentation  
3. Générer le catalogue d’erreurs (HTML/Markdown)  
4. Le publier comme artefact du pipeline ou le déployer sur un portail documentaire  

Cela garantit que la documentation correspond toujours à la version du système déployée. Aucune mise à jour manuelle n’est nécessaire et aucune dérive ne peut apparaître.

## 🌍 Publication de la documentation

La documentation générée peut être :

* publiée sur un portail documentaire interne  
* exposée via un site statique  
* attachée aux artefacts de release  

Le principe clé est :

> La documentation doit être accessible aux personnes qui investiguent les incidents en production.

## 📜 Intégration avec les logs

DiagnosableExceptions est conçu pour s’intégrer naturellement avec le logging structuré.

Les logs peuvent inclure :

* `ErrorCode`  
* `ShortMessage`  
* `InstanceId`  
* `OccurredAt`  
* le contexte de diagnostic  

Cela rend les logs non seulement lisibles, mais aussi corrélables entre systèmes.

## 🔍 Logging des inner exceptions

Par défaut, la plupart des configurations de logging traitent les exceptions comme de simples messages ou des stack traces. Elles ne parcourent pas automatiquement plusieurs inner exceptions de manière structurée et exploitable pour l’analyse.

Comme `DiagnosableException` peut agréger plusieurs inner exceptions, un filtre de logging ou un middleware devrait explicitement les extraire et les logger. Sans cela, une partie de l’information de diagnostic portée par le modèle peut rester inutilisée dans les logs.

Ce filtre peut :

* détecter les `DiagnosableException`  
* extraire leurs inner exceptions  
* logger toute la chaîne de manière structurée  

Cela préserve la profondeur diagnostique et garantit que la richesse du modèle d’erreur est réellement visible dans les logs opérationnels.

## 🔗 Lier les logs à la documentation

Un pattern puissant consiste à enrichir les exceptions diagnostiquables avec une URL vers la documentation.

Lors de la génération de la documentation, chaque erreur peut être associée à une page ou une ancre. Un filtre de logging peut alors renseigner :

```csharp
exception.HelpLink = "[https://docs.mycompany/errors/AMOUNT_CURRENCY_MISMATCH](https://docs.mycompany/errors/AMOUNT_CURRENCY_MISMATCH)"
```

Les logs de production deviennent ainsi navigables : le support peut passer directement d’une entrée de log à la documentation correspondante de l’erreur.

## 🧩 Complémentaire au logging structuré

DiagnosableExceptions ne remplace ni le logging structuré, ni les scopes, ni les correlation IDs.

Il les complète :

* logging structuré → contexte technique  
* scopes → contexte d’exécution  
* DiagnosableExceptions → signification sémantique de l’erreur  

Ensemble, ils donnent une vision complète de ce qui s’est passé.

## 🎯 L’objectif

L’intégration industrielle transforme les erreurs en langage opérationnel partagé.

Les erreurs deviennent :

* documentées  
* traçables  
* recherchables  
* exploitables  

**automatiquement**, dans le cadre du processus de build et de livraison — sans dépendre d’efforts manuels de documentation.

---

Section précédente: [Bonnes pratiques](BestPractices.fr.md) | Section suivante: [Architecture du pipeline de documentation](ArchitectureOfTheDocumentationPipeline.fr.md)

---