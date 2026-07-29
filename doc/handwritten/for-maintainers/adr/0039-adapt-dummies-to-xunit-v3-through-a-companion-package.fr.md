# ADR-0039 | Adapter JustDummies à xUnit v3 via un package compagnon

🌍 🇬🇧 [English](0039-adapt-dummies-to-xunit-v3-through-a-companion-package.md) · 🇫🇷 Français (ce fichier)

**Statut :** Accepté
**Proposé :** 2026-07-26
**Accepté :** 2026-07-26
**Décideurs :** Reefact

## Contexte

Un test qui tire des valeurs arbitraires n'est reproductible que si une graine
est fixée et rapportée. `JustDummies` fournit cela via un exécuteur qui fixe une
graine pour la durée d'un délégué et la rapporte lorsque ce délégué lève ; chaque
test sensible aux valeurs doit donc envelopper son corps dans ce délégué. La
cérémonie est reconstituée à la main dans chaque consommateur.

Un adaptateur qui la supprime a été anticipé puis perdu. Les suivis de l'ADR-0006
appelaient un adaptateur optionnel de framework de test « pour que la graine soit
exposée automatiquement, sans envelopper chaque corps » ; l'ADR-0026 a rebasé le
moteur de valeurs sur `JustDummies` sans reprendre ce suivi. La capacité est donc
anticipée par une ADR acceptée et remplacée par rien. L'audit d'architecture et
de conception de `JustDummies` du 2026-07-20 demande un oui ou un non explicite
plutôt qu'un silence prolongé, et place la décision dans le premier cycle stable.

La capacité qu'un adaptateur doit fournir est étroite : fixer une graine pour la
durée d'un test, et exposer cette graine au développeur **uniquement lorsque le
test échoue**. Une graine rapportée à chaque exécution est du bruit ; une graine
jamais rapportée laisse un échec irreproductible.

L'identité de `JustDummies` est de ne dépendre de rien au-delà de la bibliothèque
standard (ADR-0011), une frontière qu'un test d'architecture vérifie sur son
propre assembly. Elle ne peut donc pas référencer un framework de test, si bien
que tout adaptateur est un package compagnon distinct — l'arrangement que
`FirstClassErrors.Testing` établit déjà comme précédent dans ce dépôt.

Les frameworks diffèrent par ce qu'expose leur extensibilité supportée, et la
différence est décisive pour la condition « uniquement en cas d'échec » :

* **xUnit v3.** Son point d'accroche avant/après reçoit le test lui-même, et le
  contexte de test ambiant expose l'issue du test terminé — succès ou échec, sa
  cause d'échec et le détail de son exception — ainsi que le puits de sortie du
  test. La condition « uniquement en cas d'échec » est donc exprimable dans
  l'extensibilité documentée, sans aucune implication dans la découverte ni
  l'exécution des tests. Le même point d'accroche est collecté depuis la méthode,
  la classe et l'assembly, si bien qu'un attribut unique sert un test, une classe
  entière ou une suite complète, et il s'exécute une fois par cas de théorie
  plutôt qu'une fois par méthode de théorie.
* **xUnit v2.** Son point d'accroche équivalent ne reçoit que la méthode sous
  test, et son assembly ne porte aucun contexte de test. Un attribut v2 ne peut
  pas observer si le test a réussi ou échoué. Y exprimer la condition « uniquement
  en cas d'échec » exige de remplacer la chaîne de découverte et d'exécution des
  cas de test.

Les deux versions livrent des assemblies et des espaces de noms distincts, si
bien qu'un seul assembly ne peut pas référencer les deux ; supporter chacune
signifierait de toute façon un package séparé.

Les projets de test de ce dépôt s'exécutent déjà sur xUnit v3, si bien qu'un
adaptateur v3 est éprouvé par son auteur autant que documenté.

L'exécuteur à délégué continue de fonctionner sur tous les frameworks et n'est
pas affecté par cette décision ; les utilisateurs de tout autre framework ne
perdent donc aucune capacité — ils conservent la forme qui existe aujourd'hui.

