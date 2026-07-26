namespace Engine.Kernel;

using Engine.Constants;

internal sealed partial class MinorActorProgramState : IActiveKernelProgramStateSnapshot
{
    public void WriteActiveSnapshot(global::System.IO.BinaryWriter writer)
    {
        writer.Write(HighWatermark);
        writer.Write(TotalSpawned);
        writer.Write(TotalExpired);
        writer.Write(ActiveCount);
        for (var index = 0; index < ActiveCount; index++)
        {
            var actorId = _activeActorIds[index];
            writer.Write(actorId);
            writer.Write(_actorKindCodes[actorId]);
            writer.Write(_cultureIds[actorId]);
            writer.Write(_sourceEntityIds[actorId]);
            writer.Write(_sourceRuleIds[actorId]);
            writer.Write(_planeCodes[actorId]);
            writer.Write(_q[actorId]);
            writer.Write(_r[actorId]);
            writer.Write(_expiresAtTicks[actorId]);
            writer.Write(_createdAtTicks[actorId]);
            writer.Write(_nextActionTicks[actorId]);
            writer.Write(_actionCadenceTicks[actorId]);
            writer.Write(_purposeCodes[actorId]);
            writer.Write(_workDeltas[actorId].Units);
        }

        var emissionCount = 0;
        for (var index = _sourceRuleStride; index < _nextEmissionTicks.Length; index++)
        {
            if (_nextEmissionTicks[index] > 0)
            {
                emissionCount++;
            }
        }

        writer.Write(emissionCount);
        for (var index = _sourceRuleStride; index < _nextEmissionTicks.Length; index++)
        {
            if (_nextEmissionTicks[index] <= 0)
            {
                continue;
            }

            writer.Write(index);
            writer.Write(_nextEmissionTicks[index]);
        }
    }

    public void ReadActiveSnapshot(global::System.IO.BinaryReader reader)
    {
        HighWatermark = reader.ReadInt32();
        TotalSpawned = reader.ReadInt32();
        TotalExpired = reader.ReadInt32();
        var activeCount = reader.ReadInt32();
        if ((uint)HighWatermark > (uint)ActorCapacity ||
            TotalSpawned < 0 ||
            TotalExpired < 0 ||
            (uint)activeCount > (uint)HighWatermark)
        {
            throw new global::System.InvalidOperationException();
        }

        for (var index = 0; index < activeCount; index++)
        {
            ReadSnapshotActor(reader);
        }

        RebuildFreeActorSlots();
        ReadEmissionState(reader);
    }

    private void ReadEmissionState(global::System.IO.BinaryReader reader)
    {
        var emissionCount = reader.ReadInt32();
        if ((uint)emissionCount > (uint)_nextEmissionTicks.Length)
        {
            throw new global::System.InvalidOperationException();
        }

        for (var item = 0; item < emissionCount; item++)
        {
            var index = reader.ReadInt32();
            var tick = reader.ReadInt32();
            if (index < _sourceRuleStride ||
                index >= _nextEmissionTicks.Length ||
                index % _sourceRuleStride == 0 ||
                tick <= 0 ||
                _nextEmissionTicks[index] != 0)
            {
                throw new global::System.InvalidOperationException();
            }

            _nextEmissionTicks[index] = tick;
        }
    }

    private void ReadSnapshotActor(global::System.IO.BinaryReader reader)
    {
        var actorId = reader.ReadInt32();
        if ((uint)actorId == 0u || actorId > HighWatermark || _active[actorId])
        {
            throw new global::System.InvalidOperationException();
        }

        _actorKindCodes[actorId] = reader.ReadInt32();
        _cultureIds[actorId] = reader.ReadInt32();
        _sourceEntityIds[actorId] = reader.ReadInt32();
        _sourceRuleIds[actorId] = reader.ReadInt32();
        _planeCodes[actorId] = reader.ReadInt32();
        _q[actorId] = reader.ReadInt32();
        _r[actorId] = reader.ReadInt32();
        _expiresAtTicks[actorId] = reader.ReadInt32();
        _createdAtTicks[actorId] = reader.ReadInt32();
        _nextActionTicks[actorId] = reader.ReadInt32();
        _actionCadenceTicks[actorId] = reader.ReadInt32();
        _purposeCodes[actorId] = reader.ReadInt32();
        _workDeltas[actorId] = FixedScalar.FromUnits(reader.ReadInt64());
        ValidateSnapshotActor(actorId);
        _active[actorId] = true;
        AddActiveActor(actorId);
        _sourceActiveCounts[SourceRuleIndex(
            _sourceEntityIds[actorId],
            _sourceRuleIds[actorId])]++;
        Schedule(actorId);
    }

    private void RebuildFreeActorSlots()
    {
        _freeActorCount = 0;
        for (var actorId = 1; actorId <= HighWatermark; actorId++)
        {
            if (!_active[actorId])
            {
                _freeActorIds[_freeActorCount++] = actorId;
            }
        }
    }

    private void ValidateSnapshotActor(int actorId)
    {
        if (SnapshotActorIdentityIsInvalid(actorId) ||
            SnapshotActorTimingIsInvalid(actorId))
        {
            throw new global::System.InvalidOperationException();
        }
    }

    private bool SnapshotActorIdentityIsInvalid(int actorId)
    {
        return _actorKindCodes[actorId] <= 0 ||
            _cultureIds[actorId] <= 0 ||
            _sourceEntityIds[actorId] <= 0 ||
            _sourceRuleIds[actorId] <= 0 ||
            _purposeCodes[actorId] <= 0;
    }

    private bool SnapshotActorTimingIsInvalid(int actorId)
    {
        return _expiresAtTicks[actorId] < _createdAtTicks[actorId] ||
            _createdAtTicks[actorId] < 0 ||
            _nextActionTicks[actorId] < 0 ||
            _actionCadenceTicks[actorId] <= 0 ||
            _workDeltas[actorId] < 0;
    }
}
