# ADR-0026 | Rebaser les valeurs de test arbitraires sur Dummies

🌍 🇬🇧 [English](0026-rebase-testing-arbitrary-values-on-dummies.md) · 🇫🇷 Français (ce fichier)

**Statut :** Accepté
**Date :** 2026-07-19
**Décideurs :** Reefact

## Contexte

`FirstClassErrors.Testing` est un package compagnon publié (actuellement
`0.1.0-preview.1`). Il fournit les valeurs de test arbitraires au travers d'une
façade statique `Any` adossée à une source pseudo-aléatoire privée, à graine et
locale au contexte ; cette même source adosse aussi les variantes `UseAny()` de
ses deux autres points d'injection — une horloge gelable et des identifiants
d'instance gelables. Cette surface a été décidée par l'ADR-0006, dont l'action de
suivi anticipait l'extraction du moteur de valeurs générique dans un utilitaire
autonome et agnostique aux erreurs « si un second consommateur apparaît », et qui
maintenait le moteur séparable en interne à cette fin.

Depuis l'ADR-0006, cet utilitaire a été livré. L'ADR-0011 a introduit `Dummies` :
une bibliothèque de générateurs fluides autonome, sans dépendance et agnostique
aux erreurs (`IAny<T>`, matérialisée par `Generate()`), hébergée dans ce dépôt
mais ne référençant aucun projet FirstClassErrors — une frontière imposée par un
test d'architecture. L'ADR-0011 a nommé ses premiers consommateurs visés : « les
projets de tests de ce dépôt, et possiblement `FirstClassErrors.Testing` plus
tard », et a laissé une action de suivi explicitement ouverte : « Décider
séparément si `FirstClassErrors.Testing` réasseoit plus tard son moteur de valeurs
interne sur `Dummies`. »

L'état actuel porte les faits qui pèsent sur cette décision :

* **Deux moteurs indépendants coexistent.** `FirstClassErrors.Testing` puise dans
  sa propre source à graine ; `Dummies` puise dans la sienne. Chacun expose sa
  propre portée `Reproducibly`/graine, et les deux graines sont sans rapport : un
  test qui mêle les deux façades ne peut pas être rejoué depuis une unique graine
  signalée.
* **Les deux façades `Any` entrent en collision de noms de type.**
  `FirstClassErrors.Testing.Any` et `Dummies.Any` sont toutes deux
  `static class Any` ; un fichier de test important les deux espaces de noms ne
  peut pas nommer `Any` sans qualification.
* **`Dummies` couvre déjà toutes les capacités dont le rebase a besoin.** Son
  générateur d'énumération exclut des membres (`Except`, `DifferentFrom`,
  `OneOf`) ; ses points d'entrée statiques `Any.*` puisent dans un contexte
  ambiant que `Any.Reproducibly(...)` fixe pour une portée (avec `Any.WithSeed`
  pour un contexte isolé) ; et `As`/`Combine` transforment des primitives sous
  contrainte en valeurs métier. L'ADR-0020 a fait de `Generate()` l'unique
  matérialisation, en retirant les conversions implicites.
* **Le vocabulaire d'erreur ne peut pas migrer dans `Dummies`.** Les utilitaires
  `ErrorCode`, d'énumération significative (`Transience`, `InteractionDirection`)
  et de messages de `Testing` référencent des types FirstClassErrors, que la
  frontière de l'ADR-0011 interdit à `Dummies` de référencer.
* **`Dummies` n'est pas encore sur NuGet.** L'ADR-0011 ne lui donne aucun train de
  release avant sa première publication ; au sein de ce dépôt, il n'est consommé
  qu'au travers d'une référence de projet.

## Décision

`FirstClassErrors.Testing` puise toute valeur arbitraire dans `Dummies` plutôt que
dans un moteur privé : sa façade `Any` et sa source à graine sont retirées, ses
points d'injection d'horloge et d'identifiant d'instance puisent dans le contexte
ambiant reproductible de Dummies, et le vocabulaire d'erreur qu'il conserve est
exposé sous forme de fabriques métier nommées — `ErrorCodeFactory`,
`TransienceFactory`, `DiagnosticMessageFactory`, et consorts — chacune renvoyant
directement une valeur matérialisée (le cas courant) et exposant un générateur
`IAny<T>` via une méthode distincte là où la composition est nécessaire.

## Justification

* **Un moteur, une histoire de graine — l'esprit « source unique » de l'ADR-0006 à
  l'échelle du dépôt.** Deux moteurs signifiaient deux portées `Reproducibly` dont
  les graines ne se composent pas, de sorte qu'un test puisant dans les deux
  façades ne pouvait pas être rejoué depuis une seule graine. Puiser toute valeur
  dans le contexte ambiant de Dummies place les primitives, les valeurs métier,
  l'horloge et les identifiants d'instance sous un unique `Any.Reproducibly(...)`,
  si bien qu'une seule graine signalée rejoue toute l'exécution. C'est la même
  propriété « une source unique, ensemencée une fois » que l'ADR-0006 a choisie,
  étendue au moteur générique qui vit désormais hors du package.
