# ADR-0036 | Tirer les scalaires contraints à un réseau sur la grille

🌍 🇬🇧 [English](0036-draw-lattice-constrained-scalars-on-the-grid.md) · 🇫🇷 Français (ce fichier)

**Statut :** Accepté
**Date :** 2026-07-26
**Décideurs :** Reefact

## Contexte

JustDummies construit un scalaire directement pour satisfaire ses contraintes — jamais généré-puis-filtré —, détecte les contradictions au moment de la déclaration, et évite les boucles de nouvelles tentatives cachées et non bornées. Un générateur scalaire qui existe peut toujours générer, en un seul tirage. Les types à projection ordinale (les entiers, les temporels) tirent le k-ième ordinal non exclu du domaine en une passe sur un espace ordinal affine et préservant l'ordre ; le `decimal` tire un candidat et le décale dans un budget borné.

Un besoin récurrent est celui d'une valeur qui doit se situer sur une grille régulière : un multiple d'une unité (un montant en centimes entiers, une quantité à la douzaine), un `decimal` exprimable en un nombre fixe de décimales (un montant monétaire), ou un instant rond (une seconde pleine, un quart d'heure, un jour plein). Ce sont des invariants du code testé — un value object ou une précondition de contrat que la valeur doit respecter —, non ce que le test vérifie.

Aujourd'hui, une telle valeur n'est atteignable qu'en projetant après coup une valeur contrainte, `As(x => x * k)`. La projection déforme la portée déclarée (une portée exprimée dans l'unité d'avant projection ne veut plus dire ce qu'elle affirme) et fait sortir la valeur de l'algèbre de contraintes : le générateur projeté ne peut plus exclure de valeurs, ne peut plus détecter de conflit, et ne porte aucun indice de cardinalité pour les collections distinctes. Les dummies temporels à précision de tick surprennent en outre les tests qui sérialisent via un format à la seconde ou au jour, où l'aller-retour perd silencieusement la précision.

Parce que la projection ordinale est affine, les multiples d'un pas forment une progression arithmétique dans l'espace ordinal ; une grille est donc exprimable comme une dimension à part entière des moteurs d'intervalle sans quitter le modèle constructif. Les types à virgule flottante binaire n'ont pas de grille décimale (ni rationnelle générale) exacte — `0.1` n'y est pas représentable —, de sorte que la même construction ne peut y tenir. L'issue #226 recense `MultipleOf`/`WithScale` comme un ajout piloté par la demande ; le besoin de granularité temporelle y a été noté en parallèle.

## Décision

Une contrainte de réseau — `MultipleOf` sur les entiers, `WithScale` sur le `decimal`, `WithGranularity` sur les temporels — restreint un scalaire à une grille régulière tirée de manière constructive en une passe, se compose avec les bornes, exclusions et listes d'autorisation existantes, se déclare une seule fois par générateur, et est délibérément refusée aux types à virgule flottante binaire.

## Justification

Tirer sur la grille préserve l'invariant du tirage unique et sans nouvelle tentative : la projection ordinale affine fait des multiples d'un pas une progression arithmétique, de sorte que la grille devient une dimension de plus que le moteur d'intervalle échantillonne directement, plutôt qu'un post-filtre qui réintroduirait du rejet. Garder la valeur de première classe — au lieu d'une projection `As` — est tout l'enjeu : la portée déclarée conserve son sens, et les exclusions, les listes d'autorisation, la détection de conflit au moment de la déclaration et l'indice de cardinalité continuent de s'appliquer, si bien qu'une collection distincte sur une grille étroite échoue toujours par anticipation.

`WithScale` est un réseau de *valeurs* — un multiple de `10⁻ⁿ` —, non un contrat de représentation qui compléterait les zéros de fin, car l'invariant dont les appelants ont réellement besoin est « une valeur que le domaine accepte » (une fabrique monétaire qui refuse une troisième décimale), ce qui est un fait sur la valeur, non sur le rendu. Une garantie de représentation ne se composerait pas avec l'égalité de valeurs et surprendrait quiconque compare `12.30` et `12.3`.

Le réseau est refusé aux flottants binaires parce qu'une grille décimale n'y est pas exactement représentable ; l'offrir rendrait des valeurs hors grille sous une promesse que le type ne peut tenir. Il se déclare une seule fois — un second réseau différent entre en conflit plutôt que de s'intersecter silencieusement —, à l'image de la règle « déclaré une seule fois » qu'utilise déjà la liste d'autorisation, et cela épargne une combinaison par plus petit commun multiple que la demande ne justifie pas. Exposer une seule capacité du moteur comme `MultipleOf` sur les entiers et `WithGranularity` sur les temporels est ce qui permet à une seule dimension de servir les deux familles, de sorte qu'un correctif de la logique de grille atteint tous les types d'un coup.

