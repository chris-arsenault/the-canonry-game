namespace Engine.Behaviors.Impulses;

#pragma warning disable CA1062 // Impulse parameters validated by UtilityEvaluator framework

using Engine.Kinds;

/// <summary>
/// Registers the default MVP impulses for all entity kinds.
/// </summary>
public static class DefaultImpulses
{
    public static void RegisterAll(ImpulseRegistry registry)
    {
        // Settlements (deterministic, always score 1.0)
        registry.Register(EntityKind.Settlement, new PopulationGrowthImpulse());
        registry.Register(EntityKind.Settlement, new DevelopmentImpulse());

        // Nations (utility AI scored)
        registry.Register(EntityKind.Nation, new BorderTensionImpulse());
        registry.Register(EntityKind.Nation, new ConsolidationImpulse());

        // NPCs (utility AI scored)
        registry.Register(EntityKind.Npc, new HungerImpulse());
        registry.Register(EntityKind.Npc, new AmbitionImpulse());

        // Artifacts (deterministic, always score 1.0)
        registry.Register(EntityKind.Artifact, new DegradationImpulse());
        registry.Register(EntityKind.Artifact, new ResonanceAccumulationImpulse());
    }
}
