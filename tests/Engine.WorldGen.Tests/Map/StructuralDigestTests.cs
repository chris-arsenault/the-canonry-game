namespace Engine.WorldGen.Tests.Map;

using System.Globalization;
using System.Text;

using Engine.Constants;

/// <summary>
/// A change detector, deliberately not a quality gate.
///
/// Fixed-proportion gates were removed from this generator because they
/// accepted visibly bad maps while encouraging tuning of the proxy. This is a
/// different instrument: it records a compact structural fingerprint of a
/// fixed small world and fails when it moves, so an unintended change to any
/// causal layer is noticed at the moment it happens rather than discovered in
/// a screenshot weeks later. The correct response to a failure is to read the
/// diff and either accept it (update the baseline in the same commit, with the
/// reason in the message) or investigate it. It never says a map is good.
/// </summary>
public sealed class StructuralDigestTests
{
    private const string BaselineFileName = "structural-digest.baseline.txt";

    [Fact]
    public void FixedSeedWorld_KeepsItsStructuralFingerprint()
    {
        var result = PhysicalGeographyLayerTestHarness.GenerateComplete();
        var digest = BuildDigest(result.Map);
        var baselinePath = Path.Combine(AppContext.BaseDirectory, BaselineFileName);
        if (!File.Exists(baselinePath))
        {
            File.WriteAllText(baselinePath, digest);
            Assert.Fail(
                $"No structural digest baseline. One was written to {baselinePath}; " +
                "copy it next to the test and commit it.");
        }

        var expected = File.ReadAllText(baselinePath).Trim();
        if (!string.Equals(expected, digest, StringComparison.Ordinal))
        {
            var actualPath = baselinePath + ".actual";
            File.WriteAllText(actualPath, digest);
            Assert.Fail(
                "The generated world's structure changed. This is a change detector, not a " +
                "quality gate: read the difference, and if the change was intended, update " +
                $"{BaselineFileName} in the same commit and say why.\n" +
                $"expected: {expected}\nactual:   {digest}\n(actual also written to {actualPath})");
        }
    }

    private static string BuildDigest(PhysicalGeographyField map)
    {
        var landforms = new SortedDictionary<string, int>(StringComparer.Ordinal);
        var margins = new SortedDictionary<string, int>(StringComparer.Ordinal);
        var lake = 0;
        var ocean = 0;
        var land = 0;
        for (var triangleIndex = 0; triangleIndex < map.Grid.TriangleCount; triangleIndex++)
        {
            if (!map.Grid.TriangleIsInsideContentDomain(triangleIndex))
            {
                continue;
            }

            Increment(landforms, LandformName(map.LandformCodes[triangleIndex]));
            Increment(margins, MarginName(map.MarginCodes[triangleIndex]));
            switch (map.WaterKindCodes[triangleIndex])
            {
                case PhysicalWaterKindCodes.Lake:
                    lake++;
                    break;
                case PhysicalWaterKindCodes.Ocean:
                    ocean++;
                    break;
                default:
                    land++;
                    break;
            }
        }

        var builder = new StringBuilder();
        Append(builder, "landforms", landforms);
        builder.Append(' ');
        Append(builder, "margins", margins);
        builder.Append(' ');
        builder.Append(CultureInfo.InvariantCulture, $"water:lake={lake},ocean={ocean},land={land}");
        return builder.ToString();
    }

    private static void Append(
        StringBuilder builder,
        string label,
        SortedDictionary<string, int> counts)
    {
        builder.Append(label).Append(':');
        var first = true;
        foreach (var (name, count) in counts)
        {
            if (!first)
            {
                builder.Append(',');
            }

            builder.Append(CultureInfo.InvariantCulture, $"{name}={count}");
            first = false;
        }
    }

    private static void Increment(SortedDictionary<string, int> counts, string key) =>
        counts[key] = counts.GetValueOrDefault(key) + 1;

    private static string LandformName(int code) => code switch
    {
        PhysicalLandformCodes.Plain => "plain",
        PhysicalLandformCodes.Slope => "slope",
        PhysicalLandformCodes.Valley => "valley",
        PhysicalLandformCodes.Basin => "basin",
        PhysicalLandformCodes.Ridge => "ridge",
        PhysicalLandformCodes.Peak => "peak",
        PhysicalLandformCodes.Pass => "pass",
        PhysicalLandformCodes.Seabed => "seabed",
        PhysicalLandformCodes.UValley => "u-valley",
        PhysicalLandformCodes.Cirque => "cirque",
        PhysicalLandformCodes.Fjord => "fjord",
        PhysicalLandformCodes.Moraine => "moraine",
        PhysicalLandformCodes.OutwashPlain => "outwash",
        PhysicalLandformCodes.ImpactBasin => "impact-basin",
        PhysicalLandformCodes.ImpactRim => "impact-rim",
        PhysicalLandformCodes.Gorge => "gorge",
        PhysicalLandformCodes.Waterfall => "waterfall",
        _ => "other",
    };

    private static string MarginName(int code) => code switch
    {
        PhysicalMarginCodes.Passive => "passive",
        PhysicalMarginCodes.Convergent => "convergent",
        PhysicalMarginCodes.Rifted => "rifted",
        PhysicalMarginCodes.Transform => "transform",
        _ => "interior",
    };
}
