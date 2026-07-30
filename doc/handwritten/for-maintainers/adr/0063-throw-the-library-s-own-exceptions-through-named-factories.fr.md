# ADR-0063 | Lever les exceptions de la bibliothèque via des factories nommées

🌍 🇫🇷 Français (ce fichier) · 🇬🇧 [English](0063-throw-the-library-s-own-exceptions-through-named-factories.md)

**Statut :** Accepté
**Proposé :** 2026-07-30
**Accepté :** 2026-07-30
**Décideurs :** Reefact

## Contexte

JustDummies refuse les contradictions au moment de la déclaration, et dit pourquoi. Cette promesse
tient dans un message : `Cannot apply WithLength(3) because StartingWith("ORD-") already requires at
least 4 characters.` Ces messages sont bons, et ils étaient assemblés là où ils étaient levés — au
milieu du code qui décide.

Résultat : une méthode qui parle de contraintes consacre quatre lignes à de la prose. Dans les specs
d'intervalle, une boucle de tirage se lisait ainsi :

```csharp
throw new AnyGenerationException(
    $"Generation failed: no {_typeName} value near the drawn candidate satisfies the exclusions. {source.ReplayGuidance(random.Seed)}",
    random.Seed,
    new InvalidOperationException($"Every representable value within {NudgeBudget.ToString(CultureInfo.InvariantCulture)} steps of the drawn candidate, in both directions, is excluded or out of bounds. Values further away were not examined, so this is an exhausted local search rather than an empty range."));
```

Quatre lignes que le lecteur doit enjamber pour suivre l'algorithme, dont aucune ne parle de tirer
une valeur. Et quand le même échec est rapporté depuis plusieurs endroits, la formulation est
retapée : la phrase `Cannot apply X because Y.` était écrite à **84 sites de levée** dans la
bibliothèque.

La duplication est le symptôme visible, et ce n'est pas la raison de cette décision. Un message
assemblé une seule fois reste de la prose au milieu de la logique.

## Décision

Toute levée d'une exception que cette bibliothèque déclare passe par une factory statique sur cette
exception, nommée d'après l'échec qu'elle rapporte — que le message se répète ou non, `internal` sauf
si un consommateur doit construire l'exception, ne gardant rien parce que construire une exception ne
doit jamais lever, et regroupant ses arguments en value object quand nommer le cas demanderait plus
de paramètres épars qu'un lecteur ne peut en tenir dans l'ordre — tandis que les exceptions `System`,
que la bibliothèque ne possède pas, gardent la forme de clause de garde qu'ADR-0045 leur impose.

## Justification

**Le code métier reste du code métier.** `WithMinimum` parle de resserrer une borne. Qu'une
contradiction produise telle phrase anglaise est de la plomberie, et la plomberie va avec le
mécanisme. Un type d'exception *est* un mécanisme avant tout, ce qui en fait le bon domicile : le
site d'appel énonce quel échec s'est produit, et l'exception sait le dire.

**Un nom vaut mieux qu'un message au site d'appel.** `throw AnyGenerationException.GridNudgeExhausted(...)`
dit à un lecteur ce qui s'est passé en trois mots. Le message qu'elle produit le dit à
l'*utilisateur*, ce qui est un autre public et un autre moment. Les séparer permet aux deux d'être
bons.

**La règle est peu coûteuse à suivre et à vérifier.** « Ce fichier contient-il un `throw new` d'une
de nos exceptions ? » est une question à réponse binaire, et c'est ce qui fait tenir une convention.
Une règle nuancée par « quand le message se répète » demanderait un jugement à chaque site et
dériverait, comme le montrent les 84 phrases écrites à la main.

**L'uniformité est le but, pas l'économie.** Dix factories pour dix sites utilisés une fois chacun
n'est pas du gaspillage : ce sont dix sites qui se lisent comme des constats plutôt que comme de
l'assemblage de chaînes.

## Alternatives considérées

### Des factories seulement là où un message se répète

La première version de ce travail appliquait ce critère, et il est faux dans les deux sens. Il laisse
les échecs uniques assembler de la prose en ligne — précisément le cas que la boucle de tirage des
specs d'intervalle montrait au pire — et il rend la règle invérifiable, puisque « se répète » est une
propriété de toute la bibliothèque, pas du site qu'on écrit.

