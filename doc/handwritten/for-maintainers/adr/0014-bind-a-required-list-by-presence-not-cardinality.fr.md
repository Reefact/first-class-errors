# ADR-0014 | Lier une liste requise par la présence, pas la cardinalité

🌍 🇬🇧 [English](0014-bind-a-required-list-by-presence-not-cardinality.md) · 🇫🇷 Français (ce fichier)

**Statut :** Accepté
**Date :** 2026-07-18
**Décideurs :** Reefact

## Contexte

* Le binder lie une propriété de requête de type liste via `AsRequired` ou `AsOptional`,
  le même choix de présence qu'il offre pour une propriété scalaire.
* Le binder déduit « l'argument est manquant » du fait que la propriété du DTO est `null`.
  Un sérialiseur mappe un tableau JSON absent vers une propriété `null` ; un tableau JSON
  présent mais vide (`[]`) se désérialise en une liste non-null de zéro élément.
* Sur une liste manquante (`null`), `AsRequired` enregistre `REQUEST_ARGUMENT_REQUIRED` et
  `AsOptional` lie une liste vide sans rien enregistrer.
* Avant cette décision, le comportement d'une liste requise *présente mais vide* était non
  spécifié : ni documenté, ni épinglé par un test. L'implémentation la liait en succès — une
  liste vide — car « manquant » n'est que `null`, si bien qu'une liste vide atteint la boucle
  par élément, qui itère zéro fois.
