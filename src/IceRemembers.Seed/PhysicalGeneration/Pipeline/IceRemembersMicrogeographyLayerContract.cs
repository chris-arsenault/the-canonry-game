namespace IceRemembers.Seed;

using Engine.WorldGen;

internal readonly record struct IceRemembersMicrogeographyLayerContract(
    string StableName,
    int SeedSalt,
    IceRemembersMicrogeographyFacts Requires,
    IceRemembersMicrogeographyFacts Produces,
    IceRemembersMicrogeographyFacts Invalidates = IceRemembersMicrogeographyFacts.None);

internal interface IIceRemembersMicrogeographyLayer :
    IMapGenerationLayer<IceRemembersMicrogeographyMap, IceRemembersMicrogeographyGenerationContext>
{
    IceRemembersMicrogeographyLayerContract Contract { get; }
}
