namespace Engine.Domain;

using Engine.Cards;
using Engine.Graph;
using Engine.Kinds;
using Engine.Pressure;
using Engine.Resources;
using Engine.Wiring;

/// <summary>
/// Test domain providing generic placeholder content for Phase 1 development.
/// All values that were previously hard-coded in Default*.cs files live here.
/// Swap this with a real domain (The Ice Remembers) in Phase 2.
/// </summary>
public static class TestDomain
{
    public static DomainConfig Create()
    {
        return new DomainConfig
        {
            Kinds = CreateKinds(),
            Processes = CreateProcesses(),
            Relationships = CreateRelationships(),
            Pressures = CreatePressures(),
            Lifecycles = CreateLifecycles(),
            Resources = CreateResources(),
            Cultures = CreateCultures(),
        };
    }

    private static List<ProcessDefinition> CreateProcesses()
    {
        var processes = new List<ProcessDefinition>();

        // Settlement: prosperity → population (feedback: population → prosperity via NPC presence)
        var settlementTick = new ProcessDefinition("settlement_tick", "Settlement Tick",
            DefinitionRole.Process, EntityKind.Settlement, new AlwaysTrue(), new ConstantScore(0.5f, variance: 0.15f))
        { Mutations = { new ModifySelfAttributeMutation("population", 0.1f, variance: 0.3f) } };
        settlementTick.Reads.Add(AttributeRef.Self("prosperity", EntityKind.Settlement));
        settlementTick.Writes.Add(AttributeRef.Self("population", EntityKind.Settlement));
        processes.Add(settlementTick);

        // Settlement feedback: population → prosperity
        var settlementFeedback = new ProcessDefinition("settlement_feedback", "Settlement Feedback",
            DefinitionRole.Process, EntityKind.Settlement, new AlwaysTrue(), new ConstantScore(0.4f, variance: 0.1f))
        { Mutations = { new ModifySelfAttributeMutation("prosperity", 0.05f, variance: 0.25f) } };
        settlementFeedback.Reads.Add(AttributeRef.Self("population", EntityKind.Settlement));
        settlementFeedback.Writes.Add(AttributeRef.Self("prosperity", EntityKind.Settlement));
        processes.Add(settlementFeedback);

        // Nation: stability → cohesion (feedback: cohesion → stability)
        var nationTick = new ProcessDefinition("nation_tick", "Nation Tick",
            DefinitionRole.Process, EntityKind.Nation, new AlwaysTrue(), new ConstantScore(0.5f, variance: 0.15f))
        { Mutations = { new ModifySelfAttributeMutation("cohesion", -0.01f, variance: 0.3f) } };
        nationTick.Reads.Add(AttributeRef.Self("stability", EntityKind.Nation));
        nationTick.Writes.Add(AttributeRef.Self("cohesion", EntityKind.Nation));
        processes.Add(nationTick);

        var nationFeedback = new ProcessDefinition("nation_feedback", "Nation Feedback",
            DefinitionRole.Process, EntityKind.Nation, new AlwaysTrue(), new ConstantScore(0.4f, variance: 0.1f))
        { Mutations = { new ModifySelfAttributeMutation("stability", 0.005f, variance: 0.25f) } };
        nationFeedback.Reads.Add(AttributeRef.Self("cohesion", EntityKind.Nation));
        nationFeedback.Writes.Add(AttributeRef.Self("stability", EntityKind.Nation));
        processes.Add(nationFeedback);

        // NPC: hunger increases (feedback: hp → hunger via lifecycle)
        var npcTick = new ProcessDefinition("npc_tick", "NPC Tick",
            DefinitionRole.Process, EntityKind.Npc, new AlwaysTrue(), new ConstantScore(0.4f, variance: 0.15f))
        { Mutations = { new ModifySelfAttributeMutation("hunger", 0.2f, variance: 0.4f) } };
        npcTick.Reads.Add(AttributeRef.Self("hp", EntityKind.Npc));
        npcTick.Writes.Add(AttributeRef.Self("hunger", EntityKind.Npc));
        processes.Add(npcTick);

        var npcFeedback = new ProcessDefinition("npc_feedback", "NPC Feedback",
            DefinitionRole.Process, EntityKind.Npc, new AlwaysTrue(), new ConstantScore(0.3f, variance: 0.1f))
        { Mutations = { new ModifySelfAttributeMutation("hp", -0.1f, variance: 0.3f) } };
        npcFeedback.Reads.Add(AttributeRef.Self("hunger", EntityKind.Npc));
        npcFeedback.Writes.Add(AttributeRef.Self("hp", EntityKind.Npc));
        processes.Add(npcFeedback);

        // NPC: eat — consume sustenance at current hex, reduce hunger.
        // Precondition checks food at current location. If food is elsewhere,
        // the evaluator will still select eat (scored against distant providers)
        // and step the NPC toward the provider until it arrives.
        var npcEat = new ProcessDefinition("npc_eat", "Eat",
            DefinitionRole.Process, EntityKind.Npc,
            new ResourceAvailableAtSelf(ResourceType.Sustenance),
            new AttributeRatioScore("hunger", 100f, variance: 0.1f))
        { Mutations = { new ConsumeResourceAtSelfMutation(ResourceType.Sustenance, 5f, "hunger", variance: 0.3f) } };
        npcEat.Reads.Add(AttributeRef.Self("hunger", EntityKind.Npc));
        npcEat.Reads.Add(AttributeRef.Resource("sustenance"));
        npcEat.Writes.Add(AttributeRef.Self("hunger", EntityKind.Npc));
        npcEat.Writes.Add(AttributeRef.Resource("sustenance"));
        processes.Add(npcEat);

        // Artifact: resonance ↔ condition
        var artifactTick = new ProcessDefinition("artifact_tick", "Artifact Tick",
            DefinitionRole.Process, EntityKind.Artifact, new AlwaysTrue(), new ConstantScore(0.5f, variance: 0.1f))
        { Mutations = { new ModifySelfAttributeMutation("mystical_resonance", 0.05f, variance: 0.2f) } };
        artifactTick.Reads.Add(AttributeRef.Self("condition", EntityKind.Artifact));
        artifactTick.Writes.Add(AttributeRef.Self("mystical_resonance", EntityKind.Artifact));
        processes.Add(artifactTick);

        var artifactFeedback = new ProcessDefinition("artifact_feedback", "Artifact Feedback",
            DefinitionRole.Process, EntityKind.Artifact, new AlwaysTrue(), new ConstantScore(0.4f, variance: 0.1f))
        { Mutations = { new ModifySelfAttributeMutation("condition", 0.01f, variance: 0.2f) } };
        artifactFeedback.Reads.Add(AttributeRef.Self("mystical_resonance", EntityKind.Artifact));
        artifactFeedback.Writes.Add(AttributeRef.Self("condition", EntityKind.Artifact));
        processes.Add(artifactFeedback);

        // Region: fertility ↔ habitability
        var regionTick = new ProcessDefinition("region_tick", "Region Tick",
            DefinitionRole.Process, EntityKind.Region, new AlwaysTrue(), new ConstantScore(0.3f, variance: 0.1f))
        { Mutations = { new ModifySelfAttributeMutation("habitability", 0.001f, variance: 0.2f) } };
        regionTick.Reads.Add(AttributeRef.Self("fertility", EntityKind.Region));
        regionTick.Writes.Add(AttributeRef.Self("habitability", EntityKind.Region));
        processes.Add(regionTick);

        var regionFeedback = new ProcessDefinition("region_feedback", "Region Feedback",
            DefinitionRole.Process, EntityKind.Region, new AlwaysTrue(), new ConstantScore(0.2f, variance: 0.05f))
        { Mutations = { new ModifySelfAttributeMutation("fertility", 0.0001f, variance: 0.2f) } };
        regionFeedback.Reads.Add(AttributeRef.Self("habitability", EntityKind.Region));
        regionFeedback.Writes.Add(AttributeRef.Self("fertility", EntityKind.Region));
        processes.Add(regionFeedback);

        return processes;
    }

