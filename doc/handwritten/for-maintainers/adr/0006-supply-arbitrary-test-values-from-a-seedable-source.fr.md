# ADR-0006 | Fournir les valeurs de test arbitraires depuis une source unique à graine

🌍 🇬🇧 [English](0006-supply-arbitrary-test-values-from-a-seedable-source.md) · 🇫🇷 Français (ce fichier)

**Statut :** Accepté
**Date :** 2026-07-16
**Décideurs :** Reefact

## Contexte

Le package compagnon `FirstClassErrors.Testing` existe pour que les tests
portant sur les erreurs et les outcomes se lisent comme des tests portant sur
des valeurs. Il livre déjà deux points d'injection de test — une horloge
gelable et des identifiants d'instance gelables — qui partagent un même
contrat : les substitutions sont délimitées par `using`, disposables, et
locales au contexte (adossées à `AsyncLocal`), de sorte qu'elles ne fuient
jamais entre des tests s'exécutant en parallèle.

Un test a presque toujours besoin d'entrées sur lesquelles il ne fait **pas**
d'assertion : un code d'erreur, un message de diagnostic ou un message court,
un instant d'occurrence, un identifiant d'instance. Deux faits découlent de la
manière dont ces entrées sont fournies aujourd'hui.

* **Lisibilité.** Un littéral choisi à la main (`ErrorCode.Create("PAYMENT_DECLINED")`)
  se lit comme significatif — « cette valeur compte pour le test » — même
  lorsque le test ne la vérifie jamais. Le lecteur ne peut pas distinguer une
  valeur ayant fait l'objet d'une assertion d'une valeur incidente.
* **Surajustement.** Une constante fixe réutilisée dans toute une suite peut
  faire passer un test pour la mauvaise raison (il se trouve qu'elle correspond
  à cette unique constante). Une valeur qui varie d'une exécution à l'autre fait
  apparaître un tel couplage.

Les bibliothèques établies résolvent cela avec un dispositif de « valeur
anonyme / arbitraire » (AutoFixture, Bogus, l'utilitaire `Any` de GOOS, les
générateurs FsCheck). En adopter un ici doit satisfaire deux contraintes
strictes de ce package :

* **Zéro dépendance d'exécution tierce.** La promesse affichée du package est
  qu'il « n'ajoute rien à vos dépendances de production ». Un package de support
  de test livré qui prendrait une dépendance sur AutoFixture/Bogus/FsCheck
  imposerait cette dépendance, de manière transitive, à chaque consommateur.
* **Sûreté des tests parallèles sur .NET Standard 2.0.** Les exécuteurs de
  tests exécutent les classes de test de manière concurrente ; un unique
  `System.Random` partagé et mutable n'est pas thread-safe et produirait des
  interférences entre tests et des valeurs non reproductibles.
  `System.Random.Shared` n'existe pas sur la cible netstandard2.0.

Reproduire un échec qui a utilisé une valeur arbitraire nécessite une
**graine** : sans elle, une exécution en échec ne peut pas être rejouée.

## Décision

`FirstClassErrors.Testing` fournit les valeurs de test arbitraires au travers
d'une source pseudo-aléatoire unique, sans dépendance et locale au contexte,
dont le déterminisme est optionnel (opt-in) au travers d'un exécuteur
`Reproducibly` qui ensemence une exécution, signale la graine en cas d'échec,
et la rejoue à la demande ; les points d'injection de l'horloge et de
l'identifiant d'instance gagnent des variantes `UseAny` sans graine qui puisent
dans cette même source.

## Justification

* **Préserve la promesse zéro dépendance.** Une source de première partie
  construite sur `System.Random` n'ajoute aucune référence de package, si bien
  que la promesse selon laquelle le package n'ajoute rien aux dépendances d'un
  consommateur tient — la contrainte qui écarte AutoFixture/Bogus/FsCheck.
* **Sûr en parallèle en réutilisant l'idiome propre au package.** Stocker la
  source dans un `AsyncLocal` donne à chaque contexte d'exécution son propre
  générateur, ce qui est exactement le contrat « ne fuit jamais entre tests
  parallèles » que les points d'injection de l'horloge et de l'identifiant
  d'instance respectent déjà. La nouvelle surface a la même forme que la surface
  qu'elle côtoie, plutôt que d'être un second mécanisme sans rapport.
* **Arbitraire par défaut, reproductible à la demande.** Un défaut sans graine
  fait varier les valeurs d'une exécution à l'autre, ce qui est précisément ce
  qui met au jour le surajustement ; envelopper un test sensible aux valeurs
  dans `Reproducibly` fixe une graine, la signale lorsque le corps échoue, et la
  rejoue lorsqu'on lui en fournit une — de sorte qu'une exécution en échec est
  récupérable sans forcer chaque test à gérer une graine. Parce que la graine
  réside sur l'exécution qui détient le flux d'exécution, les portées autonomes
  `UseAny` de l'horloge et de l'identifiant d'instance n'ont besoin d'aucune
  surcharge à graine propre : exécutées à l'intérieur de `Reproducibly`, elles
  héritent de la graine.
