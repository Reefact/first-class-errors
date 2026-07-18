# ADR-0013 | Contrôler les collections distinctes par la cardinalité, sinon par un tirage borné

🌍 🇬🇧 [English](0013-gate-distinct-collections-by-cardinality-else-bounded-draw.md) · 🇫🇷 Français (ce fichier)

**Statut :** Proposé
**Date :** 2026-07-18
**Décideurs :** Reefact

## Contexte

`Dummies` porte un contrat fondateur : une contrainte exprime ce qu'une valeur
doit satisfaire, des contraintes contradictoires échouent au moment où elles sont
déclarées via une `ConflictingAnyConstraintException` nommant les deux côtés, et
une valeur est construite pour satisfaire ses contraintes en une seule passe —
jamais générée puis filtrée, et jamais derrière une boucle de réessai.

L'incrément « collections » ajoute les collections distinctes : `SetOf`,
`ListOf(...).Distinct()` et consorts, ainsi que les clés d'un dictionnaire. Une
collection distincte de *N* éléments n'est satisfaisable que si son générateur
d'éléments peut produire au moins *N* valeurs distinctes, ce qui est une propriété
du domaine de ce générateur.

Les générateurs d'éléments se répartissent en deux groupes. Certains tirent d'un
domaine petit et dénombrable : un booléen a deux valeurs, une énumération a ses
membres déclarés, un intervalle entier étroit ou un pool de caractères restreint a
une taille fixe. D'autres tirent d'un domaine effectivement non borné ou
simplement inconnaissable pour la collection : entiers non contraints, chaînes et
identifiants, et — de façon décisive — toute implémentation étrangère de
`IAny<T>` ou tout générateur dérivé (`As`, `Combine`), qui ne porte aucune
information de domaine. `IAny<T>` est une interface publique : la bibliothèque ne
peut donc pas supposer que tout générateur sache rapporter sa cardinalité.

Un comparateur d'égalité personnalisé ne peut que fusionner des valeurs distinctes
en un nombre moindre de classes d'équivalence ; il ne peut jamais en créer de
nouvelles.

## Décision

Une collection distincte rejette, au moment de la déclaration, tout nombre
d'éléments qui dépasse la cardinalité annoncée par le générateur d'éléments, et
construit sinon ses éléments par un tirage dédupliquant borné qui échoue à la
génération, avec une graine rejouable, si le domaine des éléments se révèle trop
petit.

## Justification

* **Échouer tôt partout où le domaine est connaissable.** Le conflit à la
  déclaration est la signature de la bibliothèque : un nombre qui dépasse un
  domaine d'éléments dénombrable est une contradiction dans l'`Arrange` du test, et
  il doit se lire comme telle, nommée des deux côtés, exactement comme tout conflit
  scalaire — non pas surgir plus tard sous forme d'un échec d'exécution
  déroutant.
* **Un tirage borné est la seule option honnête là où il ne l'est pas.** Comme la
  cardinalité d'un générateur arbitraire est généralement inconnaissable, la seule
  façon universelle d'obtenir *N* valeurs distinctes est de tirer et dédupliquer.
  Garder ce tirage borné respecte le principe « pas de boucle de réessai » ; à
  épuisement, il rapporte le manque réel via une `AnyGenerationException` nommant
  la graine — le canal d'échec qu'utilise déjà un rejet de fabrique — plutôt que de
  boucler.
* **La borne annoncée reste correcte sous un comparateur.** Puisqu'un comparateur
  ne fait que fusionner des valeurs, la cardinalité annoncée reste une borne
  *supérieure* valide : le contrôle anticipé ne rejette donc jamais une demande qui
  était en réalité satisfaisable ; un comparateur qui réduit le domaine sous le
  nombre demandé est rattrapé par le tirage borné.
