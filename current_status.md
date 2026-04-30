# Current Status

## Conversation Focus

This session has been about making the simulation tunable and observable after a large backend refactor, then narrowing from broad planner speculation back to grounded, encoded domain behavior.

The immediate design thread is now: **how to make map exploration a real goal using existing processes or very small modifications**.

## Important User Corrections

- Do not treat movement volume as a problem by itself. Movement is expected; the problem is whether the world uses the map, whether entities roam meaningfully, whether verbs are utilized, and whether individual stocks fluctuate.
- Do not keep returning to locality as an evaluation criterion. Locality can be implementation detail, but the current evaluation focus is:
  - map utilization,
  - individual physical/semantic roaming variance,
  - verb utilization.
- Do not invent domain/process concepts during design. Ground proposals in actual `IceRemembers` domain definitions and engine/planner code.
- Do not loosen tests just to get green. Tests are there to encode design gaps and invariants unless explicitly marked otherwise.
- For analysis, prefer Python/direct SQL/DuckDB over C# scripting unless C# is actually the right tool.
- Do not run tests in parallel; it has repeatedly failed in this repo.

## Actual Encoded Domain Facts

Current Ice Remembers kinds:

- `region`
- `settlement`
- `faction`
- `penguin`
- `artifact`

Current penguin attributes:

- `vitality`
- `echo`
- `warmth`

Current relevant penguin processes generated from semantics:

- `penguin_warm`: restores `vitality` from local `food` at a configured recovery host. For penguins, recovery hosts are `settlement`.
- `penguin_endure`: converts `vitality` into `warmth` when `warmth` is critical.
- `penguin_forage`: converts `region.holding` into `penguin.warmth`.
- `penguin_witness`: converts `penguin.echo` into local `population`.
- `penguin_wield`: converts `artifact.power` into `penguin.echo` when near an artifact. This is not persistent artifact ownership.
- `penguin_travel`: physical movement toward a target; currently serves the structural in-range support effect.
- `penguin_wander`: random physical drift with `AlwaysTrue`; currently does not advertise that it serves `roam`.

Current structural facts:

- `roam`
- `settlement_contribution`
- `territorial_order`

Current penguin standing drive:

- `roam`, base urgency `0.03`

Important gap:

- `penguin` has a standing drive for `roam`, but no generated process currently has `GoalEffects` for `roam`. That means the GOAP planner has a real goal with no process that satisfies it.

## Actual Planner Shape

Current GOAP planner behavior:

- Objectives come from scalar attributes and standing drives.
- A process can satisfy an objective only if its `GoalEffects` match the objective effect.
- If a process fails a precondition, the planner converts that failure into a blocker effect and recursively searches for a supporting process.
- Mobility processes currently serve structural in-range facts, allowing blocked actions to plan movement toward their target.
- There is no durable mid-term task list. The planner picks one immediate executable leaf action, executes it, and replans next tick.

Current opportunity planner caveat:

- It is not a true world-focused opportunity planner. It is still mostly an entity-centric broad root-candidate enumerator layered over the same recursive support machinery.

## Recent Implementation State

Kernel/domain leakage cleanup was performed before this summary:

- Removed hard-coded kernel concepts around `HexToHealth`.
- Removed process resource supply model codes and named-provider recovery special cases.
- Recovery is now expressed through generic process mutations using `resource_recovery`.
- `KindConfig.HexToHealthRecoveryHosts` was renamed to `ResourceRecoveryHosts`.
- Ice Remembers recovery hosts now use `ResourceRecoveryHosts`.
- Pressure system was disabled by default, with config used to enable it for tests/experiments.

Verification from that pass:

- `dotnet build --no-restore` passed.
- `dotnet test tests/Engine.Kernel.Tests/Engine.Kernel.Tests.csproj` passed.
- `dotnet test tests/Engine.Domain.Tests/Engine.Domain.Tests.csproj` passed.
- `dotnet test tests/Engine.Host.Tests/Engine.Host.Tests.csproj` passed.
- `dotnet test tests/MonteCarlo.Tests/MonteCarlo.Tests.csproj --filter FullyQualifiedName~MonteCarloBatchAnalyzerTests` passed.
- `dotnet test tests/MonteCarlo.Tests/MonteCarlo.Tests.csproj --filter FullyQualifiedName~MonteCarloOutputGapTests` still had one known failure around selected `planner_localities` not joining committed `process_executions`. The user explicitly does not want locality treated as a tuning criterion.

## Dirty Worktree Warning

The repo currently has many modified files from prior tuning/refactor work and at least these untracked files:

- `docs/plans/current-tuning-pass/planner-tuning-metrics-focus.md`
- `tests/Engine.Kernel.Tests/OpportunityPlannerResourceDemandTests.cs`

Do not assume all dirty files were changed by the current agent. Do not revert unrelated changes.

## Current Map Exploration Proposal

Smallest grounded fix:

1. Add verb-level `GoalEffects` support to `SemanticVerbConfig`.
2. Have `RuntimeProcessGenerator` copy verb-level goal effects onto generated `ProcessDefinition`.
3. Set `penguin_wander` to produce `StructuralFact.Roam`.
4. Leave `penguin_travel` serving only in-range support. Using travel as exploration would target existing entities and likely worsen clustering.

Expected planner path after that change:

```text
penguin standing drive: roam
  -> ProcessServesEffect(roam) finds penguin_wander
  -> penguin_wander precondition AlwaysTrue passes
  -> existing WanderMutation moves one adjacent physical hex
```

This enables map exploration using the existing process.

Likely follow-up if random wandering is too weak:

- Add a small, domain-neutral wander policy, e.g. `Random` vs `Explore`.
- `Explore` should choose among valid adjacent cells by lowest visit/activity count with slight randomness.
- This should preserve the current process/planner shape and only make `penguin_wander` a better implementation of `roam`.

## Next Recommended Step

Implement the smallest grounded exploration fix:

- Add `GoalEffects` to semantic verbs.
- Wire `roam` onto `penguin_wander`.
- Add tests proving:
  - `penguin_wander` has `StructuralFact(roam)` as a goal effect.
  - the compiled process objective for penguin `roam` can select `penguin_wander`.
  - `penguin_travel` remains the support process for physical in-range, not the exploration action.

