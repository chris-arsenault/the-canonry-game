namespace Engine.Kernel;

internal sealed partial class ChronicleProgramRuntime
{
    private int _openArchetypeSlot;
    private int _openBeatSlot;

    public int AddArchetype(
        int archetypeId,
        int maxActive,
        int castingCadenceTicks,
        int primaryKindId,
        int primaryRequiredRelKindId,
        float primaryRequiredRelMinStrength,
        int fadeOutcomeCodeId)
    {
        ValidatePositive(archetypeId);
        ValidatePositive(maxActive);
        ValidatePositive(castingCadenceTicks);
        ValidatePositive(primaryKindId);
        ValidatePositive(fadeOutcomeCodeId);
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
        Data.ArchetypeFadeOutcomeCodeIds[slot] = fadeOutcomeCodeId;
        Data.ArchetypeRoleStarts[slot] = Data.RoleCount + 1;
        Data.ArchetypeBeatStarts[slot] = Data.BeatCount + 1;
        _openArchetypeSlot = slot;
        _openBeatSlot = 0;
        AddRole(slot, primaryKindId, 0, 0, 0f);
        return slot;
    }

    public void AddRole(int archetypeSlot, int kindId, int relatedKindId, int relatedIncoming, float relatedMinStrength, int open = 0)
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
        Data.RoleOpen[Data.RoleCount] = open;
        Data.ArchetypeRoleCounts[archetypeSlot]++;
    }

    public int AddBeat(
        int archetypeSlot,
        int beatCodeId,
        float heatGain,
        int terminalOutcomeCodeId,
        int openKind,
        int openRoleA,
        int openRoleB,
        int openRelOrAttrId,
        float openValue,
        int strikeKind,
        int strikeActorRole,
        int strikeTargetRole,
        int strikeGambitId,
        int strikeAcceptFailure,
        int strikeJoinRole,
        int strikeFactKind,
        int strikeFactRoleA,
        int strikeFactRoleB,
        int strikeFactRelOrAttrId,
        float strikeFactValue)
    {
        EnsureOpenArchetype(archetypeSlot);
        ValidatePositive(beatCodeId);
        if (Data.BeatCount + 1 >= Data.BeatCodeIds.Length ||
            Data.ArchetypeBeatCounts[archetypeSlot] >= ChronicleProgramData.MaxBeatsPerArchetype)
        {
            throw new global::System.InvalidOperationException();
        }

        Data.BeatCount++;
        var beat = Data.BeatCount;
        Data.BeatCodeIds[beat] = beatCodeId;
        Data.BeatHeatGains[beat] = heatGain;
        Data.BeatTerminalOutcomeCodeIds[beat] = terminalOutcomeCodeId;
        Data.BeatOpenKinds[beat] = openKind;
        Data.BeatOpenRoleA[beat] = openRoleA;
        Data.BeatOpenRoleB[beat] = openRoleB;
        Data.BeatOpenRelOrAttrIds[beat] = openRelOrAttrId;
        Data.BeatOpenValues[beat] = openValue;
        Data.BeatStrikeKinds[beat] = strikeKind;
        Data.BeatStrikeActorRoles[beat] = strikeActorRole;
        Data.BeatStrikeTargetRoles[beat] = strikeTargetRole;
        Data.BeatStrikeGambitIds[beat] = strikeGambitId;
        Data.BeatStrikeAcceptFailure[beat] = strikeAcceptFailure;
        Data.BeatJoinRoles[beat] = strikeJoinRole;
        Data.BeatStrikeFactKinds[beat] = strikeFactKind;
        Data.BeatStrikeFactRoleA[beat] = strikeFactRoleA;
        Data.BeatStrikeFactRoleB[beat] = strikeFactRoleB;
        Data.BeatStrikeFactRelOrAttrIds[beat] = strikeFactRelOrAttrId;
        Data.BeatStrikeFactValues[beat] = strikeFactValue;
        Data.BeatChargeStarts[beat] = Data.ChargeCount + 1;
        Data.BeatGrantStarts[beat] = Data.GrantCount + 1;
        Data.ArchetypeBeatCounts[archetypeSlot]++;
        _openBeatSlot = beat;
        return beat;
    }

    public void AddBeatCharge(int beatSlot, int actorRole, int targetRole, int gambitId, float scoreBonus, int heatScaled)
    {
        EnsureOpenBeat(beatSlot);
        if (Data.ChargeCount + 1 >= Data.ChargeActorRoles.Length)
        {
            throw new global::System.InvalidOperationException();
        }

        Data.ChargeCount++;
        Data.ChargeActorRoles[Data.ChargeCount] = actorRole;
        Data.ChargeTargetRoles[Data.ChargeCount] = targetRole;
        Data.ChargeGambitIds[Data.ChargeCount] = gambitId;
        Data.ChargeScoreBonuses[Data.ChargeCount] = scoreBonus;
        Data.ChargeHeatScaled[Data.ChargeCount] = heatScaled;
        Data.BeatChargeCounts[beatSlot]++;
    }

    public void AddBeatGrant(int beatSlot, int role, int gambitId)
    {
        EnsureOpenBeat(beatSlot);
        ValidatePositive(gambitId);
        if (Data.GrantCount + 1 >= Data.GrantRoles.Length)
        {
            throw new global::System.InvalidOperationException();
        }

        Data.GrantCount++;
        Data.GrantRoles[Data.GrantCount] = role;
        Data.GrantGambitIds[Data.GrantCount] = gambitId;
        Data.BeatGrantCounts[beatSlot]++;
    }

    private void EnsureOpenArchetype(int archetypeSlot)
    {
        if (archetypeSlot != _openArchetypeSlot || archetypeSlot <= 0)
        {
            throw new global::System.InvalidOperationException();
        }
    }

    private void EnsureOpenBeat(int beatSlot)
    {
        if (beatSlot != _openBeatSlot || beatSlot <= 0 || beatSlot != Data.BeatCount)
        {
            throw new global::System.InvalidOperationException();
        }
    }

    private static void ValidatePositive(int value)
    {
        global::System.ArgumentOutOfRangeException.ThrowIfNegativeOrZero(value);
    }
}
