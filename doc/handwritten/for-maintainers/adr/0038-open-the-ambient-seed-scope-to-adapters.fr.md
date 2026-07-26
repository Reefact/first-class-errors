# ADR-0038 | Ouvrir la portée de graine ambiante aux adaptateurs de framework de test

🌍 🇬🇧 [English](0038-open-the-ambient-seed-scope-to-adapters.md) · 🇫🇷 Français (ce fichier)

**Statut :** Accepté
**Date :** 2026-07-26
**Décideurs :** Reefact

## Contexte

`JustDummies` tire chaque valeur arbitraire d'une source aléatoire. Les points
d'entrée statiques `Any` tirent d'une source **ambiante** qui suit le contexte
d'exécution, si bien qu'elle ne fuit jamais entre des tests exécutés en
parallèle. Le déterminisme sur cette source ambiante est optionnel, et
aujourd'hui seuls deux chemins publics y accèdent :

* `Any.Reproducibly(...)`, qui fixe une graine pour la durée d'un **délégué dont
  il est propriétaire**, exécute ce délégué et rapporte la graine si celui-ci
  lève. L'ADR-0026 en a fait le récit unique de graine du dépôt.
* `Any.WithSeed(...)`, qui crée un contexte **isolé**. Les points d'entrée
  statiques `Any` n'y tirent pas, donc il ne fixe rien pour du code qui les
  utilise.

La poignée qui ouvre et ferme une portée de graine ambiante existe, mais elle est
interne.

Un adaptateur de framework de test — le package compagnon xUnit considéré
séparément, ou tout futur adaptateur pour un autre framework — ne possède pas de
délégué enveloppant le corps du test. La couture qu'offre un framework est une
paire de points d'accroche exécutés *avant* et *après* la méthode de test. Un
adaptateur doit donc ouvrir la portée ambiante dans l'un et la fermer dans
l'autre, ce qu'aucun chemin public ne permet.

Deux faits supplémentaires pèsent sur la forme de cette ouverture.

* **Les échecs de génération portent un extrait de rejeu.** Lorsqu'un
  générateur échoue — typiquement une fabrique rejetant une valeur tirée — le
  message d'exception ajoute une indication nommant le mécanisme qui rejoue
  réellement l'exécution. Cette indication est choisie par type de source : la
  source ambiante nomme l'exécuteur à délégué, un contexte isolé se nomme
  lui-même, parce que les deux se rejouent différemment. Nommer un extrait
  que le code de l'appelant ne contient pas est un diagnostic trompeur, et l'éviter
  est la raison même pour laquelle l'indication varie.
* **Aucune des formulations existantes ne convient à une exécution fixée par un
  adaptateur.** Un test dont la graine a été fixée par un adaptateur ne contient
  aucun appel à l'exécuteur à délégué, et le rejouer signifie modifier ce que
  l'adaptateur lit — un argument d'attribut, un réglage d'exécuteur — et non
  ajouter un appel que le test n'a jamais eu.

Le dépôt dispose déjà d'un idiome établi pour les surcharges locales au contexte,
consigné dans l'ADR-0006 et employé par les coutures d'horloge et d'identifiants
d'instance du package de test : la surcharge est ouverte par un appel `Use…` et
fermée en disposant ce qu'il retourne.

`JustDummies` est en pré-1.0 et n'est pas encore publié sur NuGet (ADR-0011), donc sa
surface publique peut encore grandir sans cérémonie de compatibilité. L'identité
de la bibliothèque est de ne dépendre de rien au-delà de la bibliothèque
standard, une frontière qu'un test d'architecture vérifie sur son propre
assembly.

Le chemin d'accès alternatif — accorder à un package compagnon nommé l'accès aux
membres internes de `JustDummies` — est disponible : la bibliothèque ne déclare
aucune autorisation de ce type aujourd'hui.

## Décision

`JustDummies` expose la portée de graine ambiante sous forme de poignée publique et
disposable, dont l'ouvreur peut fournir l'extrait de rejeu que les
diagnostics d'échec de génération nommeront.

## Justification

* **La forme d'un adaptateur est avant/après, pas autour d'un délégué.**
  L'exécuteur à délégué ne peut pas servir un appelant qui n'a aucun délégué à
  envelopper, et le contexte isolé est la mauvaise source — le code sous test
  tire de la source ambiante. Une portée que l'appelant ouvre et ferme lui-même
  est la seule forme qui épouse la couture qu'un framework de test offre
  réellement.
* **C'est le caractère public, et non une autorisation d'accès aux internes, qui
  garde tous les adaptateurs possibles.** Une autorisation d'accès privilégie un
  compagnon nommé et exclut les autres : un adaptateur tiers, ou un adaptateur
  interne pour un autre framework, exigerait chacun sa propre autorisation et sa
  propre modification de `JustDummies`. Une poignée publique fait de « adapter un
  autre framework plus tard » une décision additive ne touchant rien ici — ce qui
  est précisément la propriété qui permet de prendre la décision xUnit de façon
  étroite, sans trancher le reste.
* **Porter l'extrait de rejeu préserve un invariant que la bibliothèque
  applique déjà.** Le diagnostic nomme le mécanisme qui s'applique ; c'est
  d'ailleurs pourquoi l'indication varie selon la source. Un adaptateur introduit
  une troisième manière de fixer la source ambiante, et sans moyen de le dire il
  hériterait de la formulation de l'exécuteur à délégué — annonçant, à un
  développeur dont le test ne contient aucun appel de ce genre, exactement
  l'extrait trompeur que le mécanisme existe pour empêcher. Laisser
  l'ouvreur nommer l'extrait prolonge ce design au lieu de le contourner.
