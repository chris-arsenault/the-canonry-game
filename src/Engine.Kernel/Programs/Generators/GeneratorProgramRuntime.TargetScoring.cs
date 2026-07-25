namespace Engine.Kernel;

using Engine.Constants;

internal sealed partial class GeneratorProgramRuntime
{
    private const int TargetSelectionBaseWeight = 16;
    private const int TargetSelectionCultureScoreDivisor = 8;
    private const int TargetSelectionMaxCultureBonus = 512;

    private int TargetSelectionWeight(
        Simulation simulation,
        int generatorSlot,
        int targetEntityId,
        int[] refCodes,
        int[] refEntityIds,
        int refCount)
    {
        var bonus = 0;
        var firstProduced = _producedStarts[generatorSlot];
        var endProduced = firstProduced + _producedCounts[generatorSlot];
        for (var produced = firstProduced; produced < endProduced; produced++)
        {
            bonus += ProducedTargetCultureBonus(simulation, produced, refCodes, refEntityIds, refCount);
            if (bonus >= TargetSelectionMaxCultureBonus)
            {
                return ApplyKnowledgeTargetWeight(
                    simulation,
                    generatorSlot,
                    targetEntityId,
                    TargetSelectionBaseWeight + TargetSelectionMaxCultureBonus);
            }
        }

        return ApplyKnowledgeTargetWeight(simulation, generatorSlot, targetEntityId, TargetSelectionBaseWeight + bonus);
    }

    private int ApplyKnowledgeTargetWeight(
        Simulation simulation,
        int generatorSlot,
        int targetEntityId,
        int weight) =>
        (int)FixedScalar.Max(
            1,
            weight * simulation.Knowledge.Modifier(
                simulation.World.EntityCultureId(targetEntityId),
                KnowledgeGrantDomainCodes.GeneratorPulseWeight,
                _generatorIds[generatorSlot]));

    private int ProducedTargetCultureBonus(
        Simulation simulation,
        int produced,
        int[] refCodes,
        int[] refEntityIds,
        int refCount)
    {
        if (_producedCreateIfMissing[produced] == 0 ||
            _producedCreateChances[produced] <= 0 ||
            !CulturePlacementParticipatesInTargetSelection(_producedCulturePlacementCodes[produced]))
        {
            return 0;
        }

        var cultureId = ResolveProducedCultureId(simulation.World, refCodes, refEntityIds, refCount, produced);
        var anchorId = ResolveRef(refCodes, refEntityIds, refCount, _producedPlacementRefCodes[produced]);
        if (cultureId <= 0 || anchorId <= 0)
        {
            return 0;
        }

        var physical = TargetPlaneCultureBonus(simulation, produced, cultureId, anchorId, PlaneCodes.Physical);
        var semantic = TargetPlaneCultureBonus(simulation, produced, cultureId, anchorId, PlaneCodes.Semantic);
        return global::System.Math.Max(physical, semantic);
    }

    private int TargetPlaneCultureBonus(
        Simulation simulation,
        int produced,
        int cultureId,
        int anchorId,
        int planeCode)
    {
        var placementCode = planeCode == PlaneCodes.Physical
            ? _producedPhysicalPlacementCodes[produced]
            : _producedSemanticPlacementCodes[produced];
        if (placementCode != GeneratorPlacementCodes.Area &&
            placementCode != GeneratorPlacementCodes.Locality)
        {
            return 0;
        }

        var q = planeCode == PlaneCodes.Physical
            ? simulation.World.EntityPhysicalQ(anchorId)
            : simulation.World.EntitySemanticQ(anchorId);
        var r = planeCode == PlaneCodes.Physical
            ? simulation.World.EntityPhysicalR(anchorId)
            : simulation.World.EntitySemanticR(anchorId);
        var score = CulturePlacementScore(simulation, planeCode, produced, cultureId, q, r);
        return score <= 0 ? 0 : score / TargetSelectionCultureScoreDivisor;
    }

    private static bool CulturePlacementParticipatesInTargetSelection(int placementCode) =>
        placementCode != GeneratorCulturePlacementCodes.Ignore &&
        placementCode != GeneratorCulturePlacementCodes.HostileIntrusion;
}
