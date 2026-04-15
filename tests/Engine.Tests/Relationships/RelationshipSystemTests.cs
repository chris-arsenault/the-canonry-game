namespace Engine.Tests.Relationships;

using Engine.Attributes;
using Engine.Graph;
using Engine.Kinds;
using Engine.Map;
using Engine.Relationships;

public class RelationshipSystemTests
{
    private readonly WorldGraph _graph = new();
    private readonly KindRegistry _registry = DefaultRegistry.Create();
    private readonly SemanticPlaneStore _planes = new();

    private RelationshipSystem CreateSystem()
    {
        return new RelationshipSystem(_graph, _registry);
    }

    private StubContext CreateContext()
    {
        return new StubContext(_graph, _planes);
    }

    // --- Strength Modifiers ---

    [Fact]
    public void ProximityModifier_IncreasesStrengthForNearbyEntities()
    {
        var a = _graph.CreateEntity(EntityKind.Faction, "generic_faction", "A", "c", 0, (0, 0));
        var b = _graph.CreateEntity(EntityKind.Faction, "generic_faction", "B", "c", 0, (1, 0));
        var rel = _graph.CreateRelationship("allied_with", RelationshipPolarity.Positive, a.Id, b.Id, 0.5f, 0);

        var modifier = new ProximityModifier { MaxDistance = 10, MaxDelta = 0.01f };
        var delta = modifier.ComputeDelta(rel, CreateContext());

        Assert.True(delta > 0f);
    }

    [Fact]
    public void ProximityModifier_ZeroDeltaForDistantEntities()
    {
        var a = _graph.CreateEntity(EntityKind.Faction, "generic_faction", "A", "c", 0, (0, 0));
        var b = _graph.CreateEntity(EntityKind.Faction, "generic_faction", "B", "c", 0, (5, 5));
        var rel = _graph.CreateRelationship("allied_with", RelationshipPolarity.Positive, a.Id, b.Id, 0.5f, 0);

        var modifier = new ProximityModifier { MaxDistance = 3, MaxDelta = 0.01f };
        var delta = modifier.ComputeDelta(rel, CreateContext());

        Assert.Equal(0f, delta);
    }

    [Fact]
    public void SharedCultureModifier_PositiveForSameCulture()
    {
        var a = _graph.CreateEntity(EntityKind.Faction, "generic_faction", "A", "culture_x", 0);
        var b = _graph.CreateEntity(EntityKind.Faction, "generic_faction", "B", "culture_x", 0);
        var rel = _graph.CreateRelationship("allied_with", RelationshipPolarity.Positive, a.Id, b.Id, 0.5f, 0);

        var modifier = new SharedCultureModifier();
        var delta = modifier.ComputeDelta(rel, CreateContext());

        Assert.True(delta > 0f);
    }

    [Fact]
    public void SharedCultureModifier_NegativeForDifferentCulture()
    {
        var a = _graph.CreateEntity(EntityKind.Faction, "generic_faction", "A", "culture_x", 0);
        var b = _graph.CreateEntity(EntityKind.Faction, "generic_faction", "B", "culture_y", 0);
        var rel = _graph.CreateRelationship("allied_with", RelationshipPolarity.Positive, a.Id, b.Id, 0.5f, 0);

        var modifier = new SharedCultureModifier();
        var delta = modifier.ComputeDelta(rel, CreateContext());

        Assert.True(delta < 0f);
    }

    [Fact]
    public void NeglectModifier_AlwaysNegative()
    {
        var a = _graph.CreateEntity(EntityKind.Faction, "generic_faction", "A", "c", 0);
        var b = _graph.CreateEntity(EntityKind.Faction, "generic_faction", "B", "c", 0);
        var rel = _graph.CreateRelationship("allied_with", RelationshipPolarity.Positive, a.Id, b.Id, 0.5f, 0);

        var modifier = new NeglectModifier();
        var delta = modifier.ComputeDelta(rel, CreateContext());

        Assert.True(delta < 0f);
    }

