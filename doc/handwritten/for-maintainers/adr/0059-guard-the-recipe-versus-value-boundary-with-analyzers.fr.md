# ADR-0059 | Garder la frontière recette/valeur avec des analyseurs là où le système de types ne l'atteint pas

🌍 🇫🇷 Français (ce fichier) · 🇬🇧 [English](0059-guard-the-recipe-versus-value-boundary-with-analyzers.md)

**Statut :** Proposé
**Proposé :** 2026-07-29
**Décideurs :** Reefact

## Contexte

* L'ADR-0020 a supprimé les 28 conversions implicites des générateurs JustDummies vers leur type généré, faisant de
  `Generate()` la seule matérialisation. Sa section *Risques* évaluait le danger résiduel comme borné : un
  utilisateur qui omet `.Generate()` obtient « une erreur de compilation avec un message actionnable ..., **jamais
  une valeur silencieusement fausse** ». Ses *Actions de suivi* concluaient : « Ne pas poursuivre l'analyseur
  optionnel suggéré par l'issue #190 ; la suppression le rend inutile. »
* Cette évaluation tient partout où la position cible est typée par la **valeur générée**. `int x = Any.Int32()`
  est un `CS0029`, `Any.Int32() == 5` un `CS0019`, et `Assert.Equal(Any.Int32(), value)` un `CS0411`. Là,
  supprimer la conversion a bien transformé une substitution silencieuse en erreur de compilation.
* Elle ne tient pas partout où la position cible accepte le **type statique propre** du générateur. Les
  générateurs sont des types référence : aucune conversion n'est nécessaire et il n'y en avait donc aucune à
  supprimer. `object`, `params object[]`, `dynamic`, un élément d'`object[]` ou de `List<object>`, un trou
  d'interpolation, un opérande de concaténation `string`, ainsi que les `object.ToString()` / `object.Equals`
  hérités acceptent tous un générateur tel quel.
* Aucun générateur JustDummies ne surcharge `ToString()`. Le rendre sous forme de texte produit donc le nom de
  type CLR du constructeur — `$"{Any.String()}"` donne littéralement la chaîne `"JustDummies.AnyString"`. Vérifié
  par compilation : chacune des formes ci-dessus compile sans le moindre diagnostic.
* La valeur obtenue est non vide, plausible et identique à chaque exécution. Elle atteint le code sous test comme
  s'il s'agissait d'une valeur arbitraire : le test passe au vert tout en exerçant une constante — précisément le
  résultat qu'`Any` existe pour empêcher, et celui que l'ADR-0020 avait consigné comme impossible.
* Une seconde forme voisine est silencieuse pour la même raison structurelle. Les générateurs étant des recettes
  immuables, une contrainte retourne un nouveau générateur ; un appel dont le résultat est jeté
  (`numbers.NonEmpty();`) se lit comme une mutation et perd l'invariant déclaré. Vérifié : aucun diagnostic
  compilateur, CA ou IDE ne se déclenche, même en `AnalysisLevel=latest-all`, une invocation étant une instruction
  d'expression légale.
* L'ADR-0044 a établi les analyseurs JustDummies de première partie comme la réponse du dépôt à une faute que le
  système de types ne peut pas exprimer, et son propre suivi invite à appliquer ce motif aux fautes futures de ce
  genre. L'ADR-0035 trace la frontière en sens inverse pour les conflits de contraintes : le système de types
  porte ce qui est structurel, l'analyseur porte ce qu'il ne peut pas porter.
* JustDummies est en pré-1.0 : aucun consommateur n'a encore appris l'un ou l'autre comportement.

## Décision

La frontière recette/valeur est gardée par des analyseurs JustDummies de première partie dans toute position qui
accepte le type statique propre d'un générateur, position que la suppression des conversions implicites n'a pas
fermée.

## Justification

* La décision prise par l'ADR-0020 n'est pas touchée et reste juste : `Generate()` demeure la seule
  matérialisation, et aucune conversion implicite ne revient. Ce que le présent ADR révise, c'est une **prédiction**
  que l'ADR-0020 formulait sur le monde d'après cette suppression — qu'aucune valeur silencieusement fausse ne
  pouvait y survivre — ainsi que l'action de suivi qui reposait sur cette prédiction. Un enregistrement dont le
  raisonnement est sain mais dont l'affirmation factuelle est désormais connue comme fausse se corrige par un
  nouvel enregistrement, non en laissant l'affirmation se lire comme encore vraie.
* L'analyseur que l'ADR-0020 a écarté et ceux décidés ici ne sont pas le même instrument. Celui qui fut rejeté
  était le prix du *maintien* des conversions — une surface permanente de 28 opérateurs plus une règle pour en
  policer les pièges, afin de préserver un raccourci. Ceux-ci font l'inverse : rien n'est préservé, aucune surface
  n'est ajoutée, ils ferment ce que la suppression a laissé ouvert. L'argument de l'ADR-0020 contre le premier
  n'atteint pas les seconds.
* Le point d'application suit ce que chaque mécanisme peut savoir, du même grain que l'ADR-0035 et l'ADR-0044. C#
  ne peut pas refuser un type référence dans une position typée `object`, ni rendre illégale une instruction
  d'expression ; le système de types ne *peut donc pas* porter ces deux règles, ce qui fait de l'analyseur le seul
  mécanisme disponible plutôt qu'un substitut affaibli.
* La sévérité suit le mode de défaillance plutôt que la famille. Un générateur rendu comme texte est un vert
  silencieux — la compilation réussit, le test passe, l'assertion ne veut rien dire — soit le cas que l'ADR-0044 a
  déjà jugé digne de faire échouer la compilation. Une contrainte jetée est un vert *probabiliste*, rouge
  seulement sur l'exécution qui tire hors du domaine visé : elle avertit plutôt qu'elle n'échoue.
