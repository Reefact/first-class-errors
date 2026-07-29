# ADR-0056 | Énoncer les règles de codage là où un agent peut les appliquer, et les vérifier à l'édition

🌍 🇫🇷 Français (ce fichier) · 🇬🇧 [English](0056-state-the-coding-rules-where-an-agent-can-act-on-them.md)

**Statut :** Accepté
**Proposé :** 2026-07-28
**Accepté :** 2026-07-29
**Décideurs :** Reefact

## Contexte

`FirstClassErrors.sln.DotSettings` consigne le style de code de ce dépôt. C'est un artefact
ReSharper/Rider : Rider le lit, et aucun compilateur, job de CI ou agent automatisé ne le peut.

Une part substantielle et croissante du code de ce dépôt est écrite par des agents automatisés.
Jusqu'ici, les instructions qui leur étaient données déléguaient le sujet entier à ce fichier —
*« code style and inspection severities are defined in `FirstClassErrors.sln.DotSettings`;
follow it »*. Cette phrase se lit comme une instruction, mais n'en est pas une pour un lecteur
incapable d'ouvrir le fichier qu'elle désigne.

La conséquence a été mesurée, non supposée. La règle des types explicites — déclarée au niveau
erreur dans le `.DotSettings` depuis son introduction — a dérivé jusqu'à 203 violations dans
17 fichiers, tous écrits par des agents, alors que la consigne de la suivre était en place.

L'[ADR-0055](0055-enforce-the-style-rules-the-compiler-can-express.fr.md) a comblé une partie de
cet écart en redisant la règle des types explicites dans `.editorconfig` et en la faisant appliquer
à la compilation, la CI la promouvant en erreur. Cette barrière fait autorité, mais elle ne se
déclenche que lorsque quelqu'un compile : un agent qui édite un fichier sans compiler porte la
violation jusqu'à la pull request, là où le coût de la correction est le plus élevé et où une pull
request rouge est la première chose que voit le mainteneur.

Seule une minorité des règles du `.DotSettings` possède un équivalent Roslyn. L'alignement en
colonnes des déclarations consécutives, les motifs de disposition des fichiers et les conventions
de régions ne peuvent pas être exprimés dans `.editorconfig` ; aucune barrière de compilation ne
les couvrira jamais, et si elles doivent atteindre un agent, la prose est le seul canal.

Le dépôt exécute déjà des hooks sur l'activité des agents, configurés dans un
`.claude/settings.json` versionné et implémentés en scripts shell sous `.claude/hooks/`. Celui qui
existe lit la branche et signale ; il ne réécrit jamais rien, laissant à l'agent le jugement comme
la correction.

## Décision

Les règles de codage qu'un agent doit suivre sont écrites en clair dans `CLAUDE.md`, chacune
indiquant comment elle est vérifiée, et un hook les contrôle sur le fichier que l'agent vient
d'écrire.

## Justification

Une instruction que son lecteur ne peut pas appliquer n'est pas une instruction, et les
203 violations mesurent ce que cela coûte. Remplacer le renvoi par les règles elles-mêmes est la
correction minimale : elle place la règle là où elle est lue, sous la forme où elle doit être
appliquée, avant qu'une seule ligne soit écrite. Tout le reste de cette décision n'est qu'un filet
sous celle-là.

Vérifier à l'édition plutôt qu'à la seule compilation découle de l'endroit où se situe le coût. La
barrière de compilation de l'ADR-0055 attrape la même violation, mais plus tard et de façon moins
fiable — c'est l'agent qui décide quand compiler, et celui qui s'en dispense découvre le problème
par une pull request rouge. Un contrôle qui se déclenche à l'écriture referme la boucle au moment
où l'erreur est commise, tant que le contexte qui l'a produite est encore présent et que la
correction tient en une édition. Les deux sont complémentaires plutôt que redondants : le hook est
immédiat et consultatif, la barrière de compilation fait autorité et bloque.

Le hook signale et ne réécrit pas, suivant la convention que le hook existant du dépôt établit
déjà. Ce choix compte davantage qu'il n'y paraît : un hook qui corrigerait silencieusement la
sortie laisserait l'agent croire qu'il a écrit du code conforme, et ne lui apprendrait rien pour le
fichier suivant. Laisser la correction à l'agent maintient la sortie de l'agent comme objet même de
la correction.

Écrire les règles dans `CLAUDE.md` plutôt que dans un nouveau document les garde là où un agent
regarde déjà, et associer chaque règle au mécanisme qui la vérifie prévient l'échec dont cette
décision traite : une règle énoncée sans rien derrière, c'est exactement ce qu'était le renvoi au
`.DotSettings`. Là où aucun mécanisme ne peut exister — les règles de disposition et d'alignement
que Roslyn ne sait pas exprimer — la prose le dit, et demande de la retenue plutôt que de la
conformité : ne reformate pas ce que tu n'as pas changé.

