# Logging et intégration opérationnelle

🌍 **Langues :**  
🇬🇧 [English](./LoggingIntegration.en.md) | 🇫🇷 Français (ce fichier)

FirstClassErrors ne remplace pas le logging structuré. Il ajoute une information sémantique stable au contexte technique et d’exécution déjà présent dans les logs.

Ce guide explique quoi logger, comment préserver les erreurs internes et comment relier une occurrence au catalogue généré.

Pour la génération et la publication du catalogue, voir [Générer et publier le catalogue d’erreurs](OperationalIntegration.fr.md).

## Trois couches complémentaires

| Couche | Répond à |
| --- | --- |
| propriétés structurées du log | que s’est-il passé techniquement ? |
| scopes (portées de logging ambiantes, comme `ILogger.BeginScope`) et identifiants de corrélation | dans quelle requête, quel message ou quel workflow ? |
| FirstClassErrors | que signifie cette défaillance reconnue ? |

Un événement de production utile combine les trois au lieu de tout aplatir dans un seul message.

## Propriétés minimales à logger

Pour chaque `Error`, capturez au moins :

| Propriété | Rôle |
| --- | --- |
| `Code` | clé stable de regroupement et d’alerte |
| `InstanceId` | identité de cette occurrence précise |
| `OccurredAt` | instant UTC de création de l’erreur |
| type runtime de l’erreur | domaine, infrastructure, port primaire ou port secondaire |
| `DiagnosticMessage` | explication interne pour les développeurs et le support |
| `Context` | faits structurés propres à l’occurrence |
| erreurs internes | profondeur causale ou agrégée du diagnostic |

Pour une `InfrastructureError`, capturez également :

- `Transience` ;
- `InteractionDirection`.

N’exposez pas `DiagnosticMessage` ou un contexte non filtré dans une réponse API publique sous prétexte qu’ils sont présents dans les logs.

## Exemple d’événement structuré

Un événement sérialisé peut ressembler à ceci :

```json
{
  "level": "Error",
  "message": "Payment authorization failed",
  "traceId": "91c1d1bda3be4d0c",
  "service": "checkout-api",
  "deploymentVersion": "2.4.0",
  "error": {
    "code": "PAYMENT_PROVIDER_UNAVAILABLE",
    "instanceId": "be1226ef-8464-4f88-9dca-9ab1f74da824",
    "occurredAt": "2026-07-14T13:40:52.184Z",
    "type": "SecondaryPortError",
    "direction": "Outgoing",
    "transience": "Transient",
    "diagnosticMessage": "The payment provider timed out after 5 seconds.",
    "context": {
      "PaymentId": "f646943f-eec1-46bb-8989-32a97cba60fa",
      "Provider": "ExamplePay"
    },
    "innerErrors": []
  }
}
```

Le schéma JSON exact appartient à l’application ou à l’adapter de logging. L’important est de conserver des champs structurés et requêtables.

## Logger l’`Error`, pas seulement `Exception.Message`

Une `DiagnosableException` — le type d’exception produit par `error.ToException()` — expose son modèle sémantique via `.Error` :

```csharp
catch (DiagnosableException exception) {
    Error error = exception.Error;

    logger.LogError(
        exception,
        "Operation failed with {ErrorCode} ({ErrorInstanceId})",
        error.Code,
        error.InstanceId);
}
```

Passer l’exception préserve la stack trace. Logger des propriétés nommées préserve les clés sémantiques stables.

Un formatter, enricher, filtre ou middleware peut ensuite sérialiser les autres propriétés de l’`Error` de manière cohérente.

## Préserver le contexte comme données structurées

Ne concaténez pas le contexte dans une seule chaîne de diagnostic :

```text
Payment f646... failed for provider ExamplePay
```

Préférez des champs distincts, filtrables et agrégeables :

```json
{
  "PaymentId": "f646943f-eec1-46bb-8989-32a97cba60fa",
  "Provider": "ExamplePay"
}
```

Les clés de contexte constituent un vocabulaire opérationnel. Gardez leurs noms stables et vérifiez que les valeurs sont sûres pour la destination de logs visée.

Voir [Contexte d’erreur](ErrorContext.fr.md) pour la conception des clés et les règles de sécurité des données.

## Parcourir `InnerErrors`

`DiagnosableException` n’utilise pas `Exception.InnerException` pour l’arbre diagnostique FirstClassErrors. Les causes et erreurs agrégées vivent dans :

```csharp
exception.Error.InnerErrors
```

Un adapter de logging doit parcourir explicitement cette collection :

```csharp
static object ToLogModel(Error error) {
    return new {
        Code = error.Code.ToString(),
        error.InstanceId,
        error.OccurredAt,
        Type = error.GetType().Name,
        error.DiagnosticMessage,
        Context = error.Context,
        InnerErrors = error.InnerErrors.Select(ToLogModel).ToArray()
    };
}
```

Cet exemple montre la forme récursive ; adaptez la sérialisation du contexte et les champs propres aux erreurs d’infrastructure à l’application.

