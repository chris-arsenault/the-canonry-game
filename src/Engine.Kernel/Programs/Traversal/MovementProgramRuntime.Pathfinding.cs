namespace Engine.Kernel;

internal sealed partial class MovementProgramRuntime
{
    private bool TryPlanNextStep(
        Simulation simulation,
        int entityId,
        int planeCode,
        int startQ,
        int startR,
        int targetQ,
        int targetR,
        int tick,
        out int nextQ,
        out int nextR)
    {
        nextQ = startQ;
        nextR = startR;
        var world = simulation.World;
        if (!simulation.ProgramIsEnabled(KernelProgramCodes.Movement) ||
            !world.CellIsKnown(planeCode, startQ, startR) ||
            !world.CellIsKnown(planeCode, targetQ, targetR))
        {
            return false;
        }

        var movementProfileId = Data.MovementProfileFor(world, entityId, planeCode);
        return TryPlanHierarchicalNextStep(
            simulation,
            world,
            planeCode,
            startQ,
            startR,
            targetQ,
            targetR,
            movementProfileId,
            tick,
            out nextQ,
            out nextR);
    }

    private bool TryPlanHierarchicalNextStep(
        Simulation simulation,
        WorldState world,
        int planeCode,
        int startQ,
        int startR,
        int targetQ,
        int targetR,
        int movementProfileId,
        int tick,
        out int nextQ,
        out int nextR)
    {
        nextQ = startQ;
        nextR = startR;
        if (!TryBuildSegmentRouteContext(
                world,
                LocalPathSegmentSize,
                startQ,
                startR,
                targetQ,
                targetR,
                out var localContext) ||
            !TryBuildSegmentRouteContext(
                world,
                RegionalPathSegmentSize,
                startQ,
                startR,
                targetQ,
                targetR,
                out var regionalContext))
        {
            return false;
        }

        if (localContext.StartSegment == localContext.TargetSegment)
        {
            return TrySearchPathWithinSegments(
                simulation, world, planeCode, startQ, startR, targetQ, targetR, movementProfileId, tick,
                localContext.Radius, localContext.SegmentSize, localContext.GridWidth,
                localContext.StartSegment, secondSegment: -1, out nextQ, out nextR);
        }

        if (regionalContext.StartSegment == regionalContext.TargetSegment)
        {
            return TrySearchNextLocalStep(
                simulation, world, planeCode, startQ, startR, targetQ, targetR, movementProfileId, tick,
                localContext, localContext.TargetSegment, targetQ, targetR, out nextQ, out nextR);
        }

        return TrySearchNextRegionalStep(
            simulation, world, planeCode, startQ, startR, targetQ, targetR, movementProfileId, tick,
            localContext, regionalContext, out nextQ, out nextR);
    }

    private bool TryBuildSegmentRouteContext(
        WorldState world,
        int segmentSize,
        int startQ,
        int startR,
        int targetQ,
        int targetR,
        out SegmentRouteContext context)
    {
        var radius = world.MapRadius;
        var segmentGridWidth = SegmentGridWidth(radius, segmentSize);
        context = new SegmentRouteContext(radius, segmentSize, segmentGridWidth, -1, -1);
        if (segmentGridWidth <= 0 ||
            (long)segmentGridWidth * segmentGridWidth > Scratch.SegmentCapacity)
        {
            return false;
        }

        var startSegment = SegmentIndex(radius, segmentSize, segmentGridWidth, startQ, startR);
        var targetSegment = SegmentIndex(radius, segmentSize, segmentGridWidth, targetQ, targetR);
        context = new SegmentRouteContext(radius, segmentSize, segmentGridWidth, startSegment, targetSegment);
        return startSegment >= 0 && targetSegment >= 0;
    }

