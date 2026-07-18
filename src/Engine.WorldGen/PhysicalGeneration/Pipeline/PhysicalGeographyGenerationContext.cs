namespace Engine.WorldGen;

internal sealed class PhysicalGeographyGenerationContext
{
    public PhysicalGeographyGenerationContext(
        int seed,
        PhysicalGeographyGenerationProfile profile,
        PhysicalGeographyLayerContract contract)
    {
        Profile = profile;
        LayerName = contract.StableName;
        unchecked
        {
            Seed = seed ^ contract.SeedSalt;
        }
    }

    public int Seed { get; }

    public string LayerName { get; }

    public PhysicalGeographyGenerationProfile Profile { get; }
}
