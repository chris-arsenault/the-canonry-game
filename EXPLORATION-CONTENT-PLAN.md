# Exploration Content — Implementation Plan

Frontier-founding as the range ratchet, plus an exploration content pass
across verbs, generators, gambits, and chronicle beats — every kind except
Occurrence gains a frontier-capable placement, and every new piece consumes
the terrain system (facts, sites, crossings, geological features). Content
milestones re-read docs/lore/lore-bible.md and relevant pages first; the
Great Thaw era is the exploration register.

## Opportunities and sequence

- O1 [DONE] Founding ratchet: physicalFrontier through the generator DSL;
  colonies/reefholds/harbors/pilgrim vents found at the physical frontier;
  The Thaw Calls the Bold (penguin witness, remembrance-stone, coldcraft,
  pressed-ice charter), Bulls Range the Far Currents (orca), The White Has
  Teeth (The Ice's monsters in the marches, Wilderness placement).
  Gate: founded-entity distance ratchets across a bootstrap; every kind
  beyond the startup rim (Region lands in O3).
- O2 [DONE] Expedition gambits: Raise the Waymark (penguin commits the far
  walk, packs an ice causeway home, deed costs flesh) and Sound the Far
  Passage (orca conduits an unheld reach, deep-current line, costs interior
  distance). Gate: waymark 29.9% success, far passage 36.1%; the stone
  generator answers deeds (5 deeds -> 2 stones in an 8k check run).
- O3 [DONE] Discovery made durable: kernel accessor for persisted geological
  features; The River Earns Its Name (region) and The Still Water Is
  Sounded (POI shoal) place on revealed feature anchors. Gate: 6 river
  namings, 8 lake soundings in production runs.
- O4 [DONE] Ages of wandering: The Far Wandering and The Great Crossing on
  the Peoples track; charges boost exploration verb weight, frontier
  generator pulse weight, movement step rate; Long Dark gets dampers.
  Gate: both ages opened in both gate runs.
- O5 The Far Wanderer chronicle archetype: journey milestones (beyond
  support, new landmass, naming a feature, surviving the freeze) with
  triumphant/memorial/founding endings.
- O6 [DONE] Frontier survival: Scout the White + Voyage Beyond the Charts
  (fog-adjacent seeking, waymark-trail travel influence), Winter at the
  Springs (hot-springs dwell), The Stone Remembers the Walk (remembrance-
  stone rises beside the walker; Near placement after on-cell placement
  proved always occupied). Gate: growth +1586/+1843 per bootstrap.
- O7 [DONE] Instruments: MC exploration section from map_snapshots (growth
  curve, per-kind beyond-rim share); CellCreated undercount flagged;
  validator and MC flags for stalled growth and heart-bound kinds.

Sequence: O1, O7, O3, O2+O6, O4, O5.
