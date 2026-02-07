# Writing Errors Guide

DiagnosableExceptions gives you tools.
This guide helps you use them in a consistent and meaningful way.

The goal is not just to throw exceptions, but to **express errors clearly, precisely, and usefully** for humans.

## 🎯 1. Think in *error situations*, not just failures

Each documented error should represent:

> **one specific situation in which the system cannot proceed as expected**

Avoid vague or generic errors such as:

* “Invalid operation”
* “Processing error”
* “Unexpected issue”

Prefer precise, contextual situations:

* “Amount currency mismatch”
* “Temperature below absolute zero”
* “Transaction date outside statement period”

An error should describe *what went wrong in domain terms*, not how the system reacted.

## 🏷️ 2. Writing a good **error code**

The error code is the stable, machine-readable identifier.

Good practices:

* Use a **clear domain scope**
  `AMOUNT_CURRENCY_MISMATCH`
* Keep it **stable over time**
* Avoid technical details (no class names, no method names)
* One code = one documented error situation

Think of the error code as an API contract.

## 🧾 3. Writing the **Title**

The title is a short human summary.

It should:

* be concise
* describe the situation, not the consequence
* avoid technical wording

Good:

* “Amount currency mismatch”
* “Temperature below absolute zero”

Avoid:

* “InvalidAmountOperationException”
* “Operation failed”

## 📝 4. Writing the **Description**

The description explains what the error means.

A good pattern is:

> “This error occurs trying to…”

or

> “This error occurs when…”

You may choose the phrasing that fits best, but remain consistent within the project. Consistency in wording improves readability and makes the documentation feel cohesive.

The description should:

* describe the situation in plain language
* be understandable by someone who does not know the code
* explain *what* happened, not *how the system reacted*

## 📏 5. Writing the **Rule**

The rule expresses the invariant or business constraint.

It should:

* be stated as a general truth
* describe what must always hold

Examples:

* “All monetary operations must involve amounts expressed in the same currency.”
* “Temperature cannot go below absolute zero.”

If no explicit rule exists, it is acceptable to omit it.

## 🔍 6. Writing a good **Cause**

A cause describes a plausible reason the error occurred.

It should:

* describe a **state or condition**, not an action
* avoid blaming
* be specific enough to guide investigation

Good:

* “Amounts were used in a monetary operation without having been converted to the same currency.”

Avoid:

* “The developer forgot to convert the currency.”
* “Fix the data.”

## 🧭 7. Writing a good **AnalysisLead**

An analysis lead suggests where to look first.

It should:

* start with a neutral verb such as *Verify*, *Check*, *Review*
* guide investigation, not define procedures
* avoid ticketing or support process details

Good:

* “Verify whether all amounts involved in the operation were converted to a common currency before being used together.”

Avoid:

* “Open a ticket.”
* “Contact team X.”

## 🧪 8. Writing good **Examples**

Examples illustrate how the error appears in practice.

They should:

* use realistic values
* be simple and clear
* highlight the rule violation, not edge cases

Examples are not tests — they are educational.

## 🧠 9. Separate domain from technical noise

Error documentation should focus on:

* domain meaning
* violated rules
* plausible causes

Avoid leaking:

* stack traces
* framework details
* internal class names

## 🏁 Summary

When writing errors:

| Element       | Purpose                  |
| ------------- | ------------------------ |
| Error code    | Stable identifier        |
| Title         | Short human summary      |
| Description   | What the error means     |
| Rule          | The violated invariant   |
| Cause         | Why it may have happened |
| Analysis lead | Where to investigate     |
| Examples      | How it looks in reality  |

Well-written errors are not just thrown.
They become part of the **shared understanding of how the system works — and fails.**

---

Previous section: [Core Concepts](CoreConcepts.en.md) | Next section: [Usage Patterns](UsagePatterns.en.md)

---