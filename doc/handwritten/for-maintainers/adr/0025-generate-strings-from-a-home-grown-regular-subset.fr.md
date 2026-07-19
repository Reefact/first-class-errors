# ADR-0025 | Générer les chaînes qui matchent depuis un sous-ensemble régulier maison

🌍 🇬🇧 [English](0025-generate-strings-from-a-home-grown-regular-subset.md) · 🇫🇷 Français (ce fichier)

**Statut :** Proposé
**Date :** 2026-07-18
**Décideurs :** Reefact

## Contexte

`Dummies` permet à un test de fournir des valeurs arbitraires mais valides. Une règle de validité très courante est
une **expression régulière** de format : un objet-valeur valide son entrée contre un motif (une référence de
commande, un SKU, un code devise), et un test a besoin d'une valeur qui passe cette validation sans réécrire le
format à la main. `Any.StringMatching(motif)` répond à ce besoin — générer une chaîne que le motif matche.

Trois faits cadrent sa construction :

* La bibliothèque est livrée **sans aucune dépendance runtime** et en fait un élément de son identité ; la
  frontière est vérifiée par un test d'architecture (ADR-0011). Ajouter une référence de paquet est donc un choix
  délibéré et visible, pas un détail.
* Générer une chaîne depuis un motif revient à parcourir le motif comme un automate fini. Une bibliothèque .NET
  existe (Fare, le port xeger/brics). C'est une dépendance, peu maintenue, et — comme tout générateur à base
  d'automate — elle ne peut honorer les constructs **non réguliers** (lookaround, backreferences) ; elle tend à les
  ignorer ou les mal traiter silencieusement.
* Les constructs non réguliers ne sont pas un sous-ensemble qu'on choisit d'écarter par confort : un lookahead, une
  backreference, une limite de mot ne sont **pas réguliers**, donc aucun générateur fini ne peut produire de chaînes
  les honorant. Ils sont absents de toute approche à base d'automate, maison ou non.

Le générateur est terminal : le motif est toute la spécification, il n'y a donc rien à réconcilier avec les autres
contraintes de chaîne, et les seules questions de forme ouvertes sont l'univers de caractères et jusqu'où étendre un
quantifieur non borné.

## Décision

`Any.StringMatching` analyse le **sous-ensemble régulier** du langage de motifs avec le parseur propre à la
bibliothèque et génère à partir de lui — en refusant par une `UnsupportedRegexException` first-class un construct
bien formé mais non régulier ou hors périmètre — plutôt que de dépendre d'une bibliothèque d'automates de regex.

## Justification

* **Elle préserve l'identité zéro-dépendance.** Une dépendance d'automate de regex serait la première dépendance
  runtime de la bibliothèque, apparaîtrait dans l'arbre et le SBOM de chaque consommateur, et contredirait une
  propriété que la bibliothèque annonce et garde. Le parseur maison couvre les formats qui comptent sans ce coût.
* **Elle rend la frontière honnête et first-class.** Les constructs écartés sont les non-réguliers qu'un générateur
  ne peut de toute façon pas honorer ; les refuser à la déclaration, en nommant le construct, est la signature de la
  bibliothèque — une erreur claire vaut mieux qu'une dépendance qui émet en silence une valeur qui ne matche pas
  réellement.
* **Le sous-ensemble régulier est toute la surface utile pour la validation de format.** Littéraux, classes,
  raccourcis courants, quantifieurs, alternation, groupes et ancres expriment les formats que les objets-valeurs
  valident réellement ; les constructs exclus servent à l'analyse, pas aux formats à forme fixe que vise cette
  fonctionnalité.
* **Les choix de forme restants suivent le reste de la bibliothèque.** Les terminaux puisent dans l'ASCII imprimable
  pour qu'un dummy reste lisible et que chaque caractère émis soit un vrai membre de sa classe ; un quantifieur non
  borné tire son minimum plus un petit intervalle borné, le même défaut « 0 à une poignée » qu'utilisent déjà les
  générateurs de chaînes et de collections.

## Alternatives considérées

### Dépendre de Fare (ou d'une autre bibliothèque d'automates de regex)

Considérée parce qu'elle est éprouvée, large, et livrerait la fonctionnalité plus vite. Rejetée parce qu'elle
introduit la première dépendance runtime de la bibliothèque — contredisant l'identité zéro-dépendance que garde le
test d'architecture — pour un dialecte qui n'est lui-même que le sous-ensemble régulier, et parce qu'elle renonce au
refus first-class des constructs non supportés en les traitant silencieusement. Le niveau de maintenance de la
bibliothèque est une préoccupation secondaire.

### Maison, mais visant le dialecte .NET complet

Considérée par souci d'exhaustivité. Rejetée parce que les constructs exclus (lookaround, backreferences) ne sont
pas réguliers et ne peuvent être générés par aucun moyen fini : le « dialecte complet » est donc inatteignable par
principe ; poursuivre les catégories Unicode et le reste serait un travail sans fin, pour des constructs hors de la
validation de format.

### Garder le générateur chaînable avec les autres contraintes de chaîne

Considérée pour qu'un motif puisse se combiner avec `WithLength`, `Numeric`, etc. Rejetée parce que le motif est
déjà toute la spécification : la longueur et la forme des caractères s'expriment dedans. Un générateur terminal
supprime d'emblée une classe de combinaisons contradictoires et garde la surface petite, tandis que la composition
via `As`, `OrNull`, `Combine` et les générateurs de collections — tous définis sur `IAny<T>` — reste disponible.

## Conséquences

### Positives

* La regex de format d'un objet-valeur devient une source de dummies valides en une ligne, sans nouvelle
  dépendance.
* Un construct non supporté échoue à la déclaration avec un message le nommant, jamais sous forme d'une valeur qui
  ne matche pas en silence.
* Le générateur se compose comme tous les autres et reste reproductible sous une graine.

### Négatives

* La bibliothèque porte et doit maintenir son propre parseur et générateur de regex — le plus gros bloc de logique
  qu'elle contienne — et sa correction repose sur la suite de tests (un property test vérifie les valeurs générées
  contre le vrai moteur .NET).
* Le dialecte supporté est un **contrat** : l'élargir ou le restreindre plus tard est un changement pertinent pour
  la compatibilité, et l'univers ASCII imprimable comme l'intervalle du quantifieur non borné sont des comportements
  sur lesquels des consommateurs peuvent finir par compter.

### Risques

* **Dérive du dialecte** — un motif que l'utilisateur attend voir fonctionner peut tomber hors du sous-ensemble.
  Atténué par l'erreur first-class explicite, qui nomme le construct au lieu de mal générer.
* **Bugs du parseur** — un parseur écrit à la main peut mal gérer un cas limite. Atténué par le property test contre
  le vrai moteur ; une défaillance apparaît comme une valeur que le moteur .NET rejette, attrapée en CI plutôt que
  dans le test d'un consommateur.

## Actions de suivi

* N'élargir le sous-ensemble supporté qu'en réponse à des motifs réels, en gardant le refus first-class comme filet
  de sécurité.
* Documenter le dialecte supporté dans la documentation utilisateur une fois la surface stabilisée.
* Si une génération adossée à un automate et réconciliant la longueur devient nécessaire, réexaminer — l'API
  terminale laisse cette voie ouverte sans casser les appelants.

## Références

* ADR-0011 — Héberger Dummies comme un paquet autonome dans ce dépôt (la frontière zéro-dépendance).
* Le parseur de regex, l'arbre de nœuds et le générateur, ainsi que le property test contre
  `System.Text.RegularExpressions`, dans le projet `Dummies` et ses tests.
