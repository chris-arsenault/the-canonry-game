# Generator Causal Cost Cohesion Hypothesis

## Hypothesis

Generator causal spend and output locks may be preventing cultural cohesion.
Generators can initially target entities inside strong cultural fields, spend or
lock the cause that made those entities eligible, then stop targeting the same
cultural mass. If later generator fires drift to weaker targets, the simulation
loses the feedback loop that should let successful cultural centers accumulate
more narrative weight.

This is not specific to one seed entity or one settlement subtype. A single
rookery is only an example of the broader failure mode: cultural centers may be
eligible briefly, then become causally exhausted before they can snowball.

## Validation

The validation should measure generator target eligibility against the derived
culture field:

- generator id, tick, target entity, target kind/subtype/culture
- target physical and semantic positions
- physical and semantic culture field band
- physical and semantic culture cell kind
- physical and semantic local and connected raw cultural mass
- whether the target passed generator gates
- first rejection reason when it failed

The key metric is not only whether a generator fired. The question is whether
targets inside strong connected cultural mass become ineligible after generator
activity, and whether the blocking reason is a causal spend or output lock gate.

## Useful Queries

- For each generator and culture, compare pass rate by culture field band.
- For each strong-field target that receives a generator fire, check whether it
  remains eligible on later evaluations.
- Break strong-field rejection rows down by missing required relationship,
  absence-lock relationship, terrain gate, trigger gate, and secondary binding.
- Compare generator fires per cultural component with component growth over the
  next 250-500 ticks.
- Compare births inside strong fields when strong-field generator targets remain
  eligible versus when they are mostly rejected.

## Falsification

The hypothesis is weakened if strong cultural targets remain eligible but are
not selected, or if relaxing causal spend in a diagnostic branch increases repeat
fires without improving connected cultural mass. It is strengthened if strong
cultural targets fail mostly on relationship spend or absence-lock gates shortly
after generator fire, while weaker field targets keep passing.
