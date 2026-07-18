namespace IceRemembers.Seed;

using Engine.Constants;
using Engine.WorldGen;

using IceRemembers;

internal sealed class IceRemembersMagicField
{
    public IceRemembersMagicField(int triangleCount)
    {
        BaseSurfaceIndexes = new int[triangleCount];
        SurfaceIndexes = new int[triangleCount];
        SignificantFeatureFlags = new ulong[triangleCount];
        FireIntensity = new int[triangleCount];
        AuroraIntensity = new int[triangleCount];
        PressureIntensity = new int[triangleCount];
        PressureWake = new bool[triangleCount];
        RogueImpact = new bool[triangleCount];
    }

    public int[] BaseSurfaceIndexes { get; }

    public int[] SurfaceIndexes { get; }

    public ulong[] SignificantFeatureFlags { get; }

    public int[] FireIntensity { get; }

    public int[] AuroraIntensity { get; }

    public int[] PressureIntensity { get; }

    public bool[] PressureWake { get; }

    public bool[] RogueImpact { get; }

    public int DeadGodTriangleIndex { get; set; } = -1;
}

internal static class IceRemembersMagicWorldGenerator
{
    private const int MaximumFireCores = 180;

    public static IceRemembersMagicField Generate(
        PhysicalGeographyField field,
        int seed)
    {
        var magic = new IceRemembersMagicField(field.Grid.TriangleCount);
        ClassifyNaturalSurfaces(field, magic.BaseSurfaceIndexes);
        GenerateFire(field, magic, seed ^ 0x192d31);
        GenerateAurora(field, magic);
        GeneratePressure(field, magic, seed ^ 0x5a61b9);
        ClassifyMagicSurfaces(field, magic);
        return magic;
    }

    private static void ClassifyNaturalSurfaces(PhysicalGeographyField field, Span<int> surfaces)
    {
        for (var triangleIndex = 0; triangleIndex < field.Grid.TriangleCount; triangleIndex++)
        {
            surfaces[triangleIndex] = (int)NaturalSurface(field, triangleIndex);
        }
    }

    private static IceRemembersPhysicalSurface NaturalSurface(
        PhysicalGeographyField field,
        int triangleIndex)
    {
        if (field.WaterKindCodes[triangleIndex] == PhysicalWaterKindCodes.Ocean)
        {
            return NaturalOceanSurface(field, triangleIndex);
        }

        return NaturalLandSurface(field, triangleIndex);
    }

    private static IceRemembersPhysicalSurface NaturalOceanSurface(
        PhysicalGeographyField field,
        int triangleIndex)
    {
        var special = NaturalOceanSpecialSurface(field, triangleIndex);
        if (special.HasValue)
        {
            return special.Value;
        }

        var shallow = field.BathymetricZoneCodes[triangleIndex] is
            PhysicalBathymetricZoneCodes.Shelf or PhysicalBathymetricZoneCodes.ShelfBreak;
        if (field.IceThickness[triangleIndex] >= 50)
        {
            return IceRemembersPhysicalSurface.PackIce;
        }

        if (field.Temperature[triangleIndex] <= 315)
        {
            return IceRemembersPhysicalSurface.SeaIce;
        }

        if (HasFrozenOceanNeighbor(field, triangleIndex))
        {
            return IceRemembersPhysicalSurface.Polynya;
        }

        return shallow
            ? IceRemembersPhysicalSurface.Shelf
            : IceRemembersPhysicalSurface.Ocean;
    }

    private static IceRemembersPhysicalSurface? NaturalOceanSpecialSurface(
        PhysicalGeographyField field,
        int triangleIndex)
    {
        if (field.LandformCodes[triangleIndex] == PhysicalLandformCodes.Fjord)
        {
            return IceRemembersPhysicalSurface.Fjord;
        }

        var shallow = field.BathymetricZoneCodes[triangleIndex] is
            PhysicalBathymetricZoneCodes.Shelf or PhysicalBathymetricZoneCodes.ShelfBreak;
        if (shallow &&
            field.WaterDepths[triangleIndex] <= 125 &&
            field.Temperature[triangleIndex] >= 300 &&
            field.Geothermal[triangleIndex] >= 150)
        {
            return IceRemembersPhysicalSurface.Reef;
        }

        return shallow && field.IceThickness[triangleIndex] >= 75
            ? IceRemembersPhysicalSurface.IceShelf
            : null;
    }

