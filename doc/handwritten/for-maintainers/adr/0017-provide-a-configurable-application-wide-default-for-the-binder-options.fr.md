# ADR-0017 | Fournir un défaut d'options configurable à l'échelle de l'application

🌍 🇬🇧 [English](0017-provide-a-configurable-application-wide-default-for-the-binder-options.md) · 🇫🇷 Français (ce fichier)

**Statut :** Accepté
**Date :** 2026-07-18
**Décideurs :** Reefact

## Contexte

* `Bind.PropertiesOf(request)` lie avec `RequestBinderOptions.Default`. Lier avec des
  options personnalisées exige sinon `Bind.WithOptions(options).PropertiesOf(...)` — faire
  transiter le point d'entrée configuré par chaque appel, ou le résoudre depuis un
  conteneur DI.
* Un hôte sans conteneur DI — une CLI, un worker, un petit outil — n'a aucun moyen
  host-agnostic de poser un défaut applicatif que le simple `Bind.PropertiesOf` ramasse.
* L'ADR-0012 a fixé les options d'un binder à son point d'entrée (aucun changement une
  fois la liaison commencée) et, parmi ses alternatives rejetées, a écarté « un défaut
  ambiant à l'échelle du processus, configuré une fois (un `Configure` statique) » au
  motif qu'il introduirait un état global mutable, fuirait entre les tests exécutés en
  parallèle, et pourrait être configuré au mauvais moment.
* `RequestBinderOptions` est une valeur immuable : une instance partagée ne porte aucun
  état de settings mutable, donc le hasard classique — muter un objet de settings partagé
  pendant qu'il est utilisé — ne s'y applique pas.
* La convention .NET est partagée : `JsonConvert.DefaultSettings` de `Newtonsoft.Json` est
  un global librement re-posable ; `JsonSerializerOptions` de `System.Text.Json` devient
  immuable à la première utilisation (gelé) et se configure par-instance ou via DI.
* La bibliothèque n'expose aucun autre état ambiant mutable : les points d'extension de
  l'horloge, de l'identifiant d'instance et des valeurs arbitraires sont des défauts
  immuables avec un override `AsyncLocal`, scoped, réservé aux tests (ADR-0006).
* La bibliothèque est en pré-version, non publiée sur NuGet et sans consommateur externe.

## Décision

`RequestBinderOptions.Default` — les options avec lesquelles `Bind.PropertiesOf` lie —
est un défaut applicatif posable, configuré une fois au démarrage de l'application et gelé
à la première liaison qui le lit.

## Justification

* Un défaut processus posable est le seul moyen host-agnostic pour que le simple
  `Bind.PropertiesOf` ramasse une politique applicative sans conteneur DI, ce dont une CLI
  ou un worker a besoin ; le point d'entrée favorable à la DI (`Bind.WithOptions`) reste
  disponible là où un conteneur existe.
* Le hasard contre lequel l'ADR-0012 se prémunissait — un défaut ambiant qui dérive à
  l'exécution — est supprimé par le gel à la première utilisation : dès que la première
  liaison le lit, une réaffectation lève, donc c'est un choix au moment de la composition
  qui ne peut pas changer une fois les requêtes en vol. C'est la discipline de
  `System.Text.Json` (immuable une fois utilisé), pas celle, librement mutable, de
  `JsonConvert.DefaultSettings`.
* Parce que `RequestBinderOptions` est immuable, un défaut partagé ne porte aucun état de
  settings mutable, donc le piège classique des settings globaux ne s'applique pas ; le
  seul état global est vers quelles options immuables pointe le défaut, fixé une fois.
* Garder la configuration sur `RequestBinderOptions.Default` plutôt que sur une méthode de
  `Bind` laisse le point d'entrée de liaison sans surface de configuration : un
  développeur qui lie des requêtes ne voit que des verbes de liaison.
