# ADR-0004 | Contrôler chaque pull request au regard de la base d'ADR

🌍 🇬🇧 [English](0004-check-every-pull-request-against-the-adr-base.md) · 🇫🇷 Français (ce fichier)

**Statut :** Accepté
**Date :** 2026-07-15
**Décideurs :** Reefact

## Contexte

Les pull requests sont le point d'entrée des nouvelles décisions dans le dépôt.
Sans revue délibérée, un changement peut introduire une décision non consignée,
remplacer silencieusement une décision acceptée ou contredire la base d'ADR.

Les invariants déterministes sont déjà imposés par les tests, analyzers et la CI.
Reste une question de jugement sur le diff complet : existe-t-il une décision
importante et durable, et comment se rapporte-t-elle aux décisions consignées ? Le
dépôt évolue principalement via des sessions de code instruites, mais accepte
aussi d'autres contributeurs. Un jugement de modèle a un coût, n'est pas
déterministe et ne peut pas remplacer l'autorité du mainteneur.

## Décision

Le dépôt exige que chaque pull request reçoive une revue consultative et non bloquante au regard de la base d'ADR, normalement dans la session de code instruite et sinon par un recours lancé manuellement, tandis que le mainteneur contrôle seul les statuts d'ADR et les décisions de merge.

## Justification

La revue appartient au moment de la pull request, lorsque le contexte du
changement est encore disponible. Elle reste consultative car la significativité
architecturale et les conflits sont des jugements humains ; les invariants
imposables appartiennent aux checks déterministes.

Le chemin normal en session possède le meilleur contexte et duplique peu de
travail. Un recours manuel couvre les autres contributions sans transformer le
verdict d'un modèle flottant en gate autonome. Les résultats, prompts, checklist
et limites exacts sont maintenus dans la
[spécification du processus de revue](../specifications/adr-review-process.fr.md).

L'exigence est procédurale, pas une promesse de couverture automatique parfaite :
une pull request créée hors session instruite dépend encore du lancement ou de la
réalisation manuelle de la revue de recours.

## Alternatives envisagées

### Exécuter automatiquement un modèle sur chaque pull request

Envisagé pour une couverture automatique complète. Rejeté car cela ajoute coût et
non-déterminisme à chaque changement, duplique une revue de session mieux informée
et inciterait à traiter une opinion de modèle comme un barrage.

### Encoder chaque ADR comme invariant lisible par la machine

Envisagé pour détecter les conflits de manière déterministe. Rejeté car les choix
durables ne se réduisent généralement pas à des prédicats ; ceux qui le peuvent
appartiennent aux tests ou à la CI, pas à l'ADR.

### S'appuyer sur la mémoire

Envisagé comme statu quo sans effort. Rejeté car c'est précisément ainsi que des
décisions non consignées, remplacées silencieusement ou contradictoires échappent
à la revue.

## Conséquences

### Positives

* La significativité architecturale est examinée pendant que le contexte est frais.
* Les agents peuvent rédiger des ADR proposés avec le diff complet.
* Le mainteneur conserve seul l'autorité de décision et de merge.
* Les contributeurs hors workflow agent disposent d'un recours documenté.

### Négatives

* La couverture reste best-effort et non garantie mécaniquement.
* Les revues de modèle sont non reproductibles et peuvent se tromper ou omettre.

### Risques

* La revue peut être sautée ou réduite à une case cochée. Atténuation : les
  instructions sont répétées dans `CLAUDE.md`, `AGENTS.md`, le template de PR et
  la spécification du processus.

## Actions de suivi

* Si l'usage montre une couverture insuffisante, améliorer le mécanisme consultatif
  sans en faire une décision bloquante autonome.

## Références

* [Spécification du processus de revue](../specifications/adr-review-process.fr.md).
* `AGENTS.md` et `CLAUDE.md` — instructions des agents.
* [Référence du workflow `adr-check`](../workflows/adr-check.fr.md).