    private static bool HasFrozenOceanNeighbor(PhysicalGeographyField field, int triangleIndex)
    {
        for (var edge = 0; edge < 3; edge++)
        {
            var neighbor = field.Grid.NeighborTriangleIndex(triangleIndex, edge);
            if (neighbor >= 0 &&
                field.WaterKindCodes[neighbor] == PhysicalWaterKindCodes.Ocean &&
                field.Temperature[neighbor] <= 315)
            {
                return true;
            }
        }

        return false;
    }

    private static IceRemembersPhysicalSurface NaturalLandSurface(
        PhysicalGeographyField field,
        int triangleIndex)
    {
        if (field.WaterKindCodes[triangleIndex] == PhysicalWaterKindCodes.Lake)
        {
            return IceRemembersPhysicalSurface.Lake;
        }

        if (field.BathymetricZoneCodes[triangleIndex] == PhysicalBathymetricZoneCodes.ExposedShelf)
        {
            return IceRemembersPhysicalSurface.ExposedShelf;
        }

        if (field.LandformCodes[triangleIndex] == PhysicalLandformCodes.Moraine)
        {
            return IceRemembersPhysicalSurface.Moraine;
        }

        var frozen = FrozenLandSurface(field.IceThickness[triangleIndex]);
        if (frozen.HasValue)
        {
            return frozen.Value;
        }

        if (field.LandformCodes[triangleIndex] is
            PhysicalLandformCodes.Ridge or PhysicalLandformCodes.Peak or PhysicalLandformCodes.Pass)
        {
            return IceRemembersPhysicalSurface.Ridge;
        }

        return field.Temperature[triangleIndex] >= 335 && field.Moisture[triangleIndex] >= 260
            ? IceRemembersPhysicalSurface.OpenField
            : IceRemembersPhysicalSurface.Tundra;
    }

    private static IceRemembersPhysicalSurface? FrozenLandSurface(int iceThickness)
    {
        if (iceThickness >= 300)
        {
            return IceRemembersPhysicalSurface.Glacier;
        }

        return iceThickness >= 250
            ? IceRemembersPhysicalSurface.Snowfield
            : null;
    }

    private static void GenerateFire(
        PhysicalGeographyField field,
        IceRemembersMagicField magic,
        int seed)
    {
        var cores = SelectFireCores(field, seed);
        var queue = new PriorityQueue<int, (int NegativeIntensity, int Index)>();
        SeedFireCores(field, magic, cores, queue);
        PropagateFire(field, magic, queue);
        ApplyFireClimate(field, magic);
    }

    private static void SeedFireCores(
        PhysicalGeographyField field,
        IceRemembersMagicField magic,
        ReadOnlySpan<int> cores,
        PriorityQueue<int, (int NegativeIntensity, int Index)> queue)
    {
        foreach (var core in cores)
        {
            var strength = Math.Clamp(720 + (field.HeatSourceDensity[core] / 3), 0, 1000);
            magic.FireIntensity[core] = strength;
            field.FeatureFlags[core] |= PhysicalGeographyFeatureCodes.FireCore;
            if (strength >= 940)
            {
                field.FeatureFlags[core] |= PhysicalGeographyFeatureCodes.MagmaChamber;
            }

            queue.Enqueue(core, (-strength, core));
        }
    }

