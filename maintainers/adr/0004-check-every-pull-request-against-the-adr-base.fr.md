# ADR-0004 | Contrôler chaque pull request au regard de la base d'ADR

🌍 🇬🇧 [English](0004-check-every-pull-request-against-the-adr-base.md) · 🇫🇷 Français (ce fichier)

**Statut :** Accepté
**Date :** 2026-07-15
**Décideurs :** Reefact

## Contexte

Le dépôt consigne les décisions importantes sous forme d'ADR dans `maintainers/adr/`,
mais rien ne confronte un changement à cette base au moment où le changement est
réalisé. C'est dans une pull request que les nouvelles décisions entrent dans la base
de code : elle peut embarquer une décision qui n'est jamais consignée, remplacer une
décision que porte un ADR existant sans le dire, ou contredire un ADR accepté sans que
personne ne le remarque.

Les invariants durs et mécaniques sont déjà gardés ailleurs : la règle `class` des
value objects, le floor Roslyn de l'analyzer (ADR-0001) et le floor du runtime de
l'outillage (ADR-0002) sont garantis par des tests unitaires et des jobs CI qui
échouent de façon déterministe. Ce qu'aucun test n'exprime, c'est la question plus
subtile que le lecteur d'un diff doit malgré tout se poser : y a-t-il ici une décision
d'architecture, et s'accorde-t-elle avec ce qui a déjà été décidé ?

