namespace Engine.Kernel;

internal sealed partial class MapFrontierProgramData
{
    private int _semanticBlueprintRadiusCapacity;
    private int _semanticBlueprintDiameter;
    private int _semanticBlueprintRadius;
    private bool[] _semanticBlueprintCells = [];
    private int[] _semanticBlueprintTerrainIds = [];
    private int[] _semanticBlueprintSuperregionIds = [];
    private int[] _semanticBlueprintContinentIds = [];

    private void InitializeSemanticBlueprint(int radiusCapacity)
    {
        _semanticBlueprintRadiusCapacity = radiusCapacity;
        _semanticBlueprintDiameter = checked((radiusCapacity * 2) + 1);
        var capacity = checked(_semanticBlueprintDiameter * _semanticBlueprintDiameter);
        _semanticBlueprintCells = new bool[capacity];
        _semanticBlueprintTerrainIds = new int[capacity];
        _semanticBlueprintSuperregionIds = new int[capacity];
        _semanticBlueprintContinentIds = new int[capacity];
    }

    internal void BeginSemanticBlueprintInstall(int radius)
    {
        NumericGuard.ThrowIfNegative(radius);
        if (radius > _semanticBlueprintRadiusCapacity || _semanticBlueprintRadius != 0)
        {
            throw new global::System.InvalidOperationException();
        }

        _semanticBlueprintRadius = radius;
    }

    internal void InstallSemanticBlueprintCell(
        int q,
        int r,
        int terrainId,
        int superregionId,
        int continentId)
    {
        if (_semanticBlueprintRadius <= 0 ||
            HexDistanceFromOrigin(q, r) > _semanticBlueprintRadius ||
            terrainId <= 0 ||
            superregionId <= 0 ||
            continentId <= 0)
        {
            throw new global::System.InvalidOperationException();
        }

        var index = SemanticBlueprintIndex(q, r);
        if (_semanticBlueprintCells[index])
        {
            throw new global::System.InvalidOperationException();
        }

        _semanticBlueprintCells[index] = true;
        _semanticBlueprintTerrainIds[index] = terrainId;
        _semanticBlueprintSuperregionIds[index] = superregionId;
        _semanticBlueprintContinentIds[index] = continentId;
    }

    internal bool TryGetSemanticBlueprintCell(
        int q,
        int r,
        out int terrainId,
        out int superregionId,
        out int continentId)
    {
        if (q < -_semanticBlueprintRadiusCapacity || q > _semanticBlueprintRadiusCapacity ||
            r < -_semanticBlueprintRadiusCapacity || r > _semanticBlueprintRadiusCapacity)
        {
            terrainId = 0;
            superregionId = 0;
            continentId = 0;
            return false;
        }

        var index = SemanticBlueprintIndex(q, r);
        if (!_semanticBlueprintCells[index])
        {
            terrainId = 0;
            superregionId = 0;
            continentId = 0;
            return false;
        }

        terrainId = _semanticBlueprintTerrainIds[index];
        superregionId = _semanticBlueprintSuperregionIds[index];
        continentId = _semanticBlueprintContinentIds[index];
        return true;
    }

    private int SemanticBlueprintIndex(int q, int r) =>
        checked(((q + _semanticBlueprintRadiusCapacity) * _semanticBlueprintDiameter) + r + _semanticBlueprintRadiusCapacity);

    private static int HexDistanceFromOrigin(int q, int r) =>
        (global::System.Math.Abs(q) + global::System.Math.Abs(r) + global::System.Math.Abs(q + r)) / 2;
}
