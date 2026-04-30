# Low-Altitude Domain Mixing Inspection Plan

Extraction target: reusable gameplay mechanics should live in `Engine.Gameplay`,
domain content should select/configure those mechanics, and the kernel should
only execute primitive compiled data. `Engine.Domain` may still own domain names,
graph semantics, and id resolution, but it should not be the long-term home for
gameplay-policy compilers or broad low-altitude behavior shapes.

Reconciled after the low-altitude extraction on 2026-04-30. The original issue
text is preserved below; reconciliation notes are appended under each item.

1. High: `LowAltitudeVerbProgramRuntime` directly reads generator definitions to weight low-altitude targets. See [LowAltitudeVerbProgramRuntime.cs](/home/dev/repos/the-canonry-game/src/Engine.Kernel/Programs/LowAltitudeVerbs/LowAltitudeVerbProgramRuntime.cs:658), [LowAltitudeVerbProgramRuntime.cs](/home/dev/repos/the-canonry-game/src/Engine.Kernel/Programs/LowAltitudeVerbs/LowAltitudeVerbProgramRuntime.cs:743), and [LowAltitudeVerbProgramRuntime.cs](/home/dev/repos/the-canonry-game/src/Engine.Kernel/Programs/LowAltitudeVerbs/LowAltitudeVerbProgramRuntime.cs:748). This is altitude mixing. Low altitude should consume generic map/graph signals, not inspect high-altitude generator topology. It also peeks at another kernel program's raw data table: `simulation.Generators.Data.TargetKindIds`, `BindingStarts`, `BindingCounts`, and `BindingKindIds`. See [LowAltitudeVerbProgramRuntime.cs](/home/dev/repos/the-canonry-game/src/Engine.Kernel/Programs/LowAltitudeVerbs/LowAltitudeVerbProgramRuntime.cs:755). If `GeneratorProgramData` changes layout, the verb runtime breaks. Either expose a query on `GeneratorProgramRuntime` or denormalize the affordance graph into verb data at compile time.

   Status: closed. The runtime no longer reads generator raw data tables. Generator affordance is denormalized through gameplay compilation into low-altitude primitive affordance pairs.

2. High: `LowAltitudeVerbProgramData` has become a wide union type for movement, influence, coverage growth, anchor relocation, terrain targeting, relationship targeting, relationship affinity, and generator affordance. See [LowAltitudeVerbProgramData.cs](/home/dev/repos/the-canonry-game/src/Engine.Kernel/Programs/LowAltitudeVerbs/LowAltitudeVerbProgramData.cs:7) and [LowAltitudeVerbProgramData.cs](/home/dev/repos/the-canonry-game/src/Engine.Kernel/Programs/LowAltitudeVerbs/LowAltitudeVerbProgramData.cs:82). This is not TIR-specific, but it is too domain-shaped and allows invalid combinations unless heavily validated. `RegisterVerb(...)` takes a large primitive payload, and each new verb shape requires changes across constants, kernel runtime, and domain compilation. `LowAltitudeVerbConfig` allows arbitrary operation/target/plane combinations, and the compiler mostly passes them through. See [DomainConfig.cs](/home/dev/repos/the-canonry-game/src/Engine.Domain/Config/DomainConfig.cs:119) and [DomainCompiler.LowAltitude.cs](/home/dev/repos/the-canonry-game/src/Engine.Domain/Compilation/DomainCompiler.LowAltitude.cs:33). That leaves domain-rule coupling in the kernel instead of in domain compilation.

   Status: closed. Kernel constants/runtime now use primitive opcode/selector tables, gameplay vocabulary moved to `Engine.Gameplay`, and `GameplayValidator` rejects illegal operation/target/plane combinations before runtime. Authored low-altitude verbs are split into shape-specific gameplay configs (`MovementLowAltitudeVerbConfig`, `InfluenceLowAltitudeVerbConfig`, `CoverageExpansionLowAltitudeVerbConfig`, and `AnchorRelocationLowAltitudeVerbConfig`) while the compiled kernel payload remains primitive.

3. Medium: home influence seeding is hidden engine behavior. Any kind with influence verbs gets `35` influence seeded on both planes and skips normal action that tick. See [LowAltitudeVerbProgramRuntime.cs](/home/dev/repos/the-canonry-game/src/Engine.Kernel/Programs/LowAltitudeVerbs/LowAltitudeVerbProgramRuntime.cs:157), [LowAltitudeVerbProgramRuntime.cs](/home/dev/repos/the-canonry-game/src/Engine.Kernel/Programs/LowAltitudeVerbs/LowAltitudeVerbProgramRuntime.cs:175), and [LowAltitudeVerbProgramState.cs](/home/dev/repos/the-canonry-game/src/Engine.Kernel/Programs/LowAltitudeVerbs/LowAltitudeVerbProgramState.cs:22). This should be explicit seed/config data, not implicit runtime magic.

   Status: closed. Initial influence is now explicit gameplay/domain config via `GameplayDefinition.InitialInfluence`; the kernel applies configured rules but no longer invents the default.

