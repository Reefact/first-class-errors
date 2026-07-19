# ADR-0023 | Élaguer les générateurs numériques de largeur exotique (128 bits et Half)

🌍 🇬🇧 [English](0023-prune-the-exotic-width-numeric-generators.md) · 🇫🇷 Français (ce fichier)

**Statut :** Proposé
**Date :** 2026-07-19
**Décideurs :** Reefact

## Contexte

`Dummies` fournit une large matrice numérique pour qu'un test puisse demander une
valeur arbitraire de presque tout type de nombre intégré : `Byte`/`SByte`, les
entiers 16/32/64 bits, `Int128`/`UInt128`, `Decimal` et
`Double`/`Single`/`Half`. Le but est de produire des dummies *facilement* —
couvrir tous les cas simples et une majorité des cas complexes plausibles — sans
devenir un solveur de contraintes.

Deux de ces types portent un coût disproportionné par rapport à leur usage réel :

* **`Int128`/`UInt128`** reposent sur leur propre moteur, `WideIntervalSpec` —
  un clone `UInt128` d'~185 lignes d'`OrdinalIntervalSpec`, le *quatrième*
  moteur d'intervalle de la bibliothèque, n'existant que pour ces deux
  générateurs. Un dummy 128 bits *contraint* (une plage bornée, une liste
  blanche, une exclusion) est une fraction négligeable de l'usage réel ; le type
  est net8 uniquement, donc même pas à portée d'un débutant.
* **`Half`** repose sur le moteur continu partagé `ContinuousIntervalSpec` : il
  ne coûte donc aucun moteur — seulement ~207 lignes de surface — mais un dummy
  `Half` est un besoin rare (interop ML/GPU).

Le coût est devenu concret en étendant le périmètre anticipé de cardinalité pour
que *tout* générateur à domaine fini contrôle les collections distinctes de façon
uniforme (le travail « tenir la promesse partout » derrière
[ADR-0013](0013-gate-distinct-collections-by-cardinality-else-bounded-draw.md)).
En les rentrant dans le périmètre, on a découvert que `WideIntervalSpec` avait
déjà **silencieusement dérivé** hors de la capacité de cardinalité antérieure —
il n'annonçait ni cardinalité ni appartenance — donc le clone était une dette de
maintenance vivante, et pas seulement des lignes dormantes. Chaque moteur
d'intervalle conservé est un endroit où le prochain invariant transverse devra
être tissé à la main.

Retirer des types de générateurs publics est un **changement cassant** qui touche
le socle de types supportés : c'est donc une décision produit délibérée, pas un
nettoyage de code — d'où cet ADR.

## Décision

Retirer `AnyInt128`, `AnyUInt128` et le moteur `WideIntervalSpec` qui n'existe
que pour eux ; et retirer `AnyHalf`. Conserver toute la matrice entière jusqu'à
64 bits et les générateurs flottants `Decimal`/`Double`/`Single` — la largeur que
l'on utilise réellement. Un test ayant besoin d'un dummy 128 bits ou `Half`
contraint utilise un littéral, ou un cast depuis le générateur immédiatement plus
large (`(Half)Any.Single().Between(...)`, un cast `Int64`).

## Justification

* **Le périmètre, pas l'exhaustivité.** Une bibliothèque de dummies vaut par la
  trivialité des cas courants et la facilité de la majorité des cas complexes —
  non par la couverture de tout type numérique du BCL à la même profondeur.
  `Int128`/`UInt128`/`Half` sont la traîne où la demande de dummy contraint tend
  vers zéro.
* **Un moteur de moins à tenir honnête.** Supprimer `WideIntervalSpec` retire un
  moteur d'intervalle entier — un vrai concept, pas seulement des lignes — et
  avec lui l'un des quatre endroits où chaque futur invariant transverse
  (cardinalité, appartenance, une future capacité) devrait être ré-implémenté et
  maintenu synchronisé. La dérive déjà observée en est la preuve.
* **L'échappatoire est bon marché.** La régression est bornée et locale : un cast
  ou un littéral au point d'appel couvre tous les cas que servaient les
  générateurs retirés, donc aucun test réaliste ne perd une capacité qu'il ne
  puisse trivialement retrouver.

## Alternatives considérées

### Tout garder en l'état

Le statu quo. Rejeté parce qu'il préserve un moteur entier et ~560 lignes pour un
bénéfice réel quasi nul et que — comme l'a montré la dérive silencieuse de
`WideIntervalSpec` — cette surface pourrit activement, taxant chaque futur
invariant.

### Ne couper que les générateurs 128 bits, garder `Half`

Défendable : `Half` repose sur le moteur continu partagé, donc il ne coûte aucun
*concept*, seulement des lignes, et notre étalon pondéré par les concepts traite
la largeur lisible sur un moteur partagé comme quasi gratuite (le même
raisonnement qui garde `Byte`…`UInt64`). C'est le repli naturel si `Half` s'avère
avoir de vrais utilisateurs ; la coupe 128 bits (qui retire le moteur) est la
partie porteuse.

### Unifier les moteurs d'intervalle au lieu d'élaguer

Refondre `WideIntervalSpec` dans un moteur d'intervalle générique partagé avec
`OrdinalIntervalSpec`. Rejeté : la generic math (`INumber<T>`) est net8
uniquement, donc sur le socle .NET Standard 2.0 il n'y a aucune abstraction
partagée *vers laquelle* unifier — les options honnêtes sont « garder le clone »
ou « retirer les types », pas « fusionner ».

## Conséquences

### Positives

* L'un des quatre moteurs d'intervalle disparaît ; l'invariant
  cardinalité/appartenance (et tout futur invariant) a moins de générateurs à
  atteindre.
* La surface de la bibliothèque suit de plus près l'usage réel des dummies.

### Négatives

* Un changement cassant pour tout appelant de
  `Any.Int128()`/`Any.UInt128()`/`Any.Half()` — atténué par un cast ou un
  littéral au point d'appel, et signalé dans les notes de version.
* Perte d'uniformité de la matrice numérique : la liste des types ne reflète plus
  un pour un les types numériques du BCL.

### Risques

* **Demande sous-estimée** — si les dummies 128 bits ou `Half` contraints
  s'avèrent plus utilisés que prévu, la coupe est réversible (les générateurs
  sont mécaniques), mais au prix du remaniement que cet ADR cherche à éviter.
  Atténué en livrant le retrait dans une version majeure clairement signalée.

## Actions de suivi

* À l'acceptation, retirer les générateurs, `WideIntervalSpec`, leurs entrées de
  fabrique `Any.*` et leurs tests ; signaler le changement cassant dans les notes
  de version.
* Si seule la coupe 128 bits est acceptée (`Half` conservé), laisser `AnyHalf`
  intact sur le moteur partagé.

## Références

* [ADR-0011](0011-host-dummies-as-a-standalone-package.fr.md) — Héberger Dummies comme un paquet autonome.
* [ADR-0013](0013-gate-distinct-collections-by-cardinality-else-bounded-draw.fr.md) — le périmètre anticipé de cardinalité dans lequel ces types ont été rentrés.
* Les générateurs et le moteur proposés au retrait, dans le projet `Dummies`
  (`AnyInt128`, `AnyUInt128`, `WideIntervalSpec`, `AnyHalf`).
