# Le renderer HTML

Le renderer **html** transforme le catalogue d’erreurs en un **site statique autonome** : une page d’accueil consultable et filtrable et — en mode multi-pages — une page par erreur. C’est une sortie finale destinée à la lecture humaine, pas un format pivot.

## Quand utiliser HTML plutôt que Markdown ou JSON

| Format | À utiliser pour |
| --- | --- |
| **json** | consommation machine, intégration, pivot technique stable |
| **markdown** | documentation portable dans Git, un wiki, ou une page compatible Confluence |
| **html** | un catalogue visuel et autonome : consultation locale, artefact CI/CD, portail statique, démonstration produit, portail interne de référence des erreurs |

Choisissez **html** quand le public est humain (développeurs, support, QA, exploitation) et que vous voulez un catalogue soigné et navigable. Gardez **json**/**markdown** pour l’outillage et les workflows orientés texte.

## Génération

```bash
# Site multi-pages (par défaut, recommandé pour les gros catalogues)
fce generate --format html --layout split --output ./error-catalog

# Site page unique
fce generate --format html --layout single --output ./error-catalog
```

La sortie est un dossier complet, prêt à ouvrir en local ou à publier sur n’importe quel hébergement statique (GitHub/GitLab Pages, Azure Static Web Apps, S3, un nginx interne, …). La publication reste à la charge de votre CI/CD — le renderer ne produit que les fichiers.

```text
error-catalog/
  index.html                   (CSS et JS intégrés — autonome)
  errors/                      (mode split uniquement)
    ORDER_ALREADY_SHIPPED.html
    TEMPERATURE_BELOW_ABSOLUTE_ZERO.html
  assets/
    search-index.json          (pour l'outillage ; la recherche in-page est autonome)
```

Chaque erreur a une URL stable dérivée de son **code** (`errors/ORDER_ALREADY_SHIPPED.html`, ou `#err-ORDER_ALREADY_SHIPPED` en page unique) — jamais du message, du titre ou de l’ordre de génération.

## Fonctionnalités

- **Aucune dépendance externe.** Le CSS et le JS sont intégrés dans chaque page, les icônes sont du SVG inline, la police est celle du système — chaque page est autonome et fonctionne hors-ligne depuis un simple dossier (ou seule), sans CDN.
- **Thème clair / sombre.** Suit la préférence système (`prefers-color-scheme`) par défaut, avec une bascule manuelle mémorisée dans `localStorage`.
- **Recherche et filtres.** Une recherche côté client sur toutes les erreurs (code, messages, documentation, contexte) et des filtres par source et par présence d’un détail public. La recherche fonctionne hors-ligne (elle lit des données intégrées à la page, sans requête réseau).
- **Localisation.** Les libellés sont localisés selon `--language` (par ex. `--language fr`), comme le renderer Markdown. Les messages publics suivent la culture ; le message de diagnostic interne reste dans la langue auteur.
- **Déterministe.** Les erreurs sont triées par code et aucun horodatage n’est émis, donc le site généré se compare proprement d’une version à l’autre.

## Les trois messages

Chaque erreur est présentée avec ses messages publics et internes clairement séparés :

- **Résumé public** et **Détail public** — les messages exposables.
- **Message de diagnostic interne** — signalé comme interne et rendu sous forme de ligne de log ; il n’est **jamais** présenté comme un message public exposable.

## ⚠️ Sécurité — cela peut être un artefact interne

Le site HTML peut afficher le **message de diagnostic interne** de chaque erreur. Si votre catalogue contient des diagnostics riches (identifiants, valeurs fautives, état interne), **publier le site dans un espace public peut exposer des informations sensibles**. Traitez le site généré comme potentiellement interne : publiez-le derrière les mêmes contrôles d’accès que vos logs, ou générez une variante publique à partir d’un catalogue dont les messages de diagnostic sont sûrs.

> Régénérez dans un dossier de sortie neuf : le renderer écrase les fichiers qu’il produit mais ne supprime pas les fichiers périmés laissés par une exécution précédente.
