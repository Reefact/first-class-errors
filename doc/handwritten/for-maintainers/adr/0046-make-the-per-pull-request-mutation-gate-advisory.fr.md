# ADR-0046 | Rendre la porte de mutation par pull request consultative

🌍 🇫🇷 Français (ce fichier) · 🇬🇧 [English](0046-make-the-per-pull-request-mutation-gate-advisory.md)

**Statut :** Proposé
**Date :** 2026-07-27
**Décideurs :** Reefact

## Contexte

L'ADR-0043 a fait des tests de mutation un **check requis, limité au diff, sur chaque pull request**,
adossé à un balayage complet hebdomadaire. Deux propriétés de ce check, toutes deux délibérées et
documentées, se combinent en un coût que rien de ce que contrôle une pull request ne borne :

* **Une passe complète de la suite de tests de la bibliothèque par mutant.** `"coverage-analysis"` est
  à `off` volontairement : sous le runner MTP, la sélection par test de Stryker est incomplète
  (stryker-net#3629) et rapporte un score **faux, sous-estimé** ; le chiffre exact exige donc de rejouer
  toute la suite contre chaque mutant.
* **La sélection se fait par *fichier* changé, pas par *ligne* changée.** Le `--since` de Stryker n'a
  pas de granularité à la ligne : toucher une seule ligne d'un gros fichier met **tous** les mutants de
  ce fichier sur la porte.

La conséquence est apparue sur un correctif d'une région d'une ligne dans `JustDummies/Any.cs`
(~1000 lignes, le plus gros fichier du dépôt) : le leg `justdummies` a sélectionné les mutants de tout le
fichier et tourné **~40 minutes**, bloquant le merge, alors que tous les autres checks requis
finissaient en ~2–3 minutes. Le coût suit la taille du *fichier où la modification atterrit*, qu'aucun
auteur ne peut garder petit sur un type central.

Deux faits supplémentaires pèsent sur la décision :

* La porte porte **`break: 0`** : elle n'impose **aucun seuil de score**. Sa seule assertion au moment
  de la pull request est « les legs sont allés à leur terme ». Le vrai signal de qualité imposé est le
  **balayage complet hebdomadaire** (qui remesure tout) plus le **rapport** par-PR des mutants
  survivants.
* Le job `gate` échoue (`exit 1`) dès que ses legs sont `cancelled`. Le groupe `concurrency` du workflow
  annule un run en vol dès qu'un commit plus récent atterrit sur la branche — un événement ordinaire
  (« Update branch », un merge dependabot dans `main`). Chaque supplantation a donc affiché une porte de
  mutation **« failed » fallacieuse** sur une pull request parfaitement saine.

## Décision

Sur les **pull requests**, la porte de mutation est **consultative**. Les legs par-PR tournent toujours
et rapportent le score de mutation du diff, mais le job `gate` **ne fait jamais échouer la pull
request** : un vrai échec de leg est remonté comme avertissement à investiguer, et un run annulé par une
poussée supplantante est traité comme du bruit, pas comme un échec. Le **niveau imposé est le balayage
complet hebdomadaire**. Le nom du job et du check restent stables, donc aucune entrée de protection de
branche n'a à changer.

## Justification

* **Le coût bloquant ne peut pas tenir dans un budget de feedback raisonnable sans renoncer à
  l'exactitude.** Le seul levier qui rendrait rapide « une suite complète par mutant » — la sélection de
  couverture par test — est précisément celui que l'ADR-0043 a désactivé parce qu'il ment sous MTP
  (stryker-net#3629). Bloquer un merge sur un check dont la forme honnête se compte en minutes à dizaines
  de minutes, à l'échelle de la taille du fichier touché, n'est pas un contrat de check requis
  raisonnable.
* **Le rendre consultatif ne retire presque aucun enforcement réel.** Avec `break: 0`, la porte n'a
  jamais imposé de score ; elle affirmait seulement que les legs finissaient. Un vrai échec de build qui
  casserait les legs casse aussi `Build & test`, qui reste requis. Ce qu'on abandonne, c'est un *seuil
  par-PR qui n'a jamais existé*.
* **Le balayage hebdomadaire est le vrai signal, et il est inchangé.** Il remesure chaque mutant de toute
  la bibliothèque, sans seuil, précisément pour ne pas passer `main` au rouge sur du code non édité. Des
  legs par-PR consultatifs gardent le signal rapide par-diff comme *rapport*, sans le promouvoir en
  bloqueur.
* **Les échecs par annulation de concurrence n'ont jamais été voulus.** Un run supplanté rapportant
  « gate failed » est un faux négatif ; le rapport consultatif supprime cette classe de bruit comme effet
  de bord.

## Alternatives considérées

### Le garder bloquant, le rendre rapide par la sélection de couverture par test

Rejeté : sous le runner MTP, cette sélection compte des mutants tués comme non couverts et rapporte un
score faux (stryker-net#3629). L'exactitude est la raison pour laquelle `"coverage-analysis"` est à
`off` ; l'échanger contre de la vitesse ferait mentir la porte — la seule chose que ce dépôt refuse d'un
diagnostic.

### Le garder bloquant, découper chaque gros fichier pour que la sélection par fichier reste petite

Une vraie amélioration, méritante en soi — `Any.cs` est un god-file, et le découper en fichiers de
classe partielle par thème garderait petit l'ensemble de mutants de n'importe quel diff. Mais c'est un
refactor lourd et prudent, il n'offre aucune garantie contre le *prochain* fichier qui grossit, et ce
n'est pas un préalable au merge d'un changement correct aujourd'hui. Laissé en suivi (ci-dessous), pas en
bloqueur du déblocage.

### Retirer complètement les legs par-PR et ne compter que sur le balayage hebdomadaire

Rejeté : cela jette le signal rapide par-diff qu'un contributeur utilise quand le changement est frais.
Le consultatif garde ce signal — comme rapport — sans le blocage.

### Retirer la porte de la protection de branche plutôt que de changer le job

Équivalent en effet, mais laisse le job `gate` émettre `exit 1` à l'annulation (le rouge fallacieux
persiste donc sur la page du run) et dépend d'une édition de protection de branche plutôt que d'être
autonome dans le workflow. Changer le job corrige le blocage et le bruit au même endroit.

## Conséquences

### Positives

* Le feedback de merge d'une pull request revient aux ~2–3 minutes des autres checks requis ; les legs de
  mutation ne sont plus sur le chemin critique.
* Les checks « mutation gate failed » fallacieux des runs annulés par concurrence disparaissent.
* Le **rapport** de mutation par-PR (mutants survivants, fichier et ligne) est inchangé et toujours
  remonté.

### Négatives

* Une vraie régression de mutation introduite par une pull request ne la bloque plus ; elle est attrapée
  par le balayage hebdomadaire et le rapport par-PR de niveau avertissement, avec un délai pouvant aller
  jusqu'à une semaine.

### Risques

* Le balayage complet hebdomadaire devient le **seul enforcement**. Si sa sortie n'est pas lue, la
  couverture réelle peut dériver d'un lundi à l'autre. Atténuation : le balayage publie déjà la liste des
  survivants par bibliothèque ; en garder la lecture pour habitude est la contrepartie de cette décision.

## Actions de suivi

* **Accélérer le run consultatif lui-même.** Une hausse de `concurrency` est appliquée dans
  `justdummies.json`. Les leviers de temps plus importants — retirer la suite de propriétés FsCheck de
  l'*oracle* de mutation (ses cent cas par propriété dominent le temps par mutant, et son
  non-déterminisme est la raison même du `coverage-analysis: off`), et/ou découper `JustDummies/Any.cs`
  pour que la sélection `--since` par fichier reste petite — sont des décisions séparées, consignées ici
  pour ne pas les perdre.
* **Protection de branche.** Le check de la porte peut rester listé comme requis (il passe désormais
  toujours) ou être retiré ; les deux sont cohérents avec cette décision. Aucun changement n'est requis
  pour que le workflow se comporte comme décrit.
* Réexaminer la réactivation de la sélection de couverture par test si stryker-net#3629 est corrigé en
  amont — elle rendrait à nouveau possible une porte bloquante, exacte et rapide, et pourrait remplacer
  cette décision.

## Références

* ADR-0043 — Contrôler les pull requests sur le score de mutation du diff : la décision que celui-ci
  amende. Cet ADR restreint sa moitié « pull request » de *requise* à *consultative* ; le balayage
  complet hebdomadaire qu'il a établi est inchangé.
* ADR-0011 — Héberger JustDummies comme paquet autonome : pourquoi `justdummies-mutation.yml` est un
  workflow séparé avec sa propre porte.
* stryker-net#3629 — le défaut de sélection de couverture par test sous le runner MTP qui maintient
  `"coverage-analysis": "off"`.
* `doc/handwritten/for-maintainers/workflows/mutation.en.md` et `justdummies-mutation.en.md` — le modèle
  de coût et le raisonnement « exactitude, pas vitesse » cités ci-dessus.
