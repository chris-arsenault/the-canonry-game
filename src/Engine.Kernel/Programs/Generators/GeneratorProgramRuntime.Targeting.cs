namespace Engine.Kernel;

internal sealed partial class GeneratorProgramRuntime
{
    private bool TargetPasses(
        WorldState world,
        int generatorSlot,
        int targetId,
        int[] refCodes,
        int[] refEntityIds,
        out int refCount)
    {
        refCount = 0;
        if (!KernelQuery.EntityIsActiveKind(world, targetId, _targetKindIds[generatorSlot]) ||
            !SubtypeMatches(world.EntitySubtypeId(targetId), _targetSubtypeStarts[generatorSlot], _targetSubtypeCounts[generatorSlot]) ||
            !TargetRelationshipRequirementsPass(world, generatorSlot, targetId) ||
            ExceedsTargetSaturation(world, generatorSlot, targetId) ||
            !TriggersPass(world, generatorSlot, targetId))
        {
            return false;
        }

        var targetOriginQ = world.EntityPhysicalQ(targetId);
        var targetOriginR = world.EntityPhysicalR(targetId);
        if (_targetAllowWilderness[generatorSlot] != 0 &&
            !FindFreePhysicalHex(world, targetOriginQ, targetOriginR, maxRadius: 5, out targetOriginQ, out targetOriginR))
        {
            return false;
        }

        SetRef(refCodes, refEntityIds, ref refCount, GeneratorProgramConstants.TargetReferenceCode, targetId);
        return ResolveSecondaryBindings(
            world,
            generatorSlot,
            targetId,
            targetOriginQ,
            targetOriginR,
            refCodes,
            refEntityIds,
            ref refCount,
            selectRandom: false,
            simulation: null);
    }

    private bool TargetRelationshipRequirementsPass(WorldState world, int generatorSlot, int targetId)
    {
        return RequiredDirectedRelationshipsPass(
                world,
                targetId,
                _targetRequiredOutgoingStarts[generatorSlot],
                _targetRequiredOutgoingCounts[generatorSlot],
                outgoing: true) &&
            RequiredDirectedRelationshipsPass(
                world,
                targetId,
                _targetRequiredIncomingStarts[generatorSlot],
                _targetRequiredIncomingCounts[generatorSlot],
                outgoing: false) &&
            RequiredAnyDirectionRelationshipsPass(
                world,
                targetId,
                _targetRequiredAnyStarts[generatorSlot],
                _targetRequiredAnyCounts[generatorSlot]);
    }

    private bool RequiredDirectedRelationshipsPass(
        WorldState world,
        int targetId,
        int start,
        int count,
        bool outgoing)
    {
        var end = start + count;
        for (var filter = start; filter < end; filter++)
        {
            if (!KernelQuery.HasRelationshipOfKind(world, targetId, _relationshipKindFilterIds[filter], outgoing))
            {
                return false;
            }
        }

        return true;
    }

    private bool RequiredAnyDirectionRelationshipsPass(WorldState world, int targetId, int start, int count)
    {
        var end = start + count;
        for (var filter = start; filter < end; filter++)
        {
            var relationshipKindId = _relationshipKindFilterIds[filter];
            if (!KernelQuery.HasRelationshipOfKind(world, targetId, relationshipKindId, outgoing: true) &&
                !KernelQuery.HasRelationshipOfKind(world, targetId, relationshipKindId, outgoing: false))
            {
                return false;
            }
        }

        return true;
    }

    private bool ResolveSecondaryBindings(
        WorldState world,
        int generatorSlot,
        int targetId,
        int targetOriginQ,
        int targetOriginR,
        int[] refCodes,
        int[] refEntityIds,
        ref int refCount,
        bool selectRandom,
        Simulation? simulation)
    {
        var first = _bindingStarts[generatorSlot];
        var end = first + _bindingCounts[generatorSlot];
        for (var binding = first; binding < end; binding++)
        {
            var entityId = SelectEntityForQuery(
                world,
                selectRandom ? simulation : null,
                _bindingKindIds[binding],
                targetId,
                targetOriginQ,
                targetOriginR,
                refCodes,
                refEntityIds,
                refCount,
                _bindingSubtypeStarts[binding],
                _bindingSubtypeCounts[binding],
                _bindingAdjacentToTarget[binding],
                _bindingCultureMatchCodes[binding],
                _bindingWithoutOutgoingStarts[binding],
                _bindingWithoutOutgoingCounts[binding],
                _bindingWithoutIncomingStarts[binding],
                _bindingWithoutIncomingCounts[binding]);
            if (entityId == 0)
            {
                return false;
            }

            SetRef(refCodes, refEntityIds, ref refCount, _bindingRefCodes[binding], entityId);
        }

        return true;
    }
}
