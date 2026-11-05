using System.Numerics;
using CourtyardKey;
using CourtyardKey.Content;
using CourtyardKey.Entities;
using CourtyardKey.Model;
using CourtyardKey.Scenes;
using CourtyardKey.World;
using MicroEngine.Core;
using MicroEngine.Input;
using MicroEngine.World;

[assembly: Microsoft.VisualStudio.TestTools.UnitTesting.Parallelize(Scope = Microsoft.VisualStudio.TestTools.UnitTesting.ExecutionScope.MethodLevel)]

namespace CourtyardKey.Tests;

/// <summary>
/// Runs the real game on the headless platform with a private save folder and drives it through
/// the keyboard, exactly as a player would. Every helper fails loudly when it runs out of time.
/// </summary>
internal sealed class Harness : IDisposable
{
    public string SaveDir { get; } = Path.Combine(Path.GetTempPath(), "courtyard-tests", Guid.NewGuid().ToString("N"));
    public CourtyardKeyGame Game { get; }
    public HeadlessPlatform Platform { get; }

    /// <param name="start">The debug start, or null for the title.</param>
    /// <param name="updateRate">Updates per second the game is simulated at (the game's 60 by default).</param>
    public Harness(string? start = null, int updateRate = 60)
    {
        Game = new CourtyardKeyGame(SaveDir, start, updateRate);
        Platform = HeadlessPlatform.For(Game.Settings);
        Game.InitializeForTest(Platform);
    }

    public PlayScene Play => Game.Play ?? throw new InvalidOperationException("No adventure is running.");

    public void Step(int frames = 1)
    {
        for (int i = 0; i < frames; i++) Game.StepForTest();
    }

    public void Press(Key key, int hold = 2)
    {
        Platform.Keys.Set(key, true);
        Step(hold);
        Platform.Keys.Set(key, false);
        Step(2);
    }

    public void Hold(Key key, int frames)
    {
        Platform.Keys.Set(key, true);
        Step(frames);
        Platform.Keys.Set(key, false);
    }

    public bool DialogueOpen => Game.Scenes.Current is DialogueScene;

    /// <summary>Presses confirm until no dialogue remains (and the world is playable again).</summary>
    public void ClearDialogue(int maxFrames = 3000)
    {
        for (int i = 0; i < maxFrames; i += 4)
        {
            if (!DialogueOpen && Play.Mode == PlayMode.Play) return;
            if (DialogueOpen) Press(Key.Z); else Step(2);
        }
        throw new TimeoutException("Dialogue never finished.");
    }

    /// <summary>Steers with the arrow keys toward a world point (feet position).</summary>
    public void WalkTo(Vector2 target, int maxFrames = 900, float tolerance = 2f)
    {
        for (int i = 0; i < maxFrames; i++)
        {
            var d = target - Play.Hero.Position;
            if (d.Length() <= tolerance) { ReleaseArrows(); return; }
            Platform.Keys.Set(Key.Left, d.X < -1);
            Platform.Keys.Set(Key.Right, d.X > 1);
            Platform.Keys.Set(Key.Up, d.Y < -1);
            Platform.Keys.Set(Key.Down, d.Y > 1);
            Step();
            if (DialogueOpen) ClearDialogue();
        }
        ReleaseArrows();
        throw new TimeoutException($"Could not reach {target}; hero at {Play.Hero.Position} in room {Play.Room.Key}.");
    }

    /// <summary>Walks to a tile position (room-local tile coordinates of the feet).</summary>
    public void WalkToTile(float tx, float ty, int maxFrames = 900) => WalkTo(Play.Room.TileToWorld(tx, ty), maxFrames);

    /// <summary>Holds a direction until the hero is in a different room (scroll finished).</summary>
    public void WalkIntoNextRoom(Key key, int maxFrames = 600)
    {
        string from = Play.Room.Key;
        var lane = Play.Hero.Position;
        bool horizontal = key is Key.Left or Key.Right;
        for (int i = 0; i < maxFrames && (Play.Room.Key == from || Play.Mode != PlayMode.Play); i++)
        {
            // Hold the direction while keeping to the starting lane (knockback can push the hero aside).
            var d = lane - Play.Hero.Position;
            ReleaseArrows();
            Platform.Keys.Set(key, true);
            if (horizontal && Play.Mode == PlayMode.Play) { Platform.Keys.Set(Key.Up, d.Y < -2); Platform.Keys.Set(Key.Down, d.Y > 2); }
            if (!horizontal && Play.Mode == PlayMode.Play) { Platform.Keys.Set(Key.Left, d.X < -2); Platform.Keys.Set(Key.Right, d.X > 2); }
            Step();
            if (DialogueOpen) ClearDialogue();
        }
        ReleaseArrows();
        if (Play.Room.Key == from) throw new TimeoutException($"Stayed in {from}; hero at {Play.Hero.Position}.");
        Step(2);
    }

