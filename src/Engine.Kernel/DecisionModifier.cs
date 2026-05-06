namespace Engine.Kernel;

internal readonly struct DecisionModifier
{
    public DecisionModifier(
        int subjectId,
        int kindId,
        int traitId,
        int bearerCode,
        float value0 = 0f,
        float value1 = 0f,
        float value2 = 0f)
    {
        SubjectId = subjectId;
        KindId = kindId;
        TraitId = traitId;
        BearerCode = bearerCode;
        Value0 = value0;
        Value1 = value1;
        Value2 = value2;
    }

    public int SubjectId { get; }

    public int KindId { get; }

    public int TraitId { get; }

    public int BearerCode { get; }

    public float Value0 { get; }

    public float Value1 { get; }

    public float Value2 { get; }

    public bool AppliesToGambit(WorldState world, int gambitId, int actorId, int targetId) =>
        SubjectId == gambitId && BearerHasTrait(world, actorId, targetId);

    public bool AppliesToLowAltitudeVerb(WorldState world, int kindId, int verbId, int actorId) =>
        KindId == kindId &&
        (SubjectId == 0 || SubjectId == verbId) &&
        actorId > 0 &&
        world.EntityIsActive(actorId) &&
        world.HasTrait(actorId, TraitId);

    private bool BearerHasTrait(WorldState world, int actorId, int targetId)
    {
        var bearerId = BearerCode == TraitEffectBearerCodes.GambitTarget ? targetId : actorId;
        return bearerId > 0 &&
            world.EntityIsActive(bearerId) &&
            world.HasTrait(bearerId, TraitId);
    }
}

internal delegate DecisionModifier DecisionModifierReader(int modifier);

internal static class DecisionModifierEvaluation
{
    public static float SumGambitValue(
        WorldState world,
        int modifierCount,
        DecisionModifierReader read,
        int gambitId,
        int actorId,
        int targetId,
        bool useValue1)
    {
        var total = 0f;
        for (var modifierSlot = 1; modifierSlot <= modifierCount; modifierSlot++)
        {
            var modifier = read(modifierSlot);
            if (!modifier.AppliesToGambit(world, gambitId, actorId, targetId))
            {
                continue;
            }

            total += useValue1 ? modifier.Value1 : modifier.Value0;
        }

        return total;
    }

    public static LowAltitudeDecisionModifierTotals SumLowAltitudeValues(
        WorldState world,
        int modifierCount,
        DecisionModifierReader read,
        int kindId,
        int verbId,
        int actorId)
    {
        var totals = new LowAltitudeDecisionModifierTotals();
        for (var modifierSlot = 1; modifierSlot <= modifierCount; modifierSlot++)
        {
            var modifier = read(modifierSlot);
            if (modifier.AppliesToLowAltitudeVerb(world, kindId, verbId, actorId))
            {
                totals.Add(modifier);
            }
        }

        return totals;
    }
}

internal struct LowAltitudeDecisionModifierTotals
{
    public float Weight;
    public float OpportunityBonus;
    public float FitnessWeight;

    public void Add(in DecisionModifier modifier)
    {
        Weight += modifier.Value0;
        OpportunityBonus += modifier.Value1;
        FitnessWeight += modifier.Value2;
    }
}
