namespace Engine.WorldGen;

using Engine.Constants;

internal sealed class TectonicProvinceLayer : IPhysicalGeographyLayer
{
    public PhysicalGeographyLayerContract Contract { get; } = new(
        "tectonic-provinces",
        0x6b31e7,
        PhysicalGeographyFacts.None,
        PhysicalGeographyFacts.TectonicProvinces,
        PhysicalGeographyFacts.ContinentalDomains |
        PhysicalGeographyFacts.Elevation |
        PhysicalGeographyFacts.Ocean |
        PhysicalGeographyFacts.Fractures |
        PhysicalGeographyFacts.Geothermal |
        PhysicalGeographyFacts.Validated);

    public void Apply(PhysicalGeographyField map, PhysicalGeographyGenerationContext context)
    {
        var sites = CreateSites(map.Grid, context);
        for (var triangleIndex = 0; triangleIndex < map.Grid.TriangleCount; triangleIndex++)
        {
            AssignTriangle(map, context, sites, triangleIndex);
        }
    }

    public void Validate(PhysicalGeographyField map, PhysicalGeographyGenerationContext context)
    {
        _ = context;
        var measurements = new ProvinceMeasurements();
        for (var triangleIndex = 0; triangleIndex < map.Grid.TriangleCount; triangleIndex++)
        {
            ValidatePrimitive(map, triangleIndex);
            measurements.Add(map, triangleIndex);
        }

        if (!measurements.IsComplete)
        {
            throw new InvalidOperationException("Tectonic provinces lack distinct crust and active boundaries.");
        }
    }

    private static void ValidatePrimitive(PhysicalGeographyField map, int triangleIndex)
    {
        if (IdentityIsInvalid(map, triangleIndex) ||
            ScalarIsInvalid(map, triangleIndex) ||
            MotionIsInvalid(map, triangleIndex))
        {
            throw new InvalidOperationException("Tectonic province generation produced an invalid primitive.");
        }
    }

    private static bool IdentityIsInvalid(PhysicalGeographyField map, int triangleIndex) =>
        map.CrustProvinceIds[triangleIndex] <= 0 ||
        map.CrustTypeCodes[triangleIndex] is < PhysicalCrustTypeCodes.Min or > PhysicalCrustTypeCodes.Max ||
        map.TectonicBoundaryCodes[triangleIndex] is < PhysicalTectonicBoundaryCodes.Min or
            > PhysicalTectonicBoundaryCodes.Max;

    private static bool ScalarIsInvalid(PhysicalGeographyField map, int triangleIndex) =>
        map.CrustBasePotential[triangleIndex] is < 0 or > 1000 ||
        map.CrustAssemblySeparation[triangleIndex] is < 0 or > 1000 ||
        map.TectonicStrain[triangleIndex] is < 0 or > 1000;

    private static bool MotionIsInvalid(PhysicalGeographyField map, int triangleIndex) =>
        map.CrustMotionX[triangleIndex] is < -1000 or > 1000 ||
        map.CrustMotionY[triangleIndex] is < -1000 or > 1000;

    private static ProvinceSite[] CreateSites(
        PhysicalGeographyGrid grid,
        PhysicalGeographyGenerationContext context)
    {
        var candidateCount = checked(
            context.Profile.TectonicProvinceCount * context.Profile.TectonicSiteCandidateMultiplier);
        var candidates = new SiteCandidate[candidateCount];
        var placementRadius = grid.WorkRadius * Math.Sqrt(3d) * 0.94d;
        for (var index = 0; index < candidates.Length; index++)
        {
            var angle = PhysicalNoise.HashUnit(context.Seed ^ 0x1b73, index, 0) * Math.PI * 2d;
            var radius = Math.Sqrt(PhysicalNoise.HashUnit(context.Seed ^ 0x2db9, index, 1)) *
                placementRadius;
            candidates[index] = new SiteCandidate(
                Math.Cos(angle) * radius,
                Math.Sin(angle) * radius,
                PhysicalNoise.HashUnit(context.Seed ^ 0x4a91, index, 2));
        }

        var selected = SelectSeparatedCandidates(candidates, context.Profile.TectonicProvinceCount);
        var assemblies = AssignContinentalAssemblies(selected, grid, context.Profile);
        var sites = new ProvinceSite[selected.Length];
        for (var index = 0; index < sites.Length; index++)
        {
            var angle = PhysicalNoise.HashUnit(context.Seed ^ 0x5c27, index, 0) * Math.PI * 2d;
            var speed = Lerp(
                context.Profile.TectonicMotionMinimum,
                context.Profile.TectonicMotionMaximum,
                PhysicalNoise.HashUnit(context.Seed ^ 0x73a1, index, 1));
            sites[index] = new ProvinceSite(
                selected[index].X,
                selected[index].Y,
                assemblies[index],
                Math.Cos(angle) * speed,
                Math.Sin(angle) * speed);
        }

        return sites;
    }

