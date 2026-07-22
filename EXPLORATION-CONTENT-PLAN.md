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
- O2 Expedition gambits: penguin_leads_expedition, faction_sponsors_
  expedition, orca_sounds_the_passage, transport_charts_the_coast; camps
  as POIs; fact-reading preconditions (geothermal refuges, passes).
- O3 Discovery made durable: kernel accessor for persisted geological
  features; discovery generators naming peaks/rivers/craters into POIs and
  Regions as reveal reaches them.
- O4 Ages of wandering: age_the_far_wandering (+the_great_crossing) on the
  Peoples track; charges boost exploration verb weight, frontier generator
  pulse weight, movement step rate; Long Dark gets the inverse.
- O5 The Far Wanderer chronicle archetype: journey milestones (beyond
  support, new landmass, naming a feature, surviving the freeze) with
  triumphant/memorial/founding endings.
- O6 Frontier survival: make_winter_camp, waymark_the_trail, hot-springs
  wintering; coldcraft framing for range grants; corridor persistence.
- O7 Instruments: MC exploration section from map_snapshots (growth curve,
  per-kind beyond-rim share); CellCreated undercount flagged; validator
  and MC flags for stalled growth and heart-bound kinds.

Sequence: O1, O7, O3, O2+O6, O4, O5.
