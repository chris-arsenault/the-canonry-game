namespace Engine.Tests.Behaviors;

using Engine.Attributes;
using Engine.Behaviors;
using Engine.Behaviors.Impulses;
using Engine.Graph;
using Engine.Kinds;
using Engine.Map;

public class ImpulseTests
{
    // --- Settlement Impulses ---

    [Fact]
    public void PopulationGrowth_IncreasesWithHighProsperity()
    {
        var (world, attrs) = CreateWorld();
        var settlement = world.Graph.CreateEntity(EntityKind.Settlement, "village", "Town", "c", 0, (0, 0));
        attrs.Initialize(settlement.Id, "population", 0, 10000, 500);
        attrs.Initialize(settlement.Id, "prosperity", 0, 100, 70);

        var impulse = new PopulationGrowthImpulse();
        var ws = CreateWorldState(world, attrs);
        impulse.Execute(settlement, ws);

        Assert.True(attrs.Get(settlement.Id, "population") > 500f);
    }

    [Fact]
    public void PopulationGrowth_DecreasesWithLowProsperity()
    {
        var (world, attrs) = CreateWorld();
        var settlement = world.Graph.CreateEntity(EntityKind.Settlement, "village", "Town", "c", 0, (0, 0));
        attrs.Initialize(settlement.Id, "population", 0, 10000, 500);
        attrs.Initialize(settlement.Id, "prosperity", 0, 100, 10);

        var impulse = new PopulationGrowthImpulse();
        impulse.Execute(settlement, CreateWorldState(world, attrs));

        Assert.True(attrs.Get(settlement.Id, "population") < 500f);
    }

    [Fact]
    public void Development_IncreasesWithProsperity()
    {
        var (world, attrs) = CreateWorld();
        var settlement = world.Graph.CreateEntity(EntityKind.Settlement, "village", "Town", "c", 0, (0, 0));
        attrs.Initialize(settlement.Id, "development", 0, 100, 20);
        attrs.Initialize(settlement.Id, "prosperity", 0, 100, 60);

        new DevelopmentImpulse().Execute(settlement, CreateWorldState(world, attrs));

        Assert.True(attrs.Get(settlement.Id, "development") > 20f);
    }

    // --- Nation Impulses ---

    [Fact]
    public void BorderTension_ScoresHigherWithRivals()
    {
        var (world, attrs) = CreateWorld();
        var nationA = world.Graph.CreateEntity(EntityKind.Nation, "generic", "A", "c", 0);
        var nationB = world.Graph.CreateEntity(EntityKind.Nation, "generic", "B", "c", 0);
        world.Graph.CreateRelationship("rival_of", RelationshipPolarity.Negative, nationA.Id, nationB.Id, 0.5f, 0);

        var impulse = new BorderTensionImpulse();
        var scoreWithRival = impulse.Score(nationA, CreateWorldState(world, attrs));

        // Remove rival, check score is lower
        var nationC = world.Graph.CreateEntity(EntityKind.Nation, "generic", "C", "c", 0);
        var scoreWithoutRival = impulse.Score(nationC, CreateWorldState(world, attrs));

        Assert.True(scoreWithRival > scoreWithoutRival);
    }

    [Fact]
    public void BorderTension_ReducesStability()
    {
        var (world, attrs) = CreateWorld();
        var nationA = world.Graph.CreateEntity(EntityKind.Nation, "generic", "A", "c", 0);
        var nationB = world.Graph.CreateEntity(EntityKind.Nation, "generic", "B", "c", 0);
        attrs.Initialize(nationA.Id, "stability", 0, 100, 50);
        world.Graph.CreateRelationship("rival_of", RelationshipPolarity.Negative, nationA.Id, nationB.Id, 0.5f, 0);

        new BorderTensionImpulse().Execute(nationA, CreateWorldState(world, attrs));

        Assert.True(attrs.Get(nationA.Id, "stability") < 50f);
    }

    [Fact]
    public void Consolidation_IncreasesStrainForLargeNations()
    {
        var (world, attrs) = CreateWorld();
        var nation = world.Graph.CreateEntity(EntityKind.Nation, "generic", "Big", "c", 0);
        attrs.Initialize(nation.Id, "cohesion", 0, 100, 80);

        // Give the nation 5 settlements (above threshold of 3)
        for (var i = 0; i < 5; i++)
        {
            var s = world.Graph.CreateEntity(EntityKind.Settlement, "village", $"S{i}", "c", 0);
            world.Graph.CreateRelationship("controlled_by", RelationshipPolarity.Neutral, s.Id, nation.Id, 1f, 0);
        }

        new ConsolidationImpulse().Execute(nation, CreateWorldState(world, attrs));

        Assert.True(attrs.Get(nation.Id, "cohesion") < 80f);
    }

    [Fact]
    public void Consolidation_NoStrainForSmallNations()
    {
        var (world, attrs) = CreateWorld();
        var nation = world.Graph.CreateEntity(EntityKind.Nation, "generic", "Small", "c", 0);
        attrs.Initialize(nation.Id, "cohesion", 0, 100, 80);

        // Only 2 settlements (below threshold)
        for (var i = 0; i < 2; i++)
        {
            var s = world.Graph.CreateEntity(EntityKind.Settlement, "village", $"S{i}", "c", 0);
            world.Graph.CreateRelationship("controlled_by", RelationshipPolarity.Neutral, s.Id, nation.Id, 1f, 0);
        }

        new ConsolidationImpulse().Execute(nation, CreateWorldState(world, attrs));

        Assert.Equal(80f, attrs.Get(nation.Id, "cohesion"));
    }