L'arithmétique du pas, le calage-et-décalage décimal et la formulation des messages de conflit relèvent de l'implémentation, documentée dans le code `JustDummies` (`OrdinalIntervalSpec`, `WideIntervalSpec`, `DecimalIntervalSpec`) et dans la documentation utilisateur de JustDummies — pas ici.

## Alternatives envisagées

### Conserver la projection `As(x => x * k)` comme seul moyen

Envisagée parce qu'elle ne demande aucune nouvelle API et fonctionne déjà. Rejetée parce qu'elle déforme la portée déclarée, fait sortir la valeur de l'algèbre de contraintes (pas d'exclusion, pas de détection de conflit, pas d'indice de cardinalité) et — pour la précision temporelle — ne traite pas du tout la surprise de sérialisation.

### Générer puis filtrer les tirages hors grille

Envisagée parce que c'est la manière évidente d'honorer une grille arbitraire. Rejetée parce qu'elle réintroduit une boucle de nouvelles tentatives non bornée, en contradiction avec le modèle constructif et sans boucle cachée sur lequel la bibliothèque est bâtie.

### Étendre le réseau aux types à virgule flottante binaire

Envisagée pour la symétrie de surface avec les entiers et le `decimal`. Rejetée parce qu'une grille décimale (ou rationnelle générale) n'est pas exactement représentable en virgule flottante binaire ; la contrainte rendrait donc des valeurs hors grille — une fausse promesse pire qu'un manque délibéré et documenté.

### Faire de `WithScale` un contrat de représentation

Envisagée parce que le nom évoque `decimal.Scale` et une colonne de base de données `DECIMAL(p, s)`. Rejetée parce que l'invariant dont les appelants ont besoin est au niveau de la valeur, qu'une garantie de représentation ne se compose pas avec l'égalité de valeurs, et qu'elle surprendrait sur `12.30 == 12.3`.

### Combiner les réseaux répétés par plus petit commun multiple

Envisagée parce que « multiple de 4 et de 6 » est mathématiquement « multiple de 12 », non une contradiction. Rejetée comme disproportionnée : elle ouvre un cas limite propice au dépassement pour une combinaison que la demande ne montre pas, alors que « déclaré une seule fois » est simple, sûr et cohérent avec la liste d'autorisation.

## Conséquences

### Positives

* L'invariant « valeur sur une grille » est exprimable de manière constructive : la portée déclarée reste honnête et la valeur conserve toute sa composition — bornes, exclusions, liste d'autorisation, conflit par anticipation, et l'indice de cardinalité qui laisse une collection distincte sur une grille étroite échouer par anticipation.
* Une seule capacité du moteur sert les entiers et les temporels (et le `decimal` par son propre moteur), de sorte qu'un correctif de la logique de grille atteint tous les types d'un coup.
* Le contournement `As(x => x * k)` et la surprise de sérialisation à la précision de tick disparaissent tous deux pour les types couverts.

### Négatives

* Une nouvelle dimension commutative vit désormais dans trois moteurs d'intervalle (ordinal, large, décimal) et doit y être maintenue de concert.
* La surface est délibérément asymétrique : les flottants binaires portent le vocabulaire de signe et de bornes mais aucun réseau — un manque que les utilisateurs doivent apprendre plutôt que déduire.

### Risques

* La distinction valeur-contre-représentation de `WithScale` peut surprendre les utilisateurs qui attendent une échelle complétée. Atténuation : l'énoncer comme un réseau de valeurs dans la documentation du builder et le readme.
* La grille décimale tire-et-cale au lieu d'énumérer, si bien que la masse sur les deux points de grille extrêmes est approximative. Atténuation : l'atteignabilité des deux bornes est préservée et testée, en cohérence avec le tirage décimal existant.

## Actions de suivi

* Documenter `MultipleOf`/`WithScale`/`WithGranularity` dans le readme de JustDummies et la documentation des builders (fait dans la pull request d'implémentation).
* N'ajouter le sucre temporel `WholeSeconds()`/`WholeDays()` que si la demande apparaît ; le `WithGranularity(TimeSpan)` général le couvre en attendant.
* Ne revisiter la combinaison par plus petit commun multiple des réseaux répétés que si l'usage réel montre que la règle « déclaré une seule fois » est trop stricte.

## Références

* Issue [#226](https://github.com/Reefact/first-class-errors/issues/226) — le backlog piloté par la demande qui recense `MultipleOf`/`WithScale` et la granularité temporelle.
* [ADR-0013](0013-gate-distinct-collections-by-cardinality-else-bounded-draw.md) — l'indice de cardinalité qu'un réseau alimente, et le frère du tirage borné.
* [ADR-0020](0020-materialize-dummies-only-through-generate.md) — les dummies ne se matérialisent qu'à travers `Generate()`.
* `OrdinalIntervalSpec`, `WideIntervalSpec`, `DecimalIntervalSpec` et les builders concernés dans le projet `JustDummies` ; le readme NuGet de JustDummies.
