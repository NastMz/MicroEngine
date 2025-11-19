using MicroEngine.Core.ECS;
using MicroEngine.Core.ECS.Components;
using MicroEngine.Core.ECS.Systems;
using MicroEngine.Core.Math;
using Xunit;

namespace MicroEngine.Core.Tests.ECS.Systems;

/// <summary>
/// Unit tests for DragSystem.
/// </summary>
public sealed class DragSystemTests
{
    [Fact]
    public void Update_WithStartDragRequested_StartsDragging()
    {
        // Arrange
        var world = new World();
        var system = new DragSystem();
        var entity = world.CreateEntity();

        var initialPosition = new Vector2(100f, 100f);
        var dragPosition = new Vector2(150f, 120f);

        world.AddComponent(entity, new TransformComponent { Position = initialPosition });
        world.AddComponent(entity, new DraggableComponent
        {
            IsDragging = false,
            MakeKinematicOnDrag = false,
            DragOffset = Vector2.Zero,
            StartDragRequested = true,
            DragPosition = dragPosition,
            StopDragRequested = false
        });

        // Act
        system.Update(world, 1f);

        // Assert
        var component = world.GetComponent<DraggableComponent>(entity);
        Assert.True(component.IsDragging);
        Assert.Equal(50f, component.DragOffset.X); // 150 - 100
        Assert.Equal(20f, component.DragOffset.Y); // 120 - 100
        Assert.False(component.StartDragRequested); // Should be cleared
    }

    [Fact]
    public void Update_WithStartDragRequestedAndRigidBody_MakesKinematic()
    {
        // Arrange
        var world = new World();
        var system = new DragSystem();
        var entity = world.CreateEntity();

        world.AddComponent(entity, new TransformComponent { Position = Vector2.Zero });
        world.AddComponent(entity, new RigidBodyComponent
        {
            IsKinematic = false,
            Velocity = new Vector2(100f, 100f)
        });
        world.AddComponent(entity, new DraggableComponent
        {
            IsDragging = false,
            MakeKinematicOnDrag = true,
            DragOffset = Vector2.Zero,
            StartDragRequested = true,
            DragPosition = Vector2.Zero,
            StopDragRequested = false
        });

        // Act
        system.Update(world, 1f);

        // Assert
        var rigidBody = world.GetComponent<RigidBodyComponent>(entity);
        Assert.True(rigidBody.IsKinematic);
        Assert.Equal(0f, rigidBody.Velocity.X);
        Assert.Equal(0f, rigidBody.Velocity.Y);
    }

    [Fact]
    public void Update_WhileDragging_UpdatesPosition()
    {
        // Arrange
        var world = new World();
        var system = new DragSystem();
        var entity = world.CreateEntity();

        world.AddComponent(entity, new TransformComponent { Position = new Vector2(100f, 100f) });
        world.AddComponent(entity, new DraggableComponent
        {
            IsDragging = true,
            MakeKinematicOnDrag = false,
            DragOffset = new Vector2(10f, 10f),
            StartDragRequested = false,
            DragPosition = new Vector2(200f, 150f),
            StopDragRequested = false
        });

        // Act
        system.Update(world, 1f);

        // Assert
        var transform = world.GetComponent<TransformComponent>(entity);
        Assert.Equal(190f, transform.Position.X); // 200 - 10
        Assert.Equal(140f, transform.Position.Y); // 150 - 10
    }

    [Fact]
    public void Update_WithStopDragRequested_StopsDragging()
    {
        // Arrange
        var world = new World();
        var system = new DragSystem();
        var entity = world.CreateEntity();

        world.AddComponent(entity, new TransformComponent { Position = Vector2.Zero });
        world.AddComponent(entity, new DraggableComponent
        {
            IsDragging = true,
            MakeKinematicOnDrag = false,
            DragOffset = Vector2.Zero,
            StartDragRequested = false,
            DragPosition = Vector2.Zero,
            StopDragRequested = true
        });

        // Act
        system.Update(world, 1f);

        // Assert
        var component = world.GetComponent<DraggableComponent>(entity);
        Assert.False(component.IsDragging);
        Assert.False(component.StopDragRequested); // Should be cleared
    }

    [Fact]
    public void Update_WithStopDragRequestedAndRigidBody_RestoresDynamic()
    {
        // Arrange
        var world = new World();
        var system = new DragSystem();
        var entity = world.CreateEntity();

        world.AddComponent(entity, new TransformComponent { Position = Vector2.Zero });
        world.AddComponent(entity, new RigidBodyComponent
        {
            IsKinematic = true,
            Velocity = new Vector2(50f, 50f)
        });
        world.AddComponent(entity, new DraggableComponent
        {
            IsDragging = true,
            MakeKinematicOnDrag = true,
            DragOffset = Vector2.Zero,
            StartDragRequested = false,
            DragPosition = Vector2.Zero,
            StopDragRequested = true
        });

        // Act
        system.Update(world, 1f);

        // Assert
        var rigidBody = world.GetComponent<RigidBodyComponent>(entity);
        Assert.False(rigidBody.IsKinematic);
        Assert.Equal(0f, rigidBody.Velocity.X); // Reset to zero
        Assert.Equal(0f, rigidBody.Velocity.Y);
    }

