using MicroEngine.Core.Input;
using Xunit;

namespace MicroEngine.Core.Tests.Input;

public sealed class InputActionTests
{
    [Fact]
    public void Constructor_WithValidName_SetsName()
    {
        // Act
        var action = new InputAction("Jump");

        // Assert
        Assert.Equal("Jump", action.Name);
        Assert.Empty(action.Bindings);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithInvalidName_ThrowsException(string? invalidName)
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => new InputAction(invalidName!));
    }

    [Fact]
    public void AddKeyboardBinding_AddsBinding()
    {
        // Arrange
        var action = new InputAction("Jump");

        // Act
        action.AddKeyboardBinding(Key.Space);

        // Assert
        Assert.Single(action.Bindings);
        Assert.Equal(InputBindingType.Keyboard, action.Bindings[0].Type);
        Assert.Equal((int)Key.Space, action.Bindings[0].Code);
    }

    [Fact]
    public void AddKeyboardBinding_ReturnsThis()
    {
        // Arrange
        var action = new InputAction("Jump");

        // Act
        var result = action.AddKeyboardBinding(Key.Space);

        // Assert
        Assert.Same(action, result);
    }

    [Fact]
    public void AddKeyboardBinding_AllowsChaining()
    {
        // Arrange
        var action = new InputAction("Jump");

        // Act
        action.AddKeyboardBinding(Key.Space)
            .AddKeyboardBinding(Key.W)
            .AddKeyboardBinding(Key.Up);

        // Assert
        Assert.Equal(3, action.Bindings.Count);
    }

    [Fact]
    public void AddMouseBinding_AddsBinding()
    {
        // Arrange
        var action = new InputAction("Shoot");

        // Act
        action.AddMouseBinding(MouseButton.Left);

        // Assert
        Assert.Single(action.Bindings);
        Assert.Equal(InputBindingType.Mouse, action.Bindings[0].Type);
        Assert.Equal((int)MouseButton.Left, action.Bindings[0].Code);
    }

    [Fact]
    public void AddGamepadBinding_AddsBinding()
    {
        // Arrange
        var action = new InputAction("Jump");

        // Act
        action.AddGamepadBinding(GamepadButton.A, 0);

        // Assert
        Assert.Single(action.Bindings);
        Assert.Equal(InputBindingType.Gamepad, action.Bindings[0].Type);
        Assert.Equal((int)GamepadButton.A, action.Bindings[0].Code);
        Assert.Equal(0, action.Bindings[0].GamepadIndex);
    }

    [Fact]
    public void ClearBindings_RemovesAllBindings()
    {
        // Arrange
        var action = new InputAction("Jump");
        action.AddKeyboardBinding(Key.Space);
        action.AddMouseBinding(MouseButton.Left);

        // Act
        action.ClearBindings();

        // Assert
        Assert.Empty(action.Bindings);
    }

    [Fact]
    public void RemoveKeyboardBinding_RemovesSpecificBinding()
    {
        // Arrange
        var action = new InputAction("Jump");
        action.AddKeyboardBinding(Key.Space);
        action.AddKeyboardBinding(Key.W);

        // Act
        var removed = action.RemoveKeyboardBinding(Key.Space);

        // Assert
        Assert.True(removed);
        Assert.Single(action.Bindings);
        Assert.Equal((int)Key.W, action.Bindings[0].Code);
    }

    [Fact]
    public void RemoveKeyboardBinding_ReturnsFalseIfNotFound()
    {
        // Arrange
        var action = new InputAction("Jump");
        action.AddKeyboardBinding(Key.W);

        // Act
        var removed = action.RemoveKeyboardBinding(Key.Space);

        // Assert
        Assert.False(removed);
        Assert.Single(action.Bindings);
    }

    [Fact]
    public void RemoveMouseBinding_RemovesSpecificBinding()
    {
        // Arrange
        var action = new InputAction("Shoot");
        action.AddMouseBinding(MouseButton.Left);
        action.AddMouseBinding(MouseButton.Right);

        // Act
        var removed = action.RemoveMouseBinding(MouseButton.Left);

        // Assert
        Assert.True(removed);
        Assert.Single(action.Bindings);
        Assert.Equal((int)MouseButton.Right, action.Bindings[0].Code);
    }

    [Fact]
    public void RemoveGamepadBinding_RemovesSpecificBinding()
    {
        // Arrange
        var action = new InputAction("Jump");
        action.AddGamepadBinding(GamepadButton.A, 0);
        action.AddGamepadBinding(GamepadButton.B, 0);

        // Act
        var removed = action.RemoveGamepadBinding(GamepadButton.A, 0);

        // Assert
        Assert.True(removed);
        Assert.Single(action.Bindings);
        Assert.Equal((int)GamepadButton.B, action.Bindings[0].Code);
    }
}
