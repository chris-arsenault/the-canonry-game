namespace Engine.Kernel;

using Engine.Constants;

internal sealed partial class MinorActorProgramRuntime :
    IKernelProgramRuntime<MinorActorProgramData, MinorActorProgramState>
{
    private static readonly int[] NeighborDQ = [1, 1, 0, -1, -1, 0];
    private static readonly int[] NeighborDR = [0, -1, -1, 0, 1, 1];
    private static readonly FixedScalar StageStrengthStep = FixedScalar.FromUnits(500000L);
    private static readonly FixedScalar RecoveryShare = FixedScalar.FromUnits(100000L);
    private static readonly FixedScalar FriendlySupportChance = FixedScalar.FromUnits(40000L);
    private static readonly FixedScalar HostileClashChance = FixedScalar.FromUnits(80000L);
    private static readonly FixedScalar FriendlySupportShare = FixedScalar.FromUnits(100000L);
    private static readonly FixedScalar HostileDamageShare = FixedScalar.FromUnits(150000L);
    private static readonly FixedScalar CampaignDamageShare = FixedScalar.FromUnits(250000L);
    private static readonly FixedScalar StrongFrontThreshold = FixedScalar.FromSingle(2.5f);

    public MinorActorProgramRuntime(MinorActorProgramData data, MinorActorProgramState state)
    {
        global::System.ArgumentNullException.ThrowIfNull(data);
        global::System.ArgumentNullException.ThrowIfNull(state);
        Data = data;
        State = state;
    }

    public MinorActorProgramData Data { get; }

    public MinorActorProgramState State { get; }

    internal void NoteEntityRetired(Simulation simulation, int entityId, int tick)
    {
        for (var index = State.ActiveCount - 1; index >= 0; index--)
        {
            var actorId = State.ActiveActorAt(index);
            if (State.SourceEntityId(actorId) == entityId)
            {
                _ = RetireActor(
                    simulation,
                    actorId,
                    tick,
                    MinorActorRetirementReasonCodes.SourceRetired);
            }
        }
    }

    public int RegisterRoleRule(
        int sourceKindId,
        int sourceSubtypeId,
        int roleCode,
        int planeCode,
        int assignmentRadius,
        int stride,
        int commitmentTicks,
        int replanTicks,
        FixedScalar baseStrength) =>
        Data.RegisterRoleRule(
            sourceKindId,
            sourceSubtypeId,
            roleCode,
            planeCode,
            assignmentRadius,
            stride,
            commitmentTicks,
            replanTicks,
            baseStrength);

    public void SetCampaignRelationshipKindId(int relationshipKindId) =>
        Data.CampaignRelationshipKindId = relationshipKindId;

    public void Tick(Simulation simulation, int tick)
    {
        global::System.ArgumentNullException.ThrowIfNull(simulation);
        EnsureSourceActors(simulation, tick);
        TickActiveActors(simulation, tick);
    }

    private void EnsureSourceActors(Simulation simulation, int tick)
    {
        var world = simulation.World;
        for (var index = 0; index < simulation.FrozenEntityCount; index++)
        {
            var sourceEntityId = simulation.FrozenEntityAt(index);
            var sourceKindId = world.EntityKindId(sourceEntityId);
            for (var rule = Data.FirstRuleForSourceKind(sourceKindId);
                 rule > 0;
                 rule = Data.NextRuleForSourceKind(rule))
            {
                if (!Data.RuleMatches(world, sourceEntityId, rule))
                {
                    continue;
                }

                if (Data.RoleCodes[rule] == MinorActorRoleCodes.CampaignColumn)
                {
                    EnsureCampaignColumnsForSource(
                        simulation,
                        sourceEntityId,
                        rule,
                        tick);
                    continue;
                }

                if (
                    State.ActorForSourceRule(sourceEntityId, rule) > 0 ||
                    !SourceProbeIsDue(sourceEntityId, rule, Data.ReplanTicks[rule], tick))
                {
                    continue;
                }

                CreateActorIfAssignable(
                    simulation,
                    sourceEntityId,
                    rule,
                    bindingRelationshipId: 0,
                    tick);
            }
        }
    }

    private void EnsureCampaignColumnsForSource(
        Simulation simulation,
        int sourceEntityId,
        int rule,
        int tick)
    {
        if (Data.CampaignRelationshipKindId <= 0)
        {
            return;
        }

        var world = simulation.World;
        var cursor = world.OutgoingRelationshipsOfKind(
            sourceEntityId,
            Data.CampaignRelationshipKindId);
        while (world.MoveNextRelationship(ref cursor, out var relationshipId))
        {
            if (State.ActorForRelationship(relationshipId) > 0)
            {
                continue;
            }

            CreateActorIfAssignable(simulation, sourceEntityId, rule, relationshipId, tick);
        }
    }

    private void CreateActorIfAssignable(
        Simulation simulation,
        int sourceEntityId,
        int rule,
        int bindingRelationshipId,
        int tick)
    {
        var world = simulation.World;
        var planeCode = Data.PlaneCodes[rule];
        if (!TryResolveEntityPosition(world, sourceEntityId, planeCode, out var q, out var r) ||
            !world.TryGetCellId(planeCode, q, r, z: 0, out var cellId) ||
            !TryFindAssignment(
                simulation,
                sourceEntityId,
                rule,
                bindingRelationshipId,
                q,
                r,
                out var assignment))
        {
            return;
        }

        var relationshipOccurrenceHandle = bindingRelationshipId > 0
            ? world.RelationshipOccurrenceHandle(bindingRelationshipId)
            : 0;
        var actorId = State.Create(
            world.EntityCultureId(sourceEntityId),
            sourceEntityId,
            world.EntityOccurrenceHandle(sourceEntityId),
            rule,
            bindingRelationshipId,
            relationshipOccurrenceHandle,
            Data.RoleCodes[rule],
            planeCode,
            q,
            r,
            cellId,
            EffectiveStrength(simulation, sourceEntityId, rule),
            tick);
        State.SetAssignment(
            actorId,
            assignment.Q,
            assignment.R,
            assignment.TargetingCode,
            checked(tick + Data.ReplanTicks[rule]),
            checked(tick + Data.CommitmentTicks[rule]));
        EmitCreated(simulation, actorId, tick);
        EmitAssigned(simulation, actorId, previousTargetQ: 0, previousTargetR: 0, tick);
    }

    private void TickActiveActors(Simulation simulation, int tick)
    {
        for (var index = 0; index < State.ActiveCount;)
        {
            var actorId = State.ActiveActorAt(index);
            if (TickActor(simulation, actorId, tick))
            {
                index++;
            }
        }
    }

    private bool TickActor(Simulation simulation, int actorId, int tick)
    {
        var world = simulation.World;
        if (!TryResolveActorSource(world, actorId, out var sourceEntityId, out var reasonCode))
        {
            _ = RetireActor(simulation, actorId, tick, reasonCode);
            return false;
        }

        var rule = State.SourceRuleId(actorId);
        var maximumStrength = EffectiveStrength(simulation, sourceEntityId, rule);
        var strength = FixedScalar.Min(
            maximumStrength,
            State.Strength(actorId) + (maximumStrength * RecoveryShare));
        State.SetStrength(actorId, strength);
        State.SetWorkCharge(actorId, strength);

        var assignmentValid = AssignmentIsValid(simulation, actorId);
        if (!assignmentValid ||
            (tick >= State.NextReplanTick(actorId) &&
             tick >= State.CommitmentUntilTick(actorId)))
        {
            if (!TryReplanActor(simulation, actorId, sourceEntityId, rule, tick))
            {
                _ = RetireActor(
                    simulation,
                    actorId,
                    tick,
                    MinorActorRetirementReasonCodes.NoAssignment);
                return false;
            }
        }

        MoveTowardAssignment(simulation, actorId, Data.Strides[rule], tick);
        ResolveLocalEncounters(simulation, actorId, tick);
        ApplyRoleWork(simulation, actorId, tick);
        return State.IsActive(actorId);
    }

    private bool TryResolveActorSource(
        WorldState world,
        int actorId,
        out int sourceEntityId,
        out int reasonCode)
    {
        if (!world.TryResolveEntityOccurrence(
                State.SourceEntityOccurrenceHandle(actorId),
                out sourceEntityId) ||
            sourceEntityId != State.SourceEntityId(actorId) ||
            !Data.RuleMatches(world, sourceEntityId, State.SourceRuleId(actorId)))
        {
            reasonCode = MinorActorRetirementReasonCodes.SourceInvalid;
            return false;
        }

        reasonCode = MinorActorRetirementReasonCodes.BindingInvalid;
        return BindingIsValid(world, actorId);
    }

    private bool TryReplanActor(
        Simulation simulation,
        int actorId,
        int sourceEntityId,
        int rule,
        int tick)
    {
        if (!TryFindAssignment(
                simulation,
                sourceEntityId,
                rule,
                State.BindingRelationshipId(actorId),
                State.Q(actorId),
                State.R(actorId),
                out var assignment))
        {
            return false;
        }

        var previousTargetQ = State.TargetQ(actorId);
        var previousTargetR = State.TargetR(actorId);
        State.SetAssignment(
            actorId,
            assignment.Q,
            assignment.R,
            assignment.TargetingCode,
            checked(tick + Data.ReplanTicks[rule]),
            checked(tick + Data.CommitmentTicks[rule]));
        EmitAssigned(simulation, actorId, previousTargetQ, previousTargetR, tick);
        return true;
    }

    private bool BindingIsValid(WorldState world, int actorId)
    {
        var relationshipHandle = State.BindingRelationshipOccurrenceHandle(actorId);
        if (State.RoleCode(actorId) != MinorActorRoleCodes.CampaignColumn)
        {
            return relationshipHandle == 0;
        }

        return world.TryResolveRelationshipOccurrence(relationshipHandle, out var relationshipId) &&
            relationshipId == State.BindingRelationshipId(actorId) &&
            world.RelationshipKindId(relationshipId) == Data.CampaignRelationshipKindId;
    }

    private bool AssignmentIsValid(Simulation simulation, int actorId)
    {
        var targetingCode = State.TargetingCode(actorId);
        if (targetingCode == 0 ||
            !simulation.World.CellIsKnown(
                State.PlaneCode(actorId),
                State.TargetQ(actorId),
                State.TargetR(actorId)))
        {
            return false;
        }

        return State.RoleCode(actorId) switch
        {
            MinorActorRoleCodes.CulturalFront => CulturalOpportunityAt(
                simulation,
                State.CultureId(actorId),
                State.PlaneCode(actorId),
                State.TargetQ(actorId),
                State.TargetR(actorId),
                out _,
                out _),
            MinorActorRoleCodes.RoadCrew =>
                simulation.Movement.RoadTravelCount(
                    State.PlaneCode(actorId),
                    State.TargetQ(actorId),
                    State.TargetR(actorId)) > 0 &&
                simulation.Movement.RoadQuality(
                    State.PlaneCode(actorId),
                    State.TargetQ(actorId),
                    State.TargetR(actorId),
                    simulation.CurrentTick) < 95,
            MinorActorRoleCodes.CampaignColumn => true,
            _ => false,
        };
    }

    private bool TryFindAssignment(
        Simulation simulation,
        int sourceEntityId,
        int rule,
        int bindingRelationshipId,
        int actorQ,
        int actorR,
        out MinorActorAssignment assignment)
    {
        return Data.RoleCodes[rule] switch
        {
            MinorActorRoleCodes.CulturalFront => TryFindCulturalFrontAssignment(
                simulation,
                sourceEntityId,
                rule,
                actorQ,
                actorR,
                out assignment),
            MinorActorRoleCodes.RoadCrew => TryFindRoadAssignment(
                simulation,
                sourceEntityId,
                rule,
                actorQ,
                actorR,
                out assignment),
            MinorActorRoleCodes.CampaignColumn => TryFindCampaignAssignment(
                simulation,
                sourceEntityId,
                rule,
                bindingRelationshipId,
                actorQ,
                actorR,
                out assignment),
            _ => ThrowUnknownRole(out assignment),
        };
    }

    private bool TryFindCulturalFrontAssignment(
        Simulation simulation,
        int sourceEntityId,
        int rule,
        int actorQ,
        int actorR,
        out MinorActorAssignment assignment)
    {
        assignment = default;
        var planeCode = Data.PlaneCodes[rule];
        if (!TryResolveEntityPosition(
                simulation.World,
                sourceEntityId,
                planeCode,
                out var sourceQ,
                out var sourceR))
        {
            return false;
        }

        var cultureId = simulation.World.EntityCultureId(sourceEntityId);
        FindCulturalCandidate(
            simulation,
            cultureId,
            planeCode,
            Data.AssignmentRadii[rule],
            sourceQ,
            sourceR,
            actorQ,
            actorR,
            ref assignment);
        return assignment.Score > 0;
    }

    private static void FindCulturalCandidate(
        Simulation simulation,
        int cultureId,
        int planeCode,
        int radius,
        int sourceQ,
        int sourceR,
        int actorQ,
        int actorR,
        ref MinorActorAssignment assignment)
    {
        for (var dq = -radius; dq <= radius; dq++)
        {
            var minDr = global::System.Math.Max(-radius, -dq - radius);
            var maxDr = global::System.Math.Min(radius, -dq + radius);
            for (var dr = minDr; dr <= maxDr; dr++)
            {
                var q = sourceQ + dq;
                var r = sourceR + dr;
                if (!CulturalOpportunityAt(
                        simulation,
                        cultureId,
                        planeCode,
                        q,
                        r,
                        out var targetingCode,
                        out var baseScore))
                {
                    continue;
                }

                var distance = HexDistance(actorQ, actorR, q, r);
                var score = baseScore / (1 + distance);
                SelectAssignment(q, r, targetingCode, score, ref assignment);
            }
        }
    }

    private static bool CulturalOpportunityAt(
        Simulation simulation,
        int cultureId,
        int planeCode,
        int q,
        int r,
        out int targetingCode,
        out FixedScalar score)
    {
        targetingCode = 0;
        score = 0;
        if (!simulation.World.CellIsKnown(planeCode, q, r))
        {
            return false;
        }

        var boundary = simulation.CultureLocality.CellAt(
            simulation.World,
            cultureId,
            planeCode,
            q,
            r);
        if (boundary.ForeignRawMass >= WorldState.CultureTraceRawMass &&
            (boundary.ConnectedRawMass > 0 || boundary.SupportedAdjacentCount > 0))
        {
            targetingCode = MinorActorTargetingCodes.ContestCulture;
            score = 1000 + (10 * boundary.ForeignRawMass) +
                FixedScalar.Max(0, WorldState.CultureControlledRawMass - boundary.RawMass);
            return true;
        }

        if (boundary.RawMass >= WorldState.CultureControlledRawMass)
        {
            return false;
        }

        if (boundary.SupportedAdjacentCount >= 2)
        {
            targetingCode = MinorActorTargetingCodes.BridgeGap;
            score = 500 + (50 * boundary.SupportedAdjacentCount) +
                FixedScalar.Max(0, WorldState.CultureControlledRawMass - boundary.RawMass);
            return true;
        }

        if (boundary.ConnectedRawMass > 0 || boundary.SupportedAdjacentCount > 0)
        {
            targetingCode = MinorActorTargetingCodes.ExtendBorder;
            score = 100 + boundary.ConnectedRawMass +
                FixedScalar.Max(0, WorldState.CultureControlledRawMass - boundary.RawMass);
            return true;
        }

        return false;
    }

    private bool TryFindRoadAssignment(
        Simulation simulation,
        int sourceEntityId,
        int rule,
        int actorQ,
        int actorR,
        out MinorActorAssignment assignment)
    {
        assignment = default;
        var planeCode = Data.PlaneCodes[rule];
        if (!TryResolveEntityPosition(
                simulation.World,
                sourceEntityId,
                planeCode,
                out var sourceQ,
                out var sourceR))
        {
            return false;
        }

        var radius = Data.AssignmentRadii[rule];
        for (var dq = -radius; dq <= radius; dq++)
        {
            var minDr = global::System.Math.Max(-radius, -dq - radius);
            var maxDr = global::System.Math.Min(radius, -dq + radius);
            for (var dr = minDr; dr <= maxDr; dr++)
            {
                var q = sourceQ + dq;
                var r = sourceR + dr;
                if (!simulation.World.CellIsKnown(planeCode, q, r))
                {
                    continue;
                }

                var travelCount = simulation.Movement.RoadTravelCount(planeCode, q, r);
                if (travelCount <= 0)
                {
                    continue;
                }

                var quality = simulation.Movement.RoadQuality(
                    planeCode,
                    q,
                    r,
                    simulation.CurrentTick);
                if (quality >= 95)
                {
                    continue;
                }

                var distance = HexDistance(actorQ, actorR, q, r);
                var score = (travelCount * (101 - quality)) / (1 + distance);
                SelectAssignment(
                    q,
                    r,
                    MinorActorTargetingCodes.MaintainRoad,
                    score,
                    ref assignment);
            }
        }

        return assignment.Score > 0;
    }

    private bool TryFindCampaignAssignment(
        Simulation simulation,
        int sourceEntityId,
        int rule,
        int relationshipId,
        int actorQ,
        int actorR,
        out MinorActorAssignment assignment)
    {
        assignment = default;
        var world = simulation.World;
        if (relationshipId <= 0 ||
            !world.TryResolveRelationshipOccurrence(
                world.RelationshipOccurrenceHandle(relationshipId),
                out var resolvedRelationshipId) ||
            resolvedRelationshipId != relationshipId)
        {
            return false;
        }

        var targetEntityId = world.RelationshipTargetId(relationshipId);
        var planeCode = Data.PlaneCodes[rule];
        if (!TryResolveEntityPosition(world, sourceEntityId, planeCode, out var homeQ, out var homeR) ||
            !TryResolveEntityPosition(world, targetEntityId, planeCode, out var targetQ, out var targetR))
        {
            return false;
        }

        var cultureId = world.EntityCultureId(sourceEntityId);
        FindSupportedCampaignFrontier(
            world,
            cultureId,
            planeCode,
            homeQ,
            homeR,
            ref targetQ,
            ref targetR);
        FindCampaignCandidate(
            simulation,
            cultureId,
            planeCode,
            Data.AssignmentRadii[rule],
            targetQ,
            targetR,
            actorQ,
            actorR,
            ref assignment);
        if (assignment.Score > 0)
        {
            return true;
        }

        assignment = new MinorActorAssignment(
            targetQ,
            targetR,
            MinorActorTargetingCodes.AdvanceCampaign,
            1);
        return world.CellIsKnown(planeCode, targetQ, targetR);
    }

    private static void FindCampaignCandidate(
        Simulation simulation,
        int cultureId,
        int planeCode,
        int radius,
        int targetQ,
        int targetR,
        int actorQ,
        int actorR,
        ref MinorActorAssignment assignment)
    {
        var world = simulation.World;
        for (var dq = -radius; dq <= radius; dq++)
        {
            var minDr = global::System.Math.Max(-radius, -dq - radius);
            var maxDr = global::System.Math.Min(radius, -dq + radius);
            for (var dr = minDr; dr <= maxDr; dr++)
            {
                var q = targetQ + dq;
                var r = targetR + dr;
                if (!world.CellIsKnown(planeCode, q, r))
                {
                    continue;
                }

                var boundary = simulation.CultureLocality.CellAt(world, cultureId, planeCode, q, r);
                if (boundary.ConnectedRawMass <= 0 && boundary.SupportedAdjacentCount <= 0)
                {
                    continue;
                }

                var distance = HexDistance(actorQ, actorR, q, r);
                var score = (800 + (20 * boundary.ForeignRawMass) +
                    FixedScalar.Max(0, WorldState.CultureEntrenchedRawMass - boundary.RawMass)) /
                    (1 + distance);
                SelectAssignment(
                    q,
                    r,
                    MinorActorTargetingCodes.AdvanceCampaign,
                    score,
                    ref assignment);
            }
        }
    }

    private static void FindSupportedCampaignFrontier(
        WorldState world,
        int cultureId,
        int planeCode,
        int homeQ,
        int homeR,
        ref int targetQ,
        ref int targetR)
    {
        var dq = homeQ - targetQ;
        var dr = homeR - targetR;
        var distance = HexDistance(homeQ, homeR, targetQ, targetR);
        for (var step = 0; step <= distance; step++)
        {
            var ratio = distance == 0 ? 0 : step / (FixedScalar)distance;
            var q = targetQ + (int)FixedScalar.Round(
                dq * ratio,
                global::System.MidpointRounding.AwayFromZero);
            var r = targetR + (int)FixedScalar.Round(
                dr * ratio,
                global::System.MidpointRounding.AwayFromZero);
            if (world.GetCellCultureInfluenceRaw(cultureId, planeCode, q, r) >=
                WorldState.CultureTraceRawMass)
            {
                targetQ = q;
                targetR = r;
                return;
            }
        }

        targetQ = homeQ;
        targetR = homeR;
    }

    private void MoveTowardAssignment(
        Simulation simulation,
        int actorId,
        int stride,
        int tick)
    {
        var planeCode = State.PlaneCode(actorId);
        var q = State.Q(actorId);
        var r = State.R(actorId);
        var targetQ = State.TargetQ(actorId);
        var targetR = State.TargetR(actorId);
        for (var step = 0; step < stride && (q != targetQ || r != targetR); step++)
        {
            if (!TrySelectNextCell(
                    simulation.World,
                    planeCode,
                    q,
                    r,
                    targetQ,
                    targetR,
                    out var nextQ,
                    out var nextR))
            {
                State.SetPosture(actorId, MinorActorPostureCodes.Disrupted);
                break;
            }

            q = nextQ;
            r = nextR;
        }

        if ((q != State.Q(actorId) || r != State.R(actorId)) &&
            simulation.World.TryGetCellId(planeCode, q, r, z: 0, out var cellId))
        {
            var previousQ = State.Q(actorId);
            var previousR = State.R(actorId);
            State.SetPosition(actorId, q, r, cellId);
            EmitMoved(simulation, actorId, previousQ, previousR, tick);
        }
    }

    private static bool TrySelectNextCell(
        WorldState world,
        int planeCode,
        int q,
        int r,
        int targetQ,
        int targetR,
        out int nextQ,
        out int nextR)
    {
        nextQ = q;
        nextR = r;
        var bestDistance = HexDistance(q, r, targetQ, targetR);
        for (var direction = 0; direction < NeighborDQ.Length; direction++)
        {
            var candidateQ = q + NeighborDQ[direction];
            var candidateR = r + NeighborDR[direction];
            if (!world.CellIsKnown(planeCode, candidateQ, candidateR))
            {
                continue;
            }

            var distance = HexDistance(candidateQ, candidateR, targetQ, targetR);
            if (distance < bestDistance)
            {
                nextQ = candidateQ;
                nextR = candidateR;
                bestDistance = distance;
            }
        }

        return nextQ != q || nextR != r;
    }

    private void ApplyRoleWork(Simulation simulation, int actorId, int tick)
    {
        if (!State.IsActive(actorId) ||
            State.Q(actorId) != State.TargetQ(actorId) ||
            State.R(actorId) != State.TargetR(actorId))
        {
            return;
        }

        var work = State.WorkCharge(actorId);
        if (work <= 0)
        {
            return;
        }

        var applied = State.RoleCode(actorId) switch
        {
            MinorActorRoleCodes.CulturalFront => ApplyCulturalFrontWork(
                simulation,
                actorId,
                work,
                tick),
            MinorActorRoleCodes.RoadCrew => ApplyRoadWork(
                simulation,
                actorId,
                work,
                tick),
            MinorActorRoleCodes.CampaignColumn => ApplyCampaignWork(
                simulation,
                actorId,
                work,
                tick),
            _ => 0,
        };

        if (applied > 0)
        {
            EmitWorkEvent(simulation, actorId, applied, tick);
        }
    }

    private FixedScalar ApplyCulturalFrontWork(
        Simulation simulation,
        int actorId,
        FixedScalar work,
        int tick)
    {
        var delta = CultureWorkDelta(simulation, actorId, work);
        var applied = FixedScalar.Zero;
        if (State.TargetingCode(actorId) == MinorActorTargetingCodes.ContestCulture)
        {
            applied = simulation.ReduceOtherCultureSourcesAtCell(
                State.CultureId(actorId),
                State.PlaneCode(actorId),
                State.Q(actorId),
                State.R(actorId),
                delta,
                tick);
            if (State.Strength(actorId) < StrongFrontThreshold)
            {
                return applied;
            }

            delta /= 2;
        }

        simulation.ModifyCellCultureInfluenceRawFromEntity(
            State.SourceEntityId(actorId),
            State.PlaneCode(actorId),
            State.Q(actorId),
            State.R(actorId),
            delta,
            tick);
        return applied + delta;
    }

    private static FixedScalar ApplyRoadWork(
        Simulation simulation,
        int actorId,
        FixedScalar work,
        int tick)
    {
        var cultureId = simulation.MinorActors.State.CultureId(actorId);
        var delta = work * simulation.Knowledge.Modifier(
            cultureId,
            KnowledgeGrantDomainCodes.RoadWork);
        return simulation.Movement.ReinforceRoad(
            simulation,
            simulation.MinorActors.State.PlaneCode(actorId),
            simulation.MinorActors.State.Q(actorId),
            simulation.MinorActors.State.R(actorId),
            delta,
            tick)
            ? delta
            : 0;
    }

    private FixedScalar ApplyCampaignWork(
        Simulation simulation,
        int actorId,
        FixedScalar work,
        int tick)
    {
        var delta = CultureWorkDelta(simulation, actorId, work);
        var applied = simulation.ReduceOtherCultureSourcesAtCell(
            State.CultureId(actorId),
            State.PlaneCode(actorId),
            State.Q(actorId),
            State.R(actorId),
            delta,
            tick);
        simulation.ModifyCellCultureInfluenceRawFromEntity(
            State.SourceEntityId(actorId),
            State.PlaneCode(actorId),
            State.Q(actorId),
            State.R(actorId),
            delta,
            tick);
        return applied + delta;
    }

    private FixedScalar CultureWorkDelta(
        Simulation simulation,
        int actorId,
        FixedScalar work) =>
        work *
        simulation.Options.KernelTuning.Programs.CultureInfluenceActionScale *
        simulation.Knowledge.Modifier(
            State.CultureId(actorId),
            KnowledgeGrantDomainCodes.MinorActorWork,
            State.RoleCode(actorId));

}
