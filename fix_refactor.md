# Low-Altitude Verb Refactor Status

Updated 2026-05-06.

This note replaces the stale handoff transcript for tracking item 3. The
current implementation now matches the approved direction:

- Low-altitude verb selection uses routine and opportunity buckets. The
  selected trace event records `selection_bucket_code`.
- Cell-target modes scan a bounded neighborhood around the actor and reservoir
  pick from scored candidates. The old uniform random coordinate helpers are
  removed from `LowAltitudeVerbProgramRuntime`.
- Ice Remembers low-altitude verb helpers author only the values the runtime
  uses: `weight` and `opportunityBonusOverride`. The legacy six-value tuple is
  gone.
- Relationship-strength verbs declare signed fitness polarity and thresholds:
  strengthening verbs use deficit fitness, weakening verbs use surplus fitness.
- Low-altitude trait modifiers use the three primitive deltas:
  `WeightDelta`, `OpportunityBonusDelta`, and `FitnessWeightDelta`.
- Recent-target cooldown uses the configured recent-target ring size from
  kernel tuning.
- Low-altitude tuning is loadable from JSON/TOML startup config and persists
  through kernel snapshots.

Remaining work is balance validation, not architecture completion:

- Run the requested Monte Carlo pass when item 8 and item 3 are ready for MC
  verification.
- Retune authored `opportunityBonusOverride` values only from MC evidence.
