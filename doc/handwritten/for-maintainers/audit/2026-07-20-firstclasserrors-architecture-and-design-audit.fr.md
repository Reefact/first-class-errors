# FirstClassErrors — Audit d'architecture, de conception et d'écosystème

🌍 **Langues :**  
🇫🇷 Français (ce fichier) | 🇬🇧 [English](./2026-07-20-firstclasserrors-architecture-and-design-audit.md)

**Date :** 2026-07-20
**Révision auditée :** `3bf89e3fb568beb69329b12b2ec2be14553bb8d4` (`main` au moment de l'audit)
**Périmètre :** l'ensemble de l'écosystème FirstClassErrors — bibliothèque cœur, Analyzers, GenDoc (+ Worker), CLI, RequestBinder, Testing, Dummies (en tant que membre de l'écosystème ; son audit dédié est l'[audit d'architecture et de conception de Dummies](./2026-07-20-dummies-architecture-and-design-audit.fr.md)), exemples, tests, documentation (EN + FR), base d'ADR, CI/CD et ingénierie de release.
**Statut :** consultatif. Conformément à la convention du dépôt (ADR-0004), cet audit produit des recommandations, jamais des bloqueurs ; toute modification d'ADR proposée est un brouillon que `@reefact` accepte ou rejette.
**Question posée :** *« Ce projet open source est-il cohérent, professionnel et maintenable, et pourrait-il raisonnablement devenir une référence dans son domaine ? »*

---

## À propos de cet audit

Ce rapport est le produit d'une revue de l'intégralité du dépôt menée comme un exercice de comité d'architecture, pas comme une revue de code ligne à ligne. La méthode a combiné :

* **Un vrai build et une vraie exécution des tests.** `dotnet build FirstClassErrors.sln` s'est achevé avec **0 avertissement, 0 erreur** (confirmant l'affirmation zéro-avertissement du dépôt), et `dotnet test FirstClassErrors.sln --no-build` a réussi **1 143 tests répartis sur 10 projets de test, avec 0 échec et 0 test ignoré** (405 cœur, 222 Dummies, 169 GenDoc, 152 RequestBinder, 85 Analyzers, 64 CLI, 21 tests par propriétés du cœur, 14 usage du binder, 8 Testing, 3 tests par propriétés du binder).
* **Treize revues de sous-systèmes menées en parallèle** (API du cœur, analyseurs, pipeline GenDoc, CLI, RequestBinder, Testing/Dummies, documentation utilisateur, synchronisation français/anglais, CI et outillage mainteneur, qualité des tests, exemples d'usage, structure de l'écosystème, cohérence de la vision), chacune produisant des constats appuyés sur des preuves citées.
* **Un audit dédié de chacune des 26 ADR** — solidité de la décision, conformité de l'implémentation vérifiée face au code actuel, fraîcheur, et synchronisation des traductions — plus une revue au niveau du corpus à la recherche de doublons, de contradictions et de décisions manquantes.
* **Des analyses des manques fonctionnels** face au paysage concurrentiel de la gestion d'erreurs en .NET et face à la surface d'intégration qu'une bibliothèque centrée sur les frontières est censée offrir.
* **Une passe de vérification contradictoire** au cours de laquelle chaque affirmation de sévérité critique ou haute a été réexaminée indépendamment face au code avant d'être admise dans ce rapport (résultat : 6 confirmées, 3 partiellement confirmées et reformulées dans leur forme vérifiée, 0 réfutée), suivie d'**une passe de critique de complétude** dont les sondages complémentaires (la réalité des releases sur les trois trains, la santé communautaire et le *bus factor*, la posture de sécurité, la performance des chemins du cœur, le jeu de fichiers d'instructions pour IA) sont incorporés tout au long du rapport.

Chaque constat cite des preuves concrètes (`file:line`, noms de types, identifiants d'ADR, pages de documentation). Les étiquettes de sévérité/valeur suivent la classification demandée par le comité : **Critique**, **Forte valeur**, **Valeur moyenne**, **Priorité basse**, **Hors périmètre**.

## Table des matières

1. [Résumé exécutif](#1-résumé-exécutif)
2. [Évaluation globale](#2-évaluation-globale)
3. [Évaluation de la vision du projet](#3-évaluation-de-la-vision-du-projet)
4. [Forces](#4-forces)
5. [Faiblesses](#5-faiblesses)
6. [Revue des ADR](#6-revue-des-adr)
7. [Conformité aux ADR](#7-conformité-aux-adr)
8. [Revue d'architecture](#8-revue-darchitecture)
9. [Revue de l'API publique](#9-revue-de-lapi-publique)
10. [Revue de l'écosystème](#10-revue-de-lécosystème)
11. [Revue de la documentation](#11-revue-de-la-documentation)
12. [Revue de l'expérience développeur](#12-revue-de-lexpérience-développeur)
13. [Analyse des manques fonctionnels](#13-analyse-des-manques-fonctionnels)
14. [Améliorations recommandées](#14-améliorations-recommandées)
15. [Feuille de route proposée](#15-feuille-de-route-proposée)
16. [Conclusion](#16-conclusion)

---
## 1. Résumé exécutif

**Question posée :** *Ce projet open source est-il cohérent, professionnel et maintenable, au point de pouvoir raisonnablement devenir une référence dans son domaine ?*

**Réponse : oui — avec une confiance inhabituelle sur la cohérence, le professionnalisme et la maintenabilité ; conditionnellement sur la « référence », où les conditions sont spécifiques, traitables, et tiennent surtout à livrer le dernier kilomètre plutôt qu'à réparer l'existant.**

FirstClassErrors est ce dépôt rare dont la philosophie affichée est appliquée mécaniquement plutôt que décrite comme une aspiration. Ses cinq principes de conception aboutissent chacun à un mécanisme du système de types ou de la chaîne d'outils : une erreur ne peut pas exister sans son message public (builder par étapes) ; la taxonomie documentée est portée par les types constructeurs ; le catalogue généré est régénéré à chaque PR et gaté à la release comme contrat versionné ; 18 analyseurs Roslyn embarqués ferment la boucle au moment de la compilation. Le run de build indépendant de l'audit a confirmé les affirmations de qualité : **0 avertissement, 0 erreur, 1 143/1 143 tests réussis**. Les 26 ADR ont toutes été auditées individuellement : **26 solides, 25 pleinement conformes à l'implémentation (une partielle), zéro contradiction** — une discipline de gouvernance bien au-dessus des normes industrielles. Sur le plan concurrentiel, le projet détient un fossé défensif en quatre volets (catalogue généré versionné, gating par diff de contrat, analyseurs livrés, binder de frontière réutilisant les fabriques) qu'aucune bibliothèque d'erreurs .NET grand public n'occupe.

Les faiblesses sont aussi remarquables par ce qu'elles ne sont *pas* : l'audit n'a trouvé aucun défaut architectural exigeant une refonte. Elles se concentrent plutôt en quatre groupes. **(1) Le dernier kilomètre HTTP manquant** — le catalogue promet aux clients d'API des réponses RFC 9457 qu'aucun code à l'exécution ne produit ; chaque adoptant écrit à la main l'unique mapping où la séparation public/interne peut être violée. **(2) Un corpus d'ADR inversé** — 17 des 26 ADR gouvernent des paquets périphériques tandis que les décisions fondatrices (le modèle Outcome lui-même, netstandard2.0, class-et-non-struct, le schéma d'analyseurs) ne sont pas consignées, si bien que le propre contrôle ADR par PR du projet ne peut pas se déclencher sur ses invariants les plus importants. **(3) Des écarts entre release et réalité** — une préversion a été livrée sous un processus antérieur aux trains, depuis remplacé ; le pipeline documenté à trois trains n'a jamais achevé un run de production ; les paquets `Dummies`/CLI/binder ne sont pas publiés alors que la section d'installation du README pointe vers deux d'entre eux (instructions d'installation mortes, vérifié contre nuget.org) ; les changelogs sont vides malgré la préversion livrée ; une `Dummies.dll` non publiée est pendant ce temps embarquée dans `FirstClassErrors.Testing`. **(4) Les bords maintenus à la main d'un système vérifié par machine** — deux README nuget.org inexacts, des lacunes de six guides dans les deux hubs de documentation, deux dérives confirmées du miroir français, trois workflows non documentés, et un CONTRIBUTING qui contredit l'outillage de release sur la signification des scopes de commit. Une passe de complétude a ajouté un cinquième groupe, tourné vers la communauté : un *bus factor* de 1 sans aucune disposition de continuité, des fichiers de santé communautaire manquants (code de conduite, modèles d'issue), et une surface d'injection de prompt non durcie dans la couche de revue par IA.

Six points sont classés Critiques : consigner rétroactivement les ADR fondatrices ; exécuter la séquence de publication de Dummies ; corriger les README de vitrine ; corriger les quatre dérives doc/contrat confirmées ; livrer la projection Error→ProblemDetails ; ajouter l'analyseur des `Outcome` ignorés. Trois des six se mesurent en heures, pas en semaines.

## 2. Évaluation globale

**Cohérence : exceptionnelle.** Une seule phrase de vision à chaque point d'entrée, décomposée en principes falsifiables, implémentée par des mécanismes d'application, étendue de façon cohérente aux satellites (le binder applique les principes 1 et 3 à la frontière ; l'outillage applique le modèle à lui-même — dogfooding — conformément à l'ADR-0009). La seule tension de périmètre — une bibliothèque générique de données de test vivant dans le dépôt d'erreurs — est explicitement gouvernée par l'ADR-0011 avec des déclencheurs d'extraction consignés, ce qui est exactement l'allure d'une dérive gouvernée.

**Professionnalisme : exceptionnel, avec deux réserves.** Une posture de chaîne d'approvisionnement solide (actions épinglées par SHA, publication OIDC, provenance, SBOM attestés), une philosophie de CI par preuve positive, des décisions d'API guidées par les preuves avec tests de verrouillage, et une documentation bilingue d'une qualité de référence. Les réserves : les bords vitrine/documentation énumérés ci-dessus (réels, peu coûteux, actuellement visibles des utilisateurs — y compris des instructions d'installation pour des paquets pas encore sur NuGet), et une ingénierie de release répétée mais non éprouvée : l'unique préversion livrée est antérieure au pipeline actuel, qui n'a jamais achevé un run de production. L'hygiène de l'historique Git, en revanche, est un point positif vérifié : 226 commits d'une discipline Conventional Commits constante, appliquée par hook et par la CI.

**Maintenabilité : forte.** Un graphe de dépendances acyclique et en couches avec un cœur zéro-dépendance ; des frontières appliquées par des tests et des gardes au moment du pack plutôt que par convention ; une source de vérité unique pour la topologie de release ; une couverture de doc XML totale ; un parc de tests discipliné (~1 024 tests, une vraie couverture par propriétés, un mocking quasi nul). Les risques honnêtes sont concentrés : une famille de surfaces à duplication jumelle (renderers, resolvers, miroirs, clones de convertisseurs) qu'un correctif peut manquer silencieusement ; deux points de couture de test manquants (l'orchestration de processus de GenDoc, les commandes de configuration de la CLI) ; et une obligation documentaire bilingue et multi-surfaces, lourde pour un mainteneur seul et aujourd'hui gardée par la seule discipline. Tous ont des correctifs peu coûteux et cohérents avec les patrons existants, recommandés au §14.

**Potentiel de référence : réel, conditionné au dernier kilomètre.** La différenciation est profonde et défendable — c'est la seule bibliothèque .NET où le catalogue d'erreurs est un contrat versionné, gaté à la release et localisé. Ce qui sépare l'état actuel du statut de référence n'est pas la qualité mais *l'achèvement du chemin d'adoption* : la projection ProblemDetails, le modèle de log livré, l'assistant de capture à la frontière, les code fixes, les premières releases via le pipeline complet, et la surface tournée vers la communauté (code de conduite, modèles d'issue, une déclaration de continuité pour un projet à *bus factor* 1). Chacun de ces éléments s'inscrit dans les patrons d'architecture et d'empaquetage existants ; aucun n'exige de revenir sur une décision consignée.
## 3. Évaluation de la vision du projet

### 3.1 La vision, telle qu'énoncée

FirstClassErrors énonce exactement une vision, et l'énonce à l'identique partout où un lecteur peut entrer dans le projet : *les erreurs comme des concepts de première classe, documentés, diagnosticables* — « Turn your errors into structured, living knowledge about your system » (`README.md`). La même phrase, dans une formulation quasi identique, apparaît dans la description du paquet NuGet (`FirstClassErrors/FirstClassErrors.csproj`), le README du paquet (`FirstClassErrors/README.nuget.md`), `CLAUDE.md`, `AGENTS.md`, `CONTRIBUTING.md`, et jusque dans la section de contexte de l'ADR-0009. Cela compte plus qu'il n'y paraît : les projets OSS à points d'entrée multiples accumulent d'ordinaire, au fil du temps, deux ou trois auto-descriptions légèrement divergentes, et l'auto-description divergente est le premier symptôme d'une dérive de périmètre. Ici, il y a une phrase, et elle a tenu.

La vision est ensuite décomposée en cinq principes de conception explicites (`doc/handwritten/for-users/DesignPrinciples.en.md`), chacun clos par une clause **« Conséquence »** qui nomme un comportement d'API concret et vérifiable :

1. *Une erreur est une situation reconnue* → une fabrique par situation précise, identifiée par un `ErrorCode` stable.
2. *L'erreur est le modèle ; l'exception est un transport* → la même `Error` circule sous la forme `throw error.ToException()` ou sous la forme `Outcome<T>.Failure(error)`.
3. *L'information publique et l'information interne doivent rester séparées* → le modèle à trois messages, appliqué par un builder par étapes.
4. *La documentation a sa place à côté du comportement* → `[DocumentedBy]` relie chaque fabrique à une documentation structurée dans la même classe ; le catalogue est généré depuis le code.
5. *Les diagnostics sont des hypothèses, pas des verdicts* → les entrées de diagnostic décrivent des états observables avec un `ErrorOrigin`, pas des accusations.

Parce que chaque principe s'achève sur une conséquence falsifiable, la philosophie est *testable* plutôt qu'aspirationnelle — et cet audit l'a testée.

### 3.2 L'implémentation tient-elle la vision ? Largement, oui — mécaniquement

La propriété frappante de ce dépôt est que les principes ne sont pas appliqués par convention ni par vigilance de revue ; ils sont appliqués par le système de types et par la chaîne d'outillage :

* **Le principe 3 est structurellement incontournable.** Chaque catégorie d'erreur concrète expose un `Create(...)` statique qui capture l'information *interne* (code + message de diagnostic) et renvoie un `PublicMessageStage<TError>` — un type qui, délibérément, n'est **pas** une `Error` et ne peut pas être utilisé là où on en attend une. La seule façon d'obtenir l'erreur finie est `WithPublicMessage(shortMessage, detailedMessage?)`. Une erreur ne peut donc pas exister sans que les deux publics aient été servis (`PublicMessageStage.cs`, `DomainError.cs:37-66`). Il n'y a pas d'étape `Build()` à oublier.
* **La taxonomie des docs est la taxonomie des types.** La table de composition d'`ErrorTaxonomy.en.md` (une `DomainError` n'imbrique que des `DomainError` ; une `PrimaryPortError` imbrique des `DomainError`/`PrimaryPortError` ; l'`InfrastructureError` de base accepte n'importe quelle `Error`) est portée par les types de paramètres des constructeurs et par les collections typées `PrimaryPortInnerErrors`/`SecondaryPortInnerErrors` — une erreur mal étiquetée ou mal composée est une erreur de compilation, pas un commentaire de revue (`DomainError.cs:20-24`, `PrimaryPortInnerErrors.cs:39-68`). Les erreurs de port fixent leur `InteractionDirection` dans leurs constructeurs et *calculent* la `Transience` agrégée à partir de leurs erreurs internes via un unique `TransienceCalculator` faisant autorité.
* **Le principe 4 — la « documentation vivante » — est livré de bout en bout et auto-appliqué (dogfooding).** GenDoc documente ses *propres* échecs au moyen du DSL de la bibliothèque (l'ADR-0009 consigne avec franchise qu'avant cela, le dépôt « had no real internal consumer of the model »). Le catalogue généré sous `doc/generated/gendoc/` est effacé et régénéré par la CI à chaque pull request (`gendoc-docs.yml`) et commité via la revue normale, et le catalogue est un *contrat versionné* : `release.yml` calcule `fce catalog diff` face à `errors-baseline.json` et refuse de publier une release contenant un changement cassant du catalogue sans incrément de version majeure (ADR-0010). Peu de projets font de « les docs ne dérivent jamais » un gate de release appliqué, plutôt qu'un slogan.
* **La boucle à la compilation est fermée par 18 analyseurs Roslyn (FCE001–FCE018)** qui appliquent précisément les conventions de la vision — codes d'erreur dupliqués ou instables, câblage de la documentation (`[DocumentedBy]`/`[ProvidesErrorsFor]`), qualité du contenu de la documentation, données de contexte sensibles ou surdimensionnées — et ils sont livrés *à l'intérieur* du paquet principal (`analyzers/dotnet/cs`), si bien que l'affirmation « checked at build time » du README est vraie sans la moindre installation supplémentaire.
* **Un sixième principe, non énoncé, est implémenté avec un soin inhabituel : « fabriquer une erreur ne lève jamais ».** Parce que les erreurs se construisent sur des chemins d'échec (dans un `catch`, pendant la journalisation), la construction ne lève jamais d'exception secondaire qui masquerait le problème d'origine : un code `null` se dégrade en `#UNSPECIFIED` ; des messages obligatoires manquants deviennent des sentinelles visibles (`#MISSING_DIAGNOSTIC_MESSAGE`, `#MISSING_SHORT_MESSAGE`) *et* l'omission est enregistrée comme contexte interrogeable ; un délégué `configureContext` qui lève est capturé comme donnée sous `#CANNOT_BUILD_ERROR_CONTEXT`, en préservant les entrées qu'il avait ajoutées avant d'échouer (`Error.cs:36-142`). C'est une réflexion de niveau exploitation que les bibliothèques de résultats courantes n'ont tout simplement pas.

Le projet est aussi honnête sur ses propres limites — un trait qui vaut d'être nommé parce qu'il est rare. `WhenNotToUseFirstClassErrors.en.md` nomme des contre-indications concrètes (prototypes, petits utilitaires, bibliothèques bas niveau, boucles chaudes, systèmes qu'on ne possède pas) et se termine par « The goal of this library is not to make every exception richer. » `ComparisonWithOtherLibraries.en.md` est daté, refuse d'établir un classement, concède des scénarios favorables à ErrorOr ou FluentResults, et admet que son scénario motivant « naturally plays to its strengths ». La FAQ se demande elle-même « Is this too heavy for a simple application? ». Pour un projet aux ambitions de référence, cette posture de confiance méritée est exactement la bonne.

### 3.3 Où se situe réellement l'écart entre le dire et le faire

Les écarts trouvés sont peu nombreux, petits et — c'est révélateur — surtout *méta* : la rare dérive du projet se loge dans le texte rédigé à la main à propos du système, jamais dans le système lui-même.

1. **La vitrine contredit la promesse anti-dérive (Moyen).** `README.nuget.md` annonce « 16 Roslyn analyzers in the box (FCE001–FCE016) » alors que la base de code en livre 18 (FCE001–FCE018, dont FCE017 *SensitiveDataInErrorContext* et FCE018 *OversizedErrorContextValue*, tous deux documentés sous `doc/handwritten/for-users/analyzers/`). Le même fichier promet que le catalogue « never drifts from the deployed system ». L'ironie est instructive : le seul artefact hors du pipeline de génération est celui qui a dérivé. Le §14 recommande à la fois le correctif d'une ligne et un garde-fou de comptage à la manière de `tools/`, pour que cette classe de dérive rejoigne l'ensemble vérifié par machine.
2. **La doctrine du « ne lève jamais » est invisible dans la documentation rédigée à la main (Moyen).** La doctrine et ses sentinelles ont des symptômes observables en production — un opérateur peut rencontrer `#MISSING_SHORT_MESSAGE` dans une réponse d'API ou `#CANNOT_BUILD_ERROR_CONTEXT` dans un log — et pourtant aucune page sous `doc/handwritten/for-users/` ne les mentionne (elles ne vivent que dans les docs XML). Un utilisateur qui cherche dans la documentation la chaîne sentinelle qu'il vient de voir ne trouve rien. C'est en pratique un sixième principe de conception que la page Design Principles omet.
3. **Le périmètre du changelog contredit le train de release (Faible).** `CHANGELOG.md` déclare couvrir « FirstClassErrors and FirstClassErrors.Testing », mais le train `lib` empaquette trois paquets en lockstep — les deux nommés plus `FirstClassErrors.RequestBinder` (`tools/packaging/pack.sh`, `tools/trains.sh`) — si bien que les changements côté binder n'ont aujourd'hui aucun emplacement de changelog déclaré, tandis que `RequestBinder.en.md` dit que le binder est livré « on the same release train … at the same version ».
4. **Une dérive de périmètre délibérée et gouvernée : Dummies.** Une bibliothèque générique de données de test, dont l'audience dépasse explicitement ce projet, vit dans le dépôt de la bibliothèque d'erreurs. Point crucial, la dérive est *bornée par ADR* (l'ADR-0011 consigne la décision, ses coûts et ses déclencheurs d'extraction), appliquée par machine (un test d'architecture plus un garde-fou nuspec au moment du pack prouvent que Dummies ne référence aucun assemblage FirstClassErrors), et honnête. Elle n'en dilue pas moins l'identité mono-produit d'un dépôt pré-1.0 ; les §10 et §15 y reviennent.
5. **L'ambition devance aujourd'hui la maturité démontrée (informatif, pas un défaut).** Le dépôt présente au présent la provenance des releases, les SBOM et une comparaison face à des bibliothèques établies, alors que sa réalité de release tient en une préversion : `0.1.0-preview.1` du cœur et de Testing sur nuget.org, sous un unique tag distant (`lib-v0.1.0-preview.1`) antérieur à l'actuel pipeline à trois trains, les trois changelogs n'affichant encore qu'une section `[Unreleased]` vide et la section d'installation du README pointant vers deux paquets (`FirstClassErrors.RequestBinder`, l'outil `fce`) qui ne sont pas encore sur NuGet. La machinerie de release existe et est testée en dry-run — mais, pour la question posée par le conseil, l'écart entre vision et adoption est l'honnête réponse du moment : la cohérence est prouvée ; le *statut de référence* est une candidature, pas encore un fait.

### 3.4 Verdict

C'est l'une des correspondances vision-implémentation les plus cohérentes intérieurement qu'une petite bibliothèque puisse exhiber. La philosophie n'est pas un texte marketing ; c'est un ensemble de mécanismes appliqués — builders par étapes, règles de composition typées, règles d'analyseurs, un gate de catalogue versionné — chacun traçable jusqu'à un principe énoncé. Les écarts résiduels entre le dire et le faire sont des défauts de surface documentaire qui se mesurent en phrases, pas en architecture. L'unique tension stratégique (un second produit, Dummies, dans le dépôt du produit-phare) est explicitement gouvernée par le propre processus de décision du projet, au lieu de dériver en silence.
## 4. Forces

Les preuves détaillées vivent dans les sections par domaine ; cette liste nomme les forces qui définissent le projet, approximativement dans l'ordre de leur contribution à la question posée.

1. **Une cohérence vision-implémentation mécanique, et non rhétorique (§3).** Une phrase de vision, cinq principes portant chacun une conséquence falsifiable, et un système de types + une chaîne d'outils qui les font respecter : le builder par étapes rend la séparation public/interne incontournable ; les collections d'erreurs internes typées font des règles de composition de la taxonomie des faits établis à la compilation ; le catalogue est régénéré par la CI et soumis à un gate à la release ; 18 analyseurs ferment la boucle à la compilation. Très peu de projets, quelle que soit leur taille, savent tracer chaque principe énoncé jusqu'à un mécanisme d'application.

2. **Un fossé concurrentiel en quatre volets, inoccupé (§13.1).** Catalogue d'erreurs généré, versionné et localisé ; détection de changements cassants du contrat d'erreur ; analyseurs livrés ; et binder de frontière réutilisant les fabriques — aucun d'ErrorOr, FluentResults, OneOf, LanguageExt, CSharpFunctionalExtensions, Ardalis.Result ou DotNext n'offre l'un quelconque des quatre. La différenciation est profonde et délibérée, pas du remplissage de liste de fonctionnalités.

3. **La doctrine « fabriquer une erreur ne lève jamais » (§9.2).** Originale, cohérente, implémentée de bout en bout avec des sentinelles visibles et une dégradation consignée dans le contexte — une conception de niveau opérationnel pour le chemin d'échec du chemin d'échec, que les bibliothèques de résultats grand public ne traitent tout simplement pas.

4. **Une gouvernance par ADR d'une qualité réellement inhabituelle (§6).** 26 ADR, toutes jugées solides ; zéro doublon ni contradiction, les relations de quasi-recouvrement étant explicitement désambiguïsées dans le texte ; une exception de migration éditoriale bornée et traçable (ADR-0024) exécutée dans les règles de l'art ; des décisions fondées sur des preuves (le report adossé à des benchmarks de l'ADR-0023 est un modèle de raisonnement pré-v1) ; et une conformité d'implémentation vérifiée pour 25 ADR sur 26. La discipline de processus — des décisions en une phrase, un contexte limité aux faits, une séparation décision/spécification munie d'un test falsifiable — dépasse la plupart des pratiques ADR industrielles.

5. **Des frontières appliquées par machine, partout (§8.1, §10.2).** Tests d'architecture + gardes nuspec au moment du pack pour les frontières de paquet ; tests de verrouillage de la résolution de surcharge pour les décisions d'API ; consommateurs dogfood de l'artefact packé prouvant que l'analyseur se charge sur le SDK plancher et que le bon asset par TFM est livré ; une philosophie de CI à preuve positive (grepper la preuve, ne pas se fier aux codes de sortie). Cela ferme la brèche « build vert, artefact cassé » que presque chaque projet OSS laisse ouverte — la pratique la plus transférable de tout le dépôt.

6. **Une base de qualité zéro défaut, vérifiée.** Le build exécuté par l'audit lui-même : 0 avertissement, 0 erreur ; 1 143/1 143 tests réussis sur 10 projets. La qualité des tests est à la hauteur de leur quantité (§8.2) : ~1 024 méthodes de test dans un style maison discipliné, de véritables tests par propriétés encodant les lois de monade et les invariants du binder, des tests snapshot appariés à un gate HTML structurel, un recours au mock quasi nul, et des tests qui consomment en dogfooding les propres paquets Testing/Dummies du projet. L'hygiène de l'historique git est du même niveau : 226 commits de Conventional Commits cohérents, appliqués par hook et par la CI.

7. **Une documentation qui pratique la thèse qu'elle vend (§11).** ~40 guides bilingues avec exactement une phrase d'API périmée trouvée dans tout l'ensemble anglais ; un miroir français de 185 fichiers avec deux dérives sur ~9 500 lignes miroir ; des pages de catalogue générées, régénérées et diffées à chaque PR. Des documents de cadrage honnêtes (WhenNotToUse, un comparatif daté sans classement, une FAQ qui s'ouvre sur « You can [just use exceptions] ») construisent exactement la crédibilité dont un projet de référence a besoin.

8. **Une ingénierie de chaîne d'approvisionnement et de release au-dessus de la catégorie de poids du projet (§11.5, §10.2).** 41/41 actions épinglées par SHA, permissions au moindre privilège, publication de confiance OIDC (trusted publishing), provenance SLSA, SBOM embarqués et assertés, défenses contre la dependency confusion, une stratégie de répétition de release (dry-run) à deux niveaux partageant le vrai script de pack, et une automatisation LLM consciente de l'injection, soumise à validation humaine et qui se dégrade gracieusement.

9. **Une ingénierie de sûreté à la compilation dans le binder (§8.2).** La conception jeton + scope ref struct qui rend les valeurs non validées *inatteignables à la compilation* est une application authentiquement nouvelle de la sémantique C# à la sûreté d'API, et le travail de performance qui l'entoure est mesuré, honnêtement rapporté et consigné en ADR.

10. **Une autolimitation honnête.** Le projet documente quand ne pas l'utiliser, concède nommément les forces de ses concurrents, date ses revues comparatives, et préfère différer des fonctionnalités (sélecteurs par délégué, gRPC, publishers) avec un raisonnement consigné plutôt que de les accumuler. La retenue est une force quand l'objectif est le statut de référence.

11. **Un modèle gouverné de développement assisté par IA.** ~60 % des commits sont co-signés par des agents, sous une règle explicite « l'humain décide » (les agents rédigent les ADR en Proposée et ne fusionnent jamais ; le mainteneur détient seul l'autorité sur les statuts et les fusions), une automatisation LLM consciente de l'injection, consultative et qui se dégrade gracieusement, et une couche d'instructions d'agents en cinq fichiers dont la synchronisation inter-fichiers a été vérifiée quasi parfaite. La plupart des projets n'ont aucun modèle de gouvernance pour cela ; celui-ci est assez articulé pour mériter une publication (ses lacunes — une divulgation manquante à destination des utilisateurs et un prompt non durci — sont listées au §5.6).
## 5. Faiblesses

Chaque point ci-dessous a survécu à une passe de vérification contradictoire ou s'appuie sur des preuves directes au niveau des fichiers ; aucun n'est spéculatif. Ils sont regroupés par thème et classés selon leur poids face à la question posée par le conseil. Le motif frappant : **presque rien ici n'est un défaut de conception** — les faiblesses sont des surfaces de dernier kilomètre manquantes, des risques de séquencement et des bords maintenus à la main d'un système par ailleurs vérifié par machine.

### 5.1 Stratégie / produit

1. **Le dernier kilomètre HTTP manque alors que le produit le promet (§13.2).** Le catalogue montre aux clients d'API des réponses RFC 9457 concrètes avec des types `urn:problem:{service}:{code}` ; aucun code d'exécution ne les produit. C'est une incohérence philosophique, pas seulement une lacune de confort : les exemples réseau documentés et les réponses réelles peuvent dériver librement — précisément le mode de défaillance que le projet existe pour éliminer.
2. **Les décisions fondatrices ne sont pas consignées (§6.5).** 17 des 26 ADR gouvernent les paquets périphériques ; le modèle cœur, la cible netstandard2.0, la règle classe-plutôt-que-struct, le schéma analyseurs/ID, l'approche par réflexion de GenDoc et la politique bilingue n'ont aucun dossier de décision — si bien que le contrôle par PR de l'ADR-0004 *ne peut pas se déclencher* sur les invariants les plus importants du projet.
3. **La réalité des releases diverge du récit de release documenté.** Une préversion a été publiée (cœur + Testing en `0.1.0-preview.1`, tag distant `lib-v0.1.0-preview.1`) — mais elle est antérieure au pipeline actuel à trois trains (la seule GitHub Release est un *brouillon* sous l'ancien schéma `v*`, sans attestation publiée vérifiable par le consommateur), le workflow de release documenté n'a jamais achevé une exécution de production, et les trois changelogs ne contiennent encore qu'une section `[Unreleased]` vide malgré la préversion publiée (l'affirmation « Keep a Changelog » n'a aucun historique consigné, et rien dans `release.yml` ne promeut `[Unreleased]` au moment de la coupe). Le plus visible pour l'utilisateur : **la section d'installation du README prescrit `dotnet add package FirstClassErrors.RequestBinder` et `dotnet tool install --global FirstClassErrors.Cli` — aucun des deux paquets n'existe sur NuGet** (vérifié : BlobNotFound). Le statut de référence est une candidature, pas encore un fait, et plusieurs risques ci-dessous (ID non réclamés, Dummies embarquée) sont ouverts *parce que* les trains restants n'ont pas été publiés.

### 5.2 Risques de séquencement dans l'écosystème (§10.3)

4. **`FirstClassErrors.Testing` embarque une `Dummies.dll` non publiée dans son `lib/`** — version invisible pour NuGet, future collision de même identité quand Dummies publiera, et consommateurs net8+ recevant silencieusement la surface Dummies netstandard2.0 de niveau inférieur. Documenté comme temporaire ; risqué tant que la fenêtre est ouverte.
5. **L'identifiant de paquet générique `Dummies` n'est pas réclamé sur nuget.org** alors que son train de release, ses garde-fous et son changelog existent déjà — une fenêtre de squattage ouverte sur une identité que deux ADR traitent comme durable.
6. **Une installation binder-seul perd silencieusement les analyseurs** (les dépendances nuspec excluent `Analyzers` par défaut), et la documentation ne le dit pas — le consommateur qui documente une surface d'erreurs de frontière est précisément celui qui n'a pas les règles pour la faire respecter.
7. **Deux décisions durables ne vivent que dans des commentaires :** le contrat de lockstep du train lib, et l'`InternalsVisibleTo` cœur→Testing qui rend le lockstep *définitivement obligatoire*. Les deux passent le propre test de significativité d'ADR du projet.

### 5.3 Lacunes d'application dans la propre thèse du projet

8. **Un `Outcome` ignoré ne produit aucun diagnostic (§12.3)** — le mode de défaillance central de la bibliothèque (une erreur perdue) reste sans garde-fou, alors que le cas plus rare du `ToException()` ignoré a sa règle.
9. **Les 18 diagnostics sont tous en signalement seul** — aucun fournisseur de code fix, si bien que le boilerplate de documentation qui constitue la principale taxe d'adoption de la bibliothèque ne reçoit aucune aide d'échafaudage.
10. **Le texte de documentation localisé en ressources échappe à toutes les règles de qualité de contenu** (les analyseurs fondés sur les littéraux ne peuvent pas le voir ; aucun `fce lint` ne referme la brèche), et rien ne documente la limitation.
11. **Le harnais de test des analyseurs n'asserte jamais que les extraits compilent** — les tests négatifs peuvent réussir par vacuité si l'API cœur dérive ; les assertions épinglent rarement les emplacements ou les arguments de message.
12. **`DocumentationContractVersionAttribute` documente un contrôle de version côté générateur qui n'existe pas** — un mainteneur s'appuierait sur un garde-fou qui n'est pas là.

### 5.4 Vitrines et bords de documentation (§11, §12.1)

13. **Les deux README de nuget.org sont faux de façon visible pour l'utilisateur :** celui du cœur revendique 16 analyseurs (18 sont livrés) ; celui de la CLI documente une arborescence de commandes inexistante et omet entièrement les commandes de catalogue.
14. **Dérive des pages-hub :** le README et la DocumentationMap omettent chacun six guides ; les projets d'exemple compilés et testés par snapshot ne sont liés presque nulle part ; `DeterministicTesting` (EN+FR) documente une API de semage supprimée — le seul endroit où l'on demande aux utilisateurs d'écrire du code qui ne compile pas.
15. **Le miroir français présente deux dérives confirmées** (lignes de scopes de commit `binder`/`dummies` manquantes ; 20 ancres de pied de page mortes), et la politique bilingue énoncée couvre 1 fichier tandis que la pratique en couvre ~120, sans aucun outillage de synchronisation.
16. **La documentation mainteneur est en retard sur le pipeline :** 3 des 15 workflows ne sont pas documentés ; `CONTRIBUTING.md` dit encore que le scope de commit « carries no versioning weight » alors que l'outillage de release *écarte silencieusement les commits sans scope de l'enregistrement de release* — la contradiction face aux contributeurs la plus lourde de conséquences de l'audit.

### 5.5 Dettes de niveau architecture (§8.3)

17. **Risques de duplication jumelle :** le rendu d'exemples RFC 9457 dupliqué entre deux moteurs de rendu ; la logique de résolution de source de la CLI dupliquée en deux styles divergents avec des chaînes identiques à l'octet près ; le miroir à 26 entrées `Any`/`AnyContext` ; les constructeurs d'`Error` tripliqués ; des paires convertisseur/erreur-interne quasi clonées.
18. **Deux points de couture de test manquants :** l'orchestration de processus de GenDoc (les branches timeout/kill/sortie-corrompue — la source de la moitié du catalogue `GENDOC_` — validées seulement par le chemin nominal de la CI) et les commandes config/renderer de la CLI, couplées à la console et dépourvues de tout test. Rien nulle part n'exerce la vraie surface argv de la CLI, qui est pourtant son contrat de compatibilité déclaré.
19. **Un verrou par requête et une écriture partagée sur le chemin le plus chaud du binder** (le getter de `RequestBinderOptions.Default`) — incohérent avec le travail d'optimisation sous-microseconde documenté tout autour ; un chemin rapide à double vérification préserve exactement la sémantique de l'ADR-0017.
20. **Points de friction d'API (§9.3) :** le contrat d'extensibilité de la taxonomie à moitié ouvert ; des membres de symétrie manquants (un `Then` non générique projetant la valeur ; le portage de `Create` à erreur interne unique) ; le mode de collision brutal du registre de clés de contexte global au processus, avec trois conventions de nommage concurrentes dans les sources canoniques ; le `[NotNullWhen]` manquant sur `TryGet`.

### 5.6 Surface communauté, cycle de vie et processus (constats de la passe de complétude)

Ces constats sont issus d'une passe de complétude dédiée aux aspects du dépôt qu'aucun sous-système ne possède ; chacun a été vérifié face à l'arborescence ou aux registres en ligne.

* **Un *bus factor* de 1 sans aucune disposition de continuité.** Les 226 commits sont tous du seul mainteneur ou d'agents IA sous sa direction (git shortlog : 90 humains / 136 co-signés par agent) ; il n'y a ni CODEOWNERS, ni GOVERNANCE, ni déclaration de succession ou de continuité d'accès. Pour une candidature au statut de « référence », c'est un critère standard de conseil — et l'excellente documentation de processus du dépôt le rend *plus* survivable que la plupart des projets solo, mais rien ne le dit.
* **Le profil communautaire GitHub est incomplet :** pas de CODE_OF_CONDUCT, pas de modèles d'issue, pas de SUPPORT/FUNDING ; CONTRIBUTING ne référence jamais de code de conduite ; et la page publique des issues semble restreinte alors que le README invite les utilisateurs à en ouvrir — un décalage qui mérite d'être résolu dans un sens ou dans l'autre. Le modèle de développement assisté par IA, minutieusement gouverné *en interne* (AGENTS.md, trailers Co-Authored-By, branches claude/*), n'a aucune divulgation face aux utilisateurs dans le README ou CONTRIBUTING.
* **La couche de revue par IA a une surface non durcie.** Le jeu d'instructions d'agents en cinq fichiers (CLAUDE.md, AGENTS.md, `code_review.md`, deux prompts `.github/`) est remarquablement bien synchronisé d'un fichier à l'autre, mais `code_review.md` — une spécification de revue Conventional Comments de 414 lignes, bien construite — n'a pas le durcissement anti-injection de prompt « traiter le contenu analysé comme des données » que portent ses deux prompts frères, rien dans le dépôt ne documente ce qui le consomme ou s'y lie réellement, son nom en minuscules à la racine enfreint les propres conventions du dépôt, et le texte de règles délibérément tripliqué (CLAUDE/AGENTS/code_review) n'a aucun contrôle de synchronisation alors que `tools/` héberge cinq autres vérificateurs de cohérence.
* **Le chemin cœur `Outcome`/`Error` n'est pas mesuré.** Le binder a un harnais de benchmark modèle qui alimente l'ADR-0023 ; rien d'équivalent ne vise le cœur, alors que la politique classe-plutôt-que-struct repose sur l'affirmation empirique que « error/result paths are not hot loops », que la page de comparaison n'a aucune ligne performance/allocations face à ErrorOr fondé sur des structs, et que l'avertissement boucle-chaude de `WhenNotToUse` nomme le mauvais coût (la création d'exception, que le chemin Outcome n'encourt pas — les vrais coûts par appel sont les allocations d'`Outcome`/`Error`).
* **Broutille d'empaquetage :** l'`icon.png` de 919 Ko est packé dans chaque paquet NuGet — un gain de taille facile pour la première impression au téléchargement.

### 5.7 La pratique en retard sur la conception

21. **La propriété-phare de déterminisme n'a aucun kilométrage en conditions réelles :** `Reproducibly` n'est utilisé nulle part dans les grandes suites de tests du dépôt lui-même, et l'adaptateur xUnit anticipé n'a jamais été construit — un échec dû à une valeur coïncidente dans la suite principale est aujourd'hui irrejouable.
22. **La consommation d'erreurs est sous-démontrée :** aucun exemple n'utilise `Recover`, le pipeline asynchrone, l'`Outcome` non générique ou `InfrastructureError` ; les tests des exemples fabriquent leurs assertions à la main au lieu de consommer `FirstClassErrors.Testing` en dogfooding ; et le guide `UsagePatterns` contredit l'exemple livré `Amount` sur le patron de dérivation phare du projet lui-même.
## 6. Revue des ADR

Le conseil a demandé que les ADR soient traitées comme l'un des aspects les plus importants de cette revue. Elles l'ont été : chacune des 26 ADR a été auditée individuellement — décision résumée, solidité mise à l'épreuve, conformité de l'implémentation vérifiée face à l'arbre actuel, fraîcheur jugée, jumelle française vérifiée par sondage — et le corpus a été revu dans son ensemble à la recherche de doublons, de contradictions, d'obsolescence et de décisions manquantes.

### 6.1 Le processus ADR lui-même

Le corpus vit sous `doc/handwritten/for-maintainers/adr/` : 26 ADR anglaises, 26 jumelles françaises, un README d'index et un gabarit abondamment annoté. Statuts : 23 Acceptées, 2 Remplacées (0006→0026, 0016→0018), 1 Proposée (0025). Les 26 ont toutes été rédigées entre le **2026-07-10 et le 2026-07-19** par un décideur unique (« Reefact ») — un corpus très jeune et dense, produit pour l'essentiel en réponse aux issues de revue de conception sur les paquets RequestBinder et Dummies/Testing.

La définition du processus est d'une rigueur inhabituelle, et — plus rare — le corpus la suit réellement :

* **Une séparation décision/spécification munie d'un test falsifiable.** Le README pose qu'une ADR consigne une décision, pas une spécification, avec le test décisif « si l'implémentation changeait mais que la décision tenait, l'ADR ne devrait pas avoir besoin d'être modifiée », et le gabarit embarque des garde-fous par section. Les ADR postérieures à la migration s'y conforment visiblement, déléguant la mécanique par liens vers `specifications/adr-implementation-reference.md` et la référence du workflow.
* **Zéro décision dupliquée, zéro décision contradictoire.** Chaque paire qui aurait pu se chevaucher énonce elle-même sa relation dans le texte : l'ADR-0017 « deliberately revisits one alternative rejected by ADR-0012 … does not change ADR-0012's decision » ; l'ADR-0018 remplace la 0016 avec des liens dans les deux sens ; l'ADR-0022 « refines » la 0002 avec des avertissements réciproques ; l'ADR-0021 annote les 0007/0012/0017 d'un « decision remains valid; illustrative API shape updated ». La plupart des bases d'ADR accumulent des chevauchements silencieux ; celle-ci nomme à chaque fois le type de la relation.
* **Une exception de gouvernance menée dans les règles.** L'ADR-0024 a autorisé une migration éditoriale *unique et bornée* qui a déplacé la mécanique d'implémentation hors de 13 ADR acceptées vers une référence d'implémentation bilingue — et elle a été exécutée de façon traçable : chaque ADR affectée porte le pied de page d'autorisation, les sept ancres référencées se résolvent toutes, et l'historique git consigne même une collision de numérotation entre deux branches parallèles (toutes deux revendiquaient ADR-0023), résolue par un commit de renumérotation documenté (`7d74b82`).
* **L'hygiène d'index et de statut est exacte**, jusque dans une sémantique honnête : l'unique ADR Proposée (0025) est affichée comme Proposée plutôt que promue en silence, et les ADR remplacées lient leur successeur depuis la ligne de statut.

### 6.2 Verdicts ADR par ADR

Chaque ADR a été jugée sur sa solidité (la décision est-elle juste, bien argumentée, précise ?), sur la conformité actuelle de l'implémentation et sur sa fraîcheur. Le résultat sur l'ensemble du corpus est remarquable et mérite d'être dit sans détour : **26/26 solides, 25/26 pleinement conformes (une partielle), 24/26 actuelles** — les deux entrées « obsolètes » étant précisément les deux ADR Remplacées, ce qui est la forme *saine* d'obsolescence (remplacée par un successeur, jamais modifiée en place).

| ADR | Titre (abrégé) | Solidité | Conformité | Fraîcheur |
|---|---|---|---|---|
| 0001 | Verrouiller le plancher Roslyn de l'analyseur (4.8.0) | Solide | Conforme | Actuelle |
| 0002 | Fixer le plancher du runtime d'outillage à la plus ancienne LTS (net8.0) | Solide | Conforme | Actuelle *(remplacement à échéance ~nov. 2026)* |
| 0003 | Unifier le mapping des valeurs d'Outcome sous `Then` | Solide | Conforme | Actuelle |
| 0004 | Vérifier chaque PR face à la base d'ADR | Solide | Conforme | Actuelle |
| 0005 | Nom de fabrique nu = retour d'Outcome ; `OrThrow` = levée d'exception | Solide | Conforme | Actuelle |
| 0006 | Source de valeurs arbitraires semable *(remplacée par la 0026)* | Solide (pour son moment) | Conforme (remplacement dans les règles) | Obsolète |
| 0007 | Terminaux du binder nommés `New` et `Create` | Solide | Conforme | Actuelle |
| 0008 | Types valeur nullables via des surcharges contraintes à struct | Solide | Conforme | Actuelle |
| 0009 | Les échecs d'outillage comme erreurs de première classe | Solide | Conforme | Actuelle |
| 0010 | Le catalogue GenDoc comme contrat versionné | Solide | Conforme | Actuelle |
| 0011 | Dummies comme paquet autonome dans le dépôt | Solide | Conforme | Actuelle |
| 0012 | Figer les options du binder avant le début du binding | Solide | Conforme | Actuelle |
| 0013 | Collections distinctes : gate par cardinalité, sinon tirage borné | Solide | Conforme | Actuelle |
| 0014 | Liste requise = présence, pas cardinalité | Solide | Conforme | Actuelle |
| 0015 | Plafonner `Any.Combine` à l'arité huit | Solide | Conforme | Actuelle |
| 0016 | Codes d'erreur structurels configurables *(remplacée par la 0018)* | Solide | Conforme | Obsolète |
| 0017 | Options de binder par défaut à l'échelle de l'application, gel à la première utilisation | Solide | Conforme | Actuelle |
| 0018 | Regrouper code d'erreur structurel + messages | Solide | Conforme | Actuelle |
| 0019 | Documenter les erreurs de binder surchargées dans le catalogue du consommateur | Solide | Conforme | Actuelle |
| 0020 | Matérialiser les dummies uniquement via `Generate()` | Solide | Conforme | Actuelle |
| 0021 | Arguments hors DTO comme pairs, entrée agnostique de la source | Solide | Conforme | Actuelle |
| 0022 | Plancher .NET Framework des bibliothèques à 4.7.2 | Solide | Conforme | Actuelle |
| 0023 | Conserver les sélecteurs en arbres d'expression pour le binder v1 | Solide | Conforme | Actuelle |
| 0024 | Refactoring éditorial unique des ADR acceptées | Solide | Conforme | Actuelle (consommée) |
| 0025 | Génération de chaînes par sous-ensemble regex *(Proposée)* | Solide | Conforme | Actuelle — **le statut est en retard sur le code** |
| 0026 | Rebaser les valeurs arbitraires de Testing sur Dummies | Solide | **Partielle** | Actuelle |

### 6.3 Points saillants : les décisions les plus fortes

Quelques ADR méritent d'être nommées comme modèles, parce qu'elles montrent ce que le processus produit à son meilleur :

* **L'ADR-0003 (unifier map/bind sous `Then`)** est le dossier d'API cœur le mieux écrit : la mécanique C# de meilleure surcharge qui garantit l'aplatissement est énoncée précisément dans le Contexte, le fait pré-release qui rend la rupture gratuite est consigné, la section Négatif concède honnêtement ce qui est perdu (une chaîne ne télégraphie plus quelles étapes peuvent échouer), et le risque spéculatif (une future version du langage modifiant la résolution) est couvert par des *tests de verrouillage* plutôt que par l'espoir.
* **L'ADR-0005 (nommage `OrThrow`)** contient un recadrage réellement pénétrant : la convention BCL qui mérite d'être conservée n'est pas le mot `Try` mais la règle « la variante qui s'écarte du défaut dominant porte le marqueur » — appliquée à une bibliothèque dont le défaut est l'`Outcome`. La seule objection de l'audit tient cependant : l'ADR est muette sur les fabriques levantes *isolées* sans jumelle Outcome (p. ex. `ErrorCode.Create` lève), laissant implicite le périmètre de la convention.
* **L'ADR-0023 (conserver les sélecteurs en arbres d'expression)** est pilotée par les preuves de bout en bout : le coût d'expression irréductible par site d'appel est chiffré par un harnais BenchmarkDotNet versionné dans le dépôt (~488 o / ~416 ns par propriété ; plafond du délégué mis en cache 1 ns / 0 o), l'atténuation (hisser les sélecteurs en champs statiques supprime ~85–90 % du temps) est mesurée, et le chemin rapide par délégué est correctement différé comme décision post-v1 *additive* plutôt que de doubler la surface maintenant. C'est exactement le bon raisonnement avant le gel v1.
* **L'ADR-0020 (pas de conversions implicites dans Dummies)** repose sur une sémantique C# exacte (les conversions implicites définies par l'utilisateur ne participent pas sous `var`/`object`/l'inférence générique — l'opérateur était une abstraction partielle) et sur le critère classique de conception de framework (les conversions implicites doivent être bon marché, totales, référentiellement transparentes — 28 opérateurs à effets, levants, tirant de l'aléa, n'étaient rien de tout cela).
* **Les ADR-0010/0009 (catalogue comme contrat versionné ; dogfooding)** retournent la thèse du projet sur elle-même : les échecs de GenDoc utilisent le propre modèle de la bibliothèque, et un changement cassant du catalogue ne peut pas partir sans incrément majeur, appliqué mécaniquement au moment de la release — délibérément *pas* par PR, parce que casser est légitime pendant le développement. Le raisonnement sur l'*endroit* où placer le gate est une petite leçon magistrale.
* **La séquence ADR-0012→0017→0018 (options du binder)** montre une évolution de décision disciplinée : rendre l'état invalide irreprésentable (0012), puis revisiter une alternative rejetée sous des contraintes strictement plus fortes en le disant (0017), puis remplacer la surcharge code-seul par la définition groupée parce que « code et message sont un seul concept » et que le gel à la première utilisation *force* un builder évalué à l'émission pour la localisation par requête (0018).

### 6.4 Faiblesses individuelles sur lesquelles agir

Les audits ADR par ADR n'ont fait remonter aucune décision infondée, mais un ensemble cohérent de défauts au niveau du texte. Par ordre de priorité :

1. **La prémisse de risque de l'ADR-0026 est imprécise et un suivi a été manqué (la seule lacune de conformité du corpus).** Le risque principal consigné dit que le danger d'assemblage embarqué survient « precisely because Dummies types appear in Testing's public API » — or aucun membre public de `FirstClassErrors.Testing` n'expose aujourd'hui un type Dummies ; le vrai danger est que des consommateurs compilent contre la `Dummies.dll` embarquée dans `lib/` (le README enjoint `Dummies.Any.*`). Même danger, mécanisme différent — à consigner correctement dans la référence d'implémentation. Plus concrètement : le suivi de lockstep de la doc a été manqué sur `DeterministicTesting.en.md`/`.fr.md` (ligne ~163), qui décrit toujours l'API de graine *remplacée* (`UseAny()` ne prend pas de graine ; la reproductibilité vient désormais de `Dummies.Any.Reproducibly(...)`) — une contradiction, visible de l'utilisateur, avec l'API livrée, que la règle de synchronisation inscrite au CLAUDE.md du projet rend obligatoire de corriger. Enfin, la Décision promet des méthodes de fabrique exposant des générateurs `IAny<T>` « where composition is needed », et aucune n'existe encore — dormance voulue, mais la Décision se lit comme une forme livrée.
2. **La phrase de Décision de l'ADR-0017 sous-spécifie ce qui a été décidé.** Elle dit que les options gèlent « after first binding use », mais l'implémentation gèle à *toute première lecture* du getter, et « configurable once » est en réalité « pas de réassignation après la première lecture ». La référence d'implémentation est plus précise que la décision qu'elle implémente — inversant l'ordre d'autorité voulu. Une phrase de décision affûtée règle le point.
3. **L'ADR-0022 nomme des documents référencés qui ne portent pas le contenu promis.** L'ADR dit que le job Windows, les polyfills, les exclusions et la couverture preview sont « documented in the ADR implementation reference and the CI workflow reference » — aucun des deux ne les contient (la vraie documentation vit dans d'excellents commentaires de code). Rendre vrais les documents référencés plutôt que de toucher à l'ADR acceptée.
4. **La frontière de l'ADR-0024 n'est pas énumérée là où elle prétend l'être.** L'atténuation de risque dit qu'elle « authorizes only the migration identified in its references », mais les références nomment deux documents *vivants*, pas les 13 ADR affectées ni la plage de commits exécutante. Une note « registre de migration » dans la référence d'implémentation clôt le point sans modifier le texte accepté.
5. **L'ADR-0025 est Proposée alors que son implémentation est fusionnée et déjà consommée par une décision Acceptée** (l'`ErrorCodeFactory` de l'ADR-0026 appelle `Any.StringMatching`). Selon le propre protocole du projet, seul le mainteneur peut basculer un statut — c'est précisément l'état que le modèle de l'ADR-0004 existe pour empêcher de persister. Son affirmation de test surestime aussi : le garde-fou est une Theory-oracle pilotée par corpus (46 motifs face au vrai moteur .NET), pas un test par propriété.
6. **Le champ `Date` unique efface l'histoire au remplacement.** Les ADR-0006 et 0016 n'affichent plus que leur date de remplacement ; la date d'acceptation d'origine est irrécupérable depuis le dossier — un coût réel pour un corpus qui se définit comme « un journal historique ». Une évolution du gabarit (consigner les deux dates, ou un historique de statut en une ligne) règle le point pour les remplacements futurs.
7. **« Refines » est une relation que le vocabulaire de statuts ne sait pas exprimer.** Le couple ADR-0022/0002 a inauguré un lien de raffinement (y compris la suppression d'une affirmation factuelle incidente du texte d'une ADR acceptée — à la limite extérieure de l'autorisation éditoriale de l'ADR-0024) ; les deux s'affichent comme simplement « Acceptée » dans l'index. Le propre suivi de l'ADR-0024 appelait à définir de tels liens ; l'index devrait les faire apparaître.
8. **Une tension de format mineure récurrente : des phrases de Décision composées** (les 0004, 0006, 0009 et 0016 tassent deux ou trois clauses dans l'« unique phrase » que le gabarit exige) et, à l'occasion, des heuristiques de conception énoncées dans un Contexte censé s'en tenir aux faits (le « wide constructors indicate missing domain concepts » de la 0015).

### 6.5 Constats au niveau du corpus : le problème d'inversion

L'unique faiblesse structurelle du corpus est une **inversion thématique** : 17 des 26 ADR gouvernent les paquets *périphériques* — RequestBinder (10 : 0007, 0008, 0012, 0014, 0016, 0017, 0018, 0019, 0021, 0023) et Dummies/Testing (7 : 0006, 0011, 0013, 0015, 0020, 0025, 0026) — tandis que la bibliothèque cœur en reçoit exactement deux (0003, 0005), toutes deux des raffinements de nommage d'API. Les planchers de plateforme en prennent trois (0001, 0002, 0022), GenDoc deux (0009, 0010), le processus deux (0004, 0024).

**Décisions manquantes vérifiées** — les choix fondateurs qu'un futur mainteneur questionnerait le plus, dont aucun n'est consigné par une ADR :

* le **modèle Outcome/Error lui-même** et la dualité exception/outcome (pourquoi un type résultat ; pourquoi `ToException()` comme transport opt-in) — la justification existe dans `DesignPrinciples.en.md` et la FAQ, que l'ADR-0003 cite comme autorité, mais aucun *dossier de décision* n'existe ;
* la **cible netstandard2.0** des bibliothèques livrées (n'apparaît que comme contexte dans les 0002/0022, qui la présupposent toutes deux) ;
* la **règle « classe, jamais struct » pour les value objects** — le CLAUDE.md contient une justification complète, de qualité ADR, à laquelle il ne manque que le format ;
* le **schéma d'ID de diagnostic FCE et le choix d'embarquer les analyseurs dans le paquet principal** (l'ADR-0019 cite « analyzer FCE009 » comme un acquis) ;
* l'**extraction GenDoc par réflexion à l'exécution** (`Assembly.LoadFrom` + exécution des fabriques de documentation) — la justification vit aujourd'hui dans une justification de `SuppressMessage` à l'intérieur du Worker, c'est-à-dire une argumentation piégée dans un commentaire de code ;
* la **politique de documentation bilingue EN-canonique/FR-traduction** (énoncée comme convention dans le README des ADR, jamais décidée) ;
* le **contrat de lockstep du train lib et l'`InternalsVisibleTo` cœur→Testing** (§10.3).

Pourquoi cela compte, c'est la propre logique du projet retournée sur elle-même : **l'ADR-0004 fait de la base d'ADR l'artefact face auquel chaque PR est vérifiée.** Une PR qui convertirait `ErrorCode` en struct, ajouterait une dépendance d'exécution au cœur, ou scinderait le train en lockstep ne contredirait *aucune ADR acceptée* — le contrôle sur lequel le projet s'appuie ne peut pas se déclencher sur ses invariants les plus importants. La distribution se lit aujourd'hui comme « les décisions des deux dernières semaines » plutôt que comme « les décisions du projet ».

**Recommandation (Critique — la seule de ce rapport) :** rétro-consigner ~6 ADR fondatrices en statut `Proposed`, chacune avec une phrase de Contexte énonçant explicitement qu'elle consigne une décision prise avant le début de la pratique ADR, pour ne pas falsifier l'histoire. C'est bon marché (l'essentiel de la justification existe déjà dans CLAUDE.md, DesignPrinciples, la FAQ et les commentaires de code — elle demande une relocalisation, pas une invention) et cela renforce directement la boucle de gouvernance que le projet fait déjà tourner. Une esquisse :

```markdown
# ADR-0027 | Model failures as Outcome values with opt-in exception transport
**Status:** Proposed
## Context
This ADR records a decision that predates the ADR practice (first ADR: 2026-07-10).
Its rationale previously lived in DesignPrinciples.en.md (“the error is the model;
the exception is a transport”) and FAQ.en.md (the Result<T,E> discussion). …
## Decision
An operation's failure path is a first-class Outcome/Outcome<T> value; exceptions are
an opt-in transport derived from the error, never the primary channel.
```

Deux points de niveau corpus plus modestes : le règlement des ADR viole lui-même la convention bilingue qu'il énonce (pas de `README.fr.md`/`template.fr.md` sous `adr/` alors que les 26 ADR sont toutes traduites — traduire, ou consigner l'exception), et la numérotation séquentielle n'a aucun garde-fou mécanique d'unicité alors qu'une collision s'est déjà produite dans le workflow à branches concurrentes de ce dépôt (un contrôle CI trivial dans le style existant de `tools/` clôt le point).
## 7. Conformité aux ADR

La conformité a été vérifiée ADR par ADR face à l'arbre à la révision auditée — non pas en faisant confiance à la référence d'implémentation, mais en localisant les artefacts de code, de configuration et de processus gouvernés, et en les lisant. Bilan : **25 ADR sur 26 pleinement conformes ; 1 partielle (ADR-0026) ; 0 violée.** Pour les deux ADR remplacées, la conformité a été évaluée comme « remplacement exécuté dans les règles + le code suit l'ADR qui remplace », et les deux tiennent.

Sélection de preuves de vérification (l'ensemble complet figure dans les verdicts du §6.2 ; ce tableau montre les contrôles porteurs) :

| ADR | Ce qui a été vérifié dans l'arbre actuel |
|---|---|
| 0001 | `Directory.Build.props` centralise `RoslynFloorVersion=4.8.0` en source unique avec sa justification ; le csproj des Analyzers épingle `Microsoft.CodeAnalysis.CSharp` via `VersionOverride` avec un commentaire « LOAD CONTRACT — do not bump » et expose le plancher en `AssemblyMetadata` pour que le test de garde ne puisse pas diverger ; `tools/floor-check` exerce le `.nupkg` packé sur le SDK plancher épinglé avec `CS8032;AD0001` en erreurs ; l'ignore Dependabot est présent. Quatre garde-fous indépendants, tous actifs. |
| 0002 | CLI : `net8.0` + `RollForward=Major` ; Worker : `net8.0` + `RollForward=LatestMajor` (la distinction est comprise, pas copiée — un worker en `Major` se lierait à .NET 8 sur une machine ayant 8 et 10, puis échouerait à faire `Assembly.LoadFrom` d'une cible net10) ; GenDoc n'a délibérément pas de RollForward (il se lie via le runtimeconfig de la CLI) ; le job de plancher CI exécute l'outillage net8 sur le runtime plancher. |
| 0003 | Aucun `To` public ne subsiste nulle part dans la bibliothèque ; surface `Then` unifiée avec surcharges map+bind synchrones/asynchrones ; parité asynchrone sur les récepteurs `Task` ; `OutcomeThenOverloadResolutionTests` verrouille la garantie d'aplatissement. |
| 0004 | Le processus existe dans chaque artefact nommé : `AGENTS.md` (trois issues possibles, brouillon en Proposée, autorité sur les statuts réservée au mainteneur), `CLAUDE.md` (l'essentiel inliné), la section à cases « Architecture decisions » du template de PR, `adr-check.yml` + `.github/adr-check-prompt.md` (qui implémente même l'atténuation de la fatigue d'alerte : « Bias hard toward silence »). |
| 0005 | `FromKelvin`/`FromCelsius` renvoient `Outcome<Temperature>` ; `FromKelvinOrThrow`/`FromCelsiusOrThrow` sont les variantes marquées, implémentées comme `FromKelvin(k).GetResultOrThrow()` ; l'ancienne formulation « Attempts to create » a disparu. |
| 0007 | `RequestBinder.New` enveloppe le résultat d'un constructeur total dans un succès ; `Create` renvoie l'`Outcome` de la fabrique validante tel quel (aplati) ; les délégués nommés existent parce que `BindingScope` est une `readonly ref struct` et ne peut pas être argument de type d'un `Func<>`. |
| 0008 | Paires de sélecteurs non contraint vs `where TArgument : struct` sur `PropertySource` ; le chemin struct déballe via `GetValueOrDefault()` ; les éléments de liste null enregistrent `REQUEST_ARGUMENT_REQUIRED` sous des chemins indexés. |
| 0009 | `SolutionDocumentationGenerationException : DiagnosableException` avec des constructeurs à `Error` seul ; 16 codes `GENDOC_*` répartis entre les fabriques de validation de requête (`PrimaryPortError`) et de chaîne d'outils (`SecondaryPortError`), chacun câblé `[DocumentedBy]` avec des fabriques d'exemple pures ; la CI génère et asserte le propre catalogue de l'outil. |
| 0010 | `release.yml` exécute `fce catalog diff --baseline errors-baseline.json --fail-on breaking` sur le train cli et refuse la publication à moins que le majeur n'ait été incrémenté ; la baseline n'avance qu'à l'intérieur de la procédure de release ; diff informatif de PR via `gendoc-docs.yml`. |
| 0011 | La frontière de non-référence est appliquée à trois niveaux : le csproj (zéro `ProjectReference`, avec commentaire), les tests d'architecture (aucun assemblage référencé ne commence par `FirstClassErrors` ; bibliothèque standard uniquement), le garde-fou nuspec au moment du pack. |
| 0012/0017/0018 | `Bind.Request` capture `RequestBinderOptions.Default` à la construction ; `ConfiguredBind` est sealed/readonly ; les options sont immuables ; `Default` se gèle derrière une garde à la première lecture et lève en cas d'affectation tardive ; `BinderErrorDefinition` regroupe code + builder de message évalué à l'émission, avec des défauts préservant les codes/messages livrés. |
| 0013 | Gate de cardinalité immédiat net des valeurs épinglées hors domaine ; tirage borné avec budget d'épuisement et exception nommant la graine de rejeu. (`ICardinalityHint` est `internal` — plus strict que la formulation de l'ADR, un point de décision futur latent que l'audit a signalé.) |
| 0014 | L'absence dérive uniquement de `null` ; les listes vides se lient vides ; les trois convertisseurs de liste portent mot pour mot la doc XML d'épinglage. |
| 0015 | Exactement sept surcharges `Combine` (arités 2–8) ; les suppressions S107 sur les surcharges les plus larges citent « ADR-0015 » par son id dans leurs chaînes de justification — une traçabilité décision-vers-code exemplaire. |
| 0019 | Exactement quatre points de couture de documentation publics (`SampleArgumentRequired/Invalid`, `DescribeArgumentRequired/Invalid`), chacun avec une suppression FCE009 justifiée ; les propres fabriques internes du binder délèguent aux mêmes describe-builders, si bien que la prose reste fidèle par construction. |
| 0020 | Un grep sur tout le dépôt pour `implicit operator` ne trouve exactement qu'un fichier — `ErrorCode.cs` (hors périmètre) ; `IAny<T>` a pour seul membre `Generate()` et ses docs XML énoncent le contrat. |
| 0021 | Entrée non typée enveloppe-d'abord (`Bind.Request(Func<PrimaryPortInnerErrors, PrimaryPortError>)`) ; `PropertiesOf<TDto>`/`Argument`/`ArgumentList` alimentent une seule enveloppe ; les terminaux infèrent `TCommand` du délégué assembleur. |
| 0022 | Un job CI dédié `framework-floor` exécute les tests core, core-property et binder-property avec `-f net472 -p:EnableNet472Floor=true` sur un vrai CLR .NET Framework ; `build/Net472TestFloor.props` conditionne le build interne et fournit le polyfill. |
| 0023 | Exactement six méthodes de sélecteur `Expression<Func<...>>` ; aucune famille de surcharges délégué+nom n'existe ; le projet Benchmarks, avec son README, porte chaque chiffre que l'ADR cite. |
| 0024 | La référence d'implémentation existe de façon bilingue, avec des règles de maintenance encodant le contrat continu (« Do not move rationale … out of ADRs ») ; 13 ADR portent le pied de page d'autorisation ; la mécanique a atterri là où elle doit vivre. |
| 0025 | `RegexParser` implémente exactement le sous-ensemble régulier documenté ; chaque construction hors périmètre est refusée via `UnsupportedRegexException` nommant la construction et la position ; le test-oracle valide les chaînes générées contre le vrai moteur .NET. *(Conforme en tant qu'implémentation ; le **statut** est en retard — voir §6.4.5.)* |

### La seule conformité partielle : ADR-0026

Côté code, l'ADR-0026 est pleinement réalisée : le moteur privé du paquet Testing et sa façade `Any` ont disparu ; les fabriques tirent de Dummies (`ErrorCodeFactory` utilise `Dummies.Any.StringMatching("ANY_CODE_[A-Z0-9]{6}").As(ErrorCode.Create).Generate()`) ; les points de couture d'horloge et d'identifiant d'instance participent au contexte reproductible ambiant de Dummies, si bien qu'**une seule graine `Reproducibly` rejoue un run de test entier** — le bénéfice annoncé de la décision.

Le verdict partiel vient de la *périphérie* de la décision :

1. **Un suivi de doc en lockstep manqué :** `DeterministicTesting.en.md` et `.fr.md` (~ligne 163) décrivent encore l'API de semage remplacée. C'est le seul endroit du dépôt où la documentation utilisateur contredit l'API livrée, et la propre règle de synchronisation du CLAUDE.md du projet rend le correctif obligatoire.
2. **Une clause de Décision dormante :** aucune fabrique n'expose encore la méthode promise renvoyant `IAny<T>` (« where composition is needed » — nulle part encore). Défendable, mais la Décision se lit comme une forme livrée.
3. **Un risque consigné imprécis :** le danger de la `Dummies.dll` embarquée est réel mais passe par des consommateurs qui compilent contre l'assemblage embarqué, pas (comme consigné) par des types Dummies dans l'API publique de Testing — à corriger dans la référence d'implémentation, puisque les ADR acceptées sont immuables.

### Observations connexes à la conformité

Deux endroits où l'*implémentation est plus stricte que la décision consignée* (acceptable aujourd'hui, mais chacun est un point de décision latent qui ne devrait pas se résoudre par accident) : la capacité de cardinalité de l'ADR-0013 est `internal`, si bien que des implémentations `IAny<T>` étrangères ne peuvent pas y adhérer même quand elles connaissent leur domaine ; et le second test d'architecture de l'ADR-0011 applique « bibliothèque standard uniquement », ce qui est plus que la règle consignée « aucune référence à FirstClassErrors ». Un cas limite d'implémentation qui mérite d'être consigné : la recherche du tag précédent de l'ADR-0010 compare au *plus haut* tag cli, si bien qu'un hotfix sur un majeur plus ancien (`cli-v1.x` après l'existence de `cli-v2`) ne pourrait jamais satisfaire le gate d'incrément de majeur — les hotfix d'anciens majeurs sont de fait non supportés, ce qui devrait être soit documenté comme un non-objectif, soit corrigé par une résolution au tag inférieur le plus proche.
## 8. Revue d'architecture

Cette section évalue la structure du système — découpage, frontières, cohésion, couplage, niveaux d'abstraction, extensibilité, et les patrons qui tiennent l'ensemble. (La topologie des paquets et le graphe de dépendances sont couverts au §10 ; la surface d'API publique au §9.)

### 8.1 L'idée architecturale

L'architecture repose sur trois idées porteuses, chacune tenue avec constance à travers chaque sous-système :

1. **Rendre les états invalides irreprésentables, à chaque altitude.** Le builder d'erreurs par étapes du cœur (une erreur ne peut pas exister sans son message public), les collections d'erreurs internes typées de la taxonomie (une `DomainError` ne peut pas imbriquer une erreur d'infrastructure), la conception jeton/ref struct du binder (une valeur non validée ne peut pas être *lue* — `BindingScope` est un `readonly ref struct` construit exclusivement sur la branche à zéro échec, si bien qu'un mésusage échoue à la compilation), et les options du binder gelées avant que le binding ne commence (ADR-0012 : une enveloppe d'échec à politiques de nommage mélangées n'est pas détectée, elle est inconstructible). C'est le même principe appliqué quatre fois à quatre couches différentes, ce à quoi ressemble réellement la cohérence architecturale.
2. **Les chemins d'échec sont des chemins de code de première classe.** « Fabriquer une erreur ne lève jamais » dans le cœur ; l'extracteur GenDoc qui convertit chaque échec par type/par fabrique en une donnée `ErrorDocumentationExtractionFailure` au lieu d'avorter ; la chaîne de catch à trois niveaux de la CLI qui rapporte des échecs codés `GENDOC_*` dans un format de ligne `CODE: message` épinglé par les tests ; la `ConflictingAnyConstraintException` de Dummies qui nomme les deux contraintes en conflit au moment de la déclaration et borne chaque tirage dédupliqué par une erreur de saturation nommant la graine.
3. **Chaque invariant énoncé reçoit une vérification par machine.** Des analyseurs pour les conventions de code ; des tests d'architecture plus des gardes nuspec au moment du pack pour les frontières de paquets ; des tests de verrouillage pour les décisions de résolution de surcharge ; des portes structurelles instantané + AngleSharp pour la sortie des moteurs de rendu ; des consommateurs de dogfooding sur artefact packé (`floor-check`, `dummies-check`) pour les contrats de chargement ; un diff de catalogue au moment de la release pour le contrat de documentation. La pratique la plus transférable du dépôt est que ses *commentaires décrivent l'intention et que sa CI la fait respecter* — l'intention ne flotte jamais librement.

### 8.2 Verdicts sous-système par sous-système

**Bibliothèque cœur — de niveau référence.** ~47 types publics avec une couverture de docs XML totale, zéro dépendance d'exécution, des statiques thread-safe (un registre de clés protégé par verrou ; des points de couture horloge/ids internes en `AsyncLocal` exposés au seul assemblage ami Testing). La seule ambiguïté architecturale est le contrat d'extensibilité entrouvert disséqué au §9.3(a). La duplication interne est mineure mais réelle : les trois constructeurs d'`Error` tripliquent un corps de sept affectations, et les jumelles `PrimaryPortInnerErrors`/`SecondaryPortInnerErrors` sont des quasi-clones structurels.

**Analyseurs — une ingénierie de niveau référence, une hygiène uniforme.** Une classe d'analyseur à responsabilité unique par règle (18), des assistants de faits partagés (`ErrorCodeFacts`, `KnownSymbols`, `SymbolFacts`, `OperationFacts`), des descripteurs/IDs/catégories/liens d'aide centralisés. Chaque analyseur active l'exécution concurrente, ignore le code généré, enregistre des actions ciblées sur symboles/opérations depuis `CompilationStart` (zéro parcours d'arbre syntaxique nulle part), résout les types de la bibliothèque par nom de métadonnées et reste silencieux quand le cœur n'est pas référencé ; les deux règles à l'échelle de la compilation portent correctement les étiquettes `CompilationEnd` avec un état concurrent par compilation — une subtilité que beaucoup d'analyseurs en production ratent. L'ingénierie du contrat de chargement Roslyn 4.8.0 (quatre gardes indépendantes, dont une packe l'artefact réel et prouve que l'analyseur s'est bien *chargé* en fouillant au grep la table `ReportAnalyzer`, parce qu'un analyseur jamais chargé laisse un build vert) est rare même chez les analyseurs OSS de premier plan. Lacunes : un harnais de test écrit à la main qui n'assertit jamais que les extraits compilent (les tests négatifs peuvent réussir à vide), zéro fournisseur de correctif de code, et cinq fichiers qui redéclarent des constantes de noms de métadonnées que les classes de faits partagées existent précisément à centraliser.

**GenDoc — le fleuron, et il tient.** Cinq étapes proprement séparées qui correspondent presque ligne à ligne au document d'architecture rédigé à la main : définition de la connaissance par attributs + DSL → extraction par réflexion *plus exécution* (le `Code` et le `Context` d'une erreur sont dérivés de l'exécution effective des vraies fabriques d'exemples ; une méthode de documentation dont les exemples divergent sur les codes lève) → isolation de processus par assemblage via un worker de 140 lignes lancé avec `dotnet exec --depsfile` pour que l'extraction se lie à la version de FirstClassErrors *propre à la cible* → agrégation avec découverte pilotée par le SDK, timeouts, annulation par kill de l'arbre de processus, et parsing d'opt-in qui refuse de deviner → rendu via un contrat `IErrorDocumentationRenderer` minimal placé délibérément dans le cœur netstandard2.0 pour que les plugins n'aient besoin que du paquet principal. Le déterminisme est traité comme un contrat à chaque couche (déduplication ordonnée, aucun horodatage, JSON canonique stable à l'octet avec la justification du plancher .NET 8 consignée), ce qui permet à la référence commitée et aux docs régénérées en CI de produire un diff propre — la propriété dont dépend tout le flux de travail de l'ADR-0010. Dettes architecturales : la logique de rendu des exemples RFC 9457 est dupliquée mot pour mot entre les moteurs de rendu Markdown et HTML (un schéma visible de l'utilisateur maintenu deux fois) ; l'orchestrateur statique de 661 lignes n'a **aucun point de couture d'exécuteur de processus**, si bien que les branches timeout/kill/sortie corrompue — la source de la moitié du catalogue `GENDOC_` — ne sont validées que par le chemin nominal de la CI ; et les docs XML de `DocumentationContractVersionAttribute` revendiquent un contrôle de version côté générateur **qui n'existe pas** (rien ne lit l'attribut) — un mainteneur s'appuierait raisonnablement sur une garde qui n'est pas là.

**CLI — des ports-et-adaptateurs d'école à la bonne granularité, appliqués à la moitié de la surface.** Les commandes d'extraction dépendent de ports (`IErrorDocumentationGenerator`, `ICatalogSnapshotSource`, `IOutputSink`, une fabrique de logger) avec des constructeurs doubles production/couture de test et un point de couture `Run()` sans `CommandContext` ; stdout est réservé au document, stderr aux diagnostics ; le contrat de codes de sortie (0/1/2/130) est documenté dans le code, dans les docs utilisateur, et épinglé par les tests. Mais les cinq commandes de configuration/rendu écrivent directement sur `Console.Out/Error` avec zéro test — un standard à deux vitesses au sein d'un même petit projet, et la raison probable pour laquelle une référence périmée à `fce init` survit dans un message d'erreur. Rien nulle part n'exerce la vraie surface argv de Spectre (pas de `CommandAppTester`), si bien que l'arbre de commandes, le parsing et la sortie d'aide — le *contrat de compatibilité déclaré* de l'outil — ne sont pas épinglés. La logique de résolution de source et de précédence est dupliquée dans deux styles divergents (`GenerateOptionsResolver` renvoie des codes de sortie ; `CatalogSourceResolver` lève) avec des chaînes de message identiques à l'octet maintenues deux fois.

**RequestBinder — le domaine le plus solidement ingénié.** ~2 490 lignes de code, 25 types publics, et la conception isolée la plus frappante de l'audit : l'inaccessibilité à la compilation des valeurs non validées via des jetons de champ opaques lisibles uniquement à travers un scope ref struct. Une traçabilité ADR complète (neuf ADR acceptées, l'implémentation correspondant exactement à chacune), un travail de performance fondé sur les preuves (un projet BenchmarkDotNet décomposant le coût par propriété et rapportant honnêtement que ~70–75 % relève de l'allocation d'arbres d'expression côté appelant qu'aucun changement de la bibliothèque ne peut supprimer), et une sémantique d'agrégation précise (les enveloppes imbriquées sont désambiguïsées *par identité de référence*, pas par type, si bien qu'un convertisseur qui se trouve renvoyer une `PrimaryPortError` conserve tout de même son chemin). Dettes architecturales : le getter `RequestBinderOptions.Default` prend un verrou et effectue une écriture partagée **à chaque lecture** — c'est-à-dire à chaque `Bind.Request` — précisément sur le chemin chaud que le projet a ailleurs optimisé à la sous-microseconde près (un chemin rapide volatile à double vérification préserve exactement la sémantique de l'ADR-0017) ; la réutilisation d'un terminal est tolérée silencieusement, en contradiction avec la doctrine par ailleurs bruyante du canal-bug du binder ; et la famille de convertisseurs (référence/struct × scalaire/liste/complexe) produit des classes parallèles clonées à ~95 % que toute nouvelle variante de présence doit toucher en jusqu'à quatre endroits.

**Testing/Dummies — borné avec professionnalisme.** La séparation est appliquée à trois couches indépendantes et, depuis l'ADR-0026, la duplication inter-paquets est essentiellement nulle (les 530 lignes de Testing délèguent toutes à Dummies). Les entrailles de Dummies sont soignées dans les cas difficiles : une génération construite pour satisfaire (jamais générer-puis-filtrer), des tirages dédupliqués bornés façon collectionneur de coupons, une comptabilité de cardinalité consciente du comparateur, et un analyseur à descente récursive de 457 lignes pour le sous-ensemble regex, validé contre le vrai moteur .NET utilisé comme oracle. Dettes : le miroir `Any`/`AnyContext` à 26 entrées maintenu à la main double le coût de chaque nouveau générateur sans aucun test de complétude, et la propriété-phare de déterminisme (`Reproducibly`) n'est adoptée nulle part dans les grandes suites consommatrices du dépôt lui-même — l'architecture est livrée, la pratique est à la traîne.

### 8.3 Observations transversales

**La cohésion et le couplage sont réellement bons.** Chaque projet a une seule raison de changer ; les seuls couplages inter-paquets sont délibérés et consignés (l'`InternalsVisibleTo` cœur→Testing pour le point de couture d'horloge ; les types de contrat GenDoc dans le cœur conformément à l'ADR-0010). Aucun cycle n'existe, même au moment du build. Les niveaux d'abstraction sont cohérents : des ports aux frontières de processus (CLI, worker GenDoc), l'application par le système de types aux frontières d'API, du code ordinaire entre les deux — le projet ne sur-abstrait pas (pas de conteneur DI, pas de médiateur, pas de couche d'interfaces spéculative), ce qui, pour un écosystème de bibliothèques, est la juste retenue.

**La dette architecturale récurrente est la *duplication en paires*, pas une erreur de conception.** Des moteurs de rendu jumeaux dupliquant le rendu d'exemples ; des collections d'erreurs internes jumelles ; des résolveurs jumeaux dans la CLI ; le miroir `Any`/`AnyContext` ; les constructeurs d'`Error` tripliqués ; le parallélisme sextuple des convertisseurs du binder. Chaque occurrence est individuellement défendable (souvent la distinction de type *est* précisément le but), mais collectivement elles forment le principal risque de passage à l'échelle de la maintenance du projet : un correctif appliqué à un jumeau peut silencieusement manquer l'autre, et seuls des tests d'instantané s'en apercevraient après coup. Un petit nombre d'extractions d'assistants internes (un type `ExampleRendering`, un résolveur CLI partagé, un cœur de constructeur d'`Error`, un test de complétude du miroir par réflexion) en retirerait l'essentiel sans toucher à l'API publique.

**Les points de couture de testabilité sont excellents là où ils existent et ostensiblement absents à exactement deux endroits :** l'orchestration de processus de GenDoc et les commandes de configuration/rendu de la CLI. Les deux lacunes ont la même forme — du code qui parle au monde extérieur (processus, console) sans le port que le code voisin modélise si soigneusement — et toutes deux ont des correctifs bon marché, cohérents avec le patron existant.

**L'évolution à long terme est activement gérée plutôt qu'espérée.** Les planchers de plateforme sont triplement gardés et gouvernés par ADR avec des déclencheurs de remplacement nommés (l'expiration LTS du plancher d'outillage net8.0 en novembre 2026 est déjà anticipée par la propre section de risques de l'ADR-0002) ; le catalogue est un contrat versionné doté d'une porte de release ; les décisions d'API pré-v1 sont tranchées délibérément (la forme du builder de messages de l'ADR-0018, la voie du délégué différée de l'ADR-0023) tant que casser est encore gratuit. Le seul risque d'évolution que les ADR n'ont *pas* attrapé est consigné au §10.3 : le contrat lockstep/IVT qui lie en permanence la version de Testing à celle du cœur.

### 8.4 Verdict

Architecturalement, ce dépôt est bien au-dessus de la barre qu'implique la question posée. Le découpage est juste, les frontières sont appliquées plutôt que décrites, les patrons sont appliqués avec constance à travers cinq sous-systèmes très différents, et l'évolution est gouvernée. Les dettes sont concentrées, nommées, et bon marché au regard de l'existant : deux points de couture de test manquants, une famille de risques de duplication en jumeaux, un oubli de verrouillage sur un chemin chaud, et un contrôle de version documenté mais inexistant.
## 9. Revue de l'API publique

Cette section évalue l'API telle que la rencontrerait un développeur découvrant la bibliothèque aujourd'hui : à travers IntelliSense, le premier exemple du README et les suggestions du système de types lui-même. La question posée tout du long est celle qu'a formulée le comité : *cette API semblerait-elle naturelle et inévitable ?*

### 9.1 Forme de la surface

Le paquet cœur expose environ 47 types publics répartis en cinq groupes :

| Groupe | Types |
|---|---|
| Modèle d'erreur | `Error` (abstraite), `DomainError`, `InfrastructureError`, `PrimaryPortError`, `SecondaryPortError`, `ErrorCode`, `ErrorContext`, `ErrorContextBuilder`, `ErrorContextKey`/`ErrorContextKey<T>`, `PrimaryPortInnerErrors`, `SecondaryPortInnerErrors`, `PublicMessageStage<TError>`, les énumérations `Transience`, `InteractionDirection`, `ErrorOrigin` |
| Transport par exception | `DiagnosableException` (abstraite) + une exception par catégorie (`DomainException`, `InfrastructureException`, `PrimaryPortException`, `SecondaryPortException`) |
| Modèle de résultat | `Outcome`, `Outcome<T>`, `OutcomeTaskExtensions` (24 méthodes d'extension) |
| DSL de documentation | `DescribeError` + six interfaces par étapes (`IErrorTitleStage` … `IErrorExamplesStage`), `ErrorDocumentation`, `ErrorDescription`, `ErrorDiagnostic`, `ErrorContextEntryDocumentation`, les attributs `DocumentedByAttribute`, `ProvidesErrorsForAttribute` |
| Contrat GenDoc | Espace de noms `FirstClassErrors.GenDoc` : `AssemblyErrorDocumentationReader`, les types de résultat/d'échec d'extraction, le contrat `Rendering` (`IErrorDocumentationRenderer`, `RenderRequest`, `RenderedDocument`, `RenderLayouts`) |

C'est une *grande* surface pour une bibliothèque présentée comme légère, mais les groupes sont proprement séparés par espace de noms et par moment d'usage (temps de modélisation vs temps de traitement vs temps de documentation), ce qui maintient basse la charge cognitive par moment. La découvrabilité IntelliSense est excellente parce que la couverture en documentation XML est totale et inhabituellement substantielle : justification doctrinale sur `Error`, guide de correspondance des champs RFC 9457 sur les propriétés de message (`ShortMessage` → `title`, `DetailedMessage` → `detail`), et guide de rédaction (listes de verbes, anti-patterns) au sein des interfaces du DSL. Même les justifications `SuppressMessage` sont des notes de conception multi-lignes (`ErrorCode.cs:76-82`). `GenerateDocumentationFile` est activé, si bien que tout cela atteint les consommateurs.

### 9.2 Ce qui est véritablement excellent

**Le builder par étapes comme application d'invariant.** `DomainError.Create(code, diagnosticMessage)` renvoyant un `PublicMessageStage<DomainError>` — un type qui n'est *pas* un `Error` — est un exemple d'école de l'invariant fait chemin de moindre résistance. C'est le compilateur, et non un contrôle à l'exécution, qui garantit qu'aucune erreur n'existe sans son message public. Un primo-utilisateur ne peut pas tenir l'API de travers.

**Un unique `Then` nommé par l'intention au lieu de `Map`/`Bind` (ADR-0003).** Là où les bibliothèques de résultats fonctionnelles forcent les nouveaux venus à apprendre la distinction map/bind, `Outcome<T>` expose un seul nom de continuation avec trois familles de surcharges : renvoyant un outcome (`Func<T, Outcome<TResult>>`), renvoyant une valeur (`Func<T, TResult>`), et leurs jumelles asynchrones. Le risque subtil — la résolution de surcharge de C# produisant silencieusement un `Outcome<Outcome<T>>` — a été identifié, décidé dans une ADR, et *verrouillé par une classe de tests dédiée à la résolution de surcharge* (`OutcomeThenOverloadResolutionTests.cs`, qui transforme « un emboîtement `Outcome<Outcome<T>>` silencieux en test rouge »). C'est ainsi qu'une bibliothèque de référence devrait gérer le risque de forme d'API.

**La symétrie de nommage comme règle apprenable (ADR-0005).** Le nom nu renvoie un `Outcome` ; le suffixe `OrThrow` lève (`GetResultOrThrow` ; `FromKelvinOrThrow` dans les exemples d'usage). Une seule règle, appliquée partout, fait qu'un utilisateur qui a vu une paire de fabriques peut prédire toutes les autres.

**Une histoire asynchrone disciplinée.** Les variantes synchrones et asynchrones de `Then`/`Recover`/`Finally` existent sur `Outcome`, `Outcome<T>`, `Task<Outcome>` et `Task<Outcome<T>>` ; les arguments sont validés de façon anticipée *avant* le premier await (patron de fonction locale `Core()`, documenté comme contrat au niveau de la classe) ; `ConfigureAwait(false)` est appliqué partout (correct pour le plancher net472) ; les callbacks qui renvoient des tâches `null` et les tâches qui se résolvent en outcomes `null` sont convertis en `InvalidOperationException` explicites au point de violation au lieu de refaire surface plus tard en NRE opaques (`AsyncCallbackGuard`, `OutcomeTaskExtensions.EnsureNotNull`).

**Sûreté vis-à-vis des threads et points de couture de test sans état ambiant public.** Les seuls statiques mutables sont un registre `ErrorContextKey` protégé par verrou et les redéfinitions ambiantes d'horloge/d'identifiant d'instance fondées sur `AsyncLocal` — et ces dernières sont `internal`, exposées uniquement via l'assemblage ami `FirstClassErrors.Testing`, si bien que le cœur n'expose aucun état mutable ambiant public et que les redéfinitions circulent avec l'`ExecutionContext` (les tests parallèles ne peuvent pas fuir les uns dans les autres).

### 9.3 Les frictions qu'un nouveau développeur rencontrera réellement

Rien de ce qui suit n'est de l'ordre de la refonte ; tout relève du raffinement. Mais chaque point est un endroit où l'« inévitabilité » par ailleurs solide de l'API se brise.

**(a) Le contrat d'extensibilité est ambigu — l'API envoie des signaux contradictoires.** `Error` et `DiagnosableException` ont des constructeurs `protected` et un `ToException()` abstrait, ce qui *invite* à des catégories définies par le consommateur. Mais le constructeur de `PublicMessageStage<TError>` est `internal`, si bien qu'une catégorie tierce ne peut pas reproduire la discipline de construction par étapes qui est la fonctionnalité vitrine de la bibliothèque ; et les quatre catégories concrètes sont `public` et non scellées mais n'ont que des constructeurs `internal` — scellées de fait sans le dire. Aucun document utilisateur ne répond à « puis-je ajouter une catégorie ? » (`ErrorTaxonomy`, `CoreConcepts` et la FAQ ont été passés au grep sur ce point). Un primo-utilisateur qui tente `class ConfigurationError : Error` obtient un succès partiel déroutant. Le contrat devrait être un oui explicite ou un non explicite :

* *Le fermer* (moins cher, en accord avec les hypothèses actuelles des analyseurs et de GenDoc) : documenter que les quatre catégories forment la taxonomie complète, que l'extension passe par des fabriques qui les renvoient, et sceller ou privatiser les constructeurs de façon cohérente ; ou
* *L'ouvrir* : donner à `PublicMessageStage` un chemin de création `protected`/public pour que `ConfigurationError.Create(...).WithPublicMessage(...)` soit possible pour les consommateurs.

**(b) Des lacunes de symétrie qui forcent une cérémonie que les ADR ont supprimée ailleurs.**

```csharp
// 1. L'Outcome non générique n'a pas le Then de projection de valeur que l'ADR-0003 a donné à Outcome<T>.
// Avant — produire une valeur après une commande void :
outcome.Then(() => Outcome<Receipt>.Success(receipt));
// Après (ajout recommandé, même preuve de « betterness » de surcharge que l'ADR-0003) :
outcome.Then(() => receipt);

// 2. Les erreurs de port n'ont pas le Create à erreur interne unique que DomainError et
//    InfrastructureError ont tous deux — alors qu'envelopper UNE erreur de domaine est
//    le cas de frontière le plus courant.
// Avant :
PrimaryPortError.Create(code, msg, new PrimaryPortInnerErrors().Add(domainError));
// Après (ajout recommandé) :
PrimaryPortError.Create(code, msg, domainError);
```

De plus, `PrimaryPortInnerErrors`/`SecondaryPortInnerErrors` n'implémentent ni `IEnumerable` ni une fabrique de style params, si bien qu'aucun initialiseur de collection ne fonctionne non plus. Ces asymétries sont exactement le genre de chose sur lequel trébuche un utilisateur en première semaine, parce que la bibliothèque lui a appris à s'attendre à la symétrie.

**(c) Le registre `ErrorContextKey` global au processus a un mode d'échec inter-assemblages brutal et aucun guide d'espace de noms.** L'identité d'une clé est un nom global au processus. Deux paquets indépendants enregistrant le même nom avec des types de valeur *différents* produisent une `InvalidOperationException` à l'initialisation de type du perdant — qui refait typiquement surface en `TypeInitializationException` opaque dans du code que son auteur n'a jamais touché (`ErrorContextKey.cs:133-154`). Les enregistrements même-nom/même-type fusionnent silencieusement, la première description l'emportant. Les docs XML décrivent cela correctement, mais la section du guide utilisateur consacrée aux clés (`ErrorContext.en.md`) n'offre aucune convention de préfixage, et le préfixe `#` utilisé par les clés du framework n'est pas documenté comme réservé. Pire, les sources canoniques donnent en exemple **trois conventions de nommage différentes** : les exemples des docs XML utilisent le PascalCase (`DealId`, `CorrelationId`), le guide utilisateur le SCREAMING_SNAKE (`ORDER_ID`, `STATEMENT_ID`), et les clés du framework le SCREAMING_SNAKE préfixé de `#`. Comme les noms de clés sont le contrat de sérialisation et de journalisation, deux équipes suivant les deux exemples officiels produiront des vocabulaires d'observabilité incohérents — précisément la dérive que la bibliothèque existe pour prévenir.

**(d) Annotations de flux de nullabilité manquantes sur l'API de recherche la plus utilisée.** `ErrorContext.TryGet<T>(key, out T? value)` n'a pas d'annotation `[NotNullWhen(true)]`, si bien que les consommateurs avec NRT activés doivent recourir au null-forgiving après un retour `true`. L'équipe connaît manifestement la lacune de netstandard2.0 (un commentaire interne dans `ErrorDescription` l'explique), mais l'atténuation standard — déclarer en interne les attributs reconnus par le compilateur et les embarquer — n'est appliquée nulle part. Un seul fichier de déclaration d'attributs règle cela à coût d'exécution nul :

```csharp
public bool TryGet<T>(ErrorContextKey<T> key, [NotNullWhen(true)] out T? value)
```

**(e) La direction exception→Outcome de « l'exception est un transport » n'a ni méthode d'assistance ni patron documenté.** La direction aller est de première classe (`ToException()`, `ThrowIfFailure()`, `GetResultOrThrow()`). Le retour — à une frontière qui consomme du code levant des exceptions — est l'idiome sans assistance `catch (DiagnosableException ex) { return Outcome<T>.Failure(ex.Error); }`, qui n'apparaît dans *aucun* guide et aucun exemple (grep sur l'ensemble des `.cs` et `.md` : zéro occurrence). Les nouveaux venus sont laissés à découvrir par eux-mêmes que `ex.Error` fait l'aller-retour sans perte. Documenter l'idiome (et optionnellement ajouter `Outcome.From(DiagnosableException)`) complète l'aller-retour du Principe 2 ; un `Outcome.Try(Action)` général de capture d'exceptions ne devrait *pas* être ajouté, car il contredirait la position de la bibliothèque selon laquelle les exceptions arbitraires ne sont pas des erreurs modélisées.

**(f) Petits points de finition.** `ErrorCode.Unspecified` est `internal`, si bien que les consommateurs qui veulent déclencher des alertes sur les erreurs dégradées (l'intérêt opérationnel de la sentinelle `#UNSPECIFIED`) doivent comparer des chaînes magiques — la sentinelle est doctrine publique, son instance canonique devrait l'être aussi. Dérive de docs XML : `Error.ToException()` documente une `InvalidOperationException` qu'aucune redéfinition ne peut lever ; une justification `S4136` référence encore la méthode `To` supprimée par l'ADR-0003 ; le résumé d'`Outcome` dit « fail without throwing an error » là où c'est *exception* qui est voulu. En interne, les trois constructeurs protégés d'`Error` tripliquent un corps identique de sept affectations (un futur champ devra être ajouté trois fois), et les collections jumelles `InnerErrors` sont des quasi-clones structurels.

### 9.4 Verdict

Une conception d'API de niveau référence. Le builder par étapes, l'unification de `Then` gouvernée par ADR avec ses tests de verrouillage, la doctrine du jamais-lever et la couverture totale en docs XML placent cette API au-dessus de toutes les bibliothèques de résultats .NET grand public en *discipline de conception*. La liste des frictions est réelle, mais elle se compose de complétions de symétrie, d'une clarification de contrat (l'extensibilité) et de la documentation d'un comportement déjà correct — des raffinements, pas une refonte.
## 10. Revue de l'écosystème

### 10.1 La topologie réelle

Le dépôt est un mono-repo à cinq paquets et trois trains de release comptant 24 fichiers `.csproj` (22 dans la solution, plus deux consommateurs de dogfooding délibérément hors solution sous `tools/`). Ce qui est livré, et comment :

| Unité | TFM | Train | Rôle |
|---|---|---|---|
| `FirstClassErrors` | netstandard2.0 | `lib` (tag `lib-v*`) | Cœur ; **zéro dépendance NuGet à l'exécution** ; embarque les analyseurs sous `analyzers/dotnet/cs` |
| `FirstClassErrors.Testing` | netstandard2.0 | `lib` (lockstep) | Points de couture de test (horloge/ids gelés), assertions d'outcome ; assemblage ami du cœur |
| `FirstClassErrors.RequestBinder` | netstandard2.0 | `lib` (lockstep) | Binder de la frontière d'adaptateur primaire |
| `FirstClassErrors.Cli` (`fce`) | net8.0 | `cli` (tag `cli-v*`) | Outil dotnet ; embarque le worker GenDoc dans le paquet de l'outil |
| `Dummies` | netstandard2.0; net8.0 | `dum` (tag `dum-v*`) | Bibliothèque autonome de données de test (ADR-0011) |

Tout le reste — Analyzers, GenDoc, GenDoc.Worker, deux exemples Usage, dix projets de test, un projet Benchmarks, un csproj de navigation de docs — est de la structure interne `IsPackable=false`. Le graphe de dépendances est strictement acyclique et en couches : le cœur ne dépend de rien ; Testing/RequestBinder/GenDoc/Worker sont une couche au-dessus ; la CLI au sommet. Les seules arêtes orientées vers le bas (cœur→Analyzers pour l'ordre de pack, Cli→Worker pour l'embarquement) sont des arêtes d'ordre de build `ReferenceOutputAssembly=false`, si bien qu'aucun cycle n'existe même au moment du build — et le projet Analyzers ne référence *aucun* projet du dépôt, de sorte qu'il peut se charger dans n'importe quel hôte Roslyn.

**État sur nuget.org (vérifié pendant l'audit) :** `FirstClassErrors` et `FirstClassErrors.Testing` sont publiés en `0.1.0-preview.1` ; `FirstClassErrors.RequestBinder`, `FirstClassErrors.Cli` et `Dummies` sont des identifiants non réclamés.

### 10.2 Ce qui élève cet écosystème au-dessus de la pratique OSS courante

**Les frontières sont appliquées par machine, non de simples intentions.** Chaque règle structurelle dont l'écosystème dépend possède un garde-fou exécutable :

* La règle d'autonomie de Dummies (ADR-0011) est contrôlée au moment du build par un test d'architecture (`Dummies.UnitTests/ArchitectureTests.cs`) *et* de nouveau sur l'artefact livré par une assertion nuspec au moment du pack (`tools/packaging/pack.sh`).
* Le versionnage en lockstep du train `lib` est asserté sur le `.nupkg` packé : toute dépendance intra-train non épinglée à la version co-publiée fait échouer le pack.
* L'embarquement du worker dans l'outil `fce` est asserté par grep sur le `.nupkg`.
* `tools/floor-check` et `tools/dummies-check` consomment les **artefacts packés réels** en vrais consommateurs aval — floor-check prouve que l'analyseur embarqué se charge sous le Roslyn du SDK plancher épinglé (avec `CS8032;AD0001` escaladés en erreurs), dummies-check prouve la sélection d'assets par TFM depuis des consommateurs net8.0 et net6.0 — tous deux délibérément hors solution, avec le raisonnement consigné dans les fichiers csproj.

Cela referme exactement la brèche « build vert mais paquet cassé » que la plupart des projets laissent ouverte, et c'est la pratique la plus transférable de tout le dépôt.

**Une décomposition justifiée, y compris celle qui paraît étrange.** `GenDoc.Worker` en projet séparé d'environ 140 lignes est architecturalement nécessaire, pas accidentel : il doit s'exécuter dans son propre processus avec `RollForward=LatestMajor` pour pouvoir se lier au runtime qu'exige l'*assemblage consommateur inspecté* (`dotnet exec --depsfile` contre la fermeture de dépendances de la cible) — une politique que la CLI elle-même (`RollForward=Major`) ne peut pas porter. La scission entre le contrat d'extraction livré (dans le cœur, verrouillé en version avec le consommateur conformément à l'ADR-0010) et la génération/le rendu côté outillage est tout aussi délibérée.

**Une source de vérité unique pour la topologie de release.** La correspondance train→préfixe de tag→scopes→changelog→paquet vit une seule fois dans `tools/trains.sh` et est sourcée par l'outillage d'empaquetage et de changelog ; `pack.sh` est partagé à l'identique entre la vraie release et le workflow de dry-run ; l'ajout d'un train dispose d'une checklist documentée (`AddingAReleaseTrain.en.md`).

**Des métadonnées NuGet complètes et orientées consommateur sur les cinq paquets** : descriptions substantielles, tags soignés, expression Apache-2.0, icône partagée, `README.nuget.md` par paquet, symboles snupkg avec SourceLink (délibérément désactivé pour le paquet de l'outil, avec la raison énoncée), SBOM SPDX embarqué, et une décision documentée de *ne pas* coder en dur des `PackageReleaseNotes` promptes à se périmer. La composition côté consommateur est la plus simple possible : `dotnet add package FirstClassErrors` tire automatiquement les analyseurs — aucun paquet d'analyseurs séparé à oublier.

### 10.3 Dette structurelle — petite, concentrée, et pour l'essentiel affaire de séquencement

**(1) `FirstClassErrors.Testing` fait passer en contrebande une `Dummies.dll` non publiée dans son propre dossier `lib/` (Élevé).** Parce que Dummies n'est pas encore sur NuGet, Testing la référence avec `PrivateAssets=all` et embarque la DLL via une cible de pack personnalisée (`IncludeDummiesInPackage`). Quatre conséquences : la version de Dummies qu'exécute un consommateur est invisible pour NuGet et ingérable par lui ; une fois Dummies publiée, un consommateur référençant à la fois Testing et Dummies obtient deux assemblages de même identité sans aucun mécanisme de résolution ; comme Testing ne cible que netstandard2.0, la copie embarquée est l'asset Dummies *downlevel*, si bien que les consommateurs net8+ perdent silencieusement les générateurs de types modernes (`DateOnly`/`TimeOnly`/`Int128`/`Half`) que la cible net8.0 du vrai paquet leur donnerait ; et conformément à l'ADR-0026, les types Dummies (`IAny<T>`) font partie de l'*API publique* de Testing, si bien qu'il s'agit d'une dépendance cachée envers un fournisseur de types de surface publique. Le csproj documente le plan de sortie (« switch to a PackageReference once Dummies is published »), ce qui atténue le risque sans le supprimer tant que la fenêtre est ouverte. C'est le défaut le plus lourd de conséquences de l'écosystème, et il se résout en exécutant une décision déjà prise.

**(2) L'identifiant de paquet générique `Dummies` est non réclamé alors que son train de release existe déjà (Moyen).** La prémisse même de l'ADR-0011 est qu'« une identité de paquet est coûteuse à renommer après adoption ». L'identifiant est un mot anglais courant, vérifié non réclamé au moment de l'audit ; jusqu'au premier push, n'importe quel tiers peut le prendre, et la réservation de préfixe NuGet ne peut pas protéger un mot générique isolé de la même façon que `FirstClassErrors.*` est de facto ancré par ses deux paquets publiés. Chaque garde-fou nécessaire à une première publication sûre existe déjà dans `pack.sh`. C'est un pur risque d'exécution, qui se solde à peu de frais.

**(3) Un consommateur du seul binder perd silencieusement les analyseurs (Moyen).** `dotnet pack` écrit par défaut `exclude="Build,Analyzers"` sur les dépendances du nuspec, si bien que la dépendance de `FirstClassErrors.RequestBinder` vers le cœur ne fait **pas** circuler les analyseurs FCE embarqués vers un consommateur qui n'installe que le binder — alors que tout l'argumentaire du binder est « des arbres de `PrimaryPortError` codés et documentés », dont la discipline de documentation est précisément ce que ces analyseurs appliquent. Le dépôt lui-même sait qu'une simple référence ne transporte pas les analyseurs (il les câble explicitement pour son propre dogfooding, d'après le commentaire renvoyant à l'issue #153), mais la section d'installation de `RequestBinder.en.md` présente le binder comme un `dotnet add package` autonome, sans note de référence compagne. Le correctif fort consiste à embarquer les mêmes analyseurs dans le paquet du binder (Roslyn déduplique par identité d'assemblage, et le lockstep garantit la correspondance de version) ; le correctif bon marché tient en une phrase documentée.

**(4) Deux décisions durables ne vivent que dans des commentaires, pas dans des ADR (Moyen).** (a) Le contrat de lockstep du train `lib` — *pourquoi* le binder ne doit pas avoir son propre train — n'existe que dans des commentaires de `pack.sh` et une ligne de documentation utilisateur. (b) Le cœur accorde `InternalsVisibleTo` au paquet livré `FirstClassErrors.Testing` (le point de couture de l'horloge ambiante), ce qui rend le lockstep à version exacte *définitivement obligatoire* — les internals ne sont pas une surface de compatibilité, donc Testing ne pourra jamais versionner indépendamment tant que cet IVT existe. Les deux passent mot pour mot le propre test de significativité d'ADR du projet, et l'asymétrie avec l'ADR-0011 (qui consigne la décision d'hébergement équivalente pour Dummies) est frappante. Si `pack.sh` était un jour refactoré, le seul enregistrement du raisonnement disparaîtrait avec lui.

**(5) Incohérences mineures de taxonomie de solution (Faible).** Le projet de navigation de docs prétend être exclu des configurations de build de la solution, mais seules les configurations AnyCPU l'omettent (les configurations `x64`/`x86` le construisent toujours). Les exemples Usage et le projet Benchmarks sont imbriqués sous le dossier de solution `tests`, ce qui brouille une taxonomie src/tests par ailleurs propre. Deux idiomes `InternalsVisibleTo` coexistent. Et l'*espace de noms* `FirstClassErrors.GenDoc` s'étend sur deux assemblages (le contrat livré dans le cœur, l'outillage dans le projet non empaquetable) — un placement dont la justification est saine conformément à l'ADR-0010, mais une odeur de découvrabilité qui mérite une note de doc.

### 10.4 Faut-il fusionner, scinder ou déplacer quoi que ce soit ?

L'audit a examiné les questions classiques du mono-repo et trouvé la décomposition actuelle défendable dans chaque cas : **GenDoc.Worker** doit rester séparé (politique de roll-forward du runtime) ; **Analyzers** doit rester sans dépendance (chargement dans les hôtes Roslyn) et est correctement livré à l'intérieur du paquet du cœur plutôt qu'en quatrième installation ; **RequestBinder** a sa place dans le dépôt tant qu'il est livré en lockstep et réutilise la taxonomie d'erreurs du cœur comme contrat public ; **Dummies** est la seule véritable candidate à l'extraction, et son ADR définit déjà les déclencheurs — la recommandation (§14) est d'*évaluer activement* ces déclencheurs au jalon 1.0 plutôt que d'attendre qu'ils se déclenchent. Aucun projet n'a besoin d'être créé, fusionné ou scindé aujourd'hui.

### 10.5 Verdict

La structure de l'écosystème est cohérente, professionnelle et — dans sa discipline d'application (gardes au moment du pack, dogfooding sur artefacts packés, topologie de trains à source unique) — d'une qualité authentiquement digne de référence. Les faiblesses sont des questions de *séquencement* de jeunesse (une dépendance non publiée, un identifiant non réclamé, deux décisions non consignées), pas des défauts d'architecture.
## 11. Revue de la documentation

La documentation fait ici partie du produit — la thèse du projet porte littéralement sur une documentation qui ne dérive pas — cet audit l'a donc traitée comme un produit : ~5 900 lignes réparties sur 30 guides utilisateur anglais plus une référence des analyseurs de 19 pages (le tout bilingue), une couche mainteneur (12 pages de référence des workflows, deux runbooks, une référence d'implémentation des ADR, 26 ADR bilingues), et le catalogue généré sous `doc/generated/`.

### 11.1 Qualité et exactitude du contenu : proche du niveau de référence

**L'exactitude face à l'implémentation est le résultat saillant.** L'audit a contrôlé par sondage plus de 40 références d'API à travers 12 guides — dont l'intégralité du guide RequestBinder de 724 lignes (vérifié type par type face aux sources du binder), le pipeline Outcome asynchrone, les assistants Testing/Dummies, le DSL DescribeError, le pipeline GenDoc programmatique, et chaque commande, option et code de sortie CLI cités. **Il existe exactement une erreur d'API factuelle dans l'ensemble des docs utilisateur anglaises** : `DeterministicTesting.en.md` (~ligne 163) affirme que `Clock.UseAny()`/`InstanceIds.UseAny()` « both take an optional seed » — les deux sont sans paramètre depuis le rebasage de l'ADR-0026 ; la reproductibilité passe désormais par `Dummies.Any.Reproducibly(...)`, que le guide frère documente correctement. (C'est aussi le seul suivi manqué de l'ADR-0026 ; la même phrase est périmée dans la jumelle française.) Un contrôle de liens scripté n'a trouvé aucun lien relatif cassé dans les docs utilisateur EN. `ComparisonWithOtherLibraries.en.md` porte une date de revue explicite six jours avant cet audit — la preuve d'une maintenance active, pas de l'archéologie.

**La pédagogie est réellement en couches et délibérément anti-duplicative.** Le parcours de lecture va du pitch → GettingStarted (de zéro au catalogue généré en six étapes concrètes) → principes → modèle (CoreConcepts/ErrorTaxonomy/ErrorContext) → rédaction → consommation → trio de test → exploitation → docs d'extension, avec des pieds de page précédent/suivant formant une chaîne guidée à travers 19 guides, des énoncés de périmètre par guide, des diagrammes mermaid « le modèle en une minute », et des check-lists de revue de clôture pensées pour la revue en pull request. Les guides qui se recouvrent délèguent au lieu de redire (« Detailed explanations belong in the focused guides »), et aucune contradiction sémantique n'a été trouvée entre guides sur le modèle à trois messages, les règles de composition, la transience, ou la doctrine lever-vs-retourner. Les documents de cadrage honnête (WhenNotToUse, l'ouverture de la FAQ « Why not just use normal exceptions? You can. », la comparaison sans classement) construisent exactement la crédibilité dont un projet de référence a besoin.

### 11.2 La faiblesse systémique : la découvrabilité, pas le contenu

Les défauts se concentrent sur *les pages-hub et le maillage*, pas sur la prose :

* **Les deux hubs de découverte sont incomplets.** La section Documentation du README omet six guides anglais (dont `OutcomeGuide` et `DocumentationMap` lui-même) ; `DocumentationMap` — la page de navigation par intention — en omet six autres, dont notamment le guide RequestBinder de 724 lignes, et n'est elle-même atteignable que depuis le pied de page d'une seule page. `DocumentationMap.en.md:8` affirme que le README « lists every document by area », ce qui est actuellement faux.
* **Les projets d'exemple sont quasi invisibles (Élevé).** Ni le README, ni GettingStarted, ni DocumentationMap ne lie l'un ou l'autre projet Usage. `FirstClassErrors.Usage` — la réalisation compilée, testée par snapshots, du patron de chaque guide — est liée depuis *zéro* document utilisateur. Le meilleur actif pédagogique du projet est indécouvrable depuis son parcours de lecture documenté.
* **Défauts mineurs de chaîne** : trois guides ont des liens précédent/suivant non réciproques (RequestBinder n'est dans la chaîne de personne) ; `WhenNotToUseFirstClassErrors` est nettement plus mince que ses frères alors qu'il est un point d'entrée « Discover » de premier plan ; et le nommage des clés de contexte est modélisé de façon incohérente d'un guide à l'autre (UPPER_SNAKE dans ErrorContext vs PascalCase dans les docs Testing/Logging/XML — la même incohérence que le §9.3(c) a trouvée dans les docs d'API).

### 11.3 Dérive documentation/exemples (les seules contradictions substantielles trouvées)

Trois endroits où les docs et les exemples compilés se contredisent — significatif parce que les exemples sont le matériau que l'on copie :

1. **`UsagePatterns` vs l'`Amount` livré.** Le guide montre un `Amount.Add` renvoyant un Outcome et prescrit de « centraliser la validation dans la version qui renvoie un Outcome et d'en dériver la version levante — pas l'inverse ». L'exemple livré `Amount` n'a *que* `AddOrThrow`/`SubtractOrThrow`, levant directement — réalisant l'anti-patron même contre lequel la doc met en garde (tandis que `Temperature` suit la règle). L'extrait de la doc utilise aussi `Currency != other.Currency` alors que le `Currency` de l'exemple ne définit aucun opérateur.
2. **Le guide RequestBinder surestime la couverture de l'exemple.** Il dit que l'exemple met en vitrine « every remaining overload and option », mais le chemin d'argument hors DTO documenté (`bind.Argument(...).FromRoute/FromQuery`, clés de provenance) n'apparaît nulle part dans l'exemple ni dans ses tests — une fonctionnalité de premier ordre du binder sans exemple compilé (également signalée par l'audit de l'ADR-0021).
3. **Les tests de l'exemple ne pratiquent pas le dogfooding de `FirstClassErrors.Testing`** alors que le guide dit que « le binder renvoie un Outcome, donc les assistants de test s'appliquent directement » — les tests de l'exemple recodent à la main exactement le boilerplate de fouille de contexte que le paquet Testing existe pour supprimer.

### 11.4 Synchronisation français/anglais : proche de l'exemplaire, avec deux dérives confirmées

Le miroir couvre **185 fichiers : 92 paires EN/FR avec zéro orphelin français** ; seul le règlement des ADR (`adr/README.md`, `template.md`) est non traduit. La comparaison approfondie de huit paires représentatives (README racine, OutcomeGuide, RequestBinder, ADR-0026, workflows/ci, Internationalization, LoggingIntegration, CONTRIBUTING) a trouvé une parité parfaite titre pour titre et bloc de code pour bloc de code, avec une convention de localisation de principe (identifiants/API en anglais ; littéraux de chaîne, commentaires et étiquettes mermaid traduits) et des liens de remplacement par langue dans le miroir des ADR. Sur ~9 500 lignes en miroir, il existe exactement deux dérives — toutes deux traçables à des commits ne touchant que l'anglais :

1. **`CONTRIBUTING.fr.md` liste 5 scopes de commit là où l'anglais (et le linter qui les applique) en a 7** — les lignes `binder` et `dummies` manquent, si bien qu'un contributeur lisant le français voit un contrat plus étroit que celui que la CI applique.
2. **20 des 25 guides utilisateur français pointent en pied de page vers une ancre morte** (`README.fr.md#-étapes-suivantes`, un titre qui n'existe plus) — le miroir français d'un correctif anglais dont le message de commit *affirmait à tort* que les pages françaises étaient déjà correctes.

Le constat structurel relève de la gouvernance : **la politique énoncée couvre 1 fichier tandis que la pratique en couvre ~120.** `AGENTS.md` dit que « French lives only in `doc/handwritten/for-users/README.fr.md` » ; CLAUDE.md et le template de PR ne nomment de même que le README. Seul le règlement des ADR énonce la politique réelle (l'anglais canonique, le français suit). Et aucun outil ne contrôle l'appariement, la fraîcheur ou l'intégrité des liens FR — alors que le dépôt possède cinq outils de cohérence sur mesure. Les deux dérives observées sont mécaniquement détectables ; toutes deux sont survenues à quelques jours de commits ne touchant que l'anglais. Le miroir est soutenable *aujourd'hui* parce qu'il est maintenu avec une discipline inhabituelle ; codifier la politique et ajouter un contrôle de synchronisation consultatif (complétude des paires + résolveur de liens + avertissement sur changement EN seul) est ce qui le rend soutenable à mesure que les contributeurs se multiplient.

### 11.5 Documentation mainteneur : haute qualité, en retard sur un pipeline qui va vite

La couche de référence des workflows (12 pages avec des sections « Handle with care » consignant l'intention qu'un nettoyage bien intentionné détruirait, plus la règle de préséance énoncée « quand la page et le YAML sont en désaccord, le YAML gagne — et la page doit être corrigée ») est un actif rare et réellement précieux. Mais le bouillonnement du pipeline des 17–19 juillet l'a distancée : **3 des 15 workflows (canary, dummies, gendoc-docs) n'ont aucune page de référence** ; `ci.en.md` documente deux des trois jobs de ci.yml (le job de plancher de framework net472 — le point d'application de l'ADR-0022 — est invisible dans la couche de doc) ; `release-dryrun.en.md` dit encore « the two published projects » alors que le yml packe trois trains. Plus lourd de conséquences, **`CONTRIBUTING.md` contredit la machinerie de release** : il énonce que le scope de commit « carries no versioning weight, only readability », tandis que l'outillage des trains *écarte désormais silencieusement les commits sans scope des notes de release et des changelogs* — un contributeur qui suit CONTRIBUTING à la lettre peut rendre un changement visible par l'utilisateur invisible pour l'historique de release. Une constellation de pointeurs périmés plus petits (le préambule du changelog nommant deux des trois paquets en lockstep, « ADRs are English only » dans le README mainteneur, un exemple de format de tag périmé, une citation obsolète de la liste d'autorisation des trains), individuellement triviaux, brouille collectivement l'autorité de la couche de doc.

### 11.6 Verdict

La qualité du contenu, l'exactitude, la pédagogie et la discipline bilingue sont toutes au niveau de référence ou tout proches — les docs rédigées à la main pratiquent de façon démontrable la thèse sans-dérive que le produit vend. Le mode de défaillance que cet ensemble documentaire présente réellement est la *dérive d'inventaire sur les bords* : les pages-hub, les références de workflows, CONTRIBUTING et le miroir FR se sont tous légèrement dégradés dans la même fenêtre de 72 heures de changement rapide du pipeline, parce que chacune de ces surfaces est maintenue à la main dans un dépôt qui vérifie tout le reste par machine. Le remède est la propre médecine du projet : trois petits contrôles consultatifs (inventaire des hubs de doc, couverture doc des workflows, synchronisation paires/liens FR) dans le patron `tools/` existant.
## 12. Revue de l'expérience développeur

Cette section parcourt le trajet d'adoption de bout en bout : découverte → installation → première
erreur → premier catalogue → développement quotidien → tests → exploitation, et évalue ce que le
développeur vit réellement à chaque étape.

### 12.1 Découverte et premier contact

Le README est un premier contact solide : un énoncé de problème concret (« une erreur de production est
rarement utile réduite à un type et une chaîne »), un exemple motivant complet, un lien honnête « quand
ne pas l'utiliser », et une rangée de badges (CI, quality gate et couverture SonarCloud, CodeQL, OpenSSF
Best Practices et Scorecard) qui signale un sérieux d'ingénierie exactement à l'audience dont une
bibliothèque de référence a besoin. Deux défauts de vitrine le minent :

* **Le README du paquet cœur sur nuget.org annonce « 16 Roslyn analyzers (FCE001–FCE016) » — le produit
  en livre 18.** Petit, mais c'est une dérive dans l'affirmation anti-dérive phare, sur la page que tout
  évaluateur lit en premier.
* **Le README du paquet CLI documente des commandes qui n'existent pas** (`fce renderer add|list|remove`
  au lieu du réel `fce config renderer …`), décrit mal `config show`, et *omet entièrement les commandes
  de versionnage du catalogue* — la fonctionnalité même autour de laquelle le pipeline de release est
  construit. Un nouvel utilisateur qui suit le README du paquet obtient une erreur command-not-found et
  n'apprend jamais que la fonctionnalité phare existe. C'est le correctif au plus fort impact utilisateur
  par ligne de tout l'audit.
* **La section d'installation du README du dépôt pointe vers des paquets qui ne sont pas sur NuGet.**
  `dotnet add package FirstClassErrors.RequestBinder` (ligne 118) et
  `dotnet tool install --global FirstClassErrors.Cli` (ligne 124) échouent tous deux aujourd'hui —
  vérifié contre nuget.org (BlobNotFound pour les deux identifiants). Un évaluateur au premier jour qui
  suit le README bute sur une impasse dès la première étape pour deux des trois installations annoncées.

### 12.2 Installation et première erreur

`dotnet add package FirstClassErrors` suffit véritablement : aucune dépendance d'exécution n'arrive
avec, et les 18 analyseurs sont livrés dans le paquet — aucun second paquet à connaître. Le builder par
étapes rend ensuite la première erreur difficile à mal construire : `DomainError.Create(code,
diagnosticMessage)` ne produira pas d'`Error` tant que `WithPublicMessage(...)` n'a pas été appelé, et
IntelliSense porte des conseils substantiels (la correspondance RFC 9457 sur les propriétés de message,
des conseils de rédaction dans la DSL, la justification doctrinale sur `Error` lui-même). La couverture
de la documentation XML est totale, et cela se voit : c'est une bibliothèque qu'un développeur peut
apprendre en grande partie depuis les infobulles.

Lacunes connues à cette étape : aucun énoncé à destination de l'utilisateur du **SDK/IDE minimal requis
pour que les analyseurs embarqués se chargent** (le plancher Roslyn 4.8.0 n'est documenté que dans la
documentation mainteneur — un consommateur sur un VS ancien voit les analyseurs silencieusement absents,
sans aucune doc pour l'expliquer), et les sentinelles ne-lève-jamais (`#UNSPECIFIED`,
`#MISSING_SHORT_MESSAGE`…) qu'un opérateur peut réellement rencontrer en production ne peuvent être
cherchées nulle part dans la documentation rédigée à la main.

### 12.3 Le retour d'information à la compilation : les analyseurs

L'expérience analyseur est soigneusement conçue pour être digne de confiance : les règles actives par
défaut sont cantonnées aux défauts avérés et aux erreurs probables ; chaque règle heuristique (nommage,
listes d'interdiction, détection de données sensibles) est opt-in, avec une documentation franche de ses
modes de faux positifs et de la suppression exacte à utiliser. Les diagnostics renvoient vers des pages
de documentation bilingues par règle qui résolvent réellement. Deux lacunes de DX maintiennent cela sous
la barre fixée par les suites d'analyseurs auxquelles ce projet sera comparé (Roslyn, xUnit,
Meziantou) :

* **Zéro code fix provider.** Sept règles ont des correctifs déterministes ; le stratégique est FCE009,
  dont le correctif *échafauderait le squelette de documentation DescribeError* — enseignant activement
  la convention que la suite applique, au moment où le développeur est le plus réceptif.
* **L'invariant central de la bibliothèque n'est pas appliqué : un `Outcome` silencieusement ignoré.**
  Un `error.ToException()` dont le résultat est ignoré obtient FCE016, mais `validator.Check(request);`
  — laisser tomber un `Outcome` par terre, précisément l'échec que la bibliothèque existe pour éliminer
  — ne produit aucun diagnostic. C'est le plus grand écart entre la thèse et son application
  (recommandée comme FCE019 au §14). Une seconde règle peu coûteuse et très alignée : les conflits
  nom/type d'`ErrorContextKey` sont une `InvalidOperationException` d'exécution documentée, détectable
  statiquement exactement comme FCE001 détecte les collisions de codes.

### 12.4 Le moment du catalogue

`fce generate` est le moment « aha » du produit, et tient l'essentiel de sa promesse : GettingStarted
atteint un catalogue généré en six étapes honnêtes, la discipline stdout/stderr de la CLI et son contrat
de codes de sortie rendent naturelle la composition en CI, et les échecs sont rapportés sous des codes
`GENDOC_*` stables (l'outil pratique ce que la bibliothèque prêche). Points de friction : la page de doc
du renderer HTML omet le `--service-name` obligatoire de ses exemples à copier-coller (la commande
échoue telle qu'écrite) ; `fce generate` n'élague jamais les sorties périmées, si bien que les adoptants
qui câblent la boucle de publication CI documentée accumulent des pages fantômes pour les erreurs
supprimées, à moins de redécouvrir le contournement `rm -rf` enfoui dans un commentaire de workflow du
dépôt lui-même ; et les échecs opérationnels *propres* de la CLI (renderer introuvable, baseline
manquante) sont des chaînes de forme libre sans code — l'histoire de l'erreur de première classe
s'arrête aujourd'hui à la frontière de GenDoc.

### 12.5 Développement quotidien, débogage et tests

L'ergonomie au quotidien est solide : le vocabulaire `Then`/`Recover`/`Finally` est petit et prévisible,
la parité sync/async fait que la forme du pipeline survit à un refactoring asynchrone, les attributs
`DebuggerDisplay` et la séparation en trois messages rendent l'inspection lisible, et
`InstanceId`/`OccurredAt`/`Context` donnent par défaut aux logs des points d'accroche de corrélation.
L'histoire du test est un différenciateur sur le papier — horloges et identifiants gelés, assertions
d'outcome, valeurs arbitraires semables avec rejeu du run entier — avec deux réserves côté adoption que
l'audit a vérifiées : les propres grandes suites du dépôt n'optent jamais pour `Reproducibly` (si bien
que la propriété de rejeu phare n'a aucun kilométrage en conditions réelles — un échec causé par une
valeur arbitraire fortuite dans la suite principale est aujourd'hui impossible à rejouer), et
l'adaptateur xUnit anticipé (`[ReproducibleFact]`) n'a jamais été construit. `ErrorAssertion` s'arrête
aussi une marche avant les assertions que recommande son propre guide (aucune vérification fluide du
type d'erreur, de la transience ou de l'erreur interne ; les utilisateurs retombent sur l'échappatoire
`Subject`).

### 12.6 La falaise d'adoption : de la bibliothèque au service qui tourne

Le constat de DX le plus lourd de conséquences est le **dernier kilomètre manquant**. Le binder retourne
délibérément des outcomes agnostiques du framework, mais :

* Aucun exemple ni aucune doc nulle part ne montre la projection d'un échec d'`Outcome` sur une réponse
  HTTP (statut, type de problème RFC 9457, messages publics, erreurs par argument) — alors même que le
  catalogue généré *émet déjà* des URI de type de problème et des exemples RFC 9457.
* Prêts à l'emploi, les chemins d'erreur du binder sont des noms de propriété C#, pas des clés de fil :
  chaque consommateur réel d'une API JSON en camelCase/snake_case doit écrire à la main un
  `IArgumentNameProvider` avant que les chemins `REQUEST_ARGUMENT_*` ne correspondent à son JSON — et la
  propre documentation générée du binder répertorie ce décalage comme diagnostic d'échec connu. Le
  commentaire de code qui anticipe « a host integration package may add richer helpers » n'a aucun
  paquet derrière lui.
* Une installation du binder seul perd silencieusement les analyseurs (§10.3), si bien que le
  consommateur le plus susceptible d'être en train de documenter une surface d'erreur de frontière est
  celui qui a le moins de chances de disposer des règles qui l'appliquent.

Rien de tout cela ne bloque un adoptant déterminé, mais cela place en tout début de parcours exactement
le travail d'intégration qu'une première évaluation pénalise. Le §13 développe la réponse par paquet
d'intégration.

### 12.7 Verdict

La surface développée est polie à un degré inhabituel — IntelliSense, analyseurs, ergonomie de la CLI et
coutures de test témoignent tous d'un soin DX délibéré. L'expérience se dégrade aux *bords que le projet
n'a pas encore construits* : deux vitrines inexactes, un dernier kilomètre HTTP manquant, la friction des
noms de fil dans le binder, aucun code fix, et un angle mort sur l'`Outcome` ignoré. Tout cela est
traitable sans toucher à la conception cœur, et plusieurs points (correctifs de vitrine, liens de doc,
exemples `--service-name`) coûtent quelques minutes.
## 13. Analyse des manques fonctionnels

Cette analyse prend pour hypothèse l'ambition affichée du projet — devenir l'une des meilleures bibliothèques de son domaine — et demande ce qui manque *à l'aune des propres exigences du projet*. Elle s'appuie sur une comparaison concurrentielle (ErrorOr, FluentResults, OneOf, LanguageExt, CSharpFunctionalExtensions, Ardalis.Result, DotNext.Result, et la véritable solution en place : les exceptions brutes + ProblemDetails d'ASP.NET Core), une revue de la surface d'intégration et une revue de l'outillage. Les idées dictées par la mode sont explicitement remisées au §13.6.

### 13.1 Là où FirstClassErrors gagne déjà : un fossé défensif en quatre volets que personne d'autre n'occupe

Avant les manques, la position : FirstClassErrors n'est **pas une énième bibliothèque de résultats**. Chaque concurrent grand public se centre sur l'abstraction résultat/transport ; FirstClassErrors se centre sur la *définition de l'erreur* — code stable, messages à triple audience, contexte typé, taxonomie — et traite `Outcome` et les exceptions comme des transports interchangeables d'une même définition. Quatre capacités n'existent chez aucun concurrent :

1. un **catalogue d'erreurs généré depuis le code, versionné et localisé** (HTML/Markdown/JSON, cinq cultures, exemples RFC 9457) ;
2. la **détection de changements cassants du contrat d'erreurs** (`fce catalog diff` contre une référence commitée, avec gate de release conformément à l'ADR-0010) ;
3. **18 analyseurs Roslyn livrés** qui font respecter le modèle à la compilation, embarqués dans le paquet principal ;
4. un **binder de frontière** qui réutilise les fabriques de value objects comme validation et agrège tous les échecs sous une erreur enveloppe documentée.

Capacités supplémentaires au niveau du modèle qui dépassent leurs équivalents concurrents : la séparation des messages publics/internes imposée par le builder par étapes, le vocabulaire typé de clés de contexte enregistrées (face à des métadonnées `Dictionary<string,object>`), la sémantique opérationnelle dans la taxonomie (`Transience`, `InteractionDirection`), et la doctrine de construction qui ne lève jamais. Le document de comparaison lui-même est de qualité de référence — daté, sans classement, autocritique — bien qu'il ne couvre que deux des sept alternatives pertinentes ou plus.

### 13.2 Le manque au levier le plus fort : le dernier kilomètre HTTP (Critique)

**Problème.** Le modèle cœur a été *conçu* pour la RFC 9457 — `ShortMessage` est documenté comme le `title` du problème, `DetailedMessage` comme `detail`, et le catalogue généré frappe des types de problème `urn:problem:{service}:{code}` (le CLI exige `--service-name` précisément pour les rendre). Pourtant **aucun code d'exécution ne projette un `Error` vers une forme ProblemDetails, nulle part** ; le constructeur d'URN ne vit que dans l'outillage GenDoc en net8.0, inaccessible depuis une application netstandard2.0 déployée. Chaque adoptant écrit à la main l'unique projection où le principe de conception 3 (séparation public/interne) peut être silencieusement violé, et les exemples HTTP documentés du catalogue peuvent dériver librement par rapport aux réponses réelles de l'API — *exactement la dérive que la thèse de la bibliothèque existe pour éliminer, déplacée à la frontière HTTP*. La page de comparaison concède même cet axe (« Choose ErrorOr when … error types are mapped to HTTP responses »).

**Proposition.** Deux couches, suivant le précédent des paquets satellites propre au dépôt (RequestBinder, Testing, Dummies) :

* Dans le cœur (sans dépendance, netstandard2.0) : une projection neutre —

```csharp
ProblemDetailsModel problem = error.ToProblemDetails(new ProblemDetailsOptions {
    ServiceName = "checkout-api",          // type = urn:problem:checkout-api:payment-declined
    CatalogBaseUri = "https://docs.co/errors/2.4.0/",
    IncludeDetailedMessage = true          // opt-in explicite, en miroir du contrat de DetailedMessage
});
// title = ShortMessage, detail = DetailedMessage, extensions : code, instanceId, occurredAt.
// DiagnosticMessage est inaccessible depuis cette API — par construction.
```

Cela exige d'extraire le constructeur d'URN `ProblemType` de GenDoc vers le cœur (ou un fichier source partagé) afin que l'exécution et le catalogue composent *de façon prouvable* le même URN.

* Un paquet `FirstClassErrors.AspNetCore` (net8.0+) : des terminaux `Outcome<T>` → `IResult`/`IActionResult`, un `IExceptionHandler` pour `DiagnosableException`, une politique de statut par défaut pilotée par la taxonomie (DomainError/PrimaryPortError → 4xx ; SecondaryPortError selon `Transience` → 502/503/500) avec des surcharges par code, et `services.AddFirstClassErrors(serviceName, …)` qui fait aussi le pont vers `RequestBinderOptions.Default` au moment de la composition.

**Qui en bénéficie :** les équipes API (le plus grand segment d'adoptants) et le support/l'exploitation (un `type` sur le fil qui se résout vers la page de catalogue publiée). **Pourquoi cela s'inscrit :** cela fait de la projection sûre le comportement par défaut et complète la promesse du catalogue ; c'est un *transport* dans le vocabulaire même du projet. **Valeur : Critique** — c'est le manque sur lequel les évaluateurs buteront en premier.

### 13.3 Manques à forte valeur

**(a) Livrer la projection de log canonique (Forte valeur).** `LoggingIntegration.en.md` enjoint de « project an Error to a log model once, in one place » — puis fait recopier à chaque adoptant ~25 lignes dont les subtilités (les `InnerErrors` récursifs, les champs propres à `InfrastructureError`, la distinction `InnerException`) sont exactement là où une copie manuelle se dégrade. La projection dérive entièrement de la forme du modèle, pas d'une politique applicative ; sa maison est donc la bibliothèque : `Error.ToLogModel()` renvoyant `IReadOnlyDictionary<string, object?>`, netstandard2.0, zéro dépendance ; la doc se réduit à une ligne. Définir les noms de champs résultants comme un schéma stable et documenté corrige du même coup le manque de corrélation inter-services (deux services bâtis sur cette bibliothèque émettent aujourd'hui des événements d'erreur structurellement différents, sauf à ce que les équipes se coordonnent hors bande).

**(b) La capture d'exceptions en frontière, aux conditions du projet (Forte valeur).** La FAQ prescrit le patron adaptateur (attraper l'exception tierce → la modéliser via une fabrique documentée → conserver l'exception d'exécution comme cause technique) et `DiagnosableException(error, innerException)` existe pour cela — mais il n'y a pas d'`Outcome.Try`, si bien que le patron d'adaptateur d'infrastructure le plus courant est du boilerplate à chaque site d'appel. Contrairement au `Result.Try` auto-enveloppant de FluentResults (qui violerait le principe 1), la signature devrait *forcer* une `Error` produite par fabrique :

```csharp
Outcome<Receipt> outcome = Outcome.Try(
    () => providerClient.Charge(order),
    ex => PaymentProviderError.Unavailable(ex));   // doit renvoyer Error — votre fabrique documentée
```

**(c) Des fournisseurs de correctifs de code, avec l'échafaudage FCE009 en produit-phare (Forte valeur).** Les 18 diagnostics ne font tous que signaler. La seule vraie taxe d'adoption de la bibliothèque est le boilerplate de ~25 lignes de méthode de documentation par erreur — précisément la forme d'un échafaudage. Un correctif FCE009 générant le câblage `[DocumentedBy]` plus un squelette `DescribeError` transforme la suite d'analyseurs de critique en pédagogue, au moment de réceptivité maximale. Les correctifs mécaniques pour FCE004/006/008/016 viennent presque gratuitement dans la foulée.

**(d) Un analyseur d'`Outcome` ignoré — FCE019 (Forte valeur ; sans doute l'élément isolé le plus aligné sur la philosophie de tout ce rapport).** `validator.Check(request);` laissant tomber silencieusement un `Outcome` est le mode d'échec central de la bibliothèque, et aucune règle ne le signale (FCE016 ne couvre que le `ToException()` ignoré). Sévérité Warning, activée par défaut, en miroir de la logique de détection d'abandon de FCE016, indexée sur le type de retour.

**(e) `fce lint` — des règles de qualité de contenu sur le catalogue extrait (Forte valeur).** Le texte de documentation peut légitimement provenir de ressources localisées, que les règles Roslyn fondées sur les littéraux (FCE005/014/015) ne peuvent structurellement pas voir — une équipe qui localise perd silencieusement *toute* l'application des règles de qualité de texte. Un verbe CLI appliquant les checklists de BestPractices/WritingErrorsGuide au catalogue post-extraction, post-localisation (par locale, avec les conventions existantes `--report`/`--fail-on`/codes de sortie) referme l'angle mort et rend exécutables les checklists en prose.

**(f) Élargir la page de comparaison (Forte valeur, documentation seule).** Ajouter la solution en place comme référence — exceptions + ProblemDetails intégré — déroulée sur le même scénario de paiement, plus une courte table de positionnement pour les familles union/fonctionnelles (OneOf/LanguageExt, CSharpFunctionalExtensions, Ardalis.Result). La FAQ contient déjà les arguments ; ils ne sont pas exposés là où l'évaluation se joue.

### 13.4 Manques à valeur moyenne

* **Agrégation multi-erreurs côté domaine.** La table de comparaison concède que « aggregating independent errors is left to the application ». La forme fidèle aux principes existe (les échecs se regroupent sous une *erreur enveloppe documentée*, jamais une liste anonyme — exactement le fonctionnement de `Bind.Request`) : un `Outcome.Combine(envelopeFactory, outcome1, …, assembler)` conditionné à une fabrique d'agrégat documentée, plafonné en arité dans l'esprit de l'ADR-0015, transformerait la concession en « intégré, à nos conditions ».
* **Conventions sémantiques OpenTelemetry, documentation d'abord.** Le chemin d'investigation documenté (« alerte → traceId → InstanceId de l'erreur → entrée de catalogue ») présuppose le tracing, et pourtant aucune convention d'attributs n'existe (`firstclasserrors.code`, `.instance_id`, `.transience`, …) — les consommateurs reproduiront au niveau de la télémétrie la dérive de vocabulaire que la bibliothèque interdit au niveau des clés de contexte. Publier la page de conventions d'abord ; un helper `Activity` minimal seulement si la demande le confirme.
* **Recette de retry pilotée par la transience (documentation seule).** `Transience` existe pour répondre à « réessayer a-t-il un sens ? » et aucune page ne montre son branchement sur une politique de retry (p. ex. un prédicat Polly sur `InfrastructureError { Transience: Transient }`).
* **Un contrat JSON en sérialisation seule pour les erreurs à l'exécution** (aligné sur le modèle de log ; la réhydratation explicitement hors périmètre — la désérialisation contournerait les constructeurs validants, en violation de la doctrine de CLAUDE.md ; une représentation d'*erreur reçue* est la bonne conception future si la propagation inter-services est un jour souhaitée — recommander une ADR avant tout code).
* **Des JSON Schemas pour le catalogue JSON et la référence** (l'affirmation « stable technical pivot » est aujourd'hui invérifiable par les intégrateurs ; s'apparie naturellement avec `CatalogSnapshot.CurrentSchema`).
* **Une déclaration de support AOT/trimming pour le binder** (la chaîne de repli interpréteur/réflexion existe déjà dans le code ; la promouvoir en note de compatibilité destinée aux utilisateurs ; ranger le générateur de source dans la voie de succession consignée par l'ADR-0023 — ne pas l'accélérer).
* **Une table de correspondance gRPC (documentation seule)** — CoreConcepts nomme gRPC comme transport ; HTTP a eu sa table de correspondance, gRPC n'a rien eu ; `ErrorCode` + contexte se projettent presque mot pour mot sur `google.rpc.ErrorInfo`. Aucun paquet gRPC n'est justifié par les preuves actuelles.
* **Deux analyseurs structurels pour des règles de checklist documentées mais non appliquées :** la construction d'erreurs inline hors des types `[ProvidesErrorsFor]`, et les chaînes interpolées passées comme messages publics (l'anti-patron que `WritingErrorMessages` imprime mot pour mot). Opt-in en sévérité Info, à l'image de leurs semblables FCE003–005.
* **Automatisation de l'hygiène documentaire** (vérificateur de liens, y compris les URL d'aide IDE codées en dur ; contrôle de paire et de fraîcheur EN/FR ; contrôle de couverture des docs de workflow ; contrôle d'inventaire du hub documentaire) — le correctif systémique du §11, le tout dans le patron `tools/` existant.
* **`fce config init` en véritable amorçage** (détecter la `.sln`, préremplir `serviceName`/la référence) et **une vérification de compilation des extraits** pour les guides riches en API.
* **Un projet de benchmark du cœur `Outcome`/`Error`** (en miroir du harnais exemplaire du binder) plus une ligne performance/allocations dans la table de comparaison. La politique classe-plutôt-que-struct repose sur l'affirmation empirique que les chemins d'erreur ne sont pas des boucles chaudes ; mesurer le coût réel par appel transforme cette affirmation d'assertion en preuve, arme la comparaison face à un ErrorOr fondé sur des structs, et permet à la section sur les boucles chaudes de `WhenNotToUse` de nommer le coût *réel* (les allocations, pas la création d'exceptions).
* **Complétion de la santé communautaire :** CODE_OF_CONDUCT, gabarits d'issues, un pointeur SUPPORT, et — le plus précieux pour un projet à *bus factor* 1, développé avec assistance d'IA — une courte note de continuité/gouvernance plus une phrase destinée aux utilisateurs sur le modèle de développement assisté par IA que le dépôt gouverne déjà si soigneusement en interne.

### 13.5 Forces délibérées à laisser en l'état

Le cœur agnostique au framework et zéro-dépendance ; le modèle de composition ambiant (sans DI) ; les sélecteurs par arbres d'expression pour la v1 (le report mesuré de l'ADR-0023) ; la règle classe-plutôt-que-struct ; le vocabulaire `Then`/`Recover`/`Finally`. Chacune est gouvernée par une ADR ou par CLAUDE.md avec une justification saine ; chaque manque ci-dessus est comblable *sans* y toucher.

### 13.6 Hors périmètre — des idées séduisantes, et rejetées à raison

| Idée | Pourquoi elle doit rester dehors |
|---|---|
| Alias de combinateurs Map/Bind/Match/LINQ | Explicitement rejetés (ADR-0003, FAQ) : le nommage par intention *est* l'identité de l'API ; des alias réintroduiraient la dualité que la décision a supprimée. |
| Résultats struct / conversions implicites `T`→`Outcome` | Interdits par la doctrine documentée d'invariants de classe ; l'argument ergonomique phare d'ErrorOr éroderait le vocabulaire explicite Success/Failure sur lequel la conception par étapes est bâtie. Les chemins d'erreur ne sont pas des boucles chaudes. |
| Fabriques/documentation générées par générateur de source | Supprimerait la propriété « construction et documentation revues ensemble » dont dépend le principe 4. (À distinguer du générateur de source pour les sélecteurs du binder, que l'ADR-0023 consigne à raison comme successeur possible post-v1.) |
| Publieurs maison Confluence/Backstage/site statique | Contredit la position documentée (« publishing is left to your CI/CD ») ; les pivots JSON/Markdown + les recettes CI + le contrat de plugin de rendu sont la surface d'intégration voulue. Documenter des recettes à la place. |
| Packs de thèmes HTML, extension VS Code, gabarits `dotnet new`, export SARIF, génération de doc assistée par IA | Effet de mode ou redondance : le moteur de rendu a déjà light/dark + i18n ; les correctifs de code livrent l'échafaudage à l'intérieur de la chaîne d'outils ; les diagnostics Roslyn remontent déjà nativement en CI ; une prose générée saperait « la documentation est un savoir revu ». |
| Une abstraction de DI dans le cœur | Le cœur n'a besoin d'aucun conteneur ; ajouter `M.E.DependencyInjection.Abstractions` romprait la posture zéro-dépendance sans aucun bénéfice modélisé. L'adaptateur ASP.NET fait le pont sur l'unique couture de composition. |
| Découverte automatique des catalogues des paquets référencés | Déjà écartée par un argument imparable (ADR-0019) : un binaire référencé ne peut exposer que ses valeurs par défaut ; le catalogue du consommateur est le seul lieu de documentation fidèle. |
## 14. Améliorations recommandées

Voici le backlog consolidé et classé. Chaque entrée nomme le problème, le correctif, et l'endroit où vit la discussion complète. « Critique » est employé avec parcimonie, conformément à la définition du tableau : *les problèmes qui devraient être traités avant toute évolution future*.

### Critique

| # | Recommandation | Pourquoi critique | Où |
|---|---|---|---|
| C1 | **Rédiger a posteriori les ~6 ADR fondatrices** (dualité Outcome/exception ; cible netstandard2.0 ; règle class-et-non-struct ; schéma d'ID FCE + embarquement des analyseurs ; extraction par réflexion de GenDoc ; politique bilingue) en `Proposed`, chacune énonçant qu'elle consigne une décision antérieure à la pratique | L'ADR-0004 fait de la base d'ADR le point de contrôle par PR ; des fondations non consignées sont inapplicables à ce point de contrôle. Surtout une relocalisation de justifications existantes, pas une invention | §6.5 |
| C2 | **Refermer la fenêtre de séquencement de publication :** publier (ou a minima réserver) l'identifiant `Dummies` ; à la première release lib qui suit, remplacer la `Dummies.dll` embarquée dans Testing par une vraie `PackageReference` | Les deux risques ne sont ouverts *que* jusqu'à la première publication ; chaque garde-fou nécessaire existe déjà | §10.3 |
| C3 | **Corriger les vitrines :** le compte d'analyseurs du README du cœur (16→18) ; l'arbre de commandes du README de la CLI (`fce config renderer …`), la formulation de `config show`, l'ajout des commandes `catalog` ; et corriger la section d'installation du README du dépôt, qui prescrit d'installer `FirstClassErrors.RequestBinder` et l'outil `fce` — ni l'un ni l'autre sur NuGet (les marquer « à venir avec la première release lib/cli » ou les publier) | Quelques minutes de travail ; des surfaces de premier contact qui induisent aujourd'hui activement en erreur chaque évaluateur, dont deux commandes d'installation mortes | §12.1 |
| C4 | **Corriger les quatre dérives doc/API confirmées :** la phrase de seeding périmée de `DeterministicTesting` EN+FR ; les lignes de scope `binder`/`dummies` manquantes du CONTRIBUTING FR ; les 20 ancres de pied de page FR mortes ; le « scope carries no versioning weight » de `CONTRIBUTING.md` face à l'outillage de release qui écarte les commits sans scope | Chacune est un endroit où suivre la doc produit des résultats faux ; celle de CONTRIBUTING peut effacer silencieusement des changements visibles de l'utilisateur du registre des releases | §11.4, §11.5 |
| C5 | **Livrer la projection Error→ProblemDetails** (modèle neutre dans le cœur réutilisant le constructeur d'URN `ProblemType` de GenDoc ; satellite `FirstClassErrors.AspNetCore` pour `IResult`/politique de statut/gestionnaire d'exceptions) | Le manque concurrentiel au levier le plus fort ; referme la boucle de dérive catalogue-exécution que la thèse exige | §13.2 |
| C6 | **Ajouter FCE019 : `Outcome` ignoré** (avertissement, activé par défaut) | L'invariant central de la bibliothèque — une erreur ne doit pas être perdue silencieusement — n'est aujourd'hui pas appliqué | §12.3, §13.3(d) |

### Forte valeur

1. **Lier les projets d'exemple depuis README/GettingStarted/DocumentationMap** et réconcilier les deux hubs de documentation (ajouter à chacun les six guides manquants) — les meilleurs supports pédagogiques du projet sont introuvables (§11.2).
2. **`Error.ToLogModel()`** — livrer la projection de log canonique ; définir ses noms de champs comme le schéma d'exécution stable (§13.3a).
3. **`Outcome.Try(action, ex => FactoryError(ex))`** — capture d'exception en frontière qui *exige* une erreur de fabrique documentée (§13.3b).
4. **Fournisseurs de code-fix** pour FCE009 (échafaudage de DescribeError — le porte-étendard), FCE004/006/008/016 (§13.3c).
5. **`fce lint`** — des règles de qualité de contenu sur le catalogue extrait, post-localisation (§13.3e).
6. **Durcir le harnais de test des analyseurs** (échouer sur les erreurs de compilation dans les extraits ; asserter les positions/arguments de message sur les positifs primaires) et ajouter un analyseur de conflits nom/type d'ErrorContextKey (le patron FCE001 appliqué à un plantage à l'exécution documenté) plus un test de cohérence descripteur↔documentation (§8.2-Analyzers).
7. **Automatiser l'hygiène documentaire sur le patron `tools/` :** contrôle de paires+fraîcheur EN/FR, vérificateur de liens/ancres relatifs (gardant aussi les URL de liens d'aide IDE codées en dur), contrôle de couverture des docs de workflow, contrôle d'inventaire des hubs de doc (§11.4, §11.6).
8. **Codifier la politique bilingue réelle** dans CLAUDE.md/AGENTS.md/le template de PR (l'anglais canonique ; chaque fichier de `doc/handwritten/` en miroir ; le FR mis à jour dans la même PR) (§11.4).
9. **Combler les trois manques de la référence des workflows** (pages canary, dummies, gendoc-docs) et rafraîchir les pages ci/release-dryrun (§11.5).
10. **Compléments de symétrie d'API :** `Outcome.Then<TResult>(Func<TResult>)` ; un `Create` à erreur interne unique sur les erreurs de port ; décider et déclarer le contrat d'extensibilité de la taxonomie (le fermer, ou ouvrir `PublicMessageStage`) (§9.3).
11. **Supprimer le verrou/écriture par requête de `RequestBinderOptions.Default`** (chemin rapide volatile à double vérification) et faire échouer bruyamment la réutilisation terminale (§8.2-Binder).
12. **Livrer le compagnon d'intégration hôte du binder** (un `IArgumentNameProvider` conscient du sérialiseur lisant la politique de nommage System.Text.Json ; des helpers d'extraction `HttpRequest`) — se fond naturellement dans le paquet AspNetCore de C5 (§12.6).
13. **Faire parvenir les analyseurs aux consommateurs binder-seul** (les embarquer dans le paquet du binder, ou documenter l'exigence de référence compagne) (§10.3).
14. **Câbler le contrôle de version du contrat de documentation** que les docs XML de l'attribut promettent déjà, ou retirer l'affirmation (§8.2-GenDoc).
15. **Ajouter l'élagage des sorties périmées à `fce generate`** (ou documenter l'exigence de `rm -rf` pour les adoptants) (§12.4).
16. **Élargir la page de comparaison** avec la base de référence exceptions+ProblemDetails et les familles union/fonctionnelles (§13.3f).
17. **Combler le manque d'adoption de la reproductibilité :** construire l'adaptateur xUnit façon `[ReproducibleFact]` (ou envelopper dans `Reproducibly` les suites riches en valeurs arbitraires) ; donner à Dummies une section d'accueil dans le README et un guide utilisateur avant `dum-v0.1.0` (§12.5, §10.3).
18. **Faire trancher par le mainteneur le statut de l'ADR-0025** (l'implémentation est fusionnée et consommée ; l'ADR est toujours Proposée) (§6.4.5).
19. **Compléter la surface de santé communautaire :** CODE_OF_CONDUCT, templates d'issue, pointeur SUPPORT, une courte note de continuité/gouvernance (qui peut agir si l'unique mainteneur ne le peut pas), une phrase à destination des utilisateurs sur le modèle de développement assisté par IA, et réconcilier l'invitation du README à ouvrir des issues avec les réglages d'issues du dépôt (§5.6).
20. **Réconcilier le registre des releases avec la préversion livrée :** compléter rétroactivement les trois changelogs avec `0.1.0-preview.1`, publier (ou remplacer) le brouillon de GitHub Release sous le schéma préfixé par train, et ajouter à `release.yml` une étape de promotion d'`[Unreleased]` pour que l'affirmation Keep-a-Changelog devienne vraie dès la prochaine coupe (§5.1.3).
21. **Durcir `code_review.md`** avec la règle traiter-le-contenu-analysé-comme-des-données que ses prompts frères portent déjà, et documenter ce qui le lie/le consomme (§5.6).

### Valeur moyenne

Rédiger l'ADR lockstep/IVT (§10.3) ; un `Outcome.Combine` côté domaine conditionné à une fabrique d'enveloppe documentée (§13.4) ; une page de conventions sémantiques OTel (doc d'abord) et une recette Transience/Polly (§13.4) ; un schéma JSON d'erreur en sérialisation seule + des JSON Schemas pour le catalogue/la baseline (§13.4) ; une déclaration AOT/trimming pour le binder (§13.4) ; une table de correspondance gRPC (§13.4) ; unifier les résolveurs dupliqués de la CLI et extraire le helper partagé de rendu d'exemple RFC 9457 (§8.3) ; donner aux commandes config/renderer de la CLI le patron de ports + des tests, et ajouter un étage argv `CommandAppTester` (§8.2-CLI) ; étendre `ErrorAssertion` aux assertions que le guide lui-même recommande (§12.5) ; un guide de namespacing des clés de contexte + une seule convention de nommage à travers toutes les docs (§9.3c) ; embarquer les attributs de flux `[NotNullWhen]` (§9.3d) ; documenter l'idiome de pont exception→Outcome (§9.3e) ; un exemple d'argument hors-DTO + réconcilier l'affirmation de couverture du guide RequestBinder (§11.3) ; réconcilier l'exemple `Amount` avec `UsagePatterns` (§11.3) ; pratiquer le dogfooding de `FirstClassErrors.Testing` dans les tests d'exemple (§11.3) ; un exemple de mapping transport (Outcome → record de réponse problème) (§12.6) ; deux analyseurs structurels (construction inline ; messages publics interpolés) (§13.4) ; faire passer `github.head_ref` par des variables d'environnement et documenter l'effet de checks orphelins de gendoc-docs (§11.5) ; un lot d'entretien des ADR (énumérer l'ensemble affecté par l'ADR-0024 ; affûter la phrase de décision de l'ADR-0017 ; rendre vraies les docs référencées par l'ADR-0022 ; des annotations d'index pour « refines » ; les doubles dates en cas de remplacement ; un garde-fou CI d'unicité des numéros) (§6.4) ; écrire noir sur blanc les conventions de test et étendre la couverture par propriétés aux invariants de contexte/erreurs internes (§8.2-Tests) ; exécuter la checklist de première release (dispatch en dry-run + test de fumée d'installation de l'outil) avant le premier vrai tag de train (§11.5) ; ajouter un projet de benchmark Outcome/Error au cœur et une ligne performance/allocation à la page de comparaison (§13.4) ; ajouter un contrôle de synchronisation pour le texte de règle tripliqué CLAUDE/AGENTS/code_review et normaliser le nom/l'emplacement de `code_review.md` (§5.6) ; renommer l'ancre de la référence d'implémentation de l'ADR-0022 et le reste de l'entretien des ADR tel que listé ; une page de synthèse de la chaîne d'approvisionnement à destination des mainteneurs, consolidant les politiques par fichier (§11.5).

### Priorité basse

Rendre `ErrorCode.Unspecified` public ; chaîner les trois constructeurs d'`Error` ; corriger les points de dérive des docs XML (exception documentée-mais-jamais-levée, référence `To` périmée) ; une fabrique params/`IEnumerable` pour `PrimaryPortInnerErrors` ; le message de construction par défaut de `BindingScope` ; une liste vide mise en cache pour les listes optionnelles absentes ; un rangement de la taxonomie de solution (configurations de build du csproj Documentation, les exemples hors du dossier `tests`, un seul idiome `InternalsVisibleTo`) ; un bootstrap `fce config init` ; des écritures atomiques de baseline/instantané ; normaliser/documenter le code de sortie d'erreur d'analyse de Spectre ; traduire ou exempter le règlement des ADR ; élargir les matrices de test d'analyseurs trop minces ; étiqueter les conjonctions FsCheck ; une couture de fumée pour GenDoc.Worker ; faire converger le `DocumentationFormatter` dupliqué ; balayer la constellation de pointeurs périmés (préambule de changelog, note « English only », exemple de format de tag, citation de l'allowlist de trains, incohérence de préfixe de code des exemples) ; des recettes de publication (TechDocs/MkDocs/Pages) sous forme de docs ; un renderer OpenAPI comme plugin d'exemple documenté ; hisser `WhenNotToUse` au standard maison (et corriger sa section sur les boucles chaudes pour nommer l'allocation, et non la création d'exception, comme le coût du chemin Outcome) ; réparer la réciprocité des chaînes précédent/suivant ; réduire l'`icon.png` empaqueté de 919 Ko.

### Hors périmètre

Voir §13.6 — les alias de combinateurs fonctionnels, les résultats struct, les conversions implicites, les fabriques générées par source, les publishers de première partie, les packs de thèmes, les extensions/templates IDE, SARIF, les docs générées par IA, une abstraction DI dans le cœur et l'auto-découverte de catalogue inter-paquets ont tous belle allure et sont correctement rejetés par le raisonnement consigné du projet lui-même. Ce rapport recommande de *maintenir* ces rejets.
## 15. Feuille de route proposée

Séquencée pour que chaque phase réduise les risques de la suivante, dans le respect de la contrainte propre au projet selon laquelle les changements cassants ne sont bon marché que jusqu'à la v1. Les phases sont dimensionnées pour rester réalistes pour un mainteneur seul assisté d'agents — le mode de fonctionnement que ce dépôt exécute manifestement bien.

### Phase 0 — Sprint d'hygiène (quelques jours ; à faire avant tout le reste)

Tout ce qui suit se compte en minutes ou en heures pièce, et plusieurs points induisent activement les utilisateurs en erreur aujourd'hui : les deux README de nuget.org et les commandes d'installation mortes du README du dépôt (C3), les quatre dérives documentaires confirmées (C4), l'analyseur FCE019 des `Outcome` écartés (C6, une journée tests et pages de doc comprises), la résolution du statut de l'ADR-0025, les liens du hub de documentation et des exemples, la correction de l'exemple `--service-name`, le rattrapage des changelogs avec la `0.1.0-preview.1` déjà livrée, et les fichiers de santé communautaire (code de conduite, gabarits d'issue, une note de continuité). Clore ce sprint par les trois contrôles consultatifs (synchronisation FR, vérificateur de liens, couverture documentaire des workflows) convertit toute cette classe de dérive d'un nettoyage manuel récurrent en contrôles rouges — la propre médecine du projet, appliquée à ses dernières surfaces maintenues à la main.

### Phase 1 — Clôture des fondations (1–2 semaines)

* **C1 : rédiger a posteriori les ADR fondatrices** (plus l'ADR lockstep/IVT). C'est la moitié manquante de la boucle de gouvernance, et cela ne deviendra jamais moins cher.
* **C2 : publier Dummies** (pour revendiquer l'identifiant), puis remplacer la DLL embarquée par une `PackageReference` au prochain pack de la bibliothèque ; doter Dummies de sa documentation d'accueil.
* Durcissement de la confiance dans les analyseurs : l'assertion de compilation du harnais, la règle de conflit d'ErrorContextKey, le test de cohérence descripteurs↔docs.
* Les décisions d'API pré-v1 tant que casser est gratuit : les complétions de symétrie, la décision sur le contrat d'extensibilité, le chemin rapide du getter `Default`, le garde-fou de réutilisation des terminaux, les attributs `[NotNullWhen]`. Chacune est petite ; ensemble, elles soldent tous les points de friction du §9.3.

### Phase 2 — Le dernier kilomètre (2–4 semaines ; la phase stratégique)

* **C5 : l'histoire ProblemDetails** — l'extraction de `ProblemType` vers le cœur, `error.ToProblemDetails(...)`, puis le paquet `FirstClassErrors.AspNetCore` (terminaux Outcome→`IResult`, politique de statut par taxonomie, gestionnaire d'exceptions, `AddFirstClassErrors(...)` faisant le pont vers les options du binder), en y intégrant le fournisseur de noms conscient du sérialiseur (la plus grosse friction du binder à la première utilisation).
* `Error.ToLogModel()` + le schéma documenté des champs à l'exécution ; `Outcome.Try` avec mapping par fabrique obligatoire.
* Étendre les exemples jusqu'à la consommation (un flux `Recover` asynchrone, `InfrastructureError`, le dogfooding du paquet Testing, le binding hors DTO, et un exemple de mapping de transport) — le paquet AspNetCore rend ces exemples naturels plutôt qu'artificiels.
* Élargir la page de comparaison. Après cette phase, la réponse à « pourquoi pas ErrorOr ? » est complète sur le propre terrain d'ErrorOr.

### Phase 3 — Premières publications (une fois les phases 1–2 livrées)

Exécuter la check-list de première publication déjà rédigée (une exécution à blanc par déclenchement manuel validant le trusted publishing ; le test de fumée d'installation de l'outil avant le premier tag `cli-v`). Livrer `lib-v0.x`, `cli-v0.x`, `dum-v0.x` — les premières publications passées par le pipeline *actuel* (la `0.1.0-preview.1` existante a été livrée sous le processus pré-train désormais remplacé, si bien que ces tags sont ce qui prouve réellement la machinerie : trusted publishing, attestation, le gate du catalogue et la promotion du changelog). Résoudre au passage la GitHub Release brouillon `v0.1.0-preview.1` dans le schéma du train. Rien dans ce rapport ne bloque une publication plus précoce si le mainteneur le préfère — mais publier après la phase 2 signifie que la première impression inclut l'histoire HTTP, et que la machinerie de changelog est exercée avec du contenu réel alors que les règles de scopes de CONTRIBUTING sont déjà corrigées.

### Phase 4 — Outillage de niveau référence (au fil de l'eau, tiré par la demande)

Les fournisseurs de correctifs de code (le scaffolding de FCE009 d'abord) ; `fce lint` ; les JSON Schemas ; la page de convention OTel et les recettes de retry ; l'adaptateur de reproductibilité ; les deux analyseurs structurels ; l'achèvement de la couture et des tests du CLI et le palier de tests argv ; la couture du lanceur de processus de GenDoc ; la vérification de compilation des snippets. Chaque point est indépendant — de bons lots de travail à la taille d'un agent — et aucun ne bloque les autres.

### Garde-fous permanents

Réévaluer à la v1.0 les déclencheurs d'extraction de Dummies de l'ADR-0011 ; planifier la mise en remplacement de l'ADR-0002 avant la fin du support LTS de .NET 8 (novembre 2026) ; relancer les benchmarks du binder si la surface des sélecteurs change (le propre déclencheur de l'ADR-0023) ; et garder rejetés les rejets du §13.6.
## 16. Conclusion

Cet audit s'était donné pour question de déterminer si FirstClassErrors est un projet open source cohérent, professionnel et maintenable qui pourrait raisonnablement devenir une référence dans son domaine. Après une exécution complète du build et des tests, treize revues de sous-systèmes, l'audit individuel des 26 ADR avec vérification de la conformité de l'implémentation, des analyses des manques concurrentiels et d'intégration, et la vérification contradictoire de chaque affirmation significative, la réponse est un oui assuré sur les trois premiers points et un conditionnel bien fondé sur le quatrième.

Ce qui distingue ce dépôt n'est pas un mécanisme en particulier mais une posture appliquée avec constance : **chaque affirmation que le projet fait sur lui-même est soit appliquée par une machine, soit honnêtement étiquetée comme non appliquée.** Les principes de conception sont compilés dans le système de types. La documentation est générée, diffée et gatée à la release. Les frontières de paquets sont couvertes par des tests d'architecture et assertées au pack. Les décisions d'API sont consignées avec leurs alternatives et verrouillées par des tests. Quand l'audit est parti chercher l'écart entre ce que le projet dit et ce qu'il est — le mode d'échec classique de l'open source ambitieux — il a trouvé cet écart presque entièrement confiné aux *vitrines et carrefours* écrits à la main, les quelques surfaces que la machine ne contrôle pas encore. Cette inversion (la dérive dans le méta-texte, la fidélité dans le système) est rare, et c'est le signal isolé le plus fort que l'ambition de référence est crédible.

Le chemin à partir d'ici est inhabituellement lisible, parce que les propres instruments du projet le définissent. Les décisions fondatrices ont besoin de leurs ADR pour que la boucle de gouvernance puisse les protéger. La fenêtre de séquencement de Dummies a besoin d'être refermée en exécutant une décision déjà prise. La promesse HTTP du catalogue a besoin de la projection à l'exécution qui la rend vraie. L'angle mort de l'`Outcome` écarté a besoin du seul analyseur que la thèse exige le plus évidemment. Et les dernières surfaces maintenues à la main ont besoin des mêmes contrôles consultatifs que le projet construit déjà pour tout le reste. Rien de tout cela n'est de la reprise ; tout relève de l'achèvement.

Deux observations pour clore, à l'intention du mainteneur. D'abord, la retenue documentée d'un bout à l'autre — les fonctionnalités différées, la comparaison sans classement, la page « quand ne pas l'utiliser », les rejets hors périmètre que ce rapport recommande de préserver — est un atout concurrentiel ; les bibliothèques de référence se définissent autant par ce qu'elles refusent que par ce qu'elles livrent. Ensuite, l'accomplissement le plus exportable du dépôt n'est peut-être pas du tout le modèle d'erreurs, mais la démonstration de la manière dont un projet maintenu en solo et assisté par agents peut tenir une discipline d'ingénierie de niveau industriel : des décisions gouvernées par ADR, une CI à preuve positive, du dogfooding sur artefact packé, et une documentation vivante bilingue, tout à la fois. C'est ce système qui portera FirstClassErrors à travers les releases, les intégrations et les contributeurs qui l'attendent.

---

*Audit conduit le 2026-07-20 contre la révision `3bf89e3` par une revue multi-agents orchestrée (13 revues de sous-systèmes, 26 audits par ADR, revue de corpus, 3 analyses de manques, exécution du build et des tests, vérification contradictoire de tous les constats de sévérité critique ou haute, et une passe de critique de complétude), synthétisée et éditée en ce rapport. Chaque constat cite des preuves du dépôt ; les verdicts de vérification sont reflétés dans les classifications.*
