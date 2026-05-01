# System 15: Domain Definition Graph

The definition graph is the typed graph of authored domain content. It is not
runtime world state.

## Purpose

The definition graph keeps the domain honest. It answers questions such as:

- do all referenced kinds, attributes, resources, relationships, pressures, and
  generators exist?
- do semantic stocks point at real domain attributes or resources?
- do generators create connected bundles?
- do authored gameplay nodes declare reads and writes?
- do semantic loops and coverage rules match the intended stock model?

This replaces loose worksheet reasoning with build-time validation.

## Contents

The graph contains:

- kind nodes;
- attribute nodes;
- resource nodes;
- relationship and relationship-fact nodes;
- pressure nodes;
- lifecycle nodes;
- generator nodes;
- gambit nodes;
- gameplay nodes for low-altitude verbs, map signals, and effects;
- semantic stock/flow nodes.

The removed low-altitude process deck is not part of the current definition
graph. Generic behavior bytecode remains only where gambits need it.

## Gameplay Nodes

Gameplay nodes represent reusable mechanics compiled through `Engine.Gameplay`:

- low-altitude verbs;
- map signals;
- attribute effects.

They are included so semantic drift checks see the current primary behavior
surface instead of only older graph concepts.

## Runtime Boundary

The definition graph is built from authored domain config and runtime id
registries. It validates the domain before or during host startup. It is not
queried by the kernel hot path.
