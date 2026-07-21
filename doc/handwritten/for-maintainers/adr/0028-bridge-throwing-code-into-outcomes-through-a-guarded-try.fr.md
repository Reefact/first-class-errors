# ADR-0028 | Faire entrer le code levant dans les outcomes via un Try encadré

🌍 🇬🇧 [English](0028-bridge-throwing-code-into-outcomes-through-a-guarded-try.md) · 🇫🇷 Français (ce fichier)

**Statut :** Accepté
**Date :** 2026-07-21
**Décideurs :** Reefact

## Contexte

FirstClassErrors existe pour rendre explicite le chemin d'échec d'une opération :
une erreur est une valeur qu'un appelant retourne et inspecte
(`Outcome`/`Outcome<T>`), pas une exception qui voyage invisiblement. La
bibliothèque fournit déjà les sorties du flux Outcome vers les exceptions
(`GetResultOrThrow()`, `ThrowIfFailure()`, `Error.ToException()`) ; elle n'avait
aucune entrée sanctionnée dans l'autre sens.

Or le code réel doit bien entrer dans le flux Outcome depuis des sources
levantes. Beaucoup de primitives ne signalent l'échec qu'en levant et n'exposent
aucune contrepartie non-levante sur les frameworks supportés : la bibliothèque a
pour plancher .NET Standard 2.0 / .NET Framework 4.7.2 (ADR-0022), où des formes
comme `MailAddress.TryCreate` (.NET 5+) ou `Convert.TryFromBase64String`
(.NET Core 2.1+) n'existent pas, et les bibliothèques tierces livrent
fréquemment un `Parse`/`Decode` levant sans aucun `TryXxx`. Atteindre le flux
Outcome depuis un tel appel imposait un `try`/`catch` écrit à la main qui
construit un `Outcome` — répété à chaque site d'appel.

Ce pont écrit à la main a des modes de défaillance récurrents et silencieux :

* Attraper `System.Exception` requalifie des bugs inattendus (un déréférencement
  null, un état invalide) en erreurs *anticipées* — précisément ce qu'un
  `Outcome` est documenté pour **ne pas** représenter.
* Attraper une exception de protocole riche (HTTP, socket, base de données)
  écrase plusieurs échecs distincts — chacun porteur d'une donnée de statut que
  l'appelant détient déjà — en un seul « ça a levé ».
* Envelopper un appel qui, lui, *possède* une contrepartie non-levante sur le
  framework cible fait payer un `try`/`catch` là où une vérification de résultat
  suffirait.
* Lier le type attrapé à un type d'annulation produit un catch qui ne peut
  jamais s'exécuter, car l'annulation est du contrôle de flux coopératif, pas un
  résultat d'échec ; le compilateur ne le signale pas.

