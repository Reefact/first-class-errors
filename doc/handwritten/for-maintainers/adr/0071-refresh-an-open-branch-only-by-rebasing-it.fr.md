# ADR-0071 | Rafraîchir une branche ouverte uniquement en la rebasant sur `main`

🌍 🇫🇷 Français (ce fichier) · 🇬🇧 [English](0071-refresh-an-open-branch-only-by-rebasing-it.md)

**Statut :** Accepté
**Proposé :** 2026-08-10
**Accepté :** 2026-08-10
**Décideurs :** Reefact

## Contexte

L'[ADR-0070](0070-land-pull-requests-by-rebasing-and-keep-main-linear.fr.md) a
rendu l'historique de `main` linéaire et fait du rebase la façon dont une pull
request atterrit. Elle a nommé un point qu'elle ne tranchait pas, en le laissant à
une décision distincte : `CONTRIBUTING.md` (« Branches ») propose **deux** façons
de reporter les avancées de `main` dans une branche ouverte — la rebaser sur
`origin/main` tant qu'elle n'appartient qu'à vous, ou y merger `origin/main` dès
que d'autres ont pu baser du travail dessus.

La seconde produit exactement les commits que l'ADR-0070 a supprimés. Sur les 401
commits de merge retirés de `main`, **87 étaient des `Merge branch 'main' into …`**,
créés en appliquant cette règle. Ils n'étaient pas gratuits non plus : 15 d'entre
eux portaient une résolution de conflit faite à la main, et reproduire ces
résolutions a été la partie délicate de la réécriture.

La règle ne fait d'ailleurs plus ce qu'elle annonce sous la nouvelle stratégie.
Elle a été écrite pour un dépôt à commits de merge, où un merge effectué sur une
branche restait sur la branche et où seul son résultat atteignait `main`. Avec le
*Rebase and merge*, GitHub rejoue les commits propres de la branche sur `main` :
une branche qui a mergé `main` en elle emporte donc ce merge dans ce qui
atterrit. La règle réimporte en silence le bruit que la réécriture a enlevé.

Ce que la règle protège est réel : rebaser une branche que quelqu'un d'autre a
déjà récupérée détruit son travail. Mais cette situation n'existe pas dans le
modèle de branches de ce dépôt. `CONTRIBUTING.md` énonce qu'une branche porte
**une** pull request, qu'elle est « l'espace de travail jetable d'une pull
request », et que son nom prend la forme `<author>/<short-description>` — le
propriétaire est nommé dans la ref elle-même. Une branche sur laquelle deux
personnes construisent est déjà hors du modèle, et l'option de merge est la seule
règle qui suppose le contraire.

## Décision

Une branche ouverte est mise à jour avec `main` **uniquement en la rebasant sur
`origin/main`**. On ne merge pas `origin/main` dans une branche.

## Conséquences

### Le force-push devient la façon ordinaire de rafraîchir une branche

C'est déjà le cas pour les branches que ce dépôt possède réellement.
`CONTRIBUTING.md` autorise à réécrire l'historique d'une branche tant qu'elle
n'appartient qu'à vous et impose `git push --force-with-lease` plutôt qu'un
`--force` nu ; rafraîchir, c'est la même opération. Rien de nouveau n'est
autorisé, une alternative est retirée.

### Une branche déjà récupérée par autrui n'a plus d'échappatoire, volontairement

Si une seconde personne a réellement basé du travail sur une branche ouverte, la
réponse est de coordonner le force-push ou de scinder le travail en deux branches
— pas de placer un commit de merge dans l'historique futur de `main`. Rendre ce
cas visible est précisément le but : sous la règle précédente il se réglait en
silence, par un commit que personne ne relisait.

### Le coût de la réécriture n'est pas payé une seconde fois

L'ADR-0070 enregistre que réécrire `main` est une exception unique dont la
fenêtre se referme à la 1.0.0. Laisser l'option de merge ouverte permettrait à la
forme de commit qu'elle a supprimée de réapparaître une branche à la fois, sans
qu'une seconde réécriture soit disponible pour nettoyer.

## Alternatives considérées

### Conserver l'option de merge et compter sur GitHub pour l'aplatir

Envisagée parce que le *Rebase and merge* rejoue bien les commits d'une branche,
et qu'on pourrait attendre qu'un commit de merge disparaisse au passage. Rejetée
parce que le résultat n'est pas quelque chose sur quoi s'appuyer : ce qu'un
rebase fait d'un commit de merge dépend de la forme de la branche, et une règle
dont la sûreté repose sur le comportement d'aplatissement actuel de la plateforme
est une règle qui casse en silence quand ce comportement change. L'historique du
dépôt en est la preuve — 87 commits de cette forme ont atteint `main` sous la
stratégie précédente.

### Conserver l'option de merge pour les seules branches réellement partagées

Envisagée parce que la protection offerte est réelle quand elle s'applique.
Rejetée parce qu'elle s'applique à une branche que ce dépôt n'a pas, et parce
qu'une règle assortie d'une exception se lit comme une règle assortie d'une
échappatoire. La doctrine de branche existante — une pull request, un
propriétaire, coupée fraîche, supprimée au merge — est ce qui rend l'exception
inutile ; si cette doctrine change un jour, la présente décision devra être
revue avec elle plutôt qu'affaiblie d'avance pour un cas qui ne se produit pas.
