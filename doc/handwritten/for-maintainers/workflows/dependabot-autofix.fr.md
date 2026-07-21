# Workflow `dependabot-autofix`

🌍 🇬🇧 [English](dependabot-autofix.en.md) · 🇫🇷 Français (ce fichier)

> Documentation mainteneur — fait partie de la [référence des workflows](README.fr.md).
> Ne fait pas partie de la documentation utilisateur sous `doc/`.

**Fichier du workflow :** [`.github/workflows/dependabot-autofix.yml`](../../../../.github/workflows/dependabot-autofix.yml)

## À quoi il sert

Les pull requests de Dependabot passent parfois au rouge pour des raisons vite
corrigées mais fastidieuses à diagnostiquer : un en-tête de commit `bump …` trop
long, un analyzer monté qui émet désormais un avertissement promu en erreur, un
petit changement d'API d'une bibliothèque. Ce workflow est le **compagnon de
diagnostic** de [`dependabot-automerge`](dependabot-automerge.fr.md) : automerge
décide *quand* une PR verte fusionne ; celui-ci décide *pourquoi* une PR rouge est
rouge et *comment* la réparer.

Une fois les checks d'une PR Dependabot exécutés, il demande au modèle (Claude, via
le même patron `curl` que [`changelog`](changelog.fr.md) et
[`adr-check`](adr-check.fr.md)) l'un de trois verdicts — **saine**, **réparable par
un petit changement à faible risque**, ou **nécessite un humain** — et poste un
unique commentaire. Quand le verdict est *réparable*, ce commentaire porte un
**patch prêt à appliquer** et un message de commit conforme à la convention :
appliquer le correctif devient un copier-coller, pas une enquête.

**Il n'applique, ne pousse et ne merge jamais rien.** Chaque verdict est consultatif
— un commentaire sur lequel un humain agit, exactement comme `adr-check`. Cela le
maintient dans la règle « aucun agent ne merge » du dépôt et, surtout, dans sa
frontière de chaîne d'approvisionnement (voir *À manipuler avec précaution*).

## Quand il s'exécute

- Sur **`workflow_run: completed`** de chaque workflow-barrage qu'une PR Dependabot
  traverse (`ci`, `sonar`, `analyzers`, `commit-lint`, `dummies`,
  `dependency-review`, `codeql`). Chacun qui se termine relance le triage sur l'état
  **combiné** des checks du commit de tête, de sorte que le commentaire unique
  s'affine à mesure que les checks les plus lents terminent.
- Le job est restreint aux **pull requests de Dependabot issues d'une branche de ce
  dépôt** (`workflow_run.actor == 'dependabot[bot]'` et une tête hors fork). Une PR
  humaine, ou un fork, est ignorée.

### Pourquoi `workflow_run`, pas `pull_request`

Un run `pull_request` *déclenché par Dependabot* est volontairement mis en bac à
sable par GitHub : il reçoit un token en **lecture seule** et **aucun secret du
dépôt** (seulement le magasin de secrets Dependabot, distinct). Il ne pourrait ni
lire `ANTHROPIC_API_KEY` ni commenter. `workflow_run` s'exécute dans le **contexte
de la branche de base** après la fin des checks : il a les secrets du dépôt et un
token en écriture — et il ne récupère ni n'exécute le code de la pull request. Le
code de la dépendance montée a déjà tourné, dans le contexte `ci` en lecture seule ;
ce triage ne fait que *lire* le résultat.

## Comment il s'exécute

Un job, `triage` :

1. **Résoudre la PR** depuis la charge utile `workflow_run` (repli sur
   `gh pr list --head`).
2. **Classer l'état combiné des checks** du commit de tête à partir de ses
   check-runs : *failing* (agir maintenant), *pending* (attendre la prochaine fin),
   ou *green*.
