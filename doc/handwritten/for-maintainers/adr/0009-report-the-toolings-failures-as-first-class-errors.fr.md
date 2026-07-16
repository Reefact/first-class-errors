# ADR-0009 | Rapporter les échecs de l'outillage comme des erreurs de première classe

🌍 🇬🇧 [English](0009-report-the-toolings-failures-as-first-class-errors.md) · 🇫🇷 Français (ce fichier)

**Statut :** Proposé
**Date :** 2026-07-16
**Décideurs :** Reefact

## Contexte

FirstClassErrors existe pour faire des erreurs applicatives des concepts de
première classe, documentés et diagnosticables : un code stable, un contexte
structuré, une classification de transience, et une documentation générée
depuis le code lui-même. Le dépôt livre le modèle (la bibliothèque cœur), sa
vérification à la compilation (les analyzers) et un pipeline de documentation
(GenDoc, son worker et la CLI `fce`).

Avant cette décision, le pipeline de documentation rapportait ses propres
échecs en dehors de ce modèle : une sous-classe d'`Exception` nue portant un
message libre, des exceptions du framework (`FileNotFoundException`,
`ArgumentException`) sur les chemins de garde, et des chaînes de log non
structurées. Aucun de ces canaux ne portait de code d'erreur, de contexte
structuré ni de documentation. Le dépôt n'avait aucun consommateur interne
réel du modèle : le cœur ne définit aucun code émettable qui lui soit propre,
et les seules factories documentées vivaient dans l'exemple `Usage` et le
request binder (l'issue #140 consigne ce manque côté consommateur).

La surface d'échec du générateur est opérationnelle par nature — requêtes de
génération invalides, commandes SDK en échec, worker d'extraction qui plante
ou ne répond plus — et elle est exercée par des appelants externes (la CLI,
les pipelines CI) qui ont besoin de distinguer, réessayer ou rapporter ces
échecs. Renommer un code d'erreur établi ou un type public est un changement
cassant selon les règles de compatibilité du dépôt.

## Décision

L'outillage de documentation modélise sa propre surface d'échec comme des
erreurs FirstClassErrors documentées — des codes stables préfixés `GENDOC_`
portés par `SolutionDocumentationGenerationException`, une
`DiagnosableException` — et son propre catalogue est généré et vérifié par le
pipeline même qu'il implémente.

## Justification

* **L'outil doit pratiquer ce que la bibliothèque prêche.** Une bibliothèque
  dont la thèse est « les erreurs méritent un code, un contexte et une
  documentation » se démontre le plus crédiblement par son propre outillage :
  les échecs du générateur sont exactement le genre d'erreurs opérationnelles
  que vise le modèle. Les laisser à l'état de chaînes nues contredisait la
  revendication centrale du projet dans son propre code.
* **La surface d'échec est un contrat, elle a donc besoin d'identités
  stables.** Les pipelines CI et les appelants programmatiques réagissent aux
  échecs de génération. Un code `GENDOC_` stable leur donne un point d'appui
  qui survit aux reformulations de message, et le préfixe propre à l'outil
  évite toute collision entre ses codes et les codes applicatifs quand les
  deux aboutissent dans un même catalogue généré.
* **Un seul type d'exception, désormais diagnosticable.** Les appelants
  attrapaient déjà `SolutionDocumentationGenerationException` ; conserver ce
  type unique en le rebasant sur `DiagnosableException` préserve les sites de
  catch existants tout en exposant l'erreur structurée complète, plutôt que
  d'introduire un second canal d'échec à apprendre.
* **L'auto-documentation double comme test de bout en bout.** Parce que
  l'extraction exécute les méthodes de documentation et les factories
  d'exemple, générer le catalogue de l'outil lui-même en CI prouve le
  pipeline entier — attributs, lecteur, worker, moteur de rendu — contre une
  surface d'erreur réelle et évolutive plutôt qu'un jeu de données figé.

## Alternatives envisagées

### Garder l'exception nue et se contenter de loguer les erreurs structurées

Envisagée parce qu'elle n'exigeait aucun changement des types levés. Rejetée
parce que les appelants programmatiques n'auraient jamais reçu l'erreur
structurée : le modèle se serait arrêté à la ligne de log, et l'outil serait
resté absent de son propre catalogue.

### Lever les exceptions de famille via `Error.ToException()`

Envisagée parce que c'est le chemin de levée par défaut de la bibliothèque
(`PrimaryPortException`/`SecondaryPortException`). Rejetée parce qu'elle
remplace le type d'exception unique que les appelants attrapent déjà par une
famille de types choisie par échec — un contrat plus large et plus cassant
que conserver une exception unique, propre à la génération, qui porte
l'erreur.

### Ne modéliser que les échecs du worker, garder les gardes sur les exceptions du framework

Envisagée parce que les échecs de garde (fichier solution manquant, mauvaise
extension) se projettent naturellement sur
`FileNotFoundException`/`ArgumentException`. Rejetée parce qu'elle scinde la
surface d'échec entre deux conventions : les situations les plus visibles des
utilisateurs seraient restées sans code et sans documentation — exactement le
manque que la décision vise à combler.

## Conséquences

### Positives

* Les échecs du générateur portent des codes stables, un contexte, une
  transience et une direction, et sont documentés dans un catalogue généré
  par l'outil lui-même.
* Le dépôt gagne un consommateur interne réel du modèle, qui sert d'exemple
  de référence au-delà de l'exemple synthétique `Usage`.
* La CI vérifie le pipeline de bout en bout contre le catalogue de l'outil.

### Négatives

* Les codes `GENDOC_` et le contrat porteur d'erreur de l'exception
  deviennent une surface de compatibilité : renommer un code est désormais un
  changement cassant de l'outil.
* Les appelants qui construisaient `SolutionDocumentationGenerationException`
  depuis un message doivent la construire depuis une `Error`.

### Risques

* Les exemples de documentation s'exécutent dans le worker d'extraction ; une
  factory qui gagnerait un jour des effets de bord (lancement de processus,
  accès au système de fichiers) les exécuterait à chaque génération. Les
  factories doivent rester de purs assembleurs de faits déjà calculés.

## Actions de suivi

* Étendre le modèle aux surfaces d'échec du rendu et du versionnage de
  catalogue (`LayoutNotSupportedException`, `ServiceNameRequiredException`,
  `CatalogSchemaTooNewException`).
* Étudier une API de génération retournant `Outcome` (nommage selon
  l'ADR-0005) comme variante non levante des points d'entrée du générateur.

## Références

* Issue [#142](https://github.com/Reefact/first-class-errors/issues/142) — la
  demande à laquelle cette décision répond.
* Issue [#140](https://github.com/Reefact/first-class-errors/issues/140) —
  documenter les codes des packages référencés dans le catalogue d'un
  consommateur.
* ADR-0002 — le plancher de runtime de l'outillage sur lequel s'appuie le
  modèle de worker.
* ADR-0005 — le nommage des factories pour une future API de génération
  retournant `Outcome`.
