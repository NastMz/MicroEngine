using System.Numerics;
using Arch.Core;
using MicroEngine;
using MicroEngine.Ecs;
using MicroEngine.Graphics;
using MicroEngine.Kernel;
using MicroEngine.Scenes;

namespace EcsEffects;

/// <summary>
/// The arena. Its objects are rows of the scene's component store; the scene says which systems act on them and in
/// which order, takes the player's input, and draws.
/// </summary>
public sealed class ArenaScene : Scene
{
    public const float CasterSpeed = 80, BoltSpeed = 150;
    public const int WalkerHealth = 10, CrateHealth = 12;

    private EcsStore _ecs = null!;
    private EcsInterpolation<Position> _places = null!;
    private int _fired;

    /// <summary>The scene's component store.</summary>
    public EcsStore Ecs => _ecs;
    /// <summary>Where the player's caster is.</summary>
    public Vector2 Caster { get; set; }
    /// <summary>What the next bolt carries.</summary>
    public Element Loaded { get; private set; } = Element.Fire;

    #region scene
    public override void Enter()
    {
        // The store lives with the scene. Its systems run in the order written here, every step: the ones in Systems
        // before this scene's Update, the ones in LateSystems after it.
        _ecs = this.AddEcs(Rules.Components);
        _ecs.Systems.Add(new Movement());
        _ecs.Systems.Add(new Hits());
        _ecs.Systems.Add(new Effects());
        _ecs.LateSystems.Add(new Deaths());
        _places = _ecs.Interpolated<Position>();

        Caster = new Vector2(Game.Width / 2f, Game.Height - 12);
        // Two rows of crates with a gap, a shielded guard in the gap, another one to the left, and walkers above.
        for (int i = 0; i < 6; i++) AddCrate(new Vector2(Game.Width / 2f + (i < 3 ? -42 + i * 14 : -28 + i * 14), 96));
        AddWalker(new Vector2(Game.Width / 2f, 84), Vector2.Zero, shielded: true);
        AddWalker(new Vector2(Game.Width / 2f - 60, 60), Vector2.Zero);
        for (int i = 0; i < 6; i++) AddWalker(new Vector2(30 + i * 50, 30 + i % 2 * 14), new Vector2(i % 2 == 0 ? 22 : -18, 6), shielded: i % 3 == 0);
        // Walkers keep coming: one of the scene's timers adds a row every few seconds.
        Timers.Every(4f, () => AddWalker(new Vector2(8, 30 + Game.Random.Next(80)), new Vector2(18 + Game.Random.Next(12), 5), shielded: Game.Random.Next(3) == 0));
    }
    #endregion

    #region spawn
    // A kind of thing is a choice of components. A crate has no velocity, so nothing ever moves it; it has neither
    // Freezes nor Shieldable, so ice and shields pass it by. No system mentions crates to leave them out.
    public Entity AddWalker(Vector2 at, Vector2 velocity, bool shielded = false)
    {
        var row = _ecs.World.Create(new Position { Value = at }, new Velocity { Value = velocity }, new Health { Value = WalkerHealth }, new Freezes(), new Shieldable());
        if (shielded) _ecs.World.Add<Shielded>(row);
        return row;
    }

    public Entity AddCrate(Vector2 at) => _ecs.World.Create(new Position { Value = at }, new Health { Value = CrateHealth }, new Crate());

    public Entity Shoot(Vector2 from, Vector2 velocity, int damage, Element element) => _ecs.World.Create(
        new Position { Value = from }, new Velocity { Value = velocity }, new Shot { Damage = damage, Life = Rules.ShotLife, Fired = _fired++, Element = element });
    #endregion

    public override void Update(float dt)
    {
        var moved = Caster + Input.Vector("left", "right", "up", "down") * CasterSpeed * dt;
        Caster = Vector2.Clamp(moved, new Vector2(6, 18), new Vector2(Game.Width - 6, Game.Height - 6));
        if (Input.Pressed("cancel")) Loaded = (Element)(((int)Loaded + 1) % 3);
        if (Input.Pressed("confirm")) Shoot(Caster + new Vector2(0, -6), new Vector2(0, -BoltSpeed), 2, Loaded);
    }

