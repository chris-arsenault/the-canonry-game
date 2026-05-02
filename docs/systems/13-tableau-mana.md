# System 13: Tableau / Mana

The tableau is the player's active set of world-shaping cards.

## Current Purpose

In development, the tableau is also an observability surface: it should expose
what active generators and gambits can do, why they are waiting, and what they
last changed.

## Required Tableau Diagnostics

Each active generator or gambit should expose:

- preconditions
- target requirements
- outputs
- last run tick
- execution count
- recent failure or waiting reason
- saturation relationship/fact limits when configured

## Design Rules

- Tableau cards are commitments. Removing one should not undo graph history.
- Tableau activity should be inspectable; hidden card logic creates bad tuning.
- Generators create named entities. Gambits mostly change relationships, status,
  health, or traits.
- Low-altitude verbs are not tableau cards.

## Mana

Mana is still a balancing concept, not a kernel requirement. When implemented,
it should constrain how many world-shaping cards can be active, not how many
low-altitude physics programs run.
