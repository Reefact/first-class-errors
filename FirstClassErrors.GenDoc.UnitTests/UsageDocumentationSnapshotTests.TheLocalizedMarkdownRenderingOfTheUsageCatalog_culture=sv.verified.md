# Felkatalog

## Innehållsförteckning

- [Amount-fel](#src-amount)
  - [Valutakonflikt mellan belopp](#err-amount-currency-mismatch)
- [BankTransactionFileValidator-fel](#src-bank-transaction-file-validator)
  - [Transaktionsdatum utanför utdragsperioden](#err-bank-transaction-file-date-out-of-statement-period)
  - [Avvikelse i utdragets totalbelopp](#err-bank-transaction-file-statement-total-amount-mismatch)
- [ExchangeRateProvider-fel](#src-exchange-rate-provider)
  - [Växelkurstjänsten är otillgänglig](#err-exchange-rate-service-unavailable)
  - [Valutapar som inte stöds](#err-unsupported-currency-pair)
- [StatementUploadEndpoint-fel](#src-statement-upload-endpoint)
  - [Felaktig utdragspayload](#err-malformed-statement-payload)
  - [Utdragsuppladdning hastighetsbegränsad](#err-statement-upload-rate-limited)
- [MoneyTransfer-fel](#src-money-transfer)
  - [Icke-positivt överföringsbelopp](#err-money-transfer-amount-not-positive)
  - [Ogiltig penningöverföring](#err-money-transfer-invalid)
- [Temperature-fel](#src-temperature)
  - [Temperature below absolute zero](#err-temperature-below-absolute-zero)

<a id="src-amount"></a>

## Amount-fel

Fel som uppstår vid operationer som kombinerar monetära Amount-värden.

<a id="err-amount-currency-mismatch"></a>

### Valutakonflikt mellan belopp

- **Kod:** `AMOUNT_CURRENCY_MISMATCH`
- **Källa:** `Amount`

Det här felet uppstår när flera belopp används tillsammans i en operation trots att de uttrycks i olika valutor.

> **Affärsregel:** Alla monetära operationer måste avse belopp uttryckta i samma valuta.

#### Diagnostik

- **Belopp användes i en monetär operation utan att ha konverterats till samma valuta.** — _ursprung:_ Internal — Kontrollera om alla belopp som ingår i operationen konverterades till en gemensam valuta innan de användes tillsammans.
- **Belopp som förväntades vara uttryckta i samma valuta angavs med olika valutor.** — _ursprung:_ InternalOrExternal — Kontrollera valutorna som är kopplade till varje belopp och bekräfta om en gemensam valuta förväntades för den här operationen.

#### Exempel

**Publikt svar (RFC 9457)**

```json
{
  "type": "urn:problem:sample-service:amount-currency-mismatch",
  "title": "Valutakonflikt",
  "detail": "De två beloppen använder olika valutor och kan inte kombineras.",
  "code": "AMOUNT_CURRENCY_MISMATCH"
}
```

**Diagnostik (intern — inte avsedd för extern exponering)**

```text
2026-07-04T13:42:18.734Z ERROR [Amount] Failed to perform the monetary operation because the involved amounts are expressed in different currencies: 127.33 EUR and 57689 USD. error.code=AMOUNT_CURRENCY_MISMATCH
```

<a id="src-bank-transaction-file-validator"></a>

## BankTransactionFileValidator-fel

Fel som uppstår vid validering av en uppladdad kontoutdragsfil mot dess deklarerade metadata (utdragsperiod och summor).

<a id="err-bank-transaction-file-date-out-of-statement-period"></a>

### Transaktionsdatum utanför utdragsperioden

- **Kod:** `BANK_TRANSACTION_FILE_DATE_OUT_OF_STATEMENT_PERIOD`
- **Källa:** `BankTransactionFileValidator`

Det här felet uppstår när man försöker validera en kontoutdragsfil som innehåller en eller flera transaktioner daterade utanför utdragsperioden.

> **Affärsregel:** Alla transaktioner måste ske inom utdragsperioden.

#### Diagnostik

- **Transaktionsdatumet som anges i utdragsfilen är felaktigt eller stämmer inte med det faktiska transaktionsdatumet.** — _ursprung:_ External — Kontrollera transaktionsdatumet i indatafilen och bekräfta att det stämmer med den faktiska transaktionstidslinjen.
- **Utdragsperioden som definieras i filen matchar inte den faktiska period som transaktionerna täcker.** — _ursprung:_ External — Kontrollera om start- och slutdatumen för utdraget i filen stämmer med den period som transaktionerna täcker.
- **Transaktionen bokfördes efter att utdraget genererades men inkluderades av misstag i filen.** — _ursprung:_ InternalOrExternal — Fastställ om sent bokförda transaktioner inkluderades i processen för att generera utdraget.
- **Ett internt bearbetningsfel försköt transaktionsdatumet under datatransformationen eller importen.** — _ursprung:_ Internal — Granska stegen för dataimport och transformation för att bekräfta att transaktionsdatumen bevaras utan ändring.

#### Exempel

**Publikt svar (RFC 9457)**

```json
{
  "type": "urn:problem:sample-service:bank-transaction-file-date-out-of-statement-period",
  "title": "Transaktionsdatumet ligger utanför utdragsperioden.",
  "detail": "Ett transaktionsdatum ligger utanför kontoutdragets period.",
  "code": "BANK_TRANSACTION_FILE_DATE_OUT_OF_STATEMENT_PERIOD"
}
```

**Diagnostik (intern — inte avsedd för extern exponering)**

```text
2026-07-04T13:42:18.734Z ERROR [BankTransactionFileValidator] Transaction dated 2024-02-02 is outside the statement period [2024-01-05;2024-01-31]. error.code=BANK_TRANSACTION_FILE_DATE_OUT_OF_STATEMENT_PERIOD
```

#### Kontext

| Nyckel | Typ | Beskrivning | Exempelvärden |
| --- | --- | --- | --- |
| `TRANSACTION_DATE` | `System.DateOnly` | Datumet för transaktionen som behandlas. | `02/02/2024` |

<a id="err-bank-transaction-file-statement-total-amount-mismatch"></a>

### Avvikelse i utdragets totalbelopp

- **Kod:** `BANK_TRANSACTION_FILE_STATEMENT_TOTAL_AMOUNT_MISMATCH`
- **Källa:** `BankTransactionFileValidator`

Det här felet uppstår när man försöker validera en kontoutdragsfil vars deklarerade totalbelopp inte stämmer med summan av de enskilda transaktionsbeloppen.

> **Affärsregel:** Utdragets totalbelopp måste vara lika med summan av alla transaktionsbelopp som ingår i utdraget.

#### Diagnostik

- **Det totalbelopp som deklareras i utdragsfilen stämmer inte med summan av de enskilda transaktionsbeloppen.** — _ursprung:_ External — Kontrollera det deklarerade totalbeloppet i filen och jämför det med summan av alla transaktionsbelopp.
- **En eller flera transaktioner saknas eller är duplicerade i utdragsfilen.** — _ursprung:_ External — Kontrollera om alla förväntade transaktioner förekommer exakt en gång i utdragsfilen.
- **Ett avrundnings- eller precisionsfel uppstod vid beräkningen av utdragets totalbelopp.** — _ursprung:_ InternalOrExternal — Granska hur avrundnings- och precisionsregler tillämpades vid beräkningen av utdragets summa.
- **Ett internt bearbetningsfel ändrade transaktionsbelopp under tolkning eller transformation av filen.** — _ursprung:_ Internal — Inspektera stegen för tolkning och transformation av filen för att bekräfta att transaktionsbeloppen förblir oförändrade.

#### Exempel

**Publikt svar (RFC 9457)**

```json
{
  "type": "urn:problem:sample-service:bank-transaction-file-statement-total-amount-mismatch",
  "title": "Avvikelse i utdragets totalbelopp.",
  "detail": "Det angivna totalbeloppet för kontoutdraget stämmer inte med det beräknade totalbeloppet.",
  "code": "BANK_TRANSACTION_FILE_STATEMENT_TOTAL_AMOUNT_MISMATCH"
}
```

**Diagnostik (intern — inte avsedd för extern exponering)**

```text
2026-07-04T13:42:18.734Z ERROR [BankTransactionFileValidator] The declared statement total amount (1250 EUR) does not match the computed total amount from transactions (1249.5 EUR). error.code=BANK_TRANSACTION_FILE_STATEMENT_TOTAL_AMOUNT_MISMATCH
```

<a id="src-exchange-rate-provider"></a>

## ExchangeRateProvider-fel

Fel som uppstår vid anrop till den externa växelkursleverantören (en utgående secondary-port-adapter).

<a id="err-exchange-rate-service-unavailable"></a>

### Växelkurstjänsten är otillgänglig

- **Kod:** `EXCHANGE_RATE_SERVICE_UNAVAILABLE`
- **Källa:** `ExchangeRateProvider`

Det här felet uppstår när den externa växelkursleverantören inte kan nås (en timeout, en återställd anslutning eller ett 5xx-svar). Det är övergående: anropet kan försökas igen.

> **Affärsregel:** Valutakonvertering är beroende av en nåbar växelkursleverantör.

#### Diagnostik

- **Leverantören nådde en timeout eller returnerade ett serverfel.** — _ursprung:_ External — Kontrollera leverantörens hälsa och försök anropet igen, helst med en backoff.
- **Den utgående nätverksvägen till leverantören är störd.** — _ursprung:_ InternalOrExternal — Verifiera utgående anslutning och eventuell proxy eller brandvägg mellan tjänsten och leverantören.

#### Exempel

**Publikt svar (RFC 9457)**

```json
{
  "type": "urn:problem:sample-service:exchange-rate-service-unavailable",
  "title": "Växelkurstjänsten är otillgänglig.",
  "detail": "Växelkurstjänsten är tillfälligt otillgänglig; försök igen senare.",
  "code": "EXCHANGE_RATE_SERVICE_UNAVAILABLE"
}
```

**Diagnostik (intern — inte avsedd för extern exponering)**

```text
2026-07-04T13:42:18.734Z ERROR [ExchangeRateProvider] The exchange-rate provider 'acme-fx' is unavailable (correlation 22222222-2222-2222-2222-222222222222). error.code=EXCHANGE_RATE_SERVICE_UNAVAILABLE
```

#### Kontext

| Nyckel | Typ | Beskrivning | Exempelvärden |
| --- | --- | --- | --- |
| `PROVIDER` | `System.String` | Den externa leverantören som anropades. | `acme-fx` |
| `CORRELATION_ID` | `System.Guid` | Korrelationsidentifieraren för det utgående anropet. | `22222222-2222-2222-2222-222222222222` |

<a id="err-unsupported-currency-pair"></a>

### Valutapar som inte stöds

- **Kod:** `UNSUPPORTED_CURRENCY_PAIR`
- **Källa:** `ExchangeRateProvider`

Det här felet uppstår när växelkursleverantören inte noterar någon kurs för det begärda käll-/målvalutaparet.

> **Affärsregel:** En valutakonvertering kan endast utföras för ett par som leverantören noterar.

#### Diagnostik

- **Det begärda valutaparet erbjuds inte av leverantören.** — _ursprung:_ External — Bekräfta att leverantören stöder både käll- och målvalutan innan du begär en konvertering.

#### Exempel

**Publikt svar (RFC 9457)**

```json
{
  "type": "urn:problem:sample-service:unsupported-currency-pair",
  "title": "Valutapar som inte stöds.",
  "detail": "Det begärda valutaparet stöds inte.",
  "code": "UNSUPPORTED_CURRENCY_PAIR"
}
```

**Diagnostik (intern — inte avsedd för extern exponering)**

```text
2026-07-04T13:42:18.734Z ERROR [ExchangeRateProvider] The exchange-rate provider does not quote the EUR to USD currency pair. error.code=UNSUPPORTED_CURRENCY_PAIR
```

#### Kontext

| Nyckel | Typ | Beskrivning | Exempelvärden |
| --- | --- | --- | --- |
| `FROM_CURRENCY` | `FirstClassErrors.Usage.Model.Currency` | Källvalutan för konverteringen. | `EUR` |
| `TO_CURRENCY` | `FirstClassErrors.Usage.Model.Currency` | Målvalutan för konverteringen. | `USD` |

<a id="src-statement-upload-endpoint"></a>

## StatementUploadEndpoint-fel

Fel som utlöses av HTTP-slutpunkten som tar emot uppladdade kontoutdrag (en inkommande primary-port-adapter).

<a id="err-malformed-statement-payload"></a>

### Felaktig utdragspayload

- **Kod:** `MALFORMED_STATEMENT_PAYLOAD`
- **Källa:** `StatementUploadEndpoint`

Det här felet uppstår när slutpunkten för uppladdning av utdrag tar emot en begäran vars body saknar ett obligatoriskt fält eller innehåller ett ogiltigt värde.

> **Affärsregel:** En uppladdad utdragsbegäran måste innehålla alla obligatoriska fält med ett giltigt värde.

#### Diagnostik

- **Klienten skickade en ofullständig eller felaktig begärans-body.** — _ursprung:_ External — Granska fältet som anges i kontexten och bekräfta att klienten skickar det med ett giltigt värde.

#### Exempel

**Publikt svar (RFC 9457)**

```json
{
  "type": "urn:problem:sample-service:malformed-statement-payload",
  "title": "Felaktig utdragspayload.",
  "detail": "Den uppladdade utdragsbegäran saknar ett obligatoriskt fält eller innehåller ett ogiltigt värde.",
  "code": "MALFORMED_STATEMENT_PAYLOAD"
}
```

**Diagnostik (intern — inte avsedd för extern exponering)**

```text
2026-07-04T13:42:18.734Z ERROR [StatementUploadEndpoint] The statement upload request 11111111-1111-1111-1111-111111111111 is malformed: the 'statementPeriod' field is missing or invalid. error.code=MALFORMED_STATEMENT_PAYLOAD
```

#### Kontext

| Nyckel | Typ | Beskrivning | Exempelvärden |
| --- | --- | --- | --- |
| `REQUEST_ID` | `System.Guid` | Identifieraren för den inkommande begäran. | `11111111-1111-1111-1111-111111111111` |
| `FIELD` | `System.String` | Begäransfältet som inte klarade valideringen. | `statementPeriod` |

<a id="err-statement-upload-rate-limited"></a>

### Utdragsuppladdning hastighetsbegränsad

- **Kod:** `STATEMENT_UPLOAD_RATE_LIMITED`
- **Källa:** `StatementUploadEndpoint`

Det här felet uppstår när för många utdragsuppladdningar anländer under ett kort intervall och slutpunkten stryper begäran. Det är övergående: samma begäran kan försökas igen senare.

> **Affärsregel:** Anropare måste hålla sig inom slutpunktens hastighetsgräns för uppladdning.

#### Diagnostik

- **Anroparen överskred den tillåtna begärandefrekvensen.** — _ursprung:_ External — Vänta och försök igen efter fördröjningen som anges i meddelandet.

#### Exempel

**Publikt svar (RFC 9457)**

```json
{
  "type": "urn:problem:sample-service:statement-upload-rate-limited",
  "title": "Utdragsuppladdning hastighetsbegränsad.",
  "detail": "För många utdragsuppladdningar skickades under kort tid; försök igen senare.",
  "code": "STATEMENT_UPLOAD_RATE_LIMITED"
}
```

**Diagnostik (intern — inte avsedd för extern exponering)**

```text
2026-07-04T13:42:18.734Z ERROR [StatementUploadEndpoint] The statement upload request 11111111-1111-1111-1111-111111111111 was rate-limited; retry after 30 seconds. error.code=STATEMENT_UPLOAD_RATE_LIMITED
```

#### Kontext

| Nyckel | Typ | Beskrivning | Exempelvärden |
| --- | --- | --- | --- |
| `REQUEST_ID` | `System.Guid` | Identifieraren för den inkommande begäran. | `11111111-1111-1111-1111-111111111111` |

<a id="src-money-transfer"></a>

## MoneyTransfer-fel

Fel som uppstår vid validering av en penningöverföring mellan konton.

<a id="err-money-transfer-amount-not-positive"></a>

### Icke-positivt överföringsbelopp

- **Kod:** `MONEY_TRANSFER_AMOUNT_NOT_POSITIVE`
- **Källa:** `MoneyTransfer`

Det här felet uppstår när en överföring begärs med ett belopp som är noll eller negativt.

> **Affärsregel:** Beloppet för en överföring måste vara strikt positivt.

#### Diagnostik

- **Beloppet angavs eller beräknades som noll eller ett negativt värde.** — _ursprung:_ External — Kontrollera det begärda överföringsbeloppet och bekräfta att det är större än noll.

#### Exempel

**Publikt svar (RFC 9457)**

```json
{
  "type": "urn:problem:sample-service:money-transfer-amount-not-positive",
  "title": "Överföringsbeloppet måste vara positivt.",
  "detail": "Överföringsbeloppet måste vara större än noll.",
  "code": "MONEY_TRANSFER_AMOUNT_NOT_POSITIVE"
}
```

**Diagnostik (intern — inte avsedd för extern exponering)**

```text
2026-07-04T13:42:18.734Z ERROR [MoneyTransfer] Cannot transfer -25 EUR: the amount must be strictly positive. error.code=MONEY_TRANSFER_AMOUNT_NOT_POSITIVE
```

#### Kontext

| Nyckel | Typ | Beskrivning | Exempelvärden |
| --- | --- | --- | --- |
| `TRANSFER_AMOUNT` | `FirstClassErrors.Usage.Model.Amount` | Det monetära beloppet för den försökta överföringen. | `-25 EUR` |

<a id="err-money-transfer-invalid"></a>

### Ogiltig penningöverföring

- **Kod:** `MONEY_TRANSFER_INVALID`
- **Källa:** `MoneyTransfer`

Det här felet samlar alla domänregler som bröts vid valideringen av en överföring, så att anroparen ser alla problem på en gång i stället för ett i taget.

> **Affärsregel:** En överföring måste uppfylla alla domänregler (ett strikt positivt belopp, matchande valutor, ...).

#### Diagnostik

- **Den begärda överföringen bröt mot en eller flera domänregler.** — _ursprung:_ External — Granska de sammanslagna inre felen för att se varje enskild regelöverträdelse.

#### Exempel

**Publikt svar (RFC 9457)**

```json
{
  "type": "urn:problem:sample-service:money-transfer-invalid",
  "title": "Ogiltig överföring.",
  "detail": "Överföringen uppfyller inte alla nödvändiga regler.",
  "code": "MONEY_TRANSFER_INVALID"
}
```

**Diagnostik (intern — inte avsedd för extern exponering)**

```text
2026-07-04T13:42:18.734Z ERROR [MoneyTransfer] The money transfer is invalid: it violates one or more domain rules. error.code=MONEY_TRANSFER_INVALID
```

<a id="src-temperature"></a>

## Temperature-fel

Errors raised when constructing a Temperature value from an out-of-range input.

<a id="err-temperature-below-absolute-zero"></a>

### Temperature below absolute zero

- **Kod:** `TEMPERATURE_BELOW_ABSOLUTE_ZERO`
- **Källa:** `Temperature`

This error occurs when trying to instantiate a temperature with a value that is below absolute zero.

> **Affärsregel:** Temperature cannot go below absolute zero because absolute zero is the point where particles have minimum possible energy.

#### Diagnostik

- **The value entered manually by a user is invalid.** — _ursprung:_ External — Verify the value entered by the user and assess its compliance with domain rules.
- **The value received from an external system (API, message, etc.) is invalid.** — _ursprung:_ External — Check the data provided by the upstream system and evaluate its validity against domain rules.
- **The value was loaded from corrupted or outdated persisted data.** — _ursprung:_ External — Examine the persisted data source to determine whether stored values comply with current domain rules.
- **The value was computed internally without using domain-safe methods.** — _ursprung:_ Internal — Inspect the internal computation logic to confirm that domain invariants are preserved.
- **The value originates from system configuration or defaults that are incorrect or outdated.** — _ursprung:_ External — Review the relevant configuration or default parameters to assess their compliance with domain rules.

#### Exempel

**Publikt svar (RFC 9457)**

```json
{
  "type": "urn:problem:sample-service:temperature-below-absolute-zero",
  "title": "Temperature is invalid.",
  "detail": "The temperature -1 K is below absolute zero.",
  "code": "TEMPERATURE_BELOW_ABSOLUTE_ZERO"
}
```

**Diagnostik (intern — inte avsedd för extern exponering)**

```text
2026-07-04T13:42:18.734Z ERROR [Temperature] Failed to instantiate temperature: the value -1 K is below absolute zero. error.code=TEMPERATURE_BELOW_ABSOLUTE_ZERO
```

**Publikt svar (RFC 9457)**

```json
{
  "type": "urn:problem:sample-service:temperature-below-absolute-zero",
  "title": "Temperature is invalid.",
  "detail": "The temperature -280 °C is below absolute zero.",
  "code": "TEMPERATURE_BELOW_ABSOLUTE_ZERO"
}
```

**Diagnostik (intern — inte avsedd för extern exponering)**

```text
2026-07-04T13:42:18.734Z ERROR [Temperature] Failed to instantiate temperature: the value -280 °C is below absolute zero. error.code=TEMPERATURE_BELOW_ABSOLUTE_ZERO
```

