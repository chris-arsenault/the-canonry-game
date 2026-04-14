namespace Engine.Tests.Processes;

using Engine.Processes;
using Engine.Graph;
using Engine.Kinds;
using ResourceType = Engine.Resources.ResourceType;

public class PreconditionTests
{
    // --- AND/OR composition ---

    [Fact]
    public void And_AllTrue_ReturnsTrue()
    {
        var and = new AndPrecondition([new AlwaysTrue(), new AlwaysTrue()]);
        Assert.True(and.Evaluate(new StubMutationContext()));
    }

    [Fact]
    public void And_OneFalse_ReturnsFalse()
    {
        var and = new AndPrecondition([new AlwaysTrue(), new NeverTrue()]);
        Assert.False(and.Evaluate(new StubMutationContext()));
    }

    [Fact]
    public void Or_OneFalseOneTrue_ReturnsTrue()
    {
        var or = new OrPrecondition([new NeverTrue(), new AlwaysTrue()]);
        Assert.True(or.Evaluate(new StubMutationContext()));
    }

    [Fact]
    public void Or_AllFalse_ReturnsFalse()
    {
        var or = new OrPrecondition([new NeverTrue(), new NeverTrue()]);
        Assert.False(or.Evaluate(new StubMutationContext()));
    }

    [Fact]
    public void Nested_AndOrComposition()
    {
        // AND(OR(false, true), true) = true
        var nested = new AndPrecondition([
            new OrPrecondition([new NeverTrue(), new AlwaysTrue()]),
            new AlwaysTrue(),
        ]);
        Assert.True(nested.Evaluate(new StubMutationContext()));
    }

    [Fact]
    public void Nested_AndOrComposition_Fails()
    {
        // AND(OR(false, false), true) = false
        var nested = new AndPrecondition([
            new OrPrecondition([new NeverTrue(), new NeverTrue()]),
            new AlwaysTrue(),
        ]);
        Assert.False(nested.Evaluate(new StubMutationContext()));
    }

    // --- Leaf conditions ---

    [Fact]
    public void PressureThreshold_PassesWhenInRange()
    {
        var ctx = new StubMutationContext { PressureValue = 60f };
        var check = new PressureThreshold("military_tension", new EntityId(1), min: 50f, max: 80f);
        Assert.True(check.Evaluate(ctx));
    }

    [Fact]
    public void PressureThreshold_FailsBelowMin()
    {
        var ctx = new StubMutationContext { PressureValue = 30f };
        var check = new PressureThreshold("military_tension", new EntityId(1), min: 50f, max: null);
        Assert.False(check.Evaluate(ctx));
    }

    [Fact]
    public void PressureThreshold_FailsAboveMax()
    {
        var ctx = new StubMutationContext { PressureValue = 90f };
        var check = new PressureThreshold("military_tension", new EntityId(1), min: null, max: 80f);
        Assert.False(check.Evaluate(ctx));
    }

    [Fact]
    public void AttributeThreshold_PassesInRange()
    {
        var ctx = new StubMutationContext { AttributeValue = 40f };
        var check = new AttributeThreshold(new EntityId(1), "stability", min: 20f, max: null);
        Assert.True(check.Evaluate(ctx));
    }

    [Fact]
    public void AttributeThreshold_FailsBelowMin()
    {
        var ctx = new StubMutationContext { AttributeValue = 10f };
        var check = new AttributeThreshold(new EntityId(1), "stability", min: 20f, max: null);
        Assert.False(check.Evaluate(ctx));
    }

    [Fact]
    public void EntityCountCheck_Passes()
    {
        var ctx = new StubMutationContext { EntityCount = 3 };
        var check = new EntityCountCheck(EntityKind.Nation, min: 2);
        Assert.True(check.Evaluate(ctx));
    }

    [Fact]
    public void EntityCountCheck_FailsBelowMin()
    {
        var ctx = new StubMutationContext { EntityCount = 1 };
        var check = new EntityCountCheck(EntityKind.Nation, min: 2);
        Assert.False(check.Evaluate(ctx));
    }