    private static Dictionary<EntityKind, KindConfig> CreateKinds()
    {
        return new Dictionary<EntityKind, KindConfig>
        {
            [EntityKind.Region] = new()
            {
                Subtypes = new HashSet<string> { "generic_region", "tundra", "coastal", "mountain", "plains" },
                Attributes =
                [
                    new() { Name = "fertility", Min = 0f, Max = 1f, Default = 0.5f },
                    new() { Name = "resource_richness", Min = 0f, Max = 1f, Default = 0.3f },
                    new() { Name = "habitability", Min = 0f, Max = 1f, Default = 0.5f },
                ],
                HealthAttribute = null,
            },
            [EntityKind.Settlement] = new()
            {
                Subtypes = new HashSet<string> { "generic_settlement", "village", "town", "city", "fortress", "ruin" },
                Attributes =
                [
                    new() { Name = "population", Min = 0f, Max = 100000f, Default = 100f },
                    new() { Name = "development", Min = 0f, Max = 100f, Default = 10f },
                    new() { Name = "prosperity", Min = 0f, Max = 100f, Default = 50f },
                    new() { Name = "defense", Min = 0f, Max = 100f, Default = 10f },
                ],
                HealthAttribute = "population",
            },
            [EntityKind.Nation] = new()
            {
                Subtypes = new HashSet<string> { "generic_nation", "monarchy", "republic", "tribal" },
                Attributes =
                [
                    new() { Name = "stability", Min = 0f, Max = 100f, Default = 50f },
                    new() { Name = "military_strength", Min = 0f, Max = 100f, Default = 20f },
                    new() { Name = "cohesion", Min = 0f, Max = 100f, Default = 70f },
                    new() { Name = "legitimacy", Min = 0f, Max = 100f, Default = 50f },
                ],
                HealthAttribute = "cohesion",
            },
            [EntityKind.Npc] = new()
            {
                Subtypes = new HashSet<string> { "generic_npc", "hero", "merchant", "scholar", "warrior" },
                Attributes =
                [
                    new() { Name = "hp", Min = 0f, Max = 100f, Default = 100f },
                    new() { Name = "hunger", Min = 0f, Max = 100f, Default = 0f },
                    new() { Name = "ambition", Min = 0f, Max = 100f, Default = 50f },
                    new() { Name = "age", Min = 0f, Max = 1000f, Default = 20f },
                ],
                HealthAttribute = "hp",
            },
            [EntityKind.Artifact] = new()
            {
                Subtypes = new HashSet<string> { "generic_artifact", "weapon", "armor", "instrument", "relic", "scroll" },
                Attributes =
                [
                    new() { Name = "condition", Min = 0f, Max = 100f, Default = 100f },
                    new() { Name = "mystical_resonance", Min = 0f, Max = 100f, Default = 0f },
                    new() { Name = "power", Min = 0f, Max = 100f, Default = 10f },
                ],
                HealthAttribute = "condition",
            },
        };
    }

