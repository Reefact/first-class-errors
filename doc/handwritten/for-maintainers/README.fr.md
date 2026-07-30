# Documentation mainteneur

🌍 🇬🇧 [English](README.md) · 🇫🇷 Français (ce fichier)

> Documentation pour les **mainteneurs et opérateurs** de FirstClassErrors —
> comment le projet est construit, publié et maintenu en bonne santé. Elle ne
> fait **pas** partie de la documentation utilisateur de la bibliothèque, sous
> [`doc/`](../for-users/). La version anglaise est canonique ; les pages françaises sont
> tenues à jour en parallèle.

## Sommaire

### [Référence des workflows CI/CD](workflows/README.fr.md)

Une page par workflow GitHub Actions — à quoi il sert, quand et comment il
s'exécute, ses permissions, et les décisions non évidentes qu'il ne faut pas
modifier sans en comprendre la raison. Commencez par l'[index](workflows/README.fr.md) ;
il documente aussi les conventions transverses (actions épinglées par SHA,
permissions au moindre privilège, timeouts par job, checks *required* comme vrai
barrage).

### [Répétition de release à blanc (« dry run » manuel)](ReleaseDryRun.fr.md)

Le runbook opérationnel du dry run manuel via le dispatch `release` : comment le
lancer, ce qu'il touche (et ce qu'il ne touche volontairement pas), et quand
l'utiliser. Il complète les pages [`release`](workflows/release.fr.md) et
[`release-dryrun`](workflows/release-dryrun.fr.md) de la référence, qui décrivent
ces workflows structurellement. Aussi en [anglais](ReleaseDryRun.en.md).

### [Ajouter un train de release](AddingAReleaseTrain.fr.md)

La checklist pour ajouter un nouveau paquet versionné indépendamment : l'unique
édit de données dans [`tools/trains.sh`](../../../tools/trains.sh) et les édits statiques
imposés par GitHub et la tooling (trigger de tag, options de choix, scopes du
commit-lint, packaging). Aussi en [anglais](AddingAReleaseTrain.en.md).

### [Écrire les tests de JustDummies](WritingJustDummiesTests.fr.md)

Où placer un nouveau test pour `JustDummies` — suite par l'exemple ou suite par
propriétés — et comment l'écrire pour qu'il prouve quelque chose. Une seule
question tranche : l'assertion a-t-elle un espace d'entrée ? La frontière
elle-même est enregistrée dans
[l'ADR-0040](adr/0040-split-the-justdummies-test-bed-between-example-and-property-suites.fr.md) ;
cette page l'applique. Aussi en [anglais](WritingJustDummiesTests.en.md).

### [Tool JustDummies (`dum`) — spécification](specifications/justdummies-tool.fr.md)

La spécification complète de `dum`, le scaffolder en ligne de commande de
JustDummies, qui écrit un generator nommé et composable pour un type du code du
développeur — un moteur chargeable par un hôte Roslyn, plus une CLI mince par-dessus.
Elle est implémentable telle quelle : le squelette émis, les règles de résolution des
paramètres et les décisions qui les portent ont chacun été vérifiés contre la source
de la bibliothèque, et les affirmations centrales ont été mesurées. Elle est aussi
**autonome** : elle inline chaque fait sur la bibliothèque dont elle dépend et énonce
ses exigences envers le dépôt hôte en exigences plutôt qu'en chemins, de sorte qu'elle
survit au déménagement de JustDummies dans son propre dépôt. Pas encore construit.
Aussi en [anglais](specifications/justdummies-tool.md).

### [Registres de décision d'architecture (ADR)](adr/README.md)

Des enregistrements datés des décisions importantes — leur contexte, l'option
retenue et les conséquences. Un ADR est un journal historique : il est *superseded*
par un ADR plus récent, pas édité sur place. L'[index](adr/README.md) définit le
format que suit chaque ADR et fournit un [template](adr/template.md) prêt à
copier. *(En anglais uniquement.)*

- [ADR-0001 — Verrouiller le floor Roslyn de l'analyzer](adr/0001-lock-the-analyzer-roslyn-floor.md)
  — pourquoi la version de Roslyn de l'analyzer est gelée, ce que le workflow
  [`analyzers`](workflows/analyzers.fr.md) fait respecter.
- [ADR-0002 — Fixer le floor du runtime de l'outillage sur la plus ancienne LTS supportée](adr/0002-floor-the-tooling-runtime.md)
  — pourquoi l'outillage cible `net8.0` avec roll-forward, ce que le job
  `floor` du workflow [`ci`](workflows/ci.fr.md) fait respecter.

## En rapport

- [`CONTRIBUTING.fr.md`](../for-users/CONTRIBUTING.fr.md) — conventions de commit et de pull
  request (imposées par le workflow [`commit-lint`](workflows/commit-lint.fr.md)).
