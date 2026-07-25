namespace Engine.Domain.Compilation;

using Engine.Behaviors;
using Engine.Domain.Compilation.KernelPrograms;

public static partial class DomainCompiler
{
    private static void AddAttributeRangePrecondition(
        List<BehaviorProgramPreconditionOp> ops,
        string attributeName,
        float? min,
        float? max,
        DomainIdRegistry ids,
        BehaviorProgramPrecondition operatorCode)
    {
        ops.Add(new BehaviorProgramPreconditionOp(
            operatorCode,
            ids.AttributeIdOf(attributeName).Value,
            min.HasValue ? 1 : 0,
            max.HasValue ? 1 : 0,
            X: min.HasValue
                ? KernelFixedPoint.AttributeScalar(KernelFixedPoint.AttributeUnits(min.Value))
                : FixedScalar.Zero,
            Y: max.HasValue
                ? KernelFixedPoint.AttributeScalar(KernelFixedPoint.AttributeUnits(max.Value))
                : FixedScalar.Zero));
    }

    private static void AddPlaneDistancePrecondition(
        List<BehaviorProgramPreconditionOp> ops,
        WithinPlaneDistancePrecondition distance,
        DomainIdRegistry ids)
    {
        ops.Add(new BehaviorProgramPreconditionOp(
            BehaviorProgramPrecondition.PlaneDistance,
            distance.MaxDistance,
            PlaneCode(distance.Plane),
            ids.StructuralFactIdOf(distance.InRangeFactName).Value));
    }

    private static void AddRelationshipStrengthPrecondition(
        List<BehaviorProgramPreconditionOp> ops,
        string relationshipKind,
        float? minStrength,
        DomainIdRegistry ids,
        BehaviorProgramPrecondition operatorCode)
    {
        ops.Add(new BehaviorProgramPreconditionOp(
            operatorCode,
            ids.RelationshipKindIdOf(relationshipKind).Value,
            minStrength.HasValue ? 1 : 0,
            X: minStrength.HasValue
                ? KernelFixedPoint.RelationshipStrengthScalar(
                    KernelFixedPoint.RelationshipStrengthUnits(minStrength.Value))
                : FixedScalar.Zero));
    }

    private static void AddRelationshipExistsPrecondition(
        List<BehaviorProgramPreconditionOp> ops,
        RelationshipExists relationship,
        DomainIdRegistry ids)
    {
        ops.Add(new BehaviorProgramPreconditionOp(
            BehaviorProgramPrecondition.RelationshipExists,
            ids.RelationshipKindIdOf(relationship.RelationshipKind).Value,
            relationship.MinStrength.HasValue ? 1 : 0,
            relationship.SourceId.Value,
            relationship.TargetId.Value,
            relationship.MinStrength.HasValue
                ? KernelFixedPoint.RelationshipStrengthScalar(
                    KernelFixedPoint.RelationshipStrengthUnits(relationship.MinStrength.Value))
                : FixedScalar.Zero));
    }
}
