namespace Engine.Tests.Degeneracy;

using Engine.Degeneracy;
using Engine.Events;

public class DegeneracyDetectorTests
{
    // --- Settlement Depopulation ---

    [Fact]
    public void Depopulation_NormalWhenFewDeclining()
    {
        var ctx = new StubContext { ActiveSettlementCount = 10, DecliningSettlementCount = 2 };
        Assert.Equal(Severity.Normal, new SettlementDepopulationDetector().Evaluate(ctx));
    }

    [Fact]
    public void Depopulation_WarningWhenHalfDeclining()
    {
        var ctx = new StubContext { ActiveSettlementCount = 10, DecliningSettlementCount = 6 };
        Assert.Equal(Severity.Warning, new SettlementDepopulationDetector().Evaluate(ctx));
    }

    [Fact]
    public void Depopulation_CriticalWhenMostDeclining()
    {
        var ctx = new StubContext { ActiveSettlementCount = 10, DecliningSettlementCount = 9 };
        Assert.Equal(Severity.Critical, new SettlementDepopulationDetector().Evaluate(ctx));
    }

    [Fact]
    public void Depopulation_CriticalWhenNoSettlements()
    {
        var ctx = new StubContext { ActiveSettlementCount = 0, DecliningSettlementCount = 0 };
        Assert.Equal(Severity.Critical, new SettlementDepopulationDetector().Evaluate(ctx));
    }

    // --- Political Monoculture ---

    [Fact]
    public void Monoculture_NormalWithBalancedNations()
    {
        var ctx = new StubContext { TotalSettlementCount = 10, MaxNationSettlementCount = 4 };
        Assert.Equal(Severity.Normal, new PoliticalMonocultureDetector().Evaluate(ctx));
    }

    [Fact]
    public void Monoculture_WarningWhenDominant()
    {
        var ctx = new StubContext { TotalSettlementCount = 10, MaxNationSettlementCount = 7 };
        Assert.Equal(Severity.Warning, new PoliticalMonocultureDetector().Evaluate(ctx));
    }

    [Fact]
    public void Monoculture_CriticalWhenOverwhelming()
    {
        var ctx = new StubContext { TotalSettlementCount = 10, MaxNationSettlementCount = 9 };
        Assert.Equal(Severity.Critical, new PoliticalMonocultureDetector().Evaluate(ctx));
    }

    [Fact]
    public void Monoculture_NormalWhenNoSettlements()
    {
        var ctx = new StubContext { TotalSettlementCount = 0, MaxNationSettlementCount = 0 };
        Assert.Equal(Severity.Normal, new PoliticalMonocultureDetector().Evaluate(ctx));
    }

    // --- NPC Extinction ---

    [Fact]
    public void NpcExtinction_NormalWithManyNpcs()
    {
        var ctx = new StubContext { ActiveNpcCount = 20 };
        Assert.Equal(Severity.Normal, new NpcExtinctionDetector().Evaluate(ctx));
    }

    [Fact]
    public void NpcExtinction_WarningWithFewNpcs()
    {
        var ctx = new StubContext { ActiveNpcCount = 4 };
        Assert.Equal(Severity.Warning, new NpcExtinctionDetector().Evaluate(ctx));
    }

    [Fact]
    public void NpcExtinction_CriticalWithAlmostNone()
    {
        var ctx = new StubContext { ActiveNpcCount = 1 };
        Assert.Equal(Severity.Critical, new NpcExtinctionDetector().Evaluate(ctx));
    }

    // --- Registry ---

    [Fact]
    public void Registry_EvaluatesAllDetectors()
    {
        var registry = DetectorRegistry.CreateDefault();
        var ctx = new StubContext
        {
            ActiveSettlementCount = 10,
            DecliningSettlementCount = 1,
            TotalSettlementCount = 10,
            MaxNationSettlementCount = 3,
            ActiveNpcCount = 15,
        };

        var results = registry.Evaluate(ctx, currentTick: 1);

        Assert.Equal(3, results.Count);
        Assert.All(results.Values, s => Assert.Equal(Severity.Normal, s));
    }

    [Fact]
    public void Registry_EmitsWarningOnTransition()
    {
        var eventLog = new EventLog();
        var registry = DetectorRegistry.CreateDefault(eventLog);

        // First: normal
        registry.Evaluate(new StubContext { ActiveSettlementCount = 10, DecliningSettlementCount = 1, TotalSettlementCount = 10, MaxNationSettlementCount = 3, ActiveNpcCount = 15 }, 1);

        // Second: NPC extinction warning
        registry.Evaluate(new StubContext { ActiveSettlementCount = 10, DecliningSettlementCount = 1, TotalSettlementCount = 10, MaxNationSettlementCount = 3, ActiveNpcCount = 3 }, 2);

        var warnings = eventLog.ByType(EventType.Threshold).ToList();
        Assert.Single(warnings);
        Assert.Contains("WARNING", warnings[0].Description, StringComparison.Ordinal);
    }

    [Fact]
    public void Registry_EmitsResolvedOnRecovery()
    {
        var eventLog = new EventLog();
        var registry = DetectorRegistry.CreateDefault(eventLog);

        // Trigger warning
        registry.Evaluate(new StubContext { ActiveSettlementCount = 10, DecliningSettlementCount = 1, TotalSettlementCount = 10, MaxNationSettlementCount = 3, ActiveNpcCount = 3 }, 1);

        // Recover
        registry.Evaluate(new StubContext { ActiveSettlementCount = 10, DecliningSettlementCount = 1, TotalSettlementCount = 10, MaxNationSettlementCount = 3, ActiveNpcCount = 20 }, 2);

        var events = eventLog.ByType(EventType.Threshold).ToList();
        Assert.Equal(2, events.Count);
        Assert.Contains("RESOLVED", events[0].Description, StringComparison.Ordinal);
    }

    [Fact]
    public void Registry_GetSeverity()
    {
        var registry = DetectorRegistry.CreateDefault();
        registry.Evaluate(new StubContext { ActiveSettlementCount = 10, DecliningSettlementCount = 1, TotalSettlementCount = 10, MaxNationSettlementCount = 3, ActiveNpcCount = 1 }, 1);

        Assert.Equal(Severity.Critical, registry.GetSeverity("npc_extinction"));
        Assert.Equal(Severity.Normal, registry.GetSeverity("settlement_depopulation"));
    }

    // --- Stub ---

    private sealed class StubContext : IDegeneracyContext
    {
        public int ActiveSettlementCount { get; set; } = 10;
        public int DecliningSettlementCount { get; set; }
        public int ActiveNationCount { get; set; } = 2;
        public int MaxNationSettlementCount { get; set; } = 5;
        public int TotalSettlementCount { get; set; } = 10;
        public int ActiveNpcCount { get; set; } = 10;
    }
}
