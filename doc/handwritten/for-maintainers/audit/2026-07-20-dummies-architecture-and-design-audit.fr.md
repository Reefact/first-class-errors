# Dummies — Audit d'architecture et de conception

🌍 **Langues :**  
🇫🇷 Français (ce fichier) | 🇬🇧 [English](./2026-07-20-dummies-architecture-and-design-audit.md)

**Date :** 2026-07-20
**Révision auditée :** `3bf89e3` (sommet de `main` au moment de l'audit)
**Périmètre :** la seule bibliothèque `Dummies` — `Dummies/`, `Dummies.UnitTests/`, son outillage de
garde (`tools/dummies-check/`, `.github/workflows/dummies.yml`), sa documentation, et les ADR qui la
gouvernent.
**Statut :** consultatif. Conformément à la convention du dépôt (ADR-0004), cet audit produit des
recommandations, jamais des bloqueurs ; toute modification d'ADR proposée est un brouillon que
`@reefact` accepte ou rejette.

**Méthode.** L'intégralité du code de la bibliothèque (~8 700 lignes réparties sur 54 fichiers C#) et de
la suite de tests (~2 500 lignes, 17 fichiers) a été lue ; les 26 ADR ont été classées par
applicabilité et les 8 applicables revues à la fois pour leur qualité intrinsèque et pour la conformité
de l'implémentation ; les constats ont été vérifiés de façon contradictoire face au code, et les trois
défauts de comportement rapportés ci-dessous ont été **reproduits indépendamment à l'exécution** contre
la bibliothèque compilée. La suite de tests unitaires complète a été exécutée : **222/222 réussis**
(`dotnet test Dummies.UnitTests`, exécuteur net10.0). Les jugements sont calibrés sur les objectifs
affichés de la bibliothèque — des tests lisibles, des données de test expressives, un déterminisme
optionnel, une API fluide et découvrable, la simplicité — et délibérément *pas* sur les objectifs des
frameworks de test par propriétés ou de fuzzing, ce que cette bibliothèque n'est explicitement pas.

---

## 1. Résumé exécutif

Dummies est une jeune bibliothèque construite avec un niveau d'exigence inhabituel. Son idée
architecturale centrale — projeter chaque type discret dans un espace ordinal 64 bits partagé pour
qu'un seul moteur possède les bornes, les exclusions, la détection de conflits et l'échantillonnage de
treize générateurs à la fois — est élégante et correctement exécutée. Sa discipline de messages
d'erreur (chaque contrainte contradictoire nomme les *deux* côtés, chaque échec de génération nomme la
graine qui le rejoue) surpasse celle de la plupart des bibliothèques matures du domaine. Sa base d'ADR
est exemplaire : les décisions sont consignées avec des contraintes honnêtes, de vraies alternatives et
des compromis chiffrés.

L'audit a néanmoins trouvé **trois véritables défauts de comportement**, tous reproduits à l'exécution :

1. **Critique — `AnyDecimal` ne peut jamais générer la moitié haute de sa plage.** Une fraction censée
   être uniforme dans [0, 1) est construite à partir de trois tirages de 31 bits divisés par un
   dénominateur de 96 bits, et plafonne près de 0,5 ; `Any.Decimal().Between(0m, 100m)` ne dépasse
   jamais ~49,9999 (`DecimalIntervalSpec.cs:145`).
2. **Majeur — le « nudge » d'exclusion de `AnySingle`/`AnyHalf` se bloque.** La marche
   d'évitement des collisions avance d'un ulp de *double* au lieu de l'ulp du type, si bien que la
   quantification retombe sur la même valeur et qu'une spécification satisfiable comme
   `Any.Half().Between((Half)1f, (Half)1.001f).DifferentFrom((Half)1f)` lève une
   `AnyGenerationException` pour ~la moitié des graines (`ContinuousIntervalSpec.cs:189`).
3. **Majeur — une plage de classe regex se terminant à `￿` boucle à l'infini.** La boucle
   d'expansion de la classe incrémente un `char` 16 bits qui reboucle à `0xFFFF`, de sorte que
   `Any.StringMatching(@"[ -￿]")` ne rend jamais la main (`RegexParser.cs:398`).

Les trois partagent une cause racine qu'il vaut la peine de nommer : **la suite de tests vérifie
l'appartenance, jamais l'atteignabilité.** Les tests vérifient que les valeurs générées satisfont les
contraintes ; aucun test ne vérifie que le domaine déclaré est atteignable dans son entier, ni qu'une
spécification déclarée satisfiable génère effectivement. C'est l'angle mort précis d'une suite par
ailleurs bien conçue, et le combler compte davantage que n'importe quel correctif isolé.

Un fait de cadrage atténue considérablement tout cela : **Dummies n'a jamais été publiée.** Il n'y a
aucun tag `dum-v*` ; le changelog ne contient qu'une section *Unreleased* vide. Chaque défaut ci-dessus
peut être corrigé, et chaque contrat décidé, à coût de compatibilité nul. La recommandation-phare de
cet audit est de traiter la fenêtre pré-1.0 comme l'a fait l'ADR-0020 — le moment le moins cher pour
décider — et de solder les points des §11–§12 avant la première publication.

Au-delà des défauts, les constats significatifs sont : la surface `Any`/`AnyContext` recopiée à la main
et les quatorze générateurs numériques clonés ne portent **aucun garde-fou de parité** (et la dérive de
documentation a déjà commencé) ; le **contrat de déterminisme présente des lacunes de documentation**
(des tirages concurrents dans un même scope à graine annulent silencieusement la rejouabilité ; la
stabilité des graines entre versions n'est ni promise ni écartée ; l'ancrage ADR du contrat a été perdu
lorsque l'ADR-0006 a été remplacée) ; la **cible netstandard2.0 n'est jamais exécutée par la propre
suite de tests de Dummies** (seulement transitivement, via le job de plancher de FirstClassErrors) ; et
il n'existe **aucune référence utilisateur de la surface de contraintes** — le README du dépôt ne
mentionne même pas le paquet. L'analyse des manques (§10) juge la couverture de types réellement
complète au regard de la philosophie de la bibliothèque ; les deux absences qui méritent la qualité de
surprenantes sont un combinateur de choix *de premier niveau* (`Any.OneOf<T>(params T[])` /
`Any.ElementOf(...)`) et des contraintes d'exclusion sur `AnyString` — le seul générateur scalaire qui
en soit dépourvu.

## 2. Évaluation globale

**Verdict : une bibliothèque pré-publication très solide — l'architecture et le processus sont ses
forces ; le test de correction dans l'espace des valeurs est sa seule faiblesse systémique.**

Jugée domaine par domaine face aux objectifs affichés :

| Domaine | Évaluation |
|---|---|
| Architecture | Excellente. Découpage en couches propre (générateurs publics → specs internes → échantillonnage), un moteur ordinal partagé, une séparation de moteurs fondée, des points de composition sur une interface minuscule. |
| Conception d'API | Excellente, avec une poignée d'asymétries d'apparence délibérée mais non consignées (§8). |
| Diagnostics d'erreur | Exceptionnels — la force signature de la bibliothèque. |
| Déterminisme | Conception saine, correctement implémentée au niveau `AsyncLocal`/`ExecutionContext` ; contrat sous-documenté à ses bords (§7.3). |
| Correction | Trois défauts reproduits, deux d'entre eux exactement dans le code qu'une suite fondée sur la seule appartenance ne peut pas voir (§4.1). |
| Stratégie de test | Bien formée (comportement d'abord, boîte noire, adossée à un oracle pour la regex, à l'abri des tests instables) mais aveugle à l'atteignabilité (§9.3). |
| Documentation | Docs XML remarquables ; documentation utilisateur maigre et difficile à trouver (§4.4). |
| Maintenabilité | La duplication est importante mais disciplinée (zéro erreur de copier-coller trouvée dans les familles de clones) ; le risque est la dérive non gardée, pas la pourriture présente (§9). |
| Base d'ADR | Qualité exemplaire ; deux lacunes structurelles — le contrat de déterminisme et le moteur ordinal n'ont pas d'ADR propre (§5). |

La forme d'ensemble est celle d'une bibliothèque écrite avec grand soin par un petit nombre de mains :
les *décisions* sont systématiquement justes et systématiquement consignées, tandis que les filets de
sécurité qui protègent ces décisions des mains futures (gardes de parité, tests d'atteignabilité,
références d'API) ne sont pas encore en place. Le pré-1.0 est le moment de les installer.

## 3. Forces

Elles sont méritées, vérifiées face au code, et à préserver délibérément.

### 3.1 L'unification par l'espace ordinal

Chaque type discret — les dix entiers de 64 bits ou moins, `DateTime`, `DateTimeOffset`, `TimeSpan`,
`DateOnly`, `TimeOnly` — se projette en préservant l'ordre dans un espace ordinal non signé 64 bits
(`OrdinalMapping.FromInt64` inverse le bit de signe ; `OrdinalIntervalSpec.cs:9-23`) et partage **un
seul** moteur pour les bornes, les listes d'autorisation, les exclusions, la détection de conflits, la
cardinalité et l'échantillonnage (`OrdinalIntervalSpec`). L'algorithme d'exclusion est exact — un index
tiré est projeté sur le k-ième ordinal non exclu en une seule passe sur une liste d'exclusions triée
(`OrdinalIntervalSpec.cs:194-202`) — de sorte que la génération est un tirage unique, jamais
tirer-puis-recommencer. Un correctif à un message de conflit ou à un cas limite atteint tous les
générateurs discrets simultanément. C'est le bon niveau où appliquer DRY : la *logique* est partagée
tandis que les fines façades par type restent simples et lisibles.

### 3.2 Provenance des contraintes et validation immédiate

Chaque borne se souvient de la chaîne de contrainte qui l'a posée (`"Between(1, 6)"`, `"Positive()"`),
de sorte qu'un conflit nomme **les deux** côtés au moment de la déclaration :

```
Cannot apply LessThan(10) because GreaterThan(100) already requires values greater than or equal to 101.
```

La discipline tient uniformément sur chaque générateur et chaque moteur de spécification, y compris des
validations transversales qu'on ne s'attendrait pas à trouver (un jeu de caractères `Numeric()` rejette
un préfixe contenant des lettres, en nommant le caractère fautif — `StringSpec.cs:254-275`). Combinée à
la vérification immédiate de satisfiabilité (« un générateur qui existe peut toujours générer »), un
`Arrange` impossible échoue à la ligne qui l'a écrit, pas à un tirage ultérieur. C'est la signature de
la bibliothèque, et elle est exécutée avec constance.

### 3.3 La machinerie de déterminisme est bien faite au niveau difficile

La pièce maîtresse — les générateurs stockent une `RandomSource` et ne résolvent `.Current` qu'au
moment de `Generate()` — est ce qui permet à une recette construite hors d'un `Any.Reproducibly(...)`
de générer de façon déterministe à l'intérieur (`RandomSource.cs:3-9`). La sémantique de scope
`AsyncLocal` a été examinée de près et tient : la restauration par `using` de la surcharge synchrone est
correcte ; la mutation `UseSeed` de la surcharge asynchrone ne peut pas fuir vers l'appelant (les
mutations d'`ExecutionContext` d'une méthode asynchrone ne reviennent pas en arrière) ; l'imbrication
restaure le scope externe intact ; `ConfigureAwait(false)` est sans effet sur la circulation de
l'`ExecutionContext`. La graine est rapportée de bout en bout : une fabrique utilisateur qui lève une
exception dans `.As(...)` produit une `AnyGenerationException` nommant la valeur générée *et* la graine
(`AnyDerivation.cs:59-73`) ; une saturation de collection distincte fait de même
(`CollectionState.cs:246-254`).

Deux pièges subtils liés à la double cible ont été anticipés et documentés à l'endroit exact du danger :
l'échantillonneur inclusif de `RandomSampling` n'est délibérément *pas* nommé `NextInt64` parce que, sur
la cible net8.0, la méthode d'instance à borne exclusive du framework l'emporterait dans la résolution
de surcharge et changerait silencieusement la sémantique (`RandomSource.cs:129-137`) ; et l'extension
`OrNull` est scindée en deux classes parce que des surcharges d'un même nom contraintes l'une à
`struct` et l'autre à `class` entreraient en collision (`NullableExtensions.cs:47-52`). C'est le genre
de soin qui ne se rattrape pas après coup.

### 3.4 Des échappatoires bornées partout — aucune reprise non bornée nulle part

L'affirmation « construit pour satisfaire, jamais générer-puis-filtrer » résiste à l'examen avec trois
exceptions honnêtes, consignées en ADR, chacune *bornée* : le tirage dédupliqué des collections
distinctes (budgeté, généreux façon collectionneur de coupons, remis à zéro à chaque progrès —
`CollectionState.cs:236-244`), le nudge d'exclusion du domaine continu (marche jusqu'à la valeur
représentable voisine), et l'évitement de collision d'`AnyGuid` (une incrémentation avec retenue sur
toute la largeur qui se termine de façon prouvable — `AnyGuid.cs:27-36`). Chaque mode d'échec produit un
message actionnable, porteur de graine, plutôt qu'un blocage.

