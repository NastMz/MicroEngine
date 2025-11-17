using MicroEngine.Core.Time;

namespace MicroEngine.Core.Tests.Time;

/// <summary>
/// Unit tests for <see cref="GameTime"/>.
/// </summary>
public class GameTimeTests
{
    [Fact]
    public void Constructor_InitializesWithDefaultValues()
    {
        var gameTime = new GameTime();

        Assert.Equal(0f, gameTime.DeltaTime);
        Assert.Equal(0.0, gameTime.TotalElapsedTime);
        Assert.Equal(0, gameTime.FrameCount);
        Assert.Equal(1f / 60f, gameTime.FixedDeltaTime, 5);
        Assert.Equal(1f, gameTime.TimeScale);
    }

    [Fact]
    public void Constructor_AcceptsCustomFixedDeltaTime()
    {
        var gameTime = new GameTime(0.02f);

        Assert.Equal(0.02f, gameTime.FixedDeltaTime);
    }

    [Fact]
    public void Update_IncrementsDeltaTime()
    {
        var gameTime = new GameTime();

        gameTime.Update(0.016f);

        Assert.Equal(0.016f, gameTime.DeltaTime);
    }

    [Fact]
    public void Update_IncrementsTotalElapsedTime()
    {
        var gameTime = new GameTime();

        gameTime.Update(0.016f);
        gameTime.Update(0.016f);

        Assert.Equal(0.032, gameTime.TotalElapsedTime, 5);
    }

    [Fact]
    public void Update_IncrementsFrameCount()
    {
        var gameTime = new GameTime();

        gameTime.Update(0.016f);
        gameTime.Update(0.016f);
        gameTime.Update(0.016f);

        Assert.Equal(3, gameTime.FrameCount);
    }

    [Fact]
    public void ScaledDeltaTime_AppliesTimeScale()
    {
        var gameTime = new GameTime
        {
            TimeScale = 2f
        };

        gameTime.Update(0.016f);

        Assert.Equal(0.032f, gameTime.ScaledDeltaTime, 5);
    }

    [Fact]
    public void Reset_ClearsAllValues()
    {
        var gameTime = new GameTime();

        gameTime.Update(0.016f);
        gameTime.Update(0.016f);

        gameTime.Reset();

        Assert.Equal(0f, gameTime.DeltaTime);
        Assert.Equal(0.0, gameTime.TotalElapsedTime);
        Assert.Equal(0, gameTime.FrameCount);
    }
}
