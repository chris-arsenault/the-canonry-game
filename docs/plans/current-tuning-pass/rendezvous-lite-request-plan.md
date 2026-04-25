# Rendezvous-Lite Request Plan

Current recommended next experiment after the first locality-planning Monte Carlo pass.

## Goal

Test whether the sim needs explicit graph-side demand for other entities to come to a place, without committing to full multi-entity planning or a large new locality-affordance system.

## Core Idea

Introduce short-lived requests anchored to an existing named entity or place.

- normal processes or persistent deficits emit requests
- other entities can independently decide to answer them
- requests become planner demand, not a separate planning system
- movement still obeys the current preconditions

This keeps the graph as the source of meaning while letting locality shape how those demands are fulfilled.

## Why This Is The Next Step

- it directly tests the multi-entity coordination hypothesis
- it fits the current single-actor planner and immediate execution model
- it stays closer to Canonry's graph-first behavior than a large locality-thickening layer
- it is smaller and safer than full cooperation planning

## Proposed Shape

### 1. Request concept

Add a short-lived request object with:

- anchor entity or named place
- responder kind(s)
- desired effect or fulfilling process family
- urgency
- expiry
- satisfaction limit or single-use flag

### 2. Request sources

Start narrow with only a few high-value request types. Examples:

- settlement emits `needs_order`
- artifact emits `needs_witness`
- region emits `needs_sealing`
- faction emits `needs_offerings`

These should bridge kinds rather than only reinforcing same-kind loops.

### 3. Planner integration

Feed requests into the existing planner as another demand source alongside:

- deficits
- standing drives

That means the planner can select "go answer that request" using the same serving-process and target-selection machinery it already has.

### 4. Locality integration

Keep the current locality planner, but let anchored requests contribute to bundle value.

A destination should score higher when it serves:

- one self-directed goal
- plus one anchored request

That creates a practical multi-goal visit without requiring synchronized multi-actor execution.

### 5. Execution model

Keep fulfillment single-actor.

- no atomic joint plan
- no synchronized arrival requirement
- one actor independently chooses to answer
- if multiple actors answer, the existing world-resolution rules decide the outcome

## First Slice

Do not generalize immediately. First slice should be:

1. `2-3` request types only
2. requests anchored to existing named entities only
3. only enough planner integration to treat them as additional demands
4. telemetry for emission, selection, fulfillment, expiry, and duplicate answering

## Monte Carlo Success Criteria

Look for improvement in:

- selected locality multi-goal share
- realized multi-action visits
- cross-kind co-location
- process monopoly
- occupied physical map share
- physical anisotropy

## Failure Conditions

Treat this branch as insufficient if:

- requests mostly go unanswered
- requests only thicken the existing core without improving spread
- bundle size remains effectively one
- process monopoly stays nearly unchanged

If that happens, the next branch to explore is stronger self-sufficiency / sustain-vs-ambition separation.
