using MeadowRun.Art;

namespace MeadowRun.Gameplay;

/// <summary>
/// The run that spans levels: lives, coins, score and Pip's form. Owns the scoring rules so they
/// are testable without a stage.
/// </summary>
public sealed class Session
{
    public const int StartingLives = 5;
    public const int CoinsPerLife = 100;
    public const int CoinScore = 200;
    public const int MaxLives = 99;

    /// <summary>Points for consecutive stomps without landing; after the table runs out each stomp is a 1-up.</summary>
    public static readonly int[] StompChain = [100, 200, 400, 800, 1000, 2000, 4000, 8000];

    public int Lives { get; private set; } = StartingLives;
    public int Coins { get; private set; }
    public int Score { get; private set; }
    public PipForm Form { get; set; } = PipForm.Sprout;
    /// <summary>Raised when a life is gained (for the 1-up jingle and popup).</summary>
    public event Action? LifeGained;
    /// <summary>Raised for every coin picked up (achievement progress).</summary>
    public event Action? CoinCollected;

    /// <summary>Adds a coin; every hundredth coin converts into an extra life.</summary>
    public void AddCoin()
    {
        Score += CoinScore;
        Coins++;
        CoinCollected?.Invoke();
        if (Coins >= CoinsPerLife)
        {
            Coins -= CoinsPerLife;
            AddLife();
        }
    }

    public void AddScore(int points) => Score += Math.Max(0, points);

    public void AddLife()
    {
        if (Lives < MaxLives) Lives++;
        LifeGained?.Invoke();
    }

    /// <summary>Loses a life; returns true when the run is over.</summary>
    public bool LoseLife()
    {
        Lives = Math.Max(0, Lives - 1);
        Form = PipForm.Sprout;
        return Lives == 0;
    }

    /// <summary>Score for the n-th consecutive stomp (0-based). Returns -1 when the chain awards a life instead.</summary>
    public static int StompPoints(int chain) => chain < StompChain.Length ? StompChain[chain] : -1;

    /// <summary>Applies a stomp-chain reward and returns the popup text.</summary>
    public string RewardStomp(int chain)
    {
        int points = StompPoints(chain);
        if (points < 0) { AddLife(); return "1UP"; }
        AddScore(points);
        return points.ToString(System.Globalization.CultureInfo.InvariantCulture);
    }

    /// <summary>Goal pole points by grab height (0 = bottom, 1 = top).</summary>
    public static int FlagPoints(float height01) => height01 switch
    {
        >= 0.95f => 5000,
        >= 0.75f => 2000,
        >= 0.5f => 800,
        >= 0.25f => 400,
        _ => 100,
    };

    public const int TimeBonusPerUnit = 50;

    /// <summary>Resets for a continue after game over: score and coins clear, lives refill.</summary>
    public void Continue()
    {
        Lives = StartingLives;
        Coins = 0;
        Score = 0;
        Form = PipForm.Sprout;
    }
}
