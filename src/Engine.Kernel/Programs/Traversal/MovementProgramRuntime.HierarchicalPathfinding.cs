namespace Engine.Kernel;

internal sealed partial class MovementProgramRuntime
{
    private bool TryFindNextRouteSegment(
        Simulation simulation,
        WorldState world,
        int planeCode,
        int startQ,
        int startR,
        int targetQ,
        int targetR,
        int movementProfileId,
        SegmentRouteContext context,
        int startSegment,
        int targetSegment,
        out int nextSegment,
        out int portalQ,
        out int portalR)
    {
        nextSegment = startSegment;
        portalQ = startQ;
        portalR = startR;
        if (startSegment == targetSegment)
        {
            return false;
        }

        var topologyEpoch = State.PathingTopologyEpoch;
        var roadEpoch = State.RoadMarkEpoch;
        if (State.TryGetRouteCache(
                planeCode,
                movementProfileId,
                context.SegmentSize,
                startSegment,
                targetSegment,
                topologyEpoch,
                roadEpoch,
                out nextSegment,
                out portalQ,
                out portalR))
        {
            return true;
        }

        return TryFindAndCacheRouteSegment(
            simulation,
            world,
            planeCode,
            startQ,
            startR,
            targetQ,
            targetR,
            movementProfileId,
            context,
            startSegment,
            targetSegment,
            topologyEpoch,
            roadEpoch,
            out nextSegment,
            out portalQ,
            out portalR);
    }

    private bool TryFindAndCacheRouteSegment(
        Simulation simulation,
        WorldState world,
        int planeCode,
        int startQ,
        int startR,
        int targetQ,
        int targetR,
        int movementProfileId,
        SegmentRouteContext context,
        int startSegment,
        int targetSegment,
        int topologyEpoch,
        int roadEpoch,
        out int nextSegment,
        out int portalQ,
        out int portalR)
    {
        portalQ = startQ;
        portalR = startR;
        if (!TrySearchSegmentRoute(
                simulation,
                world,
                planeCode,
                movementProfileId,
                context,
                startSegment,
                targetSegment,
                out nextSegment) ||
            !TryFindBestSegmentPortal(
                simulation,
                world,
                planeCode,
                startQ,
                startR,
                targetQ,
                targetR,
                movementProfileId,
                context,
                startSegment,
                nextSegment,
                out portalQ,
                out portalR,
                out _))
        {
            return false;
        }

        State.StoreRouteCache(
            planeCode,
            movementProfileId,
            context.SegmentSize,
            startSegment,
            targetSegment,
            topologyEpoch,
            roadEpoch,
            nextSegment,
            portalQ,
            portalR);
        return true;
    }

    private bool TrySearchSegmentRoute(
        Simulation simulation,
        WorldState world,
        int planeCode,
        int movementProfileId,
        SegmentRouteContext context,
        int startSegment,
        int targetSegment,
        out int nextSegment)
    {
        nextSegment = startSegment;
        var openCount = InitializeSegmentSearch(startSegment, targetSegment, context.GridWidth);
        while (openCount > 0)
        {
            var currentSegment = TakeLowestSegmentOpenNode(ref openCount);
            if (currentSegment == targetSegment)
            {
                return TryResolveNextSegmentStep(startSegment, targetSegment, out nextSegment);
            }

            if (!VisitSegmentNeighbors(
                    simulation,
                    world,
                    planeCode,
                    movementProfileId,
                    context,
                    currentSegment,
                    targetSegment,
                    ref openCount))
            {
                return false;
            }
        }

        return false;
    }

    private int InitializeSegmentSearch(
        int startSegment,
        int targetSegment,
        int segmentGridWidth)
    {
        Scratch.SegmentOpen.BeginEpoch();
        Scratch.SegmentClosed.BeginEpoch();
        Scratch.SegmentCameFrom.BeginEpoch();
        Scratch.SegmentGScore.BeginEpoch();
        Scratch.SegmentFScore.BeginEpoch();
        Scratch.SegmentOpenSet[0] = startSegment;
        Scratch.SegmentOpen.Add(startSegment);
        Scratch.SegmentGScore.Set(startSegment, 0f);
        Scratch.SegmentFScore.Set(startSegment, SegmentHeuristic(startSegment, targetSegment, segmentGridWidth));
        return 1;
    }

    private int TakeLowestSegmentOpenNode(ref int openCount)
    {
        var openSlot = SelectLowestSegmentScore(openCount);
        var currentSegment = Scratch.SegmentOpenSet[openSlot];
        openCount--;
        Scratch.SegmentOpenSet[openSlot] = Scratch.SegmentOpenSet[openCount];
        Scratch.SegmentOpen.Remove(currentSegment);
        Scratch.SegmentClosed.Add(currentSegment);
        return currentSegment;
    }

