# ADR-0043 | Conditionner les pull requests au score de mutation de ce qu'elles modifient

🌍 🇬🇧 [English](0043-gate-pull-requests-on-the-mutation-score-of-the-diff.md) · 🇫🇷 Français (ce fichier)

**Statut :** Accepté
**Date :** 2026-07-27
**Décideurs :** Reefact

## Contexte

FirstClassErrors livre cinq bibliothèques — `FirstClassErrors`,
`FirstClassErrors.Testing`, `FirstClassErrors.RequestBinder`, `JustDummies` et
`JustDummies.Xunit` — dont le produit *est* la sémantique : quelle erreur une
fabrique retourne, quelle branche un `Outcome` emprunte, quelle valeur une
contrainte admet. Un défaut à cet endroit n'est pas un plantage que le
consommateur voit venir ; c'est une réponse fausse délivrée avec assurance.

Le dépôt impose déjà deux signaux de qualité automatiques sur chaque pull
request : toute la suite de tests sur deux plateformes (`ci`), et la quality gate
SonarQube Cloud, couverture de lignes et de branches comprise (`sonar`). La
couverture enregistre qu'une ligne a été **exécutée** par un test. Elle ne peut
pas enregistrer si un test aurait **remarqué** que cette ligne était fausse : une
suite qui appelle une méthode sans rien affirmer de son résultat obtient la même
couverture qu'une suite qui épingle tous les cas.

Les tests de mutation mesurent exactement cette différence. L'outil réécrit la
bibliothèque un petit changement à la fois, rejoue la suite contre chaque
réécriture, et signale celles que la suite laisse passer. Sur .NET, cet outil est
Stryker.NET ; il n'existe pas d'alternative maintenue.

Quatre faits sur son exécution dans ce dépôt ont été établis par la mesure avant
que cette décision soit prise :

* **Le runner VSTest par défaut de Stryker ne fonctionne pas sur ce banc de
  tests.** Tous les projets de tests sont ici en xUnit v3, et un projet de tests
  xUnit v3 est un exécutable que l'adaptateur VSTest lance dans un processus fils
  — hors de portée des crochets in-process dont Stryker se sert à la fois pour
  capturer la couverture et pour *activer* un mutant. Le run va au bout, annonce
  un nombre de tests plausible, et score **0 %** : tous les mutants sont
  rapportés survivants, y compris des mutants que la suite tue de façon
  démontrable quand la même modification est appliquée à la main. Un barrage bâti
  sur ce runner serait rouge en permanence et ne prouverait rien.
* **Le runner Microsoft Testing Platform de Stryker fonctionne**, parce qu'il
  lance lui-même l'exécutable de tests. Il est marqué *preview* par ses auteurs.
* **Sa sélection de mutants par la couverture n'est pas encore fiable.**
  Sélection activée, des mutants que la suite tue effectivement sont classés non
  couverts et comptés contre le score ; sélection désactivée — chaque mutant
  confronté à toute la suite — la même population score nettement plus haut, et
  ce résultat correspond à ce que donne l'application des mutations à la main. La
  désactiver ne coûte pratiquement rien ici, car les suites sont rapides.
* **Le coût est d'environ une exécution de la suite par mutant.** Cela représente
  environ une seconde par mutant pour ces bibliothèques : le balayage complet
  d'une bibliothèque se compte en minutes, celui des cinq est trop long pour être
  attendu à chaque push. Ne sélectionner que les mutants touchés par un
  changement ramène le cas courant au coût fixe de l'analyse et du build.

Le mode diff de Stryker sélectionne les mutants par **fichier** modifié, pas par
ligne modifiée ; il n'y a pas de granularité à la ligne. Une modification d'une
ligne dans un gros fichier met donc tous les mutants de ce fichier sur le
barrage.

