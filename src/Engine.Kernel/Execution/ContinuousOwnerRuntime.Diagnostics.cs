namespace Engine.Kernel;

internal sealed partial class ContinuousOwnerRuntime
{
    private void NoteCellStateDeferral()
    {
        if (global::System.Threading.Interlocked.Increment(
                ref _cellStateDeferrals) > 10_000)
        {
            ThrowCellStateDeferral();
        }
    }

    private void NoteRelationshipCreationDeferral()
    {
        if (global::System.Threading.Interlocked.Increment(
                ref _relationshipCreationDeferrals) > 10_000)
        {
            ThrowRelationshipCreationDeferral();
        }
    }

    private void NoteTerrainCoverDeferral()
    {
        if (global::System.Threading.Interlocked.Increment(
                ref _terrainCoverDeferrals) > 10_000)
        {
            ThrowTerrainCoverDeferral();
        }
    }

    private void NoteTargetDrainAttempt()
    {
        if (global::System.Threading.Interlocked.Increment(
                ref _targetDrainAttempts) > 10_000)
        {
            ThrowPendingOwnerDiagnostic();
        }
    }

    private void ThrowPendingOwnerDiagnostic()
    {
        if (_commands.PendingCount != 0 ||
            _movements.PendingCount != 0 ||
            _attributes.PendingCount != 0 ||
            _culture.PendingCount != 0 ||
            _cultureReductions.PendingCount != 0 ||
            _relationships.PendingCount != 0 ||
            _relationshipEnds.PendingCount != 0 ||
            _tags.PendingCount != 0 ||
            _cellStates.PendingCount != 0 ||
            _lifecycles.PendingCount != 0 ||
            _influenceCredits.PendingCount != 0 ||
            _roadWork.PendingCount != 0)
        {
            ThrowGameplayInboxPending();
        }

        if (_movementStructures.PendingCount != 0 ||
            _cellStructures.PendingCount != 0 ||
            _cultureStructures.PendingCount != 0 ||
            _relationshipRetirements.PendingCount != 0 ||
            _entityStructures.PendingCount != 0 ||
            _roadStructures.PendingCount != 0 ||
            _relationshipFollowProjections.PendingCount != 0 ||
            _entityRetirementCleanups.PendingCount != 0)
        {
            ThrowProjectionInboxPending();
        }

        if (_attributeSets.PendingCount != 0 ||
            _entityStates.PendingCount != 0 ||
            _entityCreationRequests.PendingCount != 0 ||
            _entityCreations.PendingCount != 0 ||
            _entityCreationStructures.PendingCount != 0 ||
            _relationshipCreationRequests.PendingCount != 0 ||
            _relationshipCreations.PendingCount != 0 ||
            _relationshipCreationStructures.PendingCount != 0 ||
            _relationshipSets.PendingCount != 0 ||
            _travelStructures.PendingCount != 0 ||
            _forgeCells.PendingCount != 0 ||
            _districts.PendingCount != 0 ||
            _cellMaterializations.PendingCount != 0 ||
            _terrainCovers.PendingCount != 0 ||
            _programSignals.PendingCount != 0 ||
            _entityInfluenceMultipliers.PendingCount != 0 ||
            _cultureInfluenceMultipliers.PendingCount != 0)
        {
            ThrowStrategicInboxPending();
        }

        if (_cellStatePreparations.PendingCount != 0 ||
            _culturePreparations.PendingCount != 0)
        {
            ThrowPreparationInboxPending();
        }

        throw new global::System.InvalidOperationException();
    }

    private static void ThrowGameplayInboxPending() =>
        throw new global::System.InvalidOperationException();

    private static void ThrowProjectionInboxPending() =>
        throw new global::System.InvalidOperationException();

    private static void ThrowStrategicInboxPending() =>
        throw new global::System.InvalidOperationException();

    private static void ThrowPreparationInboxPending() =>
        throw new global::System.InvalidOperationException();

    private static void ThrowCellStateDeferral() =>
        throw new global::System.InvalidOperationException();

    private static void ThrowRelationshipCreationDeferral() =>
        throw new global::System.InvalidOperationException();

    private static void ThrowTerrainCoverDeferral() =>
        throw new global::System.InvalidOperationException();
}
