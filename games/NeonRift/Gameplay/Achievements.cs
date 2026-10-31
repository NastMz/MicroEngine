using MicroEngine.Core;

namespace NeonRift.Gameplay;

/// <summary>Neon Rift's achievement catalog. The engine tracks and saves them; the play scene reports the events.</summary>
public static class RiftAchievements
{
    public const string FirstScrap = "first-scrap", ScrapDealer = "scrap-dealer", Rooftops = "rooftops", Untouched = "untouched", Leviathan = "leviathan";
    public const int ScrapGoal = 100;

    public static readonly Achievement[] Catalog =
    [
        new(FirstScrap, "FIRST SCRAP", "Destroy your first robot."),
        new(ScrapDealer, "SCRAP DEALER", $"Destroy {ScrapGoal} robots.", ScrapGoal),
        new(Rooftops, "ROOFTOP RUNNER", "Clear the Neon Rooftops."),
        new(Untouched, "NOT A SCRATCH", "Clear a stage from its start without losing a life."),
        new(Leviathan, "LEVIATHAN DOWN", "Destroy the Leviathan dreadnought."),
    ];
}
