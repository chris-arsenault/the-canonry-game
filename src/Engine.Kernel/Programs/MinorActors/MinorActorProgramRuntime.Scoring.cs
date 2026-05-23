namespace Engine.Kernel;

using Engine.Constants;

internal sealed partial class MinorActorProgramRuntime
{
    private float MinorVerbScore(
        Simulation simulation,
        int actorId,
        int verb,
        int planeCode,
        int q,
        int r,
        float ownShare,
        float ownRaw,
        float foreignShare,
        float foreignRaw,
        in MinorActorCultureField neighborhood,
        in MinorActorCultureAttachment attachment,
        int tick)
    {
        if (Data.VerbActionCodes[verb] == MinorActorActionCodes.RoadMaintenance)
        {
            return RoadMaintenanceScore(simulation, planeCode, q, r, neighborhood, tick);
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
                tick);
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

    private static float CultureTargetScore(
        int targetingCode,
        float ownShare,
        float ownRaw,
        float foreignShare,
        float foreignRaw,
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
            _ => 0f,
        };
    }

    private static float LifecycleMaintenanceScore(
        Simulation simulation,
        int sourceEntityId,
        float ownShare,
        float ownRaw,
        in MinorActorCultureField neighborhood,
        in MinorActorCultureAttachment attachment,
        int tick)
    {
        if (!simulation.Upkeep.TryGetLifecycleMaintenanceNeed(
                simulation,
                sourceEntityId,
                tick,
                out _,
                out var ageDrainComponent,
                out var healthShortfall))
        {
            return 0f;
        }

        var connectedSupport = global::System.MathF.Max(
            Saturating(attachment.ComponentMass, 4f),
            Clamp01(neighborhood.ConnectedRaw / CultureFieldRawInfluence));
        if (connectedSupport <= 0f)
        {
            return 0f;
        }

        var ownPresence = global::System.MathF.Max(
            Clamp01(ownShare / 100f),
            Saturating(ownRaw, CultureSupportedRawInfluence));
        var ageLoad = Saturating(ageDrainComponent, 0.20f);
        var maintenanceNeed = Clamp01(healthShortfall + (LifecycleMaintenanceEarlyNeedScale * ageLoad));
        return maintenanceNeed *
            (0.50f + ageLoad) *
            connectedSupport *
            (0.50f + ownPresence) *
            LifecycleMaintenanceScoreScale;
    }

    private static float ReinforceCoreScore(
        float ownShare,
        float ownRaw,
        float foreignShare,
        in MinorActorCultureField neighborhood,
        in MinorActorCultureAttachment attachment)
    {
        var interiorHoleSupport = InteriorHoleSupport(ownRaw, attachment);
        if (ownRaw < CultureTraceRawInfluence && interiorHoleSupport <= 0f)
        {
            return 0f;
        }

        var ownPresence = global::System.MathF.Max(
            Saturating(ownRaw, CultureSupportedRawInfluence),
            interiorHoleSupport * 0.75f);
        var componentSupport = global::System.MathF.Max(
            Saturating(attachment.ComponentMass, 4f),
            interiorHoleSupport);
        var neighborSupport = Clamp01(neighborhood.SupportedAdjacentCount / 5f);
        var rawHeadroom = Clamp01((CultureFieldRawInfluence - ownRaw) / CultureFieldRawInfluence);
        return InteriorHoleBoost(interiorHoleSupport) *
            ownPresence *
            (0.35f + componentSupport) *
            (0.45f + neighborSupport) *
            (0.25f + rawHeadroom) *
            ExpansionForeignDrag(ownShare, ownRaw, foreignShare);
    }

    private static float ExtendBorderScore(
        float ownShare,
        float ownRaw,
        float foreignShare,
        in MinorActorCultureAttachment attachment)
    {
        if (attachment.ComponentMass <= 0f ||
            attachment.AdjacentSupportedCount <= 0 ||
            ownRaw >= CultureStrongRawInfluence)
        {
            return 0f;
        }

        var shareHeadroom = Clamp01((100f - ownShare) / 100f);
        var rawShortfall = Clamp01((CultureStrongRawInfluence - ownRaw) / CultureStrongRawInfluence);
        var componentSupport = Saturating(attachment.ComponentMass, 4f);
        var edgeSupport = Saturating(attachment.AdjacentSupportedCount, 2f);
        var contestBoost = 1f + (0.35f * Clamp01(foreignShare / 100f));
        return InteriorHoleBoost(InteriorHoleSupport(ownRaw, attachment)) *
            componentSupport *
            edgeSupport *
            shareHeadroom *
            (0.25f + rawShortfall) *
            contestBoost *
            ExpansionForeignDrag(ownShare, ownRaw, foreignShare);
    }

