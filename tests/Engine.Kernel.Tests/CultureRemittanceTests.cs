namespace Engine.Kernel.Tests;

using Engine.Constants;
using Engine.Kernel;

public sealed class CultureRemittanceTests
{
    private const int CultureId = 1;
    private const int KindId = 1;
    private const int AnchorRelationshipKindId = 9;

    [Fact]
    public void InfluenceProductionFarFromAnchorSplitsRemittanceToAnchorCell()
    {
        var simulation = CreateSimulation(remittanceShare: FixedScalar.FromSingle(0.5f), remittanceMinimumDistance: 6);
        var entity = simulation.CreateEntity(KindId, 1, CultureId, 0, 0, 0, 0, 0, tick: 0);

        var applied = simulation.ModifyCellCultureInfluenceRawFromEntity(
            entity,
            PlaneCodes.Physical,
            10,
            0,
            FixedScalar.FromSingle(10f),
            tick: 0);

        Assert.Equal(FixedScalar.FromSingle(10f), applied);
        Assert.Equal(FixedScalar.FromSingle(5f), simulation.World.GetCellCultureInfluenceRaw(CultureId, PlaneCodes.Physical, 10, 0));
        Assert.Equal(FixedScalar.FromSingle(5f), simulation.World.GetCellCultureInfluenceRaw(CultureId, PlaneCodes.Physical, 0, 0));
    }

    [Fact]
    public void InfluenceProductionNearAnchorIsNotSplit()
    {
        var simulation = CreateSimulation(remittanceShare: FixedScalar.FromSingle(0.5f), remittanceMinimumDistance: 6);
        var entity = simulation.CreateEntity(KindId, 1, CultureId, 0, 0, 0, 0, 0, tick: 0);

        _ = simulation.ModifyCellCultureInfluenceRawFromEntity(
            entity,
            PlaneCodes.Physical,
            4,
            0,
            FixedScalar.FromSingle(10f),
            tick: 0);

        Assert.Equal(FixedScalar.FromSingle(10f), simulation.World.GetCellCultureInfluenceRaw(CultureId, PlaneCodes.Physical, 4, 0));
        Assert.Equal(FixedScalar.FromSingle(0f), simulation.World.GetCellCultureInfluenceRaw(CultureId, PlaneCodes.Physical, 0, 0));
    }

    [Fact]
    public void ZeroRemittanceShareDisablesTheSplit()
    {
        var simulation = CreateSimulation(remittanceShare: FixedScalar.FromSingle(0f), remittanceMinimumDistance: 6);
        var entity = simulation.CreateEntity(KindId, 1, CultureId, 0, 0, 0, 0, 0, tick: 0);

        _ = simulation.ModifyCellCultureInfluenceRawFromEntity(
            entity,
            PlaneCodes.Physical,
            10,
            0,
            FixedScalar.FromSingle(10f),
            tick: 0);

        Assert.Equal(FixedScalar.FromSingle(10f), simulation.World.GetCellCultureInfluenceRaw(CultureId, PlaneCodes.Physical, 10, 0));
        Assert.Equal(FixedScalar.FromSingle(0f), simulation.World.GetCellCultureInfluenceRaw(CultureId, PlaneCodes.Physical, 0, 0));
    }

    [Fact]
    public void CultureAnchorRelationshipMovesTheAnchorToTheTarget()
    {
        var simulation = CreateSimulation(remittanceShare: FixedScalar.FromSingle(0.5f), remittanceMinimumDistance: 6);
        _ = simulation.Relationships.RegisterRelationshipKind(
            AnchorRelationshipKindId,
            cullThreshold: FixedScalar.FromSingle(0.05f),
            isCultureAnchor: true);
        var entity = simulation.CreateEntity(KindId, 1, CultureId, 0, 0, 0, 0, 0, tick: 0);
        var home = simulation.CreateEntity(KindId, 1, CultureId, 8, 0, 0, 0, 0, tick: 0);

        _ = simulation.CreateRelationship(
            AnchorRelationshipKindId,
            RelationshipPolarityCodes.Neutral,
            entity,
            home,
            strength: FixedScalar.FromSingle(1f),
            tick: 0);

        Assert.Equal(8, simulation.World.EntityCultureAnchorQ(entity, PlaneCodes.Physical));

        _ = simulation.ModifyCellCultureInfluenceRawFromEntity(
            entity,
            PlaneCodes.Physical,
            20,
            0,
            FixedScalar.FromSingle(10f),
            tick: 0);

        Assert.Equal(FixedScalar.FromSingle(5f), simulation.World.GetCellCultureInfluenceRaw(CultureId, PlaneCodes.Physical, 20, 0));
        Assert.Equal(FixedScalar.FromSingle(5f), simulation.World.GetCellCultureInfluenceRaw(CultureId, PlaneCodes.Physical, 8, 0));
    }

    private static Simulation CreateSimulation(
        FixedScalar remittanceShare,
        int remittanceMinimumDistance) =>
        new(new SimulationOptions
        {
            EntityCapacity = 16,
            MapCellCapacity = 256,
            MapInfluenceCapacity = 256,
            EventCapacity = 2048,
            SystemCapacity = 32,
            KernelTuning = new KernelTuningOptions
            {
                Programs = new KernelProgramTuningOptions
                {
                    CultureField = new CultureFieldTuningOptions
                    {
                        RemittanceShare = remittanceShare,
                        RemittanceMinimumDistance = remittanceMinimumDistance,
                    },
                },
            },
        });
}
