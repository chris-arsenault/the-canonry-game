# Attribute Model

The active domain uses a compact bucket model to keep attributes purposeful.
This is a design tool, not a validator.

## Buckets

| Bucket | Meaning | Current Examples |
| --- | --- | --- |
| Health | What keeps the entity active. | `vitality`, `condition`, `cohesion`, `temp_hp`, `stability` |
| Need / Drive | What expresses pressure or lack. | `warmth`, `provisions`, `shelter`, `pristiness` |
| Capacity | What lets the entity hold or channel power. | `echo`, `power`, `holding` |
| Projection | What lets the entity affect others. | `reach`, `dominion` |
| Alignment | Where the entity sits semantically. | physical/semantic coordinates and culture |

## Current Mapping

| Kind | Health | Need / Drive | Capacity | Projection |
| --- | --- | --- | --- | --- |
| `region` | `stability` | `pristiness` | `holding` | - |
| `settlement` | `temp_hp` | `shelter` | - | `dominion` |
| `faction` | `cohesion` | `provisions` | - | `reach` |
| `penguin` | `vitality` | `warmth` | `echo` | - |
| `artifact` | `condition` | `resonance` | `power` | `power` |

## Selection Rules

- Prefer bounded positive-high scalars.
- Avoid duplicate concepts with different names.
- Do not create attributes solely to feed old process-style action demand.
- Prefer attributes that multiple systems can use: lifecycle, generators,
  gambits, relationship maintenance, UI, or future cards.
- Cultural display names can change the language without changing mechanics.
