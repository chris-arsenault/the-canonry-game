namespace Engine.Kernel;

internal sealed partial class LowAltitudeVerbProgramState : IActiveKernelProgramStateSnapshot
{
    public void WriteActiveSnapshot(global::System.IO.BinaryWriter writer)
    {
        var rowCount = 0;
        for (var entityId = 1; entityId <= EntityCapacity; entityId++)
        {
            if (HasSnapshotState(entityId))
            {
                rowCount++;
            }
        }

        writer.Write(rowCount);
        for (var entityId = 1; entityId <= EntityCapacity; entityId++)
        {
            if (!HasSnapshotState(entityId))
            {
                continue;
            }

            writer.Write(entityId);
            writer.Write(_activeVerbSlots[entityId]);
            writer.Write(_intentIds[entityId]);
            writer.Write(_targetPlaneCodes[entityId]);
            writer.Write(_targetEntityOccurrenceHandles[entityId]);
            writer.Write(_targetQ[entityId]);
            writer.Write(_targetR[entityId]);
            writer.Write(_targetS[entityId]);
            writer.Write(_startedTicks[entityId]);
            writer.Write(_expiresTicks[entityId]);
            writer.Write(_dwellUntilTicks[entityId]);
            writer.Write(_reachedTicks[entityId]);
            writer.Write(_intentDwellTicks[entityId]);
            writer.Write(_nextStepTicks[entityId]);
            writer.Write(_stepIntervalTicks[entityId]);
            writer.Write(_pathLoosenessPermille[entityId]);
            writer.Write(_initialInfluenceApplied[entityId]);
            var recentCount = _recentTargetCounts[entityId];
            writer.Write(recentCount);
            var recentBase = entityId * _recentTargetRingSize;
            for (var slot = 0; slot < recentCount; slot++)
            {
                writer.Write(_recentTargetKeys[recentBase + slot]);
                writer.Write(_recentTargetTicks[recentBase + slot]);
            }
        }
    }

    public void ReadActiveSnapshot(global::System.IO.BinaryReader reader)
    {
        var rowCount = reader.ReadInt32();
        if ((uint)rowCount > (uint)EntityCapacity)
        {
            throw new global::System.InvalidOperationException();
        }

        for (var row = 0; row < rowCount; row++)
        {
            var entityId = reader.ReadInt32();
            if ((uint)entityId == 0u ||
                entityId > EntityCapacity ||
                HasSnapshotState(entityId))
            {
                throw new global::System.InvalidOperationException();
            }

            _activeVerbSlots[entityId] = reader.ReadInt32();
            _intentIds[entityId] = reader.ReadInt32();
            _targetPlaneCodes[entityId] = reader.ReadInt32();
            _targetEntityOccurrenceHandles[entityId] =
                reader.ReadInt32();
            _targetQ[entityId] = reader.ReadInt32();
            _targetR[entityId] = reader.ReadInt32();
            _targetS[entityId] = reader.ReadInt32();
            _startedTicks[entityId] = reader.ReadInt32();
            _expiresTicks[entityId] = reader.ReadInt32();
            _dwellUntilTicks[entityId] = reader.ReadInt32();
            _reachedTicks[entityId] = reader.ReadInt32();
            _intentDwellTicks[entityId] = reader.ReadInt32();
            _nextStepTicks[entityId] = reader.ReadInt32();
            _stepIntervalTicks[entityId] = reader.ReadInt32();
            _pathLoosenessPermille[entityId] = reader.ReadInt32();
            _initialInfluenceApplied[entityId] = reader.ReadBoolean();
            var recentCount = reader.ReadInt32();
            if ((uint)recentCount > (uint)_recentTargetRingSize)
            {
                throw new global::System.InvalidOperationException();
            }

            _recentTargetCounts[entityId] = recentCount;
            var recentBase = entityId * _recentTargetRingSize;
            for (var slot = 0; slot < recentCount; slot++)
            {
                _recentTargetKeys[recentBase + slot] = reader.ReadInt64();
                _recentTargetTicks[recentBase + slot] = reader.ReadInt32();
                if (_recentTargetKeys[recentBase + slot] == 0)
                {
                    throw new global::System.InvalidOperationException();
                }
            }

            ValidateSnapshotRow(entityId);
        }
    }

    private bool HasSnapshotState(int entityId)
    {
        return _activeVerbSlots[entityId] > 0 ||
            _initialInfluenceApplied[entityId] ||
            _recentTargetCounts[entityId] > 0;
    }

    private void ValidateSnapshotRow(int entityId)
    {
        var verbSlot = _activeVerbSlots[entityId];
        if (verbSlot == 0)
        {
            if (EmptyIntentHasRuntimeState(entityId))
            {
                throw new global::System.InvalidOperationException();
            }

            return;
        }

        if (IntentIdentityIsInvalid(entityId) || IntentTimingIsInvalid(entityId))
        {
            throw new global::System.InvalidOperationException();
        }
    }

    private bool EmptyIntentHasRuntimeState(int entityId)
    {
        return _intentIds[entityId] != 0 ||
            _targetPlaneCodes[entityId] != 0 ||
            _targetEntityOccurrenceHandles[entityId] != 0 ||
            _startedTicks[entityId] != 0 ||
            _expiresTicks[entityId] != 0;
    }

    private bool IntentIdentityIsInvalid(int entityId)
    {
        return _activeVerbSlots[entityId] < 0 ||
            _intentIds[entityId] <= 0;
    }

    private bool IntentTimingIsInvalid(int entityId)
    {
        return
            _intentIds[entityId] <= 0 ||
            _startedTicks[entityId] < 0 ||
            _expiresTicks[entityId] < _startedTicks[entityId] ||
            _intentDwellTicks[entityId] < 0 ||
            _nextStepTicks[entityId] < 0 ||
            _stepIntervalTicks[entityId] <= 0 ||
            _pathLoosenessPermille[entityId] < 0;
    }
}
