# Temperature below absolute zero

- **Code:** `TEMPERATURE_BELOW_ABSOLUTE_ZERO`
- **Source:** `Temperature`

This error occurs when trying to instantiate a temperature with a value that is below absolute zero.

> **Business rule:** Temperature cannot go below absolute zero because absolute zero is the point where particles have minimum possible energy.

## Diagnostics

- **The value entered manually by a user is invalid.** — _origin:_ External — Verify the value entered by the user and assess its compliance with domain rules.
- **The value received from an external system (API, message, etc.) is invalid.** — _origin:_ External — Check the data provided by the upstream system and evaluate its validity against domain rules.
- **The value was loaded from corrupted or outdated persisted data.** — _origin:_ External — Examine the persisted data source to determine whether stored values comply with current domain rules.
- **The value was computed internally without using domain-safe methods.** — _origin:_ Internal — Inspect the internal computation logic to confirm that domain invariants are preserved.
- **The value originates from system configuration or defaults that are incorrect or outdated.** — _origin:_ External — Review the relevant configuration or default parameters to assess their compliance with domain rules.

## Examples

- Failed to instantiate temperature: the value -1 K is below absolute zero. _(Temperature is below absolute zero.)_
- Failed to instantiate temperature: the value -280 °C is below absolute zero. _(Temperature is below absolute zero.)_

