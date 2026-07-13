# Contribuer à FirstClassErrors

🌍 **Langues:**  
🇬🇧 [English](../CONTRIBUTING.md) | 🇫🇷 Français (ce fichier)

FirstClassErrors considère les erreurs comme des concepts de premier ordre,
documentés et diagnostiquables. L’historique du dépôt doit être aussi lisible que
les erreurs que la bibliothèque produit. Ce guide définit la façon dont les
commits sont écrits ici.

## Compiler et tester

* Framework cible : **.NET Standard 2.0**.
* Compiler : `dotnet build FirstClassErrors.sln`
* Tester : `dotnet test FirstClassErrors.sln`
* Tests des analyzers, lorsque vous touchez aux analyzers : `dotnet test FirstClassErrors.Analyzers.UnitTests`

Voir [`CLAUDE.md`](../CLAUDE.md) pour l’organisation du projet et les lignes
directrices plus larges concernant les changements.

## Activer le hook de message de commit

Un hook `commit-msg` vérifie chaque message par rapport à la convention ci-dessous
avant qu’il ne soit enregistré. Il est versionné sous `.githooks/` ; activez-le une
fois par clone :

```
git config core.hooksPath .githooks
```

La même vérification s’exécute en CI sur chaque pull request : un hook contourné
(`git commit --no-verify`) est donc rattrapé avant le merge. Les commits de merge
en sont exemptés. La vérification elle-même se trouve dans
`tools/commit-lint/lint-commit-message.sh`, partagée par le hook et la CI pour que
les deux ne divergent jamais.

Le hook laisse passer les commits `fixup!`, `squash!` et `amend!` pour que vous
puissiez construire un rebase autosquash ; la CI les rejette, alors éliminez-les
(squash) avant le merge.

## Branches

### Pourquoi

Une pull request se lit par rapport à sa branche. Deux branches portent la même
fonctionnalité. La première a été coupée depuis `origin/main` il y a une heure et
contient trois commits — le diff de sa pull request *est* la fonctionnalité, et rien
d’autre. La seconde a été coupée depuis un `main` local vieux de trois semaines, puis
ravivée pour une deuxième idée une fois la première mergée ; son diff porte quinze
commits, dont douze déjà sur `main`, et le relecteur ne peut distinguer la demande du
résidu.

```
$ git log --oneline origin/main..HEAD    # la seconde branche
a1b2c3d feat(core): the change the pull request is for
9f8e7d6 feat(gendoc): a renderer already merged, dragged along
...douze autres commits déjà sur main...
```

La branche n’est pas le travail. C’est l’espace de travail jetable d’**une seule**
pull request — coupée fraîchement depuis le remote, utilisée une fois, jetée au merge.
Tout ce qui suit en découle.

### La règle

* Une branche porte **une seule pull request**, et cette pull request porte une seule
  unité de travail cohérente. Tout travail sans rapport avec cette unité DOIT prendre
  sa propre branche — la lecture, au niveau de la branche, de *deux intentions, deux
  commits*.
* `main` n’est écrite **que** par merge. Aucun commit n’atterrit directement sur
  `main` ; elle avance lorsqu’une pull request relue est mergée.
* Une branche DOIT être coupée depuis la **pointe de `origin/main`**, fraîchement
  récupérée (fetch) — jamais depuis un `main` local qui peut être en retard, ni depuis
  une autre branche thématique :

  ```
  git fetch origin
  git switch -c <author>/<short-description> origin/main
  ```
* Un nom de branche DOIT prendre la forme `<author>/<short-description>`. L’`<author>`
  est l’identifiant GitHub du propriétaire de la branche — la personne ou l’outil à qui
  le travail appartient : `sylvain/…`, `claude/…`, `dependabot/…`. La
  `<short-description>` DOIT être en anglais, en minuscules, en kebab-case, et nommer le
  changement, pas le fichier qu’il touche : `sylvain/gendoc-invalid-culture`, jamais
  `sylvain/GenDoc.cs`.
* Un outil qui génère ses propres branches possède son espace de noms et conserve sa
  structure native en dessous — `dependabot/nuget/Newtonsoft.Json-13.0.1`,
  `renovate/…`. La forme `<author>/<short-description>` lie les branches qu’une personne
  ou un agent coupe à la main ; le schéma d’un générateur lui appartient, et le combattre
  n’apporte rien.
