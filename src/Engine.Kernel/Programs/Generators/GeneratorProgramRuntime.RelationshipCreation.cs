namespace Engine.Kernel;

using Engine.Constants;

internal sealed partial class GeneratorProgramRuntime
{
    private int CreateRelationships(
        Simulation simulation,
        WorldState world,
        int generatorSlot,
        int[] refCodes,
        int[] refEntityIds,
        int refCount,
        int tick)
    {
        _ = world;
        _ = tick;
        var created = 0;
        var first = _relationshipStarts[generatorSlot];
        var end = first + _relationshipCounts[generatorSlot];
        var validCount = CountValidRelationships(
            first,
            end,
            refCodes,
            refEntityIds,
            refCount);
        if (validCount > 0)
        {
            simulation.ReserveStrategicRelationshipBlock(
                global::System.MemoryExtensions.AsSpan(
                    _reservedRelationshipIds,
                    0,
                    validCount));
        }

        for (var relationship = first; relationship < end; relationship++)
        {
            if (!TryResolveRelationshipEndpoints(
                    relationship,
                    refCodes,
                    refEntityIds,
                    refCount,
                    out var sourceId,
                    out var destinationId))
            {
                continue;
            }

            CreateRelationshipOutput(
                simulation,
                relationship,
                created,
                sourceId,
                destinationId);
            created++;
        }

        return created;
    }

    private int CountValidRelationships(
        int first,
        int end,
        int[] refCodes,
        int[] refEntityIds,
        int refCount)
    {
        var validCount = 0;
        for (var relationship = first; relationship < end; relationship++)
        {
            if (TryResolveRelationshipEndpoints(
                    relationship,
                    refCodes,
                    refEntityIds,
                    refCount,
                    out _,
                    out _))
            {
                validCount++;
            }
        }

        return validCount;
    }

    private bool TryResolveRelationshipEndpoints(
        int relationship,
        int[] refCodes,
        int[] refEntityIds,
        int refCount,
        out int sourceId,
        out int destinationId)
    {
        sourceId = ResolveRef(
            refCodes,
            refEntityIds,
            refCount,
            _relationshipSourceRefCodes[relationship]);
        destinationId = ResolveRef(
            refCodes,
            refEntityIds,
            refCount,
            _relationshipDestinationRefCodes[relationship]);
        return sourceId > 0 && destinationId > 0;
    }

    private void CreateRelationshipOutput(
        Simulation simulation,
        int relationship,
        int reservedIndex,
        int sourceId,
        int destinationId)
    {
        var relationshipKindId = _relationshipKindIds[relationship];
        if (_relationshipReplaceExistingOutgoing[relationship] != 0 &&
            !TryPlannedEntity(sourceId, out _))
        {
            KernelEffectExecutor.CullOutgoingRelationshipsOfKind(
                simulation,
                sourceId,
                relationshipKindId);
        }

        simulation.QueueStrategicRelationshipCreation(
            _reservedRelationshipIds[reservedIndex],
            relationshipKindId,
            polarityCode: RelationshipPolarityCodes.Positive,
            sourceId,
            destinationId,
            _relationshipStrengths[relationship]);
    }
}
