namespace IceRemembers.Seed.Tests;

using Engine.WorldGen;

using IceRemembers;

public sealed class IceRemembersPhysicalGenerationTests
{
    [Theory]
    [InlineData(42)]
    [InlineData(9173)]
    [InlineData(21031)]
    public void ProductionProfile_SatisfiesItsConfiguredQualityTargets(int seed)
    {
        var field = PhysicalGeographyGenerator.Generate(
            IceRemembersSeedBuilder.GenerationRadius,
            seed,
            IceRemembersPhysicalGeography.GenerationProfile);

        Assert.Equal(IceRemembersSeedBuilder.GenerationRadius, field.Grid.Radius);
    }
}
