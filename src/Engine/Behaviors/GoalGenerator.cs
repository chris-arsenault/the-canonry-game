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

                // Find and score providers from process wiring contracts
                var scoredProviders = FindAndScoreProviders(procDef, entity, worldState);

                foreach (var (providerId, providerWeight) in scoredProviders)
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

    private static List<(EntityId Id, float Weight)> FindAndScoreProviders(
        ProcessDefinition procDef, Entity consumer, IWorldState worldState)
    {
        // Collect target refs to know what to search for
        AttributeRef? searchRef = null;
        foreach (var r in procDef.Reads)
        {
            if (r.Scope == RefScope.Target && r.Kind.HasValue) { searchRef = r; break; }
        }

        if (searchRef is null)
        {
            foreach (var w in procDef.Writes)
            {
                if (w.Scope == RefScope.Target && w.Kind.HasValue) { searchRef = w; break; }
            }
        }

        if (searchRef is null)
        {
            // No target refs — entity is its own context
            return [(consumer.Id, 1f)];
        }

        // Find candidates (unscored)
        var candidates = GraphAttributeQuery.FindCandidates(
            AttributeRef.Self(searchRef.Name, searchRef.Kind!.Value), worldState);

        if (candidates.Count == 0) return [];

        // Score candidates using the process's ProviderScoreExpression
        var scored = new List<(EntityId Id, float Weight)>();
        foreach (var candidateId in candidates)
        {
            if (candidateId == consumer.Id) continue;

            var candidateEntity = worldState.Graph.GetEntity(candidateId);
            if (candidateEntity is null) continue;

            float weight;
            if (procDef.ProviderScoreExpression is not null)
            {
                var exprCtx = new ExpressionContext(consumer, candidateEntity, worldState.Grid);
                weight = Math.Max(0f, procDef.ProviderScoreExpression.Evaluate(exprCtx));
            }
            else
            {
                weight = 1f; // No expression — all providers equal
            }

            if (weight > 0f)
            {
                scored.Add((candidateId, weight));
            }
        }

        if (scored.Count == 0) return [];

        // Normalize weights relative to best
        var maxWeight = 0f;
        foreach (var (_, w) in scored)
        {
            if (w > maxWeight) maxWeight = w;
        }

        var result = new List<(EntityId, float)>();
        foreach (var (id, w) in scored)
        {
            result.Add((id, maxWeight > 0f ? w / maxWeight : 1f));
        }

        // Sort by weight, take top 3
        result.Sort((a, b) => b.Item2.CompareTo(a.Item2));
        if (result.Count > 3) result.RemoveRange(3, result.Count - 3);

        return result;
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
