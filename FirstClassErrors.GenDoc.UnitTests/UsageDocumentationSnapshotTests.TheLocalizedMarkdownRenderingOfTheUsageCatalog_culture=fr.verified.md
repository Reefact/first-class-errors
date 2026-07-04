# Catalogue des erreurs

## Table des matières

- [Erreurs Amount](#src-amount)
  - [Incohérence de devise entre montants](#err-amount-currency-mismatch)
- [Erreurs BankTransactionFileValidator](#src-bank-transaction-file-validator)
  - [Date de transaction hors de la période du relevé](#err-bank-transaction-file-date-out-of-statement-period)
  - [Incohérence du montant total du relevé](#err-bank-transaction-file-statement-total-amount-mismatch)
- [Erreurs ExchangeRateProvider](#src-exchange-rate-provider)
  - [Service de taux de change indisponible](#err-exchange-rate-service-unavailable)
  - [Paire de devises non prise en charge](#err-unsupported-currency-pair)
- [Erreurs StatementUploadEndpoint](#src-statement-upload-endpoint)
  - [Charge utile de relevé mal formée](#err-malformed-statement-payload)
  - [Téléversement de relevé limité en débit](#err-statement-upload-rate-limited)
- [Erreurs MoneyTransfer](#src-money-transfer)
  - [Montant de virement non positif](#err-money-transfer-amount-not-positive)
  - [Virement invalide](#err-money-transfer-invalid)
- [Erreurs Temperature](#src-temperature)
  - [Temperature below absolute zero](#err-temperature-below-absolute-zero)

<a id="src-amount"></a>

## Erreurs Amount

Erreurs levées lors d'opérations combinant des valeurs monétaires Amount.

<a id="err-amount-currency-mismatch"></a>

### Incohérence de devise entre montants

- **Code :** `AMOUNT_CURRENCY_MISMATCH`
- **Source :** `Amount`

Cette erreur se produit lorsqu'on tente d'utiliser ensemble plusieurs montants dans une opération alors qu'ils sont exprimés dans des devises différentes.

> **Règle métier :** Toutes les opérations monétaires doivent porter sur des montants exprimés dans la même devise.

#### Diagnostics

- **Des montants ont été utilisés dans une opération monétaire sans avoir été convertis dans la même devise.** — _origine :_ Internal — Vérifiez si tous les montants concernés par l'opération ont été convertis dans une devise commune avant d'être utilisés ensemble.
- **Des montants censés être exprimés dans la même devise ont été fournis avec des devises différentes.** — _origine :_ InternalOrExternal — Vérifiez les devises associées à chaque montant et confirmez si une devise commune était attendue pour cette opération.

#### Exemples

- Échec de l'opération monétaire car les montants concernés sont exprimés dans des devises différentes : 127.33 EUR et 57689 USD. _(Incohérence de devise)_

<a id="src-bank-transaction-file-validator"></a>

## Erreurs BankTransactionFileValidator

Erreurs levées lors de la validation d'un relevé bancaire téléversé au regard de ses métadonnées déclarées (période et totaux du relevé).

<a id="err-bank-transaction-file-date-out-of-statement-period"></a>

### Date de transaction hors de la période du relevé

- **Code :** `BANK_TRANSACTION_FILE_DATE_OUT_OF_STATEMENT_PERIOD`
- **Source :** `BankTransactionFileValidator`

Cette erreur se produit lorsqu'on tente de valider un relevé bancaire contenant une ou plusieurs transactions datées en dehors de la période du relevé.

> **Règle métier :** Toutes les transactions doivent avoir lieu au cours de la période du relevé.

#### Diagnostics

- **La date de transaction fournie dans le fichier du relevé est incorrecte ou incohérente avec la date réelle de la transaction.** — _origine :_ External — Vérifiez la date de transaction présente dans le fichier d'entrée et confirmez sa cohérence avec la chronologie réelle de la transaction.
- **La période du relevé définie dans le fichier ne correspond pas à la période réellement couverte par les transactions.** — _origine :_ External — Vérifiez si les dates de début et de fin du relevé dans le fichier concordent avec la période couverte par les transactions.
- **La transaction a été enregistrée après la génération du relevé mais a été incluse par erreur dans le fichier.** — _origine :_ InternalOrExternal — Déterminez si des transactions enregistrées tardivement ont été incluses dans le processus de génération du relevé.
- **Une erreur de traitement interne a décalé la date de transaction lors de la transformation ou de l'import des données.** — _origine :_ Internal — Examinez les étapes d'import et de transformation des données pour confirmer que les dates de transaction sont conservées sans altération.

#### Exemples

- La transaction datée du 2024-02-02 est en dehors de la période du relevé [2024-01-05;2024-01-31]. _(La date de transaction est en dehors de la période du relevé.)_

#### Contexte

| Clé | Type | Description | Exemples de valeurs |
| --- | --- | --- | --- |
| `TRANSACTION_DATE` | `System.DateOnly` | La date de la transaction en cours de traitement. | `02/02/2024` |

<a id="err-bank-transaction-file-statement-total-amount-mismatch"></a>

### Incohérence du montant total du relevé

- **Code :** `BANK_TRANSACTION_FILE_STATEMENT_TOTAL_AMOUNT_MISMATCH`
- **Source :** `BankTransactionFileValidator`

Cette erreur se produit lorsqu'on tente de valider un relevé bancaire dont le montant total déclaré ne correspond pas à la somme des montants de chaque transaction.

> **Règle métier :** Le montant total du relevé doit être égal à la somme de tous les montants de transaction inclus dans le relevé.

#### Diagnostics

- **Le montant total déclaré dans le fichier du relevé ne correspond pas à la somme des montants de chaque transaction.** — _origine :_ External — Vérifiez le montant total déclaré dans le fichier et comparez-le à la somme de tous les montants de transaction.
- **Une ou plusieurs transactions sont manquantes ou dupliquées dans le fichier du relevé.** — _origine :_ External — Vérifiez si toutes les transactions attendues sont présentes exactement une fois dans le fichier du relevé.
- **Une erreur d'arrondi ou de précision s'est produite lors du calcul du montant total du relevé.** — _origine :_ InternalOrExternal — Examinez la manière dont les règles d'arrondi et de précision ont été appliquées lors du calcul du total du relevé.
- **Une erreur de traitement interne a modifié les montants de transaction lors de l'analyse ou de la transformation du fichier.** — _origine :_ Internal — Inspectez les étapes d'analyse et de transformation du fichier pour confirmer que les montants de transaction restent inchangés.

#### Exemples

- Le montant total déclaré du relevé (1250 EUR) ne correspond pas au montant total calculé à partir des transactions (1249.5 EUR). _(Incohérence du montant total du relevé.)_

<a id="src-exchange-rate-provider"></a>

## Erreurs ExchangeRateProvider

Erreurs levées lors de l'appel au fournisseur de taux de change externe (un adaptateur sortant, secondary port).

<a id="err-exchange-rate-service-unavailable"></a>

### Service de taux de change indisponible

- **Code :** `EXCHANGE_RATE_SERVICE_UNAVAILABLE`
- **Source :** `ExchangeRateProvider`

Cette erreur se produit lorsque le fournisseur de taux de change externe est injoignable (un dépassement de délai, une réinitialisation de connexion ou une réponse 5xx). Elle est transitoire : l'appel peut être réessayé.

> **Règle métier :** La conversion de devises dépend d'un fournisseur de taux de change joignable.

#### Diagnostics

- **Le fournisseur a dépassé le délai ou a renvoyé une erreur serveur.** — _origine :_ External — Vérifiez l'état de santé du fournisseur et réessayez l'appel, idéalement avec une temporisation.
- **Le chemin réseau sortant vers le fournisseur est perturbé.** — _origine :_ InternalOrExternal — Vérifiez la connectivité sortante ainsi que tout proxy ou pare-feu entre le service et le fournisseur.

#### Exemples

- Le fournisseur de taux de change « acme-fx » est indisponible (corrélation 22222222-2222-2222-2222-222222222222). _(Service de taux de change indisponible.)_

#### Contexte

| Clé | Type | Description | Exemples de valeurs |
| --- | --- | --- | --- |
| `PROVIDER` | `System.String` | Le fournisseur externe qui a été appelé. | `acme-fx` |
| `CORRELATION_ID` | `System.Guid` | L'identifiant de corrélation de l'appel sortant. | `22222222-2222-2222-2222-222222222222` |

<a id="err-unsupported-currency-pair"></a>

### Paire de devises non prise en charge

- **Code :** `UNSUPPORTED_CURRENCY_PAIR`
- **Source :** `ExchangeRateProvider`

Cette erreur se produit lorsque le fournisseur de taux de change ne cote pas de taux pour la paire de devises source/cible demandée.

> **Règle métier :** Une conversion de devises ne peut être effectuée que pour une paire cotée par le fournisseur.

#### Diagnostics

- **La paire de devises demandée n'est pas proposée par le fournisseur.** — _origine :_ External — Confirmez que le fournisseur prend en charge les devises source et cible avant de demander une conversion.

#### Exemples

- Le fournisseur de taux de change ne cote pas la paire de devises EUR vers USD. _(Paire de devises non prise en charge.)_

#### Contexte

| Clé | Type | Description | Exemples de valeurs |
| --- | --- | --- | --- |
| `FROM_CURRENCY` | `FirstClassErrors.Usage.Model.Currency` | La devise source de la conversion. | `EUR` |
| `TO_CURRENCY` | `FirstClassErrors.Usage.Model.Currency` | La devise cible de la conversion. | `USD` |

<a id="src-statement-upload-endpoint"></a>

## Erreurs StatementUploadEndpoint

Erreurs levées par le point d'entrée HTTP qui ingère les relevés bancaires téléversés (un adaptateur entrant, primary port).

<a id="err-malformed-statement-payload"></a>

### Charge utile de relevé mal formée

- **Code :** `MALFORMED_STATEMENT_PAYLOAD`
- **Source :** `StatementUploadEndpoint`

Cette erreur se produit lorsque le point d'entrée de téléversement de relevé reçoit une requête dont le corps omet un champ obligatoire ou contient une valeur invalide.

> **Règle métier :** Une requête de relevé téléversée doit porter tous les champs obligatoires avec une valeur valide.

#### Diagnostics

- **Le client a envoyé un corps de requête incomplet ou mal formé.** — _origine :_ External — Examinez le champ nommé dans le contexte et confirmez que le client l'envoie avec une valeur valide.

#### Exemples

- La requête de téléversement de relevé 11111111-1111-1111-1111-111111111111 est mal formée : le champ « statementPeriod » est manquant ou invalide. _(Charge utile de relevé mal formée.)_

#### Contexte

| Clé | Type | Description | Exemples de valeurs |
| --- | --- | --- | --- |
| `REQUEST_ID` | `System.Guid` | L'identifiant de la requête entrante. | `11111111-1111-1111-1111-111111111111` |
| `FIELD` | `System.String` | Le champ de la requête qui a échoué à la validation. | `statementPeriod` |

<a id="err-statement-upload-rate-limited"></a>

### Téléversement de relevé limité en débit

- **Code :** `STATEMENT_UPLOAD_RATE_LIMITED`
- **Source :** `StatementUploadEndpoint`

Cette erreur se produit lorsque trop de téléversements de relevés arrivent dans un court laps de temps et que le point d'entrée limite la requête. Elle est transitoire : la même requête peut être réessayée plus tard.

> **Règle métier :** Les appelants doivent rester dans la limite de débit de téléversement du point d'entrée.

#### Diagnostics

- **L'appelant a dépassé le débit de requêtes autorisé.** — _origine :_ External — Temporisez et réessayez après le délai indiqué dans le message.

#### Exemples

- La requête de téléversement de relevé 11111111-1111-1111-1111-111111111111 a été limitée en débit ; réessayez après 30 secondes. _(Téléversement de relevé limité en débit.)_

#### Contexte

| Clé | Type | Description | Exemples de valeurs |
| --- | --- | --- | --- |
| `REQUEST_ID` | `System.Guid` | L'identifiant de la requête entrante. | `11111111-1111-1111-1111-111111111111` |

<a id="src-money-transfer"></a>

## Erreurs MoneyTransfer

Erreurs levées lors de la validation d'un virement entre comptes.

<a id="err-money-transfer-amount-not-positive"></a>

### Montant de virement non positif

- **Code :** `MONEY_TRANSFER_AMOUNT_NOT_POSITIVE`
- **Source :** `MoneyTransfer`

Cette erreur se produit lorsqu'un virement est demandé avec un montant nul ou négatif.

> **Règle métier :** Le montant d'un virement doit être strictement positif.

#### Diagnostics

- **Le montant a été saisi ou calculé comme nul ou négatif.** — _origine :_ External — Vérifiez le montant de virement demandé et confirmez qu'il est supérieur à zéro.

#### Exemples

- Impossible de virer -25 EUR : le montant doit être strictement positif. _(Le montant du virement doit être positif.)_

#### Contexte

| Clé | Type | Description | Exemples de valeurs |
| --- | --- | --- | --- |
| `TRANSFER_AMOUNT` | `FirstClassErrors.Usage.Model.Amount` | Le montant monétaire du virement tenté. | `-25 EUR` |

<a id="err-money-transfer-invalid"></a>

### Virement invalide

- **Code :** `MONEY_TRANSFER_INVALID`
- **Source :** `MoneyTransfer`

Cette erreur regroupe toutes les règles métier violées lors de la validation d'un virement, afin que l'appelant voie tous les problèmes d'un coup plutôt qu'un par un.

> **Règle métier :** Un virement doit satisfaire toutes les règles métier (un montant strictement positif, des devises identiques, ...).

#### Diagnostics

- **Une ou plusieurs règles métier ont été violées par le virement demandé.** — _origine :_ External — Examinez les erreurs internes agrégées pour voir chaque violation de règle individuelle.

#### Exemples

- Le virement est invalide : il viole une ou plusieurs règles métier. _(Virement invalide.)_

<a id="src-temperature"></a>

## Erreurs Temperature

Errors raised when constructing a Temperature value from an out-of-range input.

<a id="err-temperature-below-absolute-zero"></a>

### Temperature below absolute zero

- **Code :** `TEMPERATURE_BELOW_ABSOLUTE_ZERO`
- **Source :** `Temperature`

This error occurs when trying to instantiate a temperature with a value that is below absolute zero.

> **Règle métier :** Temperature cannot go below absolute zero because absolute zero is the point where particles have minimum possible energy.

#### Diagnostics

- **The value entered manually by a user is invalid.** — _origine :_ External — Verify the value entered by the user and assess its compliance with domain rules.
- **The value received from an external system (API, message, etc.) is invalid.** — _origine :_ External — Check the data provided by the upstream system and evaluate its validity against domain rules.
- **The value was loaded from corrupted or outdated persisted data.** — _origine :_ External — Examine the persisted data source to determine whether stored values comply with current domain rules.
- **The value was computed internally without using domain-safe methods.** — _origine :_ Internal — Inspect the internal computation logic to confirm that domain invariants are preserved.
- **The value originates from system configuration or defaults that are incorrect or outdated.** — _origine :_ External — Review the relevant configuration or default parameters to assess their compliance with domain rules.

#### Exemples

- Failed to instantiate temperature: the value -1 K is below absolute zero. _(Temperature is below absolute zero.)_
- Failed to instantiate temperature: the value -280 °C is below absolute zero. _(Temperature is below absolute zero.)_