    public void WaitUntil(Func<bool> condition, int maxFrames = 1200, string what = "condition")
    {
        for (int i = 0; i < maxFrames; i++)
        {
            if (condition()) return;
            Step();
        }
        throw new TimeoutException($"Timed out waiting for {what}.");
    }

    // ------------------------------------------------------------------ pathing and fighting bot

    /// <summary>Breadth-first path over walkable room tiles (solid actors block), then walks it cell by cell.</summary>
    public void GoToTile(int tx, int ty, int maxFrames = 2400)
    {
        var path = FindPath(tx, ty) ?? throw new InvalidOperationException($"No path to {tx},{ty} in {Play.Room.Key} from {HeroTile()}.");
        foreach (var (x, y) in path) WalkTo(Play.Room.TileToWorld(x + 0.5f, y + 0.7f), maxFrames, 3f);
    }

    public (int X, int Y) HeroTile() => Play.Room.WorldToTile(Play.Hero.Position + new Vector2(0, -4));

    private List<(int X, int Y)>? FindPath(int tx, int ty)
    {
        var room = Play.Room;
        var blocked = new bool[Room.W, Room.Hgt];
        for (int y = 0; y < Room.Hgt; y++)
            for (int x = 0; x < Room.W; x++)
            {
                var f = TileFlagsTable.For(room.TileAt(x, y));
                blocked[x, y] = (f & (TileFlags.Solid | TileFlags.Water)) != 0;
            }
        foreach (var a in Play.Actors)
            if (a.IsSolid && a is not Door)
            {
                var b = a.SolidBox;
                var (x0, y0) = room.WorldToTile(b.Position);
                var (x1, y1) = room.WorldToTile(new Vector2(b.Right - 0.1f, b.Bottom - 0.1f));
                for (int y = y0; y <= y1; y++) for (int x = x0; x <= x1; x++) if (room.Tiles.InBounds(x, y)) blocked[x, y] = true;
            }
        var start = HeroTile();
        var prev = new Dictionary<(int, int), (int, int)>();
        var queue = new Queue<(int X, int Y)>([start]);
        prev[start] = start;
        while (queue.Count > 0)
        {
            var c = queue.Dequeue();
            if (c == (tx, ty))
            {
                var path = new List<(int, int)>();
                for (var p = c; p != start; p = prev[p]) path.Add(p);
                path.Reverse();
                return path;
            }
            foreach (var (dx, dy) in new[] { (1, 0), (-1, 0), (0, 1), (0, -1) })
            {
                var n = (c.X + dx, c.Y + dy);
                if (n.Item1 < 0 || n.Item2 < 0 || n.Item1 >= Room.W || n.Item2 >= Room.Hgt || prev.ContainsKey(n)) continue;
                if (blocked[n.Item1, n.Item2] && n != (tx, ty)) continue;
                prev[n] = c;
                queue.Enqueue(n);
            }
        }
        return null;
    }

    /// <summary>Turns toward a direction with a one-frame tap (blocked taps still turn the hero).</summary>
    public void Face(Dir dir)
    {
        var key = dir switch { Dir.Up => Key.Up, Dir.Down => Key.Down, Dir.Left => Key.Left, _ => Key.Right };
        ReleaseArrows();
        Platform.Keys.Set(key, true);
        Step();
        Platform.Keys.Set(key, false);
        Step();
    }

    public void Swing()
    {
        Press(Key.Z);
        Step(14);
    }

    public void ThrowBoomerang(Dir dir)
    {
        Face(dir);
        Press(Key.C);
        WaitUntil(() => Play.ActiveBoomerang is null, 240, "the boomerang to return");
    }

    /// <summary>Fights every enemy in the room until none remain (or the room clears its ambush).</summary>
    public void ClearRoom(int maxFrames = 9000)
    {
        int frames = 0;
        while (Play.Actors.OfType<Enemy>().FirstOrDefault(e => e is not Warden) is { } enemy)
        {
            if (frames > maxFrames) throw new TimeoutException($"Could not clear {Play.Room.Key}; {enemy.GetType().Name} at {enemy.Position} stun {enemy.Stun}; mode {Play.Mode}; hero {Play.Hero.Position} {Play.Hero.State}; scenes {string.Join(",", Game.Scenes.Stack.Select(s => s.GetType().Name))}");
            frames += FightStep(enemy);
            if (DialogueOpen) ClearDialogue();
        }
        ReleaseArrows();
    }

