# Fehlerkatalog

## Inhaltsverzeichnis

- [Amount-Fehler](#src-amount)
  - [Währungskonflikt zwischen Beträgen](#err-amount-currency-mismatch)
- [BankTransactionFileValidator-Fehler](#src-bank-transaction-file-validator)
  - [Transaktionsdatum außerhalb des Auszugszeitraums](#err-bank-transaction-file-date-out-of-statement-period)
  - [Abweichung des Gesamtbetrags des Auszugs](#err-bank-transaction-file-statement-total-amount-mismatch)
- [ExchangeRateProvider-Fehler](#src-exchange-rate-provider)
  - [Wechselkursdienst nicht verfügbar](#err-exchange-rate-service-unavailable)
  - [Nicht unterstütztes Währungspaar](#err-unsupported-currency-pair)
- [StatementUploadEndpoint-Fehler](#src-statement-upload-endpoint)
  - [Fehlerhafte Auszugsnutzlast](#err-malformed-statement-payload)
  - [Auszug-Upload gedrosselt](#err-statement-upload-rate-limited)
- [MoneyTransfer-Fehler](#src-money-transfer)
  - [Nicht positiver Überweisungsbetrag](#err-money-transfer-amount-not-positive)
  - [Ungültige Geldüberweisung](#err-money-transfer-invalid)
- [Temperature-Fehler](#src-temperature)
  - [Temperature below absolute zero](#err-temperature-below-absolute-zero)

<a id="src-amount"></a>

## Amount-Fehler

Fehler, die bei Operationen ausgelöst werden, die monetäre Amount-Werte kombinieren.

<a id="err-amount-currency-mismatch"></a>

### Währungskonflikt zwischen Beträgen

- **Code:** `AMOUNT_CURRENCY_MISMATCH`
- **Quelle:** `Amount`

Dieser Fehler tritt auf, wenn mehrere Beträge gemeinsam in einer Operation verwendet werden, obwohl sie in unterschiedlichen Währungen ausgedrückt sind.

> **Geschäftsregel:** Alle monetären Operationen müssen Beträge in derselben Währung betreffen.

#### Diagnosen

- **Beträge wurden in einer monetären Operation verwendet, ohne zuvor in dieselbe Währung umgerechnet worden zu sein.** — _Ursprung:_ Internal — Prüfen Sie, ob alle an der Operation beteiligten Beträge vor der gemeinsamen Verwendung in eine gemeinsame Währung umgerechnet wurden.
- **Beträge, die in derselben Währung erwartet wurden, wurden mit unterschiedlichen Währungen bereitgestellt.** — _Ursprung:_ InternalOrExternal — Überprüfen Sie die jedem Betrag zugeordneten Währungen und bestätigen Sie, ob für diese Operation eine gemeinsame Währung erwartet wurde.

#### Beispiele

**Öffentliche Antwort (RFC 9457)**

```json
{
  "title": "Währungskonflikt",
  "detail": "Die beiden Beträge verwenden unterschiedliche Währungen und können nicht kombiniert werden.",
  "code": "AMOUNT_CURRENCY_MISMATCH"
}
```

**Diagnose (intern — nicht für externe Weitergabe)**

```text
2026-07-04T13:42:18.734Z ERROR [Amount] Failed to perform the monetary operation because the involved amounts are expressed in different currencies: 127.33 EUR and 57689 USD. error.code=AMOUNT_CURRENCY_MISMATCH
```

<a id="src-bank-transaction-file-validator"></a>

## BankTransactionFileValidator-Fehler

Fehler, die bei der Validierung einer hochgeladenen Kontoauszugsdatei anhand ihrer deklarierten Metadaten (Auszugszeitraum und Summen) ausgelöst werden.

<a id="err-bank-transaction-file-date-out-of-statement-period"></a>

### Transaktionsdatum außerhalb des Auszugszeitraums

- **Code:** `BANK_TRANSACTION_FILE_DATE_OUT_OF_STATEMENT_PERIOD`
- **Quelle:** `BankTransactionFileValidator`

Dieser Fehler tritt auf, wenn versucht wird, eine Kontoauszugsdatei zu validieren, die eine oder mehrere Transaktionen mit einem Datum außerhalb des Auszugszeitraums enthält.

> **Geschäftsregel:** Alle Transaktionen müssen innerhalb des Auszugszeitraums stattfinden.

#### Diagnosen

- **Das in der Auszugsdatei angegebene Transaktionsdatum ist falsch oder stimmt nicht mit dem tatsächlichen Transaktionsdatum überein.** — _Ursprung:_ External — Überprüfen Sie das in der Eingabedatei enthaltene Transaktionsdatum und bestätigen Sie seine Übereinstimmung mit dem tatsächlichen zeitlichen Ablauf der Transaktion.
- **Der in der Datei definierte Auszugszeitraum stimmt nicht mit dem tatsächlichen Abdeckungszeitraum der Transaktionen überein.** — _Ursprung:_ External — Prüfen Sie, ob das Anfangs- und Enddatum des Auszugs in der Datei mit dem von den Transaktionen abgedeckten Zeitraum übereinstimmen.
- **Die Transaktion wurde nach der Erstellung des Auszugs gebucht, aber versehentlich in die Datei aufgenommen.** — _Ursprung:_ InternalOrExternal — Ermitteln Sie, ob verspätet gebuchte Transaktionen in den Prozess der Auszugserstellung einbezogen wurden.
- **Ein interner Verarbeitungsfehler hat das Transaktionsdatum während der Datentransformation oder des Imports verschoben.** — _Ursprung:_ Internal — Untersuchen Sie die Phasen des Datenimports und der Transformation, um zu bestätigen, dass Transaktionsdaten unverändert erhalten bleiben.

#### Beispiele

**Öffentliche Antwort (RFC 9457)**

```json
{
  "title": "Das Transaktionsdatum liegt außerhalb des Auszugszeitraums.",
  "detail": "Ein Transaktionsdatum liegt außerhalb des Auszugszeitraums.",
  "code": "BANK_TRANSACTION_FILE_DATE_OUT_OF_STATEMENT_PERIOD"
}
```

**Diagnose (intern — nicht für externe Weitergabe)**

```text
2026-07-04T13:42:18.734Z ERROR [BankTransactionFileValidator] Transaction dated 2024-02-02 is outside the statement period [2024-01-05;2024-01-31]. error.code=BANK_TRANSACTION_FILE_DATE_OUT_OF_STATEMENT_PERIOD
```

#### Kontext

| Schlüssel | Typ | Beschreibung | Beispielwerte |
| --- | --- | --- | --- |
| `TRANSACTION_DATE` | `System.DateOnly` | Das Datum der gerade verarbeiteten Transaktion. | `02/02/2024` |

<a id="err-bank-transaction-file-statement-total-amount-mismatch"></a>

### Abweichung des Gesamtbetrags des Auszugs

- **Code:** `BANK_TRANSACTION_FILE_STATEMENT_TOTAL_AMOUNT_MISMATCH`
- **Quelle:** `BankTransactionFileValidator`

Dieser Fehler tritt auf, wenn versucht wird, eine Kontoauszugsdatei zu validieren, deren deklarierter Gesamtbetrag nicht mit der Summe der einzelnen Transaktionsbeträge übereinstimmt.

> **Geschäftsregel:** Der Gesamtbetrag des Auszugs muss der Summe aller im Auszug enthaltenen Transaktionsbeträge entsprechen.

#### Diagnosen

- **Der in der Auszugsdatei deklarierte Gesamtbetrag stimmt nicht mit der Summe der einzelnen Transaktionsbeträge überein.** — _Ursprung:_ External — Überprüfen Sie den in der Datei deklarierten Gesamtbetrag und vergleichen Sie ihn mit der Summe aller Transaktionsbeträge.
- **Eine oder mehrere Transaktionen fehlen in der Auszugsdatei oder sind doppelt vorhanden.** — _Ursprung:_ External — Prüfen Sie, ob alle erwarteten Transaktionen genau einmal in der Auszugsdatei vorhanden sind.
- **Beim Berechnen des Gesamtbetrags des Auszugs ist ein Rundungs- oder Genauigkeitsfehler aufgetreten.** — _Ursprung:_ InternalOrExternal — Untersuchen Sie, wie die Rundungs- und Genauigkeitsregeln bei der Berechnung der Auszugssumme angewendet wurden.
- **Ein interner Verarbeitungsfehler hat Transaktionsbeträge beim Parsen oder Transformieren der Datei verändert.** — _Ursprung:_ Internal — Prüfen Sie die Phasen des Parsens und der Transformation der Datei, um zu bestätigen, dass die Transaktionsbeträge unverändert bleiben.

#### Beispiele

**Öffentliche Antwort (RFC 9457)**

```json
{
  "title": "Abweichung des Gesamtbetrags des Auszugs.",
  "detail": "Der angegebene Auszugsgesamtbetrag stimmt nicht mit dem berechneten Gesamtbetrag überein.",
  "code": "BANK_TRANSACTION_FILE_STATEMENT_TOTAL_AMOUNT_MISMATCH"
}
```

**Diagnose (intern — nicht für externe Weitergabe)**

```text
2026-07-04T13:42:18.734Z ERROR [BankTransactionFileValidator] The declared statement total amount (1250 EUR) does not match the computed total amount from transactions (1249.5 EUR). error.code=BANK_TRANSACTION_FILE_STATEMENT_TOTAL_AMOUNT_MISMATCH
```

<a id="src-exchange-rate-provider"></a>

## ExchangeRateProvider-Fehler

Fehler, die beim Aufruf des externen Wechselkursanbieters ausgelöst werden (ein ausgehender Secondary-Port-Adapter).

<a id="err-exchange-rate-service-unavailable"></a>

### Wechselkursdienst nicht verfügbar

- **Code:** `EXCHANGE_RATE_SERVICE_UNAVAILABLE`
- **Quelle:** `ExchangeRateProvider`

Dieser Fehler tritt auf, wenn der externe Wechselkursanbieter nicht erreichbar ist (ein Timeout, ein Verbindungsabbruch oder eine 5xx-Antwort). Er ist transient: Der Aufruf kann wiederholt werden.

> **Geschäftsregel:** Die Währungsumrechnung hängt von einem erreichbaren Wechselkursanbieter ab.

#### Diagnosen

- **Der Anbieter hat das Zeitlimit überschritten oder einen Serverfehler zurückgegeben.** — _Ursprung:_ External — Prüfen Sie den Zustand des Anbieters und wiederholen Sie den Aufruf, idealerweise mit einem Backoff.
- **Der ausgehende Netzwerkpfad zum Anbieter ist gestört.** — _Ursprung:_ InternalOrExternal — Überprüfen Sie die ausgehende Konnektivität sowie jeden Proxy oder jede Firewall zwischen dem Dienst und dem Anbieter.

#### Beispiele

**Öffentliche Antwort (RFC 9457)**

```json
{
  "title": "Wechselkursdienst nicht verfügbar.",
  "detail": "Der Wechselkursdienst ist vorübergehend nicht verfügbar; bitte versuchen Sie es später erneut.",
  "code": "EXCHANGE_RATE_SERVICE_UNAVAILABLE"
}
```

**Diagnose (intern — nicht für externe Weitergabe)**

```text
2026-07-04T13:42:18.734Z ERROR [ExchangeRateProvider] The exchange-rate provider 'acme-fx' is unavailable (correlation 22222222-2222-2222-2222-222222222222). error.code=EXCHANGE_RATE_SERVICE_UNAVAILABLE
```

#### Kontext

| Schlüssel | Typ | Beschreibung | Beispielwerte |
| --- | --- | --- | --- |
| `PROVIDER` | `System.String` | Der externe Anbieter, der aufgerufen wurde. | `acme-fx` |
| `CORRELATION_ID` | `System.Guid` | Die Korrelationskennung des ausgehenden Aufrufs. | `22222222-2222-2222-2222-222222222222` |

<a id="err-unsupported-currency-pair"></a>

### Nicht unterstütztes Währungspaar

- **Code:** `UNSUPPORTED_CURRENCY_PAIR`
- **Quelle:** `ExchangeRateProvider`

Dieser Fehler tritt auf, wenn der Wechselkursanbieter keinen Kurs für das angeforderte Quell-/Zielwährungspaar notiert.

> **Geschäftsregel:** Eine Währungsumrechnung kann nur für ein vom Anbieter notiertes Paar durchgeführt werden.

#### Diagnosen

- **Das angeforderte Währungspaar wird vom Anbieter nicht angeboten.** — _Ursprung:_ External — Bestätigen Sie, dass der Anbieter sowohl die Quell- als auch die Zielwährung unterstützt, bevor Sie eine Umrechnung anfordern.

#### Beispiele

**Öffentliche Antwort (RFC 9457)**

```json
{
  "title": "Nicht unterstütztes Währungspaar.",
  "detail": "Das angeforderte Währungspaar wird nicht unterstützt.",
  "code": "UNSUPPORTED_CURRENCY_PAIR"
}
```

**Diagnose (intern — nicht für externe Weitergabe)**

```text
2026-07-04T13:42:18.734Z ERROR [ExchangeRateProvider] The exchange-rate provider does not quote the EUR to USD currency pair. error.code=UNSUPPORTED_CURRENCY_PAIR
```

#### Kontext

| Schlüssel | Typ | Beschreibung | Beispielwerte |
| --- | --- | --- | --- |
| `FROM_CURRENCY` | `FirstClassErrors.Usage.Model.Currency` | Die Quellwährung der Umrechnung. | `EUR` |
| `TO_CURRENCY` | `FirstClassErrors.Usage.Model.Currency` | Die Zielwährung der Umrechnung. | `USD` |

<a id="src-statement-upload-endpoint"></a>

## StatementUploadEndpoint-Fehler

Fehler, die vom HTTP-Endpunkt ausgelöst werden, der hochgeladene Kontoauszüge entgegennimmt (ein eingehender Primary-Port-Adapter).

<a id="err-malformed-statement-payload"></a>

### Fehlerhafte Auszugsnutzlast

- **Code:** `MALFORMED_STATEMENT_PAYLOAD`
- **Quelle:** `StatementUploadEndpoint`

Dieser Fehler tritt auf, wenn der Endpunkt zum Hochladen von Auszügen eine Anfrage erhält, deren Body ein Pflichtfeld auslässt oder einen ungültigen Wert enthält.

> **Geschäftsregel:** Eine hochgeladene Auszugsanfrage muss alle Pflichtfelder mit einem gültigen Wert enthalten.

#### Diagnosen

- **Der Client hat einen unvollständigen oder fehlerhaften Anfrage-Body gesendet.** — _Ursprung:_ External — Prüfen Sie das im Kontext genannte Feld und bestätigen Sie, dass der Client es mit einem gültigen Wert sendet.

#### Beispiele

**Öffentliche Antwort (RFC 9457)**

```json
{
  "title": "Fehlerhafte Auszugsnutzlast.",
  "detail": "Der hochgeladenen Auszugsanfrage fehlt ein Pflichtfeld oder sie enthält einen ungültigen Wert.",
  "code": "MALFORMED_STATEMENT_PAYLOAD"
}
```

**Diagnose (intern — nicht für externe Weitergabe)**

```text
2026-07-04T13:42:18.734Z ERROR [StatementUploadEndpoint] The statement upload request 11111111-1111-1111-1111-111111111111 is malformed: the 'statementPeriod' field is missing or invalid. error.code=MALFORMED_STATEMENT_PAYLOAD
```

#### Kontext

| Schlüssel | Typ | Beschreibung | Beispielwerte |
| --- | --- | --- | --- |
| `REQUEST_ID` | `System.Guid` | Die Kennung der eingehenden Anfrage. | `11111111-1111-1111-1111-111111111111` |
| `FIELD` | `System.String` | Das Anfragefeld, das die Validierung nicht bestanden hat. | `statementPeriod` |

<a id="err-statement-upload-rate-limited"></a>

### Auszug-Upload gedrosselt

- **Code:** `STATEMENT_UPLOAD_RATE_LIMITED`
- **Quelle:** `StatementUploadEndpoint`

Dieser Fehler tritt auf, wenn zu viele Auszug-Uploads in kurzer Zeit eintreffen und der Endpunkt die Anfrage drosselt. Er ist transient: Dieselbe Anfrage kann später erneut versucht werden.

> **Geschäftsregel:** Aufrufer müssen das Upload-Ratenlimit des Endpunkts einhalten.

#### Diagnosen

- **Der Aufrufer hat die zulässige Anfragerate überschritten.** — _Ursprung:_ External — Warten Sie und versuchen Sie es nach der in der Nachricht angegebenen Verzögerung erneut.

#### Beispiele

**Öffentliche Antwort (RFC 9457)**

```json
{
  "title": "Auszug-Upload gedrosselt.",
  "detail": "Es wurden zu viele Auszug-Uploads in kurzer Zeit gesendet; bitte versuchen Sie es später erneut.",
  "code": "STATEMENT_UPLOAD_RATE_LIMITED"
}
```

**Diagnose (intern — nicht für externe Weitergabe)**

```text
2026-07-04T13:42:18.734Z ERROR [StatementUploadEndpoint] The statement upload request 11111111-1111-1111-1111-111111111111 was rate-limited; retry after 30 seconds. error.code=STATEMENT_UPLOAD_RATE_LIMITED
```

#### Kontext

| Schlüssel | Typ | Beschreibung | Beispielwerte |
| --- | --- | --- | --- |
| `REQUEST_ID` | `System.Guid` | Die Kennung der eingehenden Anfrage. | `11111111-1111-1111-1111-111111111111` |

<a id="src-money-transfer"></a>

## MoneyTransfer-Fehler

Fehler, die bei der Validierung einer Geldüberweisung zwischen Konten ausgelöst werden.

<a id="err-money-transfer-amount-not-positive"></a>

### Nicht positiver Überweisungsbetrag

- **Code:** `MONEY_TRANSFER_AMOUNT_NOT_POSITIVE`
- **Quelle:** `MoneyTransfer`

Dieser Fehler tritt auf, wenn eine Überweisung mit einem Betrag von null oder negativ angefordert wird.

> **Geschäftsregel:** Der Betrag einer Überweisung muss strikt positiv sein.

#### Diagnosen

- **Der Betrag wurde als null oder negativer Wert eingegeben oder berechnet.** — _Ursprung:_ External — Prüfen Sie den angeforderten Überweisungsbetrag und bestätigen Sie, dass er größer als null ist.

#### Beispiele

**Öffentliche Antwort (RFC 9457)**

```json
{
  "title": "Der Überweisungsbetrag muss positiv sein.",
  "detail": "Der Überweisungsbetrag muss größer als null sein.",
  "code": "MONEY_TRANSFER_AMOUNT_NOT_POSITIVE"
}
```

**Diagnose (intern — nicht für externe Weitergabe)**

```text
2026-07-04T13:42:18.734Z ERROR [MoneyTransfer] Cannot transfer -25 EUR: the amount must be strictly positive. error.code=MONEY_TRANSFER_AMOUNT_NOT_POSITIVE
```

#### Kontext

| Schlüssel | Typ | Beschreibung | Beispielwerte |
| --- | --- | --- | --- |
| `TRANSFER_AMOUNT` | `FirstClassErrors.Usage.Model.Amount` | Der Geldbetrag der versuchten Überweisung. | `-25 EUR` |

<a id="err-money-transfer-invalid"></a>

### Ungültige Geldüberweisung

- **Code:** `MONEY_TRANSFER_INVALID`
- **Quelle:** `MoneyTransfer`

Dieser Fehler fasst alle bei der Validierung einer Überweisung verletzten Domänenregeln zusammen, sodass der Aufrufer alle Probleme auf einmal statt einzeln sieht.

> **Geschäftsregel:** Eine Überweisung muss alle Domänenregeln erfüllen (ein strikt positiver Betrag, übereinstimmende Währungen, ...).

#### Diagnosen

- **Die angeforderte Überweisung hat eine oder mehrere Domänenregeln verletzt.** — _Ursprung:_ External — Untersuchen Sie die zusammengefassten inneren Fehler, um jede einzelne Regelverletzung zu sehen.

#### Beispiele

**Öffentliche Antwort (RFC 9457)**

```json
{
  "title": "Ungültige Überweisung.",
  "detail": "Die Überweisung erfüllt nicht alle erforderlichen Regeln.",
  "code": "MONEY_TRANSFER_INVALID"
}
```

**Diagnose (intern — nicht für externe Weitergabe)**

```text
2026-07-04T13:42:18.734Z ERROR [MoneyTransfer] The money transfer is invalid: it violates one or more domain rules. error.code=MONEY_TRANSFER_INVALID
```

<a id="src-temperature"></a>

## Temperature-Fehler

Errors raised when constructing a Temperature value from an out-of-range input.

<a id="err-temperature-below-absolute-zero"></a>

### Temperature below absolute zero

- **Code:** `TEMPERATURE_BELOW_ABSOLUTE_ZERO`
- **Quelle:** `Temperature`

This error occurs when trying to instantiate a temperature with a value that is below absolute zero.

> **Geschäftsregel:** Temperature cannot go below absolute zero because absolute zero is the point where particles have minimum possible energy.

#### Diagnosen

- **The value entered manually by a user is invalid.** — _Ursprung:_ External — Verify the value entered by the user and assess its compliance with domain rules.
- **The value received from an external system (API, message, etc.) is invalid.** — _Ursprung:_ External — Check the data provided by the upstream system and evaluate its validity against domain rules.
- **The value was loaded from corrupted or outdated persisted data.** — _Ursprung:_ External — Examine the persisted data source to determine whether stored values comply with current domain rules.
- **The value was computed internally without using domain-safe methods.** — _Ursprung:_ Internal — Inspect the internal computation logic to confirm that domain invariants are preserved.
- **The value originates from system configuration or defaults that are incorrect or outdated.** — _Ursprung:_ External — Review the relevant configuration or default parameters to assess their compliance with domain rules.

#### Beispiele

**Öffentliche Antwort (RFC 9457)**

```json
{
  "title": "Temperature is invalid.",
  "detail": "The temperature -1 K is below absolute zero.",
  "code": "TEMPERATURE_BELOW_ABSOLUTE_ZERO"
}
```

**Diagnose (intern — nicht für externe Weitergabe)**

```text
2026-07-04T13:42:18.734Z ERROR [Temperature] Failed to instantiate temperature: the value -1 K is below absolute zero. error.code=TEMPERATURE_BELOW_ABSOLUTE_ZERO
```

**Öffentliche Antwort (RFC 9457)**

```json
{
  "title": "Temperature is invalid.",
  "detail": "The temperature -280 °C is below absolute zero.",
  "code": "TEMPERATURE_BELOW_ABSOLUTE_ZERO"
}
```

**Diagnose (intern — nicht für externe Weitergabe)**

```text
2026-07-04T13:42:18.734Z ERROR [Temperature] Failed to instantiate temperature: the value -280 °C is below absolute zero. error.code=TEMPERATURE_BELOW_ABSOLUTE_ZERO
```