### 3.5 Le soin aux bords

De petites choses qui révèlent le niveau d'exigence : `AnyDateTime.OneOf` se souvient des valeurs
originales de l'appelant pour que l'aller-retour ordinal ne normalise pas silencieusement le
`DateTimeKind` (`AnyDateTime.cs:124-131`) ; l'agencement d'une collection est mélangé par Fisher-Yates
pour qu'une collection factice n'annonce jamais un invariant de position sur lequel un test pourrait
s'appuyer par accident (`CollectionState.cs:46-53`) ; `CountSpec` et `StringSpec` saturent au lieu de
déborder sur des minima déclarés énormes ; la covariance d'`IAny<out T>` fait que les interfaces de
collection en lecture seule (`IReadOnlyList<T>`, etc.) sont servies gratuitement.

### 3.6 Le sous-système regex est bien bâti pour le périmètre décidé

L'analyseur syntaxique à descente récursive écrit à la main (`RegexParser.cs`, 457 lignes — le plus gros
bloc de logique unique de la bibliothèque) est bien structuré, commenté sur le « pourquoi » et
discipliné sur sa taxonomie de rejet à deux canaux : `ArgumentException` pour les motifs malformés,
`UnsupportedRegexException` nommant la construction et la position pour ceux qui sont bien formés mais
non réguliers. La suite de tests valide les chaînes générées contre le **vrai moteur regex de .NET
utilisé comme oracle** sur un corpus à graine fixe — exactement la bonne façon de tester un générateur.
(Les défauts trouvés à ses bords sont catalogués aux §4.1 et §4.2 ; ils ne changent rien à l'évaluation
selon laquelle l'approche de l'ADR-0025 était saine et honnêtement argumentée.)

### 3.7 Empaquetage, frontière et processus

La frontière zéro-dépendance, agnostique aux erreurs, est appliquée de trois façons : un commentaire de
`.csproj` énonçant la règle, un test d'architecture fondé sur l'intention qui échoue à toute référence
d'assemblage hors BCL (`ArchitectureTests.cs:27-37`), et le garde-fou d'artefact empaqueté
`dummies-check` — un vrai programme consommateur, exécuté en CI contre l'*artefact packé* pour chaque
cible, qui prouve que l'asset net8.0 porte les générateurs modernes, que l'asset netstandard2.0 ne les
porte pas, que les contraintes et conflits se comportent bien, et que des contextes à même graine
rejouent (`tools/dummies-check/Program.cs`). L'empaquetage lui-même est de niveau production : SourceLink
avec sources non suivies embarquées, builds CI déterministes, symboles snupkg, un SBOM SPDX embarqué au
moment du pack, des assets de release à provenance attestée (`Directory.Build.props:10-23`,
`Dummies.csproj:60-66`, `release.yml`). La base d'ADR qui consigne tout cela est discutée au §5 — c'est
une force en soi.

### 3.8 La philosophie d'API est cohérente et documentée là où l'utilisateur regarde

L'idée « les contraintes expriment ce que le code environnant *requiert*, jamais ce que le test
assertit » est énoncée sur le point d'entrée, sur chaque générateur, dans le README du paquet et dans le
guide utilisateur — la même phrase, délibérément. Le parti pris de ne pas offrir de contraintes
relatives à l'horloge (`AnyDateTime` n'a pas d'`InThePast()`) est documenté à chaque endroit où
l'utilisateur le chercherait, avec la justification de reproductibilité attachée. Les quasi-synonymes
révélateurs d'intention sont expliqués honnêtement : `DifferentFrom(x)` est documenté comme
sémantiquement `Except(x)` avec un nom qui porte l'intention ; `Containing` (une valeur connue
maintenant) vs `ContainingAny` (un générateur tiré au moment de la construction) est une distinction
réellement utile.

## 4. Faiblesses

Classées par sévérité. Les points 4.1 et 4.3 sont ceux qui devraient conditionner une première
publication.

### 4.1 Défauts de comportement reproduits

**(a) `AnyDecimal` n'atteint jamais la moitié haute d'une plage — critique.**

`DecimalIntervalSpec.cs:144-149` :

```csharp
// A uniform-enough fraction in [0, 1): 93 random bits over the full decimal mantissa scale.
decimal fraction = new decimal(random.Next(), random.Next(), random.Next(), false, 28) / MaxFraction;
decimal mid       = _min / 2 + _max / 2;
decimal half      = _max / 2 - _min / 2;
decimal candidate = Clamped(mid + (fraction * 2 - 1) * half);
```

`Random.Next()` renvoie un `int` non négatif, si bien que le bit de poids fort de **chaque limbe de
32 bits** de la mantisse de 96 bits est toujours zéro, tandis que `MaxFraction` est le maximum *plein*
de la mantisse 96 bits (`7,9228…`, `DecimalIntervalSpec.cs:14`). La fraction vit donc dans
[0, ~0,49999986], pas [0, 1) ; `(fraction * 2 - 1)` vit dans [−1, ~0) ; et chaque candidat atterrit dans
`[min, mid)`. Le maximum inclusif documenté sur `AnyDecimal.Between` (`AnyDecimal.cs:112`) est
inatteignable — tout comme tout ce qui est au-dessus du milieu. Reproduit indépendamment pour cet
audit : le maximum de 200 000 tirages de `Any.Decimal().Between(0m, 100m)` valait **49,99992…**.

Pourquoi cela compte au-delà de l'évidence : un test utilisant `Any.Decimal().Between(0m, 100m)` pour
exercer « n'importe quel pourcentage valide » n'exerce silencieusement jamais 50–100 — la promesse
centrale de la bibliothèque (« arbitraire mais valide, pour que les hypothèses cachées ressortent ») est
retournée en une hypothèse cachée qui lui est propre. Le correctif est petit : construire la fraction à
partir de 96 bits véritablement uniformes, par exemple :

```csharp
// after: 12 random bytes fill all three 32-bit limbs uniformly
// (the decimal ctor reads the int limbs as raw 32-bit patterns)
byte[] limbs = new byte[12];
random.NextBytes(limbs);
decimal fraction = new decimal(
    BitConverter.ToInt32(limbs, 0),
    BitConverter.ToInt32(limbs, 4),
    BitConverter.ToInt32(limbs, 8),
    false, 28) / MaxFraction;
```

(toute construction remplissant les 96 bits de mantisse de façon uniforme convient — les trois appels
`Next()` actuels fixent à zéro le bit de poids fort de chaque limbe et ne peuvent jamais tirer un limbe
de `2^31−1`), puis ajouter le test d'atteignabilité du §11 point 2. À noter : le commentaire lui-même
(« 93 random bits over the full mantissa scale ») documente une intention que le code n'honore pas — et
même 93 bits bien placés n'atteindraient pas l'octant supérieur d'un dénominateur de 96 bits.

**(b) Le nudge d'exclusion de `AnySingle`/`AnyHalf` se bloque sur des specs satisfiables — majeur.**

