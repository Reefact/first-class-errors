# ADR-0027 | Réparer les pull requests Dependabot dans une frontière de risque

🌍 🇬🇧 [English](0027-repair-dependabot-pull-requests-within-a-risk-boundary.md) · 🇫🇷 Français (ce fichier)

**Status:** Accepted
**Date:** 2026-07-21
**Decision Makers:** Reefact

## Contexte

Dependabot ouvre des pull requests de mise à jour de dépendances de routine. Elles
échouent souvent aux checks pour des raisons mécaniques à faible valeur — un en-tête
de commit `bump …` trop long, un analyzer monté promouvant désormais un
avertissement en erreur, un petit changement d'API d'une bibliothèque, une branche
en retard sur `main` — ou pour des raisons sans rapport avec la mise à jour, comme un
secret qu'un run déclenché par Dependabot ne peut pas lire.

Le dépôt exécute déjà un modèle (Claude) depuis la CI via des appels directs à l'API
pour des tâches consultatives et non reproductibles — rédiger le changelog, et le
contrôle ADR — et traite cette sortie comme consultative, jamais comme une barrière
de merge autonome. [ADR-0004](0004-check-every-pull-request-against-the-adr-base.fr.md)
acte que le mainteneur est la seule autorité qui merge les pull requests et que les
agents peuvent analyser et rédiger mais jamais merger.

L'auto-merge est déjà activé pour les mises à jour patch et minor de Dependabot une
fois les checks requis passés, les majeures étant laissées à un humain (le workflow
`dependabot-automerge`).

Le dépôt est durci : actions épinglées par SHA de commit, tokens à moindre
privilège, posture OpenSSF Scorecard. Une pull request Dependabot monte du code tiers
qui s'exécute au build et au test. GitHub met en bac à sable les runs déclenchés par
Dependabot — token en lecture seule, aucun secret du dépôt — et ne redéclenche pas
les workflows pour un push fait avec le `GITHUB_TOKEN` par défaut.

Corriger un échec de build ou d'analyzer exige de changer du code produit ou de test.
Corriger un échec mécanique n'exige de changer que l'historique ou les métadonnées :
réécrire un message de commit, renommer la pull request, ou rebaser sur `main`.

## Décision

Un triage automatisé, piloté par un modèle, peut appliquer et pousser un ensemble
borné de réparations à faible risque sur une pull request Dependabot en échec — en ne
gardant la pull request éligible à l'auto-merge que lorsque la réparation ne change
aucun contenu de fichier, et en désactivant l'auto-merge pour toute réparation qui
change du code — et il ne merge jamais une pull request lui-même.

## Justification

Supprimer la corvée mécanique est l'objectif. Un commentaire consultatif laisse
encore le mainteneur appliquer chaque correctif à la main : le triage doit donc
pouvoir appliquer et pousser pour apporter le bénéfice demandé.

Le rayon d'impact doit être à la mesure du changement. Une réparation qui n'altère
aucun contenu de fichier — réécriture de message de commit, renommage, rebase — n'est
qu'historique ou métadonnées, ne porte aucune logique relisible nouvelle, et peut
sans risque suivre l'auto-merge qui régit déjà les mises à jour de routine. Une
réparation qui change du code est du source écrit par l'IA et doit passer par une
relecture humaine : l'auto-merge est donc désactivé pour elle. Parce que la
mauvaise classification est le cas dangereux, le partage trivial-ou-code est dérivé
de l'action réellement effectuée par le workflow, pas de l'affirmation du modèle ; un
changement de code ne peut donc pas passer par l'auto-merge en étant étiqueté
trivial.

La frontière de chaîne d'approvisionnement est préservée en refusant de construire la
dépendance montée dans le contexte privilégié. La réparation ne fait que des
opérations de gestion de version ; la validation du commit poussé est déléguée au run
CI ordinaire, dans son propre contexte Dependabot en lecture seule. C'est pourquoi la
décision exclut délibérément « vérifier en construisant » : cela exécuterait du code
tiers fraîchement monté alors qu'un token en écriture et une clé API sont présents.

