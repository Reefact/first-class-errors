# ADR-0053 | Unifier la génération discrète dans un espace ordinal unique, avec un moteur dédié seulement là où le substrat arithmétique l'impose

🌍 🇫🇷 Français (ce fichier) · 🇬🇧 [English](0053-unify-discrete-generation-in-one-ordinal-space.md)

**Statut :** Proposé
**Date :** 2026-07-28
**Décideurs :** Reefact

## Contexte

JustDummies expose la même algèbre de contraintes en forme d'intervalle sur un large ensemble de types
valeur : les huit entiers de largeur fixe, `char`, `TimeSpan`, `DateTime`, `DateTimeOffset`, `DateOnly`,
`TimeOnly`, les trois types à virgule flottante binaire, `decimal` et les deux entiers 128 bits. Sur tous,
un test peut déclarer des bornes, une liste blanche, des exclusions et — là où le type a un pas naturel —
un réseau : un multiple, une granularité temporelle ou une échelle décimale.

Deux promesses valables pour toute la bibliothèque contraignent la façon d'implémenter cette algèbre. Les
valeurs sont **construites pour satisfaire** les contraintes déclarées plutôt que tirées puis filtrées :
un générateur qui existe doit produire une valeur en un seul tirage, sans boucle de reprise. Et des
contraintes qui se contredisent doivent échouer à la déclaration avec un message nommant **les deux**
côtés, ce qui exige que chaque borne porte la contrainte qui l'a posée, et non un simple nombre.

Les types se divisent selon leur substrat arithmétique, non selon leur nature :

* Tout type discret dont le domaine tient sur 64 bits — les entiers, les types temporels fondés sur les
  ticks, les numéros de jour et d'heure du jour, `char` — admet une projection **préservant l'ordre** vers
  l'intervalle des entiers non signés 64 bits. Bornes, exclusions, pas, cardinalité et échantillonnage
  deviennent alors un seul et même problème pour tous, énoncé une fois sur les ordinaux.
* `Int128` et `UInt128` ont des domaines qui dépassent 64 bits : aucune telle projection vers un ordinal
  64 bits n'existe.
* La virgule flottante binaire IEEE est continue. Ses motifs de bits sont monotones et pourraient être
  projetés, mais un tirage uniforme sur les motifs de bits n'est pas un tirage uniforme sur les valeurs —
  environ la moitié des `double` se situent dans `[-1, 1]`. Exclure un point d'un continuum diffère aussi
  en nature d'une exclusion dans un ensemble fini : la collision est de mesure nulle, et la contrainte doit
  pourtant être honorée exactement.
* `decimal` est une mantisse de 96 bits assortie d'une échelle, et il n'a pas d'échelle des valeurs
  représentables successives : une borne exclusive ne peut donc pas s'exprimer en passant à la valeur
  adjacente, comme c'est le cas pour les entiers et pour les flottants.

La cible plancher est netstandard2.0 (l'ADR-0022 fixe le plancher .NET Framework sur lequel la bibliothèque
doit continuer de se charger). Elle n'offre aucune abstraction d'arithmétique générique sur les types
numériques, ni aucun entier 128 bits, si bien que l'arithmétique ne peut pas être écrite une fois contre un
paramètre de type numérique dans du code qui doit compiler sur le plancher. C# interdit par ailleurs le
motif de classe de base générique auto-référentielle pour les générateurs publics scellés que cette API
expose.

La duplication qui en résulte est réelle et a été mesurée par l'audit d'architecture du 20/07/2026 : les
quatorze générateurs numériques sont des clones quasi identiques à substitution de type près — environ
2 450 lignes — et les cinq générateurs temporels suivent le même motif sur quelque 800 lignes de plus. Un
balayage scripté de ces familles de clones n'a trouvé aucun écart de comportement par copier-coller, et
l'issue #214 a depuis ajouté des garde-fous de parité par réflexion, sur les points d'entrée miroir comme
sur le jeu de méthodes de contrainte de chaque famille.