    private static void PropagateFire(
        PhysicalGeographyField field,
        IceRemembersMagicField magic,
        PriorityQueue<int, (int NegativeIntensity, int Index)> queue)
    {
        while (queue.TryDequeue(out var triangleIndex, out var priority))
        {
            var intensity = -priority.NegativeIntensity;
            if (intensity != magic.FireIntensity[triangleIndex])
            {
                continue;
            }

            for (var edge = 0; edge < 3; edge++)
            {
                var neighbor = field.Grid.NeighborTriangleIndex(triangleIndex, edge);
                if (neighbor < 0)
                {
                    continue;
                }

                var conductance = (field.FractureConductance[triangleIndex] +
                    field.FractureConductance[neighbor]) / 2;
                var cost = 17 + ((1000 - conductance) / 30);
                var candidate = intensity - cost;
                if (candidate > magic.FireIntensity[neighbor])
                {
                    magic.FireIntensity[neighbor] = candidate;
                    queue.Enqueue(neighbor, (-candidate, neighbor));
                }
            }
        }
    }

    private static void ApplyFireClimate(
        PhysicalGeographyField field,
        IceRemembersMagicField magic)
    {
        for (var triangleIndex = 0; triangleIndex < field.Grid.TriangleCount; triangleIndex++)
        {
            var fire = magic.FireIntensity[triangleIndex];
            if (fire <= 0)
            {
                continue;
            }

            field.Temperature[triangleIndex] = Math.Clamp(
                field.Temperature[triangleIndex] + (fire * 36 / 100),
                0,
                1000);
            field.IceThickness[triangleIndex] = Math.Max(0, field.IceThickness[triangleIndex] - (fire / 2));
        }
    }

    private static int[] SelectFireCores(PhysicalGeographyField field, int seed)
    {
        var candidates = new List<CoreCandidate>();
        for (var triangleIndex = 0; triangleIndex < field.Grid.TriangleCount; triangleIndex++)
        {
            var density = field.HeatSourceDensity[triangleIndex];
            if (density < 590 || !IsLocalMaximum(field, triangleIndex, field.HeatSourceDensity))
            {
                continue;
            }

            var score = density * 1000L +
                (long)(PhysicalNoise.HashUnit(seed, triangleIndex, 0) * 999d);
            candidates.Add(new CoreCandidate(triangleIndex, score));
        }

        candidates.Sort(static (first, second) => second.Score.CompareTo(first.Score));
        var selected = new List<int>(Math.Min(MaximumFireCores, candidates.Count));
        foreach (var candidate in candidates)
        {
            if (selected.Count >= MaximumFireCores)
            {
                break;
            }

            var radius = 4.5d + ((1000 - field.HeatSourceDensity[candidate.TriangleIndex]) / 150d);
            if (FarEnoughFromSelected(field.Grid, candidate.TriangleIndex, selected, radius))
            {
                selected.Add(candidate.TriangleIndex);
            }
        }

        if (selected.Count == 0)
        {
            selected.Add(DeepestHeatSource(field));
        }

        return selected.ToArray();
    }

    private static bool IsLocalMaximum(PhysicalGeographyField field, int triangleIndex, Span<int> values)
    {
        for (var edge = 0; edge < 3; edge++)
        {
            var neighbor = field.Grid.NeighborTriangleIndex(triangleIndex, edge);
            if (neighbor >= 0 && values[neighbor] > values[triangleIndex])
            {
                return false;
            }
        }

        return true;
    }

    private static bool FarEnoughFromSelected(
        PhysicalGeographyGrid grid,
        int candidate,
        List<int> selected,
        double radius)
    {
        var x = grid.TriangleX(candidate);
        var y = grid.TriangleY(candidate);
        var radiusSquared = radius * radius;
        foreach (var existing in selected)
        {
            var dx = x - grid.TriangleX(existing);
            var dy = y - grid.TriangleY(existing);
            if ((dx * dx) + (dy * dy) < radiusSquared)
            {
                return false;
            }
        }

        return true;
    }

    private static int DeepestHeatSource(PhysicalGeographyField field)
    {
        var best = 0;
        for (var triangleIndex = 1; triangleIndex < field.Grid.TriangleCount; triangleIndex++)
        {
            if (field.HeatSourceDensity[triangleIndex] > field.HeatSourceDensity[best])
            {
                best = triangleIndex;
            }
        }

        return best;
    }

