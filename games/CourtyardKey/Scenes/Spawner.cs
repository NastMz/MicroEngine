using System.Numerics;
using CourtyardKey.Content;
using CourtyardKey.Entities;
using CourtyardKey.Model;
using CourtyardKey.World;
using MicroEngine.World;

namespace CourtyardKey.Scenes;

/// <summary>Turns a room's map placements into actors, honouring persistent progress flags.</summary>
public static class Spawner
{
    public static string ChestFlag(Room room, int index) => $"chest:{room.Key}:{index}";
    public static string GemFlag(Room room, int x, int y) => $"gem:{room.Key}:{x},{y}";

    /// <summary>
    /// A door belongs to the edge between two rooms, so both sides share one flag. The key is
    /// the room below (vertical edges) or to the right (horizontal edges) of the boundary.
    /// </summary>
    public static string DoorFlag(Room room, Dir side) => side switch
    {
        Dir.Up => $"door:{room.Area.Id}:{room.X},{room.Y}:v",
        Dir.Down => $"door:{room.Area.Id}:{room.X},{room.Y + 1}:v",
        Dir.Left => $"door:{room.Area.Id}:{room.X},{room.Y}:h",
        _ => $"door:{room.Area.Id}:{room.X + 1},{room.Y}:h",
    };

    public static void Populate(PlayScene world, Room room, EntityList<Actor> into)
    {
        var p = world.Progress;
        bool solved = p.Has($"solved:{room.Key}");
        int chestIndex = 0, signIndex = 0, houseIndex = 0;
        foreach (var place in room.Placements)
        {
            var (x, y) = (place.X, place.Y);
            var feet = room.TileToWorld(x + 0.5f, y + 0.85f);
            switch (place.Marker)
            {
                case Marker.Tree:
                    int variant = room.Def.Ambience == Ambience.Fireflies || (x * 7 + y * 3 + room.X) % 3 == 0 ? 1 : 0;
                    into.Add(new Prop(world, room.TileToWorld(x, y + 2), world.Art[$"tree{variant}"], -6));
                    break;
                case Marker.House:
                    into.Add(new Prop(world, room.TileToWorld(x, y + 3), world.Art[houseIndex++ == 0 ? "house_home" : "house_shop"], -4));
                    break;
                case Marker.RuinPillar: into.Add(new Prop(world, room.TileToWorld(x, y + 1), world.Art["ruin_pillar"], -3)); break;
                case Marker.Pillar: into.Add(new Prop(world, room.TileToWorld(x, y + 1), world.Art["pillar"], -3)); break;
                case Marker.Slime: into.Add(new Slime(world, feet)); break;
                case Marker.Bat: into.Add(new Bat(world, feet)); break;
                case Marker.Spitter: into.Add(new Spitter(world, feet)); break;
                case Marker.Knight: into.Add(new Knight(world, feet)); break;
                case Marker.Pot: into.Add(new Pot(world, feet)); break;
                case Marker.Block: into.Add(new PushBlock(world, x, y)); break;
                case Marker.Crystal: into.Add(new Crystal(world, feet, solved)); break;
                case Marker.Sign:
                    into.Add(new Sign(world, feet, signIndex < room.Def.Signs.Count ? room.Def.Signs[signIndex] : "...")); signIndex++;
                    break;
                case Marker.Chest:
                case Marker.HiddenChest:
                    int index = chestIndex++;
                    bool opened = p.Has(ChestFlag(room, index));
                    if (place.Marker == Marker.HiddenChest && !solved && !opened) break;
                    into.Add(new Chest(world, feet, index, room.Def.Chests[index], opened));
                    break;
                case Marker.Gem:
                    if (!p.Has(GemFlag(room, x, y))) into.Add(new Pickup(world, feet, PickupKind.RedGem, pop: false, persistent: true) { Flag = GemFlag(room, x, y) });
                    break;
                case Marker.Torch: into.Add(new Flame(world, room.TileToWorld(x + 0.5f, y + 0.62f), 70)); break;
                case Marker.Brazier: into.Add(new Flame(world, room.TileToWorld(x + 0.5f, y + 0.42f), 58)); break;
                case Marker.Boss:
                    if (!p.Has(PlayScene.BossFlag)) into.Add(new Warden(world, room.TileToWorld(x + 1, y + 1)));
                    break;
                case Marker.Pedestal:
                    var pedestal = new Pedestal(world, room.TileToWorld(x + 0.5f, y + 0.9f)) { HasKey = !p.HasCourtyardKey };
                    if (p.Has(PlayScene.BossFlag)) pedestal.Reveal(instantly: true);
                    into.Add(pedestal);
                    break;
                case Marker.DoorLocked or Marker.Shutter or Marker.BigDoor:
                    AddDoor(world, room, place, into, solved);
                    break;
            }
        }
        foreach (var npc in room.Def.Npcs) into.Add(new Npc(world, npc));
    }

    private static void AddDoor(PlayScene world, Room room, Placement place, EntityList<Actor> into, bool solved)
    {
        var (x, y) = (place.X, place.Y);
        var side = y == 0 ? Dir.Up : y == Room.Hgt - 1 ? Dir.Down : x == 0 ? Dir.Left : Dir.Right;
        // Only the first cell of the two-cell opening creates the door.
        bool horizontal = side is Dir.Up or Dir.Down;
        var prev = horizontal ? (x - 1, y) : (x, y - 1);
        if (room.Placements.Any(q => q.Marker == place.Marker && (q.X, q.Y) == prev)) return;
        var kind = place.Marker switch
        {
            Marker.DoorLocked => DoorKind.Locked,
            Marker.BigDoor => DoorKind.Big,
            _ => DoorKind.Shutter,
        };
        bool open = kind switch
        {
            DoorKind.Shutter => room.Def.Rule switch
            {
                // Ambush and boss shutters start open and slam once the hero is inside.
                RoomRule.Ambush or RoomRule.Boss => true,
                _ => solved,
            },
            _ => world.Progress.Has(DoorFlag(room, side)),
        };
        into.Add(new Door(world, kind, side, x, y, open));
    }

    public static void RevealHiddenChests(PlayScene world, Room room, EntityList<Actor> into)
    {
        int index = 0;
        foreach (var place in room.Placements)
        {
            if (place.Marker is not (Marker.Chest or Marker.HiddenChest)) continue;
            int i = index++;
            if (place.Marker != Marker.HiddenChest) continue;
            var feet = room.TileToWorld(place.X + 0.5f, place.Y + 0.85f);
            if (into.OfType<Chest>().Any(c => c.Index == i)) continue;
            world.Spawn(new Chest(world, feet, i, room.Def.Chests[i], world.Progress.Has(ChestFlag(room, i))));
            world.Puff(feet + new Vector2(0, -6), big: true);
        }
    }
}
