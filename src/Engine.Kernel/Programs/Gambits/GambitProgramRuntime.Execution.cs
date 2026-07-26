namespace Engine.Kernel;

using Engine.Eventing;

internal sealed partial class GambitProgramRuntime
{
    private void ExecuteGambit(Simulation simulation, int entityId, int targetId, int gambitSlot, int tick)
    {
        global::System.Span<int> variableIds = _variableIds;
        global::System.Span<FixedScalar> variableValues = _variableValues;
        var variableCount = 0;
        var firstStep = _gambitStepStarts[gambitSlot];
        var endStep = firstStep + _gambitStepCounts[gambitSlot];
        for (var step = firstStep; step < endStep; step++)
        {
            ExecuteStep(
                simulation,
                entityId,
                targetId,
                tick,
                step,
                variableIds,
                variableValues,
                ref variableCount);
        }
    }

    private void ExecuteStep(
        Simulation simulation,
        int entityId,
        int targetId,
        int tick,
        int step,
        global::System.Span<int> variableIds,
        global::System.Span<FixedScalar> variableValues,
        ref int variableCount)
    {
        var world = simulation.World;
        var operationCode = _stepOperationCodes[step];
        if (ExecuteAttributeStep(simulation, entityId, targetId, tick, step, operationCode, variableIds, variableValues, ref variableCount) ||
            ExecuteMovementStep(simulation, world, entityId, targetId, tick, step, operationCode) ||
            ExecuteKernelStateStep(simulation, entityId, step, operationCode) ||
            ExecuteMapStep(simulation, world, entityId, targetId, tick, step, operationCode) ||
            ExecuteEntityStep(simulation, world, entityId, targetId, tick, step, operationCode))
        {
            return;
        }

        throw new global::System.InvalidOperationException();
    }

    private bool ExecuteAttributeStep(
        Simulation simulation,
        int entityId,
        int targetId,
        int tick,
        int step,
        int operationCode,
        global::System.Span<int> variableIds,
        global::System.Span<FixedScalar> variableValues,
        ref int variableCount)
    {
        switch (operationCode)
        {
            case BehaviorOperationCodes.AddAttribute:
                KernelEffectExecutor.ModifyAttribute(simulation, entityId, _stepA[step], _stepX[step]);
                return true;
            case BehaviorOperationCodes.SetAttribute:
                KernelEffectExecutor.SetAttribute(simulation, entityId, _stepA[step], _stepX[step]);
                return true;
            case BehaviorOperationCodes.AddTargetAttribute:
                KernelEffectExecutor.ModifyAttribute(simulation, targetId, _stepA[step], _stepX[step]);
                return true;
            case BehaviorOperationCodes.TransferAttribute:
                TransferAttribute(simulation, entityId, targetId, tick, step);
                return true;
            case BehaviorOperationCodes.AddAttributeFromVariable:
                AddAttributeFromVariable(simulation, entityId, tick, step, variableIds, variableValues, variableCount);
                return true;
            case BehaviorOperationCodes.AddTargetAttributeFromVariable:
                AddAttributeFromVariable(simulation, targetId, tick, step, variableIds, variableValues, variableCount);
                return true;
            case BehaviorOperationCodes.CalculateVariable:
                SetVariable(
                    variableIds,
                    variableValues,
                    ref variableCount,
                    _stepA[step],
                    EvaluateMutationExpression(simulation.World, entityId, targetId, step, variableIds, variableValues, variableCount));
                return true;
            default:
                return false;
        }
    }

    private bool ExecuteKernelStateStep(Simulation simulation, int entityId, int step, int operationCode)
    {
        if (operationCode != BehaviorOperationCodes.IncreaseWorkTier)
        {
            return false;
        }

        _ = simulation.World.IncreaseEntityWorkTier(entityId, _stepA[step]);
        return true;
    }

