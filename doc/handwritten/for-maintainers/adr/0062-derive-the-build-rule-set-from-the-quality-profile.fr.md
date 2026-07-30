# ADR-0062 | Dériver le jeu de règles Sonar du build depuis le profil qualité

🌍 🇫🇷 Français (ce fichier) · 🇬🇧 [English](0062-derive-the-build-rule-set-from-the-quality-profile.md)

**Statut :** Accepté
**Proposé :** 2026-07-29
**Accepté :** 2026-07-30
**Décideurs :** Reefact

## Contexte

SonarQube Cloud note ce projet contre un **profil qualité côté serveur** — « Sonar way », qui
porte 375 règles C# actives. Rien dans le dépôt ne savait lesquelles c'étaient. Elles ne
tournaient que dans la compilation instrumentée par le scanner du workflow `sonar`, si bien qu'un
contributeur — humain ou agent — les rencontrait *après* le merge, dans un rapport, et jamais en
écrivant le code.

Le workflow `sonar` ne comble pas ce trou et n'a jamais été construit pour.
`dotnet-sonarscanner end` téléverse l'analyse et rend la main ; il n'attend pas le Quality Gate et
ne lit pas son verdict. Le job est vert quand le *téléversement* réussit. Aucune *check* GitHub ne
porte non plus le verdict : sur les 28 *checks* d'une *pull request* récente, la seule Sonar était
le job d'analyse du dépôt lui-même. Le gate n'est donc appliqué par rien, alors que le job *est*
une *check* requise qui appelle le service — une panne bloque donc le merge, et un gate rouge ne le
bloque pas.

Le remède évident ne suffit pas. Ajouter `SonarAnalyzer.CSharp` en simple `PackageReference` ne
reproduit **pas** le profil : mesuré sur ce code, le jeu par défaut du paquet déclenche 29 règles
sur 107 sites, et laisse `S3776` (complexité cognitive) et `S1192` (littéraux dupliqués)
**éteintes** alors que le profil active les deux — les deux règles qui représentaient l'essentiel
des constats C# du rapport. Le paquet est un autre jeu de règles, plus étroit précisément là où
cela comptait.

D'autres faits ont été mesurés plutôt que supposés :

* `.editorconfig` **peut** activer une règle que le paquet livre éteinte ; `S3776` se déclenche
  alors sur 13 fichiers.
* Activer tout le profil en `warning` produit **135 sites de warning sur 33 règles** — un nombre
  fini et connu.
* Les 375 règles en `suggestion` produisent **zéro** warning. Mais un diagnostic Sonar à cette
  sévérité n'affiche **rien** dans `dotnet build`, ni en `quiet` ni en `normal` : il atteint un IDE
  et le journal SARIF (en `level: note`), aucune console. `suggestion` n'est donc pas « visible et
  inerte » — il est inerte et invisible.
* Sur les 375 règles, **33 seulement se déclenchent**. Les **342 autres ont zéro violation** dans
  l'arbre : rien ne s'oppose à ce qu'elles soient appliquées.
* `.editorconfig` prime sur un AnalyzerConfig global **dans les deux sens** : une promotion en
  `warning` et un refus en `none` gagnent tous deux.

L'API SonarCloud répond **sans authentification** pour ce projet public ; le profil et ses règles
actives tiennent en deux appels paginés. Le profil est le « Sonar way » **intégré** de
SonarSource : `isBuiltIn` est vrai et `userUpdatedAt` est nul, donc personne dans cette
organisation ne l'a jamais édité, et la dérive ne peut arriver qu'avec une livraison de
l'analyseur. L'objet profil annonce par ailleurs 378 règles actives là où l'endpoint des règles en
énumère 375 ; cet endpoint est cohérent avec lui-même sur tous les filtres et ses totaux par type
somment exactement à 375, si bien que trois règles ne peuvent pas être lues.

SonarLint a été envisagé, et mesuré aussi. C'est une extension d'IDE : elle ne tourne ni dans
`dotnet build`, ni en CI, ni pour un agent qui édite le dépôt en ligne de commande. Son mode
connecté émet un `SonarLint.xml`, mais committer ce fichier en `AdditionalFiles` n'a **pas** activé
`S3776` — le fichier porte les paramètres des règles, pas leur activation.