Tout survivant n'est pas un défaut : certains mutants sont *équivalents*, ils
changent le code sans changer le comportement observable, et aucun test ne peut
les tuer. Un seuil à 100 % est donc inatteignable par principe.

Deux des cinq bibliothèques — `JustDummies` et son adaptateur xUnit v3 — sont
déjà tenues à l'écart de toute référence aux trois autres
([ADR-0011](0011-host-dummies-as-a-standalone-package.fr.md)) et sont destinées à
migrer vers un dépôt à elles.

Les runs ont lieu sur des runners hébergés par GitHub : quatre vCPU, un plafond
de six heures par job. Un check ne peut être rendu obligatoire sur `main` que par
la protection de branche, qui nomme les checks un par un — une matrice fournit un
nom de check par branche.

## Décision

Toute pull request ciblant `main` doit franchir un seuil de score de mutation,
mesuré par Stryker.NET sur les mutants des fichiers qu'elle modifie, pour chaque
projet du dépôt dont le code est livré ou exécuté — imposé par deux barrages
indépendants, découpés le long de la frontière de dépôt à venir.

## Justification

Le barrage bouche exactement le trou que les signaux existants laissent ouvert.
`ci` prouve que la suite passe ; `sonar` prouve que le code a été exécuté. Ni
l'un ni l'autre ne distingue un test qui épingle un comportement d'un test qui ne
fait que le traverser — et cette distinction est toute la qualité d'une
bibliothèque dont le produit est sa sémantique. Les tests de mutation sont le
seul signal automatique qui la mesure.

Le rendre **obligatoire plutôt que consultatif** est l'objet de la décision, pas
un détail aggravant. Un rapport consultatif, sur un dépôt maintenu par une seule
personne, est un rapport que personne ne lit ; la pratique qu'il vise à installer
— écrire l'assertion, pas seulement l'appel — ne survit que si le merge en
dépend. Le dépôt traite déjà ainsi ses autres invariants : le cliquet de
warnings, la convention de commit et les floors supportés sont imposés, pas
suggérés.

Cantonner le barrage à **ce que la pull request modifie** est ce qui rend
l'obligation abordable. Le modèle de coût est linéaire en nombre de mutants, et
ce nombre est proportionnel au code mesuré ; mesurer le diff maintient une pull
request ordinaire au coût fixe de l'analyse et du build, là où tout mesurer, à
chaque fois, mettrait des dizaines de minutes sur chaque push pour du code que
l'auteur n'a pas touché. Le compromis accepté est que la granularité au fichier
du mode diff fait rapporter le score du fichier entier pour une petite
modification, si bien qu'une pull request peut avoir à répondre de manques
préexistants dans un fichier qu'elle n'a fait qu'effleurer. C'est un coût réel,
et c'est la bonne direction pour l'erreur : elle pousse à la hausse la couverture
des fichiers les plus faibles au contact, et le mainteneur peut toujours écarter
une branche.

Le périmètre couvre **l'outillage et les analyseurs autant que les
bibliothèques**. Leurs suites sont les lentes — elles pilotent des compilations
Roslyn et des comparaisons de snapshots, donc leurs mutants sont les chers —, mais
la dépense est une raison de les mettre dans le balayage hebdomadaire, pas une
raison de les laisser sans mesure : ce balayage est précisément le run autorisé à
durer le temps qu'il faut. Sur le barrage, le cantonnement au diff borne déjà ce
qu'ils coûtent, puisqu'une pull request qui ne touche pas le générateur ne paie
rien pour lui. Ce qui reste exclu l'est pour une autre raison que le coût : les
échantillons `Usage` et les benchmarks du binder ne sont pas du comportement
livré, et le worker de documentation est un point d'entrée de processus qu'aucun
test n'exerce en processus — le muter ne fabriquerait que des survivants qu'aucun
test ne pourrait tuer.