    private bool ExecuteMovementStep(Simulation simulation, WorldState world, int entityId, int targetId, int tick, int step, int operationCode)
    {
        switch (operationCode)
        {
            case BehaviorOperationCodes.MovePhysical:
                KernelEffectExecutor.MovePhysical(
                    simulation,
                    entityId,
                    world.EntityPhysicalQ(entityId) + _stepA[step],
                    world.EntityPhysicalR(entityId) + _stepB[step],
                    MovementCauseCodes.Travel);
                return true;
            case BehaviorOperationCodes.MoveSemantic:
                KernelEffectExecutor.MoveSemantic(
                    simulation,
                    entityId,
                    world.EntitySemanticQ(entityId) + _stepA[step],
                    world.EntitySemanticR(entityId) + _stepB[step],
                    world.EntitySemanticS(entityId) + _stepC[step],
                    MovementCauseCodes.Travel);
                return true;
            case BehaviorOperationCodes.MoveTowardTargetPhysical:
                MoveTowardTargetPhysical(simulation, world, entityId, targetId, tick);
                return true;
            case BehaviorOperationCodes.WanderPhysical:
                WanderPhysical(simulation, world, entityId, tick);
                return true;
            case BehaviorOperationCodes.MoveTowardTargetSemantic:
                MoveTowardTargetSemantic(simulation, world, entityId, targetId, tick);
                return true;
            case BehaviorOperationCodes.WanderSemantic:
                WanderSemantic(simulation, world, entityId, tick);
                return true;
            case BehaviorOperationCodes.MoveTargetToSelfPhysical:
                KernelEffectExecutor.MovePhysical(
                    simulation,
                    targetId,
                    world.EntityPhysicalQ(entityId),
                    world.EntityPhysicalR(entityId),
                    MovementCauseCodes.Forced);
                return true;
            default:
                return false;
        }
    }

    private bool ExecuteMapStep(Simulation simulation, WorldState world, int entityId, int targetId, int tick, int step, int operationCode)
    {
        switch (operationCode)
        {
            case BehaviorOperationCodes.SetTargetCoverageTerrain:
                SetTargetCoverageTerrain(simulation, world, targetId, tick, _stepA[step]);
                return true;
            case BehaviorOperationCodes.BuildTravelStructureLine:
                BuildTravelStructureLine(simulation, world, entityId, targetId, tick, step);
                return true;
            case BehaviorOperationCodes.ForgeCellVertical:
                _ = simulation.TryForgeCellVertical(entityId, _stepA[step], _stepB[step], tick);
                return true;
            case BehaviorOperationCodes.ErodeTargetCulture:
                ErodeTargetCulture(simulation, world, targetId, tick, _stepA[step], _stepX[step]);
                return true;
            default:
                return false;
        }
    }

    /// <summary>
    /// Conflict scars the field: erode the target's culture influence around
    /// the target's physical position, strongest at the center.
    /// </summary>
    private static void ErodeTargetCulture(Simulation simulation, WorldState world, int targetId, int tick, int radius, FixedScalar amountPerCell)
    {
        if (targetId <= 0 || !world.EntityIsActive(targetId))
        {
            return;
        }

        var cultureId = world.EntityCultureId(targetId);
        if (cultureId <= 0)
        {
            return;
        }

        var centerQ = world.EntityPhysicalQ(targetId);
        var centerR = world.EntityPhysicalR(targetId);
        for (var dq = -radius; dq <= radius; dq++)
        {
            for (var dr = global::System.Math.Max(-radius, -dq - radius); dr <= global::System.Math.Min(radius, -dq + radius); dr++)
            {
                var distance = (global::System.Math.Abs(dq) + global::System.Math.Abs(dr) + global::System.Math.Abs(dq + dr)) / 2;
                var falloff = 1 - FixedScalar.FromRatio(distance, radius + 1);
                _ = simulation.ModifyCellCultureInfluenceRaw(
                    cultureId,
                    PlaneCodes.Physical,
                    centerQ + dq,
                    centerR + dr,
                    -amountPerCell * falloff,
                    tick);
            }
        }
    }

    // On install, the acting artifact's tile joins the target settlement's
    // coverage, the registered subtype bonus becomes a durable district
    // record, and the artifact leaves the simulation. On reawaken, the
    // acting settlement's newest sleeping core returns to the world on its
    // tile while the tile stays city ground. See
    // docs/design/artifact-districts.md.
    private bool ExecuteDistrictStep(
        Simulation simulation,
        WorldState world,
        int entityId,
        int targetId,
        int step,
        int operationCode)
    {
        return operationCode switch
        {
            BehaviorOperationCodes.InstallDistrict =>
                InstallDistrict(simulation, world, entityId, targetId, step),
            BehaviorOperationCodes.ReawakenDistrict =>
                ReawakenDistrict(simulation, world, entityId),
            _ => false,
        };
    }

