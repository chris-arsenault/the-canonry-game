# Structural Audit: Goal-Process-Execution Pipeline

**Date:** 2026-04-14
**Status:** Diagnosis complete, fix not yet started

## Summary

The goal system fuses **what an entity needs** with **who it should target** into a single object (`EntityGoal`). Provider selection happens at goal-generation time (every 10 ticks), before any process preconditions are consulted. The evaluator then discovers provider-process incompatibilities too late, causing entities to abandon high-urgency needs via fallthrough instead of finding a viable provider.

This is the root cause of the recurring "weird blockers" — entities stuck pursuing stale or incompatible goals, cycling through fallthrough attempts, or executing low-priority actions while high-priority needs go unmet.

---

## The Inversion

### Current flow

1. **GoalGenerator** detects attribute deficit (e.g., low population)
2. **GoalGenerator** finds all processes that write that attribute, pools all candidate providers across those processes, scores each with `RewardExpression`, creates goals as `(Attribute, Provider, Urgency)`
3. Goals persist for 10 ticks
4. **EntityEvaluator** picks a goal (weighted random by urgency), filters processes by `TargetAttribute` match, sets goal's `Provider` as `ResolvedTargetId`
5. Process preconditions evaluate against that pre-selected provider
6. If all processes fail for that goal, evaluator falls through to a **different goal** (different attribute), not a different provider

### Correct flow

1. Detect attribute deficit -> goal about **what** the entity needs (no provider)
2. Find processes that can serve that goal
3. For each process, find viable providers using that process's preconditions and scoring
4. Select the best (process, provider) pair and execute

**The provider is baked into the goal before any process is consulted about whether it can use that provider.**

### Where each concern should live vs. where it actually lives

| Concern | Should live | Actually lives |
|---|---|---|
| What does the entity need? | Goal (attribute deficit) | Goal — correct |
| How to satisfy the need? | Process selection at eval time | Split: filtered at eval, but constrained by pre-selected provider |
| Who to act on? | Process-specific provider search at eval time | Goal generation (up to 10 ticks stale) |

`EntityGoal.Provider` is the structural root cause.

---

## Concrete Manifestations

### 1. Self-targeted and external-targeted processes cross-contaminate goals

`GoalGenerator.GeneratePrimaryGoals()` (line ~100-115) pools candidates across ALL serving processes for a deficit attribute. "Grow" (TargetIsSelf) and "Attract" (TargetIsNotSelf) both write `population`. The generator creates goals pairing external settlements with Grow (which will always fail TargetIsSelf) and pairing self with Attract (which will always fail TargetIsNotSelf).

`candidateIds.Add(entity.Id)` on line 117 unconditionally adds self, ensuring every external-only process gets a self-targeted goal it can never use.

### 2. RewardExpression scored without process identity

`GoalGenerator.cs` line ~129-135 takes `bestReward` across all serving processes per candidate. A candidate gets the max reward from processes with contradictory requirements. The winning reward might come from a process that can't actually use that candidate.

### 3. Score() is blind to target

`DataDrivenProcess.Score()` creates `ExpressionContext(entity)` with no target. At evaluation time, the score cannot account for the provider. All target-awareness is forced into RewardExpression at goal-gen time, compounding the early-commitment problem.

### 4. Provider search uses Writes instead of target scope

`GraphAttributeQuery.FindCandidates` looks at what the process **writes** to find providers. For a self-modifying process like Grow, writes are `Self("population", Settlement)` — Kind is Settlement, so FindCandidates returns every settlement with population > 0. These are providers Grow will never use.

### 5. Stale goals with locked providers

Goals persist 10 ticks. Provider could die, deplete the attractive attribute, move out of ideological range, or lose the fame advantage. The entity is committed regardless.

### 6. Fallthrough changes objective, not provider

When a goal fails (no viable process-provider pair), the evaluator tries a **different goal attribute**. If population is the highest-urgency need but the pre-selected provider doesn't work, the entity doesn't try other providers for population — it falls through to sustenance or fame. The entity abandons its most urgent need because of a provider mismatch.

---

## Worked Example

A settlement with low population:

1. GoalGenerator fires. Deficit: population. Serving processes: Grow (self), Attract (external).
2. Candidates pooled: self + every settlement with population > 0.
3. For "Weak" (low fame): Grow reward = 2.0, Attract reward = 3.0. Best = 3.0.
4. Goal: `(population, Weak, urgency * 3.0)` — high weight.
5. For self: Grow reward = 2.0, Attract reward = 0.0. Best = 2.0.
6. Goal: `(population, self, urgency * 2.0)` — lower weight.
7. Weighted random picks the Weak goal.
8. Evaluator: Grow serves population? Yes. Attract serves population? Yes.
9. Grow precondition: TargetIsSelf. Provider is Weak. **Fails.**
10. Attract precondition: TargetIsNotSelf + ideology + sustenance. Maybe fails too.
11. If both fail: **no process executes for population.**
12. Fallthrough to sustenance goal. Entity stockpiles food instead of growing, even though Grow could have fired if the goal had pointed at self.

