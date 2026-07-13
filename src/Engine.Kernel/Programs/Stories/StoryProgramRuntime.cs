namespace Engine.Kernel;

/// <summary>
/// The Chronicle Director: casts story archetypes over live entities, biases
/// cast members' gambits toward their roles, sustains the cast while their
/// story runs, and guarantees a resolution by the final act's deadline. The
/// forward-running counterpart of a chronicle plan: cast, plot, direction.
/// </summary>
internal sealed partial class StoryProgramRuntime : IKernelProgramRuntime<StoryProgramData, StoryProgramState>
{
    private readonly int _storyCapacity;

    public StoryProgramRuntime(SimulationOptions options)
    {
        _storyCapacity = options.StoryCapacity;
        Data = new StoryProgramData();
        Data.Allocate(options.StoryArchetypeCapacity, options.StoryItemCapacity);
        State = new StoryProgramState(options.StoryCapacity, options.StoryArchetypeCapacity);
    }

    public StoryProgramData Data { get; }

    public StoryProgramState State { get; }

    public void Tick(Simulation simulation, int tick)
    {
        var world = simulation.World;
        for (var story = 1; story <= State.HighWatermark; story++)
        {
            if (!State.Active[story])
            {
                continue;
            }

            ApplySustain(simulation, world, story);
            StepStory(simulation, world, story, tick);
        }

        for (var archetype = 1; archetype <= Data.ArchetypeCount; archetype++)
        {
            TryCast(simulation, world, archetype, tick);
        }
    }

    /// <summary>
    /// Direction: extra score for a gambit whose actor and target are bound
    /// to biased roles of a running story's current act. Read by the gambit
    /// program during candidate scoring.
    /// </summary>
    public float ScoreBias(int actorId, int targetId, int gambitId)
    {
        var total = 0f;
        for (var story = 1; story <= State.HighWatermark; story++)
        {
            if (!State.Active[story])
            {
                continue;
            }

            total += StoryBias(story, actorId, targetId, gambitId);
        }

        return total;
    }

    private float StoryBias(int story, int actorId, int targetId, int gambitId)
    {
        var archetype = State.ArchetypeSlots[story];
        var act = Data.ArchetypeActStarts[archetype] + State.ActIndices[story];
        var biasStart = Data.ActBiasStarts[act];
        var biasEnd = biasStart + Data.ActBiasCounts[act];
        var total = 0f;
        for (var bias = biasStart; bias < biasEnd; bias++)
        {
            if (RoleEntity(story, Data.BiasActorRoles[bias]) != actorId ||
                RoleEntity(story, Data.BiasTargetRoles[bias]) != targetId)
            {
                continue;
            }

            if (Data.BiasGambitIds[bias] != 0 && Data.BiasGambitIds[bias] != gambitId)
            {
                continue;
            }

            total += Data.BiasScoreBonuses[bias];
        }

        return total;
    }

    private int RoleEntity(int story, int role) =>
        State.RoleEntityIds[(story * StoryProgramData.MaxRolesPerArchetype) + role];

    private void SetRoleEntity(int story, int role, int entityId) =>
        State.RoleEntityIds[(story * StoryProgramData.MaxRolesPerArchetype) + role] = entityId;

    private void ApplySustain(Simulation simulation, WorldState world, int story)
    {
        var archetype = State.ArchetypeSlots[story];
        var act = Data.ArchetypeActStarts[archetype] + State.ActIndices[story];
        var sustainStart = Data.ActSustainStarts[act];
        var sustainEnd = sustainStart + Data.ActSustainCounts[act];
        for (var sustain = sustainStart; sustain < sustainEnd; sustain++)
        {
            var entityId = RoleEntity(story, Data.SustainRoles[sustain]);
            if (entityId > 0 && world.EntityIsActive(entityId))
            {
                KernelEffectExecutor.ModifyAttribute(
                    simulation,
                    entityId,
                    Data.SustainAttributeIds[sustain],
                    Data.SustainDeltas[sustain]);
            }
        }
    }

    private void StepStory(Simulation simulation, WorldState world, int story, int tick)
    {
        var archetype = State.ArchetypeSlots[story];
        var act = Data.ArchetypeActStarts[archetype] + State.ActIndices[story];
        var deadlineReached = tick - State.ActStartTicks[story] >= Data.ActDeadlineTicks[act];
        var conditionMet = ConditionHolds(
            world,
            story,
            Data.ActAdvanceConditionKinds[act],
            Data.ActAdvanceRoleA[act],
            Data.ActAdvanceRoleB[act],
            Data.ActAdvanceRelKindIds[act],
            Data.ActAdvanceMinStrengths[act]);
        if (!deadlineReached && !conditionMet)
        {
            return;
        }

        if (State.ActIndices[story] + 1 < Data.ArchetypeActCounts[archetype])
        {
            State.ActIndices[story]++;
            State.ActStartTicks[story] = tick;
            EmitStoryChanged(simulation, story, StoryEventTypeCodes.ActAdvanced, outcomeCodeId: 0, tick);
            return;
        }

        Resolve(simulation, world, story, tick);
    }

