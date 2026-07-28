# Workflow `justdummies-mutation`

🌍 🇬🇧 [English](justdummies-mutation.en.md) · 🇫🇷 Français (ce fichier)

> Documentation mainteneur — fait partie de la [référence des workflows](README.fr.md).
> Ne fait pas partie de la documentation utilisateur sous `doc/`.

**Fichier du workflow :** [`.github/workflows/justdummies-mutation.yml`](../../../../.github/workflows/justdummies-mutation.yml)

## À quoi il sert

Les tests de mutation des **trois composants JustDummies** : `JustDummies`, son
adaptateur xUnit v3 `JustDummies.Xunit`
([ADR-0039](../adr/0039-adapt-dummies-to-xunit-v3-through-a-companion-package.fr.md)),
et les analyseurs livrés dans le package
([ADR-0044](../adr/0044-ship-justdummies-analyzers.fr.md)).
Sur une pull request, il ne mute que les fichiers modifiés par celle-ci, pour
l'adaptateur et les analyseurs ; le générateur est mesuré par le seul balayage
hebdomadaire
([ADR-0049](../adr/0049-drop-the-justdummies-generator-from-the-per-pull-request-mutation-matrix.fr.md)).
Ce que *sont* les tests de mutation, et pourquoi ce dépôt
en fait un barrage, est expliqué une seule fois sur la page
[`mutation`](mutation.fr.md) — ce workflow est la même machine avec une matrice
différente.

## Pourquoi un workflow séparé

`JustDummies` est un package autonome et agnostique des erreurs, qui ne référence
volontairement pas `FirstClassErrors`
([ADR-0011](../adr/0011-host-dummies-as-a-standalone-package.fr.md)), et il est
destiné à un dépôt à lui. Découper le barrage de mutation le long de cette
frontière future dès maintenant fait de la migration **un déplacement de fichier
plutôt qu'une réécriture** : rien dans ce workflow ne nomme un projet
FirstClassErrors, et rien dans [`mutation`](mutation.fr.md) ne nomme un projet
JustDummies.

Cela donne aussi à JustDummies **son propre check**,
**`JustDummies mutation gate`**, indépendant de celui de FirstClassErrors. Deux
checks, deux barres qui évoluent séparément — ce dont deux bibliothèques de
maturité de test différente ont de toute façon besoin. Sur les pull requests, les
deux sont **consultatifs**
([ADR-0046](../adr/0046-make-the-per-pull-request-mutation-gate-advisory.md)) ; le
niveau imposé de chacun est son balayage complet hebdomadaire.

## Quand il s'exécute

- Sur chaque **pull request ciblant `main`** — cantonné au diff et **consultatif** :
  il rapporte le score du diff mais ne bloque jamais le merge
  ([ADR-0046](../adr/0046-make-the-per-pull-request-mutation-gate-advisory.md)).
- **Chaque semaine** sur planification (lundi, 03h47 UTC) — le balayage complet, le
  **niveau imposé**. Le créneau est décalé de celui de `mutation` pour que les deux
  balayages ne se disputent pas les runners.
- À la demande via **`workflow_dispatch`** — le balayage complet.

## Comment il s'exécute

Presque à l'identique de [`mutation`](mutation.fr.md), dont la page documente le
mécanisme en entier : `changed` mute le diff depuis le point de fourche, `gate`
regroupe la matrice sous un nom de check stable, `full` balaie tout avec le seuil
désactivé. Les configurations Stryker sont
[`build/stryker/justdummies.json`](../../../../build/stryker/justdummies.json),
[`build/stryker/justdummies-xunit.json`](../../../../build/stryker/justdummies-xunit.json)
et [`build/stryker/justdummies-analyzers.json`](../../../../build/stryker/justdummies-analyzers.json).

**Le seul point où les deux workflows diffèrent : la matrice par PR compte deux
pattes, pas trois.** Le générateur est balayé chaque semaine mais **n'est pas**
muté par pull request
([ADR-0049](../adr/0049-drop-the-justdummies-generator-from-the-per-pull-request-mutation-matrix.fr.md)).
Comme `--since` sélectionne par **fichier** changé et non par ligne changée, un
diff d'une centaine de lignes touchant l'une des grosses sources du générateur
entraîne ce fichier entier : mesuré à 844 mutants, encore en cours après une
heure, sans produire aucun score. Ce n'est pas un défaut de réglage — chaque
levier exposé par Stryker plafonne vers −36 % là où une telle patte aurait besoin
de −95 %, le sharding ne peut pas descendre sous un fichier, et les motifs
`mutate` limités aux lignes ne sélectionnent rien. L'adaptateur et les analyseurs
sont petits, terminent en quatre-vingt-dix secondes environ, et gardent leur
patte.

Deux points de cette page comptent ici plus qu'ailleurs :

- **`JustDummies` est la plus grosse bibliothèque du dépôt** — quelques milliers
  de mutants — et son balayage complet est donc le job le plus long que le dépôt
  exécute. C'est toute la raison pour laquelle le barrage est cantonné au diff
  plutôt qu'un balayage complet par pull request.