### Une factory générique prenant une raison libre

Essayée, sous la forme `Because(applying, reason)`. Elle centralise la phrase et rien d'autre :
l'appelant compose encore la raison, donc le site d'appel ne dit toujours rien de l'échec. Pire,
c'est une porte de sortie : tant qu'elle existe, aucun cas futur n'a besoin d'un nom. Rejetée — et
les quatre sites qui l'utilisaient se sont révélés être un seul cas nommable.

### Garder les arguments des factories

Envisagé puis implémenté brièvement, avant retrait. Cela contredit ADR-0045, dont la convention par
réflexion exclut les types d'exception avant même de regarder l'accessibilité — les gardes n'étaient
donc jamais exercées, et une suite verte ne disait rien à leur sujet.

### Adopter le modèle d'erreur de FirstClassErrors

Le voisin évident : FirstClassErrors modélise déjà les erreurs comme des valeurs de première classe,
avec codes, contexte et documentation générée. Rejeté au nom de la frontière que consigne ADR-0011 :
JustDummies ne doit référencer aucun projet FirstClassErrors, et est délibérément *error-agnostic*,
parce qu'elle est référencée par les projets de tests de ses consommateurs et ne doit pas leur
imposer un modèle d'erreur. Ce qui traverse cette frontière, c'est la discipline, pas les types. Les
codes d'erreur sont déclinés avec : ces échecs sont lus une fois par un développeur qui corrige son
test, et un code stable et documenté aurait un coût documentaire qu'aucun de ces lecteurs ne
percevrait.

## Conséquences

### Positives

* Le site d'appel énonce quel échec s'est produit, et rien d'autre : une méthode qui parle de
  contraintes se lit comme telle. Le message qu'elle produit s'adresse à un autre lecteur, à un autre
  moment ; les séparer permet aux deux d'être bons.
* La formulation d'un échec a un seul domicile. `ConflictingAnyConstraintException` porte la forme de
  phrase de tous les conflits, ce qui en fait le seul fichier à lire quand un message doit changer.
* La règle se vérifie à l'œil : « ce fichier lève-t-il une de nos exceptions avec `new` ? » a une
  réponse binaire, et c'est ce qui fait tenir une convention.

### Négatives

* Un nouvel échec exige une factory avant de pouvoir être levé. Cette friction est voulue — nommer le
  cas est l'étape de conception — mais c'est une friction.
* Les types d'exception grossissent, et qui cherche un message doit aller à l'exception plutôt qu'au
  code qui la lève.
* Convertir les sites existants touche la majeure partie de la bibliothèque, une tranche
  fonctionnelle à la fois.

### Risques

* Une factory nommée d'après une *forme de phrase* plutôt que d'après un échec satisferait la lettre
  de cette règle en la vidant ; la première tentative a fait exactement cela et a dû être défaite. Le
  test est de savoir si le site d'appel se lit comme un constat, sans ses arguments.
* Les messages sont du comportement observable. Les suites unitaires en assertent le contenu, donc
  une conversion qui en altérerait un échouerait — la parade étant que les suites restent vertes à
  chaque tranche, pas seulement à la fin.

## Références

* [ADR-0011](0011-host-dummies-as-a-standalone-package.fr.md) — JustDummies est autonome et
  error-agnostic ; elle ne doit référencer aucun projet FirstClassErrors.
* [ADR-0045](0045-guard-public-and-internal-arguments-against-null.fr.md) — les gardes d'arguments,
  et l'exemption des types d'exception sur laquelle cette décision s'appuie.
* [ADR-0046](0046-make-the-per-pull-request-mutation-gate-advisory.fr.md) — le check de mutation par
  PR est consultatif.
* [ADR-0049](0049-drop-the-justdummies-generator-from-the-per-pull-request-mutation-matrix.fr.md) —
  consigne que le `--since` de Stryker sélectionne par fichier, et retire le générateur JustDummies
  de la matrice par pull request à cause de ce que cela coûte.
