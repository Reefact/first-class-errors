# ADR-0052 | Tirer les nombres arbitraires dans une magnitude ordinaire

🌍 🇫🇷 Français (ce fichier) · 🇬🇧 [English](0052-draw-arbitrary-numbers-within-an-ordinary-magnitude.md)

**Statut :** Accepté
**Proposé :** 2026-07-28
**Accepté :** 2026-07-28
**Décideurs :** Reefact

## Contexte

Les générateurs flottants et décimaux échantillonnent uniformément entre leurs bornes. Non contraintes,
ces bornes sont le domaine entier du type — pour `double`, une plage couvrant quelque 616 décades.

Un tirage uniforme sur une telle plage est uniforme *par valeur*, pas par magnitude, et il y a autant de
place entre 1e307 et 1e308 qu'entre 0 et 1e307. Toute la masse de probabilité se situe donc à quelques
décades du maximum du type. Mesuré sur 5 000 tirages :

| mesure                                              | résultat     |
| --------------------------------------------------- | ------------ |
| `Any.Double()` — `\|v\| < 1e6`                        | 0 / 5000     |
| `Any.Single()` — `\|v\| < 1e34`                       | 0 / 5000     |
| `Any.Decimal()` — `\|v\| < 1e24`                      | 0 / 5000     |
| `Any.Double().Positive()` × 1,2 → `Infinity`         | 16,1 %       |
| `Any.Decimal()` × 1,2m → `OverflowException`          | 17,1 %       |
| `x + 1 == x` sur un tirage `Positive()`              | vrai         |

À ces magnitudes, un type flottant cesse de se comporter comme de l'arithmétique : une multiplication
supplémentaire déborde — en `Infinity` pour les types binaires, contagieux et produisant des `NaN` en
aval, et en `OverflowException` levée pour `decimal`. La précision est épuisée, d'où `x + 1 == x`. Une
contrainte d'échelle n'a plus de chiffre décimal sur lequel agir : `Any.Decimal().WithScale(2)` était
satisfaite par 5 000 tirages sur 5 000, tous des entiers à 29 chiffres — vraie et vide à la fois.

Les magnitudes où s'exécute le code ordinaire, et où vivent les défauts d'arrondi, de comparaison et de
formatage, ne sont jamais visitées.

Les générateurs entiers partagent la même distribution — `Any.Int32()` tire sous 1e6 dans 0,06 % des cas,
`Any.Int64()` dans 0 cas sur 5 000 — mais pas la même conséquence : un grand entier reste un entier
ordinaire, l'arithmétique entière C# wrappe silencieusement au lieu de saturer ou de lever, `x + 1 != x`
tient toujours, et un débordement d'entier dans le code testé est fréquemment un vrai défaut. Les builders
entiers reposent en outre sur le moteur ordinal partagé, dont quatre familles de builders dépendent.

`Half` s'arrête à 65 504 : son domaine entier se situe déjà dans les magnitudes ordinaires.

L'ADR-0050 a consigné la règle homologue pour les *tailles* : un dummy est petit à moins que quelque chose
ne demande explicitement plus, un maximum étant une permission et non une demande. Il a délibérément borné
sa portée aux tailles et laissé les valeurs ouvertes. JustDummies n'a jamais été publié, donc le sens du
tirage non contraint est encore libre d'être fixé — l'acquis sur lequel s'appuyait l'ADR-0041.

## Décision

Une valeur flottante ou décimale arbitraire est tirée dans une magnitude ordinaire d'un million, cette
fenêtre rognant l'intervalle déclaré et s'effaçant seulement là où elle le laisserait vide, tandis que les
générateurs entiers conservent la plage entière de leur type.

## Justification

* **Un dummy qui casse le test qu'il décore a échoué à sa seule mission.** La bibliothèque existe pour
  fournir une valeur dont le test ne se soucie pas du contenu. Une valeur qui fait déborder une
  multiplication sans rapport une fois sur six n'est pas cela : elle fait de la *fixture* la cause de
  l'échec, et le diagnostic coûte bien plus cher que ce que la valeur a fait gagner. C'est tout
  l'argument, et les mesures ci-dessus en sont la preuve.
* **Rogner plutôt que remplacer est ce qui garde la règle honnête.** Un appelant qui nomme une magnitude —
  un intervalle situé au-delà de la fenêtre — l'obtient exactement, parce que la fenêtre s'efface là où
  elle ne laisserait rien. Un appelant qui *permet* seulement une magnitude continue de tirer des valeurs
  ordinaires, parce que permettre n'est pas demander. La fenêtre ne brise donc jamais une borne déclarée ;
  elle refuse seulement de la viser. C'est la règle de l'ADR-0050 pour les tailles, transposée telle
  quelle aux valeurs, de sorte que la bibliothèque énonce un principe et non deux.
* **Elle redonne du sens aux contraintes bâties par-dessus.** Une contrainte d'échelle que tout tirage
  satisfait et qu'aucun n'exerce est pire qu'absente : elle se lit comme de la couverture dans un test qui
  n'en a pas. Les magnitudes ordinaires rendent à un `decimal` ses chiffres décimaux, donc `WithScale`
  contraint de nouveau.
* **Un million se place là où un dummy est quelconque.** Assez grand pour ressembler à une vraie quantité
  et exercer le formatage multi-chiffres, assez petit pour que toute arithmétique plausible reste à des
  centaines de décades du débordement, et il laisse à un `double` environ neuf chiffres significatifs sous
  la virgule. Un type déjà à l'intérieur n'est pas touché, et c'est pourquoi `Half` ne demande aucun cas
  particulier : une règle qui rétrécit l'extravagant et se tait ailleurs est une règle, pas une liste
  d'exceptions.
