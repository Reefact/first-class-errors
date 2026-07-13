# Politique de sécurité

🌍 **Langues:**  
🇬🇧 [English](../SECURITY.md) | 🇫🇷 Français (ce fichier)

## Versions prises en charge

Les mises à jour de sécurité sont fournies pour la dernière version stable de FirstClassErrors.

| Version                  | Prise en charge |
| ------------------------ | --------------- |
| Dernière version stable  | Oui             |
| Versions précédentes     | Non             |
| Versions préliminaires   | Au mieux        |

Les utilisateurs de versions non prises en charge doivent effectuer une mise à jour vers la dernière version stable avant de signaler une vulnérabilité.

## Signaler une vulnérabilité

Merci de ne pas signaler de vulnérabilités de sécurité via les issues, discussions, pull requests ou autres canaux publics de GitHub.

Signalez les vulnérabilités présumées de manière privée à l’aide du système d’avis de sécurité de GitHub :

[Ouvrir un rapport de vulnérabilité privé](https://github.com/Reefact/first-class-errors/security/advisories/new)

Merci d’inclure autant que possible les informations suivantes :

* Le package et la version affectés.
* L’environnement dans lequel la vulnérabilité a été observée.
* Une description de la vulnérabilité et de son impact potentiel.
* Les étapes nécessaires pour reproduire le problème.
* Une preuve de concept minimale, le cas échéant.
* Toute mesure d’atténuation ou solution de contournement connue.
* Si la vulnérabilité a déjà fait l’objet d’une divulgation publique.

N’incluez pas de secrets, de données personnelles, de jetons d’accès ni d’informations appartenant à des tiers dans le rapport.

## À quoi s’attendre

Après réception d’un rapport de vulnérabilité, les mainteneurs feront un effort raisonnable pour :

* Accuser réception dans un délai de 3 jours ouvrés.
* Fournir une évaluation initiale dans un délai de 7 jours ouvrés.
* Fournir une mise à jour de l’état au moins tous les 14 jours tant que la vulnérabilité n’est pas résolue.
* Coordonner un correctif et une divulgation publique dans un délai de 90 jours chaque fois que cela est raisonnablement possible.

Ces délais peuvent évoluer en fonction de la gravité, de la complexité, de l’exploitabilité et de la disponibilité d’un correctif sûr. Tout changement significatif du calendrier de divulgation sera discuté avec l’auteur du signalement.

Il est demandé à l’auteur du signalement de garder la vulnérabilité confidentielle jusqu’à ce qu’un correctif ou une mesure d’atténuation soit disponible et que la divulgation coordonnée soit terminée.

## Périmètre

Voici des exemples de problèmes susceptibles d’être qualifiés de vulnérabilités de sécurité :

* Accès non autorisé à des données ou à des fonctionnalités.
* Exécution de code arbitraire ou non intentionnelle.
* Contournement de l’authentification ou de l’autorisation.
* Exposition d’informations sensibles.
* Compromission de l’intégrité ou de la disponibilité des données.
* Vulnérabilités affectant le processus de build ou de publication du package.
* Vulnérabilités de la chaîne d’approvisionnement introduites par le projet.

Les éléments suivants ne sont généralement pas considérés comme des vulnérabilités de sécurité :

* Les bugs ordinaires sans impact sur la sécurité.
* Les demandes de fonctionnalités.
* Les erreurs de documentation.
* Les problèmes qui n’affectent que des versions non prises en charge et qui ne peuvent pas être reproduits sur une version prise en charge.
* Les vulnérabilités présentes dans des dépendances tierces qui n’affectent pas ce projet en pratique.

Les problèmes non liés à la sécurité doivent être signalés via le gestionnaire d’issues public de GitHub.

## Processus de divulgation

Une fois qu’une vulnérabilité a été confirmée, les mainteneurs peuvent créer un avis de sécurité GitHub privé (*GitHub Security Advisory*) pour coordonner le correctif.

Une fois qu’un correctif ou une mesure d’atténuation appropriée est disponible, les mainteneurs peuvent publier un avis contenant :

* Une description de la vulnérabilité et de son impact.
* Les versions affectées et corrigées.
* Les mesures d’atténuation ou solutions de contournement disponibles.
* Les instructions de mise à jour.
* Un identifiant CVE, le cas échéant.
* Le crédit accordé à l’auteur du signalement, sauf si l’anonymat a été demandé.

La divulgation publique ne devrait normalement intervenir qu’une fois que les utilisateurs ont accès à une version corrigée ou à une mesure d’atténuation efficace.

## Reconnaissance

Les chercheurs en sécurité qui signalent des vulnérabilités de bonne foi seront crédités dans l’avis publié lorsque cela est approprié, sauf s’ils préfèrent rester anonymes.

Ce projet n’exploite pas actuellement de programme de prime aux bugs (*bug bounty*) rémunéré.
