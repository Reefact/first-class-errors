# ADR-0022 | Fixer le floor .NET Framework de la librairie à 4.7.2

🌍 🇬🇧 [English](0022-floor-the-library-on-net-framework-4-7-2.md) · 🇫🇷 Français (ce fichier)

**Statut :** Accepté
**Date :** 2026-07-19
**Décideurs :** Reefact

## Contexte

Les librairies livrées — `FirstClassErrors`, `FirstClassErrors.Testing` et
`FirstClassErrors.RequestBinder` — ciblent **`netstandard2.0`**. Un assembly
`netstandard2.0` est *consommable* par n'importe quel runtime qui implémente le
standard, et le standard désigne **.NET Framework 4.6.1** comme son minimum sur cette
plateforme.

Ce minimum 4.6.1 est rétro-ajouté. `netstandard2.0` est sorti après .NET Framework
4.6.1, et sa prise en charge a été greffée après coup : sur 4.6.1 à 4.7.1, la façade
`netstandard.dll` et un ensemble de shims `System.*` sont livrés comme des ressources
NuGet et exigent des *binding redirects* côté consommateur pour se charger. **.NET
Framework 4.7.2 est la première version qui embarque ces façades in-box**, et la
recommandation officielle de Microsoft pour `netstandard2.0` est d'utiliser 4.7.2 ou
ultérieur.

Le support renforce la même ligne. .NET Framework 4.6, 4.6.1 (et 4.5.2) sont en fin
de support depuis avril 2022 ; 4.6.2 est la plus ancienne encore maintenue ; et
**4.8.1 est la dernière version de .NET Framework** — il n'y aura pas de 4.9.

Jusqu'ici le produit annonçait, dans `FirstClassErrors/README.nuget.md` et comme une
phrase incidente de la Justification de l'[ADR-0002](0002-floor-the-tooling-runtime.fr.md),
que la librairie « tourne sur .NET Framework 4.6.1+ ». Rien dans la CI n'a jamais
chargé les assemblies sur un runtime .NET Framework : `build-test` exécute la suite sur
.NET 10 et le job `floor` n'exécute que *l'outillage* sur le runtime .NET 8. L'annonce
de compatibilité n'a donc jamais été vérifiée.

La stack de test est **xUnit v3**, dont la cible .NET Framework la plus basse est
**`net472`** ; il n'existe aucun moyen supporté de faire tourner ces projets de tests
sur un .NET Framework antérieur. La CI garde déjà les deux autres bornes runtime du
produit : le floor .NET 8 de l'outillage ([ADR-0002](0002-floor-the-tooling-runtime.fr.md))
et, en amont de la publication, la prochaine preview de .NET (`canary.yml`).

## Décision

Le floor .NET Framework supporté pour les librairies `netstandard2.0` est **4.7.2**.

## Justification

4.7.2 est la plus basse version de .NET Framework sur laquelle la librairie tourne
*sans plomberie côté consommateur* : c'est la première à embarquer les façades
`netstandard2.0` in-box, si bien que la promesse « tourne presque partout » devient
vraie au lieu d'être conditionnée à des *binding redirects*. C'est donc le floor
honnête, là où 4.6.1 était le floor théorique.

Une annonce de support ne vaut que ce qui la vérifie. La ligne 4.6.1 n'a jamais été
exercée, ce qui est un passif pour une librairie dont la raison d'être est la
diagnosticabilité en production. Fixer le floor à 4.7.2 rend l'annonce *vérifiable à
chaque pull request*, car 4.7.2 est aussi le plus bas framework sur lequel la stack de
test elle-même peut tourner — le même nombre clôt à la fois la question du support et
celle de la vérification.

4.6.x est le mauvais endroit pour ancrer le garde-fou. 4.6 et 4.6.1 sont en fin de vie,
et la fragilité façade-et-binding-redirects de 4.6.1–4.7.1 rendrait un signal rouge
ambigu — la faute de la librairie, ou celle de la plateforme ? Un floor existe pour
donner un signal sans ambiguïté, et seule 4.7.2 en fournit un avec la stack actuelle.

Fixer le floor de la librairie à 4.7.2 est le symétrique du floor .NET 8 de
l'outillage : chaque borne runtime supportée est prouvée par son propre job, de sorte
que le produit énonce ses runtimes supportés précisément plutôt que par affirmation.
Cette décision **précise** la phrase incidente « 4.6.1 » de
l'[ADR-0002](0002-floor-the-tooling-runtime.fr.md) sans la remplacer : la décision de
cet ADR est le floor net8 de *l'outillage*, non le floor .NET Framework de la
librairie, qui n'avait pas d'ADR propre jusqu'ici.

## Alternatives envisagées

### Continuer d'annoncer .NET Framework 4.6.1+ (statu quo)

Envisagé parce que c'est le minimum `netstandard2.0` sur le papier et que cela
n'exige aucun changement.