    private static void GenerateAurora(PhysicalGeographyField field, IceRemembersMagicField magic)
    {
        var accumulation = new int[field.Grid.TriangleCount];
        var targets = new int[field.Grid.TriangleCount];
        for (var triangleIndex = 0; triangleIndex < field.Grid.TriangleCount; triangleIndex++)
        {
            accumulation[triangleIndex] = Math.Max(
                0,
                360 - field.Temperature[triangleIndex] +
                    (Math.Max(0, field.Elevation[triangleIndex]) / 10));
            targets[triangleIndex] = ColdestNeighbor(field, triangleIndex);
        }

        var order = ScalarOrder(field.Temperature);
        for (var orderIndex = order.Length - 1; orderIndex >= 0; orderIndex--)
        {
            var triangleIndex = order[orderIndex];
            var capacity = Math.Clamp(field.IceThickness[triangleIndex], 0, 900);
            var retained = accumulation[triangleIndex] * capacity / 1200;
            magic.AuroraIntensity[triangleIndex] = Math.Clamp(
                (int)Math.Round(
                    Math.Log2(accumulation[triangleIndex] + 1d) * 66d,
                    MidpointRounding.ToEven) +
                    (capacity / 4),
                0,
                1000);
            var target = targets[triangleIndex];
            if (target >= 0)
            {
                accumulation[target] = SaturatingAdd(
                    accumulation[target],
                    Math.Max(0, accumulation[triangleIndex] - retained));
            }
        }
    }

    private static int ColdestNeighbor(PhysicalGeographyField field, int triangleIndex)
    {
        var target = -1;
        var targetTemperature = field.Temperature[triangleIndex];
        for (var edge = 0; edge < 3; edge++)
        {
            var neighbor = field.Grid.NeighborTriangleIndex(triangleIndex, edge);
            if (neighbor >= 0 &&
                (field.Temperature[neighbor] < targetTemperature ||
                    (field.Temperature[neighbor] == targetTemperature && neighbor < triangleIndex &&
                        (target < 0 || neighbor < target))))
            {
                target = neighbor;
                targetTemperature = field.Temperature[neighbor];
            }
        }

        return target;
    }

    private static void GeneratePressure(
        PhysicalGeographyField field,
        IceRemembersMagicField magic,
        int seed)
    {
        var locus = SelectDeadGodLocus(field);
        magic.DeadGodTriangleIndex = locus;
        field.FeatureFlags[locus] |= PhysicalGeographyFeatureCodes.DeadGodLocus;
        const int standingRays = 720;
        for (var ray = 0; ray < standingRays; ray++)
        {
            var angle = ray / (double)standingRays * Math.PI * 2d;
            PropagatePressureRay(field, magic.PressureIntensity, locus, angle, 1000, 440, seed ^ ray);
        }

        var coastSources = SelectOceanSources(field, seed, 18, requireCoast: true);
        for (var sourceIndex = 0; sourceIndex < coastSources.Length; sourceIndex++)
        {
            var source = coastSources[sourceIndex];
            magic.RogueImpact[source] = true;
            for (var ray = 0; ray < 24; ray++)
            {
                var angle = ray / 24d * Math.PI * 2d;
                PropagatePressureRay(
                    field,
                    magic.PressureIntensity,
                    source,
                    angle,
                    620,
                    130,
                    seed ^ (sourceIndex * 397) ^ ray);
            }
        }

        var wakeSources = SelectOceanSources(field, seed ^ 0x3359, 20, requireCoast: false);
        for (var wakeIndex = 0; wakeIndex < wakeSources.Length; wakeIndex++)
        {
            TraceAncientWake(field, magic, wakeSources[wakeIndex], seed ^ (wakeIndex * 7919));
        }
    }

