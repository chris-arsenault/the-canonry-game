# System 8: Resources / Economy

Status: removed from the active engine.

## Current Position

The active low-altitude model is movement, influence, map activity, map signals,
effects, upkeep, relationships, gambits, and generators.

There are no runtime resource definitions, location-resource mutations,
resource telemetry rows, resource projection state, or resource snapshot paths.
Do not author generators, gambits, cards, or low-altitude behavior against
resource ids unless a new economy system is intentionally designed and added.

## Future Principle

If resources become active again, keep this distinction explicit:

- ambient possibility space: what a place can support;
- concrete available-now stores or services: what an entity can spend or use.

Do not quietly collapse both meanings into one number.

## Acceptable Future Uses

Resources can later support:

- terrain-driven carrying capacity;
- settlement or faction extraction;
- trade or route systems;
- generator preconditions;
- card-enabled economic systems;
- map overlays and player diagnostics.

Those should be added as explicit gameplay systems, not as hidden recovery
logic inside low altitude.