`ContinuousIntervalSpec.cs:188-198` : quand une valeur tirée entre en collision avec un point exclu, la
marche avance avec le `NextUp` **statique, en espace double** (ligne 189) au lieu du lambda `_nextUp`
*conscient du type* que `AnySingle`/`AnyHalf` fournissent précisément pour avancer dans leur propre
échelle de valeurs représentables (`AnySingle.cs:20`, `AnyHalf.cs:22`) — et que les chemins à borne
exclusive utilisent déjà correctement (lignes 120, 125). Un ulp de double au-dessus d'un `float`/`Half`
représentable se re-quantifie vers la même valeur, l'échappatoire `next > _max` de la ligne 190 est
inatteignable (`Quantized` borne d'abord à `_max`, lignes 203-209), si bien que le budget de 128 pas se
consume et qu'une spec *satisfiable* lève. Reproduit indépendamment :
`Any.Half().Between((Half)1f, (Half)1.001f).DifferentFrom((Half)1f).Generate()` a levé une
`AnyGenerationException` pour **250 graines sur 500** ; le scénario `AnyDouble` identique ne lève jamais
(sa quantification est l'identité). Le correctif tient en un jeton — `Quantized(_nextUp(candidate))` —
plus un test de non-régression par type continu.

Ce défaut mérite une note de conception : c'est exactement la classe d'échec que prédit l'architecture
de la bibliothèque. Le moteur a été paramétré par des lambdas `quantize`/`nextUp` *parce que* les types
étroits doivent avancer dans leur propre échelle ; un site d'appel dans le même fichier a oublié le
paramètre. Une suite de scénarios paramétrée et transverse aux moteurs (§9.3) est la réponse
structurelle.

**(c) Une plage de classe se terminant à `￿` boucle à l'infini — majeur.**

`RegexParser.cs:398` :

```csharp
for (char character = low; character <= high; character++) { set.Add(character); }
```

Quand `high == '￿'` (atteignable via l'échappement supporté `\uHHHH`), le `char` 16 bits reboucle à
`0x0000` et `character <= high` est toujours vrai. Reproduit indépendamment :
`Any.StringMatching(@"[ -￿]")` n'a pas rendu la main en cinq secondes (blocage dur), alors que le même
motif est une regex .NET valide. Un blocage au moment de la déclaration est le pire mode d'échec que
cette bibliothèque puisse exhiber — son identité est d'*échouer vite avec une cause nommée*. Correctif :
garder le rebouclage (`if (character == high) break;` dans la boucle, ou itérer sur un `int`), et
répliquer le contrôle dans la boucle jumelle privée `RegexAlphabet.Range` (`RegexAlphabet.cs:66-71`)
par défense en profondeur.

**(d) Les groupes d'équilibrage et les noms de groupe invalides sont acceptés silencieusement — majeur,
dans le sens qui rompt le contrat.**

`SkipGroupName` (`RegexParser.cs:295-300`) balaie jusqu'au terminateur sans aucune validation. En
conséquence `(?<-a>x)` — un *groupe d'équilibrage* (balancing group), non régulier, de la même famille
que les références arrières que la bibliothèque rejette fièrement — est traité comme un groupe nommé
ordinaire : `Any.StringMatching(@"(?<a>y)?(?<-a>x)")` génère `"x"`, que le vrai moteur ne reconnaît
**pas** (vérifié : le langage du motif est exactement `{"yx"}`). Les noms de groupe invalides
(`(?<a b>x)`) sont de même acceptés là où .NET les rejette. C'est le seul endroit trouvé par l'audit où
la promesse signature de la bibliothèque — *« une erreur claire vaut mieux qu'une valeur qui ne
correspond pas réellement »* (ADR-0025) — est rompue. Le correctif est local : valider le nom capturé
(rejeter `-` en `Unsupported("a balancing group …")`, rejeter les caractères non-mot en
`Malformed(...)`).

**(e) Défauts mineurs dans le même sous-système.** L'exception de limite de génération accuse « a nested
unbounded quantifier » même quand la vraie cause est un quantificateur *borné* de grande taille
(`(a{1000}){1000}` — le message affirme un diagnostic faux ; `RegexNode.cs:31-37`) ; quelques motifs que
le vrai moteur accepte sont refusés par prudence (`^*`, `abc$$` — tandis que `^^abc` est accepté, une
asymétrie évitable ; un `-[` en tête de classe est mal lu comme une soustraction) ; et une classe
négative bien formée dont les membres sortent de l'univers imprimable est mal classée en *malformée* au
lieu de *non supportée*. Tous ces cas échouent dans le sens sûr (refus, jamais mauvaise génération) et
sont cosmétiques à côté de (c) et (d).

### 4.2 L'affirmation « ASCII imprimable » est exagérée en trois endroits

`RegexAlphabet.cs:3-9`, `AnyPattern.cs:15-16` et `Any.cs:70` affirment tous que chaque terminal se
résout en ASCII imprimable (0x20–0x7E). Le code — correctement — émet exactement les caractères que le
motif exige : `\t`, `\a`, `\cA`, `\0` et les littéraux `\uHHHH` peuvent être non imprimables ou
non-ASCII, et le propre test de la bibliothèque l'assertit (`AnyPatternTests` — `\a` → U+0007). La
restriction ne s'applique vraiment que là où le motif laisse le caractère *libre* (raccourcis, le point,
classes négatives). Comme l'ADR-0025 déclare explicitement l'univers de caractères comme un comportement
sur lequel les consommateurs peuvent s'appuyer, les trois emplacements de doc devraient le dire
précisément (§11 point 6).

### 4.3 Des surfaces recopiées à la main sans garde-fou de parité, et la dérive a déjà commencé

Deux structures miroir doivent s'accorder méthode par méthode, et rien ne contrôle ni l'une ni l'autre :

* **`Any` ↔ `AnyContext`** : chaque point d'entrée scalaire existe deux fois (21 sur la cible
  netstandard2.0, 26 sur net8.0, en comptant les deux surcharges de `StringMatching`) —
  `Any.cs:54-317` face à `AnyContext.cs:48-296`. Le miroir est une conception légitime (la composition
  et les collections ne sont délibérément *pas* recopiées — elles héritent d'un contexte via les sources
  de leurs opérandes, ce qui est élégant), mais un nouveau type scalaire ajouté à `Any` et oublié sur
  `AnyContext` compilerait, passerait les 222 tests, et livrerait un trou dans la surface déterministe.
  La dérive de formulation est déjà visible à l'intérieur d'`AnyContext` lui-même (deux tournures
  différentes du déterminisme selon les fabriques ; sa doc de `Guid()` mentionne `Any.Reproducibly`,
  qu'un contexte fixe ignore par conception).
* **Les quatorze générateurs numériques** sont des clones identiques à l'octet près modulo substitution
  de type (~2 450 lignes ; le quatuor signé, le quatuor non signé, le trio continu et la paire large ;
  les cinq générateurs temporels suivent le même patron pour ~800 lignes de plus). Au crédit des
  familles de clones, un balayage scripté n'a trouvé **aucune erreur de copier-coller** dans le code
  lui-même — mais trois résumés XML disent « Same constraint algebra as `AnyInt32` » sur des générateurs
  où c'est littéralement faux (les types non signés n'ont pas `Positive`/`Negative` ; les types
  temporels renomment la famille de bornes), et trois DisplayName de test affirment encore que les
  générateurs « convert implicitly to their value type »
  (`AnyContinuousTests.cs:108`, `AnySignedIntegerTests.cs:87`, `AnyUnsignedIntegerTests.cs:76`) — des
  conversions que l'ADR-0020 a supprimées. Un commentaire périmé dans `SeedReproducibilityTests.cs:17-18`
  explique du code par ces mêmes conversions supprimées.

L'absence de gardes est le constat ; l'analyse d'atténuation et la recommandation (tests de parité par
réflexion, *pas* une classe de base générique) sont au §9.2.

### 4.4 La documentation n'atteint ni celui qui découvre ni l'utilisateur avancé

* Le **README du dépôt ne mentionne jamais Dummies** (vérifié : zéro occurrence), alors que le README du
  paquet renvoie au dépôt pour la « documentation complète ». Celui qui découvre le paquet sur NuGet
  arrive sur une page d'accueil portant sur une autre bibliothèque ; ce qui ressemble le plus à un guide
  Dummies (`ArbitraryTestValues.en.md`) est un guide d'intégration de FirstClassErrors.Testing qui
  renvoie lui-même à « documented with Dummies itself » — une référence circulaire.
* **Aucune référence utilisateur ne documente la surface de contraintes par générateur.** Où
  l'utilisateur apprend-il que `Except`/`OneOf`/`DifferentFrom` existent sur les numériques, que
  `WithLengthBetween` existe, que `ContainingAny` diffère de `Containing`, ou quel dialecte regex
  `StringMatching` supporte ? Aujourd'hui : seulement IntelliSense, un générateur à la fois. Le propre
  suivi de l'ADR-0025 (« documenter le dialecte supporté ») est toujours ouvert.
* La **surprise du vide-par-défaut** (une collection non contrainte peut avoir 0 élément, une chaîne non
  contrainte peut être vide) est bien documentée dans les remarques XML mais absente du README du paquet,
  là où un utilisateur qui survole en profiterait le plus — c'est un choix délibéré, porteur de
  philosophie (« un test qui itère zéro fois sur une collection non contrainte, c'est une hypothèse
  cachée qui ressort ») et il mérite d'être annoncé comme tel.

### 4.5 Lacunes du contrat de déterminisme (documentation, pas implémentation)

Détaillé au §7.3 : des tirages concurrents dans un même scope à graine annulent silencieusement la
rejouabilité (et exposent un `System.Random` non thread-safe à une course) — documenté nulle part ; les
rapports de graine peuvent nommer une graine erronée ou inapplicable pour les compositions à contexte
fixe et à sources mixtes ; la stabilité de la séquence de graines entre versions et entre TFM n'est ni
promise ni écartée ; et le contrat entier a perdu son ancrage ADR lorsque l'ADR-0006 a été remplacée.

### 4.6 La cible netstandard2.0 n'est jamais exécutée par la propre suite de Dummies

`Dummies.UnitTests` ne cible que net10.0. L'assemblage netstandard2.0 — celui que chargeront les
consommateurs .NET Framework — n'est exercé que *transitivement* : le job de plancher de
FirstClassErrors (`ci.yml:98-115`) exécute `FirstClassErrors.UnitTests` sur net472, qui prépare ses
`Arrange` avec `Dummies.Any` via référence de projet et via les fabriques de Testing, si bien que
Dummies se charge et génère bien sur le vrai CLR .NET Framework — mais sa propre suite de contrat de
222 tests (oracle regex, détection de conflits, gating de distinction, reproductibilité de graine) n'y
tourne jamais, et l'égalité même-graine-mêmes-valeurs entre les deux assets empaquetés n'est assertée
nulle part. Le dépôt possède déjà exactement la machinerie nécessaire (`build/Net472TestFloor.props`,
utilisée par `FirstClassErrors.UnitTests`) ; l'étendre à `Dummies.UnitTests` (en conditionnant hors
scope les tests net8-only) est mécanique. Voir la conformité à l'ADR-0022, §6.

### 4.7 Garde-fous d'ingénierie de release pas encore installés

Aucune référence d'API publique (`Microsoft.CodeAnalysis.PublicApiAnalyzers`), aucun
`EnablePackageValidation`/ApiCompat. Le changelog engage Dummies vers la gestion sémantique de version
tandis que l'audit lui-même démontre que la surface d'API est recopiée à la main et dérive déjà dans la
documentation ; la détection de changements cassants contre une référence publiée est le mécanisme
complémentaire que les tests de parité ne peuvent pas remplacer (une surcharge supprimée ou un type de
retour rétréci passe un test de miroir). L'avant-première-publication est le moment le moins cher pour
installer les deux. Un commentaire périmé trouvé ici : `Directory.Build.props:3-9` dit que le dépôt
livre « FirstClassErrors and FirstClassErrors.Testing » — il omet Dummies, le paquet même que ces
propriétés de pack gouvernent désormais aussi.

## 5. Revue des ADR

Dix-huit des vingt-six ADR ne concernent pas Dummies (elles nomment les analyseurs, le request binder,
l'outillage GenDoc/CLI, l'API Outcome, ou le processus du dépôt). Huit s'appliquent, et leur qualité a
été revue individuellement. Le niveau d'ensemble est assez élevé pour le dire simplement : cette base
d'ADR est un modèle du genre. Les décisions portent des contraintes honnêtes, des alternatives
réellement considérées, des inconvénients chiffrés, et des suivis qui ont effectivement été exécutés.

