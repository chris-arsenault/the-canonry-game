namespace Engine.Kernel.Tests;

using Engine.Kernel;

public sealed class PressureProgramRuntimeTests
{
    [Fact]
    public void Tick_InitializesEntityScopedPressureAtBaseline()
    {
        var simulation = CreateSimulation();
        const int pressureId = 1;
        var entity = simulation.CreateEntity(1, 1, 1, 0, 0, 0, 0, 0, 0);
        simulation.Pressures.RegisterPressure(pressureId, PressureScopeCodes.Entity, baseline: 12f, homeostasisRate: 0f, smoothingFactor: 1f);

        simulation.Pressures.Tick(simulation, tick: 1);

        Assert.Equal(12f, simulation.Pressures.GetValue(pressureId, entity));
    }

    [Fact]
    public void ApplyDelta_AffectsNextTickAndIsThenCleared()
    {
        var simulation = CreateSimulation();
        const int pressureId = 1;
        var entity = simulation.CreateEntity(1, 1, 1, 0, 0, 0, 0, 0, 0);
        simulation.Pressures.RegisterPressure(pressureId, PressureScopeCodes.Entity, baseline: 0f, homeostasisRate: 0f, smoothingFactor: 1f);

        simulation.Pressures.ApplyDelta(pressureId, entity, 30f);
        simulation.Pressures.Tick(simulation, tick: 1);
        var afterDelta = simulation.Pressures.GetValue(pressureId, entity);
        simulation.Pressures.Tick(simulation, tick: 2);

        Assert.Equal(30f, afterDelta);
        Assert.Equal(0f, simulation.Pressures.GetValue(pressureId, entity));
    }

    [Fact]
    public void Smoothing_PreventsInstantSpike()
    {
        var simulation = CreateSimulation();
        const int pressureId = 1;
        var entity = simulation.CreateEntity(1, 1, 1, 0, 0, 0, 0, 0, 0);
        simulation.Pressures.RegisterPressure(pressureId, PressureScopeCodes.Entity, baseline: 0f, homeostasisRate: 0f, smoothingFactor: 0.1f);

        simulation.Pressures.ApplyDelta(pressureId, entity, 100f);
        simulation.Pressures.Tick(simulation, tick: 1);

        Assert.InRange(simulation.Pressures.GetValue(pressureId, entity), 9.9f, 10.1f);
    }

    [Theory]
    [InlineData(500f, 100f)]
    [InlineData(-500f, -100f)]
    public void Tick_ClampsPressureToRange(float delta, float expected)
    {
        var simulation = CreateSimulation();
        const int pressureId = 1;
        var entity = simulation.CreateEntity(1, 1, 1, 0, 0, 0, 0, 0, 0);
        simulation.Pressures.RegisterPressure(pressureId, PressureScopeCodes.Entity, baseline: 0f, homeostasisRate: 0f, smoothingFactor: 1f);

        simulation.Pressures.ApplyDelta(pressureId, entity, delta);
        simulation.Pressures.Tick(simulation, tick: 1);

        Assert.Equal(expected, simulation.Pressures.GetValue(pressureId, entity));
    }

    [Fact]
    public void DisabledPressure_DoesNotRecomputeValue()
    {
        var simulation = CreateSimulation();
        const int pressureId = 1;
        var entity = simulation.CreateEntity(1, 1, 1, 0, 0, 0, 0, 0, 0);
        var slot = simulation.Pressures.RegisterPressure(pressureId, PressureScopeCodes.Entity, baseline: 0f, homeostasisRate: 0f, smoothingFactor: 1f);
        simulation.Pressures.DisablePressure(slot);

        simulation.Pressures.ApplyDelta(pressureId, entity, 40f);
        simulation.Pressures.Tick(simulation, tick: 1);

        Assert.Equal(0f, simulation.Pressures.GetValue(pressureId, entity));
    }

    private static Simulation CreateSimulation() => new(new SimulationOptions
    {
        EntityCapacity = 8,
        PressureCapacity = 4,
        PressureValueCapacity = 16,
        EventCapacity = 128,
    });
}