Le dépôt a déjà consigné ce qu'il advient d'une règle qui vit là où les lecteurs du code ne la
voient pas. L'ADR-0055 et l'ADR-0056 existent parce que la règle du type explicite, tenue seulement
dans le DotSettings de ReSharper, a dérivé à 203 violations. L'ADR-0060 consigne la règle
complémentaire : un refus s'écrit à côté de ce qu'il refuse.

## Décision

Le jeu de règles C# du build est dérivé du profil qualité SonarCloud et **appliqué par défaut** —
appartenance générée dans un AnalyzerConfig global committé en `warning`, chaque exception écrite à
la main dans `.editorconfig` avec sa raison ou son compte de sites restants, et un job hebdomadaire
qui échoue quand les deux ont divergé.

## Justification

* **Le rapport ne peut pas être le point d'application, et le durcir aggraverait les choses.**
  Personne ne lit le Quality Gate, il n'applique donc rien ; pendant ce temps le job qui le
  téléverse est requis et appelle un service tiers, si bien qu'il bloque le merge exactement quand
  il n'a rien à dire. L'application appartient à nos propres *runners*.
* **Le profil doit être lu, parce que le paquet n'est pas le profil.** Un jeu de règles qui omet
  les deux règles dont le rapport se plaignait le plus n'est pas un alignement, c'est un autre
  avis. Lire le profil est le seul agencement où le build et le rapport parlent des mêmes règles.
* **Générer l'appartenance et écrire les exceptions à la main met chaque moitié à sa place.** Ce
  que le serveur demande est un fait, change sans que personne décide, et pourrirait s'il était
  tenu à la main — exactement la dérive qui a motivé l'ADR-0055 et l'ADR-0056. Qu'une règle ne
  bloque *pas* est une décision, exige une raison, et doit être lisible à côté de la règle : c'est
  la règle de l'ADR-0060.
* **Appliquer par défaut est le seul réglage qui livre quelque chose aujourd'hui.** Une liste
  générée en `suggestion` n'aurait rien affiché dans un build : une liste que personne ne lit,
  c'est-à-dire l'échec qu'on corrige, reproduit avec plus de fichiers. Appliquer par défaut
  transforme les 342 règles déjà propres en garde-fou effectif à coût nul, et réduit la question à
  une liste nommée d'exceptions.
* **Les exceptions portent des comptes, donc l'arriéré est un diff et non une impression.** 33
  règles sont garées en `suggestion` avec le nombre de sites que chacune conserve. Vider une règle
  consiste à supprimer sa ligne ; le fichier généré l'applique ensuite sans rien à écrire. Une
  liste qui se réduit par suppression est une liste dont le progrès se voit en relecture.
* **Signaler plutôt que réparer tient un job planifié à l'écart du fichier qui gouverne les
  merges.** Un job planifié détenant l'écriture sur le fichier qui décide quelles règles bloquent
  un merge est la forme qu'un audit de sécurité des workflows a signalée deux fois sur ce dépôt.
* **Un IDE ne peut pas être le mécanisme.** SonarLint montrerait fidèlement le profil à qui a Rider
  ouvert, et à personne d'autre — l'échec précis que l'ADR-0056 a consigné. Son artefact de
  configuration ne comble pas le trou non plus ; cela a été mesuré, pas supposé.

## Alternatives envisagées

### Adopter le jeu de règles par défaut du paquet

Le changement le plus petit possible : ajouter le paquet, accepter ce qu'il active.

Rejetée parce que ce n'est pas le profil. Il omet `S3776` et `S1192`, et ses 107 violations
existantes feraient rougir la CI immédiatement — payer le prix entier de l'application pour un
alignement qu'elle ne fournit pas.

### Rendre le Quality Gate bloquant (`sonar.qualitygate.wait=true`)

Un argument de scanner, et la *check* déjà requise voudrait enfin dire quelque chose.