    private static int SelectDeadGodLocus(PhysicalGeographyField field)
    {
        var best = -1;
        var bestScore = int.MinValue;
        for (var configuredIndex = 0; configuredIndex < field.Grid.ConfiguredHexCount; configuredIndex++)
        {
            var hexIndex = field.Grid.ConfiguredHexIndexAt(configuredIndex);
            for (var local = 0; local < PhysicalGeographyCodes.MicrotrianglesPerHex; local++)
            {
                var triangleIndex = field.Grid.TriangleIndex(hexIndex, local);
                if (field.WaterKindCodes[triangleIndex] != PhysicalWaterKindCodes.Ocean)
                {
                    continue;
                }

                var score = field.WaterDepths[triangleIndex] + LocalBasinDepth(field, triangleIndex);
                if (score > bestScore)
                {
                    best = triangleIndex;
                    bestScore = score;
                }
            }
        }

        return best >= 0 ? best : throw new InvalidOperationException("Pressure magic requires a deep ocean locus.");
    }

    private static int LocalBasinDepth(PhysicalGeographyField field, int triangleIndex)
    {
        var depth = field.WaterDepths[triangleIndex];
        var total = 0;
        for (var edge = 0; edge < 3; edge++)
        {
            var neighbor = field.Grid.NeighborTriangleIndex(triangleIndex, edge);
            if (neighbor >= 0)
            {
                total += Math.Max(0, depth - field.WaterDepths[neighbor]);
            }
        }

        return total;
    }

    private static void PropagatePressureRay(
        PhysicalGeographyField field,
        int[] pressure,
        int source,
        double angle,
        int energy,
        int maximumSteps,
        int seed)
    {
        var current = source;
        var previous = -1;
        var bounces = 0;
        for (var step = 0; step < maximumSteps && energy > 0; step++)
        {
            pressure[current] = Math.Min(1000, pressure[current] + Math.Max(1, energy / 36));
            var next = BestOceanNeighbor(field, current, previous, angle);
            if (next < 0)
            {
                bounces++;
                if (bounces > 4)
                {
                    break;
                }

                angle += Math.PI * (0.58d +
                    (PhysicalNoise.HashUnit(seed, current, bounces) * 0.62d));
                previous = -1;
                energy -= 90;
                continue;
            }

            var dx = field.Grid.TriangleX(next) - field.Grid.TriangleX(current);
            var dy = field.Grid.TriangleY(next) - field.Grid.TriangleY(current);
            angle = Math.Atan2(dy, dx);
            previous = current;
            current = next;
            energy -= 3 + (bounces * 2);
        }
    }

    private static int BestOceanNeighbor(
        PhysicalGeographyField field,
        int current,
        int previous,
        double angle)
    {
        var directionX = Math.Cos(angle);
        var directionY = Math.Sin(angle);
        var best = -1;
        var bestAlignment = -2d;
        for (var edge = 0; edge < 3; edge++)
        {
            var neighbor = field.Grid.NeighborTriangleIndex(current, edge);
            if (neighbor < 0 || neighbor == previous ||
                field.WaterKindCodes[neighbor] != PhysicalWaterKindCodes.Ocean)
            {
                continue;
            }

            var dx = field.Grid.TriangleX(neighbor) - field.Grid.TriangleX(current);
            var dy = field.Grid.TriangleY(neighbor) - field.Grid.TriangleY(current);
            var length = Math.Sqrt((dx * dx) + (dy * dy));
            var alignment = length <= double.Epsilon ? -2d : ((dx / length) * directionX) + ((dy / length) * directionY);
            if (alignment > bestAlignment)
            {
                best = neighbor;
                bestAlignment = alignment;
            }
        }

        return bestAlignment >= -0.15d ? best : -1;
    }

    private static int[] SelectOceanSources(
        PhysicalGeographyField field,
        int seed,
        int count,
        bool requireCoast)
    {
        var candidates = new PriorityQueue<CoreCandidate, (long Score, int TriangleIndex)>();
        for (var triangleIndex = 0; triangleIndex < field.Grid.TriangleCount; triangleIndex++)
        {
            if (!field.Grid.TriangleIsInsideConfiguredDisk(triangleIndex) ||
                field.WaterKindCodes[triangleIndex] != PhysicalWaterKindCodes.Ocean ||
                (requireCoast && !HasDryNeighbor(field, triangleIndex)))
            {
                continue;
            }

            var candidate = new CoreCandidate(
                triangleIndex,
                (long)(PhysicalNoise.HashUnit(seed, triangleIndex, count) * long.MaxValue));
            AddBoundedCandidate(candidates, candidate, count);
        }

        var selected = new CoreCandidate[candidates.Count];
        for (var index = 0; index < selected.Length; index++)
        {
            selected[index] = candidates.Dequeue();
        }

        Array.Sort(selected, static (first, second) => second.Score.CompareTo(first.Score));
        return selected.Select(static candidate => candidate.TriangleIndex).ToArray();
    }

