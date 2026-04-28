# Analysis Biases

Working document for recurring analysis failures during TIR design and simulation tuning. The point is not etiquette; it is to stop repeating bad analytical habits that waste turns or point tuning in the wrong direction.

Operational rules and session-behavior rules live in [general-instructions.md](./general-instructions.md).

## Purpose

When evaluating the sim, prefer:

- actual measured behavior over generic sim-design instincts
- the current open workstream over whatever is easiest to tweak
- structural explanations before parameter nudges
- end-to-end tuning value over detached reporting

## Hard Corrections

### 1. Do not default to weights

Recurring bias:

- treat imbalance as a weight problem because it is easy to say and easy to change

Correction:

- start with structure: targeting, placement, planner shape, sustainment paths, availability, and execution failures
- only talk about weights after structural causes have been checked and mostly ruled out

Why this exists:

- the active backlog is mostly structural: frontier expansion, target anchoring, contention, sustainment, process tuning

### 2. Do not treat movement dominance as bad by volume alone

Recurring bias:

- see large movement share and assume movement itself should be reduced

Correction:

- movement is good if it creates wider map use, richer contacts, more local change, and more productive follow-up actions
- movement is bad only when it behaves like planner tax

Current non-starter:

- reducing movement preconditions is not the current direction

Preferred direction:

- make movement choose better destinations and produce more work after arrival

Additional correction:

- a low action-per-move ratio is not automatically pathological
- colony-style sims can sustain many travel turns per one productive action
- the real question is whether the non-movement deck is varied and consequential once movement is factored out

### 3. Do not hyper-focus on one kind without cross-kind evidence

Recurring bias:

- see a strong penguin symptom and start telling a penguin-only story

Correction:

- check whether the same pattern appears across artifact, faction, settlement, and region before narrowing the diagnosis
- only go kind-specific when the data actually isolates the problem

Example:

- mobility dominance appeared across all kinds, not just penguins

### 4. Do not over-index on the ambient resource layer

Recurring bias:

- treat local hex resources as if they are meant to be the primary global driver of behavior

Correction:

- ambient resources should matter occasionally and locally
- they should help named places accumulate local value over time
- they are not the main explanation for every planner or frontier problem

Additional correction:

- if resources or pressures are inert, consider ablation as a diagnostic
- do not preserve a dead subsystem just because it sounds simulation-rich on paper

### 5. Do not propose generic recommendations without a tuning end goal

Recurring bias:

- suggest tooling or side work in a vacuum
- produce intermediate output that does not clearly feed a real tuning decision

Correction:

- every recommendation should answer: what decision will this let us make?
- analysis should terminate in a concrete tuning or design choice, not in abstract observability for its own sake

### 6. Do not drift away from the active tickets

Recurring bias:

- narrate the problem from first principles while ignoring the actual open workstream

Correction:

- refresh the current backlog before reframing a problem
- keep analysis attached to the real threads in flight

Example:

- current process/generator work is primarily about planner physics, frontier expansion, carrying pressure, and sustainment
- it is not primarily a generator-weight exercise

### 7. Do not assume a bug when a loop can explain the result

Recurring bias:

- jump from suspicious output directly to an implementation bug

Correction:

- check both:
  - is there an implementation bug?
  - is the system behaving consistently but producing a bad loop?

Example:

- settlement corridor persistence was not just an `Across` bug; it also came from anchored settlement creation and weak low-altitude physics

### 8. Do not treat locality as the first-class problem statement

Recurring bias:

- make locality itself the center of the design, then keep adding locality-specific machinery to compensate

Correction:

- locality is an important constraint, not automatically the main source of value
- Canonry originally produced interesting behavior without locality at all; TIR should use locality to ground the graph, not let locality dominate the reasoning
- prefer fixes that work through the existing planner, process deck, stock dynamics, and graph-side demand before inventing large locality-only systems

What this means in practice:

- do not assume richer localities are the first lever
- do not assume multi-goal visits are required as a baseline success metric
- do not use locality-thickening as the default next move when simpler explanations are still live

## Current Applied Reminders

- Movement preconditions are staying.
- Process deck and low-altitude physics come before generator tuning.
- Locality is a constraint and grounding layer, not the automatic center of the design.
- Use measured downstream effects, not category-level prejudice, when calling a behavior pathological.
