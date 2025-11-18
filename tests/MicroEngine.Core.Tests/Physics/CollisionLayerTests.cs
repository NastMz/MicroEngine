using MicroEngine.Core.Physics;
using Xunit;

namespace MicroEngine.Core.Tests.Physics;

public class CollisionLayerTests
{
    [Fact]
    public void Constructor_WithName_SetsNameAndId()
    {
        var layer = new CollisionLayer(5, "Enemy");
        
        Assert.Equal(5, layer.Id);
        Assert.Equal("Enemy", layer.Name);
    }

    [Fact]
    public void Constructor_WithInvalidId_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => new CollisionLayer(-1, "Invalid"));
        Assert.Throws<ArgumentException>(() => new CollisionLayer(32, "Invalid"));
    }

    [Fact]
    public void GetMask_ReturnsCorrectBitMask()
    {
        var layer = new CollisionLayer(3, "Test");
        
        Assert.Equal(1 << 3, layer.GetMask());
    }

    [Fact]
    public void Equals_WithSameId_ReturnsTrue()
    {
        var layer1 = new CollisionLayer(5, "Layer1");
        var layer2 = new CollisionLayer(5, "Layer2");
        
        Assert.True(layer1.Equals(layer2));
        Assert.True(layer1 == layer2);
    }

    [Fact]
    public void Equals_WithDifferentId_ReturnsFalse()
    {
        var layer1 = new CollisionLayer(5, "Layer1");
        var layer2 = new CollisionLayer(6, "Layer2");
        
        Assert.False(layer1.Equals(layer2));
        Assert.True(layer1 != layer2);
    }

    [Fact]
    public void GetHashCode_WithSameId_ReturnsSameHash()
    {
        var layer1 = new CollisionLayer(5, "Layer1");
        var layer2 = new CollisionLayer(5, "Layer2");
        
        Assert.Equal(layer1.GetHashCode(), layer2.GetHashCode());
    }
}

public class CollisionMatrixTests
{
    [Fact]
    public void Constructor_InitializesWithAllCollisionsEnabled()
    {
        var matrix = new CollisionMatrix();
        
        var layer1 = new CollisionLayer(0, "Layer1");
        var layer2 = new CollisionLayer(1, "Layer2");
        
        Assert.True(matrix.CanCollide(layer1, layer2));
    }

    [Fact]
    public void SetCollision_DisablesCollision()
    {
        var matrix = new CollisionMatrix();
        var layer1 = new CollisionLayer(0, "Layer1");
        var layer2 = new CollisionLayer(1, "Layer2");
        
        matrix.SetCollision(layer1, layer2, false);
        
        Assert.False(matrix.CanCollide(layer1, layer2));
        Assert.False(matrix.CanCollide(layer2, layer1)); // Symmetric
    }

    [Fact]
    public void SetCollision_EnablesCollision()
    {
        var matrix = new CollisionMatrix();
        var layer1 = new CollisionLayer(0, "Layer1");
        var layer2 = new CollisionLayer(1, "Layer2");
        
        matrix.SetCollision(layer1, layer2, false);
        matrix.SetCollision(layer1, layer2, true);
        
        Assert.True(matrix.CanCollide(layer1, layer2));
    }

    [Fact]
    public void CanCollide_WithLayerMasks_ReturnsCorrectResult()
    {
        var matrix = new CollisionMatrix();
        var layer1 = new CollisionLayer(0, "Player");
        var layer2 = new CollisionLayer(1, "Enemy");
        var layer3 = new CollisionLayer(2, "Environment");
        
        matrix.SetCollision(layer1, layer2, true);
        matrix.SetCollision(layer1, layer3, false);
        
        int mask1 = layer1.GetMask();
        int mask2 = layer2.GetMask();
        int mask3 = layer3.GetMask();
        
        Assert.True(matrix.CanCollide(mask1, mask2));
        Assert.False(matrix.CanCollide(mask1, mask3));
    }

    [Fact]
    public void Clear_ResetsAllCollisionsToEnabled()
    {
        var matrix = new CollisionMatrix();
        var layer1 = new CollisionLayer(0, "Layer1");
        var layer2 = new CollisionLayer(1, "Layer2");
        
        matrix.SetCollision(layer1, layer2, false);
        matrix.Clear();
        
        Assert.True(matrix.CanCollide(layer1, layer2));
    }

    [Fact]
    public void IgnoreLayerCollision_DisablesCollisionBetweenLayers()
    {
        var matrix = new CollisionMatrix();
        
        matrix.IgnoreLayerCollision(0, 1);
        
        Assert.False(matrix.CanCollide(1 << 0, 1 << 1));
    }

    [Fact]
    public void EnableLayerCollision_EnablesCollisionBetweenLayers()
    {
        var matrix = new CollisionMatrix();
        
        matrix.IgnoreLayerCollision(0, 1);
        matrix.EnableLayerCollision(0, 1);
        
        Assert.True(matrix.CanCollide(1 << 0, 1 << 1));
    }
}

public class PhysicsLayersTests
{
    [Fact]
    public void Default_ReturnsLayer0()
    {
        Assert.Equal(0, PhysicsLayers.Default.Id);
        Assert.Equal("Default", PhysicsLayers.Default.Name);
    }

    [Fact]
    public void Player_ReturnsLayer1()
    {
        Assert.Equal(1, PhysicsLayers.Player.Id);
        Assert.Equal("Player", PhysicsLayers.Player.Name);
    }

    [Fact]
    public void Enemy_ReturnsLayer2()
    {
        Assert.Equal(2, PhysicsLayers.Enemy.Id);
        Assert.Equal("Enemy", PhysicsLayers.Enemy.Name);
    }

    [Fact]
    public void Environment_ReturnsLayer3()
    {
        Assert.Equal(3, PhysicsLayers.Environment.Id);
        Assert.Equal("Environment", PhysicsLayers.Environment.Name);
    }

    [Fact]
    public void Projectile_ReturnsLayer4()
    {
        Assert.Equal(4, PhysicsLayers.Projectile.Id);
        Assert.Equal("Projectile", PhysicsLayers.Projectile.Name);
    }

    [Fact]
    public void Trigger_ReturnsLayer5()
    {
        Assert.Equal(5, PhysicsLayers.Trigger.Id);
        Assert.Equal("Trigger", PhysicsLayers.Trigger.Name);
    }
}
