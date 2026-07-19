# ADR-0021 | Lier les arguments hors-DTO comme des pairs via un point d’entrée agnostique de la source et non typé

🌍 🇫🇷 Français (ce fichier) · 🇬🇧 [English](0021-bind-out-of-dto-arguments-as-peers-through-a-source-agnostic-entry.md)

**Statut :** Proposé
**Date :** 2026-07-19
**Décideurs :** Reefact

## Contexte

* Le binder construisait une commande à partir d’un unique DTO de requête. Son
  point d’entrée était centré-DTO — un binder était *démarré sur* un DTO, et
  l’enveloppe d’échec était déclarée sur le DTO en deuxième temps. Il n’existait
  aucune couture pour une entrée qui ne vit pas dans le DTO.
* De vrais adaptateurs primaires assemblent régulièrement une commande à partir de
  plus que le corps : un identifiant de route, un paramètre de query, un en-tête de
  requête, un claim. L’hôte a déjà extrait ces valeurs individuelles ; elles ont
  besoin de la même liaison — collecte exhaustive, codée, porteuse de chemin — que
  les propriétés du DTO, dans la **même** enveloppe — pour qu’un mauvais segment de
  route et un mauvais champ de corps soient rapportés ensemble.
* Le binder est agnostique du framework (contrôleurs HTTP, consommateurs de
  messages, CLI, handlers gRPC). Extraire une valeur d’une requête HTTP entrante
  relève de la connaissance de l’hôte ; la bibliothèque voit des valeurs déjà
  extraites.
* Le chemin d’erreur d’une propriété de DTO est dérivé par réflexion depuis le nom
  de propriété C# (via l’`IArgumentNameProvider` configuré) ; une valeur hors-DTO
  n’a pas de propriété sur laquelle réfléchir, son chemin doit donc être indiqué par
  l’appelant.
* La provenance d’une propriété de DTO est uniforme et implicite — chaque propriété
  vient de l’unique corps de requête. La provenance d’une valeur hors-DTO ne l’est
  pas : « route », « query » et « header » sont des origines distinctes que
  l’appelant a dû indiquer, et qui sont utiles au diagnostic pour distinguer une
  entrée en échec d’une autre.
* Nommer le type de commande au point d’entrée, l’inférer partout ailleurs, et
  garder un seul type de binder ne peuvent pas tenir tous les trois à la fois :
  avec le type de commande fixé sur une entrée générique, une liaison complexe
  imbriquée place le type imbriqué en position de *paramètre* de délégué, où
  l’inférence de groupe de méthodes de C# ne peut pas le récupérer — forçant un
  argument de type explicite à chaque site d’appel imbriqué.
* Une propriété complexe de DTO est, par construction, un chemin dans un DTO ; un
  argument hors-DTO n’a pas de DTO où cheminer.
* Les erreurs portent un contexte typé ; une clé d’accès publique permet à un
  consommateur de lire une entrée de contexte sans dépendre de son nom interne.
* La bibliothèque est en pré-release, non publiée sur NuGet et sans consommateur
  externe : la surface du point d’entrée peut donc encore changer sans migration.
* L’ADR-0007 a nommé les terminaux de construction `New` / `Create` ; l’ADR-0012 a
  fixé les options d’un binder à son point d’entrée ; l’ADR-0017 a rendu réglable le
  défaut d’options applicatif. Les trois décrivent le point d’entrée dans sa forme
  précédente, centrée-DTO.

## Décision

Le binder est agnostique de la source : son point d’entrée non typé déclare
l’enveloppe d’échec d’emblée et attache les entrées comme des pairs — un DTO via une
source de propriétés, et des arguments hors-DTO nommés individuellement (chacun
indiquant sa provenance) via une source d’arguments — le type de commande n’étant
nommé qu’au terminal `New` / `Create`, et sans argument hors-DTO complexe.

## Justification

