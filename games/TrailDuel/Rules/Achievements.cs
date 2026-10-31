using MicroEngine.Core;

namespace TrailDuel.Rules;

/// <summary>Trail Duel's achievement catalog. The engine tracks and saves them; the scenes report the events.</summary>
public static class TrailAchievements
{
    public const string FirstWin = "first-win", Seasoned = "seasoned", FirstCatch = "first-catch", Evolution = "evolution", Champion = "champion";
    public const int SeasonedGoal = 10;

    public static readonly Achievement[] Catalog =
    [
        new(FirstWin, "First Victory", "Win your first battle."),
        new(Seasoned, "Seasoned Trainer", $"Win {SeasonedGoal} battles.", SeasonedGoal),
        new(FirstCatch, "New Friend", "Catch a wild creature."),
        new(Evolution, "Growing Up", "Watch a creature evolve."),
        new(Champion, "Trail Champion", "Defeat Master Odessa in Brookhaven."),
    ];
}
