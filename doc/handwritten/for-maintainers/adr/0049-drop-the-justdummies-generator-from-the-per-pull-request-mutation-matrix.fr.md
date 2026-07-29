# ADR-0049 | Retirer le générateur JustDummies de la matrice de mutation par pull request

🌍 🇫🇷 Français (ce fichier) · 🇬🇧 [English](0049-drop-the-justdummies-generator-from-the-per-pull-request-mutation-matrix.md)

**Statut :** Accepté
**Proposé :** 2026-07-28
**Accepté :** 2026-07-28
**Décideurs :** Reefact

## Contexte

L'ADR-0043 conditionne chaque pull request au score de mutation de ce qu'elle a changé.
`justdummies-mutation.yml` exécute ce gate sous forme d'une matrice à trois pattes — le générateur
(`justdummies`), son adaptateur xUnit v3 (`justdummies-xunit`) et ses analyseurs
(`justdummies-analyzers`) —, chacune limitée au diff par `--since`.

Deux de ces trois pattes terminent en une quinzaine de secondes à une minute et demie. Celle du
générateur ne termine pas du tout.

Sur la pull request #337 — un diff de correction en quatre commits, **99 lignes de production
changées** — la patte `Mutate the diff (justdummies)` a sélectionné **844 mutants** et tournait encore
après **soixante minutes**, sans avoir produit de score, avant d'être annulée à la main. Ce n'est pas un
cas isolé : le coût de cette patte est fixé par la taille des *fichiers* que le diff touche, pas par la
taille du diff.

Trois contraintes, chacune enregistrée et mesurée, rendent ce coût structurel et non accidentel :

* **`--since` a une granularité par fichier, pas par ligne.** Stryker mute tous les mutants d'un fichier
  changé. Ces 99 lignes ont entraîné des fichiers entiers — `StringSpec.cs` (246 mutants),
  `Any.Combine.cs` (205), `ContinuousIntervalSpec.cs` (204), `CollectionState.cs` (109) — de sorte que
  près de neuf dixièmes du travail portaient sur du code que la pull request ne touche pas.
* **`"coverage-analysis": "off"` est obligatoire, pas un réglage.** Sous le runner MTP, la sélection de
  tests de Stryker classe à tort des mutants tués comme non couverts, donc chaque mutant rejoue tout
  l'oracle. C'est une décision d'exactitude (ADR-0043, et `mutation.en.md`, « Two settings that are not
  tuning knobs »), pas un levier disponible ici.
* **JustDummies est la plus grosse bibliothèque du dépôt** — quelques milliers de mutants — ce qui est
  déjà la raison pour laquelle son sweep *complet* porte `timeout-minutes: 350`.

Le débit observé sur `ubuntu-latest` est d'environ **quatorze mutants par minute**. Une patte de deux à
trois minutes n'admet donc au plus que **quarante-cinq mutants**.

Chaque levier exposé par Stryker 4.16 a été mesuré sur le diff de #337 :

| Levier | Mutants | Réduction |
|---|---|---|
| Référence (`--since`) | 844 | — |
| `mutation-level: Basic` | 648 | −23 % |
| `ignore-mutations: [string]` | 766 | −9 % |
| `ignore-mutations: [string, block, statement]` | 542 | −36 % |

Stryker.NET 4.16 n'offre **ni plafond de mutants ni échantillonnage** : les seuls filtres sont quels
mutateurs s'exécutent, quelles catégories de mutateurs s'exécutent, et quels *fichiers* sont mutés. Les
motifs `mutate` limités à des lignes — le seul levier qui ferait correspondre le travail au diff — sont
**silencieusement inertes** : `**/RegexNode.cs` sélectionne les 34 mutants de ce fichier, tandis que
`**/RegexNode.cs{153..165}`, `**/RegexNode.cs{153-165}` et les formes relatives au projet en
sélectionnent **zéro**, aussi bien dans le fichier de configuration qu'en ligne de commande. Un gate
configuré ainsi passerait au vert sans avoir rien testé.

