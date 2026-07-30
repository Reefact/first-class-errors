# Workflow `sonar-profile`

🌍 🇫🇷 Français (ce fichier) · 🇬🇧 [English](sonar-profile.en.md)

> Documentation mainteneur — fait partie de la [référence des workflows](README.fr.md).
> Ne fait pas partie de la documentation utilisateur sous `doc/`.

**Fichier du workflow :** [`.github/workflows/sonar-profile.yml`](../../../../.github/workflows/sonar-profile.yml)
**Script :** [`tools/sonar-profile/sync-profile.sh`](../../../../tools/sonar-profile/sync-profile.sh)
**Fichier généré :** [`build/sonar-profile.globalconfig`](../../../../build/sonar-profile.globalconfig)

## À quoi il sert

Il surveille le **profil qualité C#** de SonarCloud et échoue quand le dépôt s'en est écarté.

Les règles contre lesquelles le rapport SonarQube Cloud est noté vivent sur le serveur. Rien
dans ce dépôt ne savait lesquelles c'étaient, et le remède évident ne suffit pas : le paquet
NuGet `SonarAnalyzer.CSharp` embarque **son propre jeu par défaut, plus étroit**. Mesuré sur ce
code, ce défaut laisse `S3776` (complexité cognitive) et `S1192` (littéraux dupliqués)
**éteintes** alors que le profil les active toutes deux — les deux règles qui représentaient
l'essentiel des constats C# du rapport. Ajouter le paquet seul aurait donc mis le build et le
rapport en désaccord silencieux.

Le profil est donc lu et consigné. Cette liste est
`build/sonar-profile.globalconfig`, et ce workflow est ce qui s'aperçoit qu'elle pourrit.

## Les deux fichiers, et lequel décide

| Fichier | Propriétaire | Dit |
| --- | --- | --- |
| `build/sonar-profile.globalconfig` | **généré** | quelles règles le profil active — toutes en `warning`, donc le défaut est **appliquer** |
| `.editorconfig` | **écrit à la main** | les **exceptions** : `suggestion` pour une règle dont les violations ne sont pas vidées, `none` pour une règle refusée, avec sa raison |

**Le défaut est d'appliquer.** `suggestion` a été mesuré comme défaut, puis rejeté : à cette
sévérité un diagnostic Sonar n'affiche **rien** dans `dotnet build`, ni en verbosité `quiet` ni
en `normal` — il atteint un IDE et le journal SARIF, personne d'autre. Une liste générée en
`suggestion` aurait été invisible au lecteur pour qui elle existe. En `warning` le diagnostic
apparaît en console et le ratchet CI de `Directory.Build.props` en fait une erreur ; les deux ont
été vérifiés de bout en bout en introduisant une violation d'une règle appliquée.

**348 des 377 règles sont appliquées** — elles avaient zéro violation dans l'arbre, les promouvoir
ne coûtait donc rien — et **29 sont garées** dans `.editorconfig` en `suggestion`, représentant
ensemble 104 sites en suspens.

Cette liste garée **est** l'arriéré, et une règle en sort par l'une de deux portes :

* **Ses sites sont vidés.** Supprimez sa ligne, et le fichier généré l'applique dès la
  compilation suivante. Rien d'autre à écrire.
* **Les rares sites qui restent sont délibérés.** Chacun porte un `[SuppressMessage]` avec sa
  raison au site, et la ligne part quand même. C'est la porte à préférer dès qu'une poignée de
  violations se défend et que le reste de l'arbre est propre, car les deux états diffèrent sur ce
  que la règle fait *demain* : garée, elle est muette partout, y compris sur du code pas encore
  écrit ; supprimée en cinq endroits, elle est appliquée partout ailleurs.

Une règle que le code entend refuser *tout court* est une troisième chose et n'a pas sa place dans
l'arriéré : elle va avec les refus, en `none`, avec sa raison (ADR-0060). `suggestion` veut dire
« pas encore », jamais « non ».

`.editorconfig` prime sur un AnalyzerConfig global, vérifié dans les deux sens. **L'appartenance
est générée ; chaque exception est écrite.** Qui demande « pourquoi cette règle ne bloque-t-elle
pas ? » trouve la réponse, et un compte, à côté de la règle.

## Quand il s'exécute

- **Chaque semaine**, le lundi à 05h47 UTC. Pas chaque nuit : le profil est le « Sonar way »
  intégré de SonarSource — mesuré, `isBuiltIn` est vrai et `userUpdatedAt` est nul, donc personne
  ici ne l'a jamais édité et personne ne peut. La dérive arrive avec une livraison de l'analyseur,
  quelques fois par an ; un nocturne interrogerait la cadence d'un éditeur.
- À la demande via **`workflow_dispatch`**.

Il ne s'exécute délibérément **pas** sur les *pull requests*. La dérive du profil n'est pas la
faute de la PR qui se trouve ouverte, et faire échouer une PR innocente apprendrait à ignorer le
contrôle.

## Comment il s'exécute

Un job, `Quality profile drift`, sous Linux : checkout, puis
`tools/sonar-profile/sync-profile.sh --check`, qui régénère la liste depuis l'API et la compare
au fichier committé. En cas d'échec, le diff est dans le log et un résumé d'étape dit quoi en
faire.

