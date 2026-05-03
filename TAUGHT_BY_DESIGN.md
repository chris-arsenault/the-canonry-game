# Ability Transmission: `taught_by` Should Be a Relationship, Not a Fact

Status: superseded by the Phase 3 implementation. `taught_by` is now an
ordinary Ice Remembers relationship, ability seed lineage emits `taught_by`
edges, and Ice Remembers no longer registers relationship facts. This document
is historical context for why the fact-based shape was rejected.

## Context

Just shipped the `ability` entity kind to `IceRemembers` (Asset / Semantic /
health=`clarity`). Abilities are combat techniques, magical rites, and
technologies — represented as semantic-primary entities that can be held by
multiple agents at once.

To replace the misfit `holds` relationship for ability ownership (which has
`TargetFollowsSourcePhysical = true`, incoherent for multi-holder
semantic-primary assets), I added a new `practiced_by` relationship:

- Source = Asset (the ability)
- Target = Agency (the practitioner)
- Polarity = Positive
- `IsExclusive` = false (multi-holder)
- `TargetFollowsSourcePhysical` = false
- Maintenance: `AbilityPracticeMaintenance()` — `AlwaysDecay(0.0006f)` plus
  small `SameCulture(0.0003f)` bonus
- `CullThreshold` = 0.05f

That part is correct and should stay.

## The Problem

I also added `taught_by` to record transmission lineage — i.e., when a
practitioner learned the ability from another practitioner rather than
originating it independently. I implemented it as a **relationship fact**
(a tag attached to a `practiced_by` edge), because that matched the framing I
had earlier proposed and got user-confirmed.

This is wrong. The infrastructure for facts is `RelationshipFactConfig` with
only `Name`/`DisplayName`/`Description` — facts are pure tags, with no field
to carry a value. The teacher's identity therefore cannot be preserved on
the fact. All my fact-only `taught_by` can do is set a binary
"inherited vs. originated" flag on the practitioner's edge.

Everything actually useful about transmission needs the teacher's identity:

- chains of who taught whom
- severing a lineage at a chokepoint
- blaming a specific teacher when a corrupted variant spreads
- gating "challenge your teacher" gambits

A fact-only version supports none of this. The narrow case where a fact
*would* be the right shape — "this was taught but the teacher is now dead
and culled from the graph" — is a fallback, not the primary representation.

## The Fix

Replace the fact with a separate **relationship**, where the teacher's
identity is preserved naturally as the source of the edge.

Proposed shape:

| Field | Value |
|---|---|
| Name | `taught_by` |
| Source | Agency (the student/learner) |
| Target | Agency (the teacher) |
| Polarity | Positive |
| `IsExclusive` | false (a student can have multiple teachers across abilities) |
| `IsSymmetric` | false |
| `TargetFollowsSourcePhysical` | false |
| `CullThreshold` | 0.02f (cull when the bond truly fades; lineage record matters) |
| Maintenance | low decay; possibly `ContactMemory` so co-located teacher/student bonds resist decay |

Direction note: I picked `student → teacher` (Agency source → Agency target)
rather than `teacher → student` because:

- Per-student introspection ("who taught me X?") is the more common query
- Matches `led_by` shape (subordinate → leader)
- A teacher's death/cull doesn't reach back to delete the student's record;
  the student's edge to a culled teacher just becomes orphan/stale
  (which is the "tradition outlives its founder" beat we wanted from the
  fact-only fallback)

Important: `taught_by` is on the **student**, not on the ability edge. The
relationship is "Penguin A was taught by Penguin B" — it doesn't mention the
ability directly. To know "what was A taught by B?" you intersect: abilities
where both A and B have `practiced_by` edges, scoped by the relevant
`taught_by` edge.

