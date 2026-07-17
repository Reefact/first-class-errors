# ADR-0007 | Nommer les terminaux du binder New et Create

🌍 🇬🇧 [English](0007-name-the-binder-terminals-new-and-create.md) · 🇫🇷 Français (ce fichier)

**Statut :** Accepté
**Date :** 2026-07-16
**Décideurs :** Reefact

## Contexte

* `FirstClassErrors.RequestBinder` lie chaque propriété d'un DTO de requête en un value
  object, puis un **terminal** assemble les valeurs liées dans la commande (ou requête)
  et retourne `Outcome<TCommand>`.
* Les consommateurs assemblent cette commande sous deux formes qui diffèrent selon que
  l'étape d'assemblage peut elle-même échouer : un **constructeur total**
  (`new Command(...)`) ne peut pas échouer, parce que chaque champ a déjà été validé un
  par un pendant le binding ; une **fabrique validante** (`Command.Create(...)`
  retournant `Outcome<TCommand>`) peut encore échouer, parce qu'elle applique une règle
  inter-champs — comme le fait que le check-out soit postérieur au check-in — qu'aucun
  champ isolé ne pourrait vérifier à lui seul.
* C# interdit la surcharge sur le seul type de retour, et sa résolution de surcharge ne
  traverse pas la conversion implicite `TCommand` → `Outcome<TCommand>`. Deux surcharges
  de terminal portant le même nom — l'une prenant un assembleur retournant `TCommand`,
  l'autre prenant un assembleur retournant `Outcome<TCommand>` — sont donc ambiguës pour
  une lambda qui retourne `Outcome<TCommand>` : les deux sont applicables avec un
  `TCommand` inféré différent, et le compilateur rejette l'appel.
* Un terminal unique ne prenant qu'un assembleur retournant `TCommand` force un
  consommateur dont la fabrique retourne `Outcome<TCommand>` à imbriquer le résultat sous
  la forme `Outcome<Outcome<TCommand>>`, et ne laisse aucun endroit où exécuter une règle
  inter-champs.
* La bibliothèque réserve déjà le nom de fabrique simple à la variante retournant un
  `Outcome` (ADR-0005) ; dans tout le code, les fabriques `Parse` / `Create` des value
  objects retournent `Outcome<T>`, si bien que « une fabrique au nom simple restitue un
  `Outcome` » constitue un vocabulaire établi.
* L'assembleur reçoit un `BindingScope`, un `readonly ref struct` ; un ref struct ne peut
  pas être un argument de type générique, si bien qu'un assembleur ne peut pas être un
  `Func<>` et doit être un type delegate nommé.
* La bibliothèque est en pré-version, non publiée sur NuGet et sans consommateurs
  externes, si bien qu'un choix de nommage sur cette nouvelle API n'entraîne aucun coût
  de migration en aval.

## Décision

Le request binder expose deux terminaux aux noms distincts — `New`, qui prend un
constructeur total retournant la commande, et `Create`, qui prend une fabrique validante
retournant `Outcome<TCommand>` dont le résultat est aplati — plutôt qu'un terminal unique
ou deux surcharges portant le même nom.

## Justification

* Des noms distincts donnent à chaque terminal exactement une signature, de sorte que
  l'ambiguïté de surcharge que deux terminaux portant le même nom soulèveraient pour une
  lambda retournant `Outcome<TCommand>` ne peut pas survenir : le problème est supprimé au
  niveau du nom plutôt que contourné au point d'appel.