L'ADR-0038 ouvre la portée de graine ambiante sous forme de poignée publique, si
bien qu'un adaptateur n'a besoin d'aucun accès privilégié à `JustDummies` et
qu'ajouter plus tard un adaptateur pour un autre framework n'exige aucune
modification de celui-ci.

`JustDummies` est publié sur le train de release `dum`, qui ne porte actuellement
qu'un seul package. La bibliothèque a vocation à rejoindre son propre dépôt à
terme.

## Décision

`JustDummies` reçoit un package compagnon qui fixe et rapporte automatiquement la
graine pour les tests xUnit v3, et ne vise aucun autre framework de test.

## Justification

* **Le rapport « uniquement en cas d'échec » est toute la capacité, et seul v3
  sait l'exprimer.** Fixer une graine est facile partout ; décider s'il faut
  l'exposer est ce qui sépare un adaptateur utile du bruit. xUnit v3 expose
  l'issue du test terminé dans son extensibilité documentée, si bien que
  l'adaptateur est une petite quantité de code au-dessus d'un contrat supporté.
  En v2 la même condition est inatteignable depuis le point d'accroche
  correspondant et exige de s'approprier la découverte et l'exécution sur une
  surface semi-interne — un coût permanent et fragile, assumé pour un package
  explicitement non destiné à être rouvert.
* **Un seul point d'accroche couvre toute la surface.** Parce que le framework
  collecte le point d'accroche depuis la méthode, la classe et l'assembly et
  l'exécute par cas de théorie, un attribut unique sert un test, une classe, une
  suite entière et chaque cas d'une théorie. C'est toute la surface dont la
  capacité a besoin, sans second type par sorte de test et sans toucher à la
  manière dont les tests sont découverts.
* **Rien n'est retiré à personne d'autre.** L'exécuteur à délégué reste la forme
  portable et continue de fonctionner sur tous les frameworks ; choisir un
  framework pour l'adaptateur ne prive donc les utilisateurs des autres d'aucune
  capacité, mais seulement de la commodité.
* **Un package compagnon est imposé par l'identité de la bibliothèque.** La
  frontière zéro-dépendance rend impossible une référence à un framework de test
  à l'intérieur de `JustDummies`, et le dépôt livre déjà un package compagnon pour
  exactement cette raison.
* **Le choix est éprouvé par son auteur.** Les suites de ce dépôt s'exécutent sur
  xUnit v3, si bien que l'adaptateur est utilisé là où il est maintenu plutôt que
  livré sans usage.
* **L'étroitesse est délibérée et peu coûteuse à réexaminer.** Parce que
  l'ADR-0038 rend la portée de graine publiquement atteignable, un adaptateur
  pour un autre framework est une décision additive n'exigeant aucune
  modification ici — décliner les autres maintenant n'exclut donc rien.

## Alternatives considérées

### Ne rien livrer et conserver l'exécuteur à délégué

Considérée parce qu'elle ne coûte rien, fonctionne sur tous les frameworks et
correspond déjà à ce que font les consommateurs. Rejetée parce que c'est ce que
le silence a déjà produit une fois : le suivi consigné par l'ADR-0006 a été
abandonné lors du rebase et remplacé par rien, et l'audit demande que la question
soit tranchée plutôt que laissée ouverte. La cérémonie ainsi préservée est
reconstituée à la main dans chaque consommateur, ce qui est précisément le coût
que l'adaptateur existe pour supprimer.

### Supporter aussi xUnit v2, dans un second package

Considérée parce que v2 reste une base installée importante, et que « facilement
adoptable » plaide pour rejoindre les consommateurs là où ils sont. Rejetée parce
que la condition « uniquement en cas d'échec » n'est pas exprimable dans le point
d'accroche avant/après de v2 : la livrer signifie remplacer la chaîne de
découverte et d'exécution des cas de test et la maintenir indéfiniment contre une
surface semi-interne. C'est un coût disproportionné et permanent pour une
commodité dont l'absence laisse les utilisateurs v2 exactement où ils sont
aujourd'hui, avec l'exécuteur à délégué portable.

### Dériver des attributs de fait et de théorie du framework