* **Elle réalise l'action de suivi anticipée par l'ADR-0006 *et* l'ADR-0011.** Le
  moteur générique que l'ADR-0006 gardait « séparable en interne » existe désormais
  sous le nom de `Dummies`, et le « second consommateur » qu'il attendait est
  arrivé — les projets de tests de ce dépôt. Réasseoir `Testing` sur `Dummies`
  résout la question ouverte de l'ADR-0011 au lieu de maintenir indéfiniment un
  moteur parallèle.
* **Le rebase n'ajoute aucun manque de capacité.** Tout comportement dont le
  package a besoin existe déjà dans Dummies : l'exclusion de membres pour les
  énumérations significatives, un contexte ambiant reproductible pour l'horloge et
  les identifiants, et `As`/`Combine` pour assembler des valeurs métier. Rien n'a à
  être ajouté à Dummies comme préalable, si bien que le package est façonné par un
  consommateur réel plutôt que par une finalisation prise dans l'abstrait.
* **Le vocabulaire reste dans `Testing`, mais en fabriques, pas en façade.** Parce
  que la frontière de l'ADR-0011 interdit au vocabulaire d'erreur de vivre dans
  `Dummies`, il demeure dans `Testing` ; le réexprimer en types `…Factory` nommés
  plutôt qu'en un second `Any` supprime la collision de noms
  `Testing.Any`/`Dummies.Any`, laissant `Dummies.Any` comme l'unique `Any` qu'un
  test nomme, tandis que les valeurs métier proviennent de fabriques clairement
  nommées.
* **Valeur par défaut, générateur à la demande — sans ranimer le hasard que
  l'ADR-0020 a retiré.** L'appel dominant a besoin d'une valeur arbitraire, donc une
  fabrique la renvoie directement — à l'image des utilitaires de valeurs de test que
  le dépôt utilise déjà — et n'expose un générateur `IAny<T>` via une méthode
  distincte que pour la minorité de sites qui composent (`Any.ListOf`, `Combine`,
  `OrNull`). Une méthode nommée qui appelle `Generate()` en interne n'est pas la
  conversion implicite que l'ADR-0020 a retirée — le tirage est un appel explicite
  et visible, non un élargissement déguisé en affectation — de sorte que la forme
  concise pour la valeur ne coûte aucune des garanties de cette décision. Réserver
  le nom simple du cas courant à la valeur fait écho à l'ADR-0005.
* **C'est le moment le moins cher.** `Testing` est en `0.1.0-preview.1`, un package
  pré-stable sans garantie de compatibilité, donc retirer `Any` ne coûte
  aujourd'hui aucune cérémonie de migration des consommateurs ; le même
  raisonnement pré-1.0 que l'ADR-0020 a utilisé pour retirer les conversions de
  Dummies s'applique ici.

## Alternatives envisagées

### Garder le moteur propre à `Testing` et laisser les projets de tests utiliser `Dummies` directement

Envisagé parce que c'est le moindre travail et que cela livre aujourd'hui : rien
ne change dans `Testing`, et les projets de tests ajoutent simplement `Dummies`
pour les valeurs que sa façade ne couvre pas. Rejeté parce que cela
institutionnalise l'état à deux moteurs — deux graines `Reproducibly` qui ne se
composent pas et une collision de noms `Testing.Any`/`Dummies.Any` — soit la
fragmentation entre tests que l'esprit « source unique » de l'ADR-0006 existe pour
éviter, désormais reproduite à l'échelle du dépôt.

### Déplacer le vocabulaire d'erreur dans `Dummies`

Envisagé parce qu'il ne resterait qu'un seul `Any` et un seul foyer pour les
valeurs arbitraires. Rejeté parce que `Dummies` est agnostique aux erreurs par
l'ADR-0011, une frontière qu'un test d'architecture impose ; le vocabulaire
d'erreur référence des types FirstClassErrors et ne peut pas y vivre sans briser
cette promesse.

### Supprimer le vocabulaire d'erreur et inliner `As(...).Generate()` sur les sites d'appel

Envisagé parce que cela retire purement et simplement une surface publique.
Rejeté parce que cela disperse le vocabulaire d'erreur — et sa convention
« reconnaissable comme arbitraire » — sur chaque consommateur, et retire des
utilitaires livrés d'un package publié sans aucun gain par rapport au fait de les
garder comme fines fabriques.

### Conserver le nom de façade `Any` dans `Testing`, seulement réassis

Envisagé pour minimiser le remaniement des sites d'appel en préservant la forme
familière `Any.ErrorCode()`. Rejeté parce que deux `static class Any` dans deux
espaces de noms restent ambigus dès que les deux sont importés, ce qui abandonne
précisément la clarté que le rebase vise à apporter — un seul `Any`.

### Renvoyer `IAny<T>` uniformément depuis chaque fabrique

