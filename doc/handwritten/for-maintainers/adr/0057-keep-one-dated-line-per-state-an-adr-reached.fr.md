# ADR-0057 | Garder une ligne datée par état atteint par une ADR, et n'en écraser aucune

🌍 🇫🇷 Français (ce fichier) · 🇬🇧 [English](0057-keep-one-dated-line-per-state-an-adr-reached.md)

**Statut :** Accepté
**Proposé :** 2026-07-29
**Accepté :** 2026-07-29
**Décideurs :** Reefact

## Contexte

L'en-tête d'une ADR porte une unique ligne `Date :`. La convention qui s'y attache, dans sa
rédaction la plus récente, veut que cette date soit le jour où la décision a atteint son statut
**courant** — le jour où elle a été proposée tant qu'elle est *Proposée*, le jour où elle a été
acceptée une fois *Acceptée* — la supersession étant la seule exception qui laisse la date en
place.

Cette convention rend l'acceptation destructrice. Faire passer une ADR de *Proposée* à *Acceptée*
écrase la date de proposition par celle d'acceptation, et la première disparaît du registre. Cette
base se décrit elle-même comme des « enregistrements datés de décisions significatives » et comme
« un journal historique » ; les deux dates répondent à des questions différentes, et une seule
survit.

Les deux dates sont des faits sur la décision, non sur le fichier. Le moment où une décision a été
rédigée et celui où elle a été ratifiée ont chacun un sens : l'intervalle entre les deux dit si la
décision a été débattue ou entérinée d'emblée, et si un mainteneur l'a laissée attendre. Rien
d'autre dans le dépôt ne consigne cet intervalle.

Sur les 56 ADR de la base, 20 sont passées par un état *Proposé* dans ce dépôt. Les 36 autres y
sont entrées déjà *Acceptées* — les ADR-0001 à ADR-0005, par exemple, ont toutes été commitées le
2026-07-21 en un seul import, portant des dates écrites à la main allant du 2026-07-10 au
2026-07-19, antérieures à l'existence du fichier dans git. Pour celles-là, aucune date de
proposition n'est récupérable : elles n'ont jamais été proposées ici, et git ne consigne que le
moment de l'import.

Git détient l'histoire des fichiers, pas celle des décisions. Le commit qui ajoute un fichier date
la rédaction, pas la proposition ; celui qui bascule un statut date l'édition, pas la ratification.
Pour des enregistrements maintenus à la main, et pour une base créée rétrospectivement, les deux
divergent.

Les variantes `.md` et `.fr.md` de chaque ADR portent le même en-tête : toute modification de sa
forme se fait donc deux fois par enregistrement.

## Décision

L'en-tête d'une ADR porte une ligne datée par état que la décision a réellement atteint dans ce
dépôt, et aucune date n'est jamais écrasée.

## Justification

La date unique n'est pas seulement incomplète, elle est destructrice dans la seule direction qu'un
journal ne peut pas se permettre : elle supprime un fait qui avait été consigné, au moment même où
une décision est ratifiée, et rien d'autre ne le détient. Ajouter une ligne à l'acceptation plutôt
que d'en réécrire une ne coûte rien et conserve les deux faits, ce qui est tout l'objet d'un
enregistrement daté.

Nommer les lignes d'après les états supprime la règle dont l'ancienne convention avait besoin.
`Date :` signifiait autre chose selon le statut : on ne pouvait donc ni la lire sans connaître la
convention, ni la mettre à jour sans l'appliquer. `Proposé :` et `Accepté :` disent ce qu'ils sont.
La supersession n'a plus besoin d'exception taillée pour elle : une supersession n'est pas un état
que cette décision a atteint, elle n'ajoute donc aucune ligne — exactement le résultat que
l'ancienne règle obtenait en décrétant une exception.

Les enregistrements antérieurs au format sont convertis plutôt que laissés de côté, et là où la
base ne détient qu'une seule date, cette date est écrite sur les deux lignes. Ce n'est pas la
fabrication que cela paraît d'abord, et c'est cette distinction qui rend la conversion recevable :
écrire deux fois la même date n'affirme rien qui ne fût déjà affirmé. Cela dit qu'une seule date
est connue et qu'elle vaut pour les deux états — exactement ce que signifiait une ligne `Date :`
unique — alors qu'inventer une date de proposition *différente*, tirée du commit d'import ou d'où
que ce soit, affirmerait un intervalle jamais observé. La répétition énonce le manque, elle ne le
masque pas.

