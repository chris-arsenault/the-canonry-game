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
        };
    }

    private static string A(Attr attribute) => attribute.ToDomainName();
    private static string C(Culture culture) => culture.ToDomainName();
    private static string F(Fact fact) => fact.ToDomainName();
    private static string L(Rel relationship) => relationship.ToDomainName();
    private static string P(Press pressure) => pressure.ToDomainName();
    private static string R(Res resource) => resource.ToDomainName();
    private static ResourceType T(Res resource) => resource.ToResourceType();

    private static List<ProcessDefinition> CreateProcesses()
    {
        var processes = new List<ProcessDefinition>();

        // Settlement: improve ambient local food potential. This is a support action
        // that makes direct growth stronger without introducing a stockpile layer.
        var irrigate = new ProcessDefinition("settlement_irrigate", "Irrigate",
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
        var grow = new ProcessDefinition("settlement_grow", "Grow",
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

        // Settlement: propaganda — shift target civic ideology toward self.
        var propaganda = new ProcessDefinition("settlement_propaganda", "Propaganda",
            DefinitionRole.Process, EntityKind.Settlement,
            new TargetIsNotSelf(),
            Expr.Score(Expr.Lit(0.3f), 0.1f))
        { Cooldown = 15, EntityCooldown = 5, RewardExpression = Expr.SemanticDistance(A(Attr.CivicIdeology)),
          Mutations = {
            // dx = (self.civic_ideology.x - target.civic_ideology.x) * 0.01 / distance
            new CalculateMutation("dx", Expr.Div(
                Expr.Mul(Expr.Sub(Expr.SelfX(A(Attr.CivicIdeology)), Expr.TargetX(A(Attr.CivicIdeology))), Expr.Lit(0.01f)),
                Expr.Add(Expr.Distance(), Expr.Lit(1f)))), // +1 to avoid div by zero
            // dy = (self.civic_ideology.y - target.civic_ideology.y) * 0.01 / distance
            new CalculateMutation("dy", Expr.Div(
                Expr.Mul(Expr.Sub(Expr.SelfY(A(Attr.CivicIdeology)), Expr.TargetY(A(Attr.CivicIdeology))), Expr.Lit(0.01f)),
                Expr.Add(Expr.Distance(), Expr.Lit(1f)))),
            new ModifyTargetByVariableMutation(A(Attr.CivicIdeology), "dx", "x"),
            new ModifyTargetByVariableMutation(A(Attr.CivicIdeology), "dy", "y"),
        } };
        propaganda.GoalEffects.Add(AttributeRef.Target(A(Attr.CivicIdeology), EntityKind.Settlement));
        propaganda.ProviderSelectors.Add(AttributeRef.Target(A(Attr.CivicIdeology), EntityKind.Settlement));
        propaganda.Reads.Add(AttributeRef.Self(A(Attr.CivicIdeology), EntityKind.Settlement));
        propaganda.Reads.Add(AttributeRef.Target(A(Attr.CivicIdeology), EntityKind.Settlement));
        propaganda.Writes.Add(AttributeRef.Target(A(Attr.CivicIdeology), EntityKind.Settlement));
        processes.Add(propaganda);

        // Settlement: attract residents from another settlement, requires prestige advantage.
        var attract = new ProcessDefinition("settlement_attract", "Attract Population",
            DefinitionRole.Process, EntityKind.Settlement,
            new AndPrecondition([
                new TargetIsNotSelf(),
                new SelfAttributeThreshold(A(Attr.Prestige), min: 1f, max: null, plannerRelevant: false),
                new TargetAttributeThreshold(A(Attr.Residents), min: 10f, max: null, plannerRelevant: false),
                new SemanticDistanceThreshold(A(Attr.CivicIdeology), 0.5f),
            ]),
            Expr.Score(Expr.Div(Expr.Self(A(Attr.Prestige)), Expr.Lit(100f)), 0.1f))
        { Cooldown = 20, EntityCooldown = 5,
          // Provider score: prestige advantage * ideology similarity
          RewardExpression = Expr.Mul(
              Expr.Max(Expr.Lit(0f), Expr.Sub(Expr.Self(A(Attr.Prestige)), Expr.Target(A(Attr.Prestige)))),
              Expr.Sub(Expr.Lit(1f), Expr.SemanticDistance(A(Attr.CivicIdeology)))),
          Mutations = {
            // stolen = clamp(prestige_diff * ideology_sim / distance, 0, 5)
            new CalculateMutation("stolen", Expr.Clamp(
                Expr.Mul(
                    Expr.Sub(Expr.Self(A(Attr.Prestige)), Expr.Target(A(Attr.Prestige))),
                    Expr.Div(
                        Expr.Sub(Expr.Lit(1f), Expr.SemanticDistance(A(Attr.CivicIdeology))),
                        Expr.Add(Expr.Distance(), Expr.Lit(1f)))),
                Expr.Lit(0f), Expr.Lit(5f))),
            new CalculateMutation("neg_stolen", Expr.Mul(Expr.Var("stolen"), Expr.Lit(-1f))),
            new ModifySelfByVariableMutation(A(Attr.Residents), "stolen"),
            new ModifyTargetByVariableMutation(A(Attr.Residents), "neg_stolen"),
        } };
        attract.GoalEffects.Add(AttributeRef.Self(A(Attr.Residents), EntityKind.Settlement));
        attract.ProviderSelectors.Add(AttributeRef.Target(A(Attr.Residents), EntityKind.Settlement));
        attract.Reads.Add(AttributeRef.Self(A(Attr.Prestige), EntityKind.Settlement));
        attract.Reads.Add(AttributeRef.Target(A(Attr.Prestige), EntityKind.Settlement));
        attract.Reads.Add(AttributeRef.Target(A(Attr.Residents), EntityKind.Settlement));
        attract.Reads.Add(AttributeRef.Target(A(Attr.CivicIdeology), EntityKind.Settlement));
        attract.Writes.Add(AttributeRef.Self(A(Attr.Residents), EntityKind.Settlement));
        attract.Writes.Add(AttributeRef.Target(A(Attr.Residents), EntityKind.Settlement));
        processes.Add(attract);

        // NPC: eat at a named settlement. This is the only strong positive satiety
        // action; ambient food merely slows satiety loss, it does not replace eating.
        var eat = new ProcessDefinition("npc_eat", "Eat",
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
                    Expr.Add(Expr.Target(A(Attr.Prestige)), Expr.Target(A(Attr.Residents)))),
                Expr.Add(Expr.Distance(), Expr.Lit(1f))),
            Mutations =
            {
                new ModifySelfAttributeMutation(A(Attr.Satiety), 12f, variance: 0.1f),
                new ModifyTargetAttributeMutation(A(Attr.Prestige), 0.1f, variance: 0.1f),
            },
        };
        eat.GoalEffects.Add(AttributeRef.Self(A(Attr.Satiety), EntityKind.Npc));
        eat.ProviderSelectors.Add(AttributeRef.Target(A(Attr.Residents), EntityKind.Settlement));
        eat.Reads.Add(AttributeRef.Self(A(Attr.Satiety), EntityKind.Npc));
        eat.Reads.Add(AttributeRef.Target(A(Attr.Residents), EntityKind.Settlement));
        eat.Reads.Add(AttributeRef.Target(A(Attr.Prestige), EntityKind.Settlement));
        eat.Reads.Add(AttributeRef.Resource(R(Res.Food)));
        eat.Writes.Add(AttributeRef.Self(A(Attr.Satiety), EntityKind.Npc));
        eat.Writes.Add(AttributeRef.Target(A(Attr.Prestige), EntityKind.Settlement));
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
        moveToTarget.GoalEffects.Add(AttributeRef.TargetRelation(L(Rel.LocatedAt), EntityKind.Settlement));
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
        wander.GoalEffects.Add(AttributeRef.StructuralFact(F(Fact.Roam), EntityKind.Npc));
        wander.Reads.Add(AttributeRef.Self(A(Attr.Satiety), EntityKind.Npc));
        wander.Reads.Add(AttributeRef.Coordinates(EntityKind.Npc));
        wander.Writes.Add(AttributeRef.Coordinates(EntityKind.Npc));
        processes.Add(wander);

        // NPC: create works at the settlement already being visited. This is a civic
        // contribution action, not a movement magnet; blocked remote candidates should
        // simply fall away instead of generating movement sub-goals.
        var createWorks = new ProcessDefinition("npc_create_works", "Create Works",
            DefinitionRole.Process, EntityKind.Npc,
            new AndPrecondition([
                new TargetIsNotSelf(),
                new RelatedEntityMatchesTarget(L(Rel.LocatedAt), plannerRelevant: false),
                new SelfAttributeThreshold(A(Attr.Satiety), min: 70f, max: null, plannerRelevant: false),
                new TargetAttributeThreshold(A(Attr.Security), min: 25f, max: null, plannerRelevant: false),
            ]),
            Expr.Score(
                Expr.Div(
                    Expr.Add(
                        Expr.Target(A(Attr.Prestige)),
                        Expr.Div(Expr.Target(A(Attr.Residents)), Expr.Lit(10f))),
                    Expr.Lit(20f))))
        {
            Cooldown = 4,
            EntityCooldown = 1,
            RewardExpression = Expr.Div(
                Expr.Add(
                    Expr.Target(A(Attr.Prestige)),
                    Expr.Add(
                        Expr.Target(A(Attr.Security)),
                        Expr.Div(Expr.Target(A(Attr.Residents)), Expr.Lit(5f)))),
                Expr.Lit(10f)),
            Mutations =
            {
                new ModifySelfAttributeMutation(A(Attr.Satiety), -2f, variance: 0.1f),
                new ModifyTargetAttributeMutation(A(Attr.Prestige), 0.6f, variance: 0.15f),
                new ModifyRelatedEntitiesAttributeMutation(
                    L(Rel.LoyalTo),
                    RelationshipTraversalDirection.Outgoing,
                    A(Attr.Support),
                    1.0f,
                    variance: 0.15f),
            },
        };
        createWorks.GoalEffects.Add(AttributeRef.StructuralFact(F(Fact.SettlementContribution), EntityKind.Npc));
        createWorks.ProviderSelectors.Add(AttributeRef.Target(A(Attr.Residents), EntityKind.Settlement));
        createWorks.Reads.Add(AttributeRef.Self(A(Attr.Satiety), EntityKind.Npc));
        createWorks.Reads.Add(AttributeRef.Target(A(Attr.Security), EntityKind.Settlement));
        createWorks.Reads.Add(AttributeRef.Target(A(Attr.Prestige), EntityKind.Settlement));
        createWorks.Reads.Add(AttributeRef.Target(A(Attr.Residents), EntityKind.Settlement));
        createWorks.Writes.Add(AttributeRef.Self(A(Attr.Satiety), EntityKind.Npc));
        createWorks.Writes.Add(AttributeRef.Target(A(Attr.Prestige), EntityKind.Settlement));
        processes.Add(createWorks);

        // Faction: patrol controlled settlements. Support is spent to raise both local
        // settlement security and broader regional safety.
        var patrol = new ProcessDefinition("faction_patrol", "Patrol",
            DefinitionRole.Process, EntityKind.Faction,
            new AndPrecondition([
                new TargetIsNotSelf(),
                new TargetHasRelationshipToSelf(L(Rel.ControlledBy)),
                new SelfAttributeThreshold(A(Attr.Support), min: 10f, max: null, plannerRelevant: false),
                new TargetAttributeThreshold(A(Attr.Security), min: null, max: 80f, plannerRelevant: false),
            ]),
            Expr.Score(
                Expr.Div(
                    Expr.Add(
                        Expr.Sub(Expr.Lit(100f), Expr.Target(A(Attr.Security))),
                        Expr.Div(Expr.Target(A(Attr.Prestige)), Expr.Lit(2f))),
                    Expr.Lit(50f))))
        {
            Cooldown = 5,
            EntityCooldown = 1,
            RewardExpression = Expr.Div(
                Expr.Add(
                    Expr.Sub(Expr.Lit(100f), Expr.Target(A(Attr.Security))),
                    Expr.Add(
                        Expr.Target(A(Attr.Prestige)),
                        Expr.Div(Expr.Target(A(Attr.Residents)), Expr.Lit(10f)))),
                Expr.Lit(20f)),
            Mutations =
            {
                new ModifySelfAttributeMutation(A(Attr.Support), -2.5f, variance: 0.1f),
                new ModifySelfAttributeMutation(A(Attr.Influence), 0.6f, variance: 0.1f),
                new ModifyTargetAttributeMutation(A(Attr.Security), 4f, variance: 0.1f),
                new ModifyRelatedEntitiesAttributeMutation(
                    L(Rel.Contains),
                    RelationshipTraversalDirection.Incoming,
                    A(Attr.RegionalSafety),
                    2f,
                    useTargetEntity: true,
                    variance: 0.1f),
            },
        };
        patrol.GoalEffects.Add(AttributeRef.StructuralFact(F(Fact.TerritorialOrder), EntityKind.Faction));
        patrol.ProviderSelectors.Add(AttributeRef.Target(A(Attr.Security), EntityKind.Settlement));
        patrol.Reads.Add(AttributeRef.Self(A(Attr.Support), EntityKind.Faction));
        patrol.Reads.Add(AttributeRef.Self(A(Attr.Influence), EntityKind.Faction));
        patrol.Reads.Add(AttributeRef.Target(A(Attr.Security), EntityKind.Settlement));
        patrol.Reads.Add(AttributeRef.Target(A(Attr.Prestige), EntityKind.Settlement));
        patrol.Reads.Add(AttributeRef.Target(A(Attr.Residents), EntityKind.Settlement));
        patrol.Writes.Add(AttributeRef.Self(A(Attr.Support), EntityKind.Faction));
        patrol.Writes.Add(AttributeRef.Self(A(Attr.Influence), EntityKind.Faction));
        patrol.Writes.Add(AttributeRef.Target(A(Attr.Security), EntityKind.Settlement));
        processes.Add(patrol);

        // Region: form militia from the surrounding populace to stabilize contained
        // settlements. This is a safety response, not a depletion/recovery economy loop.
        var formMilitia = new ProcessDefinition("region_form_militia", "Form Militia",
            DefinitionRole.Process, EntityKind.Region,
            new AndPrecondition([
                new TargetIsSelf(),
                new SelfAttributeThreshold(A(Attr.RegionalSafety), min: null, max: 85f, plannerRelevant: false),
                new SelfAttributeThreshold(A(Attr.GeologicStability), min: 25f, max: null, plannerRelevant: false),
            ]),
            Expr.Score(
                Expr.Div(
                    Expr.Sub(Expr.Lit(100f), Expr.Self(A(Attr.RegionalSafety))),
                    Expr.Lit(30f))))
        {
            Cooldown = 6,
            EntityCooldown = 2,
            RewardExpression = Expr.Div(
                Expr.Mul(
                    Expr.Sub(Expr.Lit(100f), Expr.Self(A(Attr.RegionalSafety))),
                    Expr.Max(Expr.Lit(0.1f), Expr.Div(Expr.Self(A(Attr.GeologicStability)), Expr.Lit(100f)))),
                Expr.Lit(10f)),
            Mutations =
            {
                new ModifySelfAttributeMutation(A(Attr.RegionalSafety), 3f, variance: 0.1f),
                new ModifyRelatedEntitiesAttributeMutation(
                    L(Rel.Contains),
                    RelationshipTraversalDirection.Outgoing,
                    A(Attr.Security),
                    2f,
                    variance: 0.1f),
            },
        };
        formMilitia.GoalEffects.Add(AttributeRef.Self(A(Attr.RegionalSafety), EntityKind.Region));
        formMilitia.Reads.Add(AttributeRef.Self(A(Attr.RegionalSafety), EntityKind.Region));
        formMilitia.Reads.Add(AttributeRef.Self(A(Attr.GeologicStability), EntityKind.Region));
        formMilitia.Writes.Add(AttributeRef.Self(A(Attr.RegionalSafety), EntityKind.Region));
        processes.Add(formMilitia);

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
                    new() { Name = A(Attr.RegionalSafety), Min = 0f, Max = 100f, Default = 50f },
                    new() { Name = A(Attr.GeologicStability), Min = 0f, Max = 100f, Default = 80f },
                    new() { Name = A(Attr.MysticResonance), Min = 0f, Max = 1f, DefaultX = 0.2f, DefaultY = 0.5f,
                        PlaneDefinition = CreateMysticPlaneDefinition() },
                ],
                HealthAttribute = null,
            },
            [EntityKind.Settlement] = new()
            {
                Subtypes = new HashSet<string> { "generic_settlement", "village", "town", "city", "fortress", "ruin" },
                Attributes =
                [
                    new() { Name = A(Attr.Residents), Min = 0f, Max = 100000f, Default = 10f, Baseline = 20f },
                    new() { Name = A(Attr.Security), Min = 0f, Max = 100f, Default = 20f },
                    new() { Name = A(Attr.Prestige), Min = 0f, Max = 1000f, Default = 5f, DefaultVariance = 0.8f },
                    new() { Name = A(Attr.CivicIdeology), Min = 0f, Max = 1f, DefaultX = 0.5f, DefaultY = 0.5f,
                        PlaneDefinition = CreateCivicPlaneDefinition() },
                ],
                HealthAttribute = A(Attr.Residents),
            },
            [EntityKind.Faction] = new()
            {
                Subtypes = new HashSet<string> { "generic_faction", "order", "cult", "company", "league" },
                Attributes =
                [
                    new() { Name = A(Attr.Cohesion), Min = 0f, Max = 100f, Default = 70f },
                    new() { Name = A(Attr.Support), Min = 0f, Max = 100f, Default = 40f },
                    new() { Name = A(Attr.Influence), Min = 0f, Max = 100f, Default = 30f },
                    new() { Name = A(Attr.CivicIdeology), Min = 0f, Max = 1f, DefaultX = 0.5f, DefaultY = 0.5f,
                        PlaneDefinition = CreateCivicPlaneDefinition() },
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
                    new() { Name = A(Attr.Vitality), Min = 0f, Max = 100f, Default = 100f },
                    new()
                    {
                        Name = A(Attr.Satiety),
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
                    new() { Name = A(Attr.Ambition), Min = 0f, Max = 100f, Default = 50f },
                    new() { Name = A(Attr.CivicIdeology), Min = 0f, Max = 1f, DefaultX = 0.5f, DefaultY = 0.5f,
                        PlaneDefinition = CreateCivicPlaneDefinition() },
                ],
                HealthAttribute = A(Attr.Vitality),
                StandingDrives =
                [
                    new StandingDriveConfig
                    {
                        DesiredEffect = AttributeRef.StructuralFact(F(Fact.Roam), EntityKind.Npc),
                        BaseUrgency = 0.03f,
                    },
                    new StandingDriveConfig
                    {
                        DesiredEffect = AttributeRef.StructuralFact(F(Fact.SettlementContribution), EntityKind.Npc),
                        BaseUrgency = 0.04f,
                    },
                ],
            },
            [EntityKind.Artifact] = new()
            {
                Subtypes = new HashSet<string> { "generic_artifact", "weapon", "armor", "instrument", "relic", "scroll" },
                Attributes =
                [
                    new() { Name = A(Attr.Condition), Min = 0f, Max = 100f, Default = 100f, DriftRate = -0.01f },
                    new() { Name = A(Attr.Power), Min = 0f, Max = 100f, Default = 10f },
                    new() { Name = A(Attr.Containment), Min = 0f, Max = 100f, Default = 90f, DriftRate = -0.05f },
                    new() { Name = A(Attr.MysticResonance), Min = 0f, Max = 1f, DefaultX = 0.4f, DefaultY = 0.5f,
                        PlaneDefinition = CreateMysticPlaneDefinition() },
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

    private static Engine.Attributes.SemanticPlaneDefinition CreateCivicPlaneDefinition() => new(
        A(Attr.CivicIdeology),
        "Collectivist↔Individualist",
        "Traditionalist↔Progressive",
        [
            "Trad-Collective", "Traditionalist", "Trad-Individual",
            "Collective", "Centrist", "Individual",
            "Prog-Collective", "Progressive", "Prog-Individual",
        ]);

    private static Engine.Attributes.SemanticPlaneDefinition CreateMysticPlaneDefinition() => new(
        A(Attr.MysticResonance),
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
            new() { Id = C(Culture.CultureA), DisplayName = "Culture A" },
            new() { Id = C(Culture.CultureB), DisplayName = "Culture B" },
        ];
    }
}
