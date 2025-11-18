namespace MicroEngine.Core.Tests.Input;

/// <summary>
/// Tests for GamepadButton enum to verify value consistency.
/// </summary>
public class GamepadButtonTests
{
    [Fact]
    public void GamepadButton_Unknown_HasExpectedValue()
    {
        Assert.Equal(0, (int)Core.Input.GamepadButton.Unknown);
    }

    [Fact]
    public void GamepadButton_DPadButtons_HaveExpectedValues()
    {
        Assert.Equal(1, (int)Core.Input.GamepadButton.DPadUp);
        Assert.Equal(2, (int)Core.Input.GamepadButton.DPadRight);
        Assert.Equal(3, (int)Core.Input.GamepadButton.DPadDown);
        Assert.Equal(4, (int)Core.Input.GamepadButton.DPadLeft);
    }

    [Fact]
    public void GamepadButton_FaceButtons_HaveExpectedValues()
    {
        Assert.Equal(5, (int)Core.Input.GamepadButton.Y);
        Assert.Equal(6, (int)Core.Input.GamepadButton.B);
        Assert.Equal(7, (int)Core.Input.GamepadButton.A);
        Assert.Equal(8, (int)Core.Input.GamepadButton.X);
    }

    [Fact]
    public void GamepadButton_ShoulderButtons_HaveExpectedValues()
    {
        Assert.Equal(9, (int)Core.Input.GamepadButton.LeftBumper);
        Assert.Equal(10, (int)Core.Input.GamepadButton.LeftTrigger);
        Assert.Equal(11, (int)Core.Input.GamepadButton.RightBumper);
        Assert.Equal(12, (int)Core.Input.GamepadButton.RightTrigger);
    }

    [Fact]
    public void GamepadButton_SystemButtons_HaveExpectedValues()
    {
        Assert.Equal(13, (int)Core.Input.GamepadButton.Back);
        Assert.Equal(14, (int)Core.Input.GamepadButton.Guide);
        Assert.Equal(15, (int)Core.Input.GamepadButton.Start);
    }

    [Fact]
    public void GamepadButton_ThumbstickButtons_HaveExpectedValues()
    {
        Assert.Equal(16, (int)Core.Input.GamepadButton.LeftThumb);
        Assert.Equal(17, (int)Core.Input.GamepadButton.RightThumb);
    }

    [Fact]
    public void GamepadButton_AllValues_AreUnique()
    {
        var values = Enum.GetValues<Core.Input.GamepadButton>();
        var uniqueValues = values.Distinct().ToArray();
        
        Assert.Equal(values.Length, uniqueValues.Length);
    }
}