* La préoccupation d'isolation des tests parallèles soulevée par l'ADR-0012 se limite aux
  tests de la bibliothèque elle-même, et est satisfaite par un override scoped réservé aux
  tests (un `AsyncLocal`) — le même patron que l'horloge (ADR-0006) — qui ne touche ni ne
  gèle jamais le défaut de production.
* Le statut de pré-version signifie que la surface est arrêtée maintenant, quand il n'y a
  aucun consommateur à migrer.

## Alternatives considérées

### Garder les options au seul point d'entrée (le statu quo de l'ADR-0012)

Considérée parce qu'elle est déjà livrée et n'a aucun état global.

Rejetée parce qu'elle n'offre aucun défaut applicatif host-agnostic : chaque point d'appel
doit faire transiter le point d'entrée configuré, ou un conteneur DI doit le fournir —
indisponible pour une CLI ou un worker qui veut configurer le binder une fois.

### Un défaut global librement re-posable (le modèle JsonConvert.DefaultSettings)

Considérée parce que c'est le global posable le plus simple et une convention répandue.

Rejetée parce qu'un défaut réaffectable pendant que les requêtes sont en vol peut dériver,
réintroduisant le hasard de configuration à l'exécution contre lequel l'ADR-0012
prévenait. Le gel à la première utilisation garde l'ergonomie tout en supprimant la
dérive.

### L'injection de dépendances seule (le modèle System.Text.Json / ASP.NET)

Considérée parce que c'est l'idiome moderne là où un conteneur existe, et pleinement
sûre vis-à-vis des tests.

Rejetée comme unique mécanisme parce qu'elle n'est pas host-agnostic : une CLI, un worker,
ou tout hôte sans conteneur DI ne peut pas s'en servir pour que le simple
`Bind.PropertiesOf` ramasse un défaut applicatif. Le point d'entrée injecté reste
disponible ; cette décision ajoute le chemin sans conteneur.

## Conséquences

### Positives

* N'importe quel hôte — avec ou sans DI — configure la politique de nommage et les codes
  structurels du binder une fois au démarrage, et le simple `Bind.PropertiesOf` les
  utilise.
* Le gel à la première utilisation empêche la dérive à l'exécution ; le seul global mutable
  est une référence d'options immuables posée une fois.
* La surface de `Bind` reste sans configuration ; un `Bind.WithOptions` par appel surcharge
  quand même le défaut.

### Négatives

* La bibliothèque gagne un état global de processus (le défaut posable) — le premier de la
  bibliothèque, accepté délibérément pour l'ergonomie host-agnostic.
* Les tests de la bibliothèque ont besoin d'un seam d'override scoped réservé aux tests pour
  rester parallèle-safe ; le défaut de production n'est pas directement posable dans une
  suite parallèle.

### Risques

* Un consommateur qui lit `RequestBinderOptions.Default` avant de le configurer le gèle et
  ne peut alors plus le configurer ; atténué par le diagnostic du setter qui lève
  (« configurez au démarrage, avant la première liaison ») et par la documentation.

## Actions de suivi

* Faire apparaître le seam d'override de test aux consommateurs via un paquet de test dédié
  si une demande apparaît (il est actuellement interne, pour les tests de la bibliothèque).

## Références

* ADR-0012 — fixer les options du binder avant le début de la liaison ; cette décision
  revisite le défaut ambiant à l'échelle du processus que l'ADR-0012 avait pesé puis
  rejeté comme alternative, en l'adoptant avec des garde-fous. La décision propre à
  l'ADR-0012 — les options d'un binder sont fixées à son point d'entrée — est inchangée,
  donc l'ADR-0012 n'est pas supersédé.
* ADR-0006 — fournir les valeurs de test arbitraires depuis une source unique réamorçable ;
  le patron de seam de test `AsyncLocal` que cette décision réutilise pour ses tests.
* Issue #181 — la demande que cette décision résout.
* `JsonConvert.DefaultSettings` (Newtonsoft.Json) et `JsonSerializerOptions`
  (System.Text.Json) — les deux conventions pesées.
