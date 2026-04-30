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
            ExecuteResourceStep(simulation, world, entityId, targetId, tick, step, operationCode) ||
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
            case ProcessOperationCodes.AddAttribute:
                simulation.ModifyAttribute(entityId, _stepA[step], _stepX[step], tick);
                return true;
            case ProcessOperationCodes.SetAttribute:
                simulation.SetAttribute(entityId, _stepA[step], _stepX[step], tick);
                return true;
            case ProcessOperationCodes.AddTargetAttribute:
                simulation.ModifyAttribute(targetId, _stepA[step], _stepX[step], tick);
                return true;
            case ProcessOperationCodes.TransferAttribute:
                TransferAttribute(simulation, entityId, targetId, tick, step);
                return true;
            case ProcessOperationCodes.AddAttributeFromVariable:
                AddAttributeFromVariable(simulation, entityId, tick, step, variableIds, variableValues, variableCount);
                return true;
            case ProcessOperationCodes.AddTargetAttributeFromVariable:
                AddAttributeFromVariable(simulation, targetId, tick, step, variableIds, variableValues, variableCount);
                return true;
            case ProcessOperationCodes.CalculateVariable:
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
            case ProcessOperationCodes.MovePhysical:
                simulation.MoveEntityPhysical(entityId, world.EntityPhysicalQ(entityId) + _stepA[step], world.EntityPhysicalR(entityId) + _stepB[step], tick);
                return true;
            case ProcessOperationCodes.MoveSemantic:
                simulation.MoveEntitySemantic(entityId, world.EntitySemanticQ(entityId) + _stepA[step], world.EntitySemanticR(entityId) + _stepB[step], world.EntitySemanticS(entityId) + _stepC[step], tick);
                return true;
            case ProcessOperationCodes.MoveTowardTargetPhysical:
                MoveTowardTargetPhysical(simulation, world, entityId, targetId, tick);
                return true;
            case ProcessOperationCodes.WanderPhysical:
                WanderPhysical(simulation, world, entityId, tick);
                return true;
            case ProcessOperationCodes.MoveTowardTargetSemantic:
                MoveTowardTargetSemantic(simulation, world, entityId, targetId, tick);
                return true;
            case ProcessOperationCodes.WanderSemantic:
                WanderSemantic(simulation, world, entityId, tick);
                return true;
            default:
                return false;
        }
    }

    private bool ExecuteResourceStep(Simulation simulation, WorldState world, int entityId, int targetId, int tick, int step, int operationCode)
    {
        switch (operationCode)
        {
            case ProcessOperationCodes.AddLocationResource:
                AddLocationResource(simulation, entityId, targetId, tick, step);
                return true;
            case ProcessOperationCodes.AddResource:
                simulation.ModifyResource(_stepA[step], world.EntityPhysicalQ(entityId) + _stepB[step], world.EntityPhysicalR(entityId) + _stepC[step], _stepX[step], tick);
                return true;
            case ProcessOperationCodes.AddPressure:
                simulation.Pressures.ApplyDelta(_stepA[step], _stepB[step] > 0 ? _stepB[step] : entityId, _stepX[step]);
                world.AdvanceWorldVersion();
                return true;
            default:
                return false;
        }
    }

    private bool ExecuteEntityStep(Simulation simulation, WorldState world, int entityId, int targetId, int tick, int step, int operationCode)
    {
        switch (operationCode)
        {
            case ProcessOperationCodes.SetStatus:
                simulation.SetEntityStatus(entityId, _stepA[step], tick);
                return true;
            case ProcessOperationCodes.CreateRelationship:
                simulation.CreateRelationship(_stepA[step], _stepC[step], entityId, _stepB[step] > 0 ? _stepB[step] : targetId, _stepX[step], tick, _stepD[step], factCount: _stepD[step] > 0 ? 1 : 0);
                return true;
            case ProcessOperationCodes.SetRelationshipStrength:
                SetRelationshipStrength(simulation, world, entityId, targetId, tick, step);
                return true;
            case ProcessOperationCodes.CreateEntity:
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
            case ProcessOperationCodes.SetHistorical:
                simulation.SetEntityStatus(_stepA[step] > 0 ? _stepA[step] : entityId, EntityStatusCodes.Historical, tick);
                return true;
            case ProcessOperationCodes.AddRelatedEntitiesAttribute:
                AddRelatedEntitiesAttribute(simulation, world, entityId, targetId, tick, step);
                return true;
            case ProcessOperationCodes.CreateFixedRelationship:
                simulation.CreateRelationship(_stepA[step], _stepD[step], _stepB[step] > 0 ? _stepB[step] : entityId, _stepC[step] > 0 ? _stepC[step] : targetId, _stepX[step], tick);
                return true;
            default:
                return false;
        }
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
        simulation.ModifyAttribute(
            entityId,
            _stepA[step],
            VariableValue(variableIds, variableValues, variableCount, _stepB[step]) * (_stepX[step] == 0f ? 1f : _stepX[step]),
            tick);
    }

    private void TransferAttribute(
        Simulation simulation,
        int entityId,
        int targetId,
        int tick,
        int step)
    {
        var world = simulation.World;
        var sourceId = EndpointEntityId(entityId, targetId, _stepA[step]);
        var destinationId = EndpointEntityId(entityId, targetId, _stepC[step]);
        var amount = _stepX[step];
        if (world.GetAttribute(sourceId, _stepB[step]) < amount)
        {
            return;
        }

        simulation.ModifyAttribute(sourceId, _stepB[step], -amount, tick);
        simulation.ModifyAttribute(destinationId, _stepD[step], amount, tick);
    }

    private void CreateEntity(Simulation simulation, int entityId, int tick, int step)
    {
        var world = simulation.World;
        var q = _stepD[step] == 0 ? world.EntityPhysicalQ(entityId) : (int)_stepX[step];
        var r = _stepD[step] == 0 ? world.EntityPhysicalR(entityId) : (int)_stepY[step];
        _ = simulation.CreateEntity(
            _stepA[step],
            _stepB[step],
            _stepC[step],
            q,
            r,
            world.EntitySemanticQ(entityId),
            world.EntitySemanticR(entityId),
            world.EntitySemanticS(entityId),
            tick);
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
        var cursor = _stepC[step] == RelationshipTraversalDirectionCodes.Incoming
            ? world.IncomingRelationshipsOfKind(anchorId, _stepA[step])
            : world.OutgoingRelationshipsOfKind(anchorId, _stepA[step]);
        while (world.MoveNextRelationship(ref cursor, out var relationshipId))
        {
            var relatedEntityId = _stepC[step] == RelationshipTraversalDirectionCodes.Incoming
                ? world.RelationshipSourceId(relationshipId)
                : world.RelationshipTargetId(relationshipId);
            if (world.EntityIsActive(relatedEntityId) && world.HasAttribute(relatedEntityId, _stepB[step]))
            {
                simulation.ModifyAttribute(relatedEntityId, _stepB[step], _stepX[step], tick);
            }
        }
    }

    private void AddLocationResource(
        Simulation simulation,
        int entityId,
        int targetId,
        int tick,
        int step)
    {
        var world = simulation.World;
        var locationEntityId = _stepB[step] == 0 ? entityId : targetId;
        simulation.ModifyResource(
            _stepA[step],
            world.EntityPhysicalQ(locationEntityId),
            world.EntityPhysicalR(locationEntityId),
            _stepX[step],
            tick);
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
        var relationshipId = world.FindRelationship(entityId, relationshipTargetId, _stepA[step]);
        if (relationshipId == 0)
        {
            return;
        }

        simulation.SetRelationshipStrength(relationshipId, _stepX[step], tick);
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
            simulation.MoveEntityPhysical(entityId, nextQ, nextR, tick);
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
        _pathFScore.Set(startIndex, HexDistance(startQ, startR, targetQ, targetR));
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
        _pathFScore.Set(neighborIndex, tentativeGScore + HexDistance(neighborQ, neighborR, targetQ, targetR));
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

        simulation.MoveEntityPhysical(
            entityId,
            currentQ + DirectionQ(selectedDirection),
            currentR + DirectionR(selectedDirection),
            tick);
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
        var bestDistance = HexDistance(currentQ, currentR, targetQ, targetR);
        for (var direction = 0; direction < 6; direction++)
        {
            var q = currentQ + DirectionQ(direction);
            var r = currentR + DirectionR(direction);
            var distance = HexDistance(q, r, targetQ, targetR);
            if (distance < bestDistance)
            {
                bestQ = q;
                bestR = r;
                bestDistance = distance;
            }
        }

        simulation.MoveEntitySemantic(entityId, bestQ, bestR, -bestQ - bestR, tick);
    }

    private static void WanderSemantic(Simulation simulation, WorldState world, int entityId, int tick)
    {
        var direction = simulation.NextInt(6);
        var q = world.EntitySemanticQ(entityId) + DirectionQ(direction);
        var r = world.EntitySemanticR(entityId) + DirectionR(direction);
        simulation.MoveEntitySemantic(entityId, q, r, -q - r, tick);
    }

    private static int EndpointEntityId(int entityId, int targetId, int endpointCode)
    {
        return endpointCode == ProcessEndpointCodes.Target ? targetId : entityId;
    }

    private static float ResourceAtEntity(WorldState world, int entityId, int resourceId)
    {
        return world.GetResource(resourceId, world.EntityPhysicalQ(entityId), world.EntityPhysicalR(entityId));
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

    private static float StandardNeedUrgency(float value)
    {
        var clamped = Clamp(value, 0f, 100f);
        if (clamped >= StandardNeedComfortable)
        {
            return 0f;
        }

        if (clamped >= StandardNeedConcern)
        {
            var t = (StandardNeedComfortable - clamped) / (StandardNeedComfortable - StandardNeedConcern);
            return 0.25f * t;
        }

        if (clamped >= StandardNeedCritical)
        {
            var t = (StandardNeedConcern - clamped) / (StandardNeedConcern - StandardNeedCritical);
            return 0.25f + (0.45f * t);
        }

        var criticalT = (StandardNeedCritical - clamped) / StandardNeedCritical;
        return 0.70f + (0.30f * criticalT);
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

    private static float Clamp(float value, float minimum, float maximum)
    {
        if (value < minimum)
        {
            return minimum;
        }

        return value > maximum ? maximum : value;
    }

    private static float ClampSelectionRoll(float value)
    {
        if (value < 0f)
        {
            return 0f;
        }

        return value > 1f ? 1f : value;
    }

    private static bool ValueInRange(float value, int hasMin, int hasMax, float min, float max)
    {
        if (hasMin != 0 && value < min)
        {
            return false;
        }

        return hasMax == 0 || value <= max;
    }

    private static int PlaneDistance(WorldState world, int entityId, int targetId, int planeCode)
    {
        return planeCode == PlaneCodes.Semantic
            ? HexDistance(
                world.EntitySemanticQ(entityId),
                world.EntitySemanticR(entityId),
                world.EntitySemanticQ(targetId),
                world.EntitySemanticR(targetId))
            : HexDistance(
                world.EntityPhysicalQ(entityId),
                world.EntityPhysicalR(entityId),
                world.EntityPhysicalQ(targetId),
                world.EntityPhysicalR(targetId));
    }

    private static int HexDistance(int aq, int ar, int bq, int br)
    {
        var aS = -aq - ar;
        var bS = -bq - br;
        return (Abs(aq - bq) + Abs(ar - br) + Abs(aS - bS)) / 2;
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
