namespace Engine.Kernel;

internal sealed class FrozenTickView
{
    private readonly int[] _entityIds;
    private readonly int[] _relationshipIds;
    private int _entityCount;
    private int _relationshipCount;

    public FrozenTickView(int entityCapacity, int relationshipCapacity)
    {
        _entityIds = new int[entityCapacity];
        _relationshipIds = new int[relationshipCapacity];
    }

    public int Tick { get; private set; }
    public long WorldVersion { get; private set; }
    public int EntityCount => _entityCount;
    public int RelationshipCount => _relationshipCount;

    public void Capture(WorldState world, int tick)
    {
        Tick = tick;
        WorldVersion = world.WorldVersion;
        _entityCount = 0;
        for (var entityId = world.FirstActiveEntity();
             entityId > 0;
             entityId = world.NextActiveEntity(entityId))
        {
            _entityIds[_entityCount++] = entityId;
        }

        _relationshipCount = world.ActiveRelationshipCount;
        for (var index = 0; index < _relationshipCount; index++)
        {
            _relationshipIds[index] = world.ActiveRelationshipAt(index);
        }
    }

    public int EntityAt(int index, bool reverse) =>
        _entityIds[reverse ? _entityCount - index - 1 : index];

    public int RelationshipAt(int index, bool reverse) =>
        _relationshipIds[reverse ? _relationshipCount - index - 1 : index];
}
