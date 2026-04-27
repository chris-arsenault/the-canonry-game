namespace Engine.Kernel;

internal sealed partial class KernelTerrainData
{
    private readonly bool[] _registered;
    private readonly int[] _boundaryGroupIds;

    public KernelTerrainData(int terrainIdCapacity)
    {
        global::System.ArgumentOutOfRangeException.ThrowIfNegativeOrZero(terrainIdCapacity);
        _registered = new bool[terrainIdCapacity + 1];
        _boundaryGroupIds = new int[terrainIdCapacity + 1];
    }

    public void RegisterTerrain(int terrainId, int boundaryGroupId)
    {
        ValidatePositive(terrainId);
        ValidatePositive(boundaryGroupId);
        if ((uint)terrainId >= (uint)_registered.Length)
        {
            throw new global::System.InvalidOperationException();
        }

        _registered[terrainId] = true;
        _boundaryGroupIds[terrainId] = boundaryGroupId;
    }

    public int BoundaryGroupId(int terrainId)
    {
        EnsureTerrain(terrainId);
        return _boundaryGroupIds[terrainId];
    }

    private void EnsureTerrain(int terrainId)
    {
        if ((uint)terrainId >= (uint)_registered.Length || !_registered[terrainId])
        {
            throw new global::System.InvalidOperationException();
        }
    }

    private static void ValidatePositive(int value)
    {
        global::System.ArgumentOutOfRangeException.ThrowIfNegativeOrZero(value);
    }
}
