namespace Engine.Domain.Compilation;

using Engine.Behaviors;
using Engine.Constants;
using Engine.Domain.Compilation.KernelPrograms;

public static partial class DomainCompiler
{
    private static bool TryCompileGeographyPrecondition(
        List<BehaviorProgramPreconditionOp> ops,
        IPrecondition precondition,
        DomainIdRegistry ids)
    {
        switch (precondition)
        {
            case SelfHexFactThreshold self:
                ops.Add(HexFactRangeOp(BehaviorProgramPrecondition.SelfHexFactRange, self.Fact, self.Min, self.Max));
                return true;
            case TargetHexFactThreshold target:
                ops.Add(HexFactRangeOp(BehaviorProgramPrecondition.TargetHexFactRange, target.Fact, target.Min, target.Max));
                return true;
            case SelfOnTerrainGroup group:
                ops.Add(new BehaviorProgramPreconditionOp(
                    BehaviorProgramPrecondition.SelfOnTerrainGroup,
                    ids.TerrainBoundaryGroupIdOf(group.BoundaryGroup).Value));
                return true;
            default:
                return false;
        }
    }

    private static BehaviorProgramPreconditionOp HexFactRangeOp(
        BehaviorProgramPrecondition code,
        PhysicalHexFact fact,
        int? min,
        int? max)
    {
        if (min is null && max is null)
        {
            throw new InvalidOperationException($"Hex fact precondition on {fact} must declare a min or a max.");
        }

        return new BehaviorProgramPreconditionOp(
            code,
            HexFactCode(fact),
            min.HasValue ? QuantizeHexFact(fact, min.Value) : 0,
            max.HasValue ? QuantizeHexFact(fact, max.Value) : 255);
    }

    // The authoring enum pins its values to PhysicalHexFactCodes, so the
    // cast is the mapping; the guard catches an out-of-vocabulary cast.
    private static int HexFactCode(PhysicalHexFact fact)
    {
        var code = (int)fact;
        if (code < 0 || code >= PhysicalHexFactCodes.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(fact), fact, null);
        }

        return code;
    }

    // Thresholds are authored on each fact's natural scale and quantized
    // with the same function worldgen used to write the fact column, so a
    // banded precondition and the latent geography always agree on units.
    private static int QuantizeHexFact(PhysicalHexFact fact, int value) => fact switch
    {
        PhysicalHexFact.Elevation => PhysicalHexFactCodes.FromElevation(value),
        PhysicalHexFact.Relief or PhysicalHexFact.Roughness => PhysicalHexFactCodes.FromReliefUnits(value),
        PhysicalHexFact.LandFraction or PhysicalHexFact.RiverCoverage or PhysicalHexFact.FloodplainCoverage =>
            PhysicalHexFactCodes.FromCoverage(value),
        _ => PhysicalHexFactCodes.FromIntensity(value),
    };
}
