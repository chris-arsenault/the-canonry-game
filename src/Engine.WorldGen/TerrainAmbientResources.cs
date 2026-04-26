namespace Engine.WorldGen;

/// <summary>
/// Terrain-specific ambient resource bands used by map generation.
/// These values describe the latent possibility space of a hex, not concrete
/// actor-owned stores.
/// </summary>
public static class TerrainAmbientResources
{
    public static void Seed(HexCell cell, Random random)
    {
        ArgumentNullException.ThrowIfNull(cell);
        ArgumentNullException.ThrowIfNull(random);

        foreach (var (resourceName, range) in cell.Terrain.AmbientResources)
        {
            cell.SetAmbientResource(resourceName, range.Roll(random));
        }
    }
}

public readonly record struct AmbientValueRange(float Min, float Max)
{
    public float Roll(Random random)
    {
        ArgumentNullException.ThrowIfNull(random);
        if (Max <= Min)
        {
            return Min;
        }

        return (float)(Min + (random.NextDouble() * (Max - Min)));
    }

    public bool Contains(float value)
    {
        return value >= Min && value <= Max;
    }
}