* Le coût est borné par ce que les règles renoncent à signaler. Un diagnostic sur la frontière recette/valeur est
  peu coûteux à avoir tort, un usage légitime d'un générateur en position `object` étant rare et une suppression
  tenant en une ligne ; les règles sont néanmoins cadrées pour rester muettes sur un résultat explicitement jeté et
  sur un test négatif vérifiant un conflit, ce qui les garde utilisables dans une suite qui teste le comportement
  d'échec de la bibliothèque elle-même.

## Alternatives considérées

### Laisser cela à la documentation, comme le prescrivait le suivi de l'ADR-0020

Considérée parce que c'est la décision en vigueur, qu'elle ne coûte rien, et que la documentation de la
bibliothèque enseigne déjà longuement le modèle recette/valeur.

Rejetée parce que la documentation ne peut pas atteindre la défaillance. Le défaut produit une compilation qui
réussit et un test qui passe : il n'existe aucun moment où un lecteur est incité à consulter la documentation, ni
aucun artefact signalant que quelque chose ne va pas. Tous les autres mécanismes de la bibliothèque qui gardent ce
modèle — les conversions supprimées, les conflits de contraintes levés au plus tôt — échouent bruyamment ; laisser
ce seul cas à la prose est le seul endroit où le modèle est enseigné sans être appliqué.

### Rétablir une conversion implicite étroite pour que le compilateur refuse les positions ambiguës

Considérée parce qu'une conversion vers le type généré ferait lier la valeur plutôt que la recette dans une
position `object`, fermant le trou dans le langage plutôt qu'à côté.

Rejetée parce qu'elle réintroduit exactement ce que l'ADR-0020 a supprimé, et pour une plus mauvaise raison : la
conversion est effectuante, non idempotente et levante, et la position `object` est précisément l'endroit où son
comportement serait le moins prévisible. Ce serait échanger une faute diagnosticable contre une faute qui ne l'est
pas.

### Rendre les générateurs étanches au rendu textuel en surchargeant `ToString()`

Considérée parce qu'une surcharge retournant la valeur tirée, ou une chaîne délibérément alarmante, rendrait
`$"{Any.String()}"` inoffensif ou manifestement faux au premier coup d'œil, sans aucun analyseur.

Rejetée dans les deux lectures. Retourner une valeur tirée fait de `ToString()` un tirage effectuant et non
idempotent — la conversion implicite à nouveau, sous un autre nom. Retourner une chaîne d'alarme améliore le
symptôme sans l'empêcher : le test passe toujours, assère toujours sur une constante, et l'alarme ne se manifeste
que si un humain lit la valeur.

## Conséquences

### Positives

* Les deux formes silencieuses deviennent des diagnostics à la compilation : un générateur rendu comme texte fait
  échouer la compilation, une contrainte jetée avertit, avec un message qui enseigne le modèle plutôt que de se
  contenter de nommer la règle.
* L'affirmation factuelle de l'ADR-0020 est corrigée dans le registre au lieu d'être laissée à la découverte de
  celui qui la heurtera, et la raison pour laquelle son action de suivi ne s'applique plus est énoncée là où un
  futur mainteneur ira la chercher.
* La catégorie `JustDummies.Usage` donne un domicile aux règles recette/valeur, si bien qu'un consommateur peut les
  régler indépendamment des règles de reproductibilité.

### Négatives

* L'ensemble de règles grossit, et avec lui la surface documentaire : chaque règle porte une page anglaise et une
  page française, une entrée d'index et une ligne de suivi de version.
* Deux règles se déclenchent sur des formes qu'une suite testant le comportement d'échec de JustDummies écrit
  légitimement ; toutes deux portent donc une exclusion documentée qu'un lecteur doit connaître pour raisonner sur
  ce que les règles n'attrapent pas.

### Risques

* La famille des positions `object` est plus large que les deux règles décidées ici — un paramètre typé `object`,
  un élément de `params object[]`, `dynamic` — et la couvrir comporte un vrai faux positif : un utilitaire de test
  qui accepte délibérément `object` et matérialise lui-même. Atténué en laissant cette règle hors de la présente
  décision et en la tranchant sur des observations de mise en pratique plutôt qu'à l'avance.
* Une règle fondée sur l'absence de surcharge de `ToString()` cesserait silencieusement de s'appliquer si un
  générateur en gagnait une un jour. Atténué en résolvant spécifiquement l'`object.ToString()` hérité, de sorte
  qu'une vraie surcharge est exclue par construction et non par hypothèse.

## Actions de suivi

* Ne rien remplacer : la décision de l'ADR-0020 demeure inchangée, et son statut appartient au mainteneur s'il juge
  que l'affirmation corrigée le justifie.
* Trancher la règle restante sur les positions `object` à partir des observations recueillies sur les suites de ce
  dépôt, et pas avant.

## Références

* ADR-0020 — matérialiser les dummies uniquement par `Generate()` ; la décision que celui-ci laisse debout et dont
  il corrige l'affirmation de risque résiduel.
* ADR-0044 — fournir des analyseurs JustDummies de première partie ; le motif que la présente décision applique, et
  la source du grain de sévérité (« un vert silencieux mérite de faire échouer la compilation »).
* ADR-0035 — appliquer les conflits `Any` structurels à la compilation, ceux dépendant des valeurs à l'exécution ;
  le même raisonnement « l'application suit ce que le mécanisme peut savoir », appliqué à la surface de
  contraintes.
* Issue #190 — définir et documenter le contrat des conversions implicites de générateurs ; l'origine de
  l'analyseur que l'ADR-0020 a écarté.
