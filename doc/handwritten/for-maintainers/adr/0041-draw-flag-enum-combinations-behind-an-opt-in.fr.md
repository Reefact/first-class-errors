# ADR-0041 | Tirer les combinaisons d'enums de drapeaux derrière un opt-in

🌍 🇬🇧 [English](0041-draw-flag-enum-combinations-behind-an-opt-in.md) · 🇫🇷 Français (ce fichier)

**Statut :** Accepté
**Proposé :** 2026-07-26
**Accepté :** 2026-07-26
**Décideurs :** Reefact

## Contexte

Un enum marqué `[Flags]` déclare des bits destinés à être combinés : ses membres ne sont pas des alternatives mais les parties d'un ensemble. Ses valeurs **valides** sont donc les combinaisons, alors que les valeurs qu'il **déclare** n'en sont que les parties — `Read | Write` est une valeur que le type est conçu pour porter et qu'il ne nomme jamais. La BCL le confirme deux fois : `Enum.GetValues` ne renvoie que les membres déclarés, et `Enum.IsDefined` répond `false` pour une combinaison.

`AnyEnum<TEnum>` tire uniformément parmi les membres déclarés, un contrat que ses propres remarks énoncent. Pour un enum de drapeaux, cela signifie qu'un dummy porte au plus un bit : une branche qui en lit deux — la forme ordinaire du code consommant des drapeaux — n'est donc jamais exercée par une valeur JustDummies. C'est l'inverse de la raison d'être de la bibliothèque : la surface de contraintes existe pour faire remonter les hypothèses cachées, et ici le générateur en installe silencieusement une à son tour (« cette valeur a zéro ou un bit »). C'est la forme même d'un défaut d'atteignabilité, atteint par conception.

Le générateur est tenu par trois règles permanentes de la bibliothèque. Il construit ses valeurs de manière constructive, en un tirage, sans jamais générer-puis-filtrer. Il détecte les contraintes contradictoires au moment de l'appel fluide qui les cause, en nommant les deux côtés. Et il annonce une cardinalité distincte via `ICardinalityHint<TEnum>`, ce qui permet à une collection distincte d'enums d'échouer à la déclaration plutôt qu'à la génération — la taille du domaine de tirage fait donc partie du contrat public, pas du détail d'implémentation.

Deux propriétés des vrais enums de drapeaux comptent pour la forme du domaine. Un enum de drapeaux n'est pas tenu de déclarer un membre nul, et celui qui n'en déclare pas n'a aucune valeur « aucun drapeau » à rendre. Et un enum de drapeaux peut déclarer des **composites** — `ReadWrite = Read | Write`, `All = 7` — qui sont déjà des combinaisons, si bien que plusieurs sous-ensembles des membres déclarés retombent sur la même valeur.

JustDummies n'a jamais été publié : le sens du tirage non contraint est donc encore libre d'être figé. L'audit du 2026-07-20 a recensé les combinaisons de drapeaux comme un ajout piloté par la demande (issue #226).

## Décision

`AnyEnum<TEnum>` continue de tirer parmi les membres déclarés par défaut et acquiert `AllowingCombinations()`, une contrainte explicite élargissant le tirage à la clôture par OU des membres déclarés — plus la valeur nulle lorsque l'enum en déclare une —, refusée sur un enum qui n'est pas `[Flags]` et sur un enum comptant plus de membres non nuls qu'il n'est possible d'énumérer.

## Justification

**Le défaut ne peut pas dépendre de l'attribut.** Faire que `Any.Enum<T>()` se comporte différemment parce que le type porte `[Flags]` rendrait le tirage fonction des métadonnées d'un type plutôt que de ce que le test a écrit, c'est-à-dire exactement la classe de comportement implicite et à distance qu'ADR-0020 a retirée de cette bibliothèque en supprimant les conversions implicites. « Membres déclarés uniquement » est aussi le seul défaut *valide* pour les deux familles d'enums : un membre déclaré est toujours une valeur légitime, alors qu'une combinaison ne l'est que pour un enum de drapeaux. Le conserver coûte un appel à l'utilisateur de drapeaux et ne coûte rien à tous les autres.