    private bool VisitSegmentNeighbors(
        Simulation simulation,
        WorldState world,
        int planeCode,
        int movementProfileId,
        SegmentRouteContext context,
        int currentSegment,
        int targetSegment,
        ref int openCount)
    {
        var segmentQ = SegmentQ(context.GridWidth, currentSegment);
        var segmentR = SegmentR(context.GridWidth, currentSegment);
        for (var direction = 0; direction < 6; direction++)
        {
            if (!TryVisitSegmentNeighbor(
                    simulation,
                    world,
                    planeCode,
                    movementProfileId,
                    context,
                    segmentQ + DirectionQ(direction),
                    segmentR + DirectionR(direction),
                    currentSegment,
                    targetSegment,
                    direction,
                    ref openCount))
            {
                return false;
            }
        }

        return true;
    }

    private bool TryVisitSegmentNeighbor(
        Simulation simulation,
        WorldState world,
        int planeCode,
        int movementProfileId,
        SegmentRouteContext context,
        int neighborQ,
        int neighborR,
        int currentSegment,
        int targetSegment,
        int direction,
        ref int openCount)
    {
        if ((uint)neighborQ >= (uint)context.GridWidth ||
            (uint)neighborR >= (uint)context.GridWidth)
        {
            return true;
        }

        var neighborSegment = SegmentIndex(context.GridWidth, neighborQ, neighborR);
        if (Scratch.SegmentClosed.Contains(neighborSegment) ||
            !TryEstimateSegmentEdgeCost(
                simulation,
                world,
                planeCode,
                movementProfileId,
                context,
                currentSegment,
                neighborSegment,
                direction,
                out var edgeCost))
        {
            return true;
        }

        return TryRelaxSegmentEdge(
            neighborSegment,
            currentSegment,
            targetSegment,
            context.GridWidth,
            edgeCost,
            ref openCount);
    }

    private bool TryRelaxSegmentEdge(
        int neighborSegment,
        int currentSegment,
        int targetSegment,
        int segmentGridWidth,
        float edgeCost,
        ref int openCount)
    {
        var tentativeGScore = Scratch.SegmentGScore.GetOrDefault(currentSegment, float.PositiveInfinity) + edgeCost;
        if (tentativeGScore >= Scratch.SegmentGScore.GetOrDefault(neighborSegment, float.PositiveInfinity))
        {
            return true;
        }

        Scratch.SegmentCameFrom.Set(neighborSegment, currentSegment);
        Scratch.SegmentGScore.Set(neighborSegment, tentativeGScore);
        Scratch.SegmentFScore.Set(
            neighborSegment,
            tentativeGScore + SegmentHeuristic(neighborSegment, targetSegment, segmentGridWidth));
        return Scratch.SegmentOpen.Contains(neighborSegment) || AddSegmentOpenNode(neighborSegment, ref openCount);
    }

    private bool AddSegmentOpenNode(int segment, ref int openCount)
    {
        if (openCount >= Scratch.SegmentOpenSet.Length)
        {
            return false;
        }

        Scratch.SegmentOpenSet[openCount] = segment;
        Scratch.SegmentOpen.Add(segment);
        openCount++;
        return true;
    }

    private int SelectLowestSegmentScore(int openCount)
    {
        var selectedSlot = 0;
        var selectedSegment = Scratch.SegmentOpenSet[0];
        var selectedScore = Scratch.SegmentFScore.GetOrDefault(selectedSegment, float.PositiveInfinity);
        for (var slot = 1; slot < openCount; slot++)
        {
            var segment = Scratch.SegmentOpenSet[slot];
            var score = Scratch.SegmentFScore.GetOrDefault(segment, float.PositiveInfinity);
            if (score < selectedScore)
            {
                selectedSlot = slot;
                selectedSegment = segment;
                selectedScore = score;
            }
        }

        return selectedSlot;
    }

    private bool TryResolveNextSegmentStep(
        int startSegment,
        int targetSegment,
        out int nextSegment)
    {
        var currentSegment = targetSegment;
        var parentSegment = Scratch.SegmentCameFrom.GetOrDefault(currentSegment, -1);
        while (parentSegment != startSegment)
        {
            if (parentSegment < 0)
            {
                nextSegment = startSegment;
                return false;
            }

            currentSegment = parentSegment;
            parentSegment = Scratch.SegmentCameFrom.GetOrDefault(currentSegment, -1);
        }

        nextSegment = currentSegment;
        return true;
    }

