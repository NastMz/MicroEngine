using System.Numerics;
using MeadowRun.Art;
using MeadowRun.Content;
using MeadowRun.Gameplay.Actors;
using MicroEngine;

namespace MeadowRun.Gameplay;

internal sealed partial class Stage
{
    // Scratch lists for the per-step rules, reused so a step allocates nothing for them.
    private readonly List<Actor> _touched = [];
    private readonly List<SeedShot> _seeds = [];
    private readonly List<Enemy> _enemies = [], _walkers = [];
    private readonly List<FireBar> _fireBars = [];

    private void SpawnFromLevel(int checkpoint)
    {
        int cpIndex = 0, barIndex = 0;
        foreach (var s in Level.Spawns)
        {
            var at = s.TileTopLeft;
            switch (s.Kind)
            {
                case 'o': Spawn(new Coin(this, at)); break;
                case 'G': Goal = new GoalPole(this, at); Spawn(Goal); break;
                case 'K':
                    cpIndex++;
                    var cp = new Checkpoint(this, at, cpIndex, cpIndex <= checkpoint);
                    _checkpoints.Add(cp);
                    Spawn(cp);
                    break;
                case 'j': Spawn(new Spring(this, at)); break;
                case 'w': Spawn(new Bumblet(this, at)); break;
                case 't': Spawn(new Thornback(this, at)); break;
                case 'h': Spawn(new Hopkin(this, at)); break;
                case 'f': Spawn(new Mothwing(this, at)); break;
                case 'p': Spawn(new Spitpod(this, at)); break;
                case '_': Spawn(new MovingPlatform(this, at, vertical: false)); break;
                case 'I': Spawn(new MovingPlatform(this, at, vertical: true)); break;
                case 'v': Spawn(new FallingPlatform(this, at)); break;
                case 'F': Spawn(new FireBar(this, at, barIndex++ % 2 == 0)); break;
                case '*': Spawn(new LightProp(this, at)); break;
                case 'X':
                    Boss = new Grumbold(this, at) { Arena = FindArena(s) };
                    Spawn(Boss);
                    break;
            }
        }
    }

    /// <summary>The boss arena spans the open floor between the walls left and right of the boss marker.</summary>
    private RectF FindArena(Spawn s)
    {
        // Walls are found a few rows above the floor so the entrance doorway does not count as open.
        int row = Math.Max(0, s.Y - 3), left = s.X, right = s.X;
        while (left > 0 && !Level.IsSolid(left - 1, row)) left--;
        while (right < Level.Width - 1 && !Level.IsSolid(right + 1, row)) right++;
        int floor = s.Y;
        while (floor < Level.Height && !Level.IsSolid(s.X, floor)) floor++;
        float bottom = floor * Level.TileSize;
        return new RectF(left * Level.TileSize, MathF.Max(0, bottom - 11 * Level.TileSize), (right - left + 1) * Level.TileSize, 11 * Level.TileSize);
    }

    private void UpdateBossArena()
    {
        if (Boss is not { } boss || boss.Active) return;
        var arena = boss.Arena;
        if (Player.Bounds.Left < arena.Left + 40) return;
        // Seal the entrance behind Pip, lock the camera and wake the king.
        int gate = (int)(arena.Left / Level.TileSize) - 1;
        for (int y = 0; y < Level.Height; y++)
            if (!Level.IsSolid(gate, y) && y * Level.TileSize < arena.Bottom) Level.SetTile(gate, y, Tile.HardBlock);
        var view = new RectF(arena.Left - Level.TileSize, arena.Bottom + 2 * Level.TileSize - 180, 320, 180);
        LockCamera(view with { X = MathF.Max(0, arena.Center.X - 160) });
        Sfx("gate", 1, 1);
        boss.Wake();
        MusicSink?.Invoke("boss");
    }

    private void ResolveItems()
    {
        var pb = Player.Bounds;
        _touched.Clear();
        foreach (var actor in Actors.Overlapping(pb)) _touched.Add(actor);
        foreach (var actor in _touched)
        {
            switch (actor)
            {
                case Coin coin:
                    coin.Destroy();
                    Session.AddCoin();
                    Sfx("coin", 1, 0.55f, coin.Center);
                    Sparkle(coin.Center, Pal.Gold);
                    break;
                case PowerItem item when !item.Emerging:
                    Collect(item);
                    break;
                case Spring spring when Player.Velocity.Y > 0 && Player.Bounds.Bottom <= spring.Bounds.Top + 8:
                    spring.Compress = 0.18f;
                    Player.Launch(_lastInputJumpHeld ? Tuning.SpringBounceHeld : Tuning.SpringBounce, cuttable: false);
                    Sfx("spring", 1, 0.8f, spring.Center);
                    Dust(spring.Feet, 0, 4);
                    break;
                case Checkpoint cp when !cp.Lit:
                    cp.Lit = true;
                    CheckpointReached = Math.Max(CheckpointReached, cp.Index);
                    Sfx("checkpoint", 1, 0.8f, cp.Center);
                    Burst(cp.Center + new Vector2(0, -12), Pal.Gold, 14);
                    Popup("CHECKPOINT", cp.Center + new Vector2(0, -28), Pal.Butter);
                    break;
                case GoalPole pole when Phase == StagePhase.Playing:
                    StartFlag(pole);
                    break;
                case Sunstone stone when Phase == StagePhase.Playing:
                    stone.Destroy();
                    Phase = StagePhase.Victory;
                    _phaseTime = 0;
                    Player.Controllable = false;
                    Shake(0.4f);
                    Burst(stone.Center, Pal.Gold, 30);
                    MusicSink?.Invoke("victory");
                    Sfx("sunstone", 1, 1);
                    break;
            }
        }
    }

