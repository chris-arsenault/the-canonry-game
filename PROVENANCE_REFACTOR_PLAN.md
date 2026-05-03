# Provenance, Saturation, and Relationship Fact Refactor Plan

This plan captures the remaining work after introducing kernel-level provenance
invocations and outputs. The intent is to make provenance the durable source of
production history, retire relationship facts where they are only standing in
for saturation or provenance, and prepare the chronicle layer for explicit
event-chain queries.

## Current Baseline

- `WorldState` owns authoritative provenance history.
- Kernel events carry `ProvenanceInvocationId`.
- `ProvenanceInvocationRecorded` provides a compact event-stream signal for
  projections.
- Generator fires and gambit executions open provenance scopes.
- Mutations inside a provenance scope record output rows.
- Entity and relationship creation have direct creator invocation lookups.
- Provenance state is preallocated, append-only, snapshot persisted, and fails
  with typed capacity errors when exhausted.

## Design Decisions

- The event stream is projection fuel, not the durable source of provenance.
- The kernel provenance tables are the source of truth for history queries.
- Relationship facts should not carry provenance.
- Relationship facts should not be required to express saturation.
- Chronicle chain highlights should be curated recognizers over provenance and
  world outputs, not post-hoc parsing of prose or generic narrative tags.

## Phase 1: Bind Trigger and Parent Provenance

Parent and trigger fields exist, but generator and gambit integrations do not
yet bind specific trigger relationships or input relationships into them.

Tasks:

- Extend generator target selection to carry the concrete trigger evidence that
  caused the fire.
- When a generator fires because of a required relationship, bind:
  - `TriggerKindCode = Relationship`
  - `TriggerId = relationshipId`
  - `ParentInvocationId = RelationshipCreatedByInvocationId(triggerId)` when
    present.
- When a generator fires because of an entity-only trigger, bind:
  - `TriggerKindCode = Entity`
  - `TriggerId = targetEntityId`
- Preserve no-allocation kernel invariants while passing trigger evidence
  through targeting, retry, and fire paths.
- Add tests for a relationship-triggered generator where the produced invocation
  points back to the invocation that created the triggering relationship.

Exit criteria:

- A relationship-created event can be followed to a generator invocation that
  used that relationship as its trigger.
- Betrayal -> at-war-with -> war-occurrence style chains can be queried without
  reading narrative text.

## Phase 2: Capture Action and Provider Evidence

K-hop chronicle chains need explicit evidence for what an action used, not just
who acted and what changed.

Tasks:

- Add bounded provenance input/evidence storage if output rows are not enough
  to model "used this relationship/capability".
- For gambits, bind provider selections that matter narratively:
  - selected actor
  - selected target
  - selected provider relationship, when one made the gambit actionable
  - selected ability/resource/relationship kind, when used by the action
- Prefer concrete relationship ids over semantic role slots.
- Keep role meaning in producer/gambit definitions, not in arbitrary provenance
  key names.

Exit criteria:

- A slaying gambit can prove which practiced ability or provider relationship
  made the slaying possible.
- A taught-by chain can join teacher -> student -> ability use without bespoke
  ad hoc metadata.

## Phase 3: Retire Relationship Facts as Provenance

Facts currently look like a general relationship annotation system, but the
active use is mostly saturation and the first non-saturation case does not fit.

Tasks:

- Audit all `RelationshipFactConfig`, `RelationshipFactId`, fact arrays, and
  fact predicates.
- Identify any fact that represents actual edge qualification and should remain
  as a boolean edge qualifier.
- Migrate `taught_by` away from facts.
- Remove IceRemembers relationship fact definitions if `taught_by` is the only
  live fact.
- Remove generated relationship facts from seed data after replacement.
- Remove fact-based chronicle/provenance assumptions from docs.

Exit criteria:

- No provenance or teaching lineage depends on relationship facts.
- Any remaining relationship fact has a clear boolean edge-qualifier use case.

## Phase 4: Replace Fact-Based Saturation

Saturation should be explicit generator behavior, not hidden behind relationship
facts.

Tasks:

- Add explicit saturation modes to generator data/config, such as:
  - max runs per target
  - max active outputs per target
  - max active relationships by produced relationship kind and endpoint
  - optional output-signature matching when a generator creates multiple edge
    types.
- Make saturation count real world outputs or provenance outputs where needed.
- Remove `RequiredFactId` from saturation after equivalent behavior exists.
- Update generator catalog invariants so saturation must match produced output
  definitions directly.
- Migrate existing definitions to the new saturation fields.

Exit criteria:

- Saturation remains deterministic and cheap.
- Existing anti-duplication behavior is preserved.
- Saturation no longer requires relationship facts.

## Phase 5: Chronicle Chain Query Layer

The chronicle needs both 1-hop production details and curated k-hop story
chains.

Tasks:

- Add read-only provenance query helpers for:
  - invocation by id
  - outputs by invocation
  - created-by relationship/entity lookup
  - parent invocation traversal
  - relationship-trigger traversal
- Define a small chronicle pattern layer outside kernel mutation logic.
- First pattern candidates:
  - teacher taught student, student later used ability
  - betrayal produced hostility, hostility triggered war
  - war occurrence produced death/scar/memorial
- Keep pattern definitions downstream of simulation state.
- Cache derived chronicle chains outside authoritative world mutation state
  unless a query proves too expensive.

Exit criteria:

- Chronicle can render 1-hop details from provenance invocations.
- Chronicle can highlight named k-hop chains without semantic enrichment over
  prose.

## Phase 6: Migration and Deletion

After replacement behavior is tested, remove obsolete surface area.

Tasks:

- Delete unused relationship fact config paths.
- Delete unused kernel fact storage if no valid edge-qualifier use remains.
- Delete domain/compiler fact plumbing only after all content and tests no
  longer need it.
- Update docs:
  - relationship system
  - generator executor
  - action executor
  - event log/projection notes
  - narrative/chronicle design notes
- Remove or rewrite `TAUGHT_BY_DESIGN.md` once the real design is implemented.

Exit criteria:

- The codebase has one durable provenance system.
- Saturation is explicit.
- Relationship facts either disappear or are reduced to a small, justified
  boolean edge qualifier mechanism.

## Testing Requirements

- Kernel no-allocation checks continue to pass.
- Provenance capacity exhaustion tests cover invocation and output capacity.
- Snapshot tests prove provenance history survives save/load.
- Generator tests cover trigger relationship binding.
- Gambit tests cover provider evidence binding.
- Domain graph tests cover saturation migration.
- Chronicle tests cover at least one k-hop chain recognizer.

## Open Questions

- Whether provenance needs a separate input/evidence table or whether trigger
  and output rows are enough for the first chronicle chains.
- Whether any relationship facts remain after saturation and `taught_by`
  migration.
- Whether occurrence entities should link to provenance invocations directly or
  only through their creation output.
