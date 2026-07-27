# ADR-0042 | Sérialiser les tirages sur une source aléatoire, et borner la reproductibilité à la séquence de tirages

🌍 🇫🇷 Français (ce fichier) · 🇬🇧 [English](0042-serialize-draws-on-a-random-source.md)

**Statut :** Accepté
**Date :** 2026-07-27
**Décideurs :** Reefact

## Contexte

`JustDummies` tire chaque valeur arbitraire d'une `RandomSource`, qui possède un `System.Random`. Il existe deux sources : l'ambiante, derrière les points d'entrée statiques `Any`, dont l'état vit dans un `AsyncLocal`, et la source fixe que possède un `AnyContext` issu de `Any.WithSeed`.

`System.Random` n'est pas thread-safe. Son implémentation semée mute un tableau et deux index à chaque tirage sans aucune synchronisation ; sous contention, les deux index peuvent converger, après quoi le générateur retourne zéro définitivement. Rien ne le réinitialise. Comme la couche des valeurs projette un tirage nul sur le bas de la plage déclarée, chaque générateur se fige alors sur le minimum de son propre domaine — `0`, `""`, `Guid.Empty`, `int.MinValue` — pour toute la durée de vie restante de cette source, et aucune exception n'est levée.

Une source atteint plusieurs threads par deux voies ordinaires, dont aucune n'est un mésusage.

* Un `AsyncLocal` **descend** dans les tâches et les threads que son propriétaire démarre. Dès qu'une portée de graine est installée — ce que `Any.Reproducibly` et `Any.UseSeed` font toujours — un `Parallel.For` ou un `Task.WhenAll` à l'intérieur du test remet la même source à chaque worker. Hors portée de graine, l'état ambiant est créé paresseusement : chaque worker écrit son propre emplacement et obtient son propre générateur. Le chemin non semé est donc indemne, et c'est l'épinglage d'une graine qui crée le partage.
* Un `AnyContext` est un objet ; qui le détient peut le partager.

L'ADR-0006 a enregistré la décision d'origine et a nommé ce danger exactement — *« a single shared, mutable `System.Random` is not thread-safe and would produce cross-test interference and non-reproducible values »* — puis a retenu la localité de contexte par `AsyncLocal` comme remède. Ce remède traite l'axe **inter-tests** : deux tests en parallèle ne voient jamais la graine l'un de l'autre, ce qui est vrai et fait l'objet d'un garde-fou distinct. Il ne traite pas l'axe **intra-test**, que l'ADR n'envisage pas ; et le mécanisme qu'il choisit est précisément ce qui propage l'instance partagée. L'ADR-0006 est remplacé par l'ADR-0026, qui rebase `FirstClassErrors.Testing` sur `JustDummies` sans rouvrir la question.

Deux propriétés de la bibliothèque pèsent sur le remède. Les tirages se situent sur des chemins d'erreur et d'arrangement, jamais dans des boucles chaudes. Et `Any.UseSeed` est public depuis l'ADR-0038 — ouvert pour les adaptateurs de framework de test, mais utilisable par n'importe quel appelant, y compris dans le corps d'une boucle parallèle, où l'emplacement `AsyncLocal` propre à chaque worker rend la portée privée à cette itération.

Les promesses affichées du paquet sont que les valeurs sont arbitraires mais valides, et qu'une exécution est reproductible à partir d'une graine rapportée. La documentation utilisateur indique que la source est *« safe under parallel tests »* et explique l'`AsyncLocal` ; les remarques d'`AnyContext` indiquent qu'il est *« not thread-safe »* sans que rien ne le fasse respecter. Aucune des deux sources n'est protégée, et les deux disent des choses différentes.

`JustDummies` est pré-1.0 et non publié (ADR-0011) : le contrat peut encore être posé plutôt que corrigé.

## Décision