    private bool _lastInputJumpHeld;

    /// <summary>Test hook: enemies never touch Pip (used to prove level geometry is traversable).</summary>
    internal bool IgnoreEnemiesForTesting { get; set; }

    /// <summary>Remembers whether jump was held this step (springs and stomps bounce higher).</summary>
    public void NoteInput(PlayerInput input) => _lastInputJumpHeld = input.JumpHeld;

    private void Collect(PowerItem item)
    {
        item.Destroy();
        Freeze(0.06f);
        switch (item.Kind)
        {
            case PowerKind.LifeSprout:
                Session.AddLife();
                Popup("1UP", item.Center, Pal.Lime);
                return;
            case PowerKind.Sunpetal:
                if (Player.Form == PipForm.Sprout) Player.Form = PipForm.Bloom;
                break;
            case PowerKind.EmberSeed:
                Player.Form = PipForm.Ember;
                break;
        }
        Session.Form = Player.Form;
        Session.AddScore(1000);
        Popup("1000", item.Center, Pal.Butter);
        Sfx("powerup", 1, 0.9f, item.Center);
        Burst(Player.Center, item.Kind == PowerKind.EmberSeed ? Pal.Flame : Pal.Blush, 16);
    }

    private void ResolveEnemies(float previousBottom)
    {
        var pb = Player.Bounds;
        // Reused lists, filled in one pass (in actor order) before any rule runs.
        var seeds = _seeds;
        var enemies = _enemies;
        seeds.Clear();
        enemies.Clear();
        _fireBars.Clear();
        foreach (var actor in Actors)
        {
            switch (actor)
            {
                case SeedShot seed: seeds.Add(seed); break;
                case Enemy e when !e.Dead && e.Awake: enemies.Add(e); break;
                case FireBar bar: _fireBars.Add(bar); break;
            }
        }
        foreach (var enemy in enemies)
        {
            foreach (var seed in seeds)
            {
                if (!seed.Alive || !seed.Bounds.Intersects(enemy.Bounds)) continue;
                if (enemy is Grumbold boss) { if (!boss.Hidden) { boss.SeedHit(); seed.Pop(); } continue; }
                if (enemy is Rock) continue;
                seed.Pop();
                Knock(enemy, MathF.Sign(seed.Velocity.X));
            }
            if (enemy.Dead || !enemy.Bounds.Intersects(pb) || IgnoreEnemiesForTesting) continue;

            bool fromAbove = Player.Velocity.Y > 0 && previousBottom <= enemy.Bounds.Top + Tuning.StompTolerance;
            if (enemy is Grumbold king)
            {
                if (fromAbove && king.Active && !king.Hidden && !king.Defeated)
                {
                    if (king.Vulnerable) { king.Stomped(); RewardStomp(king, silent: true); }
                    else Sfx("clank", 1, 0.8f, king.Center);
                    Bounce(enemy);
                }
                else if (king.Harmful) Hurt("boss");
                continue;
            }
            if (fromAbove && enemy.Stompable)
            {
                enemy.Stomped();
                Bounce(enemy);
                RewardStomp(enemy, silent: false);
                Freeze(0.05f);
                Shake(0.12f);
                continue;
            }
            Hurt(enemy.GetType().Name);
        }

        // Walkers turn when they meet so they do not overlap into a single blob.
        var walkers = _walkers;
        walkers.Clear();
        foreach (var e in enemies) if (e.Walker && !e.Dead) walkers.Add(e);
        for (int i = 0; i < walkers.Count; i++)
        for (int j = i + 1; j < walkers.Count; j++)
        {
            var a = walkers[i]; var b = walkers[j];
            if (!a.Bounds.Intersects(b.Bounds)) continue;
            a.Facing = a.Center.X < b.Center.X ? -1 : 1;
            b.Facing = -a.Facing;
        }

        foreach (var bar in _fireBars)
            if (bar.Touches(pb)) Hurt("firebar");
    }

    private void Bounce(Enemy enemy)
    {
        Player.Position.Y = enemy.Bounds.Top - Player.Height;
        bool held = _lastInputJumpHeld;
        Player.Launch(held ? Tuning.StompBounceHeld : Tuning.StompBounce, cuttable: held);
    }

    private void RewardStomp(Enemy enemy, bool silent)
    {
        string text = Session.RewardStomp(_stompChain);
        Popup(text, enemy.Center + new Vector2(0, -10), text == "1UP" ? Pal.Lime : Pal.Cream);
        if (!silent) Sfx("stomp", 1 + _stompChain * 0.08f, 0.9f, enemy.Center);
        Dust(enemy.Feet, 0, 6);
        _stompChain++;
        StompSink?.Invoke();
    }

