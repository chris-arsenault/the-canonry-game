namespace Engine.Kernel;

using Engine.Eventing;

internal sealed partial class GeneratorProgramRuntime
{
    private int ResolveProducedSubtype(Simulation simulation, int producedSlot)
    {
        if (_producedSubtypeIds[producedSlot] > 0)
        {
            return _producedSubtypeIds[producedSlot];
        }

        var count = _producedSubtypeChoiceCounts[producedSlot];
        if (count == 0)
        {
            return 0;
        }

        var selected = simulation.NextInt(count);
        return _subtypeIds[_producedSubtypeChoiceStarts[producedSlot] + selected];
    }

    private bool ResolvePlacement(
        Simulation simulation,
        int anchorEntityId,
        int producedCultureId,
        int planeCode,
        int producedSlot,
        int producedSubtypeId,
        int placementCode,
        int minRadius,
        int maxRadius,
        out int q,
        out int r)
    {
        ResolveAnchorOrigin(simulation.World, anchorEntityId, planeCode, out var originQ, out var originR);
        var movementProfileId = simulation.Movement.MovementProfileFor(
            _producedKindIds[producedSlot],
            producedSubtypeId,
            planeCode);
        if (placementCode == GeneratorPlacementCodes.Locality)
        {
            return FindTerrainAwareFreeHexInCoverage(
                simulation,
                planeCode,
                producedSlot,
                movementProfileId,
                producedCultureId,
                anchorEntityId,
                originQ,
                originR,
                minRadius,
                maxRadius,
                out q,
                out r);
        }

        if (placementCode == GeneratorPlacementCodes.Frontier)
        {
            return ResolveFrontierPlacement(
                simulation, planeCode, producedSlot, movementProfileId,
                producedCultureId, originQ, originR, out q, out r);
        }

        if (placementCode == GeneratorPlacementCodes.Area)
        {
            return FindTerrainAwareHex(
                simulation,
                planeCode,
                producedSlot,
                movementProfileId,
                producedCultureId,
                originQ,
                originR,
                minRadius,
                maxRadius,
                out q,
                out r);
        }

        q = 0;
        r = 0;
        return false;
    }

    private void ResolveAnchorOrigin(
        WorldState world,
        int anchorEntityId,
        int planeCode,
        out int originQ,
        out int originR)
    {
        if (IsPendingRef(anchorEntityId))
        {
            var planIndex = PendingPlanIndex(anchorEntityId);
            originQ = PlannedEntityQ(planIndex, planeCode);
            originR = PlannedEntityR(planIndex, planeCode);
            return;
        }

        originQ = planeCode == PlaneCodes.Physical
            ? world.EntityPhysicalQ(anchorEntityId)
            : world.EntitySemanticQ(anchorEntityId);
        originR = planeCode == PlaneCodes.Physical
            ? world.EntityPhysicalR(anchorEntityId)
            : world.EntitySemanticR(anchorEntityId);
    }

    // Place at the leading edge of the produced entity's culture so the entity
    // becomes a new anchor that pushes the frontier outward. Fall back to a
    // near-origin search when the culture has no reachable edge (e.g. no mass
    // yet), so the produced entity still lands somewhere.
    private bool ResolveFrontierPlacement(
        Simulation simulation,
        int planeCode,
        int producedSlot,
        int movementProfileId,
        int producedCultureId,
        int originQ,
        int originR,
        out int q,
        out int r)
    {
        if (FindTerrainAwareHexAtCultureFrontier(
                simulation,
                planeCode,
                producedSlot,
                movementProfileId,
                producedCultureId,
                out q,
                out r))
        {
            return true;
        }

        return FindTerrainAwareHex(
            simulation,
            planeCode,
            producedSlot,
            movementProfileId,
            producedCultureId,
            originQ,
            originR,
            GeneratorProgramConstants.DefaultPlacementMinRadius,
            GeneratorProgramConstants.DefaultPlacementMaxRadius,
            out q,
            out r);
    }