    private bool InstallDistrict(Simulation simulation, WorldState world, int entityId, int targetId, int step)
    {
        if (!world.TryGetDistrictBonus(world.EntitySubtypeId(entityId), out var bonusKindCode, out var magnitude))
        {
            return true;
        }

        var capacity = world.StageDistrictCapacity(
            world.StageRuleFor(world.EntityKindId(targetId), world.EntityWorkTier(targetId)));
        if (capacity > 0 && world.ActiveDistrictCount(targetId) >= capacity)
        {
            return true;
        }

        var q = world.EntityPhysicalQ(entityId);
        var r = world.EntityPhysicalR(entityId);
        KernelEffectExecutor.SetCoverageOwner(simulation, q, r, targetId);
        _ = world.InstallDistrict(
            world.EntityKindId(entityId),
            world.EntitySubtypeId(entityId),
            world.EntityCultureId(entityId),
            targetId,
            q,
            r,
            bonusKindCode,
            magnitude,
            _stepA[step],
            _stepC[step],
            _stepX[step]);
        KernelEffectExecutor.SetEntityStatus(simulation, entityId, EntityStatusCodes.Historical);
        return true;
    }

    private static bool ReawakenDistrict(Simulation simulation, WorldState world, int entityId)
    {
        if (world.TryRemoveNewestDistrictOf(entityId, out var template))
        {
            var artifactId = KernelEffectExecutor.CreateEntity(
                simulation,
                template.ArtifactKindId,
                template.ArtifactSubtypeId,
                template.ArtifactCultureId,
                template.Q,
                template.R,
                world.EntitySemanticQ(entityId),
                world.EntitySemanticR(entityId),
                world.EntitySemanticS(entityId));
            _ = KernelEffectExecutor.CreateRelationship(
                simulation,
                template.RelationshipKindId,
                template.RelationshipPolarityCode,
                artifactId,
                entityId,
                template.RelationshipStrength);
        }

        return true;
    }

    private bool ExecuteEntityStep(Simulation simulation, WorldState world, int entityId, int targetId, int tick, int step, int operationCode)
    {
        switch (operationCode)
        {
            case BehaviorOperationCodes.InstallDistrict:
            case BehaviorOperationCodes.ReawakenDistrict:
                return ExecuteDistrictStep(simulation, world, entityId, targetId, step, operationCode);
            case BehaviorOperationCodes.DemoteStage:
                _ = world.DecreaseEntityWorkTier(
                    EndpointEntityId(entityId, targetId, _stepB[step]));
                return true;
            case BehaviorOperationCodes.SetStatus:
                KernelEffectExecutor.SetEntityStatus(simulation, entityId, _stepA[step]);
                return true;
            case BehaviorOperationCodes.CreateRelationship:
                KernelEffectExecutor.CreateRelationship(simulation, _stepA[step], _stepC[step], entityId, _stepB[step] > 0 ? _stepB[step] : targetId, _stepX[step]);
                return true;
            case BehaviorOperationCodes.SetRelationshipStrength:
                SetRelationshipStrength(simulation, world, entityId, targetId, tick, step);
                return true;
            case BehaviorOperationCodes.CreateEntity:
                CreateEntity(simulation, entityId, tick, step);
                return true;
            default:
                return ExecuteOtherEntityStep(simulation, world, entityId, targetId, tick, step, operationCode);
        }
    }

    private bool ExecuteOtherEntityStep(Simulation simulation, WorldState world, int entityId, int targetId, int tick, int step, int operationCode)
    {
        switch (operationCode)
        {
            case BehaviorOperationCodes.SetHistorical:
                KernelEffectExecutor.SetEntityStatus(simulation, HistoricalEntityId(entityId, targetId, step), EntityStatusCodes.Historical);
                return true;
            case BehaviorOperationCodes.SetTargetCultureFromSelf:
                KernelEffectExecutor.SetEntityCulture(simulation, targetId, world.EntityCultureId(entityId));
                return true;
            case BehaviorOperationCodes.AddRelatedEntitiesAttribute:
                AddRelatedEntitiesAttribute(simulation, world, entityId, targetId, tick, step);
                return true;
            case BehaviorOperationCodes.CreateFixedRelationship:
                KernelEffectExecutor.CreateRelationship(simulation, _stepA[step], _stepD[step], _stepB[step] > 0 ? _stepB[step] : entityId, _stepC[step] > 0 ? _stepC[step] : targetId, _stepX[step]);
                return true;
            default:
                return ExecuteEndpointRelationshipStep(simulation, world, entityId, targetId, tick, step, operationCode);
        }
    }

