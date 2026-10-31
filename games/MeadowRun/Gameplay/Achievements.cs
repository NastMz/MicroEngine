using MicroEngine.Core;

namespace MeadowRun.Gameplay;

/// <summary>Meadow Run's achievement catalog. The engine tracks and saves them; the scenes report the events.</summary>
public static class MeadowAchievements
{
    public const string FirstCourse = "first-course", Stomper = "stomper", CoinPurse = "coin-purse", EmberHeart = "ember-heart", Sunstone = "sunstone";
    public const int StompGoal = 25, CoinGoal = 300;

    public static readonly Achievement[] Catalog =
    [
        new(FirstCourse, "First Steps", "Clear the first course."),
        new(Stomper, "Stomp Squad", $"Stomp {StompGoal} enemies.", StompGoal),
        new(CoinPurse, "Coin Purse", $"Collect {CoinGoal} coins.", CoinGoal),
        new(EmberHeart, "Ember Heart", "Become Ember Pip with an ember seed."),
        new(Sunstone, "Sunstone Home", "Defeat Grumbold and bring back the Sunstone."),
    ];
}
