namespace Engine.Kernel;

internal sealed partial class ChronicleProgramRuntime
{
    private int _openArchetypeSlot;

    public int AddArchetype(
        int archetypeId,
        int maxActive,
        int castingCadenceTicks,
        int primaryKindId,
        int primaryRequiredRelKindId,
        float primaryRequiredRelMinStrength)
    {
        ValidatePositive(archetypeId);
        ValidatePositive(maxActive);
        ValidatePositive(castingCadenceTicks);
        ValidatePositive(primaryKindId);
        if (Data.ArchetypeCount + 1 >= Data.ArchetypeIds.Length)
        {
            throw new global::System.InvalidOperationException();
        }

        Data.ArchetypeCount++;
        var slot = Data.ArchetypeCount;
        Data.ArchetypeIds[slot] = archetypeId;
        Data.ArchetypeMaxActive[slot] = maxActive;
        Data.ArchetypeCastingCadenceTicks[slot] = castingCadenceTicks;
        Data.ArchetypePrimaryKindIds[slot] = primaryKindId;
        Data.ArchetypePrimaryRequiredRelKindIds[slot] = primaryRequiredRelKindId;
        Data.ArchetypePrimaryRequiredRelMinStrengths[slot] = primaryRequiredRelMinStrength;
        Data.ArchetypeRoleStarts[slot] = Data.RoleCount + 1;
        Data.ArchetypeActStarts[slot] = Data.ActCount + 1;
        Data.ArchetypeResolutionStarts[slot] = Data.ResolutionCount + 1;
        _openArchetypeSlot = slot;
        AddRole(slot, primaryKindId, 0, 0, 0f);
        return slot;
    }

    public void AddRole(int archetypeSlot, int kindId, int relatedKindId, int relatedIncoming, float relatedMinStrength)
    {
        EnsureOpenArchetype(archetypeSlot);
        ValidatePositive(kindId);
        if (Data.RoleCount + 1 >= Data.RoleKindIds.Length ||
            Data.ArchetypeRoleCounts[archetypeSlot] >= ChronicleProgramData.MaxRolesPerArchetype)
        {
            throw new global::System.InvalidOperationException();
        }

        Data.RoleCount++;
        Data.RoleKindIds[Data.RoleCount] = kindId;
        Data.RoleRelatedKindIds[Data.RoleCount] = relatedKindId;
        Data.RoleRelatedIncoming[Data.RoleCount] = relatedIncoming;
        Data.RoleRelatedMinStrengths[Data.RoleCount] = relatedMinStrength;
        Data.ArchetypeRoleCounts[archetypeSlot]++;
    }

    public int AddAct(
        int archetypeSlot,
        int deadlineTicks,
        int advanceConditionKind,
        int advanceRoleA,
        int advanceRoleB,
        int advanceRelKindId,
        float advanceMinStrength)
    {
        EnsureOpenArchetype(archetypeSlot);
        ValidatePositive(deadlineTicks);
        if (Data.ActCount + 1 >= Data.ActDeadlineTicks.Length)
        {
            throw new global::System.InvalidOperationException();
        }

        Data.ActCount++;
        var act = Data.ActCount;
        Data.ActDeadlineTicks[act] = deadlineTicks;
        Data.ActAdvanceConditionKinds[act] = advanceConditionKind;
        Data.ActAdvanceRoleA[act] = advanceRoleA;
        Data.ActAdvanceRoleB[act] = advanceRoleB;
        Data.ActAdvanceRelKindIds[act] = advanceRelKindId;
        Data.ActAdvanceMinStrengths[act] = advanceMinStrength;
        Data.ActBiasStarts[act] = Data.BiasCount + 1;
        Data.ActSustainStarts[act] = Data.SustainCount + 1;
        Data.ArchetypeActCounts[archetypeSlot]++;
        return act;
    }

    public void AddActBias(int actSlot, int actorRole, int targetRole, int gambitId, float scoreBonus)
    {
        EnsureOpenAct(actSlot);
        if (Data.BiasCount + 1 >= Data.BiasActorRoles.Length)
        {
            throw new global::System.InvalidOperationException();
        }

        Data.BiasCount++;
        Data.BiasActorRoles[Data.BiasCount] = actorRole;
        Data.BiasTargetRoles[Data.BiasCount] = targetRole;
        Data.BiasGambitIds[Data.BiasCount] = gambitId;
        Data.BiasScoreBonuses[Data.BiasCount] = scoreBonus;
        Data.ActBiasCounts[actSlot]++;
    }

