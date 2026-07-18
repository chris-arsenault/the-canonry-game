namespace IceRemembers.Seed;

using Engine.WorldGen;

using IceRemembers;

internal static partial class IceRemembersSeedBuilder
{
    private static float CellNoise(HexCoord coord, int salt)
    {
        unchecked
        {
            var hash = salt;
            hash = (hash * 397) ^ coord.Q;
            hash = (hash * 397) ^ coord.R;
            hash ^= hash >> 13;
            hash *= 16777619;
            return (hash & 0x7fffffff) / (float)int.MaxValue;
        }
    }

    private static HashSet<string> TerrainSet(params IceRemembersTerrain[] terrains) =>
        terrains.Select(terrain => terrain.ToDomainName()).ToHashSet(StringComparer.Ordinal);
}