Chaque tirage sur une source aléatoire est sérialisé sur le verrou propre à cette source, et la promesse de reproductibilité est bornée à une séquence de tirages pris un à la fois — une exécution parallèle ne rejoue que si chaque unité de travail ouvre sa propre portée de graine.

## Justification

* **Le défaut est un danger de mutation, non de portée : le remède appartient donc là où se trouve la mutation.** L'`AsyncLocal` répond à *quelle* source est en vigueur ; il n'a jamais pu répondre à *comment* on y touche. Ajouter un verrou laisse intacte la décision de portée de l'ADR-0006 et fournit la propriété qu'on lui prêtait à tort. Retirer l'`AsyncLocal` casserait l'isolation inter-tests qu'il assure réellement, et le remplacer par un stockage lié au thread perdrait la graine au premier `await`.
* **Sérialiser ne coûte rien qui compte, et préserve toutes les exécutions existantes.** Un verrou non contendu ne change pas l'ordre dans lequel un thread unique consomme le flux : une graine épinglée rejoue donc à l'identique bit à bit — la propriété qui permet de livrer ceci sans invalider un seul test, ici comme chez un consommateur. Sur des chemins qui relèvent de l'arrangement et non du calcul, le coût du verrou est négligeable.
* **Le générateur doit être l'unique porte, pour qu'un contournement ne compile pas.** Livrer le `Random` sous-jacent derrière une façade synchronisée laisserait silencieusement non protégé tout membre que la façade ne redéfinit pas, et le prochain tirage ajouté déciderait par accident s'il est sûr. Garder l'instance privée transforme cela en erreur de compilation. C'est le raisonnement que les ADR-0031 et ADR-0035 appliquent ailleurs : rendre la règle incassable plutôt que seulement vérifiée.
* **La promesse doit se réduire à ce que la sérialisation apporte réellement.** Le verrou est pris par tirage primitif, et une seule valeur générée peut en consommer beaucoup — une chaîne tire un caractère à la fois — de sorte que deux threads s'entrelacent *à l'intérieur* d'une même génération. Ni la séquence ni le multiensemble des valeurs générées ne sont donc stables sous parallélisme, et une promesse de reproductibilité parallèle serait fausse. Énoncer la garantie la plus étroite est ce qui maintient la fiabilité du rapport de graine — la même exigence que la bibliothèque applique déjà lorsqu'elle retient sa promesse de rejeu complet face à un générateur étranger.
* **La promesse réduite ne coûte rien à l'utilisateur, car la plus large est déjà atteignable.** Une portée ouverte dans le corps d'une boucle parallèle est privée à son worker : dériver une graine par unité de travail à partir de celle de l'exécution fait rejouer l'ensemble. Ce mécanisme est déjà public, donc la décision n'ajoute aucune surface : elle documente une capacité au lieu de la construire.
* **Une seule règle pour les deux sources supprime une contradiction.** Verrouiller le point de passage commun protège d'un coup la source ambiante et `AnyContext`, ce qui permet de remplacer la remarque « not thread-safe » non appliquée de ce dernier par le contrat désormais vrai pour les deux.

## Alternatives envisagées

### Donner à chaque thread son propre générateur

Supprime la corruption sans verrou, en dérivant un générateur par thread à partir de la graine de l'exécution. Rejetée parce qu'elle détruit la propriété pour laquelle la bibliothèque existe : la correspondance thread → sous-flux est fixée par l'ordonnanceur, donc la même graine produit des valeurs différentes d'une exécution à l'autre. Dans une bibliothèque semée, le nombre de générateurs et leur propriété *sont* le contrat de reproductibilité, pas un détail d'implémentation — la raison même pour laquelle ceci ne peut pas être traité comme un choix local de sûreté d'accès.

### Lever une exception sur usage concurrent d'une source

