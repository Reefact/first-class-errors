# Spécification du processus de revue des ADR

🌍 🇬🇧 [English](adr-review-process.en.md) · 🇫🇷 Français (ce fichier)

Cette page met en œuvre l'[ADR-0004](../adr/0004-check-every-pull-request-against-the-adr-base.fr.md).
Le processus du dépôt **exige** une revue ADR pour chaque pull request, mais cette
revue reste consultative et l'automatisation actuelle ne garantit pas
mécaniquement que chaque pull request en reçoit une.

## Question de revue

Avant de finaliser une pull request, comparer le changement complet à la base
d'ADR acceptés et demander :

* Le changement introduit-il une décision importante et durable ?
* Remplace-t-il ou précise-t-il une décision consignée ?
* Contredit-il une décision acceptée ?
* Ne modifie-t-il que l'implémentation alors que la décision reste vraie ?

Le test de significativité est : si l'implémentation change mais que la décision
tient, l'ADR ne devrait pas nécessiter d'édition.

## Résultats

| Résultat | Action requise |
|---|---|
| Aucune décision | Indiquer qu'aucune décision d'architecture n'est introduite. |
| Créer | Rédiger un ADR `Proposed` par nouvelle décision, l'indexer et le lier depuis la pull request. |
| Remplacer | Rédiger un successeur `Proposed` et identifier l'ADR accepté qu'il remplacerait. Ne pas réécrire l'ADR accepté. |
| Alerter | Signaler le conflit exact avec un ADR accepté et laisser la résolution au mainteneur. |

Un agent automatisé peut rédiger et recommander. Seul le mainteneur peut accepter,
remplacer, déprécier, merger ou autoriser un conflit.

## Chemins d'exécution

### Travail produit par un agent

`CLAUDE.md` et `AGENTS.md` portent les instructions obligatoires. L'agent qui
réalise le changement possède déjà le diff et son contexte de raisonnement : il
constitue donc le chemin principal et consigne le résultat dans la description de
la pull request.

### Autres contributeurs

Le workflow `adr-check` est un recours lancé manuellement. Il fournit une revue
indépendante lorsqu'un changement n'a pas été produit dans une session de code
instruite. Son résultat reste consultatif et non reproductible, car il s'agit
d'un jugement de modèle et non d'un invariant de build déterministe.

### Checklist de pull request

Le template de pull request consigne le résultat déclaré. La checklist rend la
revue visible mais ne prouve pas que l'analyse était complète.

## Ce qui reste mécanique

Les invariants déterministes appartiennent aux tests et aux checks CI obligatoires,
pas à l'opinion d'un LLM. La revue ADR peut révéler qu'un garde-fou manque, mais
elle ne remplace jamais les tests de floor, tests d'architecture, analyzers ou
gates de release.

## Limites et escalade

* Une pull request ouverte hors session instruite n'est couverte que si quelqu'un
  lance le recours ou réalise la revue manuellement.
* Un verdict de modèle peut être faux et ne doit jamais bloquer seul.
* Un conflit, une significativité incertaine ou un changement de statut est
  escaladé à `@reefact`.
* Les permissions et le prompt exacts vivent dans la
  [référence du workflow `adr-check`](../workflows/adr-check.fr.md).