---

## Secondary Issues

### GoalCheckContext stubs out most of the world

`GoalCheckContext` (GoalGenerator.cs line 205-243) returns 0 for pressure, null for relationships, 0 for resources. Sub-goal generation evaluates preconditions against a false picture of the world. Any precondition checking relationships, resources, or pressure appears to fail regardless of actual state.

### ProcessMutationContext.GetPressure returns 0

Line 141: `// TODO: wire pressure`. Any process precondition checking pressure is silently broken.

### Non-deterministic goal IDs

`EntityGoal` uses `Interlocked.Increment` on a static counter. IDs are global, not per-entity. Across save/load, goal IDs won't be stable. This will break serialization.

### Evaluator de-duplication may suppress valid evaluations

Line 157-161: de-duplicates on `(ProcessName, Provider)` across goals. Sub-goals for the same provider but different blocking attributes could have different precondition results, but the dedup treats them as identical.

---

## Proposed Fix

### Design Principle

Goals describe **needs**, not **intentions toward specific entities**. Provider selection is a process-level concern resolved at evaluation time, when preconditions and current state are available.

### Phase 1: Decouple Provider from Goal

**Remove `EntityGoal.Provider`.** A goal becomes `(Attribute, Urgency, ParentGoalId?, BlockingAttribute?)`. No provider, no target entity.

GoalGenerator simplifies to:
- Scan attribute deficits
- Compute urgency from baseline formula
- Generate sub-goals from blocked preconditions (using full mutation context, not stubs)
- No provider search, no RewardExpression at this stage

This is the structural fix. It breaks the fused model.

### Phase 2: Provider Search Moves Into the Evaluator

The evaluator, when serving a goal, does provider search **per process**:

```
for each goal (by urgency):
    for each process that writes goal.TargetAttribute:
        candidates = FindProviders(process)  // uses process Reads/Writes scope
        for each candidate:
            if precondition passes with (self, candidate):
                score candidate using ScoreExpression(self, candidate)
        pick best (process, candidate) pair
    if found: execute, done
    else: try next goal
```

Key changes:
- `FindProviders` is process-scoped (self-targeted processes only search self, external-targeted processes only search external)
- Preconditions are evaluated BEFORE scoring, with the real candidate as target
- ScoreExpression gets target context (fix the blind-score problem)

### Phase 3: Score() Gets Target Context

Change `IProcess.Score` signature to include the goal/target:

```csharp
float Score(Entity entity, IWorldState worldState, EntityGoal? goal);
```

`DataDrivenProcess.Score()` builds `ExpressionContext(entity, target)` so score expressions can account for the provider. This unifies RewardExpression (currently goal-gen only) and ScoreExpression (currently eval only) — a process has one scoring expression that evaluates with full context.

RewardExpression can be retired or repurposed as a lightweight pre-filter (e.g., "is this candidate even worth checking preconditions for?").

### Phase 4: Fix Secondary Issues

- Wire pressure into `ProcessMutationContext.GetPressure` (replace the TODO)
- Replace `GoalCheckContext` stub with a real read-only context that queries actual world state
- Make goal IDs deterministic and serialization-safe (entity-scoped counter, or derive from attribute + tick)
- Review the evaluator de-duplication logic for sub-goal correctness

### Phase 5: Simplify GraphAttributeQuery

With provider search moving into the evaluator and becoming process-scoped, `GraphAttributeQuery.FindCandidates` can be refined:
- Self-scoped writes -> only self is a candidate
- Location-scoped writes -> settlement at entity's coordinates
- Related-scoped writes -> traverse relationship graph
- Kind-scoped writes -> entities of that kind (current behavior, but now only used for processes that actually target external entities)

The unconditional `candidateIds.Add(entity.Id)` is removed. Self is a candidate only when the process's scope says so.

---

## Migration Path

Phase 1 is the breaking change — it removes Provider from EntityGoal, which touches GoalGenerator, EntityEvaluator, all tests that construct goals with providers, and the EvaluationReport (which currently records provider names per candidate).

Phases 2-3 are the behavioral replacement — they restore the functionality that Provider removal takes away, but in the correct location (evaluator, not goal generator).

Phases 4-5 are cleanup that become straightforward once the structural fix is in place.

All phases should land as a single coherent change. Committing Phase 1 without Phase 2 would break process execution entirely (no provider resolution). This is consistent with the project rule against committing partial features.
