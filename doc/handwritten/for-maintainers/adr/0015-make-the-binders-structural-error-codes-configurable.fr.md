# ADR-0015 | Rendre les codes d'erreur structurels du binder configurables

🌍 🇬🇧 [English](0015-make-the-binders-structural-error-codes-configurable.md) · 🇫🇷 Français (ce fichier)

**Statut :** Proposé
**Date :** 2026-07-18
**Décideurs :** Reefact

## Contexte

* Le binder fabrique exactement deux erreurs structurelles qui lui sont propres : un
  argument requis absent (`REQUEST_ARGUMENT_REQUIRED`) et un argument présent mais
  invalide (`REQUEST_ARGUMENT_INVALID`). Toute autre erreur d'un arbre d'échec vient de
  l'application — les erreurs des convertisseurs et l'enveloppe.
* Ces deux erreurs remontent dans l'enveloppe d'échec du consommateur, et donc dans sa
  surface d'erreurs et son catalogue généré.
* Avant cette décision, les deux codes étaient des constantes `private` en dur et les
  factories étaient `internal` : `RequestBindingError` n'exposait donc aucun membre
  référençable. Un consommateur devant brancher sur un code structurel — pour le mapper
  vers un statut HTTP, par exemple — ne pouvait comparer qu'une chaîne littérale, ce que
  le modèle d'erreurs codées de la bibliothèque existe précisément pour éviter.
* Une application donne couramment à tous ses codes d'erreur une convention unique (un
  préfixe ou un schéma partagé) ; deux codes d'apparence étrangère injectés par le binder
  brisent cette convention.
* `RequestBinderOptions` est la configuration unique, immuable, du binder, au point
  d'entrée, fixée une fois avant le début de la liaison (ADR-0012) et héritée par les
  binders imbriqués.
* `ErrorCode` a une égalité de valeur : un consommateur peut donc brancher sur un code
  qu'il détient symboliquement plutôt que par chaîne.
* Le catalogue généré du paquet binder lui-même est produit statiquement à partir des
  factories documentées, qui décrivent les codes par défaut.
* La bibliothèque est en pré-version, non publiée sur NuGet et sans consommateur externe :
  déplacer l'endroit où les codes sont définis n'entraîne aucun coût de migration en aval.

## Décision

Les deux codes d'erreur structurels du binder sont portés par `RequestBinderOptions`, par
défaut `REQUEST_ARGUMENT_REQUIRED` / `REQUEST_ARGUMENT_INVALID` (dont les défauts sont
exposés publiquement), pour qu'un consommateur les surcharge une seule fois au point
d'entrée et que chaque échec structurel — y compris celui d'un binder imbriqué — utilise
les codes configurés.

## Justification

* Porter les codes sur le sac d'options laisse un consommateur les aligner avec la
  convention de son catalogue, réglant l'incohérence qu'un préfixe étranger figé
  créerait — et cela réutilise exactement le mécanisme déjà employé par la politique de
  nommage (options, fixées une fois au point d'entrée, héritées par les binders
  imbriqués, ADR-0012) : il y a donc un seul endroit et une seule durée de vie pour toute
  la configuration du binder.
* Surcharger les codes résout le problème du branchement par *appropriation* plutôt que
  par simple exposition : un consommateur qui définit les codes détient les symboles
  `ErrorCode` sur lesquels il branche, donc il ne compare jamais une chaîne ; un
  consommateur qui garde les défauts branche sur les défauts exposés publiquement. Dans
  les deux cas, la promesse du modèle d'erreurs codées — référencer les codes
  symboliquement — est enfin tenue aussi pour les erreurs propres au binder.
* Garder les factories `internal` tout en n'exposant que les codes correspond à la façon
  dont un consommateur rencontre ces erreurs : il les *reconnaît* (il a besoin du code)
  mais ne les *fabrique* pas (c'est le binder), donc la surface de fabrication reste
  fermée.
* Choisir les codes actuels par défaut et les exposer publiquement laisse le comportement
  zéro-configuration inchangé et le catalogue propre du paquet binder exact, puisque les
  factories documentées décrivent toujours ces défauts.
* Le statut de pré-version signifie que la surface des options est arrêtée maintenant,
  quand il n'y a aucun consommateur à migrer.

## Alternatives considérées

### Exposer les deux codes en lecture seule, sans surcharge

Considérée parce que c'est le plus petit changement qui tue le branchement par chaîne
magique : un consommateur référence des constantes `ErrorCode` publiques au lieu de
littéraux.

Rejetée parce qu'elle laisse l'incohérence plus profonde décrite dans le Contexte — le
binder impose toujours deux codes préfixés étrangers au catalogue du consommateur. Lire
les codes laisse un consommateur les reconnaître ; cela ne lui laisse pas les faire
cadrer.

### Des prédicats (`IsRequestArgumentRequired` / `IsRequestArgumentInvalid`)

Considérés parce qu'ils se lisent par l'intention et encapsulent la comparaison.

Rejetés comme moins composables et toujours non-surchargeables : un booléen ne peut pas
indexer une table code-vers-statut, et il ne fait rien pour la cohérence du catalogue. Des
`ErrorCode` surchargeables sur les options couvrent les deux besoins, et un prédicat peut
toujours se poser par-dessus plus tard.

### Une abstraction de politique de codes (un `IBindingCodeProvider` calqué sur `IArgumentNameProvider`)

Considérée pour la symétrie avec la politique de nom d'argument.

Rejetée comme surface inutile : deux propriétés `ErrorCode` expriment la même intention,
et un préfixe est un `ErrorCode.Create` qu'un consommateur écrit une fois ; une interface
ajouterait un type sans ajouter de capacité.

## Conséquences

### Positives

* Les codes structurels du binder peuvent être alignés avec la convention du catalogue du
  consommateur, et on branche dessus symboliquement (codes possédés, ou défauts publics) —
  fermant les deux moitiés du constat.
* Toute la configuration du binder vit dans un seul objet d'options avec une seule durée
  de vie (ADR-0012) ; les codes sont hérités par les binders imbriqués exactement comme la
  politique de nommage.
* Le comportement zéro-configuration et le catalogue propre du paquet binder sont
  inchangés (les défauts sont préservés et toujours documentés).

### Négatives

* `RequestBinderOptions` gagne deux propriétés et deux paramètres de constructeur
  optionnels, et `RequestBindingError` gagne deux membres publics de code par défaut à
  documenter.
* La **documentation** d'un code surchargé est une préoccupation distincte : le catalogue
  du paquet binder documente les défauts, donc un consommateur qui surcharge les codes
  doit faire apparaître les codes effectifs dans son propre catalogue.

### Risques

* Un consommateur qui surcharge les codes à l'exécution mais documente les défauts
  dériverait entre le code émis et le code documenté ; atténué seulement en partie ici
  (les défauts restent documentés) et reporté à la décision distincte de liaison des
  catalogues.

## Actions de suivi

* Décider, séparément, comment le catalogue généré d'un consommateur fait apparaître les
  codes — éventuellement surchargés — du binder sans dériver de l'exécution (en lien avec
  #140).

## Références

* ADR-0012 — fixer les options du binder avant le début de la liaison ; la durée de vie
  des options que cette décision réutilise.
* ADR-0006 — fournir les valeurs de test arbitraires depuis une source unique
  réamorçable ; la position « aucun état ambiant mutable » que l'approche par options
  respecte.
* Issue #147 — le constat que cette décision résout.
* Issue #140 — faire apparaître les codes du binder dans le catalogue généré d'un
  consommateur (la moitié documentation reportée).
