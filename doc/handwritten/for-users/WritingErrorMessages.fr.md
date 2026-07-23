# Écrire les messages d’une erreur

🌍 **Langues :**  
🇬🇧 [English](./WritingErrorMessages.en.md) | 🇫🇷 Français (ce fichier)

Une erreur porte trois messages à l’exécution, mais ils ne s’adressent qu’à deux publics :

- des messages publics pour les utilisateurs finaux ou les clients d’API ;
- un message de diagnostic interne pour les logs, le support et les développeurs.

Séparer ces publics empêche les fuites d’informations internes tout en conservant assez de détail pour analyser une occurrence concrète.

## Les trois messages en un coup d’œil

| Message | Obligatoire | Public | Rôle |
| --- | --- | --- | --- |
| `ShortMessage` | oui | externe | un résumé court et sûr |
| `DetailedMessage` | non | externe | une explication optionnelle et maîtrisée |
| `DiagnosticMessage` | oui | interne | les informations de diagnostic propres à cette occurrence |

Ils sont créés avec un builder étagé — un builder fluide dont les étapes exigent le message de diagnostic puis le message public court avant qu’une `Error` puisse exister :

```csharp
return DomainError.Create(
        Code.CurrencyMismatch,
        diagnosticMessage: $"Impossible de combiner des montants en {left.Currency} et {right.Currency} : {left} et {right}.")
    .WithPublicMessage(
        shortMessage: "Les montants utilisent des devises différentes.",
        detailedMessage: "Tous les montants de cette opération doivent utiliser la même devise.");
```

Le builder demande d’abord le message interne et ne peut produire une `Error` qu’après la fourniture du message public court obligatoire.

## `ShortMessage` : le résumé public sûr

Le message court indique à l’appelant ce qui s’est passé sans exposer de détail d’implémentation.

Il doit être :

- affichable directement sans risque ;
- compréhensible sans connaissance interne ;
- assez concis pour une notification UI ou le `title` d’une réponse RFC 9457 (« Problem Details », le format d’erreur standard des API HTTP) ;
- stable dans son sens, même si sa formulation évolue ou est localisée.

Bon :

> « Les montants utilisent des devises différentes. »

À éviter :

> « La validation des devises a échoué dans `Amount.AddOrThrow` pour la commande 42. »

Le second exemple expose un détail d’implémentation et un identifiant d’occurrence qui n’ont pas leur place dans un résumé public réutilisable.

## `DetailedMessage` : le détail public optionnel

Le message détaillé apporte une information supplémentaire lorsque l’application choisit explicitement de l’exposer.

Il peut expliquer :

- quelle contrainte publique n’a pas été respectée ;
- quelle correction est attendue de l’appelant ;
- quelle catégorie d’entrée sûre a provoqué le rejet.

Bon :

> « Tous les montants de cette opération doivent utiliser la même devise. »

À éviter :

> « Le montant en EUR provient de PostgreSQL tandis que le montant en USD vient du fournisseur X. »

Le message détaillé reste public. Il ne doit contenir ni secret, ni topologie interne, ni stack trace, ni détail de base de données, ni identifiant privé, ni instruction réservée au support.

Omettez-le lorsqu’il ne ferait que répéter le message court.

## `DiagnosticMessage` : le détail interne de l’occurrence

Le message de diagnostic est destiné aux développeurs, au support et aux logs. Il décrit l’occurrence concrète plutôt que la définition générale de l’erreur.

Il peut contenir des faits internes utiles comme :

- des identifiants nécessaires à la corrélation ;
- les valeurs fautives, lorsqu’elles sont sûres pour les logs internes ;
- le nom d’une dépendance ;
- un timeout ou un code de réponse ;
- l’état attendu et l’état constaté.

Bon :

> « Impossible de combiner 127,33 EUR et 84,10 USD dans l’opération ORDER-7392. »

Évitez les messages trop génériques :

> « Incohérence de devise. »

La documentation stable explique déjà ce que signifie une incohérence de devise. Le message de diagnostic doit montrer ce qui distingue cette occurrence.

`error.ToException()` utilise `DiagnosticMessage` comme `Message` de l’exception produite.

## Public ne signifie pas automatiquement sans risque

Avant de placer une donnée dans un message public, demandez-vous :

1. Peut-elle révéler une information personnelle, financière, de sécurité ou propre à un tenant ?
2. Peut-elle exposer un service interne, une base de données, un chemin de fichier, une classe ou un choix d’implémentation ?
3. La formulation aide-t-elle un attaquant à distinguer des états internes qui devraient rester indifférenciés ?
4. Le même texte serait-il toujours approprié dans une réponse API visible par un client ?

En cas de doute, gardez le détail dans `DiagnosticMessage` ou dans un `ErrorContext` structuré, sous réserve de la politique de logs de l’application.

