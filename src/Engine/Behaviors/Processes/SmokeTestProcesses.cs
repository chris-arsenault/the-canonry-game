namespace Engine.Behaviors.Processes;

#pragma warning disable CA1062 // Parameters validated by EntityEvaluator framework

using Engine.Graph;
using Engine.Kinds;
using Engine.Messages;
using Engine.Wiring;

/// <summary>
/// Minimal smoke-test processes to validate the framework end-to-end.
/// One per MVP entity kind. NOT real game processes -- will be replaced
/// by designed processes after a proper design session.
/// </summary>
public static class SmokeTestProcesses
{
    public static void RegisterAll(ActivityRegistry registry)
    {
        ArgumentNullException.ThrowIfNull(registry);

        registry.RegisterProcess(EntityKind.Settlement, new SettlementTick());
        registry.RegisterProcess(EntityKind.Nation, new NationTick());
        registry.RegisterProcess(EntityKind.Npc, new NpcTick());
        registry.RegisterProcess(EntityKind.Artifact, new ArtifactTick());
        registry.RegisterProcess(EntityKind.Region, new RegionTick());
    }

    public static void RegisterContracts(WiringValidator validator)
    {
        ArgumentNullException.ThrowIfNull(validator);

        validator.RegisterContract(new WiringContract("settlement_tick",
            reads: new HashSet<AttributeRef> { AttributeRef.Self("prosperity", EntityKind.Settlement) },
            writes: new HashSet<AttributeRef> { AttributeRef.Self("population", EntityKind.Settlement) }));

        validator.RegisterContract(new WiringContract("nation_tick",
            reads: new HashSet<AttributeRef> { AttributeRef.Self("stability", EntityKind.Nation) },
            writes: new HashSet<AttributeRef> { AttributeRef.Self("cohesion", EntityKind.Nation) }));

        validator.RegisterContract(new WiringContract("npc_tick",
            reads: new HashSet<AttributeRef> { AttributeRef.Self("hunger", EntityKind.Npc) },
            writes: new HashSet<AttributeRef> { AttributeRef.Self("hp", EntityKind.Npc) }));

        validator.RegisterContract(new WiringContract("artifact_tick",
            reads: new HashSet<AttributeRef> { AttributeRef.Self("mystical_resonance", EntityKind.Artifact) },
            writes: new HashSet<AttributeRef> { AttributeRef.Self("condition", EntityKind.Artifact) }));

        validator.RegisterContract(new WiringContract("region_tick",
            reads: new HashSet<AttributeRef> { AttributeRef.Self("fertility", EntityKind.Region) },
            writes: new HashSet<AttributeRef> { AttributeRef.Self("habitability", EntityKind.Region) }));

        // Reverse contracts to close the loops (so validator doesn't flag dead ends)
        validator.RegisterContract(new WiringContract("settlement_feedback",
            reads: new HashSet<AttributeRef> { AttributeRef.Self("population", EntityKind.Settlement) },
            writes: new HashSet<AttributeRef> { AttributeRef.Self("prosperity", EntityKind.Settlement) }));

        validator.RegisterContract(new WiringContract("nation_feedback",
            reads: new HashSet<AttributeRef> { AttributeRef.Self("cohesion", EntityKind.Nation) },
            writes: new HashSet<AttributeRef> { AttributeRef.Self("stability", EntityKind.Nation) }));

        validator.RegisterContract(new WiringContract("npc_feedback",
            reads: new HashSet<AttributeRef> { AttributeRef.Self("hp", EntityKind.Npc) },
            writes: new HashSet<AttributeRef> { AttributeRef.Self("hunger", EntityKind.Npc) }));

        validator.RegisterContract(new WiringContract("artifact_feedback",
            reads: new HashSet<AttributeRef> { AttributeRef.Self("condition", EntityKind.Artifact) },
            writes: new HashSet<AttributeRef> { AttributeRef.Self("mystical_resonance", EntityKind.Artifact) }));

        validator.RegisterContract(new WiringContract("region_feedback",
            reads: new HashSet<AttributeRef> { AttributeRef.Self("habitability", EntityKind.Region) },
            writes: new HashSet<AttributeRef> { AttributeRef.Self("fertility", EntityKind.Region) }));
    }
}

/// <summary>Settlement: prosperity drives population (simplified).</summary>
internal sealed class SettlementTick : IProcess
{
    public string Name => "settlement_tick";
    public bool CanExecute(Entity entity, IWorldState worldState) => true;

