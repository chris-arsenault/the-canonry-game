namespace Engine.Kernel.Tests;

using Engine.Constants;
using Engine.Kernel;

// The terrain-reading gambit gates: a fact band at an entity's feet and a
// terrain boundary-group check. Warm ground passes a geothermal band; cold
// ground blocks it; ground outside the wanted group blocks the group gate.
public sealed class GambitGeographyPreconditionTests
{
    private const int OpenTerrain = 1;
    private const int WarmTerrain = 2;
    private const int OpenGroup = 1;
    private const int WarmGroup = 2;
    private const int AttributeId = 10;

    [Fact]
    public void SelfHexFactRange_PassesOnWarmGroundAndBlocksOnCold()
    {
        var simulation = CreateSimulation();
        InstallGeography(simulation, warmQ: 1, warmR: 0, geothermal: 150);
        const int kindId = 1;
        simulation.RegisterAttribute(kindId, AttributeId, FixedScalar.FromSingle(0f), FixedScalar.FromSingle(100f), FixedScalar.FromSingle(10f));
        var onWarm = simulation.CreateEntity(kindId, 1, 1, 1, 0, 0, 0, 0, 0);
        var onCold = simulation.CreateEntity(kindId, 1, 1, -1, 0, 0, 0, 0, 0);

        var gambit = simulation.Gambits.RegisterGambit(
            kindId,
            gambitId: 100,
            targetMode: BehaviorTargetModeCodes.Self,
            successChance: FixedScalar.FromSingle(1f));
        simulation.Gambits.SetConjunctivePreconditions(
            gambit,
            new(
                BehaviorPreconditionCodes.SelfHexFactRange,
                A: PhysicalHexFactCodes.Geothermal,
                B: 100,
                C: 255));
        simulation.Gambits.AddExpressionOp(gambit, BehaviorExpressionCodes.Literal, x: FixedScalar.FromSingle(1f));
        simulation.Gambits.AddStep(gambit, BehaviorOperationCodes.AddAttribute, AttributeId, x: FixedScalar.FromSingle(20f));

        simulation.AdvanceTicks(1);

        Assert.Equal(FixedScalar.FromSingle(30f), simulation.World.GetAttribute(onWarm, AttributeId));
        Assert.Equal(FixedScalar.FromSingle(10f), simulation.World.GetAttribute(onCold, AttributeId));
    }

    [Fact]
    public void TargetHexFactRange_ReadsTheTargetsGroundNotTheActors()
    {
        var simulation = CreateSimulation();
        InstallGeography(simulation, warmQ: 1, warmR: 0, geothermal: 150);
        const int actorKindId = 1;
        const int targetKindId = 2;
        simulation.RegisterAttribute(actorKindId, AttributeId, FixedScalar.FromSingle(0f), FixedScalar.FromSingle(100f), FixedScalar.FromSingle(10f));
        var actor = simulation.CreateEntity(actorKindId, 1, 1, -1, 0, 0, 0, 0, 0);
        simulation.CreateEntity(targetKindId, 1, 1, 1, 0, 0, 0, 0, 0);

        var gambit = simulation.Gambits.RegisterGambit(
            actorKindId,
            gambitId: 101,
            targetMode: BehaviorTargetModeCodes.KindOther,
            targetKindId: targetKindId,
            successChance: FixedScalar.FromSingle(1f));
        simulation.Gambits.SetConjunctivePreconditions(
            gambit,
            new(BehaviorPreconditionCodes.TargetIsNotSelf),
            new(
                BehaviorPreconditionCodes.TargetHexFactRange,
                A: PhysicalHexFactCodes.Geothermal,
                B: 100,
                C: 255));
        simulation.Gambits.AddExpressionOp(gambit, BehaviorExpressionCodes.Literal, x: FixedScalar.FromSingle(1f));
        simulation.Gambits.AddStep(gambit, BehaviorOperationCodes.AddAttribute, AttributeId, x: FixedScalar.FromSingle(20f));

        simulation.AdvanceTicks(1);

        // The actor stands on cold ground; only the target's warm hex is read.
        Assert.Equal(FixedScalar.FromSingle(30f), simulation.World.GetAttribute(actor, AttributeId));
    }

