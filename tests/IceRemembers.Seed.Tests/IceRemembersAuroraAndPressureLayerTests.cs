namespace IceRemembers.Seed.Tests;

using Engine.Constants;

public sealed class IceRemembersAuroraAndPressureLayerTests
{
    [Fact]
    public void AuroraField_AccumulatesChargeInColdIceAndRejectsInvalidIntensity()
    {
        var result = IceRemembersMicrogeographyLayerTestHarness.GenerateThrough<AuroraFieldLayer>();
        var charged = Enumerable.Range(0, result.Map.Physical.Grid.TriangleCount)
            .Where(index => result.Map.AuroraIntensity[index] > 0)
            .ToArray();

        Assert.NotEmpty(charged);
        var strongest = charged.MaxBy(index => result.Map.AuroraIntensity[index]);
        Assert.True(result.Map.AuroraIntensity[strongest] > 0);

        result.Map.AuroraIntensity[strongest] = 1001;
        Assert.Throws<InvalidOperationException>(() => result.Layer.Validate(result.Map, result.Context));
    }

    [Fact]
    public void PressureField_UsesOneOceanLocusAndHistoricSourcesAndRejectsLandLocus()
    {
        var result = IceRemembersMicrogeographyLayerTestHarness.GenerateThrough<PressureFieldLayer>();
        var locus = result.Map.DeadGodTriangleIndex;

        Assert.True(locus >= 0);
        Assert.Equal(PhysicalWaterKindCodes.Ocean, result.Map.Physical.WaterKindCodes[locus]);
        Assert.True(result.Map.PressureIntensity[locus] > 0);
        Assert.Contains(result.Map.PressureWake, wake => wake);
        Assert.Contains(result.Map.RogueImpact, impact => impact);

        result.Map.Physical.WaterKindCodes[locus] = PhysicalWaterKindCodes.None;
        Assert.Throws<InvalidOperationException>(() => result.Layer.Validate(result.Map, result.Context));
    }
}
