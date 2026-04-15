namespace Engine.Domain;

using Engine.Processes;
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

        // Settlement: improve ambient local food potential. This is a support action
        // that makes direct growth stronger without introducing a stockpile layer.
        var irrigate = new ProcessDefinition("settlement_irrigate", "Irrigate",
            DefinitionRole.Process, EntityKind.Settlement,
            new AndPrecondition([
                new TargetIsSelf(),
                new LocationResourceThreshold(ResourceType.Food, min: null, max: 8f, plannerRelevant: false),
            ]),
            Expr.Score(
                Expr.Div(
                    Expr.Max(Expr.Lit(0f), Expr.Sub(Expr.Lit(8f), Expr.SelfLocationResource(ResourceType.Food))),
                    Expr.Lit(8f))))
        {
            Cooldown = 2,
            EntityCooldown = 1,
            RewardExpression = Expr.Max(
                Expr.Lit(0f),
                Expr.Sub(Expr.Lit(8f), Expr.SelfLocationResource(ResourceType.Food))),
            Mutations =
            {
                new ModifyLocationResourceMutation(ResourceType.Food, 2f, variance: 0.1f),
            },
        };
        irrigate.GoalEffects.Add(AttributeRef.Resource("food"));
        irrigate.Reads.Add(AttributeRef.Resource("food"));
        irrigate.Writes.Add(AttributeRef.Resource("food"));
        processes.Add(irrigate);

        // Settlement: direct growth from ambient conditions. Food remains a map-level
        // affordance, not a concrete settlement store. Low food makes growth weak,
        // so irrigation can become the planner-visible support step.
        var grow = new ProcessDefinition("settlement_grow", "Grow",
            DefinitionRole.Process, EntityKind.Settlement,
            new AndPrecondition([
                new TargetIsSelf(),
                new LocationResourceThreshold(ResourceType.Population, min: 1f, max: null, plannerRelevant: false),
            ]),
            Expr.Score(
                Expr.Min(
                    Expr.Div(Expr.SelfLocationResource(ResourceType.Food), Expr.Lit(4f)),
                    Expr.Div(Expr.SelfLocationResource(ResourceType.Population), Expr.Lit(3f)))))
        { Cooldown = 5, EntityCooldown = 2,
          RewardExpression = Expr.Mul(
              Expr.Max(Expr.Lit(0f), Expr.Div(Expr.SelfLocationResource(ResourceType.Food), Expr.Lit(4f))),
              Expr.Max(Expr.Lit(0f), Expr.Div(Expr.SelfLocationResource(ResourceType.Population), Expr.Lit(2f)))),
          Mutations = {
            new CalculateMutation(
                "growth_amount",
                Expr.Clamp(
                    Expr.Min(
                        Expr.Div(Expr.SelfLocationResource(ResourceType.Food), Expr.Lit(4f)),
                        Expr.Div(Expr.SelfLocationResource(ResourceType.Population), Expr.Lit(2f))),
                    Expr.Lit(0.5f),
                    Expr.Lit(2f))),
            new ModifySelfByVariableMutation("residents", "growth_amount", variance: 0.2f),
            new ModifyLocationResourceMutation(ResourceType.Population, -1f),
        } };
        grow.GoalEffects.Add(AttributeRef.Self("residents", EntityKind.Settlement));
        grow.SupportRules.Add(new PlannerSupportRule(
            AttributeRef.Resource("food"),
            Expr.SelfLocationResource(ResourceType.Food),
            desiredMinimum: 4f));
        grow.Reads.Add(AttributeRef.Resource("food"));
        grow.Reads.Add(AttributeRef.Resource("population"));
        grow.Writes.Add(AttributeRef.Self("residents", EntityKind.Settlement));
        grow.Writes.Add(AttributeRef.Resource("population"));
        processes.Add(grow);

        // Settlement: propaganda — shift target civic ideology toward self.
        var propaganda = new ProcessDefinition("settlement_propaganda", "Propaganda",
            DefinitionRole.Process, EntityKind.Settlement,
            new TargetIsNotSelf(),
            Expr.Score(Expr.Lit(0.3f), 0.1f))
        { Cooldown = 15, EntityCooldown = 5, RewardExpression = Expr.SemanticDistance("civic_ideology"),
          Mutations = {
            // dx = (self.civic_ideology.x - target.civic_ideology.x) * 0.01 / distance
            new CalculateMutation("dx", Expr.Div(
                Expr.Mul(Expr.Sub(Expr.SelfX("civic_ideology"), Expr.TargetX("civic_ideology")), Expr.Lit(0.01f)),
                Expr.Add(Expr.Distance(), Expr.Lit(1f)))), // +1 to avoid div by zero
            // dy = (self.civic_ideology.y - target.civic_ideology.y) * 0.01 / distance
            new CalculateMutation("dy", Expr.Div(
                Expr.Mul(Expr.Sub(Expr.SelfY("civic_ideology"), Expr.TargetY("civic_ideology")), Expr.Lit(0.01f)),
                Expr.Add(Expr.Distance(), Expr.Lit(1f)))),
            new ModifyTargetByVariableMutation("civic_ideology", "dx", "x"),
            new ModifyTargetByVariableMutation("civic_ideology", "dy", "y"),
        } };
        propaganda.GoalEffects.Add(AttributeRef.Target("civic_ideology", EntityKind.Settlement));
        propaganda.ProviderSelectors.Add(AttributeRef.Target("civic_ideology", EntityKind.Settlement));
        propaganda.Reads.Add(AttributeRef.Self("civic_ideology", EntityKind.Settlement));
        propaganda.Reads.Add(AttributeRef.Target("civic_ideology", EntityKind.Settlement));
        propaganda.Writes.Add(AttributeRef.Target("civic_ideology", EntityKind.Settlement));
        processes.Add(propaganda);

        // Settlement: attract residents from another settlement, requires prestige advantage.
        var attract = new ProcessDefinition("settlement_attract", "Attract Population",
            DefinitionRole.Process, EntityKind.Settlement,
            new AndPrecondition([
                new TargetIsNotSelf(),
                new SelfAttributeThreshold("prestige", min: 1f, max: null, plannerRelevant: false),
                new TargetAttributeThreshold("residents", min: 10f, max: null, plannerRelevant: false),
                new SemanticDistanceThreshold("civic_ideology", 0.5f),
            ]),
            Expr.Score(Expr.Div(Expr.Self("prestige"), Expr.Lit(100f)), 0.1f))
        { Cooldown = 20, EntityCooldown = 5,
          // Provider score: prestige advantage * ideology similarity
          RewardExpression = Expr.Mul(
              Expr.Max(Expr.Lit(0f), Expr.Sub(Expr.Self("prestige"), Expr.Target("prestige"))),
              Expr.Sub(Expr.Lit(1f), Expr.SemanticDistance("civic_ideology"))),
          Mutations = {
            // stolen = clamp(prestige_diff * ideology_sim / distance, 0, 5)
            new CalculateMutation("stolen", Expr.Clamp(
                Expr.Mul(
                    Expr.Sub(Expr.Self("prestige"), Expr.Target("prestige")),
                    Expr.Div(
                        Expr.Sub(Expr.Lit(1f), Expr.SemanticDistance("civic_ideology")),
                        Expr.Add(Expr.Distance(), Expr.Lit(1f)))),
                Expr.Lit(0f), Expr.Lit(5f))),
            new CalculateMutation("neg_stolen", Expr.Mul(Expr.Var("stolen"), Expr.Lit(-1f))),
            new ModifySelfByVariableMutation("residents", "stolen"),
            new ModifyTargetByVariableMutation("residents", "neg_stolen"),
        } };
        attract.GoalEffects.Add(AttributeRef.Self("residents", EntityKind.Settlement));
        attract.ProviderSelectors.Add(AttributeRef.Target("residents", EntityKind.Settlement));
        attract.Reads.Add(AttributeRef.Self("prestige", EntityKind.Settlement));
        attract.Reads.Add(AttributeRef.Target("prestige", EntityKind.Settlement));
        attract.Reads.Add(AttributeRef.Target("residents", EntityKind.Settlement));
        attract.Reads.Add(AttributeRef.Target("civic_ideology", EntityKind.Settlement));
        attract.Writes.Add(AttributeRef.Self("residents", EntityKind.Settlement));
        attract.Writes.Add(AttributeRef.Target("residents", EntityKind.Settlement));
        processes.Add(attract);

        // NPC: eat at a named settlement. This is the only strong positive satiety
        // action; ambient food merely slows satiety loss, it does not replace eating.
        var eat = new ProcessDefinition("npc_eat", "Eat",
            DefinitionRole.Process, EntityKind.Npc,
            new AndPrecondition([
                new TargetIsNotSelf(),
                new SelfAttributeThreshold("satiety", min: null, max: 95f, plannerRelevant: false),
                new RelatedEntityMatchesTarget("located_at"),
                new LocationResourceThreshold(ResourceType.Food, min: 2f, max: null, targetLocation: true, plannerRelevant: false),
            ]),
            Expr.Score(Expr.Div(Expr.Sub(Expr.Lit(100f), Expr.Self("satiety")), Expr.Lit(20f))))
        {
            Cooldown = 3,
            EntityCooldown = 1,
            RewardExpression = Expr.Div(
                Expr.Mul(
                    Expr.TargetLocationResource(ResourceType.Food),
                    Expr.Add(Expr.Target("prestige"), Expr.Target("residents"))),
                Expr.Add(Expr.Distance(), Expr.Lit(1f))),
            Mutations =
            {
                new ModifySelfAttributeMutation("satiety", 12f, variance: 0.1f),
                new ModifyTargetAttributeMutation("prestige", 0.1f, variance: 0.1f),
            },
        };
        eat.GoalEffects.Add(AttributeRef.Self("satiety", EntityKind.Npc));
        eat.ProviderSelectors.Add(AttributeRef.Target("residents", EntityKind.Settlement));
        eat.Reads.Add(AttributeRef.Self("satiety", EntityKind.Npc));
        eat.Reads.Add(AttributeRef.Target("residents", EntityKind.Settlement));
        eat.Reads.Add(AttributeRef.Target("prestige", EntityKind.Settlement));
        eat.Reads.Add(AttributeRef.Resource("food"));
        eat.Writes.Add(AttributeRef.Self("satiety", EntityKind.Npc));
        eat.Writes.Add(AttributeRef.Target("prestige", EntityKind.Settlement));
        processes.Add(eat);

        // NPC: move toward the chosen settlement. This is intentionally the first cut
        // of a reusable movement enabler; the planner/presence abstraction will be
        // generalized further before we add many more target-specific movement chains.
        var moveToTarget = new ProcessDefinition("npc_move_to_target", "Move To Target",
            DefinitionRole.Process, EntityKind.Npc,
            new TargetIsNotSelf(),
            Expr.Score(
                Expr.Div(Expr.Lit(1f), Expr.Add(Expr.Distance(), Expr.Lit(1f))),
                0.05f))
        {
            Cooldown = 0,
            EntityCooldown = 0,
            RewardExpression = Expr.Div(Expr.Lit(1f), Expr.Add(Expr.Distance(), Expr.Lit(1f))),
            Mutations = { new MoveTowardTargetMutation() },
        };
        moveToTarget.GoalEffects.Add(AttributeRef.TargetRelation("located_at", EntityKind.Settlement));
        moveToTarget.Reads.Add(AttributeRef.Coordinates(EntityKind.Npc));
        moveToTarget.Writes.Add(AttributeRef.Coordinates(EntityKind.Npc));
        processes.Add(moveToTarget);

        // NPC: low-urgency ambient motion when nothing more pressing dominates.
        var wander = new ProcessDefinition("npc_wander", "Wander",
            DefinitionRole.Process, EntityKind.Npc,
            new AlwaysTrue(),
            Expr.Score(Expr.Lit(0.2f)))
        {
            Cooldown = 0,
            EntityCooldown = 0,
            RewardExpression = Expr.Lit(1f),
            Mutations = { new WanderMutation() },
        };
        wander.GoalEffects.Add(AttributeRef.StructuralFact("roam", EntityKind.Npc));
        wander.Reads.Add(AttributeRef.Self("satiety", EntityKind.Npc));
        wander.Reads.Add(AttributeRef.Coordinates(EntityKind.Npc));
        wander.Writes.Add(AttributeRef.Coordinates(EntityKind.Npc));
        processes.Add(wander);

        // TODO: Faction, Artifact, Region processes removed — rebuilding from ground up

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
                    new() { Name = "water_balance", Min = 0f, Max = 100f, Default = 50f },
                    new() { Name = "geologic_stability", Min = 0f, Max = 100f, Default = 80f },
                    new() { Name = "mystic_resonance", Min = 0f, Max = 1f, DefaultX = 0.2f, DefaultY = 0.5f,
                        PlaneDefinition = CreateMysticPlaneDefinition() },
                ],
                HealthAttribute = null,
            },
            [EntityKind.Settlement] = new()
            {
                Subtypes = new HashSet<string> { "generic_settlement", "village", "town", "city", "fortress", "ruin" },
                Attributes =
                [
                    new() { Name = "residents", Min = 0f, Max = 100000f, Default = 10f, Baseline = 20f },
                    new() { Name = "security", Min = 0f, Max = 100f, Default = 20f },
                    new() { Name = "prestige", Min = 0f, Max = 1000f, Default = 5f, DefaultVariance = 0.8f },
                    new() { Name = "civic_ideology", Min = 0f, Max = 1f, DefaultX = 0.5f, DefaultY = 0.5f,
                        PlaneDefinition = CreateCivicPlaneDefinition() },
                ],
                HealthAttribute = "residents",
            },
            [EntityKind.Faction] = new()
            {
                Subtypes = new HashSet<string> { "generic_faction", "order", "cult", "company", "league" },
                Attributes =
                [
                    new() { Name = "cohesion", Min = 0f, Max = 100f, Default = 70f },
                    new() { Name = "support", Min = 0f, Max = 100f, Default = 40f },
                    new() { Name = "influence", Min = 0f, Max = 100f, Default = 30f },
                    new() { Name = "civic_ideology", Min = 0f, Max = 1f, DefaultX = 0.5f, DefaultY = 0.5f,
                        PlaneDefinition = CreateCivicPlaneDefinition() },
                ],
                HealthAttribute = "cohesion",
            },
            [EntityKind.Npc] = new()
            {
                Subtypes = new HashSet<string> { "generic_npc", "hero", "merchant", "scholar", "warrior" },
                Attributes =
                [
                    new() { Name = "vitality", Min = 0f, Max = 100f, Default = 100f },
                    new()
                    {
                        Name = "satiety",
                        Min = 0f,
                        Max = 100f,
                        Default = 100f,
                        DriftRate = -0.35f,
                        LocationResourceDriftResource = ResourceType.Food,
                        LocationResourceDriftPerUnit = 0.04f,
                        LocationResourceDriftMinOffset = 0f,
                        LocationResourceDriftMaxOffset = 0.27f,
                        CriticalEffect = new CriticalEffectConfig
                        {
                            TargetAttribute = "vitality",
                            DeltaPerTick = -0.20f,
                            Description = "Critical satiety loss drains vitality",
                        },
                    },
                    new() { Name = "ambition", Min = 0f, Max = 100f, Default = 50f },
                    new() { Name = "civic_ideology", Min = 0f, Max = 1f, DefaultX = 0.5f, DefaultY = 0.5f,
                        PlaneDefinition = CreateCivicPlaneDefinition() },
                ],
                HealthAttribute = "vitality",
                StandingDrives =
                [
                    new StandingDriveConfig
                    {
                        DesiredEffect = AttributeRef.StructuralFact("roam", EntityKind.Npc),
                        BaseUrgency = 0.03f,
                    },
                ],
            },
            [EntityKind.Artifact] = new()
            {
                Subtypes = new HashSet<string> { "generic_artifact", "weapon", "armor", "instrument", "relic", "scroll" },
                Attributes =
                [
                    new() { Name = "condition", Min = 0f, Max = 100f, Default = 100f, DriftRate = -0.01f },
                    new() { Name = "power", Min = 0f, Max = 100f, Default = 10f },
                    new() { Name = "containment", Min = 0f, Max = 100f, Default = 90f, DriftRate = -0.05f },
                    new() { Name = "mystic_resonance", Min = 0f, Max = 1f, DefaultX = 0.4f, DefaultY = 0.5f,
                        PlaneDefinition = CreateMysticPlaneDefinition() },
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
            [EntityKind.Settlement] = new() { HealthAttribute = "residents", BaseRate = 0.002f, AgeCoefficient = 0.00008f },
            [EntityKind.Faction] = new() { HealthAttribute = "cohesion", BaseRate = 0.01f, AgeCoefficient = 0.00005f },
            [EntityKind.Npc] = new() { HealthAttribute = "vitality", BaseRate = 0.02f, AgeCoefficient = 0.0005f },
            [EntityKind.Artifact] = new() { HealthAttribute = "condition", BaseRate = 0.01f, AgeCoefficient = 0.0001f },
        };
    }

    private static List<ResourceConfig> CreateResources()
    {
        return
        [
            new() { Name = "food", DisplayName = "Food" },
            new() { Name = "wealth", DisplayName = "Wealth" },
            new() { Name = "population", DisplayName = "Population" },
        ];
    }

    private static Engine.Attributes.SemanticPlaneDefinition CreateCivicPlaneDefinition() => new(
        "civic_ideology",
        "Collectivist↔Individualist",
        "Traditionalist↔Progressive",
        [
            "Trad-Collective", "Traditionalist", "Trad-Individual",
            "Collective", "Centrist", "Individual",
            "Prog-Collective", "Progressive", "Prog-Individual",
        ]);

    private static Engine.Attributes.SemanticPlaneDefinition CreateMysticPlaneDefinition() => new(
        "mystic_resonance",
        "Quiescent↔Resonant",
        "Corrupting↔Purifying",
        [
            "Quiescent-Corrupting", "Corrupting", "Resonant-Corrupting",
            "Quiescent", "Balanced", "Resonant",
            "Quiescent-Purifying", "Purifying", "Resonant-Purifying",
        ]);

    private static List<CultureConfig> CreateCultures()
    {
        return
        [
            new() { Id = "culture_a", DisplayName = "Culture A" },
            new() { Id = "culture_b", DisplayName = "Culture B" },
        ];
    }
}
