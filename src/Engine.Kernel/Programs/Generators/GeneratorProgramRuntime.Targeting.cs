namespace Engine.Kernel;

internal sealed partial class GeneratorProgramRuntime
{
    private bool TargetPasses(
        Simulation simulation,
        int generatorSlot,
        int targetId,
        int[] refCodes,
        int[] refEntityIds,
        out int refCount)
    {
        return TargetPasses(
            simulation,
            generatorSlot,
            targetId,
            refCodes,
            refEntityIds,
            out refCount,
            out _);
    }

    private bool TargetPasses(
        Simulation simulation,
        int generatorSlot,
        int targetId,
        int[] refCodes,
        int[] refEntityIds,
        out int refCount,
        out int rejectionReasonCode)
    {
        refCount = 0;
        var world = simulation.World;
        if (!TargetInsideActiveProvince(world, targetId))
        {
            rejectionReasonCode = GeneratorTargetRejectionReasonCodes.InactiveOrKind;
            return false;
        }

        if (!TargetPrimaryGatesPass(simulation, generatorSlot, targetId, out rejectionReasonCode))
        {
            return false;
        }

        var targetOriginQ = world.EntityPhysicalQ(targetId);
        var targetOriginR = world.EntityPhysicalR(targetId);
        SetRef(refCodes, refEntityIds, ref refCount, GeneratorProgramConstants.TargetReferenceCode, targetId);
        if (!ResolveSecondaryBindings(
            world,
            generatorSlot,
            targetId,
            targetOriginQ,
            targetOriginR,
            refCodes,
            refEntityIds,
            ref refCount,
            selectRandom: false,
            simulation: simulation))
        {
            rejectionReasonCode = GeneratorTargetRejectionReasonCodes.SecondaryBinding;
            return false;
        }

        rejectionReasonCode = GeneratorTargetRejectionReasonCodes.None;
        return true;
    }

    private bool TargetPrimaryGatesPass(
        Simulation simulation,
        int generatorSlot,
        int targetId,
        out int rejectionReasonCode)
    {
        var world = simulation.World;
        if (!KernelQuery.EntityIsActiveKind(world, targetId, _targetKindIds[generatorSlot]))
        {
            rejectionReasonCode = GeneratorTargetRejectionReasonCodes.InactiveOrKind;
            return false;
        }

        if (!TargetSubtypePasses(world, generatorSlot, targetId))
        {
            rejectionReasonCode = GeneratorTargetRejectionReasonCodes.Subtype;
            return false;
        }

        if (!KernelQuery.TagBitsPass(
                world.EntityTagBits(targetId),
                _targetRequiredTagBits[generatorSlot],
                _targetWithoutTagBits[generatorSlot]))
        {
            rejectionReasonCode = GeneratorTargetRejectionReasonCodes.Subtype;
            return false;
        }

        if (!TargetChronicleGatesPass(simulation, generatorSlot, targetId))
        {
            rejectionReasonCode = GeneratorTargetRejectionReasonCodes.Trigger;
            return false;
        }

        return TargetRelationshipAndMapGatesPass(simulation, generatorSlot, targetId, out rejectionReasonCode);
    }

    private bool TargetChronicleGatesPass(Simulation simulation, int generatorSlot, int targetId)
    {
        var heatMin = Data.TargetChronicleHeatMin[generatorSlot];
        if (heatMin > 0f && simulation.Chronicles.HeatFor(targetId) < heatMin)
        {
            return false;
        }

        var legendaryWithin = Data.TargetChronicleLegendaryWithinTicks[generatorSlot];
        return legendaryWithin <= 0 ||
            simulation.Chronicles.LegendaryResolvedWithin(targetId, simulation.World.CurrentTick, legendaryWithin);
    }

    private bool TargetSubtypePasses(WorldState world, int generatorSlot, int targetId) =>
        KernelQuery.SubtypeAllowed(
            _subtypeIds,
            _targetSubtypeStarts[generatorSlot],
            _targetSubtypeCounts[generatorSlot],
            world.EntitySubtypeId(targetId));

