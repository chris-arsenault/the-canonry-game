namespace Engine.Domain;

using Engine.Processes;
using Engine.Graph;
using Engine.Kinds;
using Engine.Pressure;
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

        // Settlement: stockpile sustenance
        // TODO: production should scale with terrain fertility (#75)
        var stockpile = new ProcessDefinition("settlement_stockpile", "Stockpile",
            DefinitionRole.Process, EntityKind.Settlement, new AlwaysTrue(),
            Expr.Score(Expr.Lit(0.6f), 0.1f))
        { Cooldown = 3, EntityCooldown = 2, Mutations = { new ModifySelfAttributeMutation("sustenance", 1f, variance: 0.2f) } };
        stockpile.Writes.Add(AttributeRef.Self("sustenance", EntityKind.Settlement));
        processes.Add(stockpile);

        // Settlement: convert sustenance surplus into population growth
        var grow = new ProcessDefinition("settlement_grow", "Grow",
            DefinitionRole.Process, EntityKind.Settlement,
            new SelfAttributeThreshold("sustenance", min: 5f, max: null),
            Expr.Score(Expr.Div(Expr.Self("sustenance"), Expr.Lit(100f)), 0.1f))
        { Cooldown = 5, EntityCooldown = 2, Mutations = {
            new ModifySelfAttributeMutation("population", 1f, variance: 0.3f),
            new ModifySelfAttributeMutation("sustenance", -5f, variance: 0.2f),
        } };
        grow.Reads.Add(AttributeRef.Self("sustenance", EntityKind.Settlement));
        grow.Writes.Add(AttributeRef.Self("population", EntityKind.Settlement));
        grow.Writes.Add(AttributeRef.Self("sustenance", EntityKind.Settlement));
        processes.Add(grow);

        // Settlement: propaganda — shift target ideology toward self, attenuated by distance
        var propaganda = new ProcessDefinition("settlement_propaganda", "Propaganda",
            DefinitionRole.Process, EntityKind.Settlement,
            new AlwaysTrue(),
            Expr.Score(Expr.Lit(0.3f), 0.1f))
        { Cooldown = 15, EntityCooldown = 5, Mutations = {
            // dx = (self.ideology.x - target.ideology.x) * 0.01 / distance
            new CalculateMutation("dx", Expr.Div(
                Expr.Mul(Expr.Sub(Expr.SelfX("ideology"), Expr.TargetX("ideology")), Expr.Lit(0.01f)),
                Expr.Add(Expr.Distance(), Expr.Lit(1f)))), // +1 to avoid div by zero
            // dy = (self.ideology.y - target.ideology.y) * 0.01 / distance
            new CalculateMutation("dy", Expr.Div(
                Expr.Mul(Expr.Sub(Expr.SelfY("ideology"), Expr.TargetY("ideology")), Expr.Lit(0.01f)),
                Expr.Add(Expr.Distance(), Expr.Lit(1f)))),
            new ModifyTargetByVariableMutation("ideology", "dx", "x"),
            new ModifyTargetByVariableMutation("ideology", "dy", "y"),
        } };
        propaganda.Reads.Add(AttributeRef.Self("ideology", EntityKind.Settlement));
        propaganda.Writes.Add(AttributeRef.Self("ideology", EntityKind.Settlement));
        processes.Add(propaganda);

        // Settlement: attract population — steal from target, requires fame advantage
        // Stolen = base * (self.fame - target.fame) / distance. Min 10 to fire.
        var attract = new ProcessDefinition("settlement_attract", "Attract Population",
            DefinitionRole.Process, EntityKind.Settlement,
            new AndPrecondition([
                new SelfAttributeThreshold("fame", min: 1f, max: null),
                new TargetAttributeThreshold("population", min: 10f, max: null),
            ]),
            Expr.Score(Expr.Div(Expr.Self("fame"), Expr.Lit(100f)), 0.1f))
        { Cooldown = 20, EntityCooldown = 5, Mutations = {
            // stolen = clamp(fame_diff * ideology_sim / distance, 0, 5)
            new CalculateMutation("stolen", Expr.Clamp(
                Expr.Mul(
                    Expr.Sub(Expr.Self("fame"), Expr.Target("fame")),
                    Expr.Div(
                        Expr.Sub(Expr.Lit(1f), Expr.SemanticDistance("ideology")),
                        Expr.Add(Expr.Distance(), Expr.Lit(1f)))),
                Expr.Lit(0f), Expr.Lit(5f))),
            new CalculateMutation("neg_stolen", Expr.Mul(Expr.Var("stolen"), Expr.Lit(-1f))),
            new ModifySelfByVariableMutation("population", "stolen"),
            new ModifyTargetByVariableMutation("population", "neg_stolen"),
        } };
        attract.Reads.Add(AttributeRef.Self("fame", EntityKind.Settlement));
        attract.Reads.Add(AttributeRef.Self("population", EntityKind.Settlement));
        attract.Reads.Add(AttributeRef.Self("ideology", EntityKind.Settlement));
        attract.Writes.Add(AttributeRef.Self("population", EntityKind.Settlement));
        processes.Add(attract);

        // TODO: NPC, Nation, Artifact, Region processes removed — rebuilding from ground up

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
                    new() { Name = "population", Min = 0f, Max = 100000f, Default = 10f },
                    new() { Name = "sustenance", Min = 0f, Max = 100000f, Default = 0f },
                    new() { Name = "fame", Min = 0f, Max = 1000f, Default = 5f, DefaultVariance = 0.8f },
                    new() { Name = "ideology", Min = 0f, Max = 1f, DefaultX = 0.5f, DefaultY = 0.5f,
                        PlaneDefinition = new Engine.Attributes.SemanticPlaneDefinition(
                            "ideology", "Collectivist↔Individualist", "Traditionalist↔Progressive",
                            ["Trad-Collective", "Traditionalist", "Trad-Individual",
                             "Collective", "Centrist", "Individual",
                             "Prog-Collective", "Progressive", "Prog-Individual"]) },
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
                    new() { Name = "hunger", Min = 0f, Max = 100f, Default = 0f, DriftRate = 0.2f },
                    new() { Name = "ambition", Min = 0f, Max = 100f, Default = 50f },
                    new() { Name = "age", Min = 0f, Max = 1000f, Default = 20f, DriftRate = 1f },
                ],
                HealthAttribute = "hp",
            },
            [EntityKind.Artifact] = new()
            {
                Subtypes = new HashSet<string> { "generic_artifact", "weapon", "armor", "instrument", "relic", "scroll" },
                Attributes =
                [
                    new() { Name = "condition", Min = 0f, Max = 100f, Default = 100f, DriftRate = -0.01f },
                    new() { Name = "mystical_resonance", Min = 0f, Max = 100f, Default = 0f, DriftRate = 0.05f },
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
            [EntityKind.Settlement] = new() { HealthAttribute = "population", BaseRate = 0.01f, AgeCoefficient = 0.0002f },
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
