# Workflow `sonar-gate`

🌍 🇫🇷 Français (ce fichier) · 🇬🇧 [English](sonar-gate.en.md)

> Documentation mainteneur — fait partie de la [référence des workflows](README.fr.md).
> Ne fait pas partie de la documentation utilisateur sous `doc/`.

**Fichier du workflow :** [`.github/workflows/sonar-gate.yml`](../../../../.github/workflows/sonar-gate.yml)
**Script :** [`tools/sonar-profile/check-gate.sh`](../../../../tools/sonar-profile/check-gate.sh)

## À quoi il sert

Il lit chaque nuit le **verdict du Quality Gate SonarCloud** et échoue quand il n'est pas vert.

Jusqu'ici, le gate était calculé par SonarCloud et **appliqué par rien**.
`dotnet-sonarscanner end` téléverse l'analyse et rend la main ; il n'attend pas le gate et ne le
lit pas, si bien que le job [`sonar`](sonar.fr.md) est vert dès que le téléversement réussit,
quel que soit le verdict. Aucune *check* GitHub ne portait ce verdict non plus — sur les 28
*checks* d'une *pull request* récente, la seule Sonar était le job d'analyse du dépôt lui-même.
Deux constats typés VULNERABILITY ont atteint `main` derrière ce workflow durablement vert.

## Pourquoi ne pas faire attendre le scanner

`sonar.qualitygate.wait=true` ressemble à la correction évidente et n'est pas la bonne *telle que
posée*, parce qu'elle fond deux décisions : `sonar` doit-elle être une *check* **requise**, et le
verdict du gate doit-il être **lu**.

En l'état, `sonar` est requise et appelle déjà SonarCloud : une panne bloque donc déjà tous les
merges alors qu'un gate rouge ne les bloque pas. Ajouter l'attente étend cette dépendance au lieu
de la supprimer : le mode de défaillance devient « personne ne peut merger parce qu'un SaaS est
tombé ».

Lire le verdict depuis un job **planifié** sépare les deux. Le verdict est appliqué, et une panne
coûte un nocturne rouge plutôt qu'un dépôt gelé. C'est la combinaison qu'ADR-0062 a consignée
comme jamais évaluée pour elle-même — ce workflow, c'est elle.

## Pourquoi ce n'est pas redondant avec le build

`build/sonar-profile.globalconfig` applique les règles C# que le paquet `SonarAnalyzer`
implémente. C'est un **sous-ensemble strict** de ce que mesure le gate, et l'écart n'est pas
théorique :

* **Les règles d'exécution symbolique que le paquet n'exécute pas.** Mesuré : une violation de
  `S2583` était présente dans ce dépôt avec la règle appliquée en `warning` dans la configuration
  générée, et le build local n'a **rien** signalé. Le moteur de SonarCloud l'a trouvée ; le paquet
  ne peut pas.
* **Toutes les familles non-C#** — `githubactions`, `shell`, `secrets`, `xml`, `json`, `yaml`.
  Sonar analyse cinq langages ici ; le C# est 85 % des lignes et aucun des autres n'est couvert
  par un analyseur Roslyn.
* **Couverture, duplication et revue des *hotspots***, auxquelles aucun analyseur ne peut répondre.

Les deux constats qui ont mis ce gate au rouge la dernière fois relevaient de ces classes : un bug
d'exécution symbolique et une vulnérabilité `githubactions`. Le durcissement du build et ce
contrôle sont complémentaires, non alternatifs — et c'est l'absence de ce contrôle qui les a
laissés passer inaperçus.

## Quand il s'exécute

- **Chaque nuit**, à 04h11 UTC — dans le creux laissé par les jobs hebdomadaires, qui tournent le
  lundi entre 03h00 et 06h30.
- À la demande via **`workflow_dispatch`**.

Chaque nuit, contrairement au contrôle de dérive hebdomadaire
[`sonar-profile`](sonar-profile.fr.md) : un profil qualité bouge à la cadence de livraison d'un
éditeur, mais le gate bouge à **chaque merge**.

Il ne s'exécute délibérément **pas** sur les *pull requests*. C'est tout le propos : le verdict
est lu et signalé, et aucun merge n'attend jamais un tiers.

## Comment il s'exécute

Un job, `Quality gate verdict` : checkout, puis `tools/sonar-profile/check-gate.sh`, qui appelle
`/api/qualitygates/project_status` et sort en code non nul quand le statut n'est pas `OK`, en
listant les conditions en échec.

Les notes sont traduites du `1..5` brut renvoyé par l'API vers le `A..E` qu'affiche le tableau de
bord — « C » dit au lecteur où il en est, « 3 » non — et le message précise ce qu'une note pire
que A signifie réellement : la fiabilité, c'est un **Bug** ; la sécurité, une **Vulnérabilité** ;
la maintenabilité, un **Code Smell**.

## Permissions & sécurité

`contents: read`, déclaré **sur le job** (Sonar `githubactions:S8264`).

Le projet est public : aucun secret n'est nécessaire. `SONAR_TOKEN` est transmis depuis le même
secret que `sonar.yml` pour que le contrôle survive au jour où le projet cessera d'être public ;
un secret absent est une chaîne vide, que le script traite comme « non authentifié ». Chaque
requête refuse le non-HTTPS à l'appel initial **et** sur les redirections, ce qui importe parce
que la branche authentifiée envoie le token.

## À manier avec précaution

- **Il ne bloque jamais un merge, et c'est délibéré.** C'est une alarme permanente, pas un
  garde-fou. Un gate rouge produira un run rouge chaque nuit jusqu'à ce que quelqu'un agisse :
  c'est le comportement voulu, et aussi la façon dont ce contrôle peut être coupé jusqu'à
  l'inutilité.
- **Il lit le projet, pas la branche.** Le verdict reflète la dernière analyse de `main` : un
  correctif sur une branche non mergée ne le verdit pas, seul le merge le fait. Compter un
  décalage d'une analyse après chaque correctif.
- **Une condition de note nomme une classe, pas un nombre.** `new_reliability_rating: C` signifie
  « au moins un bug majeur dans la fenêtre de code neuf » ; elle ne dit pas combien. Suivre le
  lien que le script imprime.
- **Il partage son répertoire de script avec `sonar-profile` mais répond à une autre question.**
  Une dérive veut dire « la liste de règles est périmée, régénère-la » ; un gate rouge veut dire
  « quelque chose est passé, va voir ». Actions différentes, d'où deux workflows sur deux
  cadences.

## Voir aussi

- [`sonar`](sonar.fr.md) — produit l'analyse que celui-ci lit. Il téléverse ; il n'a jamais
  appliqué.
- [`sonar-profile`](sonar-profile.fr.md) — le contrôle hebdomadaire que la liste de règles
  committée correspond encore au profil du serveur.
- [`ci`](ci.fr.md) — là où le ratchet de warnings applique les règles C# que le build *peut* voir.
