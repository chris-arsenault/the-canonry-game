namespace Engine.Behaviors;

using Engine.Domain;
using Engine.Graph;
using Engine.Kinds;
using Engine.Processes;
using Engine.Wiring;

/// <summary>
/// Generates goals for an entity from attribute deficits and process wiring contracts.
/// No self/external distinction — providers derived from process Target refs.
/// Sub-goals generated from blocked preconditions.
/// </summary>
public sealed class GoalGenerator
{
    private readonly IReadOnlyDictionary<EntityKind, KindConfig> _kindConfigs;
    private readonly IReadOnlyList<ProcessDefinition> _processes;

    /// <summary>Max primary goals per entity.</summary>
    public int MaxGoals { get; set; } = 3;

    public GoalGenerator(
        IReadOnlyDictionary<EntityKind, KindConfig> kindConfigs,
        IReadOnlyList<ProcessDefinition> processes)
    {
        _kindConfigs = kindConfigs;
        _processes = processes;
    }

    public IReadOnlyList<EntityGoal> Generate(Entity entity, IWorldState worldState)
    {
        ArgumentNullException.ThrowIfNull(entity);
        ArgumentNullException.ThrowIfNull(worldState);

        if (!_kindConfigs.TryGetValue(entity.Kind, out var kindConfig))
            return [];

        var primaryGoals = GeneratePrimaryGoals(entity, kindConfig, worldState);

        var allGoals = new List<EntityGoal>();
        foreach (var goal in primaryGoals)
        {
            allGoals.Add(goal);
            allGoals.AddRange(GenerateSubGoals(goal, entity, worldState));
        }

        return allGoals;
    }

    private List<EntityGoal> GeneratePrimaryGoals(
        Entity entity, KindConfig kindConfig, IWorldState worldState)
    {
        var deficits = new List<(string Attribute, float Urgency)>();

        foreach (var attrConfig in kindConfig.Attributes)
        {
            if (attrConfig.PlaneDefinition is not null) continue;

            var scalar = entity.Attributes.GetScalar(attrConfig.Name);
            if (scalar is null) continue;

            float urgency;
            if (attrConfig.Baseline > 0f)
            {
                urgency = attrConfig.Baseline / (attrConfig.Baseline + scalar.Value);
            }
            else
            {
                var range = attrConfig.Max - attrConfig.Min;
                urgency = range > 0f ? Math.Abs(scalar.Value - attrConfig.Default) / range : 0f;
            }

            if (urgency < 0.01f) continue;
            deficits.Add((attrConfig.Name, urgency));
        }

        deficits.Sort((a, b) => b.Urgency.CompareTo(a.Urgency));

        // For each deficit, find providers from process wiring contracts
        var goals = new List<EntityGoal>();
        var usedProviders = new HashSet<(string Attr, EntityId Provider)>();

        foreach (var (attribute, urgency) in deficits)
        {
            if (goals.Count >= MaxGoals) break;

            foreach (var procDef in _processes)
            {
                if (procDef.TargetKind != entity.Kind) continue;
                if (goals.Count >= MaxGoals) break;

                var writesToAttr = false;
                foreach (var write in procDef.Writes)
                {
                    if (write.Name == attribute) { writesToAttr = true; break; }
                }

                if (!writesToAttr) continue;

                // Collect target refs from this process's contracts
                var targetRefs = CollectTargetRefs(procDef);

                // Find providers based on target refs
                var providers = targetRefs.Count > 0
                    ? FindTargetProviders(targetRefs, entity, worldState)
                    : [(entity.Id, 1f)]; // No target refs → self is the context

                foreach (var (providerId, providerWeight) in providers)
                {
                    if (goals.Count >= MaxGoals) break;
                    var key = (attribute, providerId);
                    if (usedProviders.Contains(key)) continue;
                    usedProviders.Add(key);
                    goals.Add(new EntityGoal(attribute, providerId, urgency * providerWeight));
                }
            }
        }

        return goals;
    }

    private static List<AttributeRef> CollectTargetRefs(ProcessDefinition procDef)
    {
        var refs = new List<AttributeRef>();
        foreach (var r in procDef.Reads)
        {
            if (r.Scope == RefScope.Target) refs.Add(r);
        }

        foreach (var w in procDef.Writes)
        {
            if (w.Scope == RefScope.Target) refs.Add(w);
        }

        return refs;
    }

