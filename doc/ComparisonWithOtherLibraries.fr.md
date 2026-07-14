# Comparaison avec les librairies de gestion d’erreurs

🌍 **Langues :**  
🇬🇧 [English](./ComparisonWithOtherLibraries.en.md) | 🇫🇷 Français (ce fichier)

> Comparaison revue le **14 juillet 2026** à partir des documentations publiques d’[ErrorOr](https://github.com/error-or/error-or) et de [FluentResults](https://github.com/altmann/FluentResults). Leurs API et leurs objectifs peuvent évoluer : vérifiez leur documentation actuelle avant un choix à long terme.

ErrorOr, FluentResults et FirstClassErrors permettent toutes de représenter explicitement un échec, mais elles optimisent des besoins principaux différents.

Cette page ne les classe pas. Elle montre une même situation à travers trois centres de gravité afin de choisir le modèle qui correspond réellement au système.

## Le scénario

Un fournisseur de paiement refuse une demande d’autorisation. L’application doit :

- retourner un échec sans faire planter le processus ;
- exposer un message sûr à l’appelant ;
- conserver un message de diagnostic interne ;
- garder un code stable pour les logs et le support ;
- éventuellement documenter la manière d’investiguer cet échec.

## ErrorOr : une union discriminée entre valeur et erreurs

ErrorOr place `ErrorOr<T>` au centre de son API. Ce type porte soit une valeur de succès, soit une ou plusieurs erreurs.

```csharp
private static readonly Error PaymentDeclined = Error.Failure(
    code: "PAYMENT_DECLINED",
    description: "The payment was declined.");

public ErrorOr<Receipt> Pay(Order order)
{
    if (provider.Declines(order))
    {
        return PaymentDeclined;
    }

    return new Receipt(order.Id);
}
```

L’appelant peut inspecter, faire un `Match`, un `Switch` ou composer le résultat. Les types d’erreurs intégrés et les métadonnées permettent de catégoriser l’échec et d’ajouter des informations propres à l’application.

Choisissez cette approche lorsque le besoin central est un flux ergonomique « valeur ou erreurs », notamment lorsque plusieurs erreurs de validation ou une intégration HTTP orientée résultat sont importantes.

## FluentResults : un résultat portant des raisons et des métadonnées

FluentResults place `Result` et `Result<T>` au centre de son API. Les échecs portent une ou plusieurs raisons, auxquelles peuvent être associés des métadonnées et des causes imbriquées.

```csharp
public Result<Receipt> Pay(Order order)
{
    if (provider.Declines(order))
    {
        return Result.Fail<Receipt>(
            new Error("The payment was declined.")
                .WithMetadata("Code", "PAYMENT_DECLINED"));
    }

    return Result.Ok(new Receipt(order.Id));
}
```

Le modèle de raisons est utile lorsqu’une application a besoin de chaînes riches de succès et d’échecs, de métadonnées et d’une gestion configurable des résultats.

Choisissez cette approche lorsque le résultat et son graphe de raisons sont l’abstraction principale que vous souhaitez enrichir et composer.

## FirstClassErrors : un modèle d’erreur avec plusieurs transports

FirstClassErrors place l’erreur elle-même au centre. `Outcome<T>` est un transport possible ; `ToException()` en est un autre.

```csharp
internal static DomainError PaymentDeclined(
    string providerCode,
    Guid paymentId)
{
    return DomainError.Create(
            Code.PaymentDeclined,
            diagnosticMessage:
                $"Provider refused payment {paymentId} with code {providerCode}.",
            configureContext: context =>
                context.Add(ContextKey.PaymentId, paymentId))
        .WithPublicMessage(
            shortMessage: "The payment was declined.",
            detailedMessage:
                "Use another payment method or contact your bank.");
}
```

```csharp
public Outcome<Receipt> Pay(Order order)
{
    if (provider.Declines(order, out string providerCode))
    {
        return Outcome<Receipt>.Failure(
            PaymentError.PaymentDeclined(providerCode, order.PaymentId));
    }

    return Outcome<Receipt>.Success(new Receipt(order.Id));
}
```

La même factory peut aussi alimenter un flux par exception :

```csharp
throw PaymentError
    .PaymentDeclined(providerCode, paymentId)
    .ToException();
```

L’erreur peut en outre être liée à une documentation structurée décrivant son titre, sa règle, ses causes possibles, ses pistes d’analyse et ses exemples. Le catalogue généré et son workflow de versionnage font partie de l’usage prévu de la librairie.

Choisissez cette approche lorsque la définition de l’erreur doit rester stable, documentée, diagnosticable et indépendante du fait qu’un appelant la retourne ou la lève.

## Qu’est-ce qui change entre les approches ?

| Préoccupation | ErrorOr | FluentResults | FirstClassErrors |
| --- | --- | --- | --- |
| Abstraction principale | valeur ou erreurs | résultat avec raisons | erreur structurée |
| Flux d’échec sous forme de valeur | central | central | disponible via `Outcome` |
| Erreurs ou raisons multiples | intégré | intégré | modélisé via les erreurs internes ou une agrégation applicative |
| Métadonnées / faits propres à l’occurrence | métadonnées | métadonnées | `ErrorContext` typé |
| Séparation dédiée entre messages publics et internes | définie par l’application | définie par l’application | explicite dans le modèle central |
| Transport de la même erreur sous forme d’exception typée | pas le modèle principal | pas le modèle principal | intégré via `ToException()` |
| Taxonomie domaine / infrastructure / ports | définie par l’application | définie par l’application | intégrée |
| Transience et direction de l’interaction | définies par l’application | définies par l’application | intégrées pour les erreurs d’infrastructure |
| Documentation humaine générée | hors du périmètre principal de la librairie | hors du périmètre principal de la librairie | intégrée |
| Vérification de compatibilité du catalogue | hors du périmètre principal de la librairie | hors du périmètre principal de la librairie | intégrée |

« Défini par l’application » ou « hors du périmètre principal » ne signifie pas impossible. Cela signifie que la préoccupation n’est pas l’abstraction centrale de la librairie et qu’elle peut être prise en charge par des conventions applicatives ou un outillage externe.

## Guide de décision

Choisissez **ErrorOr** lorsque :

- vous voulez principalement une union concise entre `T` et une ou plusieurs erreurs ;
- les erreurs de validation multiples sont courantes ;
- le matching et la composition fonctionnelle sont le style d’interaction principal ;
- une représentation légère de l’erreur suffit.

Choisissez **FluentResults** lorsque :

- vous souhaitez représenter des raisons de succès comme d’échec ;
- les chaînes de raisons imbriquées et les métadonnées sont centrales ;
- le graphe de résultat est lui-même le modèle que vous voulez enrichir et composer.

Choisissez **FirstClassErrors** lorsque :

- les erreurs sont des concepts durables utilisés par les développeurs, le support, l’exploitation ou les clients ;
- les messages publics et de diagnostic doivent avoir des audiences explicites ;
- une même erreur doit circuler comme donnée, outcome ou exception ;
- les échecs métier et infrastructurels doivent porter un sens opérationnel différent ;
- la documentation générée et les contrôles de compatibilité font partie du besoin.

## Une combinaison peut aussi être valable

Ces choix ne sont pas toujours exclusifs. Une application peut utiliser une librairie de résultat généraliste à certaines frontières tout en conservant un catalogue d’erreurs documenté séparé.

Avant de combiner les modèles, décidez quel type possède l’identité stable de l’erreur. Dupliquer les codes, les messages, les métadonnées et les mappings entre deux modèles concurrents crée généralement plus de travail que cela n’en économise.

## Questions à poser avant de choisir

1. Le problème principal est-il le **contrôle du flux**, la **composition des raisons** ou la **connaissance partagée des erreurs** ?
2. Une même définition d’erreur doit-elle supporter le retour et l’exception ?
3. Qui consomme le modèle : seulement le code, ou également le support et l’exploitation ?
4. Les codes et les clés de contexte sont-ils considérés comme un contrat versionné ?
5. La documentation générée est-elle une exigence ou une préoccupation externe ?
6. L’application a-t-elle besoin d’une sémantique intégrée pour le domaine et l’infrastructure ?
7. Combien de conventions êtes-vous prêt à construire autour d’un type résultat plus petit ?

Le meilleur choix est le plus petit modèle qui couvre les besoins réels sans obliger l’application à reconstruire ailleurs les sémantiques manquantes.

---

<div align="center">
<a href="Internationalisation.fr.md">← Internationalisation</a> · <a href="DocumentationMap.fr.md">Carte de la documentation</a> · <a href="FAQ.fr.md">FAQ →</a>
</div>

---