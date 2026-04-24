# Test Requirements

Systems that need testing, what to verify, and expected fidelity. Written for an agent with no context of the implementation history — only the code as it exists.

---

## 1. Goal System (GoalGenerator)

**Location:** `src/Engine/Behaviors/GoalGenerator.cs`

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

---

## 2. Entity Evaluator

**Location:** `src/Engine/Behaviors/EntityEvaluator.cs`

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
- `SemanticDistanceThreshold` computes distance between self and provider's semantic plane attribute

**Cooldowns:**
- Process on cooldown → filtered out (not a candidate)
- Entity-wide cooldown → all processes filtered for this entity
- Cooldown expiry → process becomes available again
- Cooldown jitter → cooldown duration varies ±25%

**Execution:**
- Winning process receives goal in Execute (not null)
- ProcessMutationContext.ResolvedTargetId == goal.Provider
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

**Location:** `src/Engine/Processes/Preconditions.cs`, `PreconditionResult.cs`

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

**SemanticDistanceThreshold:**
- Self and target with ideology distance ≤ threshold → passes
- Distance > threshold → fails
- Attribute is not a semantic plane → fails

**AndPrecondition:**
- All children pass → passes
- One child fails → fails, collects that child's blockers
- Multiple children fail → collects ALL children's blockers (not just first)
- Diagnostic is semicolon-separated list of all failures

**AlwaysTrue:**
- Always passes

---

## 4. Expression System

**Location:** `src/Engine/Processes/Expression.cs`

### Tests needed:

**Attribute path resolution:**
- `Expr.Self("fame")` resolves scalar attribute on self entity
- `Expr.Target("fame")` resolves scalar attribute on target entity (from ExpressionContext)
- `Expr.SelfX("ideology")` resolves X component of semantic plane
- `Expr.SelfY("ideology")` resolves Y component
- Non-existent attribute → returns 0
- Null target → target paths return 0

**Arithmetic:**
- Add, Sub, Mul, Div with known values
- Div by zero → returns 0

**Built-in functions:**
- `Distance()` → hex distance between self and target coordinates
- `SemanticDistance("ideology")` → Euclidean distance on semantic plane
- `SemanticDistance` with non-plane attribute → returns MaxValue
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

**Location:** `src/Engine/Attributes/ScalarValue.cs`, `SemanticPlane.cs`, `AttributeSet.cs`

### Tests needed:

**ScalarValue:**
- Value returns base when no static effects
- Value composes static effects: base + sum(additive) * product(multiplicative)
- Modify changes base, clamps to min/max
- ApplyTickEffects: base changes by sum(tick deltas) * product(tick multipliers)
- Trend tracks recent deltas (average of last N)
- AddTickEffect / AddStaticEffect / RemoveEffectsBySource

**SemanticPlane:**
- X and Y components with independent effects
- ModifyX / ModifyY clamp to min/max
- Label reduction via SemanticPlaneDefinition (3x3 grid)
- DistanceTo computes Euclidean distance
- ApplyTickEffects applies per-component
- Per-component tick and static effects independent

**AttributeSet:**
- GetScalar returns ScalarValue or null
- GetPlane returns SemanticPlane or null
- GetValue returns scalar value, 0 for planes
- Modify modifies scalar, no-op for planes
- ApplyTickEffects applies to all attributes
- RemoveEffectsBySource applies to all attributes

---

## 6. Mutation System

**Location:** `src/Engine/Processes/Mutations.cs`

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

**Location:** `src/Engine/Lifecycle/LifecycleSystem.cs`, `MaintenanceSystem.cs`

### Tests needed:

**MaintenanceSystem:**
- Calls ApplyTickEffects on all active entities
- Skips historical entities

**LifecycleSystem:**
- InitializeEffects registers drain tick effect on health attribute for all active entities
- UpdateDrainRates updates effect value based on age (baseRate + age * ageCoefficient)
- CheckDeaths transitions entity to Historical when health ≤ 0
- CheckDeaths emits EntityDied event
- Historical entities not processed
- Entities without lifecycle config not processed (regions)

**Integration:**
- Drain effect is visible on entity.Attributes (ScalarValue.TickEffects contains the drain)
- Drain increases with entity age
- External modifications (processes) can counteract drain

---

## 8. Provider Scoring

**Location:** `src/Engine/Behaviors/GraphAttributeQuery.cs`, `ProcessDefinition.ProviderScoreExpression`

### Tests needed:

**GraphAttributeQuery.FindCandidates:**
- Finds entities of specified kind with named attribute > 0
- Excludes entities with attribute ≤ 0
- Excludes inactive entities
- Returns unscored, unordered list

**Provider score expression:**
- GoalGenerator evaluates ProviderScoreExpression per candidate with self + candidate as context
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
