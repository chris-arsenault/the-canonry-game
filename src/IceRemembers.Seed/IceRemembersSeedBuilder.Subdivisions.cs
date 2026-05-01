namespace IceRemembers.Seed;

using Engine.Domain.Startup;
using Engine.WorldGen;

internal static partial class IceRemembersSeedBuilder
{
    private const int NearContinentId = 1;
    private const int FarContinentId = 2;
    private const int EmptyContinentId = 3;

    private const int NorthlandsSuperregionId = 11;
    private const int HeartlandsSuperregionId = 12;
    private const int CoastlandsSuperregionId = 13;
    private const int FarwakeSuperregionId = 21;
    private const int BlackwaterSuperregionId = 22;
    private const int WhiteEmptySuperregionId = 31;
    private const int DeepEmptySuperregionId = 32;

    private static DomainMapSubdivision[] CreateSubdivisionDefinitions() =>
    [
        new(NearContinentId, DomainMapSubdivisionLevel.Continent, "near_shelf", "The Near Shelf"),
        new(FarContinentId, DomainMapSubdivisionLevel.Continent, "farwake_sea", "Farwake Sea"),
        new(EmptyContinentId, DomainMapSubdivisionLevel.Continent, "white_empty_continent", "The White Empty"),
        new(NorthlandsSuperregionId, DomainMapSubdivisionLevel.Superregion, "northlands_march", "Northlands March"),
        new(HeartlandsSuperregionId, DomainMapSubdivisionLevel.Superregion, "heartlands_basin", "Heartlands Basin"),
        new(CoastlandsSuperregionId, DomainMapSubdivisionLevel.Superregion, "coastlands_shelf", "Coastlands Shelf"),
        new(FarwakeSuperregionId, DomainMapSubdivisionLevel.Superregion, "farwake_reach", "Farwake Reach"),
        new(BlackwaterSuperregionId, DomainMapSubdivisionLevel.Superregion, "blackwater_reach", "Blackwater Reach"),
        new(WhiteEmptySuperregionId, DomainMapSubdivisionLevel.Superregion, "white_empty_fields", "White Empty"),
        new(DeepEmptySuperregionId, DomainMapSubdivisionLevel.Superregion, "deep_empty", "Deep Empty"),
    ];

    private static MapSubdivisionMap CreateSubdivisions(HexGrid grid, int salt)
    {
        var map = new MapSubdivisionMap();
        foreach (var cell in grid.AllCells)
        {
            var coord = cell.Coord;
            var continentId = ChooseContinent(grid, coord, salt);
            var superregionId = ChooseSuperregion(grid, coord, continentId, salt);
            map.AssignCell(coord, superregionId, continentId);
        }

        return map;
    }

    private static int ChooseContinent(HexGrid grid, HexCoord coord, int salt)
    {
        var radius = grid.Radius;
        var near = SubdivisionDistance(coord, new HexCoord(-radius / 4, 1), salt ^ 101, 4f);
        var far = SubdivisionDistance(coord, new HexCoord((radius * 3) / 4, -(radius * 3) / 5), salt ^ 211, 3f);
        var empty = SubdivisionDistance(coord, new HexCoord(-radius / 5, (radius * 3) / 5), salt ^ 307, 2f);

        if (far < near && far < empty)
        {
            return FarContinentId;
        }

        return empty < near
            ? EmptyContinentId
            : NearContinentId;
    }

    private static int ChooseSuperregion(HexGrid grid, HexCoord coord, int continentId, int salt)
    {
        var radius = grid.Radius;
        return continentId switch
        {
            NearContinentId => ClosestSuperregion(
                coord,
                salt,
                (NorthlandsSuperregionId, new HexCoord(-radius / 2, -2)),
                (HeartlandsSuperregionId, new HexCoord(-radius / 5, 8)),
                (CoastlandsSuperregionId, new HexCoord(radius / 6, -radius / 4))),
            FarContinentId => ClosestSuperregion(
                coord,
                salt,
                (FarwakeSuperregionId, new HexCoord((radius * 3) / 4, -(radius * 3) / 5)),
                (BlackwaterSuperregionId, new HexCoord((radius * 7) / 10, -radius / 5))),
            _ => ClosestSuperregion(
                coord,
                salt,
                (WhiteEmptySuperregionId, new HexCoord(-radius / 3, (radius * 3) / 5)),
                (DeepEmptySuperregionId, new HexCoord(radius / 5, radius / 3))),
        };
    }

    private static int ClosestSuperregion(
        HexCoord coord,
        int salt,
        params (int Id, HexCoord Center)[] options)
    {
        var bestId = options[0].Id;
        var bestScore = float.PositiveInfinity;
        foreach (var option in options)
        {
            var score = SubdivisionDistance(coord, option.Center, salt ^ option.Id, 2.5f);
            if (score < bestScore)
            {
                bestId = option.Id;
                bestScore = score;
            }
        }

        return bestId;
    }

    private static float SubdivisionDistance(HexCoord coord, HexCoord center, int salt, float noiseScale) =>
        HexCoord.Distance(coord, center) + ((CellNoise(coord, salt) - 0.5f) * noiseScale);

    private static float SuperregionAffinity(
        MapSubdivisionMap subdivisionMap,
        int superregionId,
        HexCoord coord,
        float bonus)
    {
        var subdivision = subdivisionMap.GetCell(coord);
        return subdivision.SuperregionId == superregionId ? bonus : 0f;
    }
}