## Les messages et `ErrorContext` ont des rôles différents

Ne transformez pas le message de diagnostic en dump de données non structuré.

Utilisez le message pour fournir un résumé lisible :

```text
La commande ORDER-7392 ne peut pas être débitée car le fournisseur de paiement a dépassé le délai.
```

Utilisez le contexte typé pour les champs que les logs, dashboards ou outils doivent interroger :

```csharp
configureContext: ctx => ctx
    .Add(ErrCtxKey.OrderId, orderId)
    .Add(ErrCtxKey.Provider, providerName)
```

Les deux se complètent :

- `DiagnosticMessage` aide une personne à lire l’événement ;
- `ErrorContext` aide les personnes et les outils à filtrer, corréler et agréger.

Voir [Contexte d’erreur](ErrorContext.fr.md) pour la conception des clés et les règles liées aux données sensibles.

## HTTP et RFC 9457

La RFC 9457 définit « Problem Details », le format standard de réponse d’erreur des API HTTP. Le modèle d’erreur du cœur est agnostique vis-à-vis d’HTTP. Une application peut mapper :

| Valeur FirstClassErrors | Champ RFC 9457 courant |
| --- | --- |
| `Code` stable représenté comme URI | `type` |
| `ShortMessage` | `title` |
| `DetailedMessage`, lorsqu’il est explicitement exposé | `detail` |

Exemple :

```json
{
  "type": "urn:problem:billing-api:amount-currency-mismatch",
  "title": "Les montants utilisent des devises différentes.",
  "detail": "Tous les montants de cette opération doivent utiliser la même devise.",
  "status": 422
}
```

La forme `urn:problem:{service}:{code}` du champ `type` est une convention choisie par votre application (le renderer du catalogue utilise la même) ; la RFC 9457 exige seulement une URI.

`DiagnosticMessage` n’est pas un champ de réponse par défaut. L’application reste responsable du choix de `status`, de la décision d’exposer ou non `detail` et de l’application de sa politique de sécurité.

## Localisation

Les messages publics peuvent être localisés puisqu’ils s’adressent à l’appelant. Lisez `ShortMessage` et `DetailedMessage` depuis des ressources selon la culture UI sélectionnée lorsque nécessaire.

Conservez `DiagnosticMessage` dans la langue de travail interne de l’équipe. Une langue interne cohérente facilite la recherche et la corrélation dans les logs et les investigations du support, quelle que soit la langue de l’appelant.

Voir [Internationalisation](Internationalisation.fr.md) pour le flux d’extraction et de rendu.

## Exemple incorrect puis amélioré

Séparation insuffisante :

```csharp
return DomainError.Create(
        Code.CurrencyMismatch,
        diagnosticMessage: "Opération invalide.")
    .WithPublicMessage(
        shortMessage: $"Commande {orderId} : les devises SQL {left.Currency}/{right.Currency} diffèrent.",
        detailedMessage: "Contacter l’équipe Paiement avec la trace SQL.");
```

Version améliorée :

```csharp
return DomainError.Create(
        Code.CurrencyMismatch,
        diagnosticMessage: $"La commande {orderId} ne peut pas combiner des montants en {left.Currency} et {right.Currency} : {left} et {right}.",
        configureContext: ctx => ctx.Add(ErrCtxKey.OrderId, orderId))
    .WithPublicMessage(
        shortMessage: "Les montants utilisent des devises différentes.",
        detailedMessage: "Tous les montants de cette opération doivent utiliser la même devise.");
```

La version améliorée fournit une information sûre à l’appelant, un détail propre à l’occurrence au support et des données interrogeables sous forme structurée.

## Checklist de revue

Vérifiez que :

- le message court est concis, public et sans détail d’implémentation ;
- le message détaillé ajoute une information publique utile au lieu de répéter le résumé ;
- aucun message public ne contient de donnée sensible ou réservée au support ;
- le message de diagnostic explique l’occurrence concrète ;
- les valeurs interrogeables sont également placées dans le contexte structuré lorsque pertinent ;
- le message de diagnostic n’est jamais exposé à l’extérieur par défaut ;
- les messages publics sont localisés lorsque l’application prend en charge plusieurs langues ;
- la formulation interne reste cohérente entre les cultures.

Pour le titre, la description, la règle, les diagnostics et les exemples stables, revenez à [Écrire la documentation d’une erreur](WritingErrorsGuide.fr.md).

---

<div align="center">
<a href="WritingErrorsGuide.fr.md">← Écrire la documentation d’une erreur</a> · <a href="README.fr.md#-documentation">↑ Table des matières</a> · <a href="UsagePatterns.fr.md">Cas d’usage →</a>
</div>

---