    private bool ResolveProducedPlacement(
        Simulation simulation,
        int anchorEntityId,
        int producedCultureId,
        int produced,
        int producedSubtypeId,
        out int physicalQ,
        out int physicalR,
        out int semanticQ,
        out int semanticR)
    {
        if (!ResolvePlacement(
                simulation,
                anchorEntityId,
                producedCultureId,
                PlaneCodes.Physical,
                produced,
                producedSubtypeId,
                _producedPhysicalPlacementCodes[produced],
                _producedPhysicalPlacementMinRadii[produced],
                _producedPhysicalPlacementMaxRadii[produced],
                out physicalQ,
                out physicalR))
        {
            semanticQ = 0;
            semanticR = 0;
            return false;
        }

        return ResolvePlacement(
            simulation,
            anchorEntityId,
            producedCultureId,
            PlaneCodes.Semantic,
            produced,
            producedSubtypeId,
            _producedSemanticPlacementCodes[produced],
            _producedSemanticPlacementMinRadii[produced],
            _producedSemanticPlacementMaxRadii[produced],
            out semanticQ,
            out semanticR);
    }

    private bool PlanCoverage(
        Simulation simulation,
        int producedSlot,
        int ownerCultureId,
        int[] refCodes,
        int[] refEntityIds,
        int refCount,
        int originQ,
        int originR,
        out int anchorQ,
        out int anchorR,
        out int coverageCellCount)
    {
        anchorQ = originQ;
        anchorR = originR;
        coverageCellCount = 0;
        var world = simulation.World;
        var sourceOwnerId = 0;
        if (_producedCoverageModeCodes[producedSlot] == GeneratorCoverageModeCodes.SplitFromExisting)
        {
            if (_producedCoverageSourceRefCodes[producedSlot] > 0)
            {
                sourceOwnerId = ResolveRef(refCodes, refEntityIds, refCount, _producedCoverageSourceRefCodes[producedSlot]);
            }

            if (sourceOwnerId == 0)
            {
                sourceOwnerId = world.CoverageOwnerAt(originQ, originR);
            }

            // A pending same-fire entity owns no committed coverage to split.
            if (sourceOwnerId <= 0)
            {
                return false;
            }
        }

        if (!FindCoverageSeedCell(simulation, producedSlot, ownerCultureId, originQ, originR, sourceOwnerId, out anchorQ, out anchorR))
        {
            return false;
        }

        var claimedCount = BuildCoveragePatch(simulation, producedSlot, ownerCultureId, anchorQ, anchorR, sourceOwnerId);
        if (claimedCount < _producedCoverageMinimumCellCounts[producedSlot])
        {
            return false;
        }

        if (_plannedCoverageCellCount + claimedCount > _plannedCoverageCellQ.Length)
        {
            throw new global::System.InvalidOperationException();
        }

        for (var i = 0; i < claimedCount; i++)
        {
            _plannedCoverageCellQ[_plannedCoverageCellCount] = _scratchClaimedQ[i];
            _plannedCoverageCellR[_plannedCoverageCellCount] = _scratchClaimedR[i];
            _plannedCoverageCellCount++;
        }

        coverageCellCount = claimedCount;
        return true;
    }

    private void CommitPlannedCoverage(Simulation simulation, int planIndex, int ownerEntityId)
    {
        var producedSlot = _plannedProducedSlots[planIndex];
        var start = _plannedCoverageStarts[planIndex];
        var end = start + _plannedCoverageCounts[planIndex];
        for (var cell = start; cell < end; cell++)
        {
            var q = _plannedCoverageCellQ[cell];
            var r = _plannedCoverageCellR[cell];
            if (_producedCoverageTransformTerrainIds[producedSlot] > 0)
            {
                KernelEffectExecutor.SetCellState(
                    simulation,
                    q,
                    r,
                    _producedCoverageTransformTerrainIds[producedSlot],
                    ownerEntityId);
            }
            else
            {
                KernelEffectExecutor.SetCoverageOwner(simulation, q, r, ownerEntityId);
            }
        }
    }

    private bool FindCoverageSeedCell(
        Simulation simulation,
        int producedSlot,
        int producedCultureId,
        int originQ,
        int originR,
        int sourceOwnerId,
        out int seedQ,
        out int seedR)
    {
        var world = simulation.World;
        var boundaryGroupId = 0;
        if (world.CellIsKnown(originQ, originR))
        {
            boundaryGroupId = world.TerrainBoundaryGroupId(world.CellTerrainId(originQ, originR));
        }

        if (SearchCoverageSeedCell(simulation, producedSlot, producedCultureId, originQ, originR, sourceOwnerId, boundaryGroupId, out seedQ, out seedR))
        {
            return true;
        }

        if (_producedCoverageRespectTerrainBoundaries[producedSlot] == 0 || boundaryGroupId == 0)
        {
            return false;
        }

        return SearchCoverageSeedCell(simulation, producedSlot, producedCultureId, originQ, originR, sourceOwnerId, boundaryGroupId: 0, out seedQ, out seedR);
    }

