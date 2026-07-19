# Référence d'implémentation des ADR

🌍 🇫🇷 Français (ce fichier) · 🇬🇧 [English](adr-implementation-reference.md)

Ce document contient les détails d'implémentation extraits des Architecture Decision Records. Les ADR restent la source de vérité pour **ce qui a été décidé et pourquoi** ; cette référence décrit la réalisation technique actuelle et peut évoluer sans modifier ces décisions.

## Plancher de compatibilité de l'analyseur

Décision liée : [ADR-0001](../adr/0001-lock-the-analyzer-roslyn-floor.fr.md).

L'analyseur est compilé contre le plancher Roslyn déclaré par `RoslynFloorVersion` dans `Directory.Build.props`. Le package conserve l'analyseur sous `analyzers/dotnet/cs/`.

La réalisation actuelle utilise plusieurs protections complémentaires :

* la référence de package de l'analyseur est épinglée sur le plancher déclaré ;
* `RoslynFloorTests` inspecte les métadonnées d'assembly et refuse les références `Microsoft.CodeAnalysis*` plus récentes ;
* le workflow de l'analyseur construit le véritable package NuGet puis compile un exemple avec le SDK plancher, ce qui vérifie à la fois le chargement et l'empaquetage ;
* Dependabot ignore les mises à jour automatiques des packages Roslyn qui définissent le plancher.

Lors d'un changement de plancher, il faut mettre à jour la propriété centrale, le SDK plancher utilisé par le workflow et le projet de vérification, ainsi que l'exigence de compilateur documentée. Le changement architectural lui-même exige un nouvel ADR remplaçant l'ADR-0001.

## Plancher d'exécution des outils

Décisions liées : [ADR-0002](../adr/0002-floor-the-tooling-runtime.fr.md), [ADR-0022](../adr/0022-floor-the-library-on-net-framework-4-7-2.fr.md).

Les outils en ligne de commande et le worker hors processus ciblent le plus ancien runtime .NET LTS pris en charge. La CI ordinaire s'exécute avec le SDK de développement courant, tandis que des jobs dédiés exécutent les outils livrés sur le plus ancien runtime pris en charge.

Les bibliothèques netstandard2.0 ont un plancher distinct : .NET Framework 4.7.2. Des tests Windows dédiés exercent les bibliothèques concernées sur le véritable runtime .NET Framework. Les projets d'outillage restent réservés au .NET moderne.

## Vérification ADR des pull requests

Décision liée : [ADR-0004](../adr/0004-check-every-pull-request-against-the-adr-base.fr.md).

La vérification ADR est une procédure destinée au mainteneur et aux agents, documentée dans `AGENTS.md`, qui compare une modification aux décisions acceptées et détermine si elle enregistre, remplace ou contredit un ADR.

Le workflow GitHub actuel est déclenché manuellement. Il soutient donc la procédure, mais ne garantit pas à lui seul que chaque pull request a été vérifiée. Toute automatisation future de cette obligation relève de la documentation et de la configuration des workflows, pas de l'ADR-0004.

## Contrats d'implémentation du Request Binder

Décisions liées : [ADR-0007](../adr/0007-name-the-binder-terminals-new-and-create.fr.md), [ADR-0008](../adr/0008-bind-nullable-value-type-properties-through-a-struct-constrained-overload.fr.md), [ADR-0012](../adr/0012-fix-the-binder-options-before-binding-begins.fr.md), [ADR-0014](../adr/0014-bind-a-required-list-by-presence-not-cardinality.fr.md), [ADR-0017](../adr/0017-provide-a-configurable-application-wide-default-for-the-binder-options.fr.md), [ADR-0018](../adr/0018-bundle-the-binders-structural-error-code-and-messages.fr.md), [ADR-0019](../adr/0019-document-overridden-binder-errors-in-the-consumers-catalog.fr.md), [ADR-0021](../adr/0021-bind-out-of-dto-arguments-as-peers-through-a-source-agnostic-entry.fr.md).

Les propriétés de types valeur nullables sont sélectionnées au moyen de surcharges contraintes aux structures afin que les groupes de méthodes de conversion opèrent sur le type sous-jacent plutôt que sur `Nullable<T>`.

Les options du binder sont fixées avant le début du binding. `Bind.WithOptions(...)` renvoie un point d'entrée configuré réutilisable et ne conserve aucun état propre à une requête. La valeur par défaut applicative est figée à sa première lecture et refuse toute modification ultérieure.

Les échecs structurels du binder sont représentés par des définitions regroupant le code d'erreur et les messages public et diagnostique. Les consommateurs qui remplacent ces définitions les documentent dans leur propre catalogue au moyen de la surface publique décrite par l'ADR-0019.