4. Medium: `MoveHeadquarters` is a domain/gameplay term in kernel constants and behavior. See [KernelCodes.cs](/home/dev/repos/the-canonry-game/src/Engine.Constants/KernelCodes.cs:289) and [LowAltitudeVerbProgramRuntime.cs](/home/dev/repos/the-canonry-game/src/Engine.Kernel/Programs/LowAltitudeVerbs/LowAltitudeVerbProgramRuntime.cs:914). If this remains kernel-level, it should be a generic `MoveAnchor`/`RelocateEntity` primitive with plane validation.

   Status: closed at the kernel boundary. The kernel opcode is now the generic `ProgramOpcodeCodes.TryRelocateEntity`. The authored gameplay vocabulary still has `MoveHeadquarters`, which is acceptable as gameplay/domain language.

5. Medium: `ExpandCoverage` and `MoveHeadquarters` are physically implemented even though verb rules are plane-configured. See [LowAltitudeVerbProgramRuntime.cs](/home/dev/repos/the-canonry-game/src/Engine.Kernel/Programs/LowAltitudeVerbs/LowAltitudeVerbProgramRuntime.cs:824) and [LowAltitudeVerbProgramRuntime.cs](/home/dev/repos/the-canonry-game/src/Engine.Kernel/Programs/LowAltitudeVerbs/LowAltitudeVerbProgramRuntime.cs:922). Semantic terrain targets also return no candidate at runtime instead of being rejected at compile time. See [LowAltitudeVerbProgramRuntime.cs](/home/dev/repos/the-canonry-game/src/Engine.Kernel/Programs/LowAltitudeVerbs/LowAltitudeVerbProgramRuntime.cs:439). Either compile should reject non-physical uses, or the operation should encode plane capability explicitly.

   Status: closed. The operations compile to primitive steps, and gameplay validation rejects semantic terrain targets plus semantic-plane use of physical coverage/relocation semantics before runtime.

6. Medium: `MapSignals` special-cases health as the scalar feeding attractor mass. See [MapSignalsProgramData.cs](/home/dev/repos/the-canonry-game/src/Engine.Kernel/Programs/MapSignals/MapSignalsProgramData.cs:8) and [MapSignalsProgramRuntime.cs](/home/dev/repos/the-canonry-game/src/Engine.Kernel/Programs/MapSignals/MapSignalsProgramRuntime.cs:272). This is acceptable only if health is truly a kernel concept; otherwise it should be generalized to configured scalar signal inputs.

   Status: closed. Map-signal attractor mass now consumes configured scalar inputs. Health can be selected by the domain/gameplay config, but the map-signal program no longer treats health as a special kernel policy.

7. Medium: relationship maintenance plane handling is inconsistent. `ContactMemory` ignores the configured maintenance plane when recording contact; it records contact if physical distance is `<= 1` or semantic distance is `0` for every relationship. See [RelationshipProgramRuntime.cs](/home/dev/repos/the-canonry-game/src/Engine.Kernel/Programs/Relationships/RelationshipProgramRuntime.cs:85). Influence-based maintenance also drops `RelationshipMaintenanceRuleConfig.Plane` during compilation, then runtime defaults `None` to physical. See [DomainConfig.cs](/home/dev/repos/the-canonry-game/src/Engine.Domain/Config/DomainConfig.cs:250), [DomainCompiler.Mapping.cs](/home/dev/repos/the-canonry-game/src/Engine.Domain/Compilation/DomainCompiler.Mapping.cs:89), and [RelationshipProgramRuntime.cs](/home/dev/repos/the-canonry-game/src/Engine.Kernel/Programs/Relationships/RelationshipProgramRuntime.cs:292). That means semantic influence maintenance cannot actually be authored despite the intended plane-aware relationship maintenance model.

   Status: closed. The compiler preserves planes for influence/contact conditions, contact memory uses configured maintenance plane, and runtime no longer defaults missing influence plane to physical evidence.

8. Low: semantic random target bounds reuse physical `World.MapRadius`. See [LowAltitudeVerbProgramRuntime.cs](/home/dev/repos/the-canonry-game/src/Engine.Kernel/Programs/LowAltitudeVerbs/LowAltitudeVerbProgramRuntime.cs:983). Fine as a temporary axial space, but it should become a plane config if semantic space remains first-class.

   Status: closed. Semantic bounds are explicit gameplay config (`SemanticPlaneConfig`), compiled into `CompiledSemanticPlaneBounds`, installed through the host, and enforced by low-altitude semantic targeting.

9. Low: legacy `MovementProgram` still exists and is compiled/installed disabled by default. See [DomainCompiler.LowAltitude.cs](/home/dev/repos/the-canonry-game/src/Engine.Domain/Compilation/DomainCompiler.LowAltitude.cs:9) and [Simulation.cs](/home/dev/repos/the-canonry-game/src/Engine.Kernel/Simulation.cs:198). It is not active by default, but it is a parallel movement path that can confuse future analysis.

   Status: closed. The legacy `MovementProgram` files, old domain compiler movement-program path, stale `KernelProgramCodes.Movement`, config-file movement mapping, movement source code id, snapshot fields, and stale test names are removed from the live system.