**En faire une contrainte, et non une seconde factory,** place le choix là où le lecteur cherche déjà la forme d'une valeur. `Any.Enum<Permissions>().AllowingCombinations()` se lit comme un élargissement du même générateur, se compose avec `OneOf`/`Except`/`DifferentFrom` à travers le pool existant, et ne demande aucun miroir sur `AnyContext` — la factory est inchangée, donc la surface miroir maintenue à la main ne grandit pas.

**L'univers est la clôture par OU des membres déclarés, non celle des bits individuels.** Prendre les membres déclarés comme ensemble générateur absorbe un composite déclaré sans avoir à décider quels membres « sont » des bits : `ReadWrite = Read | Write` n'ajoute rien, et un enum dont les membres ne sont pas tous des puissances de deux ne demande aucun cas particulier. N'ajouter la valeur nulle que lorsqu'un membre nul est déclaré préserve la promesse que toute valeur tirée est une valeur définie par le type : un enum ne déclarant que `Left` et `Right` n'a pas de nom pour l'ensemble vide, et l'inventer serait précisément la valeur non déclarée que le défaut refuse.

**Les exclusions continuent de comparer par égalité.** `Except(Read)` interdit la valeur `Read` et laisse `Read | Write` tirable. Lire le même appel comme un masque de bits sous l'opt-in ferait qu'une méthode signifie deux choses selon qu'une autre contrainte a été déclarée — la même implicité que le défaut rejette — et supprimerait silencieusement la majeure partie de l'univers. La bibliothèque distingue déjà les quasi-synonymes par le nom quand l'intention diffère : une exclusion au niveau du bit, si elle est un jour souhaitée, sera une contrainte nommée à part plutôt qu'une mutation de celle-ci.

**Énumérer l'univers est ce qui préserve les deux garanties permanentes.** Un tirage indépendant par membre serait moins coûteux et sans plafond, mais il est uniforme sur les *sous-ensembles*, pas sur les *valeurs* : en présence d'un composite déclaré, plusieurs sous-ensembles retombent sur une même valeur, qui sort alors bien plus souvent que les autres — et un dummy biaisé est un échec plus grave qu'une contrainte refusée, parce que rien ne le révèle. Matérialiser la clôture garde aussi `ICardinalityHint` exact, ce qui préserve le conflit anticipé sur une collection distincte demandant plus de valeurs qu'il n'en existe. Le prix est que la clôture est exponentielle en nombre de membres : il lui faut un plafond.

**Au-delà du plafond, la contrainte est refusée, pas dégradée.** Un repli silencieux scinderait le générateur en deux régimes — l'un uniforme et vérifié à la déclaration, l'autre ni l'un ni l'autre — que seul le comptage des membres d'un enum permettrait de distinguer. Refuser en nommant la cause, et pointer vers la liste explicite qui sert le cas, est la réponse qu'ADR-0025 a déjà donnée pour les constructions hors du sous-ensemble supporté : une erreur claire vaut mieux qu'une valeur dont l'appelant ne peut prévoir les propriétés. Un enum de drapeaux assez large pour atteindre le plafond est très loin des formes que le vrai code déclare.

## Alternatives envisagées

### Faire des combinaisons le défaut pour les enums `[Flags]`

Envisagée parce qu'elle ne demande aucune API nouvelle et donne à l'utilisateur de drapeaux le bon domaine sans qu'il ait à le demander : « arbitraire mais valide » signifie sans doute déjà les combinaisons pour un type conçu pour les porter.

Rejetée parce que le tirage dépendrait alors des métadonnées du type et non du texte du test : ajouter `[Flags]` à un enum existant changerait silencieusement tout dummy tiré de lui — et, avant cela, toute séquence seedée. Elle élargit aussi le domaine pour les nombreux enums de drapeaux dont les consommateurs ne passent jamais que des membres simples, où un dummy à deux bits est une surprise plutôt qu'une révélation. L'appel explicite coûte une ligne et rend l'élargissement lisible au point d'appel.

### Tirer chaque membre par un pile-ou-face indépendant

Envisagée parce qu'elle tient en quelques lignes, n'a pas de plafond et ne demande aucune énumération : on OR un sous-ensemble aléatoire des membres et le résultat est une combinaison valide par construction.

Rejetée parce qu'elle est uniforme sur les sous-ensembles et non sur les valeurs, si bien que tout composite déclaré biaise fortement la distribution vers la valeur télescopée, et parce qu'elle ne peut annoncer aucune cardinalité distincte — ce qui ferait silencieusement passer les collections distinctes d'enums d'un conflit anticipé à un tirage borné, une régression par rapport au comportement actuel.