Cet agencement est la décision qui façonne le plus les entrailles de la bibliothèque, et il contraint la
façon dont tout futur générateur discret ou numérique sera ajouté. Son raisonnement ne vit que dans la
documentation XML interne, alors que des décisions plus petites — le plafond d'arité d'`Any.Combine`
(ADR-0015) — portent des enregistrements.

## Décision

Tout type valeur discret dont le domaine tient sur 64 bits est généré par un moteur partagé unique opérant
sur un espace ordinal commun d'entiers non signés 64 bits, et un moteur distinct n'existe que là où le
substrat arithmétique ne peut pas y être représenté : les entiers 128 bits, la virgule flottante binaire
IEEE et `decimal`.

## Justification

* **L'espace ordinal est ce qui permet d'énoncer les promesses difficiles une seule fois.** La
  satisfiabilité immédiate, l'exclusion exacte en un tirage, le comptage de cardinalité et la détection de
  conflit sont les parties de l'algèbre qu'il est facile de rater subtilement, et coûteux de rater en plus
  d'un endroit. Sur les ordinaux, elles forment un seul problème avec une seule implémentation, et tout
  type discret 64 bits hérite des mêmes garanties par construction plutôt que par relecture. Une borne de
  `DateTime` et une borne d'`Int64` sont alors le même objet : la promesse de nommer les deux côtés d'un
  conflit n'a pas à être regagnée type par type.
* **La séparation suit le substrat, ce qui la rend réfutable plutôt qu'affaire de goût.** Chaque moteur
  dédié existe parce qu'une propriété énoncée de son arithmétique — largeur au-delà de 64 bits, continuité,
  absence d'échelle des valeurs représentables — rend inapplicable la formulation du moteur partagé, et non
  parce que son type « semblait » différent. La règle se lit comme un test qu'un futur mainteneur peut
  appliquer : un nouveau type reçoit une projection si son domaine tient dans l'espace ordinal, et un
  moteur seulement s'il est démontrable qu'il n'y tient pas.
* **Un tirage uniforme doit être uniforme sur les valeurs, pas sur les représentations.** C'est pourquoi
  les motifs de bits monotones de la virgule flottante ne sont pas forcés dans l'espace ordinal, bien que
  la projection existe. L'uniformité ordinale est exactement juste là où des ordinaux consécutifs
  désignent des valeurs consécutives, et exactement fausse là où ce n'est pas le cas ; garder les types
  continus sur leur propre moteur préserve le sens d'« arbitraire » pour les deux groupes.
* **La cible plancher supprime l'alternative générique : le choix se réduit à un moteur partagé plus trois
  exceptions, ou à aucun partage du tout.** Sans arithmétique générique sur netstandard2.0, partager
  l'arithmétique entre types numériques exige soit une indirection ordinale, soit du code par type.
  L'espace ordinal achète le partage pour le plus grand groupe — treize types — en n'utilisant que
  l'arithmétique entière fournie par le plancher, et le paie aux trois endroits où il ne peut réellement
  pas s'appliquer.
* **La duplication acceptée est bornée et désormais gardée.** Le coût de cette décision est que les
  parties de l'algèbre non partageables sont énoncées jusqu'à quatre fois. Ce coût a été accepté en
  connaissance de cause : le balayage de l'audit n'a trouvé aucune dérive de comportement, et les
  garde-fous de parité de l'issue #214 font passer « les clones s'accordent » d'une discipline à un test
  qui échoue. Une décision dont le principal inconvénient est surveillé par un test n'est pas dans la même
  position qu'une décision dont l'inconvénient est surveillé par l'attention.

## Alternatives considérées

### Un moteur unique sur un espace ordinal élargi

Considérée comme la version de cette conception sans exception : tout projeter dans un ordinal 128 bits, ou
de précision arbitraire, et garder un moteur unique pour toute la surface numérique et discrète.

