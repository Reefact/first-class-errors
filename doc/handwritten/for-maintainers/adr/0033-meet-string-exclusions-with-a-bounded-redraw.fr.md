# ADR-0033 | Traiter les exclusions de chaînes par un tirage borné

🌍 🇬🇧 [English](0033-meet-string-exclusions-with-a-bounded-redraw.md) · 🇫🇷 Français (ce fichier)

**Statut :** Accepté
**Date :** 2026-07-22
**Décideurs :** Reefact

## Contexte

Dummies construit un scalaire directement pour satisfaire ses contraintes — jamais généré-puis-filtré — et détecte les contradictions au moment de la déclaration, de sorte qu'un générateur scalaire qui existe peut toujours générer. Il évite aussi les boucles de nouvelles tentatives cachées et non bornées.

Tous les builders scalaires sauf un exposent un trio d'exclusion (`OneOf`/`Except`/`DifferentFrom`). Pour les types à projection ordinale (entiers, types temporels, `char`, `Guid`), une exclusion est intégrée à la construction : le tirage est projeté sur le k-ième ordinal non exclu du domaine en une passe, et le fait que les exclusions laissent le domaine non vide se compte à bas coût à la déclaration.

Les chaînes n'ont pas de projection ordinale. Un `AnyString` est assemblé par disposition — préfixe, remplissage, valeurs contenues, suffixe — sur un domaine effectivement non borné. Une valeur exclue ne peut pas être retirée de ce domaine par construction, et le fait qu'un ensemble d'exclusions laisse une forme satisfaisable n'est pas décidable à bas coût en général : c'est trivial pour une longueur fixe d'un caractère, mais cela croît de façon combinatoire avec la longueur et le jeu de caractères.

