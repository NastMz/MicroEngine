using MicroEngine.Core.ECS;
using MicroEngine.Core.ECS.Components;
using MicroEngine.Core.ECS.Helpers;
using MicroEngine.Core.Graphics;
using MicroEngine.Core.Math;
using Xunit;

namespace MicroEngine.Core.Tests.ECS;

/// <summary>
/// Tests for EntityFactory predefined archetypes.
/// </summary>
public sealed class EntityFactoryTests
{
    [Fact]
    public void CreatePlayer_CreatesEntityWithPlayerComponents()
    {
        // Arrange
        var world = new World();
        var position = new Vector2(100f, 200f);

        // Act
        var player = EntityFactory.CreatePlayer(world, position);

        // Assert
        Assert.True(world.IsEntityValid(player));
        Assert.True(world.HasComponent<TransformComponent>(player));
        Assert.True(world.HasComponent<SpriteComponent>(player));
        Assert.True(world.HasComponent<RigidBodyComponent>(player));
        Assert.True(world.HasComponent<ColliderComponent>(player));
    }

    [Fact]
    public void CreatePlayer_SetsCorrectPosition()
    {
        // Arrange
        var world = new World();
        var position = new Vector2(150f, 250f);

        // Act
        var player = EntityFactory.CreatePlayer(world, position);

        // Assert
        ref var transform = ref world.GetComponent<TransformComponent>(player);
        Assert.Equal(position.X, transform.Position.X);
        Assert.Equal(position.Y, transform.Position.Y);
    }

    [Fact]
    public void CreatePlatform_CreatesStaticPlatform()
    {
        // Arrange
        var world = new World();
        var position = new Vector2(400f, 500f);
        var width = 200f;
        var height = 50f;

        // Act
        var platform = EntityFactory.CreatePlatform(world, position, width, height);

        // Assert
        Assert.True(world.IsEntityValid(platform));
        Assert.True(world.HasComponent<TransformComponent>(platform));
        Assert.True(world.HasComponent<SpriteComponent>(platform));
        Assert.True(world.HasComponent<ColliderComponent>(platform));
        Assert.False(world.HasComponent<RigidBodyComponent>(platform));
    }

    [Fact]
    public void CreateObstacle_CreatesStaticObstacle()
    {
        // Arrange
        var world = new World();
        var position = new Vector2(300f, 400f);

        // Act
        var obstacle = EntityFactory.CreateObstacle(world, position);

        // Assert
        Assert.True(world.IsEntityValid(obstacle));
        Assert.True(world.HasComponent<TransformComponent>(obstacle));
        Assert.True(world.HasComponent<SpriteComponent>(obstacle));
        Assert.True(world.HasComponent<ColliderComponent>(obstacle));
    }

    [Fact]
    public void CreateCollectible_CreatesTriggerEntity()
    {
        // Arrange
        var world = new World();
        var position = new Vector2(200f, 300f);

        // Act
        var collectible = EntityFactory.CreateCollectible(world, position);

        // Assert
        Assert.True(world.IsEntityValid(collectible));
        Assert.True(world.HasComponent<ColliderComponent>(collectible));
        
        ref var collider = ref world.GetComponent<ColliderComponent>(collectible);
        Assert.True(collider.IsTrigger);
    }

    [Fact]
    public void CreateProjectile_CreatesMovingEntity()
    {
        // Arrange
        var world = new World();
        var position = new Vector2(100f, 100f);
        var velocity = new Vector2(500f, 0f);

        // Act
        var projectile = EntityFactory.CreateProjectile(world, position, velocity);

        // Assert
        Assert.True(world.IsEntityValid(projectile));
        Assert.True(world.HasComponent<RigidBodyComponent>(projectile));
        
        ref var rigidBody = ref world.GetComponent<RigidBodyComponent>(projectile);
        Assert.Equal(velocity.X, rigidBody.Velocity.X);
        Assert.Equal(velocity.Y, rigidBody.Velocity.Y);
        Assert.False(rigidBody.UseGravity); // Projectiles don't use gravity by default
    }

    [Fact]
    public void CreateEnemy_CreatesEntityWithAI()
    {
        // Arrange
        var world = new World();
        var position = new Vector2(500f, 400f);

        // Act
        var enemy = EntityFactory.CreateEnemy(world, position);

        // Assert
        Assert.True(world.IsEntityValid(enemy));
        Assert.True(world.HasComponent<TransformComponent>(enemy));
        Assert.True(world.HasComponent<SpriteComponent>(enemy));
        Assert.True(world.HasComponent<RigidBodyComponent>(enemy));
        Assert.True(world.HasComponent<ColliderComponent>(enemy));
    }

    [Fact]
    public void CreateParticle_CreatesSmallEntity()
    {
        // Arrange
        var world = new World();
        var position = new Vector2(250f, 350f);
        var velocity = new Vector2(10f, -50f);

        // Act
        var particle = EntityFactory.CreateParticle(world, position, velocity);

        // Assert
        Assert.True(world.IsEntityValid(particle));
        Assert.True(world.HasComponent<TransformComponent>(particle));
        Assert.True(world.HasComponent<SpriteComponent>(particle));
        Assert.True(world.HasComponent<RigidBodyComponent>(particle));
        
        ref var rigidBody = ref world.GetComponent<RigidBodyComponent>(particle);
        Assert.Equal(velocity.X, rigidBody.Velocity.X);
    }
}
