using MicroEngine.Core.State;

namespace MicroEngine.Core.Tests.State;

public sealed class GameStateTests
{
    [Fact]
    public void Constructor_CreatesEmptyState()
    {
        // Act
        var gameState = new GameState();

        // Assert
        Assert.NotNull(gameState);
        Assert.Equal(0, gameState.Count);
    }

    [Fact]
    public void Set_AddsNewValue()
    {
        // Arrange
        var gameState = new GameState();

        // Act
        gameState.Set("score", 100);

        // Assert
        Assert.Equal(1, gameState.Count);
        Assert.Equal(100, gameState.Get<int>("score"));
    }

    [Fact]
    public void Set_UpdatesExistingValue()
    {
        // Arrange
        var gameState = new GameState();
        gameState.Set("score", 100);

        // Act
        gameState.Set("score", 200);

        // Assert
        Assert.Equal(1, gameState.Count);
        Assert.Equal(200, gameState.Get<int>("score"));
    }

    [Fact]
    public void Set_WithNullKey_ThrowsArgumentNullException()
    {
        // Arrange
        var gameState = new GameState();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => gameState.Set<int>(null!, 42));
    }

    [Fact]
    public void Get_ExistingKey_ReturnsValue()
    {
        // Arrange
        var gameState = new GameState();
        gameState.Set("playerName", "Hero");

        // Act
        var name = gameState.Get<string>("playerName");

        // Assert
        Assert.Equal("Hero", name);
    }

    [Fact]
    public void Get_NonExistentKey_ThrowsKeyNotFoundException()
    {
        // Arrange
        var gameState = new GameState();

        // Act & Assert
        Assert.Throws<KeyNotFoundException>(() => gameState.Get<int>("missing"));
    }

    [Fact]
    public void Get_WithNullKey_ThrowsArgumentNullException()
    {
        // Arrange
        var gameState = new GameState();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => gameState.Get<int>(null!));
    }

    [Fact]
    public void Get_WrongType_ThrowsInvalidCastException()
    {
        // Arrange
        var gameState = new GameState();
        gameState.Set("value", 42);

        // Act & Assert
        var exception = Assert.Throws<InvalidCastException>(() => gameState.Get<string>("value"));
        Assert.Contains("cannot cast", exception.Message);
    }

    [Fact]
    public void TryGet_ExistingKey_ReturnsTrueAndValue()
    {
        // Arrange
        var gameState = new GameState();
        gameState.Set("level", 5);

        // Act
        var result = gameState.TryGet<int>("level", out var level);

        // Assert
        Assert.True(result);
        Assert.Equal(5, level);
    }

    [Fact]
    public void TryGet_NonExistentKey_ReturnsFalse()
    {
        // Arrange
        var gameState = new GameState();

        // Act
        var result = gameState.TryGet<int>("missing", out var value);

        // Assert
        Assert.False(result);
        Assert.Equal(0, value);
    }

    [Fact]
    public void TryGet_WrongType_ReturnsFalse()
    {
        // Arrange
        var gameState = new GameState();
        gameState.Set("value", 42);

        // Act
        var result = gameState.TryGet<string>("value", out var stringValue);

        // Assert
        Assert.False(result);
        Assert.Null(stringValue);
    }

    [Fact]
    public void TryGet_WithNullKey_ReturnsFalse()
    {
        // Arrange
        var gameState = new GameState();

        // Act
        var result = gameState.TryGet<int>(null!, out var value);

        // Assert
        Assert.False(result);
        Assert.Equal(0, value);
    }

    [Fact]
    public void Contains_ExistingKey_ReturnsTrue()
    {
        // Arrange
        var gameState = new GameState();
        gameState.Set("key", "value");

        // Act
        var result = gameState.Contains("key");

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void Contains_NonExistentKey_ReturnsFalse()
    {
        // Arrange
        var gameState = new GameState();

        // Act
        var result = gameState.Contains("missing");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void Contains_WithNullKey_ReturnsFalse()
    {
        // Arrange
        var gameState = new GameState();

        // Act
        var result = gameState.Contains(null!);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void Remove_ExistingKey_RemovesAndReturnsTrue()
    {
        // Arrange
        var gameState = new GameState();
        gameState.Set("key", "value");

        // Act
        var result = gameState.Remove("key");

        // Assert
        Assert.True(result);
        Assert.Equal(0, gameState.Count);
        Assert.False(gameState.Contains("key"));
    }

    [Fact]
    public void Remove_NonExistentKey_ReturnsFalse()
    {
        // Arrange
        var gameState = new GameState();

        // Act
        var result = gameState.Remove("missing");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void Remove_WithNullKey_ReturnsFalse()
    {
        // Arrange
        var gameState = new GameState();

        // Act
        var result = gameState.Remove(null!);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void Clear_RemovesAllValues()
    {
        // Arrange
        var gameState = new GameState();
        gameState.Set("key1", 1);
        gameState.Set("key2", 2);
        gameState.Set("key3", 3);

        // Act
        gameState.Clear();

        // Assert
        Assert.Equal(0, gameState.Count);
        Assert.False(gameState.Contains("key1"));
        Assert.False(gameState.Contains("key2"));
        Assert.False(gameState.Contains("key3"));
    }

    [Fact]
    public void GetKeys_ReturnsAllKeys()
    {
        // Arrange
        var gameState = new GameState();
        gameState.Set("key1", 1);
        gameState.Set("key2", 2);
        gameState.Set("key3", 3);

        // Act
        var keys = gameState.GetKeys().ToList();

        // Assert
        Assert.Equal(3, keys.Count);
        Assert.Contains("key1", keys);
        Assert.Contains("key2", keys);
        Assert.Contains("key3", keys);
    }

    [Fact]
    public void GetKeys_EmptyState_ReturnsEmptyCollection()
    {
        // Arrange
        var gameState = new GameState();

        // Act
        var keys = gameState.GetKeys();

        // Assert
        Assert.Empty(keys);
    }

    [Fact]
    public void GameState_SupportsMultipleTypes()
    {
        // Arrange
        var gameState = new GameState();

        // Act
        gameState.Set("int", 42);
        gameState.Set("string", "test");
        gameState.Set("bool", true);
        gameState.Set("float", 3.14f);
        gameState.Set("double", 2.71828);

        // Assert
        Assert.Equal(42, gameState.Get<int>("int"));
        Assert.Equal("test", gameState.Get<string>("string"));
        Assert.True(gameState.Get<bool>("bool"));
        Assert.Equal(3.14f, gameState.Get<float>("float"));
        Assert.Equal(2.71828, gameState.Get<double>("double"));
    }

    [Fact]
    public void GameState_SupportsComplexTypes()
    {
        // Arrange
        var gameState = new GameState();
        var data = new { Name = "Player", Level = 10 };
        var list = new List<int> { 1, 2, 3 };

        // Act
        gameState.Set("data", data);
        gameState.Set("list", list);

        // Assert
        var retrievedData = gameState.Get<object>("data");
        var retrievedList = gameState.Get<List<int>>("list");
        
        Assert.Equal(data, retrievedData);
        Assert.Equal(list, retrievedList);
    }

    [Fact]
    public void GameState_PersistsAcrossOperations()
    {
        // Arrange
        var gameState = new GameState();
        
        // Act - Simulate game session
        gameState.Set("score", 0);
        gameState.Set("score", gameState.Get<int>("score") + 100); // Player scores
        gameState.Set("score", gameState.Get<int>("score") + 50);  // Player scores again
        gameState.Set("level", 1);
        gameState.Set("level", gameState.Get<int>("level") + 1);   // Level up

        // Assert
        Assert.Equal(150, gameState.Get<int>("score"));
        Assert.Equal(2, gameState.Get<int>("level"));
    }
}
