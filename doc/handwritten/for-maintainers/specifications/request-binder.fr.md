# Spécification des contrats du Request Binder

🌍 🇬🇧 [English](request-binder.en.md) · 🇫🇷 Français (ce fichier)

Cette page décrit les mécanismes courants derrière les décisions du Request
Binder. Les exemples publics restent dans le
[guide du Request Binder](../../for-users/RequestBinder.fr.md).

## Entrée et durée de vie

* `Bind.Request(envelopeFactory)` crée un `RequestBinder` agnostique de la source
  et fixe l'enveloppe d'échec avant d'attacher le moindre input.
* `Bind.WithOptions(options)` crée un point d'entrée configuré réutilisable ; les
  options sont fixées avant le début de la liaison.
* L'entrée nue utilise `RequestBinderOptions.Default`, configurable une fois au
  démarrage puis gelé à sa première lecture de production.
* Un DTO est attaché avec `PropertiesOf(request)` et les valeurs hors DTO avec
  `Argument(...)` ; les deux alimentent le même binder et la même enveloppe.
* Le type de commande n'est inféré qu'au terminal `New` ou `Create`.

## Sources d'inputs

### Propriétés du DTO

Un `PropertySource<TRequest>` sélectionne des propriétés simples, listes ou
complexes. Les chemins sont dérivés par l'`IArgumentNameProvider` configuré, puis
les chemins imbriqués sont composés par le binder.

### Arguments hors DTO

Un argument est une valeur déjà extraite par l'hôte. L'appelant fournit :

* le nom filaire, utilisé tel quel comme chemin d'erreur ;
* sa provenance, normalement via `FromRoute`, `FromQuery` ou la forme générale
  `From` ;
* la valeur brute et la même forme de convertisseur que pour une propriété.

Le binder n'inspecte ni requête HTTP ni objet de framework. La provenance est
stockée séparément du chemin dans un contexte d'erreur typé. Il n'existe pas de
concept distinct d'« argument complexe » : ses constituants sont liés comme
arguments pairs, puis la valeur complexe est assemblée au terminal.

## Sélecteurs de types valeur nullables

Une propriété DTO de type valeur nullable est sélectionnée par une surcharge
dédiée `where TArgument : struct` dont l'expression porte
`Nullable<TArgument>`. Le convertisseur reçoit la valeur sous-jacente non
nullable. L'ergonomie des method groups reste ainsi identique à celle des types
référence, tout en conservant `null` comme signal d'absence.

Les listes de types valeur nullables utilisent le chemin dédié correspondant. Un
élément `null` est enregistré comme argument manquant à son index ; un élément
présent est déballé avant conversion. Les propriétés DTO de type valeur non
nullable restent une erreur de programmation, car l'absence ne peut pas être
distinguée de `default(T)`.

## Modèle de résultat

Les convertisseurs retournent des tokens de champ. Les valeurs ne sont lisibles
qu'à travers le scope fourni à `New` ou `Create`, après vérification qu'aucun échec
d'input n'a été enregistré.

* `New` exécute un constructeur total et encapsule la valeur dans un succès.
* `Create` exécute une factory validante retournant `Outcome<T>` et aplatit le résultat.
* Les inputs absents ou invalides sont collectés dans l'ordre de déclaration sous
  l'unique enveloppe structurelle ; le chemin d'input invalide ne lève pas.

## Erreurs structurelles et propriété du catalogue

Les définitions « argument requis » et « argument invalide » sont regroupées en
`BinderErrorDefinition` dans `RequestBinderOptions`, code et messages publics
compris.

Lorsqu'un consommateur surcharge une définition, il possède le code effectivement
émis et le documente dans son propre type `[ProvidesErrorsFor]`. Les seams publics
du binder fournissent :

* le texte indépendant du code pour l'erreur structurelle ;
* un exemple construit depuis la définition effective du consommateur avec la
  même forme que l'erreur runtime.

GenDoc continue de scanner les projets explicitement inclus du consommateur ; il
ne traverse pas les catalogues des packages référencés et n'infère pas la
configuration runtime.

## Sources de vérité

* `FirstClassErrors.RequestBinder/Bind.cs` et les types source/convertisseur —
  forme de l'API publique.
* `RequestBinderOptions` et `BinderErrorDefinition` — configuration et erreurs
  structurelles.
* Les tests unitaires et de propriétés — résolution de surcharge, chemins, ordre
  des collections, gel des options et collect-all.
* Le [guide du Request Binder](../../for-users/RequestBinder.fr.md) — usages et
  exemples publics pris en charge.

Un changement de modèle de source, de durée de vie des options, de propriété des
codes structurels ou de sémantique des terminaux exige un ADR. Un refactoring qui
préserve ces contrats ne modifie que cette spécification et les tests.