Rejetée telle que posée, parce qu'elle fond deux décisions distinctes : `sonar` doit-elle rester
une *check* **requise**, et le verdict du gate doit-il être **lu**. En l'état la *check* est requise
et appelle déjà SonarCloud : une panne bloque donc déjà le merge alors qu'un gate rouge ne le bloque
pas, et ajouter l'attente étend cette dépendance au lieu de la supprimer. La combinaison qui mérite
examen — **non requise, et lisant le gate** — est informative sans jamais bloquer sur un tiers.
Laissée ouverte en action de suivi plutôt que tranchée ici.

### SonarLint, en mode connecté

Il se lie au serveur et montre exactement les règles du profil.

Rejetée parce que c'est une extension d'IDE. Elle ne tourne ni dans `dotnet build`, ni en CI, ni
pour un agent en ligne de commande : elle ne peut rien appliquer — et une règle appliquée seulement
tant qu'un humain a un IDE ouvert est l'échec que l'ADR-0056 a consigné.

### Committer le `SonarLint.xml` du mode connecté et le donner à l'analyseur

L'hybride séduisant : l'artefact de profil du serveur, lu par l'analyseur au build.

Rejetée parce que ça ne marche pas. Câblé en `AdditionalFiles`, il n'a pas activé `S3776` ; le
fichier porte les *paramètres* des règles, alors que l'activation est une affaire Roslyn tranchée
par la sévérité par défaut et l'AnalyzerConfig.

### Générer tout le profil en `suggestion` et promouvoir à la main

C'est ce qui a été construit d'abord : appartenance générée en `suggestion`, rien de bloquant,
chaque règle promue dans `.editorconfig` à mesure que ses sites étaient vidés. Son attrait est de
ne jamais pouvoir faire rougir un build en régénérant.

Rejetée après avoir mesuré ce que `suggestion` fait réellement : un diagnostic Sonar à cette
sévérité n'affiche rien dans `dotnet build`, à aucune verbosité. La liste aurait été invisible au
contributeur comme à tout agent, n'appliquant rien le jour de son arrivée, avec toute sa valeur due
à un travail de promotion que rien n'oblige personne à faire. Elle laissait aussi 342 règles déjà
propres non appliquées sans raison — la moitié gratuite du travail, déclinée par accident.

### Activer tout le profil en `warning`, y compris les règles ayant des violations

L'état final en une étape, sans liste garée susceptible d'être ignorée.

Rejetée sur l'enchaînement. Elle pose 135 violations bloquantes : elle ne peut pas merger avant que
toutes soient résolues — le mécanisme partirait en dernier au lieu d'en premier, et chaque décision
de promotion serait prise sous la pression d'un build rouge.

### Faire ouvrir une pull request par le job planifié, ou committer le fichier régénéré

Plus proche de « ça se met à jour tout seul », et supprime la seule commande manuelle.

Rejetée pour l'instant. Elle donne à une planification l'accès en écriture au fichier qui gouverne
quelles règles bloquent un merge, et un push fait avec le token par défaut ne redéclenche pas la
CI : la *pull request* obtenue arriverait non vérifiée. Cela compte moins qu'il n'y paraît : le
profil étant celui, intégré, de SonarSource, la dérive arrive quelques fois par an, et une commande
quelques fois par an n'est pas le coût qui justifie le risque. Listée en action de suivi plutôt que
refusée sur le principe.

### Tenir la liste de règles à la main

Pas de script, pas de job planifié, pas de fichier généré — seulement les règles que quelqu'un a
choisies, avec leurs raisons.

Rejetée parce que c'est exactement l'agencement que ce dépôt a déjà vu échouer. Une liste que rien
ne régénère s'écarte du serveur en silence, et l'ADR-0055 comme l'ADR-0056 ont été écrites après
que la règle du type explicite a dérivé à 203 violations dans les mêmes conditions.

## Conséquences

### Positives

* **342 règles Sonar sont appliquées dès l'arrivée de ceci**, à coût nul, parce qu'elles avaient
  zéro violation. Une nouvelle violation de l'une d'elles apparaît en warning en local et en erreur
  en CI — vérifié de bout en bout en en introduisant une.