    private static void AddBoundedCandidate(
        PriorityQueue<CoreCandidate, (long Score, int TriangleIndex)> candidates,
        CoreCandidate candidate,
        int capacity)
    {
        var priority = (candidate.Score, candidate.TriangleIndex);
        if (candidates.Count < capacity)
        {
            candidates.Enqueue(candidate, priority);
            return;
        }

        _ = candidates.TryPeek(out _, out var weakest);
        if (priority.CompareTo(weakest) <= 0)
        {
            return;
        }

        _ = candidates.Dequeue();
        candidates.Enqueue(candidate, priority);
    }

    private static bool HasDryNeighbor(PhysicalGeographyField field, int triangleIndex)
    {
        for (var edge = 0; edge < 3; edge++)
        {
            var neighbor = field.Grid.NeighborTriangleIndex(triangleIndex, edge);
            if (neighbor >= 0 && field.WaterKindCodes[neighbor] != PhysicalWaterKindCodes.Ocean)
            {
                return true;
            }
        }

        return false;
    }

    private static void TraceAncientWake(
        PhysicalGeographyField field,
        IceRemembersMagicField magic,
        int source,
        int seed)
    {
        var angle = PhysicalNoise.HashUnit(seed, source, 0) * Math.PI * 2d;
        var current = source;
        var previous = -1;
        for (var step = 0; step < 190; step++)
        {
            magic.PressureWake[current] = true;
            magic.PressureIntensity[current] = Math.Min(1000, magic.PressureIntensity[current] + 180);
            var next = BestOceanNeighbor(field, current, previous, angle);
            if (next < 0)
            {
                break;
            }

            angle += PhysicalNoise.HashSigned(seed, step, current) * 0.16d;
            previous = current;
            current = next;
        }
    }

    private static void ClassifyMagicSurfaces(
        PhysicalGeographyField field,
        IceRemembersMagicField magic)
    {
        for (var triangleIndex = 0; triangleIndex < field.Grid.TriangleCount; triangleIndex++)
        {
            var flags = ClassifyFeatureFlags(field, magic, triangleIndex);
            magic.SignificantFeatureFlags[triangleIndex] = flags;
            magic.SurfaceIndexes[triangleIndex] = (int)PrimarySurface(
                magic,
                triangleIndex,
                flags);
        }
    }

    private static ulong ClassifyFeatureFlags(
        PhysicalGeographyField field,
        IceRemembersMagicField magic,
        int triangleIndex)
    {
        return ClassifyFireFeatureFlags(field, magic, triangleIndex) |
            ClassifyAuroraFeatureFlags(field, magic, triangleIndex) |
            ClassifyPressureFeatureFlags(field, magic, triangleIndex) |
            ClassifyInteractionFeatureFlags(field, magic, triangleIndex);
    }

    private static ulong ClassifyFireFeatureFlags(
        PhysicalGeographyField field,
        IceRemembersMagicField magic,
        int triangleIndex)
    {
        var flags = 0UL;
        var fire = magic.FireIntensity[triangleIndex];
        var core = (field.FeatureFlags[triangleIndex] & PhysicalGeographyFeatureCodes.FireCore) != 0;
        Add(ref flags, core, IceRemembersPhysicalFeature.FirecoreVent);
        Add(ref flags, fire >= 420 && field.FractureConductance[triangleIndex] >= 560,
            IceRemembersPhysicalFeature.ThermalVein);
        Add(ref flags, IsHotSpring(field, triangleIndex, fire),
            IceRemembersPhysicalFeature.HotSpring);
        Add(ref flags, fire >= 560 && field.Slope[triangleIndex] <= 45,
            IceRemembersPhysicalFeature.BasaltFlat);
        Add(ref flags, fire is >= 220 and < 560 && field.Temperature[triangleIndex] >= 300,
            IceRemembersPhysicalFeature.ThawedHalo);
        return flags;
    }