    private static SiteCandidate[] SelectSeparatedCandidates(
        SiteCandidate[] candidates,
        int count)
    {
        var selected = new SiteCandidate[count];
        selected[0] = candidates.MaxBy(static candidate => candidate.Priority);
        for (var selectedIndex = 1; selectedIndex < selected.Length; selectedIndex++)
        {
            var best = candidates[0];
            var bestScore = double.MinValue;
            foreach (var candidate in candidates)
            {
                var minimumDistance = double.MaxValue;
                for (var existingIndex = 0; existingIndex < selectedIndex; existingIndex++)
                {
                    minimumDistance = Math.Min(
                        minimumDistance,
                        DistanceSquared(candidate, selected[existingIndex]));
                }

                var score = minimumDistance * (0.88d + (candidate.Priority * 0.24d));
                if (score > bestScore)
                {
                    best = candidate;
                    bestScore = score;
                }
            }

            selected[selectedIndex] = best;
        }

        return selected;
    }

    private static int[] AssignContinentalAssemblies(
        SiteCandidate[] sites,
        PhysicalGeographyGrid grid,
        PhysicalGeographyGenerationProfile profile)
    {
        var count = Math.Clamp(
            (int)Math.Round(
                sites.Length * profile.ContinentalCrustFraction,
                MidpointRounding.AwayFromZero),
            2,
            sites.Length - 2);
        var result = new int[sites.Length];
        var anchors = SelectAssemblyAnchors(sites, grid, profile.ContinentalAssemblyCount);
        for (var anchorIndex = 0; anchorIndex < anchors.Length; anchorIndex++)
        {
            result[anchors[anchorIndex]] = anchorIndex + 1;
        }

        var selectedCount = anchors.Length;
        while (selectedCount < count)
        {
            for (var anchorIndex = 0; anchorIndex < anchors.Length && selectedCount < count; anchorIndex++)
            {
                var neighbor = NearestUnselectedSite(sites, result, anchors[anchorIndex]);
                if (neighbor >= 0)
                {
                    result[neighbor] = anchorIndex + 1;
                    selectedCount++;
                }
            }
        }

        return result;
    }

    private static int[] SelectAssemblyAnchors(
        SiteCandidate[] sites,
        PhysicalGeographyGrid grid,
        int count)
    {
        var continentalExtent = grid.Radius * Math.Sqrt(3d) * 0.92d;
        var eligible = Enumerable.Range(0, sites.Length)
            .Where(index => DistanceSquaredFromOrigin(sites[index]) <=
                continentalExtent * continentalExtent)
            .ToArray();
        if (eligible.Length < count)
        {
            eligible = Enumerable.Range(0, sites.Length).ToArray();
        }

        var anchors = new int[count];
        anchors[0] = eligible.MaxBy(index => sites[index].Priority);
        for (var anchorIndex = 1; anchorIndex < anchors.Length; anchorIndex++)
        {
            anchors[anchorIndex] = eligible
                .Where(candidate => !anchors.Take(anchorIndex).Contains(candidate))
                .MaxBy(candidate => MinimumDistanceSquared(sites, candidate, anchors, anchorIndex));
        }

        return anchors;
    }

    private static int NearestUnselectedSite(
        SiteCandidate[] sites,
        int[] selected,
        int anchor)
    {
        var best = -1;
        var bestDistance = double.MaxValue;
        for (var candidate = 0; candidate < sites.Length; candidate++)
        {
            var distance = DistanceSquared(sites[anchor], sites[candidate]);
            if (selected[candidate] == 0 && distance < bestDistance)
            {
                best = candidate;
                bestDistance = distance;
            }
        }

        return best;
    }

    private static double MinimumDistanceSquared(
        SiteCandidate[] sites,
        int candidate,
        int[] anchors,
        int count)
    {
        var minimum = double.MaxValue;
        for (var index = 0; index < count; index++)
        {
            minimum = Math.Min(minimum, DistanceSquared(sites[candidate], sites[anchors[index]]));
        }

        return minimum;
    }

    private static double DistanceSquaredFromOrigin(SiteCandidate site) =>
        (site.X * site.X) + (site.Y * site.Y);

