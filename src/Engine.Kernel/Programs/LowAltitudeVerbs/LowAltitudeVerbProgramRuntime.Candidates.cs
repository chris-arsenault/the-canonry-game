namespace Engine.Kernel;

internal sealed partial class LowAltitudeVerbProgramRuntime
{
    private bool TryBuildCandidate(
        Simulation simulation,
        int entityId,
        int rule,
        int tick,
        out VerbTargetCandidate candidate)
    {
        return Data.TargetModeCode(rule) switch
        {
            TargetSelectorCodes.Explore => TryBuildExploreCandidate(simulation, entityId, rule, tick, out candidate),
            TargetSelectorCodes.EntityKind => TryBuildEntityKindCandidate(simulation, entityId, rule, tick, out candidate),
            TargetSelectorCodes.RelationshipTarget => TryBuildRelationshipCandidate(simulation, entityId, rule, tick, out candidate),
            TargetSelectorCodes.Terrain => TryBuildTerrainCandidate(simulation, entityId, rule, tick, out candidate),
            TargetSelectorCodes.SelfMapValue => TryBuildOwnInfluenceCandidate(simulation, entityId, rule, tick, out candidate),
            TargetSelectorCodes.OtherMapValue => TryBuildForeignInfluenceCandidate(simulation, entityId, rule, tick, out candidate),
            _ => NoCandidate(out candidate),
        };
    }

    private bool TryBuildExploreCandidate(
        Simulation simulation,
        int entityId,
        int rule,
        int tick,
        out VerbTargetCandidate candidate)
    {
        var planeCode = Data.PlaneCode(rule);
        var actorKindId = simulation.World.EntityKindId(entityId);
        var affordanceWeight = RuleAffordanceWeight(simulation.World, entityId, rule);
        var totalWeight = 0f;
        var selected = default(VerbTargetCandidate);
        for (var i = 0; i < Data.CandidateSampleCount(rule); i++)
        {
            if (!TryRandomCoordinate(simulation, planeCode, out var q, out var r, out var s))
            {
                continue;
            }

            var terrainId = simulation.World.CellTerrainId(planeCode, q, r);
            var baseWeight = LocationBaseWeight(simulation, entityId, rule, planeCode, q, r, tick);
            var affordance = TerrainAffordance(actorKindId, planeCode, terrainId);
            var weight = CandidateWeight(baseWeight, affordance, affordanceWeight);
            totalWeight += weight;
            if (simulation.NextFloat() * totalWeight <= weight)
            {
                selected = Candidate(rule, planeCode, 0, 0, q, r, s, baseWeight, affordance, affordanceWeight);
            }
        }

        if (selected.RuleSlot == 0)
        {
            return NoCandidate(out candidate);
        }

        candidate = selected;
        return true;
    }

    private bool TryBuildEntityKindCandidate(
        Simulation simulation,
        int entityId,
        int rule,
        int tick,
        out VerbTargetCandidate candidate)
    {
        var targetKindId = Data.TargetKindId(rule);
        if (targetKindId == 0)
        {
            return NoCandidate(out candidate);
        }

        return TryChooseTargetEntity(simulation, entityId, rule, tick, targetKindId, out candidate);
    }

    private bool TryBuildRelationshipCandidate(
        Simulation simulation,
        int entityId,
        int rule,
        int tick,
        out VerbTargetCandidate candidate)
    {
        if (Data.RelationshipKindId(rule) == 0)
        {
            return NoCandidate(out candidate);
        }

        var totalWeight = 0f;
        var selected = default(VerbTargetCandidate);
        if (Data.RelationshipDirectionCode(rule) is LowAltitudeVerbRelationshipDirectionCodes.Outgoing
            or LowAltitudeVerbRelationshipDirectionCodes.Either)
        {
            AddRelationshipCandidates(simulation, entityId, rule, tick, outgoing: true, ref totalWeight, ref selected);
        }

        if (Data.RelationshipDirectionCode(rule) is LowAltitudeVerbRelationshipDirectionCodes.Incoming
            or LowAltitudeVerbRelationshipDirectionCodes.Either)
        {
            AddRelationshipCandidates(simulation, entityId, rule, tick, outgoing: false, ref totalWeight, ref selected);
        }

        if (selected.RuleSlot == 0)
        {
            return NoCandidate(out candidate);
        }

        candidate = selected;
        return true;
    }