    // --- RelationshipSystem: strength modification ---

    [Fact]
    public void System_ModifiesStrengthOverMultipleTicks()
    {
        var a = _graph.CreateEntity(EntityKind.Faction, "generic_faction", "A", "culture_x", 0, (0, 0));
        var b = _graph.CreateEntity(EntityKind.Faction, "generic_faction", "B", "culture_x", 0, (1, 0));
        var rel = _graph.CreateRelationship("allied_with", RelationshipPolarity.Positive, a.Id, b.Id, 0.5f, 0);

        var system = CreateSystem();
        system.AddModifier(new ProximityModifier { MaxDelta = 0.01f });
        system.AddModifier(new SharedCultureModifier { MatchDelta = 0.005f });

        var context = CreateContext();
        for (var i = 1; i <= 10; i++)
        {
            system.Tick(i, context);
        }

        // After 10 ticks with proximity + shared culture bonus, strength should increase
        Assert.True(rel.Strength > 0.5f, $"Expected > 0.5, got {rel.Strength}");
    }

    [Fact]
    public void System_StrengthNeverExceeds1()
    {
        var a = _graph.CreateEntity(EntityKind.Faction, "generic_faction", "A", "c", 0, (0, 0));
        var b = _graph.CreateEntity(EntityKind.Faction, "generic_faction", "B", "c", 0, (0, 1));
        _graph.CreateRelationship("allied_with", RelationshipPolarity.Positive, a.Id, b.Id, 0.99f, 0);

        var system = CreateSystem();
        system.AddModifier(new ProximityModifier { MaxDelta = 0.1f });

        var context = CreateContext();
        for (var i = 1; i <= 20; i++)
        {
            system.Tick(i, context);
        }

        var rel = _graph.OutgoingRelationships(a.Id, "allied_with").First();
        Assert.True(rel.Strength <= 1f);
    }

    // --- Culling ---

    [Fact]
    public void System_CullsWeakRelationships()
    {
        var a = _graph.CreateEntity(EntityKind.Faction, "generic_faction", "A", "c", 0);
        var b = _graph.CreateEntity(EntityKind.Faction, "generic_faction", "B", "c", 0);
        _graph.CreateRelationship("allied_with", RelationshipPolarity.Positive, a.Id, b.Id, 0.02f, 0);

        var system = CreateSystem();
        system.AddModifier(new NeglectModifier { DecayDelta = -0.02f });

        system.Tick(1, CreateContext());

        // After one tick, strength should drop to ~0.0 and be culled (allied_with threshold 0.1)
        Assert.Equal(0, _graph.ActiveRelationshipCount);
        Assert.Equal(1, _graph.HistoricalRelationshipCount);
    }

    [Fact]
    public void System_CulledRelationshipPreservesHistory()
    {
        var a = _graph.CreateEntity(EntityKind.Faction, "generic_faction", "A", "c", 0);
        var b = _graph.CreateEntity(EntityKind.Faction, "generic_faction", "B", "c", 0);
        _graph.CreateRelationship("allied_with", RelationshipPolarity.Positive, a.Id, b.Id, 0.02f, 0);

        var system = CreateSystem();
        system.AddModifier(new NeglectModifier { DecayDelta = -0.02f });

        system.Tick(1, CreateContext());

        var history = _graph.AllRelationshipsBetween(a.Id, b.Id).ToList();
        Assert.Single(history);
        Assert.False(history[0].IsActive);
    }

    [Fact]
    public void System_DoesNotCullAboveThreshold()
    {
        var a = _graph.CreateEntity(EntityKind.Faction, "generic_faction", "A", "c", 0);
        var b = _graph.CreateEntity(EntityKind.Faction, "generic_faction", "B", "c", 0);
        _graph.CreateRelationship("allied_with", RelationshipPolarity.Positive, a.Id, b.Id, 0.5f, 0);

        var system = CreateSystem();
        system.AddModifier(new NeglectModifier { DecayDelta = -0.001f });

        system.Tick(1, CreateContext());

        Assert.Equal(1, _graph.ActiveRelationshipCount);
    }

