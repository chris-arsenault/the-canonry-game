namespace Engine.Map;

/// <summary>
/// Terrain-specific ambient resource bands used by map generation.
/// These values describe the latent possibility space of a hex, not concrete
/// actor-owned stores.
/// </summary>
public static class TerrainAmbientResources
{
    public static TerrainAmbientProfile GetProfile(TerrainType terrain)
    {
        return terrain switch
        {
            TerrainType.Tundra => new TerrainAmbientProfile(
                Food: new AmbientValueRange(4f, 6f),
                Wealth: new AmbientValueRange(2f, 4f),
                Population: new AmbientValueRange(3f, 5f)),
            TerrainType.HotSprings => new TerrainAmbientProfile(
                Food: new AmbientValueRange(5f, 7f),
                Wealth: new AmbientValueRange(2f, 4f),
                Population: new AmbientValueRange(4f, 6f)),
            TerrainType.Shelf => new TerrainAmbientProfile(
                Food: new AmbientValueRange(1f, 3f),
                Wealth: new AmbientValueRange(1f, 2f),
                Population: new AmbientValueRange(1f, 2f)),
            TerrainType.Crags => new TerrainAmbientProfile(
                Food: new AmbientValueRange(0f, 2f),
                Wealth: new AmbientValueRange(2f, 5f),
                Population: new AmbientValueRange(0f, 2f)),
            TerrainType.Wastes => new TerrainAmbientProfile(
                Food: new AmbientValueRange(0f, 1f),
                Wealth: new AmbientValueRange(1f, 3f),
                Population: new AmbientValueRange(0f, 2f)),
            TerrainType.Ocean => new TerrainAmbientProfile(
                Food: new AmbientValueRange(0f, 1f),
                Wealth: new AmbientValueRange(0f, 1f),
                Population: new AmbientValueRange(0f, 1f)),
            TerrainType.Reef => new TerrainAmbientProfile(
                Food: new AmbientValueRange(2f, 5f),
                Wealth: new AmbientValueRange(3f, 5f),
                Population: new AmbientValueRange(2f, 4f)),
            TerrainType.Ice => new TerrainAmbientProfile(
                Food: new AmbientValueRange(0f, 2f),
                Wealth: new AmbientValueRange(0f, 2f),
                Population: new AmbientValueRange(0f, 3f)),
            _ => new TerrainAmbientProfile(
                Food: new AmbientValueRange(0f, 0f),
                Wealth: new AmbientValueRange(0f, 0f),
                Population: new AmbientValueRange(0f, 0f)),
        };
    }

    public static void Seed(HexCell cell, Random random)
    {
        ArgumentNullException.ThrowIfNull(cell);
        ArgumentNullException.ThrowIfNull(random);

        var profile = GetProfile(cell.Terrain);
        cell.AmbientFood = profile.Food.Roll(random);
        cell.AmbientWealth = profile.Wealth.Roll(random);
        cell.AmbientPopulation = profile.Population.Roll(random);
    }
}

public readonly record struct TerrainAmbientProfile(
    AmbientValueRange Food,
    AmbientValueRange Wealth,
    AmbientValueRange Population);

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
