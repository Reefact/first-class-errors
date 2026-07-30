# ADR-0064 | Exempter tout le chemin de report d'échec de la convention de garde null

🌍 🇫🇷 Français (ce fichier) · 🇬🇧 [English](0064-exempt-the-whole-failure-reporting-path-from-the-null-guard-convention.md)

**Statut :** Accepté
**Proposé :** 2026-07-30
**Accepté :** 2026-07-30
**Décideurs :** Reefact

Remplace [ADR-0045](0045-guard-public-and-internal-arguments-against-null.fr.md).

## Contexte

ADR-0045 impose à tout membre public et interne de refuser un argument référence non-nullable `null`
par une `ArgumentNullException`, et l'applique via une convention par réflexion dans
`JustDummies.UnitTests` qui découvre les membres au lieu de les nommer. Elle exemptait les types
d'exception, pour une raison qui mérite d'être répétée : leurs constructeurs s'exécutent pendant
qu'une erreur est traitée, donc y lever une `ArgumentNullException` remplacerait l'échec rapporté par
un échec sur le fait de le rapporter, et l'original serait perdu.

Cette exemption est indexée sur le fait d'**être** une `Exception`. Le danger, lui, ne l'est pas.

ADR-0063 a fait passer les levées par des factories nommées d'après l'échec, et l'une d'elles devait
dire laquelle de deux contraintes un conflit doit blâmer. Cinq chaînes éparses dans un ordre que rien
ne vérifie était la mauvaise signature ; la paire — une contrainte et ce qu'elle affirme — est donc
devenue un petit type, `ConstraintClaim`. Il est construit au site de levée, en argument de la
factory :

```csharp
throw ConflictingAnyConstraintException.Contradicts(applying,
                                                    ConstraintClaim.Of(_exactConstraint!, $"already fixes the count at {V(exact)}"),
                                                    ConstraintClaim.Of(_minConstraint!,   $"already requires at least {Elements(_min)}"));
```

`ConstraintClaim` n'est pas une `Exception` : la convention l'a donc inspecté et a fait échouer la
construction tant qu'il ne gardait pas ses arguments. Ajouter ces gardes a satisfait la convention et
recréé exactement ce qu'ADR-0045 interdit, une trame d'appel plus tôt : un `null` surgirait
désormais en `ArgumentNullException` depuis un helper, au lieu du conflit que le code rapportait.

La convention avait raison : la règle telle qu'écrite s'appliquait. La règle telle qu'écrite était
tracée une trame trop étroite.

## Décision

La règle d'ADR-0045 tient intégralement, son exemption étant élargie des types d'exception à tout type
qui n'existe que pour construire une exception de la bibliothèque, déclaré par un marqueur interne que
la convention par réflexion lit, plutôt qu'inféré depuis l'usage.

## Justification

**Le danger appartient au chemin, pas au type de base.** Ce qui rend une garde nuisible ici, c'est
*quand* elle s'exécute — pendant qu'un échec est rapporté — et cela tient à l'usage du type, non à ce
dont il hérite. Une règle indexée sur `: Exception` attrape le cas courant et rate le reste ; et le
reste est précisément ce que créent les factories d'ADR-0063.

**Un marqueur garde l'exemption honnête.** L'alternative est l'inférence, et l'inférence devrait
deviner : « n'est utilisé que par des factories d'exception » n'est pas une propriété qu'un test par
réflexion peut établir, et toute approximation raterait des cas ou exempterait en silence des types
qui devraient être gardés. Un marqueur tient en une ligne, se cherche au grep, et n'est faux que si
quelqu'un l'écrit à tort — ce qu'un relecteur voit.

**On n'abandonne rien en pratique.** Tous les sites d'appel de ce chemin passent des valeurs que le
compilateur a prouvées non-nulles. La garde défendait contre un cas que le compilateur refuse déjà,
au prix de masquer de vrais échecs si elle se déclenchait.

## Alternatives considérées

### Garder les gardes sur les types helpers

Ce que le code faisait avant cet ADR, et ce que la convention imposait. Cela recrée le masquage
qu'ADR-0045 existe pour empêcher, à une trame de là où ADR-0045 l'interdit. Rejeté sur le fond, pas
par confort : la garde n'est pas seulement redondante, elle est nuisible dans la seule circonstance
où elle s'exécuterait.

### Inférer l'exemption depuis l'usage

« Exempter un type dont tous les appelants sont des factories d'exception » sonne rigoureux et n'est
pas implémentable depuis les métadonnées de réflexion, qui voient des signatures et non des graphes
d'appel. Tout substitut — nommage, espace de noms, assignabilité — serait une supposition, et une
supposition qui retire silencieusement une garde vaut moins que pas de règle.

### Rendre le type helper privé à l'exception

Un type imbriqué privé est déjà hors du périmètre de la convention : aucun ADR n'aurait été
nécessaire. Mais alors le site de levée ne peut plus en construire, et la factory revient à cinq
chaînes éparses dans un ordre que rien ne vérifie — la signature qu'ADR-0063 a rejetée. L'exemption
existe pour que le site d'appel reste lisible.

### Replier la paire dans le message au site d'appel

Composer la phrase au site de levée supprime le type et la question avec, et réinstalle la prose au
milieu du code métier qu'ADR-0063 a été écrit pour faire cesser. Rejeté là-bas, rejeté ici.

## Conséquences

### Positives

* Le danger qu'ADR-0045 avait identifié est désormais couvert partout où il se produit, au lieu de
  partout où un type hérite d'`Exception`.
* Un site de levée peut construire les arguments qui rendent son message lisible sans que le type
  helper réintroduise le masquage une trame d'appel plus tôt.
* L'exemption se cherche au grep. Un marqueur, une raison écrite dessus, et un relecteur voit tous les
  types qui s'en réclament.

### Négatives

* L'exemption devient quelque chose qu'un contributeur applique, là où elle découlait du seul système
  de types. Elle coûte une décision au moment d'écrire le type.
* L'exigence de garde, qui ne souffrait aucune exception hors des types d'exception, en souffre
  désormais une de plus — une règle à deux exemptions s'énonce un peu moins simplement qu'à une.

### Risques

* Un marqueur posé sur un type qui n'est *pas* confiné au chemin d'échec retirerait silencieusement
  une vraie exigence de garde, et aucun test ne peut l'attraper : le marqueur est cru sur parole.
  Parades : il est `internal`, il ne vise que classes et structures, et sa raison est écrite sur
  l'attribut, si bien qu'un relecteur rencontre l'argument avant l'usage.
* Le compilateur porte désormais ce que portait la garde. C'est plus fort pour les appelants internes,
  mais cela signifie qu'un appelant par réflexion, ou qui force un `null!`, atteindrait le
  constructeur sans contrôle — compromis accepté, puisqu'un tel appelant a déjà quitté le contrat.

## Références

* [ADR-0045](0045-guard-public-and-internal-arguments-against-null.fr.md) — la règle que celui-ci
  remplace et reprend, et l'exemption qu'il élargit.
* [ADR-0063](0063-throw-the-library-s-own-exceptions-through-named-factories.fr.md) — les factories
  nommées dont les value objects ont rendu l'exemption étroite insuffisante.
