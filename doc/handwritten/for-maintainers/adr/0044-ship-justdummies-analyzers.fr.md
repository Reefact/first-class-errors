# ADR-0044 | Fournir des analyseurs JustDummies de première partie, et garder avec eux la surface asynchrone reproductible

🌍 🇫🇷 Français (ce fichier) · 🇬🇧 [English](0044-ship-justdummies-analyzers.md)

**Statut :** Proposé
**Date :** 2026-07-27
**Décideurs :** Reefact

## Contexte

* `JustDummies` est une bibliothèque de support de test : toute sa valeur tient à ce qu'un arrangement cassé
  *échoue*. `Any.Reproducibly` exécute un corps de test sous une graine épinglée et la rapporte en cas d'échec. Elle
  surchargeait sur un seul nom une `Action` synchrone et un `Func<Task>` asynchrone.
* Ce jeu de surcharges cachait un piège à échec silencieux. Une lambda `async` est une meilleure conversion vers
  `Func<Task>` que vers `Action`, donc `Any.Reproducibly(async () => { ... })` se liait à la surcharge asynchrone, qui
  retournait un `Task`. Une méthode de test est en général un `void` synchrone : le `Task` retourné était jeté ; les
  assertions du corps s'exécutaient sur une continuation après le retour de la méthode, et l'échec ne surgissait — au
  mieux — que plus tard sous forme d'`UnobservedTaskException`. **Le test passait au vert.** Le `CS4014` natif du
  compilateur ne se déclenche pas dans une méthode synchrone : rien n'avertissait.
* Renommer la surcharge asynchrone en `ReproduciblyAsync`, conforme au TAP, corrige le nommage, mais seul il *rouvre*
  le piège de l'autre côté : `Reproducibly` n'ayant plus que des surcharges `Action`, une lambda `async` se lie à
  `Action` en **`async void`**, dont l'exception d'après le premier `await` échappe entièrement au `try/catch` de la
  portée reproductible.
* C# n'offre aucun `Task` non-jetable, ni aucun moyen d'interdire une conversion lambda-`async`→`Action`. Les deux
  erreurs résiduelles — passer un corps async à `Reproducibly`, et jeter un `Task` de `ReproduciblyAsync` — ne sont
  donc pas exprimables dans le système de types.
* `FirstClassErrors` fournit déjà des analyseurs Roslyn (`FCE001`…`FCE022`) dans son propre package NuGet.
  `JustDummies` n'en fournissait aucun, et c'est une bibliothèque **autonome, agnostique des erreurs** (un ADR le
  garde : elle ne doit jamais dépendre de FirstClassErrors). Une règle propre à JustDummies ne peut pas vivre dans
  `FirstClassErrors.Analyzers` — cet assembly est livré dans le package FirstClassErrors et porte l'identité FCE — un
  consommateur de JustDummies ne la recevrait jamais.
* D'autres gardes ont été envisagées et rejetées : une surcharge « poison » `[Obsolete(error: true)]` (un membre
  déprécié dans une 1.0 toute neuve est un contresens qui salit la surface livrée), et une surcharge asynchrone qui
  bloque avec `GetAwaiter().GetResult()` (le sync-over-async risque le *deadlock* sous un `SynchronizationContext`
  capturé — l'anti-pattern que l'async existe pour éviter).

## Décision

`JustDummies` fournit ses propres analyseurs Roslyn de première partie, dans un nouveau projet `JustDummies.Analyzers`
empaqueté dans le package NuGet `JustDummies` (`analyzers/dotnet/cs`), agnostique des erreurs et indépendant de
`FirstClassErrors`, sous un schéma d'identifiants de diagnostic propre à JustDummies (`JDxxx`, en miroir de `FCExxx`).