### ADR-0006 — Fournir des valeurs de test arbitraires depuis une source unique semable *(Remplacée)*

**Qualité : exemplaire, historiquement.** Les contraintes étaient réelles (promesse zéro-dépendance,
sûreté des tests parallèles netstandard2.0 sans `Random.Shared`), les quatre alternatives ont été
pesées équitablement, et ses suivis (extraire le moteur quand un second consommateur apparaît ; envisager
un adaptateur xUnit) ont été honorés ou consciemment différés. Son analyse du risque de collision du
défaut non semé est exactement à la bonne profondeur. **Problème :** sa mise en remplacement a créé une
lacune — voir « lacunes structurelles » plus bas.

### ADR-0011 — Héberger Dummies comme paquet autonome *(Acceptée)*

**Qualité : bonne.** Le raisonnement nom/identité/frontière est sain et la règle de non-référence est
vérifiée par machine. Deux points de précision. Premièrement, l'invariant *appliqué* est plus fort que
celui *consigné* : le test d'architecture interdit **toute** référence hors BCL
(`ArchitectureTests.cs:27-37`), et l'ADR-0025 s'appuie sur une « identité zéro-dépendance … la frontière
est vérifiée par machine (ADR-0011) » — mais le texte de décision de l'ADR-0011 n'interdit que de
référencer des *projets FirstClassErrors*. La règle du zéro-dépendance-*tierce*, porteuse pour tout
l'argument de l'ADR-0025, n'est consignée nulle part comme décision. Deuxièmement, les alternatives ne
pèsent jamais les risques de l'identifiant NuGet ultra-générique `Dummies` (squattage/collision/
recherchabilité) — une identité de paquet que l'ADR elle-même qualifie de coûteuse à renommer. Aucun de
ces points ne change la décision ; les deux méritent une ligne au dossier.

### ADR-0013 — Gater les collections distinctes par cardinalité, sinon par tirage borné *(Acceptée)*