    private static float BridgeGapScore(
        float ownShare,
        float ownRaw,
        float foreignShare,
        in MinorActorCultureAttachment attachment)
    {
        if (attachment.ComponentCount < 2 ||
            ownRaw >= CultureSupportedRawInfluence)
        {
            return 0f;
        }

        var shareHeadroom = Clamp01((100f - ownShare) / 100f);
        var componentSupport = Saturating(attachment.ComponentMass, 4f);
        var bridgeSupport = 1f + (0.45f * global::System.Math.Min(3, attachment.ComponentCount - 1));
        return InteriorHoleBoost(InteriorHoleSupport(ownRaw, attachment)) *
            componentSupport *
            shareHeadroom *
            bridgeSupport *
            ExpansionForeignDrag(ownShare, ownRaw, foreignShare);
    }

    private static float ReduceOtherCultureScore(
        float ownShare,
        float ownRaw,
        float foreignShare,
        float foreignRaw,
        in MinorActorCultureField neighborhood,
        in MinorActorCultureAttachment attachment)
    {
        if (foreignRaw <= 0f && foreignShare <= 0f)
        {
            return 0f;
        }

        var componentSupport = Saturating(attachment.ComponentMass, 4f);
        var connectedSupport = Clamp01(neighborhood.ConnectedRaw / CultureFieldRawInfluence);
        var interiorHoleSupport = InteriorHoleSupport(ownRaw, attachment);
        var support = global::System.MathF.Max(
            global::System.MathF.Max(componentSupport, connectedSupport),
            interiorHoleSupport);
        if (support <= 0f)
        {
            return 0f;
        }

        var ownPresence = global::System.MathF.Max(
            global::System.MathF.Max(
                Clamp01(ownShare / 100f),
                Saturating(ownRaw, CultureSupportedRawInfluence)),
            interiorHoleSupport);
        var foreignPresence = ForeignCleanupSignal(foreignShare, foreignRaw);
        return InteriorHoleBoost(interiorHoleSupport) *
            foreignPresence *
            support *
            (0.35f + ownPresence) *
            (0.75f + (0.5f * ownPresence));
    }

    private static float InteriorHoleSupport(float ownRaw, in MinorActorCultureAttachment attachment) =>
        CultureInfluenceTargetScorer.InteriorHoleSupport(
            ownRaw,
            attachment.ComponentMass,
            attachment.AdjacentSupportedCount);

    private static float InteriorHoleBoost(float support) =>
        1f + (2f * support);

    private static float ExpansionForeignDrag(float ownShare, float ownRaw, float foreignShare)
    {
        var foreignCompetition = ForeignContestSignal(foreignShare);
        if (foreignCompetition <= 0f)
        {
            return 1f;
        }

        var ownBoundary = global::System.MathF.Max(
            Clamp01(ownShare / 100f),
            Saturating(ownRaw, CultureStrongRawInfluence));
        return 1f - (0.7f * foreignCompetition * (1f - ownBoundary));
    }

    private static float ForeignCleanupSignal(float foreignShare, float foreignRaw)
    {
        var shareSignal = ForeignContestSignal(foreignShare);
        var rawSignal = Saturating(foreignRaw, CultureSupportedRawInfluence) * 0.65f;
        return global::System.MathF.Max(shareSignal, rawSignal);
    }

    private static float ForeignContestSignal(float foreignShare)
    {
        if (foreignShare <= CultureForeignTraceToleranceShare)
        {
            return 0f;
        }

        return Clamp01((foreignShare - CultureForeignTraceToleranceShare) / CultureForeignContestScaleShare);
    }

    private static float RouteCultureScore(
        Simulation simulation,
        int planeCode,
        int q,
        int r,
        float ownRaw,
        in MinorActorCultureField neighborhood,
        in MinorActorCultureAttachment attachment,
        int tick)
    {
        var roadQuality = simulation.Movement.RoadQuality(planeCode, q, r, tick);
        var travelCount = simulation.Movement.RoadTravelCount(planeCode, q, r);
        var roadMemory = global::System.MathF.Max(Clamp01(roadQuality / 100f), Clamp01(travelCount / 4f));
        if (roadMemory <= 0f)
        {
            return 0f;
        }

        var rawHeadroom = Clamp01((CultureStrongRawInfluence - ownRaw) / CultureStrongRawInfluence);
        var localCultureSupport = Clamp01(neighborhood.ConnectedRaw / CultureFieldRawInfluence);
        var componentSupport = Saturating(attachment.ComponentMass, 4f);
        return roadMemory *
            rawHeadroom *
            (0.25f + localCultureSupport + componentSupport);
    }

    private static float RoadMaintenanceScore(
        Simulation simulation,
        int planeCode,
        int q,
        int r,
        in MinorActorCultureField neighborhood,
        int tick)
    {
        var roadQuality = simulation.Movement.RoadQuality(planeCode, q, r, tick);
        var travelCount = simulation.Movement.RoadTravelCount(planeCode, q, r);
        var roadNeed = Clamp01((100f - roadQuality) / 100f);
        var travelMemory = Clamp01(travelCount / 8f);
        var cultureSupport = Clamp01(neighborhood.ConnectedRaw / CultureFieldRawInfluence);
        if (travelMemory <= 0f && cultureSupport <= 0f)
        {
            return 0f;
        }

        return (0.25f + cultureSupport) * (0.35f + travelMemory) * (0.2f + roadNeed);
    }
}