La branche « interdire explicitement », et la plus conforme à l'habitude de la bibliothèque d'échouer vite sur une contradiction. Rejetée pour deux motifs. Un tirage concurrent n'est pas une contradiction : un test qui parallélise sans avoir besoin d'un rejeu appel par appel est légitime, et sous verrou il fonctionne. Et la détection n'est pas fiable dans la forme qui compte — un test `async` reprend légitimement sur un autre thread sans la moindre concurrence, donc toute vérification d'affinité de thread rejetterait du code correct, tandis qu'un vrai détecteur de chevauchement coûte ce que coûte un verrou en apportant moins.

### Utiliser un générateur thread-safe de la plateforme

`Random.Shared` est thread-safe et sans verrou. Rejetée parce qu'il ne peut pas être semé, ce qui condamne toute la surface de reproductibilité, et parce qu'il n'existe pas sur la cible `netstandard2.0` sur laquelle la bibliothèque plancher (ADR-0022).

### Ne rien faire et documenter la limitation

Rejetée parce que la défaillance est silencieuse et que son résultat est indiscernable d'une valeur légitime : un dummy devenu `0`, `""` ou `Guid.Empty` est exactement la valeur la plus susceptible de faire passer une assertion pour la mauvaise raison. Une limitation qu'un utilisateur ne peut ni observer ni détecter n'est pas une limitation que la documentation peut solder.

## Conséquences

### Positives

* Des tirages concurrents ne peuvent plus dégrader une source, ni sur le chemin ambiant ni sur celui du contexte, et une source reste utilisable pour les tirages séquentiels pris après une section parallèle.
* Les exécutions semées existantes sont inchangées : les séquences mono-thread sont identiques bit à bit.
* Les deux sources énoncent un seul contrat au lieu de deux contradictoires.
* La génération parallèle reproductible devient une recette exprimable et documentée, au lieu d'une impossibilité tue.

### Négatives

* Chaque tirage passe par un verrou, y compris l'immense majorité qui est mono-thread et ne peut pas contendre.
* La promesse de reproductibilité est désormais explicitement conditionnelle, ce qui est une phrase plus faible à écrire dans la documentation que celle qu'un lecteur aurait pu supposer.
* Les appelants de la source interne passent désormais par ses méthodes plutôt que par un `Random` : un futur tirage primitif devra être ajouté à ce type avant de pouvoir être utilisé.

### Risques

* Un utilisateur qui parallélise en attendant que la graine seule rejoue l'exécution constatera que non. Atténuation : la condition est énoncée dans la documentation XML du tirage et des deux points d'entrée de graine, et la recette par unité de travail est documentée dans le guide utilisateur.
* La sérialisation rend un arrangement *pathologiquement* parallèle plus lent qu'il ne le serait autrement. Accepté : la génération de dummies n'est pas un chemin chaud, et l'alternative est une corruption silencieuse.

## Actions de suivi

* À revisiter seulement si une charge mesurée montre que le verrou est significatif, ce qui reviendrait à rouvrir l'idée des sous-flux par unité de travail comme couture *publique* plutôt que comme substitution interne.

## Références

* Issue [#310](https://github.com/Reefact/first-class-errors/issues/310) — le défaut et ses mesures.
* [ADR-0006](0006-supply-arbitrary-test-values-from-a-seedable-source.fr.md) — la décision d'origine sur la source semable, qui nomme le danger et ne traite que l'axe inter-tests.
* [ADR-0026](0026-rebase-testing-arbitrary-values-on-dummies.fr.md) — remplace l'ADR-0006 sans rouvrir la question.
* [ADR-0038](0038-open-the-ambient-seed-scope-to-adapters.fr.md) — rend publique la portée de graine ambiante, ce qui met la recette par unité de travail à portée.
* [ADR-0022](0022-floor-the-library-on-net-framework-4-7-2.fr.md) — le plancher `netstandard2.0` qui écarte `Random.Shared`.
* [ADR-0031](0031-name-any-factories-after-their-clr-type.fr.md), [ADR-0035](0035-enforce-structural-any-conflicts-at-compile-time.fr.md) — le précédent « rendre la règle incassable plutôt que seulement vérifiée ».
