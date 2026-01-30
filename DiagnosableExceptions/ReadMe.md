## Description
Cette bibliothèque .NET structure les erreurs comme des objets de domaine diagnosticables, afin d'améliorer
la compréhension métier, l'exploitation et le support en production. Elle propose un modèle cohérent d'exception,
de documentation et de diagnostic pour transformer une erreur technique en information exploitable.

### 1) L'erreur comme objet de domaine
`DiagnosableException` est la base de toutes les exceptions applicatives. Elle impose :
- un **code d'erreur stable** (ex. `PAYMENT.DECLINED`) pour classifier les erreurs,
- un **identifiant d'instance** pour corréler chaque occurrence,
- un **horodatage** d'apparition,
- un **message court** pour l'UX et un message détaillé pour la logique métier,
- une liste d'**inner exceptions** lorsque plusieurs causes se combinent.

Le résultat est une erreur qui devient un objet métier durable, traçable et comparable dans le temps.

### 2) Distinguer domaine et infrastructure
`DomainException` représente une violation de règle métier ou d'invariant : ces erreurs sont **non transitoires**
et doivent être interprétées dans le contexte du modèle.

`InfrastructureException` sépare les erreurs techniques (réseau, base de données, API externe) et ajoute
un **indicateur de transience** (`IsTransient`) pour décider si un retry est pertinent.

Cette séparation rend les décisions d'exploitation plus claires : on traite différemment une erreur métier
et une panne d'infrastructure.

### 3) Documentation intégrée et versionnée
La documentation des erreurs est intégrée au code via :
- un **modèle de documentation** structuré (`ErrorDocumentation`) avec titre, explication, règle métier,
  diagnostics et exemples,
- une **API fluide** (`DescribeError`) pour construire cette documentation de manière lisible et cohérente,
- un **rattachement explicite** de la documentation à la source d'erreur via les factories d'exceptions.

On obtient une documentation **proche de la source de vérité**, versionnée avec le code, et exploitable pour
le support, les PO et les développeurs.

### 4) Diagnostics actionnables
Chaque erreur peut inclure des diagnostics structurés via `ErrorDiagnostic` :
- **cause** (ex. bug système, données invalides),
- **type** (`ErrorCauseType`) pour orienter rapidement l'investigation,
- **correctif** ou action recommandée.

Cela réduit le temps de diagnostic et facilite l'escalade vers la bonne équipe.

### 5) Exemples concrets et reproductibles
La documentation peut inclure des exemples réels d'entrées ou de scénarios qui déclenchent une erreur. Ces exemples
enrichissent la documentation et améliorent la qualité des tests et des reproductions d'incident.

### 6) Lier les erreurs à leurs entités métier
`ProvidesErrorsForAttribute` relie une exception à un type métier (ex. `Order`, `Invoice`, `Temperature`),
renforçant le **langage ubiquitaire** et la lisibilité du domaine.

### 7) Alternative aux exceptions pour le contrôle de flux
Avec `TryOutcome<T>`, la bibliothèque fournit un modèle explicite de succès/échec qui évite de lever des exceptions
pour piloter le flux applicatif, tout en gardant la possibilité d'exposer une erreur structurée.

---
**En résumé** : cette bibliothèque transforme les erreurs en **faits métier documentés et actionnables**, ce qui
améliore la compréhension du domaine, accélère la résolution d'incidents et renforce la qualité du support.
