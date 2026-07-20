// Map-generation regression diagnostics (see docs/analysis/map-generation-
// review-20260719.md). Console reporting tool; style rules relaxed on purpose.
#pragma warning disable S1541, S3776, MA0051, S3358, MA0193, S104, S1200, MA0048

namespace WorldGeneratorTool;

using Engine.Constants;
using Engine.WorldGen;

internal static partial class WorldDiagnosticsCli
{
    /// <summary>
    /// The M4.0 acceptance criterion: coasts must be tellable apart by margin
    /// type. Reports how much coastline each margin owns and how the shelf
    /// differs between them — if shelf width does not separate, the margin
    /// fact is persisted but not consumed.
    /// </summary>
    private static void ReportMargins(PhysicalGeographyField field)
    {
        Console.WriteLine("== Tectonic margins ==");
        string[] names = ["interior", "passive", "convergent", "rifted", "transform"];
        var all = new long[names.Length];
        var coastal = new long[names.Length];
        var shelfTriangles = new long[names.Length];
        var landTriangles = new long[names.Length];
        for (var t = 0; t < field.Grid.TriangleCount; t++)
        {
            if (!field.Grid.TriangleIsInsideConfiguredDisk(t))
            {
                continue;
            }

            var margin = field.MarginCodes[t];
            if (margin < 0 || margin >= names.Length)
            {
                continue;
            }

            all[margin]++;
            var isLand = field.WaterKindCodes[t] == PhysicalWaterKindCodes.None;
            if (isLand)
            {
                landTriangles[margin]++;
                if (HasOceanNeighbor(field, t))
                {
                    coastal[margin]++;
                }
            }
            else if (field.BathymetricZoneCodes[t] is PhysicalBathymetricZoneCodes.Shelf
                or PhysicalBathymetricZoneCodes.ShelfBreak)
            {
                shelfTriangles[margin]++;
            }
        }

        for (var margin = 0; margin < names.Length; margin++)
        {
            if (all[margin] == 0)
            {
                continue;
            }

            var shelfPerCoast = coastal[margin] == 0
                ? 0d
                : shelfTriangles[margin] / (double)coastal[margin];

            // Coastline length against the square root of enclosed land area:
            // a smooth blob sits near 4, and the value climbs as the shoreline
            // becomes crenulated. This is the number that says whether coasts
            // are ragged, and reporting it per margin says whether they are
            // ragged in *different ways*.
            var crenulation = landTriangles[margin] <= 0
                ? 0d
                : coastal[margin] / Math.Sqrt(landTriangles[margin]);
            Console.WriteLine(
                $"  {names[margin],-11} triangles={all[margin],8} land={landTriangles[margin],8} " +
                $"coastline={coastal[margin],6} shelf/coast={shelfPerCoast,6:0.00} " +
                $"crenulation={crenulation,6:0.00}");
        }

        ReportCoastalForms(field);
    }

    /// <summary>
    /// Whether plate drift actually mixed history into new settings: ocean-born
    /// pressure now standing on land, and volcanic construction spread across
    /// epochs as an age-progressive hotspot track.
    /// </summary>
    internal static void ReportPlateHistory(
        PhysicalGeographyField field,
        IceRemembers.Seed.IceRemembersMicrogeographyMap magic)
    {
        Console.WriteLine("\n== Plate drift mixing ==");
        long landWithPressureHistory = 0;
        long landTotal = 0;
        long freshCrust = 0;
        var volcanicByEpoch = new Dictionary<int, int>();
        for (var t = 0; t < field.Grid.TriangleCount; t++)
        {
            if (!field.Grid.TriangleIsInsideConfiguredDisk(t))
            {
                continue;
            }

            if (field.AdvectionSourceIndex[t] < 0)
            {
                freshCrust++;
            }

            if (field.VolcanicConstructionHistory[t] > 0)
            {
                var epoch = field.VolcanicLastActiveEpoch[t];
                volcanicByEpoch[epoch] = volcanicByEpoch.GetValueOrDefault(epoch) + 1;
            }

            if (field.WaterKindCodes[t] != PhysicalWaterKindCodes.None)
            {
                continue;
            }

            landTotal++;
            if (magic.PressureHistory[t] > 0 || magic.PressureCoastalHistory[t] > 0)
            {
                landWithPressureHistory++;
            }
        }

        Console.WriteLine(
            $"  land carrying ocean-born pressure history: {landWithPressureHistory} of {landTotal} " +
            $"({landWithPressureHistory * 100d / Math.Max(1, landTotal):0.0}%)");
        Console.WriteLine($"  crust created fresh at rifts this epoch: {freshCrust}");
        Console.Write("  volcanic construction by last-active epoch:");
        foreach (var (epoch, count) in volcanicByEpoch.OrderBy(pair => pair.Key))
        {
            Console.Write($" e{epoch}:{count}");
        }

        Console.WriteLine();
    }