    private bool TryFindBestSegmentPortal(
        Simulation simulation,
        WorldState world,
        int planeCode,
        int startQ,
        int startR,
        int targetQ,
        int targetR,
        int movementProfileId,
        SegmentRouteContext context,
        int fromSegment,
        int toSegment,
        out int portalQ,
        out int portalR,
        out float edgeCost)
    {
        portalQ = 0;
        portalR = 0;
        edgeCost = float.PositiveInfinity;
        if (!TrySegmentDirection(context.GridWidth, fromSegment, toSegment, out var direction) ||
            !TrySegmentBounds(
                context.Radius,
                context.SegmentSize,
                context.GridWidth,
                fromSegment,
                out var minQ,
                out var minR,
                out var maxQ,
                out var maxR))
        {
            return false;
        }

        var found = false;
        var bestScore = float.PositiveInfinity;
        var edgeBaseCost = SegmentEdgeBaseCost(planeCode, context.SegmentSize, fromSegment, toSegment);
        EvaluateSegmentPortalBoundary(
            simulation,
            world,
            planeCode,
            direction,
            minQ,
            minR,
            maxQ,
            maxR,
            startQ,
            startR,
            targetQ,
            targetR,
            movementProfileId,
            edgeBaseCost,
            ref found,
            ref bestScore,
            ref portalQ,
            ref portalR,
            ref edgeCost);

        return found;
    }

    private bool TryEstimateSegmentEdgeCost(
        Simulation simulation,
        WorldState world,
        int planeCode,
        int movementProfileId,
        SegmentRouteContext context,
        int fromSegment,
        int toSegment,
        int direction,
        out float edgeCost)
    {
        edgeCost = float.PositiveInfinity;
        if (!TrySegmentBounds(
                context.Radius,
                context.SegmentSize,
                context.GridWidth,
                fromSegment,
                out var minQ,
                out var minR,
                out var maxQ,
                out var maxR))
        {
            return false;
        }

        var found = false;
        var edgeBaseCost = SegmentEdgeBaseCost(planeCode, context.SegmentSize, fromSegment, toSegment);
        EvaluateSegmentEdgeBoundary(
            simulation,
            world,
            planeCode,
            direction,
            minQ,
            minR,
            maxQ,
            maxR,
            movementProfileId,
            edgeBaseCost,
            ref found,
            ref edgeCost);

        return found;
    }

    private void EvaluateSegmentPortalBoundary(
        Simulation simulation,
        WorldState world,
        int planeCode,
        int direction,
        int minQ,
        int minR,
        int maxQ,
        int maxR,
        int startQ,
        int startR,
        int targetQ,
        int targetR,
        int movementProfileId,
        float edgeBaseCost,
        ref bool found,
        ref float bestScore,
        ref int portalQ,
        ref int portalR,
        ref float edgeCost)
    {
        switch (direction)
        {
            case 0:
                for (var r = minR; r <= maxR; r++)
                {
                    EvaluateSegmentPortalPair(
                        simulation, world, planeCode, maxQ, r, maxQ + 1, r,
                        startQ, startR, targetQ, targetR, movementProfileId, edgeBaseCost,
                        ref found, ref bestScore, ref portalQ, ref portalR, ref edgeCost);
                }

                return;
            case 1:
                EvaluateSegmentPortalPair(
                    simulation, world, planeCode, maxQ, minR, maxQ + 1, minR - 1,
                    startQ, startR, targetQ, targetR, movementProfileId, edgeBaseCost,
                    ref found, ref bestScore, ref portalQ, ref portalR, ref edgeCost);
                return;
            case 2:
                for (var q = minQ; q <= maxQ; q++)
                {
                    EvaluateSegmentPortalPair(
                        simulation, world, planeCode, q, minR, q, minR - 1,
                        startQ, startR, targetQ, targetR, movementProfileId, edgeBaseCost,
                        ref found, ref bestScore, ref portalQ, ref portalR, ref edgeCost);
                }

                return;
            case 3:
                for (var r = minR; r <= maxR; r++)
                {
                    EvaluateSegmentPortalPair(
                        simulation, world, planeCode, minQ, r, minQ - 1, r,
                        startQ, startR, targetQ, targetR, movementProfileId, edgeBaseCost,
                        ref found, ref bestScore, ref portalQ, ref portalR, ref edgeCost);
                }

                return;
            case 4:
                EvaluateSegmentPortalPair(
                    simulation, world, planeCode, minQ, maxR, minQ - 1, maxR + 1,
                    startQ, startR, targetQ, targetR, movementProfileId, edgeBaseCost,
                    ref found, ref bestScore, ref portalQ, ref portalR, ref edgeCost);
                return;
            default:
                for (var q = minQ; q <= maxQ; q++)
                {
                    EvaluateSegmentPortalPair(
                        simulation, world, planeCode, q, maxR, q, maxR + 1,
                        startQ, startR, targetQ, targetR, movementProfileId, edgeBaseCost,
                        ref found, ref bestScore, ref portalQ, ref portalR, ref edgeCost);
                }

                return;
        }
    }