La première application rend la surface asynchrone reproductible non-abusable : le point d'entrée asynchrone est
`Any.ReproduciblyAsync(Func<Task>)` (nommé TAP, retourne un `Task` que l'on `await`), le synchrone reste
`Any.Reproducibly(Action)`, et deux analyseurs de sévérité *error* ferment ce que les types ne peuvent pas — **JD001**,
une lambda `async` passée à `Any.Reproducibly`, et **JD002**, un `Task` de `Any.ReproduciblyAsync` jeté.

## Justification

* Le défaut est invisible là où ça compte le plus — une compilation qui passe sur un test qui échoue — donc une erreur
  de compilation est la seule contrainte assez forte. Un avertissement, ou de la documentation, laisse le vert vert.
* Le choix de la contrainte suit ce que chaque mécanisme peut porter (le même grain qu'ADR-0035). Le système de types
  *ne peut pas* exprimer « ce `Task` doit être attendu » ni « cette lambda async ne doit pas se lier ici », donc un
  analyseur est l'outil légitime — pas un pis-aller, le seul mécanisme disponible. Là où le langage *peut* porter la
  règle, on le préfère ; ici il ne peut pas.
* Un analyseur de première partie n'est pas exotique pour ce dépôt — il en livre et en teste déjà, avec un contrat de
  chargement Roslyn épinglé au plancher et des règles à suivi de version. Étendre cette discipline à JustDummies
  réutilise un patron éprouvé plutôt que d'en inventer un, et garde les règles JustDummies dans le package JustDummies,
  là où est leur public.
* Les alternatives rejetées échangent chacune le vert-silencieux contre un échec pire ou plus laid : la surcharge
  poison livre un membre déprécié dès le premier jour ; la surcharge bloquante échange un vert-silencieux contre un
  *deadlock* possible. L'analyseur laisse la surface publique propre (deux méthodes honnêtes) et l'échec bruyant (une
  erreur de compilation).
* Séparer `Reproducibly`/`ReproduciblyAsync` par le nom — plutôt que de garder un seul nom surchargé — est ce qui rend
  JD001 et JD002 précis : chaque règle vise une seule méthode, donc aucune ne se déclenche à tort sur l'usage correct
  de l'autre.

## Alternatives considérées

### Garder `Reproducibly(Func<Task>)` surchargé et n'ajouter qu'un analyseur « ne pas jeter »

Envisagé car c'est le plus petit changement, une seule règle. Rejeté car il laisse une méthode qui retourne un `Task`
sans le suffixe `Async` (violation TAP et smell de nommage durable, figé à 1.0), et car la surcharge qui retourne un
`Task` jetable est précisément la forme qu'exploite le piège — le choix de nommage et le choix de sûreté se font mieux
ensemble.

### Surcharge poison — `[Obsolete("Use ReproduciblyAsync", error: true)] Reproducibly(Func<Task>)`

Envisagé car il ferme le piège `async void` purement au niveau langage, sans analyseur. Rejeté car `[Obsolete]`
signifie « déprécié depuis une version antérieure », dont une 1.0 neuve n'a aucune ; il livre un membre à jamais
non-appelable dans la toute première surface publique, ce qui se lit comme une erreur plutôt qu'un design.

### Surcharge asynchrone bloquante — `void Reproducibly(Func<Task>)` qui exécute le corps via `GetAwaiter().GetResult()`

Envisagé car il n'expose aucun `Task` à jeter et ne nécessite aucun analyseur. Rejeté car il impose du
sync-over-async à tout corps de test asynchrone, ce qui peut *deadlocker* sous un `SynchronizationContext` capturé ;
échanger un vert-silencieux contre un gel intermittent n'est pas un progrès pour un outil de test.

### Mettre la règle JustDummies dans `FirstClassErrors.Analyzers`

Envisagé car le projet d'analyseur existe déjà. Rejeté car cet assembly est livré dans le package FirstClassErrors et
porte l'identité FCE : un consommateur de JustDummies seul ne recevrait jamais la règle, et faire transiter une règle
JustDummies par la bibliothèque d'erreurs casse la frontière d'autonomie que garde le test d'architecture.

## Conséquences

### Positives

* Le piège du vert-silencieux devient une erreur de compilation : `Any.Reproducibly(async …)` (JD001) et un
  `Any.ReproduciblyAsync(…)` jeté (JD002) font tous deux échouer la build, avec un message pointant vers la correction.
* Le point d'entrée asynchrone est nommé TAP (`ReproduciblyAsync`), donc il se lit correctement et `CS4014` couvre
  gratuitement le cas `await`-dans-une-méthode-async.
* JustDummies acquiert une histoire d'analyseurs de première partie extensible à de futures règles, dans son propre
  package, sans couplage à FirstClassErrors.

### Négatives

* Le renommage est un changement cassant de la surface publique (pré-version, non livrée) : `Reproducibly(Func<Task>)`
  devient `ReproduciblyAsync`. Acceptable seulement dans la fenêtre pré-1.0, sans coût de migration puisqu'il n'y a
  aucun consommateur.
* Un deuxième projet d'analyseur, une cible d'empaquetage et un schéma d'ID de diagnostic alourdissent le dépôt et la
  build du package JustDummies.

### Risques

* Le contrat de chargement de `JustDummies.Analyzers` doit rester épinglé au plancher Roslyn, comme
  `FirstClassErrors.Analyzers`, sinon l'analyseur échoue silencieusement à se charger (CS8032) sur des SDK plus
  anciens ; atténué en épinglant `Microsoft.CodeAnalysis.CSharp` à `$(RoslynFloorVersion)`.
* JD001/JD002 détectent l'invocation par le nom de métadonnées `JustDummies.Any` et le nom de méthode ; un futur
  renommage de ces membres désactiverait silencieusement les règles, donc leurs noms font désormais partie du contrat
  de diagnostic.

## Actions de suivi

* Aucune requise pour la surface reproductible. Appliquer le même patron d'analyseur de première partie quand une
  future erreur JustDummies n'est exprimable qu'à la compilation.
* La provenance des messages de conflit d'`AnyEnum` / `AnyGuid` (issue #314) est sans rapport et non affectée.

## Références

* ADR-0035 — imposer les conflits Any structurels à la compilation, ceux dépendant de la valeur à l'exécution ; le
  grain « les types là où ils peuvent porter la règle, des vérifications là où ils ne peuvent pas » que suit cette
  décision.
* ADR-0031 — nommer les fabriques d'Any d'après leur type CLR ; précédent pour « rendre la règle in-cassable plutôt
  que seulement vérifiée », et pour la discipline de nommage TAP sur la surface.
* ADR-0042 — sérialiser les tirages sur une source aléatoire ; le correctif de reproductibilité frère (#310 / #311).
* Issue #317 — le piège du vert-silencieux que cet ADR résout.
