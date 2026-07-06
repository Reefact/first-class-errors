# CI/CD et intégration opérationnelle

🌍 **Langues:**  
🇬🇧 [English](./OperationalIntegration.en.md) | 🇫🇷 Français (ce fichier)

FirstClassErrors révèle toute sa valeur lorsqu’il est intégré dans la chaîne de livraison et les outils opérationnels. L’objectif n’est pas seulement de définir la connaissance liée aux erreurs, mais de la rendre automatiquement accessible aux personnes qui en ont besoin : développeurs, équipes de support et opérateurs.

## 📦 La documentation comme artefact de build

La documentation des erreurs doit être générée automatiquement pendant la CI.

Étape typique dans un pipeline :

1. Compiler la solution  
2. Exécuter le générateur de documentation `fce` (`fce generate`)  
3. Générer le catalogue d’erreurs (Markdown ou JSON)  
4. Le publier comme artefact du pipeline ou le déployer sur un portail documentaire  

Cela garantit que la documentation correspond toujours à la version du système déployée. Aucune mise à jour manuelle n’est nécessaire et aucune dérive ne peut apparaître.

Vous pouvez produire le catalogue par langue en ajoutant `--language <…>` (par ex. une matrice CI sur `en`, `fr`, `sv`) ; les noms de fichiers et les ancres restent stables d’une langue à l’autre. Voir [Internationalisation](Internationalisation.fr.md).

## 🌍 Publication de la documentation

La documentation générée peut être :

* publiée sur un portail documentaire interne  
* exposée via un site statique  
* attachée aux artefacts de release  

Le principe clé est :

> La documentation doit être accessible aux personnes qui investiguent les incidents en production.

## 📜 Intégration avec les logs

FirstClassErrors est conçu pour s’intégrer naturellement avec le logging structuré.

Les logs peuvent inclure :

* le code d'erreur
* l'identifiant unique de l'erreur
* l'horodatage de l'occurrence
* le contexte d’erreur

Cela rend les logs non seulement lisibles, mais aussi corrélables entre systèmes.

## 🔍 Logging des inner errors

Par défaut, la plupart des configurations de logging traitent les exceptions comme de simples messages ou des stack traces. Elles ne parcourent pas automatiquement l’information de diagnostic portée par une `DiagnosableException` de manière structurée et exploitable pour l’analyse.

Une `DiagnosableException` ne renseigne pas `Exception.InnerException` ; la chaîne de diagnostic vit plutôt sur son `Error`. Via `exception.Error.InnerErrors` (une liste d’`Error`), un filtre de logging ou un middleware devrait explicitement parcourir et logger cette chaîne. Sans cela, une partie de l’information de diagnostic portée par le modèle peut rester inutilisée dans les logs.

Ce filtre peut :

* détecter les `DiagnosableException`  
* lire son `.Error`  
* parcourir `Error.InnerErrors` et logger toute la chaîne de manière structurée  

Cela préserve la profondeur diagnostique et garantit que la richesse du modèle d’erreur est réellement visible dans les logs opérationnels.

## 🔗 Lier les logs à la documentation

Un pattern puissant consiste à enrichir l’exception levée (via son `Error`) avec une URL vers la documentation.

Lors de la génération de la documentation, chaque erreur peut être associée à une page ou une ancre. Un filtre de logging peut alors renseigner :

```
exception.HelpLink = "https://docs.mycompany/errors/AMOUNT_CURRENCY_MISMATCH"
```

Les logs de production deviennent ainsi navigables : le support peut passer directement d’une entrée de log à la documentation correspondante de l’erreur.

## 🧩 Complémentaire au logging structuré

FirstClassErrors ne remplace ni le logging structuré, ni les scopes, ni les correlation IDs.

Il les complète :

* logging structuré → contexte technique  
* scopes → contexte d’exécution  
* FirstClassErrors → signification sémantique de l’erreur  

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

<table width="100%">
<tr>
<td align="left">Section précédente: <a href="BestPractices.fr.md">Bonnes pratiques</a></td>
<td align="center"><a href="README.fr.md#-étapes-suivantes">📚 Table des matières</a></td>
<td align="right">Section suivante: <a href="ArchitectureOfTheDocumentationPipeline.fr.md">Architecture du pipeline de documentation</a></td>
</tr>
</table>

---