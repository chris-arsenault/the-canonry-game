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
        VerbTargetCandidate candidate,
        int selectionBucketCode = LowAltitudeVerbSelectionBucketCodes.None)
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
            candidate.AffordanceSourceKindMask,
            candidate.AffordanceShapeMask,
            candidate.AffordanceContributionCount,
            candidate.GeneratorAffordanceCount,
            candidate.GambitAffordanceCount,
            candidate.HiddenAffordanceCount,
            selectionBucketCode,
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
        FixedScalar baseWeight,
        AffordanceContribution affordance,
        FixedScalar opportunityBonus)
    {
        var affordanceBonus = affordance.Score * opportunityBonus;
        var weight = ClampCandidateWeight(baseWeight * (1 + affordanceBonus));
        var hiddenAffordanceCount = affordance.ContributionCount > 0
            ? affordance.ContributionCount - 1
            : 0;
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
            affordance.SourceKindMask,
            affordance.ShapeMask,
            affordance.ContributionCount,
            affordance.GeneratorContributionCount,
            affordance.GambitContributionCount,
            hiddenAffordanceCount,
            weight);
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

    private static int Abs(int value) => value < 0 ? -value : value;

    private int CellTargetSearchRadius(WorldState world, int rule, int targetMode)
    {
        var planeRadius = Data.PlaneCode(rule) == PlaneCodes.Semantic
            ? EffectiveSemanticRadius(world)
            : world.PhysicalMapRadius;
        var distanceScale = EffectiveDistanceScale(rule, targetMode);
        var scaledRadius = distanceScale > 0
            ? (int)FixedScalar.Ceiling(distanceScale * 2)
            : 1;
        var radius = global::System.Math.Max(1, global::System.Math.Max(scaledRadius, Data.TargetRadius(rule)));
        return global::System.Math.Min(planeRadius, radius);
    }

    private bool CellTargetCoordinateAllowed(WorldState world, int planeCode, int q, int r) =>
        planeCode == PlaneCodes.Semantic
            ? SemanticCoordinateAllowed(world, q, r)
            : world.CellIsKnown(q, r);

    private static int TargetS(int planeCode, int q, int r) =>
        planeCode == PlaneCodes.Semantic ? -q - r : 0;

    private static FixedScalar Clamp01(FixedScalar value)
    {
        if (value < 0)
        {
            return 0;
        }

        return value > 1 ? 1 : value;
    }

    private readonly record struct VerbTargetCandidate(
        int RuleSlot,
        int PlaneCode,
        int TargetEntityId,
        int TargetKindId,
        int Q,
        int R,
        int S,
        FixedScalar BaseWeight,
        FixedScalar AffordanceScore,
        FixedScalar AffordanceBonus,
        int AffordanceSourceKindCode,
        int AffordanceSourceId,
        int AffordanceShapeCode,
        int AffordanceSourceKindMask,
        int AffordanceShapeMask,
        int AffordanceContributionCount,
        int GeneratorAffordanceCount,
        int GambitAffordanceCount,
        int HiddenAffordanceCount,
        FixedScalar Weight);
}