* Le nom de branche ne porte **aucun type**. Le type est une propriété de chaque commit,
  vérifiée là par le hook et par la CI ; une branche rassemble des commits de plusieurs
  types, et un préfixe unique en nommerait un et cacherait les autres — la même raison
  pour laquelle un titre de pull request à intentions multiples ne prend pas de `type:`
  (voir *Titres de pull request* plus bas). Le propriétaire est ce que le nom ajoute, car
  le propriétaire est ce que les commits ne portent pas.
* Une branche vit exactement aussi longtemps que sa pull request reste **ouverte**, et
  PEUT être réutilisée uniquement pour cette même demande — corrections de relecture,
  changements demandés sur la pull request.
* Une fois la pull request **mergée ou fermée**, la branche est terminée. Elle NE DOIT
  PAS être ravivée, pas même pour un suivi sur le même sujet : une pull request mergée ne
  peut pas décrire un nouveau travail, et une pull request fermée a été mise de côté. Le
  suivi passe par une nouvelle branche, coupée fraîchement depuis `origin/main`.
* Pour reporter la progression de `main` dans une branche ouverte : tant que la branche
  n’est qu’à vous, **rebasez-la** sur `origin/main` ; dès que d’autres ont pu baser du
  travail dessus, **mergez** plutôt `origin/main` dedans. L’un comme l’autre garde la
  branche à jour sans réécrire ce qu’un collaborateur a déjà récupéré (pull).
* Réécrire l’historique d’une branche — un force-push, un `git rebase -i` — est
  acceptable tant que la branche n’est **qu’à vous**, et c’est ainsi qu’un message de
  commit rejeté par le lint ou par un relecteur se corrige, même en cours de relecture :
  un message rejeté ne peut pas être corrigé par un commit de suivi (voir *Messages de
  commit*). Dès que quelqu’un d’autre a pu **baser du travail dessus**, son historique NE
  DOIT PAS être réécrit — un force-push jette ce qui a été construit par-dessus. Le
  travail qui n’est pas le vôtre n’est pas le vôtre à réécrire ou à supprimer.
* Avant d’ouvrir la pull request, **lisez la branche** par rapport à un `origin/main`
  frais :

  ```
  git fetch origin
  git log  --oneline origin/main..HEAD     # les commits que la demande ajoute
  git diff --stat    origin/main...HEAD    # les fichiers qu’elle touche
  ```

  Si l’un ou l’autre montre quelque chose qui ne concerne pas la demande, la branche a
  dérivé — scindez-la avant la relecture, pas après.

### La doctrine

**La branche est l’unité de travail en cours ; la pull request est ce qu’elle devient.**
Une branche, une pull request, une unité de travail — le même un-pour-un que la doctrine
trace entre le commit et son changement.

**Le nom dit qui, les commits disent quoi.** Une branche possède une pull request qui
peut porter à la fois une fonctionnalité, le refactoring qui l’a préparée, et ses tests ;
aucun type unique ne la nomme honnêtement. Le type vit sur chaque commit, là où le hook
l’impose. Le nom de branche ajoute la seule chose que les commits omettent — à qui
appartient le travail — de sorte que `claude/…` et `dependabot/…` ne sont pas des
exceptions mais la règle elle-même, lue de la même façon sur un humain ou sur une machine.

**Une branche est jetable.** Son historique est préservé par le commit de merge qui
l’intègre ; la référence elle-même est coupée à neuf et supprimée au merge. Rien de
précieux ne vit uniquement sur une branche.

**Une branche mergée est épuisée.** La raviver empile du nouveau travail sur un historique
déjà réglé et bifurque depuis un `main` qui a bougé. Le relecteur en paie le prix, lisant
le résidu comme s’il s’agissait de la demande.

**Coupez depuis le remote, pas depuis le local.** Un `main` local est en retard
silencieusement ; une branche coupée depuis lui traîne ce retard dans chaque diff.
`origin/main`, fraîchement récupérée, est la seule base.

**Un travail sans rapport est une nouvelle branche, pas un passager.** Une branche qui
porte deux changements force une pull request qui ne peut décrire ni l’un ni l’autre — la
forme, au niveau de la branche, du commit qui porte deux intentions.

### Exemples

