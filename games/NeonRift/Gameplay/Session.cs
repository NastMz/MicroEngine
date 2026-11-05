using MicroEngine.Kernel;

namespace NeonRift.Gameplay;

/// <summary>Run-wide state: lives, continues, score and the weapon carried between stages.</summary>
/// <param name="highScore">The best score before this run (the saved record), which the run's score may beat.</param>
public sealed class Session(int highScore = 0) : IStateHashed
{
    /// <summary>Adds every field of the run: lives, continues, score, high score, weapon and deaths.</summary>
    public void HashState(StateHasher hash)
    {
        hash.Add(Lives);
        hash.Add(Continues);
        hash.Add(Score);
        hash.Add(HighScore);
        hash.Add((int)Weapon);
        hash.Add(Deaths);
    }

    public const int StartLives = 3;
    public const int StartContinues = 3;
    /// <summary>Every multiple of this score grants an extra life.</summary>
    public const int ExtraLifeEvery = 20000;

    public int Lives { get; set; } = StartLives;
    public int Continues { get; set; } = StartContinues;
    public int Score { get; private set; }
    /// <summary>The best score ever reached: the saved record, or this run's score once it is higher.</summary>
    public int HighScore { get; private set; } = Math.Max(0, highScore);
    public WeaponKind Weapon { get; set; } = WeaponKind.Pulse;
    public int Deaths { get; set; }
    public event Action? LifeGained;

    public void AddScore(int points)
    {
        int before = Score / ExtraLifeEvery;
        Score += points;
        HighScore = Math.Max(HighScore, Score);
        for (int i = before; i < Score / ExtraLifeEvery; i++)
        {
            Lives++;
            LifeGained?.Invoke();
        }
    }

    /// <summary>Spends a continue: full lives, score reset, weapon lost. Returns false when none are left.</summary>
    public bool UseContinue()
    {
        if (Continues <= 0) return false;
        Continues--;
        Lives = StartLives;
        Score = 0;
        Weapon = WeaponKind.Pulse;
        return true;
    }
}