    private void EvaluateSegmentEdgeBoundary(
        Simulation simulation,
        WorldState world,
        int planeCode,
        int direction,
        int minQ,
        int minR,
        int maxQ,
        int maxR,
        int movementProfileId,
        float edgeBaseCost,
        ref bool found,
        ref float edgeCost)
    {
        switch (direction)
        {
            case 0:
                for (var r = minR; r <= maxR; r++)
                {
                    EvaluateSegmentEdgePair(
                        simulation, world, planeCode, maxQ, r, maxQ + 1, r,
                        movementProfileId, edgeBaseCost, ref found, ref edgeCost);
                }

                return;
            case 1:
                EvaluateSegmentEdgePair(
                    simulation, world, planeCode, maxQ, minR, maxQ + 1, minR - 1,
                    movementProfileId, edgeBaseCost, ref found, ref edgeCost);
                return;
            case 2:
                for (var q = minQ; q <= maxQ; q++)
                {
                    EvaluateSegmentEdgePair(
                        simulation, world, planeCode, q, minR, q, minR - 1,
                        movementProfileId, edgeBaseCost, ref found, ref edgeCost);
                }

                return;
            case 3:
                for (var r = minR; r <= maxR; r++)
                {
                    EvaluateSegmentEdgePair(
                        simulation, world, planeCode, minQ, r, minQ - 1, r,
                        movementProfileId, edgeBaseCost, ref found, ref edgeCost);
                }

                return;
            case 4:
                EvaluateSegmentEdgePair(
                    simulation, world, planeCode, minQ, maxR, minQ - 1, maxR + 1,
                    movementProfileId, edgeBaseCost, ref found, ref edgeCost);
                return;
            default:
                for (var q = minQ; q <= maxQ; q++)
                {
                    EvaluateSegmentEdgePair(
                        simulation, world, planeCode, q, maxR, q, maxR + 1,
                        movementProfileId, edgeBaseCost, ref found, ref edgeCost);
                }

                return;
        }
    }

    private void EvaluateSegmentPortalPair(
        Simulation simulation,
        WorldState world,
        int planeCode,
        int q,
        int r,
        int neighborQ,
        int neighborR,
        int startQ,
        int startR,
        int targetQ,
        int targetR,
        int movementProfileId,
        float edgeBaseCost,
        ref bool found,
        ref float bestScore,
        ref int portalQ,
        ref int portalR,
        ref float edgeCost)
    {
        if (!TryGetPortalSurfaceCost(simulation, world, planeCode, q, r, movementProfileId, out _) ||
            !TryGetPortalSurfaceCost(simulation, world, planeCode, neighborQ, neighborR, movementProfileId, out var neighborTerrainCost))
        {
            return;
        }

        var candidateCost = SegmentPortalEdgeCost(edgeBaseCost, neighborTerrainCost);
        if (float.IsInfinity(candidateCost))
        {
            return;
        }

        var score = KernelQuery.HexDistance(startQ, startR, q, r) +
            KernelQuery.HexDistance(neighborQ, neighborR, targetQ, targetR) +
            candidateCost;
        if (score >= bestScore)
        {
            return;
        }

        bestScore = score;
        edgeCost = candidateCost;
        portalQ = neighborQ;
        portalR = neighborR;
        found = true;
    }

    private void EvaluateSegmentEdgePair(
        Simulation simulation,
        WorldState world,
        int planeCode,
        int q,
        int r,
        int neighborQ,
        int neighborR,
        int movementProfileId,
        float edgeBaseCost,
        ref bool found,
        ref float edgeCost)
    {
        if (!TryGetPortalSurfaceCost(simulation, world, planeCode, q, r, movementProfileId, out _) ||
            !TryGetPortalSurfaceCost(simulation, world, planeCode, neighborQ, neighborR, movementProfileId, out var neighborTerrainCost))
        {
            return;
        }

        var candidateCost = SegmentPortalEdgeCost(edgeBaseCost, neighborTerrainCost);
        if (candidateCost >= edgeCost)
        {
            return;
        }

        edgeCost = candidateCost;
        found = true;
    }

