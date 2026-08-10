# ADR-0070 | Intégrer les pull requests par rebase et garder l'historique de `main` linéaire

🌍 🇫🇷 Français (ce fichier) · 🇬🇧 [English](0070-land-pull-requests-by-rebasing-and-keep-main-linear.md)

**Statut :** Proposé
**Proposé :** 2026-08-10
**Décideurs :** Reefact

## Contexte

`main` portait 1366 commits, dont 401 merges. Parmi ces merges, 314 sont les
commits `Merge pull request #NN` écrits par le bouton de merge de GitHub, et 87
sont des commits `Merge branch 'main' into …` créés lorsqu'une branche
récupérait `main` en cours de revue. Les 965 commits restants — 71 % — sont ceux
qui ont changé quelque chose.

Les commits de merge ne sont pas seulement du bruit. Ils coûtent trois choses
concrètes :

* `git log main` entrelace les branches développées en parallèle : lire
  l'historique revient à séparer ce qui s'est passé du moment où les branches se
  sont rencontrées.
* `git bisect` et `git blame` traversent des arbres que personne n'a jamais
  écrits : l'arbre d'un commit de merge est la réconciliation de deux branches
  par une machine, pas un état qu'un contributeur a rédigé, relu ou exécuté.
* Les règles du dépôt sont justifiées par le commit de merge. `AGENTS.md`,
  `CONTRIBUTING.md`, `CLAUDE.md` et la commande `/tidy-history` soutiennent tous
  que l'historique d'une branche doit être propre *parce que le commit de merge
  le conserve*. L'argument tient, mais il nomme le mauvais mécanisme.

Ce dernier point est le plus important. Ce dépôt exige déjà que chaque commit se
tienne seul, lint chaque en-tête en CI et demande à un agent de nettoyer une
branche avant la revue. La stratégie de merge est la seule partie du processus
qui ne découle pas de cette doctrine : elle conserve la *forme* d'une branche —
quand elle a commencé, quand elle a été rafraîchie, quand elle a atterri — alors
que tout le reste porte sur le *contenu* de ses commits.

### Pourquoi maintenant

La bibliothèque n'a pas atteint la 1.0.0. Son unique tag est
`lib-v0.1.0-preview.1`, aucune release n'est publiée et aucune pull request n'est
ouverte. Rien, hors de ce dépôt, ne dépend d'un identifiant de commit : aucun
consommateur n'épingle un SHA, aucune entrée de changelog n'en cite un, et aucun
package publié ne porte de SourceLink qui en désigne un.

C'est toute la fenêtre pendant laquelle réécrire `main` coûte peu, et elle se
referme à la 1.0.0. Dès qu'une version stable est livrée, son SourceLink, ses
notes de version et les références que les consommateurs épinglent font des
identifiants de `main` une partie de ce que le projet promet ; les réécrire
cesse alors d'être une tâche de dépôt pour devenir une rupture pour tout l'aval.
Le faire maintenant achète un historique contre lequel la ligne 1.0.0 pourra se
lire. Le faire plus tard reviendrait à ne pas le faire du tout.

## Décision

L'historique de `main` est **linéaire**. La décision a deux volets.

**Les pull requests atterrissent par rebase.** Le *Rebase and merge* de GitHub
devient la seule stratégie activée sur ce dépôt ; les commits de merge et les
squash merges sont désactivés. Les commits d'une pull request sont rejoués sur
`main` tels qu'ils ont été écrits, et aucun commit n'est créé pour enregistrer
l'intégration.

**L'historique existant est réécrit linéaire, une fois.** Les 401 commits de
merge sont supprimés et les 965 commits rédigés sont rejoués dans l'ordre du
premier parent, en préservant pour chacun son message, son auteur, son
committer et ses dates.

La réécriture n'est pas un simple `git rebase` de tout l'historique. Elle
parcourt la chaîne de premier parent de `main` et, pour chaque merge de pull
request, rejoue les commits de cette pull request puis **épingle** l'arbre
obtenu à celui qu'avait réellement le commit de merge. Cet épinglage est ce qui
rend l'opération sûre : 15 des 401 merges portaient une résolution de conflit
faite à la main, qu'un rejeu naïf aurait perdue en silence. L'épinglage
rétablit le contenu historique exact à chaque point où `main` a avancé : toute
dérive reste contenue dans une seule pull request et ne peut jamais franchir une
frontière.

La réécriture n'est acceptée que sur preuves, pas sur intention. Les
vérifications qui devaient passer, et qui sont passées :