    private bool TrySearchNextRegionalStep(
        Simulation simulation,
        WorldState world,
        int planeCode,
        int startQ,
        int startR,
        int targetQ,
        int targetR,
        int movementProfileId,
        int tick,
        SegmentRouteContext localContext,
        SegmentRouteContext regionalContext,
        out int nextQ,
        out int nextR)
    {
        nextQ = startQ;
        nextR = startR;
        if (!TryFindNextRouteSegment(
                simulation,
                world,
                planeCode,
                startQ,
                startR,
                targetQ,
                targetR,
                movementProfileId,
                regionalContext,
                regionalContext.StartSegment,
                regionalContext.TargetSegment,
                out var _,
                out var regionalPortalQ,
                out var regionalPortalR))
        {
            return false;
        }

        var portalLocalSegment = SegmentIndex(
            localContext.Radius,
            localContext.SegmentSize,
            localContext.GridWidth,
            regionalPortalQ,
            regionalPortalR);
        if (portalLocalSegment < 0)
        {
            return false;
        }

        return TrySearchNextLocalStep(
            simulation,
            world,
            planeCode,
            startQ,
            startR,
            regionalPortalQ,
            regionalPortalR,
            movementProfileId,
            tick,
            localContext,
            portalLocalSegment,
            regionalPortalQ,
            regionalPortalR,
            out nextQ,
            out nextR);
    }

    private bool TrySearchNextLocalStep(
        Simulation simulation,
        WorldState world,
        int planeCode,
        int startQ,
        int startR,
        int targetQ,
        int targetR,
        int movementProfileId,
        int tick,
        SegmentRouteContext localContext,
        int targetLocalSegment,
        int localTargetQ,
        int localTargetR,
        out int nextQ,
        out int nextR)
    {
        nextQ = startQ;
        nextR = startR;
        if (localContext.StartSegment == targetLocalSegment)
        {
            return TrySearchSingleLocalSegment(
                simulation, world, planeCode, startQ, startR, localTargetQ, localTargetR, movementProfileId, tick,
                localContext, out nextQ, out nextR);
        }

        return TrySearchRoutedLocalStep(
            simulation, world, planeCode, startQ, startR, targetQ, targetR, movementProfileId, tick,
            localContext, targetLocalSegment, localTargetQ, localTargetR, out nextQ, out nextR);
    }

    private bool TrySearchSingleLocalSegment(
        Simulation simulation,
        WorldState world,
        int planeCode,
        int startQ,
        int startR,
        int targetQ,
        int targetR,
        int movementProfileId,
        int tick,
        SegmentRouteContext localContext,
        out int nextQ,
        out int nextR) =>
        TrySearchPathWithinSegments(
            simulation,
            world,
            planeCode,
            startQ,
            startR,
            targetQ,
            targetR,
            movementProfileId,
            tick,
            localContext.Radius,
            localContext.SegmentSize,
            localContext.GridWidth,
            localContext.StartSegment,
            secondSegment: -1,
            out nextQ,
            out nextR);

    private bool TrySearchRoutedLocalStep(
        Simulation simulation,
        WorldState world,
        int planeCode,
        int startQ,
        int startR,
        int targetQ,
        int targetR,
        int movementProfileId,
        int tick,
        SegmentRouteContext localContext,
        int targetLocalSegment,
        int localTargetQ,
        int localTargetR,
        out int nextQ,
        out int nextR)
    {
        if (!TryFindNextRouteSegment(
                simulation,
                world,
                planeCode,
                startQ,
                startR,
                targetQ,
                targetR,
                movementProfileId,
                localContext,
                localContext.StartSegment,
                targetLocalSegment,
                out var nextSegment,
                out var portalQ,
                out var portalR))
        {
            nextQ = startQ;
            nextR = startR;
            return false;
        }

        if (nextSegment == targetLocalSegment)
        {
            portalQ = localTargetQ;
            portalR = localTargetR;
        }

        return TrySearchPathWithinSegments(
            simulation,
            world,
            planeCode,
            startQ,
            startR,
            portalQ,
            portalR,
            movementProfileId,
            tick,
            localContext.Radius,
            localContext.SegmentSize,
            localContext.GridWidth,
            localContext.StartSegment,
            nextSegment,
            out nextQ,
            out nextR);
    }

