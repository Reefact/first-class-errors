# Workflow `dependabot-autofix`

🌍 🇬🇧 [English](dependabot-autofix.en.md) · 🇫🇷 Français (ce fichier)

> Documentation mainteneur — fait partie de la [référence des workflows](README.fr.md).
> Ne fait pas partie de la documentation utilisateur sous `doc/`.

**Fichier du workflow :** [`.github/workflows/dependabot-autofix.yml`](../../../../.github/workflows/dependabot-autofix.yml)

## À quoi il sert

Les pull requests de Dependabot passent parfois au rouge pour des raisons vite
corrigées mais fastidieuses à traquer : un analyzer monté qui émet désormais un
avertissement promu en erreur, un petit changement d'API d'une bibliothèque, une
branche en retard sur `main`. Ce workflow est le compagnon de **réparation** de
[`dependabot-automerge`](dependabot-automerge.fr.md) : automerge décide *quand* une
PR verte fusionne ; celui-ci décide *pourquoi* une PR rouge est rouge, **la
répare**, et pousse le correctif.

Une fois les checks d'une PR Dependabot exécutés, il demande au modèle (Claude, via
le même patron `curl` que [`changelog`](changelog.fr.md) et
[`adr-check`](adr-check.fr.md)) un verdict — **saine**, **réparable par un petit
changement à faible risque**, ou **nécessite un humain** — et, quand c'est
réparable, applique l'une d'un ensemble fixe d'actions et la pousse.

**La règle d'auto-merge suit le risque du correctif :**

| Correctif | Action | Auto-merge |
| --- | --- | --- |
| Réécrire un en-tête de commit | `rewrite_commit_message` | **conservé** (trivial) |
| Renommer la pull request | `retitle_pr` | **conservé** (trivial) |
| Rebaser sur `main` | `rebase` | **conservé** (trivial) |
| Modifier du code produit/test | `apply_patch` | **désactivé** — relecture humaine |

Un correctif **trivial** ne change que l'historique ou les métadonnées : la PR reste
éligible à fusionner seule une fois les checks passés. Un correctif **de code**
change le contenu de fichiers : l'auto-merge est alors coupé et le changement écrit
par l'IA attend une relecture humaine. Le workflow décide trivial-ou-code **à partir
de l'action réellement effectuée**, jamais sur parole du modèle, et il ne **merge**
jamais rien lui-même.

## Quand il s'exécute

- Sur **`workflow_run: completed`** de chaque workflow-barrage qu'une PR Dependabot
  traverse (`ci`, `sonar`, `analyzers`, `commit-lint`, `dummies`,
  `dependency-review`, `codeql`). Chacun qui se termine le relance sur l'état
  **combiné** des checks du commit de tête.
- Restreint aux **pull requests de Dependabot issues d'une branche de ce dépôt**
  (`workflow_run.actor == 'dependabot[bot]'`, tête hors fork). Les PR humaines et de
  fork sont ignorées.

## Comment il s'exécute

Un job, `autofix` :

1. **Résoudre la PR**, puis **classer** l'état combiné des checks du commit de
   tête : *failing* (agir), *pending* (attendre la prochaine fin), *green* (retirer
   tout commentaire périmé).
2. En *failing*, [`collect-context.sh`](../../../../tools/dependabot-autofix/collect-context.sh)
   rassemble le diff de la PR, les noms des checks en échec et les logs des jobs en
   échec — **le tout via l'API, rien n'est construit** — et un appel Anthropic
   renvoie un verdict JSON (verdict, action, `explanation`, et selon le cas
   `patch` / `commit_message` / `pr_title`).
3. Quand le verdict est **fixable**, un *second* checkout de la branche de tête de
   la PR (avec un token capable de pousser) laisse
   [`apply-fix.sh`](../../../../tools/dependabot-autofix/apply-fix.sh) effectuer
   l'action et la pousser. Un correctif **de code** voit alors l'auto-merge coupé.
4. Le workflow **compose lui-même le commentaire** à partir du verdict et de
   l'action réellement effectuée (le commentaire ne prétend donc jamais un correctif
   qui n'a pas abouti) et met à jour l'unique commentaire marqué
   (`<!-- dependabot-autofix -->`).