* **La forme « portée disposable » est déjà l'idiome maison.** L'ADR-0006 l'a
  établie pour les surcharges d'horloge et d'identifiants d'instance, si bien que
  l'ajout se reconnaît comme la même chose plutôt que comme un second mécanisme
  sans rapport.
* **C'est le moment le moins coûteux.** Le package est en pré-1.0 et non publié,
  donc la surface peut être façonnée maintenant ; un paramètre ajouté plus tard à
  un membre publié est plus perturbateur qu'un paramètre présent dès l'origine.

## Alternatives considérées

### Accorder au package compagnon l'accès aux membres internes de JustDummies

Considérée parce qu'elle n'ajoute aucune surface publique : l'adaptateur
utiliserait la poignée interne existante telle quelle. Rejetée parce qu'elle
privilégie un compagnon nommé — tout autre adaptateur, interne ou tiers, aurait
besoin de sa propre autorisation et donc de sa propre modification de `JustDummies` —
et parce qu'elle couple les identités d'assembly des deux packages pour une
capacité qui n'est pas, en elle-même, privée.

### Exposer la portée sans extrait de rejeu

Considérée comme le plus petit ajout possible, reportant la question du
diagnostic jusqu'à l'existence d'un adaptateur. Rejetée parce qu'elle livre le
diagnostic trompeur que le mécanisme d'indication existe pour empêcher : toute
exécution fixée par un adaptateur dont la génération échoue dirait au
développeur d'utiliser un appel que son test ne contient pas. Elle reporte en
outre l'ajout d'un paramètre sur un membre publié, ce qui est l'ordre le plus
perturbateur.

### Formuler l'indication ambiante de façon neutre, pour qu'elle ne soit jamais fausse

Considérée parce qu'une indication ne nommant aucun mécanisme ne peut pas nommer
le mauvais. Rejetée parce qu'elle fait payer le cas rare par le cas dominant :
les utilisateurs de l'exécuteur à délégué perdraient un extrait actionnable
— l'appel exact à écrire — pour accommoder un appelant qui peut simplement
énoncer la sienne.

### Laisser les adaptateurs réutiliser l'exécuteur à délégué

Considérée parce qu'elle n'exige rien de nouveau. Rejetée parce que les points
d'accroche avant/après d'un framework ne donnent à un adaptateur aucun délégué à
passer : il observe le test, il ne l'invoque pas.

## Conséquences

### Positives

* Tout framework de test peut être adapté sans accès privilégié à `JustDummies` et
  sans modification supplémentaire de celui-ci, si bien que chaque adaptateur
  supplémentaire est une décision indépendante et additive.
* Une exécution dont un adaptateur a fixé la graine rapporte un extrait de
  rejeu correspondant au code de l'appelant, ce qui préserve la garantie qu'un
  diagnostic ne nomme jamais un mécanisme que le lecteur n'utilise pas.
* L'ajout réutilise l'idiome établi de portée disposable au lieu d'introduire une
  seconde forme pour le même concept.

### Négatives

* Une troisième manière publique de contrôler la graine, aux côtés de l'exécuteur
  à délégué et du contexte isolé. La documentation doit garder les trois
  distinctes et dire laquelle le lecteur cherche.
* L'extrait de rejeu est fourni par l'appelant et ne peut pas être validé
  par `JustDummies` ; une formulation maladroite dégrade donc le diagnostic qu'elle
  devait améliorer.

### Risques

* Un appelant qui ouvre la portée sans la fermer laisse fuir une graine fixée
  vers ce qui s'exécute ensuite dans le même contexte d'exécution. Le risque est
  borné par l'idiome — la propriété appartient à qui a ouvert la portée — et
  c'est le même contrat que portent déjà les surcharges d'horloge et
  d'identifiants d'instance.
* Une poignée publique invite à un usage hors adaptateur de framework de test, là
  où l'exécuteur à délégué servirait mieux. C'est une affaire de documentation,
  pas de justesse : la portée se comporte identiquement quelle que soit la
  manière dont elle est ouverte.

## Actions de suivi

* Documenter l'ajout dans le guide utilisateur, en anglais et en français de
  concert, en distinguant les trois manières de contrôler la graine et en
  désignant le cas de l'adaptateur comme celui pour lequel cette poignée existe.
* Réexaminer la forme portant l'extrait si un second adaptateur montre
  qu'elle reste habituellement inutilisée.

## Références

* ADR-0006 — Fournir les valeurs de test arbitraires depuis une source unique
  semable : l'idiome de portée disposable que cet ajout réutilise, et le suivi
  anticipant un adaptateur de framework de test.
* ADR-0011 — Héberger JustDummies comme package autonome : l'identité zéro-dépendance
  et la latitude pré-1.0 sur lesquelles cette décision s'appuie.
* ADR-0026 — Rebaser les valeurs arbitraires du package de test sur JustDummies : le
  récit unique de graine que cette source ambiante porte désormais.
* ADR-0039 — Adapter JustDummies à xUnit v3 via un package compagnon : le premier
  consommateur de cette poignée.
* Issue #226 — le backlog des « nice-to-have » de JustDummies où l'adaptateur est
  suivi.