    public float Score(Entity entity, IWorldState worldState)
    {
        return 0.5f; // Always moderately viable
    }

    public SimulationMessage Execute(Entity entity, IWorldState worldState)
    {
        var prosperity = worldState.Attributes.Get(entity.Id, "prosperity");
        var delta = (prosperity - 50f) * 0.01f; // Above 50 = growth, below 50 = decline
        worldState.Attributes.Modify(entity.Id, "population", delta);

        return new ProcessExecutedMessage(worldState.CurrentTick, entity.Id, entity.Kind,
            Name, $"{entity.Name}: population {(delta >= 0 ? "grew" : "declined")} ({delta:+0.0;-0.0})");
    }
}

/// <summary>Nation: stability drives cohesion (simplified).</summary>
internal sealed class NationTick : IProcess
{
    public string Name => "nation_tick";
    public bool CanExecute(Entity entity, IWorldState worldState) => true;

    public float Score(Entity entity, IWorldState worldState)
    {
        return 0.5f;
    }

    public SimulationMessage Execute(Entity entity, IWorldState worldState)
    {
        var stability = worldState.Attributes.Get(entity.Id, "stability");
        var delta = (stability - 50f) * 0.005f;
        worldState.Attributes.Modify(entity.Id, "cohesion", delta);

        return new ProcessExecutedMessage(worldState.CurrentTick, entity.Id, entity.Kind,
            Name, $"{entity.Name}: cohesion {(delta >= 0 ? "held" : "strained")} ({delta:+0.0;-0.0})");
    }
}

/// <summary>NPC: hunger drains HP (simplified).</summary>
internal sealed class NpcTick : IProcess
{
    public string Name => "npc_tick";
    public bool CanExecute(Entity entity, IWorldState worldState) => true;

    public float Score(Entity entity, IWorldState worldState)
    {
        var hunger = worldState.Attributes.Get(entity.Id, "hunger");
        return 0.3f + (hunger / 200f); // Hungrier = more likely
    }

    public SimulationMessage Execute(Entity entity, IWorldState worldState)
    {
        var hunger = worldState.Attributes.Get(entity.Id, "hunger");
        worldState.Attributes.Modify(entity.Id, "hunger", 0.2f); // Hunger increases
        var hpDrain = hunger * 0.005f; // Hunger causes HP loss
        worldState.Attributes.Modify(entity.Id, "hp", -hpDrain);

        return new ProcessExecutedMessage(worldState.CurrentTick, entity.Id, entity.Kind,
            Name, $"{entity.Name}: hunger {hunger:F0}, HP drain {hpDrain:F2}");
    }
}

/// <summary>Artifact: resonance slows degradation (simplified).</summary>
internal sealed class ArtifactTick : IProcess
{
    public string Name => "artifact_tick";
    public bool CanExecute(Entity entity, IWorldState worldState) => true;

    public float Score(Entity entity, IWorldState worldState)
    {
        return 0.5f;
    }

    public SimulationMessage Execute(Entity entity, IWorldState worldState)
    {
        var resonance = worldState.Attributes.Get(entity.Id, "mystical_resonance");
        worldState.Attributes.Modify(entity.Id, "mystical_resonance", 0.05f); // Accumulate
        var repair = resonance * 0.002f; // Resonance slows degradation
        worldState.Attributes.Modify(entity.Id, "condition", repair);

        return new ProcessExecutedMessage(worldState.CurrentTick, entity.Id, entity.Kind,
            Name, $"{entity.Name}: resonance {resonance:F1}, repair {repair:F3}");
    }
}

/// <summary>Region: fertility drives habitability (simplified).</summary>
internal sealed class RegionTick : IProcess
{
    public string Name => "region_tick";
    public bool CanExecute(Entity entity, IWorldState worldState) => true;

    public float Score(Entity entity, IWorldState worldState)
    {
        return 0.3f; // Low priority, regions are mostly passive
    }

    public SimulationMessage Execute(Entity entity, IWorldState worldState)
    {
        // Regions just maintain habitability based on their inherent fertility
        // (This is a placeholder -- real process would read map cell data)
        var fertility = worldState.Attributes.Get(entity.Id, "fertility");
        worldState.Attributes.Modify(entity.Id, "habitability", (fertility - 0.5f) * 0.01f);

        return new ProcessExecutedMessage(worldState.CurrentTick, entity.Id, entity.Kind,
            Name, $"{entity.Name}: habitability adjusted");
    }
}