    // --- Disposition ---

    [Fact]
    public void Disposition_HighForSimilarEntities()
    {
        var a = _graph.CreateEntity(EntityKind.Faction, "generic_faction", "A", "c", 0);
        var b = _graph.CreateEntity(EntityKind.Faction, "generic_faction", "B", "c", 0);

        var govPlane = new SemanticPlaneDefinition("government", "x", "y",
            ["ll", "ml", "hl", "lm", "mm", "hm", "lh", "mh", "hh"]);
        _planes.RegisterPlane(govPlane);
        _planes.SetPosition(a.Id, "government", 0.3f, 0.3f);
        _planes.SetPosition(b.Id, "government", 0.35f, 0.35f); // Very close

        var calc = new DispositionCalculator(CreateContext());
        calc.RegisterPlane("government");

        var disp = calc.ComputeDisposition(a.Id, b.Id);
        Assert.True(disp > 0.8f, $"Expected > 0.8, got {disp}");
    }

    [Fact]
    public void Disposition_LowForDifferentEntities()
    {
        var a = _graph.CreateEntity(EntityKind.Faction, "generic_faction", "A", "c", 0);
        var b = _graph.CreateEntity(EntityKind.Faction, "generic_faction", "B", "c", 0);

        var govPlane = new SemanticPlaneDefinition("government", "x", "y",
            ["ll", "ml", "hl", "lm", "mm", "hm", "lh", "mh", "hh"]);
        _planes.RegisterPlane(govPlane);
        _planes.SetPosition(a.Id, "government", 0.1f, 0.1f);
        _planes.SetPosition(b.Id, "government", 0.9f, 0.9f); // Opposite corners

        var calc = new DispositionCalculator(CreateContext());
        calc.RegisterPlane("government");

        var disp = calc.ComputeDisposition(a.Id, b.Id);
        Assert.True(disp < 0.3f, $"Expected < 0.3, got {disp}");
    }

    [Fact]
    public void Disposition_DefaultWhenNoPlanesRegistered()
    {
        var a = _graph.CreateEntity(EntityKind.Faction, "generic_faction", "A", "c", 0);
        var b = _graph.CreateEntity(EntityKind.Faction, "generic_faction", "B", "c", 0);

        var calc = new DispositionCalculator(CreateContext()) { DefaultDisposition = 0.5f };

        Assert.Equal(0.5f, calc.ComputeDisposition(a.Id, b.Id));
    }

    [Fact]
    public void Disposition_IsSymmetric()
    {
        var a = _graph.CreateEntity(EntityKind.Faction, "generic_faction", "A", "c", 0);
        var b = _graph.CreateEntity(EntityKind.Faction, "generic_faction", "B", "c", 0);

        var plane = new SemanticPlaneDefinition("ideology", "x", "y",
            ["1", "2", "3", "4", "5", "6", "7", "8", "9"]);
        _planes.RegisterPlane(plane);
        _planes.SetPosition(a.Id, "ideology", 0.2f, 0.8f);
        _planes.SetPosition(b.Id, "ideology", 0.7f, 0.3f);

        var calc = new DispositionCalculator(CreateContext());
        calc.RegisterPlane("ideology");

        Assert.Equal(
            calc.ComputeDisposition(a.Id, b.Id),
            calc.ComputeDisposition(b.Id, a.Id));
    }

    // --- Stub context ---

    private sealed class StubContext(WorldGraph graph, SemanticPlaneStore planes) : IRelationshipContext
    {
        public Entity? GetEntity(EntityId id) => graph.GetEntity(id);

        public float GetAttribute(EntityId entityId, string attributeName) => 0f;

        public (int Q, int R)? GetCoordinates(EntityId entityId) =>
            graph.GetEntity(entityId)?.Coordinates;

        public float? GetSemanticDistance(EntityId a, EntityId b, string planeName) =>
            planes.GetDistance(a, b, planeName);
    }
}