### Exposer les combinaisons par une factory séparée

Envisagée parce qu'un point d'entrée distinct énoncerait l'intention encore plus fort et pourrait porter sa propre surface de contraintes.

Rejetée parce qu'elle duplique toute l'algèbre de contraintes des enums pour un seul élargissement, et parce qu'il faudrait la mirrorer sur `AnyContext`, faisant grandir la surface miroir que les gardes de parité existent pour surveiller. Une contrainte sur le builder existant se compose avec tout ce qui y est déjà.

### Lire `Except` comme un masque de bits sous l'opt-in

Envisagée parce que « aucune valeur portant le bit Read » est une demande plausible, et que réutiliser `Except` n'exigerait aucun nom nouveau.

Rejetée parce qu'elle fait qu'une méthode signifie deux choses différentes selon qu'une autre contrainte a été déclarée, et parce qu'elle est la plus destructrice des deux lectures : exclure un bit retirerait la moitié de l'univers, ce qu'un appelant écrivant `Except` sur un enum n'a aucune raison d'attendre. Un nom distinct reste disponible pour ce besoin.

## Conséquences

### Positives

* Un dummy de drapeaux peut porter les combinaisons que le type existe pour porter : une branche lisant deux bits est exercée.
* Le défaut est inchangé : aucun tirage existant, aucune séquence seedée, aucun comportement documenté ne bouge.
* Le domaine élargi passe par le hint de cardinalité existant, donc une collection distincte de combinaisons continue d'échouer à la déclaration plutôt qu'à la génération.
* Les refus — pas `[Flags]`, trop de membres — ont lieu à la déclaration et nomment leur cause, comme le reste de la surface de contraintes.

### Négatives

* L'utilisateur de drapeaux doit savoir que l'appel existe ; un générateur tirant des membres simples reste le défaut qu'il rencontre d'abord.
* L'univers est matérialisé : un enum proche du plafond coûte de la mémoire et un calcul unique proportionnels à son nombre de combinaisons.
* L'opt-in est sensible à l'ordre face à une liste d'autorisation nommant des combinaisons : appliqué après `OneOf`, il élargit un univers que la liste a déjà épinglé, et ne change donc rien.

### Risques

* Un enum assez large pour être refusé est un type supporté dont les combinaisons ne peuvent pas être tirées du tout. Mitigation : le message nomme le plafond et pointe vers la liste explicite ; la forme est très loin de ce que le vrai code déclare, et le plafond peut être relevé par une décision ultérieure sur preuves.
* La sensibilité à l'ordre avec `OneOf` pourrait se lire comme un no-op silencieux. Mitigation : documentée sur les deux membres, et l'ordre inverse — une liste nommant une combinaison avant l'opt-in — échoue avec un message nommant la contrainte manquante plutôt que de l'accepter.

## Actions de suivi

* Si une exclusion au niveau du bit est demandée, l'introduire sous son propre nom plutôt qu'en élargissant `Except`.
* Revisiter le plafond d'énumération si un vrai enum de drapeaux est un jour signalé contre lui.

## Références

* [ADR-0020](0020-materialize-dummies-only-through-generate.fr.md) — la suppression du comportement implicite piloté par les métadonnées dont le défaut reprend le raisonnement, et l'argument de calendrier pré-1.0 réutilisé ici.
* [ADR-0025](0025-generate-strings-from-a-home-grown-regular-subset.fr.md) — la règle « un refus clair vaut mieux qu'une valeur imprévisible » que le plafond applique.
* [ADR-0013](0013-gate-distinct-collections-by-cardinality-else-bounded-draw.fr.md) — le contrat du hint de cardinalité que l'univers matérialisé garde exact.
* [ADR-0032](0032-draw-arbitrary-values-from-an-explicit-top-level-pool.fr.md) — le tirage sur pool explicite vers lequel pointe le message du plafond.
* [ADR-0037](0037-vary-the-datetimeoffset-offset-dimension.fr.md) — le précédent d'une dimension optionnelle dont le défaut reste intouché, y compris la même interaction d'énumération terminale avec `OneOf`.
* Issue [#226](https://github.com/Reefact/first-class-errors/issues/226) — l'entrée de backlog que ceci résout.