L'imposer par **deux barrages plutôt qu'un** ne coûte rien aujourd'hui et achète
la migration. Les packages JustDummies sont déjà isolés du reste par construction,
et ils partent ; une matrice unique devrait être réécrite, et son entrée de check
obligatoire renégociée, précisément au moment où la partie la moins intéressante
d'une séparation de dépôt devrait être sa CI. Deux barrages font de cette étape un
déplacement de fichier. Ils laissent aussi les deux barres évoluer séparément — ce
qui est nécessaire, puisque les bibliothèques se situent à des niveaux de maturité
de test visiblement différents et qu'une barre unique devrait être calée sur la
plus faible des deux.

Épingler le moteur de mutation compte pour la même raison que le floor Roslyn de
l'analyseur ([ADR-0001](0001-lock-the-analyzer-roslyn-floor.md)) : un moteur plus
récent invente de nouveaux mutants, et le score bougerait sans qu'une ligne de
code change. Un seuil n'a de sens que face à un générateur figé.

Le statut **preview** du runner dont dépend le barrage est la principale
faiblesse de la décision, et elle est acceptée en connaissance de cause :
l'alternative est l'absence totale de signal de mutation, puisque le runner
supporté ne sous-estime pas — il rapporte zéro. L'atténuation tient à ce que le
mode de défaillance est bruyant et non silencieux : une régression du runner qui
cesserait d'activer les mutants ramènerait tous les scores à zéro et ferait
échouer le barrage, au lieu de le laisser passer discrètement.

Enfin, le seuil est fixé **sous 100 %** parce que les mutants équivalents rendent
100 % inatteignable, et parce que c'est un *score* qui est contrôlé, pas
l'absence de survivants : c'est le rapport, et non le code de sortie, que le
mainteneur lit pour décider si un survivant est une assertion manquante ou un
mutant équivalent.

Chaque bibliothèque reçoit son **propre** seuil, dérivé de son score mesuré et
non d'une cible choisie dans l'abstrait. Un chiffre unique pour cinq
bibliothèques devrait être soit assez bas pour la plus faible — et donc sans
mordant pour celles déjà au sommet —, soit assez haut pour la plus forte, et donc
rouge dès le premier jour pour les autres. Le dériver par bibliothèque fait du
barrage un cliquet : il interdit la régression par rapport au niveau déjà atteint,
passe à l'introduction, et ne peut que monter. Le prix de ce choix, c'est que la
barre est basse là où le banc de tests est faible — précisément là où le barrage
serait le plus utile ; relever ces seuils à mesure que le balayage hebdomadaire
révèle de la marge est l'usage prévu du cliquet, et c'est délibérément une
décision du mainteneur plutôt qu'un automatisme.

## Alternatives envisagées

### Publier le score de mutation sans faire échouer le build

Envisagée parce qu'elle ne porte aucun risque : aucune pull request n'est jamais
bloquée par un outil en preview, et le chiffre est quand même publié.

Rejetée parce qu'elle ne change rien. Les invariants de qualité du dépôt sont
tous imposés plutôt que suggérés, précisément parce qu'un signal qu'il ne coûte
rien d'ignorer finit ignoré. La décision qui mérite d'être consignée ici, c'est
qu'une pull request doit répondre des assertions qu'elle n'a pas écrites ; un
rapport consultatif ne formule pas cette exigence.

### Muter chaque bibliothèque en entier sur chaque pull request

Envisagée parce que c'est la mesure honnête : un score sur toute la bibliothèque
est comparable d'un run à l'autre, et il ne peut pas être contourné en
n'effleurant que les bords d'un fichier.

