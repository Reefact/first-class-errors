# ADR-0066 | Déclarer un objet-valeur par un attribut, et faire respecter son identité par convention

🌍 🇫🇷 Français (ce fichier) · 🇬🇧 [English](0066-declare-a-value-object-and-enforce-its-identity.md)

**Statut :** Proposé
**Proposé :** 2026-07-30
**Décideurs :** Reefact

## Contexte

La bibliothèque porte trois valeurs faites pour être comparées ou transportées par leur contenu plutôt que par
l'instance qu'on tient : une contrainte déclarée (ADR-0065), la paire d'un sujet blâmé et de ce qu'il affirme, et ce
dont un tirage échoué a besoin pour être rejoué. Deux des trois se décrivent dans leurs propres remarques comme des
valeurs comme toutes les autres de ce dépôt, et sont immuables, à constructeur privé atteint par des factories.

Une seule des trois portait une identité de valeur. Les deux autres répondaient « est-ce le même ? » par référence,
et en silence : un type référence compare par identité tant que personne n'écrit une autre réponse, ce qui ne lève
aucun avertissement du compilateur, ne fait échouer aucun test, et ne se lit pas du tout pour un relecteur. Celle qui
avait son identité l'avait parce que du code la comparait avec `==`, ce qui forçait la question ; rien ne la forçait
pour les deux autres, et le trou est parti en production.

L'opérateur `==` est la moitié qui se dégrade le plus discrètement. Un type auquel manque `Equals` en manque au moins
visiblement pour qui lit le type ; un type auquel manquent les opérateurs compile encore à chaque `a == b`, et y
compare des références.

L'immuabilité ne désigne pas une valeur ici. Les générateurs et les spécifications sont immuables aussi — ils sont
reconstruits plutôt que mutés à chaque contrainte — mais deux générateurs identiquement contraints sont deux
recettes, pas une valeur ; les comparer par contenu répondrait à une question qui n'a pas de sens pour eux.

Le dépôt traite déjà une règle de cette forme par un marqueur plus une convention par réflexion : la convention de
garde null d'ADR-0045 découvre les membres au lieu de les nommer, et ADR-0064 déclare son exemption par
`[BuiltOnTheFailurePath]` plutôt que de l'inférer. ADR-0056 consigne ce qu'il advient d'une règle dans ce dépôt quand
rien ne peut agir dessus : une règle de type explicite a dérivé à 203 violations tant qu'elle vivait là où seul un
lecteur pouvait l'appliquer.

## Décision

Un type dont les instances sont des valeurs se déclare par `[ValueObject]`, et une convention par réflexion tient
chaque type marqué à une identité de valeur complète.

## Justification

Le trou que cela ferme est invisible par construction, ce qui fait de la convention le bon instrument plutôt que
l'attention ou la relecture. Rien, chez une valeur privée de son égalité, ne paraît fautif : le type est immuable,
ses factories sont nommées, ses remarques disent que c'est une valeur. Seule la question posée révèle la réponse, et
deux valeurs sur trois sont parties sans que personne ne la pose.

Le marqueur gagne sa place parce que la règle ne peut pas être déduite. Détecter les valeurs par l'immuabilité
embarquerait les générateurs et les spécifications et leur exigerait une égalité qui les décrirait mal. Les déduire
d'un motif de nommage serait pire : cela ferait reposer l'application sur une convention pas moins fragile que celle
qu'on applique. Déclarer est une décision qu'un humain prend une fois par type, et une décision est exactement ce
qu'un attribut consigne — le raisonnement même qu'ADR-0064 a appliqué à sa propre exemption plutôt que de l'inférer
de la forme d'un type.

Faire respecter la paire d'opérateurs est ce qui rentabilise le mieux le coût. C'est le seul membre de l'ensemble
dont l'absence change le comportement sans changer le fait que le code compile : donc celui qu'un relecteur est le
moins capable d'attraper, et une convention le plus.

La convention vérifie la structure, et s'y arrête délibérément. Savoir si deux instances égales hachent pareil, et
si les champs choisis pour l'égalité sont les bons, sont des questions sur le sens d'un type précis auxquelles
aucune réflexion sur sa forme ne peut répondre ; elles appartiennent aux tests de ce type. Ce que la réflexion peut
trancher — scellé, immuable, et l'ensemble des membres présent — est précisément la moitié qui disparaît quand
personne ne regarde, et elle ne peut pas être satisfaite par accident.

Le scellement est exigé plutôt qu'encouragé parce qu'une valeur non scellée ne peut pas garder son égalité
symétrique : une sous-classe portant un champ de plus est égale à sa base dans un sens et inégale dans l'autre, ce
qui rompt le contrat dont dépend tout type de collection. Rejeter une structure marquée redit, là où c'est
applicable, la règle permanente selon laquelle une valeur gardant un invariant est une classe : une structure expose
un constructeur sans paramètre produisant une instance ayant contourné toute factory.