    private static List<(EntityId Id, float Weight)> FindTargetProviders(
        List<AttributeRef> targetRefs, Entity consumer, IWorldState worldState)
    {
        foreach (var tRef in targetRefs)
        {
            if (!tRef.Kind.HasValue) continue;

            var providers = GraphAttributeQuery.FindProviders(
                AttributeRef.Self(tRef.Name, tRef.Kind.Value),
                consumer, worldState, topN: 3);

            if (providers.Count == 0) continue;

            // Normalize weights relative to the best provider
            var maxWeight = 0f;
            foreach (var (_, w) in providers)
            {
                if (w > maxWeight) maxWeight = w;
            }

            var result = new List<(EntityId, float)>();
            foreach (var (providerId, weight) in providers)
            {
                if (providerId == consumer.Id) continue;
                var normalizedWeight = maxWeight > 0f ? weight / maxWeight : 1f;
                result.Add((providerId, normalizedWeight));
            }

            if (result.Count > 0) return result;
        }

        return [];
    }

    private List<EntityGoal> GenerateSubGoals(
        EntityGoal parentGoal, Entity entity, IWorldState worldState)
    {
        var subGoals = new List<EntityGoal>();

        foreach (var procDef in _processes)
        {
            if (procDef.TargetKind != entity.Kind) continue;

            var writesToGoalAttr = false;
            foreach (var write in procDef.Writes)
            {
                if (write.Name == parentGoal.Attribute) { writesToGoalAttr = true; break; }
            }

            if (!writesToGoalAttr) continue;

            var context = new GoalCheckContext(entity, parentGoal.Provider, worldState);
            var result = procDef.Precondition.Evaluate(context);
            if (result.Passed) continue;

            foreach (var blockingAttr in result.BlockingAttributes)
            {
                var alreadyExists = false;
                foreach (var existing in subGoals)
                {
                    if (existing.BlockingAttribute == blockingAttr) { alreadyExists = true; break; }
                }

                if (!alreadyExists)
                {
                    subGoals.Add(new EntityGoal(
                        parentGoal.Attribute, parentGoal.Provider, parentGoal.Urgency,
                        parentGoalId: parentGoal.Id, blockingAttribute: blockingAttr));
                }
            }
        }

        return subGoals;
    }

    private sealed class GoalCheckContext : IMutationContext
    {
        private readonly Entity _entity;
        private readonly IWorldState _worldState;

        public int CurrentTick => _worldState.CurrentTick;
        public EntityId SelfEntityId => _entity.Id;
        public Random Random => Random.Shared;
        public EntityId? ResolvedTargetId { get; }

        public GoalCheckContext(Entity entity, EntityId targetId, IWorldState worldState)
        {
            _entity = entity;
            ResolvedTargetId = targetId;
            _worldState = worldState;
        }

        public float GetPressure(string pressureName, EntityId scopeId) => 0f;
        public float GetAttribute(EntityId entityId, string attributeName) =>
            _worldState.Graph.GetEntity(entityId)?.Attributes.GetValue(attributeName) ?? 0f;
        public int CountEntitiesByKind(EntityKind kind) => _worldState.Graph.GetEntitiesByKind(kind).Count;
        public Relationship? FindRelationship(EntityId sourceId, EntityId targetId, string kind) => null;
        public int? GetLastFiredTick() => null;
        public Entity? GetEntity(EntityId id) => _worldState.Graph.GetEntity(id);
        public EntityId? FindRelatedEntity(EntityId sourceId, string relationshipKind) => null;
        public IReadOnlyCollection<EntityId> GetEntitiesByKind(EntityKind kind) => _worldState.Graph.GetEntitiesByKind(kind);
        public float GetResource((int Q, int R) coordinates, Resources.ResourceType resourceType) => 0f;
        public Entity CreateEntity(EntityKind kind, string subtype, string name, string culture, (int Q, int R)? coordinates) => throw new NotSupportedException();
        public void CreateRelationship(string kind, RelationshipPolarity polarity, EntityId sourceId, EntityId targetId, float strength) { }
        public void ModifyAttribute(EntityId entityId, string attributeName, float delta) { }
        public void ModifyPressure(string pressureName, EntityId scopeId, float delta) { }
        public void SetTag(EntityId entityId, string key, string value) { }
        public void SetEntityHistorical(EntityId entityId) { }
        public float ConsumeResource((int Q, int R) coordinates, Resources.ResourceType resourceType, float amount) => 0f;
        public void SetCoordinates(EntityId entityId, int q, int r) { }
        public void CullRelationship(EntityId sourceId, string relationshipKind) { }
        public void SetVariable(string name, float value) { }
        public float GetVariable(string name) => 0f;
    }
}