    private float SegmentEdgeBaseCost(
        int planeCode,
        int segmentSize,
        int fromSegment,
        int toSegment)
    {
        var mark = global::System.Math.Max(
            State.RoadMark(planeCode, segmentSize, fromSegment),
            State.RoadMark(planeCode, segmentSize, toSegment));
        return segmentSize * RoadMarkCostMultiplier(mark);
    }

    private static float SegmentPortalEdgeCost(float edgeBaseCost, float terrainCost)
    {
        return global::System.MathF.Max(
            1f,
            edgeBaseCost + global::System.MathF.Min(terrainCost, 10f));
    }

    private bool TryGetPortalSurfaceCost(
        Simulation simulation,
        WorldState world,
        int planeCode,
        int q,
        int r,
        int movementProfileId,
        out float terrainCost)
    {
        terrainCost = float.PositiveInfinity;
        if (!world.CellIsKnown(planeCode, q, r))
        {
            return false;
        }

        terrainCost = SurfaceBaseTravelCost(simulation, planeCode, movementProfileId, q, r);
        return terrainCost > 0f;
    }

    private static float RoadMarkCostMultiplier(int mark)
    {
        return mark switch
        {
            MovementProgramState.RoadMarkHigh => 0.65f,
            MovementProgramState.RoadMarkLow => 0.85f,
            _ => 1f,
        };
    }

    private static int SegmentGridWidth(int radius, int segmentSize)
    {
        return (((radius * 2) + 1) + segmentSize - 1) / segmentSize;
    }

    private static int SegmentIndex(
        int radius,
        int segmentSize,
        int segmentGridWidth,
        int q,
        int r)
    {
        var segmentQ = (q + radius) / segmentSize;
        var segmentR = (r + radius) / segmentSize;
        if ((uint)segmentQ >= (uint)segmentGridWidth || (uint)segmentR >= (uint)segmentGridWidth)
        {
            return -1;
        }

        return SegmentIndex(segmentGridWidth, segmentQ, segmentR);
    }

    private static int SegmentIndex(int segmentGridWidth, int segmentQ, int segmentR)
    {
        return (segmentQ * segmentGridWidth) + segmentR;
    }

    private static int SegmentQ(int segmentGridWidth, int segment)
    {
        return segment / segmentGridWidth;
    }

    private static int SegmentR(int segmentGridWidth, int segment)
    {
        return segment % segmentGridWidth;
    }

    private static int SegmentHeuristic(int segment, int targetSegment, int segmentGridWidth)
    {
        return KernelQuery.HexDistance(
            SegmentQ(segmentGridWidth, segment),
            SegmentR(segmentGridWidth, segment),
            SegmentQ(segmentGridWidth, targetSegment),
            SegmentR(segmentGridWidth, targetSegment));
    }

    private static bool TrySegmentDirection(
        int segmentGridWidth,
        int fromSegment,
        int toSegment,
        out int direction)
    {
        var fromQ = SegmentQ(segmentGridWidth, fromSegment);
        var fromR = SegmentR(segmentGridWidth, fromSegment);
        var deltaQ = SegmentQ(segmentGridWidth, toSegment) - fromQ;
        var deltaR = SegmentR(segmentGridWidth, toSegment) - fromR;
        for (var candidate = 0; candidate < 6; candidate++)
        {
            if (DirectionQ(candidate) == deltaQ && DirectionR(candidate) == deltaR)
            {
                direction = candidate;
                return true;
            }
        }

        direction = -1;
        return false;
    }

    private static bool TrySegmentBounds(
        int radius,
        int segmentSize,
        int segmentGridWidth,
        int segment,
        out int minQ,
        out int minR,
        out int maxQ,
        out int maxR)
    {
        minQ = 0;
        minR = 0;
        maxQ = 0;
        maxR = 0;
        if ((uint)segment >= (uint)(segmentGridWidth * segmentGridWidth))
        {
            return false;
        }

        var segmentQ = SegmentQ(segmentGridWidth, segment);
        var segmentR = SegmentR(segmentGridWidth, segment);
        minQ = -radius + (segmentQ * segmentSize);
        minR = -radius + (segmentR * segmentSize);
        maxQ = global::System.Math.Min(radius, minQ + segmentSize - 1);
        maxR = global::System.Math.Min(radius, minR + segmentSize - 1);
        return true;
    }
}
