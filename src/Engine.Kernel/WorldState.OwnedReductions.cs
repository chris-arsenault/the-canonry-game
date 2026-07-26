namespace Engine.Kernel;

internal sealed partial class WorldState
{
    internal void PrepareOwnedCultureInfluence(
        int cultureId,
        int planeCode,
        int q,
        int r,
        int z,
        int deltaUnits)
    {
        EnsureCultureIndex(cultureId);
        ValidateInfluencePlaneCode(planeCode);
        ValidateCellZ(z);
        if (deltaUnits == 0)
        {
            return;
        }

        var cellId = _cells.GetOrAdd(planeCode, q, r, z);
        var previous = PositiveCultureInfluenceRawUnits(cultureId, cellId);
        var current = KernelFixedPoint.SaturatingAdd(
            previous,
            deltaUnits,
            0,
            CultureFullRawMassUnits);
        if (current > 0)
        {
            _mapLayers.RawCultureInfluence.EnsureRowForOwnedWrite(
                cultureId,
                cellId);
        }

        _cultureHighWatermark = global::System.Math.Max(
            _cultureHighWatermark,
            cultureId);
    }

    internal int ModifyOwnedCellCultureInfluenceRawUnits(
        int cultureId,
        int planeCode,
        int q,
        int r,
        int z,
        int deltaUnits,
        int tick,
        in KernelExecutionContext context)
    {
        EnsureCultureIndex(cultureId);
        ValidateInfluencePlaneCode(planeCode);
        ValidateCellZ(z);
        ValidatePositive(context.SourceCodeId);
        if (deltaUnits == 0)
        {
            return 0;
        }

        var cellId = _cells.Require(planeCode, q, r, z);
        var previous = PositiveCultureInfluenceRawUnits(cultureId, cellId);
        var current = KernelFixedPoint.SaturatingAdd(
            previous,
            deltaUnits,
            0,
            CultureFullRawMassUnits);
        if (current == previous)
        {
            return 0;
        }

        var applied = current - previous;
        _mapLayers.RawCultureInfluence.SetOwnedValue(
            cultureId,
            cellId,
            current);
        _mapLayers.AddTotalRawCultureInfluence(cellId, applied);
        if (planeCode == PlaneCodes.Physical)
        {
            AddCheckedAtomic(ref _totalPhysicalCultureRawMass, applied);
        }

        MarkCultureFieldDirty(cultureId);
        _ = tick;
        return applied;
    }

    internal int ReduceOtherCultureSourcesAtCellOwned(
        int ownCultureId,
        int planeCode,
        int q,
        int r,
        int deltaUnits,
        int tick,
        in KernelExecutionContext context,
        global::System.Span<int> changedCultureIds)
    {
        ValidatePositive(ownCultureId);
        ValidateInfluencePlaneCode(planeCode);
        ValidatePositive(context.SourceCodeId);
        if (deltaUnits <= 0)
        {
            return 0;
        }

        var cellId = _cells.Require(planeCode, q, r, z: 0);
        var changedCultureCount = 0;
        for (var cultureId = 1;
             cultureId <= CultureHighWatermark;
             cultureId++)
        {
            if (cultureId == ownCultureId)
            {
                continue;
            }

            var influence = PositiveCultureInfluenceRawUnits(
                cultureId,
                cellId);
            if (influence <= 0)
            {
                continue;
            }

            var appliedDelta = global::System.Math.Min(
                deltaUnits,
                influence);
            var applied = ModifyOwnedCellCultureInfluenceRawUnits(
                cultureId,
                planeCode,
                q,
                r,
                z: 0,
                -appliedDelta,
                tick,
                context);
            if (applied == 0)
            {
                continue;
            }

            changedCultureIds[changedCultureCount++] = cultureId;
        }

        return changedCultureCount;
    }

    internal void ReconcileOwnedCultureInfluence(
        int cultureId,
        int planeCode,
        int q,
        int r,
        int z)
    {
        var cellId = _cells.Require(planeCode, q, r, z);
        _mapLayers.RawCultureInfluence.ReconcileOwnedValue(
            cultureId,
            cellId);
    }

    private void AddCheckedAtomic(ref long target, int delta)
    {
        if (_singleWorker)
        {
            target = checked(target + delta);
            return;
        }

        var current = global::System.Threading.Volatile.Read(ref target);
        while (true)
        {
            var next = checked(current + delta);
            var observed =
                global::System.Threading.Interlocked.CompareExchange(
                    ref target,
                    next,
                    current);
            if (observed == current)
            {
                return;
            }

            current = observed;
        }
    }
}