    [Fact]
    public void SelfOnTerrainGroup_MatchesGroupOfTheCellUnderfoot()
    {
        var simulation = CreateSimulation();
        InstallGeography(simulation, warmQ: 1, warmR: 0, geothermal: 150);
        const int wantsWarmKind = 1;
        const int wantsOpenKind = 2;
        simulation.RegisterAttribute(wantsWarmKind, AttributeId, FixedScalar.FromSingle(0f), FixedScalar.FromSingle(100f), FixedScalar.FromSingle(10f));
        simulation.RegisterAttribute(wantsOpenKind, AttributeId, FixedScalar.FromSingle(0f), FixedScalar.FromSingle(100f), FixedScalar.FromSingle(10f));
        var onWarmGround = simulation.CreateEntity(wantsWarmKind, 1, 1, 1, 0, 0, 0, 0, 0);
        var alsoOnWarmGround = simulation.CreateEntity(wantsOpenKind, 1, 1, 1, 1, 0, 0, 0, 0);

        var wantsWarm = simulation.Gambits.RegisterGambit(
            wantsWarmKind,
            gambitId: 102,
            targetMode: BehaviorTargetModeCodes.Self,
            successChance: FixedScalar.FromSingle(1f));
        simulation.Gambits.SetConjunctivePreconditions(
            wantsWarm,
            new(BehaviorPreconditionCodes.SelfOnTerrainGroup, A: WarmGroup));
        simulation.Gambits.AddExpressionOp(wantsWarm, BehaviorExpressionCodes.Literal, x: FixedScalar.FromSingle(1f));
        simulation.Gambits.AddStep(wantsWarm, BehaviorOperationCodes.AddAttribute, AttributeId, x: FixedScalar.FromSingle(20f));

        var wantsOpen = simulation.Gambits.RegisterGambit(
            wantsOpenKind,
            gambitId: 103,
            targetMode: BehaviorTargetModeCodes.Self,
            successChance: FixedScalar.FromSingle(1f));
        simulation.Gambits.SetConjunctivePreconditions(
            wantsOpen,
            new(BehaviorPreconditionCodes.SelfOnTerrainGroup, A: OpenGroup));
        simulation.Gambits.AddExpressionOp(wantsOpen, BehaviorExpressionCodes.Literal, x: FixedScalar.FromSingle(1f));
        simulation.Gambits.AddStep(wantsOpen, BehaviorOperationCodes.AddAttribute, AttributeId, x: FixedScalar.FromSingle(20f));

        simulation.AdvanceTicks(1);

        Assert.Equal(FixedScalar.FromSingle(30f), simulation.World.GetAttribute(onWarmGround, AttributeId));
        // (1,1) is warm-group ground too, so the open-group gate blocks there.
        Assert.Equal(FixedScalar.FromSingle(10f), simulation.World.GetAttribute(alsoOnWarmGround, AttributeId));
    }

    // A radius-2 field of open terrain with one warm patch: the flagged hex
    // and its column neighbour (1,1) carry WarmTerrain (its own boundary
    // group) and the given geothermal fact; everything else reads zero.
    private static void InstallGeography(Simulation simulation, int warmQ, int warmR, byte geothermal)
    {
        simulation.RegisterTerrain(
            OpenTerrain, boundaryGroupId: OpenGroup, movementCost: FixedScalar.FromSingle(3f),
            influenceMultiplier: FixedScalar.FromSingle(1f), claimCostMultiplier: FixedScalar.FromSingle(1f), claimChanceMultiplier: FixedScalar.FromSingle(1f),
            roadBuildMultiplier: FixedScalar.FromSingle(1f), roadSpeedMultiplier: FixedScalar.FromSingle(1f));
        simulation.RegisterTerrain(
            WarmTerrain, boundaryGroupId: WarmGroup, movementCost: FixedScalar.FromSingle(3f),
            influenceMultiplier: FixedScalar.FromSingle(1f), claimCostMultiplier: FixedScalar.FromSingle(1f), claimChanceMultiplier: FixedScalar.FromSingle(1f),
            roadBuildMultiplier: FixedScalar.FromSingle(1f), roadSpeedMultiplier: FixedScalar.FromSingle(1f));
        simulation.KernelData.PhysicalGeography.BeginInstall(4);
        simulation.World.SetMapRadius(4);
        ForEachHex(2, (q, r) =>
        {
            var warm = (q == warmQ && r == warmR) || (q == warmQ && r == warmR + 1);
            var facts = new byte[PhysicalHexFactCodes.Count];
            if (warm)
            {
                facts[PhysicalHexFactCodes.Geothermal] = geothermal;
            }

            var terrainId = warm ? WarmTerrain : OpenTerrain;
            simulation.KernelData.PhysicalGeography.InstallCell(
                q, r, terrainId, superregionId: 1, continentId: 1,
                crossingWord: 0, fieldWord: 0, initialCoverTerrainId: 0, facts);
            simulation.SeedCell(q, r, terrainId, ownerEntityId: 0);
        });
    }

    private static void ForEachHex(int radius, global::System.Action<int, int> visit)
    {
        for (var q = -radius; q <= radius; q++)
        {
            for (var r = -radius; r <= radius; r++)
            {
                if ((global::System.Math.Abs(q) + global::System.Math.Abs(r) + global::System.Math.Abs(q + r)) / 2 <= radius)
                {
                    visit(q, r);
                }
            }
        }
    }

    private static Simulation CreateSimulation() => KernelTestSimulation.Create(new SimulationOptions
    {
        EntityCapacity = 16,
        RelationshipCapacity = 16,
        AttributeCapacity = 64,
        MapCellCapacity = 1024,
        EventCapacity = 512,
        GambitCapacity = 4,
        GambitStepCapacity = 32,
        GeneratorCapacity = 4,
        RelationshipKindIdCapacity = 64,
        TerrainIdCapacity = 8,
        SystemCapacity = 24,
        MapGenerationRadiusCapacity = 8,
    });
}