* **Un seul principe, appliqué là où son information existe.** Répartir l'échec
  entre la déclaration (quand le domaine est dénombrable) et la génération (quand
  il ne l'est pas) n'est pas un affaiblissement du principe de conflit anticipé
  mais son extension fidèle au seul endroit où l'information nécessaire pour être
  anticipé fait défaut.

## Alternatives considérées

### Toujours échouer à la génération, en abandonnant le contrôle anticipé de cardinalité

Considérée parce qu'un canal d'échec unique est plus simple à expliquer et à
implémenter. Rejetée parce qu'elle jette le diagnostic signature de la
bibliothèque précisément là où il est peu coûteux et certain — un ensemble de trois
booléens, une énumération à qui l'on demande plus de membres qu'elle n'en déclare —
transformant une contradiction évidente de l'`Arrange` en une surprise à
l'exécution.

### Faire de la cardinalité une partie de `IAny<T>`, pour trancher chaque demande tôt

Considérée parce qu'une cardinalité obligatoire sur chaque générateur permettrait
de trancher toute demande distincte à la déclaration. Rejetée parce que `IAny<T>`
est un contrat public, avec des implémentations étrangères et des générateurs
dérivés (`As`, `Combine`) incapables de rapporter honnêtement une borne ; la
garantie serait inapplicable et souvent fausse, et elle imposerait à chaque
implémenteur une valeur que la plupart ne peuvent pas fournir.

### Tirer sans borne jusqu'à ce que *N* valeurs distinctes apparaissent

Considérée parce qu'un tirage non borné se termine toujours quand la demande est
satisfaisable. Rejetée parce qu'il ne se termine jamais quand la demande ne l'est
*pas*, ce qui est exactement le cas que cette décision doit diagnostiquer : elle
transformerait une demande impossible en blocage au lieu d'une erreur, brisant le
principe de travail borné de la bibliothèque.

## Conséquences

### Positives

* Le diagnostic signature à la déclaration atteint désormais les collections
  distinctes partout où le domaine des éléments est dénombrable.
* Les demandes sur des domaines inconnus ou réduits par un comparateur échouent
  toujours de façon sûre et reproductible, avec une graine à rejouer, jamais en
  blocage.
* La capacité de cardinalité est interne et optionnelle : le contrat public
  `IAny<T>` reste inchangé et les générateurs étrangers continuent de fonctionner
  tels quels.

### Négatives

* Le moment de l'échec n'est pas uniforme : la même contradiction logique surgit à
  la déclaration pour un domaine connu-petit et à la génération pour un domaine
  inconnaissable, ce que l'utilisateur doit comprendre.
* Le tirage borné s'exécute jusqu'à un budget choisi ; une demande poussée
  pathologiquement près de la taille réelle d'un domaine inconnu pourrait en
  principe échouer bien qu'elle fût satisfaisable — astronomiquement improbable pour
  les collections de taille « dummy » que vise la bibliothèque.

### Risques

* **Indice surestimé** — un générateur pourrait annoncer une cardinalité plus
  grande que les valeurs distinctes qu'il produit réellement, de sorte que le
  contrôle anticipé manque un vrai conflit. Atténué parce que l'indice est défini
  comme une borne supérieure et que le tirage borné rattrape tout manque résiduel à
  la génération.
* **Mauvais réglage du budget** — un budget de tirage trop petit produirait des
  échecs de génération fallacieux. Atténué en dimensionnant le budget sur une
  cardinalité connue et en gardant un plancher généreux pour les domaines inconnus.

## Actions de suivi

* Documenter les deux canaux d'échec dans la documentation utilisateur une fois la
  surface « collections » stabilisée.
* Réexaminer le budget de tirage si un usage réel fait un jour apparaître un
  épuisement fallacieux.

## Références

* ADR-0011 — Héberger Dummies comme un paquet autonome dans ce dépôt.
* Le moteur de collection distincte et la capacité de cardinalité, dans le projet
  `Dummies` (`CollectionState`, `ICardinalityHint`).
