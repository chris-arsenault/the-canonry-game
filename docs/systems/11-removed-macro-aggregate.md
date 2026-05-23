# System 11: Removed Macro Aggregate

Status: removed.

The former broad aggregation layer is no longer part of the runtime, domain
config, events, telemetry, persistence, projection, generator gates, gambit
gates, traits, or cards.

Use existing world state directly instead:

- relationship strength for numeric generator and gambit gates;
- health for survival, damage, decay, repair, and recovery;
- map signals, culture influence, coverage, and locality queries for spatial or
  environmental conditions.

Do not introduce a replacement aggregate that bypasses shared query and effect
execution.
