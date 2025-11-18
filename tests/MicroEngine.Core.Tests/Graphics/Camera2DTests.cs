using MicroEngine.Core.Graphics;
using MicroEngine.Core.Math;
using Xunit;

namespace MicroEngine.Core.Tests.Graphics;

public sealed class Camera2DTests
{
    [Fact]
    public void Constructor_Default_SetsDefaultValues()
    {
        // Arrange & Act
        var camera = new Camera2D();

        // Assert
        Assert.Equal(Vector2.Zero, camera.Position);
        Assert.Equal(Vector2.Zero, camera.Offset);
        Assert.Equal(0f, camera.Rotation);
        Assert.Equal(1f, camera.Zoom);
    }

    [Fact]
    public void Constructor_WithPosition_SetsPosition()
    {
        // Arrange
        var position = new Vector2(100f, 200f);

        // Act
        var camera = new Camera2D(position);

        // Assert
        Assert.Equal(position, camera.Position);
        Assert.Equal(1f, camera.Zoom);
    }

    [Fact]
    public void Constructor_WithPositionAndZoom_SetsBothValues()
    {
        // Arrange
        var position = new Vector2(100f, 200f);
        const float ZOOM = 2f;

        // Act
        var camera = new Camera2D(position, ZOOM);

        // Assert
        Assert.Equal(position, camera.Position);
        Assert.Equal(ZOOM, camera.Zoom);
    }

    [Fact]
    public void Zoom_SetterPreventsSmallerThanMinValue()
    {
        // Arrange
        var camera = new Camera2D();

        // Act
        camera.Zoom = 0f; // Should clamp to 0.0001f
        var zeroZoom = camera.Zoom;

        camera.Zoom = -5f; // Should also clamp to 0.0001f
        var negativeZoom = camera.Zoom;

        // Assert
        Assert.True(zeroZoom > 0f);
        Assert.True(negativeZoom > 0f);
        Assert.Equal(zeroZoom, negativeZoom);
    }

    [Fact]
    public void Move_AddsOffsetToPosition()
    {
        // Arrange
        var camera = new Camera2D(new Vector2(100f, 100f));
        var moveOffset = new Vector2(50f, -30f);

        // Act
        camera.Move(moveOffset);

        // Assert
        Assert.Equal(new Vector2(150f, 70f), camera.Position);
    }

    [Fact]
    public void Rotate_AddsAngleToRotation()
    {
        // Arrange
        var camera = new Camera2D { Rotation = 45f };

        // Act
        camera.Rotate(90f);

        // Assert
        Assert.Equal(135f, camera.Rotation);
    }

    [Fact]
    public void AdjustZoom_AddsFactorToZoom()
    {
        // Arrange
        var camera = new Camera2D { Zoom = 1f };

        // Act
        camera.AdjustZoom(0.5f);

        // Assert
        Assert.Equal(1.5f, camera.Zoom);
    }

    [Fact]
    public void AdjustZoom_PreventsZoomBelowMinValue()
    {
        // Arrange
        var camera = new Camera2D { Zoom = 0.5f };

        // Act
        camera.AdjustZoom(-1f); // Would result in -0.5, should clamp

        // Assert
        Assert.True(camera.Zoom > 0f);
    }

    [Fact]
    public void Reset_RestoresDefaultValues()
    {
        // Arrange
        var camera = new Camera2D(new Vector2(100f, 200f), 2f)
        {
            Offset = new Vector2(50f, 50f),
            Rotation = 45f
        };

        // Act
        camera.Reset();

        // Assert
        Assert.Equal(Vector2.Zero, camera.Position);
        Assert.Equal(Vector2.Zero, camera.Offset);
        Assert.Equal(0f, camera.Rotation);
        Assert.Equal(1f, camera.Zoom);
    }

    [Fact]
    public void Follow_InterpolatesPositionToTarget()
    {
        // Arrange
        var camera = new Camera2D(Vector2.Zero);
        var target = new Vector2(100f, 100f);

        // Act
        camera.Follow(target, 0.5f); // 50% interpolation

        // Assert
        Assert.Equal(new Vector2(50f, 50f), camera.Position);
    }