Rejetée pour deux raisons. La première est le coût : le coût par mutant est une
exécution complète de la suite du projet, si bien qu'un balayage complet de tout
le périmètre se compte en heures — payées à chaque push, essentiellement pour
re-mesurer du code que personne n'a changé. La seconde suffit à elle seule : **un
score sur tout un projet est bien trop insensible pour servir de barrage**. La
plus grosse bibliothèque porte quelques milliers de mutants : un comportement
nouvellement ajouté et non affirmé déplace donc son score d'une fraction de
pour-cent — bien en dessous de tout seuil qui ne serait pas lui-même du bruit. Le score cantonné au diff est sensible précisément parce
que son dénominateur est petit : une poignée de mutants neufs, dont un survit,
c'est une chute visible. Le balayage hebdomadaire récupère le chiffre sur toute la
bibliothèque là où il a sa place — comme tendance, pas comme barrage.

### Relever l'exigence de couverture SonarQube Cloud à la place

Envisagée parce que la quality gate existe déjà, est déjà obligatoire, et ne
rapporte déjà que sur le code neuf — le mécanisme même dont cette décision a
besoin.

Rejetée parce qu'elle mesure autre chose. La couverture ne peut pas descendre
sous 100 % pour une ligne qu'un test exécute sans rien en affirmer ; relever le
pourcentage exigé achète des lignes exécutées, pas du comportement épinglé. Les
deux signaux sont complémentaires, et celui-ci n'a pas de substitut.

### Conserver le runner VSTest par défaut de Stryker

Envisagée parce que c'est la configuration supportée et non-preview, et que
préférer un composant en preview dans un check obligatoire n'est pas une décision
à prendre à la légère.

Rejetée parce qu'elle ne fonctionne pas du tout sur ce banc de tests. Vérifié par
la mesure : elle rapporte tous les mutants comme survivants, y compris des
mutants qui cassent la suite de façon démontrable quand on les applique à la
main. La choisir reviendrait soit à un barrage rouge en permanence, soit à un
seuil assez bas pour ne rien vouloir dire.

### Restreindre le périmètre aux bibliothèques livrées, en laissant l'outillage dehors

Envisagée, et retenue dans un premier temps, pour son coût : les tests
d'analyseurs et de générateur compilent du code et lancent des processus, leurs
mutants sont donc d'un ordre de grandeur plus chers que ceux d'une bibliothèque,
et leur comportement visible de l'extérieur est déjà tenu par les jobs
`analyzers`, `gendoc-docs` et le `floor` de `ci`.

Rejetée parce que le coût qu'elle évite est précisément celui que le balayage
hebdomadaire est là pour absorber, et parce que l'exclusion laissait un trou
plutôt qu'une frontière : une pull request ne touchant que les analyseurs n'aurait
franchi aucun barrage de mutation. Mesuré avant de revenir sur la décision, les
projets exclus portent à peu près autant de mutants que les bibliothèques
FirstClassErrors déjà dans le périmètre, et Stryker tourne sur tous — suites à
snapshots et tests lanceurs de processus compris.

## Conséquences

### Positives

* Une pull request qui ajoute du comportement sans ajouter l'assertion qui
  l'épingle est refusée automatiquement, sur le code qu'elle modifie, avant la
  revue.
* Les fichiers les moins bien testés s'améliorent au contact : en toucher un met
  tout son score de mutation sur le barrage.
* Le balayage hebdomadaire donne aux parties non touchées des bibliothèques une
  tendance que rien d'autre dans la chaîne ne produit.
* Le diagnostic est concret. Un barrage en échec nomme le mutant survivant, son
  fichier et sa ligne — il dit quelle assertion manque, pas seulement qu'un
  chiffre est trop bas.

### Négatives

* Une pull request qui touche un gros fichier faiblement couvert paie pour les
  manques préexistants de ce fichier, pas seulement pour son propre changement.
* Deux checks obligatoires de plus sur le chemin critique de chaque merge, et une
  pièce mobile de plus à maintenir en état — l'épinglage de l'outil, le mode du
  runner et les seuils doivent tous être entretenus délibérément.
* Les deux workflows sont des fichiers quasi identiques. Tant que la séparation
  n'a pas eu lieu, un correctif sur l'un est un correctif sur l'autre, et rien ne
  l'impose.
