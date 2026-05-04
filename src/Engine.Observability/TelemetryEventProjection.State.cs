namespace Engine.Observability;

internal sealed partial class TelemetryEventProjection
{
    private GeneratorStateProjection GetGeneratorState(int generatorId)
    {
        if (_generatorStates.TryGetValue(generatorId, out var state))
        {
            return state;
        }

        state = new GeneratorStateProjection(generatorId);
        _generatorStates.Add(generatorId, state);
        return state;
    }

    private float ObservedHealth(ObservedEntityProjection entity) =>
        entity.Attributes.Count == 0 ? 0f : entity.Attributes.Values.Average();

    private sealed class PendingGeneratorFire(int tick, int generatorId, int causeId, int remaining)
    {
        public int Tick { get; } = tick;

        public int GeneratorId { get; } = generatorId;

        public int CauseId { get; } = causeId;

        public int Remaining { get; set; } = remaining;
    }

}

internal readonly record struct ObservedRelationshipKey(
    int SourceEntityId,
    int TargetEntityId,
    int RelationshipKindId);

internal readonly record struct ObservedMapCellKey(int Q, int R);

internal sealed class GeneratorStateProjection(int generatorId)
{
    public int GeneratorId { get; } = generatorId;

    public bool Enabled { get; set; } = true;

    public int RunCount { get; set; }

    public int LastRunTick { get; set; }
}

internal sealed class ObservedMapCellProjection(
    ObservedMapCellKey key,
    int terrainId,
    int ownerEntityId,
    int superregionId,
    int continentId,
    int lastUpdatedTick,
    long worldVersion)
{
    public ObservedMapCellKey Key { get; } = key;

    public int TerrainId { get; } = terrainId;

    public int OwnerEntityId { get; } = ownerEntityId;

    public int SuperregionId { get; } = superregionId;

    public int ContinentId { get; } = continentId;

    public int LastUpdatedTick { get; } = lastUpdatedTick;

    public long WorldVersion { get; } = worldVersion;
}

internal sealed class ObservedRelationshipProjection(
    ObservedRelationshipKey key,
    int changeCodeId,
    int polarityCode,
    float strength,
    bool isActive,
    int lastUpdatedTick,
    long worldVersion)
{
    public ObservedRelationshipKey Key { get; } = key;

    public int RelationshipKindId => Key.RelationshipKindId;

    public int ChangeCodeId { get; } = changeCodeId;

    public int PolarityCode { get; } = polarityCode;

    public float Strength { get; } = strength;

    public bool IsActive { get; } = isActive;

    public int LastUpdatedTick { get; } = lastUpdatedTick;

    public long WorldVersion { get; } = worldVersion;
}

internal sealed class ObservedEntityProjection(int entityId)
{
    public int EntityId { get; } = entityId;

    public int KindId { get; internal set; }

    public int SubtypeId { get; internal set; }

    public int CultureId { get; internal set; }

    public int NameSeed { get; internal set; }

    public int? PreviousStatusCode { get; internal set; }

    public int StatusCode { get; internal set; }

    public int PhysicalQ { get; internal set; }

    public int PhysicalR { get; internal set; }

    public int SemanticQ { get; internal set; }

    public int SemanticR { get; internal set; }

    public int SemanticS { get; internal set; }

    public int? CreatedTick { get; internal set; }

    public int LastUpdatedTick { get; internal set; }

    public long WorldVersion { get; internal set; }

    public Dictionary<int, float> Attributes { get; } = [];
}