    internal static void ReportMagicPrevalence(
        PhysicalGeographyField field,
        IceRemembers.Seed.IceRemembersMicrogeographyMap magic)
    {
        // The whole disk is the wrong denominator for "will a player ever see
        // this": early play only reaches a fraction of it, and magic is
        // clustered rather than spread, so a healthy global share can still
        // mean zero encounters in the reachable region.
        Console.WriteLine("\n== Magic prevalence (whole disk) ==");
        ReportMagicPrevalenceWithin(field, magic, int.MaxValue);
        Console.WriteLine("\n== Magic prevalence (early-play radius 40) ==");
        ReportMagicPrevalenceWithin(field, magic, 40);
    }

    private static void ReportMagicPrevalenceWithin(
        PhysicalGeographyField field,
        IceRemembers.Seed.IceRemembersMicrogeographyMap magic,
        int maximumHexDistance)
    {
        string[] fireSurfaces =
            ["firecore_vent", "thermal_vein", "hot_spring", "basalt_flat", "thawed_halo", "steam_fracture"];
        string[] auroraSurfaces = ["aurora_field", "light_well", "frost_prism", "melt_line"];
        string[] pressureSurfaces =
        [
            "pressure_god_locus", "crush_zone", "wake_scar", "interference_lattice",
            "rogue_impact", "thermal_war_rim", "pressure_aurora_breach",
        ];
        var palette = IceRemembers.IceRemembersPhysicalGeography.SurfacePalette;

        long total = 0, land = 0, ocean = 0;
        long fireAny = 0, fireStrong = 0, pressureAny = 0, pressureStrong = 0, auroraAny = 0;
        long fireSurface = 0, auroraSurface = 0, pressureSurface = 0;
        var perSurface = new Dictionary<string, long>(StringComparer.Ordinal);
        for (var t = 0; t < field.Grid.TriangleCount; t++)
        {
            if (!field.Grid.TriangleIsInsideConfiguredDisk(t) ||
                field.Grid.HexDistanceAt(field.Grid.TriangleHexIndex(t)) > maximumHexDistance)
            {
                continue;
            }

            total++;
            if (field.WaterKindCodes[t] == PhysicalWaterKindCodes.None)
            {
                land++;
            }
            else if (field.WaterKindCodes[t] == PhysicalWaterKindCodes.Ocean)
            {
                ocean++;
            }

            fireAny += magic.FireIntensity[t] > 0 ? 1 : 0;
            fireStrong += magic.FireIntensity[t] >= 310 ? 1 : 0;
            auroraAny += magic.AuroraIntensity[t] > 0 ? 1 : 0;
            pressureAny += magic.PressureIntensity[t] > 0 ? 1 : 0;
            pressureStrong += magic.PressureIntensity[t] >= 310 ? 1 : 0;

            var name = palette[magic.SurfaceIndexes[t]];
            if (Array.IndexOf(fireSurfaces, name) >= 0) { fireSurface++; }
            else if (Array.IndexOf(auroraSurfaces, name) >= 0) { auroraSurface++; }
            else if (Array.IndexOf(pressureSurfaces, name) >= 0) { pressureSurface++; }
            else { continue; }

            perSurface[name] = perSurface.GetValueOrDefault(name) + 1;
        }

        Console.WriteLine($"  total={total} land={land} ocean={ocean}");
        Console.WriteLine($"  FIELD  fire>0: {fireAny,8} ({Pct(fireAny, total)}) | >=310: {fireStrong,8} ({Pct(fireStrong, total)})");
        Console.WriteLine($"  FIELD  pres>0: {pressureAny,8} ({Pct(pressureAny, total)}) | >=310: {pressureStrong,8} ({Pct(pressureStrong, total)})");
        Console.WriteLine($"  FIELD  aur >0: {auroraAny,8} ({Pct(auroraAny, total)})");
        Console.WriteLine($"  SURFACE fire: {fireSurface,8} ({Pct(fireSurface, total)})");
        Console.WriteLine($"  SURFACE aur : {auroraSurface,8} ({Pct(auroraSurface, total)})");
        Console.WriteLine($"  SURFACE pres: {pressureSurface,8} ({Pct(pressureSurface, total)})");
        Console.WriteLine($"  SURFACE all magic: {fireSurface + auroraSurface + pressureSurface} ({Pct(fireSurface + auroraSurface + pressureSurface, total)})");
        foreach (var (name, count) in perSurface.OrderByDescending(pair => pair.Value))
        {
            Console.WriteLine($"    {name,-24} {count,8} ({Pct(count, total)})");
        }
    }

    private static string Pct(long part, long total) =>
        $"{part * 100d / Math.Max(1, total):0.00}%";

