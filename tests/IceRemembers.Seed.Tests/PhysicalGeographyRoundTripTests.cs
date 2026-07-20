namespace IceRemembers.Seed.Tests;

using Engine.Constants;
using Engine.Domain.Startup;
using Engine.Projection;

/// <summary>
/// Generated facts cross four boundaries on their way to the screen: field,
/// blueprint, serialized world, projection. Each hop re-indexes triangles and
/// re-encodes conventions, and a convention that shifts in one hop is
/// invisible to every single-stage test. The sector-versus-direction edge-mask
/// defect lived in exactly such a hop.
/// </summary>
[Trait("Category", "Slow")]
public sealed class PhysicalGeographyRoundTripTests
{
    private static readonly Lazy<DomainPhysicalGeographyBlueprint> Blueprint =
        new(() => IceRemembersSeedBuilder.BuildAcceptedStartupSeed(42).StartupSeed.PhysicalGeography!);

    private static DomainPhysicalGeographyBlueprint Physical => Blueprint.Value;

    [Fact]
    public void MicrotriangleOffsets_AreDenseAndOrdered()
    {
        for (var cellIndex = 0; cellIndex < Physical.Cells.Count; cellIndex++)
        {
            Assert.Equal(
                cellIndex * PhysicalGeographyCodes.MicrotrianglesPerHex,
                Physical.Cells[cellIndex].MicrotriangleOffset);
        }

        Assert.Equal(
            Physical.Cells.Count * PhysicalGeographyCodes.MicrotrianglesPerHex,
            Physical.Microtriangles.Count);
    }

    [Fact]
    public void FlowEdges_ResolveToTrianglesThatExistInTheBlueprint()
    {
        var cellByCoord = BuildCellIndex();
        var resolved = 0;
        foreach (var (cellIndex, cell) in Enumerate())
        {
            for (var local = 0; local < PhysicalGeographyCodes.MicrotrianglesPerHex; local++)
            {
                var triangle = Physical.Microtriangles[cell.MicrotriangleOffset + local];
                if (triangle.FlowEdge is < 0 or > 2)
                {
                    continue;
                }

                PhysicalMicrotriangleTopology.ResolveNeighbor(
                    cell.Q,
                    cell.R,
                    local,
                    triangle.FlowEdge,
                    out var neighborQ,
                    out var neighborR,
                    out var neighborLocal);
                if (!cellByCoord.TryGetValue((neighborQ, neighborR), out var neighborCell))
                {
                    // Flow leaving the cropped disk is legal.
                    continue;
                }

                var offset = Physical.Cells[neighborCell].MicrotriangleOffset + neighborLocal;
                Assert.InRange(offset, 0, Physical.Microtriangles.Count - 1);
                resolved++;
                _ = cellIndex;
            }
        }

        Assert.True(resolved > 1000, $"only {resolved} flow edges resolved");
    }

    /// <summary>
    /// Hex-level edge-mask bits use the HexCoord direction convention while
    /// hex geometry uses sector order. If the two drift, shared-edge features
    /// stop pairing across the boundary.
    /// </summary>
    [Fact]
    public void SharedEdgeMasks_PairAcrossHexBoundaries()
    {
        var cellByCoord = BuildCellIndex();
        var comparisons = 0;
        foreach (var (_, cell) in Enumerate())
        {
            var coord = new HexCoord(cell.Q, cell.R);
            for (var direction = 0; direction < 6; direction++)
            {
                var neighborCoord = coord.Neighbor(direction);
                if (!cellByCoord.TryGetValue((neighborCoord.Q, neighborCoord.R), out var neighborIndex))
                {
                    continue;
                }

                var neighbor = Physical.Cells[neighborIndex];
                var opposite = (direction + 3) % 6;
                Assert.Equal(Bit(cell.RiverEdgeMask, direction), Bit(neighbor.RiverEdgeMask, opposite));
                Assert.Equal(Bit(cell.CoastEdgeMask, direction), Bit(neighbor.CoastEdgeMask, opposite));
                Assert.Equal(Bit(cell.RidgeEdgeMask, direction), Bit(neighbor.RidgeEdgeMask, opposite));
                comparisons++;
            }
        }

        Assert.True(comparisons > 1000, $"only {comparisons} shared edges compared");
    }

