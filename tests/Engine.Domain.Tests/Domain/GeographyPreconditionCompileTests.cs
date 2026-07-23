namespace Engine.Domain.Tests.Domain;

using Engine.Behaviors;
using Engine.Constants;
using Engine.Domain.Compilation;
using Engine.Domain.Compilation.KernelPrograms;

using IceRemembers;

// The terrain-reading preconditions compile to banded fact predicates in the
// fact's own quantization, and terrain groups resolve through the registry
// that also numbers the compiled terrain table - one id space, no drift.
public sealed class GeographyPreconditionCompileTests
{
    [Fact]
    public void HexFactThresholds_QuantizeWithTheFactsOwnScale()
    {
        var config = IceRemembersDefinition.Create();
        var gambit = config.Gambits.First(g => g.Id == "penguin_raises_the_waymark");
        gambit.Precondition = new AndPrecondition(
        [
            gambit.Precondition,
            new SelfHexFactThreshold(PhysicalHexFact.Geothermal, min: 400),
            new TargetHexFactThreshold(PhysicalHexFact.Elevation, min: 100, max: 800),
        ]);

        var compiled = DomainCompiler.Compile(config);
        var compiledId = compiled.Ids.GambitIdOf("penguin_raises_the_waymark").Value;
        var rule = Assert.Single(compiled.Programs.Gambits.Gambits, g => g.GambitId == compiledId);

        Assert.Contains(rule.Preconditions, op =>
            op.Code == BehaviorProgramPrecondition.SelfHexFactRange &&
            op.A == PhysicalHexFactCodes.Geothermal &&
            op.B == PhysicalHexFactCodes.FromIntensity(400) &&
            op.C == 255);
        Assert.Contains(rule.Preconditions, op =>
            op.Code == BehaviorProgramPrecondition.TargetHexFactRange &&
            op.A == PhysicalHexFactCodes.Elevation &&
            op.B == PhysicalHexFactCodes.FromElevation(100) &&
            op.C == PhysicalHexFactCodes.FromElevation(800));
    }

    [Fact]
    public void SelfOnTerrainGroup_ResolvesTheGroupIdTheTerrainTableUses()
    {
        var config = IceRemembersDefinition.Create();
        var gambit = config.Gambits.First(g => g.Id == "penguin_raises_the_waymark");
        gambit.Precondition = new AndPrecondition(
        [
            gambit.Precondition,
            new SelfOnTerrainGroup("warm_land"),
        ]);

        var compiled = DomainCompiler.Compile(config);
        var compiledId = compiled.Ids.GambitIdOf("penguin_raises_the_waymark").Value;
        var rule = Assert.Single(compiled.Programs.Gambits.Gambits, g => g.GambitId == compiledId);

        var groupId = compiled.Ids.TerrainBoundaryGroupIdOf("warm_land").Value;
        Assert.True(groupId > 0);
        Assert.Contains(rule.Preconditions, op =>
            op.Code == BehaviorProgramPrecondition.SelfOnTerrainGroup &&
            op.A == groupId);

        // The compiled terrain table numbers groups from the same registry:
        // hot springs land in warm_land.
        var hotSpringsId = compiled.Ids.TerrainIdOf(IceRemembersTerrain.HotSprings.ToDomainName()).Value;
        var hotSprings = Assert.Single(
            compiled.KernelData.Terrain.Terrains,
            terrain => terrain.TerrainId == hotSpringsId);
        Assert.Equal(groupId, hotSprings.BoundaryGroupId);
    }

    [Fact]
    public void HexFactThreshold_WithoutAnyBoundIsRejected()
    {
        var config = IceRemembersDefinition.Create();
        var gambit = config.Gambits.First(g => g.Id == "penguin_raises_the_waymark");
        gambit.Precondition = new AndPrecondition(
        [
            gambit.Precondition,
            new SelfHexFactThreshold(PhysicalHexFact.Temperature),
        ]);

        Assert.Throws<InvalidOperationException>(() => DomainCompiler.Compile(config));
    }
}
