namespace Engine.Kernel;

internal sealed partial class TickDeltaBuffer
{
    private void ResolveCulture(Simulation simulation)
    {
        if (_singleWorker)
        {
            ResolveCultureSingleWorker(simulation);
            return;
        }

        PrepareCulture(simulation);
        ExecuteOwnerStage(
            simulation,
            CultureOwnerStage,
            _resolvedCultureGroupCount);
        CompleteCulture(simulation);
        ExecuteOwnerStage(
            simulation,
            InfluenceCreditOwnerStage,
            _influenceCreditCount);
    }

    private void PrepareCulture(Simulation simulation)
    {
        global::System.Array.Clear(
            _cultureAppliedUnits,
            0,
            _cultureCount);
        _cultureStorageArena.Clear();
        PrepareCultureGroups(simulation.World);
        DeterministicSort.Sort(
            _resolvedCultureGroups,
            _resolvedCultureGroupCount);
    }

    private void CompleteCulture(Simulation simulation)
    {
        BuildInfluenceCreditsAndDirtyLocality(simulation);
        if (_influenceCreditCount > 0)
        {
            DeterministicSort.Sort(
                _influenceCredits,
                _influenceCreditCount);
        }
    }

    private void ResolveCultureSingleWorker(Simulation simulation)
    {
        var index = 0;
        while (index < _cultureCount)
        {
            var first = _culture[index];
            var end = index + 1;
            long sum = first.DeltaUnits;
            while (end < _cultureCount &&
                   first.SameDestination(_culture[end]))
            {
                sum = checked(sum + _culture[end].DeltaUnits);
                end++;
            }

            ApplySingleCultureGroup(
                simulation,
                index,
                end,
                ClampToInt(sum));
            index = end;
        }
    }

    private void ApplySingleCultureGroup(
        Simulation simulation,
        int start,
        int end,
        int sumUnits)
    {
        if (sumUnits == 0)
        {
            return;
        }

        var first = _culture[start];
        var world = simulation.World;
        world.PrepareOwnedCultureInfluence(
            first.CultureId,
            first.PlaneCode,
            first.Q,
            first.R,
            first.Z,
            sumUnits);
        using var scope = simulation.EnterExecutionContext(first.Context);
        var applied = world.ModifyOwnedCellCultureInfluenceRawUnits(
            first.CultureId,
            first.PlaneCode,
            first.Q,
            first.R,
            first.Z,
            sumUnits,
            simulation.CurrentTick,
            first.Context);
        world.ReconcileOwnedCultureInfluence(
            first.CultureId,
            first.PlaneCode,
            first.Q,
            first.R,
            first.Z);
        if (applied != 0 && first.Z == 0)
        {
            simulation.CultureLocality.MarkDirty(
                world,
                first.PlaneCode,
                first.Q,
                first.R);
        }

        if (applied > 0)
        {
            CreditSingleCultureSources(
                world,
                start,
                end,
                first.PlaneCode,
                applied);
        }
    }

    private void CreditSingleCultureSources(
        WorldState world,
        int start,
        int end,
        int planeCode,
        int applied)
    {
        var remaining = applied;
        for (var contribution = start;
             contribution < end && remaining > 0;
             contribution++)
        {
            var source = _culture[contribution];
            if (source.SourceEntityOccurrenceHandle <= 0 ||
                source.DeltaUnits <= 0 ||
                !world.TryResolveEntityOccurrence(
                    source.SourceEntityOccurrenceHandle,
                    out var sourceEntityId))
            {
                continue;
            }

            var credited = global::System.Math.Min(
                remaining,
                source.DeltaUnits);
            world.NoteEntityInfluenceProduction(
                sourceEntityId,
                planeCode,
                credited);
            remaining -= credited;
        }
    }

    private void PrepareCultureGroups(WorldState world)
    {
        _resolvedCultureGroupCount = 0;
        var index = 0;
        while (index < _cultureCount)
        {
            var first = _culture[index];
            var end = index + 1;
            long sum = first.DeltaUnits;
            while (end < _cultureCount &&
                   first.SameDestination(_culture[end]))
            {
                sum = checked(sum + _culture[end].DeltaUnits);
                end++;
            }

            var sumUnits = ClampToInt(sum);
            if (sumUnits == 0)
            {
                index = end;
                continue;
            }

            world.PrepareOwnedCultureInfluence(
                first.CultureId,
                first.PlaneCode,
                first.Q,
                first.R,
                first.Z,
                sumUnits);
            _resolvedCultureGroups[_resolvedCultureGroupCount++] =
                new ResolvedCultureGroup(
                    index,
                    sumUnits,
                    first.CultureId,
                    first.PlaneCode,
                    first.Q,
                    first.R,
                    first.Z);
            index = end;
        }
    }