Le sharding de la patte sur les fichiers changés a également été envisagé et mesuré. Plusieurs motifs
`--mutate` se composent bien en union (34 + 13 = 47 mutants, vérifié), donc le sharding est
implémentable — mais un shard ne peut pas être plus petit qu'un fichier, et huit des fichiers centraux de
la bibliothèque dépassent à eux seuls le budget de quarante-cinq mutants : `RegexParser.cs` (507),
`DecimalIntervalSpec.cs` (388), `OrdinalIntervalSpec.cs` (387), `WideIntervalSpec.cs` (382),
`StringSpec.cs` (357), `UriSpec.cs` (304), `ContinuousIntervalSpec.cs` (284), `CollectionState.cs` (188).

La meilleure combinaison atteignable tourne autour de −50 %, pour une exigence de −95 %.

## Décision

La patte `justdummies` est **retirée de la matrice par pull request** dans `justdummies-mutation.yml`.
`justdummies-xunit` et `justdummies-analyzers` conservent la leur. Le score de mutation du générateur
continue d'être mesuré par le **sweep complet hebdomadaire**, inchangé.

Le job `gate` et son nom de check sont inchangés, donc aucune entrée de branch protection ne bouge.

## Justification

* **La patte ne produit rien aujourd'hui.** Elle n'est pas lente, elle est inachevée : soixante minutes
  de runner pour aucun score. Retirer un check qui ne rapporte jamais ne perd aucun signal — cela cesse
  de payer pour son absence.
* **Les mesures ferment les alternatives.** Chaque levier interne plafonne à −36 %, le sharding est buté
  par le plus gros fichier changé, et le périmètre à la ligne n'existe pas dans cette version de Stryker.
  Ce n'est pas « on n'a pas assez réglé ».
* **L'ADR-0046 lui a déjà retiré son autorité.** Le gate par PR est consultatif ; la barre appliquée est
  le sweep hebdomadaire. Cette patte rapportait dans un canal qui ne peut pas refuser une pull request.
* **Le retrait étroit préserve ce qui marche.** Les pattes adaptateur et analyseurs sont petites,
  terminent en quatre-vingt-dix secondes, et gardent le retour de mutation par PR là où il est abordable.
  Retirer les trois jetterait un signal fonctionnel pour corriger un problème qui ne le concerne pas.

## Alternatives envisagées

### Sharder la patte sur les fichiers changés

Envisagée parce qu'elle ne demande aucun ADR — c'est un détail d'implémentation du « gate le diff » de
l'ADR-0043, et la composition de plusieurs motifs `--mutate` a été vérifiée. Rejetée parce que le
plancher d'un shard est un fichier entier : sur le diff de #337, le shard `StringSpec.cs` seul fait
246 mutants, soit environ dix-huit minutes, et huit fichiers centraux dépassent individuellement le
budget. Cela transformerait « ne termine jamais » en « quinze à vingt minutes dès que la pull request
touche quelque chose d'intéressant » : de la vraie machinerie, pour une cible toujours manquée.

### Plafonner le travail avec `mutation-level` et `ignore-mutations`

