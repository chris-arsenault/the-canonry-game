# Attribute Model

This document records the retired bucket model so it does not creep back into
new content. The active model is health plus traits.

## Active Rule

Each active entity kind declares exactly one mutable scalar health attribute.
All other per-entity differentiation should be authored as one of:

- a trait for mostly static behavior identity;
- relationship strength for graph-state gates and magnitude;
- relationship facts for edge-specific provenance or event meaning;
- locality queries for physical or semantic context;
- resources or map signals for aggregate world context.

## Retired Buckets

The old buckets were Health, Need / Drive, Capacity, Projection, and Alignment.
Only Health remains as an attribute bucket. Need, capacity, and projection
proved too generic: they became numeric personality slots that generators and
gambits could lean on without producing readable graph state.

Do not revive old attributes such as `echo`, `warmth`, `dominion`, `shelter`,
`reach`, `provisions`, `holding`, `pristiness`, `power`, or `resonance` as a
generic second stock. If one of those concepts matters, re-author it as a
specific trait, relationship, relationship fact, locality rule, resource, or
map signal.

## Numeric Gates

When a generator or gambit needs a number, prefer relationship strength first.
For example, artifact cleansing should require a strong relevant edge such as
`anchored` or `holds` above an authored threshold, plus any health or locality
requirements the event needs.

Locality gates are also valid: "near a scar", "inside the target region",
"within a threatened map signal", or "on the semantic margin" are often better
than inventing a scalar attribute.