    private bool ExecuteEndpointRelationshipStep(Simulation simulation, WorldState world, int entityId, int targetId, int tick, int step, int operationCode)
    {
        switch (operationCode)
        {
            case BehaviorOperationCodes.CreateEndpointRelationship:
                CreateEndpointRelationship(simulation, entityId, targetId, tick, step);
                return true;
            case BehaviorOperationCodes.SetEndpointRelationshipStrength:
                SetEndpointRelationshipStrength(simulation, world, entityId, targetId, tick, step);
                return true;
            case BehaviorOperationCodes.RemoveEndpointRelationship:
                RemoveEndpointRelationship(simulation, world, entityId, targetId, tick, step);
                return true;
            case BehaviorOperationCodes.CullEndpointIncomingRelationships:
                CullEndpointIncomingRelationships(simulation, world, entityId, targetId, step);
                return true;
            default:
                return false;
        }
    }

    private static void SetTargetCoverageTerrain(Simulation simulation, WorldState world, int targetId, int tick, int terrainId)
    {
        KernelQuery.ForEachCoverageCell(
            world,
            targetId,
            (q, r) =>
            {
                KernelEffectExecutor.SetCellTerrain(simulation, q, r, terrainId);
                return true;
            });
    }

    private void BuildTravelStructureLine(Simulation simulation, WorldState world, int entityId, int targetId, int tick, int step)
    {
        var planeCode = _stepB[step];
        var startQ = CurrentQ(world, entityId, planeCode);
        var startR = CurrentR(world, entityId, planeCode);
        var targetQ = CurrentQ(world, targetId, planeCode);
        var targetR = CurrentR(world, targetId, planeCode);
        var distance = KernelQuery.HexDistance(startQ, startR, targetQ, targetR);
        var maxDistance = _stepC[step];
        if (maxDistance > 0 && distance > maxDistance)
        {
            return;
        }

        if (distance == 0)
        {
            simulation.Movement.SetTravelStructure(
                simulation,
                planeCode,
                startQ,
                startR,
                _stepA[step],
                tick);
            return;
        }

        for (var index = 0; index <= distance; index++)
        {
            CubeLineCoordinate(startQ, startR, targetQ, targetR, index, distance, out var q, out var r);
            simulation.Movement.SetTravelStructure(
                simulation,
                planeCode,
                q,
                r,
                _stepA[step],
                tick);
        }
    }

    private static int CurrentQ(WorldState world, int entityId, int planeCode) =>
        planeCode == PlaneCodes.Semantic ? world.EntitySemanticQ(entityId) : world.EntityPhysicalQ(entityId);

    private static int CurrentR(WorldState world, int entityId, int planeCode) =>
        planeCode == PlaneCodes.Semantic ? world.EntitySemanticR(entityId) : world.EntityPhysicalR(entityId);

    private static void CubeLineCoordinate(
        int startQ,
        int startR,
        int targetQ,
        int targetR,
        int index,
        int distance,
        out int q,
        out int r)
    {
        var t = (FixedScalar)index / distance;
        var startS = -startQ - startR;
        var targetS = -targetQ - targetR;
        var x = Lerp(startQ, targetQ, t);
        var z = Lerp(startR, targetR, t);
        var y = Lerp(startS, targetS, t);
        CubeRound(x, y, z, out q, out r);
    }

    private static FixedScalar Lerp(int start, int target, FixedScalar t) => start + ((target - start) * t);

    private static void CubeRound(FixedScalar x, FixedScalar y, FixedScalar z, out int q, out int r)
    {
        var roundedX = (int)FixedScalar.Round(x, global::System.MidpointRounding.AwayFromZero);
        var roundedY = (int)FixedScalar.Round(y, global::System.MidpointRounding.AwayFromZero);
        var roundedZ = (int)FixedScalar.Round(z, global::System.MidpointRounding.AwayFromZero);
        var xDiff = FixedScalar.Abs(roundedX - x);
        var yDiff = FixedScalar.Abs(roundedY - y);
        var zDiff = FixedScalar.Abs(roundedZ - z);
        if (xDiff > yDiff && xDiff > zDiff)
        {
            roundedX = -roundedY - roundedZ;
        }
        else if (yDiff > zDiff)
        {
            roundedY = -roundedX - roundedZ;
        }
        else
        {
            roundedZ = -roundedX - roundedY;
        }

        _ = roundedY;
        q = roundedX;
        r = roundedZ;
    }

