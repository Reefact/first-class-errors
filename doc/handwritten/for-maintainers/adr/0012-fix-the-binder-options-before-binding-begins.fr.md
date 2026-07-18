# ADR-0012 | Fixer les options du binder avant le début de la liaison

🌍 🇬🇧 [English](0012-fix-the-binder-options-before-binding-begins.md) · 🇫🇷 Français (ce fichier)

**Statut :** Proposé
**Date :** 2026-07-18
**Décideurs :** Reefact

## Contexte

* Le binder résout le chemin d'argument de chaque propriété liée — la clé rapportée dans
  les chemins d'erreur, comme `GuestEmail` ou `Stay.CheckIn` — via l'`IArgumentNameProvider`
  porté par `RequestBinderOptions`.
* Avant cette décision, les options étaient posées par une méthode d'instance `WithOptions`
  sur `RequestBinder<TRequest>`, appelable à n'importe quel point de la chaîne fluide, y
  compris après que certaines propriétés aient déjà été liées.
* Chaque liaison de propriété lit les options en vigueur au moment où elle est liée : changer
  le provider entre deux liaisons produit donc des chemins d'argument sous deux politiques de
  nommage différentes dans une même enveloppe d'échec — par exemple `GuestEmail` à côté de
  `guest_email`.
* Le binder collecte chaque échec dans une seule enveloppe ; un client lit les chemins
  d'argument pour les remapper vers les clés qu'il a envoyées, et compte sur une politique de
  nommage cohérente sur toute l'enveloppe.
* Le binder trace déjà une frontière nette entre une erreur client (enregistrée, surfacée une
  fois) et une erreur de programmation (levée), et traite un mauvais usage de son API comme
  une erreur de programmation surfacée bruyamment plutôt qu'une incohérence silencieuse.
* La `WithOptions` d'instance était documentée « appelez-la avant de lier la moindre
  propriété », mais rien ne l'imposait : l'ordre était une convention en prose, pas une
  garantie à la compilation ni à l'exécution.
* La bibliothèque n'expose aucun état ambiant mutable ailleurs : les points d'extension de
  l'horloge, de l'identifiant d'instance et des valeurs arbitraires sont tous un défaut
  immuable plus un override `AsyncLocal`, scoped, réservé aux tests (ADR-0006), et le cœur
  n'expose délibérément aucun état global mutable.
* `RequestBinderOptions` ne porte aucun état par requête : le provider mappe un `PropertyInfo`
  vers un nom et ne dépend de rien de l'instance de requête.
* La bibliothèque est en pré-version, non publiée sur NuGet et sans consommateur externe :
  déplacer l'endroit où les options sont posées n'entraîne donc aucun coût de migration en
  aval.

## Décision

Les options du binder sont fixées une seule fois, au point d'entrée —
`Bind.WithOptions(options).PropertiesOf(request)` — avant que la moindre propriété ne soit
liée, et la possibilité de les changer une fois la liaison commencée est retirée.

## Justification

* Fixer les options avant que le binder n'existe rend une enveloppe incohérente impossible à
  écrire plutôt que simplement déconseillée : une fois la liaison commencée, il n'existe aucun
  point de la chaîne fluide où la politique de nommage puisse être échangée, donc l'enveloppe
  à deux politiques décrite dans le Contexte ne peut pas survenir. Cela ferme le défaut au
  niveau de la forme de l'API, dans l'esprit du canal d'erreur de programmation déjà présent
  mais d'un cran plus fort — l'erreur est non-compilable, pas levée.
* Placer les options avant `PropertiesOf` plutôt qu'entre `PropertiesOf` et `FailWith` les
  garde indépendantes du type de requête : une politique de nommage concerne comment une
  propriété est nommée, pas quelle requête est liée, donc elle n'a pas à — et désormais ne —
  dépend du `TRequest`, ce qui permet aussi de réutiliser le point d'entrée configuré d'une
  requête à l'autre.
* Garder les options comme un argument explicite passé au point d'entrée, plutôt qu'un défaut
  ambiant que le binder lit, est cohérent avec la position établie de la bibliothèque : une
  vraie dépendance de production se passe explicitement et le cœur n'expose aucun état global
  mutable (ADR-0006). Une politique de nommage est un vrai choix de production à variation
  légitime : c'est donc une dépendance explicite, pas une configuration ambiante.