Considérée parce qu'elle produit un attribut unique et auto-descriptif par sorte
de test, correspondant au nom qu'avait esquissé le suivi de l'ADR-0006. Rejetée
parce qu'elle coûte un type par sorte de test, ne se compose pas avec des
attributs de fait tiers, ne peut être appliquée ni à une classe ni à un assembly,
et achète une exposition aux internes de la découverte en échange d'aucune
capacité qui manquerait au point d'accroche avant/après.

### Construire un adaptateur agnostique du framework

Considérée parce qu'elle servirait tous les frameworks d'un coup et rendrait le
choix sans objet. Rejetée parce qu'il n'existe aucun point d'accroche
inter-frameworks sur lequel la bâtir : la capacité est définie par ce que chaque
framework expose d'un test terminé, et ces surfaces n'ont ni forme ni vocabulaire
communs.

## Conséquences

### Positives

* La reproductibilité devient déclarative et optionnelle à la granularité que
  l'auteur choisit — un test, une classe ou une suite entière — au lieu d'un
  délégué enveloppant chaque corps sensible aux valeurs.
* Une exécution en échec nomme sa graine sans que l'auteur ait anticipé l'échec,
  ce qui est le cas que l'exécuteur à délégué ne sert que lorsqu'il a été
  appliqué à l'avance.
* L'adaptateur est éprouvé par les suites de ce dépôt.

### Négatives

* Un nouveau package publié, avec sa propre documentation en anglais et en
  français, sa propre référence d'API publique et sa propre place dans la chaîne
  de build et de release.
* La commodité n'atteint que les utilisateurs de xUnit v3 ; tout autre framework
  conserve l'exécuteur à délégué.
* Le plancher de frameworks supportés du package est celui du framework de test,
  au-dessus du plancher que `JustDummies` conserve — les deux ne peuvent donc pas
  partager une même liste de cibles.

### Risques

* L'adaptateur dépend du contrat avant/après du framework et de son exposition de
  l'issue d'un test terminé. Une future version majeure pourrait changer l'un ou
  l'autre. L'exposition est bornée : l'adaptateur utilise l'extensibilité
  documentée, pas les internes, si bien qu'un changement se manifesterait comme
  un échec de compilation ou de comportement dans sa propre suite plutôt que
  silencieusement.
* Une portée de graine ouverte avant un test doit être fermée même lorsque le
  test lève, sans quoi la graine fixée fuit vers ce qui s'exécute ensuite dans le
  même contexte d'exécution.

## Actions de suivi

* Publier le package sur le train `dum` existant dans un premier temps, pour
  qu'il soit versionné avec `JustDummies`. **Lorsque `JustDummies` rejoindra son propre
  dépôt, réexaminer si le package compagnon a besoin de son propre train** — un
  train partagé n'est approprié que tant que les deux sont livrés depuis le même
  endroit et à la même cadence.
* Documenter l'adaptateur dans le guide utilisateur et le readme du package, en
  anglais et en français de concert, en présentant l'exécuteur à délégué comme la
  forme portable et l'adaptateur comme la commodité xUnit v3.
* Ne réexaminer un adaptateur pour un autre framework que sur demande démontrée ;
  l'ADR-0038 garde chacun additif.

## Références

* ADR-0038 — Ouvrir la portée de graine ambiante aux adaptateurs de framework de
  test : la poignée publique dont ce package est le premier consommateur.
* ADR-0006 — Fournir les valeurs de test arbitraires depuis une source unique
  semable : le suivi qui a anticipé cet adaptateur.
* ADR-0011 — Héberger JustDummies comme package autonome : la frontière
  zéro-dépendance qui impose un package compagnon.
* ADR-0026 — Rebaser les valeurs arbitraires du package de test sur JustDummies : le
  rebase dans lequel le suivi anticipé a été abandonné.
* `doc/handwritten/for-maintainers/audit/2026-07-20-dummies-architecture-and-design-audit.md`
  — l'audit demandant une décision explicite.
* Issue #226 — le backlog des « nice-to-have » de JustDummies où l'adaptateur est
  suivi.
