namespace TheCanonryGame.UI.Tests;

using TheCanonryGame.UI;

/// <summary>
/// The painter draws each shared hex edge from exactly one side, so hex-edge
/// features (coast, ridge, cliff, pass, fracture) must not double-draw or
/// vanish. Ownership depends on the sector-to-neighbour mapping, which is a
/// second copy of the lattice's cross-hex adjacency; when the two drift apart
/// edges are drawn twice or not at all. This asserts the contract exhaustively
/// over a patch rather than trusting the two copies to stay in step.
/// </summary>
public sealed class SharedHexEdgeOwnershipTests
{
    /// <summary>
    /// Geometric sector s faces this axial delta, mirroring the corrected
    /// cross-hex table in PhysicalMicrotriangleTopology.
    /// </summary>
    private static readonly (int Q, int R)[] SectorDelta =
    [
        (1, 0),
        (0, 1),
        (-1, 1),
        (-1, 0),
        (0, -1),
        (1, -1),
    ];

    [Fact]
    public void EveryInteriorSharedEdge_HasExactlyOneOwner()
    {
        const int extent = 4;
        var checkedEdges = 0;
        for (var q = -extent; q <= extent; q++)
        {
            for (var r = -extent; r <= extent; r++)
            {
                for (var sector = 0; sector < 6; sector++)
                {
                    var neighborQ = q + SectorDelta[sector].Q;
                    var neighborR = r + SectorDelta[sector].R;
                    if (Math.Abs(neighborQ) > extent || Math.Abs(neighborR) > extent)
                    {
                        continue;
                    }

                    var mine = PhysicalMicrogeographyRenderDecisions.OwnsSharedHexEdge(
                        q,
                        r,
                        sector,
                        neighborExists: true);
                    var theirs = PhysicalMicrogeographyRenderDecisions.OwnsSharedHexEdge(
                        neighborQ,
                        neighborR,
                        OppositeSector(sector),
                        neighborExists: true);

                    Assert.True(
                        mine != theirs,
                        $"hex ({q},{r}) sector {sector} and hex ({neighborQ},{neighborR}) " +
                        $"sector {OppositeSector(sector)} share one edge but ownership was " +
                        $"{mine} / {theirs}");
                    checkedEdges++;
                }
            }
        }

        Assert.True(checkedEdges > 100, $"only {checkedEdges} shared edges were exercised");
    }

    [Fact]
    public void CroppedEdges_AreAlwaysDrawnBySoleRemainingSide()
    {
        for (var sector = 0; sector < 6; sector++)
        {
            Assert.True(PhysicalMicrogeographyRenderDecisions.OwnsSharedHexEdge(
                q: 0,
                r: 0,
                sector,
                neighborExists: false));
        }
    }

    [Fact]
    public void SectorsOutsideTheHex_AreRejected()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            PhysicalMicrogeographyRenderDecisions.OwnsSharedHexEdge(0, 0, -1, true));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            PhysicalMicrogeographyRenderDecisions.OwnsSharedHexEdge(0, 0, 6, true));
    }

    private static int OppositeSector(int sector) => (sector + 3) % 6;
}
