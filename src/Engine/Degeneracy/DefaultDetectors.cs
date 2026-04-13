namespace Engine.Degeneracy;

/// <summary>
/// Settlement depopulation: too many settlements are declining.
/// </summary>
public sealed class SettlementDepopulationDetector : IDegeneracyDetector
{
    public string Name => "settlement_depopulation";
    public string WarningMessage => "Many settlements are in decline.";

    public float WarningThreshold { get; set; } = 0.5f;
    public float CriticalThreshold { get; set; } = 0.8f;

    public Severity Evaluate(IDegeneracyContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (context.ActiveSettlementCount == 0)
        {
            return Severity.Critical;
        }

        var ratio = (float)context.DecliningSettlementCount / context.ActiveSettlementCount;
        if (ratio >= CriticalThreshold) return Severity.Critical;
        if (ratio >= WarningThreshold) return Severity.Warning;
        return Severity.Normal;
    }
}

/// <summary>
/// Political monoculture: one nation controls too much of the world.
/// </summary>
public sealed class PoliticalMonocultureDetector : IDegeneracyDetector
{
    public string Name => "political_monoculture";
    public string WarningMessage => "One nation dominates the world.";

    public float WarningThreshold { get; set; } = 0.6f;
    public float CriticalThreshold { get; set; } = 0.8f;

    public Severity Evaluate(IDegeneracyContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (context.TotalSettlementCount == 0)
        {
            return Severity.Normal;
        }

        var ratio = (float)context.MaxNationSettlementCount / context.TotalSettlementCount;
        if (ratio >= CriticalThreshold) return Severity.Critical;
        if (ratio >= WarningThreshold) return Severity.Warning;
        return Severity.Normal;
    }
}

/// <summary>
/// NPC extinction: too few NPCs remain.
/// </summary>
public sealed class NpcExtinctionDetector : IDegeneracyDetector
{
    public string Name => "npc_extinction";
    public string WarningMessage => "NPC population critically low.";

    public int WarningThreshold { get; set; } = 5;
    public int CriticalThreshold { get; set; } = 2;

    public Severity Evaluate(IDegeneracyContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (context.ActiveNpcCount <= CriticalThreshold) return Severity.Critical;
        if (context.ActiveNpcCount <= WarningThreshold) return Severity.Warning;
        return Severity.Normal;
    }
}