Les valeurs extérieures au DTO entrent par le point de binding indépendant de la source et participent comme des pairs au même flux d'accumulation et de construction que les valeurs issues du DTO. Les surcharges exactes, contraintes génériques, noms et exemples sont de la documentation d'API et appartiennent à la documentation utilisateur du Request Binder et au code source.

## Compatibilité du catalogue GenDoc

Décision liée : [ADR-0010](../adr/0010-treat-gendocs-error-catalog-as-a-versioned-contract.fr.md).

Le catalogue d'erreurs généré est traité comme un artefact de compatibilité versionné. L'automatisation de release compare le catalogue généré à la baseline associée à la dernière version compatible et signale les incompatibilités avant publication.

La baseline n'est mise à jour par le processus de release qu'après une publication compatible réussie. Les étapes de workflow, commandes, chemins d'artefacts et procédures de reprise sont maintenus dans la référence des workflows. Les mainteneurs doivent notamment prendre en compte le cas où la publication réussit mais où la mise à jour suivante de la baseline échoue.

## Contrats de génération de Dummies

Décisions liées : [ADR-0006](../adr/0006-supply-arbitrary-test-values-from-a-seedable-source.fr.md), [ADR-0011](../adr/0011-host-dummies-as-a-standalone-package.fr.md), [ADR-0013](../adr/0013-gate-distinct-collections-by-cardinality-else-bounded-draw.fr.md), [ADR-0015](../adr/0015-cap-any-combine-at-arity-eight.fr.md), [ADR-0020](../adr/0020-materialize-dummies-only-through-generate.fr.md).

Dummies est livré comme package autonome sans dépendance sur le package d'exécution FirstClassErrors. La génération n'est pas seedée par défaut ; la génération reproductible est choisie explicitement et expose la seed nécessaire pour rejouer les échecs.

La génération de collections distinctes compare d'abord le nombre demandé à l'indication de cardinalité du générateur d'éléments, lorsque `ICardinalityHint` sait en fournir une, diminuée des valeurs fixées en dehors de ce domaine via `Containing(...)` et des tirages opaques demandés via `ContainingAny(...)` — les deux élargissent ce que le générateur doit encore fournir lui-même plutôt que de compter contre lui. Une plage flottante ou décimale n'est pas considérée comme dénombrable à bas coût, car énumérer ses valeurs représentables relève d'une arithmétique de bits spécifique au type, disproportionnée pour l'usage « dummy » ; un tel générateur ne participe donc au contrôle anticipé que s'il est fixé sur une liste blanche explicite ou une valeur unique (`OneOf`, `Zero`, `Between(x, x)`), jamais via une plage plus large. Lorsque la cardinalité est inconnue, elle effectue un nombre borné de tirages et échoue explicitement plutôt que de boucler indéfiniment. Cette borne est un mécanisme de sûreté, pas une preuve que tout générateur externe ou biaisé réussira dès lors qu'un nombre suffisant de valeurs distinctes existe théoriquement. `CollectionState` et `ICardinalityHint` unifient la cardinalité et l'appartenance derrière une seule interface, afin qu'un générateur à domaine fini ne puisse pas sortir du périmètre anticipé via un comparateur.

`Any.Combine` fournit des surcharges jusqu'à l'arité huit. Les arités supérieures sont volontairement exclues de cette surface de confort et doivent utiliser la composition ou une factory spécifique au domaine.

La matérialisation s'effectue uniquement par `Generate()`. Les opérations du builder décrivent la génération et ne produisent pas d'effets de bord cachés.

## Surfaces publiques uniquement destinées à la documentation

Décision liée : [ADR-0019](../adr/0019-document-overridden-binder-errors-in-the-consumers-catalog.fr.md).

Les membres publics ajoutés uniquement pour rendre possible l'analyse ou l'extraction de documentation doivent rester minimaux, stables et clairement liés au contrat du catalogue. Avant d'ajouter un membre similaire, il faut vérifier si des métadonnées, des descripteurs générés ou une découverte côté analyseur peuvent répondre au besoin sans étendre l'API d'exécution.

## Règles de maintenance

* Modifier cette référence lorsque les mécanismes d'implémentation changent mais que les décisions restent valides.
* Écrire un nouvel ADR lorsque le choix architectural, la promesse de compatibilité ou le compromis accepté change.
* Conserver depuis chaque ADR concerné un lien vers la section pertinente de cette référence.
* Ne pas déplacer hors des ADR la justification, les alternatives rejetées ni les conséquences architecturales.