**Qualité : remarquable.** L'argument de solidité — ne compter que les éléments que le générateur doit
fournir, créditer les valeurs `Containing` hors de son domaine, traiter les tirages opaques
`ContainingAny` de façon conservatrice, laisser le tirage borné être le filet de sécurité final — est
énoncé dans le document et reflété de façon prouvable dans le code
(`CollectionState.Validate`/`CardinalityCap`/`FixedOutsideCount`). La section des risques anticipe même
un mauvais réglage du budget et enjoint de « réviser sur preuves plutôt que de décrire l'échec comme
impossible ». **Problème (partagé avec l'ADR-0015) :** elle diffère « l'interface exacte de l'indice,
l'état de collection, le budget de tirage, la charge utile d'exception et la propagation de graine » vers
la référence d'implémentation — mais la section Dummies de cette référence
(`adr-implementation-reference.md:58-68`) ne consigne aucune de ces spécificités (pas de chiffres de
budget, pas de charge utile d'exception, pas de règle de propagation de graine). Le renvoi promet plus
que la destination ne contient ; soit enrichir la référence, soit adoucir le renvoi.

### ADR-0015 — Plafonner Any.Combine à l'arité huit *(Acceptée)*

**Qualité : bonne.** Honnête sur le caractère heuristique du plafond, avec une échappatoire définie
(ajouter des arités de façon compatible via une nouvelle décision sur preuves). Les alternatives sont
réelles. Le même point sur le renvoi à la référence d'implémentation que pour l'ADR-0013 s'applique.

### ADR-0020 — Matérialiser les dummies uniquement via Generate() *(Acceptée)*

**Qualité : exemplaire — le meilleur document de la base.** Preuves concrètes (les formes syntaxiques où
la conversion se comportait mal en silence, tirées de la suite elle-même), trois alternatives pesées
équitablement dont la voie analyseur qu'elle décline délibérément, coûts honnêtes, et l'argument de
timing pré-1.0 énoncé comme tel. Elle a de plus manifestement orienté des travaux ultérieurs (l'ADR-0026
réutilise à la fois son patron de raisonnement et son cadrage de risque). Aucune modification
recommandée.

### ADR-0022 — Fixer le plancher de support .NET Framework à 4.7.2 *(Acceptée)*

**Qualité : politique saine ; formulation de périmètre vieillie.** « Une promesse de compatibilité qui
n'est pas exercée ne peut pas fournir une frontière de support fiable » est le bon principe. Mais l'ADR
précède Dummies et parle des « bibliothèques `netstandard2.0` livrées » sans les nommer ; savoir si
Dummies est dans son périmètre relève désormais de l'inférence, et le job de plancher ne l'inclut pas
(§6). Quand le mainteneur touchera à nouveau à ce domaine, une clarification d'une ligne des paquets
couverts lèverait l'ambiguïté — ou la décision de plancher propre à Dummies pourrait chevaucher la
nouvelle ADR de déterminisme proposée ci-dessous.

### ADR-0025 — Générer des chaînes correspondantes depuis un sous-ensemble régulier maison *(Proposée)*

**Qualité : un dossier construire-ou-acheter d'une honnêteté inhabituelle.** Le rejet de Fare est
argumenté sur des motifs d'identité et de contrat d'erreur (abandon silencieux des constructions non
régulières vs refus de première classe), pas sur du FUD ; le cadrage « les constructions non régulières
sont impossibles pour *tout* générateur fini, donc le sous-ensemble n'est pas une coupe de confort » est
exactement juste ; la décision de générateur terminal est bien argumentée. **Problèmes :** (1) Elle est
toujours **Proposée** alors qu'elle est entièrement implémentée, livrée dans le README du paquet, et
*porteuse pour l'ADR-0026 Acceptée* (dont `ErrorCodeFactory` est bâti sur `StringMatching`) — tant que
le statut n'a pas basculé, une décision acceptée repose formellement sur une décision indécise. Le rôle
de l'audit est de le signaler ; seul `@reefact` bascule un statut. (2) La phrase de justification « les
terminaux tirent de l'ASCII imprimable » est imprécise — `\s` inclut la tabulation (0x09) et les
échappements explicites émettent exactement le caractère qu'ils nomment (§4.2) ; la formulation devrait
être corrigée *avant* l'acceptation, puisque l'ADR elle-même déclare l'univers comme un comportement
pertinent pour la compatibilité. (3) Elle cite « un test par propriété » contre le vrai moteur ; ce qui
existe est un test-oracle à graine fixe et corpus fixe dans le projet de tests unitaires — excellent,
mais pas par propriété ; le texte devrait dire ce qu'est le filet de sécurité.

### ADR-0026 — Rebaser les valeurs arbitraires du paquet de test sur Dummies *(Acceptée)*

**Qualité : un dossier de consolidation approfondi** — six vraies alternatives, la justification de
l'histoire à graine unique, un risque d'empaquetage intermédiaire honnête. **Deux dérives de
précision :** (1) le texte de décision dit que chaque fabrique expose « an `IAny<T>` generator through a
distinct method where composition is needed » — aucune fabrique n'expose une telle méthode aujourd'hui
(vérifié : zéro occurrence d'`IAny` dans les sources de `FirstClassErrors.Testing`). YAGNI défendable,
mais le texte se lit comme une forme d'API décidée, et un contrôle de conformité dans un an ne pourra pas
distinguer le report délibéré de la migration inachevée. (2) Sa clause de risque dit que le danger de
double assemblage existe « precisely because Dummies types appear in Testing's public API » —
aujourd'hui aucun n'y apparaît ; la prémisse est mal énoncée (le danger est réel pour d'autres raisons
tant que Dummies est livrée dans l'artefact). Comme les ADR acceptées ne sont jamais éditées en place,
les deux relèvent d'une courte note dans la référence d'implémentation.

### Lacunes structurelles de la base (recommandations de création)

1. **Le contrat de déterminisme de Dummies n'a pas d'ADR acceptée.** La source ambiante `AsyncLocal`,
   le `Reproducibly` optionnel, l'épinglage paresseux, le rapport de graine à l'échec — la garantie
   joyau de la couronne — a été décidée dans l'ADR-0006, désormais Remplacée *et* cadrée sur
   FirstClassErrors.Testing ; la décision de l'ADR-0026 porte sur le rebasage de Testing, pas sur le
   contrat propre de Dummies. Un futur mainteneur qui demande « pourquoi `AsyncLocal` et pas un
   paramètre ? pourquoi un `System.Random` mis en course est-il acceptable ? » ne trouve le raisonnement
   que dans un dossier remplacé. **Recommandation : rédiger une ADR Proposée** (« Dummies fournit des
   valeurs arbitraires depuis une source ambiante, semable, locale au contexte d'exécution, avec
   reproductibilité optionnelle ») reprenant la justification de l'ADR-0006 et réglant, dans le même
   document, les bords ouverts que cet audit a fait remonter : la sémantique de concurrence à flux
   logique unique, le point de couture fermé `IHasRandomSource`, et la politique de stabilité de graine
   entre versions (§7.3).
2. **L'architecture du moteur ordinal n'a pas d'ADR.** Un espace ordinal 64 bits partagé avec quatre
   moteurs par substrat arithmétique est une décision durable, questionnable-par-un-futur-mainteneur
   (pourquoi quatre moteurs ? pourquoi `decimal` n'est-il pas projeté en ordinal ?) qui ne vit
   aujourd'hui que dans des docs XML internes. Elle passe le propre test d'ADR du dépôt (« si
   l'implémentation changeait mais que la décision tenait… »). Une courte ADR Proposée corrigerait
   l'asymétrie avec des décisions bien plus petites (plafonds d'arité) qui, elles, ont eu des dossiers.

## 6. Conformité aux ADR

| ADR | Statut | Conformité de l'implémentation |
|---|---|---|
| 0006 (historique) | Remplacée | **Conforme et dépassée.** Le contrat de graine hérité (local au contexte, déterminisme optionnel, rapport de graine) est implémenté fidèlement ; Dummies ajoute l'`AnyContext` isolé que l'ADR d'origine ne faisait qu'anticiper. |
| 0011 | Acceptée | **Conforme.** Aucune référence à FirstClassErrors ; frontière vérifiée par machine (`ArchitectureTests`) ; identité autonome, train de release et docs en place. Note : l'application est *plus forte* que la décision consignée (§5). |
| 0013 | Acceptée | **Conforme, vérifiée en détail** — gate immédiat net des crédits `Containing` hors domaine, comptage conservateur de `ContainingAny`, arithmétique à l'abri du débordement, budget borné, les deux canaux d'échec. **Une déviation mineure :** le message de saturation promet *inconditionnellement* le rejeu `Any.Reproducibly({seed}, …)` (`CollectionState.cs:246-254` ; la garde `seed is not null` est du code mort — la graine n'y peut jamais être nulle). Pour un générateur d'éléments **étranger** dont les tirages ignorent la source ambiante, cette promesse est fausse ; l'ADR dit que les échecs sont « explicites et reproductibles ». Qualifier le message quand le générateur d'éléments ne porte aucune source de la bibliothèque. |
| 0015 | Acceptée | **Conforme exactement** — arités 2–8, pas plus ; suppressions localisées avec justifications renvoyant à l'ADR (`Any.cs:622-623`) ; plafond documenté sur la surcharge d'arité 8. |
| 0020 | Acceptée | **Pleinement conforme.** Aucune conversion implicite nulle part ; `Generate()` est la seule matérialisation ; générateurs vérifiés immuables (chaque méthode fluide renvoie une nouvelle instance). Résidu : trois DisplayName de test et un commentaire *décrivent* encore les conversions supprimées (§4.3). |
| 0022 | Acceptée | **Partielle pour Dummies.** L'asset netstandard2.0 n'est chargé et exercé sur net472 que transitivement via le job de plancher de FirstClassErrors ; la propre suite de Dummies n'y tourne jamais, et le README du paquet n'énonce aucun plancher .NET Framework (celui de FirstClassErrors le fait). À solder avant la première publication (§11 point 5). |
| 0025 | Proposée | **Conforme sur chaque clause majeure** (analyseur maison, refus de première classe, générateur terminal, zéro dépendance, univers ASCII imprimable *par défaut*, spread borné des quantificateurs non bornés). Les défauts §4.1(c)/(d) sont des bugs de qualité *à l'intérieur* du périmètre décidé, pas des déviations — avec la réserve que (d) rompt la *promesse* de refus que l'ADR consigne. Un bord de taxonomie : une classe négative bien formée hors de l'univers imprimable lève `ArgumentException` (« malformée ») au lieu d'`UnsupportedRegexException`. |
| 0026 | Acceptée | **Conforme sur chaque clause exécutée** — moteur unique, scope de graine unique, `Testing.Any` supprimé, fabriques livrées, horloge/ids sur le contexte ambiant, docs mises à jour EN/FR. La moitié « distinct `IAny<T>` method » non implémentée et la prémisse de risque mal énoncée sont consignées au §5. |

## 7. Revue de l'architecture

### 7.1 Découpage en couches et forme

La bibliothèque compte trois couches propres : **générateurs fluides publics** (fins, par type, scellés,
immuables) → **moteurs de spécification internes** (`OrdinalIntervalSpec`, `WideIntervalSpec`,
`ContinuousIntervalSpec`, `DecimalIntervalSpec`, `StringSpec`, `CountSpec`, `CollectionState<T>`) →
**primitives d'échantillonnage** (`RandomSampling`). La surface publique ne laisse jamais fuir de type
interne ; les moteurs internes ne touchent jamais directement l'état ambiant (les sources sont passées
vers le bas). Les points de composition — `.As(factory)`, `Any.Combine(...)`, les fabriques de
collection — sont tous définis sur l'`IAny<out T>` à un seul membre, qui est aussi petit qu'une interface
peut l'être (ISP par construction) et covariant, si bien que les générateurs dérivés et étrangers
traversent chaque point de couture uniformément.

La **séparation en quatre moteurs est fondée, pas accidentelle** : les types discrets projetables sur
64 bits partagent `OrdinalIntervalSpec` ; les entiers 128 bits ont besoin de `WideIntervalSpec` seulement
parce que netstandard2.0 n'a pas `UInt128` (les deux sont des jumeaux mot pour mot — l'unique
duplication regrettable, forcée par le TFM) ; les flottants IEEE ont besoin d'un échantillonnage continu
avec quantification consciente du type ; `decimal` n'est ni projetable en ordinal (mantisse 96 bits ×
échelle) ni IEEE. L'existence de chaque moteur est justifiée par son substrat arithmétique. Ce qui
*manque*, c'est l'ADR qui le consigne (§5), et — comme l'a montré §4.1(b) — une suite de tests paramétrée
exerçant chaque moteur à travers chacune de ses façades de type.

La **hiérarchie de collections** est un CRTP propre comme un manuel : `AnyCollection<TItem, TResult,
TSelf>` porte la surface fluide partagée de compte/contenance renvoyant `TSelf` (sans le classique cast
non sûr `(TSelf)this` — les types concrets implémentent une fabrique `With(state)`), et les cinq
générateurs concrets n'ajoutent que la mise en forme des éléments et la conversion `Build(List<TItem>)`.
L'exception est `AnyDictionary`, qui ne peut pas hériter de la base (son élément est une paire) et donc
**duplique toute la façade de compte mot pour mot** (~60 lignes, `AnyDictionary.cs:51-113`) et n'offre
aucune contrainte de contenance — le seul endroit de la famille collection où le partage a échoué.
Extraire la façade de compte au-dessus de `CollectionState` (ou ajouter `ContainingKey`, qui
chevaucherait gratuitement la machinerie d'état de clés existante) refermerait à la fois la duplication
et le trou de test reconnu (`AnyCollectionTests.cs:161-163` le commente).

### 7.2 Extensibilité

**Pour les utilisateurs, la conception est fermée, et c'est un choix légitime mais non documenté.**
`IAny<T>` est public, donc n'importe qui peut implémenter un générateur et le composer via
`As`/`Combine`/collections. Mais `RandomSource`, `IHasRandomSource` et `ICardinalityHint<T>` sont tous
internes, si bien qu'un générateur étranger (a) ne peut pas tirer de la source semée ambiante — sous
`Any.Reproducibly` ses valeurs ne rejouent pas, et (b) ne peut pas annoncer un domaine fini — une
collection distincte au-dessus de lui emprunte toujours le chemin du tirage borné (sûr, et exactement ce
que promet l'ADR-0013). La dégradation est gracieuse partout (vérifié : `OrNull` retombe sur la source
ambiante pour le tirage à pile ou face du null ; `Combine` propage les sources `null` sans échouer). Ce
qui manque est un paragraphe honnête sur la doc XML d'`IAny<T>` disant aux implémenteurs où ils se
situent — aujourd'hui le contrat n'est découvrable qu'en lisant du code interne. Si le point de couture
doit s'ouvrir un jour, un `ISeedableAny` dans une release mineure est la forme naturelle ; rien ne
demande à être décidé maintenant, sinon la documentation.

**Pour les mainteneurs**, ajouter un nouveau type scalaire touche 6 à 9 fichiers (générateur, `Any`,
`AnyContext`, tests, docs utilisateur EN/FR, README du paquet, `dummies-check` si net8-only,
éventuellement un moteur de spec). Le processus est mécanique mais réel, et n'est que partiellement gardé
(§9.2).

### 7.3 La machinerie de déterminisme — plongée en profondeur

L'implémentation est correcte au niveau qu'il est difficile de bien faire (§3.3). Les risques restants
sont tous des risques de *documentation de contrat*, et ils se regroupent en quatre :

**(a) La concurrence dans un même scope à graine annule silencieusement la rejouabilité — non
documenté.** Un `AsyncLocal` copie la *référence* : les enfants `Task.Run`/`Parallel.ForEach` à
l'intérieur d'un corps `Reproducibly` voient tous la même instance `SeededRandom`. Deux conséquences.
Premièrement, même avec un entrelacement bénin, l'*ordre* des tirages devient dépendant de
l'ordonnanceur, si bien que la graine rapportée ne rejoue plus le run — la garantie même pour laquelle la
fonctionnalité existe. Deuxièmement, `System.Random` n'est pas thread-safe, et netstandard2.0 n'offre
aucune alternative thread-safe ; un tirage mis en course peut corrompre l'état (sur .NET Framework, un
`Random` mis en course peut dégénérer et renvoyer des zéros). Les docs expliquent soigneusement que la
source « ne fuit jamais *entre* les tests qui tournent en parallèle » (vrai — flux logiques différents)
mais ne disent rien du parallélisme *à l'intérieur* d'un corps. Le correctif est un paragraphe honnête
sur `Reproducibly` (« un run semé est à flux logique unique ; les tirages concurrents dans le corps ne
sont ni rejouables ni sûrs ») — plus, optionnellement, consigner dans la nouvelle ADR de déterminisme
pourquoi un forkage par flux (une source enfant par `Task.Run`) n'a pas été tenté (il changerait toute
séquence et compliquerait `WithSeed` ; la restriction honnête est la bonne V1).

**(b) Les rapports de graine peuvent nommer une graine erronée ou inapplicable dans une composition à
source mixte/fixe.** `Combine` propage la source d'opérande **première non nulle** pour le rapport
d'échec (`Any.cs:446` et al.). `Any.Combine(Any.WithSeed(1).Int32(), Any.WithSeed(2).Int32(), throwing)`
échoue avec « seeded with 1; reproduce with `Any.Reproducibly(1, …)` » — doublement faux : la graine 2
n'est pas rapportée, et l'instruction est inapplicable parce que `Reproducibly` épingle la source
*ambiante*, que les générateurs adossés à `FixedRandomSource` ignorent par conception. C'est un cas
limite (mélanger des contextes semés dans une même composition est inhabituel), mais le mode d'échec est
un *diagnostic trompeur avec assurance* dans la bibliothèque dont la signature est l'honnêteté
diagnostique. Un petit correctif l'atteint : laisser le type de source produire l'indice de rejeu
(ambiante → « reproduce with `Any.Reproducibly({seed}, …)` » ; fixe → « ce générateur tire de
`Any.WithSeed({seed})`, qui rejoue déjà de lui-même »), et faire collecter à `Combine` les sources
distinctes plutôt que la première.

**(c) La stabilité de graine entre versions et entre runtimes n'est ni promise ni écartée.** La
description du paquet dit « any run is reproducible from a reported seed » sans réserve. Dans un même
processus, cela tient. Entre *versions de la bibliothèque*, tout changement d'ordre ou de nombre de
tirages change silencieusement chaque séquence — et l'ADR-0025 reconnaît déjà que les consommateurs
peuvent s'appuyer sur les formes générées. Entre *runtimes*, un `new Random(seed)` semé conserve
l'algorithme historique sur .NET moderne précisément par compatibilité, de sorte que la surface commune
devrait s'accorder entre les assets netstandard2.0 et net8.0 — mais rien ne le teste (§4.6), et la
documentation de `Random` se réserve explicitement le droit que les implémentations diffèrent entre
versions du framework. La politique mature, avant la v1 : **promettre la stabilité au sein d'une version
de paquet, l'écarter entre versions**, une phrase dans le README et dans la nouvelle ADR de déterminisme.
(Pour comparaison : FsCheck et AutoFixture ont tous deux appris à l'écarter explicitement.)

**(d) L'épinglage ambiant paresseux rend un échec *non enveloppé* seulement approximativement
rejouable.** Hors de `Reproducibly`, le premier tirage dans un flux logique épingle une graine
mémorisée. Les tirages survenus *avant* le bloc fautif dans le même flux (une fixture, un `Arrange`
antérieur) consomment de la même séquence, de sorte que rejouer « juste le corps du test » avec la graine
rapportée peut diverger. La conception est juste (c'est pour cela que `Reproducibly` existe) ; le récit
de rejeu du guide utilisateur pourrait porter une phrase disant que la fidélité de rejeu commence à la
frontière du scope.

Des non-problèmes vérifiés qu'il vaut la peine de consigner pour ne pas les rejuger : la sémantique
d'`ExecutionContext` de la surcharge asynchrone (correcte — voir §3.3) ;
`NewSeed() = Guid.NewGuid().GetHashCode()` (usage tolérant aux collisions, analysé dans l'ADR-0006) ;
l'étendue de graine sous xUnit (chaque invocation de test est son propre cadre asynchrone ; un
constructeur de classe partagé participe au flux de son test, ce qui est le bon scope) ; la
ré-énumération de `SequenceOf` (matérialisée une fois, ne re-tire jamais).

### 7.4 SOLID, brièvement et seulement là où cela vaut la peine

SRP : les générateurs portent la surface fluide, les moteurs portent la sémantique — propre. OCP :
ajouter une *contrainte* à un type discret est l'ajout d'une méthode de façade au-dessus d'une opération
de moteur existante ; ajouter un *type* est délibérément fermé (générateurs scellés, moteurs internes) —
le bon compromis pour une bibliothèque riche en invariants. LSP : la base de collection CRTP est saine
(pas d'astuce d'auto-cast, borne `TSelf` imposée). ISP : `IAny<T>` à membre unique ; les deux membres
d'`ICardinalityHint<T>` voyagent ensemble par conception explicite et documentée (une cardinalité sans
appartenance serait fausse — la doc d'interface l'argumente). DIP est intentionnellement absent au point
de couture utilisateur (aucune abstraction d'aléatoire injectable) — c'est *cela* la décision
d'extensibilité fermée du §7.2, acceptable mais méritant son paragraphe de documentation.

## 8. Revue de l'API

### 8.1 L'algèbre de contraintes est uniforme là où cela compte

La matrice vérifiée : les cinq générateurs d'entiers signés et les quatre générateurs continus/décimaux
exposent exactement `Positive · Negative · Zero · NonZero · GreaterThan[OrEqualTo] · LessThan[OrEqualTo]
· Between · OneOf · Except · DifferentFrom` ; les cinq générateurs non signés retirent exactement
`Positive`/`Negative` (sans objet ici — `NonZero` couvre l'intention) ; les quatre générateurs de type
instant (`DateTime`, `DateTimeOffset`, `DateOnly`, `TimeOnly`) renomment la famille de bornes en
vocabulaire métier (`After`/`AfterOrEqualTo`/`Before`/`BeforeOrEqualTo`/`Between`) avec une sémantique
inclusive/exclusive identique, tandis qu'`AnyTimeSpan` — une magnitude, pas un instant — garde
correctement l'algèbre numérique complète, y compris `Positive`/`Negative`/`Zero` ; `AnyChar` porte les
familles de caractères plus le trio d'exclusion ; `AnyGuid` a
`NonEmpty`/`Empty`/`OneOf`/`Except`/`DifferentFrom` ; `AnyEnum` a le trio d'exclusion avec validation des
membres déclarés ; les collections partagent `NonEmpty · Empty · WithCount · WithMinCount · WithMaxCount
· WithCountBetween · Containing · ContainingAny` (+ variantes `Distinct` là où c'est pertinent). Les
bornes sont uniformément inclusives pour `Between`/`…OrEqualTo` et exclusives pour
`GreaterThan`/`LessThan`/`After`/`Before` — aucune surprise sémantique n'a été trouvée nulle part dans la
matrice. Ce niveau de cohérence sur dix-neuf générateurs d'intervalle écrits à la main — plus leurs
frères chaîne, char, guid, enum, bool et collection — est un accomplissement en soi.

### 8.2 Les asymétries qui valent d'être corrigées ou consignées

* **`AnyString` est le seul générateur scalaire sans contraintes d'exclusion** — pas d'`OneOf`, pas
  d'`Except`, pas de `DifferentFrom`. « Un nom différent de celui que je détiens déjà » est l'un des
  besoins de chaîne factice les plus courants (c'est exactement pourquoi `DifferentFrom` existe partout
  ailleurs, selon sa propre doc XML). La raison honnête du trou : les chaînes ne sont pas projetées en
  ordinal, donc les exclusions ne peuvent pas chevaucher le moteur d'intervalle ; `DifferentFrom`
  nécessiterait soit un retirage borné (collisions attendues ≈ 0 pour toute spec non triviale — cohérent
  avec les autres échappatoires bornées de la bibliothèque) soit un ajustement d'agencement conscient de
  la spec. Recommandé (§10 Indispensable) :

  ```csharp
  // Aujourd'hui — aucun moyen d'exprimer ceci :
  string other = Any.String().NonEmpty().Generate();     // pourrait être égal à l'existant !
  // Proposé :
  string other = Any.String().NonEmpty().DifferentFrom(existing).Generate();
  ```

* **`AnyDictionary` abandonne `Containing`/`ContainingAny`** et duplique la façade de compte (§7.1).
  `ContainingKey(TKey)` chevaucherait la machinerie d'état de clés existante sans changement.
* **`Any.Bool()` est la seule déviation de la convention de fabriques aux noms CLR** (`Int32`, `SByte`,
  `Single`, … sont tous des noms CLR ; le nom CLR ici est `Boolean`). La forme courte est sans doute la
  meilleure ergonomie — mais alors la convention devient « noms CLR, sauf un », et après la 1.0 le
  renommage est cassant dans les deux sens. Décider délibérément et consigner une ligne, avant la
  publication (le dépôt a des ADR précisément pour cette classe de décision de nommage).
* **`PairOf`/`TripleOf` s'arrêtent à l'arité 3** tandis que `Combine` va jusqu'à 8. Défendable (les
  tuples au-delà de 3 se lisent mal ; `Combine` les couvre), mais le point d'arrêt n'est consigné nulle
  part — une phrase de doc le referme.

### 8.3 Découvrabilité et cérémonie

Le point d'entrée statique `Any.` rend toute la surface scalaire découvrable en une frappe, et les
méthodes fluides de chaque générateur énumèrent tout son vocabulaire de contraintes dans IntelliSense —
bien. Deux points de couture sont moins découvrables : `As` et `OrNull` sont des méthodes d'extension
dans des classes statiques séparées (invisibles tant que le `using` n'existe pas — bien que l'espace de
noms soit partagé, donc en pratique elles apparaissent), et `As` est le `Select` de la bibliothèque sous
un nom d'intention métier ; une ligne de doc faisant le pont depuis le vocabulaire LINQ (« `As` est le
`Select` des générateurs — nommé pour son usage dominant : passer par la fabrique d'un objet-valeur »)
aiderait les lecteurs familiers de LINQ. La cérémonie terminale `Generate()` est le compromis de
l'ADR-0020, consciemment chiffré là-bas ; l'audit confirme que le coût est réel mais petit (un appel par
matérialisation), que le bénéfice (aucune conversion cachée à effet de bord) est structurel, et que la
décision doit tenir. `AnyContext` ne recopie que les scalaires — la composition hérite du contexte via
les sources d'opérandes, ce qui est *plus* élégant que le recopiage et correctement documenté.

### 8.4 Nommage

`StartingWith`/`EndingWith`/`Containing`, `After`/`Before`, `DifferentFrom` vs `Except`, `Containing` vs
`ContainingAny` — le vocabulaire est révélateur d'intention et se lit sur le site d'appel comme la
philosophie l'entend. Les fabriques aux noms de type CLR (`Any.Int32()`, pas `Any.Int()`) sont cohérentes
avec les noms de type des générateurs (`AnyInt32`) et contournent les restrictions de mots-clés C# ;
c'est défendable et, plus important, uniforme (le cas `Bool` du §8.2 mis à part).

## 9. Revue de la maintenabilité

### 9.1 Duplication, mesurée

Quatre familles de clones parmi les générateurs numériques (quatuor signé, quatuor non signé, trio
continu, paire large — identiques à l'octet près modulo substitution de type ; ~2 450 lignes), les cinq
générateurs temporels sur le même patron (~800 lignes), la logique de contrainte-et-conflit quadruplée à
travers les quatre moteurs (~910 lignes), et le miroir scalaire `Any`/`AnyContext` (~300 lignes riches en
doc). Une comparaison scriptée n'a trouvé **aucune erreur de copier-coller comportementale** à travers
les familles de clones — preuve d'une vraie discipline — tandis que toute la dérive trouvée jusqu'ici est
une dérive de *documentation* (§4.3), exactement le genre pour lequel les gardes n'existent pas encore.

### 9.2 Atténuation : des gardes, pas de génériques

Le refactoring évident — une base générique CRTP (`AnyOrdinal<T, TSelf>`) — bute sur les contraintes de
ce projet : C# exige une classe de base publique pour un générateur public scellé (CS0060), si bien que
le point de couture du moteur interne fuirait dans l'API publique ; netstandard2.0 n'a pas de
mathématiques génériques (`INumber<T>` est net7+), donc les lambdas `Ord`/`Val`/d'affichage par type
demeurent ; et la barre affichée de la bibliothèque est la simplicité de maintenance, que 14 fichiers
plats, ennuyeux et « greppables » servent mieux qu'une base astucieuse. Les générateurs de source/T4
achètent la déduplication au prix d'une machinerie de build et d'une débogabilité — mauvais compromis ici
aussi. **Recommandé à la place : des gardes de parité exécutables**, ~3 courts tests par réflexion :

1. *Parité du miroir :* chaque méthode statique publique `Any` renvoyant un type de générateur a une
   contrepartie d'instance `AnyContext` de nom/signature/type de retour identiques, par TFM (~20 lignes ;
   tue d'un coup la classe de dérive du §4.3).
2. *Parité de l'algèbre :* chaque famille de générateurs expose son ensemble exact de noms de méthodes
   attendus (la matrice du §8.1, encodée une fois comme donnée) — un nouveau générateur privé de
   `DifferentFrom`, ou une méthode renommée, échoue avec un diff nommé.
3. *Suite de scénarios transverse aux moteurs :* un fichier de test paramétré passe la même batterie de
   scénarios (un tirage pleine-plage touche les deux moitiés ; les bornes de `Between` sont atteignables ;
   `DifferentFrom` sur un domaine étroit ; interaction `OneOf`+`Except` ; messages de conflit) contre
   **chaque** générateur via de petits adaptateurs par type. C'est la suite qui aurait attrapé §4.1(a) et
   §4.1(b) avant toute revue humaine.

En complément, installer les gardes d'ingénierie de release du §4.7 (référence d'API publique +
validation de paquet) — ils attrapent la classe de changements cassants que les tests de parité ne
peuvent pas.

### 9.3 Stratégie de test

Ce qui existe est bien formé : un nommage comportement-d'abord qui se lit comme documentation vivante ;
les *messages* d'exception testés comme des contrats de première classe ; l'oracle regex du vrai moteur ;
des tests de non-régression qui encodent l'historique des bugs (le test de course d'`AnyGuid` qui court
contre une échéance au lieu de bloquer la suite) ; des assertions façon-propriété à l'abri des tests
instables (les tirages non semés assertés seulement contre leur domaine déclaré) ; et une posture
strictement boîte noire — aucun `InternalsVisibleTo` n'existe, si bien que les 222 tests n'exercent que
la surface publique. Ce dernier fait coupe dans les deux sens et devrait être tenu pour un choix
délibéré : il prouve que l'API publique suffit à spécifier la bibliothèque (et rend les refactorings de
moteur transparents aux tests), *et* il est cohérent avec la façon dont les deux défauts d'atteignabilité
ont survécu — aucun test ne regarde directement la couverture de l'espace de valeurs d'un moteur. Les
ajouts qui referment l'écart, par ordre de levier : la suite de scénarios transverse ci-dessus ; des
**assertions d'atteignabilité** (pour chaque générateur, une boucle semée sur `Between(lo, hi)` doit
observer des valeurs dans les deux moitiés et toucher les deux bornes — peu coûteux, déterministe sous
`WithSeed`) ; un test de limite de génération pour `AnyPattern` (actuellement non testée) ; des tests
dédiés aux contrats documentés-mais-non-testés (enum vide, capturabilité de la base `AnyException`,
chemin du comparateur de clés de `DictionaryOf`) ; et l'assertion même-graine inter-TFM dans
`dummies-check` (étendre `SeedBatch` d'une séquence de référence comparée entre les cibles consommatrices
net8.0 et net6.0, et étendre le test de fumée pour couvrir les tirages
`OrNull`/`SequenceOf`/`PairOf`/`StringMatching`/enum, que le garde-fou d'artefact empaqueté ne touche
actuellement jamais).

### 9.4 Organisation et hygiène

La racine plate de 54 fichiers est acceptable aujourd'hui parce que la discipline de nommage fait office
de dossiers (`Any*` = générateurs, `*Spec` = moteurs, `Regex*` = sous-système de motifs) ; regrouper en
dossiers est un polissage optionnel, à ne faire qu'à l'occasion d'un autre changement structurel. Points
d'hygiène trouvés : le membre mort `RegexCharacters.Count` ; la garde-null morte dans
`CollectionState.Exhausted` (ligne §6/ADR-0013) ; les commentaires et DisplayName périmés du §4.3 ;
l'en-tête périmé de `Directory.Build.props` (§4.7).

## 10. Analyse des manques fonctionnels

Méthode : chaque proposition a été passée au crible de (i) la philosophie de la bibliothèque (les
contraintes expriment des invariants ; pas de fausses données réalistes, pas de graphes d'objets, pas de
couplage à l'horloge), (ii) le test de composition — *`As`/`Combine`/`StringMatching` peuvent-ils déjà
exprimer ceci en une ligne lisible ?* — et (iii) le coût complet d'un nouveau générateur (générateur +
`Any` + `AnyContext` + données de parité + tests + docs EN/FR + README du paquet + éventuellement
`dummies-check`). La barre de l'**Indispensable** est celle du mandat : une absence véritablement
surprenante. La conception composition-d'abord de la bibliothèque garde cette liste courte — la plupart
des types BCL sont déjà à un `As` de distance, ce qui est la conception fonctionnant comme prévu.

### Indispensable

**1. Un combinateur de choix de premier niveau : `Any.OneOf<T>(params T[])` et
`Any.ElementOf<T>(IReadOnlyList<T>)`.**
Choisir un élément arbitraire dans un ensemble fourni par l'appelant est parmi les besoins factices les
plus courants dans les vraies suites (« l'une des trois devises configurées », « l'un des états de cette
table »). Aujourd'hui `OneOf` n'existe qu'*à l'intérieur* des générateurs typés — il n'y a aucun moyen de
tirer d'un ensemble d'objets métier ou de chaînes. Chaque utilisateur recode les mêmes trois lignes (et
oublie la source semée, cassant silencieusement `Reproducibly` pour ce tirage — un piège que la
bibliothèque existe pour prévenir) :

```csharp
// Aujourd'hui — codé à la main, et non conscient de la graine :
var currencies = new[] { eur, usd, gbp };
var currency   = currencies[new Random().Next(currencies.Length)];   // graine ambiante ignorée !

// Proposé — conscient de la graine, cohérent avec la philosophie, validé immédiatement (ensemble vide → lève) :
Currency currency = Any.OneOf(eur, usd, gbp).Generate();
Order    order    = Any.ElementOf(existingOrders).Generate();
```

Constructif (tirage unique), trivialement implémentable sur la source ambiante avec un
`ICardinalityHint` (compte distinct du réservoir — il se compose gratuitement avec les collections
distinctes), recopié sur `AnyContext`. Qui en bénéficie : chaque consommateur, chaque semaine. Coût : un
petit générateur. C'est l'ajout au plus fort levier disponible.

**2. `AnyString.DifferentFrom(string)` / `Except(params string[])`.**
L'asymétrie du §8.2 : le générateur le plus utilisé est le seul scalaire qui ne puisse pas exclure de
valeurs. Coût honnête : un retirage borné (le patron d'échappatoire établi de la bibliothèque) ou une
exclusion consciente des fragments ; l'un comme l'autre s'inscrit dans le modèle de validation
`StringSpec` existant. Qui en bénéficie : quiconque teste des chemins d'égalité/inégalité avec des
identifiants chaîne — un cas très courant. (`OneOf` sur les chaînes est alors gratuit via la
proposition 1.)

### Souhaitable

* **Générateur `Uri`** (`Any.Uri().UsingHttps().WithHost("example.com")`) — le seul type BCL de type
  valeur à la fois couramment nécessaire dans les tests et réellement pénible à composer à la main
  (règles de validité schéma/hôte/chemin/query). Intégré aux deux TFM. Coût modéré (sa propre mini
  algèbre de contraintes) ; un timing guidé par la demande convient.
* **`WithChars(string pool)` / alphabet personnalisé sur `AnyString`** — aujourd'hui le texte non-ASCII
  (accents, i18n) n'est atteignable que via des littéraux `StringMatching` ; un réservoir personnalisé
  est une petite extension composable du mécanisme de jeu de caractères existant, et débloque le cas
  d'usage du code sensible à l'i18n sans aucune machinerie de tables Unicode.
* **`MultipleOf(int)` sur les entiers / `WithScale(int)` sur decimal** — « un montant valide en
  centimes », « une quantité en douzaines » : de véritables invariants (pas des assertions) qui forcent
  aujourd'hui des contournements `As(x => x * 100)` qui déforment la plage déclarée. Constructif à
  implémenter (tirer dans l'espace du quotient).
* **`ContainingKey(TKey)` sur `AnyDictionary`** (§7.1/§8.2) — referme d'un coup un trou d'API, une
  duplication et un trou de test.
* **Combinaisons d'enum [Flags], en opt-in** (`Any.Enum<Permissions>().AllowingCombinations()`) —
  aujourd'hui les valeurs combinées non déclarées sont inatteignables *par conception* (membres-déclarés-
  seulement est le bon défaut) ; un opt-in explicite respecte le défaut tout en servant les domaines
  riches en drapeaux. Nécessite une position documentée sur ce que « valide » signifie pour les drapeaux
  (union des membres déclarés).
* **`WithOffset`/contrôle d'offset sur `AnyDateTimeOffset`** — la dimension d'offset est actuellement
  dégénérée (toujours zéro, documenté) ; les tests qui exercent l'arithmétique d'offset ne peuvent pas la
  faire varier. Un tirage d'offset borné (±14 h en minutes, selon les propres règles du type) préserve la
  validité.
* **Granularité temporelle** (`WholeSeconds()`/`WholeDays()` ou `WithGranularity(TimeSpan)`) — les
  instants à précision de tick sont presque jamais ronds, ce qui surprend les tests qui sérialisent des
  horodatages ; constructif via le moteur ordinal (tirer dans l'espace des granules, multiplier).
  Referme aussi entretemps le trou de documentation (« les valeurs sont à précision de tick »).
* **Terminal `GenerateMany(int)`** — sucre pour « N valeurs sans la cérémonie `ListOf` » ; une *méthode
  nommée* renvoyant `IReadOnlyList<T>`, donc elle reste dans la lettre et l'esprit de l'ADR-0020.
* **Un adaptateur de graine pour framework de test** (`[ReproducibleFact]`) — anticipé par les suivis de
  l'ADR-0006, abandonné lors du rebasage, remplacé par rien. Dummies zéro-dépendance ne peut pas
  référencer xUnit, c'est donc une décision de *paquet compagnon* (`Dummies.Xunit`) — méritant une ADR
  explicite oui/non plutôt que le silence, car chaque consommateur re-dérive aujourd'hui à la main
  l'habitude d'envelopper dans `Reproducibly`.

### Idées optionnelles

`Version` (composable aujourd'hui :
`Combine(Any.Int32().Between(0,99), …, (ma,mi,pa) => new Version(ma,mi,pa))` ; faible fréquence) ;
`IPAddress`/`IPEndPoint` (intégrés, de niche ; une recette de doc d'abord) ; `Encoding` et `CultureInfo`
(faisables **seulement** depuis un réservoir fixe embarqué — l'ensemble des cultures installées est un
danger de reproductibilité entre machines que la bibliothèque ne doit pas hériter ; les deux sont
subsumés par la proposition 1 + un réservoir documenté) ; `MailAddress`, chemins de système de fichiers,
`Stream`, blobs `byte[]` (toutes des recettes d'une ligne sur la surface existante — `ArrayOf(Any.Byte())`
*est* déjà le générateur de blob ; les documenter dans la section recettes du guide utilisateur plutôt
que de livrer des générateurs) ; sucre `KeyValuePair` ; collections `Queue`/`Stack`/`LinkedList` et
`Sorted*` (conversions `As` d'une ligne ; un `Sorted()` de première classe nécessite un gate de
comparabilité analogue à l'indice de cardinalité — la conception existe si la demande apparaît) ;
`BigInteger` (intégré aux deux TFM mais rompt la symétrie « pleine plage sauf contrainte » — il n'y a pas
de pleine plage ; nécessite sa propre position de défaut borné) ; `Rune` (cible net8 ; en conflit avec le
modèle de texte délibérément ASCII-centrique tant que `WithChars` n'a pas atterri) ; sucre
`ContainingAll(params T[])`.

### Hors périmètre (recommandé de rester absent, avec les raisons)

* **Filtrage `Where(predicate)`** — générer-puis-filtrer est l'exact opposé du modèle constructif de la
  bibliothèque ; des prédicats insatisfiables réintroduisent la classe de reprise non bornée que toute la
  conception existe pour exclure. La réponse existante (exprimer l'invariant en contraintes, ou
  construire via `As` depuis un tirage contraint) est la philosophie.
* **Enregistrement de générateurs / graphes d'objets façon AutoFixture** — le remplissage automatique
  piloté par réflexion est le produit voisin que le README écarte explicitement ; de simples membres C#
  sont le mécanisme de réutilisation.
* **Collections immuables** — `System.Collections.Immutable` est un paquet externe sur la cible
  netstandard2.0, donc un générateur romprait l'identité zéro-dépendance là-bas ; côté consommateur,
  `.As(ImmutableList.CreateRange)` est une ligne. (Une surface net8-only fracturerait l'API entre TFM
  pour un gain marginal — ne vaut pas la peine.)
* **`Index`/`Range`** — la validité est contextuelle (dépend de la longueur de la séquence), donc
  « arbitraire mais valide » ne peut pas tenir de façon autonome.
* **`RegionInfo`**, **`Complex`** — dépendant de l'environnement resp. de niche scientifique ; les deux
  échouent au test de fréquence.
* **Fausses données réalistes** (noms, e-mails, adresses) — explicitement écartées ; Bogus existe.

## 11. Améliorations recommandées

Par ordre de priorité ; les points 1–7 sont la porte pré-publication recommandée.

1. **Corriger les trois défauts reproduits** — construction de la fraction décimale
   (`DecimalIntervalSpec.cs:145`), nudge conscient du type (`ContinuousIntervalSpec.cs:189` →
   `_nextUp`), garde de débordement du char (`RegexParser.cs:398` + `RegexAlphabet.Range`) ; et la
   validation de groupe d'équilibrage/nom dans `SkipGroupName` (§4.1 d). Chacun avec un test de
   non-régression.
2. **Ajouter des tests d'atteignabilité et la suite de scénarios transverse aux moteurs** (§9.3) — la
   réponse structurelle à la classe de défauts, pas seulement aux instances.
3. **Ajouter les gardes de parité** (§9.2) : test de miroir `Any`↔`AnyContext`, test de matrice
   d'algèbre.
4. **Solder le contrat de déterminisme** (§7.3) : documenter le semis à flux logique unique sur
   `Reproducibly` ; des indices de rejeu conscients du type de source (et le rapport multi-source de
   `Combine`) ; la phrase de politique de stabilité entre versions ; la qualification générateur-étranger
   dans le message de saturation (garde-null morte retirée). Rédiger l'**ADR de déterminisme** et l'**ADR
   du moteur ordinal** (§5, lacunes structurelles) en `Proposée` pour `@reefact`.
5. **Exécuter Dummies sur ses planchers** : importer `build/Net472TestFloor.props` dans
   `Dummies.UnitTests` (tests net8-only conditionnés hors scope), l'ajouter à la boucle de plancher de
   ci.yml ; ajouter l'assertion de séquence de référence inter-TFM à `dummies-check` ; énoncer le
   plancher .NET Framework dans le README du paquet (suivi de l'ADR-0022).
6. **Passe de documentation** : faire apparaître Dummies dans le README du dépôt (table des paquets +
   sommaire) ; écrire le guide utilisateur Dummies avec la référence de contraintes par générateur et le
   dialecte `StringMatching` (refermant le suivi de l'ADR-0025) ; corriger les trois emplacements « ASCII
   imprimable » (§4.2) ; annoncer le comportement vide-par-défaut dans le README du paquet ; corriger les
   commentaires/DisplayName périmés (§4.3) et l'en-tête de `Directory.Build.props`.
7. **Gardes d'ingénierie de release** : référence d'API publique (`PublicApiAnalyzers`) et
   `EnablePackageValidation` ; décider `Bool()` vs `Boolean()` et le consigner ; demander à `@reefact`
   de trancher le statut de l'ADR-0025 (après sa correction de formulation) ; consigner les deux
   clarifications de l'ADR-0026 dans la référence d'implémentation ; enrichir ou adoucir les renvois à la
   référence d'implémentation des ADR-0013/0015.
8. **Livrer les deux fonctionnalités Indispensables** (§10) : `Any.OneOf<T>`/`Any.ElementOf<T>`, et les
   exclusions de chaîne (`DifferentFrom`/`Except` sur `AnyString`).
9. **`AnyDictionary`** : extraire la façade de compte partagée ; ajouter `ContainingKey`.
10. **Ensuite, guidé par la demande** : la liste Souhaitable (§10), chacune sur preuve de besoin, avec les
    données de garde de parité mises à jour comme partie du « terminé » de chaque ajout.

## 12. Feuille de route proposée

**Phase 0 — avant la première release `dum-v*` (correction et contrat).** Points 1–7 ci-dessus. La
justification est celle de l'ADR-0020 : chacun de ces points est bon marché maintenant et coûteux après
adoption — le correctif décimal change chaque séquence semée (un non-événement aujourd'hui, un événement
de compatibilité après la v1) ; la politique de déterminisme, le nommage `Bool`, la référence d'API et
les statuts d'ADR sont tous des décisions d'une ligne ou d'un fichier qui deviennent des migrations plus
tard. Critère de sortie : la table des faiblesses du §4 est vide, sauf les points explicitement différés
par décision consignée.

**Phase 1 — premier cycle stable (complétude au sein de la philosophie).** Point 8 (les deux
Indispensables, additifs et à faible risque), point 9, la section recettes du guide utilisateur (blobs,
chemins, Version, Uri-via-Combine — transformant les types de la liste Optionnelle en documentation
plutôt qu'en surface), et la décision de paquet compagnon `Dummies.Xunit` (oui ou non, en ADR).

**Phase 2 — croissance guidée par la demande.** Les Souhaitables au fur et à mesure que de vraies
demandes arrivent (`Uri` et `WithChars` d'abord, au vu des preuves actuelles), chaque ajout portant son
entrée de matrice de parité, ses tests et ses docs EN/FR comme une seule unité. Revisiter la liste
Optionnelle chaque année ; résister à la liste Hors périmètre en permanence — c'est ce qui garde cette
bibliothèque telle qu'elle est.

## 13. Conclusion

Dummies est ce à quoi ressemble une bibliothèque focalisée quand les auteurs savent exactement à quoi
elle sert et — tout aussi important — à quoi elle ne sert pas. Le moteur en espace ordinal, les
diagnostics à provenance de contraintes, la discipline d'échappatoires bornées et la trace d'ADR sont
tous meilleurs que la norme de la catégorie, et la conception composition-d'abord garde honnête la
surface de fonctionnalités future : la plupart des « types manquants » sont correctement à un `As` de
distance, pas à un générateur de distance.

Les constats de l'audit se concentrent en un seul endroit : l'espace entre le comportement *déclaré* et
le comportement *atteignable*. Deux des trois défauts reproduits vivent exactement là, invisibles à une
suite fondée sur la seule appartenance ; les surfaces miroir dérivent exactement là où aucune garde ne
regarde ; la promesse de déterminisme est saine précisément jusqu'aux bords qu'aucun document ne décrit.
Tout cela est corrigeable de ce côté-ci de la première publication, la plupart en quelques jours, et les
points de plus grande valeur ne sont pas les correctifs mais les gardes — la suite d'atteignabilité, les
tests de parité, la référence d'API — qui rendent impossible de livrer silencieusement le prochain défaut
de chaque classe.

La Phase 0 faite, c'est une bibliothèque qui peut promettre de façon crédible ce que dit son README :
arbitraire mais valide, des conflits nommés à la ligne qui les a causés, et tout run rejouable depuis une
graine rapportée — sur chaque cible pour laquelle elle est livrée.

## 14. Suivi des issues

Les recommandations de la §11 ont été ouvertes en issues GitHub le 2026-07-20, sur le gabarit d'issue
Dummies du dépôt. Cette table est un **instantané figé** : l'état vivant de chaque issue (ouverte, fermée,
en cours) vit dans le tracker, pas ici — ne pas maintenir de statut dans ce document.

| Point §11 | Issue(s) | Phase (§12) |
|---|---|---|
| 1 — Corriger les défauts reproduits | [#206](https://github.com/Reefact/first-class-errors/issues/206) AnyDecimal moitié haute · [#207](https://github.com/Reefact/first-class-errors/issues/207) nudge Single/Half · [#208](https://github.com/Reefact/first-class-errors/issues/208) blocage U+FFFF · [#209](https://github.com/Reefact/first-class-errors/issues/209) groupes d'équilibrage · [#210](https://github.com/Reefact/first-class-errors/issues/210) bords regex mineurs | 0 |
| 2 — Atteignabilité + suite transverse | [#213](https://github.com/Reefact/first-class-errors/issues/213) | 0 |
| 3 — Gardes de parité | [#214](https://github.com/Reefact/first-class-errors/issues/214) | 0 |
| 4 — Solder le contrat de déterminisme | [#216](https://github.com/Reefact/first-class-errors/issues/216) doc contrat + ADR · [#217](https://github.com/Reefact/first-class-errors/issues/217) ADR moteur ordinal · [#211](https://github.com/Reefact/first-class-errors/issues/211) rapport de graine · [#212](https://github.com/Reefact/first-class-errors/issues/212) message de saturation | 0 |
| 5 — Exécuter sur les planchers | [#215](https://github.com/Reefact/first-class-errors/issues/215) | 0 |
| 6 — Passe de documentation | [#218](https://github.com/Reefact/first-class-errors/issues/218) README + guide utilisateur · [#219](https://github.com/Reefact/first-class-errors/issues/219) ASCII imprimable & docs périmées | 0 |
| 7 — Gardes d'ingénierie de release | [#221](https://github.com/Reefact/first-class-errors/issues/221) baseline API · [#222](https://github.com/Reefact/first-class-errors/issues/222) nommage Bool · [#220](https://github.com/Reefact/first-class-errors/issues/220) hygiène ADR | 0 |
| 8 — Livrer les Indispensables | [#223](https://github.com/Reefact/first-class-errors/issues/223) Any.OneOf/ElementOf · [#224](https://github.com/Reefact/first-class-errors/issues/224) exclusions AnyString | 1 |
| 9 — AnyDictionary | [#225](https://github.com/Reefact/first-class-errors/issues/225) | 1 |
| 10 — Souhaitables guidés par la demande | [#226](https://github.com/Reefact/first-class-errors/issues/226) backlog | 2 |

---

*Produit par un audit mené par agents (revue multi-agents avec vérification contradictoire ; tous les
défauts rapportés reproduits indépendamment contre la bibliothèque compilée ; suite de tests complète
exécutée). Consultatif au sens de l'ADR-0004 : recommandations et brouillons seulement — chaque décision
demeure au mainteneur.*
