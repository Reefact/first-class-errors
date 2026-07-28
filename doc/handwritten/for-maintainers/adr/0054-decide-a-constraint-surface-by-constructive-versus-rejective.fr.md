# ADR-0054 | Décider la surface de contraintes d'un générateur par constructif contre rejectif, et non par terminalité

🌍 🇫🇷 Français (ce fichier) · 🇬🇧 [English](0054-decide-a-constraint-surface-by-constructive-versus-rejective.md)

**Statut :** Accepté
**Date :** 2026-07-28
**Décideurs :** Reefact

Supersède l'[ADR-0030](0030-draw-arbitrary-strings-from-an-explicit-terminal-set.fr.md).

## Contexte

Chaque générateur `JustDummies` est une recette fluente : chaque contrainte restreint ce qui peut être tiré, deux
contraintes contradictoires échouent à la déclaration avec une `ConflictingAnyConstraintException` qui nomme les deux
côtés, et la valeur est construite pour satisfaire toute la spécification plutôt que générée puis filtrée. Quelles
contraintes un générateur donné expose se décidait jusqu'ici générateur par générateur.

`OneOf` est le cas le plus net de cette dérive. Mesuré sur `main` par réflexion sur les méthodes d'instance publiques
du type retourné, autres que `Generate()` :

| appel | retourne | contraintes chaînables |
|---|---|---|
| `Any.Int32().OneOf(1, 2)` | `AnyInt32` | 13 — composable |
| `Any.DateTime().OneOf(d)` | `AnyDateTime` | 9 — composable |
| `Any.DateTimeOffset().OneOf(x)` | `AnyDateTimeOffset` | 11 — composable |
| `Any.Guid().OneOf(g)` | `AnyGuid` | 5 — composable |
| `Any.String().OneOf("a", "b")` | `AnyStringOneOf` | 0 — terminal |
| `Any.OneOf(x, y)` | `AnyOneOf<T>` | 0 — terminal |

Quatre familles renvoient leur propre builder composable ; deux renvoient un type distinct sans issue. Rien, au site
d'appel, ne distingue les deux.

Autres faits qui cadrent le choix :

* L'ADR-0030 a rendu `Any.String().OneOf(...)` terminal, au motif que réconcilier un ensemble de valeurs explicite avec
  le préfixe, le suffixe, les valeurs contenues, la famille de caractères, la casse et la longueur d'une chaîne
  multiplierait les combinaisons contradictoires et leurs messages de conflit, pour une combinaison dont personne
  n'avait besoin. Elle listait en *Risque* qu'un appelant puisse attendre la composabilité du `OneOf` scalaire et être
  surpris. L'ADR-0025 a rendu `Any.StringMatching(...)` terminal sur le même raisonnement, et l'ADR-0030 s'est alignée
  dessus comme précédent.
* Le manque n'est pas théorique. `Any.ElementOf(existingOrders).DifferentFrom(theOneAlreadyUsed)` — tirer un autre
  élément d'une fixture — n'existe pas, et `Any.String().OneOf("abc", "de").WithLength(3)` non plus, alors que `"abc"`
  satisfait les deux. Le contournement LINQ pour le premier, `pool.Where(x => x != used).ToArray()`, fonctionne mais
  rapporte un domaine vidé en `ArgumentException: At least one value is required`, ce qui blâme l'appelant pour un
  tableau vide au lieu de nommer les deux contraintes en jeu. Les familles numériques, elles, nomment les deux.
* Les deux coûts que l'ADR-0030 évitait ne sont pas symétriques. Longueur, préfixe, suffixe, valeurs contenues, famille
  de caractères et casse *mettent en forme* une chaîne que le générateur construit. `Except`/`DifferentFrom` ne mettent
  rien en forme : elles retirent des valeurs.
* Les chaînes n'ont pas de projection ordinale où intégrer une exclusion, si bien que sur une chaîne mise en forme une
  exclusion est déjà satisfaite par un **retirage borné** — une exception documentée au « construit, jamais filtré »
  que le readme du paquet énonce, et le seul échec qu'`AnyString` diffère à la génération.
* `AnyPattern` fait déjà tourner une boucle bornée construire-vérifier-retirer à chaque tirage : depuis l'ADR-0048,
  chaque valeur construite est vérifiée contre le vrai moteur .NET et retirée en cas d'échec, pour que « une valeur
  générée matche son motif » tienne par construction.
* Les collections distinctes bornent à la déclaration selon la cardinalité et l'appartenance annoncées par le
  générateur d'éléments, via l'interface interne `ICardinalityHint<T>` (ADR-0013). `AnyStringOneOf` et `AnyOneOf<T>`
  l'annoncent tous deux ; `AnyString` non.
