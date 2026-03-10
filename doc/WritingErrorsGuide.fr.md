# Guide d’écriture des erreurs

DiagnosableExceptions vous fournit des outils. Ce guide vous aide à les utiliser de manière cohérente et porteuse de sens.

L’objectif n’est pas seulement de lever des exceptions, mais d’**exprimer les erreurs de façon claire, précise et utile** pour des humains.

## 🎯 1. Pensez en *situations d’erreur*, pas seulement en échecs

Chaque erreur documentée doit représenter :

> **une situation précise dans laquelle le système ne peut pas continuer comme prévu**

Évitez les erreurs vagues ou génériques comme :

* « Opération invalide »  
* « Erreur de traitement »  
* « Problème inattendu »  

Préférez des situations précises et contextualisées :

* « Incohérence de devise des montants »  
* « Température sous le zéro absolu »  
* « Date de transaction hors période du relevé »  

Une erreur doit décrire *ce qui s’est mal passé en termes métier*, pas la réaction du système.

## 🏷️ 2. Écrire un bon **code d’erreur**

Le code d’erreur est l’identifiant stable, lisible par machine.

Bonnes pratiques :

* Utiliser un **périmètre métier clair** `AMOUNT_CURRENCY_MISMATCH`  
* Le garder **stable dans le temps**  
* Éviter les détails techniques (pas de noms de classes, pas de noms de méthodes)  
* Un code = une situation d’erreur documentée  

Considérez le code d’erreur comme un contrat d’API.

## 🧾 3. Écrire le **Title**

Le titre est un résumé humain court.

Il doit :

* être concis  
* décrire la situation, pas la conséquence  
* éviter le vocabulaire technique  

Bon :

* « Incohérence de devise des montants »  
* « Température sous le zéro absolu »  

À éviter :

* « InvalidAmountOperationException »  
* « L’opération a échoué »  

## 📝 4. Écrire la **Description**

La description explique la signification de l’erreur.

Un bon schéma est :

> « Cette erreur survient lorsque… »

La description doit :

* décrire la situation en langage simple  
* être compréhensible par quelqu’un qui ne connaît pas le code  
* expliquer *ce qui s’est passé*, pas *comment le système a réagi*  

La cohérence dans la formulation améliore la lisibilité globale de la documentation.

## 📏 5. Écrire la **régle**

La règle exprime l’invariant ou la contrainte métier.

Elle doit :

* être formulée comme une vérité générale  
* décrire ce qui doit toujours être respecté  

Exemples :

* « Toutes les opérations monétaires doivent impliquer des montants exprimés dans la même devise. »  
* « La température ne peut pas descendre sous le zéro absolu. »  

S’il n’y a pas de règle explicite, il est acceptable d’omettre cette section.

## 🔍 6. Écrire une bonne **Cause**

Une cause décrit une raison plausible de l’erreur.

Elle doit :

* décrire un **état ou une condition**, pas une action  
* éviter toute accusation  
* être suffisamment précise pour guider l’investigation  

Bon :

* « Des montants ont été utilisés dans une opération monétaire sans avoir été convertis dans la même devise. »  

À éviter :

* « Le développeur a oublié de convertir la devise. »  
* « Corriger les données. »  

## 🧭 7. Écrire une bonne **piste d’analyse** (AnalysisLead)

Une piste d’analyse suggère où regarder en premier.

Elle doit :

* commencer par un verbe neutre comme *Vérifier*, *Contrôler*, *Examiner*  
* guider l’investigation, pas définir des procédures  
* éviter les détails de processus de support  

Bon :

* « Vérifier si tous les montants impliqués ont été convertis dans une devise commune avant d’être utilisés ensemble. »  

À éviter :

* « Ouvrir un ticket. »  
* « Contacter l’équipe X. »  

## 🧪 8. Écrire de bons **Exemples**

Les exemples illustrent l’apparence de l’erreur en pratique.

Ils doivent :

* utiliser des valeurs réalistes  
* être simples et clairs  
* mettre en évidence la violation de la règle, pas des cas extrêmes  

Les exemples ne sont pas des tests — ils ont un rôle pédagogique.

## 🧠 9. Séparer le domaine du bruit technique

La documentation des erreurs doit se concentrer sur :

* le sens métier  
* les règles violées  
* les causes plausibles  

Évitez d’y faire apparaître :

* des stack traces  
* des détails du framework  
* des noms de classes internes  

## 🏁 Résumé

Quand vous écrivez une erreur :

| Élément         | Rôle                          |
| --------------- | ----------------------------- |
| Code d’erreur   | Identifiant stable            |
| Titre           | Résumé humain court           |
| Description     | Signification de l’erreur     |
| Règle           | Invariant violé               |
| Cause           | Pourquoi cela a pu arriver    |
| Piste d'analyse | Où commencer l’investigation  |
| Exemples        | À quoi cela ressemble         |

Des erreurs bien écrites ne sont pas simplement levées. Elles deviennent une partie de la **compréhension partagée du fonctionnement — et des échecs — du système.**

---

Section précédente: [Guide du contexte d’erreur](ErrorContext.fr.md) | Section suivante: [Cas d’usage](UsagePatterns.fr.md)

---