Si seule l’erreur externe est loggée, la cause la plus utile peut disparaître de l’analyse opérationnelle.

## Conserver le sens externe et interne

Prenons une requête entrante rejetée parce qu’une valeur métier ne peut pas être construite :

```text
PrimaryPortError: REQUEST_REJECTED
└── DomainError: AMOUNT_NEGATIVE
```

L’erreur externe explique ce qui s’est passé à la frontière. L’erreur interne explique la règle métier violée. Logger les deux conserve toute l’histoire diagnostique.

N’aplatissez pas l’arbre en un code synthétique unique et ne remplacez pas l’erreur externe par la cause la plus profonde.

## Corréler l’occurrence

`InstanceId` identifie une occurrence d’erreur. Il complète, sans les remplacer :

- les identifiants de trace et de span ;
- les identifiants de requête ou de message ;
- les identifiants métier placés dans `ErrorContext` ;
- la version de déploiement.

Un parcours d’investigation utile est :

```text
alerte → traceId → InstanceId de l’erreur → contexte métier → entrée du catalogue
```

Incluez la version de déploiement afin que le support ouvre le catalogue correspondant au code exécuté, et non la documentation la plus récente par erreur.

## Relier les logs au catalogue

Un événement peut inclure une URL de documentation dérivée du code et de l’emplacement du catalogue déployé :

```text
https://docs.mycompany/errors/releases/2.4.0/payment-provider-unavailable
```

L’URL peut être publiée comme propriété structurée telle que `error.documentationUrl`.

Lorsque l’objet exception est utilisé par un outil qui comprend `Exception.HelpLink`, l’application peut aussi le renseigner avant le logging ou la relance :

```csharp
exception.HelpLink = documentationUrl;
```

Le catalogue généré reste la source d’explication ; le log conserve le lien de navigation, pas une copie dupliquée de la documentation.

## Logger un outcome sans lever

Un échec d’`Outcome` peut ne jamais devenir une exception. Loggez directement son `Error` lorsque l’application décide que l’échec doit apparaître dans les logs opérationnels :

```csharp
Outcome<Receipt> outcome = checkout.Pay(order);

if (outcome.IsFailure) {
    logger.LogWarning(
        "Checkout failed with {ErrorCode} ({ErrorInstanceId})",
        outcome.Error!.Code,
        outcome.Error.InstanceId);
}
```

Ne levez pas une exception uniquement pour que le framework de logging voie l’erreur. Un sérialiseur partagé peut gérer à la fois `Error` et `DiagnosableException.Error`.

## Choisir le niveau selon l’impact opérationnel

La présence d’une `Error` n’implique pas automatiquement un log de niveau erreur.

| Situation | Traitement possible |
| --- | --- |
| saisie utilisateur rejetée comme prévu | information ou warning selon la politique |
| élément rejeté dans un batch normal | warning ou métrique, éventuellement échantillonné |
| dépendance sortante en échec transitoire | warning ou erreur selon les retries et le résultat final |
| retries épuisés provoquant l’échec de la requête | erreur |
| erreur métier déjà retournée à l’appelant et totalement attendue | éventuellement aucun log à cette couche |

Utilisez `Transience`, `InteractionDirection`, le résultat applicatif et la fréquence pour guider la politique. Évitez les logs dupliqués à chaque couche de la stack.

## Protéger les données sensibles

Avant de logger le contexte ou les messages de diagnostic, vérifiez qu’ils ne contiennent pas :

- secrets, mots de passe, tokens ou credentials ;
- données personnelles non maîtrisées ;
- corps complets de requête ou de réponse ;
- données de paiement ou autres valeurs réglementées ;
- collections ou fichiers non bornés.

Le public interne autorise davantage de détail technique ; il ne supprime pas les obligations de sécurité, de confidentialité, de rétention ou de contrôle d’accès.

## Checklist de revue

Avant d’approuver une intégration de logging, vérifiez que :

- la stack trace de l’exception et l’`Error` structurée sont toutes deux préservées lorsqu’une exception existe ;
- code, instance id, instant, type, message de diagnostic et contexte sont requêtables ;
- la direction et la transience sont loggées pour l’infrastructure ;
- `InnerErrors` est parcouru récursivement ;
- les échecs d’outcome peuvent être loggés sans fabriquer d’exception ;
- les identifiants de trace, métier, occurrence et déploiement restent distincts ;
- le niveau de log reflète l’impact opérationnel et non la simple présence d’une erreur ;
- les données sensibles et non bornées sont exclues ;
- les liens documentaires ciblent la version du catalogue correspondant au déploiement ;
- la même erreur n’est pas loggée à répétition par chaque couche.

---

<div align="center">
<a href="OperationalIntegration.fr.md">← Générer et publier le catalogue</a> · <a href="README.fr.md#-étapes-suivantes">↑ Table des matières</a> · <a href="CatalogVersioning.fr.md">Versionnage du catalogue →</a>
</div>

---