* L'issue #337 a établi qu'un échec de génération ne peut affirmer que ce que la recherche a réellement établi : un
  budget dépensé se rapporte comme un budget dépensé, jamais comme une preuve d'impossibilité.
* Rien n'a été publié. `PublicAPI.Shipped.txt` ne contient que `#nullable enable`, aucun tag `dum-v*` n'existe et la
  version est `0.1.0-dev` : changer un type de retour et supprimer un type public ne coûte rien aujourd'hui, et serait
  une version majeure après la première publication.

## Décision

Les contraintes qu'un générateur expose se décident selon que chacune est **constructive** — elle décrit une valeur que
le générateur doit construire, et n'est offerte que là où il sait en construire une qui la satisfait — ou **rejective**
— elle retire des valeurs d'un domaine, et est offerte partout — plutôt qu'en déclarant un générateur terminal.

## Justification

* **« Terminal » décrivait le type retourné, pas le domaine : impossible d'en raisonner.** Les ADR-0030 et ADR-0025 ont
  chacune abouti à un refus solide, mais l'ont consigné comme une propriété du générateur : *celui-ci n'expose rien de
  plus*. Un appelant ne peut pas le prévoir, et un mainteneur qui ajoute le générateur suivant non plus — le tableau
  mesuré ci-dessus est ce à quoi ressemble une règle que personne ne peut appliquer, après que quatre familles sont
  parties d'un côté et deux de l'autre. Constructif contre rejectif est une propriété de la contrainte : le même test
  répond pour tout générateur, y compris ceux qui ne sont pas encore écrits.
* **Un ensemble de valeurs fourni par l'appelant est un domaine, pas une mise en forme : le coût combinatoire que
  l'ADR-0030 refusait ne se présente jamais.** L'argument de l'ADR-0030 était qu'un ensemble explicite devrait être
  réconcilié avec chaque contrainte de mise en forme, chaque réconciliation exigeant sa propre analyse de conflit. C'est
  vrai tant que le générateur *construit* une chaîne. Dès lors que les valeurs sont fournies, il n'y a plus rien à
  construire : chaque autre contrainte devient un test que chaque valeur passe ou échoue, le domaine est l'ensemble des
  valeurs qui passent, et la satisfaisabilité est l'unique question de savoir s'il en reste. Une question remplace la
  matrice — et elle est tranchée précocement, donc la promesse qu'un générateur qui existe sait générer est tenue.
* **Le refus sur un motif survit au recadrage, et y gagne une raison qu'il n'avait pas.** Une contrainte de forme sur
  `Any.StringMatching(...)` exigerait de construire une valeur dans l'intersection de deux langages réguliers, une
  machinerie que la bibliothèque n'a pas et n'ajouterait pas pour cela. C'est désormais un énoncé sur la contrainte
  plutôt que sur le type : le refus tient à ce qui ne peut pas être construit, pas à une étiquette, et il explique
  pourquoi la paire d'exclusion est admise à côté au lieu de ressembler à une incohérence.
* **Une contrainte rejective ne demande aucune machinerie nouvelle et ne crée aucune exception nouvelle au « construit,
  jamais filtré ».** Sur une chaîne mise en forme, les exclusions passent déjà par un retirage borné, et cette
  exception est déjà documentée. Sur un motif, la boucle qui porterait l'exclusion est celle que l'ADR-0048 fait déjà
  tourner à chaque tirage ; l'exclusion y est un prédicat de plus. Sur un ensemble de valeurs la question ne se pose
  même pas : le domaine est fini et énumérable, donc les valeurs exclues sont retirées à la déclaration et le tirage
  reste un unique choix uniforme.
* **La symétrie rend la surface apprenable ; nommer les deux côtés la garde honnête.** Un appelant qui a rencontré
  `Except`/`DifferentFrom` sur un générateur peut les attendre sur le suivant, et un domaine vidé rapporte les deux
  contraintes qui l'ont vidé au lieu d'une erreur d'argument sur un tableau que l'appelant n'a jamais écrit. C'est le
  même contrat « un Arrange impossible est un défaut du test » que la bibliothèque applique partout ailleurs, étendu
  aux deux endroits qui en étaient sortis.
* **Là où une recherche bornée porte l'exclusion, l'échec garde la seule affirmation qu'il peut soutenir.** Un
  générateur de motif construit des valeurs depuis son motif ; il n'énumère jamais le langage, donc un budget épuisé est
  un indice et non une preuve, et le message le dit — le standard posé par l'issue #337, appliqué au seul nouveau mode
  d'échec que cette décision crée.
* **La fenêtre est ouverte maintenant et se referme à la première publication.** Rendre le `OneOf` des chaînes
  composable change un type de retour et supprime un type public. Rien n'étant publié, c'est gratuit ; après `dum-v1`,
  c'est une version majeure, et l'asymétrie devrait être subie ou payée.

