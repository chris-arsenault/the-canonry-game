namespace IceRemembers.Seed;

using IceRemembers;

internal sealed class IceRemembersMicrogeographyGenerationContext
{
    public IceRemembersMicrogeographyGenerationContext(
        int rootSeed,
        IceRemembersMicrogeographyGenerationProfile profile,
        IceRemembersMicrogeographyLayerContract contract)
    {
        RootSeed = rootSeed;
        Profile = profile ?? throw new ArgumentNullException(nameof(profile));
        Contract = contract;
        Seed = rootSeed ^ contract.SeedSalt;
    }

    public int RootSeed { get; }

    public int Seed { get; }

    public IceRemembersMicrogeographyGenerationProfile Profile { get; }

    public IceRemembersMicrogeographyLayerContract Contract { get; }
}
