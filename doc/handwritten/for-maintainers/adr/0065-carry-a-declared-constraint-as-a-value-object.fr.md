# ADR-0065 | Porter une contrainte déclarée comme objet-valeur, non comme son texte rendu

🌍 🇫🇷 Français (ce fichier) · 🇬🇧 [English](0065-carry-a-declared-constraint-as-a-value-object.md)

**Statut :** Accepté
**Proposé :** 2026-07-30
**Accepté :** 2026-07-31
**Décideurs :** Reefact

## Contexte

Une contradiction entre deux contraintes échoue à la déclaration, avec un message nommant les deux
côtés — `Cannot apply Between(0, 100) because GreaterThan(200) is already defined.` Nommer la
contrainte que l'appelant a écrite, dans l'orthographe qu'il a écrite, fait partie du contrat de la
bibliothèque : une contradiction dans l'`Arrange` d'un test est un défaut du test et doit se lire
comme tel. ADR-0063 fait passer ces levées par des factories nommées d'après l'échec.

Jusqu'à cette décision, une contrainte atteignait ces messages sous forme de chaîne assemblée au site
qui la déclarait. Il y en avait environ 290, répartis sur une trentaine de fichiers : les méthodes
fluides des générateurs, les quatre moteurs d'intervalle, les spécifications de chaîne, de
collection, de comptage et d'URI. Trois formes revenaient — un nom seul, un nom avec ses arguments
rendus, et un nom dont la bibliothèque ne doit pas rendre les arguments parce que le type du pool est
opaque et que son `ToString` appartient à l'appelant. Chaque site écrivait ses propres parenthèses.

Trois propriétés découlent de cet arrangement, et ce sont des faits sur le code tel qu'il était :

* L'orthographe n'était pas liée à la méthode qu'elle nommait. Renommer une méthode publique laissait
  ses diagnostics en arrière, et un nom mal orthographié était un littéral qui compilait.
* Une spécification ne fait pas que rendre une contrainte ; elle les **compare**. Une vingtaine de
  comparaisons décident si une seconde déclaration est une redéclaration inoffensive — le même appel
  avec les mêmes arguments, qui rend la spécification inchangée — ou un vrai conflit. Certaines
  étaient écrites en comparaison ordinale de chaînes, d'autres avec `==`.
* Plusieurs points d'entrée de moteur prennent une contrainte à côté d'autres chaînes — un nom de
  type, une borne rendue, une clause d'épuisement — sans rien pour les distinguer que leur position.

Deux autres faits pèsent sur la forme de la solution. Construire une exception ne doit jamais lever,
ce que consigne ADR-0064 et pourquoi le chemin de report d'échec est exempté des gardes d'arguments.
Et le `ConstraintClaim` d'ADR-0063 apparie un sujet blâmé avec ce qu'il affirme : ce sujet est
généralement une contrainte que l'appelant a écrite, mais pas toujours — une partie d'une forme peut
être blâmée aussi, et ce sont des phrases que la bibliothèque compose.

Le dépôt a déjà vécu ceci avec des règles que seul un lecteur applique : ADR-0056 consigne une règle
de type explicite qui avait dérivé à 203 violations tant qu'elle vivait dans un fichier de réglages
sur lequel rien ne pouvait agir.

## Décision

Une contrainte déclarée est portée dans toute la bibliothèque comme un objet-valeur qui se rend
lui-même, jamais comme le texte qu'il rend.

## Justification

La ponctuation qui fait lire une contrainte comme un appel appartient à un seul endroit. Écrite à 290
sites, c'est 290 occasions de diverger, et une divergence dans un diagnostic est invisible jusqu'à ce
que quelqu'un lise le message qui s'est trompé.

Lier le nom à la méthode par `nameof` convertit deux classes de défauts en échecs de compilation. Un
renommage emporte désormais ses diagnostics au lieu de les laisser périmés en silence, et une faute
d'orthographe cesse d'être un littéral qui compile. C'est le même geste qu'argumente ADR-0056 : une
règle que le compilateur peut exprimer doit l'être là plutôt que confiée à l'attention, puisque
l'attention est précisément ce dont on a montré qu'elle échouait.

