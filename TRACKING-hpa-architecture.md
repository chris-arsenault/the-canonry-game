# HPA Architecture Tracking

Status: backlog for long-term movement architecture design.

## Deferred Items

1. Design portal-aware HPA routing before replacing the current segment route
   cache.
   - Problem: the current 5/10-hex HPA route cache is keyed around segment
     pairs, so route planning can choose a next segment and portal that are
     plausible for the segment pair but incoherent for the actor's actual
     current coordinate, entry portal, and final target.
   - Observed symptom: an actor already in the same 10-hex region as its target
     can be routed toward a bad 5-hex portal and move away from the target.
   - Intended architecture: represent high-level path nodes as concrete segment
     boundary portals, not only segment ids, so abstract routing tracks entry
     and exit coordinates through each segment.
   - Required design work: portal generation/pruning, cache key granularity,
     road-mark influence, invalidation cadence, unreachable-route handling, and
     reachability-query cost control.
   - Constraint: do not implement this as a broad routing rewrite without a
     design pass and a 500-tick MC usability check.
