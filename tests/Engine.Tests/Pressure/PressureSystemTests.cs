namespace Engine.Tests.Pressure;

using Engine.Graph;
using Engine.Kinds;
using Engine.Pressure;

public class PressureSystemTests
{
    // --- Basic computation ---

    [Fact]
    public void Compute_ReturnsBaselineWithNoFactors()
    {
        var system = new PressureSystem();
        system.RegisterDefinition(new PressureDefinition("test", PressureScope.Regional, baseline: 0f));

        var scopeId = new EntityId(100);
        var contexts = CreateContexts(scopeId, []);

        system.Compute(contexts);

        Assert.Equal(0f, system.GetValue("test", scopeId));
    }

    [Fact]
    public void Compute_FactorPushesValue()
    {
        var system = new PressureSystem();
        var def = new PressureDefinition("test", PressureScope.Regional,
            baseline: 0f, homeostasisRate: 0f, smoothingFactor: 1f); // No smoothing/homeostasis for clarity
        def.Factors.Add(new FixedFactor("push", 50f));
        system.RegisterDefinition(def);

        var scopeId = new EntityId(100);
        var contexts = CreateContexts(scopeId, []);

        system.Compute(contexts);

        Assert.True(system.GetValue("test", scopeId) > 0f);
    }

    // --- Homeostasis ---

    [Fact]
    public void Homeostasis_DriftsTowardBaseline()
    {
        var system = new PressureSystem();
        var def = new PressureDefinition("test", PressureScope.Regional,
            baseline: 0f, homeostasisRate: 0.5f, smoothingFactor: 1f);
        def.Factors.Add(new FixedFactor("push", 50f));
        system.RegisterDefinition(def);

        var scopeId = new EntityId(100);
        var contexts = CreateContexts(scopeId, []);

        // First compute with a push of 50
        system.Compute(contexts);
        var afterPush = system.GetValue("test", scopeId);

        // Remove the factor and compute again - should drift toward baseline
        def.Factors.Clear();
        system.Compute(contexts);
        var afterDrift = system.GetValue("test", scopeId);

        Assert.True(Math.Abs(afterDrift) < Math.Abs(afterPush),
            $"Should drift toward 0. After push: {afterPush}, after drift: {afterDrift}");
    }

    // --- Smoothing ---

    [Fact]
    public void Smoothing_PreventsInstantSpikes()
    {
        var system = new PressureSystem();
        var def = new PressureDefinition("test", PressureScope.Regional,
            baseline: 0f, homeostasisRate: 0f, smoothingFactor: 0.1f); // Very smooth
        def.Factors.Add(new FixedFactor("spike", 100f));
        system.RegisterDefinition(def);

        var scopeId = new EntityId(100);
        var contexts = CreateContexts(scopeId, []);

        system.Compute(contexts);

        // With smoothing 0.1, should only reach ~10% of target in first tick
        Assert.True(system.GetValue("test", scopeId) < 50f,
            $"Should be smoothed, got {system.GetValue("test", scopeId)}");
    }

    // --- Clamping ---

    [Fact]
    public void Compute_ClampsToRange()
    {
        var system = new PressureSystem();
        var def = new PressureDefinition("test", PressureScope.Regional,
            baseline: 0f, homeostasisRate: 0f, smoothingFactor: 1f);
        def.Factors.Add(new FixedFactor("huge", 500f));
        system.RegisterDefinition(def);

        var scopeId = new EntityId(100);
        system.Compute(CreateContexts(scopeId, []));

        Assert.True(system.GetValue("test", scopeId) <= 100f);
    }

    [Fact]
    public void Compute_ClampsNegative()
    {
        var system = new PressureSystem();
        var def = new PressureDefinition("test", PressureScope.Regional,
            baseline: 0f, homeostasisRate: 0f, smoothingFactor: 1f);
        def.Factors.Add(new FixedFactor("negative", -500f));
        system.RegisterDefinition(def);

        var scopeId = new EntityId(100);
        system.Compute(CreateContexts(scopeId, []));

        Assert.True(system.GetValue("test", scopeId) >= -100f);
    }

    // --- Direct delta from cards ---

    [Fact]
    public void ApplyDelta_AffectsNextCompute()
    {
        var system = new PressureSystem();
        system.RegisterDefinition(new PressureDefinition("test", PressureScope.Regional,
            baseline: 0f, homeostasisRate: 0f, smoothingFactor: 1f));

        var scopeId = new EntityId(100);

        system.ApplyDelta("test", scopeId, 30f);
        system.Compute(CreateContexts(scopeId, []));

        Assert.True(system.GetValue("test", scopeId) > 0f);
    }

    [Fact]
    public void ApplyDelta_ClearedAfterCompute()
    {
        var system = new PressureSystem();
        system.RegisterDefinition(new PressureDefinition("test", PressureScope.Regional,
            baseline: 0f, homeostasisRate: 0.5f, smoothingFactor: 1f));

        var scopeId = new EntityId(100);

        system.ApplyDelta("test", scopeId, 50f);
        system.Compute(CreateContexts(scopeId, []));
        var afterDelta = system.GetValue("test", scopeId);

        // Second compute without delta - should drift back
        system.Compute(CreateContexts(scopeId, []));
        var afterSecond = system.GetValue("test", scopeId);

        Assert.True(Math.Abs(afterSecond) < Math.Abs(afterDelta));
    }

