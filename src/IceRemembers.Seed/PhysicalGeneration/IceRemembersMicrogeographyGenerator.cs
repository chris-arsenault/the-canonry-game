namespace IceRemembers.Seed;

using Engine.WorldGen;

using IceRemembers;

internal static class IceRemembersMicrogeographyGenerator
{
    public static IceRemembersMicrogeographyMap Generate(
        PhysicalGeographyField physical,
        int seed,
        IceRemembersMicrogeographyGenerationProfile profile) =>
        IceRemembersMicrogeographyPipeline.CreateDefault(profile).Generate(physical, seed);
}