## Alternatives considérées

### Garder les types terminaux et leur donner les contraintes de mise en forme

Considérée parce qu'elle préserve intactes les décisions des ADR-0030 et ADR-0025 tout en refermant le manque de
capacité : un `AnyStringOneOf` composable répondrait à `OneOf("abc", "de").WithLength(3)` sans changer ce que
`Any.String().OneOf` renvoie.

Rejetée parce qu'elle referme le manque en dupliquant la surface au lieu de supprimer l'asymétrie : l'ensemble des
contraintes existerait deux fois, sur deux types, avec deux jeux de messages de conflit à garder en phase, et
l'appelant devrait toujours savoir quel type il tient. L'asymétrie que montre le tableau mesuré est le défaut ; un
second type composable la laisse en place.

### Ne rendre composable que `Any.String().OneOf`, et laisser le pool et le motif terminaux

Considérée parce qu'elle corrige le cas au coût le plus visible — l'ensemble de valeurs des chaînes — pour le plus
petit changement, et laisse deux décisions intactes.

Rejetée parce qu'elle corrige l'instance et non la règle. `Any.ElementOf(orders).DifferentFrom(used)` est l'idiome pour
lequel ce travail existe et manquerait encore, et le générateur suivant retomberait sur le même arbitrage non
documenté. Consigner la distinction est ce qui rend la surface prévisible ; l'appliquer à un seul des trois endroits
qu'elle couvre ne consignerait rien.

### Ouvrir aussi le motif aux contraintes de forme, par génération puis filtrage

Considérée pour une symétrie complète : avec une boucle de retirage déjà en place, une contrainte de longueur ou de
préfixe pourrait être satisfaite en tirant jusqu'à ce qu'une valeur la satisfasse, ce qui donnerait à tous les
générateurs de chaînes les mêmes contraintes.

Rejetée parce qu'elle satisferait une contrainte *constructive* par rejet, la seule chose que la bibliothèque refuse de
faire. Le nombre de tirages attendu est non borné et dépend du motif — une contrainte de longueur que le motif produit
rarement transforme une déclaration en loterie silencieuse — donc l'échec dépendrait de la chance plutôt que de la
spécification, et la promesse du conflit précoce serait discrètement abandonnée pour toute une classe de contraintes.
Construire dans l'intersection de deux langages réguliers est la seule façon honnête de les offrir, et c'est hors
périmètre.

### Intersecter le motif et les contraintes de forme par un produit d'automates

Considérée comme la forme honnête de l'alternative précédente : compiler le motif et les contraintes de forme en
automates et générer depuis le produit satisferait les contraintes constructives par construction, sans aucun filtrage.

Rejetée pour son coût et pour l'identité du paquet. Elle ajouterait un moteur d'automates à une bibliothèque dont tout
le sous-ensemble régulier est délibérément maison et petit (ADR-0025), pour une combinaison qu'aucun cas d'usage
rapporté ne demande — l'appelant qui veut une valeur mise en forme écrit la forme dans le motif. La décision refuse la
contrainte, et le refus est maintenant énoncé comme une limite de la machinerie plutôt que comme une propriété du type,
donc la porte reste ouverte si un besoin réel apparaît.

### Laisser le cas du pool au LINQ de l'appelant

Considérée parce que `pool.Where(x => x != used).ToArray()` fonctionne déjà, ne demande aucune API et garde
`AnyOneOf<T>` minimal.

Rejetée parce qu'elle dégrade exactement ce que la bibliothèque existe pour protéger : le diagnostic. Filtrer jusqu'au
vide lève `ArgumentException: At least one value is required (Parameter 'values')`, ce qui blâme l'appelant pour un
tableau vide au lieu de nommer le pool et l'exclusion qui l'ont vidé — alors que les familles numériques rapportent
`Cannot apply DifferentFrom(42) because it forbids every value OneOf(42) allows`. Elle sort aussi une décision de
domaine de la spécification pour la placer dans le code d'arrangement, où une collection distincte ne peut plus la
voir.

## Conséquences

### Positives

* Un seul test — cette contrainte est-elle constructive ou rejective ? — répond à ce que n'importe quel générateur,
  présent ou futur, peut exposer. L'asymétrie mesurée devient une règle plutôt qu'une table de précédents.
* `Any.String().OneOf(...)` compose avec toutes les contraintes de chaîne, et un ensemble vidé nomme les deux
  contraintes en jeu — même verdict quel que soit celui des deux déclaré en premier, chaque ordre le formulant du
  côté d'où arrive la seconde déclaration.
