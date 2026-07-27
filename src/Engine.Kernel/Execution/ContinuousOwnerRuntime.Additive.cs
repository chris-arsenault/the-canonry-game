namespace Engine.Kernel;

internal sealed partial class ContinuousOwnerRuntime
{
    private bool DrainAttributes(int ownerIndex)
    {
        var cursor = _attributes.BeginDrain(ownerIndex);
        if (!_attributes.MoveNext(ref cursor, out var combined))
        {
            return false;
        }

        long sum = combined.DeltaUnits;
        while (_attributes.MoveNext(ref cursor, out var delta))
        {
            if (combined.SameDestination(delta))
            {
                sum = checked(sum + delta.DeltaUnits);
                continue;
            }

            ApplyAttributeDelta(combined with
            {
                DeltaUnits = ClampMessageSum(sum),
            });
            combined = delta;
            sum = delta.DeltaUnits;
        }

        ApplyAttributeDelta(combined with
        {
            DeltaUnits = ClampMessageSum(sum),
        });
        return true;
    }

    private void ApplyAttributeDelta(in AttributeDelta delta)
    {
        if (!_simulation.World.TryResolveEntityOccurrence(
                delta.EntityOccurrenceHandle,
                out var entityId))
        {
            return;
        }

        using var scope =
            _simulation.EnterExecutionContext(delta.Context);
        _simulation.World.ModifyAttributeUnits(
            entityId,
            delta.AttributeId,
            delta.DeltaUnits,
            KernelWorkerThread.OwnerTick,
            delta.Context);
    }

    private bool DrainRelationships(int ownerIndex)
    {
        var cursor = _relationships.BeginDrain(ownerIndex);
        if (!_relationships.MoveNext(ref cursor, out var combined))
        {
            return false;
        }

        long sum = combined.DeltaUnits;
        while (_relationships.MoveNext(ref cursor, out var delta))
        {
            if (combined.SameDestination(delta))
            {
                sum = checked(sum + delta.DeltaUnits);
                continue;
            }

            ApplyRelationshipDelta(combined with
            {
                DeltaUnits = ClampMessageSum(sum),
            });
            combined = delta;
            sum = delta.DeltaUnits;
        }

        ApplyRelationshipDelta(combined with
        {
            DeltaUnits = ClampMessageSum(sum),
        });
        return true;
    }

    private void ApplyRelationshipDelta(in RelationshipDelta delta)
    {
        if (!_simulation.World.TryResolveRelationshipOccurrence(
                delta.RelationshipOccurrenceHandle,
                out var relationshipId))
        {
            return;
        }

        using var scope =
            _simulation.EnterExecutionContext(delta.Context);
        _simulation.World.ModifyRelationshipStrengthUnits(
            relationshipId,
            delta.DeltaUnits,
            KernelWorkerThread.OwnerTick,
            delta.Context);
    }

    private bool DrainCulture(int ownerIndex)
    {
        var cursor = _culture.BeginDrain(ownerIndex);
        if (!_culture.MoveNext(ref cursor, out var combined))
        {
            return false;
        }

        long sum = combined.DeltaUnits;
        while (_culture.MoveNext(ref cursor, out var delta))
        {
            if (combined.SameSourceDestination(delta))
            {
                sum = checked(sum + delta.DeltaUnits);
                continue;
            }

            ApplyCultureDelta(combined with
            {
                DeltaUnits = ClampMessageSum(sum),
            });
            combined = delta;
            sum = delta.DeltaUnits;
        }

        ApplyCultureDelta(combined with
        {
            DeltaUnits = ClampMessageSum(sum),
        });
        return true;
    }

