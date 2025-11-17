namespace MicroEngine.Core.Tests.Input;

/// <summary>
/// Tests for MouseButton enum to verify value consistency.
/// </summary>
public class MouseButtonTests
{
    [Fact]
    public void MouseButton_PrimaryButtons_HaveExpectedValues()
    {
        Assert.Equal(0, (int)Core.Input.MouseButton.Left);
        Assert.Equal(1, (int)Core.Input.MouseButton.Right);
        Assert.Equal(2, (int)Core.Input.MouseButton.Middle);
    }

    [Fact]
    public void MouseButton_ExtendedButtons_HaveExpectedValues()
    {
        Assert.Equal(3, (int)Core.Input.MouseButton.Side);
        Assert.Equal(4, (int)Core.Input.MouseButton.Extra);
        Assert.Equal(5, (int)Core.Input.MouseButton.Forward);
        Assert.Equal(6, (int)Core.Input.MouseButton.Back);
    }

    [Fact]
    public void MouseButton_AllValues_AreUnique()
    {
        var values = Enum.GetValues<Core.Input.MouseButton>();
        var uniqueValues = values.Distinct().ToArray();
        
        Assert.Equal(values.Length, uniqueValues.Length);
    }
}