Le script fait deux appels d'API : le profil qualité lié au projet, puis ses règles actives
(paginées). Les deux en lecture seule.

## Permissions & sécurité

`contents: read`, déclaré **sur le job** plutôt qu'au niveau du workflow, pour qu'un job ajouté
plus tard n'hérite de rien qu'il n'ait demandé (Sonar `githubactions:S8264`).

`SONAR_TOKEN` est transmis depuis le même secret que [`sonar`](sonar.fr.md), mais n'est **pas
requis** : le projet est public et l'API répond sans authentification. Le passer quand même est
ce qui maintiendra ce workflow le jour où le projet cessera d'être public, au lieu d'un 403.

## À manier avec précaution

- **Il signale, il ne répare pas.** L'alternative — un job planifié détenant l'accès en écriture
  au fichier même qui gouverne quelles règles bloquent un merge — est la forme qu'un audit de
  sécurité des workflows a déjà signalée deux fois sur ce dépôt. Le promouvoir en ouvreur de
  *pull request* est un petit changement si l'arbitrage est jugé valable un jour, mais c'est une
  décision, pas une commodité.
- **Le script échoue fermé, de trois façons.** Une réponse vide ou courte avorte *sans toucher au
  fichier* : moins de 100 règles est traité comme « ce n'est pas un vrai profil », donc un hoquet
  d'API ne peut pas réécrire le jeu de règles. Une clé de projet en désaccord avec `sonar.yml`
  avorte. Et un décompte en désaccord avec l'`activeRuleCount` du profil est signalé haut et
  fort — sur ce projet les deux diffèrent de trois, et l'endpoint des règles est cohérent avec
  lui-même sur tous les filtres alors que le décompte ne l'est pas : **trois règles ne peuvent pas
  être lues et ne sont donc pas configurées**. C'est imprimé à chaque exécution, pas avalé.
- **Une édition manuelle du fichier généré est attrapée par le même mécanisme**, parce qu'une
  édition manuelle *est* une dérive. Il n'y a pas de garde séparé et il n'en faut pas.
- **La clé de projet et l'organisation doivent correspondre à `sonar.yml`, et le script le
  vérifie.** Elles sont dupliquées dans le script et surchargeables par variable
  d'environnement ; quand les valeurs par défaut sont utilisées, le script les compare aux
  arguments du scanner dans `sonar.yml` et avorte en cas d'écart. Sans ce contrôle, un renommage
  laisserait ce job valider un projet que personne ne regarde, en vert.
- **Une règle sans identifiant de diagnostic Roslyn est signalée, pas écartée.** Les clés Sonar
  d'une autre forme que `S<chiffres>` (un patron de règle, un contrôle non-Roslyn) sont écrites
  sur la sortie d'erreur avec leur nombre.
- **Régénérer peut faire rougir la CI, et c'est le propos.** Le défaut étant d'appliquer, une
  règle que le profil ajoute arrive *bloquante*. Qui régénère doit alors la nettoyer ou la garer
  dans `.editorconfig` avec son compte, délibérément, avant que ça merge. Le job hebdomadaire est
  l'avertissement qui fait de ça une décision plutôt qu'une surprise.
- **Garer est un état temporaire et rien ne l'impose.** Une règle peut rester en `suggestion`
  indéfiniment, et si la liste garée ne fait que croître, l'agencement a acheté une liste et
  aucune application. Les comptes dans `.editorconfig` existent pour que cette tendance soit
  visible dans un diff.
- **La version de l'analyseur est épinglée, et la dépingler est un chantier.** « Appliquée »
  signifie « zéro violation mesurée contre cette version de l'analyseur ». Une version plus
  récente peut faire sortir une règle jusque-là muette sur du code non touché : une montée de
  version est donc un lot de travail, pas de la maintenance de routine.

## L'arriéré actuel

Les 29 règles garées dans `.editorconfig` représentent **104 sites**. Toute autre règle que le
profil active est déjà appliquée : cette liste est donc l'intégralité de ce que Sonar demande et
que ce code ne fait pas encore. À promouvoir famille par famille, en supprimant chaque ligne à
mesure que ses sites sont vidés.

La concentration, par ordre décroissant : `S3776` (19, complexité cognitive), `S1244` (15,
égalité de flottants — toutes dans des projets de test, où l'égalité exacte est déjà justifiée),
`S3878` (14, tableaux pour `params`), `S3218` (8, membres internes masquant l'externe), `S107` (6,
trop de paramètres — une décision que le dépôt a déjà consignée comme délibérée).

Notez ce que ces chiffres ne sont **pas** : SonarCloud remonte beaucoup moins d'issues que
l'arriéré n'a de sites, parce qu'il classe les treize projets de tests comme du code de test et
n'y lève pas la plupart des règles, là où le jeu de règles du build s'applique partout. Un
SonarCloud vert est donc un jalon, pas la ligne d'arrivée — cette liste, oui.

## Voir aussi

- [`sonar`](sonar.fr.md) — l'analyse avec laquelle on se réconcilie. Elle rapporte ; elle n'a
  jamais appliqué.
- [`ci`](ci.fr.md) — là où le ratchet de warnings transforme une règle promue en règle bloquante.
- [`lint`](lint.fr.md) — le même geste pour les fichiers que le compilateur C# ne voit jamais.
