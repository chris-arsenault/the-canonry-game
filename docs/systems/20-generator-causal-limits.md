# System 20: Generator Causal Limits

Generator causal limits replace saturation. They are not a second ledger,
cooldown table, or boutique gate. They are authored gameplay
feedback: when a generator fires, the world state that justified the fire must
be consumed, relieved, transformed, or locked by ordinary graph state.

Slot occupants receive a kernel-level causal-spend limit multiplier. That
multiplier raises the amount of ordinary causal spend a slot occupant can
absorb before becoming ineligible, but it does not remove the causal-limit
contract: every generator still needs a meaningful cause, spend/transform, and
reopen path.

## Problem

A generator can stay eligible after it fires. If nothing about the eligible
situation changes, the same generator can repeatedly produce the same event
package against the same target cluster. Legacy saturation solved that by
counting outputs. That bounded repetition, but it did not make the simulation
more meaningful: it was a hidden cap dressed as world logic.

The replacement must keep the useful behavior while preserving an open-ended
simulation. New targets and newly created causes must become eligible
immediately. Old causes should stop firing only because the game world changed.

## Definition

A causal limit has three parts:

- **Cause:** the specific existing world condition that makes the generator
  eligible.
- **Spend or transform:** the concrete mutation caused by firing.
- **Reopen path:** the normal gameplay loop that can recreate or strengthen the
  cause later.

If a proposal cannot name all three, it is not a causal limit.

## Hard Rules

1. A stricter target query is not enough. Adding another required entity,
   relationship, subtype, or locality filter only narrows eligibility; it does
   not prevent repeated firing unless the generator changes that same condition.
2. A secondary target is not a spend unless it is consumed, transformed, moved
   out of eligibility, or given an ordinary output-state lock.
3. Do not add relationship kinds whose main purpose is to remember that a
   generator already fired. That is saturation by another name.
4. Do not add per-generator counters, recent-run ledgers, or hidden target-cluster
   memory.
5. Do not spend a trigger relationship just because it is available. Break or
   weaken a relationship only when the event meaning says that relationship was
   relieved, consumed, damaged, or resolved.
6. A durable output-state lock is valid only when it is itself meaningful world
   state. For example, "this penguin now has a home base" is valid because the
   output changes graph behavior. "this target was already served by this
   generator" is not.
7. A map or terrain gate is valid only when the generator changes the map or
   consumes finite spatial opportunity in the gated locality.

## Decision Procedure

For every generator with a persistent gate, answer these questions before
choosing an implementation:

1. What condition remains true after the generator fires today?
2. What exact part of that condition causes the repeat problem?
3. What mutation would make that condition false or weaker immediately after
   firing?
4. Which existing low-altitude, gambit, relationship-maintenance, terrain, or
   lifecycle process can rebuild it later?
5. Would newly created targets or newly created causes still be eligible without
   waiting for a hidden run counter?

If the answer to question 3 is "add another target," reject it and look again.

## Declaration Contract

Each authored generator must declare a `CausalLimit` string in plain language.
This is not compiler-proofed semantics. It is a design note and review hook for
the agent or human changing the generator. The invariant test only checks that
the declaration exists and points back to this document when it is missing.

The declaration should name the cause and the spend/transform in one sentence,
for example: "Depends on high trade-route strength and reduces the selected
trade route strength."

The catalog invariant is intentionally shallow: each Ice Remembers generator
must have non-empty `CausalLimit` text. The declaration keeps the causal design
visible during review without adding a second semantic validator.

## Valid Patterns

- **Relationship pressure spend:** require a strong relationship and reduce the
  selected relationship instance when the event spends intensity without ending
  the relationship.
- **Relationship resolution:** remove the selected relationship instance when
  the event actually resolves that relationship.
- **Output-state lock:** require absence of a normal relationship or state, then
  produce that state as part of the generator.
- **Terrain or coverage transform:** require local map state below or above a
  threshold, then transform terrain or consume finite placement opportunity.
- **Status or lifecycle transform:** change an entity's ordinary status when the
  event meaning is that the entity has been used up, retired, damaged, or moved
  out of the active pool.

## Invalid Patterns

- Add a same-culture secondary target and leave it unchanged.
- Require an extra settlement or artifact and leave the original cause unchanged.
- Create `founded_from`, `served_by`, `wardened_by`, or similar relationship
  kinds only to block later generator fires.
- Keep a max-count table or hidden repeat-control path under another name.
- Lower a broad class of relationships because the runtime cannot select the
  specific causal relationship.

## Ice Remembers Catalog

The active Ice Remembers generator catalog is source of truth for individual
causal-limit declarations. Do not maintain a parallel generator table in this
document; it goes stale as authored content changes. The durable invariant is
that every active generator has non-empty `CausalLimit` text and that reviewers
apply the decision procedure above when changing generator causes or outputs.

Historical removed generators are useful examples during design review, but
they should stay in commit history or research notes rather than this current
system contract.