    private static void AssignTriangle(
        PhysicalGeographyField map,
        PhysicalGeographyGenerationContext context,
        ReadOnlySpan<ProvinceSite> sites,
        int triangleIndex)
    {
        var sample = WarpedSample(map, context, triangleIndex);
        var selection = SelectSites(sites, sample.X, sample.Y);
        var site = sites[selection.Nearest];
        map.CrustProvinceIds[triangleIndex] = selection.Nearest + 1;
        map.CrustMotionX[triangleIndex] = ScaleMotion(site.MotionX);
        map.CrustMotionY[triangleIndex] = ScaleMotion(site.MotionY);
        var affinity = Math.Clamp(
            0.5d +
                ((selection.OceanicDistance - selection.ContinentalDistance) /
                    (context.Profile.ContinentalTransitionWidth * 2d)),
            0d,
            1d);
        var potential = site.Continental
            ? Math.Max(affinity, context.Profile.ContinentalCrustPotentialFloor)
            : Math.Min(affinity, context.Profile.OceanicCrustPotentialCeiling);
        var separation = InterAssemblySeparation(sites, selection, context.Profile);
        potential -= separation;
        if (site.Continental && separation > 0d)
        {
            potential = Math.Min(
                potential,
                context.Profile.ContinentalCrustPotentialFloor - separation);
        }

        var riftedMargin = site.Continental &&
            separation >= context.Profile.ContinentalAssemblySeparationDepth * 0.12d;
        map.CrustAssemblyIds[triangleIndex] = riftedMargin ? 0 : site.AssemblyId;
        map.CrustTypeCodes[triangleIndex] = site.Continental && !riftedMargin
            ? PhysicalCrustTypeCodes.Continental
            : PhysicalCrustTypeCodes.Oceanic;
        if (riftedMargin)
        {
            potential = Math.Min(
                potential,
                context.Profile.OceanicCrustPotentialCeiling - separation);
        }

        map.CrustAssemblySeparation[triangleIndex] = ScaleUnit(separation);
        map.CrustBasePotential[triangleIndex] = ScaleUnit(potential);
        AssignBoundary(map, context.Profile, sites, triangleIndex, selection);
    }

    private static SamplePoint WarpedSample(
        PhysicalGeographyField map,
        PhysicalGeographyGenerationContext context,
        int triangleIndex)
    {
        var x = map.Grid.TriangleX(triangleIndex);
        var y = map.Grid.TriangleY(triangleIndex);
        var warpX = PhysicalNoise.FractalNoise(
            x,
            y,
            context.Seed ^ 0x16f1,
            context.Profile.ContinentalWarpScale,
            3) * context.Profile.ContinentalWarpStrength;
        var warpY = PhysicalNoise.FractalNoise(
            x,
            y,
            context.Seed ^ 0x62bd,
            context.Profile.ContinentalWarpScale,
            3) * context.Profile.ContinentalWarpStrength;
        return new SamplePoint(x + warpX, y + warpY);
    }

    private static SiteSelection SelectSites(
        ReadOnlySpan<ProvinceSite> sites,
        double sampleX,
        double sampleY)
    {
        var nearest = -1;
        var second = -1;
        var nearestDistance = double.MaxValue;
        var secondDistance = double.MaxValue;
        var continentalDistance = double.MaxValue;
        var oceanicDistance = double.MaxValue;
        for (var siteIndex = 0; siteIndex < sites.Length; siteIndex++)
        {
            var distance = Distance(sampleX, sampleY, sites[siteIndex].X, sites[siteIndex].Y);
            if (sites[siteIndex].Continental)
            {
                continentalDistance = Math.Min(continentalDistance, distance);
            }
            else
            {
                oceanicDistance = Math.Min(oceanicDistance, distance);
            }

            if (distance < nearestDistance)
            {
                second = nearest;
                secondDistance = nearestDistance;
                nearest = siteIndex;
                nearestDistance = distance;
            }
            else if (distance < secondDistance)
            {
                second = siteIndex;
                secondDistance = distance;
            }
        }

        return new SiteSelection(
            nearest,
            second,
            nearestDistance,
            secondDistance,
            continentalDistance,
            oceanicDistance);
    }

