# Tick-10,000 UI Performance Finding

Point-in-time finding, 2026-07-18. The UI optimization pass materially improves
the actual Godot managed UI workload at the reported population boundary. It
does not yet prove GPU-rendered desktop performance or establish that every
later, larger world will hold the same frame rate.

## Workload and Method

Both builds used Godot 4.6.2 Mono in Debug configuration, seed 4200, the real
interactive runtime, and a deterministic prewarm to tick 10,000. The controlled
paused snapshot contained 180 projected entities. The live comparison then ran
the simulation writer at 10 ticks per second.

The baseline reconstructed the pre-optimization UI while retaining the same
current simulation and projection implementation. It did not render the new
technology overview, which biases the comparison slightly in the baseline's
favor. The optimized build used the current batched entity layer, incremental
projection invalidation, draw gating, stable browser/legend presentation, and
overlay culling.

The launch command was:

```bash
godot --headless --disable-vsync --max-fps 0 --print-fps --path . -- \
  --prewarm-ticks=10000 --prewarm-tick-rate=10 --seed=4200
```

Whole-process CPU and resident memory were sampled from `/proc` for 10 seconds
without a profiler attached. Separate 15-second `dotnet-trace` sampled-thread
time traces identified the managed/native call stacks. Startup and prewarm were
excluded from every measurement.

Headless mode exercises the real Godot scene tree, C# `_Process` calls,
projection scans, allocation, node traversal, and draw-command construction. It
uses the dummy renderer, so these numbers do not measure GPU rasterization or a
specific desktop compositor.

## Results

The first steady live window after prewarm produced these measurements:

| Metric | Old UI | Optimized UI | Change |
|---|---:|---:|---:|
| Mean reported FPS | 36.2 | 53.1 | +46.5% |
| Process CPU, one-core scale | 172.2% | 134.8% | -21.7% |
| Resident memory | 858,424 KiB | 845,176 KiB | -13,248 KiB (-1.5%) |

The live startup logs raced the writer in the measured build: the old UI logged
180 projected entities and the optimized UI logged 174 after both had reached
the same paused 180-entity snapshot. That 3.3% census difference is a limitation
of the live comparison, not enough to explain the 46.5% FPS gap. The profiling
launch path now binds the exact prewarmed snapshot before resuming the writer so
future comparisons do not have this race.

In the matched paused 180-entity snapshot, both builds reached the headless
driver's roughly 145 FPS ceiling, so frame count could not distinguish them.
The trace still did: sampled native draw-leaf observations fell from 5,299 to
150, a 97.2% reduction, and resident memory was about 12 MiB lower.

The live traces show where the improvement came from:

| Sampled UI stack | Old UI | Optimized UI | Change |
|---|---:|---:|---:|
| `EntityTrailLayer._Draw` | 360 | 165 | -54.2% |
| `MinorActorFlowLayer._Draw` | 325 | 79 | -75.7% |
| `ChronicleAuraLayer._Draw` | 68 | 31 | -54.4% |
| `GameRenderTree.Present` | 151 | 204 | +35.1% |

These are sampled stack observations, not invocation counters. The optimized
build reaches `Present` more often because it renders more frames while spending
substantially fewer samples in the expensive overlay draw paths.

Local trace artifacts are retained under `.artifacts/profiles/`:

- `ui-baseline-tick10000.nettrace`
- `ui-optimized-tick10000.nettrace`
- `ui-baseline-live10tps-tick10000.nettrace`
- `ui-optimized-live10tps-tick10000.nettrace`

## Conclusion

The performance did increase. The strongest user-visible proxy available in
this environment improved by about 47%, and the process used about 22% less CPU
under the live tick-10,000 workload. The controlled paused trace independently
confirms that the old per-frame trail and flow rebuilding was removed rather
than merely displaced.

This is not a claim that the UI is finished. The optimized live run still shows
frame-time variance as the world continues evolving. `ChronicleAuraLayer` and
the broader `GameRenderTree.Present` path are now the next UI investigation
surfaces. A GPU-backed capture on the target desktop is still required before
setting a player-facing frame-rate budget.
