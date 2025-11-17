namespace MicroEngine.Core.Tests.Input;

/// <summary>
/// Tests for GamepadAxis enum to verify value consistency.
/// </summary>
public class GamepadAxisTests
{
    [Fact]
    public void GamepadAxis_LeftStick_HaveExpectedValues()
    {
        Assert.Equal(0, (int)Core.Input.GamepadAxis.LeftX);
        Assert.Equal(1, (int)Core.Input.GamepadAxis.LeftY);
    }

    [Fact]
    public void GamepadAxis_RightStick_HaveExpectedValues()
    {
        Assert.Equal(2, (int)Core.Input.GamepadAxis.RightX);
        Assert.Equal(3, (int)Core.Input.GamepadAxis.RightY);
    }

    [Fact]
    public void GamepadAxis_Triggers_HaveExpectedValues()
    {
        Assert.Equal(4, (int)Core.Input.GamepadAxis.LeftTrigger);
        Assert.Equal(5, (int)Core.Input.GamepadAxis.RightTrigger);
    }

    [Fact]
    public void GamepadAxis_AllValues_AreUnique()
    {
        var values = Enum.GetValues<Core.Input.GamepadAxis>();
        var uniqueValues = values.Distinct().ToArray();
        
        Assert.Equal(values.Length, uniqueValues.Length);
    }

    [Fact]
    public void GamepadAxis_Count_IsCorrect()
    {
        var values = Enum.GetValues<Core.Input.GamepadAxis>();
        Assert.Equal(6, values.Length);
    }
}
