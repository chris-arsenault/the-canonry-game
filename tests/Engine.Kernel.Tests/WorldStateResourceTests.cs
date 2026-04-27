namespace Engine.Kernel.Tests;

using Engine.Eventing;
using Engine.Kernel;

public sealed class WorldStateResourceTests
{
    [Fact]
    public void SetResource_StoresAmbientValueAndEmitsChange()
    {
        var simulation = CreateSimulation();
        using var cursor = simulation.Events.CreateCursor(KernelEventCursorStart.Latest);

        using (simulation.EnterExecutionContext(simulation.ProcessSourceCodeId, programId: 0))
        {
            simulation.SetResource(resourceId: 1, q: 2, r: -1, value: 5f, tick: 0);
        }

        Assert.Equal(5f, simulation.World.GetResource(1, 2, -1));
        Assert.True(simulation.Events.TryRead(cursor, out var read));
        var changed = read.Event.AsResourceChanged();
        Assert.Equal(1, changed.ResourceId);
        Assert.Equal(2, changed.Q);
        Assert.Equal(-1, changed.R);
        Assert.Equal(0f, changed.OldValue);
        Assert.Equal(5f, changed.NewValue);
    }

    [Fact]
    public void SetResource_ClampsNegativeValues()
    {
        var simulation = CreateSimulation();

        using (simulation.EnterExecutionContext(simulation.ProcessSourceCodeId, programId: 0))
        {
            simulation.SetResource(resourceId: 1, q: 0, r: 0, value: 4f, tick: 0);
            simulation.SetResource(resourceId: 1, q: 0, r: 0, value: -10f, tick: 1);
        }

        Assert.Equal(0f, simulation.World.GetResource(1, 0, 0));
    }

    [Fact]
    public void ModifyResource_AccumulatesAndNeverDropsBelowZero()
    {
        var simulation = CreateSimulation();

        using (simulation.EnterExecutionContext(simulation.ProcessSourceCodeId, programId: 0))
        {
            simulation.ModifyResource(1, 0, 0, 8f, tick: 0);
            simulation.ModifyResource(1, 0, 0, 3f, tick: 1);
            simulation.ModifyResource(1, 0, 0, -20f, tick: 2);
        }

        Assert.Equal(0f, simulation.World.GetResource(1, 0, 0));
    }

    private static Simulation CreateSimulation() => new(new SimulationOptions
    {
        ResourceCapacity = 16,
        EventCapacity = 64,
    });
}
