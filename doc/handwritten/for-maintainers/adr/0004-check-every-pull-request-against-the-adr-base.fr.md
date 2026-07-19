# ADR-0004 | Contrôler chaque pull request au regard de la base d'ADR

🌍 🇬🇧 [English](0004-check-every-pull-request-against-the-adr-base.md) · 🇫🇷 Français (ce fichier)

**Statut :** Accepté
**Date :** 2026-07-15
**Décideurs :** Reefact

## Contexte

Les pull requests sont le point d'entrée des nouvelles décisions architecturales dans le dépôt. Une modification peut introduire une décision non enregistrée, remplacer une décision existante sans le signaler ou contredire un ADR accepté.

Les invariants mécaniques sont déjà protégés par les tests et la CI. La question restante — savoir si un diff contient ou contredit une décision architecturale — exige du jugement et du contexte plutôt qu'une règle déterministe.

Le mainteneur est la seule autorité qui fusionne les pull requests et modifie les statuts des ADR. Les agents peuvent analyser les changements et rédiger des ADR proposés, mais ils ne les acceptent, ne les remplacent, ne les déprécient ni ne les fusionnent.

Le dépôt accueille à la fois des travaux assistés par agent et des contributions sans agent en session. Une vérification fondée sur un modèle est consultative et non reproductible ; elle ne doit donc pas devenir un gate autonome de fusion.

## Décision

Chaque pull request fait l'objet d'une revue consultative au regard de la base d'ADR acceptés, réalisée en session lorsqu'un agent porte la modification ou invoquée explicitement par le contributeur dans les autres cas, les agents étant limités à la rédaction d'ADR `Proposé` et le mainteneur conservant seul l'autorité de décision.

## Justification

La revue doit intervenir au moment de la pull request, lorsque le contexte de l'implémentation est encore disponible et qu'une décision peut encore être enregistrée ou contestée avant fusion.

Elle reste consultative, car la portée architecturale relève du jugement. Les invariants déterministes doivent continuer à être imposés mécaniquement, tandis que le mainteneur reste responsable de l'acceptation de la recommandation et de toute transition de statut.

Utiliser l'agent déjà présent dans la session évite une seconde analyse moins contextualisée. Un mécanisme explicite pour les autres contributeurs préserve l'accessibilité sans transformer l'avis non déterministe d'un modèle en gate automatique.

Les instructions d'agent, la checklist et les mécanismes de workflow actuels sont documentés dans `AGENTS.md`, `CLAUDE.md`, la [référence d'implémentation des ADR](../specifications/adr-implementation-reference.fr.md#vérification-adr-des-pull-requests) et la [référence du workflow `adr-check`](../workflows/adr-check.fr.md).

## Alternatives envisagées

### Exécuter automatiquement une vérification par modèle sur chaque pull request

Envisagé car cela maximiserait la couverture apparente. Rejeté parce que cela ajouterait un coût, dupliquerait une analyse en session mieux contextualisée et placerait un jugement non déterministe sur une surface de CI presque obligatoire.

### Encoder chaque ADR sous forme d'invariant vérifiable mécaniquement

Envisagé car les vérifications déterministes sont fiables. Rejeté parce qu'une partie seulement des décisions architecturales peut être exprimée mécaniquement ; celles qui le peuvent appartiennent déjà aux tests ou à la CI, tandis que l'ADR doit rester un relevé de décision humain.

### S'appuyer sur la mémoire

Envisagé car cela ne nécessite aucun processus. Rejeté parce que cela laisse précisément ouverte la faille que le corpus d'ADR doit combler.

## Conséquences

### Positives

* La portée architecturale est examinée avant fusion tant que le raisonnement est encore disponible.
* Les agents peuvent rédiger les enregistrements à faible coût sans acquérir l'autorité de décision.
* Les contributeurs sans agent en session disposent d'un mécanisme explicite.
* Aucun avis de modèle n'empêche le mainteneur de fusionner.

### Négatives

* La couverture relève du processus et n'est pas garantie mécaniquement.
* La revue est non déterministe et peut produire des faux positifs ou des omissions.
* Un contributeur peut oublier d'invoquer la revue de secours.

### Risques

* L'expression « chaque pull request » pourrait être comprise comme une garantie automatisée. Mesure : cet ADR définit une obligation de processus ; le workflow actuel est déclenché manuellement et ne prouve pas à lui seul une exécution universelle.
* Des résultats répétés sans valeur pourraient conduire à ignorer la revue. Mesure : conserver des prompts fortement orientés vers le silence pour les changements d'implémentation ordinaires.

## Actions de suivi

* Ne réexaminer une automatisation plus forte que si la couverture procédurale se révèle insuffisante, et conserver tout futur contrôle par modèle comme consultatif sauf décision distincte contraire.

## Références

* `AGENTS.md` — la procédure des agents et l'autorité sur les statuts.
* `CLAUDE.md` — les instructions en session.
* [Référence d'implémentation des ADR — Vérification ADR des pull requests](../specifications/adr-implementation-reference.fr.md#vérification-adr-des-pull-requests)
* [Référence du workflow `adr-check`](../workflows/adr-check.fr.md)
* [ADR-0024](0024-allow-a-one-time-editorial-refactoring-of-accepted-adrs.fr.md) — autorise cette extraction éditoriale.
