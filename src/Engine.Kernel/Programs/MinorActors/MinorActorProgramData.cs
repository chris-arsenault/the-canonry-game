namespace Engine.Kernel;

using Engine.Constants;

internal sealed class MinorActorProgramData : IKernelProgramData
{
    internal readonly int[] SourceKindIds;
    internal readonly int[] SourceSubtypeIds;
    internal readonly int[] RoleCodes;
    internal readonly int[] PlaneCodes;
    internal readonly int[] AssignmentRadii;
    internal readonly int[] Strides;
    internal readonly int[] CommitmentTicks;
    internal readonly int[] ReplanTicks;
    internal readonly FixedScalar[] BaseStrengths;
    internal readonly int[] FirstRuleBySourceKind;
    internal readonly int[] LastRuleBySourceKind;
    internal readonly int[] NextRuleBySourceKind;

    public MinorActorProgramData(int ruleCapacity, int kindCapacity = 256)
    {
        NumericGuard.ThrowIfNegativeOrZero(ruleCapacity);
        NumericGuard.ThrowIfNegativeOrZero(kindCapacity);
        SourceKindIds = new int[ruleCapacity + 1];
        SourceSubtypeIds = new int[ruleCapacity + 1];
        RoleCodes = new int[ruleCapacity + 1];
        PlaneCodes = new int[ruleCapacity + 1];
        AssignmentRadii = new int[ruleCapacity + 1];
        Strides = new int[ruleCapacity + 1];
        CommitmentTicks = new int[ruleCapacity + 1];
        ReplanTicks = new int[ruleCapacity + 1];
        BaseStrengths = new FixedScalar[ruleCapacity + 1];
        FirstRuleBySourceKind = new int[kindCapacity + 1];
        LastRuleBySourceKind = new int[kindCapacity + 1];
        NextRuleBySourceKind = new int[ruleCapacity + 1];
    }

    public int ProgramId => KernelProgramCodes.MinorActors;

    public int SchemaVersion => 6;

    public int RuleCapacity => SourceKindIds.Length - 1;

    public int RuleCount { get; private set; }

    internal int CampaignRelationshipKindId;

    public int RegisterRoleRule(
        int sourceKindId,
        int sourceSubtypeId,
        int roleCode,
        int planeCode,
        int assignmentRadius,
        int stride,
        int commitmentTicks,
        int replanTicks,
        FixedScalar baseStrength)
    {
        ValidateRule(
            sourceKindId,
            sourceSubtypeId,
            roleCode,
            planeCode,
            assignmentRadius,
            stride,
            commitmentTicks,
            replanTicks,
            baseStrength);
        if (RuleCount >= RuleCapacity)
        {
            throw new global::System.InvalidOperationException();
        }

        var rule = ++RuleCount;
        SourceKindIds[rule] = sourceKindId;
        SourceSubtypeIds[rule] = sourceSubtypeId;
        RoleCodes[rule] = roleCode;
        PlaneCodes[rule] = planeCode;
        AssignmentRadii[rule] = assignmentRadius;
        Strides[rule] = stride;
        CommitmentTicks[rule] = commitmentTicks;
        ReplanTicks[rule] = replanTicks;
        BaseStrengths[rule] = baseStrength;
        AppendRuleIndex(sourceKindId, rule);
        return rule;
    }

    public int FirstRuleForSourceKind(int kindId) =>
        (uint)kindId < (uint)FirstRuleBySourceKind.Length
            ? FirstRuleBySourceKind[kindId]
            : 0;

    public int NextRuleForSourceKind(int rule) => NextRuleBySourceKind[rule];

    public bool RuleMatches(WorldState world, int entityId, int rule)
    {
        if (!world.EntityIsActive(entityId) ||
            world.EntityKindId(entityId) != SourceKindIds[rule])
        {
            return false;
        }

        var subtypeId = SourceSubtypeIds[rule];
        return subtypeId == 0 || world.EntitySubtypeId(entityId) == subtypeId;
    }

    private void AppendRuleIndex(int sourceKindId, int rule)
    {
        if ((uint)sourceKindId >= (uint)FirstRuleBySourceKind.Length)
        {
            throw new global::System.InvalidOperationException();
        }

        var previous = LastRuleBySourceKind[sourceKindId];
        if (previous > 0)
        {
            NextRuleBySourceKind[previous] = rule;
        }
        else
        {
            FirstRuleBySourceKind[sourceKindId] = rule;
        }

        LastRuleBySourceKind[sourceKindId] = rule;
    }

    private static void ValidateRule(
        int sourceKindId,
        int sourceSubtypeId,
        int roleCode,
        int planeCode,
        int assignmentRadius,
        int stride,
        int commitmentTicks,
        int replanTicks,
        FixedScalar baseStrength)
    {
        NumericGuard.ThrowIfNegativeOrZero(sourceKindId);
        NumericGuard.ThrowIfNegative(sourceSubtypeId);
        if (roleCode is not (
                MinorActorRoleCodes.CulturalFront or
                MinorActorRoleCodes.RoadCrew or
                MinorActorRoleCodes.CampaignColumn))
        {
            throw new global::System.ArgumentOutOfRangeException(nameof(roleCode));
        }

        if (planeCode is not (
                Engine.Constants.PlaneCodes.Physical or
                Engine.Constants.PlaneCodes.Semantic))
        {
            throw new global::System.ArgumentOutOfRangeException(nameof(planeCode));
        }

        NumericGuard.ThrowIfNegativeOrZero(assignmentRadius);
        NumericGuard.ThrowIfNegativeOrZero(stride);
        NumericGuard.ThrowIfNegativeOrZero(commitmentTicks);
        NumericGuard.ThrowIfNegativeOrZero(replanTicks);
        NumericGuard.ThrowIfNegativeOrZero(baseStrength);
    }
}
