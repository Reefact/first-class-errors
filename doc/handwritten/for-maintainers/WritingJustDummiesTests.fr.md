# Écrire les tests de JustDummies

🌍 🇬🇧 [English](WritingJustDummiesTests.en.md) · 🇫🇷 Français (ce fichier)

> Où placer un nouveau test pour `JustDummies`, et comment l'écrire. La frontière
> entre les deux suites est enregistrée dans
> [l'ADR-0040](adr/0040-split-the-justdummies-test-bed-between-example-and-property-suites.fr.md) ;
> cette page explique comment l'appliquer.

## Les deux suites

| Projet | Porte | Style |
|---|---|---|
| `JustDummies.PropertyTests` | Les invariants vrais pour **tout** argument de contrainte légal | Propriétés FsCheck sur contraintes générées |
| `JustDummies.UnitTests` | Les contrats dont le sujet est un **cas spécifique et nommé** | Exemples xUnit + NFluent |

Les deux tournent sur `net10.0` et sur le plancher .NET Framework 4.7.2 : chacune
prouve donc sa moitié contre l'asset `netstandard2.0` que les consommateurs
chargent réellement.

## L'unique question à se poser

> *Mon assertion a-t-elle un espace d'entrée ?*

Quelque chose que l'appelant aurait pu passer autrement — une borne, une longueur,
une cardinalité, un vivier, une graine, un motif, un décalage — sans que
l'assertion cesse d'être vraie ?

* **Oui** → c'est une propriété. Générez cette entrée et quantifiez dessus.
* **Non** → c'est un exemple. Figez-le et affirmez-le directement.

C'est toute la règle. Tout ce qui suit n'en est que l'application.

### Va dans la suite par propriétés

* **Contenance et stricture.** `Between(min, max)` contient ; `GreaterThan` est
  strict ; `GreaterThanOrEqualTo` admet sa propre borne. La borne est l'espace
  d'entrée.
* **Forme.** `WithLength(n)` produit exactement `n` ; `StartingWith(prefix)`
  commence effectivement par lui ; `WithCount(n)` rend exactement `n` éléments.
* **Grilles.** `MultipleOf`, `WithScale`, `WithGranularity` — le pas est l'espace
  d'entrée, et l'ancre diffère selon le type : c'est là que ça dérape.
* **Allers-retours.** Une valeur générée depuis un motif est reconnue par le vrai
  moteur ; une URI générée s'analyse et porte la forme demandée.
* **Déterminisme.** Deux contextes de même graine concordent — pour *toute* graine,
  pas pour 12345.
* **Composition.** `As`, `OrNull`, `Combine`, les viviers explicites : la valeur
  composée porte la contrainte de chaque partie, quelles qu'aient été ces
  contraintes.
* **Légalité dépendante de la valeur.** Quand le même appel est légal ou illégal
  selon son argument, une propriété est la seule façon honnête de l'énoncer — se
  ramifier sur la valeur, jamais sur la forme de l'appel.

### Reste dans la suite par l'exemple

* **Contenu des messages.** Un conflit doit nommer *les deux* contraintes fautives.
  La formulation est sensible au sens d'application : affirmez-la sur un cas figé,
  et ailleurs affirmez le **type** de l'exception.
* **Arguments nuls ou vides.** `null` n'a pas d'espace d'entrée.
* **Extrêmes nommés du domaine.** `int.MinValue`, `byte.MaxValue`, un vivier vide —
  une coordonnée précise, plus claire et moins chère figée que quantifiée.
* **Atteignabilité.** Qu'une plage bornée soit effectivement atteinte, que les deux
  branches d'un tirage soient observées. C'est statistique, non universel : figez
  une graine.
* **Conventions structurelles.** Le miroir `Any` ↔ `AnyContext`, le nommage des
  fabriques, la frontière d'assemblage autonome. De la réflexion sur une table
  d'attentes fixe ; il n'y a rien à générer.
* **Régressions datées.** Un défaut qui a réellement eu lieu, figé aux coordonnées
  où il a eu lieu. Une propriété couvrant le même terrain ne la retire **pas** — la
  spécificité *est* la valeur. Référencez l'issue en commentaire.

## Ajouter une fonctionnalité

1. Écrivez d'abord les tests par l'exemple : c'est ainsi qu'on découvre la forme, et
   ce sont eux qui portent les messages de conflit que votre nouvelle contrainte doit
   produire.
2. Posez ensuite la question ci-dessus à chaque invariant écrit. Tout ce qui vaut
   pour tout argument devient une propriété, et l'exemple qui en figeait un seul
   disparaît avec.
3. Si votre contrainte interagit avec une contrainte existante, l'interaction est
   presque toujours une propriété : elle a deux espaces d'entrée.

## Corriger un défaut

1. Figez le défaut en exemple, aux coordonnées où il a été trouvé, avec le numéro
   d'issue en commentaire. C'est la régression, et elle reste pour toujours.
2. Demandez-vous ensuite si le défaut avait un espace d'entrée que l'exemple ne
   couvre pas. L'issue #206 en avait un : le bug du milieu d'intervalle décimal a été
   trouvé sur un intervalle et vivait sur tous. Ajoutez aussi la propriété.
3. Les deux atterrissent. La régression prouve le cas exact ; la propriété prouve la
   classe.

## Écrire la propriété

Utilisez les helpers partagés de `PropertyTestSupport.cs` :

* `Generators.OrderedPair(values)` — un `(min, max)` bien formé, paires dégénérées
  comprises. Les intervalles épinglés sont un coin historiquement fragile : ne les
  filtrez pas, ramifiez-vous dessus.
* `Generators.WithEdges(values, edges)` — les générateurs numériques de FsCheck sont
  bornés en taille et se massent près de zéro : sans cela les extrémités du domaine
  ne seraient pour ainsi dire jamais tirées. C'est précisément là que vit un décalage
  d'une unité.
* `Expect.EveryDraw(generator, invariant)` — un générateur est une recette, pas une
  valeur : un seul tirage par cas ne teste presque rien de son aléa. Huit tirages par
  cas, sur cent cas, est la valeur par défaut.
* `Expect.Draws(generator, count)` — quand la propriété raisonne sur un lot
  (distinction, atteignabilité) plutôt que sur chaque valeur isolément.

Les règles qui gardent une propriété honnête :

* **Affirmez les types d'exception, jamais le texte des messages.** Les messages sont
  sensibles au sens d'application et changeront ; cette assertion appartient à un
  exemple.
* **Sachez quand l'exception est levée.** Les conflits sont levés à *l'appel* fluide,
  pas à `Generate()`. La validation des arguments précède la détection de conflit et
  l'emporte quand les deux s'appliqueraient.
* **Gardez les coins dégénérés** que vos arguments générés peuvent produire :
  intervalle vide, intervalle épinglé, cardinalité nulle, vivier épuisé. Soit le
  générateur les évite, soit le prédicat s'y ramifie.
* **Figez une graine pour tout ce qui est statistique.** « Les deux moitiés sont
  atteintes », « `null` finit par être tiré » sont probabilistes. Sous graine figée
  elles sont déterministes ; sans elle, elles deviennent instables. Dites-le en
  commentaire.
* **Restez rapide.** Cent cas fois huit tirages font déjà huit cents tirages. Plafonnez
  longueurs et cardinalités dans les dizaines, pas les milliers.

## Avant de pousser

* `dotnet test JustDummies.PropertyTests` et `dotnet test JustDummies.UnitTests`.
* Les segments du plancher, que la CI exécute et qu'un `dotnet test` ordinaire ignore :
  `dotnet build JustDummies.PropertyTests -c Release -f net472 -p:EnableNet472Floor=true`.
  Tout ce qui utilise une API .NET 8+ appartient à `ModernTypeInvariantProperties.cs`,
  que le fichier projet exclut de ce segment.
* **Vérifiez que votre propriété peut échouer.** Une propriété qui rend `true` pour la
  mauvaise raison passe en silence et ne prouve rien. Cassez-la volontairement une fois
  — inversez la comparaison, retirez une borne — constatez qu'elle rougit, puis
  remettez-la. C'est la seule défense bon marché contre une suite verte parce qu'elle
  n'affirme rien.
