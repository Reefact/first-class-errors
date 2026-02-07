# When Not to Use DiagnosableExceptions

DiagnosableExceptions is designed for systems where errors carry meaning, rules, and operational consequences. It is not intended to be used everywhere.

## 🧪 Prototypes and throwaway code

If the code:

* is short-lived
* exploratory
* not meant to be maintained

then the overhead of structured error documentation is unnecessary.

## 🧩 Very small utilities

For simple tools or scripts where:

* there is no support process
* errors are only for developers
* the system has no domain complexity

standard exceptions are usually sufficient.

## ⚙️ Low-level technical libraries

Libraries that deal primarily with:

* memory
* threading primitives
* serialization internals
* protocol implementations

often benefit more from technical exceptions than semantic documentation.

This library is about expressing **application-level meaning**, not low-level mechanics.

## 🚀 Performance-critical inner loops

In extremely performance-sensitive paths, creating rich exception objects purely for control flow may not be appropriate.

In such cases:

* use lightweight validation
* avoid error object creation unless necessary

## 🔄 Systems without long-term ownership

If there is:

* no support team
* no operational investigation
* no need for traceable error knowledge

then the documentation pipeline provides limited value.

## 🎯 Rule of thumb

Use DiagnosableExceptions when:

* errors represent rules or constraints
* systems are long-lived
* multiple teams interact with the software
* support and operations need insight

Avoid it when errors are merely technical signals with no lasting semantic meaning.

The goal of this library is not to make every exception richer. It is to make meaningful errors explicit and durable.

---

Previous section: [Design Principles](DesignPrinciples.en.md) | Next section: [Core Concepts](CoreConcepts.en.md)

---