    private bool SearchCoverageSeedCell(
        Simulation simulation,
        int producedSlot,
        int producedCultureId,
        int originQ,
        int originR,
        int sourceOwnerId,
        int boundaryGroupId,
        out int seedQ,
        out int seedR)
    {
        seedQ = 0;
        seedR = 0;
        var bestScore = int.MaxValue;
        var found = false;
        var world = simulation.World;
        for (var radius = 0; radius <= world.PhysicalMapRadius; radius++)
        {
            found = TrySearchCoverageSeedRadius(
                    simulation,
                    producedSlot,
                    producedCultureId,
                    originQ,
                    originR,
                    radius,
                    sourceOwnerId,
                    boundaryGroupId,
                    ref bestScore,
                    ref seedQ,
                    ref seedR) || found;
        }

        return found;
    }

    private bool TrySearchCoverageSeedRadius(
        Simulation simulation,
        int producedSlot,
        int producedCultureId,
        int originQ,
        int originR,
        int radius,
        int sourceOwnerId,
        int boundaryGroupId,
        ref int bestScore,
        ref int seedQ,
        ref int seedR)
    {
        var found = false;
        for (var dq = -radius; dq <= radius; dq++)
        {
            found = SearchCoverageSeedColumn(
                simulation,
                producedSlot,
                producedCultureId,
                originQ,
                originR,
                radius,
                dq,
                sourceOwnerId,
                boundaryGroupId,
                ref bestScore,
                ref seedQ,
                ref seedR) || found;
        }

        return found;
    }

    private bool SearchCoverageSeedColumn(
        Simulation simulation,
        int producedSlot,
        int producedCultureId,
        int originQ,
        int originR,
        int radius,
        int dq,
        int sourceOwnerId,
        int boundaryGroupId,
        ref int bestScore,
        ref int seedQ,
        ref int seedR)
    {
        var found = false;
        var minDr = global::System.Math.Max(-radius, -dq - radius);
        var maxDr = global::System.Math.Min(radius, -dq + radius);
        for (var dr = minDr; dr <= maxDr; dr++)
        {
            found = SelectCoverageSeedCandidate(
                simulation,
                producedSlot,
                producedCultureId,
                originQ,
                originR,
                radius,
                dq,
                dr,
                sourceOwnerId,
                boundaryGroupId,
                ref bestScore,
                ref seedQ,
                ref seedR) || found;
        }

        return found;
    }

    private bool SelectCoverageSeedCandidate(
        Simulation simulation,
        int producedSlot,
        int producedCultureId,
        int originQ,
        int originR,
        int radius,
        int dq,
        int dr,
        int sourceOwnerId,
        int boundaryGroupId,
        ref int bestScore,
        ref int seedQ,
        ref int seedR)
    {
        var q = originQ + dq;
        var r = originR + dr;
        var world = simulation.World;
        if (!CoverageSeedCandidateMatches(world, producedSlot, q, r, originQ, originR, radius, sourceOwnerId, boundaryGroupId))
        {
            return false;
        }

        if (!PlacementCellAllowsCulture(simulation, PlaneCodes.Physical, producedSlot, producedCultureId, q, r))
        {
            return false;
        }

        var score = ScoreCoverageCell(simulation, producedSlot, producedCultureId, q, r, originQ, originR, boundaryGroupId);
        if (score >= bestScore)
        {
            return true;
        }

        bestScore = score;
        seedQ = q;
        seedR = r;
        return true;
    }

    private bool CoverageSeedCandidateMatches(
        WorldState world,
        int producedSlot,
        int q,
        int r,
        int originQ,
        int originR,
        int radius,
        int sourceOwnerId,
        int boundaryGroupId)
    {
        return KernelQuery.HexDistance(originQ, originR, q, r) == radius &&
            CoverageCellIsEligible(world, producedSlot, q, r, sourceOwnerId, boundaryGroupId, alreadyClaimedCount: 0);
    }

