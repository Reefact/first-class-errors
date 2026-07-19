# ADR-0023 | Conserver les sélecteurs à arbres d'expression pour l'API v1 du binder

🌍 🇬🇧 [English](0023-keep-expression-tree-selectors-for-the-v1-binder-api.md) · 🇫🇷 Français (ce fichier)

**Statut :** Accepté
**Date :** 2026-07-19
**Décideurs :** Reefact

## Contexte

Le binder de requêtes sélectionne les propriétés des DTO au travers de
sélecteurs à arbres d'expression (`r => r.GuestEmail`), dont il dérive à la
fois la valeur et le nom d'argument derrière chaque chemin d'erreur.
L'issue #151 (constat de revue 15/19) mesurait un coût du chemin nominal
d'environ 2,2–2,4 µs et ~700 o alloués par propriété liée, et conditionnait
le remède à une décision à prendre **avant le gel de l'API v1**.

La re-mesure sur le code actuel (harnais BenchmarkDotNet dans
`FirstClassErrors.RequestBinder.Benchmarks`, dont le README porte les tables
complètes) a décomposé ce coût :

* **~70–75 % du coût est l'arbre d'expression lui-même**, alloué par le
  compilateur C# *au site d'appel de l'appelant* à chaque exécution —
  ~488 o et ~416 ns par propriété. Roslyn met en cache les lambdas
  déléguées mais jamais les lambdas d'arbres d'expression, et aucun
  changement à l'intérieur de la librairie ne peut supprimer une allocation
  qui se produit avant l'entrée dans la librairie.
