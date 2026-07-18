namespace Engine.WorldGen;

public static class PhysicalGeographyGenerator
{
    public static PhysicalGeographyField Generate(
        int radius,
        int seed,
        PhysicalGeographyGenerationProfile profile) =>
        PhysicalGeographyPipeline.CreateDefault(profile).Generate(radius, seed);
}
