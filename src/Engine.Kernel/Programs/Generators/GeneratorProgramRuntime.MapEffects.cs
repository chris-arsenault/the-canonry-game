namespace Engine.Kernel;

internal sealed partial class GeneratorProgramRuntime
{
    private void ExecuteSetTerrainPatch(
        Simulation simulation,
        WorldState world,
        int effectSlot,
        int[] refCodes,
        int[] refEntityIds,
        int refCount)
    {
        if (_effectValues[effectSlot] < 1 && simulation.NextScalar() > _effectValues[effectSlot])
        {
            return;
        }

        var centerEntityId = ResolveRef(refCodes, refEntityIds, refCount, _effectRefOrKindIds[effectSlot]);
        var terrainId = _effectOptionCodes[effectSlot];
        if (centerEntityId == 0 || terrainId == 0)
        {
            return;
        }

        var planeCode = _effectPlaneCodes[effectSlot];
        var centerQ = CurrentQ(world, centerEntityId, planeCode);
        var centerR = CurrentR(world, centerEntityId, planeCode);
        if (!world.CellIsKnown(planeCode, centerQ, centerR))
        {
            return;
        }

        var candidateCount = BuildTerrainPatchCandidates(world, effectSlot, planeCode, centerQ, centerR);
        ApplyTerrainPatchCandidates(simulation, effectSlot, planeCode, terrainId, candidateCount);
    }

    private int BuildTerrainPatchCandidates(
        WorldState world,
        int effectSlot,
        int planeCode,
        int centerQ,
        int centerR)
    {
        var radius = _effectRadii[effectSlot];
        var candidateCount = 0;
        var boundaryGroupId = world.TerrainBoundaryGroupId(world.CellTerrainId(planeCode, centerQ, centerR));
        for (var dq = -radius; dq <= radius; dq++)
        {
            var minDr = global::System.Math.Max(-radius, -dq - radius);
            var maxDr = global::System.Math.Min(radius, -dq + radius);
            for (var dr = minDr; dr <= maxDr; dr++)
            {
                var q = centerQ + dq;
                var r = centerR + dr;
                if (!TerrainPatchCandidateIsEligible(world, effectSlot, planeCode, q, r, boundaryGroupId))
                {
                    continue;
                }

                if (candidateCount >= _scratchQ.Length)
                {
                    throw new global::System.InvalidOperationException();
                }

                _scratchQ[candidateCount] = q;
                _scratchR[candidateCount] = r;
                _scratchScore[candidateCount] = ScoreTerrainPatchCandidate(world, effectSlot, planeCode, q, r, centerQ, centerR);
                candidateCount++;
            }
        }

        return candidateCount;
    }

    private bool TerrainPatchCandidateIsEligible(
        WorldState world,
        int effectSlot,
        int planeCode,
        int q,
        int r,
        int boundaryGroupId)
    {
        if (!world.CellIsKnown(planeCode, q, r))
        {
            return false;
        }

        var terrainId = world.CellTerrainId(planeCode, q, r);
        if (!TerrainInFilter(terrainId, _effectAllowedTerrainStarts[effectSlot], _effectAllowedTerrainCounts[effectSlot], emptyPasses: true))
        {
            return false;
        }

        return _effectInstanceSelectionCodes[effectSlot] == 0 ||
            boundaryGroupId == 0 ||
            world.TerrainBoundaryGroupId(terrainId) == boundaryGroupId;
    }

    private int ScoreTerrainPatchCandidate(
        WorldState world,
        int effectSlot,
        int planeCode,
        int q,
        int r,
        int centerQ,
        int centerR)
    {
        var score = KernelQuery.HexDistance(centerQ, centerR, q, r) * 10;
        if (!TerrainInFilter(world.CellTerrainId(planeCode, q, r), _effectPreferredTerrainStarts[effectSlot], _effectPreferredTerrainCounts[effectSlot], emptyPasses: true))
        {
            score += 20;
        }

        return score;
    }

    private void ApplyTerrainPatchCandidates(
        Simulation simulation,
        int effectSlot,
        int planeCode,
        int terrainId,
        int candidateCount)
    {
        var limit = _effectMaxCellCounts[effectSlot] <= 0
            ? candidateCount
            : global::System.Math.Min(candidateCount, _effectMaxCellCounts[effectSlot]);
        for (var applied = 0; applied < limit; applied++)
        {
            var selected = SelectBestTerrainPatchCandidate(candidateCount);
            KernelEffectExecutor.SetCellTerrain(
                simulation,
                planeCode,
                _scratchQ[selected],
                _scratchR[selected],
                terrainId);
            candidateCount--;
            _scratchQ[selected] = _scratchQ[candidateCount];
            _scratchR[selected] = _scratchR[candidateCount];
            _scratchScore[selected] = _scratchScore[candidateCount];
        }
    }

    private int SelectBestTerrainPatchCandidate(int candidateCount)
    {
        var selected = 0;
        var selectedScore = _scratchScore[0];
        for (var i = 1; i < candidateCount; i++)
        {
            if (_scratchScore[i] < selectedScore)
            {
                selected = i;
                selectedScore = _scratchScore[i];
            }
        }

        return selected;
    }
}
