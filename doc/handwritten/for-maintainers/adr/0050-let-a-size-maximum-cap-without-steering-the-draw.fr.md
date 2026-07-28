# ADR-0050 | Laisser un maximum de taille plafonner sans piloter le tirage, et plafonner une taille explicitement demandée

🌍 🇫🇷 Français (ce fichier) · 🇬🇧 [English](0050-let-a-size-maximum-cap-without-steering-the-draw.md)

**Statut :** Accepté
**Date :** 2026-07-28
**Décideurs :** Reefact

## Contexte

JustDummies permet à un test de déclarer une taille par deux familles : `WithLength`, `WithMinLength`,
`WithMaxLength` et `WithLengthBetween` sur les chaînes ; `WithCount`, `WithMinCount`, `WithMaxCount` et
`WithCountBetween` sur les collections.

Non contraint, un dummy est délibérément petit : une chaîne tire entre 0 et 16 caractères, une
collection entre 0 et 8 éléments. L'ADR-0025 désigne ce défaut comme le « 0 to a handful » que les
générateurs de chaînes et de collections partagent déjà, et le réutilise pour les quantificateurs regex
non bornés.

Un maximum déclaré, en revanche, ne se compose pas avec ce défaut — il le **remplace**. Le tirage devient
uniforme sur tout l'intervalle déclaré, si bien que la borne haute sert aussi d'indice de taille :
`WithMaxLength(100000)` produit des chaînes d'environ 60 000 caractères, là où le même générateur laissé
non contraint en produit de 0 à 16. Deux politiques de taille différentes s'appliquent à ce qu'un lecteur
perçoit comme une seule et même chose.

La seule validation d'argument sur ces méthodes est la non-négativité ; rien ne borne le haut. Poussés à
`int.MaxValue`, les quatre points d'entrée ont été mesurés comme se comportant de quatre manières
différentes :

| déclaration                   | comportement mesuré                                                |
| ----------------------------- | ------------------------------------------------------------------ |
| `WithLength(int.MaxValue)`     | `ArgumentOutOfRangeException` nommant un paramètre interne, issue d'un débordement arithmétique dans le tirage |
| `WithMaxLength(int.MaxValue)`  | rend une chaîne d'environ 130 Mo                                    |
| `WithMaxCount(int.MaxValue)`   | s'exécute pendant des minutes                                       |
| `WithCount(int.MaxValue)`      | échoue immédiatement                                                |

Cette divergence n'est pas conçue. Deux des quatre découlent directement du maximum qui pilote le tirage.
Les deux autres partagent un même chemin de code et ne diffèrent que par l'endroit où le nombre demandé
tombe par rapport aux limites de l'allocateur : une demande de capacité est refusée d'emblée, l'autre est
accordée puis remplie élément par élément. Aucun des quatre échecs n'est levé par la bibliothèque : deux
sont des exceptions du BCL nommant des paramètres que l'appelant n'a jamais écrits, un est une attente
non bornée, un est une valeur silencieusement énorme.

La bibliothèque possède déjà une taxonomie d'exceptions. Une erreur d'appelant sur un argument isolé
remonte en exception d'argument du BCL — `UnsupportedRegexException` le documente pour un pattern mal
formé, et l'ADR-0045 l'a fixé pour `null` sur toute la surface, avec un test de convention par réflexion
pour l'appliquer. Une contradiction *entre* contraintes déclarées lève une
`ConflictingAnyConstraintException` à la déclaration. Une génération qui échoue malgré des contraintes
acceptées lève une `AnyGenerationException`.

Les grandes tailles ont des usages légitimes : les tests qui exercent une limite métier (« refuse un
libellé de plus de 255 caractères », « le lot se découpe au-delà de 1 000 éléments »). Ces tailles sont
calibrées sur la limite testée — des centaines, des milliers, des dizaines de milliers — soit deux ordres
de grandeur en dessous des valeurs qui produisent les comportements ci-dessus.

JustDummies n'a jamais été publié, donc le sens d'une borne déclarée est encore libre d'être fixé (le
même acquis sur lequel s'appuyait l'ADR-0041).

## Décision

Un maximum de taille déclaré ne fait jamais que rétrécir un tirage et ne l'élargit jamais au-delà du
spread par défaut, et une taille que le générateur doit réellement produire — une longueur ou une
cardinalité exacte ou minimale — est refusée au-delà de 1 000 000 par une `ArgumentOutOfRangeException` à
la déclaration.

## Justification