    [Fact]
    public void Projection_PreservesBlueprintTriangleFacts()
    {
        var projection = PhysicalGeographyProjection.FromBlueprint(Physical);
        Assert.Equal(Physical.Cells.Count, projection.Cells.Count);

        // Sample across the disk rather than all 1.2M triangles.
        var step = Math.Max(1, Physical.Cells.Count / 500);
        for (var cellIndex = 0; cellIndex < Physical.Cells.Count; cellIndex += step)
        {
            var cell = Physical.Cells[cellIndex];
            Assert.True(projection.TryGetCell(new HexCoord(cell.Q, cell.R), out var projected));
            var triangles = projection.MicrotrianglesFor(projected);
            Assert.Equal(PhysicalGeographyCodes.MicrotrianglesPerHex, triangles.Length);
            for (var local = 0; local < triangles.Length; local++)
            {
                var source = Physical.Microtriangles[cell.MicrotriangleOffset + local];
                Assert.Equal(source.RiverOrder, triangles[local].RiverOrder);
                Assert.Equal(source.RiverSpineEdgeMask, triangles[local].RiverSpineEdgeMask);
                Assert.Equal(source.WaterKindCode, triangles[local].WaterKindCode);
                Assert.Equal(source.SurfaceIndex, triangles[local].SurfaceIndex);
                Assert.Equal(source.LandformCode, triangles[local].LandformCode);
            }
        }
    }

    [Fact]
    public void SpineEdgeMasks_PairAcrossHexBoundaries()
    {
        var cellByCoord = BuildCellIndex();
        var paired = 0;
        foreach (var (_, cell) in Enumerate())
        {
            for (var local = 0; local < PhysicalGeographyCodes.MicrotrianglesPerHex; local++)
            {
                var triangle = Physical.Microtriangles[cell.MicrotriangleOffset + local];
                for (var edge = 0; edge < 3; edge++)
                {
                    if ((triangle.RiverSpineEdgeMask & (1 << edge)) == 0)
                    {
                        continue;
                    }

                    PhysicalMicrotriangleTopology.ResolveNeighbor(
                        cell.Q,
                        cell.R,
                        local,
                        edge,
                        out var neighborQ,
                        out var neighborR,
                        out var neighborLocal);
                    if (!cellByCoord.TryGetValue((neighborQ, neighborR), out var neighborCell))
                    {
                        continue;
                    }

                    var back = ReturnEdge(neighborQ, neighborR, neighborLocal, cell.Q, cell.R, local);
                    Assert.True(back >= 0, "spine edge has no reciprocal edge");
                    var neighbor = Physical.Microtriangles[
                        Physical.Cells[neighborCell].MicrotriangleOffset + neighborLocal];
                    Assert.True(
                        (neighbor.RiverSpineEdgeMask & (1 << back)) != 0,
                        $"spine edge at ({cell.Q},{cell.R}) local {local} edge {edge} is not " +
                        $"mirrored at ({neighborQ},{neighborR}) local {neighborLocal} edge {back}");
                    paired++;
                }
            }
        }

        Assert.True(paired > 100, $"only {paired} spine edges were paired");
    }

    private static int ReturnEdge(int q, int r, int local, int expectedQ, int expectedR, int expectedLocal)
    {
        for (var edge = 0; edge < 3; edge++)
        {
            PhysicalMicrotriangleTopology.ResolveNeighbor(
                q,
                r,
                local,
                edge,
                out var backQ,
                out var backR,
                out var backLocal);
            if (backQ == expectedQ && backR == expectedR && backLocal == expectedLocal)
            {
                return edge;
            }
        }

        return -1;
    }

    private static Dictionary<(int Q, int R), int> BuildCellIndex()
    {
        var index = new Dictionary<(int Q, int R), int>(Physical.Cells.Count);
        for (var cellIndex = 0; cellIndex < Physical.Cells.Count; cellIndex++)
        {
            index[(Physical.Cells[cellIndex].Q, Physical.Cells[cellIndex].R)] = cellIndex;
        }

        return index;
    }

    private static IEnumerable<(int Index, DomainPhysicalGeographyCell Cell)> Enumerate()
    {
        // Sampling keeps this a boundary test rather than a full-field sweep.
        var step = Math.Max(1, Physical.Cells.Count / 4000);
        for (var cellIndex = 0; cellIndex < Physical.Cells.Count; cellIndex += step)
        {
            yield return (cellIndex, Physical.Cells[cellIndex]);
        }
    }

    private static int Bit(int mask, int direction) => (mask >> direction) & 1;
}
