using MicroEngine.Core.ECS;
using MicroEngine.Core.ECS.Components;
using MicroEngine.Core.ECS.Helpers;
using MicroEngine.Core.Graphics;
using MicroEngine.Core.Math;
using Xunit;

namespace MicroEngine.Core.Tests.ECS;

/// <summary>
/// Tests for EntityBuilder fluent API.
/// </summary>
public sealed class EntityBuilderTests
{
    [Fact]
    public void Build_CreatesEntity()
    {
        // Arrange
        var world = new World();
        var builder = new EntityBuilder(world);

        // Act
        var entity = builder.Build();

        // Assert
        Assert.True(world.IsEntityValid(entity));
    }

    [Fact]
    public void WithTransform_AddsTransformComponent()
    {
        // Arrange
        var world = new World();
        var position = new Vector2(100f, 200f);

        // Act
        var entity = new EntityBuilder(world)
            .WithTransform(position)
            .Build();

        // Assert
        Assert.True(world.HasComponent<TransformComponent>(entity));
        ref var transform = ref world.GetComponent<TransformComponent>(entity);
        Assert.Equal(position.X, transform.Position.X);
        Assert.Equal(position.Y, transform.Position.Y);
    }

    [Fact]
    public void WithTransform_WithAllParameters_SetsAllValues()
    {
        // Arrange
        var world = new World();
        var position = new Vector2(100f, 200f);
        var rotation = 45f;
        var scale = new Vector2(2f, 2f);

        // Act
        var entity = new EntityBuilder(world)
            .WithTransform(position, rotation, scale)
            .Build();

        // Assert
        ref var transform = ref world.GetComponent<TransformComponent>(entity);
        Assert.Equal(position.X, transform.Position.X);
        Assert.Equal(rotation, transform.Rotation);
        Assert.Equal(scale.X, transform.Scale.X);
    }

    [Fact]
    public void WithSprite_AddsSpriteComponent()
    {
        // Arrange
        var world = new World();
        var tint = Color.Red;

        // Act
        var entity = new EntityBuilder(world)
            .WithSprite(tint)
            .Build();

        // Assert
        Assert.True(world.HasComponent<SpriteComponent>(entity));
        ref var sprite = ref world.GetComponent<SpriteComponent>(entity);
        Assert.Equal(tint.R, sprite.Tint.R);
        Assert.True(sprite.Visible);
    }

    [Fact]
    public void WithRigidBody_AddsRigidBodyComponent()
    {
        // Arrange
        var world = new World();

        // Act
        var entity = new EntityBuilder(world)
            .WithRigidBody(mass: 2f, useGravity: false)
            .Build();

        // Assert
        Assert.True(world.HasComponent<RigidBodyComponent>(entity));
        ref var rigidBody = ref world.GetComponent<RigidBodyComponent>(entity);
        Assert.Equal(2f, rigidBody.Mass);
        Assert.False(rigidBody.UseGravity);
    }

    [Fact]
    public void WithBoxCollider_AddsBoxCollider()
    {
        // Arrange
        var world = new World();

        // Act
        var entity = new EntityBuilder(world)
            .WithBoxCollider(32f, 64f)
            .Build();

        // Assert
        Assert.True(world.HasComponent<ColliderComponent>(entity));
        ref var collider = ref world.GetComponent<ColliderComponent>(entity);
        Assert.Equal(ColliderShape.Rectangle, collider.Shape);
        Assert.Equal(32f, collider.Size.X);
        Assert.Equal(64f, collider.Size.Y);
    }

    [Fact]
    public void WithCircleCollider_AddsCircleCollider()
    {
        // Arrange
        var world = new World();

        // Act
        var entity = new EntityBuilder(world)
            .WithCircleCollider(16f)
            .Build();

        // Assert
        Assert.True(world.HasComponent<ColliderComponent>(entity));
        ref var collider = ref world.GetComponent<ColliderComponent>(entity);
        Assert.Equal(ColliderShape.Circle, collider.Shape);
        Assert.Equal(16f, collider.Size.X);
    }

    [Fact]
    public void WithTrigger_SetsTriggerFlag()
    {
        // Arrange
        var world = new World();

        // Act
        var entity = new EntityBuilder(world)
            .WithBoxCollider(10f, 10f, isTrigger: true)
            .Build();

        // Assert
        ref var collider = ref world.GetComponent<ColliderComponent>(entity);
        Assert.True(collider.IsTrigger);
    }

    [Fact]
    public void FluentAPI_AllowsChaining()
    {
        // Arrange
        var world = new World();

        // Act
        var entity = new EntityBuilder(world)
            .WithTransform(new Vector2(10f, 20f))
            .WithSprite(Color.Blue)
            .WithRigidBody()
            .WithBoxCollider(32f, 32f)
            .Build();

        // Assert
        Assert.True(world.HasComponent<TransformComponent>(entity));
        Assert.True(world.HasComponent<SpriteComponent>(entity));
        Assert.True(world.HasComponent<RigidBodyComponent>(entity));
        Assert.True(world.HasComponent<ColliderComponent>(entity));
    }

    [Fact]
    public void Build_WithoutComponents_CreatesEmptyEntity()
    {
        // Arrange
        var world = new World();

        // Act
        var entity = new EntityBuilder(world).Build();

        // Assert
        Assert.True(world.IsEntityValid(entity));
        Assert.False(world.HasComponent<TransformComponent>(entity));
        Assert.False(world.HasComponent<SpriteComponent>(entity));
    }

    [Fact]
    public void WithName_SetsEntityName()
    {
        // Arrange
        var world = new World();
        var name = "TestEntity";

        // Act
        var entity = new EntityBuilder(world)
            .WithName(name)
            .Build();

        // Assert
        Assert.Equal(name, world.GetEntityName(entity));
    }
}