    /// <summary>
    /// Is the world's wetness concentrated on low ground? Buckets land by
    /// height above sea level and reports the wet surface mix in each, plus
    /// how much lake sits nearby, so "low landmasses are marshy" becomes a
    /// measurement rather than an impression.
    /// </summary>
    internal static void ReportWetness(
        PhysicalGeographyField field,
        IceRemembers.Seed.IceRemembersMicrogeographyMap magic)
    {
        var palette = IceRemembers.IceRemembersPhysicalGeography.SurfacePalette;
        int[] cuts = [25, 60, 120, 250, 500, int.MaxValue];
        string[] names = ["0-25", "25-60", "60-120", "120-250", "250-500", "500+"];
        var landCount = new long[cuts.Length];
        var wetCount = new long[cuts.Length];
        var lakeAdjacent = new long[cuts.Length];
        var perSurface = new Dictionary<string, long>(StringComparer.Ordinal);
        long totalLand = 0;

        long seaLevel = long.MinValue;
        for (var t = 0; t < field.Grid.TriangleCount; t++)
        {
            if (field.WaterKindCodes[t] == PhysicalWaterKindCodes.Ocean)
            {
                seaLevel = Math.Max(seaLevel, field.WaterSurfaceElevations[t]);
            }
        }

        string[] wet =
            ["river_valley", "river_channel", "floodplain", "delta", "wet_tundra", "lake"];
        for (var t = 0; t < field.Grid.TriangleCount; t++)
        {
            if (!field.Grid.TriangleIsInsideConfiguredDisk(t) ||
                field.WaterKindCodes[t] != PhysicalWaterKindCodes.None)
            {
                continue;
            }

            totalLand++;
            var height = PhysicalElevation.ToUnits(field.Elevation[t] - seaLevel);
            var bucket = 0;
            while (bucket < cuts.Length - 1 && height > cuts[bucket])
            {
                bucket++;
            }

            landCount[bucket]++;
            var name = palette[magic.SurfaceIndexes[t]];
            if (Array.IndexOf(wet, name) >= 0)
            {
                wetCount[bucket]++;
                perSurface[name] = perSurface.GetValueOrDefault(name) + 1;
            }

            for (var edge = 0; edge < 3; edge++)
            {
                var neighbor = field.Grid.NeighborTriangleIndex(t, edge);
                if (neighbor >= 0 && field.WaterKindCodes[neighbor] == PhysicalWaterKindCodes.Lake)
                {
                    lakeAdjacent[bucket]++;
                    break;
                }
            }
        }

        Console.WriteLine("\n== Wetness by height above sea level (land only) ==");
        for (var bucket = 0; bucket < cuts.Length; bucket++)
        {
            if (landCount[bucket] == 0) { continue; }
            Console.WriteLine(
                $"  {names[bucket],-8} land={landCount[bucket],7} " +
                $"wet={wetCount[bucket] * 100d / landCount[bucket],5:0.0}% " +
                $"lakeshore={lakeAdjacent[bucket] * 100d / landCount[bucket],5:0.0}%");
        }

        Console.WriteLine($"  total land={totalLand}");
        foreach (var (name, count) in perSurface.OrderByDescending(pair => pair.Value))
        {
            Console.WriteLine($"    {name,-16} {count,8} ({count * 100d / Math.Max(1, totalLand):0.0}% of land)");
        }
    }

    /// <summary>
    /// The named coast forms from review: offshore island groups, narrow
    /// peninsulas and isthmuses, and drowned glacial inlets.
    /// </summary>
    private static void ReportCoastalForms(PhysicalGeographyField field)
    {
        var landmassSizes = new Dictionary<int, int>();
        long isthmus = 0;
        long fjord = 0;
        for (var t = 0; t < field.Grid.TriangleCount; t++)
        {
            if (!field.Grid.TriangleIsInsideConfiguredDisk(t))
            {
                continue;
            }

            if (field.LandformCodes[t] == PhysicalLandformCodes.Fjord)
            {
                fjord++;
            }

            if (field.WaterKindCodes[t] != PhysicalWaterKindCodes.None)
            {
                continue;
            }

            var landmass = field.LandmassIds[t];
            if (landmass > 0)
            {
                landmassSizes[landmass] = landmassSizes.GetValueOrDefault(landmass) + 1;
            }

            // Land with ocean on two or more sides is a neck: a peninsula tip,
            // an isthmus, or a barrier strand.
            var oceanSides = 0;
            for (var edge = 0; edge < 3; edge++)
            {
                var neighbor = field.Grid.NeighborTriangleIndex(t, edge);
                if (neighbor >= 0 && field.WaterKindCodes[neighbor] == PhysicalWaterKindCodes.Ocean)
                {
                    oceanSides++;
                }
            }

            if (oceanSides >= 2)
            {
                isthmus++;
            }
        }

        var islands = landmassSizes.Values.Count(size => size is > 0 and < 400);
        var large = landmassSizes.Values.Count(size => size >= 400);
        Console.WriteLine(
            $"  landmasses: {large} major, {islands} islands (<400 triangles); " +
            $"neck triangles={isthmus} fjord triangles={fjord}");
    }

    private static bool HasOceanNeighbor(PhysicalGeographyField field, int triangleIndex)
    {
        for (var edge = 0; edge < 3; edge++)
        {
            var neighbor = field.Grid.NeighborTriangleIndex(triangleIndex, edge);
            if (neighbor >= 0 && field.WaterKindCodes[neighbor] == PhysicalWaterKindCodes.Ocean)
            {
                return true;
            }
        }

        return false;
    }

}
