# Felkatalog

## Innehållsförteckning

- [Amount-fel](#src-amount)
  - [Valutakonflikt mellan belopp](#err-amount-currency-mismatch)
- [BankTransactionFileValidator-fel](#src-bank-transaction-file-validator)
  - [Transaktionsdatum utanför utdragsperioden](#err-bank-transaction-file-date-out-of-statement-period)
  - [Avvikelse i utdragets totalbelopp](#err-bank-transaction-file-statement-total-amount-mismatch)
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

- Den monetära operationen misslyckades eftersom de berörda beloppen uttrycks i olika valutor: 127.33 EUR och 57689 USD. _(Valutakonflikt)_

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

- Transaktionen daterad 2024-02-02 ligger utanför utdragsperioden [2024-01-05;2024-01-31]. _(Transaktionsdatumet ligger utanför utdragsperioden.)_

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

- Utdragets deklarerade totalbelopp (1250 EUR) stämmer inte med det totalbelopp som beräknats från transaktionerna (1249.5 EUR). _(Avvikelse i utdragets totalbelopp.)_

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

- Failed to instantiate temperature: the value -1 K is below absolute zero. _(Temperature is below absolute zero.)_
- Failed to instantiate temperature: the value -280 °C is below absolute zero. _(Temperature is below absolute zero.)_