* Faire déclarer l’enveloppe par l’entrée et attacher le DTO et les arguments comme
  des pairs est ce qui permet à une valeur de route/query/en-tête de se lier dans la
  *même* enveloppe, avec les *mêmes* chemins et les *mêmes* deux codes structurels,
  qu’une propriété de corps — c’est l’exigence. Une entrée centrée-DTO n’a nulle
  part où placer une entrée absente du DTO ; un modèle de pairs, si.
* L’entrée est non typée parce que le modèle agnostique de la source retire la
  raison de la typer. La commande n’est plus « construite sur un DTO » ; elle est
  assemblée au terminal à partir de pairs. Nommer le type de commande au terminal
  résout la tension à trois du Contexte : le terminal l’infère depuis l’assembleur,
  il n’est donc nommé nulle part ailleurs, un seul type de binder sert la liaison de
  premier niveau et la liaison imbriquée, et — parce que le type imbriqué n’apparaît
  désormais qu’en position de *retour* de la liaison imbriquée — l’inférence de
  groupe de méthodes le récupère sans argument de type explicite. L’intention reste
  exprimée, par le nom de la fabrique d’enveloppe et par le type propre de la
  commande, pas par un paramètre de type redondant sur l’entrée.
* Un argument hors-DTO indique son propre nom parce qu’il n’y a pas de propriété
  d’où le dériver ; le nom est utilisé tel quel comme chemin, l’appelant contrôle
  donc directement la clé du fil, là précisément où une propriété de DTO s’en remet
  au fournisseur de noms.
* Capturer la provenance d’un argument, et *seulement* celle d’un argument, colle à
  l’endroit où l’information existe et vaut la peine d’être conservée : l’origine
  d’un argument a été indiquée par l’appelant et distingue des échecs par ailleurs
  semblables, tandis que l’origine d’une propriété de DTO est l’unique corps
  implicite et serait du bruit sur chaque propriété. L’asymétrie est délibérée, pas
  un oubli.
* Réutiliser la surface de convertisseurs existante (`AsRequired`, `AsOptional`, les
  optionnels valeur et référence, et la forme liste) pour les arguments garde un
  seul modèle mental pour chaque entrée : un argument ne diffère d’une propriété que
  par la façon dont il est nommé et sourcé, jamais par la façon dont il est lié.
* Omettre un argument hors-DTO complexe est une conséquence de ce que sont les deux
  concepts, pas une coupe de fonctionnalité : une propriété complexe déréférence un
  chemin de DTO, et un argument n’a pas de DTO à déréférencer. Une valeur complexe
  assemblée à partir de plusieurs arguments s’exprime en liant chacun comme un pair
  et en les combinant dans le terminal — aucun nouveau concept requis.
* Le statut de pré-release signifie que la forme du point d’entrée est arrêtée
  maintenant, quand il n’y a aucun consommateur à migrer.

## Alternatives considérées

### Garder l’entrée centrée-DTO et traiter une valeur hors-DTO comme un DTO synthétique à une propriété

Considérée parce qu’elle réutilise le chemin de propriété existant sans changement.

Rejetée parce qu’elle force l’appelant à emballer chaque valeur libre dans un DTO
jetable dans le seul but de satisfaire la forme d’entrée, et le chemin dérivé par
réflexion rapporte alors le nom de propriété de cet emballage plutôt que la clé du
fil voulue par l’appelant — l’inverse de ce dont un argument hors-DTO a besoin.

### Typer le point d’entrée sur la commande (un `Bind.To<TCommand>` générique)

Considérée parce qu’elle énonce le type cible en tête, se lit comme intention-d’abord,
et garde un seul type de binder.

Rejetée parce que, le type de commande étant fixé sur l’entrée, une liaison complexe
imbriquée place le type imbriqué en position de paramètre de délégué où l’inférence
de groupe de méthodes ne peut pas le récupérer, donc chaque site d’appel imbriqué
doit épeler un argument de type explicite — une écharde persistante sur la forme de
binder la plus courante. L’entrée non typée retire l’écharde tout en exprimant
l’intention via l’enveloppe et le type de commande.

