namespace Engine.Kernel;

using Engine.Eventing;

internal sealed partial class GambitProgramRuntime
{
    private void ExecuteGambit(Simulation simulation, int entityId, int targetId, int gambitSlot, int tick)
    {
        global::System.Span<int> variableIds = _variableIds;
        global::System.Span<float> variableValues = _variableValues;
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
        global::System.Span<float> variableValues,
        ref int variableCount)
    {
        var world = simulation.World;
        var operationCode = _stepOperationCodes[step];
        if (ExecuteAttributeStep(simulation, entityId, targetId, tick, step, operationCode, variableIds, variableValues, ref variableCount) ||
            ExecuteMovementStep(simulation, world, entityId, targetId, tick, step, operationCode) ||
            ExecuteMapStep(simulation, world, targetId, tick, step, operationCode) ||
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
        global::System.Span<float> variableValues,
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

    private bool ExecuteMovementStep(Simulation simulation, WorldState world, int entityId, int targetId, int tick, int step, int operationCode)
    {
        switch (operationCode)
        {
            case BehaviorOperationCodes.MovePhysical:
                KernelEffectExecutor.MovePhysical(simulation, entityId, world.EntityPhysicalQ(entityId) + _stepA[step], world.EntityPhysicalR(entityId) + _stepB[step]);
                return true;
            case BehaviorOperationCodes.MoveSemantic:
                KernelEffectExecutor.MoveSemantic(simulation, entityId, world.EntitySemanticQ(entityId) + _stepA[step], world.EntitySemanticR(entityId) + _stepB[step], world.EntitySemanticS(entityId) + _stepC[step]);
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
                KernelEffectExecutor.MovePhysical(simulation, targetId, world.EntityPhysicalQ(entityId), world.EntityPhysicalR(entityId));
                return true;
            default:
                return false;
        }
    }

    private bool ExecuteMapStep(Simulation simulation, WorldState world, int targetId, int tick, int step, int operationCode)
    {
        switch (operationCode)
        {
            case BehaviorOperationCodes.SetTargetCoverageTerrain:
                SetTargetCoverageTerrain(simulation, world, targetId, tick, _stepA[step]);
                return true;
            default:
                return false;
        }
    }

    private bool ExecuteEntityStep(Simulation simulation, WorldState world, int entityId, int targetId, int tick, int step, int operationCode)
    {
        switch (operationCode)
        {
            case BehaviorOperationCodes.SetStatus:
                KernelEffectExecutor.SetEntityStatus(simulation, entityId, _stepA[step]);
                return true;
            case BehaviorOperationCodes.CreateRelationship:
                KernelEffectExecutor.CreateRelationship(simulation, _stepA[step], _stepC[step], entityId, _stepB[step] > 0 ? _stepB[step] : targetId, _stepX[step], _stepD[step], factCount: _stepD[step] > 0 ? 1 : 0);
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
            case BehaviorOperationCodes.AddRelatedEntitiesAttribute:
                AddRelatedEntitiesAttribute(simulation, world, entityId, targetId, tick, step);
                return true;
            case BehaviorOperationCodes.CreateFixedRelationship:
                KernelEffectExecutor.CreateRelationship(simulation, _stepA[step], _stepD[step], _stepB[step] > 0 ? _stepB[step] : entityId, _stepC[step] > 0 ? _stepC[step] : targetId, _stepX[step]);
                return true;
            case BehaviorOperationCodes.CreateEndpointRelationship:
                CreateEndpointRelationship(simulation, entityId, targetId, tick, step);
                return true;
            case BehaviorOperationCodes.SetEndpointRelationshipStrength:
                SetEndpointRelationshipStrength(simulation, world, entityId, targetId, tick, step);
                return true;
            case BehaviorOperationCodes.RemoveEndpointRelationship:
                RemoveEndpointRelationship(simulation, world, entityId, targetId, tick, step);
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

    private void AddAttributeFromVariable(
        Simulation simulation,
        int entityId,
        int tick,
        int step,
        global::System.Span<int> variableIds,
        global::System.Span<float> variableValues,
        int variableCount)
    {
        KernelEffectExecutor.ModifyAttribute(
            simulation,
            entityId,
            _stepA[step],
            VariableValue(variableIds, variableValues, variableCount, _stepB[step]) * (_stepX[step] == 0f ? 1f : _stepX[step]));
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
            minimumStrength: 0f,
            requiredFactId: 0,
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

    private float EvaluateMutationExpression(
        WorldState world,
        int entityId,
        int targetId,
        int step,
        global::System.ReadOnlySpan<int> variableIds,
        global::System.ReadOnlySpan<float> variableValues,
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
            defaultValue: 0f,
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

    private void MoveTowardTargetPhysical(Simulation simulation, WorldState world, int entityId, int targetId, int tick)
    {
        var currentQ = world.EntityPhysicalQ(entityId);
        var currentR = world.EntityPhysicalR(entityId);
        var targetQ = world.EntityPhysicalQ(targetId);
        var targetR = world.EntityPhysicalR(targetId);
        if (currentQ == targetQ && currentR == targetR)
        {
            return;
        }

        if (TryFindNextPhysicalPathStep(world, currentQ, currentR, targetQ, targetR, out var nextQ, out var nextR))
        {
            KernelEffectExecutor.MovePhysical(simulation, entityId, nextQ, nextR);
        }
    }

    private bool TryFindNextPhysicalPathStep(
        WorldState world,
        int startQ,
        int startR,
        int targetQ,
        int targetR,
        out int nextQ,
        out int nextR)
    {
        nextQ = startQ;
        nextR = startR;
        if (!world.CellIsKnown(startQ, startR) ||
            !world.CellIsKnown(targetQ, targetR))
        {
            return false;
        }

        var radius = world.MapRadius;
        var width = (radius * 2) + 1;
        if (width <= 0 || (long)width * width > _pathOpenSet.Length)
        {
            return false;
        }

        var startIndex = PhysicalPathIndex(radius, width, startQ, startR);
        var targetIndex = PhysicalPathIndex(radius, width, targetQ, targetR);
        if (startIndex < 0 || targetIndex < 0)
        {
            return false;
        }

        return SearchPhysicalPath(
            world,
            startIndex,
            targetIndex,
            radius,
            width,
            startQ,
            startR,
            targetQ,
            targetR,
            out nextQ,
            out nextR);
    }

    private bool SearchPhysicalPath(
        WorldState world,
        int startIndex,
        int targetIndex,
        int radius,
        int width,
        int startQ,
        int startR,
        int targetQ,
        int targetR,
        out int nextQ,
        out int nextR)
    {
        nextQ = startQ;
        nextR = startR;
        var openCount = InitializePhysicalPathSearch(startIndex, startQ, startR, targetQ, targetR);
        while (openCount > 0)
        {
            var currentIndex = TakeLowestPhysicalPathOpenNode(ref openCount);
            if (currentIndex == targetIndex)
            {
                return TryResolveNextPhysicalPathStep(startIndex, targetIndex, radius, width, out nextQ, out nextR);
            }

            if (!VisitPhysicalPathNeighbors(world, currentIndex, radius, width, targetQ, targetR, ref openCount))
            {
                return false;
            }
        }

        return false;
    }

    private int InitializePhysicalPathSearch(int startIndex, int startQ, int startR, int targetQ, int targetR)
    {
        _pathOpen.BeginEpoch();
        _pathClosed.BeginEpoch();
        _pathCameFrom.BeginEpoch();
        _pathGScore.BeginEpoch();
        _pathFScore.BeginEpoch();
        _pathOpenSet[0] = startIndex;
        _pathOpen.Add(startIndex);
        _pathGScore.Set(startIndex, 0f);
        _pathFScore.Set(startIndex, KernelQuery.HexDistance(startQ, startR, targetQ, targetR));
        return 1;
    }

    private int TakeLowestPhysicalPathOpenNode(ref int openCount)
    {
        var openSlot = SelectLowestPathScore(openCount);
        var currentIndex = _pathOpenSet[openSlot];
        openCount--;
        _pathOpenSet[openSlot] = _pathOpenSet[openCount];
        _pathOpen.Remove(currentIndex);
        _pathClosed.Add(currentIndex);
        return currentIndex;
    }

    private bool VisitPhysicalPathNeighbors(
        WorldState world,
        int currentIndex,
        int radius,
        int width,
        int targetQ,
        int targetR,
        ref int openCount)
    {
        var currentQ = PhysicalPathQ(radius, width, currentIndex);
        var currentR = PhysicalPathR(radius, width, currentIndex);
        for (var direction = 0; direction < 6; direction++)
        {
            if (!TryVisitPhysicalPathNeighbor(world, currentIndex, currentQ, currentR, direction, radius, width, targetQ, targetR, ref openCount))
            {
                return false;
            }
        }

        return true;
    }

    private bool TryVisitPhysicalPathNeighbor(
        WorldState world,
        int currentIndex,
        int currentQ,
        int currentR,
        int direction,
        int radius,
        int width,
        int targetQ,
        int targetR,
        ref int openCount)
    {
        var neighborQ = currentQ + DirectionQ(direction);
        var neighborR = currentR + DirectionR(direction);
        if (!world.CellIsKnown(neighborQ, neighborR))
        {
            return true;
        }

        var neighborIndex = PhysicalPathIndex(radius, width, neighborQ, neighborR);
        if (neighborIndex < 0 || _pathClosed.Contains(neighborIndex))
        {
            return true;
        }

        var tentativeGScore = _pathGScore.GetOrDefault(currentIndex, float.PositiveInfinity) + 1f;
        if (tentativeGScore >= _pathGScore.GetOrDefault(neighborIndex, float.PositiveInfinity))
        {
            return true;
        }

        _pathCameFrom.Set(neighborIndex, currentIndex);
        _pathGScore.Set(neighborIndex, tentativeGScore);
        _pathFScore.Set(neighborIndex, tentativeGScore + KernelQuery.HexDistance(neighborQ, neighborR, targetQ, targetR));
        return _pathOpen.Contains(neighborIndex) || AddPhysicalPathOpenNode(neighborIndex, ref openCount);
    }

    private bool AddPhysicalPathOpenNode(int neighborIndex, ref int openCount)
    {
        if (openCount >= _pathOpenSet.Length)
        {
            return false;
        }

        _pathOpenSet[openCount] = neighborIndex;
        _pathOpen.Add(neighborIndex);
        openCount++;
        return true;
    }

    private int SelectLowestPathScore(int openCount)
    {
        var selectedSlot = 0;
        var selectedIndex = _pathOpenSet[0];
        var selectedScore = _pathFScore.GetOrDefault(selectedIndex, float.PositiveInfinity);
        for (var slot = 1; slot < openCount; slot++)
        {
            var index = _pathOpenSet[slot];
            var score = _pathFScore.GetOrDefault(index, float.PositiveInfinity);
            if (score < selectedScore)
            {
                selectedSlot = slot;
                selectedIndex = index;
                selectedScore = score;
            }
        }

        return selectedSlot;
    }

    private bool TryResolveNextPhysicalPathStep(
        int startIndex,
        int targetIndex,
        int radius,
        int width,
        out int nextQ,
        out int nextR)
    {
        var currentIndex = targetIndex;
        var parentIndex = _pathCameFrom.GetOrDefault(currentIndex, -1);
        while (parentIndex != startIndex)
        {
            if (parentIndex < 0)
            {
                nextQ = 0;
                nextR = 0;
                return false;
            }

            currentIndex = parentIndex;
            parentIndex = _pathCameFrom.GetOrDefault(currentIndex, -1);
        }

        nextQ = PhysicalPathQ(radius, width, currentIndex);
        nextR = PhysicalPathR(radius, width, currentIndex);
        return true;
    }

    private int PhysicalPathIndex(int radius, int width, int q, int r)
    {
        var adjustedQ = q + radius;
        var adjustedR = r + radius;
        if ((uint)adjustedQ >= (uint)width || (uint)adjustedR >= (uint)width)
        {
            return -1;
        }

        var index = (adjustedQ * width) + adjustedR;
        return (uint)index < (uint)_pathCameFrom.Length ? index : -1;
    }

    private static int PhysicalPathQ(int radius, int width, int index)
    {
        return (index / width) - radius;
    }

    private static int PhysicalPathR(int radius, int width, int index)
    {
        return (index % width) - radius;
    }

    private static void WanderPhysical(Simulation simulation, WorldState world, int entityId, int tick)
    {
        var currentQ = world.EntityPhysicalQ(entityId);
        var currentR = world.EntityPhysicalR(entityId);
        // Future movement costs belong in an explicit movement program.
        var candidateCount = 0;
        for (var direction = 0; direction < 6; direction++)
        {
            if (world.CellIsKnown(currentQ + DirectionQ(direction), currentR + DirectionR(direction)))
            {
                candidateCount++;
            }
        }

        if (candidateCount == 0)
        {
            return;
        }

        var selected = simulation.NextInt(candidateCount);
        var seen = 0;
        var selectedDirection = 0;
        for (var direction = 0; direction < 6; direction++)
        {
            if (!world.CellIsKnown(currentQ + DirectionQ(direction), currentR + DirectionR(direction)))
            {
                continue;
            }

            if (seen == selected)
            {
                selectedDirection = direction;
                break;
            }

            seen++;
        }

        KernelEffectExecutor.MovePhysical(
            simulation,
            entityId,
            currentQ + DirectionQ(selectedDirection),
            currentR + DirectionR(selectedDirection));
    }

    private static void MoveTowardTargetSemantic(Simulation simulation, WorldState world, int entityId, int targetId, int tick)
    {
        var currentQ = world.EntitySemanticQ(entityId);
        var currentR = world.EntitySemanticR(entityId);
        var targetQ = world.EntitySemanticQ(targetId);
        var targetR = world.EntitySemanticR(targetId);
        if (currentQ == targetQ && currentR == targetR)
        {
            return;
        }

        var bestQ = currentQ;
        var bestR = currentR;
        var bestDistance = KernelQuery.HexDistance(currentQ, currentR, targetQ, targetR);
        for (var direction = 0; direction < 6; direction++)
        {
            var q = currentQ + DirectionQ(direction);
            var r = currentR + DirectionR(direction);
            var distance = KernelQuery.HexDistance(q, r, targetQ, targetR);
            if (distance < bestDistance)
            {
                bestQ = q;
                bestR = r;
                bestDistance = distance;
            }
        }

        KernelEffectExecutor.MoveSemantic(simulation, entityId, bestQ, bestR, -bestQ - bestR);
    }

    private static void WanderSemantic(Simulation simulation, WorldState world, int entityId, int tick)
    {
        var direction = simulation.NextInt(6);
        var q = world.EntitySemanticQ(entityId) + DirectionQ(direction);
        var r = world.EntitySemanticR(entityId) + DirectionR(direction);
        KernelEffectExecutor.MoveSemantic(simulation, entityId, q, r, -q - r);
    }

    private static int EndpointEntityId(int entityId, int targetId, int endpointCode)
    {
        return endpointCode == BehaviorEndpointCodes.Target ? targetId : entityId;
    }

    private static float VariableValue(
        global::System.ReadOnlySpan<int> variableIds,
        global::System.ReadOnlySpan<float> variableValues,
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

        return 0f;
    }

    private static void SetVariable(
        global::System.Span<int> variableIds,
        global::System.Span<float> variableValues,
        ref int variableCount,
        int variableId,
        float value)
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

    private void RecordGambit(int entityId, int gambitId, int tick)
    {
        if ((uint)entityId >= (uint)_actionHistoryCounts.Length)
        {
            throw new global::System.InvalidOperationException();
        }

        var slot = _actionHistoryNextSlots[entityId];
        var index = GambitHistoryIndex(entityId, slot);
        _actionHistoryTicks[index] = tick;
        _actionHistoryGambitIds[index] = gambitId;
        _actionHistoryNextSlots[entityId] = (slot + 1) % GambitHistoryLength;
        if (_actionHistoryCounts[entityId] < GambitHistoryLength)
        {
            _actionHistoryCounts[entityId]++;
        }
    }

    private int CountRecentActions(int entityId, int gambitId, int sinceTick)
    {
        if ((uint)entityId >= (uint)_actionHistoryCounts.Length)
        {
            return 0;
        }

        var count = 0;
        var historyCount = _actionHistoryCounts[entityId];
        for (var slot = 0; slot < historyCount; slot++)
        {
            var index = GambitHistoryIndex(entityId, slot);
            if (_actionHistoryGambitIds[index] == gambitId && _actionHistoryTicks[index] >= sinceTick)
            {
                count++;
            }
        }

        return count;
    }

    private static int GambitHistoryIndex(int entityId, int slot)
    {
        return (entityId * GambitHistoryLength) + slot;
    }

    private static int CooldownWithJitter(Simulation simulation, int cooldown)
    {
        var jitter = 1f + ((simulation.NextFloat() * 0.5f) - 0.25f);
        var ticks = (int)(cooldown * jitter);
        return ticks < 1 ? 1 : ticks;
    }

    private static float BaselineUrgency(float value, float baseline)
    {
        return baseline > 0f ? baseline / (baseline + value) : 0f;
    }

    private static float RangeUrgency(float value, float minimum, float maximum, float defaultValue)
    {
        var range = maximum - minimum;
        return range > 0f ? Abs(value - defaultValue) / range : 0f;
    }

    private static float Max(float left, float right)
    {
        return left > right ? left : right;
    }

    private static float Clamp01(float value)
    {
        if (value < 0f)
        {
            return 0f;
        }

        return value > 1f ? 1f : value;
    }

    private static float ClampSelectionRoll(float value)
    {
        if (value < 0f)
        {
            return 0f;
        }

        return value > 1f ? 1f : value;
    }

    private static int DirectionQ(int direction)
    {
        return direction switch
        {
            0 => 1,
            1 => 1,
            2 => 0,
            3 => -1,
            4 => -1,
            _ => 0,
        };
    }

    private static int DirectionR(int direction)
    {
        return direction switch
        {
            0 => 0,
            1 => -1,
            2 => -1,
            3 => 0,
            4 => 1,
            _ => 1,
        };
    }

}
