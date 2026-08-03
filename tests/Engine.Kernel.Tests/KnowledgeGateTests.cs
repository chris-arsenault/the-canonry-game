namespace Engine.Kernel.Tests;

using Engine.Constants;
using Engine.Kernel;
using Engine.Kernel.Programs;

/// <summary>
/// Knowledge enablement gates: gated content is default-absent and exists
/// only while the relevant culture claims the technology's site.
/// </summary>
public sealed class KnowledgeGateTests
{
    private const int CultureId = 1;
    private const int KindId = 1;
    private const int AttributeId = 10;
    private const int TargetKindId = 1;
    private const int CreatedKindId = 2;
    private const int TargetSubtypeId = 10;
    private const int OtherSubtypeId = 12;
    private const int CreatedSubtypeId = 11;
    private const int GeneratorId = 100;
    private const int CreatedRef = 2;

    [Fact]
    public void CultureClaimsTech_MapsOneBasedIdsAndZeroNeverPasses()
    {
        var simulation = CreateSimulation();
        _ = simulation.RegisterKnowledgeSite(0, 0, tier: 0);
        _ = simulation.RegisterKnowledgeSite(6, 0, tier: 1);

        Claim(simulation, 0, 0);
        simulation.Knowledge.Tick(simulation, tick: 1);

        Assert.True(simulation.Knowledge.CultureClaimsTech(CultureId, techId: 1));
        Assert.False(simulation.Knowledge.CultureClaimsTech(CultureId, techId: 2));
        Assert.False(simulation.Knowledge.CultureClaimsTech(CultureId, techId: 0));
    }

    [Fact]
    public void GambitPrecondition_LocksWithoutClaimAndOpensWithClaim()
    {
        var withoutClaim = RunGatedGambit(claimTech: false);
        var withClaim = RunGatedGambit(claimTech: true);

        Assert.Equal(FixedScalar.FromSingle(10f), withoutClaim);
        Assert.Equal(FixedScalar.FromSingle(30f), withClaim);
    }

    [Fact]
    public void GeneratorRequiredTech_BlocksTargetsOfCulturesWithoutClaim()
    {
        Assert.False(RunGatedGenerator(claimTech: false, requiredTechId: 1, widenTechId: 0, targetSubtypeFilter: 0));
        Assert.True(RunGatedGenerator(claimTech: true, requiredTechId: 1, widenTechId: 0, targetSubtypeFilter: 0));
    }

    [Fact]
    public void GeneratorWidenTech_BypassesSubtypeFilterOnlyWithClaim()
    {
        Assert.False(RunGatedGenerator(claimTech: false, requiredTechId: 0, widenTechId: 1, targetSubtypeFilter: OtherSubtypeId));
        Assert.True(RunGatedGenerator(claimTech: true, requiredTechId: 0, widenTechId: 1, targetSubtypeFilter: OtherSubtypeId));
    }

    [Fact]
    public void MovementTechRule_SwitchesProfileOnlyWhileClaimHolds()
    {
        var simulation = CreateSimulation();
        _ = simulation.RegisterKnowledgeSite(0, 0, tier: 0);
        simulation.Movement.RegisterTechMovementProfile(
            KindId,
            techId: 1,
            PlaneCodes.Physical,
            movementProfileId: 2);
        var entityId = simulation.CreateEntity(KindId, 1, CultureId, 0, 0, 0, 0, 0, 0);

        Assert.Equal(1, simulation.Movement.MovementProfileFor(simulation, entityId, PlaneCodes.Physical));

        Claim(simulation, 0, 0);
        simulation.Knowledge.Tick(simulation, tick: 1);

        Assert.Equal(2, simulation.Movement.MovementProfileFor(simulation, entityId, PlaneCodes.Physical));

        _ = simulation.ModifyCellCultureInfluenceRaw(
            CultureId,
            PlaneCodes.Semantic,
            0,
            0,
            -WorldState.CultureClaimedRawMass,
            tick: 2);
        simulation.Knowledge.Tick(simulation, tick: 2);

        Assert.Equal(1, simulation.Movement.MovementProfileFor(simulation, entityId, PlaneCodes.Physical));
    }

