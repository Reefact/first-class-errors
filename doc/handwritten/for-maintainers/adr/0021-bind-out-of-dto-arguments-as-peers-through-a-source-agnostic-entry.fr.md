# ADR-0021 | Lier les arguments hors DTO comme des pairs via un point d'entrée agnostique de la source et non typé

🌍 🇫🇷 Français (ce fichier) · 🇬🇧 [English](0021-bind-out-of-dto-arguments-as-peers-through-a-source-agnostic-entry.md)

**Statut :** Accepté
**Date :** 2026-07-19
**Décideurs :** Reefact

## Contexte

Les adaptateurs primaires assemblent couramment une commande à partir de plusieurs origines : corps de requête, valeurs de route, paramètres de requête, en-têtes, claims ou métadonnées de message.

Le Request Binder d'origine partait du DTO et ne disposait d'aucun emplacement naturel pour une valeur déjà extraite qui n'appartenait pas à ce DTO. Envelopper chaque valeur dans un DTO synthétique déformerait son chemin et sa provenance.

Toutes les entrées doivent participer au même résultat collectant les erreurs afin que les échecs du corps et des arguments externes soient rapportés ensemble avec des erreurs structurelles et des chemins cohérents.

Une propriété de DTO peut dériver son nom par réflexion ; un argument hors DTO doit déclarer son propre chemin et peut également porter une provenance distinguant route, query ou header.

Typer le point d'entrée sur la commande finale entre en conflit avec la préservation de l'inférence des groupes de méthodes pour les bindings imbriqués. Le type de commande n'est requis qu'au moment de construire l'objet final.

## Décision

Le Request Binder utilise un point d'entrée non typé et agnostique de la source qui déclare d'abord l'enveloppe d'échec, attache comme pairs des sources de propriétés de DTO et des sources d'arguments hors DTO nommés individuellement, puis ne nomme le type de commande qu'au terminal `New` ou `Create`.

## Justification

Traiter les sources comme des pairs permet d'accumuler tous les échecs dans une seule enveloppe, quelle que soit l'origine depuis laquelle l'hôte a extrait la valeur.

Le point d'entrée non typé supprime un type de commande redondant dans les étapes intermédiaires et préserve l'inférence des bindings imbriqués, tandis que le terminal exprime toujours le type construit au moyen de l'assembleur.

Un argument doit posséder son chemin explicite puisqu'aucune propriété réfléchie n'existe. Sa provenance reste un fait diagnostique typé distinct au lieu d'être encodée dans la chaîne du chemin.

Réutiliser le même vocabulaire de convertisseurs pour les propriétés et les arguments préserve un modèle mental unique : les sources diffèrent par leur nommage et leur provenance, pas par leur validation ou conversion.

Les valeurs complexes assemblées à partir de plusieurs arguments libres sont exprimées en liant chaque argument comme pair puis en les composant au terminal. La décision n'interdit pas un futur argument complexe first-class si une sémantique agnostique de l'hôte et concrète apparaît ; elle refuse seulement d'en inventer une sans référent réel.

Les types d'entrée exacts, les API de sources, les helpers de provenance, les signatures génériques et les exemples sont documentés dans la [référence d'implémentation des ADR](../specifications/adr-implementation-reference.fr.md#contrats-dimplémentation-du-request-binder) et le guide du Request Binder.

## Alternatives envisagées

### Conserver le point d'entrée centré DTO et envelopper les arguments dans des DTO synthétiques

Envisagé pour réutiliser le chemin existant. Rejeté parce que cela ajoute de la cérémonie et rapporte les noms de propriétés du wrapper plutôt que les véritables clés du protocole.

### Typer le point d'entrée sur la commande

Envisagé pour rendre la cible explicite plus tôt. Rejeté parce que cela dégrade l'inférence des groupes de méthodes dans les bindings imbriqués et impose des arguments de type explicites répétés.

### Ajouter immédiatement un argument complexe hors DTO

Envisagé pour la symétrie avec les propriétés complexes de DTO. Rejeté parce qu'une propriété complexe représente un chemin dans un DTO, tandis qu'un argument libre n'a aucun graphe d'objets équivalent à parcourir. La composition de pairs couvre déjà la construction depuis plusieurs valeurs libres.

### Encoder la provenance dans le chemin d'erreur

Envisagé pour éviter une seconde valeur de contexte. Rejeté parce que le chemin et l'origine répondent à deux questions distinctes et doivent rester typés séparément.

## Conséquences

### Positives

* Le corps, la route, la query, les headers et les valeurs similaires se lient ensemble dans un même résultat.
* Le binding imbriqué conserve l'inférence des groupes de méthodes sans arguments de type explicites.
* Les échecs d'arguments portent une provenance typée sans analyse de messages.
* Les propriétés et arguments réutilisent le même modèle de conversion.

### Négatives

* La forme du point d'entrée diffère des exemples centrés DTO des ADR et documents antérieurs.
* La surface publique distingue désormais les sources de propriétés des sources d'arguments.
* Les appelants doivent fournir des conventions stables de noms et de provenance pour les arguments libres.

### Risques

* Les labels de provenance pourraient dériver au sein d'une application. Mesure : fournir et documenter des raccourcis standards pour les origines courantes.
* Les utilisateurs pourraient attendre une API d'argument complexe par simple symétrie visuelle. Mesure : documenter la composition de pairs et ne réexaminer la question qu'à partir d'un cas concret.

## Actions de suivi

* Maintenir le guide bilingue du Request Binder et le README du package alignés sur le point d'entrée agnostique de la source.
* Évaluer séparément des packages d'intégration hôte si les consommateurs ont besoin d'une extraction spécifique à un framework plutôt que du binding de valeurs déjà extraites.

## Références

* [Référence d'implémentation des ADR — Contrats d'implémentation du Request Binder](../specifications/adr-implementation-reference.fr.md#contrats-dimplémentation-du-request-binder)
* [ADR-0007](0007-name-the-binder-terminals-new-and-create.fr.md) — le nom des terminaux reste valide ; la forme illustrative du point d'entrée est mise à jour par cet ADR.
* [ADR-0012](0012-fix-the-binder-options-before-binding-begins.fr.md) — les options restent fixées au point d'entrée agnostique de la source ; la forme illustrative de l'API est mise à jour par cet ADR.
* [ADR-0017](0017-provide-a-configurable-application-wide-default-for-the-binder-options.fr.md) — la valeur par défaut reste valide ; la forme illustrative de l'API est mise à jour par cet ADR.
* Issue #148.
* [ADR-0024](0024-allow-a-one-time-editorial-refactoring-of-accepted-adrs.fr.md) — autorise cette extraction éditoriale.
