# Test Requirements

Systems that need testing, what to verify, and expected fidelity. Written for an agent with no context of the implementation history — only the code as it exists.

> Migration note: this document was written against the legacy `src/Engine/`
> monolith. The conceptual coverage targets still apply, but the engine has
> since been split into a microkernel (`Engine.Kernel`), domain compilation
> (`Engine.Domain`), and consumer projects. The `Location:` lines below have
> been updated to point at the current homes; some legacy implementation
> classes no longer exist as discrete files — their responsibilities are now inside kernel program
> runtimes operating over primitive ID tables. See `docs/architecture.md` for
> the runtime model and verify the current home of any system before writing
> tests for it.

---

## 1. Goal System (planner/process goal generation)

**Location:** `src/Engine.Kernel/Programs/Processes/` (process-side goal
selection) and `src/Engine.Kernel/Programs/Planning/` (planner runtime).
Process and gambit objective definitions are compiled from
`src/Engine.Domain/Processes/` (`ProcessDefinition`,
`ProcessSupportRule`, etc.) by `DomainCompiler`.

**What it does:** Generates goals for entities from attribute deficits and process wiring contracts. Produces sub-goals from blocked preconditions.

### Tests needed:

**Goal generation from attribute deficits:**
- Entity with population=10, baseline=20 → generates a population goal with urgency ≈ 0.67
- Entity with population=20, baseline=20 → urgency ≈ 0.5
- Entity with population=100, baseline=20 → urgency ≈ 0.17 (low, growth is comfortable)
- Entity with no attribute deficits → no goals generated
- Entity with multiple deficits → goals sorted by urgency, capped at 3

**Provider finding and scoring:**
- Process with no ProviderScoreExpression → all providers score 1.0
- Process with ProviderScoreExpression → providers scored by expression (verify expression is evaluated with self + candidate as context)
- Providers with score ≤ 0 excluded
- Top 3 providers kept
- No process writes matching the deficit attribute → no goals for that attribute

**Sub-goal generation:**
- Process serves goal but precondition fails → sub-goal created for each BlockingAttribute in the PreconditionResult
- Process serves goal and precondition passes → no sub-goal
- Multiple processes fail with same blocking attribute → only one sub-goal (deduplication)
- Sub-goals inherit parent's provider and urgency

