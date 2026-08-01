namespace Engine.Kernel.Tests;

using Engine.Constants;
using Engine.Kernel;

public sealed class EntityTagTests
{
    private const int SettlementKindId = 1;
    private const int RaiderKindId = 2;
    private const int PreysOnKindId = 5;
    private const int TerrifiedTagId = 3;
    private const int CultureId = 1;

    [Fact]
    public void RelationshipTagRule_SetsAndClearsWithHysteresis()
    {
        var simulation = CreateSimulation();
        var settlement = simulation.CreateEntity(SettlementKindId, 1, CultureId, 0, 0, 0, 0, 0, tick: 0);
        var raider = simulation.CreateEntity(RaiderKindId, 1, CultureId, 1, 0, 0, 0, 0, tick: 0);
        _ = simulation.Effects.RegisterRelationshipTagRule(
            SettlementKindId,
            TerrifiedTagId,
            PreysOnKindId,
            RelationshipMatchDirectionCodes.Incoming,
            setAtOrAbove: FixedScalar.FromSingle(0.6f),
            clearBelow: FixedScalar.FromSingle(0.3f));

        var predation = simulation.CreateRelationship(PreysOnKindId, 0, raider, settlement, FixedScalar.FromSingle(0.8f), tick: 0);
        simulation.AdvanceTicks(1);
        Assert.True(simulation.World.EntityHasTag(settlement, TerrifiedTagId));

        // Between clear and set thresholds: hysteresis holds the tag.
        simulation.SetRelationshipStrength(predation, FixedScalar.FromSingle(0.45f), tick: 1);
        simulation.AdvanceTicks(1);
        Assert.True(simulation.World.EntityHasTag(settlement, TerrifiedTagId));

        simulation.SetRelationshipStrength(predation, FixedScalar.FromSingle(0.1f), tick: 2);
        simulation.AdvanceTicks(1);
        Assert.False(simulation.World.EntityHasTag(settlement, TerrifiedTagId));
    }

    [Fact]
    public void GeneratorTargetTagRequirement_GatesEligibility()
    {
        var simulation = CreateSimulation();
        SeedMap(simulation);
        var settlement = simulation.CreateEntity(SettlementKindId, 1, CultureId, 0, 0, 0, 0, 0, tick: 0);
        var slot = simulation.Generators.RegisterGenerator(
            100,
            SettlementKindId,
            fireWeight: 1,
            maxRuns: 0,
            targetRequiredTagBits: 1 << TerrifiedTagId);
        simulation.Generators.AddTrigger(slot, SettlementKindId);
        AddProducedEntity(simulation, slot);

        simulation.AdvanceTicks(30);
        Assert.Equal(1, simulation.World.EntityHighWatermark);

        _ = simulation.SetEntityTag(settlement, TerrifiedTagId, assigned: true, tick: 30);
        simulation.AdvanceTicks(30);
        Assert.True(simulation.World.EntityHighWatermark > 1, "Tagged target did not become eligible.");
    }

    private static void AddProducedEntity(Simulation simulation, int slot) =>
        simulation.Generators.AddProduced(
            slot,
            2,
            RaiderKindId,
            1,
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

    private static void SeedMap(Simulation simulation)
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
                simulation.CreateCell(PlaneCodes.Semantic, q, r, terrainId: 1, ownerEntityId: 0);
            }
        }
    }

    private static Simulation CreateSimulation() => KernelTestSimulation.Create(new SimulationOptions
    {
        EntityCapacity = 16,
        RelationshipCapacity = 16,
        AttributeCapacity = 32,
        MapCellCapacity = 128,
        EventCapacity = 1024,
        GeneratorCapacity = 4,
        GeneratorItemCapacity = 64,
        RelationshipKindIdCapacity = 16,
        TerrainIdCapacity = 8,
        SystemCapacity = 24,
        Seed = 1,
    });
}