    private bool TryBuildTerrainCandidate(
        Simulation simulation,
        int entityId,
        int rule,
        int tick,
        out VerbTargetCandidate candidate)
    {
        var planeCode = Data.PlaneCode(rule);
        var actorKindId = simulation.World.EntityKindId(entityId);
        var affordanceWeight = RuleAffordanceWeight(simulation.World, entityId, rule);
        var totalWeight = 0f;
        var selected = default(VerbTargetCandidate);
        for (var i = 0; i < Data.CandidateSampleCount(rule) * 3; i++)
        {
            if (!TryRandomCoordinate(simulation, planeCode, out var q, out var r, out var s))
            {
                continue;
            }

            var terrainId = simulation.World.CellTerrainId(planeCode, q, r);
            if (!Data.TerrainAllowed(rule, terrainId) ||
                !simulation.Movement.CanEnter(simulation, planeCode, actorKindId, q, r))
            {
                continue;
            }

            var baseWeight = LocationBaseWeight(simulation, entityId, rule, planeCode, q, r, tick);
            var affordance = TerrainAffordance(actorKindId, planeCode, terrainId);
            var weight = CandidateWeight(baseWeight, affordance, affordanceWeight);
            totalWeight += weight;
            if (simulation.NextFloat() * totalWeight <= weight)
            {
                selected = Candidate(rule, planeCode, 0, 0, q, r, s, baseWeight, affordance, affordanceWeight);
            }
        }

        if (selected.RuleSlot == 0)
        {
            return NoCandidate(out candidate);
        }

        candidate = selected;
        return true;
    }

    private bool TryBuildOwnInfluenceCandidate(
        Simulation simulation,
        int entityId,
        int rule,
        int tick,
        out VerbTargetCandidate candidate)
    {
        var planeCode = Data.PlaneCode(rule);
        var actorKindId = simulation.World.EntityKindId(entityId);
        var affordanceWeight = RuleAffordanceWeight(simulation.World, entityId, rule);
        var selectedQ = CurrentQ(simulation.World, entityId, planeCode);
        var selectedR = CurrentR(simulation.World, entityId, planeCode);
        var selectedS = planeCode == PlaneCodes.Semantic ? -selectedQ - selectedR : 0;
        var totalWeight = 0f;
        var selected = default(VerbTargetCandidate);
        for (var i = 0; i < Data.CandidateSampleCount(rule) * 4; i++)
        {
            if (!TryRandomCoordinate(simulation, planeCode, out var q, out var r, out var s))
            {
                continue;
            }

            var influence = simulation.World.GetCellInfluence(entityId, planeCode, q, r);
            if (influence <= 0f)
            {
                continue;
            }

            var baseWeight = LocationBaseWeight(simulation, entityId, rule, planeCode, q, r, tick) +
                ((100f - influence) / 100f) * 2f;
            var affordance = InfluenceAffordance(
                actorKindId,
                planeCode,
                Data.OperationCode(rule),
                actorKindId,
                AffordancePolarityCodes.Increase,
                influence);
            var weight = CandidateWeight(baseWeight, affordance, affordanceWeight);
            totalWeight += weight;
            if (simulation.NextFloat() * totalWeight <= weight)
            {
                selectedQ = q;
                selectedR = r;
                selectedS = s;
                selected = Candidate(rule, planeCode, 0, 0, q, r, s, baseWeight, affordance, affordanceWeight);
            }
        }

        if (selected.RuleSlot == 0 &&
            simulation.World.GetCellInfluence(entityId, planeCode, selectedQ, selectedR) <= 0f)
        {
            return NoCandidate(out candidate);
        }

        candidate = selected.RuleSlot == 0
            ? Candidate(rule, planeCode, 0, 0, selectedQ, selectedR, selectedS, Data.Weight(rule))
            : selected;
        return true;
    }

