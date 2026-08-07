# Workflow `mutation`

🌍 🇬🇧 [English](mutation.en.md) · 🇫🇷 Français (ce fichier)

> Documentation mainteneur — fait partie de la [référence des workflows](README.fr.md).
> Ne fait pas partie de la documentation utilisateur sous `doc/`.

**Fichier du workflow :** [`.github/workflows/mutation.yml`](../../../../.github/workflows/mutation.yml)

## À quoi il sert

La couverture répond à *« cette ligne a-t-elle été exécutée par un test ? »*. Les
tests de mutation répondent à la question qui compte vraiment : *« un test
aurait-il remarqué quoi que ce soit si cette ligne avait été fausse ? »*.

[Stryker.NET](https://stryker-mutator.io/docs/stryker-net/introduction/) réécrit
la bibliothèque un petit changement à la fois — inverser une comparaison,
supprimer une instruction, retourner l'autre constante, retirer un bloc —, la
recompile, et rejoue la suite de tests contre chaque réécriture. Un **mutant** que
la suite laisse passer est un **survivant** : un comportement que le code a et que
rien n'affirme. Un mutant tué, c'est un test qui fait son travail.

Ce workflow rend ce contrôle automatique. Sur une pull request, il ne mute **que
les fichiers modifiés par la pull request** et rapporte le score **sans bloquer le
merge** — consultatif depuis l'[ADR-0046](../adr/0046-make-the-per-pull-request-mutation-gate-advisory.md),
car la sélection par-*fichier* du `--since` de Stryker fait suivre le coût à la
taille du fichier où atterrit un changement, pas au changement. Le **balayage
hebdomadaire** est le niveau imposé, et il mesure tout le reste.

Son périmètre, c'est **tout projet FirstClassErrors dont le code est livré ou
exécuté** : les trois bibliothèques (`FirstClassErrors`,
`FirstClassErrors.Testing`, `FirstClassErrors.RequestBinder`) *et* l'outillage —
la ligne de commande `fce`, le générateur de documentation, les analyseurs
Roslyn. Ce qui en reste dehors, et pourquoi, est sous *À manipuler avec
précaution* plus bas.

**JustDummies n'est pas mesuré ici.** Il est parti dans un dépôt à lui
([ADR-0011](../adr/0011-host-dummies-as-a-standalone-package.fr.md), exécuté par
[ADR-0069](../adr/0069-consume-justdummies-from-its-own-repository.fr.md)), en
emportant son workflow — ce que ce workflow était précisément écrit pour rendre
possible : un déplacement de fichier, pas une édition. Il est mesuré dans
[`Reefact/just-dummies`](https://github.com/Reefact/just-dummies).

## Quand il s'exécute

- Sur chaque **pull request ciblant `main`** — cantonné au diff et **consultatif** :
  il rapporte le score du diff mais ne bloque jamais le merge
  ([ADR-0046](../adr/0046-make-the-per-pull-request-mutation-gate-advisory.md)).
- **Chaque semaine** sur planification (lundi, 03h23 UTC) — le balayage complet, et
  le **niveau imposé**.
- À la demande via **`workflow_dispatch`** — le balayage complet.

## Comment il s'exécute

Chaque bibliothèque mutée a sa propre configuration Stryker sous
[`build/stryker/`](../../../../build/stryker/) : le projet à muter, les projets de
tests censés tuer ses mutants, et les seuils. Rien de la politique d'exécution ne
vit uniquement dans le YAML : `dotnet stryker --config-file
build/stryker/core.json` sur la machine d'un mainteneur applique donc exactement
le même barrage que la CI.

Le moteur lui-même est épinglé dans
[`.config/dotnet-tools.json`](../../../../.config/dotnet-tools.json) et restauré
par `dotnet tool restore`. Cet épinglage est porteur : un Stryker plus récent
invente de nouveaux mutants, ce qui déplace tous les scores à lui seul.

### `changed` — le diff, sur chaque pull request

Une branche de matrice par projet du périmètre. Chaque branche :

1. Fait un checkout avec **`fetch-depth: 0`** — le `--since` de Stryker compare à
   un commit, l'historique doit donc être présent.
2. Résout le **point de fourche** (`git merge-base` entre la base de la pull
   request et `HEAD`), et non la tête de la branche de base : celle-ci a pu
   avancer depuis la création de la branche, et tout fichier modifié sur `main`
   entre-temps serait alors compté comme « modifié par cette pull request ».
3. Lance Stryker avec `--since:<point de fourche>` : seuls les mutants **des
   fichiers touchés par la pull request** sont testés.
4. Rend les mutants survivants — statut, fichier, ligne, nature de la réécriture —
   dans le résumé du run, pour qu'un barrage en échec se diagnostique sans quitter
   la page.
5. Uploade les rapports HTML et JSON en artefact — `if: always()`, car la vue HTML
   montre chaque survivant *dans sa source*, ce que le tableau du résumé ne peut
   pas faire.

Une branche dont le projet n'a pas été touché par la pull request ne sélectionne
aucun mutant, signale *« unable to calculate a mutation score »*, et sort en 0.
C'est un succès — et c'est le cas courant, la plupart des pull requests ne
touchant qu'un projet.

### `gate` — l'unique check consultatif

Une matrice produit un check par branche. `gate` les regroupe sous un nom de check
stable — **`Mutation gate`** — pour que la protection de branche ait une seule
entrée à viser, plutôt que de redéclarer les noms des branches à chaque évolution
de la matrice.

Il est **consultatif** ([ADR-0046](../adr/0046-make-the-per-pull-request-mutation-gate-advisory.md)) :
il rapporte l'agrégat des legs du diff mais **ne fait jamais échouer la pull
request**. Un vrai échec de leg est remonté comme `::warning::` à investiguer, et un
run annulé par une poussée supplantante est traité comme du bruit, pas comme un
échec. Il s'exécute avec `if: always()` pour rapporter après une branche en échec
*ou annulée* au lieu d'être skippé. Le niveau imposé est le balayage `full`
hebdomadaire, pas ce check.

### `full` — le balayage hebdomadaire

Les mêmes six branches, filtre `--since` retiré : tous les mutants de tous les
projets du périmètre. Il est **consultatif par construction** — `--break-at 0`
désactive le seuil — car son rôle est de publier une tendance, pas de faire virer
`main` au rouge un lundi matin pour du code que personne n'a touché. Il se lit
dans le rapport HTML uploadé.

## Deux réglages qui n'en sont pas

`build/stryker/*.json` porte deux réglages qui ressemblent à de l'optimisation et
n'en sont pas. Les deux ont été établis par la mesure ; changer l'un ou l'autre
casse le barrage en silence plutôt que de le ralentir.

### `"test-runner": "mtp"` — obligatoire, pas une préférence

**Le runner VSTest par défaut de Stryker ne fonctionne pas du tout sur ce banc de
tests.** Tous les projets de tests sont ici en xUnit v3, et un projet de tests
xUnit v3 *est* un exécutable que l'adaptateur VSTest lance dans un processus fils
— hors de portée des crochets in-process dont Stryker se sert pour capturer la
couverture et, surtout, pour **activer** le mutant. Le run va au bout, annonce un
nombre de tests plausible, et score **0 %** : tous les mutants reviennent
« survivants », y compris des mutants qui cassent la suite de façon démontrable
quand on applique la même modification à la main. Ticket amont :
[stryker-net#3117](https://github.com/stryker-mutator/stryker-net/issues/3117).

Le runner Microsoft Testing Platform lance lui-même l'exécutable de tests : le
mutant est donc activé et le score est réel. Stryker le marque **preview** et le
signale à chaque run ; cet avertissement est attendu ici, ce n'est pas une erreur
de configuration.

Si une future montée de version de Stryker fait s'effondrer tous les scores à
zéro, c'est la première chose à vérifier.

### `"coverage-analysis": "off"` — pour l'exactitude, pas pour la vitesse

Stryker fait normalement une passe de couverture au préalable, pour que chaque
mutant ne rejoue que les tests qui l'atteignent. Sous le runner MTP, cette
sélection est encore incomplète — voir
[stryker-net#3629](https://github.com/stryker-mutator/stryker-net/issues/3629) —
et des mutants que la suite tue *effectivement* sont classés non couverts et
comptés contre le score. Mesuré sur `Error.cs`, la même population score 75 %
sélection activée et 100 % sélection coupée — et c'est le 100 % qui est le vrai
chiffre.

La couper coûte peu sur les bibliothèques, dont les suites sont rapides — de la
fraction de seconde à quelques secondes par mutant. Cela coûte davantage sur
l'outillage, dont les suites compilent du Roslyn et comparent des snapshots. C'est
une raison de garder ces branches hors du chemin critique, pas une raison de
réactiver une sélection qui rapporte le mauvais chiffre.

## Le modèle de coût, et pourquoi le barrage est cantonné au diff

**Une exécution complète de la suite de tests de la bibliothèque par mutant**,
plus environ deux minutes de coût fixe par branche (analyse de la solution,
build, run de tests initial, génération des mutants). Le balayage complet d'une
bibliothèque se compte donc en minutes ; celui de toutes, à chaque push, n'est pas
quelque chose que l'on attend devant son écran.

C'est ce qui rend le cantonnement au diff pertinent pour un check obligatoire.
Cela explique aussi deux choses qui surprennent :

- **La sélection se fait par *fichier* modifié, pas par *ligne* modifiée.** Le
  `--since` de Stryker n'a pas de granularité à la ligne. Ajouter une ligne dans
  un gros fichier sélectionne **tous** les mutants de ce fichier : le barrage
  rapporte alors le score de mutation du fichier entier — pas seulement celui de
  ce qui a été ajouté. Sur les plus gros fichiers, cela fait un job long et un
  score qui reflète une dette préexistante.
- **Une pull request qui n'ajoute que des tests sélectionne quand même des
  mutants**, via les fichiers de tests qu'elle modifie.

## D'où viennent les seuils

Chaque projet porte son propre `break` dans `build/stryker/*.json`, et les valeurs
diffèrent de l'une à l'autre à dessein. Elles ne traduisent **pas** un avis sur le
niveau que tel projet devrait atteindre : chacune a été fixée à partir du score de
balayage complet mesuré sur ce projet au moment de l'introduction du barrage,
arrondi vers le bas, avec un peu de marge pour l'éventuel mutant équivalent.

**Cinq projets n'ont pas encore de barre** : les analyseurs, le générateur de
documentation, la ligne de commande, `JustDummies`, et les analyseurs JustDummies
arrivés avec l'[just-dummies ADR-0023](https://github.com/Reefact/just-dummies/blob/main/doc/handwritten/for-maintainers/adr/0023-ship-justdummies-analyzers.md). Aucun
score de balayage complet n'a été mesuré pour eux — pour la plupart, parce que le
balayage est trop long pour avoir été exécuté interactivement — et une barre n'a
**pas** été devinée : leur `break` vaut `0`. Leurs branches tournent quand même,
échouent toujours sur un build cassé ou une suite en échec, et listent toujours
leurs survivants ; elles ne refusent simplement pas encore une pull request sur un
score. C'est le premier balayage hebdomadaire qui fournira ces cinq chiffres.

Cela fait du barrage un **cliquet**, pas une aspiration. Il dit *ne descendez pas
sous le niveau où cette bibliothèque est déjà* — une barre que toutes franchissent
dès le premier jour, si bien que le barrage ne démarre jamais rouge, et qui ne
peut que monter. Relever une valeur quand le balayage hebdomadaire montre de la
marge est l'usage prévu ; en baisser une devrait ressembler à une décision.

La conséquence à garder en tête : une bibliothèque nettement sous les 100 % a une
barre basse aujourd'hui, et une pull request qui touche l'un de ses fichiers les
plus faibles peut quand même passer dessous. C'est le barrage qui fonctionne, pas
qui se trompe — le rapport dit quelle assertion manque.

`JustDummies` échappait à cette règle — son balayage était trop long pour avoir
servi de calibration, il partait donc avec son barrage sur le score coupé. Cette
exception est partie avec lui ; le dépôt qui le porte désormais porte aussi sa
calibration.

## Quand le survivant est un mutant équivalent

Il arrive que la réponse honnête soit que le mutant ne peut pas être tué : la
réécriture ne change pas le comportement observable, aucun test ne saurait donc
faire la différence. Écrire un test pour le poursuivre reviendrait à écrire un
test qui affirme un détail d'implémentation — pire que le manque.

Stryker accepte cette réponse dans la source, au plus près du code, sous forme de
commentaire :

```csharp
// Stryker disable once Statement : the trace call has no observable effect
```

La forme est `// Stryker disable [once] <mutateur|all> [: raison]`, avec
`// Stryker restore all` pour refermer un bloc sans `once`. Préférez `once`,
préférez nommer le mutateur plutôt que `all`, et donnez toujours la raison — une
exclusion non documentée est indiscernable d'un test manquant six mois plus tard.
N'y recourez qu'après avoir établi que le mutant est bien équivalent ; baisser un
seuil pour faire taire un survivant masque avec lui tous les survivants à venir.

## Permissions & sécurité

`contents: read` seulement. Le workflow fait un checkout, un build et lance des
tests ; il ne stocke aucun secret et n'a besoin d'aucun périmètre en écriture.

## À manipuler avec précaution

- **`fetch-depth: 0` est nécessaire**, ce n'est pas une habitude. Un clone
  superficiel rend le point de fourche inatteignable et `--since` ne peut plus le
  résoudre.
- **`--since` veut une branche, un tag ou un vrai SHA de commit — `HEAD` est
  refusé.** `--since:HEAD` fait échouer tout le run avec *« No branch or tag or
  commit found with given target »* ; c'est pourquoi le workflow résout d'abord
  `git merge-base` en SHA au lieu de passer une expression de révision.
- **Le cliquet de warnings de la CI n'a pas besoin d'être coupé ici.**
  L'inquiétude est légitime — Stryker compile du code *muté*, et un mutant lève
  couramment un warning que l'original n'avait pas — mais, mesure faite,
  `GITHUB_ACTIONS=true` ne change rien : Stryker compile les mutants via Roslyn
  avec ses propres options et n'hérite pas du `TreatWarningsAsErrors` de
  [`Directory.Build.props`](../../../../Directory.Build.props). Le nombre
  d'erreurs de compilation est identique cliquet actif ou coupé. Si un futur
  Stryker se mettait à en tenir compte, des mutants deviendraient silencieusement
  des erreurs de compilation au lieu d'être testés — c'est ce compteur, dans le
  log du run, qui le révélerait.
- **`if: always()` sur `gate` est porteur.** Retirez-le et `gate` est skippé dès
  qu'une branche échoue ou est annulée, donc il ne rapporte jamais l'agrégat —
  l'avertissement consultatif ([ADR-0046](../adr/0046-make-the-per-pull-request-mutation-gate-advisory.md))
  serait silencieusement perdu précisément quand il y a quelque chose à dire.
- **La version de Stryker est épinglée dans le manifeste d'outils.** La monter est
  un acte délibéré : attendez-vous à voir les scores bouger, et relisez les
  seuils.
- **Les seuils vivent dans `build/stryker/*.json`, pas dans le YAML.** C'est ce
  qui garde un run local et la CI d'accord. `break` est la valeur qui fait échouer
  le build ; `high`/`low` ne colorent que le rapport.
- **Trois choses restent hors périmètre, et aucune pour une raison de coût.** Les
  échantillons `Usage` et les benchmarks du binder ne sont pas du comportement
  livré — un échantillon et un harnais de mesure. `FirstClassErrors.GenDoc.Worker`
  est un point d'entrée de processus qu'aucun test n'exerce *en processus* : il est
  prouvé de bout en bout par le job `floor` de [`ci`](ci.fr.md), et le muter ne
  fabriquerait que des survivants qu'aucun test ne pourrait tuer. Tout le reste de
  ce qui se compile dans un artefact livré est mesuré.
- **Les branches d'outillage sont les lentes.** Les suites d'analyseurs et de
  générateur pilotent des compilations Roslyn et des comparaisons de snapshots :
  chacun de leurs mutants coûte plus cher que celui d'une bibliothèque. C'est
  pourquoi elles relèvent du balayage hebdomadaire — le run autorisé à durer le
  temps qu'il faut — et pourquoi le cantonnement au diff compte plus pour elles
  que partout ailleurs : une pull request qui ne touche pas le générateur ne paie
  rien pour lui.
- **Un survivant n'est pas automatiquement un bug**, et la réponse à un mutant
  équivalent est un commentaire `// Stryker disable once` avec sa raison, jamais un
  seuil abaissé — voir *Quand le survivant est un mutant équivalent* plus haut.

## L'exécuter en local

```bash
dotnet tool restore
dotnet stryker --config-file build/stryker/core.json
```

C'est le balayage complet d'une bibliothèque, et cela prend un moment. Pour
reproduire ce que fait le barrage sur une branche :

```bash
dotnet stryker --config-file build/stryker/core.json --since:$(git merge-base origin/main HEAD)
```

Les rapports atterrissent dans `StrykerOutput/` (ignoré par git) ; ouvrez
`reports/mutation-report.html`.

## Voir aussi

- [`ci`](ci.fr.md) — le barrage principal, et l'endroit où le cliquet de warnings
  est imposé.
- [`sonar`](sonar.fr.md) — couverture de lignes et de branches. Les tests de
  mutation en sont le complément, pas le remplacement : Sonar dit ce qui a été
  *exécuté*, ce workflow dit ce qui a été *vérifié*.
- [ADR 0043 — Gate pull requests on the mutation score of what they
  changed](../adr/0043-gate-pull-requests-on-the-mutation-score-of-the-diff.md)
  — la décision que ce workflow met en œuvre *(rédigée en anglais)*.