| Branche | Pourquoi elle convient |
|---|---|
| `sylvain/add-html-renderer` | Propriétaire et changement, nommés simplement. Le type qu’elle portera vit dans ses commits. |
| `claude/gendoc-invalid-culture` | La branche d’un agent ; la description nomme la zone, pas `GenDoc.cs`. |
| `dependabot/nuget/Newtonsoft.Json-13.0.1` | Un générateur conserve sa structure native sous son espace de noms `dependabot/`. |
| `sylvain/security-policy` | La description seule porte le sujet ; la branche n’a besoin d’aucun type. |

### Anti-patterns

| Branche ou manœuvre | Ce qui ne va pas |
|---|---|
| un commit poussé directement sur `main` | `main` n’avance que par merge. Même un correctif d’une ligne prend une branche et une pull request. |
| `patch-1`, `my-work`, `tmp` | Aucun propriétaire, et ça ne nomme rien. Un nom de branche se lit dans la liste des pull requests ; il DOIT dire qui possède quoi. |
| `feat/add-html-renderer` | Un type à la place du propriétaire. Le type appartient aux commits ; le préfixe de branche est le propriétaire : `sylvain/add-html-renderer`. |
| `sylvain/GenDoc.cs` | Nomme un fichier. Il devrait nommer le changement : `sylvain/gendoc-invalid-culture`. |
| `sylvain/corrige-le-rendu` | Pas en anglais. |
| raviver une branche mergée `claude/add-html-renderer` pour un suivi | Une branche mergée est épuisée. Coupez le suivi à neuf depuis `origin/main`. |
| une branche coupée depuis un `main` local vieux de trois semaines | Le diff de la pull request se remplit de commits déjà sur `main`. Fetchez d’abord ; coupez depuis `origin/main`. |
| une seule branche portant une fonctionnalité et un ajustement CI sans rapport | Aucune pull request unique ne décrit les deux. Deux branches, deux demandes. |
| force-pusher une branche sur laquelle d’autres ont construit | Réécrit un historique partagé et jette le travail poussé par-dessus. Ne réécrivez que tant que la branche n’est qu’à vous. |

## Messages de commit

