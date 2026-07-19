# Documentation mainteneur

🌍 🇬🇧 [English](README.md) · 🇫🇷 Français (ce fichier)

> Documentation pour les **mainteneurs et opérateurs** de FirstClassErrors —
> comment le projet est construit, publié et maintenu en bonne santé. Elle ne fait
> **pas** partie de la documentation utilisateur. La version anglaise est canonique
> et les pages françaises sont tenues à jour en parallèle.

## Sommaire

### [Spécifications mainteneur](specifications/README.fr.md)

Références évolutives de l'implémentation technique et opérationnelle actuelle des
décisions d'architecture acceptées : compatibilité des plateformes, revue d'ADR,
contrats du Request Binder, versionnement du catalogue GenDoc et génération
Dummies. Elles évoluent lorsque les mécanismes changent sans modifier la décision.

### [Référence des workflows CI/CD](workflows/README.fr.md)

Une page par workflow GitHub Actions — objectif, déclencheurs, permissions,
structure exacte et contraintes non évidentes. L'index documente aussi les
conventions transverses des workflows.

### [Répétition de release à blanc](ReleaseDryRun.fr.md)

Le runbook opérationnel du dry run manuel de release. Il complète les références
des workflows [`release`](workflows/release.fr.md) et
[`release-dryrun`](workflows/release-dryrun.fr.md).

### [Ajouter un train de release](AddingAReleaseTrain.fr.md)

La checklist pour ajouter un package versionné indépendamment et mettre à jour les
surfaces statiques de packaging, tags, choix et commit-lint autour de
`tools/trains.sh`.

### [Registres de décision d'architecture](adr/README.md)

Enregistrements datés des décisions importantes — choix, justification,
alternatives et conséquences. Les détails d'implémentation actuels appartiennent
aux spécifications ci-dessus. Les ADR acceptés sont normalement immuables ; la
migration éditoriale unique est consignée par
[l'ADR-0023](adr/0023-extract-specifications-from-accepted-adrs.fr.md).

## En rapport

- [`CONTRIBUTING.fr.md`](../for-users/CONTRIBUTING.fr.md) — conventions de commit et de pull request.