    private void ApplyCultureDelta(in CultureDelta delta)
    {
        if (!_simulation.World.CultureInfluenceStorageIsPrepared(
                delta.CultureId,
                delta.PlaneCode,
                delta.Q,
                delta.R,
                delta.Z,
                delta.DeltaUnits))
        {
            _culturePreparations.Add(
                _topology.StructuralOwner,
                delta);
            return;
        }

        using var scope =
            _simulation.EnterExecutionContext(delta.Context);
        var applied =
            _simulation.World.ModifyOwnedCellCultureInfluenceRawUnits(
                delta.CultureId,
                delta.PlaneCode,
                delta.Q,
                delta.R,
                delta.Z,
                delta.DeltaUnits,
                KernelWorkerThread.OwnerTick,
                delta.Context);
        if (applied == 0)
        {
            return;
        }

        RecordCultureStorage(delta);
        if (applied > 0 &&
            delta.SourceEntityOccurrenceHandle > 0 &&
            _simulation.World.TryResolveEntityOccurrence(
                delta.SourceEntityOccurrenceHandle,
                out var sourceEntityId))
        {
            _influenceCredits.Add(
                _topology.EntityOwner(sourceEntityId),
                new InfluenceCreditDelta(
                    sourceEntityId,
                    delta.PlaneCode,
                    global::System.Math.Min(
                        applied,
                        delta.DeltaUnits)));
        }
    }

    private bool DrainCultureReductions(int ownerIndex)
    {
        var cursor = _cultureReductions.BeginDrain(ownerIndex);
        if (!_cultureReductions.MoveNext(ref cursor, out var combined))
        {
            return false;
        }

        long sum = combined.DeltaUnits;
        while (_cultureReductions.MoveNext(ref cursor, out var delta))
        {
            if (combined.SameDestination(delta))
            {
                sum = checked(sum + delta.DeltaUnits);
                continue;
            }

            ApplyCultureReduction(combined with
            {
                DeltaUnits = ClampMessageSum(sum),
            });
            combined = delta;
            sum = delta.DeltaUnits;
        }

        ApplyCultureReduction(combined with
        {
            DeltaUnits = ClampMessageSum(sum),
        });
        return true;
    }

    private void ApplyCultureReduction(
        in CultureReductionDelta delta)
    {
        using var scope =
            _simulation.EnterExecutionContext(delta.Context);
        var scratch = _simulation.DirtyCultureScratch;
        var changedCount =
            _simulation.World.ReduceOtherCultureSourcesAtCellOwned(
                delta.OwnCultureId,
                delta.PlaneCode,
                delta.Q,
                delta.R,
                delta.DeltaUnits,
                KernelWorkerThread.OwnerTick,
                delta.Context,
                scratch);
        for (var changed = 0; changed < changedCount; changed++)
        {
            _cultureStructures.Add(
                _topology.StructuralOwner,
                new CultureStorageKey(
                    scratch[changed],
                    delta.PlaneCode,
                    delta.Q,
                    delta.R,
                    Z: 0));
        }
    }

    private bool DrainInfluenceCredits(int ownerIndex)
    {
        var cursor = _influenceCredits.BeginDrain(ownerIndex);
        if (!_influenceCredits.MoveNext(ref cursor, out var combined))
        {
            return false;
        }

        long sum = combined.DeltaUnits;
        while (_influenceCredits.MoveNext(ref cursor, out var delta))
        {
            if (combined.SameDestination(delta))
            {
                sum = checked(sum + delta.DeltaUnits);
                continue;
            }

            ApplyInfluenceCredit(combined with
            {
                DeltaUnits = ClampMessageSum(sum),
            });
            combined = delta;
            sum = delta.DeltaUnits;
        }

        ApplyInfluenceCredit(combined with
        {
            DeltaUnits = ClampMessageSum(sum),
        });
        return true;
    }

    private void ApplyInfluenceCredit(
        in InfluenceCreditDelta delta)
    {
        _simulation.World.NoteEntityInfluenceProduction(
            delta.EntityId,
            delta.PlaneCode,
            delta.DeltaUnits);
    }

    private static int ClampMessageSum(long value)
    {
        if (value > int.MaxValue)
        {
            return int.MaxValue;
        }

        return value < int.MinValue
            ? int.MinValue
            : (int)value;
    }
}