    private void AddAttributeFromVariable(
        Simulation simulation,
        int entityId,
        int tick,
        int step,
        global::System.Span<int> variableIds,
        global::System.Span<FixedScalar> variableValues,
        int variableCount)
    {
        KernelEffectExecutor.ModifyAttribute(
            simulation,
            entityId,
            _stepA[step],
            VariableValue(variableIds, variableValues, variableCount, _stepB[step]) * (_stepX[step] == 0 ? 1 : _stepX[step]));
    }

    private void TransferAttribute(
        Simulation simulation,
        int entityId,
        int targetId,
        int tick,
        int step)
    {
        var sourceId = EndpointEntityId(entityId, targetId, _stepA[step]);
        var destinationId = EndpointEntityId(entityId, targetId, _stepC[step]);
        KernelEffectExecutor.TransferAttribute(
            simulation,
            sourceId,
            _stepB[step],
            destinationId,
            _stepD[step],
            _stepX[step]);
    }

    private void CreateEntity(Simulation simulation, int entityId, int tick, int step)
    {
        var world = simulation.World;
        var q = _stepD[step] == 0 ? world.EntityPhysicalQ(entityId) : (int)_stepX[step];
        var r = _stepD[step] == 0 ? world.EntityPhysicalR(entityId) : (int)_stepY[step];
        _ = KernelEffectExecutor.CreateEntity(
            simulation,
            _stepA[step],
            _stepB[step],
            _stepC[step],
            q,
            r,
            world.EntitySemanticQ(entityId),
            world.EntitySemanticR(entityId),
            world.EntitySemanticS(entityId));
    }

    private void AddRelatedEntitiesAttribute(
        Simulation simulation,
        WorldState world,
        int entityId,
        int targetId,
        int tick,
        int step)
    {
        var anchorId = _stepD[step] == 0 ? entityId : targetId;
        KernelQuery.ForEachRelatedEndpoint(
            world,
            anchorId,
            _stepA[step],
            minimumStrength: 0,
            outgoing: _stepC[step] != RelationshipTraversalDirectionCodes.Incoming,
            (_, relatedEntityId) =>
            {
                if (world.EntityIsActive(relatedEntityId) && world.HasAttribute(relatedEntityId, _stepB[step]))
                {
                    KernelEffectExecutor.ModifyAttribute(simulation, relatedEntityId, _stepB[step], _stepX[step]);
                }

                return true;
            });
    }

    private FixedScalar EvaluateMutationExpression(
        WorldState world,
        int entityId,
        int targetId,
        int step,
        global::System.ReadOnlySpan<int> variableIds,
        global::System.ReadOnlySpan<FixedScalar> variableValues,
        int variableCount)
    {
        return EvaluateExpression(
            world,
            entityId,
            targetId,
            _stepExpressionStarts[step],
            _stepExpressionCounts[step],
            _mutationExpressionCodes,
            _mutationExpressionA,
            _mutationExpressionX,
            defaultValue: 0,
            variableIds,
            variableValues,
            variableCount);
    }

    private void SetRelationshipStrength(Simulation simulation, WorldState world, int entityId, int targetId, int tick, int step)
    {
        var relationshipTargetId = _stepB[step] > 0 ? _stepB[step] : targetId;
        var relationshipId = KernelQuery.FindRelationship(world, entityId, relationshipTargetId, _stepA[step]);
        if (relationshipId == 0)
        {
            return;
        }

        KernelEffectExecutor.SetRelationshipStrength(simulation, relationshipId, _stepX[step]);
    }

    private void SetEndpointRelationshipStrength(Simulation simulation, WorldState world, int entityId, int targetId, int tick, int step)
    {
        var sourceId = EndpointEntityId(entityId, targetId, _stepB[step]);
        var relationshipTargetId = EndpointEntityId(entityId, targetId, _stepC[step]);
        var relationshipId = KernelQuery.FindRelationship(world, sourceId, relationshipTargetId, _stepA[step]);
        if (relationshipId == 0)
        {
            return;
        }

        KernelEffectExecutor.SetRelationshipStrength(simulation, relationshipId, _stepX[step]);
    }

    private void CullEndpointIncomingRelationships(Simulation simulation, WorldState world, int entityId, int targetId, int step)
    {
        _ = world;
        var endpointId = EndpointEntityId(entityId, targetId, _stepB[step]);
        KernelEffectExecutor.CullIncomingRelationshipsOfKind(simulation, endpointId, _stepA[step]);
    }

