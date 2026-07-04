# Fehlerkatalog

## Inhaltsverzeichnis

- [Amount-Fehler](#src-amount)
  - [Währungskonflikt zwischen Beträgen](#err-amount-currency-mismatch)
- [BankTransactionFileValidator-Fehler](#src-bank-transaction-file-validator)
  - [Transaktionsdatum außerhalb des Auszugszeitraums](#err-bank-transaction-file-date-out-of-statement-period)
  - [Abweichung des Gesamtbetrags des Auszugs](#err-bank-transaction-file-statement-total-amount-mismatch)
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

- Die monetäre Operation ist fehlgeschlagen, da die beteiligten Beträge in unterschiedlichen Währungen ausgedrückt sind: 127.33 EUR und 57689 USD. _(Währungskonflikt)_

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

- Die auf 2024-02-02 datierte Transaktion liegt außerhalb des Auszugszeitraums [2024-01-05;2024-01-31]. _(Das Transaktionsdatum liegt außerhalb des Auszugszeitraums.)_

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

- Der deklarierte Gesamtbetrag des Auszugs (1250 EUR) stimmt nicht mit dem aus den Transaktionen berechneten Gesamtbetrag (1249.5 EUR) überein. _(Abweichung des Gesamtbetrags des Auszugs.)_

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

- Failed to instantiate temperature: the value -1 K is below absolute zero. _(Temperature is below absolute zero.)_
- Failed to instantiate temperature: the value -280 °C is below absolute zero. _(Temperature is below absolute zero.)_

