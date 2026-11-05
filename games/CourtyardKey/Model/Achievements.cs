using CourtyardKey.Content;
using MicroEngine.Core;

namespace CourtyardKey.Model;

/// <summary>Courtyard Key's achievement catalog. The engine tracks and saves them; the play scene reports the events.</summary>
public static class CourtyardAchievements
{
    public const string FirstFoe = "first-foe", Defender = "vale-defender", WindCatcher = "wind-catcher", Cartographer = "cartographer", Warden = "warden";
    public const int DefenderGoal = 30;
    /// <summary>Rooms in the Sunken Courtyard, counted from its layout.</summary>
    public static readonly int DungeonRooms = Dungeon.Create().Rooms.Cast<RoomDef?>().Count(r => r is not null);

    public static readonly Achievement[] Catalog =
    [
        new(FirstFoe, "First Strike", "Defeat your first monster."),
        new(Defender, "Vale Defender", $"Defeat {DefenderGoal} monsters.", DefenderGoal),
        new(WindCatcher, "Wind Catcher", "Find the Wind Boomerang."),
        new(Cartographer, "Cartographer", "Explore every room of the Sunken Courtyard.", DungeonRooms),
        new(Warden, "Warden Felled", "Defeat the Moss Warden."),
    ];

    /// <summary>Visited Sunken Courtyard rooms, the progress of <see cref="Cartographer"/>.</summary>
    public static int VisitedDungeonRooms(Progress progress)
    {
        int count = 0;
        foreach (var room in progress.Visited)
            if (room.StartsWith(Dungeon.Id + ":", StringComparison.Ordinal)) count++;
        return count;
    }
}
