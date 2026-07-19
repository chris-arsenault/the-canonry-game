namespace Engine.WorldGen;

using Engine.Constants;

internal sealed class ImpactCrateringLayer : IPhysicalGeographyLayer
{
    public PhysicalGeographyLayerContract Contract { get; } = new(
        "impact-cratering",
        0x4d73a1,
        PhysicalGeographyFacts.Elevation | PhysicalGeographyFacts.LandscapeHistory,
        PhysicalGeographyFacts.Elevation |
        PhysicalGeographyFacts.SedimentSupply |
        PhysicalGeographyFacts.Impacts);

    public void Apply(PhysicalGeographyField map, PhysicalGeographyGenerationContext context)
    {
        map.ImpactExcavation.Clear();
        map.ImpactRimUplift.Clear();
        map.ImpactEjectaThickness.Clear();
        map.ImpactHeat.Clear();
        var centers = SelectCenters(map, context);
        for (var impactIndex = 0; impactIndex < centers.Length; impactIndex++)
        {
            var radius = ImpactRadius(map, context, impactIndex);
            ApplyImpact(map, context.Profile, centers[impactIndex], radius);
        }
    }

    public void Validate(PhysicalGeographyField map, PhysicalGeographyGenerationContext context)
    {
        LandscapeSurface.Validate(map);
        var centers = 0;
        var excavated = 0;
        var raisedRim = 0;
        for (var triangleIndex = 0; triangleIndex < map.Grid.TriangleCount; triangleIndex++)
        {
            if (!ImpactStateIsValid(map, triangleIndex))
            {
                throw new InvalidOperationException("Impact geography contains invalid physical history.");
            }

            centers += (map.FeatureFlags[triangleIndex] & PhysicalGeographyFeatureCodes.ImpactCenter) != 0
                ? 1
                : 0;
            excavated += map.ImpactExcavation[triangleIndex] > 0 ? 1 : 0;
            raisedRim += map.ImpactRimUplift[triangleIndex] > 0 ? 1 : 0;
        }

        if (context.Profile.ImpactCraterCount > 0 &&
            (centers == 0 || excavated == 0 || raisedRim == 0))
        {
            throw new InvalidOperationException("Configured impacts did not produce crater and rim geography.");
        }
    }

    private static bool ImpactStateIsValid(PhysicalGeographyField map, int triangleIndex) =>
        map.ImpactExcavation[triangleIndex] >= 0 &&
        map.ImpactRimUplift[triangleIndex] >= 0 &&
        map.ImpactEjectaThickness[triangleIndex] >= 0 &&
        map.ImpactHeat[triangleIndex] is >= 0 and <= 1000 &&
        ((map.FeatureFlags[triangleIndex] & PhysicalGeographyFeatureCodes.ImpactCenter) == 0 ||
            map.ImpactExcavation[triangleIndex] > 0);

    private static int[] SelectCenters(
        PhysicalGeographyField map,
        PhysicalGeographyGenerationContext context)
    {
        var count = ScaledImpactCount(map, context.Profile.ImpactCraterCount);
        if (count == 0)
        {
            return [];
        }

        var selection = new BoundedCandidateSelection(Math.Max(256, count * 256));
        for (var triangleIndex = 0; triangleIndex < map.Grid.TriangleCount; triangleIndex++)
        {
            if (!map.Grid.TriangleIsInsideConfiguredDisk(triangleIndex))
            {
                continue;
            }

            var x = (int)Math.Round(
                map.Grid.TriangleX(triangleIndex) * 256d,
                MidpointRounding.AwayFromZero);
            var y = (int)Math.Round(
                map.Grid.TriangleY(triangleIndex) * 256d,
                MidpointRounding.AwayFromZero);
            selection.Consider(triangleIndex, PhysicalNoise.HashUnit(context.Seed, x, y));
        }

        return SelectSeparated(map, selection.GetBestIndexes(), count, context.Profile);
    }

    private static int ScaledImpactCount(PhysicalGeographyField map, int configuredCount) =>
        configuredCount == 0
            ? 0
            : Math.Min(configuredCount, Math.Max(1, map.Grid.ConfiguredHexCount / 150));

    private static int[] SelectSeparated(
        PhysicalGeographyField map,
        IReadOnlyList<int> candidates,
        int count,
        PhysicalGeographyGenerationProfile profile)
    {
        var spacing = Math.Min(
            profile.ImpactMinimumSpacing,
            Math.Max(3d, map.Grid.Radius * 0.72d));
        var minimumSquared = spacing * spacing;
        var selected = new List<int>(count);
        foreach (var candidate in candidates)
        {
            if (selected.TrueForAll(center => DistanceSquared(map, center, candidate) >= minimumSquared))
            {
                selected.Add(candidate);
                if (selected.Count == count)
                {
                    return selected.ToArray();
                }
            }
        }

        foreach (var candidate in candidates)
        {
            if (!selected.Contains(candidate))
            {
                selected.Add(candidate);
                if (selected.Count == count)
                {
                    break;
                }
            }
        }

        return selected.ToArray();
    }

    private static double ImpactRadius(
        PhysicalGeographyField map,
        PhysicalGeographyGenerationContext context,
        int impactIndex)
    {
        var maximum = Math.Min(
            context.Profile.ImpactMaximumRadius,
            Math.Max(2d, map.Grid.Radius * 0.24d));
        var minimum = Math.Min(context.Profile.ImpactMinimumRadius, maximum * 0.72d);
        return minimum + ((maximum - minimum) *
            PhysicalNoise.HashUnit(context.Seed ^ 0x3a91, impactIndex, 0));
    }

