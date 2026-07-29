# ADR-0058 | Supprimer CA1510 tant que le plancher antérieur à .NET 6 tient

🌍 🇫🇷 Français (ce fichier) · 🇬🇧 [English](0058-suppress-ca1510-while-the-netstandard-floor-stands.md)

**Statut :** Accepté
**Proposé :** 2026-07-29
**Accepté :** 2026-07-29
**Décideurs :** Reefact

## Contexte

`CA1510` demande que toute garde d'argument de la forme

```csharp
if (source is null) { throw new ArgumentNullException(nameof(source)); }
```

soit réécrite en `ArgumentNullException.ThrowIfNull(source);`. L'aide est plus
concise, et elle porte `[CallerArgumentExpression]`, ce qui évite de répéter le
nom du paramètre.

Le rapport SonarQube Cloud en compte **323** — de loin le plus gros groupe de
constats du projet, environ 55 % de tous les code smells. Ils se répartissent en
deux populations que le rapport présente à l'identique et qui ne le sont pas :

* **314 dans `JustDummies`** et **1 dans `JustDummies.UnitTests`**. Les deux
  projets sont multi-ciblés de part et d'autre de la frontière .NET 6 —
  `netstandard2.0;net8.0` pour la bibliothèque, `net10.0;net472` pour sa suite de
  contrat sur le plancher de support (ADR-0022). `ArgumentNullException.ThrowIfNull`
  est arrivée avec .NET 6 : l'analyzer la voit sur la jambe moderne et signale
  chaque garde, alors que *le même fichier source* doit continuer à compiler sur
  la jambe qui ne l'a pas.
* **8 dans `FirstClassErrors.GenDoc`**, qui ne cible que `net8.0`. Là, rien ne
  s'y oppose.

L'échappatoire évidente — un polyfill — n'existe pas pour cette API. Un polyfill
fonctionne quand le compilateur lie par le nom et que la forme est purement
compile-time : un attribut comme `CallerArgumentExpressionAttribute` se déclare
dans son propre assembly et le compilateur le reconnaît. `ThrowIfNull` n'est ni
l'un ni l'autre. C'est une **méthode statique sur un type BCL qui existe déjà en
downlevel**, et C# n'a pas de méthodes d'extension statiques : la seule façon de
la fournir serait de déclarer un `System.ArgumentNullException` concurrent qui
gagne la résolution de nom sur l'ancienne jambe. Masquer un type d'exception du
framework pour satisfaire une règle de style échange un gain cosmétique contre un
piège.

`CA1510` est signalée en sévérité **Info**. Elle n'a jamais fait échouer un
build, et elle ne porte ni sur la fiabilité ni sur la sécurité.

## Décision

`CA1510` est supprimée, par projet et avec la raison inscrite dans le fichier
projet, pour les deux projets qui doivent compiler sous .NET 6 ; elle est
honorée partout où le plancher ne s'applique pas, et les huit gardes de
`FirstClassErrors.GenDoc` sont réécrites avec l'aide.

## Justification

* **La règle est insatisfiable là où elle crie le plus fort.** 315 des 323
  constats sont dans du source qui doit compiler sur un framework cible dépourvu
  de l'API. Aucune modification de ces fichiers ne les résout ; seul un
  déplacement du plancher le ferait.
* **Les alternatives coûtent plus que la règle ne vaut.** Réécrire chaque garde
  en appel à une aide maison `Guard.NotNull` toucherait 315 sites d'appel,
  ajouterait une indirection à chaque vérification d'argument, et perdrait
  précisément le comportement `[CallerArgumentExpression]` qui motive la règle.
  Encadrer chaque garde d'un `#if NET6_0_OR_GREATER` doublerait le nombre de
  lignes de toutes les gardes de la bibliothèque.
* **Une suppression qui porte sa raison vaut mieux qu'une suppression muette.**
  Le `NoWarn` est dans les deux fichiers projet qui portent la contrainte, à côté
  d'un commentaire qui nomme le plancher et cette ADR : le prochain mainteneur
  lit la raison là où il rencontre l'effet, et sait ce qui la rendra caduque.
* **La suppression est cantonnée, pas globale.** Elle n'est ni dans
  `Directory.Build.props` ni dans `.editorconfig` : un projet qui ne franchit pas
  la frontière conserve la règle. `FirstClassErrors.GenDoc` le démontre en s'y
  conformant.
