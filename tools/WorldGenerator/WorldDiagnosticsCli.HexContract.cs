// Hex-contract instrument: what the quantized fact columns, derived
// capacity, and placement taste actually look like on a built seed. This is
// the measurement side of the terrain reset - retuning reads this report,
// it does not guess.
#pragma warning disable S1541, S3776, MA0051, S3358, MA0193, S104, S1200, MA0048

namespace WorldGeneratorTool;

using System.Globalization;

using Engine.Constants;
using Engine.Domain.Startup;
using Engine.Kinds;
using Engine.WorldGen;

using IceRemembers;
using IceRemembers.Seed;

internal static partial class WorldDiagnosticsCli
{
    private static readonly string[] FactNames =
    [
        "temperature", "moisture", "geothermal", "elevation", "relief", "roughness",
        "landFraction", "riverCoverage", "floodplainCoverage", "iceThickness",
        "fireMean", "fireMax", "auroraMean", "auroraMax", "pressureMean", "pressureMax",
    ];

    private static int ReportHexContract(int seed)
    {
        Console.WriteLine($"Building accepted startup seed {seed} for the hex-contract report...");
        var result = IceRemembersSeedBuilder.BuildAcceptedStartupSeed(seed);
        var startupSeed = result.StartupSeed;
        var blueprint = startupSeed.PhysicalGeography;
        if (blueprint is null || blueprint.Radius == 0)
        {
            Console.Error.WriteLine("Startup seed carries no physical geography blueprint.");
            return 2;
        }

        Console.WriteLine($"accepted={result.Seed.BaseSeed}/{result.Seed.Attempt}");
        foreach (var rejection in result.RejectedAttempts)
        {
            Console.WriteLine($"  rejected {rejection}");
        }
        var heartCoords = startupSeed.Cells
            .Where(static cell => cell.PlaneCode == PlaneCodes.Physical)
            .Select(static cell => new HexCoord(cell.Q, cell.R))
            .ToHashSet();
        var heartCells = blueprint.Cells
            .Where(cell => heartCoords.Contains(new HexCoord(cell.Q, cell.R)))
            .ToArray();

        ReportFactColumns("full field", blueprint.Cells);
        ReportFactColumns("heart", heartCells);
        ReportHeartTerrainShares(startupSeed);
        ReportCapacity(blueprint, heartCoords);
        ReportTasteSpreads(blueprint, heartCoords);
        ReportEntityDispersion(startupSeed);
        return 0;
    }

    // Per-column distribution plus saturation at both ends of the byte.
    // Saturation is the quantizer-health signal: a column pinned at 255
    // means the world outgrew the quantizer's range and gameplay has gone
    // blind to differences that exist.
    private static void ReportFactColumns(string scope, IReadOnlyList<DomainPhysicalGeographyCell> cells)
    {
        Console.WriteLine($"\n== fact columns ({scope}, {cells.Count} hexes) ==");
        Console.WriteLine($"{"fact",-20} {"p10",5} {"p50",5} {"p90",5} {"max",5} {"%at0",6} {"%at255",7}");
        var facts = new byte[PhysicalHexFactCodes.Count];
        var columns = new byte[PhysicalHexFactCodes.Count][];
        for (var code = 0; code < PhysicalHexFactCodes.Count; code++)
        {
            columns[code] = new byte[cells.Count];
        }

        for (var index = 0; index < cells.Count; index++)
        {
            DomainPhysicalGeographyFacts.Quantize(cells[index], facts);
            for (var code = 0; code < PhysicalHexFactCodes.Count; code++)
            {
                columns[code][index] = facts[code];
            }
        }

        for (var code = 0; code < PhysicalHexFactCodes.Count; code++)
        {
            var column = columns[code];
            Array.Sort(column);
            var atZero = column.Count(static value => value == 0);
            var atMax = column.Count(static value => value == 255);
            Console.WriteLine(
                $"{FactNames[code],-20} {Percentile(column, 10),5} {Percentile(column, 50),5} " +
                $"{Percentile(column, 90),5} {column[^1],5} " +
                $"{100f * atZero / column.Length,5:0.0}% {100f * atMax / column.Length,6:0.0}%");
        }
    }

    private static void ReportHeartTerrainShares(DomainStartupSeed startupSeed)
    {
        var physical = startupSeed.Cells
            .Where(static cell => cell.PlaneCode == PlaneCodes.Physical)
            .ToArray();
        Console.WriteLine($"\n== heart terrain shares ({physical.Length} cells) ==");
        foreach (var group in physical
            .GroupBy(static cell => cell.Terrain, StringComparer.Ordinal)
            .OrderByDescending(static group => group.Count()))
        {
            Console.WriteLine(
                $"{group.Key,-16} {group.Count(),5} ({100f * group.Count() / physical.Length,5:0.0}%)");
        }
    }

