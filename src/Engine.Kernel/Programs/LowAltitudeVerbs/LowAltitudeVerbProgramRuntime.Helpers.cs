namespace Engine.Kernel;

internal sealed partial class LowAltitudeVerbProgramRuntime
{
    private void EmitIntentTrace(
        Simulation simulation,
        int tick,
        int entityId,
        int rule,
        int intentId,
        int statusCode,
        VerbTargetCandidate candidate)
    {
        simulation.EmitTrace(
            tick,
            intentId,
            entityId,
            TraceSubjectCode(rule),
            rule,
            Data.OperationCode(rule),
            ActionFamilyCodes.LowAltitudeVerb,
            Data.VerbId(rule),
            Data.PlaneCode(rule),
            Data.TargetModeCode(rule),
            candidate.TargetKindId,
            candidate.TargetEntityId,
            statusCode,
            Data.Weight(rule),
            candidate.BaseWeight,
            candidate.Weight,
            candidate.Weight,
            candidate.AffordanceSourceKindCode,
            candidate.AffordanceSourceId,
            candidate.AffordanceShapeCode,
            candidate.AffordanceScore,
            candidate.AffordanceBonus);
    }

    private int TraceSubjectCode(int rule) => Data.OperationCode(rule) switch
    {
        ProgramOpcodeCodes.MoveActor => TraceSubjectCodes.MovementIntent,
        ProgramOpcodeCodes.AdjustRelationshipStrength => TraceSubjectCodes.RelationshipIntent,
        _ => TraceSubjectCodes.InfluenceIntent,
    };

    private static VerbTargetCandidate Candidate(
        int rule,
        int planeCode,
        int targetEntityId,
        int targetKindId,
        int q,
        int r,
        int s,
        float baseWeight) =>
        new(rule, planeCode, targetEntityId, targetKindId, q, r, s, baseWeight, 0f, 0f, 0, 0, 0, baseWeight);

    private static VerbTargetCandidate Candidate(
        int rule,
        int planeCode,
        int targetEntityId,
        int targetKindId,
        int q,
        int r,
        int s,
        float baseWeight,
        AffordanceContribution affordance,
        float affordanceWeight)
    {
        var affordanceBonus = affordance.Score * affordanceWeight;
        var weight = ClampCandidateWeight(baseWeight + affordanceBonus);
        return new(
            rule,
            planeCode,
            targetEntityId,
            targetKindId,
            q,
            r,
            s,
            baseWeight,
            affordance.Score,
            affordanceBonus,
            affordance.SourceKindCode,
            affordance.SourceId,
            affordance.ShapeCode,
            weight);
    }

    private static bool NoCandidate(out VerbTargetCandidate candidate)
    {
        candidate = default;
        return false;
    }

    private static int CurrentQ(WorldState world, int entityId, int planeCode) =>
        planeCode == PlaneCodes.Physical ? world.EntityPhysicalQ(entityId) : world.EntitySemanticQ(entityId);

    private static int CurrentR(WorldState world, int entityId, int planeCode) =>
        planeCode == PlaneCodes.Physical ? world.EntityPhysicalR(entityId) : world.EntitySemanticR(entityId);

    private static int HexDistance(int aq, int ar, int bq, int br)
    {
        var asCoord = -aq - ar;
        var bsCoord = -bq - br;
        return (Abs(aq - bq) + Abs(ar - br) + Abs(asCoord - bsCoord)) / 2;
    }

    private static int MaxAbs(int a, int b, int c) => global::System.Math.Max(Abs(a), global::System.Math.Max(Abs(b), Abs(c)));

    private static int Abs(int value) => value < 0 ? -value : value;

    private static float Clamp01(float value)
    {
        if (value < 0f)
        {
            return 0f;
        }

        return value > 1f ? 1f : value;
    }

    private static int DirectionQ(int direction) => direction switch
    {
        0 => 1,
        1 => 1,
        2 => 0,
        3 => -1,
        4 => -1,
        _ => 0,
    };

    private static int DirectionR(int direction) => direction switch
    {
        0 => 0,
        1 => -1,
        2 => -1,
        3 => 0,
        4 => 1,
        _ => 1,
    };

    private readonly record struct VerbTargetCandidate(
        int RuleSlot,
        int PlaneCode,
        int TargetEntityId,
        int TargetKindId,
        int Q,
        int R,
        int S,
        float BaseWeight,
        float AffordanceScore,
        float AffordanceBonus,
        int AffordanceSourceKindCode,
        int AffordanceSourceId,
        int AffordanceShapeCode,
        float Weight);
}