    private void RemoveEndpointRelationship(Simulation simulation, WorldState world, int entityId, int targetId, int tick, int step)
    {
        var sourceId = EndpointEntityId(entityId, targetId, _stepB[step]);
        var relationshipTargetId = EndpointEntityId(entityId, targetId, _stepC[step]);
        var relationshipId = KernelQuery.FindRelationship(world, sourceId, relationshipTargetId, _stepA[step]);
        if (relationshipId == 0)
        {
            return;
        }

        KernelEffectExecutor.CullRelationship(simulation, relationshipId);
    }

    private void CreateEndpointRelationship(Simulation simulation, int entityId, int targetId, int tick, int step)
    {
        KernelEffectExecutor.CreateRelationship(
            simulation,
            _stepA[step],
            _stepD[step],
            EndpointEntityId(entityId, targetId, _stepB[step]),
            EndpointEntityId(entityId, targetId, _stepC[step]),
            _stepX[step]);
    }

    private int HistoricalEntityId(int entityId, int targetId, int step)
    {
        return _stepA[step] > 0 ? _stepA[step] : EndpointEntityId(entityId, targetId, _stepB[step]);
    }

    private static void MoveTowardTargetPhysical(Simulation simulation, WorldState world, int entityId, int targetId, int tick)
    {
        _ = tick;
        var targetQ = world.EntityPhysicalQ(targetId);
        var targetR = world.EntityPhysicalR(targetId);
        simulation.Movement.Navigate(simulation, entityId, PlaneCodes.Physical, targetQ, targetR, simulation.CurrentTick);
    }

    private static void WanderPhysical(Simulation simulation, WorldState world, int entityId, int tick)
    {
        _ = world;
        _ = tick;
        simulation.Movement.Wander(simulation, entityId, PlaneCodes.Physical, simulation.CurrentTick);
    }

    private static void MoveTowardTargetSemantic(Simulation simulation, WorldState world, int entityId, int targetId, int tick)
    {
        _ = tick;
        var targetQ = world.EntitySemanticQ(targetId);
        var targetR = world.EntitySemanticR(targetId);
        simulation.Movement.Navigate(simulation, entityId, PlaneCodes.Semantic, targetQ, targetR, simulation.CurrentTick);
    }

    private static void WanderSemantic(Simulation simulation, WorldState world, int entityId, int tick)
    {
        _ = world;
        _ = tick;
        simulation.Movement.Wander(simulation, entityId, PlaneCodes.Semantic, simulation.CurrentTick);
    }

    private static int EndpointEntityId(int entityId, int targetId, int endpointCode)
    {
        return endpointCode == BehaviorEndpointCodes.Target ? targetId : entityId;
    }

    private static FixedScalar VariableValue(
        global::System.ReadOnlySpan<int> variableIds,
        global::System.ReadOnlySpan<FixedScalar> variableValues,
        int variableCount,
        int variableId)
    {
        for (var i = 0; i < variableCount; i++)
        {
            if (variableIds[i] == variableId)
            {
                return variableValues[i];
            }
        }

        return 0;
    }

    private static void SetVariable(
        global::System.Span<int> variableIds,
        global::System.Span<FixedScalar> variableValues,
        ref int variableCount,
        int variableId,
        FixedScalar value)
    {
        for (var i = 0; i < variableCount; i++)
        {
            if (variableIds[i] == variableId)
            {
                variableValues[i] = value;
                return;
            }
        }

        if (variableCount >= variableIds.Length)
        {
            throw new global::System.InvalidOperationException();
        }

        variableIds[variableCount] = variableId;
        variableValues[variableCount] = value;
        variableCount++;
    }

    private static int CooldownWithJitter(
        Simulation simulation,
        int entityOccurrenceHandle,
        int gambitId,
        int cooldown,
        int attempt)
    {
        var jitter = 1 + ((simulation.RandomScalar(
            entityOccurrenceHandle,
            gambitId,
            KernelRandomPurposeCodes.GambitCooldown,
            attempt) * FixedScalar.FromUnits(500000L)) - FixedScalar.FromUnits(250000L));
        var ticks = (int)(cooldown * jitter);
        return ticks < 1 ? 1 : ticks;
    }

    private static FixedScalar Clamp01(FixedScalar value)
    {
        if (value < 0)
        {
            return 0;
        }

        return value > 1 ? 1 : value;
    }

}