    private void ResolveCultureOwner(
        Simulation simulation,
        int ownerIndex,
        int ownerCount)
    {
        var index = OwnerStart(
            ownerIndex,
            ownerCount,
            _resolvedCultureGroupCount,
            CultureOwnerStage);
        var limit = OwnerStart(
            ownerIndex + 1,
            ownerCount,
            _resolvedCultureGroupCount,
            CultureOwnerStage);
        while (index < limit)
        {
            ApplyCultureGroup(simulation, _resolvedCultureGroups[index++]);
        }
    }

    private void ApplyCultureGroup(
        Simulation simulation,
        in ResolvedCultureGroup group)
    {
        var first = _culture[group.OriginalStart];
        using var scope = simulation.EnterExecutionContext(first.Context);
        var applied = simulation.World.ModifyOwnedCellCultureInfluenceRawUnits(
            group.CultureId,
            group.PlaneCode,
            group.Q,
            group.R,
            group.Z,
            group.SumUnits,
            simulation.CurrentTick,
            first.Context);
        _cultureAppliedUnits[group.OriginalStart] = applied;
        if (applied != 0)
        {
            _cultureStorageArena.Add(
                AppendWorkerIndex,
                new CultureStorageKey(
                    group.CultureId,
                    group.PlaneCode,
                    group.Q,
                    group.R,
                    group.Z));
        }
    }

    private void BuildInfluenceCreditsAndDirtyLocality(
        Simulation simulation)
    {
        _influenceCreditCount = 0;
        var index = 0;
        while (index < _cultureCount)
        {
            var first = _culture[index];
            var end = index + 1;
            while (end < _cultureCount &&
                   first.SameDestination(_culture[end]))
            {
                end++;
            }

            var applied = _cultureAppliedUnits[index];
            if (applied != 0 && first.Z == 0)
            {
                simulation.CultureLocality.MarkDirty(
                    simulation.World,
                    first.PlaneCode,
                    first.Q,
                    first.R);
            }

            if (applied > 0)
            {
                BuildInfluenceCredits(
                    simulation.World,
                    index,
                    end,
                    first.PlaneCode,
                    applied);
            }

            index = end;
        }
    }

    private void BuildInfluenceCredits(
        WorldState world,
        int start,
        int end,
        int planeCode,
        int applied)
    {
        var remaining = applied;
        for (var contribution = start;
             contribution < end && remaining > 0;
             contribution++)
        {
            var source = _culture[contribution];
            if (source.SourceEntityOccurrenceHandle <= 0 ||
                source.DeltaUnits <= 0 ||
                !world.TryResolveEntityOccurrence(
                    source.SourceEntityOccurrenceHandle,
                    out var sourceEntityId))
            {
                continue;
            }

            var credited = global::System.Math.Min(
                remaining,
                source.DeltaUnits);
            _influenceCredits[_influenceCreditCount++] =
                new InfluenceCreditDelta(
                    sourceEntityId,
                    planeCode,
                    credited);
            remaining -= credited;
        }
    }

    private void ResolveInfluenceCreditOwner(
        Simulation simulation,
        int ownerIndex,
        int ownerCount)
    {
        var index = OwnerStart(
            ownerIndex,
            ownerCount,
            _influenceCreditCount,
            InfluenceCreditOwnerStage);
        var limit = OwnerStart(
            ownerIndex + 1,
            ownerCount,
            _influenceCreditCount,
            InfluenceCreditOwnerStage);
        while (index < limit)
        {
            var first = _influenceCredits[index];
            var end = index + 1;
            long sum = first.DeltaUnits;
            while (end < limit &&
                   first.SameDestination(_influenceCredits[end]))
            {
                sum = checked(sum + _influenceCredits[end].DeltaUnits);
                end++;
            }

            simulation.World.NoteEntityInfluenceProduction(
                first.EntityId,
                first.PlaneCode,
                ClampToInt(sum));
            index = end;
        }
    }

    private void ResolveCultureReductions(Simulation simulation)
    {
        if (_singleWorker)
        {
            ResolveCultureReductionsSingleWorker(simulation);
            return;
        }

        PrepareCultureReductions(simulation);
        ExecuteOwnerStage(
            simulation,
            CultureReductionOwnerStage,
            _resolvedCultureReductionGroupCount);
        CompleteCultureReductions(simulation);
    }

    private void PrepareCultureReductions(Simulation simulation)
    {
        global::System.Array.Clear(
            _cultureReductionChanged,
            0,
            _cultureReductionCount);
        PrepareCultureReductionGroups(simulation.World);
        DeterministicSort.Sort(
            _resolvedCultureReductionGroups,
            _resolvedCultureReductionGroupCount);
    }

    private void CompleteCultureReductions(Simulation simulation)
    {
        MarkCultureReductionLocalityDirty(simulation);
        ReconcileCultureStorage(simulation.World);
    }

    private void ResolveCultureReductionsSingleWorker(
        Simulation simulation)
    {
        var index = 0;
        while (index < _cultureReductionCount)
        {
            var first = _cultureReductions[index];
            var end = index + 1;
            long sum = first.DeltaUnits;
            while (end < _cultureReductionCount &&
                   first.SameDestination(_cultureReductions[end]))
            {
                sum = checked(sum + _cultureReductions[end].DeltaUnits);
                end++;
            }

            ApplySingleCultureReduction(
                simulation,
                first,
                ClampToInt(sum));
            index = end;
        }
    }