    private static FixedScalar RunGatedGambit(bool claimTech)
    {
        var simulation = CreateSimulation();
        _ = simulation.RegisterKnowledgeSite(0, 0, tier: 0);
        simulation.RegisterAttribute(KindId, AttributeId, FixedScalar.FromSingle(0f), FixedScalar.FromSingle(100f), FixedScalar.FromSingle(10f));
        var entityId = simulation.CreateEntity(KindId, 1, CultureId, 0, 0, 0, 0, 0, 0);

        var gambit = simulation.Gambits.RegisterGambit(
            KindId,
            gambitId: 100,
            targetMode: BehaviorTargetModeCodes.Self,
            successChance: FixedScalar.FromSingle(1f));
        simulation.Gambits.SetConjunctivePreconditions(
            gambit,
            new(BehaviorPreconditionCodes.CultureHoldsTech, BehaviorEndpointCodes.Self, 1));
        simulation.Gambits.AddExpressionOp(gambit, BehaviorExpressionCodes.Literal, x: FixedScalar.FromSingle(1f));
        simulation.Gambits.AddStep(gambit, BehaviorOperationCodes.AddAttribute, AttributeId, x: FixedScalar.FromSingle(20f));

        if (claimTech)
        {
            Claim(simulation, 0, 0);
        }

        simulation.Knowledge.Tick(simulation, tick: 0);
        simulation.AdvanceTicks(1);
        return simulation.World.GetAttribute(entityId, AttributeId);
    }

    private static bool RunGatedGenerator(
        bool claimTech,
        int requiredTechId,
        int widenTechId,
        int targetSubtypeFilter)
    {
        var simulation = CreateSimulation();
        _ = simulation.RegisterKnowledgeSite(0, 0, tier: 0);
        SeedPhysicalMap(simulation);
        var target = simulation.CreateEntity(TargetKindId, TargetSubtypeId, CultureId, 0, 0, 0, 0, 0, tick: 0);

        var slot = simulation.Generators.RegisterGenerator(
            GeneratorId,
            TargetKindId,
            fireWeight: 1);
        simulation.Generators.AddTrigger(slot, TargetKindId);
        if (targetSubtypeFilter > 0)
        {
            simulation.Generators.AddTargetSubtype(slot, targetSubtypeFilter);
        }

        simulation.Generators.SetTargetKnowledgeGates(slot, requiredTechId, widenTechId);
        simulation.Generators.AddProduced(
            slot,
            CreatedRef,
            CreatedKindId,
            CreatedSubtypeId,
            createIfMissing: 1,
            hasQuery: 0,
            queryAdjacentToTarget: 0,
            queryCultureMatchCode: GeneratorCultureMatchCodes.Any,
            physicalPlacementCode: GeneratorPlacementCodes.Area,
            semanticPlacementCode: GeneratorPlacementCodes.Area,
            placementRefCode: GeneratorProgramConstants.TargetReferenceCode,
            cultureRefCode: GeneratorProgramConstants.TargetReferenceCode,
            createChance: FixedScalar.FromSingle(1f),
            coverageModeCode: GeneratorCoverageModeCodes.None,
            coverageSourceRefCode: 0,
            coverageDesiredCellCount: 0,
            coverageMinimumCellCount: 0,
            coverageMinimumSourceRemainingCellCount: 0,
            coverageRespectTerrainBoundaries: 0,
            coverageTransformTerrainId: 0);

        if (claimTech)
        {
            Claim(simulation, 0, 0);
        }

        simulation.Knowledge.Tick(simulation, tick: 0);
        simulation.AdvanceTicks(1);
        return simulation.World.EntityHighWatermark > target;
    }

    private static void SeedPhysicalMap(Simulation simulation)
    {
        simulation.RegisterTerrain(1, boundaryGroupId: 1);
        simulation.World.SetMapRadius(2);
        for (var q = -2; q <= 2; q++)
        {
            var minR = Math.Max(-2, -q - 2);
            var maxR = Math.Min(2, -q + 2);
            for (var r = minR; r <= maxR; r++)
            {
                simulation.CreateCell(q, r, terrainId: 1, ownerEntityId: 0);
            }
        }
    }

    private static Simulation CreateSimulation()
    {
        var simulation = KernelTestSimulation.Create(new SimulationOptions
        {
            EntityCapacity = 16,
            RelationshipCapacity = 16,
            AttributeCapacity = 64,
            MapCellCapacity = 512,
            MapInfluenceCapacity = 512,
            EventCapacity = 1024,
            GambitCapacity = 4,
            GambitStepCapacity = 32,
            GeneratorCapacity = 4,
            GeneratorItemCapacity = 64,
            RelationshipKindIdCapacity = 64,
            KindIdCapacity = 8,
            TerrainIdCapacity = 8,
            SystemCapacity = 32,
            KnowledgeSiteCapacity = 8,
            KnowledgeGrantCapacity = 16,
            KnowledgeModifierCapacity = 32,
            Seed = 1,
        });
        KernelTestSimulation.SeedSemanticCells(simulation, radius: 8);
        return simulation;
    }

    private static void Claim(Simulation simulation, int q, int r) =>
        _ = simulation.ModifyCellCultureInfluenceRaw(
            CultureId,
            PlaneCodes.Semantic,
            q,
            r,
            WorldState.CultureClaimedRawMass,
            tick: 0);
}
