namespace Engine.Kernel;

using Engine.Constants;

internal sealed partial class MinorActorProgramRuntime
{
    private FixedScalar MinorVerbScore(
        Simulation simulation,
        int actorId,
        int verb,
        int planeCode,
        int q,
        int r,
        FixedScalar ownShare,
        FixedScalar ownRaw,
        FixedScalar foreignShare,
        FixedScalar foreignRaw,
        in MinorActorCultureField neighborhood,
        in MinorActorCultureAttachment attachment,
        int tick,
        ref MinorActorLifecycleNeed lifecycleNeed)
    {
        if (Data.VerbActionCodes[verb] == MinorActorActionCodes.RoadMaintenance)
        {
            return RoadMaintenanceScore(simulation, planeCode, q, r, neighborhood, tick);
        }

        if (State.PurposeCode(actorId) == MinorActorPurposeCodes.CampaignLabor)
        {
            return CampaignVerbScore(
                Data.VerbTargetingCodes[verb], ownRaw, foreignRaw, neighborhood);
        }

        if (State.PurposeCode(actorId) == MinorActorPurposeCodes.RoadLabor)
        {
            return RouteCultureScore(simulation, planeCode, q, r, ownRaw, neighborhood, attachment, tick);
        }

        if (Data.VerbActionCodes[verb] == MinorActorActionCodes.LifecycleMaintenance)
        {
            return LifecycleMaintenanceScore(
                simulation,
                State.SourceEntityId(actorId),
                ownShare,
                ownRaw,
                neighborhood,
                attachment,
                tick,
                ref lifecycleNeed);
        }

        return Data.VerbActionCodes[verb] == MinorActorActionCodes.CultureCleanup
            ? ReduceOtherCultureScore(ownShare, ownRaw, foreignShare, foreignRaw, neighborhood, attachment)
            : CultureTargetScore(
                Data.VerbTargetingCodes[verb],
                ownShare,
                ownRaw,
                foreignShare,
                foreignRaw,
                neighborhood,
                attachment);
    }

    /// <summary>
    /// A campaign column brings its own support: it presses from thin own
    /// ground with local presence, not from a deep home field.
    /// Press rewards a target-facing cell where own mass is adjacent and
    /// there is foreign ground or headroom to take; break rewards enemy mass
    /// the column can reach. Both stay finite on the thin frontier where the
    /// interior scorers collapse to zero.
    /// </summary>
    private static FixedScalar CampaignVerbScore(
        int targetingCode,
        FixedScalar ownRaw,
        FixedScalar foreignRaw,
        in MinorActorCultureField neighborhood)
    {
        var localPresence = Clamp01(
            FixedScalar.FromRatio(neighborhood.SupportedAdjacentCount, 3));
        if (localPresence <= 0)
        {
            return 0;
        }

        if (targetingCode == MinorActorVerbTargetingCodes.ReduceOtherCulture)
        {
            var enemy = Saturating(foreignRaw, CultureSupportedRawInfluence);
            return enemy <= 0 ? 0 : (FixedScalar.FromUnits(500000L) + localPresence) * enemy;
        }

        var headroom = Clamp01((CultureStrongRawInfluence - ownRaw) / CultureStrongRawInfluence);
        var contest = Saturating(foreignRaw, CultureSupportedRawInfluence);
        return localPresence * (FixedScalar.FromUnits(350000L) + headroom) * (FixedScalar.FromUnits(500000L) + contest);
    }