## Alternatives considérées

### Exiger l'identité de tout type immuable, sans marqueur

Considérée parce qu'elle ne demande rien à déclarer et ne peut pas être oubliée sur un type neuf.

Rejetée parce qu'elle n'est pas vraie de tout type immuable ici. Les générateurs et les spécifications sont
immuables et ne sont pas des valeurs : la règle leur imposerait donc une égalité dénuée de sens, ou exigerait une
liste d'exclusion — qui est un marqueur inversé, et qui grossit en silence à mesure que la bibliothèque grandit.

### Déduire les valeurs d'une convention de nommage ou d'espace de noms

Considérée parce qu'elle ne demanderait ni attribut ni liste.

Rejetée parce qu'elle ferait reposer l'application sur une convention exactement aussi peu appliquée que celle
qu'elle remplace. Un type renommé hors du motif quitterait la convention en silence, ce qui est précisément
l'échec que cette décision existe pour empêcher.

### S'appuyer sur un analyseur plutôt qu'un test

Considérée parce que le dépôt livre des analyseurs de première main (ADR-0044) et en emploie un là où le système de
types ne peut pas exprimer une règle (ADR-0059).

Rejetée parce que la règle porte sur les types propres de la bibliothèque, non sur la façon dont un consommateur
écrit son code. Un analyseur est le bon instrument quand le diagnostic doit atteindre le build d'un consommateur ;
ici le public est ce dépôt, et sa propre suite applique déjà des conventions de cette forme par réflexion.

### Utiliser des `record` pour ces valeurs

Considérée parce qu'un `record` génère tout l'ensemble d'identité, donc le trou ne pourrait pas survenir.

Rejetée parce que l'égalité générée porte sur tous les membres, ce qui n'est pas toujours la bonne réponse : l'une
de ces valeurs compare une contrainte **en plus** du texte qui la rend, précisément pour qu'une phrase se lisant
comme une contrainte ne soit pas prise pour elle. Un `record` ferait de plus du constructeur primaire un point
d'entrée public, là où ces types font délibérément passer la construction par des factories nommées.

## Conséquences

### Positives

* Une valeur qui oublie son identité fait échouer un test au lieu de partir en production.
* La paire d'opérateurs — le membre dont l'absence est silencieuse — est appliquée comme le reste.
* Ce qu'est un type est déclaré là où le type est, et un lecteur l'apprend du type lui-même.
* Les types marqués sont énumérables : l'ensemble des valeurs de la bibliothèque est désormais une question qui a
  une réponse.

### Négatives

* Une valeur nouvelle doit être marquée pour être couverte ; oublier le marqueur la laisse non vérifiée, et seul un
  relecteur l'attrape.
* La convention contraint ses types marqués au-delà de l'égalité — scellé, immuable, classe — donc une valeur future
  légitime qui devrait être autrement devrait argumenter plutôt que simplement différer.

### Risques

* La vérification structurelle peut se lire comme suffisante. Un type peut porter tout l'ensemble et comparer
  quand même les mauvais champs ; la convention ne dit rien là-dessus, et sa propre documentation le dit plutôt que
  de laisser le lecteur supposer l'inverse.
* Le marqueur peut être posé sur ce qui n'est pas une valeur, ce qui exigerait une égalité la décrivant mal.
  Atténué par la seule relecture — l'attribut est une affirmation, et une affirmation fausse est une décision
  fausse, pas une règle cassée.

## Actions de suivi

* Examiner si les valeurs de `FirstClassErrors` — qui portent déjà leurs identités — doivent se déclarer de la même
  façon ; les deux assemblages ne peuvent pas partager l'attribut, JustDummies étant autonome par ADR-0011.

## Références

* [ADR-0011](0011-host-dummies-as-a-standalone-package.fr.md) — JustDummies ne dépend de rien dans ce dépôt.
* [ADR-0044](0044-ship-justdummies-analyzers.fr.md) — analyseurs de première main.
* [ADR-0045](0045-guard-public-and-internal-arguments-against-null.fr.md) — une convention qui découvre les membres
  au lieu de les nommer.
* [ADR-0056](0056-state-the-coding-rules-where-an-agent-can-act-on-them.fr.md) — une règle sur laquelle rien ne peut
  agir dérive.
* [ADR-0059](0059-guard-the-recipe-versus-value-boundary-with-analyzers.fr.md) — quand l'analyseur est l'instrument.
* [ADR-0064](0064-exempt-the-whole-failure-reporting-path-from-the-null-guard-convention.fr.md) — un marqueur
  consignant une décision plutôt que de l'inférer.
* [ADR-0065](0065-carry-a-declared-constraint-as-a-value-object.fr.md) — la valeur dont l'égalité a forcé la
  question.