Rejetée d'abord au titre de la cible plancher — netstandard2.0 n'a aucun type entier 128 bits, si bien que
le moteur partagé ne pourrait pas compiler là où la bibliothèque doit se charger, et un substitut à
précision arbitraire placerait un type numérique allouant sur chaque tirage des treize types qui n'en ont
aucun besoin. Elle n'atteindrait pas non plus ce qu'elle promet : élargir l'ordinal ne traite que le
problème de largeur, laissant l'uniformité en virgule flottante et l'échelle manquante de `decimal`
exactement en l'état. Le moteur unifié aurait toujours besoin de branches par substrat, ayant perdu la
propriété qui rendait l'unification intéressante.

### Un moteur par type, sans partage

Considérée pour sa simplicité : chaque générateur possède ses bornes, ses exclusions et son échantillonnage,
sans indirection à comprendre ni notion d'ordinal à apprendre.

Rejetée parce qu'elle multiplie les parties difficiles de l'algèbre — satisfiabilité immédiate, projection
des exclusions, provenance des conflits — par le nombre de types plutôt que par le nombre de substrats. Les
familles de clones mesurées par l'audit montrent ce que cela coûte même là où le code est produit par
discipline : la duplication qui subsiste sous cette décision est la part non partageable, et un moteur par
type dupliquerait aussi la part partageable.

### Une classe de base numérique générique sur un paramètre de type auto-référentiel

Considérée comme la voie offerte par le langage pour abstraire l'arithmétique sans indirection ordinale, ce
qui donnerait une implémentation unique tout en préservant l'arithmétique native de chaque type.

Rejetée comme indisponible plutôt qu'indésirable : C# interdit ce motif pour les types de générateurs
publics scellés que cette API expose, et netstandard2.0 ne fournit aucune contrainte d'arithmétique
générique à travers laquelle l'arithmétique pourrait s'exprimer — la classe de base n'aurait donc rien à
abstraire. L'issue #214 a enregistré les tests de parité par réflexion comme mitigation de la duplication
que cette alternative devait supprimer.

### Projeter la virgule flottante par ses motifs de bits

Considérée parce que les formats binaires IEEE s'ordonnent de façon monotone en tant qu'entiers, ce qui
rend la projection disponible et ferait entrer trois types de plus dans le moteur partagé.

Rejetée parce qu'elle change silencieusement ce que signifie une valeur arbitraire. L'uniformité sur les
ordinaux devient une uniformité sur les représentations, ce qui, en virgule flottante, concentre le tirage
près de zéro ; et l'exclusion ponctuelle sur un continuum, qui doit être honorée exactement sur un ensemble
de mesure nulle, est un problème différent de l'exclusion sur un ensemble ordinal fini. La projection est
possible, la sémantique ne l'est pas, et cette équivalence est la seule raison de partager un moteur.

### Projeter `decimal` par sa mantisse et son échelle

Considérée pour la même raison : `decimal` est discret, une injection dans un espace ordinal paraît donc
naturelle, et il ne resterait que deux moteurs dédiés.

Rejetée parce que la discrétion de `decimal` n'est pas uniforme. Une même valeur possède plusieurs
représentations à des échelles différentes, et la distance entre valeurs représentables adjacentes dépend de
l'échelle : une projection préservant l'ordre vers un intervalle ordinal contigu n'existe donc pas sans
fixer d'abord une échelle — laquelle est une contrainte que l'appelant peut déclarer ou non. Les bornes et
les pas de `decimal` doivent par conséquent s'exprimer dans sa propre arithmétique.

## Conséquences

### Positives

* La partie difficile de l'algèbre discrète — satisfiabilité immédiate, exclusion exacte, cardinalité,
  provenance des conflits — a une seule implémentation pour treize types : un correctif ou une nouvelle
  contrainte atterrit une fois pour tous.
* Ajouter un type discret dont le domaine tient sur 64 bits, c'est une projection et un nom d'affichage,
  pas un nouveau moteur.
