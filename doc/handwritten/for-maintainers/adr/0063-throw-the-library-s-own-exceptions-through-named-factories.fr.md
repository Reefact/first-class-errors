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

**Toute levée d'une exception appartenant à cette bibliothèque passe par une factory statique sur
cette exception, nommée d'après l'échec qu'elle rapporte.**

* La factory nomme un **cas**, pas une forme de phrase : `NoValueSatisfies`, `NoValueRemains`,
  `AlreadyDefined`, `GridNudgeExhausted`. Une méthode qui nomme la *grammaire* du message
  (`Because(applying, reason)`, prenant une raison libre) n'est pas une factory et ne convient pas :
  c'est le constructeur avec un préfixe, et ses sites d'appel ne disent rien.
* La règle vaut **que le message se répète ou non**. Un échec rapporté d'un seul endroit, une seule
  fois, et pour toujours, reçoit quand même un nom.
* Les factories sont `internal` sauf si un consommateur doit construire l'exception. Le type garde
  ses constructeurs publics ; rien de la surface publique ne change.
* Quand plusieurs factories partagent une phrase, un helper **privé** peut la posséder, pour que sa
  forme existe à un seul endroit. Privé parce qu'il nomme une grammaire : tout appelant doit être un
  cas nommé.
* **Rien sur le chemin de construction ne garde ses arguments.** ADR-0045 exempte déjà les types
  d'exception et la convention par réflexion les ignore, pour la raison qui y est donnée : construire
  une exception ne doit jamais lever, sinon l'échec rapporté est remplacé par un échec sur le fait de
  le rapporter. Des paramètres non-nullables confient le contrat au compilateur.
* Quand nommer le cas demanderait plus d'arguments épars qu'un lecteur ne peut en tenir dans l'ordre,
  ceux qui vont ensemble deviennent un **value object** — une classe, immuable, et sur ce chemin
  non validante pour la raison ci-dessus. `ConstraintClaim` (une contrainte et ce qu'elle affirme)
  est le premier.

**Cela ne concerne que les exceptions déclarées par cette bibliothèque** — la hiérarchie
`DummyException` : `ConflictingAnyConstraintException`, `AnyGenerationException`,
`UnsupportedRegexException`. Cela ne s'applique **pas** aux exceptions `System`, ni à aucun type que
la bibliothèque ne possède pas. `ArgumentNullException`, `ArgumentException` et
`ArgumentOutOfRangeException` gardent leur forme de clause de garde
(`if (x is null) { throw new ArgumentNullException(nameof(x)); }`), qu'ADR-0045 impose et qu'aucune
factory n'améliorerait — et qu'on ne pourrait de toute façon pas greffer sur un type qu'on ne
déclare pas.

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

* Un nouvel échec exige une factory avant de pouvoir être levé. C'est la friction voulue : nommer le
  cas est l'étape de conception, et elle précède l'écriture du message.
* Les types d'exception grossissent. `ConflictingAnyConstraintException` porte la forme de phrase de
  tous les conflits de la bibliothèque, ce qui en fait le fichier à lire quand un message doit
  changer — et le seul.
* Les messages sont du comportement observable et les suites unitaires en assertent le contenu : la
  conversion est donc vérifiable, une suite verte étant la garantie au octet près qu'aucune
  formulation n'a bougé.
* Convertir les sites existants touche la majeure partie de la bibliothèque. Cela se fait par
  **tranches fonctionnelles** — les specs d'intervalle, les specs de taille, les générateurs `Any*`,
  les specs de collection et d'URI, le moteur de motifs — choisies pour que chaque pull request soit
  une unité qu'un relecteur peut nommer en une phrase.
* Les tests de mutation sélectionnent par fichier — le coût d'une tranche suit les fichiers qu'elle
  touche, pas les lignes qu'elle change — donc une tranche large peut dépasser le budget consultatif
  par PR et ne remonter aucun score. C'est une conséquence du découpage, jamais une contrainte sur
  lui : la cohérence fonctionnelle décide des frontières, et le balayage hebdomadaire reste la mesure
  imposée (ADR-0046).

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