* La revue de niveau mainteneur du request binder (spéc de conception #126) a soulevé ce
  point comme constat 19 sur 19 (issue #155) : malgré 100 % de couverture ligne et branche,
  aucune assertion ne fixait si une liste requise vide est valide, si bien que le comportement
  pouvait changer silencieusement.
* Pour une propriété scalaire, « requis » signifie « présent » — un argument non-null ; la
  conversion de sa valeur est une préoccupation distincte que porte la fabrique d'objet valeur.
* Le rôle du binder est la présence et la conversion par élément : il enregistre une erreur
  codée par élément en échec et collecte chaque échec dans une seule enveloppe. Il ne porte
  aucune notion de taille de collection.
* Une règle de cardinalité minimale (« au moins un élément ») est une règle métier qui varie
  selon le cas d'usage, et la bibliothèque place déjà les invariants métier dans l'objet valeur
  ou la commande qu'une valeur liée alimente.
* La bibliothèque est en pré-version, non publiée sur NuGet et sans consommateur externe :
  fixer le contrat maintenant n'entraîne aucun coût de migration.

## Décision

Une liaison de liste requise contraint uniquement la présence de la liste — une liste absente
(`null`) enregistre `REQUEST_ARGUMENT_REQUIRED`, tandis qu'une liste présente mais vide est
valide et se lie à une liste vide.

## Justification

* La présence est le seul sens que « requis » porte déjà pour une propriété scalaire, et une
  liste est une propriété comme une autre ; laisser une liste vide compter comme « manquante »
  ferait signifier à `AsRequired` « présent » pour un scalaire et « présent et non vide » pour
  une liste — deux contrats sous un même nom, le genre d'incohérence que la conception
  délibérément à faible surface du binder évite.
* Garder la cardinalité hors du binder respecte la séparation que la bibliothèque trace déjà :
  le binder répond à « l'argument a-t-il été envoyé, et chaque élément se convertit-il ? », et
  l'objet valeur ou la commande qu'il alimente répond à « est-ce une valeur métier valide ? »,
  là où une règle de taille minimale a sa place. Inscrire « non vide » dans chaque liste requise
  imposerait la politique d'un domaine à toutes, et cette politique varie réellement
  (zéro-ou-plus ici, au-moins-un ailleurs).
* Lire « manquant » comme `null` garde le binder fidèle à ce que le client a réellement envoyé :
  un tableau absent et un tableau vide sont deux messages différents sur le fil, et une liste
  vide est une valeur délibérée, pas une absence. Enregistrer `REQUEST_ARGUMENT_REQUIRED` pour
  une liste que le client a bien envoyée rapporterait mal ce message.
* La revue n'a trouvé aucun défaut dans le comportement, seulement qu'il n'était pas asserté ;
  ratifier le comportement existant comme contrat — plutôt que le changer — est le choix le
  moins surprenant, et le statut de pré-version permet de fixer le contrat maintenant sans
  consommateur à migrer.

## Alternatives considérées

### Traiter une liste requise présente mais vide comme manquante

Envisagée parce que « requis » peut familièrement suggérer « au moins un », si bien qu'un
appelant pourrait s'attendre à ce qu'une liste requise vide soit rejetée.

Rejetée parce qu'elle confond présence et cardinalité : elle donnerait à `AsRequired` deux sens
différents pour les scalaires et les listes, inscrirait la politique de taille minimale d'un
domaine dans le binder, et rapporterait mal comme une absence un tableau vide délibérément
envoyé.

### Ajouter une liaison requise non-vide distincte (ou une option de compte minimal)

Envisagée parce que certaines requêtes ont réellement besoin d'au moins un élément, et un
`AsRequiredNonEmpty` explicite (ou une option de compte) l'exprimerait au niveau du binder.

Rejetée pour l'instant parce qu'elle élargit la surface du binder avec une préoccupation de
cardinalité que l'objet valeur ou la commande exprime déjà, et qu'aucune exigence actuelle ne la
réclame. Elle reste ouverte comme opt-in futur : cette décision ne fixe que le sens par défaut de
« requis », si bien qu'une variante non-vide ultérieure l'étendrait, sans la contredire.

### Laisser le comportement non spécifié

Envisagée parce que c'est le plus petit changement — l'implémentation lie déjà une liste requise
vide en succès.

Rejetée parce qu'un contrat non asserté est précisément le constat de #155 : il peut changer
silencieusement lors d'un remaniement. Une décision énoncée plus un test d'épinglage le rend
stable.

## Conséquences

### Positives

* « Requis » a un sens unique pour les propriétés scalaires et de liste : présent, non-null.
* Le binder reste une frontière de présence et de conversion ; les règles de cardinalité métier
  restent dans le domaine, où elles peuvent varier selon le cas d'usage.
* Le contrat est documenté et épinglé par des tests, si bien qu'il ne peut plus dériver
  silencieusement.

### Négatives

* Une requête qui a réellement besoin d'une liste non vide doit l'imposer dans l'objet valeur ou
  la commande qu'elle alimente, pas via le binder — une petite étape explicite pour le
  consommateur.

### Risques

* Un besoin futur de cardinalité minimale exigerait une nouvelle surface de liaison opt-in ;
  atténué par le fait que cette décision ne fixe que le sens par défaut (laissant la place à une
  telle surface) et que la cardinalité est exprimable dans le domaine aujourd'hui.

## Actions de suivi

* Documenté dans le même changement : la doc de référence du binder
  ([`RequestBinder.fr.md`](../../for-users/RequestBinder.fr.md) et sa version anglaise) et la
  doc XML de l'API sur les trois convertisseurs de liste.
* Épinglé dans le même changement : des tests unitaires sur les convertisseurs de liste simple,
  de type valeur et complexe, plus des invariants property-based pour les garanties de
  collecte-globale et de stabilité des chemins dans `FirstClassErrors.RequestBinder.PropertyTests`.

## Références

* Issue #155 — le constat que cette décision résout (constat 19 sur 19 de la revue du request
  binder).
* Pull request #126 — la spéc de conception du request binder à laquelle ce contrat appartient.
* ADR-0007 — nommer les terminaux du binder New et Create ; une décision d'API publique sœur sur
  le même binder.
* ADR-0012 — fixer les options du binder avant le début de la liaison ; une autre décision de
  contrat du binder.