La bibliothèque traite par ailleurs une erreur sans code stable comme une odeur
(ses analyzers de codes d'erreur), et un message d'exception brut comme une
donnée à curer avant qu'elle n'entre dans une erreur. Une conversion
automatique exception-vers-erreur violerait les deux.

Enfin, l'ADR-0005 a réservé le nom de fabrique *simple* à la variante retournant
un Outcome et retiré le préfixe `Try` des fabriques (`TryXxx` → `Xxx`), car un
nom `TryXxx` emprunte la forme BCL `bool TryXxx(..., out T)` sans l'honorer.

## Décision

FirstClassErrors fournit `Outcome.Try` comme le seul pont sanctionné d'une
opération levante vers un `Outcome` — attrapant un unique type d'exception nommé
par l'appelant, exigeant un mapper d'erreur explicite, et laissant toujours
l'annulation se propager — et maintient ce pont délibérément étroit en signalant
son mésusage par des analyzers d'usage plutôt qu'en élargissant l'API.

## Justification

* **Une primitive remplace un motif répété et fragile.** Le pont
  `try`/`catch`-vers-`Outcome` écrit à la main se répète à chaque frontière et y
  reproduit les mêmes fautes. Le replier dans un seul appel fait de la forme
  correcte la forme par défaut et efface le boilerplate.
* **Le mapper est obligatoire parce que les erreurs doivent rester
  first-class.** Une conversion automatique exception-vers-erreur produirait des
  erreurs sans code stable et laisserait fuiter des messages d'exception bruts —
  exactement ce que les conventions de code et de contexte d'erreur de la
  bibliothèque découragent. Forcer l'appelant à mapper est ce qui garde l'erreur
  produite curée et diagnosticable.
* **Un seul type attrapé garde l'Outcome honnête.** Un `Outcome` modélise un
  échec anticipé ; attraper largement transformerait des bugs en erreurs
  anticipées. Nommer un type d'exception est ce qui sépare l'échec que
  l'opération est censée produire du crash qu'elle n'est pas censée produire.
* **L'annulation doit se propager.** L'annulation coopérative est une demande
  d'arrêt, pas un échec à capturer ; la laisser passer est ce qui empêche `Try`
  d'avaler l'annulation d'un appelant.
* **Ce sont les analyzers, pas une surface de type plus étroite, qui tracent la
  frontière.** Les usages légitimes (primitives levantes-seulement sans
  contrepartie sur le framework cible, API tierces levantes) et les mésusages
  partagent la *même* signature et ne diffèrent que par un contexte que le
  compilateur ne voit pas. Retirer de la capacité bloquerait les cas valides
  pour lesquels la primitive existe ; un diagnostic de compilation, réglable,
  marque le mésusage tout en laissant la capacité intacte. Chaque garde nomme un
  danger précis issu du Contexte — catch trop large, résultat de protocole
  jeté, alternative non-levante disponible, catch d'annulation mort — et le
  consommateur peut le monter ou le supprimer. L'ADR-0005 notait qu'une
  convention laissée à la seule revue finit par se réintroduire ; ici la
  frontière est outillée dès le départ.
* **`Try` est un nom cohérent ici, et ne rouvre pas l'ADR-0005.** L'ADR-0005
  gouverne les noms de *fabrique*, où un préfixe `TryXxx` promettait faussement
  la forme BCL `bool`+`out`. `Outcome.Try` n'est pas une fabrique ni un préfixe
  `TryXxx` : c'est une opération d'ordre supérieur qui prend le travail comme un
  délégué et retourne un `Outcome<T>`. L'argument délégué rend sa forme
  non-équivoque, donc elle n'emprunte aucune fausse promesse ; les deux
  décisions sont orthogonales.

## Alternatives envisagées

### Aucune primitive — laisser les appelants écrire leur propre try/catch

Envisagée parce qu'elle n'ajoute aucune surface publique et n'impose aucune
opinion.

Rejetée parce qu'elle reproduit chaque mode de défaillance ci-dessus à chaque
site d'appel sans rien pour les attraper, et enterre le chemin d'échec dans une
plomberie impérative que la bibliothèque existe pour faire ressortir. L'absence
de pont sanctionné est précisément ce qui a laissé les fautes se répéter.

### Un builder fluent multi-catch (`Try(...).Catch<A>(...).Catch<B>(...)`)

Envisagé parce que le chaînage se lit bien quand plusieurs types d'exception se
mappent vers des erreurs différentes.

Rejeté parce qu'il invite activement à attraper de nombreux types —
réintroduisant le danger du catch trop large sous forme d'affordance d'API —
complique l'endroit où l'annulation est gérée, et sert un cas que la plupart des
ponts n'ont pas : la majorité honnête anticipe exactement un type d'exception.
La primitive étroite mono-type est l'outil des 90 % ; les frontières réellement
multi-exceptions sont mieux servies par un adaptateur explicite.

### Conversion automatique exception-vers-erreur (sans mapper obligatoire)

Envisagée pour l'ergonomie — l'appelant n'aurait pas à écrire de mapper.

Rejetée parce que l'erreur produite n'aurait aucun code stable et porterait un
message d'exception brut, les deux choses que les conventions de code et de
contexte d'erreur existent pour empêcher. La petite cérémonie d'un mapper
obligatoire est le prix d'une erreur first-class.

### Encadrer la frontière en réduisant la surface de type plutôt que par des analyzers

Envisagée parce que rendre le mésusage impossible est plus fort que le signaler.

Rejetée parce que les appels légitimes et illégitimes partagent une seule
signature et ne diffèrent que par un contexte invisible au système de types ;
tout rétrécissement qui bloquerait le mésusage bloquerait aussi les cas anciens
frameworks et tiers pour lesquels la primitive existe. Le jugement appartient au
site d'usage, et c'est là qu'un analyzer parle.

## Conséquences

### Positives

* Il existe une façon unique, évidente et sûre d'entrer dans le flux Outcome
  depuis du code levant, et sa forme par défaut est la bonne ; le boilerplate
  récurrent se réduit à un seul appel.
* Les erreurs produites par `Try` gardent un code mappé et stable et un message
  curé, donc restent first-class.
* Les quatre modes de mésusage sont attrapés à la compilation et réglables par
  consommateur, donc la garde informe sans bloquer l'usage légitime.
* La sémantique d'annulation est préservée sans que l'appelant ait à y penser.

### Négatives

* Nouvelle surface publique — formes à valeur et à effet de bord, chacune
  synchrone et asynchrone — à maintenir et documenter sur le plancher
  netstandard2.0 / net472.
* Quatre analyzers d'usage (FCE019–FCE022) avec leurs pages de règle bilingues
  et leurs tests, à garder cohérents avec le comportement de la primitive.
* Le mapper obligatoire ajoute une petite cérémonie inévitable à chaque appel.

### Risques

* Les analyzers sont indicatifs et supprimables, donc un mésusage déterminé peut
  quand même être livré : la garde réduit l'erreur, elle ne l'élimine pas.
  Mitigation — les deux gardes à défaut prouvé (catch trop large, catch
  d'annulation mort) sont activées par défaut en warning.
* Si le type attrapé ou le comportement d'annulation de `Try` change un jour, les
  quatre analyzers et leur documentation doivent bouger en même temps ou ils
  induiront en erreur. Mitigation — le comportement est verrouillé par des tests.
* Un lecteur peut percevoir le nom `Try` comme en conflit avec l'ADR-0005.
  Mitigation — cet ADR consigne pourquoi les deux sont orthogonaux.

## Actions de suivi

* Garder le README/guide EN et la traduction française synchronisés pour la
  guidance `Try` et les pages de règle FCE019–FCE022.
* Garder les analyzers FCE019–FCE022 et leurs tests alignés sur le comportement
  documenté de la primitive à chaque fois que l'un ou l'autre change.

## Références

* ADR-0005 — réserver le nom de fabrique simple à la variante retournant un
  Outcome (nommage ; orthogonal — explique pourquoi `Try` ici ne soulève aucun
  conflit).
* ADR-0022 — plancher de la bibliothèque à .NET Framework 4.7.2 (pourquoi les
  primitives levantes-seulement sans `TryXxx` sont un cas réel et supporté).
* ADR-0003 — unifier le mapping de valeur d'Outcome sous Then (contexte de l'API
  Outcome).
* [Guide Outcome](../../for-users/OutcomeGuide.fr.md) — explication, côté
  utilisateur, de l'entrée et de la sortie du flux Outcome.
* Pages de règle des analyzers [FCE019](../../for-users/analyzers/FCE019.fr.md),
  [FCE020](../../for-users/analyzers/FCE020.fr.md),
  [FCE021](../../for-users/analyzers/FCE021.fr.md),
  [FCE022](../../for-users/analyzers/FCE022.fr.md).
