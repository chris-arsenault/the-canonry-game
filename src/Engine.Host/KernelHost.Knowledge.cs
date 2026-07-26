namespace Engine.Host;

using Engine.Gameplay;
using Engine.Observability;

public sealed record KnowledgeOverviewSnapshot(
    long Revision,
    IReadOnlyList<KnowledgeCultureSnapshot> Cultures);

public sealed record KnowledgeCultureSnapshot(
    int CultureId,
    int ClaimedTechs,
    int MaxTier,
    IReadOnlyList<KnowledgeClaimSnapshot> Claims,
    IReadOnlyList<KnowledgeModifierSnapshot> Modifiers);

public sealed record KnowledgeClaimSnapshot(
    string TechId,
    int Tier);

public sealed record KnowledgeModifierSnapshot(
    KnowledgeGrantDomain Domain,
    int TargetId,
    string? TargetStableName,
    int AuthoredTargetCode,
    float Multiplier);

public sealed partial class KernelHost
{
    public long KnowledgeRevision
    {
        get
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            return _kernel.Knowledge.Revision;
        }
    }

    /// <summary>
    /// Builds a presentation-safe snapshot of current knowledge claims and the
    /// effective modifier totals produced by them. Modifier values are read
    /// from the kernel's derived table rather than recomputed in presentation.
    /// </summary>
    public KnowledgeOverviewSnapshot ComputeKnowledgeOverview()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var world = _kernel.World;
        var knowledge = _kernel.Knowledge;
        var cultures = new KnowledgeCultureSnapshot[world.CultureHighWatermark];
        for (var cultureId = 1; cultureId <= world.CultureHighWatermark; cultureId++)
        {
            cultures[cultureId - 1] = BuildKnowledgeCultureSnapshot(knowledge, cultureId);
        }

        return new KnowledgeOverviewSnapshot(knowledge.Revision, cultures);
    }

    private KnowledgeCultureSnapshot BuildKnowledgeCultureSnapshot(
        Engine.Kernel.KnowledgeProgramRuntime knowledge,
        int cultureId)
    {
        var claims = new List<KnowledgeClaimSnapshot>(knowledge.ClaimedTechs(cultureId));
        var modifierTargets = new Dictionary<KnowledgeModifierKey, KnowledgeGrantConfig>();
        for (var siteIndex = 0; siteIndex < _knowledgeSites.Count; siteIndex++)
        {
            if (!knowledge.CultureClaimsSite(cultureId, siteIndex))
            {
                continue;
            }

            var site = _knowledgeSites[siteIndex];
            claims.Add(new KnowledgeClaimSnapshot(site.TechId, site.Tier));
            foreach (var grant in site.Grants)
            {
                var targetId = DomainStartupSeedInstaller.ResolveKnowledgeGrantTarget(DomainIds, grant);
                modifierTargets.TryAdd(new KnowledgeModifierKey(grant.Domain, targetId), grant);
            }
        }

        var modifiers = new KnowledgeModifierSnapshot[modifierTargets.Count];
        var modifierIndex = 0;
        foreach (var entry in modifierTargets.OrderBy(static entry => entry.Key.Domain).ThenBy(static entry => entry.Key.TargetId))
        {
            var grant = entry.Value;
            modifiers[modifierIndex++] = new KnowledgeModifierSnapshot(
                entry.Key.Domain,
                entry.Key.TargetId,
                grant.Target,
                grant.TargetCode,
                knowledge.Modifier(
                    cultureId,
                    DomainStartupSeedInstaller.KnowledgeGrantDomainCode(entry.Key.Domain),
                    entry.Key.TargetId).ToSingle());
        }

        return new KnowledgeCultureSnapshot(
            cultureId,
            knowledge.ClaimedTechs(cultureId),
            knowledge.MaxTier(cultureId),
            claims,
            modifiers);
    }

    private readonly record struct KnowledgeModifierKey(KnowledgeGrantDomain Domain, int TargetId);

    /// <summary>
    /// Records, per culture, how many authored technologies it has claimed and
    /// the highest tier it has reached. The kernel's knowledge program is the
    /// single source of truth: it computes claims on each scheduled run from the semantic
    /// culture field against the installed knowledge-site hexes and this reads
    /// those tallies.
    /// </summary>
    public void RecordKnowledgeTelemetry(TelemetryBatch batch, int runId, int tick)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(batch);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(runId);
        if (_knowledgeSites.Count == 0)
        {
            return;
        }

        if (!_knowledgeSiteSymbolsRecorded)
        {
            for (var siteIndex = 0; siteIndex < _knowledgeSites.Count; siteIndex++)
            {
                var site = _knowledgeSites[siteIndex];
                batch.KnowledgeSiteSymbols.Add(
                    new KnowledgeSiteSymbolRow(runId, siteIndex, site.TechId, site.Tier));
            }

            _knowledgeSiteSymbolsRecorded = true;
        }

        var world = _kernel.World;
        var knowledge = _kernel.Knowledge;
        for (var cultureId = 1; cultureId <= world.CultureHighWatermark; cultureId++)
        {
            var claimed = knowledge.ClaimedTechs(cultureId);
            if (claimed > 0)
            {
                batch.KnowledgeSnapshots.Add(
                    new KnowledgeSnapshotRow(runId, tick, cultureId, claimed, knowledge.MaxTier(cultureId)));
                for (var siteIndex = 0; siteIndex < _knowledgeSites.Count; siteIndex++)
                {
                    if (!knowledge.CultureClaimsSite(cultureId, siteIndex))
                    {
                        continue;
                    }

                    var site = _knowledgeSites[siteIndex];
                    batch.KnowledgeClaimSnapshots.Add(
                        new KnowledgeClaimSnapshotRow(
                            runId,
                            tick,
                            cultureId,
                            siteIndex,
                            site.Tier));
                }
            }
        }
    }
}
