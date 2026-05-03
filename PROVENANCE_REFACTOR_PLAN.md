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

Status: implemented for generator fires. Relationship-gated generators now bind
the concrete trigger relationship and use that relationship's creator
invocation as the parent when present. Entity-only generator triggers bind the
target entity.

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

Status: implemented for first-pass gambit provider evidence without adding a
separate evidence table. Gambit invocations use primary entity for actor,
secondary entity for selected target, and trigger fields for concrete provider
relationship, selected provider attribute, or selected entity evidence.

Tasks:

- Add bounded provenance input/evidence storage if output rows are not enough
  to model "used this relationship/capability".
- For gambits, bind provider selections that matter narratively:
  - selected actor
  - selected target
  - selected provider relationship, when one made the gambit actionable
  - selected ability or relationship kind, when used by the action
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

Status: implemented for Ice Remembers teaching lineage. `taught_by` is now an
ordinary relationship, Ice Remembers registers no relationship facts, and the
startup ability lineage no longer emits relationship facts. Generic relationship
fact plumbing remains until saturation and any remaining edge-qualifier uses are
retired or justified.

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

## Phase 4: Replace Fact-Based Saturation With Causal Spend

The stable replacement for saturation should not be another ledger, fact table,
or hidden runtime memory system. Saturation exists today because generator
eligibility can remain true after a generator fires, causing repeated quick
fires against the same situation. The replacement is to make generator firing
consume, relieve, or transform the gameplay condition that made the event
eligible.

In other words: a generator should not ask "has this target been saturated?" It
should ask "is there enough visible world state here to justify this event, and
what does this event spend or change so the same situation is no longer
immediately eligible?"

Specific consume/transform mappings are content design work and should be
locked down from the actual available gates, effects, relationships, map
signals, terrain, coverage, and statuses in the codebase. This phase only locks
the direction: no replacement saturation ledger.

Chosen first-pass causal spend model:

- Do not treat "consume the trigger edge" as universal. A generator may break a
  trigger relationship only when the event actually resolves that relationship.
  Otherwise the spend should preserve the relationship and change its intensity
  or leave a durable output-state gate.
- Add trigger relationship strength gates and effects:
  - relationship-gated generators can require the selected trigger relationship
    to be above a minimum strength;
  - `ModifyTriggerRelationshipStrength` applies a delta to that concrete
    relationship id;
  - for conceptually paired relationships such as `at_war_with` and
    `trade_route`, it applies the same delta to the reciprocal relationship
    between the same endpoints when present;
  - strength spend is allowed only for relationship kinds with an ordinary
    maintenance path that can rebuild strength. In Ice Remembers this fits
    `at_war_with` through agency-conflict maintenance and `trade_route` through
    spatial/influence maintenance.
- Extend `RemoveTargetRelationships` for resolving events:
  - by default it keeps the existing broad target-kind behavior;
  - when configured with trigger-relationship instance selection, it removes
    the concrete relationship id selected as trigger evidence;
  - reciprocal removal is explicit configuration for paired relationships;
  - it leaves unrelated same-kind relationships intact.
- Add negative target relationship gates for real output-state locks, such as
  "this penguin does not already have a home base" or "this region does not
  already have an anchored relic." This is not a ledger; it is ordinary world
  state that can later decay or be broken by relationship maintenance.
- Do not add influence spend in the first pass. War and trade already have
  low-altitude/gambit mechanisms that can create or rebuild the trigger
  relationships. Add direct influence reduction only if simulation evidence
  shows rapid re-trigger loops after relationship-strength spend lands.

Chosen Ice Remembers mappings:

| Generator | Current persistent cause | Causal spend / transform |
| --- | --- | --- |
| `war_makes_wardens` | A concrete `at_war_with` trigger edge on the target faction. | Require a strong selected war relationship, then reduce that war pair's strength. The war is not ended; its current pressure is spent into a warden, holdfast, and optional weapon. |
| `ice_settles_accounts` | A concrete `at_war_with` trigger edge plus an offered penguin from the target culture. | Keep the offered penguin historical effect. Replace target-wide war removal with breaking the selected `at_war_with` pair. This is the catalog's war-ending generator. |
| `war_breaks_the_land` | A concrete `at_war_with` trigger edge on the target faction. | Require a very strong selected war relationship, reduce that war pair's strength, and keep the scar coverage / wastes terrain transform. The war is spent into durable land damage, not automatically ended. |
| `what_the_spring_remembers` | A concrete incoming `cleansed_by` trigger edge on the region. | Require the region to have no existing incoming `anchored` relationship. The event converts a cleansed place into an anchored relic/witness/sanctuary state; the durable anchor prevents immediate repeats and can decay or be broken like any relationship. |
| `routes_draw_readers` | A concrete `trade_route` trigger edge from the settlement. | Require a strong selected trade route, then reduce that route pair's strength. The route is not destroyed; route intensity/wealth is spent into a broker, harbor, and possible order/instrument. Spatial/influence maintenance can rebuild it. |
| `what_the_artifact_knows` | A concrete `holds` trigger edge from the penguin to an artifact. | Require the penguin to have no existing outgoing `home_base` relationship. The event gives the penguin a rookery/home base, which blocks immediate repeats through real world state. Do not break `holds` unless a later trigger-counterpart binding lets the held artifact become the anchored seal directly. |

Tasks:

- Audit every generator with `TargetSaturation` and identify the persistent
  eligibility condition that currently survives firing.
- For each generator, choose a causal spend/transform of existing gameplay
  state, such as:
  - removing or weakening the trigger relationship;
  - transforming the target, terrain, coverage, or produced locality;
  - changing health/status where the event meaning justifies it;
  - moving an artifact/entity out of the state that made the generator
    eligible.
- Add only the shared query/effect primitives needed to express those real
  gameplay transformations. Do not add a generic "recently served" state,
  per-target counter, or target-cluster table.
- Do not introduce or preserve relationship kinds whose primary purpose is to
  gate a generator or record that one boutique event happened. A relationship
  kind used in generator eligibility must have independent graph value: it
  should be meaningful to other systems, support traversal and narrative
  queries, affect low-altitude behavior, or describe a reusable world relation.
  If a relationship would only mean "this output was already produced here,"
  it is saturation by another name and should be rejected.
- Add compiler or lint checks that persistent generator gates are paired with a
  declared consume/transform effect, unless the generator is explicitly
  justified as safe to repeat.
- Remove `TargetSaturation`, `RequiredFactId`, and relationship-fact dependency
  from generator saturation paths after the causal replacements are in place.
- Treat durable duplicate-output prevention as a separate modeling problem. If
  a duplicate must be impossible, represent that impossibility in ordinary world
  state and generator gates, not in a saturation subsystem.
- Migrate the Ice Remembers generator catalog one generator at a time, with
  tests that prove:
  - immediate repeat firing is suppressed because the cause was changed;
  - the generator can become eligible again if gameplay rebuilds the cause;
  - unrelated new targets or newly created causes remain eligible.

Exit criteria:

- There is no runtime saturation ledger or fact-backed saturation mechanism.
- Repeated quick firing is prevented by changed gameplay state, not by hidden
  memory.
- New targets and newly created causes can trigger generators immediately.
- Existing anti-repeat behavior is preserved where it is still desired.
- Any remaining output uniqueness rule is expressed through ordinary gates and
  world state, not called saturation.

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
