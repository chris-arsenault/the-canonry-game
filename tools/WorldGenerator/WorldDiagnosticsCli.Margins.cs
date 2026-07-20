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
            Console.WriteLine(
                $"  {names[margin],-11} triangles={all[margin],8} land={landTriangles[margin],8} " +
                $"coastline={coastal[margin],6} shelf/coast={shelfPerCoast,6:0.00}");
        }
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