    private static bool IsHotSpring(PhysicalGeographyField field, int triangleIndex, int fire)
    {
        var wetFireSpring = fire >= 430 &&
            (field.WaterKindCodes[triangleIndex] == PhysicalWaterKindCodes.Lake ||
                field.RiverOrders[triangleIndex] > 0 || field.Moisture[triangleIndex] >= 650);
        var verdantGeothermalSpring = field.Geothermal[triangleIndex] >= 720 &&
            field.Temperature[triangleIndex] >= 335 &&
            field.Moisture[triangleIndex] >= 260;
        return wetFireSpring || verdantGeothermalSpring;
    }

    private static ulong ClassifyAuroraFeatureFlags(
        PhysicalGeographyField field,
        IceRemembersMagicField magic,
        int triangleIndex)
    {
        var flags = 0UL;
        var aurora = magic.AuroraIntensity[triangleIndex];
        Add(ref flags, aurora >= 470 && field.IceThickness[triangleIndex] >= 260,
            IceRemembersPhysicalFeature.AuroraField);
        Add(ref flags, aurora >= 680 && field.LandformCodes[triangleIndex] is
            PhysicalLandformCodes.Cirque or PhysicalLandformCodes.Basin,
            IceRemembersPhysicalFeature.LightWell);
        Add(ref flags, aurora >= 510 && field.LandformCodes[triangleIndex] is
            PhysicalLandformCodes.Peak or PhysicalLandformCodes.Ridge,
            IceRemembersPhysicalFeature.FrostPrism);
        Add(ref flags, aurora >= 650 &&
            field.IceThickness[triangleIndex] is >= 80 and <= 360 &&
            field.Temperature[triangleIndex] is >= 275 and <= 380,
            IceRemembersPhysicalFeature.MeltLine);
        return flags;
    }

    private static ulong ClassifyPressureFeatureFlags(
        PhysicalGeographyField field,
        IceRemembersMagicField magic,
        int triangleIndex)
    {
        var flags = 0UL;
        var pressure = magic.PressureIntensity[triangleIndex];
        Add(ref flags, triangleIndex == magic.DeadGodTriangleIndex,
            IceRemembersPhysicalFeature.PressureGodLocus);
        Add(ref flags, pressure >= 690, IceRemembersPhysicalFeature.CrushZone);
        Add(ref flags, magic.PressureWake[triangleIndex], IceRemembersPhysicalFeature.WakeScar);
        Add(ref flags, pressure >= 380 &&
            PhysicalNoise.UnitNoise(
                field.Grid.TriangleX(triangleIndex),
                field.Grid.TriangleY(triangleIndex),
                0x621d,
                7d,
                1) >= 0.54d,
            IceRemembersPhysicalFeature.InterferenceLattice);
        Add(ref flags, magic.RogueImpact[triangleIndex], IceRemembersPhysicalFeature.RogueImpact);
        return flags;
    }

    private static ulong ClassifyInteractionFeatureFlags(
        PhysicalGeographyField field,
        IceRemembersMagicField magic,
        int triangleIndex)
    {
        var flags = 0UL;
        var fire = magic.FireIntensity[triangleIndex];
        var aurora = magic.AuroraIntensity[triangleIndex];
        var pressure = magic.PressureIntensity[triangleIndex];
        Add(ref flags, fire >= 260 && aurora >= 340,
            IceRemembersPhysicalFeature.ThermalWarRim);
        Add(ref flags, fire >= 390 && pressure >= 420 && field.FractureConductance[triangleIndex] >= 520,
            IceRemembersPhysicalFeature.SteamFracture);
        Add(ref flags, pressure >= 390 && aurora >= 430 &&
            field.WaterKindCodes[triangleIndex] == PhysicalWaterKindCodes.Ocean &&
            field.IceThickness[triangleIndex] >= 220,
            IceRemembersPhysicalFeature.PressureAuroraBreach);
        return flags;
    }

