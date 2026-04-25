# General Instructions

Working document for non-analysis operating rules that repeatedly mattered during TIR design and implementation. This is about execution discipline, communication, completion, and domain handling.

Analysis-specific biases live in [analysis-biases.md](./analysis-biases.md).

## Domain Discipline

### 1. Do not rename or reinterpret domain language casually

Failure mode:

- improvise setting language, values, or domain terms without grounding in TIR docs

Instruction:

- use the lore bible, era docs, and established domain naming
- when naming is still under review, stay provisional
- do not silently normalize tentative language into code or docs

## Completion Discipline

### 2. Do not call partial work done

Failure mode:

- treat a thin implementation slice as ticket-complete
- paper over known holes in order to move on

Instruction:

- if work is partial, say exactly what remains
- if the ticket is supposed to be finished, actually finish it

Related rule:

- do not loosen tests just to get green unless the test itself is wrong relative to the intended behavior

### 3. Do not leave stale or duplicate systems behind

Failure mode:

- leave legacy branches, dead abstractions, or duplicate pathways because cleanup feels optional

Instruction:

- stale code and stale docs create future drift
- if a migration is supposed to be done, remove the old path instead of keeping it around "for now"

## Design Session Behavior

These are working rules for interactive design passes.

### 4. Present concrete review sets

Instruction:

- in iterative design turns, present a concrete review set, not vague status chatter
- prefer 4-6 reviewable items when the user is acting as design editor

### 5. Do not stop for irrelevant permission checks

Instruction:

- if a reasonable next step is clear and low-risk, do it
- do not waste turns asking for approval on side issues

### 6. Do not make unilateral naming updates that require review

Instruction:

- if naming is explicitly under review, present it first
- do not silently push reviewed-language decisions into code or docs without that pass

### 7. Inspect before improvising

Instruction:

- if uncertain, inspect the code or data first instead of filling the gap with generic intuition

## Current Applied Reminders

- Movement preconditions are staying.
- Process deck and low-altitude physics come before generator tuning.