    private bool TargetRelationshipAndMapGatesPass(
        Simulation simulation,
        int generatorSlot,
        int targetId,
        out int rejectionReasonCode)
    {
        var world = simulation.World;
        if (!TargetRelationshipRequirementsPass(world, generatorSlot, targetId))
        {
            rejectionReasonCode = GeneratorTargetRejectionReasonCodes.RequiredRelationship;
            return false;
        }

        if (!TargetRelationshipAbsenceRequirementsPass(simulation, generatorSlot, targetId))
        {
            rejectionReasonCode = GeneratorTargetRejectionReasonCodes.RelationshipAbsence;
            return false;
        }

        if (!TargetTerrainRequirementsPass(world, generatorSlot, targetId))
        {
            rejectionReasonCode = GeneratorTargetRejectionReasonCodes.Terrain;
            return false;
        }

        if (!TriggersPass(world, generatorSlot, targetId))
        {
            rejectionReasonCode = GeneratorTargetRejectionReasonCodes.Trigger;
            return false;
        }

        rejectionReasonCode = GeneratorTargetRejectionReasonCodes.None;
        return true;
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

    private bool TargetRelationshipAbsenceRequirementsPass(Simulation simulation, int generatorSlot, int targetId)
    {
        var multiplier = simulation.CultureSlots.CausalSpendLimitMultiplier(targetId);
        return RelationshipAbsenceRequirementsPass(
                simulation,
                targetId,
                _targetWithoutOutgoingStarts[generatorSlot],
                _targetWithoutOutgoingCounts[generatorSlot],
                outgoing: true,
                multiplier) &&
            RelationshipAbsenceRequirementsPass(
                simulation,
                targetId,
                _targetWithoutIncomingStarts[generatorSlot],
                _targetWithoutIncomingCounts[generatorSlot],
                outgoing: false,
                multiplier);
    }

    private bool RelationshipAbsenceRequirementsPass(
        Simulation simulation,
        int entityId,
        int start,
        int count,
        bool outgoing,
        int multiplier)
    {
        var end = start + count;
        for (var filter = start; filter < end; filter++)
        {
            var relationshipKindId = _relationshipKindFilterIds[filter];
            var maximumExclusive = RelationshipAbsenceMaximumExclusive(
                simulation,
                relationshipKindId,
                filter,
                multiplier);
            if (KernelQuery.RelationshipEndpointCount(
                    simulation.World,
                    entityId,
                    relationshipKindId,
                    outgoing) >= maximumExclusive)
            {
                return false;
            }
        }

        return true;
    }

    private int RelationshipAbsenceMaximumExclusive(
        Simulation simulation,
        int relationshipKindId,
        int filter,
        int multiplier)
    {
        if (multiplier <= 1 ||
            _relationshipKindFilterAllowsSlotCausalLimitMultiplier[filter] == 0 ||
            simulation.Relationships.KindIsExclusive(relationshipKindId))
        {
            return 1;
        }

        return multiplier;
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
                simulation,
                selectRandom,
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
                _bindingSamePhysicalCultureComponentAsTarget[binding],
                _bindingWithoutOutgoingStarts[binding],
                _bindingWithoutOutgoingCounts[binding],
                _bindingWithoutIncomingStarts[binding],
                _bindingWithoutIncomingCounts[binding],
                _bindingRequiredTagBits[binding],
                _bindingWithoutTagBits[binding],
                _bindingCultureIds[binding],
                _bindingRelatedToTargetKindIds[binding],
                _bindingRelatedToTargetIncoming[binding],
                _bindingRelatedToTargetMinStrengths[binding]);
            if (entityId == 0)
            {
                return false;
            }

            SetRef(refCodes, refEntityIds, ref refCount, _bindingRefCodes[binding], entityId);
        }

        return true;
    }
}