    private bool TrySearchPathWithinSegments(
        Simulation simulation,
        WorldState world,
        int planeCode,
        int startQ,
        int startR,
        int targetQ,
        int targetR,
        int movementProfileId,
        int tick,
        int radius,
        int segmentSize,
        int segmentGridWidth,
        int firstSegment,
        int secondSegment,
        out int nextQ,
        out int nextR)
    {
        nextQ = startQ;
        nextR = startR;
        if (!TrySegmentBounds(radius, segmentSize, segmentGridWidth, firstSegment, out var minQ, out var minR, out var maxQ, out var maxR))
        {
            return false;
        }

        if (secondSegment >= 0)
        {
            if (!TrySegmentBounds(radius, segmentSize, segmentGridWidth, secondSegment, out var secondMinQ, out var secondMinR, out var secondMaxQ, out var secondMaxR))
            {
                return false;
            }

            minQ = global::System.Math.Min(minQ, secondMinQ);
            minR = global::System.Math.Min(minR, secondMinR);
            maxQ = global::System.Math.Max(maxQ, secondMaxQ);
            maxR = global::System.Math.Max(maxR, secondMaxR);
        }

        var widthQ = (maxQ - minQ) + 1;
        var widthR = (maxR - minR) + 1;
        if (widthQ <= 0 || widthR <= 0 || (long)widthQ * widthR > Scratch.PathCapacity)
        {
            return false;
        }

        return SearchPath(
            simulation,
            world,
            planeCode,
            startQ,
            startR,
            targetQ,
            targetR,
            movementProfileId,
            tick,
            minQ,
            minR,
            widthQ,
            widthR,
            radius,
            segmentSize,
            segmentGridWidth,
            firstSegment,
            secondSegment,
            out nextQ,
            out nextR);
    }

    private bool SearchPath(
        Simulation simulation,
        WorldState world,
        int planeCode,
        int startQ,
        int startR,
        int targetQ,
        int targetR,
        int movementProfileId,
        int tick,
        int pathMinQ,
        int pathMinR,
        int pathWidthQ,
        int pathWidthR,
        int radius,
        int segmentSize,
        int segmentGridWidth,
        int firstAllowedSegment,
        int secondAllowedSegment,
        out int nextQ,
        out int nextR)
    {
        nextQ = startQ;
        nextR = startR;
        var startIndex = PathIndex(pathMinQ, pathMinR, pathWidthQ, pathWidthR, startQ, startR);
        var targetIndex = PathIndex(pathMinQ, pathMinR, pathWidthQ, pathWidthR, targetQ, targetR);
        if (startIndex < 0 || targetIndex < 0)
        {
            return false;
        }

        var openCount = InitializePathSearch(startIndex, startQ, startR, targetQ, targetR);
        while (openCount > 0)
        {
            var currentIndex = TakeLowestPathOpenNode(ref openCount);
            if (currentIndex == targetIndex)
            {
                return TryResolveNextPathStep(
                    startIndex,
                    targetIndex,
                    pathMinQ,
                    pathMinR,
                    pathWidthR,
                    out nextQ,
                    out nextR);
            }

            if (!VisitPathNeighbors(
                    simulation,
                    world,
                    currentIndex,
                    pathMinQ,
                    pathMinR,
                    pathWidthQ,
                    pathWidthR,
                    targetQ,
                    targetR,
                    movementProfileId,
                    planeCode,
                    tick,
                    radius,
                    segmentSize,
                    segmentGridWidth,
                    firstAllowedSegment,
                    secondAllowedSegment,
                    ref openCount))
            {
                return false;
            }
        }

        return false;
    }