* Le reste était interne à la librairie — lectures par réflexion non mises
  en cache, boxing des types valeur nullables, chaînes de chemin
  construites d'avance pour les éléments de liste et les préfixes
  imbriqués — et a été éliminé par la mise en cache de getters compilés et
  le report de la construction des chemins au chemin d'échec
  (l'implémentation de l'issue #151). Ce qui reste à l'intérieur de la
  librairie est la forme objet-par-propriété de l'API fluent elle-même.
* Le même benchmark montre que le coût du sélecteur est déjà évitable
  **avec l'API actuelle** : des sélecteurs hissés dans des champs statiques
  lient 5 propriétés en ~342 ns / 880 o au lieu de ~2 805 ns / 3 600 o, et
  un sélecteur délégué mis en cache par le compilateur coûterait
  ~1 ns / 0 o.
* Un site d'appel du binder lie un DTO une fois par requête à la frontière
  de l'adaptateur primaire ; le travail environnant (désérialisation, E/S,
  l'appel du domaine) se mesure en dizaines à centaines de µs.

Deux remèdes au niveau de l'API existent : des surcharges prenant un nom et
un délégué simple (`SimpleProperty("GuestEmail", d => d.GuestEmail)`),
additives et non cassantes, ou le remplacement pur et simple de l'API à
expressions, qui est cassant. Chaque surcharge de sélecteur existe six fois
(scalaire et liste, référence et type valeur, complexe), si bien qu'une
famille déléguée parallèle double à peu près la surface des sélecteurs et
crée deux façons idiomatiques d'écrire chaque liaison.

## Décision

Le binder v1 sélectionne les propriétés des DTO exclusivement au travers de
sélecteurs à arbres d'expression ; le coût d'expression par appel est
accepté pour la v1, et tout chemin rapide à base de délégués est reporté à
une décision post-v1, additive.

## Justification

Le coût absolu mesuré — quelques µs par requête — est négligeable face aux
dizaines à centaines de µs qu'une requête passe à la frontière qu'elle lie ;
le coût par propriété ne compte que dans des formes atypiques (DTO très
larges sur des endpoints très chauds), et ces appelants disposent déjà d'une
échappatoire sans rupture : hisser les sélecteurs dans des champs statiques,
ce qui supprime ~85–90 % du temps et ~75 % de l'allocation avec l'API
exactement telle qu'elle est.

Un idiome de sélection unique vaut davantage pour la v1 que les
nanosecondes résiduelles : une famille déléguée+nom dupliquée doublerait une
surface de six surcharges, fragmenterait le style des sites d'appel entre
bases de code, et réintroduirait le nommage de propriétés par chaînes que
l'API à expressions existe précisément pour empêcher — son point d'entrée
unique est ce qui permet au binder de dériver la valeur, le nom et la garde
de mauvaise déclaration d'une seule construction. Une famille déléguée étant
purement additive, la reporter ne coûte rien : elle peut être livrée dans
n'importe quelle mineure post-v1 si le profilage de consommateurs réels
montre un jour que l'arbre au site d'appel compte, alors que la livrer
maintenant est un engagement de surface que la v1 porterait pour toujours.

## Alternatives considérées

### Ajouter dès maintenant des surcharges délégué+nom

Considérée parce que c'est la seule façon d'atteindre le plancher de ~0 o
par sélecteur (délégués mis en cache par le compilateur) sans rien casser.
Rejetée pour la v1 : elle double la surface des sélecteurs,
institutionnalise deux façons de lier avant la première version stable, et
optimise un coût que le hissage permet déjà aux appelants chauds d'éliminer
aujourd'hui.

### Remplacer l'API à expressions par délégué+nom

Considérée pour le même plancher avec un idiome unique. Rejetée : c'est une
refonte cassante de la construction centrale du binder, elle ramène le
nommage des arguments à des chaînes, et contredit le contrat de sélecteur
vérifié à la compilation sur lequel s'appuient les analyseurs et les gardes.

### Générer les sélecteurs à la compilation (générateur de source)

Considérée comme la voie de long terme vers l'ergonomie des expressions au
coût des délégués. Rejetée pour la v1 comme périmètre, non sur le fond :
c'est un nouveau composant avec sa propre surface de compatibilité ; la
base d'ADR l'enregistre comme le successeur naturel si le coût résiduel
venait à compter.

## Conséquences

### Positives

* Un seul idiome de sélection en v1 ; la surface de six surcharges reste en
  l'état.
* Les optimisations internes de l'issue #151 tiennent par elles-mêmes : les
  chemins et la réflexion ne coûtent plus rien sur une liaison entièrement
  valide, quelle que soit l'évolution ultérieure de la décision sur les
  sélecteurs.
* Les appelants chauds disposent d'une atténuation documentée et mesurée
  (sélecteurs hissés) qui n'exige aucun changement de la librairie.

### Négatives

* Le style par défaut au site d'appel continue de payer ~488 o / ~416 ns
  par propriété liée pour l'arbre d'expression.

### Risques

* Si l'adoption de la v1 révèle des charges où le coût du sélecteur domine
  en pratique, le remède est additif (surcharges déléguées ou générateur de
  source) — le risque se borne à porter le successeur de cet ADR, jamais à
  un changement cassant.

## Actions de suivi

* Documenter l'atténuation par sélecteurs hissés dans le guide utilisateur
  du binder dès que le sujet se présentera chez les consommateurs.
* Relancer `FirstClassErrors.RequestBinder.Benchmarks` au prochain
  changement de la surface des sélecteurs du binder, et revisiter cet ADR
  chiffres en main.

## Références

* Issue #151 — Binder : la liaison par propriété utilise une réflexion non
  mise en cache et alloue une chaîne de chemin sur le chemin nominal
  (constat de revue 15/19).
* `FirstClassErrors.RequestBinder.Benchmarks/README.md` — harnais de
  mesure, tables avant/après complètes et décomposition du coût.
* [ADR-0008](0008-bind-nullable-value-type-properties-through-a-struct-constrained-overload.fr.md) —
  la famille de surcharges contrainte `struct` sur laquelle la surface des
  sélecteurs est bâtie.
* [ADR-0021](0021-bind-out-of-dto-arguments-as-peers-through-a-source-agnostic-entry.fr.md) —
  l'entrée hors-DTO, dont le chemin par nom est la forme non-expression
  existante du binder.
* [ADR-0022](0022-floor-the-library-on-net-framework-4-7-2.fr.md) — le
  floor .NET Framework 4.7.2 contre lequel le cache de getters compilés a
  été vérifié.
