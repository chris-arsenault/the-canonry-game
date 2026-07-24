# Artifact Districts

Owner-specified design (2026-07-24). Supersedes every earlier "fungible
legacy" sketch for installed artifacts.

## Model

An artifact installing into a settlement becomes a **district**:

- The install claims a hex into the settlement's coverage footprint — the
  city physically grows by one tile, with the artifact at its core.
- The district's identity and bonus derive from the **artifact's subtype
  and traits**: healing, movement speed, culture bonuses, gambit grants.
- The artifact itself goes **historical** at install: it no longer
  participates in the simulation at all. The district record carries its
  bonus as durable landscape/host state (this is the typed installed-legacy
  component from `ordered-emergence-population-plan-20260718.md`, realized
  spatially).
- A **specific reawakening gambit** is the only way back: it restores the
  artifact to active at the district tile and ends the district's bonus.
  The tile remains city ground. There is no sacking path; if destruction
  mechanics ever exist they destroy the artifact, not free it.

## District record (kernel state, snapshot-persisted)

Per district: source artifact id (historical), host settlement id, tile
(q, r), bonus kind code, magnitude, active flag. Fixed capacity.

## Bonus derivation (v1)

Registered subtype → bonus mapping (authored at host configuration, kernel
stays domain-agnostic): relic and seal → Healing (host health sustain per
tick); instrument and tome → Culture (raw influence emission on the tile
for the host's culture); weapon → Healing as a warding aura for now — the
Road bonus kind (permanent road quality on the tile, movement speed) is
reserved in the code table but has no application pass yet.
Trait-differentiated bonuses, condition-scaled magnitudes, and
gambit-grant districts extend the same registry later.

## Surfaces

Kernel: district storage + install/reawaken ops + per-tick bonus
application (effects pass) + snapshot block. Domain: InstallDistrict /
ReawakenDistrict mutations + compile cases + a host-side subtype-bonus
registration. Authored: the enshrine gambit becomes the install trigger;
one reawakening gambit.