If we want per-ability lineage attribution (e.g., "A was taught Wake-Song
specifically by B, but Ice-Reading by C"), we need a different shape: either
(a) a relationship fact attached to `practiced_by` carrying the teacher id —
which still requires extending `RelationshipFactConfig` to support values, or
(b) a `taught_<ability>_by` per-ability relationship variant, which doesn't
scale.

For the first pass, the per-student `taught_by` relationship is enough. It
expresses transmission lineage without ability-level attribution. We can
revisit per-ability attribution later if gambits require it.

## What Currently Exists That Needs Changing

### Schema — `src/IceRemembers/IceRemembersSchema.cs`

**Keep:**
- `IceRemembersRelationship.PracticedBy` enum entry and `"practiced_by"` in
  `DomainNames`

**Change:**
- Add `IceRemembersRelationship.TaughtBy` enum entry and `"taught_by"` in
  `DomainNames`
- Remove `IceRemembersRelationshipFact` enum and
  `IceRemembersRelationshipFactExtensions` class entirely (the only fact it
  registered was `TaughtBy`, which is becoming a relationship)

### Definition — `src/IceRemembers/IceRemembersDefinition.cs`

**Keep:**
- The `practiced_by` `RelationshipConfig` entry in `CreateRelationships()`
- The `AbilityPracticeMaintenance()` helper

**Change:**
- Add a `taught_by` `RelationshipConfig` entry in `CreateRelationships()`
  matching the shape above (agency → agency, positive, non-exclusive,
  non-symmetric, low cull threshold, light maintenance — probably reuse
  `AgencyBondMaintenance()` or define `TaughtByMaintenance()` with
  `AlwaysDecay(0.0003f)` + `SameCulture(0.0004f)` + optional
  `ContactMemory(10f, 0.0008f)`)
- Revert `CreateRelationshipFacts()` to `=> []` (or leave it returning the
  list if you intend to register other facts in the future, but remove the
  `taught_by` fact entry)

### Seed — `src/IceRemembers.Seed/IceRemembersSeedBuilder.NewKinds.cs`

The current `CreateAbilityPractitioner` helper creates a `practiced_by` edge
and tags secondary holders with the `taught_by` fact:

```csharp
var facts = taughtBy
    ? new[] { IceRemembersRelationshipFact.TaughtBy.ToDomainName() }
    : null;

seed.CreateRelationship(
    IceRemembersRelationship.PracticedBy.ToDomainName(),
    RelationshipPolarity.Positive,
    ability.SeedEntityId,
    holder.SeedEntityId,
    strength,
    facts);
```

This needs to become two separate relationship creations for the secondary
holder: a `practiced_by` edge with no fact, plus a separate `taught_by` edge
from the secondary holder (student) to the primary holder (teacher).

The seed plans currently are:

| Ability | Primary holder | Secondary holder |
|---|---|---|
| Ice-Reading | Captain Greyfeather | Elder Deepcurrent |
| Fire-Core Craft | Trader Shellback | Captain Greyfeather |
| Wake-Song | Singer Underwake | Elder Deepcurrent |

After the fix, the seed should produce:

- Three `practiced_by` edges, one per primary holder, no facts
- Three `practiced_by` edges, one per secondary holder, no facts
- Three `taught_by` edges: Elder Deepcurrent → Captain Greyfeather (for
  Ice-Reading), Captain Greyfeather → Trader Shellback (for Fire-Core
  Craft), Elder Deepcurrent → Singer Underwake (for Wake-Song). Note that
  the per-student `taught_by` relationship doesn't reference the ability;
  the ability is implied by intersecting the `practiced_by` edges of the
  pair.

Suggested helper redesign:

```csharp
private static void CreateAbilityPractitioner(
    SeedAssembly seed,
    SeedEntity ability,
    string holderName,
    float strength)
{
    var holder = ResolveEntityByName(seed, holderName);
    seed.CreateRelationship(
        IceRemembersRelationship.PracticedBy.ToDomainName(),
        RelationshipPolarity.Positive,
        ability.SeedEntityId,
        holder.SeedEntityId,
        strength);
}

private static void CreateTeachingLineage(
    SeedAssembly seed,
    string studentName,
    string teacherName,
    float strength)
{
    var student = ResolveEntityByName(seed, studentName);
    var teacher = ResolveEntityByName(seed, teacherName);
    seed.CreateRelationship(
        IceRemembersRelationship.TaughtBy.ToDomainName(),
        RelationshipPolarity.Positive,
        student.SeedEntityId,
        teacher.SeedEntityId,
        strength);
}
```

Caller becomes:

```csharp
foreach (var plan in AbilityPlans)
{
    // ... ability entity creation, anchor, etc. ...
    CreateAbilityPractitioner(seed, ability, plan.PrimaryHolderName, 0.85f);
    CreateAbilityPractitioner(seed, ability, plan.SecondaryHolderName, 0.7f);
    CreateTeachingLineage(seed, plan.SecondaryHolderName, plan.PrimaryHolderName, 0.75f);
}
```

Note: a single `taught_by` edge per pair is created across all three
abilities even though some pairs (Captain Greyfeather ↔ Elder Deepcurrent)
appear in multiple plans. The relationship is non-exclusive, so multiple
edges between the same pair are allowed; or we can deduplicate in
`CreateTeachingLineage` if the relationship-graph layer rejects duplicates.

### Docs — `docs/systems/entity-kind-registry.md` and similar

**Add** a brief note in the relationship vocabulary section listing
`practiced_by` (asset → agency) and `taught_by` (agency → agency).

## Verification

Run `make ci` from the repo root. No parallel tests in this repo.

## Why I Got It Wrong

I had earlier proposed `taught_by` as "a fact that annotates the
`practiced_by` edge with the teacher entity id." When I implemented and
discovered facts can't carry values, I downgraded to a tag-only fact rather
than re-architecting to a relationship. I optimized for "least change to
the prior plan" instead of "best fit for the actual requirement." The user
called this out, and on reflection the relationship is clearly the right
shape — it preserves teacher identity for free via the source/target
endpoints, and the fact-only version supports almost none of the
transmission semantics it was meant to enable.