    #region question
    private static readonly QueryDescription AlightAndOpen = new QueryDescription().WithAll<Position, Burning>().WithNone<Shielded>();

    /// <summary>
    /// Everything burning within <paramref name="radius"/> of <paramref name="center"/> that has no shield: walkers
    /// and crates alike. The question is a description of the rows wanted, and only those rows are walked.
    /// </summary>
    public int BurningNear(Vector2 center, float radius, List<Entity>? found = null)
    {
        int count = 0;
        foreach (ref var chunk in _ecs.World.Query(in AlightAndOpen))
        {
            var positions = chunk.GetSpan<Position>();
            for (int i = 0; i < chunk.Count; i++)
            {
                if (Vector2.DistanceSquared(positions[i].Value, center) >= radius * radius) continue;
                found?.Add(chunk.Entity(i));
                count++;
            }
        }
        return count;
    }
    #endregion

    #region draw
    private static readonly QueryDescription Things = new QueryDescription().WithAll<Position, Health>();
    private static readonly QueryDescription Bolts = new QueryDescription().WithAll<Position, Shot>();

    public override void Draw(IRenderer r)
    {
        var world = _ecs.World;
        bool flicker = (int)(Age * 12) % 2 == 0;
        foreach (ref var chunk in world.Query(in Things))
        {
            var positions = chunk.GetSpan<Position>();
            // What a chunk's rows have is the same for all of them: asked once, not per row.
            bool crate = chunk.Has<Crate>(), frozen = chunk.Has<Frozen>(), burning = chunk.Has<Burning>(), shielded = chunk.Has<Shielded>();
            for (int i = 0; i < chunk.Count; i++)
            {
                // The store kept each position from the start of the step: a row is drawn between that and where it is.
                var at = _places.Blended(chunk.Entity(i), in positions[i]).Value;
                var body = RectF.FromCenter(at, crate ? 10 : 7, crate ? 10 : 7);
                r.FillRect(body, frozen ? Ice : crate ? Wood : Walker);
                if (shielded) r.DrawRect(RectF.FromCenter(at, 11, 11), Color.White);
                if (burning) r.FillCircle(at + new Vector2(0, flicker ? -6 : -7), flicker ? 2.5f : 2, Flame);
                if (burning && !shielded && Vector2.DistanceSquared(at, Caster) < Rules.NearRadius * Rules.NearRadius) r.DrawRect(RectF.FromCenter(at, 15, 15), Flame);
            }
        }
        foreach (ref var chunk in world.Query(in Bolts))
        {
            var positions = chunk.GetSpan<Position>();
            var shots = chunk.GetSpan<Shot>();
            for (int i = 0; i < chunk.Count; i++)
                r.FillCircle(_places.Blended(chunk.Entity(i), in positions[i]).Value, 1.5f, Tint(shots[i].Element));
        }
        r.FillRect(RectF.FromCenter(Caster, 9, 5), Tint(Loaded));
        r.FillRect(RectF.FromCenter(Caster + new Vector2(0, -4), 3, 4), Tint(Loaded));

        var deaths = _ecs.LateSystems.Get<Deaths>();
        r.FillRect(new RectF(0, 0, r.Width, 12), Color.Black.WithAlpha(0.6f));
        Font.Draw(r, $"Z: SHOOT {Loaded.ToString().ToUpperInvariant()}   X: SWITCH   KILLS {deaths.Kills}   CRATES {deaths.CratesBroken}   BURNING NEAR {BurningNear(Caster, Rules.NearRadius)}", new Vector2(4, 3), Color.White);
    }

    private static Color Tint(Element element) => element switch { Element.Fire => Flame, Element.Ice => Ice, _ => Color.Hex("f4f4f4") };

    private static readonly Color Walker = Color.Hex("b13e53"), Wood = Color.Hex("a66a3a"), Ice = Color.Hex("73eff7"), Flame = Color.Hex("ffcd75");
    #endregion

    // The store hashes its rows and its systems; the scene adds what it keeps itself.
    public override void HashState(StateHasher hash)
    {
        hash.Add(Caster);
        hash.Add((int)Loaded);
        hash.Add(_fired);
    }
}