    private bool ConditionHolds(
        WorldState world,
        int story,
        int conditionKind,
        int roleA,
        int roleB,
        int relKindId,
        float minStrength)
    {
        switch (conditionKind)
        {
            case StoryConditionCodes.RolePairRelationship:
                var sourceId = RoleEntity(story, roleA);
                var targetId = RoleEntity(story, roleB);
                if (sourceId <= 0 || targetId <= 0)
                {
                    return false;
                }

                var relationshipId = KernelQuery.FindRelationship(world, sourceId, targetId, relKindId);
                return relationshipId != 0 && world.RelationshipStrength(relationshipId) >= minStrength;
            case StoryConditionCodes.RoleHistorical:
                var entityId = RoleEntity(story, roleA);
                return entityId > 0 && !world.EntityIsActive(entityId);
            default:
                return false;
        }
    }

    private void Resolve(Simulation simulation, WorldState world, int story, int tick)
    {
        var archetype = State.ArchetypeSlots[story];
        var resolution = SelectResolution(simulation, world, story, archetype);
        if (resolution != 0)
        {
            ApplyResolution(simulation, world, story, resolution);
        }

        EmitStoryChanged(
            simulation,
            story,
            StoryEventTypeCodes.Resolved,
            resolution == 0 ? 0 : Data.ResolutionOutcomeCodeIds[resolution],
            tick);
        State.Active[story] = false;
        State.ArchetypeActiveCounts[archetype]--;
    }

    private void EmitStoryChanged(Simulation simulation, int story, int eventTypeCode, int outcomeCodeId, int tick)
    {
        simulation.EmitStoryChanged(
            tick,
            State.StorySequences[story],
            Data.ArchetypeIds[State.ArchetypeSlots[story]],
            eventTypeCode,
            State.ActIndices[story],
            outcomeCodeId,
            RoleEntity(story, 0),
            RoleEntity(story, 1),
            RoleEntity(story, 2),
            RoleEntity(story, 3));
    }

    private int SelectResolution(Simulation simulation, WorldState world, int story, int archetype)
    {
        var start = Data.ArchetypeResolutionStarts[archetype];
        var end = start + Data.ArchetypeResolutionCounts[archetype];
        var totalWeight = 0;
        for (var resolution = start; resolution < end; resolution++)
        {
            if (Data.ResolutionWhenKinds[resolution] != StoryConditionCodes.None &&
                ConditionHolds(
                    world,
                    story,
                    Data.ResolutionWhenKinds[resolution],
                    Data.ResolutionWhenRoleA[resolution],
                    Data.ResolutionWhenRoleB[resolution],
                    Data.ResolutionWhenRelKindIds[resolution],
                    Data.ResolutionWhenMinStrengths[resolution]))
            {
                return resolution;
            }

            if (Data.ResolutionWhenKinds[resolution] == StoryConditionCodes.None)
            {
                totalWeight += Data.ResolutionWeights[resolution];
            }
        }

        if (totalWeight <= 0)
        {
            return 0;
        }

        var pick = simulation.NextInt(totalWeight);
        for (var resolution = start; resolution < end; resolution++)
        {
            if (Data.ResolutionWhenKinds[resolution] != StoryConditionCodes.None)
            {
                continue;
            }

            pick -= Data.ResolutionWeights[resolution];
            if (pick < 0)
            {
                return resolution;
            }
        }

        return 0;
    }

    private void ApplyResolution(Simulation simulation, WorldState world, int story, int resolution)
    {
        var relStart = Data.ResolutionRelStarts[resolution];
        var relEnd = relStart + Data.ResolutionRelCounts[resolution];
        for (var rel = relStart; rel < relEnd; rel++)
        {
            ApplyResolutionRelationship(simulation, world, story, rel);
        }

        var mask = Data.ResolutionHistoricalRoleMasks[resolution];
        for (var role = 0; role < StoryProgramData.MaxRolesPerArchetype; role++)
        {
            if ((mask & (1 << role)) == 0)
            {
                continue;
            }

            var entityId = RoleEntity(story, role);
            if (entityId > 0 && world.EntityIsActive(entityId))
            {
                KernelEffectExecutor.SetEntityStatus(simulation, entityId, EntityStatusCodes.Historical);
            }
        }
    }

    private void ApplyResolutionRelationship(Simulation simulation, WorldState world, int story, int rel)
    {
        var sourceId = RoleEntity(story, Data.ResolutionRelSourceRoles[rel]);
        var targetId = RoleEntity(story, Data.ResolutionRelTargetRoles[rel]);
        if (sourceId <= 0 || targetId <= 0 ||
            !world.EntityIsActive(sourceId) || !world.EntityIsActive(targetId))
        {
            return;
        }

        if (Data.ResolutionRelCullInstead[rel] != 0)
        {
            var relationshipId = KernelQuery.FindRelationship(world, sourceId, targetId, Data.ResolutionRelKindIds[rel]);
            if (relationshipId != 0)
            {
                KernelEffectExecutor.CullRelationship(simulation, relationshipId);
            }

            return;
        }

        KernelEffectExecutor.CreateRelationship(
            simulation,
            Data.ResolutionRelKindIds[rel],
            Data.ResolutionRelPolarities[rel],
            sourceId,
            targetId,
            Data.ResolutionRelStrengths[rel]);
    }
}
