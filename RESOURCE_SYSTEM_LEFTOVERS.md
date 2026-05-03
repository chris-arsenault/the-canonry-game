# Resource System Leftovers

Hex resources and wealth are no longer part of the current game design, but
resource primitives still exist in several engine and domain layers.

Treat these as leftover infrastructure, not available gameplay vocabulary, when
designing new generators, gambits, cards, or low-altitude behavior. Do not add
new content that depends on location resources or wealth unless the resource
system is intentionally reintroduced first.

Known leftover areas to audit or remove later:

- Resource ids and default catalog registration.
- Location resource preconditions, expressions, and mutations.
- Kernel resource storage and snapshot support.
- Tests that verify resource primitives rather than current gameplay.
