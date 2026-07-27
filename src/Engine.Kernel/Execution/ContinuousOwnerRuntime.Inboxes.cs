namespace Engine.Kernel;

internal sealed partial class ContinuousOwnerRuntime
{
    private IContinuousOwnerInbox[] CreateInboxRegistry() =>
    [
        _commands,
        _movements,
        _attributes,
        _culture,
        _cultureReductions,
        _relationships,
        _relationshipEnds,
        _tags,
        _cellStates,
        _cellStatePreparations,
        _culturePreparations,
        _lifecycles,
        _influenceCredits,
        _roadWork,
        _movementStructures,
        _cellStructures,
        _cultureStructures,
        _relationshipRetirements,
        _entityStructures,
        _roadStructures,
        _attributeSets,
        _entityStates,
        _entityCreationRequests,
        _entityCreations,
        _entityCreationStructures,
        _relationshipCreationRequests,
        _relationshipCreations,
        _relationshipCreationStructures,
        _relationshipSets,
        _travelStructures,
        _forgeCells,
        _districts,
        _cellMaterializations,
        _terrainCovers,
        _relationshipFollowProjections,
        _entityRetirementCleanups,
        _programSignals,
        _entityInfluenceMultipliers,
        _cultureInfluenceMultipliers,
    ];

    private void AttachInboxActivity()
    {
        var inboxes = CreateInboxRegistry();
        for (var inboxIndex = 0;
             inboxIndex < inboxes.Length;
             inboxIndex++)
        {
            inboxes[inboxIndex].AttachActivity(_inboxActivity);
        }
    }

    private bool HasPendingOwnerWork(
        int firstOwner,
        int ownerLimit) =>
        _inboxActivity.HasPending(firstOwner, ownerLimit);

    private int PendingCount() => _inboxActivity.PendingCount;
}
