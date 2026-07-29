# ADR-0055 | Faire appliquer par le compilateur les règles de style qu'il sait exprimer, et laisser le DotSettings faire autorité pour les autres

🌍 🇫🇷 Français (ce fichier) · 🇬🇧 [English](0055-enforce-the-style-rules-the-compiler-can-express.md)

**Statut :** Accepté
**Proposé :** 2026-07-28
**Accepté :** 2026-07-29
**Décideurs :** Reefact

## Contexte

Le style de code du dépôt, ses conventions de nommage et la sévérité de ses inspections sont consignés
dans `FirstClassErrors.sln.DotSettings`, un artefact ReSharper/Rider. Parmi ses règles figure
l'obligation d'écrire les types explicitement plutôt que de les laisser inférer, déclarée au niveau
erreur depuis l'introduction du fichier.

Ce fichier est lu par Rider et par rien d'autre. Aucun compilateur, aucun job de CI, aucun formateur en
ligne de commande et aucun agent automatisé éditant ce dépôt ne sait l'interpréter. Une règle consignée
là n'est donc appliquée que tant qu'un humain a la solution ouverte dans Rider, et seulement sur les
fichiers qu'il touche.

Les contributions à ce dépôt incluent désormais des agents automatisés, qui éditent les sources
directement et ne peuvent en aucun cas lire ce fichier. La règle des types explicites a dérivé en
conséquence : 203 violations se sont accumulées dans 17 fichiers — tous des projets de test — alors que
la règle était nominalement au niveau erreur et que rien n'en a jamais signalé une seule.

Roslyn fournit des analyseurs de style, configurés par `.editorconfig`, qui couvrent un sous-ensemble de
ce qu'exprime le DotSettings. La préférence pour les types explicites appartient à ce sous-ensemble.
Plusieurs autres règles du dépôt n'en font pas partie : les motifs de disposition des fichiers,
l'alignement en colonnes des déclarations consécutives et les conventions de nommage des régions n'ont
aucun équivalent Roslyn et ne peuvent pas être exprimés dans `.editorconfig`.

Ces analyseurs ne s'exécutent pas pendant la compilation tant qu'une propriété de build ne les active
pas. Configurer la règle dans `.editorconfig` seul ne change rien : mesuré sur ce dépôt, une compilation
complète de la solution avec la règle configurée et la propriété absente n'émet aucun diagnostic.

Le dépôt promeut déjà tout avertissement du compilateur en erreur dans la CI ; un diagnostic rapporté
comme avertissement est donc bloquant à l'entrée tout en restant consultatif pendant l'itération locale.

Deux propriétés de l'organisation précédente pèsent sur la décision. Le `.editorconfig` portait un
en-tête affirmant qu'il ne définissait délibérément aucune règle de style C# « pour que les deux
configurations ne puissent jamais diverger ». Et le DotSettings désactive la prise en charge
d'EditorConfig, ce qui signifie que Rider ne lit pas `.editorconfig` du tout — les deux configurations
étaient donc déjà indépendantes, et divergeaient déjà sur au moins un point d'hygiène des blancs.

Le moteur ReSharper est par ailleurs distribué comme outil en ligne de commande, lequel lit directement
le DotSettings et rendrait en principe toute la configuration exécutable hors de Rider. Il a été évalué
empiriquement avant cette décision ; les mesures sont consignées dans la pull request référencée.

## Décision

Les règles de style que Roslyn sait exprimer sont redites dans `.editorconfig` et appliquées par la
compilation, tandis que `FirstClassErrors.sln.DotSettings` reste la source de vérité pour les règles que
Roslyn ne sait pas exprimer.

## Justification

Une règle que seul un IDE fait respecter est une règle à laquelle seuls certains auteurs sont soumis, et
la dérive mesure ce que cela coûte : 203 violations sous une règle déjà réglée au niveau erreur. L'échec
n'est pas que la règle était floue ou non consignée — elle était consignée, et à la sévérité la plus
forte que l'outil propose. L'échec est que rien, hors d'un seul éditeur, ne pouvait l'observer.

Cet écart ne se comble pas par de la documentation. Certains auteurs de ce dépôt sont des agents
incapables d'analyser le DotSettings, et décrire ses règles en prose ailleurs produirait un troisième
énoncé de la même règle sans aucun mécanisme derrière. Ce qui atteint tous les auteurs, humains ou
automatisés, c'est un diagnostic émis par le compilateur. Redire la règle dans le seul dialecte que
le compilateur comprend est la seule façon de l'appliquer à qui écrit réellement le code.

Activer les analyseurs pendant la compilation, et non seulement en CI, découle du même raisonnement. Le
but est de mettre la règle sous les yeux de qui écrit le code au moment où il l'écrit — un contributeur
sans ReSharper, ou un agent qui compile pour vérifier son travail — plutôt que de la faire apparaître
une fois la pull request ouverte et le diff déjà formé. La rapporter comme avertissement garde
l'itération locale praticable, pendant que le cliquet de CI existant la rend bloquante avant tout merge.
La laisser non bloquante a été envisagé puis écarté : un avertissement que personne ne traite est
précisément l'état dans lequel la règle se trouvait déjà.