Tout est **best-effort et sûr par omission** : un patch qui ne s'applique pas, un
rebase en conflit, une erreur d'API, une réponse non-JSON — chacun laisse la PR
intacte et se rabat sur un commentaire *correctif suggéré* ou *nécessite un humain*
plutôt que de pousser un changement cassé ou d'échouer en rouge. Un **garde
anti-boucle** l'empêche d'agir deux fois sur le même push : dès que le *committer* du
commit de tête est `github-actions[bot]`, il attend le prochain push de Dependabot
avant d'agir à nouveau.

## Permissions & sécurité

Défaut du workflow `contents: read`. Le job s'élargit à `contents: write` (pousser
le correctif ; désactiver l'auto-merge), `pull-requests: write` (commenter,
renommer) et `checks: read` + `actions: read` (lire les check-runs et les logs en
échec).

Deux secrets :

- **`ANTHROPIC_API_KEY`** (requis) — un secret Actions ; `workflow_run` lit les
  secrets Actions, donc aucune copie côté secrets Dependabot n'est nécessaire.
  Absent → le workflow avertit et ne fait rien.
- **`DEPENDABOT_AUTOFIX_TOKEN`** (recommandé) — un PAT fin ou un token d'app GitHub
  avec **contents: write** + **pull-requests: write** sur ce dépôt, utilisé
  uniquement pour le push. GitHub **ne** redéclenche **pas** les workflows pour un
  push fait avec le `GITHUB_TOKEN` par défaut ; un token dédié fait re-tourner `ci`
  sur le correctif, ce dont un auto-merge *conservé* a besoin pour aboutir. Sans lui
  le correctif est quand même poussé, mais les checks doivent être redéclenchés à la
  main (p. ex. fermer/rouvrir la PR).

**La frontière de chaîne d'approvisionnement est délibérée.** La réparation ne fait
que des opérations git — apply, reword, rebase, push — et **ne construit jamais la
dépendance montée**. Le token en écriture et la clé API ne côtoient donc jamais du
code tiers fraîchement monté ; le commit poussé est validé par le run `ci` ordinaire,
dans son propre contexte Dependabot en lecture seule.

## À manipuler avec précaution

- **Trivial conserve l'auto-merge ; code le désactive — et ce partage est imposé
  d'après l'action, pas d'après la parole du modèle.** Ne laissez pas `apply_patch`
  être traité comme trivial : un changement de code doit toujours passer par une
  relecture humaine.
- **Le choix du token de push est structurant.** Avec le seul `GITHUB_TOKEN`, un
  correctif poussé ne fait pas re-tourner `ci` : un auto-merge conservé restera en
  attente tant que les checks ne sont pas redéclenchés. Réglez
  `DEPENDABOT_AUTOFIX_TOKEN` pour le comportement voulu.
- **Il ne fait rien tant qu'il n'est pas sur `main`.** `workflow_run` ne se
  déclenche que pour les fichiers de workflow de la branche par défaut.
- **Il ne merge jamais et n'*active* jamais l'auto-merge.** L'activation est le rôle
  de [`dependabot-automerge`](dependabot-automerge.fr.md) ; celui-ci ne fait que le
  *désactiver* sur un correctif de code.
- **Il ne change jamais une version de dépendance**, et les échecs non corrigeables
  par le code (`sonar`/couverture sans secret, un blocage de politique
  `dependency-review`/CodeQL) sont *diagnostiqués*, pas corrigés.
- **Le garde anti-boucle et le garde acteur/hors-fork comptent.** Ils l'empêchent
  d'agir deux fois sur un push et tiennent le chemin en écriture à l'écart des PR
  humaines et de fork.

## Liens connexes

- [`dependabot-automerge`](dependabot-automerge.fr.md) — active l'auto-merge sur une
  PR Dependabot patch/minor verte ; celui-ci répare une PR rouge (et coupe
  l'auto-merge quand son correctif est du code).
- [`commit-lint`](commit-lint.fr.md) — exempte les commits écrits par Dependabot,
  de sorte qu'un en-tête `bump …` long n'échoue plus à lui seul ; celui-ci traite le
  reste.
- [`dependency-review`](dependency-review.fr.md) — un blocage y vaut *nécessite un
  humain*, jamais contourné automatiquement.
- [`.github/dependabot.yml`](../../../../.github/dependabot.yml) — ce que Dependabot
  met à jour et ignore.
- Prompt : [`.github/dependabot-autofix-prompt.md`](../../../../.github/dependabot-autofix-prompt.md).