| Contrôle | Résultat |
| --- | --- |
| Arbre du sommet de `main`, avant et après | identique |
| Arbre à chacune des 362 étapes de premier parent | 362 / 362 identiques |
| Commits rédigés préservés (auteur, e-mail, date, sujet) | 965 / 965 |
| Corps des messages de commit, comparés en multiensemble | 0 perdu, 0 ajouté |
| Conflits de rejeu | 0 |
| Commits de merge restants | 0 |
| Build et suite de tests sur le sommet réécrit | 974 tests, 0 échec |

La réécriture est une **exception unique, et ce record ne fait pas précédent pour
une seconde**. À partir d'ici, `main` ne fait que croître : son historique se
corrige par de nouveaux commits, jamais en réécrivant ceux déjà publiés. Une
réécriture ultérieure exigerait son propre ADR et devrait répondre à l'objection
à laquelle celle-ci n'échappe que par le calendrier — qu'à ce moment-là, les
identifiants appartiennent au contrat publié.

## Conséquences

### Tous les SHA de commit de `main` changent

C'est le prix de la décision, et il se paie une fois. Quiconque détient un clone
doit le recloner ou le réinitialiser ; un clone qui ferait un `pull` regrefferait
sinon l'ancien historique. Aucune pull request ouverte n'est invalidée et aucune
release publiée n'est cassée : la seule release GitHub est un brouillon non
taggé.

### L'historique publié reste atteignable

Les anciens SHA ne disparaissent pas. GitHub conserve `refs/pull/NN/head` pour
chaque pull request ouverte, si bien que les commits référencés par chaque pull
request mergée restent résolvables et que les liens dans les issues et les
revues continuent de fonctionner. Une référence de sauvegarde du `main`
d'avant la réécriture est poussée au préalable, et c'est elle que servirait une
restauration.

### Rafraîchir une branche en y mergeant `main` ne convient plus

`CONTRIBUTING.md` (« Branches ») propose aujourd'hui deux façons de reporter les
avancées de `main` dans une branche ouverte : rebaser tant que la branche
n'appartient qu'à vous, merger `main` dedans dès que d'autres ont pu baser du
travail dessus. La seconde produit exactement les commits
`Merge branch 'main' into …` que cette décision supprime. La présente ADR ne
tranche pas : la règle de rafraîchissement protège le travail déjà récupéré par
un collaborateur, ce qui est une préoccupation distincte de la forme de
l'historique, et les branches de ce dépôt n'ont en pratique qu'un propriétaire.
Le point est nommé ici pour que la prochaine personne à le rencontrer sache
qu'il s'agit d'un cas connu et non d'un oubli.

### Deux tags se déplacent, et 28 signatures sont perdues

`lib-v0.1.0-preview.1` et `archive/justdummies-adr` sont repointés sur les
commits équivalents de l'historique réécrit, le second conservant son
annotation, son tagueur et sa date. 28 commits portaient une signature ; un
commit réécrit ne peut pas la conserver, ils deviennent non signés. Aucune
protection de branche n'exigeait de commits signés.

### Les dates d'auteur ne sont plus monotones

75 paires de commits consécutifs présentent une date d'auteur postérieure avant
une antérieure. C'est inhérent à un historique linéaire construit à partir de
travaux menés en parallèle, et c'est ce qu'une stratégie de rebase aurait produit
dès l'origine.

## Alternatives considérées

### Changer la stratégie pour la suite et conserver les merges existants

L'option la moins coûteuse, rejetée pour la raison même qui motive la décision.
Un demi-historique n'est pas un historique lisible : `git log` entrelacerait
toujours et `git bisect` traverserait toujours des arbres que personne n'a
écrits, sur les 1366 commits qui représentent toute la vie du projet à ce jour.
Le coût de la réécriture est borné et payé une fois ; celui du bruit se paie à
chaque lecture.

### Écraser chaque pull request en un commit unique

Rejeté. Cela produit mécaniquement un historique linéaire — un commit par pull
request, aucun rejeu, aucun conflit — mais détruit la discipline par commit que
ce dépôt s'attache réellement à faire respecter. Les 965 commits rédigés, chacun
avec un en-tête linté et une intention unique, s'effondreraient en 314 dont les
messages seraient les titres des pull requests. La doctrine dit que le commit
est l'unité du changement ; l'écrasement fait de la pull request cette unité.

### Réécrire avec un simple `git rebase --root`

Rejeté comme non sûr, plutôt que comme faux. Cela rejouerait les mêmes commits,
mais sans aucune garantie sur le résultat : rien ne vérifie que l'arbre obtenu à
chaque intégration correspond à ce que `main` contenait réellement, si bien que
les 15 merges résolus à la main pourraient être perdus en silence et que la
divergence n'apparaîtrait que bien plus tard, sous la forme d'un bug sans cause
apparente. La méthode par épinglage et vérification coûte un script et fournit
une preuve.