* **Le maximum qui pilote est la cause, pas un quatrième symptôme.** Dès lors qu'un maximum ne fait que
  plafonner, une borne lâche n'enfle plus le tirage, et deux des quatre comportements mesurés cessent
  d'exister : un `WithMaxLength` énorme rend une chaîne petite ordinaire, un `WithMaxCount` énorme une
  collection petite ordinaire. Supprimer une cause vaut mieux que garder quatre effets, et c'est ce qui
  rend le garde restant assez petit pour tenir en une phrase.
* **Une borne est une permission, pas une demande.** « Au plus N » énonce ce que la valeur ne doit pas
  dépasser ; il ne dit rien de la taille souhaitée. C'est de la lire comme une demande que vient le
  désaccord entre le défaut non contraint et le défaut borné, pour ce qu'un lecteur perçoit comme le même
  générateur. Sous cette décision, une seule politique gouverne la taille partout : un dummy est petit à
  moins que quelque chose ne demande explicitement plus, et seuls un minimum, une taille exacte ou un
  fragment requis peuvent le demander.
* **Ne plafonner que ce qui doit être produit supprime les faux positifs du garde.** Un maximum ne coûte
  rien à honorer, donc plafonner une chaîne à une largeur de colonne de quatre millions reste légal et
  continue de produire de petits dummies. Seule une taille que la bibliothèque devrait matérialiser est
  refusée — soit exactement l'ensemble qui décide de la mémoire et du travail que coûte un tirage.
* **Le plafond relève de la validation d'argument, pas des exceptions propres à la bibliothèque.** Une
  taille trop grande est un argument isolément inutilisable, exactement comme la taille négative déjà
  rejetée à cet endroit ; ce n'est pas une contradiction entre deux contraintes, et ce n'est pas une
  génération qui a échoué. Suivre la taxonomie laisse inchangé le nombre de types d'exceptions et de
  catégories documentées, et remplace un message nommant un paramètre interne par un message nommant le
  paramètre que l'appelant a écrit.
* **1 000 000 se place dans l'écart entre le légitime et l'absurde.** C'est cinq ordres de grandeur
  au-dessus du spread par défaut, donc l'usage ordinaire ne peut pas l'approcher ; c'est deux ordres de
  grandeur au-dessus de la plus grande limite métier qu'un test de bord exerce plausiblement, donc un tel
  test n'est jamais refusé ; et une valeur de cette taille se matérialise encore en millisecondes, donc
  le plafond ne transforme jamais un test lent en test rapide — il transforme un gel ou un échec
  d'allocation en échec diagnostiquable. Un plafond plus bas commencerait à refuser le test de bord qui
  vérifie légitimement une entrée de 64 Ko.
* **C'est un test de convention qui maintient la règle vraie.** Le débordement derrière le premier
  comportement mesuré existe parce que la même arithmétique a déjà été rendue sûre une ligne plus haut et
  pas ici ; une règle appliquée à la main à chaque méthode prenant une taille sera oubliée par le
  prochain builder exactement de la même façon. L'ADR-0045 a établi l'application par réflexion comme la
  réponse de ce dépôt à une règle qui doit tenir sur toute une surface, y compris les membres pas encore
  écrits.

## Alternatives considérées

### Plafonner tout argument de taille, maxima compris

Considérée pour l'uniformité d'une règle unique sans exception à retenir. Rejetée : un maximum est
gratuit à honorer dès lors qu'il ne pilote plus le tirage, donc en refuser un grand n'achète aucune
protection tout en refusant une déclaration légitime — un plafond reflétant une limite de stockage
supérieure au plafond. La règle tient en une phrase dans les deux cas, et cette version-ci n'a pas de
faux positifs.

### Lever le dépassement de plafond comme une exception de la bibliothèque

Considérée : une `ConflictingAnyConstraintException`, ou un nouveau membre de la hiérarchie propre à la
bibliothèque, pour que toute la surface d'échec s'attrape en une clause. Rejetée parce qu'elle contredit
la taxonomie consignée ailleurs dans la bibliothèque : un argument isolément inutilisable est une erreur
d'appelant, pas une interaction de contraintes, et le faire correspondre à un conflit ferait dire deux
choses différentes au mot « conflit ». Réserver la hiérarchie de la bibliothèque à ce que la bibliothèque
décide elle-même est ce qui garde cette hiérarchie signifiante.

### Offrir une échappatoire par appel pour les très grandes tailles

Considérée, pour qu'aucun usage légitime ne soit jamais bloqué. Rejetée sur le terrain de la demande :
aucun usage de ce type n'est recensé, l'échappatoire invite le mésusage que le plafond existe pour
empêcher, et en ajouter une plus tard est un ajout non cassant alors qu'en retirer une serait cassant. Un
besoin réel se traite en révisant le plafond — une décision — plutôt que par un contournement par appel.

