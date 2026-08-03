# Kind-general build, 9x30k over-time report (2026-08-03)

Nine seeds (9100-9104 minus 9105, 9106-9109) at 30,000 ticks on the
kind-general build (commit d763278e), run as three 3-seed batches
(kindgen-r20/r20b/r20c). Live monitors watched for failure signatures;
measurements were taken per 2,500-tick window from streams and final
telemetry. Windows are numbered w1-w12 (w12 = tick 30,000). Census
shares are Aurora/Nightshelf/Wake-Singers percent of living entities
(world culture omitted, typically 2-10%).

## Completion status

Eight of nine worlds completed cleanly. Seed 9100 crashed at ~tick
22,500: the telemetry event publisher (KernelEventPublisher.TryWrite)
hard-faulted after a dropped-rows warning phase (834 dropped at tick
22,000). Two pressures combined: the world's population grew linearly
with no plateau (295 -> 932 by w9, density pinned at ~1.0), and the
kind-general target iteration publishes a target-evaluation event per
candidate of BOTH kinds, roughly doubling evaluation-event volume for
actor-targeting generators. 9100's telemetry is clean through tick
20,000 and included below to w9.

## Per-world trajectories

Census per window (Aurora/Nightshelf/Wake-Singers %):

- 9100 (CRASH @22.5k): steady Aurora ~57/20/20 from w1 to w9. No
  drift; the crash was capacity, not balance.
- 9101: 28/29/38 -> settles w3 at ~16/18/50; Wake-Singers hold ~50%
  through w12. Physical top 0.86-0.90 (Wake-Singers) throughout.
- 9102: Nightshelf-led throughout, 54% -> 43-50%; Wake-Singers second.
  Physical top stays Wake-Singers 0.49-0.69 - census leader and
  physical leader differ for the whole run.
- 9103: contested. Aurora leads w2-w5 (43-45%), Nightshelf overtakes
  w6-w10 (46-52%), Aurora recovers to 42% by w12. Physical lead
  alternates c3/c1 all run.
- 9104: Aurora steady 40-46% for 12 windows; Nightshelf fades 44 ->
  15-18. Physical top Aurora 0.49-0.64.
- 9106: the envelope excursion. Nightshelf 55% (w1) climbs to 77%
  (w9-w10) - past the 60% target for four windows - then falls back to
  62% at w12. Physical lead flips from Wake-Singers to Nightshelf
  (peak 0.72). Aurora is crushed to 7-12% during the excursion.
- 9107: contested. Nightshelf leads early (44%), Aurora overtakes from
  w7 and holds 35-40%; three-way from w4 onward (all cultures 24-40%).
  Physical lead alternates.
- 9108: Nightshelf-led 43-54% with Aurora rising 23 -> 34 by w12. The
  most territorially pluralist world measured: physical top share
  0.35-0.49 all run, horizon 0.37, with the mass lead rotating among
  all three cultures.
- 9109: drifts from three-way (31/39/26) to Nightshelf 56% by w8 and
  holds. Physical lead passes from Wake-Singers to Nightshelf at w9.

Population per window (selected): 9101 plateaus ~470 then 552 at w12;
9102 plateaus ~380; 9103 300 -> 550 in the final third; 9104 grows to
a plateau ~810; 9106 550 -> 761 in the final third; 9107 steady growth
to 690; 9108 grows to ~910; 9109 plateaus ~560. Horizon densities
0.43-0.62 (band 0.3-0.5; three worlds sit just above the top edge).

## Findings

1. **The census balance is durable but not bounded.** Most worlds lock
   a stable ratio by ~tick 7,500 and hold it for 22,000 more ticks. But
   9106 shows a culture can run to 77% for a multi-window stretch and
   9100 held 57-60% steadily. At the 30k horizon, 8 of 9 worlds sit at
   or under ~62% max share; the envelope is typical, not guaranteed.
2. **Leadership rotates across worlds and, newly, within them.** At
   horizon: Nightshelf leads 5 worlds, Aurora 3, Wake-Singers 1. Two
   worlds (9103, 9107) changed census leaders mid-run and one (9106)
   had an excursion-and-retreat. The 20k-scale finding (rotation across
   worlds) extends to genuine within-world contests at 30k.
3. **Wake-Singers fade at long horizons.** The former fixed hegemon is
   the smallest culture at horizon on 5 of 9 worlds (13-16% on four of
   them) and leads only one. Horizon means are roughly Aurora 33%,
   Nightshelf 42%, Wake-Singers 24%. A mild Nightshelf lean is visible
   at 30k that the 20k fans did not show.
4. **Populations resume growing in the final third** on 6 of 9 worlds
   after plateauing mid-run. Candidate driver: succession-anchor
   accretion - every the_line_continues launch founds a Monumental-band
   point of interest (20k-tick lifespan), so anchors outlive their
   founders and accumulate; on the crashed world POIs reached 204 at
   tick 20k (~2.7x the 20k-horizon norm), and more anchors mean more
   sheltered ground and more eligible succession parents.
5. **The capacity ceiling is real at 30k.** One world crashed on
   telemetry pressure; the surviving heavy worlds (9104, 9108) wrote
   180-213M rows each. Kernel-lane items: the publisher should degrade
   (sample or drop) instead of hard-faulting, and the kind-general
   evaluation telemetry doubles candidate-event volume and deserves a
   cheaper encoding. Both filed in the backlog alongside the seed-9105
   movement-array item.

## Register deltas at 30k (vs the 20k baselines)

Smallest-culture population at horizon: 63-140 (no collapse below the
9101 world-culture floor; Aurora's worst is 9106 at ~65). Death-rate
and time-on-own-ground measures were not re-extracted for all nine
worlds in this pass; the trajectory JSONs are in the session scratchpad
and the telemetry.duckdb files remain under
.artifacts/monte-carlo/kindgen-r20*.
