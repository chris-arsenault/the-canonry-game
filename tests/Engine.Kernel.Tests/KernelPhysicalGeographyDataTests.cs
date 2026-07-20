namespace Engine.Kernel.Tests;

using Engine.Constants;
using Engine.Kernel;

public sealed class KernelPhysicalGeographyDataTests
{
    [Fact]
    public void InstallCell_StoresGameplayProjectionWords()
    {
        var data = new KernelPhysicalGeographyData(radiusCapacity: 4);
        data.BeginInstall(2);
        var crossingWord = PhysicalCrossingCodes.WithClass(0, direction: 2, PhysicalCrossingCodes.Cliff);
        crossingWord = PhysicalCrossingCodes.WithClass(crossingWord, direction: 5, PhysicalCrossingCodes.River);
        var fieldWord = PhysicalHexFieldCodes.PackBands(warmth: 2, fire: 0, aurora: 3, pressure: 1) |
            PhysicalHexFieldCodes.SummitFlag |
            PhysicalHexFieldCodes.CraterFlag;

        data.InstallCell(1, -1, baseTerrainId: 7, superregionId: 3, continentId: 2, crossingWord, fieldWord, initialCoverTerrainId: 9);

        Assert.Equal(crossingWord, data.CrossingWord(1, -1));
        Assert.Equal(fieldWord, data.FieldWord(1, -1));
        Assert.Equal(9, data.InitialCoverTerrainId(1, -1));
        Assert.Equal(PhysicalCrossingCodes.Cliff, PhysicalCrossingCodes.ClassAt(data.CrossingWord(1, -1), 2));
        Assert.Equal(PhysicalCrossingCodes.River, PhysicalCrossingCodes.ClassAt(data.CrossingWord(1, -1), 5));
        Assert.Equal(PhysicalCrossingCodes.Open, PhysicalCrossingCodes.ClassAt(data.CrossingWord(1, -1), 0));
        Assert.Equal(2, PhysicalHexFieldCodes.BandAt(data.FieldWord(1, -1), PhysicalHexFieldCodes.WarmthShift));
        Assert.Equal(3, PhysicalHexFieldCodes.BandAt(data.FieldWord(1, -1), PhysicalHexFieldCodes.AuroraShift));
        Assert.NotEqual(0, data.FieldWord(1, -1) & PhysicalHexFieldCodes.SummitFlag);
    }

    [Fact]
    public void Accessors_ReturnZeroOutsideTheInstalledField()
    {
        var data = new KernelPhysicalGeographyData(radiusCapacity: 4);
        data.BeginInstall(2);
        data.InstallCell(0, 0, baseTerrainId: 1, superregionId: 1, continentId: 1, crossingWord: 5, fieldWord: 3, initialCoverTerrainId: 0);

        Assert.Equal(0, data.CrossingWord(2, 2));
        Assert.Equal(0, data.FieldWord(9, 9));
        Assert.Equal(0, data.InitialCoverTerrainId(-9, 0));
    }

    [Fact]
    public void InstallCell_RejectsWordsOutsideTheirMasks()
    {
        var data = new KernelPhysicalGeographyData(radiusCapacity: 4);
        data.BeginInstall(2);

        Assert.Throws<global::System.InvalidOperationException>(() =>
            data.InstallCell(0, 0, 1, 1, 1, crossingWord: 1 << 12, fieldWord: 0, initialCoverTerrainId: 0));
        Assert.Throws<global::System.InvalidOperationException>(() =>
            data.InstallCell(0, 0, 1, 1, 1, crossingWord: 0, fieldWord: 1 << 13, initialCoverTerrainId: 0));
        Assert.Throws<global::System.InvalidOperationException>(() =>
            data.InstallCell(0, 0, 1, 1, 1, crossingWord: 0, fieldWord: 0, initialCoverTerrainId: -1));
    }
}