    [Fact]
    public void Follow_ClampsSpeedBetweenZeroAndOne()
    {
        // Arrange
        var camera = new Camera2D(Vector2.Zero);
        var target = new Vector2(100f, 100f);

        // Act - speed > 1 should be clamped to 1 (instant)
        camera.Follow(target, 2f);

        // Assert
        Assert.Equal(target, camera.Position);
    }

    [Fact]
    public void LookAt_SetsPositionDirectly()
    {
        // Arrange
        var camera = new Camera2D(Vector2.Zero);
        var target = new Vector2(123f, 456f);

        // Act
        camera.LookAt(target);

        // Assert
        Assert.Equal(target, camera.Position);
    }

    [Fact]
    public void ScreenToWorld_ConvertsScreenCoordinatesToWorldSpace()
    {
        // Arrange
        var camera = new Camera2D(new Vector2(100f, 100f));
        camera.Offset = new Vector2(640f, 360f); // Screen center
        var screenPos = new Vector2(640f, 360f); // Center of 1280x720 screen

        // Act
        var worldPos = camera.ScreenToWorld(screenPos, 1280, 720);

        // Assert
        // Screen center should map to camera position
        Assert.True(MathHelper.ApproximatelyEqual(camera.Position.X, worldPos.X, 0.1f));
        Assert.True(MathHelper.ApproximatelyEqual(camera.Position.Y, worldPos.Y, 0.1f));
    }

    [Fact]
    public void WorldToScreen_ConvertsWorldCoordinatesToScreenSpace()
    {
        // Arrange
        var camera = new Camera2D(new Vector2(100f, 100f));
        camera.Offset = new Vector2(640f, 360f); // Screen center
        var worldPos = new Vector2(100f, 100f); // Camera position

        // Act
        var screenPos = camera.WorldToScreen(worldPos, 1280, 720);

        // Assert
        // Camera position should map to screen center (offset)
        Assert.True(MathHelper.ApproximatelyEqual(camera.Offset.X, screenPos.X, 0.1f));
        Assert.True(MathHelper.ApproximatelyEqual(camera.Offset.Y, screenPos.Y, 0.1f));
    }

    [Fact]
    public void ScreenToWorld_WithZoom_AdjustsCoordinates()
    {
        // Arrange
        var camera = new Camera2D(Vector2.Zero, 2f); // 2x zoom
        camera.Offset = new Vector2(640f, 360f);
        var screenPos = new Vector2(840f, 360f); // 200 pixels right of center

        // Act
        var worldPos = camera.ScreenToWorld(screenPos, 1280, 720);

        // Assert
        // With 2x zoom, 200 pixels on screen = 100 units in world space
        Assert.True(MathHelper.ApproximatelyEqual(100f, worldPos.X, 1f));
    }

    [Fact]
    public void GetVisibleBounds_ReturnsCorrectRectangle()
    {
        // Arrange
        var camera = new Camera2D(Vector2.Zero, 1f);
        camera.Offset = new Vector2(640f, 360f);

        // Act
        var bounds = camera.GetVisibleBounds(1280, 720);

        // Assert
        // With zoom 1, visible area should match screen size
        Assert.True(MathHelper.ApproximatelyEqual(1280f, bounds.Width, 1f));
        Assert.True(MathHelper.ApproximatelyEqual(720f, bounds.Height, 1f));
    }

    [Fact]
    public void GetVisibleBounds_WithZoom_AdjustsBounds()
    {
        // Arrange
        var camera = new Camera2D(Vector2.Zero, 2f); // 2x zoom
        camera.Offset = new Vector2(640f, 360f);

        // Act
        var bounds = camera.GetVisibleBounds(1280, 720);

        // Assert
        // With 2x zoom, visible area is half the screen size
        Assert.True(MathHelper.ApproximatelyEqual(640f, bounds.Width, 10f));
        Assert.True(MathHelper.ApproximatelyEqual(360f, bounds.Height, 10f));
    }
}