    // --- MVP Pressure Definitions ---

    [Fact]
    public void MilitaryTension_HigherWith2Nations()
    {
        var system = new PressureSystem();
        system.RegisterDefinition(TestDefaultPressures.MilitaryTension());

        var scopeId = new EntityId(100);

        // Context with 2 nations
        var graph = new WorldGraph();
        var n1 = graph.CreateEntity(TestKinds.Faction, "generic", "N1", "c", 0);
        var n2 = graph.CreateEntity(TestKinds.Faction, "generic", "N2", "c", 0);

        var context = new StubPressureContext(graph, [n1.Id, n2.Id]);
        system.Compute(new Dictionary<EntityId, IPressureContext> { [scopeId] = context });

        Assert.True(system.GetValue("military_tension", scopeId) > 0f);
    }

    [Fact]
    public void MilitaryTension_ZeroWithNoNations()
    {
        var system = new PressureSystem();
        system.RegisterDefinition(TestDefaultPressures.MilitaryTension());

        var scopeId = new EntityId(100);
        var graph = new WorldGraph();
        var context = new StubPressureContext(graph, []);

        system.Compute(new Dictionary<EntityId, IPressureContext> { [scopeId] = context });

        // With no nations, should remain near baseline (0)
        Assert.True(Math.Abs(system.GetValue("military_tension", scopeId)) < 1f);
    }

    [Fact]
    public void CulturalFriction_HigherWithMultipleCultures()
    {
        var system = new PressureSystem();
        system.RegisterDefinition(TestDefaultPressures.CulturalFriction());

        var graph = new WorldGraph();
        var e1 = graph.CreateEntity(TestKinds.Settlement, "village", "S1", "culture_a", 0);
        var e2 = graph.CreateEntity(TestKinds.Settlement, "village", "S2", "culture_b", 0);

        var scopeId = new EntityId(100);
        var context = new StubPressureContext(graph, [e1.Id, e2.Id]);
        system.Compute(new Dictionary<EntityId, IPressureContext> { [scopeId] = context });

        Assert.True(system.GetValue("cultural_friction", scopeId) > 0f);
    }

    [Fact]
    public void CulturalFriction_ZeroWithSingleCulture()
    {
        var system = new PressureSystem();
        system.RegisterDefinition(TestDefaultPressures.CulturalFriction());

        var graph = new WorldGraph();
        var e1 = graph.CreateEntity(TestKinds.Settlement, "village", "S1", "same_culture", 0);
        var e2 = graph.CreateEntity(TestKinds.Settlement, "village", "S2", "same_culture", 0);

        var scopeId = new EntityId(100);
        var context = new StubPressureContext(graph, [e1.Id, e2.Id]);
        system.Compute(new Dictionary<EntityId, IPressureContext> { [scopeId] = context });

        // Same culture everywhere -> friction = 0 (or near 0 after smoothing)
        Assert.True(system.GetValue("cultural_friction", scopeId) < 1f);
    }

    // --- Introspection ---

    [Fact]
    public void DefinitionNames_ListsAll()
    {
        var system = new PressureSystem();
        foreach (var def in TestDefaultPressures.All())
        {
            system.RegisterDefinition(def);
        }

        var names = system.DefinitionNames.ToHashSet();
        Assert.Contains("military_tension", names);
        Assert.Contains("economic_stress", names);
        Assert.Contains("cultural_friction", names);
        Assert.Contains("demographic_pressure", names);
    }

    // --- Helpers ---

    private sealed class FixedFactor(string name, float value) : IContributingFactor
    {
        public string Name => name;
        public float Evaluate(IPressureContext context) => value;
    }

    private static Dictionary<EntityId, IPressureContext> CreateContexts(
        EntityId scopeId, IReadOnlyCollection<EntityId> entities)
    {
        var graph = new WorldGraph();
        return new Dictionary<EntityId, IPressureContext>
        {
            [scopeId] = new StubPressureContext(graph, entities),
        };
    }

    private sealed class StubPressureContext(WorldGraph graph, IReadOnlyCollection<EntityId> entitiesInScope) : IPressureContext
    {
        public IReadOnlyCollection<EntityId> EntitiesInScope => entitiesInScope;

        public Entity? GetEntity(EntityId id) => graph.GetEntity(id);

        public int CountEntities(Func<Entity, bool> predicate)
        {
            var count = 0;
            foreach (var id in entitiesInScope)
            {
                var entity = graph.GetEntity(id);
                if (entity is not null && predicate(entity))
                {
                    count++;
                }
            }

            return count;
        }

        public int CountRelationships(Func<Relationship, bool> predicate)
        {
            var count = 0;
            foreach (var id in entitiesInScope)
            {
                foreach (var rel in graph.OutgoingRelationships(id))
                {
                    if (predicate(rel))
                    {
                        count++;
                    }
                }
            }

            return count;
        }

        public float GetAttribute(EntityId entityId, string attributeName) => 0f;
    }
}
