# Spécification du contrat de catalogue GenDoc

🌍 🇬🇧 [English](gendoc-catalog-contract.en.md) · 🇫🇷 Français (ce fichier)

Cette page décrit comment les erreurs de première classe propres à GenDoc sont
versionnées avec le train `cli`, en mettant en œuvre les
[ADR-0009](../adr/0009-report-the-toolings-failures-as-first-class-errors.fr.md)
et [ADR-0010](../adr/0010-treat-gendocs-error-catalog-as-a-versioned-contract.fr.md).

## Frontière du contrat

Les codes `GENDOC_`, messages publics, noms et types de clés de contexte de GenDoc
sont émis par l'outil `fce`. Ils appartiennent donc à la surface publique de
compatibilité du train `cli`, même si GenDoc n'est pas publié comme package NuGet
autonome.

La classification existante du diff de catalogue fait autorité :

* supprimer ou renommer un code est cassant ;
* supprimer une clé de contexte ou changer son type est cassant ;
* les ajouts compatibles et changements purement documentaires suivent les
  classifications produites par `fce catalog diff`.

## Cycle de vie de la baseline

1. Une baseline commitée représente le catalogue de la dernière release `cli`
   publiée avec succès.
2. La génération documentaire des pull requests compare le catalogue courant à
   cette baseline et expose l'impact en attente.
3. La baseline n'avance pas pendant le développement ordinaire.
4. Après une publication `cli` réussie, l'automatisation régénère la baseline
   depuis l'état livré et commite le résultat sur `main`.

La baseline doit être produite uniquement par `fce catalog update` ; l'éditer à
la main invalide la mesure du contrat.

## Gate de release

Au moment de la release, le workflow exécute `fce catalog diff` contre la baseline
commitée. Si le diff est cassant, la version candidate du train `cli` doit porter
un changement majeur. Le gate n'interdit pas un développement cassant ; il
interdit de le publier sous une version qui promet la compatibilité.

Si la publication réussit mais que le push de baseline échoue, la release existe
déjà et la baseline reste obsolète. L'opérateur doit la restaurer depuis le
catalogue publié avant la release suivante ; relancer une étape de publication ne
doit pas tenter de republier la même version.

## Sources de vérité

* Les factories documentées de `FirstClassErrors.GenDoc` — catalogue courant.
* La baseline GenDoc commitée sous `doc/generated/` — dernier contrat publié.
* `.github/workflows/gendoc-docs.yml` — génération et rapport de diff en pull request.
* `.github/workflows/release.yml` — gate de version, ordre de publication et
  avancement de la baseline.
* La [référence de versionnement des catalogues](../../for-users/CatalogVersioningReference.fr.md)
  — sémantique des commandes et classifications de compatibilité.

Changer la définition d'une rupture ou le train propriétaire du catalogue exige
un ADR. Un recâblage de workflow qui préserve ces règles ne modifie que cette page
et les références de workflows.