Le travail sur le dépôt est en grande partie produit au cours de sessions de codage
Claude Code, qui chargent `CLAUDE.md` (et, lorsqu'on le leur demande, `AGENTS.md`)
comme instructions et détiennent, au sein de la session, l'intégralité du diff, la base
d'ADR et le raisonnement qui a produit le changement. Il est aussi ouvert à des
contributeurs qui n'utilisent pas Claude Code. Un workflow GitHub Actions peut exécuter
un modèle sur déclenchement manuel, comme le fait déjà le workflow `changelog`. Un tel
appel de modèle a un coût par requête, et le modèle est référencé par un alias flottant,
de sorte que son verdict n'est pas reproductible.

Le mainteneur (`Reefact`) est la seule autorité qui merge une pull request et qui
accepte un ADR ; aucun agent ne merge, et un ADR est immuable une fois accepté (une
décision est réexaminée au moyen d'un ADR de remplacement).

## Décision

Chaque pull request est contrôlée au regard de la base d'ADR sous la forme d'une
recommandation consultative et non bloquante — automatiquement au sein d'une session de
codage Claude Code et sur déclenchement manuel pour les contributeurs sans Claude Code
— un agent rédigeant tout ADR comme `Proposé` et le mainteneur étant seul à l'accepter,
le remplacer ou le déprécier.

## Justification

Le contrôle a sa place au niveau de la pull request parce que c'est là que les décisions
entrent dans la base de code ; se demander « faut-il consigner ceci ? » pendant que le
contexte est encore frais, c'est ce dont la base d'ADR a besoin et qu'elle n'obtient pas
encore.

Il est consultatif, jamais bloquant, parce que les décisions qu'il fait émerger relèvent
du jugement, et non de conditions qu'une machine tranche : les invariants durs qui
*peuvent* être tranchés mécaniquement sont déjà verrouillés par les tests et la CI, et
conditionner un merge à l'opinion d'un modèle contredirait la règle selon laquelle le
mainteneur seul merge.

La voie automatique s'exécute au sein d'une session Claude Code parce que l'agent qui
s'y trouve est le mieux placé pour effectuer le contrôle — il détient déjà le diff, la
base d'ADR et la raison pour laquelle le changement a été fait, de sorte que le contrôle
ajoute peu au travail que la session accomplit déjà, avec plus de contexte que n'en
pourrait porter aucun appel séparé.

Le workflow manuel existe pour qu'un contributeur sans Claude Code reste couvert ; le
garder manuel — comme le workflow jumeau `changelog` — apporte cette couverture sans
transformer un verdict de LLM non reproductible en une barrière autonome sur chaque pull
request.

Un agent rédige et propose ; il ne fixe jamais le statut d'un ADR. Cela maintient le
mainteneur comme autorité de décision, en cohérence avec « aucun agent ne merge » et
avec l'immuabilité des ADR.

## Alternatives envisagées

### Un contrôle automatique par modèle sur chaque pull request dans la CI

Envisagé parce qu'il couvrirait chaque pull request — rédigée par un humain ou par un
agent, avec Claude Code ou non — de façon déterministe et sans que personne ait à penser
à le lancer.

Rejeté parce qu'un appel de modèle autonome sur chaque pull request entraîne un coût par
requête, introduit un contrôle non déterministe sur une surface quasi obligatoire, et
duplique — avec moins de contexte — ce qu'une session Claude Code accomplit déjà ; la
couverture qu'il apporterait est assurée à la place par le contrôle en session, complété
par le déclenchement manuel.

### Encoder l'invariant de chaque ADR dans un champ vérifiable par machine

Envisagé parce qu'un invariant net et déclaré rendrait la détection de conflits plus
fiable qu'un raisonnement sur de la prose.

Rejeté parce que les invariants durs qui se prêtent à une vérification mécanique sont
déjà garantis par les tests et la CI, qui le font mieux et de façon déterministe ; et
ajouter un champ de spécification à un ADR contredit le principe selon lequel un ADR est
un enregistrement de décision, non une spécification, ce qui éroderait la lisibilité par
un humain qui fait tout l'intérêt du format.

### S'en remettre à la mémoire, sans aucun contrôle

Envisagé parce que c'est le statu quo sans effort.

Rejeté parce que c'est précisément la faille que la base d'ADR existe pour combler : des
décisions embarquées dans une pull request restent alors non consignées, en remplacent
silencieusement une précédente, ou contredisent un ADR accepté.

## Conséquences

### Positives

* La question « y a-t-il une décision à consigner ? » est posée sur chaque pull request,
  pendant que le contexte qui a produit le changement est encore frais.
* La rédaction est peu coûteuse : l'agent en session dispose déjà de tout ce dont il a
  besoin.
* Rien ne bloque un merge ; le mainteneur conserve l'autorité exclusive sur le statut des
  ADR.
* Les contributeurs sans Claude Code disposent d'un recours de première classe.

### Négatives

* Le contrôle en session est fait au mieux : c'est une consigne que l'agent suit, non une
  barrière dure.
* La couverture d'une pull request ouverte sans Claude Code dépend du fait que quelqu'un
  déclenche le workflow.
* Le verdict consultatif est non déterministe — il utilise un alias de modèle flottant —
  et n'est donc pas reproductible.

### Risques

* Un agent saute le contrôle en session. Atténuation : l'essentiel figure dans `CLAUDE.md`
  (chargé de façon fiable), un élément de checklist est présent sur chaque pull request,
  et le workflow manuel constitue une voie indépendante.
* Les fausses alertes habituent l'équipe à ignorer le contrôle. Atténuation : le prompt
  est fortement orienté vers le silence sur les changements de routine.

## Actions de suivi

* Aucune qui soit bloquante. Si la consigne en session s'avère peu fiable en pratique,
  ajouter un hook Claude Code au périmètre étroit et non bloquant qui exécute le même
  contrôle — à ne pas construire de manière préventive.

## Références

* `AGENTS.md` — « Architecture decisions » (la procédure de l'agent).
* `CLAUDE.md` — l'essentiel par session, inséré en ligne.
* [référence du workflow `adr-check`](../workflows/adr-check.fr.md).
* [ADR-0001](0001-lock-the-analyzer-roslyn-floor.fr.md), [ADR-0002](0002-floor-the-tooling-runtime.fr.md)
  — des exemples des invariants durs que ce contrôle laisse délibérément aux tests et à
  la CI.
