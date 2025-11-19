using MicroEngine.Core.ECS;
using MicroEngine.Core.ECS.Components;
using MicroEngine.Core.ECS.Systems;
using MicroEngine.Core.Graphics;
using MicroEngine.Core.Math;
using Xunit;

namespace MicroEngine.Core.Tests.ECS.Systems;

/// <summary>
/// Unit tests for CameraControllerSystem.
/// </summary>
public sealed class CameraControllerSystemTests
{
    [Fact]
    public void Update_WithMovementDirection_UpdatesCameraPosition()
    {
        // Arrange
        var world = new World();
        var system = new CameraControllerSystem();
        var entity = world.CreateEntity();

        var initialPosition = new Vector2(100f, 100f);
        var camera = new Camera2D
        {
            Position = initialPosition,
            Offset = Vector2.Zero,
            Rotation = 0f,
            Zoom = 1f
        };

        world.AddComponent(entity, new CameraComponent
        {
            Camera = camera,
            MovementSpeed = 100f,
            ZoomSpeed = 1f,
            MinZoom = 0.25f,
            MaxZoom = 4f,
            DefaultPosition = Vector2.Zero,
            MoveDirection = new Vector2(1f, 0f), // Move right
            ZoomDelta = 0f,
            ResetRequested = false
        });

        // Act
        system.Update(world, 1f); // 1 second

        // Assert
        var component = world.GetComponent<CameraComponent>(entity);
        Assert.Equal(200f, component.Camera.Position.X); // 100 + (100 * 1)
        Assert.Equal(100f, component.Camera.Position.Y); // Unchanged
    }

    [Fact]
    public void Update_WithZoomDelta_UpdatesCameraZoom()
    {
        // Arrange
        var world = new World();
        var system = new CameraControllerSystem();
        var entity = world.CreateEntity();

        var camera = new Camera2D
        {
            Position = Vector2.Zero,
            Offset = Vector2.Zero,
            Rotation = 0f,
            Zoom = 1f
        };

        world.AddComponent(entity, new CameraComponent
        {
            Camera = camera,
            MovementSpeed = 100f,
            ZoomSpeed = 2f,
            MinZoom = 0.25f,
            MaxZoom = 4f,
            DefaultPosition = Vector2.Zero,
            MoveDirection = Vector2.Zero,
            ZoomDelta = 1f, // Zoom in
            ResetRequested = false
        });

        // Act
        system.Update(world, 0.5f); // 0.5 seconds

        // Assert
        var component = world.GetComponent<CameraComponent>(entity);
        Assert.Equal(2f, component.Camera.Zoom); // 1 + (1 * 2 * 0.5)
    }

    [Fact]
    public void Update_WithZoomDelta_ClampsToMinZoom()
    {
        // Arrange
        var world = new World();
        var system = new CameraControllerSystem();
        var entity = world.CreateEntity();

        var camera = new Camera2D
        {
            Position = Vector2.Zero,
            Offset = Vector2.Zero,
            Rotation = 0f,
            Zoom = 0.5f
        };

        world.AddComponent(entity, new CameraComponent
        {
            Camera = camera,
            MovementSpeed = 100f,
            ZoomSpeed = 2f,
            MinZoom = 0.25f,
            MaxZoom = 4f,
            DefaultPosition = Vector2.Zero,
            MoveDirection = Vector2.Zero,
            ZoomDelta = -1f, // Zoom out
            ResetRequested = false
        });

        // Act
        system.Update(world, 1f); // 1 second

        // Assert
        var component = world.GetComponent<CameraComponent>(entity);
        Assert.Equal(0.25f, component.Camera.Zoom); // Clamped to min
    }

    [Fact]
    public void Update_WithZoomDelta_ClampsToMaxZoom()
    {
        // Arrange
        var world = new World();
        var system = new CameraControllerSystem();
        var entity = world.CreateEntity();

        var camera = new Camera2D
        {
            Position = Vector2.Zero,
            Offset = Vector2.Zero,
            Rotation = 0f,
            Zoom = 3.5f
        };

        world.AddComponent(entity, new CameraComponent
        {
            Camera = camera,
            MovementSpeed = 100f,
            ZoomSpeed = 2f,
            MinZoom = 0.25f,
            MaxZoom = 4f,
            DefaultPosition = Vector2.Zero,
            MoveDirection = Vector2.Zero,
            ZoomDelta = 1f, // Zoom in
            ResetRequested = false
        });

        // Act
        system.Update(world, 1f); // 1 second

        // Assert
        var component = world.GetComponent<CameraComponent>(entity);
        Assert.Equal(4f, component.Camera.Zoom); // Clamped to max
    }