* Le build et le rapport parlent des mêmes règles, et la liste qui le dit est dans le dépôt au lieu
  d'être déduite.
* L'arriéré est une liste nommée de 33 règles et 135 sites, portant des comptes, qui se réduit par
  suppression — son progrès, ou son absence, se voit dans un diff.
* Toute règle qui ne bloque *pas* le dit sur une ligne écrite à la main, avec une raison ou un
  nombre.
* Aucun nouveau secret et aucune action tierce : l'API est publique et le script utilise `curl` et
  `jq`.

### Négatives

* 135 violations restent en suspens sur 33 règles garées, et rien n'oblige à les traiter.
* Régénérer après un changement de profil peut faire rougir la CI, ce que le design `suggestion`
  rejeté ne pouvait jamais faire. C'est le comportement voulu, mais cela fait de la régénération
  une décision et non une corvée.
* « Appliquée » signifie « zéro violation mesurée contre la version épinglée de l'analyseur ». Une
  montée de version peut faire sortir une règle muette sur du code non touché : la monter est donc
  un lot de travail.
* Le jeu de règles vit dans deux fichiers, et un lecteur doit savoir que le fichier généré énonce
  l'appartenance tandis que celui écrit à la main énonce les exceptions.
* Trois règles que le profil compte ne peuvent pas être lues depuis l'endpoint des règles, et ne
  sont donc pas configurées. Le script le dit à chaque exécution ; rien ne permet aujourd'hui de le
  résoudre.
* Résoudre une dérive demande qu'un humain lance une commande ; le job planifié reste rouge
  jusque-là.

### Risques

* Un job planifié qui ne fait qu'échouer est un job qu'on peut couper. S'il reste rouge une semaine
  sans que personne agisse, il devient du bruit et le mécanisme meurt sans que personne ait décidé
  de le tuer.
* La liste garée peut croître au lieu de se réduire. Rien ne force une règle à sortir de
  `suggestion`, et une liste qui ne gagnerait que des lignes signifierait que l'agencement a acheté
  un arriéré et l'a appelé progrès. Les comptes sont là pour que ce soit lisible.
* Supprimer une ligne garée sans avoir vidé les sites de cette règle fait rougir des *pull
  requests* sans rapport. Seule la relecture l'empêche.
* Une panne SonarCloud prolongée rend le job planifié durablement rouge. Il échoue fermé — il
  n'écrit jamais sur une mauvaise réponse — mais une *check* rouge que personne ne peut réparer
  invite à la couper.
* Lire « le build applique désormais tout ce que Sonar demande » serait exagéré de 33 règles et 135
  sites. Il en applique 342 sur 375, et l'écart est nommé.

## Actions de suivi

* Vider les 33 règles garées famille par famille, chacune dans sa propre *pull request*, en
  supprimant sa ligne de `.editorconfig` quand ses sites tombent à zéro — ou en la passant à `none`
  avec une raison si le code la refuse.
* Trancher séparément les deux questions du Quality Gate : `sonar` doit-elle rester une *check*
  **requise**, et le scanner doit-il **lire** le gate. La combinaison « non requise, et lisant le
  gate » n'a jamais été évaluée pour elle-même.
* Reconsidérer si le job planifié doit ouvrir une *pull request* plutôt qu'échouer, une fois qu'on
  saura à quelle fréquence le profil bouge réellement.

## Références

* ADR-0055 — redire les règles de style exprimables par le compilateur là où le build les voit.
* ADR-0056 — énoncer les règles là où un agent peut s'en saisir ; pourquoi une règle
  DotSettings-seule a dérivé.
* ADR-0058 — décliner une règle que le plancher de support rend insatisfiable.
* ADR-0060 — un refus est consigné à côté de ce qu'il refuse.
* [Référence du workflow `sonar-profile`](../workflows/sonar-profile.fr.md) — comment le script, le
  fichier généré et le job planifié sont câblés.
* [Référence du workflow `sonar`](../workflows/sonar.fr.md) — l'analyse avec laquelle on se
  réconcilie.
