# ADR-0048 | Garantir qu'une valeur regex générée matche son pattern, par redraw borné

🌍 🇫🇷 Français (ce fichier) · 🇬🇧 [English](0048-guarantee-a-generated-regex-value-matches-by-bounded-redraw.md)

**Statut :** Proposé
**Date :** 2026-07-27
**Décideurs :** Reefact

## Contexte

`Any.StringMatching(...)` parse un pattern en arbre une fois et, à chaque tirage, le parcourt pour
**construire** une valeur directement — jamais générer-puis-filtrer. La construction reflète le
sous-ensemble régulier de la sémantique du moteur .NET, de sorte qu'une valeur générée est un membre
authentique du pattern.

Quelques coins de la gestion du **match à vide** du moteur ne peuvent pas être reflétés
structurellement, parce que la réponse de .NET à « la chaîne vide matche-t-elle ? » pour une
**alternative nullable sous un quantificateur** est implémentation-définie et dépend de détails qu'une
construction structurelle ne porte pas : l'**ordre** des alternatives, et la **forme** de la branche vide
(un `|` nu contre un atome quantifié à zéro comme `\S{0}`). Mesuré (issue #335) :

| pattern (ancré, `IgnoreCase`)         | le moteur matche `""` |
| ------------------------------------- | --------------------- |
| `(?:\S{0}b{0}){1,2}`                   | oui                   |
| `(?:r{1,2}\|\S{0}){1,2}`               | **non**               |
| `(?:\S{0}\|r{1,2}){1,2}` (ordre inversé) | oui                 |
| `(?:r\|){1,2}` (branche vide nue)     | oui                   |

La construction structurelle a choisi la branche `\S{0}b{0}` et émis `""`, que le moteur refuse ensuite
pour cette forme — donc `Any.StringMatching` a retourné une valeur que le pattern même dont elle est
issue ne matche pas. Les patterns qui déclenchent ça sont dégénérés : FsCheck *génère* `\S{0}` (matcher
`\S` zéro fois) ; un humain écrit `\S*`. Mais le contrat « une valeur générée matche son pattern » était
violé.

## Décision

Après la construction structurelle, la valeur est **vérifiée contre le vrai moteur .NET** (un match
complet ancré sous la seule option honorée par le générateur — `IgnoreCase`) et **redessinée en cas
d'échec**, borné. La vérification a le dernier mot : une valeur que le moteur refuserait n'est jamais
retournée. Épuiser le plafond lève une `AnyGenerationException`.

## Justification

* **Tenir l'invariant par construction, pas par modélisation.** Les coins à-vide du moteur sont
  ordre-dépendants, forme-dépendants, et spécifiques à l'implémentation et à la version — un jeu perdu à
  poursuivre dans un modèle écrit à la main. Vérifier la sortie contre le moteur fait tenir « une valeur
  générée matche son pattern » pour ce défaut **et toute divergence future** entre la construction
  structurelle et le moteur, sans règle arcanique à maintenir.
* **Le redraw borné est l'idiome maison.** L'ADR-0033 satisfait déjà les exclusions de chaînes par un
  redraw borné : un chemin structurel rapide plus un filet borné. C'est la même forme pour la même
  raison.
* **Le coût est négligeable.** Un pattern supporté matche à la première construction ; seuls ces coins
  rares redessinent, et une valeur valide apparaît en une poignée de tirages. La génération n'est pas une
  boucle chaude, et `Any.StringMatching(Regex)` détient déjà un `Regex` compilé. Le plafond transforme un
  pattern que la construction ne peut jamais satisfaire en une erreur claire au lieu d'une boucle
  illimitée.
* **La reproductibilité est préservée.** Le redraw consomme d'autres tirages de la même source seedée,
  donc une graine rejoue le run exactement.

## Alternatives considérées

### Modéliser la sémantique de match à vide du moteur

Rejeté. Le comportement ci-dessus est ordre-dépendant, forme-dépendant, et n'est pas documenté par le
moteur comme un contrat stable ; un modèle serait fragile et devrait être revu à chaque évolution du
moteur — sans jamais être prouvé complet.

### Refuser les patterns dégénérés comme non supportés

Considéré : refuser un terme quantifié à zéro (`X{0}`) et/ou une alternative nullable sous quantificateur
par une `UnsupportedRegexException`, en gardant la génération purement structurelle. Rejeté parce que
détecter **chaque** divergence en amont est presque aussi dur que la modéliser — le risque étant de
refuser des patterns valides tout en en ratant d'autres — et cela rétrécit une capacité documentée pour
des patterns simplement inhabituels, pas hors du sous-ensemble supporté. Le redraw borné couvre toute la
classe sans détecteur fragile.

## Conséquences

### Positives

* « Une valeur générée matche son pattern » est incassable — pour ce bug et pour toute divergence future
  modèle/moteur. La property de round-trip `IgnoreCase` (#335) tient par construction, pas par chance de
  la graine.

### Négatives

* Une valeur est construite puis vérifiée, plutôt que construite et retournée inconditionnellement — un
  petit écart au « jamais généré puis filtré ». Le mécanisme principal reste structurel ; la vérification
  est un filet pour l'échec rare, et la documentation du générateur le dit.
* Un pattern véritablement insatisfiable lève désormais une `AnyGenerationException` après le plafond au
  lieu de retourner une valeur fausse — un échec plus clair, mais un échec là où l'ancien code retournait
  silencieusement du n'importe quoi.

## Références

* ADR-0033 — Satisfaire les exclusions de chaînes par un redraw borné : l'idiome que ceci réutilise.
* ADR-0030 — Tirer des chaînes arbitraires depuis un ensemble terminal explicite : la philosophie
  structurelle « construire, pas filtrer » que ceci complète plutôt que remplace.
* Issue #335 — le flake de round-trip `IgnoreCase` qui a rendu la divergence de match à vide concrète.