L'agent ne merge jamais, conformément à l'ADR-0004. Il applique des correctifs et,
pour un changement de code, retire un auto-merge qu'un autre workflow aurait pu poser ;
le mainteneur et les checks requis restent le garde-barrière. Accepter un
identifiant de push dédié et restreint est un coût nécessaire, car GitHub ne
relancera pas les checks sur un correctif poussé avec le token par défaut, et un
auto-merge conservé dépend de la relance de ces checks.

## Alternatives considérées

### Consultatif seulement — commenter un patch suggéré, ne jamais pousser

Considéré parce que c'est l'option la moins privilégiée et qu'elle reflète le
précédent du contrôle ADR, qui ne fait que commenter.

Rejeté parce qu'elle ne supprime pas l'effort du mainteneur par pull request, ce qui
est toute la motivation : l'humain appliquerait encore chaque correctif.

### Autonomie totale — appliquer des correctifs de code et les laisser s'auto-merger

Considéré parce que cela règlerait même les casses au niveau du code sans étape
humaine.

Rejeté parce que cela mergerait des changements de source écrits par l'IA sans
relecture, contredisant l'ADR-0004 et la culture de relecture humaine du dépôt.

### Vérifier le correctif en construisant avant de pousser

Considéré parce que construire attraperait un mauvais patch avant qu'il n'atteigne la
branche.

Rejeté parce que construire une dépendance fraîchement montée avec un token en
écriture et la clé API présents franchit la frontière de chaîne d'approvisionnement
que le dépôt maintient ; le run CI ordinaire valide déjà le commit poussé sans
risque.

## Conséquences

### Positives

* Les pull requests Dependabot de routine atteignent un état vert et mergeable avec un
  effort du mainteneur réduit ou nul.
* Chaque pull request Dependabot en échec reçoit un triage cohérent et tracé, avec un
  verdict clair.
* Les correctifs triviaux continuent de se merger seuls, comme le font déjà les mises
  à jour de routine.

### Négatives

* Un identifiant de push dédié et restreint doit exister et être protégé.
* Un correctif trivial peut se merger sans relecture humaine — accepté, car il ne
  change aucun contenu de fichier.
* Revalider les correctifs poussés consomme des runs CI supplémentaires.

### Risques

* L'identifiant de push pourrait être détourné en cas de fuite — atténué en le
  restreignant à l'écriture contents et pull-request sur ce dépôt et en ne l'utilisant
  que pour le push.
* Le modèle pourrait étiqueter un changement de code comme trivial — atténué en
  dérivant la décision trivial-ou-code de l'action réellement effectuée, pas du modèle.
* Une réparation pourrait boucler, ou entrer en conflit avec les propres rebases de
  Dependabot — atténué en agissant au plus une fois par push de Dependabot et en ne
  changeant jamais la version de la dépendance.

## Actions de suivi

* Provisionner l'identifiant de push restreint pour que les correctifs poussés
  relancent la CI.
* S'assurer que la protection de branche marque les checks CI comme requis, afin qu'un
  auto-merge conservé ne puisse pas merger avant eux.
* Réexaminer le comportement du triage après quelques vraies pull requests Dependabot
  et ajuster l'ensemble d'actions ou les workflows surveillés si nécessaire.

## Références

* [Référence du workflow `dependabot-autofix`](../workflows/dependabot-autofix.fr.md)
  — comment la décision est implémentée (déclencheurs, gardes, token de push,
  commentaire).
* [Référence du workflow `dependabot-automerge`](../workflows/dependabot-automerge.fr.md)
  — l'auto-merge que cette décision conserve ou désactive.
* [ADR-0004](0004-check-every-pull-request-against-the-adr-base.fr.md) — le mainteneur
  est la seule autorité de merge ; la sortie du modèle est consultative.
