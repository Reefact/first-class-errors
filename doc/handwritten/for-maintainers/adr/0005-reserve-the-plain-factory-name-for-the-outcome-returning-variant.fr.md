# ADR-0005 | Réserver le nom de fabrique simple à la variante retournant un Outcome

🌍 🇬🇧 [English](0005-reserve-the-plain-factory-name-for-the-outcome-returning-variant.md) · 🇫🇷 Français (ce fichier)

**Statut :** Accepté
**Date :** 2026-07-16
**Décideurs :** Reefact

## Contexte

FirstClassErrors existe pour rendre explicite le chemin d'échec d'une opération :
une erreur est une valeur qu'un appelant retourne et inspecte
(`Outcome`/`Outcome<T>`), et non une exception qui circule de manière invisible.
La levée d'exception reste prise en charge comme commodité optionnelle — le README
la présente comme un choix (« vous choisissez comment elle circule ») — mais
retourner un `Outcome` est le chemin par défaut et de première classe de la
bibliothèque.

Les opérations de type fabrique se présentent donc couramment en deux variantes
sur les mêmes entrées : l'une retourne `Outcome<T>`, l'autre lève une exception en
cas d'échec (par exemple `Temperature.FromKelvin` / `TryFromKelvin`, celle qui
lève étant implémentée comme la variante Outcome suivie de `GetResultOrThrow()`).
C# ne peut pas surcharger sur le seul type de retour, si bien que les deux
variantes doivent porter des noms distincts — l'une d'elles doit être marquée.

La BCL de .NET possède déjà une convention omniprésente pour l'une de ces formes.
`TryXxx` signifie `bool TryXxx(..., out T result)` : elle retourne un `bool`,
fournit sa valeur via un paramètre `out`, et se lit en ligne dans un `if`
(`if (int.TryParse(s, out var n))`). Les développeurs, les IDE et les analyzers
présument tous cette forme exacte à la vue d'un préfixe `Try`. Deux faits en
découlent :

* **Forme.** Un `TryXxx` dans cette bibliothèque retourne `Outcome<T>` — il
  emprunte le nom sans la forme `bool`+`out`, si bien qu'il ne peut pas être
  utilisé en ligne dans un `if` et rompt discrètement l'attente que le nom
  établit.
* **Disposition.** Dans la BCL, le nom *simple* est celui qui lève (`Parse`) et le
  nom *marqué* est celui qui est sûr (`TryParse`). Le code actuel suit cette
  disposition (`FromKelvin` lève, `TryFromKelvin` est sûr) — plaçant le nom
  simple, non marqué, sur le chemin qui lève, c'est-à-dire le chemin que cette
  bibliothèque traite comme l'exception plutôt que comme le défaut.

`Outcome<T>` expose déjà `GetResultOrThrow()`, si bien que « lever en cas
d'échec » dispose d'un vocabulaire établi dans la bibliothèque.

## Décision

La variante retournant un Outcome porte le nom de fabrique simple (`FromKelvin`,
retournant `Outcome<T>`) ; la variante qui lève est marquée d'un suffixe `OrThrow`
(`FromKelvinOrThrow`).

## Justification

* **Un seul principe, appliqué au défaut de cette bibliothèque.** La convention qui
  mérite d'être conservée de la BCL n'est pas le mot `Try` mais la règle
  sous-jacente : *la variante qui s'écarte du défaut dominant porte le marqueur.*
  Le défaut de la BCL est de lever, donc sa variante sûre est marquée (`Try`) ; le
  défaut de cette bibliothèque est de retourner un `Outcome`, donc sa variante qui
  lève est l'écart et prend le marqueur (`OrThrow`). Même règle, référence opposée
  — ce qui est précisément la raison pour laquelle réutiliser `Try` ici serait
  incohérent, et non un simple conflit de noms.
* **L'appel risqué est celui qui est visible.** Toute la thèse de la bibliothèque
  est qu'une exception est le mode d'échec qui se cache au site d'appel. `OrThrow`
  la remet dans le nom : `FromKelvinOrThrow(k)` annonce qu'elle peut lever ;
  `FromKelvin(k)` annonce qu'elle retourne une valeur à inspecter. Le nom simple et
  facile d'accès est celui qui est sûr.
* **Aucune attente empruntée.** Retirer `Try` supprime la fausse promesse d'une
  forme `bool`+`out` en ligne que la méthode n'a jamais eue.