10. High: the branch replaces generated processes with direct low-altitude program config, but the definition graph does not yet own or validate that new primary behavior. Ice Remembers now sets `Processes = []` and installs verbs/effects/map signals instead. See [IceRemembersDefinition.cs](/home/dev/repos/the-canonry-game/src/IceRemembers/IceRemembersDefinition.cs:45). `DefinitionGraphBuilder` still models processes, gambits, generators, resources, relationships, pressures, lifecycles, and attributes, but not low-altitude verbs, effects, or map signals. See [DefinitionGraphBuilder.cs](/home/dev/repos/the-canonry-game/src/Engine.Domain/Graph/DefinitionGraphBuilder.cs:19). That may be the right direction, but it leaves the new primary behavior outside the semantic drift checks the architecture describes.

    Status: closed. The definition graph now includes gameplay nodes for low-altitude verbs, map signals, and effects, with read/write modeling and validator hooks.

11. Medium: program sequencing and responsibility boundaries are drifting from the low-altitude plan. Current registration order is `Movement` -> `LowAltitudeVerbs` -> `MapActivity` -> `MapSignals` -> `Effects` -> `Upkeep` -> `Relationships` -> `Processes` -> `Gambits` -> `Pressures` -> `Generators` -> `Degeneracy`. See [Simulation.cs](/home/dev/repos/the-canonry-game/src/Engine.Kernel/Simulation.cs:196). `MapSignals` reads `MapActivity`, and `Effects` reads `MapSignals`, so those dependencies are order-sensitive. `LowAltitudeVerbs` also reads `MapActivity` and `MapSignals` but currently runs before both, so verbs intentionally or accidentally see last tick's signals. If the one-tick delay is acceptable, it must be explicit and protected from accidental reorder. If relationship drift is meant to run after movement/activity and before lifecycle/upkeep, the current `Effects`/`Upkeep`/`Relationships` order is wrong. This also interacts with item 9: `MovementProgram` exists but is disabled by default while `LowAltitudeVerbs` handles movement too, so the movement responsibility split should be clarified before it hardens.

    Status: closed. `Movement` is no longer in the registration order. Relationship maintenance now runs after `MapSignals` and before `Effects`/`Upkeep`, matching the low-altitude plan. The current dependency intent is explicit and startup-validated: `MapSignals` reads same-tick `MapActivity`, `Effects` reads same-tick `MapSignals`, and `LowAltitudeVerbs` reads previous-tick activity/signals through `LowAltitudeSignalSnapshot`.

12. Medium: cross-program coupling now forms an implicit dependency DAG through the `Simulation` god-object. `MapSignals` reads `MapActivity`, world influence, and coverage; `Effects` reads `MapSignals`; `LowAltitudeVerbs` reads `MapActivity`, `MapSignals`, and generator data; `Movement` reads `MapActivity`. These are not domain-string leaks, but they are architectural dependencies with no declared contract. The kernel needs either explicit program dependency metadata/order validation or a clear convention that these reads use previous-tick state.

    Status: closed for the current low-altitude graph. `LowAltitudeVerbs` no longer reads `MapActivity`, `MapSignals`, or generator data directly; it reads a snapshot and compiled affordance tables. Dependency metadata and startup validation cover the current same-tick and previous-tick derived-signal edges. Future cross-program derived reads should extend this same metadata contract rather than adding implicit ordering.

13. Medium: per-cell per-entity influence has become a substantial ownership/territoriality system under the "low-altitude physics" label. It is strategically defensible, but `GrowInfluence`, `ReduceInfluence`, `ExpandCoverage`, and `MoveHeadquarters` are more than drift on existing strengths; they create a new territorial control layer that can promote cells into coverage ownership. See [LowAltitudeVerbProgramRuntime.cs](/home/dev/repos/the-canonry-game/src/Engine.Kernel/Programs/LowAltitudeVerbs/LowAltitudeVerbProgramRuntime.cs:804). This should be explicitly acknowledged as a first-class low-altitude map-control system, not treated as incidental physics.

    Status: closed. Influence/map control is now represented as first-class gameplay config (`MapControl`, `Influence`, `Coverage`, `Territory`) rather than incidental physics.

## Remaining Issues

No open issues remain in this tracker. New cross-program derived reads, new low-altitude verb shapes, or new map-signal scalar drivers should open fresh tracker items with tests instead of reopening this cleanup slice.

## Clean Areas

The hard domain-string boundary appears to hold. Kernel/eventing/constants code
does not contain TIR nouns such as penguin/orca/warmth/witness, and mutable
kernel surfaces remain primitive arrays sized at construction.

`MapActivity`, `Effects`, and most of `RelationshipProgram` are mostly generic
numeric programs. They consume ids and condition codes rather than TIR strings.
The main problems are not literal domain names; they are hidden gameplay rules,
altitude coupling, implicit cross-program dependencies, and the low-altitude
verb program becoming too broad.