    private int InitializePathSearch(int startIndex, int startQ, int startR, int targetQ, int targetR)
    {
        Scratch.PathOpen.BeginEpoch();
        Scratch.PathClosed.BeginEpoch();
        Scratch.PathCameFrom.BeginEpoch();
        Scratch.PathGScore.BeginEpoch();
        Scratch.PathFScore.BeginEpoch();
        Scratch.PathOpenSet[0] = startIndex;
        Scratch.PathOpen.Add(startIndex);
        Scratch.PathGScore.Set(startIndex, 0f);
        Scratch.PathFScore.Set(startIndex, KernelQuery.HexDistance(startQ, startR, targetQ, targetR));
        return 1;
    }

    private int TakeLowestPathOpenNode(ref int openCount)
    {
        var openSlot = SelectLowestPathScore(openCount);
        var currentIndex = Scratch.PathOpenSet[openSlot];
        openCount--;
        Scratch.PathOpenSet[openSlot] = Scratch.PathOpenSet[openCount];
        Scratch.PathOpen.Remove(currentIndex);
        Scratch.PathClosed.Add(currentIndex);
        return currentIndex;
    }

    private bool VisitPathNeighbors(
        Simulation simulation,
        WorldState world,
        int currentIndex,
        int pathMinQ,
        int pathMinR,
        int pathWidthQ,
        int pathWidthR,
        int targetQ,
        int targetR,
        int movementProfileId,
        int planeCode,
        int tick,
        int radius,
        int segmentSize,
        int segmentGridWidth,
        int firstAllowedSegment,
        int secondAllowedSegment,
        ref int openCount)
    {
        var currentQ = PathQ(pathMinQ, pathWidthR, currentIndex);
        var currentR = PathR(pathMinR, pathWidthR, currentIndex);
        for (var direction = 0; direction < 6; direction++)
        {
            if (!TryVisitPathNeighbor(
                    simulation,
                    world,
                    currentIndex,
                    currentQ,
                    currentR,
                    direction,
                    pathMinQ,
                    pathMinR,
                    pathWidthQ,
                    pathWidthR,
                    targetQ,
                    targetR,
                    movementProfileId,
                    planeCode,
                    tick,
                    radius,
                    segmentSize,
                    segmentGridWidth,
                    firstAllowedSegment,
                    secondAllowedSegment,
                    ref openCount))
            {
                return false;
            }
        }

        return true;
    }

    private bool TryVisitPathNeighbor(
        Simulation simulation,
        WorldState world,
        int currentIndex,
        int currentQ,
        int currentR,
        int direction,
        int pathMinQ,
        int pathMinR,
        int pathWidthQ,
        int pathWidthR,
        int targetQ,
        int targetR,
        int movementProfileId,
        int planeCode,
        int tick,
        int radius,
        int segmentSize,
        int segmentGridWidth,
        int firstAllowedSegment,
        int secondAllowedSegment,
        ref int openCount)
    {
        var neighborQ = currentQ + DirectionQ(direction);
        var neighborR = currentR + DirectionR(direction);
        var neighborIndex = PathIndex(pathMinQ, pathMinR, pathWidthQ, pathWidthR, neighborQ, neighborR);
        if (neighborIndex < 0 || Scratch.PathClosed.Contains(neighborIndex))
        {
            return true;
        }

        if (!world.CellIsKnown(planeCode, neighborQ, neighborR) ||
            !PathSegmentAllowed(radius, segmentSize, segmentGridWidth, neighborQ, neighborR, firstAllowedSegment, secondAllowedSegment))
        {
            return true;
        }

        var movementCost = simulation.KernelData.Terrain.RoadAdjustedTravelCost(
            world.CellTerrainId(planeCode, neighborQ, neighborR),
            movementProfileId,
            RoadQuality(planeCode, neighborQ, neighborR, tick));
        if (float.IsInfinity(movementCost) || movementCost <= 0f)
        {
            return true;
        }

        var tentativeGScore = Scratch.PathGScore.GetOrDefault(currentIndex, float.PositiveInfinity) + movementCost;
        if (tentativeGScore >= Scratch.PathGScore.GetOrDefault(neighborIndex, float.PositiveInfinity))
        {
            return true;
        }

        Scratch.PathCameFrom.Set(neighborIndex, currentIndex);
        Scratch.PathGScore.Set(neighborIndex, tentativeGScore);
        Scratch.PathFScore.Set(neighborIndex, tentativeGScore + KernelQuery.HexDistance(neighborQ, neighborR, targetQ, targetR));
        return Scratch.PathOpen.Contains(neighborIndex) || AddPathOpenNode(neighborIndex, ref openCount);
    }