### Ajouter un argument hors-DTO complexe reflétant la propriété complexe

Considérée par symétrie avec le côté DTO, pour que les arguments et les propriétés
offrent les mêmes formes.

Rejetée parce qu’elle n’a pas de référent : une propriété complexe lie un DTO
imbriqué atteint par un chemin, et un argument hors-DTO n’a ni DTO ni chemin. L’API
d’apparence symétrique nommerait une chose qui n’existe pas ; la composition
existante pairs-plus-terminal couvre déjà « une valeur complexe bâtie à partir
d’arguments ».

### Encoder la provenance dans le chemin d’erreur de l’argument (par exemple `route:bookingId`)

Considérée parce qu’elle ne nécessite aucune deuxième clé de contexte.

Rejetée parce qu’elle amalgame deux faits distincts — *où* est l’entrée (le chemin,
servant à localiser le champ) et *quel type d’origine* elle a (la source, servant à
classer l’échec) — en une seule chaîne que les consommateurs devraient alors parser,
le parsing de message même que le binder existe pour éviter. Une clé de contexte
séparée et typée garde les deux faits de première classe.

## Conséquences

### Positives

* Une commande assemblée à partir d’un corps et de tout mélange de valeurs de
  route/query/en-tête se lie en une passe, dans une enveloppe, avec un seul jeu de
  chemins et de codes.
* Le site d’appel de la liaison imbriquée n’a besoin d’aucun argument de type
  explicite ; un seul type de binder sert la liaison de premier niveau et imbriquée.
* L’échec d’un argument porte sa provenance, un consommateur peut donc classer les
  échecs (une mauvaise route vs un mauvais en-tête) sans parser les messages.
* La surface de convertisseurs est inchangée, un argument est donc lié avec
  exactement les verbes d’une propriété.

### Négatives

* La forme du point d’entrée change de la forme centrée-DTO que l’ADR-0007,
  l’ADR-0012 et l’ADR-0017 décrivent en prose ; les décisions de ces ADR sont
  inchangées, mais leur surface illustrative est désormais historique.
* Deux façons d’attacher une entrée (source de propriétés, source d’arguments) sont
  une surface un peu plus large qu’une seule — acceptée parce qu’elles modélisent
  deux provenances réellement différentes, pas deux saveurs de la même chose.

### Risques

* Un appelant pourrait chercher un « argument complexe » inexistant et être
  brièvement surpris par son absence ; atténué en documentant la composition
  pairs-plus-terminal comme la voie prévue pour bâtir une valeur complexe à partir
  d’arguments.
* Des étiquettes de provenance libres pourraient dériver dans une base de code
  (« route » vs « path ») ; atténué par les raccourcis de provenance (`FromRoute`,
  `FromQuery`, …) qui fixent les étiquettes courantes, laissant le `From(source, …)`
  brut pour le reste.

## Actions de suivi

* Mettre à jour le guide du request-binder (EN + FR) et le README du paquet vers
  l’entrée agnostique de la source et la section des arguments hors-DTO.
* Envisager un paquet d’intégration hôte qui extrait les valeurs d’une requête HTTP
  entrante (plutôt que d’étiqueter celles déjà extraites), si une demande de
  consommateur apparaît.

## Références

* ADR-0007 — nommer les terminaux du binder New et Create ; le terminal qui porte
  désormais aussi le paramètre de type de commande. Décision inchangée.
* ADR-0012 — fixer les options du binder avant le début de la liaison ; les options
  sont toujours fixées au point d’entrée (désormais agnostique de la source).
  Décision inchangée.
* ADR-0017 — fournir un défaut applicatif configurable pour les options du binder ;
  le défaut soutient toujours le point d’entrée nu. Décision inchangée.
* Issue #148 — la demande que cette décision résout.
* [`fluent-request-binder`](https://github.com/Reefact/fluent-request-binder) — le
  binder antérieur dont le modèle source/argument a informé cette décision.
