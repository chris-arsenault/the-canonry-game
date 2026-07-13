# Handoff: kernel fixed-capacity pools are lifetime budgets

**Symptom:** 100k-tick Monte Carlo runs crash mid-simulation with a
parameterless `InvalidOperationException`. 25k runs pass. Repro:

```bash
dotnet run -c Release --project tools/MonteCarlo/MonteCarlo.csproj -- \
  run --run-id cap-repro-<date> --runs 1 --ticks 100000 --seed 7900
```

Fails within a few minutes of simulation (not at startup). The MC CLI now
prints the full stack for `InvalidOperationException` (fixed in `ef144ec`).

**Mechanism (confirmed for one pool):** relationship slots are never
recycled. `WorldState.CullRelationship` sets `_relationshipActive = false`
but leaves `_relationshipAllocated = true` (culled edges are the queryable
historical record), and `FindRelationshipSlot`
(`WorldState.RelationshipIndexes.cs:25`) only reuses unallocated slots. So
`SimulationOptions.RelationshipCapacity` is a budget for every relationship
*ever created* in a run, not for live edges.

**State:** capacities were raised in `ef144ec` (relationships 8192→65536,
entities 4096→16384) and the run *still* fails with a similar stack — the
top frame was lost to output truncation, so the second throw site is
unconfirmed. Candidates: another never-recycled fixed pool
(`MinorActorCapacity` 4096, provenance output buffers), or runaway
relationship creation — check whether the new `allies_answer_the_call`
generator (`IceRemembersGeneratorDefinition.WarSaga.cs`) re-creates
duplicate `at_war_with` edges on every fire for the same trio (its target
precondition stays true after firing; generator `R(...)` bundles may not
dedupe/replace existing edges).

**Wanted:** (1) capture the uncut stack from the repro; (2) fix the actual
exhaustion — dedupe/replace on generator relationship bundles if that's the
leak, and/or recycle or evict historical slots; (3) design note: the
full-game horizon is ~2.5M ticks, so lifetime-total pools need an eviction
or archival story, not just larger arrays. Kernel discipline applies:
`docs/kernel/simulation-kernel-discipline.md` (fixed capacity, no
allocations, primitive events).
