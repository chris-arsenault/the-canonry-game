namespace Engine.Host;

using Engine.Constants;
using Engine.Domain.Startup;
using Engine.Kernel;
using Engine.Observability;

public sealed partial class KernelHost
{
    // A culture claims a tech when its semantic idea-mass holds the tech's hex or
    // a cell within this radius of it at or above the claimed band. The radius
    // keeps single-hex control from being razor-thin: holding the ground around
    // the tech counts, not only the exact cell.
    private const float KnowledgeClaimBand = 30f; // WorldState.CultureClaimedRawMass
    private const int KnowledgeClaimRadius = 2;

    /// <summary>
    /// Records, per culture, how many authored technologies it has claimed and
    /// the highest tier it has reached. Computed from the semantic culture field
    /// against the seeded knowledge-site hexes.
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

        var world = _kernel.World;
        for (var cultureId = 1; cultureId <= world.CultureHighWatermark; cultureId++)
        {
            var claimed = 0;
            var maxTier = -1;
            foreach (var site in _knowledgeSites)
            {
                if (!CultureHoldsSite(world, cultureId, site))
                {
                    continue;
                }

                claimed++;
                if (site.Tier > maxTier)
                {
                    maxTier = site.Tier;
                }
            }

            if (claimed > 0)
            {
                batch.KnowledgeSnapshots.Add(new KnowledgeSnapshotRow(runId, tick, cultureId, claimed, maxTier));
            }
        }
    }

    private static bool CultureHoldsSite(WorldState world, int cultureId, DomainKnowledgeSite site)
    {
        for (var dq = -KnowledgeClaimRadius; dq <= KnowledgeClaimRadius; dq++)
        {
            var minR = Math.Max(-KnowledgeClaimRadius, -dq - KnowledgeClaimRadius);
            var maxR = Math.Min(KnowledgeClaimRadius, -dq + KnowledgeClaimRadius);
            for (var dr = minR; dr <= maxR; dr++)
            {
                var q = site.Q + dq;
                var r = site.R + dr;
                if (world.CellIsKnown(PlaneCodes.Semantic, q, r) &&
                    world.GetCellCultureInfluenceRaw(cultureId, PlaneCodes.Semantic, q, r) >= KnowledgeClaimBand)
                {
                    return true;
                }
            }
        }

        return false;
    }
}
