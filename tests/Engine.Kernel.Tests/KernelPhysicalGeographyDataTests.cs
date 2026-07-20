namespace Engine.Kernel.Tests;

using Engine.Constants;
using Engine.Kernel;

public sealed class KernelPhysicalGeographyDataTests
{
    [Fact]
    public void InstallCell_StoresGameplayProjectionWordsAndFacts()
    {
        var data = new KernelPhysicalGeographyData(radiusCapacity: 4);
        data.BeginInstall(2);
        var crossingWord = PhysicalCrossingCodes.WithClass(0, direction: 2, PhysicalCrossingCodes.Cliff);
        crossingWord = PhysicalCrossingCodes.WithClass(crossingWord, direction: 5, PhysicalCrossingCodes.River);
        var fieldWord = PhysicalHexFieldCodes.SummitFlag | PhysicalHexFieldCodes.CraterFlag;
        var facts = new byte[PhysicalHexFactCodes.Count];
        facts[PhysicalHexFactCodes.Temperature] = 120;
        facts[PhysicalHexFactCodes.AuroraMean] = 200;
        facts[PhysicalHexFactCodes.RiverCoverage] = 64;

        data.InstallCell(1, -1, baseTerrainId: 7, superregionId: 3, continentId: 2, crossingWord, fieldWord, initialCoverTerrainId: 9, facts);

        Assert.Equal(crossingWord, data.CrossingWord(1, -1));
        Assert.Equal(fieldWord, data.FieldWord(1, -1));
        Assert.Equal(9, data.InitialCoverTerrainId(1, -1));
        Assert.Equal(PhysicalCrossingCodes.Cliff, PhysicalCrossingCodes.ClassAt(data.CrossingWord(1, -1), 2));
        Assert.Equal(PhysicalCrossingCodes.River, PhysicalCrossingCodes.ClassAt(data.CrossingWord(1, -1), 5));
        Assert.Equal(PhysicalCrossingCodes.Open, PhysicalCrossingCodes.ClassAt(data.CrossingWord(1, -1), 0));
        Assert.Equal(120, data.Fact(1, -1, PhysicalHexFactCodes.Temperature));
        Assert.Equal(200, data.Fact(1, -1, PhysicalHexFactCodes.AuroraMean));
        Assert.Equal(64, data.Fact(1, -1, PhysicalHexFactCodes.RiverCoverage));
        Assert.Equal(0, data.Fact(1, -1, PhysicalHexFactCodes.PressureMax));
        Assert.NotEqual(0, data.FieldWord(1, -1) & PhysicalHexFieldCodes.SummitFlag);
    }

    [Fact]
    public void Accessors_ReturnZeroOutsideTheInstalledField()
    {
        var data = new KernelPhysicalGeographyData(radiusCapacity: 4);
        data.BeginInstall(2);
        var facts = new byte[PhysicalHexFactCodes.Count];
        data.InstallCell(0, 0, baseTerrainId: 1, superregionId: 1, continentId: 1, crossingWord: 5, fieldWord: 3, initialCoverTerrainId: 0, facts);

        Assert.Equal(0, data.CrossingWord(2, 2));
        Assert.Equal(0, data.FieldWord(9, 9));
        Assert.Equal(0, data.InitialCoverTerrainId(-9, 0));
        Assert.Equal(0, data.Fact(2, 2, PhysicalHexFactCodes.Temperature));
    }

    [Fact]
    public void InstallCell_RejectsWordsAndFactsOutsideTheirContracts()
    {
        var data = new KernelPhysicalGeographyData(radiusCapacity: 4);
        data.BeginInstall(2);
        var facts = new byte[PhysicalHexFactCodes.Count];

        Assert.Throws<global::System.InvalidOperationException>(() =>
            data.InstallCell(0, 0, 1, 1, 1, crossingWord: 1 << 12, fieldWord: 0, initialCoverTerrainId: 0, facts));
        Assert.Throws<global::System.InvalidOperationException>(() =>
            data.InstallCell(0, 0, 1, 1, 1, crossingWord: 0, fieldWord: 1 << 5, initialCoverTerrainId: 0, facts));
        Assert.Throws<global::System.InvalidOperationException>(() =>
            data.InstallCell(0, 0, 1, 1, 1, crossingWord: 0, fieldWord: 0, initialCoverTerrainId: -1, facts));
        Assert.Throws<global::System.InvalidOperationException>(() =>
            data.InstallCell(0, 0, 1, 1, 1, crossingWord: 0, fieldWord: 0, initialCoverTerrainId: 0, new byte[3]));
    }

    [Fact]
    public void FactQuantizers_MapTheirScalesOntoBytes()
    {
        Assert.Equal(0, PhysicalHexFactCodes.FromIntensity(0));
        Assert.Equal(255, PhysicalHexFactCodes.FromIntensity(1000));
        Assert.Equal(255, PhysicalHexFactCodes.FromIntensity(5000));
        Assert.Equal(0, PhysicalHexFactCodes.FromCoverage(0));
        Assert.Equal(255, PhysicalHexFactCodes.FromCoverage(24));
        Assert.Equal(0, PhysicalHexFactCodes.FromElevation(-1000));
        Assert.Equal(32, PhysicalHexFactCodes.FromElevation(0));
        Assert.Equal(255, PhysicalHexFactCodes.FromReliefUnits(4000));
    }
}