La duplication ainsi introduite est acceptée délibérément. Une couverture partielle mais appliquée vaut
mieux qu'une couverture complète qui ne l'est pas, et les deux énoncés disent la même chose plutôt que
de se recouvrir de façon ambiguë. Elle coûte aussi moins qu'il n'y paraît : la garantie que revendiquait
l'ancien en-tête du `.editorconfig` — que les deux configurations ne pouvaient jamais diverger — était
fausse au moment où elle a été écrite, puisque Rider ignore entièrement `.editorconfig`. Ce qui est
perdu, c'est l'apparence d'une source de vérité unique, pas la propriété elle-même. Chaque fichier nomme
désormais l'autre et précise quel outil lit lequel, de sorte qu'une modification d'un côté est
visiblement une modification que l'autre doit suivre.

Le périmètre reste étroit à dessein. Seules les règles ayant un véritable équivalent Roslyn migrent ; les
autres restent propres à Rider et continueront de dériver pour les agents, ce que cette décision ne
prétend pas résoudre. Prétendre le contraire serait pire que l'état actuel, car un contributeur en
déduirait raisonnablement qu'une compilation verte signifie que tout le style est respecté.

## Alternatives considérées

### Garder le DotSettings comme unique configuration et décrire ses règles en prose pour les agents

Envisagée parce qu'elle préserve la source de vérité unique, principe autour duquel l'organisation
précédente était bâtie, et n'exige aucune modification du build.

Écartée parce que la prose n'est pas une application. La règle qui a dérivé était déjà consignée, déjà
au niveau erreur et déjà sans ambiguïté ; la redire à un troisième endroit n'aurait détecté aucune des
203 violations. Cela se dégrade aussi : une description maintenue à la main à côté du fichier qu'elle
décrit est une chose de plus à synchroniser, sans rien pour signaler qu'elle a pris du retard.

### Exécuter le moteur ReSharper en ligne de commande, pour rendre le DotSettings lui-même exécutable

Envisagée parce que c'est la seule option qui conserve une configuration unique tout en l'appliquant
entièrement, y compris les règles d'alignement et de disposition que Roslyn ne sait pas exprimer. Elle
était l'option préférée jusqu'à ce qu'elle soit mesurée.

Écartée sur preuves. Nettoyer un seul fichier prend plusieurs minutes, car le moteur charge et analyse
la solution entière quelle que soit la restriction demandée — bien trop lent pour s'exécuter après chaque
édition, ce qui était l'usage visé. Pire, appliquer le profil de nettoyage du dépôt lui-même ne préserve
pas le code : il supprime des conversions nécessaires à la résolution de surcharges, laissant la solution
non compilable, et il réécrit les fichiers d'approbation des tests, ce qui les découple silencieusement
de ce que le générateur produit. L'alternative échoue sur la correction avant d'échouer sur la vitesse.

### Déplacer toute la configuration dans `.editorconfig` et retirer le DotSettings

Envisagée parce qu'elle rétablirait une source de vérité unique, de l'autre côté.

Écartée parce que la correspondance n'existe pas. Les motifs de disposition, l'alignement en colonnes des
déclarations consécutives et les conventions de régions constituent une part substantielle de ce que
consigne le DotSettings, et `.editorconfig` n'en sait exprimer aucun. Le résultat ne serait pas une
configuration unique, mais une configuration unique doublée d'une perte silencieuse de règles.

## Conséquences

### Positives

* La règle s'applique à tous les auteurs et tous les outils, pas seulement à qui a Rider ouvert.
* La dérive devient détectable au moment où elle est introduite, plutôt qu'à la relecture, ou jamais.
* Les contributeurs automatisés sont soumis à la règle pour la première fois, sans dépendre de la
  lecture d'une quelconque documentation.
* L'en-tête du `.editorconfig` énonce désormais ce qui est réellement vrai de la façon dont les deux
  fichiers sont lus.

### Négatives

* Une règle est désormais énoncée à deux endroits et doit être synchronisée à la main.
* Seul un sous-ensemble du style du dépôt est appliqué, et une compilation verte peut être lue comme
  signifiant davantage qu'elle ne signifie.
* Le dépôt n'a plus de source de vérité unique pour le style, en apparence sinon en fait.

### Risques

* Les deux configurations pourraient diverger silencieusement : rien ne vérifie que la règle du
  `.editorconfig` et sa contrepartie dans le DotSettings disent toujours la même chose.
* Un contributeur peut supposer que toutes les règles du DotSettings sont appliquées, et être surpris
  par celles qui ne le sont pas.
* L'alignement en colonnes reste non appliqué hors de Rider et continuera de dériver, y compris dans les
  groupes de déclarations que la mise en œuvre de cette décision a réécrits.

## Actions de suivi

* Décider si les autres règles du DotSettings ayant un équivalent Roslyn — l'ordre des modificateurs et
  les modificateurs d'accessibilité notamment — doivent suivre le même chemin, ou si la frontière reste
  là où cette décision la place.
* Réaligner les groupes de déclarations laissés périmés par la réécriture des types explicites, ce que
  seul le moteur ReSharper sait faire correctement.
* Examiner s'il faut autoriser Rider à lire `.editorconfig`, afin que l'hygiène des blancs déjà versionnée
  par le dépôt s'applique des deux côtés.

## Références

* [ADR-0031](0031-name-any-factories-after-their-clr-type.fr.md) — le même geste dans un
  autre registre : une convention rendue vérifiable par la machine plutôt que laissée à l'attention.
* [ADR-0045](0045-guard-public-and-internal-arguments-against-null.fr.md) — une règle appliquée par une
  convention de réflexion, pour la même raison.
* Pull request [#360](https://github.com/Reefact/first-class-errors/pull/360) — la mise en œuvre, et les
  mesures sur lesquelles reposent les alternatives écartées.
