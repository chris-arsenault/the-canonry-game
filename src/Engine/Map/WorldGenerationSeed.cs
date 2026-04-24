namespace Engine.Map;

/// <summary>
/// Replays an accepted procedural world exactly. The terrain and entity streams
/// are separate so runtime systems can wire entity-created subscribers between
/// map creation and seed population without changing the generated world.
/// </summary>
public readonly record struct WorldGenerationSeed(
    int BaseSeed,
    int Attempt,
    int TerrainSeed,
    int EntitySeed)
{
    public static WorldGenerationSeed FromBaseSeed(int baseSeed, int attempt)
    {
        return new WorldGenerationSeed(
            baseSeed,
            attempt,
            Derive(baseSeed, attempt, unchecked((int)0x6d2b79f5)),
            Derive(baseSeed, attempt, unchecked((int)0x1b873593)));
    }

    private static int Derive(int baseSeed, int attempt, int salt)
    {
        unchecked
        {
            var hash = baseSeed;
            hash = (hash * 397) ^ attempt;
            hash = (hash * 397) ^ salt;
            hash ^= hash >> 16;
            hash *= unchecked((int)0x85ebca6b);
            hash ^= hash >> 13;
            hash *= unchecked((int)0xc2b2ae35);
            hash ^= hash >> 16;
            return hash;
        }
    }
}