    private static FixedScalar CultureTargetScore(
        int targetingCode,
        FixedScalar ownShare,
        FixedScalar ownRaw,
        FixedScalar foreignShare,
        FixedScalar foreignRaw,
        in MinorActorCultureField neighborhood,
        in MinorActorCultureAttachment attachment)
    {
        return targetingCode switch
        {
            MinorActorVerbTargetingCodes.ReinforceHomeMass => ReinforceCoreScore(ownShare, ownRaw, foreignShare, neighborhood, attachment),
            MinorActorVerbTargetingCodes.ExtendConnectedBorder => ExtendBorderScore(ownShare, ownRaw, foreignShare, attachment),
            MinorActorVerbTargetingCodes.BridgeGap => BridgeGapScore(ownShare, ownRaw, foreignShare, attachment),
            MinorActorVerbTargetingCodes.ReduceOtherCulture => ReduceOtherCultureScore(
                ownShare,
                ownRaw,
                foreignShare,
                foreignRaw,
                neighborhood,
                attachment),
            _ => 0,
        };
    }

    private static FixedScalar LifecycleMaintenanceScore(
        Simulation simulation,
        int sourceEntityId,
        FixedScalar ownShare,
        FixedScalar ownRaw,
        in MinorActorCultureField neighborhood,
        in MinorActorCultureAttachment attachment,
        int tick,
        ref MinorActorLifecycleNeed lifecycleNeed)
    {
        ResolveLifecycleMaintenanceNeed(
            simulation,
            sourceEntityId,
            tick,
            ref lifecycleNeed);
        if (lifecycleNeed.MaintenanceNeed <= 0)
        {
            return 0;
        }

        var connectedSupport = FixedScalar.Max(
            Saturating(attachment.ConnectedRawMass, 4),
            Clamp01(neighborhood.ConnectedRaw / CultureFieldRawInfluence));
        if (connectedSupport <= 0)
        {
            return 0;
        }

        var ownPresence = FixedScalar.Max(
            Clamp01(ownShare / 100),
            Saturating(ownRaw, CultureSupportedRawInfluence));
        return lifecycleNeed.MaintenanceNeed *
            (FixedScalar.FromUnits(500000L) + lifecycleNeed.AgeLoad) *
            connectedSupport *
            (FixedScalar.FromUnits(500000L) + ownPresence) *
            LifecycleMaintenanceScoreScale;
    }

    private static void ResolveLifecycleMaintenanceNeed(
        Simulation simulation,
        int sourceEntityId,
        int tick,
        ref MinorActorLifecycleNeed lifecycleNeed)
    {
        if (lifecycleNeed.Resolved)
        {
            return;
        }

        lifecycleNeed.Resolved = true;
        if (!simulation.Effects.TryGetLifecycleMaintenanceNeed(
                simulation,
                sourceEntityId,
                tick,
                out _,
                out var ageDrainComponent,
                out var healthShortfall))
        {
            return;
        }

        lifecycleNeed.AgeLoad = Saturating(
            ageDrainComponent,
            FixedScalar.FromUnits(200000L));
        lifecycleNeed.MaintenanceNeed = Clamp01(
            healthShortfall +
            (LifecycleMaintenanceEarlyNeedScale * lifecycleNeed.AgeLoad));
    }

    private static FixedScalar ReinforceCoreScore(
        FixedScalar ownShare,
        FixedScalar ownRaw,
        FixedScalar foreignShare,
        in MinorActorCultureField neighborhood,
        in MinorActorCultureAttachment attachment)
    {
        var interiorHoleSupport = InteriorHoleSupport(ownRaw, attachment);
        if (ownRaw < CultureTraceRawInfluence && interiorHoleSupport <= 0)
        {
            return 0;
        }

        var ownPresence = FixedScalar.Max(
            Saturating(ownRaw, CultureSupportedRawInfluence),
            interiorHoleSupport * FixedScalar.FromUnits(750000L));
        var localitySupport = FixedScalar.Max(
            Saturating(attachment.ConnectedRawMass, 4),
            interiorHoleSupport);
        var neighborSupport = Clamp01(
            FixedScalar.FromRatio(neighborhood.SupportedAdjacentCount, 5));
        var rawHeadroom = Clamp01((CultureFieldRawInfluence - ownRaw) / CultureFieldRawInfluence);
        return InteriorHoleBoost(interiorHoleSupport) *
            ownPresence *
            (FixedScalar.FromUnits(350000L) + localitySupport) *
            (FixedScalar.FromUnits(450000L) + neighborSupport) *
            (FixedScalar.FromUnits(250000L) + rawHeadroom) *
            CultureInfluenceTargetScorer.StrongCenterBoost(attachment.StrongCenterSupport) *
            ExpansionForeignDrag(ownShare, ownRaw, foreignShare);
    }