    // --- NPC Impulses ---

    [Fact]
    public void Hunger_IncreasesEachTick()
    {
        var (world, attrs) = CreateWorld();
        var npc = world.Graph.CreateEntity(EntityKind.Npc, "generic", "Hero", "c", 0, (3, -3));
        attrs.Initialize(npc.Id, "hunger", 0, 100, 10);

        new HungerImpulse().Execute(npc, CreateWorldState(world, attrs));

        Assert.True(attrs.Get(npc.Id, "hunger") > 10f);
    }

    [Fact]
    public void Hunger_ScoresHigherWhenHungry()
    {
        var (world, attrs) = CreateWorld();
        var hungryNpc = world.Graph.CreateEntity(EntityKind.Npc, "generic", "Hungry", "c", 0);
        attrs.Initialize(hungryNpc.Id, "hunger", 0, 100, 80);

        var fedNpc = world.Graph.CreateEntity(EntityKind.Npc, "generic", "Fed", "c", 0);
        attrs.Initialize(fedNpc.Id, "hunger", 0, 100, 5);

        var impulse = new HungerImpulse();
        var ws = CreateWorldState(world, attrs);

        Assert.True(impulse.Score(hungryNpc, ws) > impulse.Score(fedNpc, ws));
    }

    [Fact]
    public void Ambition_ScoresLowerWhenHungry()
    {
        var (world, attrs) = CreateWorld();
        var npc = world.Graph.CreateEntity(EntityKind.Npc, "generic", "Hero", "c", 0);
        attrs.Initialize(npc.Id, "ambition", 0, 100, 70);

        var impulse = new AmbitionImpulse();

        attrs.Initialize(npc.Id, "hunger", 0, 100, 5);
        var fedScore = impulse.Score(npc, CreateWorldState(world, attrs));

        attrs.Set(npc.Id, "hunger", 80);
        var hungryScore = impulse.Score(npc, CreateWorldState(world, attrs));

        Assert.True(fedScore > hungryScore);
    }

    // --- Artifact Impulses ---

    [Fact]
    public void Degradation_ReducesCondition()
    {
        var (world, attrs) = CreateWorld();
        var artifact = world.Graph.CreateEntity(EntityKind.Artifact, "weapon", "Blade", "c", 0);
        attrs.Initialize(artifact.Id, "condition", 0, 100, 90);

        new DegradationImpulse().Execute(artifact, CreateWorldState(world, attrs));

        Assert.True(attrs.Get(artifact.Id, "condition") < 90f);
    }

    [Fact]
    public void Resonance_AccumulatesFasterWhenHeld()
    {
        var (world, attrs) = CreateWorld();
        var artifact = world.Graph.CreateEntity(EntityKind.Artifact, "weapon", "Blade", "c", 0);
        var npc = world.Graph.CreateEntity(EntityKind.Npc, "generic", "Holder", "c", 0);
        attrs.Initialize(artifact.Id, "mystical_resonance", 0, 100, 10);

        // Without holder
        var impulse = new ResonanceAccumulationImpulse();
        impulse.Execute(artifact, CreateWorldState(world, attrs));
        var withoutHolder = attrs.Get(artifact.Id, "mystical_resonance");

        // Reset and add holder
        attrs.Set(artifact.Id, "mystical_resonance", 10f);
        world.Graph.CreateRelationship("holds", RelationshipPolarity.Neutral, npc.Id, artifact.Id, 1f, 0);
        impulse.Execute(artifact, CreateWorldState(world, attrs));
        var withHolder = attrs.Get(artifact.Id, "mystical_resonance");

        Assert.True(withHolder > withoutHolder);
    }

    // --- Default registration ---

    [Fact]
    public void DefaultImpulses_RegistersAllKinds()
    {
        var registry = new ImpulseRegistry();
        DefaultImpulses.RegisterAll(registry);

        Assert.True(registry.HasImpulses(EntityKind.Settlement));
        Assert.True(registry.HasImpulses(EntityKind.Nation));
        Assert.True(registry.HasImpulses(EntityKind.Npc));
        Assert.True(registry.HasImpulses(EntityKind.Artifact));
    }

    // --- Helpers ---

    private static (TestWorld World, AttributeStore Attrs) CreateWorld()
    {
        var world = TestMapBuilder.BuildDefault();
        return (world, new AttributeStore());
    }

    private static StubWorldState CreateWorldState(TestWorld world, AttributeStore attrs)
    {
        return new StubWorldState
        {
            GraphOverride = world.Graph,
            AttributesOverride = attrs,
            GridOverride = world.Grid,
            RegionMapOverride = world.RegionMap,
            SpatialIndexOverride = new SpatialIndex(world.Grid, world.RegionMap, world.Graph),
        };
    }

    private sealed class StubWorldState : IWorldState
    {
        public WorldGraph Graph => GraphOverride ?? new WorldGraph();
        public AttributeStore Attributes => AttributesOverride ?? new AttributeStore();
        public Engine.Attributes.EffectSystem Effects { get; } = new();
        public HexGrid Grid => GridOverride ?? new HexGrid(1);
        public RegionMap RegionMap => RegionMapOverride ?? new RegionMap();
        public SpatialIndex SpatialIndex => SpatialIndexOverride ?? new SpatialIndex(Grid, RegionMap, Graph);
        public int CurrentTick { get; set; }

        public WorldGraph? GraphOverride { get; set; }
        public AttributeStore? AttributesOverride { get; set; }
        public HexGrid? GridOverride { get; set; }
        public RegionMap? RegionMapOverride { get; set; }
        public SpatialIndex? SpatialIndexOverride { get; set; }
    }
}
