namespace Engine.Gameplay;

using Engine.Constants;

internal sealed class LowAltitudeVerbShape
{
    public required LowAltitudeVerbOperation Operation { get; init; }
    public required int OperationCode { get; init; }
    public required IReadOnlySet<LowAltitudeVerbTargetMode> TargetModes { get; init; }
    public bool PhysicalAllowed { get; init; }
    public bool SemanticAllowed { get; init; }
    public bool RequiresInfluenceDelta { get; init; }
    public bool AllowsCoverageGrowth { get; init; }
    public bool AllowsRelocation { get; init; }
    public bool RequiresMapValue { get; init; }
    public bool RequiresRelationshipStrengthDelta { get; init; }
    public required IReadOnlyList<CompiledProgramStep> PrimitiveSteps { get; init; }

    public bool AllowsPlane(string plane) =>
        string.Equals(plane, "semantic", StringComparison.OrdinalIgnoreCase)
            ? SemanticAllowed
            : PhysicalAllowed;

    public static LowAltitudeVerbShape For(LowAltitudeVerbOperation operation) =>
        operation switch
        {
            LowAltitudeVerbOperation.Move => Move,
            LowAltitudeVerbOperation.GrowInfluence => GrowInfluence,
            LowAltitudeVerbOperation.ConsolidateInfluence => ConsolidateInfluence,
            LowAltitudeVerbOperation.ReduceInfluence => ReduceInfluence,
            LowAltitudeVerbOperation.ExpandCoverage => ExpandCoverage,
            LowAltitudeVerbOperation.MoveHeadquarters => MoveHeadquarters,
            LowAltitudeVerbOperation.AdjustRelationshipStrength => AdjustRelationshipStrength,
            _ => throw new ArgumentOutOfRangeException(nameof(operation), operation, null),
        };

    private static readonly LowAltitudeVerbShape Move = new()
    {
        Operation = LowAltitudeVerbOperation.Move,
        OperationCode = ProgramOpcodeCodes.MoveActor,
        TargetModes = new HashSet<LowAltitudeVerbTargetMode>
        {
            LowAltitudeVerbTargetMode.Explore,
            LowAltitudeVerbTargetMode.EntityKind,
            LowAltitudeVerbTargetMode.RelationshipTarget,
            LowAltitudeVerbTargetMode.Terrain,
        },
        PhysicalAllowed = true,
        SemanticAllowed = true,
        PrimitiveSteps = [Step(ProgramOpcodeCodes.MoveActor)],
    };

    private static readonly LowAltitudeVerbShape GrowInfluence = new()
    {
        Operation = LowAltitudeVerbOperation.GrowInfluence,
        OperationCode = ProgramOpcodeCodes.AddOwnedCellScalar,
        TargetModes = new HashSet<LowAltitudeVerbTargetMode>
        {
            LowAltitudeVerbTargetMode.OwnInfluence,
            LowAltitudeVerbTargetMode.EntityKind,
        },
        PhysicalAllowed = true,
        SemanticAllowed = true,
        RequiresInfluenceDelta = true,
        PrimitiveSteps = [Step(ProgramOpcodeCodes.AddOwnedCellScalar)],
    };

    private static readonly LowAltitudeVerbShape ConsolidateInfluence = new()
    {
        Operation = LowAltitudeVerbOperation.ConsolidateInfluence,
        OperationCode = ProgramOpcodeCodes.AddOwnedCellScalar,
        TargetModes = new HashSet<LowAltitudeVerbTargetMode>
        {
            LowAltitudeVerbTargetMode.OwnInfluence,
        },
        PhysicalAllowed = true,
        SemanticAllowed = true,
        RequiresInfluenceDelta = true,
        PrimitiveSteps = [Step(ProgramOpcodeCodes.AddOwnedCellScalar)],
    };

    private static readonly LowAltitudeVerbShape ReduceInfluence = new()
    {
        Operation = LowAltitudeVerbOperation.ReduceInfluence,
        OperationCode = ProgramOpcodeCodes.SubtractOtherCellScalar,
        TargetModes = new HashSet<LowAltitudeVerbTargetMode>
        {
            LowAltitudeVerbTargetMode.ForeignInfluence,
        },
        PhysicalAllowed = true,
        SemanticAllowed = true,
        RequiresInfluenceDelta = true,
        PrimitiveSteps = [Step(ProgramOpcodeCodes.SubtractOtherCellScalar)],
    };

    private static readonly LowAltitudeVerbShape ExpandCoverage = new()
    {
        Operation = LowAltitudeVerbOperation.ExpandCoverage,
        OperationCode = ProgramOpcodeCodes.TryClaimCell,
        TargetModes = new HashSet<LowAltitudeVerbTargetMode>
        {
            LowAltitudeVerbTargetMode.OwnInfluence,
        },
        PhysicalAllowed = true,
        RequiresInfluenceDelta = true,
        AllowsCoverageGrowth = true,
        RequiresMapValue = true,
        PrimitiveSteps =
        [
            Step(ProgramOpcodeCodes.AddOwnedCellScalar),
            Step(ProgramOpcodeCodes.TryClaimCell),
        ],
    };

    private static readonly LowAltitudeVerbShape MoveHeadquarters = new()
    {
        Operation = LowAltitudeVerbOperation.MoveHeadquarters,
        OperationCode = ProgramOpcodeCodes.TryRelocateEntity,
        TargetModes = new HashSet<LowAltitudeVerbTargetMode>
        {
            LowAltitudeVerbTargetMode.OwnInfluence,
        },
        PhysicalAllowed = true,
        RequiresInfluenceDelta = true,
        AllowsRelocation = true,
        RequiresMapValue = true,
        PrimitiveSteps =
        [
            Step(ProgramOpcodeCodes.AddOwnedCellScalar),
            Step(ProgramOpcodeCodes.TryRelocateEntity),
        ],
    };

    private static readonly LowAltitudeVerbShape AdjustRelationshipStrength = new()
    {
        Operation = LowAltitudeVerbOperation.AdjustRelationshipStrength,
        OperationCode = ProgramOpcodeCodes.AdjustRelationshipStrength,
        TargetModes = new HashSet<LowAltitudeVerbTargetMode>
        {
            LowAltitudeVerbTargetMode.RelationshipTarget,
        },
        PhysicalAllowed = true,
        SemanticAllowed = true,
        RequiresRelationshipStrengthDelta = true,
        PrimitiveSteps = [Step(ProgramOpcodeCodes.AdjustRelationshipStrength)],
    };

    private static CompiledProgramStep Step(int opcode) => new(opcode, 0, 0f);
}