    private static FixedScalar ExtendBorderScore(
        FixedScalar ownShare,
        FixedScalar ownRaw,
        FixedScalar foreignShare,
        in MinorActorCultureAttachment attachment)
    {
        if (attachment.ConnectedRawMass <= 0 ||
            attachment.AdjacentSupportedCount <= 0 ||
            ownRaw >= CultureStrongRawInfluence)
        {
            return 0;
        }

        var shareHeadroom = Clamp01((100 - ownShare) / 100);
        var rawShortfall = Clamp01((CultureStrongRawInfluence - ownRaw) / CultureStrongRawInfluence);
        var localitySupport = Saturating(attachment.ConnectedRawMass, 4);
        var edgeSupport = Saturating(attachment.AdjacentSupportedCount, 2);
        var contestBoost = 1 + (FixedScalar.FromUnits(350000L) * Clamp01(foreignShare / 100));
        return InteriorHoleBoost(InteriorHoleSupport(ownRaw, attachment)) *
            localitySupport *
            edgeSupport *
            shareHeadroom *
            (FixedScalar.FromUnits(250000L) + rawShortfall) *
            contestBoost *
            ExpansionForeignDrag(ownShare, ownRaw, foreignShare);
    }

    private static FixedScalar BridgeGapScore(
        FixedScalar ownShare,
        FixedScalar ownRaw,
        FixedScalar foreignShare,
        in MinorActorCultureAttachment attachment)
    {
        if (attachment.AdjacentSupportedCount < 2 ||
            ownRaw >= CultureSupportedRawInfluence)
        {
            return 0;
        }

        var shareHeadroom = Clamp01((100 - ownShare) / 100);
        var localitySupport = Saturating(attachment.ConnectedRawMass, 4);
        var bridgeSupport = 1 +
            (FixedScalar.FromUnits(150000L) * global::System.Math.Min(3, attachment.AdjacentSupportedCount - 1));
        return InteriorHoleBoost(InteriorHoleSupport(ownRaw, attachment)) *
            localitySupport *
            shareHeadroom *
            bridgeSupport *
            ExpansionForeignDrag(ownShare, ownRaw, foreignShare);
    }

    private static FixedScalar ReduceOtherCultureScore(
        FixedScalar ownShare,
        FixedScalar ownRaw,
        FixedScalar foreignShare,
        FixedScalar foreignRaw,
        in MinorActorCultureField neighborhood,
        in MinorActorCultureAttachment attachment)
    {
        if (foreignRaw <= 0 && foreignShare <= 0)
        {
            return 0;
        }

        var localitySupport = Saturating(attachment.ConnectedRawMass, 4);
        var connectedSupport = Clamp01(neighborhood.ConnectedRaw / CultureFieldRawInfluence);
        var interiorHoleSupport = InteriorHoleSupport(ownRaw, attachment);
        var support = FixedScalar.Max(
            FixedScalar.Max(localitySupport, connectedSupport),
            interiorHoleSupport);
        if (support <= 0)
        {
            return 0;
        }

        var ownPresence = FixedScalar.Max(
            FixedScalar.Max(
                Clamp01(ownShare / 100),
                Saturating(ownRaw, CultureSupportedRawInfluence)),
            interiorHoleSupport);
        var foreignPresence = ForeignCleanupSignal(foreignShare, foreignRaw);
        return InteriorHoleBoost(interiorHoleSupport) *
            foreignPresence *
            support *
            (FixedScalar.FromUnits(350000L) + ownPresence) *
            (FixedScalar.FromUnits(750000L) + (FixedScalar.FromUnits(500000L) * ownPresence));
    }

