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
        if (!world.CellIsKnown(planeCode, startQ, startR) ||
            !world.CellIsKnown(planeCode, targetQ, targetR))
        {
            return false;
        }

        var radius = world.MapRadius;
        var width = (radius * 2) + 1;
        if (width <= 0 || (long)width * width > Scratch.PathCapacity)
        {
            return false;
        }

        var startIndex = PathIndex(radius, width, startQ, startR);
        var targetIndex = PathIndex(radius, width, targetQ, targetR);
        if (startIndex < 0 || targetIndex < 0)
        {
            return false;
        }

        return SearchPath(
            simulation,
            world,
            startIndex,
            targetIndex,
            radius,
            width,
            startQ,
            startR,
            targetQ,
            targetR,
            world.EntityKindId(entityId),
            planeCode,
            tick,
            out nextQ,
            out nextR);
    }

    private bool SearchPath(
        Simulation simulation,
        WorldState world,
        int startIndex,
        int targetIndex,
        int radius,
        int width,
        int startQ,
        int startR,
        int targetQ,
        int targetR,
        int actorKindId,
        int planeCode,
        int tick,
        out int nextQ,
        out int nextR)
    {
        nextQ = startQ;
        nextR = startR;
        var openCount = InitializePathSearch(startIndex, startQ, startR, targetQ, targetR);
        while (openCount > 0)
        {
            var currentIndex = TakeLowestPathOpenNode(ref openCount);
            if (currentIndex == targetIndex)
            {
                return TryResolveNextPathStep(startIndex, targetIndex, radius, width, out nextQ, out nextR);
            }

            if (!VisitPathNeighbors(simulation, world, currentIndex, radius, width, targetQ, targetR, actorKindId, planeCode, tick, ref openCount))
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
        int radius,
        int width,
        int targetQ,
        int targetR,
        int actorKindId,
        int planeCode,
        int tick,
        ref int openCount)
    {
        var currentQ = PathQ(radius, width, currentIndex);
        var currentR = PathR(radius, width, currentIndex);
        for (var direction = 0; direction < 6; direction++)
        {
            if (!TryVisitPathNeighbor(simulation, world, currentIndex, currentQ, currentR, direction, radius, width, targetQ, targetR, actorKindId, planeCode, tick, ref openCount))
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
        int radius,
        int width,
        int targetQ,
        int targetR,
        int actorKindId,
        int planeCode,
        int tick,
        ref int openCount)
    {
        var neighborQ = currentQ + DirectionQ(direction);
        var neighborR = currentR + DirectionR(direction);
        _ = world;
        if (!CanEnter(simulation, planeCode, actorKindId, neighborQ, neighborR))
        {
            return true;
        }

        var neighborIndex = PathIndex(radius, width, neighborQ, neighborR);
        if (neighborIndex < 0 || Scratch.PathClosed.Contains(neighborIndex))
        {
            return true;
        }

        var movementCost = TravelCost(simulation, planeCode, actorKindId, neighborQ, neighborR, tick);
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
        int radius,
        int width,
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

        nextQ = PathQ(radius, width, currentIndex);
        nextR = PathR(radius, width, currentIndex);
        return true;
    }

    private int PathIndex(int radius, int width, int q, int r)
    {
        var adjustedQ = q + radius;
        var adjustedR = r + radius;
        if ((uint)adjustedQ >= (uint)width || (uint)adjustedR >= (uint)width)
        {
            return -1;
        }

        var index = (adjustedQ * width) + adjustedR;
        return (uint)index < (uint)Scratch.PathCapacity ? index : -1;
    }

    private static int PathQ(int radius, int width, int index)
    {
        return (index / width) - radius;
    }

    private static int PathR(int radius, int width, int index)
    {
        return (index % width) - radius;
    }
}
