namespace Engine.Tests.Clock;

using Engine.Clock;

public class TickClockTests
{
    private readonly TickClock _clock = new();

    // --- Basic tick advancement ---

    [Fact]
    public void Tick_IncrementsCounter()
    {
        Assert.Equal(0, _clock.CurrentTick);

        _clock.Tick();
        Assert.Equal(1, _clock.CurrentTick);

        _clock.Tick();
        Assert.Equal(2, _clock.CurrentTick);
    }

    [Fact]
    public void Tick_WhenPaused_DoesNotAdvance()
    {
        _clock.Speed = SimulationSpeed.Paused;

        var result = _clock.Tick();

        Assert.False(result);
        Assert.Equal(0, _clock.CurrentTick);
    }

    [Fact]
    public void Tick_WhenNotPaused_ReturnsTrue()
    {
        var result = _clock.Tick();
        Assert.True(result);
    }

    // --- Speed control ---

    [Fact]
    public void AdvanceFrame_ProcessesTicksPerSpeed()
    {
        _clock.Speed = SimulationSpeed.Fast; // 2x

        var processed = _clock.AdvanceFrame();

        Assert.Equal(2, processed);
        Assert.Equal(2, _clock.CurrentTick);
    }

    [Fact]
    public void AdvanceFrame_VeryFast_Processes4()
    {
        _clock.Speed = SimulationSpeed.VeryFast; // 4x

        var processed = _clock.AdvanceFrame();

        Assert.Equal(4, processed);
        Assert.Equal(4, _clock.CurrentTick);
    }

    [Fact]
    public void AdvanceFrame_Paused_ProcessesZero()
    {
        _clock.Speed = SimulationSpeed.Paused;

        var processed = _clock.AdvanceFrame();

        Assert.Equal(0, processed);
        Assert.Equal(0, _clock.CurrentTick);
    }

    [Fact]
    public void SpeedChange_TakesEffectImmediately()
    {
        _clock.Speed = SimulationSpeed.VeryFast;
        _clock.AdvanceFrame(); // 4 ticks
        Assert.Equal(4, _clock.CurrentTick);

        _clock.Speed = SimulationSpeed.Normal;
        _clock.AdvanceFrame(); // 1 tick
        Assert.Equal(5, _clock.CurrentTick);
    }

    // --- Cadence ---

    [Fact]
    public void CadenceSystem_RunsEveryNTicks()
    {
        var runCount = 0;
        _clock.Register("system_a", TickPhase.LowAltitude, cadence: 5, _ => runCount++);

        for (var i = 0; i < 10; i++)
        {
            _clock.Tick();
        }

        // Ticks 5 and 10 match cadence 5 (tick % 5 == 0)
        Assert.Equal(2, runCount);
    }

    [Fact]
    public void Cadence1_RunsEveryTick()
    {
        var runCount = 0;
        _clock.Register("always", TickPhase.LowAltitude, cadence: 1, _ => runCount++);

        for (var i = 0; i < 10; i++)
        {
            _clock.Tick();
        }

        Assert.Equal(10, runCount);
    }

    [Fact]
    public void Register_RejectsCadenceZero()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            _clock.Register("bad", TickPhase.LowAltitude, cadence: 0, _ => { }));
    }

    // --- Phase ordering ---

    [Fact]
    public void PhaseOrdering_IsStrict()
    {
        var executionOrder = new List<string>();

        _clock.Register("card_exec", TickPhase.CardExecution, 1,
            _ => executionOrder.Add("CardExecution"));
        _clock.Register("low_alt", TickPhase.LowAltitude, 1,
            _ => executionOrder.Add("LowAltitude"));
        _clock.Register("pressure", TickPhase.Pressure, 1,
            _ => executionOrder.Add("Pressure"));
        _clock.Register("card_pre", TickPhase.CardPreconditions, 1,
            _ => executionOrder.Add("CardPreconditions"));
        _clock.Register("events", TickPhase.EventNotification, 1,
            _ => executionOrder.Add("EventNotification"));

        _clock.Tick();

        Assert.Equal(
        [
            "LowAltitude",
            "Pressure",
            "CardPreconditions",
            "CardExecution",
            "EventNotification",
        ], executionOrder);
    }

    [Fact]
    public void LowAltitude_WritesBeforePressureReads()
    {
        var value = 0;

        _clock.Register("writer", TickPhase.LowAltitude, 1, _ => value = 42);
        _clock.Register("reader", TickPhase.Pressure, 1, _ =>
        {
            // Pressure phase should see the value written by low-altitude
            Assert.Equal(42, value);
        });

        _clock.Tick();
    }

    [Fact]
    public void CardExecution_NotVisibleToLowAltitude_SameTick()
    {
        var cardValue = 0;
        var lowAltSaw = -1;

        // Cards write in phase 3
        _clock.Register("card", TickPhase.CardExecution, 1, _ => cardValue = 99);
        // Low-altitude reads in phase 0 -- should NOT see card's write from same tick
        _clock.Register("low_alt", TickPhase.LowAltitude, 1, _ => lowAltSaw = cardValue);

        _clock.Tick();

        // Low-altitude ran first (phase 0), saw initial value (0), not card's value (99)
        Assert.Equal(0, lowAltSaw);
        // Card did write (for next tick)
        Assert.Equal(99, cardValue);
    }

    // --- Unregister ---

    [Fact]
    public void Unregister_StopsExecution()
    {
        var runCount = 0;
        _clock.Register("removable", TickPhase.LowAltitude, 1, _ => runCount++);

        _clock.Tick();
        Assert.Equal(1, runCount);

        _clock.Unregister("removable");

        _clock.Tick();
        Assert.Equal(1, runCount); // Did not increment
    }

    // --- Introspection ---

    [Fact]
    public void GetSystemsInPhase_ReturnsRegistered()
    {
        _clock.Register("a", TickPhase.LowAltitude, 1, _ => { });
        _clock.Register("b", TickPhase.Pressure, 1, _ => { });
        _clock.Register("c", TickPhase.LowAltitude, 1, _ => { });

        var lowAlt = _clock.GetSystemsInPhase(TickPhase.LowAltitude).ToList();
        Assert.Equal(2, lowAlt.Count);
        Assert.Contains("a", lowAlt);
        Assert.Contains("c", lowAlt);
    }

    [Fact]
    public void GetSystemsRunningOnTick_RespectssCadence()
    {
        _clock.Register("every1", TickPhase.LowAltitude, 1, _ => { });
        _clock.Register("every5", TickPhase.LowAltitude, 5, _ => { });
        _clock.Register("every10", TickPhase.LowAltitude, 10, _ => { });

        var tick5 = _clock.GetSystemsRunningOnTick(5).ToHashSet();
        Assert.Contains("every1", tick5);
        Assert.Contains("every5", tick5);
        Assert.DoesNotContain("every10", tick5);

        var tick10 = _clock.GetSystemsRunningOnTick(10).ToHashSet();
        Assert.Contains("every1", tick10);
        Assert.Contains("every5", tick10);
        Assert.Contains("every10", tick10);
    }

    // --- Tick callback receives current tick ---

    [Fact]
    public void Callback_ReceivesCurrentTick()
    {
        var receivedTick = -1;
        _clock.Register("ticker", TickPhase.LowAltitude, 1, tick => receivedTick = tick);

        _clock.Tick();
        Assert.Equal(1, receivedTick);

        _clock.Tick();
        Assert.Equal(2, receivedTick);
    }
}
