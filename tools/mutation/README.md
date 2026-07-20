# Mutation testing

Scoped, on-demand mutation testing over the generator's algorithmic core.

## Why this scope

The twisted-lattice defect was a wrong constant in a lookup table that stayed
internally consistent, so every structural validation passed. Mutation testing
is the instrument that finds that class directly: it would have reported the
cross-hex neighbour deltas as a *surviving mutant* — "nothing fails when I
change this constant" — which is exactly the missing signal.

It is scoped to `PhysicalGeneration/Algorithms` and `PhysicalGeographyGrid`
because that is where lookup tables, comparison operators, and index arithmetic
live, and because whole-solution mutation against a generator that takes about
a minute per world is not viable. Run it on demand or nightly, never per
commit.

## Running

```bash
dotnet tool install --global dotnet-stryker   # once
cd tools/mutation
dotnet stryker --config-file stryker-config.json
```

The HTML report lands in `StrykerOutput/`. `break` is set to 0 so the run
reports rather than fails: the output is a list of places worth a test, not a
gate.

## Reading the result

A surviving mutant means no test distinguishes the mutated code from the
original. For a constant table or an index expression that usually means a real
gap. Prefer adding a test that pins the *behaviour* — for the lattice that
meant conformance against world geometry, not asserting the table's literal
values, which would have passed equally on the twisted table.