`AnyString` était le seul builder scalaire sans contraintes d'exclusion, alors que « une valeur différente de celle que je détiens déjà » — tester un chemin d'inégalité avec un identifiant de type chaîne tout en préservant son format — est un besoin courant de chaîne factice (issue #224). L'écrire à la main avec une boucle de nouvelles tentatives oublie généralement la source seedée et casse la reproductibilité, précisément le piège que la bibliothèque existe pour éviter.

La bibliothèque accepte déjà un endroit où une valeur qu'un appelant a déclarée peut malgré tout ne pas se matérialiser : une collection distincte sur un domaine non dénombrable tire-et-déduplique sous un budget borné et échoue à la génération, de manière reproductible, lorsqu'elle ne le peut pas (ADR-0013). `AnyString.OneOf` est un générateur terminal distinct qui ne se combine pas avec les autres contraintes (ADR-0030).

## Décision

`AnyString.DifferentFrom`/`Except` sont satisfaits par un nouveau tirage borné de la disposition constructive, et une exclusion qui rend la forme insatisfaisable échoue à la génération par une erreur reproductible portant la seed, plutôt qu'au moment de la déclaration.

## Justification

Comme une chaîne ne porte aucune projection ordinale, une exclusion ne peut pas être intégrée à la disposition comme pour les types ordinaux ; un nouveau tirage est donc la seule stratégie générale — la même échappatoire qu'une collection distincte utilise déjà lorsqu'elle ne sait pas compter son domaine. La borner garantit la terminaison et transforme une exclusion insatisfaisable en un échec diagnostiquable et reproductible plutôt qu'en blocage ; porter la seed maintient cet échec dans le contrat de reproductibilité de la bibliothèque.

L'échec est différé plutôt qu'anticipé parce que la satisfaisabilité d'une chaîne sous exclusion n'est pas décidable à bas coût en général. Un contrôle complet au moment de la déclaration est donc irréalisable, et un contrôle partiel diagnostiquerait certaines specs insatisfaisables à la déclaration et d'autres seulement à la génération — une couture incohérente, pire qu'une règle unique et prévisible. Différer uniformément est le choix honnête, et cela confine l'écart aux seules exclusions : toute autre contrainte de chaîne reste constructive et validée par anticipation.

Accepter cet écart est justifié car les alternatives sont pires : laisser le manque garde le builder le plus utilisé comme le seul scalaire incapable d'exclure et renvoie les utilisateurs vers des boucles de nouvelles tentatives qui cassent le seed, tandis qu'imposer un verdict anticipé exige une procédure de décision que le domaine n'admet pas à bas coût. Le coût — un unique cas, cerné et documenté, où un générateur de chaîne qui existe peut malgré tout échouer — est le compromis déjà accepté pour les collections distinctes, et les collisions attendues sont ≈ 0 pour toute forme non triviale, de sorte que le chemin rapide constructif est préservé en pratique.

Le budget de nouveau tirage, le contenu de l'exception et la propagation de la seed relèvent de l'implémentation, documentés dans le code `Dummies` (`StringSpec`) et la documentation utilisateur de Dummies — pas ici.

## Alternatives envisagées

### Laisser `AnyString` sans contraintes d'exclusion

Envisagé parce que cela préservait la règle purement constructive des scalaires et n'exigeait aucun nouveau canal d'échec. Rejeté parce que cela laissait le builder le plus utilisé comme le seul scalaire incapable d'exprimer une exclusion, forçant des boucles de nouvelles tentatives écrites à la main qui cassent silencieusement le seed.

### Décider la satisfaisabilité par anticipation, comme les builders ordinaux

Envisagé parce que le diagnostic au moment de la déclaration est la norme de la bibliothèque pour les contraintes contradictoires. Rejeté parce que la satisfaisabilité d'une chaîne sous exclusion n'est pas décidable à bas coût en général ; un contrôle anticipé partiel serait une couture incohérente, diagnostiquant certaines specs tôt et d'autres tard.

### Tirer sans borne

Envisagé car une exclusion satisfaisable finirait par aboutir. Rejeté parce qu'une exclusion insatisfaisable boucquerait indéfiniment, violant le principe d'absence de boucles cachées non bornées.

### Évitement conscient de la spec à la disposition

Envisagé parce que construire la chaîne pour esquiver l'ensemble exclu garderait l'exclusion constructive et anticipée. Rejeté comme disproportionné : éviter correctement un ensemble exclu arbitraire sur les positions libres de la disposition est complexe pour un chemin que le nouveau tirage n'emprunte pour ainsi dire jamais ; on pourra le réexaminer si les faits le justifient.

## Conséquences

### Positives

* La paire d'exclusion est désormais uniforme sur tous les builders scalaires ; le besoin courant « identifiant différent, même forme » est servi, seedé et reproductible.
* Le chemin rapide constructif est inchangé pour toute spec sans exclusion, et en pratique pour les exclusions aussi (collisions ≈ 0).
* Une exclusion insatisfaisable échoue de manière sûre, reproductible, et nomme la seed à rejouer — cohérent avec l'ADR-0013.

### Négatives

* « Un `AnyString` qui existe peut toujours générer » ne tient plus sans condition : une exclusion trop serrée est le seul cas différé à la génération.
* Le moment de l'échec d'une exclusion de chaîne insatisfaisable diffère du diagnostic anticipé, au moment de la déclaration, que donnent les builders ordinaux.

### Risques

* Un budget mal calibré pourrait faire échouer une forme théoriquement satisfaisable mais extrêmement serrée. Mesure : documenter le budget et le réviser sur la base de faits, plutôt que de présenter l'échec comme impossible (la posture de l'ADR-0013).
* Les utilisateurs pourraient s'attendre à ce que l'exclusion de chaîne soit constructive comme pour les builders numériques. Mesure : énoncer explicitement le nouveau tirage et son échec différé dans la documentation du builder et le readme de Dummies.

## Actions de suivi

* Documenter le nouveau tirage et l'échec différé portant la seed dans le readme de Dummies et la documentation du builder (fait dans la pull request d'implémentation).
* Réexaminer le budget si l'usage réel révèle des épuisements indus.
* Envisager l'évitement conscient de la spec seulement si les faits montrent que le tirage borné est insuffisant.

## Références

* [ADR-0013](0013-gate-distinct-collections-by-cardinality-else-bounded-draw.fr.md) — le canal frère « tirage borné avec échec différé ».
* [ADR-0030](0030-draw-arbitrary-strings-from-an-explicit-terminal-set.fr.md) — `AnyString.OneOf` reste terminal et ne se combine pas avec les exclusions.
* [ADR-0020](0020-materialize-dummies-only-through-generate.fr.md) — les dummies se matérialisent uniquement via `Generate()`.
* `StringSpec` et `AnyString` dans le projet `Dummies` ; le readme NuGet de Dummies.
* Issue [#224](https://github.com/Reefact/first-class-errors/issues/224).