    public void AddActSustain(int actSlot, int role, int attributeId, float deltaPerPulse)
    {
        EnsureOpenAct(actSlot);
        ValidatePositive(attributeId);
        if (Data.SustainCount + 1 >= Data.SustainRoles.Length)
        {
            throw new global::System.InvalidOperationException();
        }

        Data.SustainCount++;
        Data.SustainRoles[Data.SustainCount] = role;
        Data.SustainAttributeIds[Data.SustainCount] = attributeId;
        Data.SustainDeltas[Data.SustainCount] = deltaPerPulse;
        Data.ActSustainCounts[actSlot]++;
    }

    public int AddResolution(
        int archetypeSlot,
        int outcomeCodeId,
        int weight,
        int whenKind,
        int whenRoleA,
        int whenRoleB,
        int whenRelKindId,
        float whenMinStrength,
        int historicalRoleMask)
    {
        EnsureOpenArchetype(archetypeSlot);
        ValidatePositive(outcomeCodeId);
        if (Data.ResolutionCount + 1 >= Data.ResolutionOutcomeCodeIds.Length)
        {
            throw new global::System.InvalidOperationException();
        }

        Data.ResolutionCount++;
        var resolution = Data.ResolutionCount;
        Data.ResolutionOutcomeCodeIds[resolution] = outcomeCodeId;
        Data.ResolutionWeights[resolution] = weight;
        Data.ResolutionWhenKinds[resolution] = whenKind;
        Data.ResolutionWhenRoleA[resolution] = whenRoleA;
        Data.ResolutionWhenRoleB[resolution] = whenRoleB;
        Data.ResolutionWhenRelKindIds[resolution] = whenRelKindId;
        Data.ResolutionWhenMinStrengths[resolution] = whenMinStrength;
        Data.ResolutionRelStarts[resolution] = Data.ResolutionRelCount + 1;
        Data.ResolutionHistoricalRoleMasks[resolution] = historicalRoleMask;
        Data.ArchetypeResolutionCounts[archetypeSlot]++;
        return resolution;
    }

    public void AddResolutionRelationship(
        int resolutionSlot,
        int sourceRole,
        int targetRole,
        int relKindId,
        int polarityCode,
        float strength,
        int cullInstead)
    {
        EnsureOpenResolution(resolutionSlot);
        ValidatePositive(relKindId);
        if (Data.ResolutionRelCount + 1 >= Data.ResolutionRelKindIds.Length)
        {
            throw new global::System.InvalidOperationException();
        }

        Data.ResolutionRelCount++;
        Data.ResolutionRelSourceRoles[Data.ResolutionRelCount] = sourceRole;
        Data.ResolutionRelTargetRoles[Data.ResolutionRelCount] = targetRole;
        Data.ResolutionRelKindIds[Data.ResolutionRelCount] = relKindId;
        Data.ResolutionRelPolarities[Data.ResolutionRelCount] = polarityCode;
        Data.ResolutionRelStrengths[Data.ResolutionRelCount] = strength;
        Data.ResolutionRelCullInstead[Data.ResolutionRelCount] = cullInstead;
        Data.ResolutionRelCounts[resolutionSlot]++;
    }

    private void EnsureOpenArchetype(int archetypeSlot)
    {
        if (archetypeSlot != _openArchetypeSlot || archetypeSlot <= 0)
        {
            throw new global::System.InvalidOperationException();
        }
    }

    private void EnsureOpenAct(int actSlot)
    {
        if (actSlot != Data.ActCount || actSlot <= 0)
        {
            throw new global::System.InvalidOperationException();
        }
    }

    private void EnsureOpenResolution(int resolutionSlot)
    {
        if (resolutionSlot != Data.ResolutionCount || resolutionSlot <= 0)
        {
            throw new global::System.InvalidOperationException();
        }
    }

    private static void ValidatePositive(int value)
    {
        global::System.ArgumentOutOfRangeException.ThrowIfNegativeOrZero(value);
    }
}