3. En **green**, retirer tout commentaire de triage périmé (la PR s'est rétablie).
4. En **failing**, [`collect-context.sh`](../../../../tools/dependabot-autofix/collect-context.sh)
   assemble le diff de la PR, les noms des checks en échec et les logs des jobs en
   échec — **le tout via l'API GitHub, rien n'est récupéré localement** — et un
   appel Anthropic renvoie le verdict, un patch optionnel et le corps du
   commentaire.
5. [`upsert-comment.sh`](../../../../tools/dependabot-autofix/upsert-comment.sh)
   poste, rafraîchit ou retire l'**unique** commentaire marqué
   (`<!-- dependabot-autofix -->`).

Comme `adr-check`, chaque appel externe est **best-effort** : un log manquant, une
erreur d'API, un refus, une réponse tronquée ou non-JSON deviennent chacun un
`::warning::` et un no-op, jamais un check rouge. Le workflow est consultatif ; il
ne doit pas fabriquer un échec de son propre fait.

## Permissions & sécurité

Défaut du workflow `contents: read`. Le job s'élargit à `checks: read` +
`actions: read` (pour lire les check-runs et les logs des runs en échec) et
`pull-requests: write` (pour gérer son unique commentaire) — et rien d'autre. Il a
besoin du **secret de dépôt `ANTHROPIC_API_KEY`** (un secret Actions ;
`workflow_run` lit les secrets Actions, donc — contrairement à un run Dependabot
`pull_request` — il n'a *pas* besoin d'une copie côté secrets Dependabot).

## À manipuler avec précaution

- **Il est consultatif. Il n'applique jamais le patch, ne pousse ni ne merge.** Le
  patch du commentaire est à appliquer et relire par un humain. Ne « faites pas
  évoluer » ce workflow vers des commits poussés sans lire le point suivant.
- **La frontière de chaîne d'approvisionnement est toute la conception.** Ce
  workflow ne récupère ni ne construit jamais la PR : le token en écriture et la clé
  API n'entrent donc jamais à portée d'un paquet tiers fraîchement monté. Une
  variante auto-applicante devrait construire le code monté pour vérifier un
  correctif, exécutant du code non fiable *avec* ces identifiants — un arbitrage
  réel et délibéré, pas un réglage. Décidez-le en connaissance de cause avant de
  passer le déclencheur à `pull_request_target` ou d'ajouter une étape de build.
- **Il ne fait rien tant qu'il n'est pas sur `main`.** `workflow_run` ne se
  déclenche que pour les fichiers de workflow présents sur la branche par défaut du
  dépôt. Sur une branche de fonctionnalité, le workflow est inerte ; il commence à
  trier une fois mergé.
- **Il lit les checks ; il ne les rejoue pas.** Les échecs non corrigeables par le
  code — `sonar`/couverture qui ne peut lire un secret sur un run Dependabot, un
  blocage de politique `dependency-review`/CodeQL — sont *diagnostiqués*, pas
  corrigés. Les corriger relève de la configuration du dépôt ou du jugement humain.
- **Le garde-fou acteur + hors-fork compte.** Il tient le chemin élargi
  `pull-requests: write` à l'écart des PR humaines et des forks.
- **Ce sont toujours les checks *required* qui font barrage.** Ceci poste un
  commentaire ; cela ne change aucun statut de check. Ce qui bloque un mauvais merge,
  c'est la protection de branche sur `main`.

## Liens connexes

- [`dependabot-automerge`](dependabot-automerge.fr.md) — active l'auto-merge sur une
  PR Dependabot patch/minor verte ; celui-ci explique une PR rouge.
- [`commit-lint`](commit-lint.fr.md) — **exempte désormais les commits écrits par
  Dependabot**, de sorte qu'un en-tête `bump …` long ne fait plus échouer le lint à
  lui seul ; ce workflow traite les échecs résiduels.
- [`dependency-review`](dependency-review.fr.md) — le barrage de vulnérabilités au
  moment de la PR, que traverse aussi une PR Dependabot ; un blocage y vaut
  *nécessite un humain*, jamais contourné automatiquement.
- [`.github/dependabot.yml`](../../../../.github/dependabot.yml) — ce que Dependabot
  met à jour et ce qu'il ignore.
- Prompt : [`.github/dependabot-autofix-prompt.md`](../../../../.github/dependabot-autofix-prompt.md).