* Parce que les options ne portent aucun état par requête, les fixer à un point d'entrée
  réutilisable laisse une application configurer la politique une fois — par exemple au
  démarrage — et la réutiliser pour chaque requête sans la faire transiter par chaque liaison :
  l'ergonomie que visait le setter d'instance retiré, désormais sans le piège de l'ordre.
* Le statut de pré-version signifie que la forme de l'API est arrêtée maintenant, quand il n'y
  a aucun consommateur à migrer.

## Alternatives considérées

### Verrouiller les options à l'exécution dès la première liaison

Considérée parce qu'elle garde la `WithOptions` d'instance et n'ajoute qu'une garde : un appel
tardif, une fois une propriété liée, lève — cohérent avec le canal d'erreur de programmation
du binder.

Rejetée parce qu'elle détecte le mauvais usage au lieu de l'empêcher : le code de l'enveloppe
incohérente compile toujours et n'échoue qu'à l'exécution, et les options dépendent encore
inutilement du type de requête. Déplacer le setter avant `PropertiesOf` rend la même erreur
non-représentable sans coût supplémentaire.

### Un défaut ambiant à l'échelle du processus, configuré une fois (un `Configure` statique)

Considérée parce qu'elle laisserait `Bind.PropertiesOf(request)` ramasser une politique à
l'échelle de l'application sans rien faire transiter par les points d'appel.

Rejetée parce qu'elle introduirait le premier état global mutable de la bibliothèque,
contredisant la position établie « aucun état ambiant mutable » (ADR-0006) : il fuirait entre
les tests exécutés en parallèle et réintroduirait un piège « configuré au mauvais moment » qui
lui est propre. L'ergonomie de configuration applicative appartient à la future intégration
ASP.NET Core, via l'injection de dépendances, où elle est sûre vis-à-vis des tests par
construction.

### Garder le setter d'instance et documenter l'ordre plus fermement

Considérée parce que c'est le plus petit changement.

Rejetée parce qu'un « appelez avant de lier » en prose est exactement la convention non imposée
qui a produit le défaut ; la documentation ne peut pas rendre l'enveloppe incohérente
non-représentable.

## Conséquences

### Positives

* Une seule enveloppe d'échec rapporte toujours les chemins d'argument sous une seule politique
  de nommage ; l'incohérence à deux politiques est impossible à écrire.
* Les options ne dépendent plus du type de requête, et le point d'entrée configuré est
  réutilisable d'une requête à l'autre — une politique configurée une fois, réutilisée par
  requête.
* Le binder préserve intacte la propriété « aucun état global mutable » de la bibliothèque.

### Négatives

* La chaîne fluide gagne un point d'entrée distinct
  (`Bind.WithOptions(...).PropertiesOf(...)`) à côté du défaut `Bind.PropertiesOf(...)`, et un
  nouveau type public `ConfiguredBind` à documenter.
* Un consommateur qui posait les options après `PropertiesOf` / `FailWith` doit déplacer
  l'appel avant `PropertiesOf` — un changement de source, atténué par le statut de pré-version
  (aucun consommateur externe).

### Risques

* Un besoin futur de faire varier les options par binder imbriqué ne cadrerait pas avec le
  modèle « fixé au point d'entrée » ; atténué par le fait que les binders imbriqués héritent
  des options du parent par conception, et que cela est hors des exigences actuelles.

## Actions de suivi

* Documenter l'ergonomie de configuration applicative (injection de dépendances) lors de la
  construction de l'intégration ASP.NET Core, plutôt que d'ajouter une configuration ambiante
  au cœur.

## Références

* ADR-0006 — fournir les valeurs de test arbitraires depuis une source unique réamorçable ; la
  position « aucun état ambiant mutable » que cette décision préserve.
* ADR-0007 — nommer les terminaux du binder New et Create ; une décision d'API publique sœur
  sur le même binder.
* Issue #145 — le constat que cette décision résout.
* Pull request #126 — la fonctionnalité de request binder à laquelle ces options appartiennent.