    private int BuildCoveragePatch(
        Simulation simulation,
        int producedSlot,
        int producedCultureId,
        int anchorQ,
        int anchorR,
        int sourceOwnerId)
    {
        var world = simulation.World;
        var frontierCount = 1;
        var visitedCount = 0;
        var claimedCount = 0;
        _scratchQ[0] = anchorQ;
        _scratchR[0] = anchorR;
        var boundaryGroupId = world.TerrainBoundaryGroupId(world.CellTerrainId(anchorQ, anchorR));
        var desiredCount = _producedCoverageDesiredCellCounts[producedSlot];
        while (frontierCount > 0 && claimedCount < desiredCount)
        {
            var selected = SelectBestFrontierCell(
                simulation,
                producedSlot,
                producedCultureId,
                frontierCount,
                anchorQ,
                anchorR,
                boundaryGroupId);
            var currentQ = _scratchQ[selected];
            var currentR = _scratchR[selected];
            RemoveCoverageFrontierCell(selected, ref frontierCount);

            if (!TryClaimCoveragePatchCell(
                    simulation,
                    producedSlot,
                    producedCultureId,
                    sourceOwnerId,
                    boundaryGroupId,
                    currentQ,
                    currentR,
                    ref visitedCount,
                    ref claimedCount))
            {
                continue;
            }

            AddCoveragePatchNeighbors(world, currentQ, currentR, visitedCount, ref frontierCount);
        }

        return claimedCount;
    }

    private void RemoveCoverageFrontierCell(int selected, ref int frontierCount)
    {
        frontierCount--;
        _scratchQ[selected] = _scratchQ[frontierCount];
        _scratchR[selected] = _scratchR[frontierCount];
    }

    private bool TryClaimCoveragePatchCell(
        Simulation simulation,
        int producedSlot,
        int producedCultureId,
        int sourceOwnerId,
        int boundaryGroupId,
        int currentQ,
        int currentR,
        ref int visitedCount,
        ref int claimedCount)
    {
        if (ScratchContains(_scratchVisitedQ, _scratchVisitedR, visitedCount, currentQ, currentR))
        {
            return false;
        }

        _scratchVisitedQ[visitedCount] = currentQ;
        _scratchVisitedR[visitedCount] = currentR;
        visitedCount++;
        if (!CoverageCellCanBeClaimed(
                simulation,
                producedSlot,
                producedCultureId,
                sourceOwnerId,
                boundaryGroupId,
                currentQ,
                currentR,
                claimedCount))
        {
            return false;
        }

        _scratchClaimedQ[claimedCount] = currentQ;
        _scratchClaimedR[claimedCount] = currentR;
        claimedCount++;
        return true;
    }

    private bool CoverageCellCanBeClaimed(
        Simulation simulation,
        int producedSlot,
        int producedCultureId,
        int sourceOwnerId,
        int boundaryGroupId,
        int q,
        int r,
        int claimedCount) =>
        CoverageCellIsEligible(simulation.World, producedSlot, q, r, sourceOwnerId, boundaryGroupId, claimedCount) &&
        PlacementCellAllowsCulture(simulation, PlaneCodes.Physical, producedSlot, producedCultureId, q, r);

    private void AddCoveragePatchNeighbors(
        WorldState world,
        int currentQ,
        int currentR,
        int visitedCount,
        ref int frontierCount)
    {
        for (var direction = 0; direction < 6; direction++)
        {
            var q = currentQ + DirectionQ(direction);
            var r = currentR + DirectionR(direction);
            AddCoveragePatchNeighborIfEligible(world, q, r, visitedCount, ref frontierCount);
        }
    }

    private void AddCoveragePatchNeighborIfEligible(
        WorldState world,
        int q,
        int r,
        int visitedCount,
        ref int frontierCount)
    {
        if (frontierCount >= _scratchQ.Length)
        {
            throw new global::System.InvalidOperationException();
        }

        if (!world.CellIsKnown(q, r) ||
            ScratchContains(_scratchVisitedQ, _scratchVisitedR, visitedCount, q, r))
        {
            return;
        }

        _scratchQ[frontierCount] = q;
        _scratchR[frontierCount] = r;
        frontierCount++;
    }

    private int SelectBestFrontierCell(
        Simulation simulation,
        int producedSlot,
        int producedCultureId,
        int frontierCount,
        int anchorQ,
        int anchorR,
        int boundaryGroupId)
    {
        var best = 0;
        var bestScore = int.MaxValue;
        for (var i = 0; i < frontierCount; i++)
        {
            var score = ScoreCoverageCell(
                simulation,
                producedSlot,
                producedCultureId,
                _scratchQ[i],
                _scratchR[i],
                anchorQ,
                anchorR,
                boundaryGroupId);
            _scratchScore[i] = score;
            if (score < bestScore)
            {
                best = i;
                bestScore = score;
            }
        }

        return best;
    }