**Goal/process matching specifics:**
- Process with `TargetIsSelf` precondition: when evaluated against self as provider, precondition passes. When evaluated against another entity, it fails and generates no sub-goals (because TargetIsSelf doesn't report a blocking attribute).
- Process with `TargetIsNotSelf`: opposite behavior.

**Sub-goal mutation context:**
- Mutation context resolves the planner-selected target by ID, not by
  name; the runtime carries primitive `(entityId, attributeId)` keys, not
  string lookups (`ProcessMutationContext.ResolvedTargetId` is the
  legacy term — verify this is now expressed as a primitive target slot
  on the kernel mutation context).

---

## 2. Entity Evaluator (process program runtime)

**Location:** `src/Engine.Kernel/Programs/Processes/ProcessProgramRuntime.cs`
(plus `GambitProgramRuntime`, `GeneratorProgramRuntime` for the analogous
gambit and generator paths). The legacy `EntityEvaluator` class is gone;
its responsibilities — goal selection, candidate filtering, cooldowns,
weighted random selection, mutation execution — are now inside the kernel
program runtimes operating over fixed-capacity scratch buffers.

**What it does:** Each tick, selects a goal, finds serving processes, filters by cooldowns and preconditions, picks one via weighted random, executes with the goal passed through.

### Tests needed:

**Goal selection:**
- Entity with one goal → that goal is selected
- Entity with multiple goals → selection is weighted by urgency (run many iterations, verify higher-urgency goals are selected more often)
- All goals tried in sequence when selected goal has no viable processes (fallthrough)

**Process-goal matching (ServesGoal):**
- Process that writes to goal's TargetAttribute → serves the goal
- Process that writes to a different attribute → does not serve
- Sub-goal with BlockingAttribute → process must write to the blocking attribute

**Precondition evaluation with goal context:**
- `TargetIsSelf` passes when goal.Provider == entity.Id
- `TargetIsSelf` fails when goal.Provider is another entity
- `TargetIsNotSelf` opposite
- `TargetAttributeThreshold` reads the provider entity's attributes
- `SelfAttributeThreshold` reads the evaluating entity's attributes (not the provider)

**Cooldowns:**
- Process on cooldown → filtered out (not a candidate)
- Entity-wide cooldown → all processes filtered for this entity
- Cooldown expiry → process becomes available again
- Cooldown jitter → cooldown duration varies ±25%

**Execution:**
- Winning process receives the goal in execute (not null)
- The mutation context's resolved target ID matches the planner-selected
  provider entity ID (legacy: `ProcessMutationContext.ResolvedTargetId`)
- Mutations that reference target modify the provider entity's attributes
- Mutations that reference self modify the evaluating entity's attributes
- Cooldowns applied after execution

**EvaluationReport:**
- Report is stored on entity.LastEvaluation after each tick
- Report contains all evaluated candidates with correct scores and precondition results
- Report marks the selected process
- Report is not null even when no process fires (empty candidates)

**Edge cases:**
- Entity with no goals and no goal generator → EvaluateWithoutGoals fires processes without target
- Entity on entity-wide cooldown → nothing fires, report produced
- All processes on cooldown for all goals → nothing fires

---

## 3. Precondition System

**Location:** `src/Engine.Domain/Processes/Preconditions.cs`,
`src/Engine.Domain/Processes/PreconditionResult.cs` (definitions and
authoring shapes). Precondition codes used at runtime are in
`src/Engine.Constants/ProcessPreconditionCodes.cs`; runtime evaluation
runs inside `src/Engine.Kernel/Programs/Processes/`.

### Tests needed:

**PreconditionResult structure:**
- Pass result: Passed=true, diagnostic set, empty BlockingAttributes
- Fail result: Passed=false, diagnostic set, BlockingAttributes populated

**TargetIsSelf / TargetIsNotSelf:**
- When ResolvedTargetId == SelfEntityId → TargetIsSelf passes, TargetIsNotSelf fails
- When ResolvedTargetId != SelfEntityId → opposite
- When ResolvedTargetId is null → both fail (no target)

**SelfAttributeThreshold:**
- Entity with attribute above min → passes
- Entity with attribute below min → fails with diagnostic showing actual value and threshold
- BlockingAttributes contains the attribute name

**TargetAttributeThreshold:**
- Target entity with attribute above min → passes
- Target entity with attribute below min → fails
- No target (ResolvedTargetId null) → fails with "no target" diagnostic

**AndPrecondition:**
- All children pass → passes
- One child fails → fails, collects that child's blockers
- Multiple children fail → collects ALL children's blockers (not just first)
- Diagnostic is semicolon-separated list of all failures

**AlwaysTrue:**
- Always passes

---

## 4. Expression System

**Location:** `src/Engine.Domain/Processes/Expression.cs` (authoring
shapes and `Expr.*` builders). Runtime expression codes are in
`src/Engine.Constants/ProcessExpressionCodes.cs`; runtime evaluation
runs inside the kernel program runtimes
(`src/Engine.Kernel/Programs/Processes/`,
`src/Engine.Kernel/Programs/Gambits/`).

### Tests needed:

**Attribute path resolution:**
- `Expr.Self("fame")` resolves scalar attribute on self entity
- `Expr.Target("fame")` resolves scalar attribute on target entity (from ExpressionContext)
- Non-existent attribute → returns 0
- Null target → target paths return 0

**Arithmetic:**
- Add, Sub, Mul, Div with known values
- Div by zero → returns 0

**Built-in functions:**
- `Distance()` → hex distance between self and target coordinates
- Min, Max, Clamp, Abs with known values
- Jitter → produces variance (run multiple times, verify distinct results)

**Context variables:**
- `VariableRef` resolves from MutationContext when ExpressionContext.MutationContext is set
- `VariableRef` returns 0 when MutationContext is null

**CalculateMutation:**
- Evaluates expression, stores result as named variable on context
- Subsequent CalculateMutation can read prior variables via VariableRef
- ModifySelfByVariableMutation reads the stored variable
- ModifyTargetByVariableMutation reads the stored variable and modifies target entity

---

## 5. Attribute System

**Location:** `src/Engine.Kernel/PrimitiveFloatTable.cs`,
`src/Engine.Kernel/PrimitiveIntTable.cs`,
`src/Engine.Kernel/KernelData/KernelEntityData.cs`,
`src/Engine.Kernel/WorldState.cs` (attribute mutation API). Attributes are now
stored as primitive float values keyed by `(entityId, attributeId)` in
fixed-capacity tables. Attribute metadata (min/max/default/variance) is
registered through `Simulation.RegisterAttribute`.

### Tests needed:

**Primitive Attributes:**
- Registered attributes are keyed by `(entityKindId, attributeId)`.
- Entity attribute values initialize from default plus variance.
- Attribute mutation clamps values to registered min/max.
- Missing attributes fail loudly in kernel-facing mutation paths.
- Attribute changes emit primitive events with source code IDs.

**EffectsProgram:**
- Attribute-delta effects are registered independently from lifecycle upkeep.
- Relationship-distance effects can add or drain an attribute.
- Relationship-influence effects can add or drain an attribute at the entity's physical location.
- Effects mutate only explicitly configured attributes.

---

## 6. Mutation System

**Location:** `src/Engine.Domain/Processes/Mutations.cs` (authoring
shapes), `src/Engine.Constants/ProcessOperationCodes.cs` (runtime
operation codes). Mutation execution runs inside
`src/Engine.Kernel/Programs/Processes/`.

### Tests needed:

**ModifySelfAttributeMutation:**
- Modifies entity's own scalar attribute by delta with variance
- Variance=0 → exact delta

**ModifyTargetAttributeMutation:**
- Modifies target entity's attribute (from ResolvedTargetId on context)
- No target → no-op

**ModifySelfByVariableMutation / ModifyTargetByVariableMutation:**
- Reads named variable from context
- Applies to scalar (no component) or semantic plane component ("x" / "y")
- Variable not set → delta is 0

**CalculateMutation:**
- Evaluates expression with self + target from context
- Stores result as named variable
- Expression can reference context variables from prior CalculateMutation

**StockpileMutation (if still exists) / ConsumeResourceAtSelfMutation (if still exists):**
- Verify these domain-specific mutations have been removed. If they still exist, that's a bug.

---

## 7. Lifecycle and Maintenance

**Location:** `src/Engine.Kernel/Programs/Upkeep/` — the upkeep program
runtime owns lifecycle drain, maintenance, and death transitions. The
legacy `LifecycleSystem` / `MaintenanceSystem` classes are gone. Domain
authoring of lifecycle rules (per-kind drain coefficients, base rates,
ageing curves) lives in `src/Engine.Domain/Lifecycle/` and is compiled
into the kernel by `DomainCompiler`.

### Tests needed:

**LifecycleSystem:**
- Applies lifecycle health drain directly through `UpkeepProgram`.
- Late-life aging can accelerate drain after the configured start age.
- Transitions entities to historical when health reaches zero.
- Emits lifecycle/death events through primitive kernel events.
- Ignores entities without lifecycle config.

**Integration:**
- Drain is visible as primitive attribute-change events sourced from upkeep.
- Non-lifecycle health modifiers are visible as primitive attribute-change events sourced from effects.
- Drain increases with entity age.
- External modifications can counteract drain.

---

## 8. Provider Scoring

**Location:** `src/Engine.Kernel/Programs/Processes/` (provider candidate
selection and per-candidate scoring) and
`src/Engine.Domain/Processes/ProcessDefinition.cs` (where
`ProviderSelectors` and the per-candidate scoring expression are
authored). The legacy `GraphAttributeQuery` is gone; provider candidates
are now enumerated against the primitive ID indexes inside
`WorldState`.

### Tests needed:

**Provider candidate selection:**
- Finds entities of specified kind with the named attribute above the
  required threshold
- Excludes entities with the attribute at or below the threshold
- Excludes inactive (Historical) entities
- Returns candidates without ranking (ranking is the next step)

**Provider score expression:**
- Process runtime evaluates the per-candidate scoring expression with self
  + candidate as context
- Score of 0 excludes the candidate
- Null expression → all candidates score 1.0
- Weights are normalized (best = 1.0, others proportional)

---

## 9. Integration / Simulation

### Tests needed:

**Settlement lifecycle (IceRemembers):**
- Settlement starts with population=10
- Stockpile fires (produces sustenance)
- Grow fires when sustenance ≥ 3 (converts to population)
- Population increases over early ticks
- Lifecycle drain increases with age
- Eventually drain exceeds growth → population peaks and declines

**Settlement propaganda/attract:**
- Settlement with higher fame generates attract goal against lower-fame settlement
- Attract fails precondition when ideology distance > 0.5
- Propaganda fires as sub-goal to converge ideology
- After propaganda reduces ideology distance, attract becomes viable
- Attract transfers population: self gains, target loses (symmetric, max 5)
- Target with population < 10 → attract precondition fails

**Cooldown behavior:**
- After stockpile fires, it's on cooldown for ~2 ticks
- During stockpile cooldown, other processes (propaganda, grow) can fire
- Entity cooldown prevents ALL processes for ~1-2 ticks

**EvaluationReport accuracy:**
- Report matches what actually happened (selected process, scores, diagnostics)
- UI can read report without engine computation
