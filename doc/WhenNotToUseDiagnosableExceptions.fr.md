# Quand ne pas utiliser DiagnosableExceptions

DiagnosableExceptions est conçu pour des systèmes où les erreurs portent du sens, des règles et des conséquences opérationnelles. Il n’a pas vocation à être utilisé partout.

## 🧪 Prototypes et code jetable

Si le code :

* a une durée de vie courte  
* est exploratoire  
* n’a pas vocation à être maintenu  

alors le coût de la documentation d’erreurs structurée est inutile.

## 🧩 Très petits utilitaires

Pour des outils simples ou des scripts où :

* il n’y a pas de processus de support  
* les erreurs ne concernent que les développeurs  
* le système n’a pas de complexité métier  

les exceptions standard sont généralement suffisantes.

## ⚙️ Bibliothèques techniques bas niveau

Les bibliothèques qui traitent principalement de :

* mémoire  
* primitives de threading  
* internals de sérialisation  
* implémentations de protocoles  

bénéficient souvent davantage d’exceptions techniques que de documentation sémantique.

Cette bibliothèque sert à exprimer le **sens applicatif**, pas les mécanismes bas niveau.

## 🚀 Boucles internes critiques en performance

Dans des chemins extrêmement sensibles à la performance, créer des objets d’exception riches uniquement pour le flux de contrôle peut ne pas être approprié.

Dans ces cas :

* utilisez des validations légères  
* évitez la création d’objets d’erreur si ce n’est pas nécessaire  

## 🔄 Systèmes sans responsabilité long terme

S’il n’y a :

* pas d’équipe de support  
* pas d’investigation opérationnelle  
* pas de besoin de traçabilité de la connaissance liée aux erreurs  

alors le pipeline de documentation apporte peu de valeur.

## 🎯 Règle empirique

Utilisez DiagnosableExceptions lorsque :

* les erreurs représentent des règles ou des contraintes  
* les systèmes sont conçus pour durer  
* plusieurs équipes interagissent avec le logiciel  
* le support et l’exploitation ont besoin de compréhension  

Évitez-le lorsque les erreurs ne sont que des signaux techniques sans signification sémantique durable.

L’objectif de cette bibliothèque n’est pas de rendre toutes les exceptions plus riches. Il est de rendre les erreurs porteuses de sens explicites et durables.

---

Section précédente: [Principes de conception](DesignPrinciples.fr.md) | Section suivante: [Concepts clés](CoreConcepts.fr.md)

---