Le `.DotSettings` conserve son rôle inchangé. Il reste ce que Rider applique et ce que le
mainteneur édite ; rien dans cette décision ne demande à quiconque de maintenir le style à deux
endroits à la main, puisque `CLAUDE.md` n'énonce que le sous-ensemble qu'un agent peut appliquer et
nomme le contrôle qui tient chacun honnête.

## Alternatives considérées

### Laisser le renvoi au `.DotSettings` et s'appuyer sur la seule barrière de compilation

Envisagée parce que l'ADR-0055 rend déjà la règle des types explicites bloquante, donc rien de non
conforme ne peut fusionner, et parce qu'elle garde le jeu d'instructions réduit.

Écartée parce qu'elle repousse la découverte de chaque violation au point le plus tardif possible.
La barrière ne se déclenche que si quelqu'un compile ; un agent qui édite et pousse sans compiler
transforme une correction d'une ligne en pull request rouge. Elle ne couvre par ailleurs rien
au-delà du sous-ensemble Roslyn, c'est-à-dire l'essentiel du style du dépôt.

### Faire corriger la violation par le hook au lieu de la signaler

Envisagée parce qu'elle garantirait une sortie conforme quoi qu'écrive l'agent, et parce qu'un
formateur est l'outil évident pour cela.

Écartée pour deux raisons. Le hook existant du dépôt établit le lire-et-signaler comme convention,
et s'en écarter silencieusement rendrait le comportement des hooks imprévisible. Plus important :
un hook qui rapièce derrière l'agent laisse celui-ci croire que sa sortie était correcte, si bien
que la même erreur revient au fichier suivant ; la dérive que cette décision traite est un défaut
d'apprentissage, pas de formatage. L'option de réécriture a par ailleurs été écartée
indépendamment pour le moteur ReSharper dans l'ADR-0055, sur preuve qu'elle ne préserve pas le code.

### Placer les règles dans un document dédié plutôt que dans `CLAUDE.md`

Envisagée parce que la liste est appelée à grandir, et qu'un document de standards de codage en est
le foyer conventionnel.

Écartée parce que `CLAUDE.md` est ce qu'un agent lit sans qu'on le lui demande. Un document séparé
aurait besoin d'un renvoi depuis `CLAUDE.md` pour être trouvé — c'est-à-dire précisément
l'indirection qui a échoué ici.

## Conséquences

### Positives

* Une règle qu'un agent doit suivre est désormais énoncée sous une forme qu'il peut appliquer,
  avant qu'il n'écrive.
* Les violations apparaissent à l'édition, où la correction tient en une ligne, plutôt que sur une
  pull request rouge.
* Les règles qu'aucun outil ne sait vérifier — disposition, alignement, régions — atteignent un
  agent pour la première fois, sous forme de demande de retenue.
* La liste a un foyer évident, si bien que la règle suivante est ajoutée plutôt que supposée.

### Négatives

* La règle des types explicites est désormais énoncée à trois endroits : le `.DotSettings`,
  `.editorconfig` et `CLAUDE.md`. Chacun a un lecteur distinct, mais ils doivent s'accorder.
* Le hook s'exécute après chaque édition de fichier : son coût est payé en permanence et ses
  contrôles doivent rester peu coûteux.
* Un contrôle textuel ne peut pas être aussi précis qu'un compilateur ; une part de jugement reste
  chez l'agent.

### Risques

* Les trois énoncés de la règle pourraient diverger ; rien ne vérifie qu'ils s'accordent.
* Un hook bruyant est un hook ignoré. Si les faux positifs s'accumulent à mesure que des règles
  s'ajoutent, les signalements cessent d'être lus et le mécanisme s'arrête silencieusement.
* Les règles sans mécanisme derrière elles reposent sur la retenue de l'agent, ce contre quoi cette
  décision argumente par ailleurs. Elles sont une atténuation, pas une garantie.

## Actions de suivi

* Ajouter les règles restantes qu'un agent peut appliquer à mesure qu'elles sont identifiées,
  chacune avec son contrôle.
* Surveiller le taux de faux positifs du hook à mesure que des règles s'ajoutent, et retirer ou
  restreindre tout contrôle incapable de rester silencieux sur du code conforme.
* Réexaminer si les règles du `.DotSettings` sans équivalent Roslyn peuvent être contrôlées
  textuellement, ou si la retenue reste la seule réponse disponible.

## Références

* [ADR-0055](0055-enforce-the-style-rules-the-compiler-can-express.fr.md) — la moitié « compilation »
  du même problème, et les mesures qui la fondent.
* [ADR-0045](0045-guard-public-and-internal-arguments-against-null.fr.md) — une convention rendue
  observable plutôt que laissée à l'attention.
