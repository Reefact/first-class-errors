# Catalogue des erreurs

## Table des matières

- [Erreurs Amount](#src-amount)
  - [Incohérence de devise entre montants](#err-amount-currency-mismatch)
- [Erreurs BankTransactionFileValidator](#src-bank-transaction-file-validator)
  - [Date de transaction hors de la période du relevé](#err-bank-transaction-file-date-out-of-statement-period)
  - [Incohérence du montant total du relevé](#err-bank-transaction-file-statement-total-amount-mismatch)
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

