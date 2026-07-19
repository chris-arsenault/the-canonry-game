namespace Engine.WorldGen.Tests.Map;

public sealed class PhysicalEvolutionSessionTests
{
    [Fact]
    public void ConfiguredEpochs_AccumulateDeterministicPhysicalHistory()
    {
        var epochs = Epochs();
        var first = Generate(epochs);
        var second = Generate(epochs);

        Assert.Equal(epochs.Length, first.CompletedEpochCount);
        Assert.Equal(first.Elevation.ToArray(), second.Elevation.ToArray());
        Assert.Equal(first.GeothermalHistory.ToArray(), second.GeothermalHistory.ToArray());
        Assert.Contains(first.GeothermalHistory.ToArray(), value => value > 0);
        Assert.Contains(first.TectonicActivityHistory.ToArray(), value => value > 0);
    }

    [Fact]
    public void EpochStrength_ChangesIncrementalUpliftAndRejectsInvalidConfiguration()
    {
        var quiet = PhysicalGeographyEvolutionSession.Begin(
            6,
            PhysicalGeographyLayerTestHarness.Seed,
            PhysicalGeographyLayerTestHarness.Profile);
        var active = PhysicalGeographyEvolutionSession.Begin(
            6,
            PhysicalGeographyLayerTestHarness.Seed,
            PhysicalGeographyLayerTestHarness.Profile);
        quiet.BeginEpoch(Epoch("quiet", tectonic: 0), 1);
        active.BeginEpoch(Epoch("active", tectonic: 1000), 1);
        quiet.ApplyGeology();
        active.ApplyGeology();

        Assert.False(quiet.Map.BedrockElevation.SequenceEqual(active.Map.BedrockElevation));
        Assert.Throws<InvalidOperationException>(() => Epoch("invalid", tectonic: 1001).Validate());
    }

    [Fact]
    public void TectonicEpoch_ChangesActivityWithoutMovingProvinceIdentity()
    {
        var session = PhysicalGeographyEvolutionSession.Begin(
            6,
            PhysicalGeographyLayerTestHarness.Seed,
            PhysicalGeographyLayerTestHarness.Profile);
        var provinces = session.Map.CrustProvinceIds.ToArray();
        var initialBoundary = session.Map.TectonicBoundaryCodes.ToArray();
        var epoch = Epoch("active-boundaries", tectonic: 800);
        var layer = new TectonicEpochLayer();
        var context = new PhysicalGeographyGenerationContext(
            PhysicalGeographyLayerTestHarness.Seed,
            PhysicalGeographyLayerTestHarness.Profile,
            layer.Contract,
            epoch,
            0,
            1);

        layer.Apply(session.Map, context);
        layer.Validate(session.Map, context);

        Assert.Equal(provinces, session.Map.CrustProvinceIds.ToArray());
        Assert.False(initialBoundary.SequenceEqual(session.Map.TectonicBoundaryCodes.ToArray()));

        var interior = Enumerable.Range(0, session.Map.Grid.TriangleCount)
            .First(index => session.Map.InitialTectonicBoundaryCodes[index] == 0);
        session.Map.TectonicBoundaryCodes[interior] = 1;
        Assert.Throws<InvalidOperationException>(() => layer.Validate(session.Map, context));
    }

    [Fact]
    public void ExternalCoastalForcing_ErodesOnlyAValidDryCoast()
    {
        var session = ReadyForDomainForcing();
        var coast = FindDryCoast(session.Map, out _);
        var before = session.Map.CumulativeCoastalErosion[coast];
        session.Map.ExternalCoastalForcing[coast] = 1000;
        session.MarkDomainForcingComplete();
        session.ApplyCoastalEvolution();

        Assert.True(session.Map.CumulativeCoastalErosion[coast] > before);
        Assert.Equal(1000, session.Map.ExternalCoastalForcingHistory[coast]);

        var invalid = ReadyForDomainForcing();
        _ = FindDryCoast(invalid.Map, out var ocean);
        invalid.Map.ExternalCoastalForcing[ocean] = 1000;
        invalid.MarkDomainForcingComplete();
        Assert.Throws<InvalidOperationException>(invalid.ApplyCoastalEvolution);
    }

    private static PhysicalGeographyField Generate(PhysicalGeographyEpochDefinition[] epochs)
    {
        var session = PhysicalGeographyEvolutionSession.Begin(
            6,
            PhysicalGeographyLayerTestHarness.Seed,
            PhysicalGeographyLayerTestHarness.Profile);
        foreach (var epoch in epochs)
        {
            ApplyEpoch(session, epoch, epochs.Length);
        }

        return session.Complete();
    }

    private static PhysicalGeographyEvolutionSession ReadyForDomainForcing()
    {
        var session = PhysicalGeographyEvolutionSession.Begin(
            6,
            PhysicalGeographyLayerTestHarness.Seed,
            PhysicalGeographyLayerTestHarness.Profile);
        var epoch = Epoch("coasts", tectonic: 500);
        session.BeginEpoch(epoch, 1);
        session.ApplyGeology();
        session.ApplyClimate();
        session.ApplyHydrology();
        session.ApplyGlaciation();
        session.ApplyPostGlacialReconciliation();
        return session;
    }

    private static void ApplyEpoch(
        PhysicalGeographyEvolutionSession session,
        PhysicalGeographyEpochDefinition epoch,
        int count)
    {
        session.BeginEpoch(epoch, count);
        session.ApplyGeology();
        session.ApplyClimate();
        session.ApplyHydrology();
        session.ApplyGlaciation();
        session.ApplyPostGlacialReconciliation();
        session.MarkDomainForcingComplete();
        session.ApplyCoastalEvolution();
        session.CompleteEpoch();
    }

    private static PhysicalGeographyEpochDefinition[] Epochs() =>
    [
        Epoch("wet-uplift", tectonic: 900) with
        {
            HydrologyStrength = 1000,
            GlaciationStrength = 250,
            TemperatureOffset = 60,
        },
        Epoch("glacial-retreat", tectonic: 250) with
        {
            HydrologyStrength = 700,
            GlaciationStrength = 900,
            TemperatureOffset = -80,
        },
    ];

    private static PhysicalGeographyEpochDefinition Epoch(string name, int tectonic) => new()
    {
        StableName = name,
        TectonicStrength = tectonic,
        HydrologyStrength = 600,
        GlaciationStrength = 500,
        CoastalStrength = 700,
    };

    private static int FindDryCoast(PhysicalGeographyField map, out int ocean)
    {
        for (var triangleIndex = 0; triangleIndex < map.Grid.TriangleCount; triangleIndex++)
        {
            if (map.WaterKindCodes[triangleIndex] != Engine.Constants.PhysicalWaterKindCodes.None)
            {
                continue;
            }

            for (var edge = 0; edge < 3; edge++)
            {
                var neighbor = map.Grid.NeighborTriangleIndex(triangleIndex, edge);
                if (neighbor >= 0 &&
                    map.WaterKindCodes[neighbor] == Engine.Constants.PhysicalWaterKindCodes.Ocean)
                {
                    ocean = neighbor;
                    return triangleIndex;
                }
            }
        }

        throw new InvalidOperationException("Test map contains no dry coast.");
    }
}
