namespace TheCanonryGame.UI.Tests;

using TheCanonryGame.UI;

public sealed class PhysicalMicrogeographyRenderDecisionTests
{
    [Fact]
    public void FeatureDetailKeepsAggregateMasksOutOfMicrotriangleLod()
    {
        Assert.Equal(
            PhysicalMicrogeographyFeatureDetail.Microtriangle,
            PhysicalMicrogeographyRenderDecisions.FeatureDetail(
                featuresEnabled: true,
                drawMicrotriangles: true,
                drawAggregateHexes: false));
        Assert.Equal(
            PhysicalMicrogeographyFeatureDetail.Aggregate,
            PhysicalMicrogeographyRenderDecisions.FeatureDetail(
                featuresEnabled: true,
                drawMicrotriangles: false,
                drawAggregateHexes: true));
        Assert.Equal(
            PhysicalMicrogeographyFeatureDetail.None,
            PhysicalMicrogeographyRenderDecisions.FeatureDetail(
                featuresEnabled: false,
                drawMicrotriangles: true,
                drawAggregateHexes: false));
    }

    [Fact]
    public void FeatureDetailRejectsOverlappingGeometryLods()
    {
        Assert.Throws<InvalidOperationException>(() =>
            PhysicalMicrogeographyRenderDecisions.FeatureDetail(
                featuresEnabled: true,
                drawMicrotriangles: true,
                drawAggregateHexes: true));
    }

    [Theory]
    [InlineData(0, true)]
    [InlineData(1, true)]
    [InlineData(2, false)]
    [InlineData(3, false)]
    [InlineData(4, false)]
    [InlineData(5, true)]
    public void SharedAggregateEdgesHaveExactlyOneOwner(int sector, bool expectedOwner)
    {
        Assert.Equal(
            expectedOwner,
            PhysicalMicrogeographyRenderDecisions.OwnsSharedHexEdge(
                q: 0,
                r: 0,
                sector,
                neighborExists: true));
    }

    [Fact]
    public void CroppedBoundaryRetainsItsAggregateEdge()
    {
        Assert.True(PhysicalMicrogeographyRenderDecisions.OwnsSharedHexEdge(
            q: 0,
            r: 0,
            sector: 3,
            neighborExists: false));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            PhysicalMicrogeographyRenderDecisions.OwnsSharedHexEdge(
                q: 0,
                r: 0,
                sector: 6,
                neighborExists: false));
    }

    [Fact]
    public void SurfaceViewsPreserveWaterUnderFrozenAndMagicSurfaces()
    {
        Assert.True(PhysicalMicrogeographyRenderDecisions.UsesWaterUnderlay(
            PhysicalGeographyView.Surface));
        Assert.True(PhysicalMicrogeographyRenderDecisions.UsesWaterUnderlay(
            PhysicalGeographyView.NaturalSurface));
        Assert.False(PhysicalMicrogeographyRenderDecisions.UsesWaterUnderlay(
            PhysicalGeographyView.Hydrology));
        Assert.False(PhysicalMicrogeographyRenderDecisions.UsesWaterUnderlay(
            PhysicalGeographyView.MagicComposite));
    }
}
