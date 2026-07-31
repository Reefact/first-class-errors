# ADR-0067 | Traiter les codes de sortie du CLI comme un contrat fermé et publié

🌍 🇫🇷 Français (ce fichier) · 🇬🇧 [English](0067-treat-the-cli-s-exit-codes-as-a-closed-published-contract.md)

**Statut :** Accepté
**Proposé :** 2026-07-31
**Accepté :** 2026-07-31
**Décideurs :** Reefact

## Contexte

`fce` est un outil de chaîne d'intégration avant d'être un outil interactif. `fce catalog diff`
existe pour faire échouer un job lorsque le catalogue d'erreurs dérive : son rapport part sur la
sortie standard, et la chaîne se branche sur ce que le processus retourne. La documentation
utilisateur le dit et le montre — la référence du versionnage de catalogue liste un tableau de codes
de sortie par commande, et les pipelines d'exemple du guide CI propagent le code pour décider du
résultat du job. Pour cette commande, le code de sortie n'est pas un effet de bord de l'exécution :
c'est la réponse.

Quatre codes sont publiés aujourd'hui : `0` (la commande a fait ce qu'on lui demandait), `1` (erreur
d'exécution), `2` (`catalog diff` a trouvé des changements atteignant le seuil choisi par
`--fail-on`) et `130` (l'exécution a été interrompue). `2` est délibérément distinct de `1` : une
chaîne doit pouvoir séparer « l'outil a fonctionné et le catalogue a bougé » de « l'outil n'a pas pu
s'exécuter ». `130` est la valeur conventionnelle d'un processus tué par SIGINT, `128 + 2`.

Jusqu'à récemment ces nombres étaient des littéraux nus à 32 endroits dans neuf fichiers de commande,
et les trois commandes retournant `130` portaient chacune son propre commentaire en prose expliquant
sa signification. Ils sont désormais nommés dans un type `ExitCodes` par exécutable, et les tests de
commande les assertent — `130` compris. Les nommer est ce qui rend l'ensemble lisible pour la
première fois ; ce n'est pas ce qui en fait une promesse.

L'ensemble n'est en réalité pas fermé aujourd'hui. Le CLI délègue l'analyse de la ligne de commande à
Spectre.Console.Cli, qui possède le chemin d'échec d'une ligne inanalysable et répond avec un code de
son choix. Une sous-commande inconnue sort en `-1` — rapporté 255 par un shell POSIX — sans rien
écrire sur aucun des deux flux standard. Cette cinquième valeur ne figure dans aucun tableau, a été
choisie par une dépendance plutôt que par ce dépôt, et atteint un appelant qui a demandé quelque
chose que l'outil n'a pas. L'audit d'architecture et de conception du 2026-07-20 avait déjà consigné
ce point comme un élément à normaliser et documenter.

Rien ne contraint l'ensemble mécaniquement. Les tests assertent les valeurs que les commandes
retournent aujourd'hui, ce qui n'est pas la même affirmation que « aucune commande ne peut en
retourner une sixième ». Une commande ajoutée demain compile tout aussi bien en retournant `3`, et un
script qui lit `2` comme « des changements ont été trouvés » casse silencieusement si une commande
ultérieure emprunte `2` pour autre chose.

## Décision

Les codes de sortie que retournent `fce` et son worker forment un ensemble fermé aux significations
publiées et fixes, possédé par ce dépôt, et étendu ou modifié uniquement par un acte délibéré et
documenté.

## Justification

Un code de sortie est la seule partie d'un outil en ligne de commande qu'une machine consomme. Tout
le reste de ce que `fce` émet — le rapport, les lignes de journal, les diagnostics — est lu par une
personne capable de s'adapter ; le code de sortie est lu par une chaîne qui ne le peut pas. Cela le
place dans la même catégorie qu'une signature d'API publique : le dépôt traite déjà le renommage d'un
code d'erreur ou d'un type public comme un changement cassant, et un code de sortie sur lequel un job
se branche pèse autant. Il est déjà publié, donc la promesse existe qu'elle soit consignée ou non ; ce
qui manquait, c'est le relevé de ce qu'elle couvre et de ce que la rompre coûterait.

La consigner comme décision plutôt que la laisser à l'état d'habitude, c'est ce que démontre le trou
en `-1`. Un contrat implicite ne reste pas entier tout seul : personne n'a décidé qu'une ligne de
commande inanalysable devait sortir en `-1` silencieusement, et personne ne l'a remarqué tant que les
nombres étaient des littéraux dispersés dans neuf fichiers. Un contrat énoncé une fois peut être
confronté ; un contrat qui n'existe que dans la somme de ses sites d'appel dérive sans que personne
n'ait pris la décision de le laisser faire.

La décision porte sur l'ensemble, pas sur la façon dont il est épelé. Si `ExitCodes` devenait une
énumération, si le CLI quittait Spectre.Console.Cli, ou si les commandes étaient entièrement
réécrites, « ces codes signifient ces choses et l'ensemble est fermé » tiendrait toujours et ce
relevé n'aurait pas à être modifié — ce qui est précisément le test que cette base applique pour
décider si une décision y a sa place.

Fermer l'ensemble coûte la liberté d'ajouter un code à la légère, et ce coût est le but. Un sixième
code est bon marché à ajouter et cher à reprendre, parce que l'outil ne peut pas savoir quelles
chaînes lisent déjà le cinquième. Rendre l'ajout délibéré — un changement de documentation dans les
deux langues, pesé comme tout autre changement de compatibilité — place le coût là où il est visible,
au moment du choix plutôt qu'au moment où le build d'un utilisateur casse.

## Alternatives considérées

### Laisser les codes implicites, comme ils l'étaient

Considérée parce que les valeurs sont déjà assertées par les tests de commande et déjà listées dans
la documentation utilisateur : un lecteur qui regarde aux deux bons endroits peut reconstituer
l'ensemble.

Rejetée parce que reconstituer n'est pas promettre. Les tests épinglent ce que les commandes font ;
ils n'interdisent pas un sixième code, et ils n'ont rien dit pendant que le chemin d'erreur d'analyse
répondait `-1` en dehors de tout tableau publié. C'est le mode de défaillance d'un contrat non
consigné, observé dans ce dépôt plutôt qu'imaginé.

### Traiter les codes de sortie comme un détail d'implémentation de chaque commande

Considérée parce que chaque commande décide de son propre dénouement, et qu'on pourrait soutenir que
les codes appartiennent à la commande plutôt qu'à l'outil.

Rejetée comme contredite par la documentation publiée et par la vocation de l'outil. Les tableaux de
référence sont par commande, mais une chaîne lit un seul nombre d'un seul processus, et `0`, `1` et
`130` signifient la même chose dans toutes les commandes par conception. Éparpiller la propriété,
c'est ainsi que `130` en est venu à être expliqué par trois commentaires séparés disant la même
chose.

### Modéliser l'ensemble par une énumération plutôt que par des constantes entières nommées

Considérée parce qu'une énumération ferait de l'ensemble un type, et qu'une valeur hors de cet
ensemble exigerait une conversion explicite.

Rejetée parce que les valeurs doivent atteindre le processus en entiers bruts : le contrat du
framework de commandes retourne `int`, donc chaque commande convertirait à son retour, et le
compilateur ne pourrait toujours pas empêcher une conversion de `3`. Le typage est nominal alors que
la friction est réelle — et la décision consignée ici porte sur le caractère fermé et publié de
l'ensemble, ce qu'aucune construction C# n'exprime dans un sens ou dans l'autre.

### Corriger le code d'erreur d'analyse dans le cadre de cette décision

Considérée parce que ce trou est ce qui a révélé le problème, et que le combler ici réglerait
l'affaire d'un seul mouvement.

Rejetée parce que la valeur qu'une ligne de commande inanalysable doit retourner, et ce qu'elle doit
afficher, est un choix de conception avec ses propres arbitrages — réutiliser `1`, ou réserver un code
distinct pour qu'une chaîne puisse séparer une invocation fautive d'une exécution échouée. Ce relevé
établit que l'ensemble est fermé et possédé ; il laisse le choix de cette valeur au suivi qui le
comblera, ce qui relève de la spécification.

## Conséquences

### Positives

* Une chaîne qui se branche sur le code de sortie de `fce` dispose d'une promesse sur laquelle
  s'appuyer d'une version à l'autre, et les recettes CI du versionnage de catalogue reposent sur
  quelque chose de consigné plutôt que sur le comportement du moment.
* Une nouvelle commande a une réponse à « que dois-je retourner » qui n'exige pas de lire neuf autres
  fichiers.
* Étendre l'ensemble devient visible : c'est une décision assortie d'un changement de documentation
  dans les deux langues, pas un littéral tapé à un `return`.

### Négatives

* Ajouter un code de sortie coûte désormais davantage que taper un nombre — les tableaux de référence
  anglais et français bougent avec lui, et l'ajout se pèse comme un changement de compatibilité.
* Le dépôt assume une promesse dont il n'admettait pas la propriété jusqu'ici, y compris pour les
  chemins auxquels une dépendance répond actuellement.

### Risques

* Le chemin d'erreur d'analyse en `-1` contredit la décision le jour même où elle est proposée. Tant
  que le suivi ne l'a pas comblé, l'ensemble publié et le comportement réel de l'outil divergent — le
  relevé rend cette divergence visible, il ne la supprime pas.
* Rien ne vérifie la règle. Une commande future peut retourner `3` et compiler, et aucun test ni
  analyseur n'y objectera ; ce relevé est une règle que le relecteur applique, pas une que le build
  fait respecter — l'arrangement contre lequel l'ADR-0056 met en garde, accepté ici parce que la
  surface est petite et relue.

## Suites à donner

* Normaliser et documenter le code de sortie d'une ligne de commande inanalysable, et lui donner un
  diagnostic sur la sortie d'erreur — l'élément déjà soulevé par l'audit d'architecture et de
  conception du 2026-07-20.
* Tenir les tableaux de codes de sortie de la référence du versionnage de catalogue, en anglais comme
  en français, en phase avec les types `ExitCodes` à chaque changement de l'ensemble.

## Références

* [ADR-0056](0056-state-the-coding-rules-where-an-agent-can-act-on-them.fr.md) — ce qu'il advient
  d'une règle sur laquelle rien ne peut agir, qui est le risque accepté par ce relevé.
* Référence des commandes de versionnage de catalogue, anglaise et française — les tableaux de codes
  de sortie publiés.
* Guide CI du versionnage de catalogue — les pipelines d'exemple qui se branchent sur le code.
* Audit d'architecture et de conception du 2026-07-20 — le code de sortie d'erreur d'analyse soulevé
  comme élément.