    private bool AddPathOpenNode(int neighborIndex, ref int openCount)
    {
        if (openCount >= Scratch.PathOpenSet.Length)
        {
            return false;
        }

        Scratch.PathOpenSet[openCount] = neighborIndex;
        Scratch.PathOpen.Add(neighborIndex);
        openCount++;
        return true;
    }

    private int SelectLowestPathScore(int openCount)
    {
        var selectedSlot = 0;
        var selectedIndex = Scratch.PathOpenSet[0];
        var selectedScore = Scratch.PathFScore.GetOrDefault(selectedIndex, float.PositiveInfinity);
        for (var slot = 1; slot < openCount; slot++)
        {
            var index = Scratch.PathOpenSet[slot];
            var score = Scratch.PathFScore.GetOrDefault(index, float.PositiveInfinity);
            if (score < selectedScore)
            {
                selectedSlot = slot;
                selectedIndex = index;
                selectedScore = score;
            }
        }

        return selectedSlot;
    }

    private bool TryResolveNextPathStep(
        int startIndex,
        int targetIndex,
        int pathMinQ,
        int pathMinR,
        int pathWidthR,
        out int nextQ,
        out int nextR)
    {
        var currentIndex = targetIndex;
        var parentIndex = Scratch.PathCameFrom.GetOrDefault(currentIndex, -1);
        while (parentIndex != startIndex)
        {
            if (parentIndex < 0)
            {
                nextQ = 0;
                nextR = 0;
                return false;
            }

            currentIndex = parentIndex;
            parentIndex = Scratch.PathCameFrom.GetOrDefault(currentIndex, -1);
        }

        nextQ = PathQ(pathMinQ, pathWidthR, currentIndex);
        nextR = PathR(pathMinR, pathWidthR, currentIndex);
        return true;
    }

    private static bool PathSegmentAllowed(
        int radius,
        int segmentSize,
        int segmentGridWidth,
        int q,
        int r,
        int firstAllowedSegment,
        int secondAllowedSegment)
    {
        var segment = SegmentIndex(radius, segmentSize, segmentGridWidth, q, r);
        return segment == firstAllowedSegment || segment == secondAllowedSegment;
    }

    private static int PathIndex(
        int minQ,
        int minR,
        int widthQ,
        int widthR,
        int q,
        int r)
    {
        var adjustedQ = q - minQ;
        var adjustedR = r - minR;
        if ((uint)adjustedQ >= (uint)widthQ || (uint)adjustedR >= (uint)widthR)
        {
            return -1;
        }

        return (adjustedQ * widthR) + adjustedR;
    }

    private static int PathQ(int minQ, int widthR, int index)
    {
        return (index / widthR) + minQ;
    }

    private static int PathR(int minR, int widthR, int index)
    {
        return (index % widthR) + minR;
    }

    private readonly struct SegmentRouteContext
    {
        public SegmentRouteContext(
            int radius,
            int segmentSize,
            int segmentGridWidth,
            int startSegment,
            int targetSegment)
        {
            Radius = radius;
            SegmentSize = segmentSize;
            GridWidth = segmentGridWidth;
            StartSegment = startSegment;
            TargetSegment = targetSegment;
        }

        public int Radius { get; }

        public int SegmentSize { get; }

        public int GridWidth { get; }

        public int StartSegment { get; }

        public int TargetSegment { get; }
    }
}