    [Fact]
    public void Update_WithoutTransformComponent_SkipsEntity()
    {
        // Arrange
        var world = new World();
        var system = new DragSystem();
        var entity = world.CreateEntity();

        // Only DraggableComponent, no TransformComponent
        world.AddComponent(entity, new DraggableComponent
        {
            IsDragging = false,
            MakeKinematicOnDrag = false,
            DragOffset = Vector2.Zero,
            StartDragRequested = true,
            DragPosition = Vector2.Zero,
            StopDragRequested = false
        });

        // Act & Assert - should not throw
        system.Update(world, 1f);

        // Component should remain unchanged (not processed)
        var component = world.GetComponent<DraggableComponent>(entity);
        Assert.True(component.StartDragRequested); // Not cleared because entity was skipped
    }

    [Fact]
    public void Update_WithMultipleDraggableEntities_UpdatesAll()
    {
        // Arrange
        var world = new World();
        var system = new DragSystem();

        var entity1 = world.CreateEntity();
        var entity2 = world.CreateEntity();

        world.AddComponent(entity1, new TransformComponent { Position = new Vector2(100f, 100f) });
        world.AddComponent(entity1, new DraggableComponent
        {
            IsDragging = true,
            DragOffset = new Vector2(5f, 5f),
            DragPosition = new Vector2(110f, 120f),
            MakeKinematicOnDrag = false,
            StartDragRequested = false,
            StopDragRequested = false
        });

        world.AddComponent(entity2, new TransformComponent { Position = new Vector2(200f, 200f) });
        world.AddComponent(entity2, new DraggableComponent
        {
            IsDragging = true,
            DragOffset = new Vector2(10f, 10f),
            DragPosition = new Vector2(250f, 260f),
            MakeKinematicOnDrag = false,
            StartDragRequested = false,
            StopDragRequested = false
        });

        // Act
        system.Update(world, 1f);

        // Assert
        var transform1 = world.GetComponent<TransformComponent>(entity1);
        var transform2 = world.GetComponent<TransformComponent>(entity2);

        Assert.Equal(105f, transform1.Position.X); // 110 - 5
        Assert.Equal(115f, transform1.Position.Y); // 120 - 5

        Assert.Equal(240f, transform2.Position.X); // 250 - 10
        Assert.Equal(250f, transform2.Position.Y); // 260 - 10
    }

    [Fact]
    public void Update_StartDragWhileAlreadyDragging_IgnoresRequest()
    {
        // Arrange
        var world = new World();
        var system = new DragSystem();
        var entity = world.CreateEntity();

        world.AddComponent(entity, new TransformComponent { Position = Vector2.Zero });
        world.AddComponent(entity, new DraggableComponent
        {
            IsDragging = true, // Already dragging
            DragOffset = new Vector2(50f, 50f), // Existing offset
            MakeKinematicOnDrag = false,
            StartDragRequested = true, // Trying to start drag again
            DragPosition = new Vector2(100f, 100f),
            StopDragRequested = false
        });

        // Act
        system.Update(world, 1f);

        // Assert
        var component = world.GetComponent<DraggableComponent>(entity);
        Assert.True(component.IsDragging); // Still dragging
        Assert.Equal(50f, component.DragOffset.X); // Offset unchanged
        Assert.Equal(50f, component.DragOffset.Y);
        Assert.True(component.StartDragRequested); // Request NOT cleared (already dragging)
    }

    [Fact]
    public void Update_StopDragWhileNotDragging_IgnoresRequest()
    {
        // Arrange
        var world = new World();
        var system = new DragSystem();
        var entity = world.CreateEntity();

        world.AddComponent(entity, new TransformComponent { Position = Vector2.Zero });
        world.AddComponent(entity, new DraggableComponent
        {
            IsDragging = false, // Not dragging
            DragOffset = Vector2.Zero,
            MakeKinematicOnDrag = false,
            StartDragRequested = false,
            DragPosition = Vector2.Zero,
            StopDragRequested = true // Trying to stop drag
        });

        // Act
        system.Update(world, 1f);

        // Assert
        var component = world.GetComponent<DraggableComponent>(entity);
        Assert.False(component.IsDragging); // Still not dragging
        Assert.True(component.StopDragRequested); // Request NOT cleared (condition not met)
    }
}
