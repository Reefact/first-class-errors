# ADR-0018 | Regrouper le code et les messages d’une erreur structurelle du binder dans une seule définition

🌍 🇬🇧 [English](0018-bundle-the-binders-structural-error-code-and-messages.md) · 🇫🇷 Français (ce fichier)

**Statut :** Accepté
**Date :** 2026-07-18
**Décideurs :** Reefact

## Contexte

* Le binder fabrique exactement deux erreurs structurelles qui lui sont propres : un
  argument requis absent (`REQUEST_ARGUMENT_REQUIRED`) et un argument présent mais invalide
  (`REQUEST_ARGUMENT_INVALID`). L’ADR-0016 a rendu leurs **codes** configurables sur
  `RequestBinderOptions`, résolvant l’issue #147.
* Chacune de ces deux erreurs porte aussi des **messages publics** — un résumé court et un
  détail optionnel — exposés à un utilisateur final ou à un client d’API. Avant cette
  décision, ces messages étaient des littéraux anglais codés en dur, sans point de
  surcharge ni de localisation (issue #149).
* Le reste de la bibliothèque localise les messages publics selon le patron
  d’internationalisation : la fabrique d’une erreur résout des ressources propres à la
  culture **au moment où l’erreur est construite**, si bien que le message suit la culture
  ambiante de la requête ; le message de diagnostic reste dans une seule langue interne par
  convention.
* Le binder fabrique lui-même ces deux erreurs — leurs fabriques sont internes — de sorte
  qu’un consommateur ne peut pas injecter de chaînes localisées comme il le fait lorsqu’il
  écrit les fabriques de ses propres erreurs. C’est la faille qui brise le récit i18n de la
  bibliothèque à la frontière de l’adaptateur primaire.
* `RequestBinderOptions` est la configuration unique, immuable, du point d’entrée du binder,
  fixée une fois avant que la liaison ne commence (ADR-0012) et héritée par les binders
  imbriqués ; son défaut applicatif est gelé à la première utilisation.
* Un code et son message sont un seul concept dans le modèle d’erreurs codées de la
  bibliothèque. Les exposer comme deux réglages indépendants permet à un consommateur de
  surcharger l’un et d’oublier l’autre, produisant un code séparé d’un message qui ne lui
  correspond plus.
* La bibliothèque est en pré-version (`v0.1.0-preview.1`), non publiée, sans consommateur
  externe : remodeler la surface des options n’a aucun coût de migration — et cette forme
  doit être arrêtée avant le gel de l’API v1.

## Décision

Chacune des deux erreurs structurelles du binder est configurée sur `RequestBinderOptions`
comme une définition unique regroupant son code d’erreur et un constructeur de message
évalué au moment où l’erreur est levée — remplaçant la surcharge du code seul de l’ADR-0016
et rendant les messages publics localisables par requête.

## Justification

* Regrouper le code avec son message garde le modèle d’erreurs codées cohérent : les deux
  facettes qui, ensemble, présentent un échec structurel à un consommateur sont configurées
  comme une unité, si bien qu’une surcharge ne peut jamais séparer un code de son message —
  le mode de défaillance qu’autorisaient les deux réglages indépendants.
* Réutiliser `RequestBinderOptions` (fixée une fois, héritée par les binders imbriqués,
  ADR-0012) garde un seul endroit et une seule durée de vie pour toute la configuration du
  binder, le mécanisme même qu’utilisent déjà la politique de nommage et les codes de
  l’ADR-0016.
* Faire du message un **constructeur évalué à l’émission**, plutôt qu’une chaîne stockée,
  est ce qui permet à un même hôte de servir plusieurs langues : il lit la culture ambiante
  par requête, conformément au patron de la bibliothèque qui résout les messages quand
  l’erreur est construite. Un message capturé à la construction des options figerait une
  langue, et la durée de vie « gelé à la première utilisation » des options la rendrait
  permanente — annulant l’exigence même.
* Ne localiser que les messages publics, et laisser le diagnostic dans une seule langue
  interne, préserve la convention selon laquelle les logs d’un même type d’erreur ne se
  scindent pas selon la langue de la requête.
* Faire défauter chaque définition sur le code livré et ses messages anglais garde le
  comportement sans configuration inchangé et le catalogue propre du paquet binder exact ;
  un consommateur qui ne surcharge rien n’est pas affecté, et les défauts sont exposés pour
  qu’un consommateur puisse toujours brancher symboliquement sur un code structurel.
* Remplacer la surcharge du code seul de l’ADR-0016, plutôt qu’ajouter un réglage de message
  en parallèle, est sans coût de migration tant que la bibliothèque est en pré-version, et
  arrête la surface des options comme un seul concept avant le gel v1.

## Alternatives considérées

### Un point de surcharge de message séparé, à côté des codes configurables

Considéré parce que c’est le plus petit ajout à la surface que l’ADR-0016 a déjà livrée.

Rejeté parce qu’il recrée exactement le découplage que le modèle d’erreurs codées existe
pour éviter : un consommateur pourrait aligner le code et oublier le message, ou l’inverse,
et la configuration du binder se scinderait en deux concepts sans gain.

### Stocker les messages de surcharge comme des chaînes fixes sur les options

Considéré parce qu’une simple chaîne est plus simple qu’un constructeur.

Rejeté parce que les options gèlent à la première utilisation : des chaînes stockées
figeraient une langue pour la durée de vie du processus — annulant la localisation par
requête, la seule propriété qu’exige l’issue #149.

### Exposer publiquement les fabriques de messages, pour que le consommateur fabrique les erreurs

Considéré par symétrie avec la façon dont un consommateur localise ses propres erreurs (il
en écrit les fabriques).

Rejeté parce qu’il ouvre les invariants structurels du binder — transience, la clé de
contexte du chemin d’argument, le câblage de l’erreur interne — à l’erreur du consommateur.
Le binder doit continuer de fabriquer ces erreurs et n’exposer que leur présentation : le
code et les messages publics.

## Conséquences

### Positives

* Le code et les messages structurels du binder se surchargent comme une unité cohérente, et
  un consommateur servant des clients non anglophones les localise par requête — clôturant
  l’issue #149.
* Toute la configuration du binder reste sur un seul objet d’options avec une seule durée de
  vie (ADR-0012), héritée par les binders imbriqués.
* Le comportement sans configuration et le catalogue propre du paquet binder sont inchangés
  (défauts préservés et toujours documentés, et toujours exposés pour le branchement
  symbolique).

### Négatives

* La surface des options change de forme : la surcharge du code seul consignée par
  l’ADR-0016 est remplacée par une définition code-et-message. Un changement cassant,
  acceptable en pré-version.
* Le message de diagnostic reste non surchargeable et anglais par convention ; un
  consommateur qui veut un diagnostic localisé n’est délibérément pas servi.

### Risques

* Le constructeur de message d’un consommateur s’exécute pendant la fabrication de l’erreur
  et pourrait lever, contrairement aux fabriques toujours sûres de la bibliothèque. Cela
  rejoint le contrat des points d’extension existants (le fournisseur de nom d’argument est
  aussi du code consommateur invoqué pendant la liaison) et relève de la responsabilité du
  consommateur.

## Actions de suivi

* Résolu à l’acceptation : l’ADR-0016 est marquée `Superseded`, remplacée par celle-ci —
  décision du mainteneur.
* La question de la dérive de catalogue d’un code surchargé, différée par l’ADR-0016 (liée à
  #140), est inchangée par cette décision.

## Références

* ADR-0016 — rendre configurables les codes d’erreur structurels du binder ; la surcharge du
  code seul que cette décision remplace.
* ADR-0012 — fixer les options du binder avant que la liaison ne commence ; la durée de vie
  des options que cette décision réutilise.
* Issue #149 — le constat que cette décision résout.
* Issue #147 — le constat que l’ADR-0016 a résolu (la moitié des codes).
* [Internationalisation](../../for-users/Internationalisation.fr.md) — le patron de
  localisation de la bibliothèque que cette décision suit.