    private static void ReportCapacity(
        DomainPhysicalGeographyBlueprint blueprint,
        IReadOnlySet<HexCoord> heartCoords)
    {
        var taste = new IceRemembersSeedTaste(blueprint.Cells);
        var capacities = heartCoords
            .Select(taste.Capacity)
            .Order()
            .ToArray();
        var habitable = capacities.Count(static value => value >= 1f);
        var rich = capacities.Count(static value => value >= 3f);
        Console.WriteLine($"\n== derived capacity (heart, {capacities.Length} cells) ==");
        Console.WriteLine(
            $"p10={FloatPercentile(capacities, 10):0.00} p50={FloatPercentile(capacities, 50):0.00} " +
            $"p90={FloatPercentile(capacities, 90):0.00} max={capacities[^1]:0.00}");
        Console.WriteLine(
            $"habitable (>=1): {habitable} ({100f * habitable / capacities.Length:0.0}%)  " +
            $"rich (>=3): {rich} ({100f * rich / capacities.Length:0.0}%)");
    }

    // The spread a taste sees is the differentiation placement actually
    // has. A flat spread means identity has nothing to read on this world.
    private static void ReportTasteSpreads(
        DomainPhysicalGeographyBlueprint blueprint,
        IReadOnlySet<HexCoord> heartCoords)
    {
        var taste = new IceRemembersSeedTaste(blueprint.Cells);
        var probes = new (string Label, KindId Kind, string? Culture)[]
        {
            ("settlement", IceRemembersKinds.Settlement, null),
            ("penguin/aurora_stack", IceRemembersKinds.Penguin, IceRemembersCulture.AuroraStack.ToDomainName()),
            ("penguin/nightshelf", IceRemembersKinds.Penguin, IceRemembersCulture.Nightshelf.ToDomainName()),
            ("penguin/wake_singers", IceRemembersKinds.Penguin, IceRemembersCulture.WakeSingers.ToDomainName()),
            ("orca/wake_singers", IceRemembersKinds.Orca, IceRemembersCulture.WakeSingers.ToDomainName()),
            ("monster", IceRemembersKinds.Monster, null),
            ("artifact", IceRemembersKinds.Artifact, null),
        };
        Console.WriteLine($"\n== placement taste spreads (heart) ==");
        Console.WriteLine($"{"taste",-24} {"p10",8} {"p50",8} {"p90",8} {"spread",8}");
        foreach (var (label, kind, culture) in probes)
        {
            var scores = heartCoords
                .Select(coord => taste.Score(kind, culture, coord))
                .Order()
                .ToArray();
            var p10 = FloatPercentile(scores, 10);
            var p90 = FloatPercentile(scores, 90);
            Console.WriteLine(
                $"{label,-24} {p10,8:0.0} {FloatPercentile(scores, 50),8:0.0} {p90,8:0.0} {p90 - p10,8:0.0}");
        }
    }

    private static void ReportEntityDispersion(DomainStartupSeed startupSeed)
    {
        Console.WriteLine("\n== seed entity dispersion (physical) ==");
        Console.WriteLine($"{"kind",-18} {"count",5} {"minNN",6} {"meanNN",7} {"adj",4}");
        foreach (var group in startupSeed.Entities
            .GroupBy(static entity => entity.Kind, StringComparer.Ordinal)
            .OrderBy(static group => group.Key, StringComparer.Ordinal))
        {
            var members = group.ToArray();
            if (members.Length < 2)
            {
                continue;
            }

            var nearest = members
                .Select(entity => members
                    .Where(other => other.SeedEntityId != entity.SeedEntityId)
                    .Min(other => HexCoord.Distance(
                        new HexCoord(entity.PhysicalQ, entity.PhysicalR),
                        new HexCoord(other.PhysicalQ, other.PhysicalR))))
                .ToArray();
            var adjacent = nearest.Count(static distance => distance <= 1);
            Console.WriteLine(
                $"{group.Key,-18} {members.Length,5} {nearest.Min(),6} {nearest.Average(),7:0.0} {adjacent,4}");
        }
    }

    private static byte Percentile(byte[] sorted, int percentile) =>
        sorted[Math.Min(sorted.Length - 1, sorted.Length * percentile / 100)];

    private static float FloatPercentile(float[] sorted, int percentile) =>
        sorted[Math.Min(sorted.Length - 1, sorted.Length * percentile / 100)];
}
