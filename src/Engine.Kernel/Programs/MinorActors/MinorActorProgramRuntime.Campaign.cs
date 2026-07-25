namespace Engine.Kernel;

using Engine.Constants;

/// <summary>
/// Campaign labor: while a faction wages a campaign, its worker cohorts
/// muster at the live frontier toward the campaign target instead of home.
/// Continuous labor at the front is the world's mass projection - the thing
/// that can actually move a sharpened boundary.
/// </summary>
internal sealed partial class MinorActorProgramRuntime
{
    public void SetCampaignRelationshipKindId(int relationshipKindId) =>
        Data.CampaignRelationshipKindId = relationshipKindId;

    /// <summary>
    /// A campaign column labors at the front, not at home: the cohort's
    /// spawn anchor is the entity the source is waging a campaign against.
    /// No living campaign edge means no column.
    /// </summary>
    private bool TryResolveCampaignTarget(WorldState world, int sourceEntityId, out int targetEntityId)
    {
        targetEntityId = 0;
        if (Data.CampaignRelationshipKindId <= 0)
        {
            return false;
        }

        var cursor = world.OutgoingRelationshipsOfKind(sourceEntityId, Data.CampaignRelationshipKindId);
        while (world.MoveNextRelationship(ref cursor, out var relationshipId))
        {
            var candidate = world.RelationshipTargetId(relationshipId);
            if (candidate > 0 && world.EntityIsActive(candidate))
            {
                targetEntityId = candidate;
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// A column musters where the fight is, not at the enemy's hearth: walk
    /// the line from the campaign target back toward home and anchor at the
    /// first cell where the attacker's culture already holds ground - the
    /// live frontier. The verbs' own scoring then has a field to push from,
    /// and the front rolls forward as the field advances.
    /// </summary>
    private void AdvanceAnchorToFrontier(
        WorldState world,
        int sourceEntityId,
        int planeCode,
        ref int anchorQ,
        ref int anchorR)
    {
        if (!TryResolveSourcePosition(world, sourceEntityId, planeCode, out var homeQ, out var homeR))
        {
            return;
        }

        var cultureId = world.EntityCultureId(sourceEntityId);
        var dq = homeQ - anchorQ;
        var dr = homeR - anchorR;
        var distance = (global::System.Math.Abs(dq) + global::System.Math.Abs(dr) + global::System.Math.Abs(dq + dr)) / 2;
        for (var step = 0; step <= distance; step++)
        {
            var t = distance == 0 ? 0 : step / (FixedScalar)distance;
            var q = anchorQ + (int)FixedScalar.Round(dq * t, global::System.MidpointRounding.AwayFromZero);
            var r = anchorR + (int)FixedScalar.Round(dr * t, global::System.MidpointRounding.AwayFromZero);
            if (world.GetCellCultureInfluenceRaw(cultureId, planeCode, q, r) >= WorldState.CultureTraceRawMass)
            {
                anchorQ = q;
                anchorR = r;
                return;
            }
        }

        anchorQ = homeQ;
        anchorR = homeR;
    }

    /// <summary>
    /// A column stands on its own thin ground with the most enemy contact:
    /// own trace required, foreign mass and edge headroom preferred. The
    /// ordinary spawn scorer maximizes connected mass and would march the
    /// column into its own core, where no campaign verb has work.
    /// </summary>
    private bool TrySelectCampaignSpawnCell(
        Simulation simulation,
        int cultureId,
        int planeCode,
        int anchorQ,
        int anchorR,
        int radius,
        out int q,
        out int r)
    {
        q = anchorQ;
        r = anchorR;
        var bestScore = FixedScalar.Zero;
        for (var dq = -radius; dq <= radius; dq++)
        {
            var minDr = global::System.Math.Max(-radius, -dq - radius);
            var maxDr = global::System.Math.Min(radius, -dq + radius);
            for (var dr = minDr; dr <= maxDr; dr++)
            {
                var candidateQ = anchorQ + dq;
                var candidateR = anchorR + dr;
                if (!simulation.World.CellIsKnown(planeCode, candidateQ, candidateR))
                {
                    continue;
                }

                var boundary = simulation.CultureLocality.CellAt(
                    simulation.World,
                    cultureId,
                    planeCode,
                    candidateQ,
                    candidateR);
                if (boundary.RawMass < WorldState.CultureTraceRawMass)
                {
                    continue;
                }

                var headroom = FixedScalar.Max(
                    0,
                    (2 * WorldState.CultureEntrenchedRawMass) - boundary.RawMass);
                var score = 1 + (2 * boundary.ForeignRawMass) + (FixedScalar.FromUnits(50000L) * headroom);
                if (score > bestScore)
                {
                    q = candidateQ;
                    r = candidateR;
                    bestScore = score;
                }
            }
        }

        return bestScore > 0;
    }
}