    [Fact]
    public void CooldownCheck_PassesWhenNeverFired()
    {
        var ctx = new StubMutationContext { LastFiredTick = null, CurrentTickValue = 10 };
        var check = new CooldownCheck(5);
        Assert.True(check.Evaluate(ctx));
    }

    [Fact]
    public void CooldownCheck_PassesAfterCooldown()
    {
        var ctx = new StubMutationContext { LastFiredTick = 5, CurrentTickValue = 20 };
        var check = new CooldownCheck(10);
        Assert.True(check.Evaluate(ctx));
    }

    [Fact]
    public void CooldownCheck_FailsDuringCooldown()
    {
        var ctx = new StubMutationContext { LastFiredTick = 5, CurrentTickValue = 10 };
        var check = new CooldownCheck(10);
        Assert.False(check.Evaluate(ctx));
    }

    // --- Describe ---

    [Fact]
    public void Describe_ProducesReadableOutput()
    {
        var and = new AndPrecondition([
            new PressureThreshold("tension", new EntityId(1), min: 50f, max: null),
            new EntityCountCheck(EntityKind.Nation, min: 2),
        ]);

        var desc = and.Describe();
        Assert.Contains("ALL(", desc, StringComparison.Ordinal);
        Assert.Contains("pressure(", desc, StringComparison.Ordinal);
        Assert.Contains("entity_count(", desc, StringComparison.Ordinal);
    }

    // --- Test helpers ---

    private sealed class NeverTrue : IPrecondition
    {
        public bool Evaluate(IMutationContext context) => false;
        public string Describe() => "never_true";
    }

    private sealed class StubMutationContext : IMutationContext
    {
        public int CurrentTick => CurrentTickValue;
        public EntityId SelfEntityId => new EntityId(0);
        public Random Random { get; } = new Random(42);
        public (int Q, int R)? ResolvedTarget => null;
        public int CurrentTickValue { get; set; } = 10;
        public float PressureValue { get; set; }
        public float AttributeValue { get; set; }
        public int EntityCount { get; set; }
        public int? LastFiredTick { get; set; }
        public Relationship? RelationshipResult { get; set; }

        public float GetPressure(string pressureName, EntityId scopeId) => PressureValue;
        public float GetAttribute(EntityId entityId, string attributeName) => AttributeValue;
        public int CountEntitiesByKind(EntityKind kind) => EntityCount;
        public Relationship? FindRelationship(EntityId sourceId, EntityId targetId, string kind) => RelationshipResult;
        public int? GetLastFiredTick() => LastFiredTick;
        public Entity? GetEntity(EntityId id) => null;

        public Entity CreateEntity(EntityKind kind, string subtype, string name, string culture, (int Q, int R)? coordinates) =>
            new(new EntityId(99), kind, subtype, name, culture, CurrentTick, coordinates);

        public void CreateRelationship(string kind, RelationshipPolarity polarity, EntityId sourceId, EntityId targetId, float strength) { }
        public void ModifyAttribute(EntityId entityId, string attributeName, float delta) { }
        public void ModifyPressure(string pressureName, EntityId scopeId, float delta) { }
        public void SetTag(EntityId entityId, string key, string value) { }
        public void SetEntityHistorical(EntityId entityId) { }
        public EntityId? FindRelatedEntity(EntityId sourceId, string relationshipKind) => null;
        public IReadOnlyCollection<EntityId> GetEntitiesByKind(EntityKind kind) => [];
        public float GetResource((int Q, int R) coordinates, ResourceType resourceType) => 0f;
        public float ConsumeResource((int Q, int R) coordinates, ResourceType resourceType, float amount) => 0f;
        public void SetCoordinates(EntityId entityId, int q, int r) { }
        public void CullRelationship(EntityId sourceId, string relationshipKind) { }
    }
}
