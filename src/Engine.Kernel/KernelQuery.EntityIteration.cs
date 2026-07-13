namespace Engine.Kernel;

internal static partial class KernelQuery
{
    public static int EntityRangeEndExclusive(WorldState world) =>
        world.EntityHighWatermark + 1;

    public static int FirstActiveEntity(WorldState world) =>
        NextActiveEntity(world, 0);

    public static int NextActiveEntity(WorldState world, int entityId)
    {
        for (var candidateId = entityId + 1; candidateId <= world.EntityHighWatermark; candidateId++)
        {
            if (world.EntityIsActive(candidateId))
            {
                return candidateId;
            }
        }

        return 0;
    }

    public static int FirstActiveEntityOfKind(WorldState world, int kindId)
    {
        for (var entityId = world.FirstEntityOfKind(kindId);
             entityId > 0;
             entityId = world.NextEntityOfKind(entityId))
        {
            if (world.EntityIsActive(entityId))
            {
                return entityId;
            }
        }

        return 0;
    }

    public static int NextActiveEntityOfKind(WorldState world, int entityId)
    {
        for (var candidateId = world.NextEntityOfKind(entityId);
             candidateId > 0;
             candidateId = world.NextEntityOfKind(candidateId))
        {
            if (world.EntityIsActive(candidateId))
            {
                return candidateId;
            }
        }

        return 0;
    }
}
