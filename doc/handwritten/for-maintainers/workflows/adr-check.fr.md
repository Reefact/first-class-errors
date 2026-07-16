# Workflow `adr-check`

🌍 🇬🇧 [English](adr-check.en.md) · 🇫🇷 Français (ce fichier)

> Documentation mainteneur — fait partie de la [référence des workflows](README.fr.md).
> Ne fait pas partie de la documentation utilisateur sous `doc/`.

**Fichier du workflow :** [`.github/workflows/adr-check.yml`](../../../../.github/workflows/adr-check.yml)

## À quoi il sert

`adr-check` confronte une **branche** à la base d'ADR et indique si elle embarque
une décision d'architecture que le projet consigne en [ADR](../adr/README.md).
C'est le **repli manuel** pour un contributeur **sans Claude Code** — dont la
session de codage effectue le même contrôle automatiquement (voir
[`AGENTS.md`](../../../../AGENTS.md) → « Architecture decisions »). Les trois mêmes issues,
lancées à la demande depuis l'onglet Actions au lieu de l'être dans une session.

Il est **consultatif** : il **ne bloque jamais** rien et **n'écrit jamais d'ADR**.
Il remonte l'une de trois observations pour qu'un humain agisse :

- **New decision** — une décision durable pas encore consignée ;
- **Supersedes** — une modification d'une décision qu'un ADR consigne déjà ;
- **Conflicts** — une contradiction avec un ADR accepté.

Rédiger l'ADR (en `Status: Proposed`) puis l'accepter, le remplacer ou le déprécier
restent le fait d'un humain ou d'un agent.

## Quand il s'exécute

- Sur **`workflow_dispatch`** uniquement — déclenché à la main depuis l'onglet
  Actions sur la branche à vérifier. Aucun déclencheur automatique par pull request :
  consigner une décision est un acte relu par un humain, exactement comme le
  workflow frère [`changelog`](changelog.fr.md), et un appel de modèle autonome sur
  chaque pull request n'est ni voulu ni nécessaire.
- Une entrée, **`base`** (défaut `main`) — la ref contre laquelle la branche est
  différenciée.
- Comme tout `workflow_dispatch`, il n'apparaît dans l'onglet Actions qu'une fois ce
  fichier sur la **branche par défaut**.

## Comment il s'exécute

Un seul job, `adr-check` :

1. Checkout de la branche déclenchée avec `fetch-depth: 0`, pour que le merge-base
   avec la base se résolve.
2. **Skip s'il n'y a pas de clé.** Si `ANTHROPIC_API_KEY` manque, le job le signale
   (dans le log et le résumé de run) et sort en 0.
3. Résout `base` en le **point de divergence** de la branche (`git merge-base`), pour
   que le diff soit exactement ce que la branche introduit.
4. **Collecte le contexte** avec
   [`tools/adr-check/collect-context.sh`](../../../../tools/adr-check/collect-context.sh) :
   la liste des fichiers modifiés et le diff unifié (via `git`, plafonné en octets)
   plus la base d'ADR courante (index et chaque ADR, plafonnée en octets), en un seul
   paquet délimité.
5. **Interroge le modèle** sous
   [`.github/adr-check-prompt.md`](../../../../.github/adr-check-prompt.md), qui définit les
   trois issues, pousse fortement vers le silence, et exige un unique verdict JSON
   `{ analysis, needs_report, outcomes, report }`.
6. **Écrit le verdict dans le résumé de run** (`$GITHUB_STEP_SUMMARY`) — la sortie
   principale, lisible qu'une pull request existe ou non. Une branche propre reçoit
   une ligne « rien à signaler ».
7. **Le reflète sur la pull request, s'il y en a une d'ouverte** pour la branche :
   quand `needs_report` est vrai, le rapport est posté (ou rafraîchi) comme l'unique
   commentaire repéré par le marqueur caché `<!-- adr-check -->`, via
   [`tools/adr-check/upsert-comment.sh`](../../../../tools/adr-check/upsert-comment.sh) ;
   quand c'est faux, le commentaire qui traîne est retiré. Sans pull request ouverte,
   le résumé est la seule sortie.

## Permissions & sécurité

Le token de haut niveau est en lecture seule (`contents: read`). Le job n'ajoute que
`pull-requests: write`, et seulement pour poster le commentaire optionnel quand une
pull request est ouverte — il n'écrit rien dans le dépôt.

- **Secret :** `ANTHROPIC_API_KEY` (secret du dépôt), partagé avec le workflow
  [`changelog`](changelog.fr.md). Comme le seul déclencheur est `workflow_dispatch` —
  réservé aux comptes ayant l'accès en écriture, jamais à un fork — la clé n'est
  jamais exposée à un fork.
- **Le diff et le texte des ADR non fiables sont traités comme des données.** Le
  paquet est échappé en JSON via `jq --arg` et encadré par des délimiteurs ; le prompt
  reçoit la consigne de traiter chaque bloc comme des données, pas des instructions.

## À manier avec précaution

- **Il est consultatif par construction — gardez-le ainsi.** Il est manuel et
  n'écrit qu'un résumé de run (et un commentaire optionnel) ; il n'a aucun moyen de
  bloquer un merge, et chaque mode d'échec (pas de secret, erreur d'API, refus,
  `max_tokens`, sortie non parsable) est un `::warning::` avec `exit 0`. Ne le câblez
  pas sur `pull_request` et n'en faites pas un check *required* — c'est tout l'intérêt
  de le cantonner à Claude Code plus un lancement manuel.
- **Le verdict est un rappel, pas une sentence.** Le modèle peut mal juger de la
  portée. Un humain décide ; le mainteneur possède l'ADR et son statut. Le rapport le
  dit.
- **Précision plutôt que rappel sur le rappel.** Le prompt est réglé pour rester
  silencieux sur les changements de routine (corrections de bug, tests, docs, montées
  de dépendances, refactorings sans changement de contrat). S'il crie au loup,
  resserrez les non-déclencheurs du prompt.
- **`claude-sonnet-5` est un alias flottant.** Il résout vers le snapshot Sonnet 5
  courant, donc un verdict peut évoluer avec le temps. Acceptable pour un avis ; ne le
  traitez pas comme reproductible.
- **Le contexte est plafonné en octets.** `DIFF_MAX_BYTES` et `ADR_MAX_BYTES` bornent
  le paquet ; une troncature est annoncée en ligne. Un très gros diff est jugé sur sa
  première tranche plus la liste des fichiers modifiés.
- **Même machine que [`changelog`](changelog.fr.md).** Les deux sont des workflows LLM
  à dispatch manuel (API via payload construit avec `jq`, texte non fiable en données,
  humain dans la boucle) ; gardez-les cohérents quand l'un change.

## En rapport

- [`AGENTS.md`](../../../../AGENTS.md) — le contrôle ADR propre à l'agent qui écrit le code,
  lancé automatiquement dans une session Claude Code ; ce workflow en est le pendant
  manuel pour les contributeurs sans Claude Code.
- [Référence des ADR](../adr/README.md) — le format, les conventions et la note
  « quand écrit-on un ADR ? ».
- [`changelog`](changelog.fr.md) — le workflow frère LLM-en-CI à dispatch manuel dont
  celui-ci s'inspire.
