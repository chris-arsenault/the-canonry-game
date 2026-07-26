namespace Engine.Kernel;

internal sealed partial class MinorActorProgramState : IActiveKernelProgramStateSnapshot
{
    public void WriteActiveSnapshot(global::System.IO.BinaryWriter writer)
    {
        writer.Write(HighWatermark);
        writer.Write(TotalCreated);
        writer.Write(TotalRetired);
        for (var actorId = 1; actorId <= HighWatermark; actorId++)
        {
            writer.Write(_generations[actorId]);
        }

        writer.Write(ActiveCount);
        for (var index = 0; index < ActiveCount; index++)
        {
            var actorId = _activeActorIds[index];
            writer.Write(actorId);
            writer.Write(_cultureIds[actorId]);
            writer.Write(_sourceEntityIds[actorId]);
            writer.Write(_sourceEntityOccurrenceHandles[actorId]);
            writer.Write(_sourceRuleIds[actorId]);
            writer.Write(_bindingRelationshipIds[actorId]);
            writer.Write(_bindingRelationshipOccurrenceHandles[actorId]);
            writer.Write(_roleCodes[actorId]);
            writer.Write(_planeCodes[actorId]);
            writer.Write(_q[actorId]);
            writer.Write(_r[actorId]);
            writer.Write(_previousQ[actorId]);
            writer.Write(_previousR[actorId]);
            writer.Write(_targetQ[actorId]);
            writer.Write(_targetR[actorId]);
            writer.Write(_targetingCodes[actorId]);
            writer.Write(_postureCodes[actorId]);
            writer.Write(_createdAtTicks[actorId]);
            writer.Write(_nextReplanTicks[actorId]);
            writer.Write(_commitmentUntilTicks[actorId]);
            writer.Write(_strengths[actorId].Units);
            writer.Write(_workCharges[actorId].Units);
            writer.Write(_cellIds[actorId]);
        }
    }

    public void ReadActiveSnapshot(global::System.IO.BinaryReader reader)
    {
        HighWatermark = reader.ReadInt32();
        TotalCreated = reader.ReadInt32();
        TotalRetired = reader.ReadInt32();
        if ((uint)HighWatermark > (uint)ActorCapacity ||
            TotalCreated < 0 ||
            TotalRetired < 0)
        {
            throw new global::System.InvalidOperationException();
        }

        for (var actorId = 1; actorId <= HighWatermark; actorId++)
        {
            _generations[actorId] = reader.ReadInt32();
            if (_generations[actorId] < 0)
            {
                throw new global::System.InvalidOperationException();
            }
        }

        var activeCount = reader.ReadInt32();
        if ((uint)activeCount > (uint)HighWatermark)
        {
            throw new global::System.InvalidOperationException();
        }

        for (var index = 0; index < activeCount; index++)
        {
            ReadSnapshotActor(reader);
        }

        RebuildFreeActorSlots();
    }

    private void ReadSnapshotActor(global::System.IO.BinaryReader reader)
    {
        var actorId = reader.ReadInt32();
        if ((uint)actorId == 0u || actorId > HighWatermark || _active[actorId])
        {
            throw new global::System.InvalidOperationException();
        }

        _cultureIds[actorId] = reader.ReadInt32();
        _sourceEntityIds[actorId] = reader.ReadInt32();
        _sourceEntityOccurrenceHandles[actorId] = reader.ReadInt32();
        _sourceRuleIds[actorId] = reader.ReadInt32();
        _bindingRelationshipIds[actorId] = reader.ReadInt32();
        _bindingRelationshipOccurrenceHandles[actorId] = reader.ReadInt32();
        _roleCodes[actorId] = reader.ReadInt32();
        _planeCodes[actorId] = reader.ReadInt32();
        _q[actorId] = reader.ReadInt32();
        _r[actorId] = reader.ReadInt32();
        _previousQ[actorId] = reader.ReadInt32();
        _previousR[actorId] = reader.ReadInt32();
        _targetQ[actorId] = reader.ReadInt32();
        _targetR[actorId] = reader.ReadInt32();
        _targetingCodes[actorId] = reader.ReadInt32();
        _postureCodes[actorId] = reader.ReadInt32();
        _createdAtTicks[actorId] = reader.ReadInt32();
        _nextReplanTicks[actorId] = reader.ReadInt32();
        _commitmentUntilTicks[actorId] = reader.ReadInt32();
        _strengths[actorId] = FixedScalar.FromUnits(reader.ReadInt64());
        _workCharges[actorId] = FixedScalar.FromUnits(reader.ReadInt64());
        var cellId = reader.ReadInt32();
        ValidateSnapshotActor(actorId, cellId);
        _active[actorId] = true;
        LinkToCell(actorId, cellId);
        AddActiveActor(actorId);

        var handle = OccurrenceHandle(actorId);
        if (_roleCodes[actorId] == Engine.Constants.MinorActorRoleCodes.CampaignColumn)
        {
            if (_actorByRelationship[_bindingRelationshipIds[actorId]] != 0)
            {
                throw new global::System.InvalidOperationException();
            }

            _actorByRelationship[_bindingRelationshipIds[actorId]] = handle;
        }
        else
        {
            var sourceIndex = SourceRuleIndex(_sourceEntityIds[actorId], _sourceRuleIds[actorId]);
            if (_actorBySourceRule[sourceIndex] != 0)
            {
                throw new global::System.InvalidOperationException();
            }

            _actorBySourceRule[sourceIndex] = handle;
        }
    }

    private void RebuildFreeActorSlots()
    {
        _freeActorCount = 0;
        for (var actorId = HighWatermark; actorId >= 1; actorId--)
        {
            if (!_active[actorId])
            {
                _freeActorIds[_freeActorCount++] = actorId;
            }
        }
    }

    private void ValidateSnapshotActor(int actorId, int cellId)
    {
        if (SnapshotActorIdentityIsInvalid(actorId) ||
            SnapshotActorStateIsInvalid(actorId, cellId) ||
            SnapshotActorBindingIsInvalid(actorId))
        {
            throw new global::System.InvalidOperationException();
        }
    }

    private bool SnapshotActorIdentityIsInvalid(int actorId) =>
        _cultureIds[actorId] <= 0 ||
        _sourceEntityIds[actorId] <= 0 ||
        _sourceEntityOccurrenceHandles[actorId] <= 0 ||
        _sourceRuleIds[actorId] <= 0 ||
        _roleCodes[actorId] is < Engine.Constants.MinorActorRoleCodes.CulturalFront
            or > Engine.Constants.MinorActorRoleCodes.CampaignColumn ||
        _planeCodes[actorId] is not (
            Engine.Constants.PlaneCodes.Physical or
            Engine.Constants.PlaneCodes.Semantic);

    private bool SnapshotActorStateIsInvalid(int actorId, int cellId) =>
        _postureCodes[actorId] is < Engine.Constants.MinorActorPostureCodes.Unassigned
            or > Engine.Constants.MinorActorPostureCodes.Disrupted ||
        _createdAtTicks[actorId] < 0 ||
        _nextReplanTicks[actorId] < 0 ||
        _commitmentUntilTicks[actorId] < 0 ||
        _strengths[actorId] <= 0 ||
        _workCharges[actorId] < 0 ||
        (uint)cellId == 0u ||
        cellId >= _firstActorByCell.Length;

    private bool SnapshotActorBindingIsInvalid(int actorId) =>
        _roleCodes[actorId] == Engine.Constants.MinorActorRoleCodes.CampaignColumn &&
        (_bindingRelationshipIds[actorId] <= 0 ||
         _bindingRelationshipOccurrenceHandles[actorId] <= 0);
}