    private static void ApplySingleCultureReduction(
        Simulation simulation,
        in CultureReductionDelta delta,
        int sumUnits)
    {
        var world = simulation.World;
        if (!world.TryGetCellId(
                delta.PlaneCode,
                delta.Q,
                delta.R,
                z: 0,
                out _))
        {
            return;
        }

        using var scope = simulation.EnterExecutionContext(delta.Context);
        var scratch = simulation.DirtyCultureScratch;
        var changedCount = world.ReduceOtherCultureSourcesAtCellOwned(
            delta.OwnCultureId,
            delta.PlaneCode,
            delta.Q,
            delta.R,
            sumUnits,
            simulation.CurrentTick,
            delta.Context,
            scratch);
        if (changedCount > 0)
        {
            simulation.CultureLocality.MarkDirty(
                world,
                delta.PlaneCode,
                delta.Q,
                delta.R);
        }

        for (var changed = 0; changed < changedCount; changed++)
        {
            world.ReconcileOwnedCultureInfluence(
                scratch[changed],
                delta.PlaneCode,
                delta.Q,
                delta.R,
                z: 0);
        }
    }

    private void PrepareCultureReductionGroups(WorldState world)
    {
        _resolvedCultureReductionGroupCount = 0;
        var index = 0;
        while (index < _cultureReductionCount)
        {
            var first = _cultureReductions[index];
            var end = index + 1;
            long sum = first.DeltaUnits;
            while (end < _cultureReductionCount &&
                   first.SameDestination(_cultureReductions[end]))
            {
                sum = checked(sum + _cultureReductions[end].DeltaUnits);
                end++;
            }

            if (world.TryGetCellId(
                    first.PlaneCode,
                    first.Q,
                    first.R,
                    z: 0,
                    out _))
            {
                _resolvedCultureReductionGroups[
                    _resolvedCultureReductionGroupCount++] =
                    new ResolvedCultureReductionGroup(
                        index,
                        ClampToInt(sum),
                        first.OwnCultureId,
                        first.PlaneCode,
                        first.Q,
                        first.R);
            }

            index = end;
        }
    }

    private void ResolveCultureReductionOwner(
        Simulation simulation,
        int ownerIndex,
        int ownerCount)
    {
        var index = OwnerStart(
            ownerIndex,
            ownerCount,
            _resolvedCultureReductionGroupCount,
            CultureReductionOwnerStage);
        var limit = OwnerStart(
            ownerIndex + 1,
            ownerCount,
            _resolvedCultureReductionGroupCount,
            CultureReductionOwnerStage);
        while (index < limit)
        {
            ApplyCultureReductionGroup(
                simulation,
                _resolvedCultureReductionGroups[index++]);
        }
    }

    private void ApplyCultureReductionGroup(
        Simulation simulation,
        in ResolvedCultureReductionGroup group)
    {
        var first = _cultureReductions[group.OriginalStart];
        using var scope = simulation.EnterExecutionContext(first.Context);
        var scratch = simulation.DirtyCultureScratch;
        var changedCount =
            simulation.World.ReduceOtherCultureSourcesAtCellOwned(
                group.OwnCultureId,
                group.PlaneCode,
                group.Q,
                group.R,
                group.SumUnits,
                simulation.CurrentTick,
                first.Context,
                scratch);
        _cultureReductionChanged[group.OriginalStart] = changedCount > 0;
        for (var changed = 0; changed < changedCount; changed++)
        {
            _cultureStorageArena.Add(
                AppendWorkerIndex,
                new CultureStorageKey(
                    scratch[changed],
                    group.PlaneCode,
                    group.Q,
                    group.R,
                    Z: 0));
        }
    }

    private void MarkCultureReductionLocalityDirty(
        Simulation simulation)
    {
        var index = 0;
        while (index < _cultureReductionCount)
        {
            var first = _cultureReductions[index];
            var end = index + 1;
            while (end < _cultureReductionCount &&
                   first.SameDestination(_cultureReductions[end]))
            {
                end++;
            }

            if (_cultureReductionChanged[index])
            {
                simulation.CultureLocality.MarkDirty(
                    simulation.World,
                    first.PlaneCode,
                    first.Q,
                    first.R);
            }

            index = end;
        }
    }

    private void ReconcileCultureStorage(WorldState world)
    {
        var count = _cultureStorageArena.Compact();
        DeterministicSort.Sort(_cultureStorageKeys, count);
        var index = 0;
        while (index < count)
        {
            var key = _cultureStorageKeys[index];
            var end = index + 1;
            while (end < count && key.SameDestination(
                       _cultureStorageKeys[end]))
            {
                end++;
            }

            world.ReconcileOwnedCultureInfluence(
                key.CultureId,
                key.PlaneCode,
                key.Q,
                key.R,
                key.Z);
            index = end;
        }

        _cultureStorageArena.Clear();
    }
}