    /// <summary>One short burst of attacking the target. Returns frames spent.</summary>
    private int FightStep(Enemy e)
    {
        var hero = Play.Hero;
        bool armoured = e is Knight k && !k.Stunned || e is Bat && Play.Progress.HasBoomerang;
        var toHero = hero.Center - e.Center;
        // Knights must be stunned first (or flanked); everything else is struck from wherever we stand.
        if (armoured && Play.Progress.HasBoomerang)
        {
            var aim = e.Center - hero.Center;
            if (MathF.Abs(aim.X) < 7 || MathF.Abs(aim.Y) < 7)
            {
                ThrowBoomerang(DirExtensions.FromVector(aim));
                return 30;
            }
            var spot = MathF.Abs(aim.X) < MathF.Abs(aim.Y) ? new Vector2(e.Center.X, hero.Center.Y) : new Vector2(hero.Center.X, e.Center.Y);
            return SteerFor(spot + new Vector2(0, 4), 12);
        }
        var reach = e.Hurtbox;
        var swingDir = DirExtensions.FromVector(reach.Center - hero.Center);
        if (!(e is Knight { Stunned: false }) && SwordReach(swingDir, hero.Position).Intersects(reach))
        {
            ReleaseArrows();
            Face(swingDir);
            Swing();
            return 18;
        }
        Dir side = e is Knight { Stunned: false } knight
            ? (knight.Facing.IsHorizontal() ? (toHero.Y < 0 ? Dir.Up : Dir.Down) : (toHero.X < 0 ? Dir.Left : Dir.Right))
            : DirExtensions.FromVector(toHero);
        var target = e.Center + side.ToVector() * 13 + new Vector2(0, 4);
        float distance = (hero.Position - target).Length();
        if (distance > 28) return PathToward(target);
        if (distance > 5) return SteerFor(target, 10);
        ReleaseArrows();
        Face(side.Opposite());
        Swing();
        return 18;
    }

    /// <summary>Where the sword would hit when swung from <paramref name="feet"/> (mirrors the hero's swing box).</summary>
    private static MicroEngine.RectF SwordReach(Dir facing, Vector2 feet) => facing switch
    {
        Dir.Down => new(feet.X - 8, feet.Y - 6, 16, 18),
        Dir.Up => new(feet.X - 8, feet.Y - 28, 16, 18),
        Dir.Right => new(feet.X + 2, feet.Y - 16, 17, 16),
        _ => new(feet.X - 19, feet.Y - 16, 17, 16),
    };

    /// <summary>Takes the first steps of a tile path toward a far target (walls and statues in the way).</summary>
    private int PathToward(Vector2 target)
    {
        var (tx, ty) = Play.Room.WorldToTile(target + new Vector2(0, -4));
        var path = Play.Room.Tiles.InBounds(tx, ty) ? FindPath(tx, ty) : null;
        if (path is null || path.Count == 0) return SteerFor(target, 10);
        var (x, y) = path[0];
        var cell = Play.Room.TileToWorld(x + 0.5f, y + 0.7f);
        return SteerFor(cell, Math.Max(4, Math.Min(20, (int)((cell - Play.Hero.Position).Length() / 1.4f))));
    }

    public int SteerFor(Vector2 target, int frames)
    {
        for (int i = 0; i < frames; i++)
        {
            var d = target - Play.Hero.Position;
            Platform.Keys.Set(Key.Left, d.X < -1);
            Platform.Keys.Set(Key.Right, d.X > 1);
            Platform.Keys.Set(Key.Up, d.Y < -1);
            Platform.Keys.Set(Key.Down, d.Y > 1);
            Step();
        }
        ReleaseArrows();
        return frames;
    }

    /// <summary>Opens the chest at a tile: approach from below, face up, press confirm, read the text.</summary>
    public void OpenChestAt(int tx, int ty)
    {
        GoToTile(tx, ty + 1);
        WalkTo(Play.Room.TileToWorld(tx + 0.5f, ty + 1.6f), 300, 2f);
        Face(Dir.Up);
        Press(Key.Z);
        WaitUntil(() => DialogueOpen, 300, "the treasure caption");
        ClearDialogue();
    }

    public void ReleaseArrows()
    {
        foreach (var k in new[] { Key.Left, Key.Right, Key.Up, Key.Down }) Platform.Keys.Set(k, false);
    }

    public void Dispose()
    {
        try { if (Directory.Exists(SaveDir)) Directory.Delete(SaveDir, true); } catch (IOException) { }
    }
}