Rejeté parce que cela n'a jamais été vérifié et ne peut l'être à moindre coût : la
prise en charge de `netstandard2.0` par 4.6.1 est rétro-ajoutée et fragile, les
versions concernées sont largement en fin de vie, et xUnit v3 ne peut pas cibler en
dessous de `net472` — prouver l'annonce demanderait une seconde stack de test et des
*binding redirects* côté consommateur, pour un runtime que presque plus personne ne
devrait déployer.

### Fixer le floor à 4.6.2 (la plus ancienne 4.6.x encore maintenue)

Envisagé parce que 4.6.2, contrairement à 4.6/4.6.1, est encore maintenue, ce qui
garderait le plus bas numéro encore supporté.

Rejeté parce que 4.6.2 précède les façades in-box : elle porte la même fragilité de
*binding redirects* que 4.6.1, et reste sous le floor de xUnit v3 — elle demeure donc
invérifiable avec la stack actuelle, et le signal du garde-fou resterait ambigu.

### Fixer le floor des librairies sur toute la matrice .NET moderne (net6/net8/… en jambes bloquantes)

Envisagé comme la réponse conventionnelle « tester sur tout » pour une réassurance
large.

Rejeté parce que la librairie est un unique assembly `netstandard2.0` et que les
runtimes modernes forment une seule famille CoreCLR : le delta comportemental entre
majors, pour des value objects sans dépendance, est négligeable ; les majors en fin de
vie ne devraient pas être « floorés » du tout ; et une matrice par-major réintroduit
exactement le tapis roulant par-release que l'[ADR-0002](0002-floor-the-tooling-runtime.fr.md)
a rejeté. La seule frontière qui compte est .NET Framework versus .NET moderne, que
`net472` couvre, tandis que le dernier runtime (`build-test`) et la prochaine preview
(`canary.yml`) couvrent déjà l'extrémité moderne.

## Conséquences

### Positives

* Le support .NET Framework annoncé est désormais **vérifié à chaque pull request** par
  un job Windows dédié, et non plus seulement affirmé.
* Le floor est **gelé** : 4.8.1 est la dernière version de .NET Framework, donc ce
  garde-fou ne poursuit jamais une cible mouvante et n'exige aucun entretien
  par-release.
* L'histoire des runtimes supportés du produit est symétrique et précise : un floor
  .NET Framework de la librairie (4.7.2) et un floor de l'outillage (.NET 8), chacun
  prouvé par son propre job, avec la prochaine preview surveillée en amont.

### Négatives

* Les consommateurs figés sur .NET Framework 4.6.1–4.7.1 perdent une annonce de
  support qui n'avait jamais été vérifiée ; ils doivent être sur 4.7.2 ou ultérieur.
  Accepté : 4.7.2 est le floor `netstandard2.0` pratique et les versions inférieures
  sont largement en fin de vie.
* Un petit polyfill `IsExternalInit` réservé aux tests et un chemin de build
  conditionné à `net472` sont ajoutés aux projets de tests concernés. Les **librairies
  livrées ne sont pas touchées** — elles n'utilisent ni `init` ni records — donc rien
  dans le produit ne dépend du polyfill.

### Risques

* La jambe `net472` ne tourne que sous Windows ; une régression spécifique à
  .NET Framework est invisible sur les jambes Linux jusqu'à l'exécution du job Windows.
  Atténué en exécutant le job à chaque pull request.
* `FirstClassErrors.RequestBinder.UnitTests` ne peut pas rejoindre le floor car ses
  fixtures lient `DateOnly`, un type .NET 6+ absent de .NET Framework ; RequestBinder
  est « flooré » via ses property tests à la place. Accepté : les scénarios exclus
  exercent un type qui ne peut de toute façon pas exister sur `net472`.

## Actions de suivi

* Indiquer 4.7.2+ dans `FirstClassErrors/README.nuget.md` (fait dans ce changement).
* Ajouter le job `framework-floor` à `ci.yml` et le fichier partagé
  `build/Net472TestFloor.props` qui porte la jambe `net472` conditionnée (fait dans ce
  changement).
* Faire de `framework-floor` un **status check requis** dans la protection de branche
  pour qu'il bloque les merges, conformément à l'intention que le floor soit imposé, et
  non pas indicatif.
* Si un mainteneur souhaite réconcilier la phrase incidente « 4.6.1 » de
  l'[ADR-0002](0002-floor-the-tooling-runtime.fr.md), y ajouter une note d'errata
  renvoyant à cet ADR ; cet ADR fait autorité sur le floor .NET Framework de la
  librairie.

## Références

* [ADR-0002](0002-floor-the-tooling-runtime.fr.md) — le floor du runtime de
  l'outillage ; la décision sœur, sur une borne runtime, que cet ADR précise.
* [ADR-0001](0001-lock-the-analyzer-roslyn-floor.fr.md) — le floor Roslyn de
  l'analyseur, la troisième borne d'outillage supporté.
* `FirstClassErrors/README.nuget.md` — l'énoncé de support côté utilisateur.
* `build/Net472TestFloor.props`, le job `framework-floor` dans
  `.github/workflows/ci.yml`, et l'exécution des tests de librairies sur la preview
  dans `.github/workflows/canary.yml` — là où cette décision est imposée.
