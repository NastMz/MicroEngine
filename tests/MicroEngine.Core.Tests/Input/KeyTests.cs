namespace MicroEngine.Core.Tests.Input;

/// <summary>
/// Tests for Key enum to verify value ranges and consistency.
/// </summary>
public class KeyTests
{
    [Fact]
    public void Key_AlphabeticKeys_HaveConsecutiveValues()
    {
        // Verify A-Z are consecutive
        Assert.Equal(65, (int)Core.Input.Key.A);
        Assert.Equal(66, (int)Core.Input.Key.B);
        Assert.Equal(90, (int)Core.Input.Key.Z);
    }

    [Fact]
    public void Key_NumericKeys_HaveConsecutiveValues()
    {
        // Verify 0-9 are consecutive
        Assert.Equal(48, (int)Core.Input.Key.Zero);
        Assert.Equal(49, (int)Core.Input.Key.One);
        Assert.Equal(57, (int)Core.Input.Key.Nine);
    }

    [Fact]
    public void Key_FunctionKeys_HaveConsecutiveValues()
    {
        // Verify F1-F12 are consecutive
        Assert.Equal(290, (int)Core.Input.Key.F1);
        Assert.Equal(291, (int)Core.Input.Key.F2);
        Assert.Equal(301, (int)Core.Input.Key.F12);
    }

    [Fact]
    public void Key_ArrowKeys_HaveExpectedValues()
    {
        Assert.Equal(262, (int)Core.Input.Key.Right);
        Assert.Equal(263, (int)Core.Input.Key.Left);
        Assert.Equal(264, (int)Core.Input.Key.Down);
        Assert.Equal(265, (int)Core.Input.Key.Up);
    }

    [Fact]
    public void Key_SpecialKeys_HaveExpectedValues()
    {
        Assert.Equal(32, (int)Core.Input.Key.Space);
        Assert.Equal(257, (int)Core.Input.Key.Enter);
        Assert.Equal(256, (int)Core.Input.Key.Escape);
        Assert.Equal(259, (int)Core.Input.Key.Backspace);
        Assert.Equal(258, (int)Core.Input.Key.Tab);
    }

    [Fact]
    public void Key_ModifierKeys_HaveExpectedValues()
    {
        Assert.Equal(340, (int)Core.Input.Key.LeftShift);
        Assert.Equal(341, (int)Core.Input.Key.LeftControl);
        Assert.Equal(342, (int)Core.Input.Key.LeftAlt);
        Assert.Equal(344, (int)Core.Input.Key.RightShift);
        Assert.Equal(345, (int)Core.Input.Key.RightControl);
        Assert.Equal(346, (int)Core.Input.Key.RightAlt);
    }
}