Rejetée sur les chiffres ci-dessus : −36 % au mieux, un ordre de grandeur d'écart. Cela coûte en outre du
signal au mauvais endroit — retirer les mutations `statement` et `block`, c'est cesser de tester la
suppression des gardes d'arguments, qui fait partie des défauts que la mutation attrape le mieux
(l'ADR-0045 est la décision que ces gardes mettent en œuvre).

### Échantillonner un sous-ensemble borné de mutants par pull request

Envisagée parce qu'un signal partiel sur une patte consultative est défendable. Rejetée parce que Stryker
n'expose aucun échantillonnage : les mutants sont générés de façon déterministe et exhaustive depuis
l'arbre syntaxique, donc le seul moyen de borner leur nombre est de borner les *fichiers*. Sélectionner
les fichiers changés jusqu'à un budget de mutants biaise systématiquement l'échantillon vers les petits,
si bien que `RegexParser`, `StringSpec`, `UriSpec` et les moteurs d'intervalles — là où la mutation vaut
le plus — ne seraient jamais couverts par pull request. Faire tourner la sélection remonte à l'inverse
des survivants dans des fichiers que la pull request n'a pas touchés, ce qui n'aide le relecteur à
décider de rien.

### Exécuter une patte nocturne limitée au diff

Rejetée parce qu'elle n'existe pas telle que décrite : la limitation au diff a besoin du point de fork
d'une pull request, et après un merge il n'y a plus de diff auquel se comparer. Une exécution nocturne ne
peut être que le sweep *complet* — le job le plus long du dépôt — sept fois par semaine au lieu d'une, ce
qui coûte plus cher, pas moins.

### Augmenter `timeout-minutes` au-delà de soixante

Rejetée : la patte rapporterait une heure ou plus après que la pull request est prête, sur un check qui
ne peut pas la bloquer. C'est le coût sans le bénéfice.

## Conséquences

### Positives

* Une heure de runner par pull request touchant le générateur, dépensée pour aucun résultat, est
  récupérée.
* La liste des checks de la pull request se stabilise en quatre-vingt-dix secondes environ, au lieu de
  rester suspendue à une patte qui ne rapporte jamais.
* Le retour de mutation par PR survit là où il fonctionne — l'adaptateur et les analyseurs.

### Négatives

* Une régression de mutation dans le générateur est vue le lundi plutôt que sur la pull request qui l'a
  introduite. Avec `break: 0` sur cette bibliothèque, rien n'était appliqué sur la pull request de toute
  façon ; ce qui est perdu est la liste des survivants dans le résumé du run, pas un gate.
* `justdummies-mutation.yml` et `mutation.yml` n'exécutent plus une matrice identique. Cette parité est
  énoncée dans `justdummies-mutation.en.md`, que cette décision impose de mettre à jour.

### Risques

* **Le seuil que cela reporte.** `justdummies.json` porte `break: 0` parce qu'aucun score n'a été
  mesuré ; le premier sweep complet doit publier le chiffre qui le fixera. Le jour où il le fera, la
  patte par PR redeviendra souhaitable — et cette décision devra être revisitée plutôt que tenue pour
  acquise. Atténuation : l'action de suivi ci-dessous.

## Actions de suivi

* Mettre à jour `justdummies-mutation.en.md` et sa traduction française : la matrice compte deux pattes
  sur les pull requests, trois sur le sweep complet, et pourquoi.
* Rouvrir cette décision si Stryker acquiert des motifs `mutate` limités aux lignes qui fonctionnent, ou
  si la sélection de couverture MTP (stryker-net#3629) est corrigée de sorte que `"coverage-analysis"`
  puisse être activé — l'un comme l'autre change le modèle de coût qui décide de ceci.
* Revisiter quand le premier sweep hebdomadaire publiera le chiffre JustDummies et que `break` cessera
  d'être 0.

## Références

* ADR-0043 — Conditionner les pull requests au score de mutation du diff : la décision que celle-ci
  restreint.
* ADR-0046 — Rendre le gate de mutation par pull request consultatif : pourquoi la patte n'a aucune
  autorité à perdre.
* ADR-0047 — Mesurer la mutation de JustDummies contre la seule suite unitaire : la tentative précédente
  pour rendre cette patte abordable.
* ADR-0045 — Garder les arguments publics et internes contre `null` : les gardes que `ignore-mutations`
  cesserait de tester.
* `.github/workflows/justdummies-mutation.yml`, `build/stryker/justdummies.json`.
* Pull request #337 — le run dont l'annulation après soixante minutes a motivé ceci.
* [stryker-net#3629](https://github.com/stryker-mutator/stryker-net/issues/3629) — le défaut de sélection
  de couverture MTP derrière `"coverage-analysis": "off"`.
