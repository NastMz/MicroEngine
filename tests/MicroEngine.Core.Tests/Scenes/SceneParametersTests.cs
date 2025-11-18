using MicroEngine.Core.Scenes;

namespace MicroEngine.Core.Tests.Scenes;

public sealed class SceneParametersTests
{
    [Fact]
    public void Empty_ReturnsEmptyInstance()
    {
        // Act
        var parameters = SceneParameters.Empty;

        // Assert
        Assert.NotNull(parameters);
        Assert.Equal(0, parameters.Count);
    }

    [Fact]
    public void Create_ReturnsBuilder()
    {
        // Act
        var builder = SceneParameters.Create();

        // Assert
        Assert.NotNull(builder);
    }

    [Fact]
    public void Builder_Add_StoresValue()
    {
        // Arrange
        var builder = SceneParameters.Create();

        // Act
        var parameters = builder
            .Add("level", 5)
            .Add("playerName", "Test")
            .Build();

        // Assert
        Assert.Equal(2, parameters.Count);
        Assert.Equal(5, parameters.Get<int>("level"));
        Assert.Equal("Test", parameters.Get<string>("playerName"));
    }

    [Fact]
    public void Builder_Add_WithNullKey_ThrowsArgumentNullException()
    {
        // Arrange
        var builder = SceneParameters.Create();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => builder.Add<int>(null!, 42));
    }

    [Fact]
    public void Builder_Add_DuplicateKey_ThrowsArgumentException()
    {
        // Arrange
        var builder = SceneParameters.Create();
        builder.Add("key", 1);

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => builder.Add("key", 2));
        Assert.Contains("already exists", exception.Message);
    }

    [Fact]
    public void Get_ExistingKey_ReturnsValue()
    {
        // Arrange
        var parameters = SceneParameters.Create()
            .Add("score", 1000)
            .Build();

        // Act
        var score = parameters.Get<int>("score");

        // Assert
        Assert.Equal(1000, score);
    }

    [Fact]
    public void Get_NonExistentKey_ThrowsKeyNotFoundException()
    {
        // Arrange
        var parameters = SceneParameters.Empty;

        // Act & Assert
        Assert.Throws<KeyNotFoundException>(() => parameters.Get<int>("missing"));
    }

    [Fact]
    public void Get_WithNullKey_ThrowsArgumentNullException()
    {
        // Arrange
        var parameters = SceneParameters.Empty;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => parameters.Get<int>(null!));
    }

    [Fact]
    public void Get_WrongType_ThrowsInvalidCastException()
    {
        // Arrange
        var parameters = SceneParameters.Create()
            .Add("value", 42)
            .Build();

        // Act & Assert
        var exception = Assert.Throws<InvalidCastException>(() => parameters.Get<string>("value"));
        Assert.Contains("cannot cast", exception.Message);
    }

    [Fact]
    public void TryGet_ExistingKey_ReturnsTrueAndValue()
    {
        // Arrange
        var parameters = SceneParameters.Create()
            .Add("difficulty", "Hard")
            .Build();

        // Act
        var result = parameters.TryGet<string>("difficulty", out var difficulty);

        // Assert
        Assert.True(result);
        Assert.Equal("Hard", difficulty);
    }

    [Fact]
    public void TryGet_NonExistentKey_ReturnsFalse()
    {
        // Arrange
        var parameters = SceneParameters.Empty;

        // Act
        var result = parameters.TryGet<int>("missing", out var value);

        // Assert
        Assert.False(result);
        Assert.Equal(0, value);
    }

    [Fact]
    public void TryGet_WrongType_ReturnsFalse()
    {
        // Arrange
        var parameters = SceneParameters.Create()
            .Add("value", 42)
            .Build();

        // Act
        var result = parameters.TryGet<string>("value", out var stringValue);

        // Assert
        Assert.False(result);
        Assert.Null(stringValue);
    }

    [Fact]
    public void TryGet_WithNullKey_ReturnsFalse()
    {
        // Arrange
        var parameters = SceneParameters.Empty;

        // Act
        var result = parameters.TryGet<int>(null!, out var value);

        // Assert
        Assert.False(result);
        Assert.Equal(0, value);
    }

    [Fact]
    public void Contains_ExistingKey_ReturnsTrue()
    {
        // Arrange
        var parameters = SceneParameters.Create()
            .Add("key", "value")
            .Build();

        // Act
        var result = parameters.Contains("key");

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void Contains_NonExistentKey_ReturnsFalse()
    {
        // Arrange
        var parameters = SceneParameters.Empty;

        // Act
        var result = parameters.Contains("missing");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void Contains_WithNullKey_ReturnsFalse()
    {
        // Arrange
        var parameters = SceneParameters.Empty;

        // Act
        var result = parameters.Contains(null!);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void Builder_SupportsMethodChaining()
    {
        // Act
        var parameters = SceneParameters.Create()
            .Add("int", 1)
            .Add("string", "test")
            .Add("bool", true)
            .Add("float", 3.14f)
            .Build();

        // Assert
        Assert.Equal(4, parameters.Count);
        Assert.Equal(1, parameters.Get<int>("int"));
        Assert.Equal("test", parameters.Get<string>("string"));
        Assert.True(parameters.Get<bool>("bool"));
        Assert.Equal(3.14f, parameters.Get<float>("float"));
    }

    [Fact]
    public void Parameters_AreImmutable()
    {
        // Arrange
        var builder = SceneParameters.Create().Add("key", "value");
        var parameters1 = builder.Build();
        var parameters2 = builder.Build();

        // Act
        var value1 = parameters1.Get<string>("key");
        var value2 = parameters2.Get<string>("key");

        // Assert - Both instances have independent data
        Assert.Equal("value", value1);
        Assert.Equal("value", value2);
        Assert.Equal(1, parameters1.Count);
        Assert.Equal(1, parameters2.Count);
    }

    [Fact]
    public void Builder_SupportsComplexTypes()
    {
        // Arrange
        var data = new { Name = "Player", Level = 10 };
        var list = new List<int> { 1, 2, 3 };

        // Act
        var parameters = SceneParameters.Create()
            .Add("data", data)
            .Add("list", list)
            .Build();

        // Assert
        var retrievedData = parameters.Get<object>("data");
        var retrievedList = parameters.Get<List<int>>("list");
        
        Assert.Equal(data, retrievedData);
        Assert.Equal(list, retrievedList);
    }
}