La conversion vaut l'édition parce que l'alternative est une base à deux formes d'en-tête pour
toujours. La moitié des enregistrements répondant à « quand cela a-t-il été proposé ? » et l'autre
moitié incapable de le faire ferait peser la charge sur chaque lecteur futur, pour économiser une
passe mécanique sur des fichiers dont les dates ne sont pas contestées.

## Alternatives considérées

### Conserver la date unique et accepter la perte

Envisagée parce que c'est le statu quo, qu'il a été réaffirmé il y a quelques jours, et qu'elle
n'exige rien.

Écartée parce que la perte est silencieuse et définitive. La date de proposition disparaît au
moment de l'acceptation, sans qu'aucune trace dans l'enregistrement n'indique qu'une date a été
remplacée — le lecteur d'une ADR acceptée ne peut pas savoir si elle a été ratifiée d'emblée ou
après un mois, ni même que la question se pose.

### Conserver la date unique, et retrouver les dates de proposition dans git au besoin

Envisagée parce que git détient bien l'historique des fichiers, si bien que rien ne semble
réellement perdu, et parce qu'elle garde l'en-tête sur une ligne.

Écartée parce que git date le fichier, pas la décision. Pour les 36 enregistrements importés déjà
acceptés, le premier commit date l'import ; pour ceux maintenus à la main, un basculement de statut
date l'édition. Ni l'un ni l'autre n'est la date de proposition, et un lecteur n'aurait aucun moyen
de savoir lequel des deux il consulte.

### Changer le format mais laisser les enregistrements antérieurs sous la forme à une date

Envisagée, et d'abord préférée, parce que 36 des 56 enregistrements n'ont aucune date de
proposition à récupérer : ils sont entrés déjà acceptés. Les convertir semblait exiger d'en
inventer une, ce que cette décision existe précisément pour empêcher.

Écartée une fois la règle de conversion arrêtée : là où une seule date est connue, elle est écrite
sur les deux lignes, ce qui n'invente rien — cela répète une date que la base détenait déjà, et la
répétition est elle-même l'énoncé qu'aucun intervalle n'a été consigné. L'objection tombait, ne
laissant que le coût d'une passe mécanique face au coût permanent d'une base où la moitié des
enregistrements répond à « quand cela a-t-il été proposé ? » et l'autre non.

## Conséquences

### Positives

* Accepter une ADR cesse de détruire un fait consigné.
* L'intervalle entre proposition et acceptation devient lisible, pour la première fois.
* L'en-tête se décrit lui-même : chaque ligne dit quel état elle date, si bien que ni la lecture ni
  la mise à jour n'exigent de connaître une convention.
* La supersession n'a plus besoin d'exception ; elle n'ajoute simplement aucune ligne.

### Négatives

* Chaque enregistrement de la base a été édité pour porter le nouvel en-tête, dans les deux
  variantes de langue.
* Pour les 36 enregistrements qui n'ont jamais connu d'état *Proposé* ici, les deux lignes portent
  la même date et ne disent rien de plus que la ligne unique.
* Chaque ADR gagne une ligne à l'acceptation, dans les deux variantes de langue.
* Le template et la section « Format » ont dû changer : ce qui en a été dérivé auparavant est
  désormais périmé.

### Risques

* Un lecteur peut prendre deux dates identiques pour un intervalle nul — une décision ratifiée le
  jour de sa rédaction — plutôt que pour « une seule date est connue ». Le README distingue les
  deux, mais seulement pour qui l'atteint.
* Un agent ou un mainteneur habitué à l'ancienne règle peut écraser `Proposé :` à l'acceptation par
  réflexe, rétablissant silencieusement la perte que cette décision supprime. Rien ne le vérifie.

## Actions de suivi

* Surveiller les premières acceptations sous le nouveau format pour détecter le réflexe d'écrasement,
  et envisager un contrôle s'il se reproduit.
* Si un enregistrement antérieur venait à être superséé, laisser sa ligne `Date :` unique en place :
  c'est le successeur qui porte les nouvelles dates.

## Références

* [ADR-0024](0024-allow-a-one-time-editorial-refactoring-of-accepted-adrs.fr.md) — l'exception bornée
  à la règle de ne pas éditer une ADR acceptée en place, et le précédent d'une intervention sur toute
  la base sous une règle énoncée et traçable.
* [ADR-0004](0004-check-every-pull-request-against-the-adr-base.fr.md) — pourquoi cette base est
  vérifiée à chaque pull request, et donc pourquoi ses enregistrements sont lus et pas seulement
  écrits.