Envisagé pour une cohérence stricte avec le modèle recette-contre-valeur de
Dummies. Rejeté parce que cela taxe le cas dominant « donne-moi une valeur
arbitraire » d'un `.Generate()` obligatoire sans bénéfice : une méthode de valeur
nommée n'est pas le hasard de conversion implicite que l'ADR-0020 a retiré, de
sorte que la valeur par défaut conserve les garanties de cette décision tout en
restant concise.

## Conséquences

### Positives

* Un unique moteur de valeurs arbitraires et une unique portée `Reproducibly`/graine
  à l'échelle du dépôt : un test mêlant primitives, valeurs métier, horloge et
  identifiants d'instance se rejoue depuis une seule graine signalée.
* `Dummies.Any` est l'unique `Any` ; la collision de noms de type disparaît, et les
  valeurs métier se lisent depuis des fabriques explicitement nommées.
* `Testing` cesse de maintenir un moteur de valeurs parallèle ; la machinerie
  générique vit une seule fois, dans `Dummies`, façonnée par un consommateur réel
  du dépôt.

### Négatives

* Un changement cassant sur un package publié : `Testing.Any` et son `Reproducibly`
  sont retirés, et les consommateurs migrent vers `Dummies.Any` et les fabriques.
  Acceptable en `0.1.0-preview.1`, et la raison pour laquelle la décision est prise
  avant une release stable.
* `Testing` gagne une dépendance sur `Dummies`. Tant que `Dummies` n'est pas sur
  NuGet, le package `Testing` doit embarquer `Dummies` dans son propre artefact
  pour rester restaurable — un arrangement d'intérim, non l'état final.
* La suite de tests migre de `Any.*` vers `Dummies.Any.*().Generate()` et les
  nouvelles fabriques, et la couverture de reproductibilité/déterminisme de la
  façade retirée est retravaillée sur la nouvelle surface.

### Risques

* Embarquer `Dummies` dans l'artefact `Testing` devient un risque d'identité de
  type dès l'instant où `Dummies` est référençable indépendamment sur NuGet — deux
  assemblies `Dummies` de même identité mais d'origine distincte — précisément
  parce que des types Dummies apparaissent dans l'API publique de `Testing`.
  Atténué par l'action de suivi consistant à basculer vers une dépendance NuGet dès
  la première publication de Dummies.
* Un appelant portant l'ancien modèle mental `Testing.Any` peut chercher un membre
  retiré ; le risque est borné car l'omission est une erreur de compilation au
  message actionnable, jamais une valeur erronée silencieuse — la même classe de
  risque que l'ADR-0020 a acceptée.
* La règle « valeur par défaut, générateur via une méthode distincte » tient par la
  revue et la documentation tant que, le cas échéant, l'outillage ne l'impose pas —
  la même dépendance que l'ADR-0006 a déjà acceptée pour l'habitude « arbitraire ⇒
  utiliser la source ».

## Actions de suivi

* À l'acceptation, l'ADR-0006 est supersédé par le présent ADR (son statut passe à
  *Superseded* avec un lien vers ici), et l'action de suivi ouverte de l'ADR-0011 —
  décider si `Testing` se réasseoit sur `Dummies` — est résolue par cette décision.
* Basculer la dépendance `Dummies` que `Testing` embarque dans son package vers un
  `PackageReference` NuGet dès la première publication de `Dummies`, dénouant
  l'arrangement d'intérim et retirant le risque de double assembly.
* Mettre à jour le guide de test destiné aux utilisateurs et le README du package
  `Testing`, en anglais et en français de manière synchronisée : `Any` est retiré,
  les valeurs arbitraires proviennent de `Dummies`, et les fabriques métier sont
  introduites.
* Ajouter un petit projet `FirstClassErrors.Testing.UnitTests` ne portant que les
  tests de contrat que le package conserve — reproductibilité de l'horloge et des
  identifiants d'instance sous `Any.Reproducibly`, et fabriques d'énumération
  significative ne renvoyant jamais la sentinelle — et retirer les assertions de
  valeur enveloppante désormais couvertes en transitif et par `Dummies.UnitTests`.

## Références

* ADR-0006 — Fournir les valeurs de test arbitraires depuis une source unique à
  graine : la décision que celui-ci supersède, et l'action de suivi (extraire le
  moteur générique pour un second consommateur) qu'il réalise.
* ADR-0011 — Héberger Dummies comme package autonome : la frontière agnostique aux
  erreurs qui garde le vocabulaire dans `Testing`, et l'action de suivi ouverte que
  celui-ci résout.
* ADR-0020 — Matérialiser les dummies uniquement par `Generate()` : le hasard de
  conversion implicite qu'une fabrique de valeur nommée ne ranime pas, et le
  raisonnement pré-1.0 réutilisé ici.
* ADR-0005 — Réserver le nom de fabrique simple à la variante renvoyant un Outcome :
  la décision de nommage antérieure dans le même esprit, le nom simple servant le
  cas courant.