    [Fact]
    public void Update_WithResetRequested_ResetsCameraToDefault()
    {
        // Arrange
        var world = new World();
        var system = new CameraControllerSystem();
        var entity = world.CreateEntity();

        var defaultPosition = new Vector2(100f, 100f);
        var camera = new Camera2D
        {
            Position = new Vector2(500f, 500f),
            Offset = Vector2.Zero,
            Rotation = 45f,
            Zoom = 2f
        };

        world.AddComponent(entity, new CameraComponent
        {
            Camera = camera,
            MovementSpeed = 100f,
            ZoomSpeed = 1f,
            MinZoom = 0.25f,
            MaxZoom = 4f,
            DefaultPosition = defaultPosition,
            MoveDirection = Vector2.Zero,
            ZoomDelta = 0f,
            ResetRequested = true
        });

        // Act
        system.Update(world, 1f);

        // Assert
        var component = world.GetComponent<CameraComponent>(entity);
        Assert.Equal(defaultPosition.X, component.Camera.Position.X);
        Assert.Equal(defaultPosition.Y, component.Camera.Position.Y);
        Assert.Equal(1f, component.Camera.Zoom);
        Assert.Equal(0f, component.Camera.Rotation);
        Assert.False(component.ResetRequested); // Should be cleared
    }

    [Fact]
    public void Update_ClearsCommandsAfterProcessing()
    {
        // Arrange
        var world = new World();
        var system = new CameraControllerSystem();
        var entity = world.CreateEntity();

        var camera = new Camera2D
        {
            Position = Vector2.Zero,
            Offset = Vector2.Zero,
            Rotation = 0f,
            Zoom = 1f
        };

        world.AddComponent(entity, new CameraComponent
        {
            Camera = camera,
            MovementSpeed = 100f,
            ZoomSpeed = 1f,
            MinZoom = 0.25f,
            MaxZoom = 4f,
            DefaultPosition = Vector2.Zero,
            MoveDirection = new Vector2(1f, 1f),
            ZoomDelta = 1f,
            ResetRequested = false
        });

        // Act
        system.Update(world, 1f);

        // Assert
        var component = world.GetComponent<CameraComponent>(entity);
        Assert.Equal(0f, component.MoveDirection.X);
        Assert.Equal(0f, component.MoveDirection.Y);
        Assert.Equal(0f, component.ZoomDelta);
    }

    [Fact]
    public void Update_WithMultipleCameras_UpdatesAll()
    {
        // Arrange
        var world = new World();
        var system = new CameraControllerSystem();

        var entity1 = world.CreateEntity();
        var entity2 = world.CreateEntity();

        var camera1 = new Camera2D { Position = Vector2.Zero, Offset = Vector2.Zero, Rotation = 0f, Zoom = 1f };
        var camera2 = new Camera2D { Position = Vector2.Zero, Offset = Vector2.Zero, Rotation = 0f, Zoom = 1f };

        world.AddComponent(entity1, new CameraComponent
        {
            Camera = camera1,
            MovementSpeed = 100f,
            ZoomSpeed = 1f,
            MinZoom = 0.25f,
            MaxZoom = 4f,
            DefaultPosition = Vector2.Zero,
            MoveDirection = new Vector2(1f, 0f),
            ZoomDelta = 0f,
            ResetRequested = false
        });

        world.AddComponent(entity2, new CameraComponent
        {
            Camera = camera2,
            MovementSpeed = 50f,
            ZoomSpeed = 1f,
            MinZoom = 0.25f,
            MaxZoom = 4f,
            DefaultPosition = Vector2.Zero,
            MoveDirection = new Vector2(0f, 1f),
            ZoomDelta = 0f,
            ResetRequested = false
        });

        // Act
        system.Update(world, 1f);

        // Assert
        var component1 = world.GetComponent<CameraComponent>(entity1);
        var component2 = world.GetComponent<CameraComponent>(entity2);

        Assert.Equal(100f, component1.Camera.Position.X);
        Assert.Equal(0f, component1.Camera.Position.Y);

        Assert.Equal(0f, component2.Camera.Position.X);
        Assert.Equal(50f, component2.Camera.Position.Y);
    }
}