* Conserver deux terminaux — plutôt qu'un seul terminal exclusivement `Outcome` — permet
  à un constructeur total de rester total : le cas courant construit la commande
  directement sans l'envelopper dans un `Outcome` de succès, tandis que `Create` aplatit
  le cas validant afin que l'`Outcome<TCommand>` d'une fabrique ne soit jamais imbriqué.
  Cela répond aux deux défaillances que décrit le Contexte (l'outcome imbriqué et la règle
  inter-champs sans endroit où s'exécuter).
* Nommer chaque terminal d'après l'assembleur qu'il prend rend le choix auto-sélectif : un
  consommateur qui écrit un `new` se tourne vers `New`, celui qui écrit une fabrique
  `.Create` se tourne vers `Create`. Le nom réutilise la distinction constructeur/fabrique
  que tout développeur C# possède déjà, si bien qu'il ne nécessite aucune recherche
  distincte.
* Le moyen mnémotechnique est cohérent avec l'ADR-0005 plutôt qu'en tension avec lui :
  l'ADR-0005 réserve le nom de fabrique simple à la variante retournant un `Outcome`, et
  `Create` est ici précisément le terminal retournant un `Outcome`. L'axe de l'ADR-0005
  (retourner un `Outcome` versus lever une exception) est orthogonal à celui-ci (un
  assembleur à valeur nue versus un assembleur retournant un `Outcome`) ; aucun des deux
  terminaux ne lève d'exception, si bien qu'aucun marqueur `OrThrow` ne s'applique.
* `Create` retourne l'échec de la fabrique inchangé plutôt que de le ré-envelopper, parce
  qu'une règle inter-champs est une préoccupation métier que la fabrique possède, tandis
  que l'enveloppe du binder regroupe les échecs de binding d'arguments. Garder les deux
  canaux séparés permet à un appelant de distinguer un argument malformé d'une combinaison
  valide rejetée.
* Le statut de pré-version signifie que le nommage est fixé maintenant, alors qu'il n'y a
  aucun consommateur à migrer.

## Alternatives envisagées

### Un terminal unique prenant un assembleur retournant un Outcome

Envisagé parce que c'est la surface la plus réduite — un nom unique, l'aplatissement
couvrant les deux cas dès lors que l'appelant enveloppe un constructeur total dans un
`Outcome` de succès.

Rejeté parce qu'il force le cas courant, qui ne peut pas échouer, à envelopper chaque
construction dans un `Outcome` de succès, exposant une plomberie de résultat dont le cas
total n'a par ailleurs pas besoin.

### Deux surcharges `Build` portant le même nom

Envisagé parce que la surcharge est la façon idiomatique, en C#, d'accepter deux formes
d'arguments sous un même verbe.

Rejeté parce qu'une lambda retournant `Outcome<TCommand>` est applicable aux deux
surcharges avec un `TCommand` inféré différent, que la résolution de surcharge n'en préfère
aucune, et que l'appel ne compile donc pas.

### `Build` et `BuildValidated` (un nom nu, un suffixé)

Envisagé comme des noms distincts qui évitent déjà l'ambiguïté.

Rejeté comme asymétrique : un verbe nu à côté d'une variante suffixée se lit comme « le
vrai et un cas particulier », alors que les deux sont des pairs portant sur des formes
d'assembleur différentes.

### Paires symétriques privilégiant la sémantique (`Assemble` / `Validate`, `BuildFrom` / `BuildThrough`)

Envisagé parce que les mots énoncent la sémantique peut-échouer / ne-peut-pas-échouer pour
un lecteur qui découvre l'API.

Rejeté au profit de `New` / `Create`, qui optimisent plutôt pour le développeur qui écrit
l'appel : le nom correspond au constructeur ou à la fabrique déjà en main, réutilisant une
convention existante plutôt que d'enseigner un nouveau vocabulaire.

## Conséquences

### Positives

* L'ambiguïté de surcharge est impossible : un nom, une signature chacun.
* Un constructeur total reste non enveloppé, et une fabrique validante se compose sans
  `Outcome` imbriqué.
* Le nom du terminal est un moyen mnémotechnique d'usage lié au constructeur ou à la
  fabrique que le consommateur écrit déjà, et réutilise la convention
  `Create`-retourne-`Outcome` du code (ADR-0005).
* Les échecs de binding d'arguments et l'échec inter-champs d'une fabrique restent
  distinguables au point d'appel.

### Négatives

* `New` et `Create` sont des quasi-synonymes en anglais, si bien que « seul `Create` peut
  échouer » est porté par la convention constructeur/fabrique plutôt que par les mots ; un
  lecteur peu familier de la convention doit consulter la documentation de l'API.
* `New` est un identifiant de méthode inhabituel — valide en C# (seul le `new` en
  minuscules est le mot-clé) mais nécessitant un échappement (`[New]`) pour être appelé
  depuis VB.NET.
* Deux terminaux publics et deux types delegate d'assembleur publics à documenter plutôt
  qu'un seul.

### Risques

* Sans outillage qui l'impose, un terminal ultérieur pour une troisième forme d'assembleur
  pourrait s'écarter de la convention et diluer le moyen mnémotechnique ; atténué pour
  l'instant par cette ADR et par la revue.

## Références

* ADR-0005 — réserver le nom de fabrique simple à la variante retournant un Outcome, la
  convention que `Create` réutilise ici.
* ADR-0003 — unifier le mapping de valeur d'Outcome sous `Then`, du contexte sur le
  nommage de la surface Outcome pour l'intention plutôt que pour la mécanique.
* Pull request #126 — la fonctionnalité de request binder à laquelle ce terminal
  appartient.