* **Les générateurs entiers sont exclus sur preuve, pas par commodité.** Leur distribution est la même,
  leur conséquence non : rien dans les mesures ne montre l'arithmétique entière se casser, et le
  débordement qu'un grand entier peut provoquer en aval est souvent le défaut qu'un test doit révéler
  plutôt qu'un bruit qu'il doit éviter. Y étendre la règle atteindrait de surcroît le moteur ordinal
  partagé dont dépendent quatre familles de builders, pour un dommage non démontré.

## Alternatives considérées

### Échantillonner log-uniformément sur tout le domaine

Considérée comme l'option gardant toute magnitude atteignable tout en rendant les extrêmes rares, plus
proche de « n'importe quelle valeur du type » qu'une fenêtre bornée. Rejetée sur ses chiffres : elle
corrigerait le débordement (la décade supérieure tombe à environ 0,01 % des tirages) mais ne toucherait
presque pas le second défaut — la fenêtre ordinaire de 1 à 1e6 fait 6 décades sur 616, donc elle serait
visitée environ 1 % du temps — tout en en introduisant un troisième, puisque la moitié des tirages
tomberait sous 1e0 avec une longue traîne vers 1e-200. Des valeurs aussi petites cassent une autre classe
de code — divisions, comparaisons à epsilon, accumulations qui absorbent le terme — donc l'échange revient
à troquer une pathologie contre deux.

### Mélanger valeurs ordinaires et valeurs remarquables

Considérée parce que tirer majoritairement des valeurs ordinaires avec un 0, un ±1 ou un extrême du
domaine de temps en temps donnerait de la couverture de bord gratuite. Rejetée parce qu'elle rend le dummy
*remarquable* : un tirage sur dix ferait de la fixture le sujet du test, et une suite qui échoue une fois
sur dix pour une raison que le test n'a jamais nommée est exactement le mode de défaillance que cette
décision existe pour supprimer. Un test qui veut un extrême doit le nommer.

### Étendre la règle aux générateurs entiers

Considérée par cohérence, puisque les exclure laisse la bibliothèque avec deux politiques par défaut pour
des nombres. Rejetée pour cette décision sur la preuve ci-dessus — même distribution, conséquence
matériellement plus douce — et sur le rayon d'explosion, le moteur ordinal étant partagé par quatre
familles de builders. L'asymétrie est acceptée sciemment et consignée ici plutôt que laissée à découvrir,
et elle est un candidat légitime pour un ADR ultérieur si le cas entier venait à mordre.

### Ajouter un opt-in explicite pour les valeurs extrêmes

Considérée parce que le comportement actuel stresse les débordements par accident, et que borner le défaut
y met fin. Rejetée comme API inutile : la capacité existe déjà et se lit mieux qu'un opt-in — un
intervalle nommant la magnitude est honoré exactement. Une couverture qui se déclenche 16 % du temps dans
des tests qui parlent d'autre chose est un coût plutôt qu'un bénéfice, et rendre ce test explicite est un
gain sur ce que la suite dit d'elle-même.

## Conséquences

### Positives

* L'arithmétique ordinaire sur un tirage non contraint reste finie et ne lève pas, sur tous les types
  continus.
* Les valeurs générées occupent enfin les magnitudes où vivent les défauts d'arrondi, de comparaison et de
  formatage.
* Les contraintes posées sur la valeur — l'échelle avant tout — contraignent de nouveau quelque chose.
* La bibliothèque énonce un principe unique pour les tailles et les valeurs : un dummy est quelconque à
  moins que quelque chose ne demande explicitement le contraire.

### Négatives

* Un appelant ayant déclaré un intervalle large ne reçoit plus de valeurs réparties dessus :
  `Between(0, double.MaxValue)` rend des valeurs ordinaires. C'est la lecture voulue — la borne est
  honorée, pas visée — mais elle surprendra quiconque lisait une borne large comme une demande, et la
  documentation doit l'énoncer plutôt que de la laisser deviner.
* La couverture accidentelle des débordements que fournissait l'ancien défaut disparaît. Il faut la
  demander explicitement, ce qui est un gain d'intention et une perte pour qui s'y appuyait sans le
  savoir.
* La bibliothèque porte deux politiques par défaut pour des nombres : types continus bornés, entiers
  pleine plage.

### Risques

* Un million est une constante défendable, pas dérivée. L'argument repose sur l'écart entre les magnitudes
  qu'emploie le code ordinaire et celles où les types se comportent mal, pas sur une mesure d'un
  consommateur particulier.
* Un consommateur dont le domaine vit légitimement au-dessus de la fenêtre — quantités astronomiques ou
  cryptographiques — doit nommer son intervalle. Atténué par le fait que la fenêtre s'efface précisément
  dans ce cas.

## Actions de suivi

* Énoncer la règle dans la documentation du package, là où la surface de contraintes est décrite.
* Réexaminer l'exclusion des entiers si un consommateur signale la même classe de dommage.

## Références

* ADR-0050 — Let a size maximum cap without steering the draw : le même principe appliqué aux tailles,
  dont cette décision reprend délibérément le vocabulaire (« une borne est une permission, pas une
  demande »).
* ADR-0041 — Draw flag-enum combinations behind an opt-in : l'acquis selon lequel une bibliothèque non
  publiée peut encore fixer le sens d'un tirage non contraint.
* ADR-0040 — Split the JustDummies test bed between example and property suites : pourquoi la règle de la
  fenêtre est quantifiée en propriétés tandis que les extrêmes mesurés restent des exemples.