* La frontière entre moteurs est énoncée comme une propriété du substrat : un futur mainteneur peut décider
  où appartient un nouveau type sans rejuger l'architecture.
* Un tirage discret reste uniforme sur les valeurs de son type, et aucune projection dans l'espace des
  représentations ne déforme un tirage continu. Quelle magnitude favorise un tirage continu non contraint
  est une décision distincte, enregistrée dans l'ADR-0052 ; celle-ci ne règle que le fait que la
  déformation ne vient jamais de la projection.

### Négatives

* Quatre moteurs signifient que les parties de l'algèbre qui *paraissent* partageables sont écrites jusqu'à
  quatre fois, et qu'une évolution de l'algèbre peut devoir être appliquée dans chacune. Le moteur 128 bits
  est délibérément un frère mot pour mot du moteur ordinal, ce qui est le cas le plus net de ce coût.
* Un lecteur doit apprendre l'indirection ordinale avant de suivre comment une borne de `DateTime` devient
  un tirage ; le moteur partagé est agnostique du domaine par conception, donc rien en lui ne nomme les
  types qu'il sert.
* La décision fixe la frontière des 64 bits comme seuil de partage. C'est l'arithmétique entière la plus
  large que fournit la cible plancher, pas un optimum démontré.

### Risques

* Les familles de clones peuvent dériver en comportement à la prochaine édition de l'une d'elles. Mitigé
  par les garde-fous de parité de l'issue #214, qui échouent sur une contrainte renommée ou manquante, et
  borné par le fait que la dérive trouvée par l'audit portait sur la documentation, non sur le comportement.
* Un futur type pourrait tenir dans l'espace ordinal en principe alors que sa sémantique rend l'uniformité
  ordinale fausse, comme c'est le cas de la virgule flottante. Le test de la décision est énoncé en termes
  de représentabilité : un tel cas doit donc être reconnu sur ses propres mérites plutôt qu'en appliquant
  la règle à la lettre.

## Actions de suivi

* Enregistrer séparément le contrat de déterminisme et de source ambiante (issue #216) ; c'est l'autre
  décision transversale que l'audit a trouvée non enregistrée, et celle-ci ne la règle pas.
* À la prochaine divergence entre le moteur 128 bits et le moteur ordinal, préciser si elle est
  intentionnelle ou s'il s'agit d'une dérive — cette décision attend qu'ils restent frères mot pour mot.

## Références

* ADR-0011 — Héberger JustDummies comme paquet autonome dans ce dépôt : la décision d'empaquetage à
  l'intérieur de laquelle se place cette architecture interne.
* ADR-0013 — Verrouiller les collections distinctes par cardinalité, sinon par tirage borné : le
  raisonnement de cardinalité que sert le comptage du moteur partagé.
* ADR-0015 — Plafonner `Any.Combine` à l'arité huit : la décision plus petite dont l'enregistrement
  existant rendait l'absence de celle-ci frappante.
* ADR-0022 — Fixer le plancher de support .NET Framework de la bibliothèque à 4.7.2 : la cible plancher qui
  supprime l'alternative de l'arithmétique générique.
* ADR-0025 — Générer des chaînes correspondantes depuis un sous-ensemble régulier maison : la décision
  voisine de construire les valeurs constructivement plutôt que par filtrage.
* ADR-0052 — Tirer les nombres arbitraires dans une magnitude ordinaire : la décision voisine qui régit
  ce que favorise un tirage continu non contraint, sur les deux moteurs que celle-ci garde séparés.
* Issue #217 — l'item de l'audit qui a demandé cet enregistrement.
* Issue #214 — les garde-fous de parité sur lesquels cette décision s'appuie pour garder sûre la
  duplication qu'elle accepte.
* [Audit d'architecture et de conception JustDummies du 20/07/2026](../audit/2026-07-20-dummies-architecture-and-design-audit.fr.md),
  §5 — là où l'enregistrement manquant a été signalé, avec la taille mesurée des familles de clones.
