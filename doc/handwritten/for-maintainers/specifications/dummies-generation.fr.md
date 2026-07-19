# Spécification des contrats de génération de Dummies

🌍 🇬🇧 [English](dummies-generation.en.md) · 🇫🇷 Français (ce fichier)

Cette page décrit les mécanismes de génération courants derrière les
[ADR-0011](../adr/0011-host-dummies-as-a-standalone-package.fr.md),
[ADR-0013](../adr/0013-gate-distinct-collections-by-cardinality-else-bounded-draw.fr.md),
[ADR-0015](../adr/0015-cap-any-combine-at-arity-eight.fr.md) et
[ADR-0020](../adr/0020-materialize-dummies-only-through-generate.fr.md).

## Frontière du package

`Dummies` est un package `netstandard2.0` indépendant qui ne référence aucun
projet FirstClassErrors. Un test d'architecture garde cette frontière. Son contrat
public de générateur est `IAny<T>` et les valeurs sont matérialisées explicitement
par `Generate()`.

## Modèle de contraintes et de génération

* Les spécifications sont immuables ; appliquer une contrainte retourne une
  nouvelle spécification.
* Les contraintes contradictoires échouent à leur déclaration avec
  `ConflictingAnyConstraintException`.
* Une valeur satisfaisable est construite directement depuis la spécification
  complète ; la génération scalaire n'utilise ni generate-then-filter ni retry
  non borné.
* L'exécution reproductible passe par `Any.Reproducibly(seed, ...)` ; une erreur de
  génération porte la graine lorsqu'elle est active.

## Collections distinctes

La distinction est imposée en deux couches.

### Cardinalité connue

Les générateurs internes peuvent implémenter `ICardinalityHint` et annoncer une
borne supérieure du nombre de valeurs distinctes produisibles. Lorsque le minimum
ou le compte exact demandé dépasse cette borne, la déclaration échoue immédiatement.

Le hint ne fait volontairement pas partie d'`IAny<T>` public : des générateurs
étrangers ou dérivés peuvent être incapables d'annoncer une borne fiable.

### Cardinalité inconnue ou réduite par comparer

La génération déduplique dans un `HashSet<T>` avec le comparer configuré. Elle est
bornée par un budget de collisions et lève `AnyGenerationException` si elle
n'obtient pas assez de valeurs nouvelles.

Le budget courant dans `CollectionState<T>` est :

* `64 × cardinalité` lorsque la cardinalité connue ne dépasse pas 1 000 000 ;
* sinon `64 × nombre cible` ;
* avec un minimum de 10 000 et un maximum de `int.MaxValue`.

Pour le remplissage principal, le compteur de collisions est remis à zéro après
chaque valeur nouvelle. Un générateur `ContainingAny(...)` reçoit le même budget
pour trouver sa contribution distincte. L'épuisement rapporte le compte atteint,
le compte cible, la source des collisions et la graine rejouable lorsqu'elle existe.

Un tirage probabiliste borné peut échouer pour un générateur étranger satisfaisable
mais très biaisé. C'est une conséquence acceptée du support de générateurs dont le
domaine et la distribution sont inconnus ; elle n'est ni impossible ni qualifiée
d'« astronomiquement improbable ». Un faux épuisement réel doit conduire à
réexaminer le budget ou à exposer une capacité plus riche.

## Composition hétérogène

`Any.Combine` fournit des surcharges plates et sans réflexion pour deux à huit
générateurs. Chaque surcharge génère ses parties puis appelle le composeur fourni.
Les arités sept et huit dépassent la règle Sonar S107 car le composeur ajoute un
paramètre ; les suppressions locales sont intentionnelles et doivent conserver
leur justification.

Les arités hétérogènes supérieures ne font pas partie du contrat public. Les
ajouter serait non cassant mais exige de réexaminer le plafond architectural. Une
composition homogène par `params` reste une fonctionnalité indépendante possible.

## Sources de vérité

* `Dummies/CollectionState.cs` et `Dummies/ICardinalityHint.cs` — distinction,
  budget et cardinalité.
* `Dummies/Any.cs` — façade publique et plafond de `Combine`.
* Les tests unitaires et de propriétés de Dummies — conflits précoces,
  reproductibilité, comparers et arités.
* La documentation utilisateur Dummies — surface fluide prise en charge.
