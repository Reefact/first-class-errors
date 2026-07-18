# ADR-0015 | Plafonner Any.Combine à l'arité huit

🌍 🇬🇧 [English](0015-cap-any-combine-at-arity-eight.md) · 🇫🇷 Français (ce fichier)

**Statut :** Proposé
**Date :** 2026-07-18
**Décideurs :** Reefact

## Contexte

`Dummies` compose des générateurs contraints en objets plus gros via
`Any.Combine(parties..., compose)` : les parties sont générées, puis remises à une
lambda constructeur fournie par l'appelant, de sorte qu'un objet-valeur ou un
agrégat est assemblé sans réflexion et que le constructeur du domaine reste le seul
gardien. Construire facilement de gros objets est un objectif explicite de la
bibliothèque.

C# n'a pas de générique variadique hétérogène : passer *N* parties de types
différents en un seul appel exige une surcharge distincte par arité, chacune avec
*N*+1 paramètres de type générique et *N*+1 paramètres de valeur. Jusqu'ici,
`Combine` n'existait que pour deux et trois parties ; en composer davantage
imposait l'imbrication (`Combine(Combine(a, b, …), c, …)`) ou le passage par des
tuples, qui exposent tous deux un accès positionnel `Item1..ItemN` ou des lambdas
imbriquées au site d'appel.

Le dépôt exécute une analyse SonarCloud dont la règle S107 signale une méthode à
plus de sept paramètres. Un `Combine` de sept parties a huit paramètres (sept
générateurs plus le composeur) et de huit parties, neuf — les deux plus grandes
surcharges franchissent donc ce seuil. La pratique établie du dépôt est de garder
l'analyse propre, en supprimant une règle en ligne avec une justification là où une
exception délibérée est faite.

Un constructeur qui a besoin de beaucoup de parties est lui-même souvent un signal
de conception — un objet-valeur intermédiaire manquant.

## Décision

`Any.Combine` offre des surcharges de deux jusqu'à huit parties et s'arrête là, en
acceptant le *code smell* de nombre de paramètres et de génériques des deux plus
grandes surcharges comme un compromis délibéré en faveur d'un site d'appel de
composition plat et sans réflexion.

## Justification

* **Cela sert directement l'objectif « construire de gros objets ».** Un
  `Combine(a, b, c, d, e, (…) => new Thing(…))` plat, avec des paramètres de lambda
  nommés par l'appelant, se lit bien mieux que des `Combine` imbriqués ou un accès
  tuple `Item1..ItemN`, et garde la composition sans réflexion — la raison d'être
  même de `Combine`.
* **Huit est le bon endroit pour le plafond.** Huit parties couvrent la quasi-
  totalité des constructeurs DDD écrits à la main ; au-delà, l'objet est assez
  complexe pour que des objets-valeurs intermédiaires soient la conception plus
  saine, donc un plafond à huit pousse vers cette structure plutôt que de lisser des
  constructeurs arbitrairement larges.
* **Le smell est inhérent, pas accidentel.** Il n'existe pas de façon moins
  « smell » de passer *N* parties de types différents en un seul appel ; le nombre
  de paramètres et de génériques est le coût irréductible de la composition
  hétérogène. Supprimer S107 avec une justification sur les deux plus grandes
  surcharges enregistre ce compromis au niveau du code, et cet ADR enregistre
  pourquoi il est acceptable.
* **S'arrêter avant seize garde la surface bornée.** Égaler le plafond de seize
  arguments de `Func` ajouterait des surcharges maintenues à la main et entièrement
  documentées dont la valeur marginale est faible et dont le coût en boilerplate est
  réel ; la demande au-delà de huit ne le justifie pas.

## Alternatives considérées

### Ne garder que les arités deux et trois ; composer davantage par imbrication

Considérée parce qu'elle n'ajoute aucune surface. Rejetée parce que l'imbrication
force un accès tuple positionnel (`Item1..ItemN`) ou des lambdas imbriquées au site
d'appel — illisible précisément là où la bibliothèque promet une construction facile
de gros objets.

### Un builder fluide accumulant un tuple (`Combine(a).And(b).And(c)…`)

Considérée comme moyen d'éviter une surcharge par arité. Rejetée parce que `.And`
aurait lui-même besoin d'une surcharge par arité source, et que le tuple accumulé
réexpose l'accès positionnel — elle échange une forme de boilerplate contre un site
d'appel pire.

### Étendre jusqu'à l'arité seize

Considérée par souci d'exhaustivité, pour égaler `Func`. Rejetée parce que les
constructeurs de neuf à seize parties sont un *code smell* que la bibliothèque ne
devrait pas lisser, et que chaque surcharge est une surface documentée et maintenue
à la main pour une demande réelle négligeable.

### Un tableau `params` de générateurs de même type

Considérée pour le cas homogène. Rejetée parce qu'elle ne fonctionne que si toutes
les parties partagent un même type et qu'elle perd le typage par partie — elle ne
sert pas le cas du constructeur hétérogène pour lequel `Combine` existe. Elle reste
une option orthogonale, ajoutable plus tard sans toucher à cette décision.

## Conséquences

### Positives

* Les gros objets-valeurs et agrégats se composent en un seul appel plat, lisible et
  sans réflexion, avec des paramètres nommés par l'appelant.
* Le plafond oriente doucement les constructeurs très larges vers des objets-valeurs
  intermédiaires.

### Négatives

* Cinq surcharges de plus sur la façade, maintenues à la main et entièrement
  documentées.
* Les deux plus grandes surcharges portent une suppression S107 en ligne — une
  exception documentée et localisée à la règle du nombre de paramètres.

### Risques

* **Pression sur le plafond** — un besoin réel de neuf parties ou plus pourrait
  réapparaître. Atténué parce que c'est en soi un signal de conception ; le plafond
  n'est réexaminé que si le besoin se révèle courant, et ajouter des arités
  supérieures plus tard reste non-breaking.

## Actions de suivi

* Si un besoin de composition homogène (même type) apparaît, envisager un `Combine`
  à base de `params` séparément — c'est orthogonal à cette décision.
* Refléter le plafond d'arité dans la documentation utilisateur une fois la surface
  stabilisée.

## Références

* ADR-0011 — Héberger Dummies comme un paquet autonome dans ce dépôt.
* Les suppressions S107 sur les surcharges `Combine` d'arité sept et huit.
