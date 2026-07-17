# ADR-0010 | Traiter le catalogue d'erreurs de GenDoc comme un contrat versionné

🌍 🇬🇧 [English](0010-treat-gendocs-error-catalog-as-a-versioned-contract.md) · 🇫🇷 Français (ce fichier)

**Statut :** Accepté
**Date :** 2026-07-17
**Décideurs :** Reefact

## Contexte

L'ADR-0009 a établi que `FirstClassErrors.GenDoc` documente ses propres échecs
comme des erreurs de première classe, en donnant à chacun un code stable préfixé
`GENDOC_` et un contexte structuré. Ces codes et ces clés de contexte sont émis
aux appelants à l'exécution et constituent les identités sur lesquelles
s'appuient les consommateurs externes — pipelines CI, intégrateurs, support.

GenDoc n'a pas de package NuGet propre : il est embarqué dans l'outil `fce`,
publié sur le train `cli` (`tools/packaging/pack.sh`). Un changement du catalogue
d'erreurs propre à GenDoc — un code renommé ou supprimé, une clé de contexte
retirée ou dont le type change — est donc un changement de ce qu'émet le package
`cli`, indiscernable de l'extérieur de tout autre changement de compatibilité de
ce package.

La bibliothèque livre déjà les mécanismes pour traiter un catalogue comme un
contrat versionné : `fce catalog update` enregistre une baseline, `fce catalog
diff` la compare, et la comparaison classe chaque changement en Cassant,
Compatible ou Informationnel (un code supprimé ou une clé de contexte
supprimée/retypée est Cassant). Jusqu'ici ces commandes étaient offertes aux
consommateurs documentant leurs propres erreurs, mais jamais appliquées au
catalogue de GenDoc lui-même : rien n'enregistrait la baseline de GenDoc, et rien
ne vérifiait un changement au regard de la version que le train `cli` s'apprêtait
à publier. Les deux trains suivent le versionnage sémantique, et le dépôt impose
déjà les Conventional Commits, mais aucun de ces mécanismes ne relie un changement
cassant du *catalogue d'erreurs* au *numéro de version* qui le publie.

## Décision

Un changement cassant du catalogue d'erreurs propre à GenDoc, mesuré par `fce
catalog diff` contre une baseline committée, exige un incrément de version majeure
du train `cli`, imposé au moment de la publication.

## Justification

* **La surface d'échec est un contrat publié : elle doit être versionnée comme
  tel.** L'ADR-0009 a fait des codes de GenDoc des identités stables dont
  dépendent les consommateurs ; une identité stable qui peut disparaître en
  silence sous un incrément de version d'apparence compatible n'est pas réellement
  stable. Le versionnage sémantique est la promesse que le package `cli` fait
  déjà, et un code supprimé ou renommé est exactement le genre de rupture que
  cette promesse existe pour signaler.
* **Imposer au release, pas à la pull request.** Un changement cassant du
  catalogue n'est pas fautif en soi — un changement délibéré est précisément ce à
  quoi sert une version majeure. Seule sa publication *silencieuse*, sous une
  version qui promet la compatibilité, est le problème. Bloquer les pull requests
  entraverait le développement incrémental normal ; imposer au release cible le
  seul point où la promesse de compatibilité est réellement faite, et laisse le
  travail quotidien libre.
* **Comparer au dernier release, pas à une cible mouvante.** La baseline n'avance
  que lorsqu'un release `cli` publie. Entre deux releases elle reste fixe, de
  sorte que le diff répond toujours à « qu'est-ce qui a changé depuis la dernière
  chose réellement publiée » — la question à laquelle le numéro de version doit
  répondre — quel que soit le nombre de pull requests intercalées.
* **Réutiliser les mécanismes de contrat existants, sans nouveau jugement.** La
  classification Cassant de `fce catalog diff` est déjà définie et testée ; cette
  décision la relie à la version du release plutôt que d'inventer une seconde
  notion de ce qu'être « cassant » signifie pour les erreurs propres à l'outil.

## Alternatives envisagées

### S'en remettre aux Conventional Commits et à la vigilance des relecteurs

Envisagée parce que le dépôt exige déjà un marqueur `!`/`BREAKING CHANGE:` sur les
commits cassants, vérifié en CI. Rejetée parce que ce marqueur est rédigé à la
main d'après l'intention du commit, alors qu'une rupture du catalogue peut être un
effet de bord non voulu (un refactor qui supprime une clé de contexte) ; rien ne
reliait le marqueur à une mesure mécanique du catalogue, de sorte qu'une rupture
silencieuse pouvait encore être publiée sous un incrément mineur.

### Contrôler la pull request plutôt que le release

Envisagée parce qu'elle fait apparaître la rupture au plus tôt. Rejetée parce
qu'un changement cassant est légitime en cours de développement tant que le
release final porte l'incrément majeur ; le bloquer par pull request pénaliserait
l'itération normale et forcerait des décisions de version prématurées, alors que
le contrôle au release attrape la même rupture au seul moment où elle compte.

### Publier GenDoc comme son propre package sur son propre train

Envisagée parce qu'un train dédié permettrait de versionner le catalogue
indépendamment. Rejetée comme disproportionnée : GenDoc n'a pas de consommateur
autonome (il ne tourne qu'à l'intérieur de `fce`), et le modèle d'outillage de
l'ADR-0002 le garde délibérément embarqué ; un nouveau train ajouterait une
machinerie de release sans bénéfice pour aucun consommateur.

## Conséquences

### Positives

* Un changement cassant des erreurs propres à GenDoc ne peut plus être publié sous
  une version `cli` non majeure : le release échoue jusqu'à ce que la version ou
  le changement soit réconcilié.
* Le catalogue gagne une baseline committée et un rapport de diff par pull
  request, de sorte que l'impact de compatibilité en attente est visible à la
  relecture.
* La documentation vivante que la CI régénère s'appuie sur un contrat explicite,
  ancré au release, plutôt que sur un instantané au mieux.

### Négatives

* Publier le train `cli` dépend désormais d'une baseline committée et d'une étape
  de diff ; un mainteneur doit comprendre qu'accepter un changement cassant
  signifie incrémenter la version majeure (ou revenir en arrière), pas contourner
  le contrôle.
* La baseline est rafraîchie par un push direct sur `main` après un release
  réussi — une écriture automatisée hors du flux normal de pull request, limitée à
  ce moment précis.

### Risques

* Une baseline périmée ou éditée à la main pourrait mal mesurer un changement.
  Mitigation : la baseline n'est jamais écrite que par `fce catalog update`,
  exécuté par le release après une publication réelle, de sorte qu'elle reflète
  toujours le dernier catalogue publié.

## Actions de suivi

* Aucune au-delà du câblage des workflows eux-mêmes (`gendoc-docs.yml` et le
  contrôle dans `release.yml`) ; le mécanisme vit dans les workflows et les
  commandes `fce catalog` que la documentation de référence couvre déjà.

## Références

* ADR-0009 — GenDoc modélisant ses propres échecs comme des erreurs de première
  classe, les codes que ce contrat versionne.
* ADR-0002 — le modèle de runtime de l'outillage qui garde GenDoc embarqué dans le
  train `cli` plutôt que publié séparément.
* Issue [#167](https://github.com/Reefact/first-class-errors/issues/167) — la
  demande à laquelle cette décision répond.
* [Référence du versionnage de catalogue](../../for-users/CatalogVersioningReference.fr.md)
  — les mécanismes `fce catalog update`/`diff` réutilisés ici.
