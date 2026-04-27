namespace Engine.Domain.Tests.Processes;

using Engine.Graph;
using Engine.Kinds;
using Engine.Processes;
using Engine.Wiring;

public class PreconditionResultTests
{
    [Fact]
    public void TargetIsSelf_PassesWhenTargetIsSelf()
    {
        var ctx = CreateContext(selfId: 1, targetId: 1);
        var result = new TargetIsSelf().Evaluate(ctx);
        Assert.True(result.Passed);
    }

    [Fact]
    public void TargetIsSelf_FailsWhenTargetIsDifferent()
    {
        var ctx = CreateContext(selfId: 1, targetId: 2);
        var result = new TargetIsSelf().Evaluate(ctx);
        Assert.False(result.Passed);
    }

    [Fact]
    public void TargetIsNotSelf_PassesWhenTargetIsDifferent()
    {
        var ctx = CreateContext(selfId: 1, targetId: 2);
        var result = new TargetIsNotSelf().Evaluate(ctx);
        Assert.True(result.Passed);
    }

    [Fact]
    public void TargetIsNotSelf_FailsWhenTargetIsSelf()
    {
        var ctx = CreateContext(selfId: 1, targetId: 1);
        var result = new TargetIsNotSelf().Evaluate(ctx);
        Assert.False(result.Passed);
    }

    [Fact]
    public void SelfAttributeThreshold_DiagnosticShowsValues()
    {
        var graph = new WorldGraph();
        var entity = graph.CreateEntity(TestKinds.Settlement, "village", "Town", "c", tick: 0);
        entity.Attributes.Add(new Engine.Attributes.ScalarValue("sustenance", 0, 100, 2));

        var ctx = CreateContextWithEntity(entity);
        var result = new SelfAttributeThreshold("sustenance", min: 5f, max: null).Evaluate(ctx);

        Assert.False(result.Passed);
        Assert.Contains("sustenance", result.Diagnostic, System.StringComparison.Ordinal);
        Assert.Contains("2.0", result.Diagnostic, System.StringComparison.Ordinal);
        Assert.Contains(AttributeRef.Self("sustenance", TestKinds.Settlement), result.Blockers);
    }

    [Fact]
    public void AndPrecondition_CollectsAllBlockers()
    {
        var graph = new WorldGraph();
        var entity = graph.CreateEntity(TestKinds.Settlement, "village", "Town", "c", tick: 0);
        entity.Attributes.Add(new Engine.Attributes.ScalarValue("fame", 0, 100, 0));
        entity.Attributes.Add(new Engine.Attributes.ScalarValue("sustenance", 0, 100, 1));

        var ctx = CreateContextWithEntity(entity);
        var and = new AndPrecondition([
            new SelfAttributeThreshold("fame", min: 5f, max: null),
            new SelfAttributeThreshold("sustenance", min: 10f, max: null),
        ]);

        var result = and.Evaluate(ctx);

        Assert.False(result.Passed);
        Assert.Contains(AttributeRef.Self("fame", TestKinds.Settlement), result.Blockers);
        Assert.Contains(AttributeRef.Self("sustenance", TestKinds.Settlement), result.Blockers);
    }

    private static StubMutationContext CreateContext(int selfId, int targetId)
    {
        return new StubMutationContext(new EntityId(selfId), new EntityId(targetId));
    }

    private static StubMutationContext CreateContextWithEntity(Entity entity)
    {
        return new StubMutationContext(entity);
    }

    private sealed class StubMutationContext : IMutationContext
    {
        private readonly Entity? _entity;
        public int CurrentTick => 0;
        public EntityId SelfEntityId { get; }
        public Random Random { get; } = new(42);
        public EntityId? ResolvedTargetId { get; }

        public StubMutationContext(EntityId selfId, EntityId targetId)
        {
            SelfEntityId = selfId;
            ResolvedTargetId = targetId;
        }

        public StubMutationContext(Entity entity)
        {
            _entity = entity;
            SelfEntityId = entity.Id;
            ResolvedTargetId = entity.Id;
        }

        public float GetPressure(string pressureName, EntityId scopeId) => 0f;
        public float GetAttribute(EntityId entityId, string attributeName) => 0f;
        public int CountEntitiesByKind(EntityKind kind) => 0;
        public Relationship? FindRelationship(EntityId sourceId, EntityId targetId, string kind) => null;
        public int? GetLastFiredTick() => null;
        public Entity? GetEntity(EntityId id) => _entity?.Id == id ? _entity : null;
        public EntityId? FindRelatedEntity(EntityId sourceId, string relationshipKind) => null;
        public IReadOnlyCollection<EntityId> GetEntitiesByKind(EntityKind kind) => [];
        public float GetResource((int Q, int R) coordinates, Engine.Resources.ResourceType resourceType) => 0f;
        public Entity CreateEntity(EntityKind kind, string subtype, string name, string culture, (int Q, int R)? coordinates) => throw new System.NotSupportedException();
        public void CreateRelationship(string kind, RelationshipPolarity polarity, EntityId sourceId, EntityId targetId, float strength) { }
        public void ModifyAttribute(EntityId entityId, string attributeName, float delta) { }
        public void ModifyPressure(string pressureName, EntityId scopeId, float delta) { }
        public void ModifyResource((int Q, int R) coordinates, Engine.Resources.ResourceType resourceType, float delta) { }
        public void SetEntityHistorical(EntityId entityId) { }
        public float ConsumeResource((int Q, int R) coordinates, Engine.Resources.ResourceType resourceType, float amount) => 0f;
        public void SetCoordinates(EntityId entityId, int q, int r) { }
        public void SetSemanticCoordinates(EntityId entityId, int q, int r) { }
        public void CullRelationship(EntityId sourceId, string relationshipKind) { }
        public void SetVariable(string name, float value) { }
        public float GetVariable(string name) => 0f;
    }
}