    private static void ApplyImpact(
        PhysicalGeographyField map,
        PhysicalGeographyGenerationProfile profile,
        int center,
        double radius)
    {
        map.FeatureFlags[center] |= PhysicalGeographyFeatureCodes.ImpactCenter;
        var scale = radius / Math.Max(radius, profile.ImpactMaximumRadius);
        var depth = Math.Max(
            1,
            (int)Math.Round(
                profile.ElevationScale * profile.ImpactDepthRatio / 1000d * scale,
                MidpointRounding.AwayFromZero));
        var rimHeight = Math.Max(
            1,
            (int)Math.Round(
                profile.ElevationScale * profile.ImpactRimHeightRatio / 1000d * scale,
                MidpointRounding.AwayFromZero));
        var ejecta = new List<WeightedTriangle>();
        long excavation = 0;
        for (var triangleIndex = 0; triangleIndex < map.Grid.TriangleCount; triangleIndex++)
        {
            var distance = Math.Sqrt(DistanceSquared(map, center, triangleIndex));
            excavation += Excavate(map, triangleIndex, distance, radius, depth);
            RaiseRim(map, triangleIndex, distance, radius, rimHeight);
            FractureAndHeat(map, profile, triangleIndex, distance, radius);
            AddEjectaCandidate(ejecta, triangleIndex, distance, radius);
        }

        map.SedimentProducedTotal = checked(map.SedimentProducedTotal + excavation);
        var deposited = DepositEjecta(map, profile, ejecta, excavation);
        map.SedimentExportedTotal = checked(
            map.SedimentExportedTotal + excavation - deposited);
    }

    private static int Excavate(
        PhysicalGeographyField map,
        int triangleIndex,
        double distance,
        double radius,
        int depth)
    {
        if (distance > radius)
        {
            return 0;
        }

        var normalized = distance / radius;
        var cut = Math.Max(
            1,
            (int)Math.Round(depth * (1d - (normalized * normalized)), MidpointRounding.AwayFromZero));
        map.BedrockElevation[triangleIndex] = checked(map.BedrockElevation[triangleIndex] - cut);
        map.ImpactExcavation[triangleIndex] = checked(map.ImpactExcavation[triangleIndex] + cut);
        LandscapeSurface.Rebuild(map, triangleIndex);
        return cut;
    }

    private static void RaiseRim(
        PhysicalGeographyField map,
        int triangleIndex,
        double distance,
        double radius,
        int rimHeight)
    {
        var normalized = distance / radius;
        var influence = 1d - (Math.Abs(normalized - 1d) / 0.34d);
        if (influence <= 0d)
        {
            return;
        }

        var uplift = Math.Max(
            1,
            (int)Math.Round(rimHeight * influence, MidpointRounding.AwayFromZero));
        map.BedrockElevation[triangleIndex] = checked(map.BedrockElevation[triangleIndex] + uplift);
        map.ImpactRimUplift[triangleIndex] = checked(map.ImpactRimUplift[triangleIndex] + uplift);
        LandscapeSurface.Rebuild(map, triangleIndex);
    }

    private static void FractureAndHeat(
        PhysicalGeographyField map,
        PhysicalGeographyGenerationProfile profile,
        int triangleIndex,
        double distance,
        double radius)
    {
        var fractureInfluence = 1d - (distance / (radius * 1.45d));
        if (fractureInfluence > 0d)
        {
            map.FractureConductance[triangleIndex] = Math.Max(
                map.FractureConductance[triangleIndex],
                (int)Math.Round(
                    profile.ImpactFractureStrength * fractureInfluence,
                    MidpointRounding.AwayFromZero));
        }

        if (radius < profile.ImpactThermalRadiusThreshold || distance > radius)
        {
            return;
        }

        map.ImpactHeat[triangleIndex] = Math.Max(
            map.ImpactHeat[triangleIndex],
            (int)Math.Round(1000d * (1d - (distance / radius)), MidpointRounding.AwayFromZero));
    }

    private static void AddEjectaCandidate(
        List<WeightedTriangle> candidates,
        int triangleIndex,
        double distance,
        double radius)
    {
        if (distance < radius || distance > radius * 2.2d)
        {
            return;
        }

        var weight = Math.Max(
            1,
            (int)Math.Round(
                1000d * (1d - ((distance - radius) / (radius * 1.2d))),
                MidpointRounding.AwayFromZero));
        candidates.Add(new WeightedTriangle(triangleIndex, weight));
    }

    private static long DepositEjecta(
        PhysicalGeographyField map,
        PhysicalGeographyGenerationProfile profile,
        IReadOnlyList<WeightedTriangle> candidates,
        long excavation)
    {
        var remaining = excavation * profile.ImpactEjectaFraction / 1000L;
        var remainingWeight = candidates.Sum(candidate => (long)candidate.Weight);
        var deposited = 0L;
        foreach (var candidate in candidates)
        {
            if (remaining <= 0 || remainingWeight <= 0)
            {
                break;
            }

            var requested = (int)Math.Min(
                int.MaxValue,
                Math.Max(1L, remaining * candidate.Weight / remainingWeight));
            var amount = LandscapeSurface.Deposit(map, candidate.TriangleIndex, requested);
            map.ImpactEjectaThickness[candidate.TriangleIndex] = checked(
                map.ImpactEjectaThickness[candidate.TriangleIndex] + amount);
            remaining -= amount;
            remainingWeight -= candidate.Weight;
            deposited += amount;
        }

        return deposited;
    }

    private static double DistanceSquared(PhysicalGeographyField map, int first, int second)
    {
        var dx = map.Grid.TriangleX(first) - map.Grid.TriangleX(second);
        var dy = map.Grid.TriangleY(first) - map.Grid.TriangleY(second);
        return (dx * dx) + (dy * dy);
    }

    private readonly record struct WeightedTriangle(int TriangleIndex, int Weight);
}