    private static List<RelationshipConfig> CreateRelationships()
    {
        var spatial = new HashSet<EntityCategory> { EntityCategory.Spatial };
        var agency = new HashSet<EntityCategory> { EntityCategory.Agency };
        var asset = new HashSet<EntityCategory> { EntityCategory.Asset };
        var all = new HashSet<EntityCategory> { EntityCategory.Spatial, EntityCategory.Agency, EntityCategory.Asset, EntityCategory.Record };

        return
        [
            new() { Name = "borders", Polarity = RelationshipPolarity.Neutral, ValidSourceCategories = spatial, ValidTargetCategories = spatial, IsSymmetric = true },
            new() { Name = "contains", Polarity = RelationshipPolarity.Neutral, ValidSourceCategories = spatial, ValidTargetCategories = spatial },
            new() { Name = "controlled_by", Polarity = RelationshipPolarity.Neutral, ValidSourceCategories = spatial, ValidTargetCategories = agency, IsExclusive = true },
            new() { Name = "located_at", Polarity = RelationshipPolarity.Neutral, ValidSourceCategories = agency, ValidTargetCategories = spatial, IsExclusive = true },
            new() { Name = "home_base", Polarity = RelationshipPolarity.Neutral, ValidSourceCategories = agency, ValidTargetCategories = spatial, IsExclusive = true },
            new() { Name = "allied_with", Polarity = RelationshipPolarity.Positive, ValidSourceCategories = agency, ValidTargetCategories = agency, IsSymmetric = true, CullThreshold = 0.1f },
            new() { Name = "rival_of", Polarity = RelationshipPolarity.Negative, ValidSourceCategories = agency, ValidTargetCategories = agency, CullThreshold = 0.05f },
            new() { Name = "led_by", Polarity = RelationshipPolarity.Neutral, ValidSourceCategories = agency, ValidTargetCategories = agency, IsExclusive = true },
            new() { Name = "member_of", Polarity = RelationshipPolarity.Neutral, ValidSourceCategories = agency, ValidTargetCategories = agency },
            new() { Name = "loyal_to", Polarity = RelationshipPolarity.Positive, ValidSourceCategories = agency, ValidTargetCategories = agency },
            new() { Name = "holds", Polarity = RelationshipPolarity.Neutral, ValidSourceCategories = agency, ValidTargetCategories = asset },
            new() { Name = "created_by", Polarity = RelationshipPolarity.Neutral, ValidSourceCategories = asset, ValidTargetCategories = agency },
            new() { Name = "stored_at", Polarity = RelationshipPolarity.Neutral, ValidSourceCategories = asset, ValidTargetCategories = spatial, IsExclusive = true },
            new() { Name = "involved_in", Polarity = RelationshipPolarity.Neutral, ValidSourceCategories = all, ValidTargetCategories = all },
        ];
    }