- **`"test-runner": "mtp"` et `"coverage-analysis": "off"` ne sont pas des
  réglages de confort.** Avec le runner VSTest par défaut de Stryker, ces suites
  scorent 0 % — tous les mutants rapportés survivants, parce que le runner ne sait
  pas activer un mutant dans un projet de tests xUnit v3. Lisez
  [cette section](mutation.fr.md#deux-réglages-qui-nen-sont-pas) avant de toucher
  à l'un ou à l'autre.

## `JustDummies` n'a pas encore de seuil de score

La barre de chaque autre bibliothèque a été fixée à partir d'un balayage complet
mesuré sur cette bibliothèque
([comment et pourquoi](mutation.fr.md#doù-viennent-les-seuils)). Pas celle de
`JustDummies` : elle porte quelques milliers de mutants sur une suite lourde, son
balayage complet dépasse largement l'heure, et **aucun score n'a été mesuré pour
elle**. Plutôt que d'inventer un chiffre,
[`justdummies.json`](../../../../build/stryker/justdummies.json) met `break` à
**0** — le barrage sur le score est coupé pour cette seule bibliothèque.

C'est délibéré et c'est temporaire. La branche s'exécute toujours, échoue toujours
sur un build cassé ou une suite en échec, et liste toujours ses mutants
survivants dans le résumé du run ; ce qu'elle ne fait pas encore, c'est refuser
une pull request sur un score. **Le premier balayage hebdomadaire publie le
chiffre sur toute la bibliothèque** — c'est ce run que ce seuil attend.
Lisez-le, et fixez `break` à partir de là, exactement comme les barres des autres
bibliothèques l'ont été.

`JustDummies.Xunit` n'appelle pas cette réserve : elle est assez petite pour que
sa barre vienne d'un balayage complet comme les autres, et elle barre normalement.

La branche des analyseurs part elle aussi avec `break` à **0**, pour une autre
raison : ses survivants résiduels sont les mutants d'infrastructure d'analyseur et
de chaînes de descripteurs que portent aussi les analyseurs FirstClassErrors —
elle rapporte donc au lieu de bloquer
([ADR-0044](../adr/0044-ship-justdummies-analyzers.fr.md)).

## Permissions & sécurité

`contents: read` seulement. Le workflow fait un checkout, un build et lance des
tests ; il ne stocke aucun secret et n'a besoin d'aucun périmètre en écriture.

## Quand JustDummies partira dans son propre dépôt

À emporter tel quel :

- ce fichier de workflow, renommé `mutation.yml` là-bas (et son `name:` avec) ;
- [`build/stryker/justdummies.json`](../../../../build/stryker/justdummies.json),
  [`build/stryker/justdummies-xunit.json`](../../../../build/stryker/justdummies-xunit.json)
  et [`build/stryker/justdummies-analyzers.json`](../../../../build/stryker/justdummies-analyzers.json) ;
- [`.config/dotnet-tools.json`](../../../../.config/dotnet-tools.json) —
  l'épinglage de Stryker ;
- cette page, augmentée des sections partagées de [`mutation`](mutation.fr.md)
  repliées dedans, puisque la page à laquelle elle renvoie n'existera pas là-bas.

Puis changer exactement une chose : le champ **`solution`** des trois
configurations, qui nomme encore `FirstClassErrors.sln`. Les chemins `project` et
`test-projects` sont déjà relatifs au dépôt et inchangés par la migration.

De ce côté-ci, supprimer ce workflow, ses configurations et cette page, et
retirer l'entrée `JustDummies mutation gate` de la protection de branche.

## À manipuler avec précaution

- **Gardez ce workflow et [`mutation`](mutation.fr.md) synchronisés.** Ils sont
  dupliqués à dessein — c'est ce qui fait de la migration un déplacement de
  fichier —, donc un correctif sur l'un est un correctif sur l'autre tant que la
  séparation n'a pas eu lieu.
- Tout ce qui figure sous
  [*À manipuler avec précaution* sur la page `mutation`](mutation.fr.md#à-manipuler-avec-précaution)
  vaut ici mot pour mot : `fetch-depth: 0`, `--since` qui refuse `HEAD`,
  `if: always()` sur `gate`, le moteur épinglé, l'endroit où vivent les seuils.

## L'exécuter en local

```bash
dotnet tool restore
dotnet stryker --config-file build/stryker/justdummies.json
```

C'est le balayage complet de la plus grosse bibliothèque, et cela prend un
moment. Pour reproduire ce que fait le barrage sur une branche :

```bash
dotnet stryker --config-file build/stryker/justdummies.json --since:$(git merge-base origin/main HEAD)
```

Les rapports atterrissent dans `StrykerOutput/` (ignoré par git) ; ouvrez
`reports/mutation-report.html`.

## Voir aussi

- [`mutation`](mutation.fr.md) — la même machine pour les bibliothèques
  FirstClassErrors, et l'endroit où le mécanisme est documenté en entier.
- [`justdummies`](../../../../.github/workflows/justdummies.yml) *(pas encore de
  page de référence)* — l'autre workflow cantonné à JustDummies : il prouve que
  les assets `netstandard2.0` et `net8.0` publiés se comportent bien sur leurs
  runtimes respectifs.
- [ADR 0043 — Gate pull requests on the mutation score of what they
  changed](../adr/0043-gate-pull-requests-on-the-mutation-score-of-the-diff.fr.md)
  — la décision que les deux workflows mettent en œuvre.
