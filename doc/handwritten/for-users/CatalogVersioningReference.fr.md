# Référence du versionnage du catalogue

🌍 **Langues :**  
🇬🇧 [English](./CatalogVersioningReference.en.md) | 🇫🇷 Français (ce fichier)

Cette page constitue la référence technique de `fce catalog update`, `fce catalog diff` et du fichier de baseline. Pour découvrir le mécanisme, commencez par [Versionnage du catalogue](CatalogVersioning.fr.md).

## 🧾 Snapshot et baseline

Le **snapshot canonique** est une projection JSON du catalogue contenant uniquement les données suivies par le versionnage :

- le code de chaque erreur ;
- son titre et sa source ;
- le nom et le type de ses clés de contexte.

La **baseline** est un snapshot choisi comme référence et commité dans le dépôt. Par défaut, son chemin est `errors-baseline.json`.

Le snapshot est indépendant du renderer utilisé pour publier le catalogue humain. Il est déterministe : les erreurs sont triées par code, les clés de contexte par nom et les fins de ligne sont normalisées. Les commandes `fce catalog` extraient toujours le snapshot sous la culture `en`, afin que la baseline ne dépende pas de la langue du catalogue.

## `fce catalog update`

```bash
fce catalog update --solution MyApp.sln
```

La commande extrait le catalogue courant puis crée ou remplace la baseline.

> Exécuter `catalog update` revient à **accepter explicitement le contrat courant**, y compris ses éventuels changements cassants.

Comportements :

| Situation | Comportement |
| --- | --- |
| Baseline absente | Le fichier est créé. |
| Baseline déjà identique | Aucun changement n'est écrit. |
| Catalogue différent | La baseline est remplacée et les changements acceptés sont résumés. |
| Baseline illisible ou corrompue | Elle est régénérée avec un avertissement. |
| Baseline produite par un schéma plus récent | La commande refuse de la rétrograder et échoue. |

Codes de sortie :

| Code | Signification |
| --- | --- |
| `0` | Baseline créée, déjà à jour ou remplacée avec succès. |
| `1` | Erreur d'exécution ou schéma de baseline plus récent que l'outil. |
| `130` | Exécution interrompue. |

## `fce catalog diff`

```bash
fce catalog diff --solution MyApp.sln
```

La commande compare la baseline au snapshot courant et écrit le rapport sur la sortie standard.

Codes de sortie :

| Code | Signification |
| --- | --- |
| `0` | Aucun changement n'atteint le seuil défini par `--fail-on`. |
| `2` | Au moins un changement atteint ce seuil. |
| `1` | Erreur d'exécution : baseline manquante, extraction impossible, fichier invalide, etc. |
| `130` | Exécution interrompue. |

### Politique d'échec : `--fail-on`

```bash
fce catalog diff --solution MyApp.sln --fail-on breaking
```

| Valeur | Effet |
| --- | --- |
| `breaking` | Valeur par défaut. Échoue uniquement sur les changements cassants. |
| `any` | Échoue dès qu'un changement est détecté, y compris un ajout compatible ou une modification informative. |
| `none` | Produit le rapport sans jamais échouer à cause d'une dérive. |

### Format du rapport : `--report`

```bash
fce catalog diff --solution MyApp.sln --report markdown
```

| Valeur | Usage |
| --- | --- |
| `text` | Valeur par défaut, destinée à la lecture dans un terminal. |
| `markdown` ou `md` | Rapport prêt à publier dans une pull request. |
| `json` | Rapport exploitable par un outil. |

### Comparer un snapshot existant : `--against`

```bash
fce catalog diff --against candidate-snapshot.json
```

Cette variante compare la baseline à un fichier de snapshot existant au lieu d'extraire le catalogue depuis le code. Elle est utile pour comparer deux artefacts de release.

## Classification des changements

| Changement | Impact |
| --- | --- |
| Code d'erreur supprimé | Cassant |
| Clé de contexte supprimée | Cassant |
| Type d'une clé de contexte modifié | Cassant |
| Code d'erreur ajouté | Compatible |
| Clé de contexte ajoutée | Compatible |
| Titre ou source modifié | Informatif |

Un renommage est représenté comme une suppression suivie d'un ajout et reste donc cassant. Lorsque exactement une nouvelle erreur possède le même titre que l'erreur supprimée, le rapport signale un renommage probable.

## Produire un snapshot sans modifier la baseline

```bash
fce generate --snapshot artifacts/error-catalog.snapshot.json
```

`fce generate --snapshot` écrit le snapshot canonique en plus du catalogue rendu. Contrairement aux commandes `fce catalog`, ce snapshot reflète la langue sélectionnée pour le rendu. Une baseline commitée doit rester indépendante de la langue : utilisez de préférence `fce catalog update`, ou imposez `--language en`.

## Configuration dans `fce.json`

Les chemins peuvent être centralisés :

```json
{
  "solution": "MyApp.sln",
  "baseline": "errors-baseline.json",
  "snapshot": "artifacts/error-catalog.snapshot.json"
}
```

Les chemins relatifs sont résolus par rapport au fichier de configuration.

## Options d'extraction communes

Les commandes `catalog update` et `catalog diff` acceptent notamment :

| Option | Rôle |
| --- | --- |
| `--solution <PATH>` | Extrait le catalogue depuis une solution. |
| `--assemblies <PATH>` | Extrait depuis une ou plusieurs assemblies déjà construites. |
| `--baseline <PATH>` | Remplace le chemin de baseline configuré. |
| `--configuration <NAME>` | Choisit la configuration de build. |
| `--framework <TFM>` | Restreint une solution multi-cible à un framework. |
| `--no-build` | Utilise les binaires existants sans reconstruire. |
| `--strict` | Arrête l'extraction à la première erreur. |
| `--verbose` | Écrit les diagnostics détaillés sur la sortie d'erreur. |

---

<div align="center">
<a href="CatalogVersioning.fr.md">← Guide du versionnage</a> · <a href="README.fr.md#-documentation">↑ Table des matières</a> · <a href="CatalogVersioningCI.fr.md">Intégration CI/CD →</a>
</div>

---