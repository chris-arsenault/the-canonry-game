namespace Engine.Kernel;

internal sealed partial class MovementProgramState : IActiveKernelProgramStateSnapshot
{
    public void WriteActiveSnapshot(global::System.IO.BinaryWriter writer)
    {
        writer.Write(_pathingTopologyEpoch);
        writer.Write(_roadMarkEpoch);
        writer.Write(_roadCellCount);
        for (var index = 0; index < _roadCellCount; index++)
        {
            var cellId = _roadCellIds[index];
            writer.Write(cellId);
            writer.Write(_roadQuality[cellId]);
            writer.Write(_roadQualityFloors[cellId]);
            writer.Write(_roadTravelCounts[cellId]);
            writer.Write(_lastRoadTicks[cellId]);
        }

        writer.Write(_travelStructureCellCount);
        for (var index = 0; index < _travelStructureCellCount; index++)
        {
            var cellId = _travelStructureCellIds[index];
            writer.Write(cellId);
            writer.Write(_travelStructureIds[cellId]);
        }
    }

    public void ReadActiveSnapshot(global::System.IO.BinaryReader reader)
    {
        _pathingTopologyEpoch = reader.ReadInt32();
        _roadMarkEpoch = reader.ReadInt32();
        if (_pathingTopologyEpoch <= 0 || _roadMarkEpoch <= 0)
        {
            throw new global::System.InvalidOperationException();
        }

        ReadRoadCells(reader);
        ReadTravelStructureCells(reader);
        _localRoadMarks.Clear();
        _regionalRoadMarks.Clear();
        _localRoadMarkScratch.Clear();
        _regionalRoadMarkScratch.Clear();
        _localRoadMarksDirty = true;
        _regionalRoadMarksDirty = true;
        ClearRouteCache();
    }

    private void ReadRoadCells(global::System.IO.BinaryReader reader)
    {
        var roadCellCount = reader.ReadInt32();
        if ((uint)roadCellCount > (uint)_roadCellIds.Length)
        {
            throw new global::System.InvalidOperationException();
        }

        for (var index = 0; index < roadCellCount; index++)
        {
            var cellId = ReadCellId(reader);
            if (_roadCellPositions[cellId] != 0)
            {
                throw new global::System.InvalidOperationException();
            }

            _roadQuality[cellId] = reader.ReadInt32();
            _roadQualityFloors[cellId] = reader.ReadInt32();
            _roadTravelCounts[cellId] = reader.ReadInt32();
            _lastRoadTicks[cellId] = reader.ReadInt32();
            if (_roadQuality[cellId] < 0 ||
                _roadQualityFloors[cellId] < 0 ||
                _roadTravelCounts[cellId] < 0 ||
                _lastRoadTicks[cellId] < 0)
            {
                throw new global::System.InvalidOperationException();
            }

            AddActiveCell(_roadCellIds, _roadCellPositions, ref _roadCellCount, cellId);
        }
    }

    private void ReadTravelStructureCells(global::System.IO.BinaryReader reader)
    {
        var travelStructureCellCount = reader.ReadInt32();
        if ((uint)travelStructureCellCount > (uint)_travelStructureCellIds.Length)
        {
            throw new global::System.InvalidOperationException();
        }

        for (var index = 0; index < travelStructureCellCount; index++)
        {
            var cellId = ReadCellId(reader);
            var travelStructureId = reader.ReadInt32();
            if (travelStructureId <= 0 || _travelStructureCellPositions[cellId] != 0)
            {
                throw new global::System.InvalidOperationException();
            }

            _travelStructureIds[cellId] = travelStructureId;
            AddActiveCell(
                _travelStructureCellIds,
                _travelStructureCellPositions,
                ref _travelStructureCellCount,
                cellId);
        }
    }

    private int ReadCellId(global::System.IO.BinaryReader reader)
    {
        var cellId = reader.ReadInt32();
        if ((uint)cellId == 0u || cellId >= _roadQuality.Length)
        {
            throw new global::System.InvalidOperationException();
        }

        return cellId;
    }
}
