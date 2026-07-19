# Spécifications mainteneur

🌍 🇬🇧 [English](README.md) · 🇫🇷 Français (ce fichier)

Ces pages décrivent les **contrats techniques et opérationnels courants** qui
mettent en œuvre les décisions d'architecture. Contrairement à un ADR accepté,
une spécification est destinée à évoluer lorsque les mécanismes d'implémentation
changent sans que la décision ne change.

La séparation est volontaire :

* un [ADR](../adr/README.md) consigne **ce qui a été décidé et pourquoi** ;
* une spécification décrit **comment les décisions acceptées sont actuellement
  réalisées** ;
* les références de workflows décrivent la structure GitHub Actions exacte et
  ses permissions ;
* les guides utilisateur décrivent l'expérience publique plutôt que les
  mécanismes de maintenance.

Lorsqu'une évolution de spécification modifierait la décision elle-même, et non
sa seule implémentation, un nouvel ADR doit précéder la modification du contrat.

## Index

| Spécification | Décisions mises en œuvre |
|---|---|
| [Compatibilité des plateformes](platform-compatibility.fr.md) | ADR-0001, ADR-0002, ADR-0022 |
| [Processus de revue des ADR](adr-review-process.fr.md) | ADR-0004 |
| [Contrats du Request Binder](request-binder.fr.md) | ADR-0008, ADR-0012, ADR-0017, ADR-0018, ADR-0019, ADR-0021 |
| [Contrat de catalogue GenDoc](gendoc-catalog-contract.fr.md) | ADR-0009, ADR-0010 |
| [Contrats de génération de Dummies](dummies-generation.fr.md) | ADR-0011, ADR-0013, ADR-0015, ADR-0020 |

## Règles de maintenance

1. La page anglaise est canonique ; sa version française est mise à jour dans la
   même pull request.
2. Préférer les liens vers le code source et les références de workflows à la
   copie de longs extraits de code ou de YAML.
3. Décrire les contrats observables et la source de vérité de chaque valeur.
4. Ne modifier un ADR que lorsqu'une décision change, hors migration éditoriale
   unique autorisée par l'[ADR-0023](../adr/0023-extract-specifications-from-accepted-adrs.fr.md).
