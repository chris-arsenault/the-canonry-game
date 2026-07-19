namespace IceRemembers.Seed;

using IceRemembers;

internal sealed class IceRemembersMicrogeographyGenerationContext
{
    public IceRemembersMicrogeographyGenerationContext(
        int rootSeed,
        IceRemembersMicrogeographyGenerationProfile profile,
        IceRemembersMicrogeographyLayerContract contract,
        IceRemembersWorldGenerationEpoch? epoch = null,
        int epochIndex = -1,
        int epochCount = 0)
    {
        RootSeed = rootSeed;
        Profile = profile ?? throw new ArgumentNullException(nameof(profile));
        Contract = contract;
        Epoch = epoch;
        EpochIndex = epochIndex;
        EpochCount = epochCount;
        Seed = rootSeed ^ contract.SeedSalt ^ EpochSeedSalt(epochIndex);
    }

    public int RootSeed { get; }

    public int Seed { get; }

    public IceRemembersMicrogeographyGenerationProfile Profile { get; }

    public IceRemembersMicrogeographyLayerContract Contract { get; }

    public IceRemembersWorldGenerationEpoch? Epoch { get; }

    public int EpochIndex { get; }

    public int EpochCount { get; }

    public int FireStrength => Epoch?.FireStrength ?? 1000;

    public int AuroraStrength => Epoch?.AuroraStrength ?? 1000;

    public int PressureStrength => Epoch?.PressureStrength ?? 1000;

    private static int EpochSeedSalt(int epochIndex) => epochIndex < 0
        ? 0
        : unchecked((epochIndex + 1) * 0x27d4eb2d);
}
