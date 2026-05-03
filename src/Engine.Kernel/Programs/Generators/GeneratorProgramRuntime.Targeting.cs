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
            !KernelQuery.SubtypeAllowed(_subtypeIds, _targetSubtypeStarts[generatorSlot], _targetSubtypeCounts[generatorSlot], world.EntitySubtypeId(targetId)) ||
            !TargetRelationshipRequirementsPass(world, generatorSlot, targetId) ||
            !TargetRelationshipAbsenceRequirementsPass(world, generatorSlot, targetId) ||
            !TargetTerrainRequirementsPass(world, generatorSlot, targetId) ||
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
        return KernelQuery.AllRelationshipKindRequirementsPass(
                world,
                targetId,
                _relationshipKindFilterIds,
                _relationshipKindFilterMinimumStrengths,
                _targetRequiredOutgoingStarts[generatorSlot],
                _targetRequiredOutgoingCounts[generatorSlot],
                outgoing: true) &&
            KernelQuery.AllRelationshipKindRequirementsPass(
                world,
                targetId,
                _relationshipKindFilterIds,
                _relationshipKindFilterMinimumStrengths,
                _targetRequiredIncomingStarts[generatorSlot],
                _targetRequiredIncomingCounts[generatorSlot],
                outgoing: false) &&
            KernelQuery.AnyDirectionRelationshipKindRequirementsPass(
                world,
                targetId,
                _relationshipKindFilterIds,
                _relationshipKindFilterMinimumStrengths,
                _targetRequiredAnyStarts[generatorSlot],
                _targetRequiredAnyCounts[generatorSlot]);
    }

    private bool TargetRelationshipAbsenceRequirementsPass(WorldState world, int generatorSlot, int targetId)
    {
        return KernelQuery.NoRelationshipKindRequirementsPresent(
                world,
                targetId,
                _relationshipKindFilterIds,
                _targetWithoutOutgoingStarts[generatorSlot],
                _targetWithoutOutgoingCounts[generatorSlot],
                outgoing: true) &&
            KernelQuery.NoRelationshipKindRequirementsPresent(
                world,
                targetId,
                _relationshipKindFilterIds,
                _targetWithoutIncomingStarts[generatorSlot],
                _targetWithoutIncomingCounts[generatorSlot],
                outgoing: false);
    }

    private bool TargetTerrainRequirementsPass(WorldState world, int generatorSlot, int targetId)
    {
        var originQ = world.EntityPhysicalQ(targetId);
        var originR = world.EntityPhysicalR(targetId);
        var start = _targetTerrainRequirementStarts[generatorSlot];
        var end = start + _targetTerrainRequirementCounts[generatorSlot];
        for (var requirement = start; requirement < end; requirement++)
        {
            if (!KernelQuery.TerrainCellCountInRadiusBelow(
                    world,
                    originQ,
                    originR,
                    _targetTerrainRequirementTerrainIds[requirement],
                    _targetTerrainRequirementRadii[requirement],
                    _targetTerrainRequirementMaximumExclusive[requirement]))
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
            var entityId = SelectSharedEntity(
                world,
                selectRandom ? simulation : null,
                _bindingKindIds[binding],
                targetId,
                targetOriginQ,
                targetOriginR,
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
