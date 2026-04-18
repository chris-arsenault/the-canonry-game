namespace Engine.Domain.TestDomain;

using Engine.Domain.Config;
using Engine.Processes;
using Engine.Graph;
using Engine.Kinds;
using Engine.Pressure;
using Engine.Resources;
using Engine.Wiring;
using Attr = Engine.Domain.TestDomain.TestDomainAttributeId;
using Culture = Engine.Domain.TestDomain.TestDomainCulture;
using Fact = Engine.Domain.TestDomain.TestDomainStructuralFact;
using Press = Engine.Domain.TestDomain.TestDomainPressure;
using Proc = Engine.Domain.TestDomain.TestDomainProcessId;
using Rel = Engine.Domain.TestDomain.TestDomainRelationship;
using Res = Engine.Domain.TestDomain.TestDomainResource;

/// <summary>
/// Test domain providing generic placeholder content for Phase 1 development.
/// All values that were previously hard-coded in Default*.cs files live here.
/// Swap this with a real domain (The Ice Remembers) in Phase 2.
/// </summary>
public static class TestDomainDefinition
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
            Semantics = TestDomainSemanticDefinition.Create(),
        };
    }

    private static string A(Attr attribute) => attribute.ToDomainName();
    private static string C(Culture culture) => culture.ToDomainName();
    private static string F(Fact fact) => fact.ToDomainName();
    private static string L(Rel relationship) => relationship.ToDomainName();
    private static string P(Press pressure) => pressure.ToDomainName();
    private static string ProcId(Proc processId) => processId.ToProcessId();
    private static string R(Res resource) => resource.ToDomainName();
    private static ResourceType T(Res resource) => resource.ToResourceType();

    private static List<ProcessDefinition> CreateProcesses()
    {
        var processes = new List<ProcessDefinition>();

        // Settlement: improve ambient local food potential. This is a support action
        // that makes direct growth stronger without introducing a stockpile layer.
        var irrigate = new ProcessDefinition(ProcId(Proc.SettlementIrrigate), "Irrigate",
            DefinitionRole.Process, EntityKind.Settlement,
            new AndPrecondition([
                new TargetIsSelf(),
                new LocationResourceThreshold(T(Res.Food), min: null, max: 8f, plannerRelevant: false),
            ]),
            Expr.Score(
                Expr.Div(
                    Expr.Max(Expr.Lit(0f), Expr.Sub(Expr.Lit(8f), Expr.SelfLocationResource(T(Res.Food)))),
                    Expr.Lit(8f))))
        {
            Cooldown = 2,
            EntityCooldown = 1,
            RewardExpression = Expr.Max(
                Expr.Lit(0f),
                Expr.Sub(Expr.Lit(8f), Expr.SelfLocationResource(T(Res.Food)))),
            Mutations =
            {
                new ModifyLocationResourceMutation(T(Res.Food), 2f, variance: 0.1f),
            },
        };
        irrigate.GoalEffects.Add(AttributeRef.Resource(R(Res.Food)));
        irrigate.Reads.Add(AttributeRef.Resource(R(Res.Food)));
        irrigate.Writes.Add(AttributeRef.Resource(R(Res.Food)));
        processes.Add(irrigate);

        // Settlement: direct growth from ambient conditions. Food remains a map-level
        // affordance, not a concrete settlement store. Low food makes growth weak,
        // so irrigation can become the planner-visible support step.
        var grow = new ProcessDefinition(ProcId(Proc.SettlementGrow), "Grow",
            DefinitionRole.Process, EntityKind.Settlement,
            new AndPrecondition([
                new TargetIsSelf(),
                new LocationResourceThreshold(T(Res.Population), min: 1f, max: null, plannerRelevant: false),
            ]),
            Expr.Score(
                Expr.Min(
                    Expr.Div(Expr.SelfLocationResource(T(Res.Food)), Expr.Lit(4f)),
                    Expr.Div(Expr.SelfLocationResource(T(Res.Population)), Expr.Lit(3f)))))
        { Cooldown = 5, EntityCooldown = 2,
          RewardExpression = Expr.Mul(
              Expr.Max(Expr.Lit(0f), Expr.Div(Expr.SelfLocationResource(T(Res.Food)), Expr.Lit(4f))),
              Expr.Max(Expr.Lit(0f), Expr.Div(Expr.SelfLocationResource(T(Res.Population)), Expr.Lit(2f)))),
          Mutations = {
            new CalculateMutation(
                "growth_amount",
                Expr.Clamp(
                    Expr.Min(
                        Expr.Div(Expr.SelfLocationResource(T(Res.Food)), Expr.Lit(4f)),
                        Expr.Div(Expr.SelfLocationResource(T(Res.Population)), Expr.Lit(2f))),
                    Expr.Lit(0.5f),
                    Expr.Lit(2f))),
            new ModifySelfByVariableMutation(A(Attr.Residents), "growth_amount", variance: 0.2f),
        } };
        grow.GoalEffects.Add(AttributeRef.Self(A(Attr.Residents), EntityKind.Settlement));
        grow.SupportRules.Add(new PlannerSupportRule(
            AttributeRef.Resource(R(Res.Food)),
            Expr.SelfLocationResource(T(Res.Food)),
            desiredMinimum: 4f));
        grow.Reads.Add(AttributeRef.Resource(R(Res.Food)));
        grow.Reads.Add(AttributeRef.Resource(R(Res.Population)));
        grow.Writes.Add(AttributeRef.Self(A(Attr.Residents), EntityKind.Settlement));
        processes.Add(grow);

        // NPC: eat at a named settlement. Simplified: satiety-only, settlement target picked
        // by proximity; prestige/civic influence on eating was removed with the attribute cleanup.
        var eat = new ProcessDefinition(ProcId(Proc.NpcEat), "Eat",
            DefinitionRole.Process, EntityKind.Npc,
            new AndPrecondition([
                new TargetIsNotSelf(),
                new SelfAttributeThreshold(A(Attr.Satiety), min: null, max: 95f, plannerRelevant: false),
                new RelatedEntityMatchesTarget(L(Rel.LocatedAt)),
                new LocationResourceThreshold(T(Res.Food), min: 2f, max: null, targetLocation: true, plannerRelevant: false),
            ]),
            Expr.Score(Expr.Div(Expr.Sub(Expr.Lit(100f), Expr.Self(A(Attr.Satiety))), Expr.Lit(20f))))
        {
            Cooldown = 3,
            EntityCooldown = 1,
            RewardExpression = Expr.Div(
                Expr.Mul(
                    Expr.TargetLocationResource(T(Res.Food)),
                    Expr.Target(A(Attr.Residents))),
                Expr.Add(Expr.Distance(), Expr.Lit(1f))),
            Mutations =
            {
                new ModifySelfAttributeMutation(A(Attr.Satiety), 12f, variance: 0.1f),
            },
        };
        eat.GoalEffects.Add(AttributeRef.Self(A(Attr.Satiety), EntityKind.Npc));
        eat.ProviderSelectors.Add(AttributeRef.Target(A(Attr.Residents), EntityKind.Settlement));
        eat.Reads.Add(AttributeRef.Self(A(Attr.Satiety), EntityKind.Npc));
        eat.Reads.Add(AttributeRef.Target(A(Attr.Residents), EntityKind.Settlement));
        eat.Reads.Add(AttributeRef.Resource(R(Res.Food)));
        eat.Writes.Add(AttributeRef.Self(A(Attr.Satiety), EntityKind.Npc));
        processes.Add(eat);

        // NPC: move toward the chosen settlement. This is intentionally the first cut
        // of a reusable movement enabler; the planner/presence abstraction will be
        // generalized further before we add many more target-specific movement chains.
        var moveToTarget = new ProcessDefinition(ProcId(Proc.NpcMoveToTarget), "Move To Target",
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
        moveToTarget.GoalEffects.Add(AttributeRef.TargetRelation(L(Rel.LocatedAt), EntityKind.Settlement));
        moveToTarget.Reads.Add(AttributeRef.Coordinates(EntityKind.Npc));
        moveToTarget.Writes.Add(AttributeRef.Coordinates(EntityKind.Npc));
        processes.Add(moveToTarget);

        // NPC: low-urgency ambient motion when nothing more pressing dominates.
        var wander = new ProcessDefinition(ProcId(Proc.NpcWander), "Wander",
            DefinitionRole.Process, EntityKind.Npc,
            new AlwaysTrue(),
            Expr.Score(Expr.Lit(0.2f)))
        {
            Cooldown = 0,
            EntityCooldown = 0,
            RewardExpression = Expr.Lit(1f),
            Mutations = { new WanderMutation() },
        };
        wander.GoalEffects.Add(AttributeRef.StructuralFact(F(Fact.Roam), EntityKind.Npc));
        wander.Reads.Add(AttributeRef.Self(A(Attr.Satiety), EntityKind.Npc));
        wander.Reads.Add(AttributeRef.Coordinates(EntityKind.Npc));
        wander.Writes.Add(AttributeRef.Coordinates(EntityKind.Npc));
        processes.Add(wander);

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
                    new() { Name = A(Attr.GeologicStability), Category = AttributeCategory.Health, Min = 0f, Max = 100f, Default = 80f },
                    new() { Name = A(Attr.RegionalSafety), Category = AttributeCategory.Capacity, Min = 0f, Max = 100f, Default = 50f },
                    new() { Name = A(Attr.Pristineness), Category = AttributeCategory.Need, Min = 0f, Max = 100f, Default = 70f },
                ],
                HealthAttribute = A(Attr.GeologicStability),
                Description = "A geographically connected area of the world. A region's semantic alignment is the spirit or animist ideals of the place itself — the land's own tendencies — distinct from the people, factions, or settlements that happen to occupy it.",
            },
            [EntityKind.Settlement] = new()
            {
                Subtypes = new HashSet<string> { "generic_settlement", "village", "town", "city", "fortress", "ruin" },
                Attributes =
                [
                    new() { Name = A(Attr.Residents), Category = AttributeCategory.Health, Min = 0f, Max = 100000f, Default = 10f, Baseline = 20f },
                    new() { Name = A(Attr.Dominion), Category = AttributeCategory.Projection, Min = 0f, Max = 100f, Default = 20f },
                    new() { Name = A(Attr.Sanitation), Category = AttributeCategory.Need, Min = 0f, Max = 100f, Default = 60f },
                ],
                HealthAttribute = A(Attr.Residents),
            },
            [EntityKind.Faction] = new()
            {
                Subtypes = new HashSet<string> { "generic_faction", "order", "cult", "company", "league" },
                Attributes =
                [
                    new() { Name = A(Attr.Cohesion), Category = AttributeCategory.Health, Min = 0f, Max = 100f, Default = 70f },
                    new() { Name = A(Attr.Influence), Category = AttributeCategory.Projection, Min = 0f, Max = 100f, Default = 30f },
                    new() { Name = A(Attr.Provisions), Category = AttributeCategory.Need, Min = 0f, Max = 100f, Default = 50f },
                ],
                HealthAttribute = A(Attr.Cohesion),
                StandingDrives =
                [
                    new StandingDriveConfig
                    {
                        DesiredEffect = AttributeRef.StructuralFact(F(Fact.TerritorialOrder), EntityKind.Faction),
                        BaseUrgency = 0.035f,
                    },
                ],
            },
            [EntityKind.Npc] = new()
            {
                Subtypes = new HashSet<string> { "generic_npc", "hero", "merchant", "scholar", "warrior" },
                Attributes =
                [
                    new() { Name = A(Attr.Vitality), Category = AttributeCategory.Health, Min = 0f, Max = 100f, Default = 100f },
                    new() { Name = A(Attr.Motivation), Category = AttributeCategory.Capacity, Min = 0f, Max = 100f, Default = 50f },
                    new()
                    {
                        Name = A(Attr.Satiety),
                        Category = AttributeCategory.Need,
                        Min = 0f,
                        Max = 100f,
                        Default = 100f,
                        DriftRate = -0.35f,
                        LocationResourceDriftResource = T(Res.Food),
                        LocationResourceDriftPerUnit = 0.04f,
                        LocationResourceDriftMinOffset = 0f,
                        LocationResourceDriftMaxOffset = 0.27f,
                        CriticalEffect = new CriticalEffectConfig
                        {
                            TargetAttribute = A(Attr.Vitality),
                            DeltaPerTick = -0.20f,
                            Description = "Critical satiety loss drains vitality",
                        },
                    },
                ],
                HealthAttribute = A(Attr.Vitality),
                StandingDrives =
                [
                    new StandingDriveConfig
                    {
                        DesiredEffect = AttributeRef.StructuralFact(F(Fact.Roam), EntityKind.Npc),
                        BaseUrgency = 0.03f,
                    },
                ],
            },
            [EntityKind.Artifact] = new()
            {
                Subtypes = new HashSet<string> { "generic_artifact", "weapon", "armor", "instrument", "relic", "scroll" },
                Attributes =
                [
                    new() { Name = A(Attr.Condition), Category = AttributeCategory.Health, Min = 0f, Max = 100f, Default = 100f, DriftRate = -0.01f },
                    new() { Name = A(Attr.Power), Category = AttributeCategory.Capacity, Min = 0f, Max = 100f, Default = 10f },
                    new() { Name = A(Attr.Aura), Category = AttributeCategory.Drive, Min = 0f, Max = 100f, Default = 90f, DriftRate = -0.05f },
                ],
                HealthAttribute = A(Attr.Condition),
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
            new() { Name = L(Rel.Borders), Polarity = RelationshipPolarity.Neutral, ValidSourceCategories = spatial, ValidTargetCategories = spatial, IsSymmetric = true },
            new() { Name = L(Rel.Contains), Polarity = RelationshipPolarity.Neutral, ValidSourceCategories = spatial, ValidTargetCategories = spatial },
            new() { Name = L(Rel.ControlledBy), Polarity = RelationshipPolarity.Neutral, ValidSourceCategories = spatial, ValidTargetCategories = agency, IsExclusive = true },
            new() { Name = L(Rel.LocatedAt), Polarity = RelationshipPolarity.Neutral, ValidSourceCategories = agency, ValidTargetCategories = spatial, IsExclusive = true },
            new() { Name = L(Rel.HomeBase), Polarity = RelationshipPolarity.Neutral, ValidSourceCategories = agency, ValidTargetCategories = spatial, IsExclusive = true },
            new() { Name = L(Rel.AlliedWith), Polarity = RelationshipPolarity.Positive, ValidSourceCategories = agency, ValidTargetCategories = agency, IsSymmetric = true, CullThreshold = 0.1f },
            new() { Name = L(Rel.RivalOf), Polarity = RelationshipPolarity.Negative, ValidSourceCategories = agency, ValidTargetCategories = agency, CullThreshold = 0.05f },
            new() { Name = L(Rel.LedBy), Polarity = RelationshipPolarity.Neutral, ValidSourceCategories = agency, ValidTargetCategories = agency, IsExclusive = true },
            new() { Name = L(Rel.MemberOf), Polarity = RelationshipPolarity.Neutral, ValidSourceCategories = agency, ValidTargetCategories = agency },
            new() { Name = L(Rel.LoyalTo), Polarity = RelationshipPolarity.Positive, ValidSourceCategories = agency, ValidTargetCategories = agency },
            new() { Name = L(Rel.Holds), Polarity = RelationshipPolarity.Neutral, ValidSourceCategories = agency, ValidTargetCategories = asset },
            new() { Name = L(Rel.CreatedBy), Polarity = RelationshipPolarity.Neutral, ValidSourceCategories = asset, ValidTargetCategories = agency },
            new() { Name = L(Rel.StoredAt), Polarity = RelationshipPolarity.Neutral, ValidSourceCategories = asset, ValidTargetCategories = spatial, IsExclusive = true },
            new() { Name = L(Rel.InvolvedIn), Polarity = RelationshipPolarity.Neutral, ValidSourceCategories = all, ValidTargetCategories = all },
        ];
    }

    private static List<PressureConfig> CreatePressures()
    {
        return
        [
            new() { Name = P(Press.MilitaryTension), Scope = PressureScope.Regional, Baseline = 0f, HomeostasisRate = 0.05f, SmoothingFactor = 0.2f },
            new() { Name = P(Press.EconomicStress), Scope = PressureScope.Regional, Baseline = 0f, HomeostasisRate = 0.08f, SmoothingFactor = 0.25f },
            new() { Name = P(Press.CulturalFriction), Scope = PressureScope.Regional, Baseline = 0f, HomeostasisRate = 0.06f, SmoothingFactor = 0.2f },
            new() { Name = P(Press.DemographicPressure), Scope = PressureScope.Regional, Baseline = 0f, HomeostasisRate = 0.1f, SmoothingFactor = 0.3f },
        ];
    }

    private static Dictionary<EntityKind, LifecycleConfig> CreateLifecycles()
    {
        return new Dictionary<EntityKind, LifecycleConfig>
        {
            [EntityKind.Settlement] = new() { HealthAttribute = A(Attr.Residents), BaseRate = 0.002f, AgeCoefficient = 0.00008f },
            [EntityKind.Faction] = new() { HealthAttribute = A(Attr.Cohesion), BaseRate = 0.01f, AgeCoefficient = 0.00005f },
            [EntityKind.Npc] = new() { HealthAttribute = A(Attr.Vitality), BaseRate = 0.02f, AgeCoefficient = 0.0005f },
            [EntityKind.Artifact] = new() { HealthAttribute = A(Attr.Condition), BaseRate = 0.01f, AgeCoefficient = 0.0001f },
            [EntityKind.Region] = new() { HealthAttribute = A(Attr.GeologicStability), BaseRate = 0.001f, AgeCoefficient = 0.00002f },
        };
    }

    private static List<ResourceConfig> CreateResources()
    {
        return
        [
            new() { Name = R(Res.Food), DisplayName = "Food" },
            new() { Name = R(Res.Wealth), DisplayName = "Wealth" },
            new() { Name = R(Res.Population), DisplayName = "Population" },
        ];
    }

    private static List<CultureConfig> CreateCultures()
    {
        return
        [
            new() { Id = C(Culture.CultureA), DisplayName = "Culture A" },
            new() { Id = C(Culture.CultureB), DisplayName = "Culture B" },
        ];
    }
}