    private static void AssignBoundary(
        PhysicalGeographyField map,
        PhysicalGeographyGenerationProfile profile,
        ReadOnlySpan<ProvinceSite> sites,
        int triangleIndex,
        SiteSelection selection)
    {
        var boundaryAmount = Math.Clamp(
            1d - ((selection.SecondDistance - selection.NearestDistance) /
                Math.Max(0.001d, profile.RidgeWidth * 1.8d)),
            0d,
            1d);
        if (boundaryAmount <= 0d || selection.Second < 0)
        {
            map.TectonicBoundaryCodes[triangleIndex] = PhysicalTectonicBoundaryCodes.None;
            map.TectonicStrain[triangleIndex] = 0;
            return;
        }

        var firstSite = sites[selection.Nearest];
        var secondSite = sites[selection.Second];
        var dx = secondSite.X - firstSite.X;
        var dy = secondSite.Y - firstSite.Y;
        var length = Math.Max(0.001d, Math.Sqrt((dx * dx) + (dy * dy)));
        var relativeMotion = ((secondSite.MotionX - firstSite.MotionX) * dx / length) +
            ((secondSite.MotionY - firstSite.MotionY) * dy / length);
        var boundary = BoundaryCode(relativeMotion, profile.TectonicConvergenceThreshold);
        map.TectonicBoundaryCodes[triangleIndex] = boundary;
        map.TectonicStrain[triangleIndex] = ScaleUnit(
            boundaryAmount * Math.Clamp(0.48d + Math.Abs(relativeMotion), 0d, 1d));
    }

    private static double InterAssemblySeparation(
        ReadOnlySpan<ProvinceSite> sites,
        SiteSelection selection,
        PhysicalGeographyGenerationProfile profile)
    {
        if (selection.Second < 0 ||
            sites[selection.Nearest].AssemblyId == 0 ||
            sites[selection.Second].AssemblyId == 0 ||
            sites[selection.Nearest].AssemblyId == sites[selection.Second].AssemblyId)
        {
            return 0d;
        }

        var amount = Math.Clamp(
            1d - ((selection.SecondDistance - selection.NearestDistance) /
                Math.Max(0.001d, profile.RidgeWidth * 2.8d)),
            0d,
            1d);
        return amount * profile.ContinentalAssemblySeparationDepth;
    }

    private static int BoundaryCode(double relativeMotion, double threshold)
    {
        if (relativeMotion < -threshold)
        {
            return PhysicalTectonicBoundaryCodes.Convergent;
        }

        return relativeMotion > threshold
            ? PhysicalTectonicBoundaryCodes.Divergent
            : PhysicalTectonicBoundaryCodes.Transform;
    }

    private static int ScaleMotion(double value) =>
        (int)Math.Round(Math.Clamp(value, -1d, 1d) * 1000d, MidpointRounding.AwayFromZero);

    private static int ScaleUnit(double value) =>
        (int)Math.Round(Math.Clamp(value, 0d, 1d) * 1000d, MidpointRounding.AwayFromZero);

    private static double Distance(double ax, double ay, double bx, double by)
    {
        var dx = ax - bx;
        var dy = ay - by;
        return Math.Sqrt((dx * dx) + (dy * dy));
    }

    private static double DistanceSquared(SiteCandidate first, SiteCandidate second)
    {
        var dx = first.X - second.X;
        var dy = first.Y - second.Y;
        return (dx * dx) + (dy * dy);
    }

    private static double Lerp(double first, double second, double amount) =>
        first + ((second - first) * amount);

    private readonly record struct SiteCandidate(double X, double Y, double Priority);

    private readonly record struct SamplePoint(double X, double Y);

    private readonly record struct SiteSelection(
        int Nearest,
        int Second,
        double NearestDistance,
        double SecondDistance,
        double ContinentalDistance,
        double OceanicDistance);

    private readonly record struct ProvinceSite(
        double X,
        double Y,
        int AssemblyId,
        double MotionX,
        double MotionY)
    {
        public bool Continental => AssemblyId > 0;
    }

    private sealed class ProvinceMeasurements
    {
        private readonly HashSet<int> _provinces = [];
        private int _boundaries;
        private int _continental;
        private int _oceanic;

        public bool IsComplete =>
            _provinces.Count >= 4 && _boundaries > 0 && _continental > 0 && _oceanic > 0;

        public void Add(PhysicalGeographyField map, int triangleIndex)
        {
            _provinces.Add(map.CrustProvinceIds[triangleIndex]);
            if (map.CrustTypeCodes[triangleIndex] == PhysicalCrustTypeCodes.Continental &&
                map.CrustAssemblyIds[triangleIndex] <= 0)
            {
                throw new InvalidOperationException("Continental crust lacks an assembly identity.");
            }

            _continental += map.CrustTypeCodes[triangleIndex] == PhysicalCrustTypeCodes.Continental ? 1 : 0;
            _oceanic += map.CrustTypeCodes[triangleIndex] == PhysicalCrustTypeCodes.Oceanic ? 1 : 0;
            _boundaries += map.TectonicBoundaryCodes[triangleIndex] !=
                PhysicalTectonicBoundaryCodes.None ? 1 : 0;
        }
    }
}
