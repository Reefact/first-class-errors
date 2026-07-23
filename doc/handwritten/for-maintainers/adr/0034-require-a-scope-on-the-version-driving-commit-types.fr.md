# ADR-0034 | Exiger un scope sur les types de commit qui pilotent la version

🌍 🇬🇧 [English](0034-require-a-scope-on-the-version-driving-commit-types.md) · 🇫🇷 Français (ce fichier)

**Statut :** Accepté
**Date :** 2026-07-23
**Décideurs :** Reefact

## Contexte

Le dépôt publie plusieurs paquets versionnés indépendamment, regroupés en **trains de release** ; `tools/trains.sh` est la source de vérité unique qui associe chaque scope de Conventional Commit à un train (`lib`, `cli`, `dum`). Les release notes GitHub et le changelog d'un train sont générés (`tools/packaging/release-notes.sh`, `tools/changelog/collect-prs.sh`) en sélectionnant les commits dont le scope appartient à ce train. Un commit sans scope n'appartient à aucun train et est écarté. Cet écart est voulu : les commits d'infrastructure (`ci`, `chore`, `build`, `docs`) ne portent pas de scope et n'ont pas à figurer dans le dossier de release d'un paquet.

La convention de commit (CONTRIBUTING.md) rend le scope syntaxiquement optionnel sur tous les types. Les deux types **qui pilotent la version** sont `feat` (pilote `MINOR`) et `fix` (pilote `PATCH`) ; par définition, ils dénotent un changement visible par le consommateur d'un paquet. `commit-lint` — un unique script partagé par le hook local `commit-msg` et la vérification CI — valide la forme de l'en-tête et, jusqu'ici, acceptait un `feat`/`fix` sans scope.