* **Le nom porte l'intention.** Recourir à une valeur explicitement
  *arbitraire* se lit comme « cette entrée est incidente », ce qui est la
  distinction qu'un littéral choisi à la main ne peut pas faire. Les variantes
  `UseAny` étendent la famille existante `UseFixed` / `UseSequential` sur les
  mêmes points d'injection, de sorte que l'horloge et les identifiants gagnent
  la même expression « la valeur est ici sans importance » qu'offre la fabrique
  de valeurs.

## Alternatives envisagées

### Dépendre d'AutoFixture, Bogus ou FsCheck

Ce sont les outils matures et bien compris pour les données de test
arbitraires. Rejeté parce que chacun est une dépendance d'exécution tierce
qu'un package de support de test livré pousserait sur chaque consommateur,
brisant la promesse zéro dépendance du package ; leur machinerie plus large de
graphes d'objets et de générateurs dépasse également ce dont a besoin un
utilitaire petit et ciblé du type « donne-moi une valeur sur laquelle je ne
fais pas d'assertion ».

### Une source statique partagée sans localité de contexte

L'implémentation la plus simple — un unique `Random` statique derrière la
façade. Rejeté parce qu'elle n'est pas sûre sous exécution parallèle des tests :
les tirages concurrents interfèrent, les valeurs ne sont pas reproductibles, et
le couplage entre tests sans rapport est exactement ce que les points
d'injection existants du package ont été conçus pour éviter.

### Un générateur basé sur instance (`new …(seed)`) comme surface primaire

La forme de style AutoFixture, où un test construit un générateur et l'appelle.
Rejeté comme surface primaire parce qu'elle fait circuler l'état du générateur à
travers chaque test et diverge de l'idiome établi du package `Use*` / portée
disposable ; elle reste disponible comme ajout futur possible pour les appelants
qui veulent un objet explicite, mais ce n'est pas la forme que le package met en
avant.

### Ajouter des fabriques de test aux value objects eux-mêmes

Placer « fabrique-en une arbitraire » sur `ErrorCode`, `Error`, et consorts dans
la bibliothèque cœur. Rejeté parce que cela mêle une préoccupation de test à des
types de production et livrerait une surface réservée aux tests dans le package
principal.

## Conséquences

### Positives

* Une entrée incidente est lisible comme incidente sur le site d'appel, et une
  exécution en échec est reproductible une fois qu'une graine est fixée.
* Aucune nouvelle dépendance n'atteint les consommateurs, et la nouvelle surface
  réutilise le contrat existant du package — sûr en parallèle, à portée
  disposable — plutôt que d'en inventer un second.

### Négatives

* Une nouvelle surface publique sur un package livré, qui doit être maintenue et
  tenue documentée en anglais et en français.
* L'utilitaire d'énumération générique peut encore renvoyer une valeur
  sentinelle (telle que `Unknown`) ; les appelants qui ont besoin d'une valeur
  *significative* doivent utiliser les utilitaires dédiés par énumération qui
  l'excluent.
* `ErrorContextKey` est délibérément laissé en dehors de la première surface :
  les clés vivent dans un registre à l'échelle du processus sans réinitialisation
  publique, de sorte que forger des clés arbitraires accumulerait de l'état
  global au fil d'une exécution. Les tests nécessitant une clé arbitraire
  restent sans réponse tant que cela n'a pas été conçu.

### Risques

* Le défaut sans graine puise dans un générateur par contexte ; si deux
  contextes démarrent par coïncidence à partir de la même graine, leurs valeurs
  « arbitraires » coïncident. C'est inoffensif (les valeurs ne font pas l'objet
  d'assertions) et c'est atténué en dérivant la graine par défaut d'un
  identifiant neuf par contexte.
* Reproduire un échec exige que l'auteur ait enveloppé le test dans
  `Reproducibly`, et ne tient que tant que la séquence d'appels `Any` du corps
  est déterministe ; un test ordinaire non enveloppé qui échoue sur une valeur
  arbitraire n'est toujours pas rejouable à partir de sa seule sortie.
* Tant qu'elle n'est pas imposée par l'outillage, l'habitude « valeur
  arbitraire ⇒ utiliser la source, pas un littéral » ne tient que par la revue
  et la documentation.

## Actions de suivi

* Documenter la surface dans le guide de test, en anglais et en français, de
  manière synchronisée.
* Envisager une conception pour des valeurs `ErrorContextKey` arbitraires qui
  respecte le registre à l'échelle du processus.
* Envisager un adaptateur optionnel de framework de test (par exemple un
  `[ReproducibleFact]` xUnit) afin que la graine soit exposée automatiquement,
  sans envelopper chaque corps dans `Reproducibly`.
* Envisager un générateur basé sur instance si les appelants demandent un objet
  explicite.
* Envisager d'extraire le moteur de valeurs générique dans un utilitaire
  autonome et agnostique aux erreurs si un second consommateur apparaît ; il est
  maintenu séparable en interne de la surface spécifique aux erreurs à cette fin.

## Références

* `doc/handwritten/for-users/ArbitraryTestValues.en.md` — le guide où la nouvelle surface est
  documentée.
* ADR-0005 — décision de nommage antérieure dans le même esprit (un nom devrait
  annoncer ce que fait un appel) ; contexte seulement, pas un précédent pour ce
  choix.