### Traiter `Between` comme une demande explicite de sa plage

Considérée parce que `WithLengthBetween(0, 100000)` se lit comme une demande de valeurs réparties sur
cette plage, et que sous cette décision il rend le petit défaut à la place. Rejetée parce qu'elle
briserait l'identité entre `WithLengthBetween(a, b)` et le même générateur déclaré avec un minimum et un
maximum : deux écritures d'une seule contrainte tireraient différemment, alors que l'uniformité de
l'algèbre de contraintes est une propriété délibérée de cette API. En pratique, une plage partant de zéro
s'écrit pour exprimer une limite, et un test qui veut de grandes valeurs relève le minimum — ce qui se
lit pour ce que c'est.

### Ne corriger que le débordement arithmétique

Considérée comme le changement minimal, puisque c'est le seul comportement qui produit un message
déroutant. Rejetée : elle traite le moins nuisible des quatre. Une chaîne de 130 Mo silencieuse et une
exécution de plusieurs minutes coûtent bien plus cher à diagnostiquer qu'une exception au message
médiocre, et ni l'une ni l'autre n'est touchée par une arithmétique protégée du débordement.

## Conséquences

### Positives

* Une seule politique de taille sur toute l'API : un dummy est petit à moins que quelque chose ne demande
  explicitement plus.
* Deux des quatre comportements mesurés disparaissent comme conséquence de la politique, sans qu'aucun
  garde n'intervienne.
* Une taille absurde est signalée à la déclaration, contre le paramètre que l'appelant a écrit, au lieu
  de se manifester par un gel, un échec d'allocation ou un message du BCL sur de l'arithmétique interne.
* Le débordement arithmétique devient inatteignable, puisqu'une taille produite ne peut plus approcher la
  plage où il survient.
* Aucun nouveau type d'exception et aucune nouvelle catégorie documentée.

### Négatives

* Une déclaration qui produisait de grandes valeurs en produit désormais de petites. Que
  `WithMaxLength(100000)` rende des chaînes de 0 à 16 caractères est le nouveau comportement voulu, et il
  surprendra quiconque lisait la borne comme un indice de taille — la documentation doit énoncer la règle
  explicitement plutôt que de la laisser deviner.
* `WithLengthBetween(0, N)` rend le spread par défaut plutôt que des valeurs réparties sur la plage, ce
  qui est le prix accepté pour garder `Between` décomposable.
* Le plafond est une constante sans dérivation. Il est défendable, pas démontrable, et l'argument qui le
  soutient repose sur l'écart entre le légitime et l'absurde plutôt que sur une mesure du runtime.

### Risques

* Un consommateur ayant légitimement besoin de plus que le plafond est bloqué jusqu'à révision. Atténué
  par la taille de l'écart : le plafond est très au-dessus de tout test de bord calibré sur une limite
  métier.
* Le test de convention doit reconnaître un paramètre porteur de taille pour tenir un futur builder à la
  règle ; un paramètre de taille nommé hors convention y échapperait silencieusement. C'est la même
  exposition que l'ADR-0045 a acceptée pour sa propre règle par réflexion.

## Actions de suivi

* Trancher, à l'implémentation, si le plafond porte sur la taille que l'appelant énonce directement ou
  sur le minimum effectif une fois les fragments requis comptés — les deux ne diffèrent que pour une
  déclaration dont le préfixe, le suffixe ou les valeurs contenues sont eux-mêmes proches du plafond.
* Garder l'arithmétique du tirage protégée du débordement indépendamment du plafond : l'inatteignabilité
  est une propriété de la règle actuelle, pas une garantie, et la forme sûre ne coûte rien.
* Énoncer la règle dans la documentation utilisateur, anglaise et française, là où les contraintes de
  taille sont décrites.

## Références

* ADR-0025 — Generate strings from a home-grown regular subset : nomme le défaut « 0 to a handful » que
  cette décision restaure là où un maximum le court-circuitait.
* ADR-0041 — Draw flag-enum combinations behind an opt-in : l'acquis selon lequel une bibliothèque non
  publiée peut encore fixer le sens d'un tirage non contraint.
* ADR-0045 — Guard public and internal arguments against null : la posture de validation d'argument et le
  test de convention par réflexion que cette décision réutilise.
* Issue #226 — le backlog JustDummies sous lequel les items demand-driven de l'audit ont été classés.
