# ADR-0037 | Faire varier la dimension d'offset de DateTimeOffset

🌍 🇬🇧 [English](0037-vary-the-datetimeoffset-offset-dimension.md) · 🇫🇷 Français (ce fichier)

**Statut :** Proposé
**Date :** 2026-07-26
**Décideurs :** Reefact

## Contexte

Un `DateTimeOffset` porte deux dimensions : l'instant (son `UtcTicks`) et le décalage (offset) par rapport à UTC. Le décalage est la raison d'être du type face à un simple `DateTime`. `AnyDateTimeOffset` ne fait varier que l'instant et fixe le décalage à `TimeSpan.Zero`, une limitation que ses propres remarks documentent. Le code dont le comportement dépend du décalage — rendu local, arithmétique de décalage, égalité « même instant, décalage différent » — ne peut donc pas obtenir de Dummies un décalage varié mais valide, et le bug latent courant « le code suppose un décalage nul » n'est jamais révélé par une valeur dummy.

`DateTimeOffset` contraint son décalage à un nombre entier de minutes dans ±14:00, et exige que les ticks locaux (`UtcTicks + offset`) restent dans la plage `DateTime` ; aux extrêmes du domaine, tout décalage n'est pas valide pour un instant donné. Dummies construit une valeur de manière constructive pour satisfaire ses contraintes, détecte les contradictions au moment de la déclaration, et ne retente jamais. La comparaison se fait par instant, et `OneOf` renvoie déjà les valeurs fournies telles quelles, décalage compris, car reconstruire à partir du seul instant normaliserait le décalage. L'issue #226 recense un tirage de décalage borné comme un ajout piloté par la demande ; l'issue #297 en assure le suivi.

## Décision

`AnyDateTimeOffset` acquiert une dimension de décalage optionnelle — `WithOffset` épingle un décalage en minutes entières et `WithOffsetBetween` en tire un borné —, tandis que le défaut non contraint reste `TimeSpan.Zero`, et l'instant est resserré à la déclaration de sorte que tout décalage admis produise une valeur valide.

## Justification

Atteindre le décalage fait de `AnyDateTimeOffset` un générateur fidèle à son propre type et révèle la classe de bugs « suppose un décalage UTC » qu'un générateur épinglé à zéro masque. Le garder optionnel — le défaut reste `TimeSpan.Zero` — rend l'ajout non cassant : les tests qui s'appuient aujourd'hui sur un décalage nul, ou qui sérialisent en `+00:00`, continuent de fonctionner.

Resserrer l'instant à la déclaration, plutôt que de caler ou rejeter le décalage à chaque tirage, est ce qui préserve le modèle constructif, en un seul tirage et sans nouvelle tentative : dès que la fenêtre d'instant admet tous les décalages de la plage demandée, le décalage devient un tirage indépendant qui ne peut jamais produire une valeur hors plage. Cela réutilise aussi le resserrement de bornes du moteur d'intervalle, si bien qu'une fenêtre d'instant sans place pour le décalage demandé entre en conflit par anticipation en nommant les deux côtés — exactement comme toute autre contrainte. Offrir un épinglage et un tirage borné reprend l'idiome pin/`Between` déjà présent dans la bibliothèque, et la règle des minutes entières dans ±14:00 reprend celle de `DateTimeOffset`. `OneOf` continue de renvoyer ses valeurs telles quelles car c'est une énumération terminale de valeurs exactes, de sorte que la dimension de décalage ne régit que le tirage construit.

L'arithmétique du décalage, les bornes de resserrement de l'instant et le tirage relèvent de l'implémentation, documentée dans le code `AnyDateTimeOffset` et dans la documentation utilisateur de Dummies — pas ici.

## Alternatives envisagées

### Faire varier le décalage par défaut

Envisagée parce que « n'importe quel `DateTimeOffset` valide » inclut sans doute n'importe quel décalage, faisant de l'épinglage actuel à zéro le choix le moins fidèle. Rejetée parce que c'est un changement de comportement cassant : les tests qui vérifient `Offset == TimeSpan.Zero`, ou qui sérialisent en `+00:00`, casseraient. L'option optionnelle livre la capacité de façon additive ; faire varier par défaut ne pourra être revu que dans une future version majeure.

### Caler ou rejeter le décalage à chaque tirage près des bords

Envisagée parce qu'elle laisse le domaine de l'instant intact. Rejetée parce qu'elle réintroduit soit un échec conditionnel à chaque tirage (contre le modèle sans nouvelle tentative), soit un rétrécissement silencieux du décalage difficile à raisonner. Resserrer l'instant une seule fois, en amont, est plus simple et toujours valide.

### Ne livrer que `WithOffset` (épinglage), sans tirage borné

Envisagée comme surface minimale. Rejetée parce que le cas d'usage moteur — exercer une logique sensible au décalage sur une plage de décalages — est précisément le tirage borné ; un épinglage seul ne le sert pas.

### Laisser le manque

Envisagée parce que la plupart du code traite un `DateTimeOffset` comme un instant. Rejetée parce qu'elle laisse `AnyDateTimeOffset` un générateur infidèle dont le décalage ne varie jamais, et pousse quiconque a besoin d'un décalage varié vers une construction faite à la main qui ignore généralement la graine.

## Conséquences

### Positives

* Le code sensible au décalage devient exerçable, et le bug latent « suppose un décalage UTC » attrapable, avec une valeur qui reste valide par construction.
* L'ajout est non cassant : le défaut non contraint est inchangé.
* Une combinaison instant/décalage impossible est diagnostiquée par anticipation via le moteur existant, en nommant les deux contraintes.

### Négatives

* `AnyDateTimeOffset` porte désormais une seconde dimension et son propre état de décalage propagé à travers chaque transformation.
* La dimension de décalage est spécifique à `DateTimeOffset` — les autres générateurs temporels n'ont pas de décalage — une spécificité délibérée plutôt qu'une surface uniforme.

### Risques

* Un décalage épinglé près du bord du domaine resserre la fenêtre d'instant atteignable ; un utilisateur pourrait lire le conflit *eager* qui en résulte comme fallacieux. Atténuation : le conflit nomme les deux contraintes, et le comportement est documenté.
* `WithOffset` combiné à `OneOf` ne remplace pas le décalage propre d'une valeur `OneOf`. Atténuation : documenté, et cohérent avec la sémantique d'énumération terminale de `OneOf`.

## Actions de suivi

* Documenter `WithOffset`/`WithOffsetBetween` dans le readme de Dummies et la documentation des builders (fait dans la pull request d'implémentation).
* N'envisager un raccourci pour « n'importe quel décalage valide » que si `WithOffsetBetween(-14h, +14h)` s'avère une friction en pratique.
* Ne revisiter la variation du décalage par défaut que dans une future version majeure.

## Références

* Issue [#297](https://github.com/Reefact/first-class-errors/issues/297) — l'issue dédiée à cette fonctionnalité.
* Issue [#226](https://github.com/Reefact/first-class-errors/issues/226) — le backlog Nice-to-Have dont elle a été détachée.
* [ADR-0030](0030-draw-arbitrary-strings-from-an-explicit-terminal-set.md) — la sémantique d'énumération terminale que suit `OneOf`.
* `AnyDateTimeOffset` dans le projet `Dummies` ; le readme NuGet de Dummies.