    private bool CoverageCellIsEligible(
        WorldState world,
        int producedSlot,
        int q,
        int r,
        int sourceOwnerId,
        int boundaryGroupId,
        int alreadyClaimedCount)
    {
        if (!world.CellIsKnown(q, r) || PendingCoverageClaims(q, r))
        {
            return false;
        }

        if (!CoverageModeAllowsCell(world, producedSlot, q, r, sourceOwnerId, alreadyClaimedCount))
        {
            return false;
        }

        var terrainId = world.CellTerrainId(q, r);
        if (!TerrainInFilter(terrainId, _producedCoverageAllowedTerrainStarts[producedSlot], _producedCoverageAllowedTerrainCounts[producedSlot], emptyPasses: true))
        {
            return false;
        }

        return _producedCoverageRespectTerrainBoundaries[producedSlot] == 0 ||
               boundaryGroupId == 0 ||
               world.TerrainBoundaryGroupId(terrainId) == boundaryGroupId;
    }

    private bool CoverageModeAllowsCell(
        WorldState world,
        int producedSlot,
        int q,
        int r,
        int sourceOwnerId,
        int alreadyClaimedCount)
    {
        var mode = _producedCoverageModeCodes[producedSlot];
        if (mode == GeneratorCoverageModeCodes.CreateNew)
        {
            return world.CoverageOwnerAt(q, r) == 0;
        }

        return mode != GeneratorCoverageModeCodes.SplitFromExisting ||
            SplitCoverageAllowsCell(world, producedSlot, q, r, sourceOwnerId, alreadyClaimedCount);
    }

    private bool SplitCoverageAllowsCell(
        WorldState world,
        int producedSlot,
        int q,
        int r,
        int sourceOwnerId,
        int alreadyClaimedCount)
    {
        if (sourceOwnerId == 0 || world.CoverageOwnerAt(q, r) != sourceOwnerId)
        {
            return false;
        }

        var remaining = world.CoverageOwnerCellCount(sourceOwnerId) - alreadyClaimedCount - 1;
        return remaining >= _producedCoverageMinimumSourceRemainingCellCounts[producedSlot] &&
            RemainingCoverageIsContiguous(world, sourceOwnerId, q, r, alreadyClaimedCount);
    }

    private bool RemainingCoverageIsContiguous(
        WorldState world,
        int sourceOwnerId,
        int removedQ,
        int removedR,
        int alreadyClaimedCount)
    {
        var remainingCount = CountRemainingCoverageCells(
            world,
            sourceOwnerId,
            removedQ,
            removedR,
            alreadyClaimedCount,
            out var firstQ,
            out var firstR);
        if (remainingCount == 0)
        {
            return false;
        }

        var visitedCount = 1;
        var queueHead = 0;
        var queueCount = 1;
        _scratchConnectivityQ[0] = firstQ;
        _scratchConnectivityR[0] = firstR;
        _scratchConnectivityQueueQ[0] = firstQ;
        _scratchConnectivityQueueR[0] = firstR;

        while (queueHead < queueCount)
        {
            var currentQ = _scratchConnectivityQueueQ[queueHead];
            var currentR = _scratchConnectivityQueueR[queueHead];
            queueHead++;

            AddCoverageConnectivityNeighbors(
                world,
                sourceOwnerId,
                removedQ,
                removedR,
                alreadyClaimedCount,
                currentQ,
                currentR,
                ref visitedCount,
                ref queueCount);
        }

        return visitedCount == remainingCount;
    }

    private int CountRemainingCoverageCells(
        WorldState world,
        int sourceOwnerId,
        int removedQ,
        int removedR,
        int alreadyClaimedCount,
        out int firstQ,
        out int firstR)
    {
        firstQ = 0;
        firstR = 0;
        var selectedFirstQ = 0;
        var selectedFirstR = 0;
        var remainingCount = 0;
        KernelQuery.ForEachCoverageCell(
            world,
            sourceOwnerId,
            (q, r) =>
            {
                if (CoverageRemovalContains(q, r, removedQ, removedR, alreadyClaimedCount))
                {
                    return true;
                }

                if (remainingCount == 0)
                {
                    selectedFirstQ = q;
                    selectedFirstR = r;
                }

                remainingCount++;
                return true;
            });

        firstQ = selectedFirstQ;
        firstR = selectedFirstR;
        return remainingCount;
    }