Cette section adapte la spécification [Conventional Commits 1.0.0](https://www.conventionalcommits.org/en/v1.0.0/).
Les mots-clés DOIT, NE DOIT PAS, DEVRAIT et PEUT doivent être interprétés comme décrit
dans [BCP 14](https://www.rfc-editor.org/info/bcp14), et uniquement lorsqu’ils
apparaissent en majuscules.

### Pourquoi

Une release se prépare. Il faut savoir ce qu’une branche contient, ce qu’il faut y
reporter, et quel numéro de version en sort.

```
a3f1c2e fix bug
8b41d90 update renderer
1d0e4aa wip
```

Cet historique n’apprend rien. Chaque question force à ouvrir un diff.

```
a3f1c2e fix(gendoc): render error examples with the invariant culture
8b41d90 feat(gendoc): emit an RFC 9457 problem type in examples
1d0e4aa refactor(gendoc): extract output routing into a writer
```

Celui-ci répond à trois questions sans ouvrir un seul diff : ce que la branche contient,
quel commit reporter dans une release, et si la version passe de `1.4.2` à `1.4.3` ou à
`1.5.0`. C’est la lecture du relecteur, et de celui qui prépare la release. Demain, ce
sera celle d’un outil.

### La règle

La règle porte sur **chaque commit**, pas sur un message de merge. Un commit voyage
seul : il est cherry-piqué sur une branche de release, listé dans un `git log --oneline`,
lu isolément six mois plus tard. Son message DOIT se suffire à lui-même.

#### Forme

```
<type>[(<scope>)][!]: <description>

[body]

[footers]
```

* Le commit DOIT commencer par un type, éventuellement suivi d’un scope et d’un `!`, puis
  d’un deux-points et d’un espace.
* Tout ce qui est écrit dans le message DOIT être en anglais — en-tête, corps et footers.
* Un commit DOIT porter un seul type, celui de son intention. Deux intentions
  indépendantes DOIVENT être deux commits : le message force la scission qui devrait avoir
  lieu.

#### Types

Classés ici avec d’abord les deux types qui pilotent la version, puis le reste par ordre
alphabétique. La liste est fermée.

| Type | Quand l’utiliser | Effet minimal sur la version |
|---|---|---|
| `feat` | Une nouvelle capacité, visible pour le consommateur du package | `MINOR` |
| `fix` | La correction d’un comportement défectueux | `PATCH` |
| `build` | Système de build, dépendances, packaging, artefacts de déploiement | aucun imposé |
| `chore` | Ce qui ne touche ni le code de production ni sa livraison | aucun imposé |
| `ci` | Configuration du pipeline | aucun imposé |
| `docs` | Documentation uniquement | aucun imposé |
| `perf` | Un gain de performance, à comportement observable constant | aucun imposé |
| `refactor` | Restructuration, à comportement observable constant | aucun imposé |
| `revert` | L’annulation d’un commit antérieur | selon ce qu’il annule |
| `style` | Formatage sans effet sémantique | aucun imposé |
| `test` | Tests uniquement | aucun imposé |

Le type DOIT être en minuscules et appartenir à ce tableau. Un breaking change porté par
n’importe lequel de ces types produit un `MAJOR`.

#### Scope

Le scope PEUT être fourni. Lorsqu’il est présent, il DOIT être en minuscules et DOIT être
l’un des suivants :

| Scope | Couvre |
|---|---|
| `core` | `FirstClassErrors` — la bibliothèque d’exécution (`Error`, `Outcome`, `ErrorCode`, `ErrorContextKey`, …) |
| `analyzers` | `FirstClassErrors.Analyzers` — les analyzers Roslyn et leurs diagnostics `FCExxx` |
| `cli` | `FirstClassErrors.Cli` — l’outil en ligne de commande |
| `gendoc` | `FirstClassErrors.GenDoc` et son worker — le générateur de documentation |
| `testing` | `FirstClassErrors.Testing` — le package de support aux tests |

Cette liste vit ici, dans le dépôt, là où un outil peut la vérifier. Un scope NE DOIT PAS
être un nom de fichier ni un nom de classe : ceux-ci bougent ; la zone qu’ils habitent,
non. `fix(core):`, jamais `fix(ErrorCode.cs):`.

Le scope est optionnel ici parce que les deux packages publiés (`FirstClassErrors` et
`FirstClassErrors.Testing`) partagent une seule version pilotée par tag : le scope ne porte
aucun poids de versioning, seulement de la lisibilité. Ce qui n’appartient pas à un
composant ne prend **aucun** scope — l’infrastructure du dépôt (la solution,
`Directory.Build.props`, les workflows, `.gitignore`, `CLAUDE.md`), la documentation à
l’échelle du dépôt, les ADR, et les exemples de `FirstClassErrors.Usage` : `ci: …`,
`docs: …`.

Lorsqu’un changement atomique traverse plusieurs composants, le commit DOIT porter tous
leurs scopes, séparés par des virgules sans espace et classés par ordre alphabétique.
L’ordre est alphabétique pour qu’une paire donnée s’écrive toujours de la même façon, et se
retrouve avec un seul `git log --grep`.

```
fix(cli,gendoc): thread cancellation through the generate command
```

#### Description

* Elle DOIT être à l’impératif présent : `add`, pas `added` ni `adds`. La description
  complète une phrase — *If applied, this commit will …* — et seul l’impératif y convient :
  *…will add Outcome.Map*.
* Elle DOIT commencer par une lettre minuscule et NE DOIT PAS se terminer par un point. La
  ligne d’en-tête n’est pas une phrase ; c’est un titre.
* La ligne d’en-tête complète — type, scope optionnel, `!` optionnel, deux-points et
  description — DOIT tenir en 72 caractères. Au-delà, une fois le hash abrégé préfixé, elle
  déborde des 80 colonnes d’un terminal dans un `git log --oneline`.

#### Corps

Le corps PEUT être fourni, après une ligne vide. Il explique **pourquoi** le changement a
lieu — la contrainte, le symptôme, le compromis. Le *quoi* est déjà dans le diff ; le
répéter est du bruit.

Lorsque ce pourquoi n’est pas lisible depuis le diff, le corps DEVRAIT être fourni. S’en
abstenir se paie six mois plus tard, sur un commit que plus personne ne peut interpréter.

#### Footers

Les footers PEUVENT être fournis, après une ligne vide. Chaque footer DOIT prendre la forme
`Token: value`. Le token DOIT être des mots séparés par des traits d’union, **chaque mot
avec une majuscule** : `Co-Authored-By`, `Reviewed-By`, `Refs`, `Reverts`.
`BREAKING CHANGE` est la seule exception à cette forme.

> Cette casse « chaque mot avec une majuscule » est un écart délibéré par rapport à la
> convention habituelle à initiale unique. Elle existe pour que les footers écrits à la
> main restent cohérents avec les trailers que les commits automatisés de ce dépôt portent
> déjà — `Co-Authored-By`, `Claude-Session`. Une seule règle pour tous les footers vaut
> mieux que deux.

Lorsqu’une issue existe, son numéro DOIT figurer dans un footer `Refs:`, et NE DOIT PAS
apparaître dans la description — la description énonce le changement, pas l’endroit où il a
été demandé. Le footer porte la clé (`#142`), jamais l’URL : un message de commit ne se
réécrit pas, et le numéro survit là où une adresse ne survit pas. (Un footer d’outillage
comme `Claude-Session` est une URL par nature, et est exempté de ce dernier point.)

Un commit n’est **pas** l’endroit pour fermer une issue. La fermeture relève du workflow du
dépôt : mettez `Closes #142` dans la description de la pull request, et GitHub ferme l’issue
au merge. Le commit lui-même reste neutre, portant tout au plus un `Refs:`.

#### Breaking changes

Un breaking change DOIT être signalé deux fois : par un `!` placé juste avant le
deux-points, et par un footer `BREAKING CHANGE:` en majuscules.

```
feat(core)!: fail Outcome<T>.To with an Outcome instead of throwing

BREAKING CHANGE: Outcome<T>.To returns a failed Outcome<TTarget> where it
used to throw on a null conversion. Callers must handle the Outcome instead
of catching.
```

Le `!` est ce que l’on voit dans un `git log --oneline`. Le footer est ce que l’on lit au
moment de migrer. Les deux ont des lecteurs différents ; aucun ne remplace l’autre.

Ce qui est cassant se lit sur le **contrat publié**, pas sur le code interne. Dans ce dépôt,
ce contrat inclut explicitement les codes d’erreur, les identifiants de diagnostic
(`FCExxx`) et les types publics : renommer l’un d’eux est un breaking change (voir
`CLAUDE.md`). Renommer un type `internal` ne casse rien.

#### Reverts

Un commit de revert DOIT porter le type `revert`, reprendre la description du commit annulé,
et référencer son SHA dans un footer `Reverts:`.

```
revert(gendoc): emit an RFC 9457 problem type in examples

Reverts: b36765a
```

L’effet d’un revert sur la version se qualifie comme celui de n’importe quel commit : du
point de vue du consommateur, sur le contrat publié. Annuler un changement pas encore
publié neutralise son effet. Retirer une capacité déjà publiée est un breaking change, et le
commit DOIT alors porter le `!` et le footer `BREAKING CHANGE`.

### La doctrine

**L’issue est l’unité de la demande, le commit l’unité du changement.** Une issue produit
autant de commits qu’elle porte d’intentions : la fonctionnalité, le refactoring qui l’a
préparée, le correctif trouvé en relecture. Chacun porte son propre type, tous portent le
même `Refs:`.

**Le type est l’intention, pas le contenu du diff.** Une fonctionnalité arrive avec ses
tests, sa documentation d’API, son exemple : le commit reste un `feat`. `test` et `docs`
désignent un changement qui touche *uniquement* aux tests, *uniquement* à la documentation.
Scinder un `feat` en cinq commits parce qu’il s’étend sur cinq répertoires fabrique des
commits qui ne compilent pas seuls.

**`feat` ou `fix` se décide depuis l’extérieur du composant.** Le critère n’est pas la
taille du diff, c’est ce que le consommateur du package observe. Trois lignes qui restaurent
le comportement promis sont un `fix`. Une ligne qui ouvre une nouvelle capacité est un
`feat`.

**`refactor` et `perf` font une promesse : le comportement observable ne change pas.** Un
`refactor` qui corrige un bug au passage est un `fix` mal étiqueté — et la correction devient
invisible pour celui qui prépare la release.

**`chore` n’est pas la poubelle.** Tout ce qui ne rentre nulle part y atterrit, et le type
finit par ne plus rien signifier. Avant d’écrire `chore`, relisez le tableau.

**Ce qui est cassant se lit sur le contrat publié**, pas sur le code interne. Un type
`internal` renommé ne casse rien. Un type de retour modifié, un champ sérialisé renommé, un
code d’erreur, un identifiant de diagnostic — eux, oui.

**Le mauvais type se corrige avant le merge.** Un `git rebase -i` réécrit le message tant que
le commit n’a pas atteint une branche partagée. Après cela, le coût de la correction dépasse
le coût de l’erreur : laissez-le et passez à autre chose.

**Le numéro de version se décide en lisant l’historique.** Celui qui prépare la release y lit
l’incrément : un `fix` isolé donne un `PATCH`, un `feat` impose au moins un `MINOR`, un
breaking change impose un `MAJOR`. `FirstClassErrors` et `FirstClassErrors.Testing` partagent
une seule version pilotée par tag, donc l’incrément le plus élevé de la release s’applique aux
deux.

**Qui décide de quoi.** L’auteur du commit choisit le type et le scope. Le relecteur de la
pull request refuse un message non conforme comme il refuse du code non conforme. Les
mainteneurs sont responsables de la liste des scopes et de la liste des types.

### Exemples

**Une fonctionnalité, avec scope et issue.**

```
feat(analyzers): add FCE016 for an undocumented error code

Refs: #142
```

**Un correctif dont le pourquoi n’est pas lisible depuis le diff.**

```
fix(gendoc): render error examples with the invariant culture

Sample amounts were formatted with the host's culture, so the Verify
baselines matched on an invariant machine and failed on a comma-decimal one.
CI and developers disagreed on the very same commit.

Refs: #128
```

**Un refactoring, ne promettant rien d’autre qu’un comportement identique.** Ni corps ni
footer : le diff dit tout.

```
refactor(core): extract transience computation into TransienceCalculator
```

**Un breaking change, avec l’instruction de migration.**

```
feat(core)!: fail Outcome<T>.To with an Outcome instead of throwing

BREAKING CHANGE: Outcome<T>.To returns a failed Outcome<TTarget> where it
used to throw on a null conversion. Callers must handle the Outcome instead
of catching.

Refs: #150
```

### Anti-patterns

Classés comme le sont les règles : type, scope, description, corps, breaking, issue.

| Message | Ce qui ne va pas |
|---|---|
| `chore: handle a null error code` | Un `fix` déguisé. Celui qui prépare la release ne le verra pas, et la version ne bougera pas alors qu’elle le devrait. |
| `feat: refactor the extraction reader` | Le type contredit la description. L’un des deux ment. |
| `fix(core): correct transience and add a CI cache` | Deux changements, deux commits. Aucune version ne peut décrire celui-ci. |
| `fix(ErrorCode.cs): formatting` | Le scope nomme un fichier. Il désigne une zone : `core`. |
| `feat(gendoc, cli): carry the source description` | Un espace après la virgule, et l’ordre n’est pas alphabétique. Deux orthographes pour une même paire — écrivez `feat(cli,gendoc):`. |
| `fix(core): Fixed the null dereference.` | Majuscule, passé, point final. Trois règles de forme enfreintes, un seul mot utile. |
| `feat(core): add support` | Un support pour quoi ? La description doit se suffire à elle-même dans un `git log`. |
| `fix(core): change line 42 of Error` | La description nomme une ligne. Elle devrait nommer un changement. |
| `fix(gendoc): render with the invariant culture` — corps : `Replaced DateTime.Now with CultureInfo.InvariantCulture` | Le corps répète le diff. Il devrait dire pourquoi la culture variait d’un hôte à l’autre. |
| `feat(core)!: fail Outcome<T>.To with an Outcome` — sans footer | Le `!` avertit ; il ne fait migrer personne. |
| `feat(core): add Outcome.Map (#142)` | L’issue mange les 72 caractères de la description. Sa place est un footer. |
| `refs: #142` | Token en minuscules. Le token du footer est `Refs`. |

### Adoption

Ce guide est la règle pour les commits de ce dépôt. S’en écarter requiert une justification —
un ADR sous `maintainers/adr/`, ou une mise à jour de ce guide.

Il s’applique à partir de son adoption, à chaque commit créé après. L’historique antérieur
n’est pas réécrit.

L’application repose aujourd’hui sur la relecture de pull request, qui refuse un message non
conforme et laisse l’auteur le corriger avant le merge. Le dépôt verrouillera la règle à son
tour : un hook `commit-msg` qui refuse le message à l’écriture, doublé d’une vérification CI,
puisqu’un hook local peut être contourné. Les commits de merge, générés par GitHub, en sont
exemptés.

### Crédits

Cette section adapte la spécification [Conventional Commits 1.0.0](https://www.conventionalcommits.org/en/v1.0.0/),
publiée sous [CC BY 3.0](https://creativecommons.org/licenses/by/3.0/).

## Titres de pull request

La convention ci-dessus régit chaque commit. Une pull request a besoin d’une ligne à elle, et
ce n’est pas le même objet : le commit est l’unité du changement, la **pull request l’unité de
la demande** — la relation que la doctrine trace déjà entre le commit et l’issue. Une pull
request PEUT donc rassembler plusieurs commits, de plusieurs types.

Son titre se lit à trois endroits : la liste des pull requests ouvertes, le commit
`Merge pull request #NN` que GitHub écrit lorsque la branche est intégrée (ce dépôt fusionne
avec un commit de merge), et le brouillon des notes de version. Il mérite le même soin qu’un
en-tête de commit. Contrairement à un commit, il n’est **pas** linté ; il tient sur la
relecture, comme le code.

### La règle

* Le titre DOIT être en **anglais**, comme tout le reste consigné ici.
* Il DOIT nommer la pull request **entière**, pas l’un de ses commits. Les types par commit
  vivent dans les commits, là où le hook et la CI les vérifient ; le titre dit ce que la
  branche livre.
* Sa forme découle du nombre d’intentions que porte la pull request :
  * **Une seule intention** — la branche fait une seule sorte de chose. Le titre DOIT
    refléter l’en-tête de commit auquel il se réduit : `<type>[(<scope>)][!]: <description>`,
    sous les règles mêmes de la section ci-dessus — impératif présent, minuscule après le
    deux-points, pas de point final. Le titre d’une pull request à un seul commit est
    l’en-tête de ce commit, mot pour mot.
  * **Plusieurs intentions** — la branche porte une fonctionnalité et le refactoring qui l’a
    préparée, ou un correctif et le test qui le fige. Le titre NE DOIT PAS emprunter un unique
    préfixe `type:` : il nommerait un commit et cacherait les autres. Il énonce le sujet en
    mots simples, comme un titre — une majuscule initiale, pas de point final. Un préfixe
    thématique (`Release supply chain: …`) est bienvenu ; un type Conventional Commits ne
    l’est pas, sauf s’il est honnêtement le seul.
* Gardez le titre dans les **72 caractères** que vise un en-tête de commit, pour que la liste
  des pull requests l’affiche en entier.
* La référence de l’issue vit dans la **description**, jamais dans le titre : `Closes #NN`
  lorsque la pull request ferme l’issue, pour que GitHub la ferme au merge ; `Refs: #NN`
  sinon. Le titre énonce le changement, pas l’endroit où il a été demandé. Un breaking change
  se signale de la même façon que sur un commit — le `!` et la note `BREAKING CHANGE:` sont
  portés par le commit, et la case « Breaking change » du template le répète — pas le titre.

### Exemples

| Titre | Pourquoi il convient |
|---|---|
| `ci: add dependency review on pull requests` | Une seule intention. Le titre est l’en-tête de commit. |
| `feat(analyzers): add FCE016 for an undocumented error code` | Une seule intention, avec scope. L’issue qu’il ferme vit dans la description, pas ici. |
| `Adopt and enforce a Conventional Commits convention` | Le guide, le hook et le garde-fou CI — plusieurs commits de plusieurs types. Un titre simple les nomme tous. |
| `Release supply chain: build provenance + embedded SBOM` | Plusieurs intentions sous un même thème. Un préfixe thématique le porte ; aucun `type:` unique ne serait honnête. |

### Anti-patterns

| Titre | Ce qui ne va pas |
|---|---|
| `feat: various improvements` | Un type sur un fourre-tout. Soit c’est une seule intention — nommez-la — soit il y en a plusieurs, et `feat:` les cache. |
| `fix(core): Fixed the null dereference.` | La forme à intention unique, portant les défauts propres à l’en-tête de commit : majuscule, passé, point final. |
| `Add Outcome.Map (#142)` | Le numéro d’issue a sa place dans le `Closes`/`Refs` de la description, là où GitHub le lit — pas à manger le titre. |
| `Corrige le rendu des exemples` | Pas en anglais. |
