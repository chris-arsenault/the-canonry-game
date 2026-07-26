namespace Engine.Kernel;

using Engine.Constants;

internal sealed partial class GambitProgramRuntime
{
    private bool ActorPreconditionsPass(
        Simulation simulation,
        int gambitSlot,
        int entityId,
        int tick)
    {
        var destination = _gambitActorPreconditionEntries[gambitSlot];
        if (destination == GambitPreconditionDestinationCodes.Failed)
        {
            return false;
        }

        if (_gambitActorValueCounts[gambitSlot] == 0)
        {
            return destination == GambitPreconditionDestinationCodes.Passed;
        }

        PrepareActorPreconditionValues(simulation, gambitSlot, entityId, tick);
        if (destination == GambitPreconditionDestinationCodes.Passed)
        {
            return true;
        }

        var start = _gambitActorPreconditionStarts[gambitSlot];
        while (destination > 0)
        {
            var precondition = start + destination - 1;
            var passes = _scratch.ActorPreconditionValues[
                _actorPreValueSlots[precondition]];
            destination = passes
                ? _actorPreTrueDestinations[precondition]
                : _actorPreFalseDestinations[precondition];
        }

        return destination == GambitPreconditionDestinationCodes.Passed;
    }

    private void PrepareActorPreconditionValues(
        Simulation simulation,
        int gambitSlot,
        int entityId,
        int tick)
    {
        var start = _gambitPreconditionStarts[gambitSlot];
        var actorValueCount = _gambitActorValueCounts[gambitSlot];
        for (var actorValueSlot = 1;
             actorValueSlot <= actorValueCount;
             actorValueSlot++)
        {
            var precondition = _actorValuePreconditionIndexes[
                start + actorValueSlot - 1];
            _scratch.ActorPreconditionValues[actorValueSlot] =
                EvaluateGambitPredicate(
                    simulation,
                    gambitSlot,
                    precondition,
                    entityId,
                    entityId,
                    tick);
        }
    }

    private bool CandidatePreconditionsPass(
        Simulation simulation,
        int gambitSlot,
        int entityId,
        int targetId,
        int tick,
        bool relationshipTargetSourceSatisfied)
    {
        var destination = _gambitPreconditionEntries[gambitSlot];
        var start = _gambitPreconditionStarts[gambitSlot];
        while (destination > 0)
        {
            var precondition = start + destination - 1;
            var actorValueSlot = _preActorValueSlots[precondition];
            bool passes;
            if (_preRelationshipTargetSources[precondition] != 0 &&
                relationshipTargetSourceSatisfied)
            {
                passes = true;
            }
            else if (actorValueSlot > 0)
            {
                passes = _scratch.ActorPreconditionValues[actorValueSlot];
            }
            else
            {
                passes = EvaluateGambitPredicate(
                    simulation,
                    gambitSlot,
                    precondition,
                    entityId,
                    targetId,
                    tick);
            }

            destination = passes
                ? _preTrueDestinations[precondition]
                : _preFalseDestinations[precondition];
        }

        return destination == GambitPreconditionDestinationCodes.Passed;
    }
}