    private void Knock(Enemy enemy, float direction)
    {
        if (!enemy.Knockable) return;
        enemy.Knocked(direction >= 0 ? 1 : -1);
        Session.AddScore(enemy.Points);
        Popup(enemy.Points.ToString(System.Globalization.CultureInfo.InvariantCulture), enemy.Center, Pal.Cream);
        Sfx("knock", 1, 0.8f, enemy.Center);
        Burst(enemy.Center, Pal.Flame, 6);
    }

    private void ResolveHazards()
    {
        if (Phase != StagePhase.Playing) return;
        var pb = Player.Bounds;
        if (pb.Top > Level.PixelHeight) { Die("pit"); return; }
        int ts = Level.TileSize;
        for (int y = (int)MathF.Floor(pb.Top / ts); y <= (int)MathF.Floor((pb.Bottom - 0.01f) / ts); y++)
        for (int x = (int)MathF.Floor(pb.Left / ts); x <= (int)MathF.Floor((pb.Right - 0.01f) / ts); x++)
        {
            int id = Level.Tiles[x, y];
            if (id == Tile.Lava && pb.Bottom > y * ts + 6) { Burst(Player.Feet, Pal.Flame, 16); Sfx("sizzle", 1, 1, Player.Center); Die("lava"); return; }
            if (id == Tile.Spikes && pb.Bottom > y * ts + 9 && pb.Right > x * ts + 2 && pb.Left < x * ts + ts - 2)
            {
                if (Player.Invulnerable <= 0) { Hurt("spikes"); if (Phase == StagePhase.Playing) Player.Launch(220, cuttable: false); }
                return;
            }
        }
    }

    /// <summary>Head-bump on a block: coins, power-ups, breaking bricks, and knocking out anything on top.</summary>
    public void BumpBlock(int x, int y)
    {
        int id = Level.Tiles[x, y];
        var top = new Vector2(x * Level.TileSize, y * Level.TileSize);
        bool bumped = true;
        switch (id)
        {
            case Tile.CoinBlock:
                Level.SetTile(x, y, Tile.UsedBlock);
                Spawn(new PopCoin(this, top + new Vector2(8, -6)));
                Session.AddCoin();
                Sfx("coin", 1, 0.55f, top);
                break;
            case Tile.MultiCoinBrick:
            {
                int left = _multiCoins.TryGetValue((x, y), out var n) ? n : 8;
                _multiCoins[(x, y)] = --left;
                Spawn(new PopCoin(this, top + new Vector2(8, -6)));
                Session.AddCoin();
                Sfx("coin", 1 + (8 - left) * 0.02f, 0.55f, top);
                if (left <= 0) Level.SetTile(x, y, Tile.UsedBlock);
                break;
            }
            case Tile.PowerBlock:
                Level.SetTile(x, y, Tile.UsedBlock);
                Spawn(new PowerItem(this, top, Player.Form == PipForm.Sprout ? PowerKind.Sunpetal : PowerKind.EmberSeed));
                Sfx("sprout", 1, 0.8f, top);
                break;
            case Tile.LifeBlock:
            case Tile.HiddenLife:
                Level.SetTile(x, y, Tile.UsedBlock);
                Spawn(new PowerItem(this, top, PowerKind.LifeSprout));
                Sfx("sprout", 1.2f, 0.8f, top);
                break;
            case Tile.Brick when Player.Form != PipForm.Sprout:
                Level.SetTile(x, y, Tile.Empty);
                var c = top + new Vector2(8, 8);
                foreach (var (vx, vy) in new[] { (-70f, -260f), (70f, -260f), (-50f, -170f), (50f, -170f) })
                    Spawn(new Shard(this, c + new Vector2(vx * 0.05f, vy * 0.02f), new Vector2(vx, vy)));
                Session.AddScore(50);
                Sfx("break", 1, 0.9f, top);
                Shake(0.15f);
                bumped = false;
                break;
            case Tile.Brick:
                Sfx("bump", 1, 0.7f, top);
                break;
            default:
                Sfx("thud", 1, 0.5f, top);
                bumped = false;
                break;
        }
        if (bumped) _bumps[(x, y)] = 0;
        if (id == Tile.Brick || bumped) KnockFromBelow(x, y);
    }

    private void KnockFromBelow(int x, int y)
    {
        var area = new RectF(x * Level.TileSize - 2, y * Level.TileSize - 4, Level.TileSize + 4, 6);
        foreach (var actor in Actors.Overlapping(area).ToList())
        {
            switch (actor)
            {
                case Enemy enemy when !enemy.Dead && enemy.Knockable: Knock(enemy, enemy.Center.X >= x * Level.TileSize + 8 ? 1 : -1); break;
                case Coin coin:
                    coin.Destroy();
                    Spawn(new PopCoin(this, coin.Center));
                    Session.AddCoin();
                    break;
                case PowerItem item: item.Pop(); break;
            }
        }
    }
}