* `Any.ElementOf(orders).DifferentFrom(used)` et `Any.StringMatching(p).DifferentFrom(existing)` existent, avec les
  diagnostics de conflit et d'échec que le reste de la bibliothèque donne.
* `AnyString` annonce la cardinalité de l'ensemble de valeurs survivant, donc une collection distincte sur un
  générateur de chaînes à pool borne toujours précocement — la garantie que l'ADR-0030 obtenait via `AnyStringOneOf`
  est tenue par le type qui le remplace.
* Un type public disparaît et aucun n'est ajouté.

### Négatives

* Un ensemble de valeurs de chaînes n'est plus un type à usage unique dont la vacuité est impossible par
  construction ; c'est un filtre dont la satisfaisabilité doit être validée à chaque contrainte suivante, et cette
  validation est du code qui peut être faux.
* Avec un ensemble de valeurs en vigueur, `Containing(...)` est satisfait en testant la valeur fournie plutôt que par
  la mise en page côte à côte du chemin constructif : un `"aba"` fourni satisfait donc `Containing("ab").Containing("ba")`
  alors qu'une chaîne construite ne le pourrait jamais. Les deux chemins donnent la même réponse partout où le chemin
  constructif sait construire, mais le chemin à pool est strictement plus permissif, et cette différence doit être
  documentée.
* Cette permissivité n'est atteignable que là où le chemin constructif n'avait pas déjà refusé. Une combinaison qu'il
  rejette de son propre chef est refusée dès sa déclaration — le générateur ne peut pas savoir qu'un ensemble de
  valeurs arrive, et différer ce refus coûterait à toute chaîne mise en forme son conflit précoce — donc ces mêmes
  contraintes avec `OneOf` en dernier entrent encore en conflit, alors qu'avec `OneOf` en premier elles passent.
  L'ordre est par ailleurs indifférent ; ici il ne l'est pas, et la surface doit le dire au lieu de promettre une
  symétrie qu'elle n'a pas.
* `AnyPattern` n'est plus descriptible comme n'exposant rien : le cadrage « générateur terminal » de l'ADR-0025 a
  désormais besoin de la qualification constructif/rejectif pour rester exact.

### Risques

* Un appelant peut lire la paire d'exclusion du motif comme une invitation à y attendre aussi des contraintes de forme,
  et lire leur absence comme un oubli. Atténué en énonçant le refus et son motif — aucune machinerie pour construire
  dans l'intersection de deux langages réguliers — dans la documentation du type lui-même et pas seulement ici.
* Une exclusion sur un motif peut vider un petit langage, et le retirage qui le découvre dépense tout son budget avant
  d'échouer. Atténué en gardant ce budget séparé de celui du match, pour qu'aucun des deux échecs n'emprunte les
  preuves de l'autre, et par un message qui affirme le budget dépensé et explicitement pas l'impossibilité (issue #337).
* Valider un ensemble de valeurs contre chaque contrainte coûte O(valeurs × contraintes) à la déclaration. Atténué par
  le domaine : ce sont des ensembles écrits à la main dans du code d'arrangement de test, évalués une fois par
  générateur, jamais par tirage.

## Actions de suivi

* Passer l'ADR-0030 au statut *Superseded* avec un lien vers celle-ci, une fois ce document accepté.
* Décider si l'ADR-0025 a besoin d'un successeur : sa décision — générer depuis un sous-ensemble régulier maison —
  reste intacte, mais sa description du générateur comme *terminal* est restreinte par ce document. Signalé plutôt que
  traité : cette ADR ne revisite pas la façon dont les motifs sont générés.

## Références

* ADR-0030 — Tirer des chaînes arbitraires depuis un ensemble de valeurs explicite et terminal : la décision que
  celle-ci supersède, et le *Risque* qu'elle consignait sur les appelants attendant la composabilité.
* ADR-0025 — Générer les chaînes qui matchent depuis un sous-ensemble régulier maison : le précédent de générateur
  terminal sur lequel l'ADR-0030 s'est alignée, et la raison pour laquelle une contrainte constructive sur un motif
  reste refusée.
* ADR-0048 — Garantir qu'une valeur regex générée matche son motif, par retirage borné : la boucle que rejoint une
  exclusion.
* ADR-0013 — Borner les collections distinctes par la cardinalité, sinon par un tirage borné : le contrat
  `ICardinalityHint` auquel un ensemble de valeurs doit continuer de répondre.
* ADR-0045 — Garder les arguments publics et internes contre null : la convention que suit chaque nouveau membre.
* Issue #352 — l'item d'audit qui a demandé ce document.
* Issue #337 — le standard de véracité des affirmations pour un budget épuisé.
* `AnyString`, `StringSpec`, `AnyOneOf<T>` et `AnyPattern` dans le projet `JustDummies`.