    private bool TryBuildForeignInfluenceCandidate(
        Simulation simulation,
        int entityId,
        int rule,
        int tick,
        out VerbTargetCandidate candidate)
    {
        var planeCode = Data.PlaneCode(rule);
        var actorKindId = simulation.World.EntityKindId(entityId);
        var affordanceWeight = RuleAffordanceWeight(simulation.World, entityId, rule);
        var totalWeight = 0f;
        var selected = default(VerbTargetCandidate);
        for (var i = 0; i < Data.CandidateSampleCount(rule) * 4; i++)
        {
            if (!TryRandomCoordinate(simulation, planeCode, out var q, out var r, out var s))
            {
                continue;
            }

            var influencerId = StrongestAllowedOtherInfluencerAt(simulation, entityId, rule, planeCode, q, r, out var influence);
            if (influencerId == 0)
            {
                continue;
            }

            var influencerKindId = simulation.World.EntityKindId(influencerId);
            var baseWeight = LocationBaseWeight(simulation, entityId, rule, planeCode, q, r, tick, influencerId) +
                (influence / 100f) * 3f;
            var affordance = InfluenceAffordance(
                actorKindId,
                planeCode,
                Data.OperationCode(rule),
                influencerKindId,
                AffordancePolarityCodes.Reduce,
                influence);
            var weight = CandidateWeight(baseWeight, affordance, affordanceWeight);
            totalWeight += weight;
            if (simulation.NextFloat() * totalWeight <= weight)
            {
                selected = Candidate(rule, planeCode, influencerId, influencerKindId, q, r, s, baseWeight, affordance, affordanceWeight);
            }
        }

        if (selected.RuleSlot == 0)
        {
            return NoCandidate(out candidate);
        }

        candidate = selected;
        return true;
    }

    private bool TryChooseTargetEntity(
        Simulation simulation,
        int entityId,
        int rule,
        int tick,
        int targetKindId,
        out VerbTargetCandidate candidate)
    {
        var world = simulation.World;
        var totalWeight = 0f;
        var selected = default(VerbTargetCandidate);
        for (var targetEntityId = 1; targetEntityId <= world.EntityHighWatermark; targetEntityId++)
        {
            if (targetEntityId == entityId ||
                !world.EntityIsActive(targetEntityId) ||
                world.EntityKindId(targetEntityId) != targetKindId)
            {
                continue;
            }

            AddEntityCandidate(simulation, entityId, rule, tick, targetEntityId, ref totalWeight, ref selected);
        }

        if (selected.RuleSlot == 0)
        {
            return NoCandidate(out candidate);
        }

        candidate = selected;
        return true;
    }

    private void AddRelationshipCandidates(
        Simulation simulation,
        int entityId,
        int rule,
        int tick,
        bool outgoing,
        ref float totalWeight,
        ref VerbTargetCandidate selected)
    {
        var world = simulation.World;
        var relationshipKindId = Data.RelationshipKindId(rule);
        var cursor = outgoing
            ? world.OutgoingRelationshipsOfKind(entityId, relationshipKindId)
            : world.IncomingRelationshipsOfKind(entityId, relationshipKindId);
        while (world.MoveNextRelationship(ref cursor, out var relationshipId))
        {
            var targetEntityId = outgoing
                ? world.RelationshipTargetId(relationshipId)
                : world.RelationshipSourceId(relationshipId);
            if (!world.EntityIsActive(targetEntityId))
            {
                continue;
            }

            AddRelationshipCandidate(simulation, entityId, rule, tick, targetEntityId, relationshipId, outgoing, ref totalWeight, ref selected);
        }
    }
}
