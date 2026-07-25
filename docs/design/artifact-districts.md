# Artifact Districts

Active-retention design (2026-07-24). Supersedes the same-instance
reawakening model.

## Model

An artifact installing into a settlement becomes a **district**:

- The install claims a hex into the settlement's coverage footprint — the
  city physically grows by one tile, with the artifact at its core.
- The district's template derives from the artifact's kind, subtype, and
  culture. Its current bonus comes from the subtype's authored bonus rule.
- The installing occurrence retires and is not referenced by the district.
  The district carries a compact artifact template, active bonus, host, and
  tile.
- A **specific reawakening gambit** consumes the newest district and creates a
  fresh artifact occurrence at its tile with a fresh authored relationship to
  the host. It does not restore identity, attributes, traits, old
  relationships, or provenance from the installing occurrence.
- The fresh occurrence becomes eligible for gambits on the following tick. It
  cannot re-install itself during the gambit iteration that created it.

## District record (kernel state, snapshot-persisted)

Per active district: artifact kind/subtype/culture template, host settlement
id, tile `(q, r)`, bonus kind and magnitude, and reawakened relationship
kind/polarity/strength. Rows are dense. Consumption or host retirement removes
the row and shifts the active tail, so elapsed time cannot exhaust district
capacity through inactive records.

## Bonus derivation (v1)

Registered subtype → bonus mapping (authored at host configuration, kernel
stays domain-agnostic): relic and seal → Healing (host health sustain per
tick); instrument and tome → Culture (raw influence emission on the tile for
the installed artifact template's culture); weapon → Healing as a warding aura for now — the
Road bonus kind (permanent road quality on the tile, movement speed) is
reserved in the code table but has no application pass yet.
Trait-differentiated bonuses, condition-scaled magnitudes, and
gambit-grant districts extend the same registry later.

## Surfaces

Kernel: district storage + install/reawaken ops + incrementally maintained
per-host healing totals and per-culture/cell emission totals + per-tick bonus
application in entity resolution + snapshot block. Domain: InstallDistrict /
ReawakenDistrict mutations + compile cases + a host-side subtype-bonus
registration. Authored: the enshrine gambit becomes the install trigger;
one reawakening gambit.
