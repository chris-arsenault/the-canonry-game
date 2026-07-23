using Godot;

using TheCanonryGame.UI;

namespace TheCanonryGame.UI.Tests;

public sealed class PhysicalMap3DCameraControllerTests
{
    [Theory]
    [InlineData(0f, 0f, -1f)]
    [InlineData(1.5707964f, -1f, 0f)]
    public void ForwardInputMovesTowardTheViewedTerrain(float yaw, float expectedX, float expectedZ)
    {
        var direction = PhysicalMap3DCameraController.WorldPanDirection(
            new Vector2(0f, -1f),
            yaw).Normalized();

        Assert.Equal(expectedX, direction.X, precision: 4);
        Assert.Equal(expectedZ, direction.Z, precision: 4);
    }

    [Theory]
    [InlineData(0f, 1f, 0f)]
    [InlineData(1.5707964f, 0f, -1f)]
    public void RightInputMovesTowardCameraRight(float yaw, float expectedX, float expectedZ)
    {
        var direction = PhysicalMap3DCameraController.WorldPanDirection(
            new Vector2(1f, 0f),
            yaw).Normalized();

        Assert.Equal(expectedX, direction.X, precision: 4);
        Assert.Equal(expectedZ, direction.Z, precision: 4);
    }
}
