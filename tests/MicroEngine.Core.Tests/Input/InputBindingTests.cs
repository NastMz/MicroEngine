using MicroEngine.Core.Input;
using Xunit;

namespace MicroEngine.Core.Tests.Input;

public sealed class InputBindingTests
{
    [Fact]
    public void Constructor_WithKeyboard_SetsProperties()
    {
        // Act
        var binding = new InputBinding(InputBindingType.Keyboard, (int)Key.Space);

        // Assert
        Assert.Equal(InputBindingType.Keyboard, binding.Type);
        Assert.Equal((int)Key.Space, binding.Code);
        Assert.Equal(-1, binding.GamepadIndex);
    }

    [Fact]
    public void Constructor_WithMouse_SetsProperties()
    {
        // Act
        var binding = new InputBinding(InputBindingType.Mouse, (int)MouseButton.Left);

        // Assert
        Assert.Equal(InputBindingType.Mouse, binding.Type);
        Assert.Equal((int)MouseButton.Left, binding.Code);
    }

    [Fact]
    public void Constructor_WithGamepad_SetsProperties()
    {
        // Act
        var binding = new InputBinding(InputBindingType.Gamepad, (int)GamepadButton.A, 0);

        // Assert
        Assert.Equal(InputBindingType.Gamepad, binding.Type);
        Assert.Equal((int)GamepadButton.A, binding.Code);
        Assert.Equal(0, binding.GamepadIndex);
    }

    [Fact]
    public void Equals_WithSameValues_ReturnsTrue()
    {
        // Arrange
        var binding1 = new InputBinding(InputBindingType.Keyboard, (int)Key.Space);
        var binding2 = new InputBinding(InputBindingType.Keyboard, (int)Key.Space);

        // Act & Assert
        Assert.Equal(binding1, binding2);
        Assert.True(binding1 == binding2);
        Assert.False(binding1 != binding2);
    }

    [Fact]
    public void Equals_WithDifferentValues_ReturnsFalse()
    {
        // Arrange
        var binding1 = new InputBinding(InputBindingType.Keyboard, (int)Key.Space);
        var binding2 = new InputBinding(InputBindingType.Keyboard, (int)Key.W);

        // Act & Assert
        Assert.NotEqual(binding1, binding2);
        Assert.False(binding1 == binding2);
        Assert.True(binding1 != binding2);
    }

    [Fact]
    public void GetHashCode_IsConsistent()
    {
        // Arrange
        var binding = new InputBinding(InputBindingType.Keyboard, (int)Key.Space);

        // Act
        var hash1 = binding.GetHashCode();
        var hash2 = binding.GetHashCode();

        // Assert
        Assert.Equal(hash1, hash2);
    }

    [Fact]
    public void ToString_KeyboardBinding_ReturnsDescription()
    {
        // Arrange
        var binding = new InputBinding(InputBindingType.Keyboard, (int)Key.Space);

        // Act
        var str = binding.ToString();

        // Assert
        Assert.Contains("Key:", str);
        Assert.Contains("Space", str);
    }

    [Fact]
    public void ToString_MouseBinding_ReturnsDescription()
    {
        // Arrange
        var binding = new InputBinding(InputBindingType.Mouse, (int)MouseButton.Left);

        // Act
        var str = binding.ToString();

        // Assert
        Assert.Contains("Mouse:", str);
    }

    [Fact]
    public void ToString_GamepadBinding_ReturnsDescription()
    {
        // Arrange
        var binding = new InputBinding(InputBindingType.Gamepad, (int)GamepadButton.A, 0);

        // Act
        var str = binding.ToString();

        // Assert
        Assert.Contains("Gamepad0:", str);
    }
}
