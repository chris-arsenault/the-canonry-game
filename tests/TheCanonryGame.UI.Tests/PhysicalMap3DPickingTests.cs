using Godot;

using TheCanonryGame.UI;

namespace TheCanonryGame.UI.Tests;

public sealed class PhysicalMap3DPickingTests
{
    [Fact]
    public void DownwardRayIntersectsReconstructedTriangleAtExpectedDistance()
    {
        var hit = PhysicalMap3DPicker.TryIntersectTriangle(
            new Vector3(0.25f, 10f, 0.25f),
            Vector3.Down,
            new Vector3(0f, 2f, 0f),
            new Vector3(0f, 2f, 1f),
            new Vector3(1f, 2f, 0f),
            out var distance);

        Assert.True(hit);
        Assert.Equal(8f, distance, precision: 4);
    }

    [Fact]
    public void RayOutsideTriangleDoesNotProduceInspectionHit()
    {
        var hit = PhysicalMap3DPicker.TryIntersectTriangle(
            new Vector3(2f, 10f, 2f),
            Vector3.Down,
            new Vector3(0f, 2f, 0f),
            new Vector3(0f, 2f, 1f),
            new Vector3(1f, 2f, 0f),
            out _);

        Assert.False(hit);
    }
}