    private static List<PressureConfig> CreatePressures()
    {
        return
        [
            new() { Name = "military_tension", Scope = PressureScope.Regional, Baseline = 0f, HomeostasisRate = 0.05f, SmoothingFactor = 0.2f },
            new() { Name = "economic_stress", Scope = PressureScope.Regional, Baseline = 0f, HomeostasisRate = 0.08f, SmoothingFactor = 0.25f },
            new() { Name = "cultural_friction", Scope = PressureScope.Regional, Baseline = 0f, HomeostasisRate = 0.06f, SmoothingFactor = 0.2f },
            new() { Name = "demographic_pressure", Scope = PressureScope.Regional, Baseline = 0f, HomeostasisRate = 0.1f, SmoothingFactor = 0.3f },
        ];
    }

    private static Dictionary<EntityKind, Domain.LifecycleConfig> CreateLifecycles()
    {
        return new Dictionary<EntityKind, Domain.LifecycleConfig>
        {
            [EntityKind.Settlement] = new() { HealthAttribute = "population", BaseRate = 0.05f, AgeCoefficient = 0.0001f },
            [EntityKind.Nation] = new() { HealthAttribute = "cohesion", BaseRate = 0.01f, AgeCoefficient = 0.00005f },
            [EntityKind.Npc] = new() { HealthAttribute = "hp", BaseRate = 0.02f, AgeCoefficient = 0.0005f },
            [EntityKind.Artifact] = new() { HealthAttribute = "condition", BaseRate = 0.01f, AgeCoefficient = 0.0001f },
        };
    }

    private static List<ResourceConfig> CreateResources()
    {
        return
        [
            new() { Name = "sustenance", DisplayName = "Sustenance" },
            new() { Name = "gold", DisplayName = "Gold" },
        ];
    }

    private static List<CultureConfig> CreateCultures()
    {
        return
        [
            new() { Id = "culture_a", DisplayName = "Culture A" },
            new() { Id = "culture_b", DisplayName = "Culture B" },
        ];
    }
}