    private void AddCoverageConnectivityNeighbors(
        WorldState world,
        int sourceOwnerId,
        int removedQ,
        int removedR,
        int alreadyClaimedCount,
        int currentQ,
        int currentR,
        ref int visitedCount,
        ref int queueCount)
    {
        for (var direction = 0; direction < 6; direction++)
        {
            var neighborQ = currentQ + DirectionQ(direction);
            var neighborR = currentR + DirectionR(direction);
            if (!CoverageNeighborIsReachable(world, sourceOwnerId, removedQ, removedR, alreadyClaimedCount, neighborQ, neighborR, visitedCount))
            {
                continue;
            }

            AddCoverageConnectivityNeighbor(neighborQ, neighborR, ref visitedCount, ref queueCount);
        }
    }

    private bool CoverageNeighborIsReachable(
        WorldState world,
        int sourceOwnerId,
        int removedQ,
        int removedR,
        int alreadyClaimedCount,
        int neighborQ,
        int neighborR,
        int visitedCount)
    {
        return world.CoverageOwnerAt(neighborQ, neighborR) == sourceOwnerId &&
            !CoverageRemovalContains(neighborQ, neighborR, removedQ, removedR, alreadyClaimedCount) &&
            !ScratchContains(_scratchConnectivityQ, _scratchConnectivityR, visitedCount, neighborQ, neighborR);
    }

    private void AddCoverageConnectivityNeighbor(
        int neighborQ,
        int neighborR,
        ref int visitedCount,
        ref int queueCount)
    {
        if (visitedCount >= _scratchConnectivityQ.Length || queueCount >= _scratchConnectivityQueueQ.Length)
        {
            throw new global::System.InvalidOperationException();
        }

        _scratchConnectivityQ[visitedCount] = neighborQ;
        _scratchConnectivityR[visitedCount] = neighborR;
        visitedCount++;
        _scratchConnectivityQueueQ[queueCount] = neighborQ;
        _scratchConnectivityQueueR[queueCount] = neighborR;
        queueCount++;
    }

    private bool CoverageRemovalContains(int q, int r, int removedQ, int removedR, int alreadyClaimedCount)
    {
        if (q == removedQ && r == removedR)
        {
            return true;
        }

        return ScratchContains(_scratchClaimedQ, _scratchClaimedR, alreadyClaimedCount, q, r);
    }

    private int ScoreCoverageCell(
        Simulation simulation,
        int producedSlot,
        int producedCultureId,
        int q,
        int r,
        int originQ,
        int originR,
        int boundaryGroupId)
    {
        var world = simulation.World;
        if (!world.CellIsKnown(q, r))
        {
            return int.MaxValue;
        }

        var score = KernelQuery.HexDistance(originQ, originR, q, r) * 10;
        var terrainId = world.CellTerrainId(q, r);
        if (!TerrainInFilter(terrainId, _producedCoveragePreferredTerrainStarts[producedSlot], _producedCoveragePreferredTerrainCounts[producedSlot], emptyPasses: true))
        {
            score += 20;
        }

        if (_producedCoverageRespectTerrainBoundaries[producedSlot] != 0 &&
            boundaryGroupId != 0 &&
            world.TerrainBoundaryGroupId(terrainId) != boundaryGroupId)
        {
            score += 100;
        }

        score -= CoverageCulturePlacementDiscount(simulation, producedSlot, producedCultureId, q, r);
        return score;
    }

    private int CoverageCulturePlacementDiscount(
        Simulation simulation,
        int producedSlot,
        int producedCultureId,
        int q,
        int r)
    {
        var score = CulturePlacementScore(simulation, PlaneCodes.Physical, producedSlot, producedCultureId, q, r);
        if (score <= 0)
        {
            return score / 8;
        }

        return global::System.Math.Min(60, score / 8);
    }

    private bool TerrainInFilter(int terrainId, int start, int count, bool emptyPasses)
    {
        if (count == 0)
        {
            return emptyPasses;
        }

        var end = start + count;
        for (var i = start; i < end; i++)
        {
            if (_terrainFilterIds[i] == terrainId)
            {
                return true;
            }
        }

        return false;
    }

    private int EffectivePlacementMaxRadius(WorldState world, int planeCode, int maxRadius)
    {
        if (maxRadius != GeneratorProgramConstants.UnboundedPlacementMaxRadius)
        {
            return maxRadius;
        }

        return planeCode == PlaneCodes.Physical
            ? world.PhysicalMapRadius
            : EffectiveSemanticRadius(world);
    }

    private bool SemanticCoordinateAllowed(WorldState world, int q, int r) =>
        world.CellIsKnown(PlaneCodes.Semantic, q, r);

    private static int EffectiveSemanticRadius(WorldState world) =>
        world.SemanticMapRadius;

}