* Les mutants équivalents rendent une partie de la distance restante jusqu'à
  100 % inatteignable : le seuil relève donc du jugement, pas d'un calcul.
* Le balayage hebdomadaire est long — des heures, dominées par la plus grosse
  bibliothèque et par les suites lentes de l'outillage. C'est assumé : c'est la
  raison pour laquelle ce balayage est hebdomadaire et consultatif, pas un
  barrage.

### Risques

* **Le runner Microsoft Testing Platform est en preview.** Une régression pourrait
  déplacer les scores d'une version du moteur à l'autre. Atténué par l'épinglage
  du moteur dans le manifeste d'outils, qui fait de la montée de version un acte
  délibéré, et par le caractère bruyant du mode de défaillance : un runner qui
  cesse d'activer les mutants ramène tous les scores à zéro.
* **Sa sélection par la couverture est désactivée** : le coût est donc d'une
  exécution complète de la suite par mutant. C'est supportable aujourd'hui parce
  que ces suites sont rapides ; une suite nettement plus lente rendrait le
  balayage, puis le barrage, coûteux.
* **Les seuils sont calibrés sur les scores d'aujourd'hui.** Une bibliothèque
  ajoutée plus tard avec un banc de tests plus faible échouerait au barrage à son
  premier contact plutôt qu'à son introduction.
* **`JustDummies` part sans seuil de score.** Son balayage est trop long pour
  servir de calibration interactive : cette bibliothèque est donc barrée sur tout
  sauf sur un score, jusqu'à ce que le premier balayage hebdomadaire en fournisse
  un.
* **Les pull requests de tests seuls sélectionnent aussi des mutants**, via les
  fichiers de tests qu'elles modifient : une pull request qui n'ajoute que des
  tests peut donc être barrée.

## Actions de suivi

* Rendre les deux checks agrégés — `Mutation gate` et `JustDummies mutation gate`
  — obligatoires sur `main` dans la protection de branche ; un workflow ne peut
  pas se rendre obligatoire lui-même.
* Quand JustDummies migrera dans son propre dépôt, emporter son workflow, ses deux
  configurations et le manifeste d'outil tels quels, puis repointer le champ
  `solution` ; la page de référence du workflow porte la check-list.
* Réexaminer le choix du runner quand le support Microsoft Testing Platform de
  Stryker sortira de preview, et réactiver la sélection par la couverture quand
  elle classera correctement les mutants couverts.
* Relire les seuils après chaque montée de version du moteur, et après tout ajout
  d'une bibliothèque au périmètre.
* Fixer un seuil pour les projets qui n'en ont pas encore — `JustDummies`, les
  analyseurs, le générateur de documentation et la ligne de commande — à partir du
  premier balayage hebdomadaire. Leurs balayages sont trop longs pour être
  exécutés interactivement : aucun score n'a donc été mesuré pour eux, et leurs
  barrages de score sont livrés désactivés plutôt que devinés.

## Références

* [Référence du workflow `mutation`](../workflows/mutation.fr.md) — comment la
  décision est mise en œuvre, et les réglages qu'elle expose.
* [ADR-0001](0001-lock-the-analyzer-roslyn-floor.fr.md) — le précédent en matière
  d'épinglage d'une version d'outil qui, sinon, déplacerait seule un résultat
  mesuré.
* [ADR-0040](0040-split-the-justdummies-test-bed-between-example-and-property-suites.fr.md)
  — le découpage du banc de tests dont les deux suites alimentent ce barrage.
* [stryker-net#3117](https://github.com/stryker-mutator/stryker-net/issues/3117)
  — le signalement amont du runner VSTest de Stryker face à xUnit v3.
* [stryker-net#3629](https://github.com/stryker-mutator/stryker-net/issues/3629)
  — la limitation amont de l'analyse de couverture sous le runner Microsoft
  Testing Platform.