La conséquence a déjà été observée et documentée (issue #231, PR #292) : un contributeur qui suit la convention à la lettre peut écrire un `feat:`/`fix:` visible par l'utilisateur et sans scope, qui disparaît alors silencieusement à la fois des release notes et du changelog. Le changement est livré dans les binaires mais n'apparaît jamais dans le dossier de release, sans erreur à aucun moment.

## Décision

`commit-lint` exige un scope de Conventional Commit sur les deux types qui pilotent la version, `feat` et `fix`, et rejette un commit sans scope — de façon identique au hook `commit-msg` et à la vérification CI — tandis que tout autre type conserve le scope optionnel.

## Justification

Le dossier de release est partitionné uniquement par le scope ; sur les types qui pilotent la version, le scope n'est donc pas une aide à la lisibilité mais la clé de routage qui décide si un changement visible par le consommateur est enregistré, ou non. Un `feat`/`fix` sans scope n'est pas simplement moins lisible — il est retiré du compte rendu public de ce qui a été livré, en silence.

La documentation seule ne peut pas combler ce manque. La règle déjà énoncée dans CONTRIBUTING dépend du fait que l'auteur se souvienne d'une conséquence invisible au moment d'écrire le commit ; l'échec ne produit aucun signal et ne se révèle que plus tard, comme un trou qu'un utilisateur remarque. Porter la règle dans `commit-lint` transforme une convention à mémoriser en un invariant appliqué, attrapé à l'écriture et — pour un hook local contourné — de nouveau en CI, la même application en couches sur laquelle la convention s'appuie déjà pour ses autres règles.

Confiner l'exigence à `feat` et `fix`, c'est ce qui la garde correcte plutôt que seulement stricte : seuls ces deux types pilotent une version et dénotent un changement visible par le consommateur. Les types qui ne pilotent pas la version sont précisément le travail d'infrastructure et à l'échelle du dépôt qui n'appartient légitimement à aucun train ; y exiger un scope serait du bruit et présenterait à tort du travail de dépôt comme du travail de composant.

Le coût — rejeter certains messages valides aujourd'hui — est borné et auto-correcteur : l'auteur ajoute un scope dès que le hook ou la CI le signale, guidé par un message qui nomme la conséquence. La grammaire de l'en-tête et le câblage partagé hook/CI relèvent de l'implémentation, dans `tools/commit-lint/lint-commit-message.sh` et le workflow commit-lint — pas ici.

## Alternatives envisagées

### Continuer à documenter la règle sans l'appliquer (l'état après #231)

Envisagé parce que #231 / #292 ont déjà corrigé la documentation, de sorte que la convention énonce désormais l'exigence en prose. Rejeté parce qu'une règle en prose contre un échec silencieux et invisible repose sur le fait que l'auteur s'en souvienne précisément quand rien ne signale l'erreur ; le dossier de release peut toujours perdre un changement visible par l'utilisateur, ce que la règle existe pour empêcher.

### Exiger un scope sur tous les types

Envisagé parce qu'une règle unique et uniforme « toujours un scope » est la plus simple à énoncer et à implémenter. Rejeté parce que les types qui ne pilotent pas la version sont le cas légitime « sans scope » — infrastructure, documentation à l'échelle du dépôt, ADR, exemples de `FirstClassErrors.Usage` — et y forcer un scope serait du bruit et attribuerait à tort du travail de dépôt à un composant et à son train.

### Modifier l'outillage de release pour conserver les feat/fix sans scope

Envisagé parce que l'écart se produit dans `release-notes.sh` / `collect-prs.sh`, qui pourraient au contraire router un commit qui pilote la version et sans scope vers un train par défaut. Rejeté parce qu'il n'existe pas de train par défaut correct pour un changement visible par le consommateur et sans scope : le scope est précisément ce qui identifie quel paquet a changé, donc deviner classerait le changement dans le dossier de release du mauvais paquet — une erreur plus subtile et plus difficile à remarquer que refuser le commit ambigu à la source.

## Conséquences

### Positives

* Un `feat`/`fix` visible par l'utilisateur ne peut plus disparaître des release notes ni du changelog ; le dossier de release est complet par construction.
* La règle que la convention documente déjà devient appliquée — à l'écriture et en CI — comblant le manque que #231 ne pouvait que décrire.
* Le travail d'infrastructure et à l'échelle du dépôt reste sans scope, de sorte que l'application n'ajoute aucune friction là où un scope n'aurait pas de sens.

### Négatives

* Des messages de commit valides aujourd'hui (`feat: …` / `fix: …` sans scope) sont désormais rejetés ; les contributeurs doivent ajouter un scope.
* La convention gagne une règle dépendante du type (scope requis pour `feat`/`fix`, optionnel sinon) au lieu d'un « scope optionnel » uniforme, une légère augmentation de la surface de la règle.

### Risques

* Un changement véritablement transverse ou limité aux samples, visible par l'utilisateur, pourrait n'avoir aucun scope unique naturel. Mesure : un changement qui traverse plusieurs composants porte déjà tous leurs scopes (la convention exige la liste complète séparée par des virgules) ; un changement qui n'appartient réellement à aucun composant n'est, par définition, pas un `feat`/`fix` de composant et prend un type qui ne pilote pas la version.
* L'historique et les branches en cours créés avant l'adoption peuvent contenir des `feat`/`fix` sans scope. Mesure : la règle s'applique à partir de son adoption, aux nouveaux commits, cohérent avec CONTRIBUTING → « Adoption » ; l'historique antérieur n'est pas réécrit et les commits de merge restent exemptés.

## Actions de suivi

* Appliquer l'exigence dans `tools/commit-lint/lint-commit-message.sh` (fait dans la pull request d'implémentation).
* Énoncer la règle appliquée dans CONTRIBUTING.md et sa traduction française (fait dans la pull request d'implémentation).
* Aucune modification de l'outillage de release n'est nécessaire : la sémantique de partition est inchangée ; seul le linter gagne l'exigence.

## Références

* Issue [#293](https://github.com/Reefact/first-class-errors/issues/293) — la présente décision.
* Issue [#231](https://github.com/Reefact/first-class-errors/issues/231) et PR [#292](https://github.com/Reefact/first-class-errors/pull/292) — ont fait surgir et documenté la dérive que cette décision applique.
* `tools/trains.sh`, `tools/packaging/release-notes.sh`, `tools/changelog/collect-prs.sh` — la partition par scope et l'écart des commits sans scope.
* `tools/commit-lint/lint-commit-message.sh` et `.github/workflows/commit-lint.yml` — là où l'exigence est appliquée.
* CONTRIBUTING.md → « Scope » et « Adoption ».