L'égalité doit appartenir au type plutôt qu'à chaque site de comparaison, parce que la comparaison
porte du comportement : c'est elle qui sépare une redéclaration qui doit être un no-op d'une qui doit
entrer en conflit. Définir `==` fait partie de la décision et non du confort — ces comparaisons sont
écrites avec, et un type référence sans opérateur compare des identités en silence, transformant
chaque redéclaration légitime en conflit sans rien dans le compilateur ni dans le système de types
pour l'attraper. C'est le seul mode de défaillance de ce domaine qu'aucune autre garde n'aurait
trouvé.

Rendre au moment où la contrainte est déclarée, plutôt qu'au moment où un message est composé, est ce
qui rend le type sûr sur le chemin que protège ADR-0064. Une contrainte est citée pendant qu'une
exception se construit ; si la citer pouvait composer quoi que ce soit, elle pourrait échouer là.
Relire un texte produit sur le chemin qui a réussi, non.

Typer la seule contrainte appliquée n'aurait pas suffi. Les comparaisons opposent la contrainte
appliquée à celle qu'une spécification a enregistrée : les deux côtés doivent donc être du même type,
sinon la comparaison se dégrade en quelque chose de plus faible sans le dire. Les épingles stockées
portent donc le type aussi, et les factories d'exception qui les citent l'acceptent — ce qui ferme la
surface : dès que tout paramètre signifiant « une contrainte » a le type, une contrainte ne peut plus
s'écrire en littéral nulle part dans la bibliothèque.

L'emplacement du sujet de `ConstraintClaim` reste capable de porter une phrase, parce qu'un sujet
blâmé n'est réellement pas toujours une contrainte. Nommer ce cas plutôt que laisser une phrase
passer par l'emplacement de contrainte préserve le sens de cet emplacement, et une phrase ne porte
pas de contrainte — ce qui la rend précisément jamais égale à celle qu'on applique, la comparaison
dont dépend le choix du blâme.

Le coût accepté est un changement large et mécanique : l'état stocké de chaque moteur et chaque
méthode fluide ont bougé d'un coup, parce que la signature d'un moteur partagé ne peut pas changer
pour un seul appelant. Il a été pris par tranches, chacune compilant et passant seule.

## Alternatives considérées

### Conserver les chaînes et ajouter une convention de nommage

Considérée parce qu'elle ne coûte rien à adopter et laisse chaque site d'appel tel quel.

Rejetée parce que c'est l'arrangement qui existait déjà, et que les propriétés qui lui manquent sont
celles qui comptent : une convention ne peut pas faire suivre un renommage, ni faire échouer le build
sur une faute, ni donner son sens à une comparaison. ADR-0056 consigne ce qu'il advient d'une règle
de ce genre dans ce dépôt quand rien ne peut agir dessus.

### Ajouter un analyseur vérifiant la forme des littéraux

Considérée parce que le dépôt livre déjà des analyseurs de première main (ADR-0044) et en emploie un
là où le système de types n'atteint pas (ADR-0059).

Rejetée parce que le système de types *atteint* ici. Un analyseur vérifierait qu'un littéral ressemble
à un appel tout en le laissant littéral — il ne pourrait ni lier l'orthographe à la méthode, ni
donner sa sémantique à la comparaison de redéclaration. ADR-0059 emploie un analyseur là où aucun
type n'exprime la règle ; ici un type l'exprime.

### En faire une structure

Considérée pour l'allocation sur un chemin qui s'exécute une fois par contrainte déclarée.

Rejetée sous la règle permanente du dépôt selon laquelle une valeur portant un invariant est une
classe : une structure expose un constructeur sans paramètre produisant une instance ayant contourné
toute factory. Le même raisonnement que `ConstraintClaim` énonce pour lui-même.