* **Elle expire d'elle-même.** Le jour où `JustDummies` abandonnera
  `netstandard2.0` et où la suite de tests abandonnera `net472`, les lignes
  `NoWarn` deviendront mortes et la règle pourra être honorée partout. Il n'y a
  rien d'autre à se rappeler.

## Alternatives envisagées

### Réécrire les gardes via une aide maison `Guard.NotNull`

Une aide interne unique, appelée depuis chaque garde, supprimerait le motif que
l'analyzer reconnaît : la règle se tairait sans aucune suppression.

Rejetée parce qu'elle modifie 315 sites d'appel sans rien acheter que le lecteur
souhaitait : la garde ne se lit pas mieux, chaque vérification d'argument gagne
un niveau d'indirection, et l'ergonomie `[CallerArgumentExpression]` qui rend
`ThrowIfNull` attrayante n'est de toute façon pas reproductible sur
`netstandard2.0`. Elle inventerait aussi un second idiome de garde à côté de
celui qu'utilise le reste du dépôt.

### Encadrer chaque garde d'un `#if NET6_0_OR_GREATER`

Strictement correct, et honore la règle sur la jambe moderne.

Rejetée pour la lisibilité : cela transforme une garde d'une ligne en cinq, 315
fois, dans une bibliothèque dont les gardes d'arguments sont les lignes les plus
lues.

### Polyfiller `ArgumentNullException.ThrowIfNull`

Envisagée en premier, et la raison d'être de cette ADR. Rejetée parce qu'elle
n'est pas réalisable : le membre est statique sur un type qui existe déjà en
downlevel, et le fournir exigerait de masquer `System.ArgumentNullException`
lui-même.

### Supprimer globalement dans `Directory.Build.props` ou `.editorconfig`

Moins cher encore — une ligne pour tout le dépôt.

Rejetée parce qu'elle éteindrait la règle pour des projets qui *peuvent*
l'honorer, `FirstClassErrors.GenDoc` en tête, et parce que l'`.editorconfig` de
ce dépôt ne porte délibérément aucune sévérité de diagnostic (il le dit en
en-tête : le style et les sévérités d'inspection sont l'affaire du DotSettings).

### Abandonner `netstandard2.0` dans `JustDummies`

Résout le constat purement et simplement.

Rejetée parce que le plancher est une promesse produit, pas un détail
d'implémentation : la portée du package — et le plancher de support .NET
Framework 4.7.2 que consigne l'ADR-0022 — vaut plus qu'une règle de style.

## Conséquences

### Positives

* 323 constats disparaissent : 315 par une suppression qui énonce sa raison, 8
  en s'y conformant.
* La contrainte est écrite là où elle mord, si bien que le prochain lecteur n'a
  pas à la redécouvrir depuis une erreur de compilation.
* Les projets qui peuvent honorer la règle continuent de le faire, et les
  nouveaux en héritent.

### Négatives

* Deux fichiers projet portent un `NoWarn` qu'il faudra retirer quand le
  plancher bougera ; rien n'impose ce retrait au-delà de cette ADR.
* Une nouvelle garde écrite dans `JustDummies` ne sera pas orientée vers l'aide
  moderne sur la jambe moderne, puisque la règle est éteinte pour tout le projet
  plutôt que pour la seule construction interne downlevel.

### Risques

* Ne lire que le compte (« 55 % des smells partis ») surestime le changement.
  Rien n'a été amélioré dans le code pour les 315 ; seul le rapport l'a été. Les
  huit réécritures de `FirstClassErrors.GenDoc` constituent l'intégralité du
  changement substantiel.
* Un contributeur futur pourrait prendre ce `NoWarn` pour une licence à ignorer
  d'autres conseils de l'analyzer dans ces projets. Il est cantonné à un seul
  identifiant de règle précisément pour rendre cette lecture difficile à tenir.

## Actions de suivi

* Retirer les deux entrées `NoWarn`, et la raison d'être de cette ADR, si et
  quand `JustDummies` abandonnera `netstandard2.0` et `JustDummies.UnitTests`
  `net472`.

## Références

* ADR-0022 — le plancher de support .NET Framework 4.7.2 auquel ces projets sont tenus.
* ADR-0011 — `JustDummies` comme package autonome, dont le plancher sert la portée.
* `JustDummies/JustDummies.csproj`, `JustDummies.UnitTests/JustDummies.UnitTests.csproj` — où vit la suppression.