* **Réutilise le vocabulaire existant.** `OrThrow` fait écho à `GetResultOrThrow()`,
  et la fabrique qui lève est littéralement `Xxx(...).GetResultOrThrow()` — le nom
  reflète l'implémentation au lieu d'introduire un nouveau terme.

## Alternatives envisagées

Le véritable choix porte sur *laquelle des deux variantes porte le marqueur*, et
non simplement sur le mot à utiliser.

### Marquer la variante sûre, laisser celle qui lève simple (la disposition du statu quo)

C'est la disposition de la BCL : le nom simple lève, le nom marqué retourne un
`Outcome`. Quel que soit le marqueur choisi pour le côté sûr — `Try` (statu quo),
`XxxOrError`, `AttemptXxx`, `XxxSafe` — la disposition partage un même défaut :
elle laisse l'appel qui *lève* non marqué, de sorte que le seul appel qui peut
échouer de manière invisible porte le nom le plus court et le plus semblable à un
défaut. Pour une bibliothèque dont la raison d'être est de rendre explicite le
chemin d'échec, c'est à l'envers. `Try` entre de plus en conflit avec la forme de
la BCL ; les autres évitent le conflit mais pas le défaut sous-jacent.

### Marquer la variante qui lève avec un mot différent (`XxxThrows`, `XxxUnsafe`)

Même disposition que la décision, suffixe différent. `OrThrow` est préféré
uniquement parce qu'il existe déjà dans la surface de l'API (`GetResultOrThrow`) ;
introduire un synonyme fragmenterait le vocabulaire.

## Conséquences

### Positives

* Le mode d'échec de chaque fabrique est lisible à partir de son nom au site
  d'appel : un `FromKelvin` simple retourne un `Outcome` à inspecter, tandis que
  `FromKelvinOrThrow` annonce d'emblée qu'il peut lever.
* Le nom simple, celui vers lequel on se tourne le plus, appartient désormais au
  chemin de première classe de la bibliothèque, si bien que la variante vers
  laquelle un appelant se dirige par défaut est celle qui est inspectable.
* Aucune méthode n'annonce la forme `Try`/`out` de la BCL qu'elle n'implémente pas.

### Négatives

* Les fabriques existantes dans `FirstClassErrors.Usage` doivent être renommées :
  les `TryFromKelvin` / `TryFromCelsius` retournant un Outcome perdent le préfixe
  `Try`, et les `FromKelvin` / `FromCelsius` qui lèvent prennent le suffixe
  `OrThrow`. Leurs sites d'appel (par exemple l'initialiseur `AbsoluteZero`) et
  leurs résumés de documentation XML — la formulation « Attempts to create… » —
  doivent être mis à jour en conséquence.
* Les exemples de documentation qui utilisent le préfixe `Try` (`GettingStarted`,
  `UsagePatterns`, en EN comme en FR) doivent être reformulés avec les nouveaux
  noms.
* Il s'agit d'un renommage cassant de membres publics. La bibliothèque est en
  pré-version et non publiée sur NuGet, sans consommateurs externes, si bien
  qu'elle n'entraîne aujourd'hui aucun coût de migration en aval.

### Risques

* Les documentations anglaise et française doivent être renommées de concert ; un
  passage partiel laisserait la documentation canonique et sa traduction décrire
  des API différentes.
* Tant que la convention n'est pas appliquée par l'outillage, elle ne tient que par
  la revue et la documentation, si bien qu'une nouvelle méthode `TryXxx` retournant
  un `Outcome` peut se réintroduire inaperçue.

## Actions de suivi

* Renommer `TryFromKelvin` / `TryFromCelsius` avec le nom simple, ajouter les
  variantes `OrThrow` correspondantes, et mettre à jour leurs sites d'appel et
  leurs résumés de documentation XML.
* Reformuler de concert les exemples de documentation EN + FR qui utilisent le
  préfixe `Try` (`GettingStarted`, `UsagePatterns`).
* L'application de la convention par un analyzer est reportée à une révision
  ultérieure.

## Références

* `README.md` — la formulation « vous choisissez comment elle circule » qui fait de
  `Outcome` le chemin par défaut.
* ADR-0003 — décision antérieure connexe sur le nommage de l'API Outcome (contexte
  uniquement ; pas un précédent pour ce choix).