### Rendre paresseusement, en composant le texte quand un message le demande

Considérée parce qu'une contrainte n'atteignant jamais un conflit ne serait alors jamais rendue, et
la plupart ne l'atteignent pas.

Rejetée parce que le moment où une contrainte *est* rendue est celui où une exception se construit,
soit le seul endroit où la bibliothèque ne doit pas faire un travail qui peut échouer (ADR-0064).
Échanger une garantie sur le chemin d'échec contre une allocation sur le chemin de succès va dans le
mauvais sens.

### Ne typer que la contrainte appliquée, en laissant les stockées en chaînes

Considérée comme un changement plus petit atteignant l'essentiel du bénéfice.

Rejetée parce que les deux sont comparées l'une à l'autre. Laisser un côté en chaîne force soit un
rendu à chaque comparaison — réintroduisant le texte que la décision supprime — soit une comparaison
signifiant moins qu'avant.

## Conséquences

### Positives

* L'orthographe d'une contrainte suit la méthode qu'elle nomme ; un renommage emporte les diagnostics.
* Un nom de contrainte mal orthographié ou inventé est un échec de build, non un message que
  personne ne lit avant qu'il soit faux.
* Redéclaration contre conflit est une propriété du type, décidée une fois au lieu de vingt.
* Les parenthèses existent une fois.
* Des paramètres voisins jusque-là interchangeables sont maintenant distinguables par type.
* Citer une contrainte dans un message ne peut pas échouer, par construction et non par inspection.
* Un littéral de contrainte ne peut plus s'écrire nulle part dans la bibliothèque ; le compilateur le
  refuse.

### Négatives

* Un changement large : chaque générateur, l'état stocké de chaque moteur et les factories
  d'exception ont bougé.
* Un second petit objet-valeur vit à côté de `ConstraintClaim` dans le même domaine, et un lecteur
  doit les distinguer — une contrainte, contre un sujet apparié à ce qu'il affirme.
* L'égalité, ses opérateurs et leur couverture sont désormais à la charge du type.

### Risques

* Un sujet blâmé n'est pas toujours une contrainte, donc une forme « phrase » subsiste. Un
  contributeur pourrait y faire passer une vraie contrainte et perdre le typage pour ce message.
  Atténué en nommant la factory de phrase pour le cas qui la justifie, plutôt que de laisser un
  emplacement stringly-typed acceptant les deux.
* Les générateurs rendent encore leurs propres arguments via des helpers par type : les *arguments*
  d'une contrainte restent donc des chaînes assemblées localement. La surface est plus petite qu'avant
  et spécifique au type par nature, mais c'est là qu'une incohérence de rendu pourrait encore
  apparaître.

## Actions de suivi

* Envisager de dédupliquer les rendus d'arguments par générateur, quasi identiques d'un générateur
  scalaire à l'autre et ne divergeant que là où un type rend réellement différemment.
* Réexaminer si le sujet de `ConstraintClaim` et ce type doivent fusionner, une fois les cas de
  phrase mieux compris.

## Références

* [ADR-0040](0040-split-the-justdummies-test-bed-between-example-and-property-suites.fr.md) — quelle
  suite possède la formulation d'un message.
* [ADR-0044](0044-ship-justdummies-analyzers.fr.md) — analyseurs de première main.
* [ADR-0056](0056-state-the-coding-rules-where-an-agent-can-act-on-them.fr.md) — une règle sur
  laquelle rien ne peut agir dérive.
* [ADR-0059](0059-guard-the-recipe-versus-value-boundary-with-analyzers.fr.md) — analyseurs là où le
  système de types n'atteint pas.
* [ADR-0063](0063-throw-the-library-s-own-exceptions-through-named-factories.fr.md) — factories de
  levée nommées, et `ConstraintClaim`.
* [ADR-0064](0064-exempt-the-whole-failure-reporting-path-from-the-null-guard-convention.fr.md) —
  construire un report d'échec ne doit pas échouer.