    private static IceRemembersPhysicalSurface PrimarySurface(
        IceRemembersMagicField magic,
        int triangleIndex,
        ulong flags)
    {
        if (Has(flags, IceRemembersPhysicalFeature.PressureAuroraBreach))
        {
            return IceRemembersPhysicalSurface.PressureAuroraBreach;
        }

        if (Has(flags, IceRemembersPhysicalFeature.SteamFracture))
        {
            return IceRemembersPhysicalSurface.SteamFracture;
        }

        if (Has(flags, IceRemembersPhysicalFeature.ThermalWarRim))
        {
            return IceRemembersPhysicalSurface.ThermalWarRim;
        }

        ReadOnlySpan<(IceRemembersPhysicalFeature Feature, IceRemembersPhysicalSurface Surface)> precedence =
        [
            (IceRemembersPhysicalFeature.PressureGodLocus, IceRemembersPhysicalSurface.PressureGodLocus),
            (IceRemembersPhysicalFeature.FirecoreVent, IceRemembersPhysicalSurface.FirecoreVent),
            (IceRemembersPhysicalFeature.LightWell, IceRemembersPhysicalSurface.LightWell),
            (IceRemembersPhysicalFeature.HotSpring, IceRemembersPhysicalSurface.HotSpring),
            (IceRemembersPhysicalFeature.CrushZone, IceRemembersPhysicalSurface.CrushZone),
            (IceRemembersPhysicalFeature.WakeScar, IceRemembersPhysicalSurface.WakeScar),
            (IceRemembersPhysicalFeature.RogueImpact, IceRemembersPhysicalSurface.RogueImpact),
            (IceRemembersPhysicalFeature.FrostPrism, IceRemembersPhysicalSurface.FrostPrism),
            (IceRemembersPhysicalFeature.AuroraField, IceRemembersPhysicalSurface.AuroraField),
            (IceRemembersPhysicalFeature.InterferenceLattice, IceRemembersPhysicalSurface.InterferenceLattice),
            (IceRemembersPhysicalFeature.ThermalVein, IceRemembersPhysicalSurface.ThermalVein),
            (IceRemembersPhysicalFeature.BasaltFlat, IceRemembersPhysicalSurface.BasaltFlat),
            (IceRemembersPhysicalFeature.MeltLine, IceRemembersPhysicalSurface.MeltLine),
            (IceRemembersPhysicalFeature.ThawedHalo, IceRemembersPhysicalSurface.ThawedHalo),
        ];
        foreach (var candidate in precedence)
        {
            if (Has(flags, candidate.Feature))
            {
                return candidate.Surface;
            }
        }

        return (IceRemembersPhysicalSurface)magic.BaseSurfaceIndexes[triangleIndex];
    }

    private static void Add(ref ulong flags, bool condition, IceRemembersPhysicalFeature feature)
    {
        if (condition)
        {
            flags |= IceRemembersPhysicalGeography.FeatureFlag(feature);
        }
    }

    private static bool Has(ulong flags, IceRemembersPhysicalFeature feature) =>
        (flags & IceRemembersPhysicalGeography.FeatureFlag(feature)) != 0;

    private static int[] ScalarOrder(Span<int> values)
    {
        var counts = new int[1001];
        foreach (var value in values)
        {
            counts[Math.Clamp(value, 0, 1000)]++;
        }

        var offset = 0;
        for (var index = 0; index < counts.Length; index++)
        {
            var count = counts[index];
            counts[index] = offset;
            offset += count;
        }

        var order = new int[values.Length];
        for (var index = 0; index < values.Length; index++)
        {
            var bucket = Math.Clamp(values[index], 0, 1000);
            order[counts[bucket]++] = index;
        }

        return order;
    }

    private static int SaturatingAdd(int first, int second) =>
        first > int.MaxValue - second ? int.MaxValue : first + second;

    private readonly record struct CoreCandidate(int TriangleIndex, long Score);
}