    private static FixedScalar InteriorHoleSupport(FixedScalar ownRaw, in MinorActorCultureAttachment attachment) =>
        CultureInfluenceTargetScorer.InteriorHoleSupport(
            ownRaw,
            attachment.ConnectedRawMass,
            attachment.AdjacentSupportedCount);

    private static FixedScalar InteriorHoleBoost(FixedScalar support) =>
        1 + (2 * support);

    private static FixedScalar ExpansionForeignDrag(FixedScalar ownShare, FixedScalar ownRaw, FixedScalar foreignShare)
    {
        var foreignCompetition = ForeignContestSignal(foreignShare);
        if (foreignCompetition <= 0)
        {
            return 1;
        }

        var ownBoundary = FixedScalar.Max(
            Clamp01(ownShare / 100),
            Saturating(ownRaw, CultureStrongRawInfluence));
        return 1 - (FixedScalar.FromUnits(700000L) * foreignCompetition * (1 - ownBoundary));
    }

    private static FixedScalar ForeignCleanupSignal(FixedScalar foreignShare, FixedScalar foreignRaw)
    {
        var shareSignal = ForeignContestSignal(foreignShare);
        var rawSignal = Saturating(foreignRaw, CultureSupportedRawInfluence) * FixedScalar.FromUnits(650000L);
        return FixedScalar.Max(shareSignal, rawSignal);
    }

    private static FixedScalar ForeignContestSignal(FixedScalar foreignShare)
    {
        if (foreignShare <= CultureForeignTraceToleranceShare)
        {
            return 0;
        }

        return Clamp01((foreignShare - CultureForeignTraceToleranceShare) / CultureForeignContestScaleShare);
    }

    private static FixedScalar RouteCultureScore(
        Simulation simulation,
        int planeCode,
        int q,
        int r,
        FixedScalar ownRaw,
        in MinorActorCultureField neighborhood,
        in MinorActorCultureAttachment attachment,
        int tick)
    {
        var roadQuality = simulation.Movement.RoadQuality(planeCode, q, r, tick);
        var travelCount = simulation.Movement.RoadTravelCount(planeCode, q, r);
        var roadMemory = FixedScalar.Max(
            Clamp01(roadQuality / 100),
            Clamp01(FixedScalar.FromRatio(travelCount, 4)));
        if (roadMemory <= 0)
        {
            return 0;
        }

        var rawHeadroom = Clamp01((CultureStrongRawInfluence - ownRaw) / CultureStrongRawInfluence);
        var localCultureSupport = Clamp01(neighborhood.ConnectedRaw / CultureFieldRawInfluence);
        var localitySupport = Saturating(attachment.ConnectedRawMass, 4);
        return roadMemory *
            rawHeadroom *
            (FixedScalar.FromUnits(250000L) + localCultureSupport + localitySupport);
    }

    private static FixedScalar RoadMaintenanceScore(
        Simulation simulation,
        int planeCode,
        int q,
        int r,
        in MinorActorCultureField neighborhood,
        int tick)
    {
        var roadQuality = simulation.Movement.RoadQuality(planeCode, q, r, tick);
        var travelCount = simulation.Movement.RoadTravelCount(planeCode, q, r);
        var roadNeed = Clamp01((100 - roadQuality) / 100);
        var travelMemory = Clamp01(FixedScalar.FromRatio(travelCount, 8));
        var cultureSupport = Clamp01(neighborhood.ConnectedRaw / CultureFieldRawInfluence);
        if (travelMemory <= 0 && cultureSupport <= 0)
        {
            return 0;
        }

        return (FixedScalar.FromUnits(250000L) + cultureSupport) * (FixedScalar.FromUnits(350000L) + travelMemory) * (FixedScalar.FromUnits(200000L) + roadNeed);
    }
}
