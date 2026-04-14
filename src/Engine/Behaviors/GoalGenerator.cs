namespace Engine.Behaviors;

using Engine.Domain;
using Engine.Graph;
using Engine.Kinds;
using Engine.Processes;

/// <summary>
/// Generates goals for an entity from attribute deficits and available providers.
/// Also generates sub-goals by analyzing blocked preconditions on serving processes.
/// Replaces NeedResolver.
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

    /// <summary>Generate goals for an entity. Returns primary goals + sub-goals.</summary>
    public IReadOnlyList<EntityGoal> Generate(Entity entity, IWorldState worldState)
    {
        ArgumentNullException.ThrowIfNull(entity);
        ArgumentNullException.ThrowIfNull(worldState);

        if (!_kindConfigs.TryGetValue(entity.Kind, out var kindConfig))
            return [];

        // 1. Compute attribute deficits → primary goals
        var primaryGoals = GeneratePrimaryGoals(entity, kindConfig, worldState);

        // 2. For each primary goal, analyze blocked processes → sub-goals
        var allGoals = new List<EntityGoal>();
        foreach (var goal in primaryGoals)
        {
            allGoals.Add(goal);
            var subGoals = GenerateSubGoals(goal, entity, worldState);
            allGoals.AddRange(subGoals);
        }

        return allGoals;
    }

    private List<EntityGoal> GeneratePrimaryGoals(
        Entity entity, KindConfig kindConfig, IWorldState worldState)
    {
        // Find attribute needs
        var deficits = new List<(string Attribute, float Urgency)>();

        // Health attribute always generates a goal — entity always wants more health
        if (kindConfig.HealthAttribute is not null)
        {
            var healthScalar = entity.Attributes.GetScalar(kindConfig.HealthAttribute);
            if (healthScalar is not null)
            {
                var range = healthScalar.Max - healthScalar.Min;
                // Urgency: lower health = more urgent. At max health, urgency is still > 0.
                var healthRatio = range > 0 ? (healthScalar.Value - healthScalar.Min) / range : 1f;
                var urgency = Math.Max(0.1f, 1f - healthRatio); // Always at least 0.1
                deficits.Add((kindConfig.HealthAttribute, urgency));
            }
        }

        // Other attributes: deficit from default
        foreach (var attrConfig in kindConfig.Attributes)
        {
            if (attrConfig.Name == kindConfig.HealthAttribute) continue; // Already handled
            if (attrConfig.PlaneDefinition is not null) continue;

            var scalar = entity.Attributes.GetScalar(attrConfig.Name);
            if (scalar is null) continue;

            var range = attrConfig.Max - attrConfig.Min;
            if (range <= 0f) continue;

            var distance = Math.Abs(scalar.Value - attrConfig.Default);
            var urgency = distance / range;
            if (urgency > 0.01f)
            {
                deficits.Add((attrConfig.Name, urgency));
            }
        }

        // Sort by urgency, take top N
        deficits.Sort((a, b) => b.Urgency.CompareTo(a.Urgency));

        var goals = new List<EntityGoal>();
        var usedProviders = new HashSet<EntityId>();

        foreach (var (attribute, urgency) in deficits)
        {
            if (goals.Count >= MaxGoals) break;

            // Self-directed goal
            goals.Add(new EntityGoal(attribute, entity.Id, urgency));

            // External goal: find providers on the graph
            var providers = GraphAttributeQuery.FindProviders(
                Wiring.AttributeRef.Self(attribute, entity.Kind),
                entity, worldState, topN: 1);

            foreach (var (providerId, _) in providers)
            {
                if (providerId == entity.Id) continue;
                if (usedProviders.Contains(providerId)) continue;
                if (goals.Count >= MaxGoals) break;

                usedProviders.Add(providerId);
                goals.Add(new EntityGoal(attribute, providerId, urgency * 0.7f));
            }
        }

        return goals;
    }

    private List<EntityGoal> GenerateSubGoals(
        EntityGoal parentGoal, Entity entity, IWorldState worldState)
    {
        var subGoals = new List<EntityGoal>();

        // Find processes that write to the parent goal's attribute
        foreach (var procDef in _processes)
        {
            if (procDef.TargetKind != entity.Kind) continue;

            var writesToGoalAttr = false;
            foreach (var write in procDef.Writes)
            {
                if (write.Name == parentGoal.Attribute)
                {
                    writesToGoalAttr = true;
                    break;
                }
            }

            if (!writesToGoalAttr) continue;

            // Build a temporary context with the goal's provider as target
            var context = new GoalCheckContext(entity, parentGoal.Provider, worldState);

            // Check if precondition fails
            if (procDef.Precondition.Evaluate(context)) continue; // Passes — no sub-goal needed

            // Precondition fails — what's blocking?
            var blockingAttrs = procDef.Precondition.GetBlockingAttributes();

            foreach (var blockingAttr in blockingAttrs)
            {
                // Check we don't already have a sub-goal for this
                var alreadyExists = false;
                foreach (var existing in subGoals)
                {
                    if (existing.BlockingAttribute == blockingAttr)
                    {
                        alreadyExists = true;
                        break;
                    }
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

    /// <summary>Minimal IMutationContext for precondition checking during goal generation.</summary>
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
        public int CountEntitiesByKind(Kinds.EntityKind kind) =>
            _worldState.Graph.GetEntitiesByKind(kind).Count;
        public Relationship? FindRelationship(EntityId sourceId, EntityId targetId, string kind) => null;
        public int? GetLastFiredTick() => null;
        public Entity? GetEntity(EntityId id) => _worldState.Graph.GetEntity(id);
        public EntityId? FindRelatedEntity(EntityId sourceId, string relationshipKind) => null;
        public IReadOnlyCollection<EntityId> GetEntitiesByKind(Kinds.EntityKind kind) =>
            _worldState.Graph.GetEntitiesByKind(kind);
        public float GetResource((int Q, int R) coordinates, Resources.ResourceType resourceType) => 0f;
        public Entity CreateEntity(Kinds.EntityKind kind, string subtype, string name, string culture, (int Q, int R)? coordinates) =>
            throw new NotSupportedException();
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
