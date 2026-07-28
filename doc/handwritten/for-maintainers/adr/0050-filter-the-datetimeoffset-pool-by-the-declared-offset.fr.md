# ADR-0050 | Filtrer le pool DateTimeOffset par le décalage déclaré

🌍 🇫🇷 Français (ce fichier) · 🇬🇧 [English](0050-filter-the-datetimeoffset-pool-by-the-declared-offset.md)

**Statut :** Accepté
**Date :** 2026-07-28
**Décideurs :** Reefact

Supersède l'[ADR-0037](0037-vary-the-datetimeoffset-offset-dimension.fr.md).

## Contexte

L'ADR-0037 a doté `AnyDateTimeOffset` d'une dimension de décalage et a consigné, sous *Risques*, que la combiner à
`OneOf` laisserait le décalage inappliqué : « `WithOffset` combiné à `OneOf` ne remplace pas le décalage propre d'une
valeur `OneOf`. Atténuation : documenté, et cohérent avec la sémantique d'énumération terminale de `OneOf`. »

L'atténuation n'a pas tenu, et le risque est plus grand qu'une surprise.

La documentation XML publique de `WithOffset` énonce qu'elle « épingle la dimension de décalage — **chaque valeur
générée porte exactement ce décalage** » et déclare lever `ConflictingAnyConstraintException` « lorsque la contrainte
en contredit une déjà déclarée ». Combinée à `OneOf`, elle ne faisait ni l'un ni l'autre : la contrainte était
abandonnée dans les deux ordres de déclaration, aucune exception n'était levée, et les valeurs sortaient avec leur
propre décalage. Le contrat publié disait l'inverse de ce que faisait le code, et le readme de JustDummies ne
mentionne pas du tout l'interaction.

La bibliothèque répond à cette forme de façon cohérente partout ailleurs.
`Any.Int32().OneOf(1, 2, 3).GreaterThan(10)` et `Any.DateTime().OneOf(d1, d2).After(2022)` lèvent toutes deux une
`ConflictingAnyConstraintException` ; `OneOf(1, 2, 3).GreaterThan(1)` resserre et tire. `DateTimeOffset` était la
seule famille où une contrainte déclarée après un pool n'était ni appliquée ni refusée.

La règle qui gouverne une contrainte fluent dans ce dépôt est qu'une méthode offerte par la DSL doit être honorée
quand ses arguments le permettent et doit échouer quand ils ne le permettent pas. L'abandonner en silence n'est ni
l'un ni l'autre.

## Décision

Un décalage déclaré **filtre** le pool `OneOf` aux valeurs dont il admet le décalage, dans les deux ordres de
déclaration, et entre en conflit lorsqu'il n'en admet aucune.

## Justification

* **Cela restaure le contrat publié.** `WithOffset` promet que chaque valeur générée porte ce décalage, et c'est
  désormais le cas — y compris pour une valeur du pool, puisqu'une valeur portant un autre décalage n'est simplement
  pas tirée.
* **Cela conserve la moitié juste de l'ADR-0037.** Une valeur du pool est toujours rendue telle quelle, décalage
  compris : la reconstruire depuis l'instant normaliserait le décalage vers UTC, ce que l'ADR-0037 voulait
  précisément éviter. Ce qui change, c'est *quelles* valeurs du pool peuvent être tirées, pas la façon de rendre
  celle qui l'est.
* **Cela met les deux ordres d'accord.** Déclarer le pool d'abord ou le décalage d'abord aboutit maintenant au même
  verdict, propriété que la bibliothèque garantit déjà pour toute autre paire de contraintes et sur laquelle un
  appelant n'a aucun moyen de raisonner autrement.
* **Une contradiction est signalée plutôt qu'avalée.** Un décalage qu'aucune valeur du pool ne porte est une
  spécification que le générateur ne peut pas satisfaire ; échouer à la déclaration est la raison d'être du contrôle
  anticipé, et c'est ce que la documentation annonçait déjà à l'appelant.

## Alternatives envisagées

### Conserver le comportement et corriger plutôt la documentation

Envisagée parce que c'est la résolution la moins chère et parce que l'ADR-0037 y était déjà parvenue par le
raisonnement. Rejetée parce que la documentation devrait alors décrire une règle valable pour une famille de
générateurs et pour aucune autre, et parce que l'appelant qui écrit `WithOffset` après un pool demande quelque chose
que la bibliothèque sait trancher : ou bien une valeur du pool porte ce décalage, ou bien aucune. Documenter un
no-op silencieux n'en fait pas une bonne réponse, et la divergence se découvrirait là où un test passe pour la
mauvaise raison.

### Réécrire le décalage de la valeur du pool avec celui déclaré

Envisagée parce qu'elle honore `WithOffset` littéralement dans tous les cas, sans contradiction à signaler. Rejetée
parce qu'elle modifie la valeur fournie par l'appelant : `OneOf` énumère des valeurs exactes, et en rendre une qui
n'a jamais été dans le pool est une surprise pire que celle qu'on supprime. Elle détruit aussi l'instant, puisque
déplacer le décalage en conservant l'heure locale donne un autre point dans le temps.

### Rendre `OneOf` terminal sur `AnyDateTimeOffset`

Envisagée parce qu'un type terminal rendrait la combinaison inécrivable, ce qui est la garantie la plus forte
possible. Rejetée parce qu'elle supprime des combinaisons légitimes et utiles — `OneOf(...).Except(...)`, et un
décalage que certaines valeurs du pool portent bel et bien — et parce que la question plus large des pools terminaux
se règle pour elle-même du côté des pools de chaînes et d'objets, plutôt que famille par famille.

## Conséquences

### Positives

* `WithOffset` et `WithOffsetBetween` veulent dire la même chose quel que soit ce que le générateur porte par
  ailleurs.
* Une combinaison pool/décalage impossible est signalée à la déclaration, avec un message nommant ce qui a été
  demandé et ce que le pool admet.
* `AnyDateTimeOffset` cesse d'être la seule famille où une contrainte déclarée peut disparaître.

### Négatives

* Un appelant qui s'appuyait sur l'ancien silence — écrire `WithOffset` après un pool en attendant que le pool
  l'emporte — obtient désormais soit un pool filtré, soit un conflit. Ce comportement était contredit par la
  documentation de la méthode elle-même, donc le changement corrige le code plutôt que l'attente, mais c'est un
  changement de comportement dans un générateur déjà livré.

### Risques

* **Un pool d'une seule valeur au décalage discordant échoue là où il générait.** C'est la correction voulue, et le
  message nomme les deux côtés pour que la solution soit évidente — retirer la contrainte de décalage, ou mettre au
  pool une valeur qui le porte. Atténuation : le message énonce ce que la dimension de décalage admet.

## Actions de suivi

* Passer l'ADR-0037 au statut *Superseded* avec un lien vers celle-ci.
* Garder en phase la section décalage du readme de JustDummies : elle documente `WithOffset` et `WithOffsetBetween`
  sans mentionner `OneOf`, ce qui n'était exact que sous l'ancien comportement.

## Références

* ADR-0037 — Faire varier la dimension d'offset de DateTimeOffset : la décision que celle-ci supersède, et l'entrée
  *Risques* qu'elle referme.
* ADR-0030 — Tirer des chaînes arbitraires d'un ensemble terminal explicite : la sémantique de pool terminal sur
  laquelle l'ADR-0037 s'appuyait.
* `AnyDateTimeOffset` dans le projet `JustDummies`.
