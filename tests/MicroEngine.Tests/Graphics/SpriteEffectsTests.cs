using System.Numerics;
using MicroEngine.Core;
using MicroEngine.Graphics;
using MicroEngine.Kernel;
using MicroEngine.Scenes;
using MicroEngine.Testing;

namespace MicroEngine.Tests;

/// <summary>
/// One-shot sprite effects: "play this clip once here, after a delay, mirrored, tinted, then go away"
/// (<see cref="SpriteEffects"/>, <c>Scene.Effects</c>).
/// </summary>
[TestClass]
public sealed class SpriteEffectsTests
{
    private readonly record struct Drawn(RectF Source, Vector2 Position, Color Tint, bool FlipX, int Layer, float Depth);

    /// <summary>Remembers what is drawn in a list it reuses.</summary>
    private sealed class Recording : IRenderer
    {
        public readonly List<Drawn> Draws = new(64);
        public int Width => 320;
        public int Height => 180;
        public void Clear(Color color) { }
        public void Draw(Texture texture, RectF source, Vector2 position, Color tint, in DrawOptions options = default) => Draws.Add(new(source, position, tint, options.FlipX, 0, 0));
        public void DrawStretched(Texture texture, RectF source, RectF destination, Color tint) { }
        public void FillRect(RectF rect, Color color) { }
        public void FillCircle(Vector2 center, float radius, Color color) { }
        public void FillTriangle(Vector2 a, Vector2 b, Vector2 c, Color color) { }
        public void DrawLine(Vector2 from, Vector2 to, float thickness, Color color) { }
        public void PushCamera(Camera2D camera) { }
        public void PopCamera() { }
        public void SetBlendMode(BlendMode mode) { }
        public void SetClip(RectF? screenRect) { }
        public void PushState(in DrawState state) { }
        public void PopState() { }
        public void PushTarget(RenderTarget2D target) { }
        public void PopTarget() { }
    }

    /// <summary>Trail Duel's own player of effects (games/TrailDuel/Scenes/SpriteFx.cs), rule for rule.</summary>
    private sealed class SpriteFx
    {
        private sealed class Playing
        {
            public required IReadOnlyList<Sprite> Frames;
            public Vector2 At;
            public float Time, Delay, Fps;
            public bool FlipX;
            public Color Tint;
        }

        private readonly List<Playing> _playing = [];
        public bool Busy => _playing.Count > 0;

        public void Play(AnimationClip clip, Vector2 at, float? fps = null, float delay = 0, bool flipX = false, Color? tint = null) =>
            _playing.Add(new Playing { Frames = clip.Frames, At = at, Fps = fps ?? clip.FramesPerSecond, Delay = delay, FlipX = flipX, Tint = tint ?? Color.White });

        public void Update(float dt)
        {
            foreach (var p in _playing)
            {
                if (p.Delay > 0) p.Delay -= dt;
                else p.Time += dt;
            }
            _playing.RemoveAll(p => p.Time * p.Fps >= p.Frames.Count);
        }

        public void Draw(IRenderer r)
        {
            foreach (var p in _playing)
            {
                if (p.Delay > 0) continue;
                var f = p.Frames[Math.Min(p.Frames.Count - 1, (int)(p.Time * p.Fps))];
                var at = new Vector2(MathF.Round(p.At.X), MathF.Round(p.At.Y));
                r.Draw(f.Texture, f.Source, at - (p.FlipX ? new Vector2(f.Width - f.Origin.X, f.Origin.Y) : f.Origin), p.Tint, new DrawOptions(FlipX: p.FlipX));
            }
        }
    }

    private static SpriteAtlas Atlas()
    {
        var builder = new SpriteAtlasBuilder();
        builder.AddFrames("puff", Enumerable.Range(0, 4).Select(i => new PixelImage(6 + i, 6)), 10, new Vector2(3, 6), loop: false);
        builder.AddFrames("spark", Enumerable.Range(0, 3).Select(i => new PixelImage(4, 4 + i)), 20, new Vector2(2, 2), loop: false);
        return builder.Build(new HeadlessGraphicsDevice());
    }

    private static string? RepositoryRoot()
    {
        for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir is not null; dir = dir.Parent)
            if (File.Exists(Path.Combine(dir.FullName, "MicroEngine.slnx"))) return dir.FullName;
        return null;
    }

    [TestMethod]
    public void The_effects_of_a_battle_show_the_same_frames_on_the_same_updates_as_the_games_own_player()
    {
        if (RepositoryRoot() is not { } root) { Assert.Inconclusive("the repository's games are not beside this build"); return; }
        var sheet = SpriteSheet.Load(Path.Combine(root, "games", "TrailDuel", "Content", "fx", "fx.sheet"));
        using var atlas = new SpriteAtlasBuilder().AddSheet(sheet).Build(new HeadlessGraphicsDevice());
        AnimationClip Fx(string name) => atlas.Clip("fx:" + name);
        var feet = new Vector2(212.4f, 77.6f);
        var at = new Vector2(96.5f, 110.25f);
        var teal = new Color(40, 200, 180);

        // The plays of BattleScene.Present.cs, on the updates a battle makes them: an attack, its echo, a boost, a faint.
        var plays = new (int Update, string Name, Vector2 At, float? Fps, float Delay, bool FlipX, Color? Tint)[]
        {
            (0, "spark-ring", at, 20, 0, false, null),
            (3, "thunder", feet + new Vector2(-7, -6), null, 0, false, null),
            (3, "thunder", feet + new Vector2(8, -2), null, 0.08f, true, null),
            (3, "spark-ring", at, null, 0.12f, false, null),
            (40, "leaves", at, null, 0, true, null),
            (40, "leaves", at + new Vector2(6, 4), null, 0.1f, false, null),
            (41, "aura", feet + new Vector2(0, -4), null, 0, false, teal),
            (70, "boost", feet + new Vector2(0, -2), null, 0, false, null),
            (70, "sparkle", at, null, 0, false, null),
            (95, "smoke", feet + new Vector2(0, -8), null, 0.15f, false, null),
            (96, "explosion", feet, null, 0, true, null),
            (130, "spark-ring", at + new Vector2(0, -4), 16, 0, false, null),
            (131, "flame", feet, null, 0, false, null),
            (131, "geyser", feet, null, 0, true, null),
            (131, "rock-spike", feet, null, 0.05f, false, null),
            (131, "gale", at, null, 0.2f, true, teal),
        };

        var theirs = new SpriteFx();
        var ours = new SpriteEffects { SnapToPixels = true };
        var expected = new Recording();
        var actual = new Recording();
        int shown = 0, busy = 0;
        for (int update = 0; update < 300; update++)
        {
            foreach (var p in plays)
            {
                if (p.Update != update) continue;
                theirs.Play(Fx(p.Name), p.At, p.Fps, p.Delay, p.FlipX, p.Tint);
                ours.Play(Fx(p.Name), p.At, delay: p.Delay, flipX: p.FlipX, tint: p.Tint, framesPerSecond: p.Fps);
            }
            theirs.Update(1 / 60f);
            ours.Update(1 / 60f);
            Assert.AreEqual(theirs.Busy, ours.Busy, $"update {update}: busy");
            expected.Draws.Clear();
            actual.Draws.Clear();
            theirs.Draw(expected);
            ours.Draw(actual);
            CollectionAssert.AreEqual(expected.Draws, actual.Draws, $"update {update}: the frames drawn, where, mirrored and tinted");
            shown += actual.Draws.Count;
            if (ours.Busy) busy++;
        }
        Assert.IsTrue(shown > 400, $"the effects were drawn: {shown}");
        Assert.IsTrue(busy > 100 && !ours.Busy, "they played and they all ended");
    }

    [TestMethod]
    public void An_effect_waits_its_delay_plays_once_and_is_gone()
    {
        using var atlas = Atlas();
        var effects = new SpriteEffects();
        var r = new Recording();
        var puff = effects.Play(atlas.Clip("puff"), new Vector2(50.4f, 20), delay: 0.1f);
        Assert.AreEqual(1, effects.Count);
        Assert.IsTrue(effects.IsPlaying(puff));
        effects.Draw(r);
        Assert.AreEqual(0, r.Draws.Count, "nothing shows during the delay");

        effects.Update(0.125f);                      // the delay ends
        effects.Draw(r);
        Assert.AreEqual(new Drawn(atlas["puff0"].Source, new Vector2(50.4f, 20) - new Vector2(3, 6), Color.White, false, 0, 0), r.Draws.Single(), "the first frame, anchored, where it was played");
        effects.Update(0.25f);                       // 0.25 s at 10 frames a second: the third frame
        r.Draws.Clear();
        effects.Draw(r);
        Assert.AreEqual(atlas["puff2"].Source, r.Draws.Single().Source);
        effects.Update(0.25f);                       // 0.5 s: past its four frames
        Assert.AreEqual(0, effects.Count);
        Assert.IsFalse(effects.Busy);
        Assert.IsFalse(effects.IsPlaying(puff));
        Assert.IsFalse(effects.IsPlaying(default), "no effect");
    }

    [TestMethod]
    public void An_effect_is_mirrored_around_its_anchor_tinted_played_at_another_rate_moved_and_stopped()
    {
        using var atlas = Atlas();
        var effects = new SpriteEffects();
        var r = new Recording();
        var red = new Color(255, 0, 0);
        var spark = effects.Play(atlas.Clip("spark"), new Vector2(10, 10), flipX: true, tint: red, framesPerSecond: 2);
        var other = effects.Play(atlas.Clip("puff"), new Vector2(80, 80));
        effects.Update(0.6f);                        // 0.6 s at 2 frames a second: the second frame; the puff (0.4 s) is over
        Assert.IsFalse(effects.IsPlaying(other));
        Assert.IsTrue(effects.IsPlaying(spark));
        effects.Draw(r);
        var frame = atlas["spark1"];
        Assert.AreEqual(new Drawn(frame.Source, new Vector2(10, 10) - new Vector2(frame.Width - 2, 2), red, true, 0, 0), r.Draws.Single());

        Assert.IsTrue(effects.Move(spark, new Vector2(40, 12)), "an effect follows whatever the game moves it to");
        r.Draws.Clear();
        effects.Draw(r);
        Assert.AreEqual(new Vector2(40, 12) - new Vector2(frame.Width - 2, 2), r.Draws.Single().Position);

        Assert.IsTrue(effects.Stop(spark));
        Assert.IsFalse(effects.Stop(spark), "already gone");
        Assert.IsFalse(effects.Move(spark, default));
        Assert.AreEqual(0, effects.Count);

        effects.Play(atlas.Clip("puff"), default);
        effects.Play(atlas.Clip("spark"), default);
        effects.Clear();
        Assert.AreEqual(0, effects.Count);
        Assert.ThrowsExactly<ArgumentNullException>(() => effects.Play(null!, default));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => effects.Play(atlas.Clip("puff"), default, framesPerSecond: 0));
        effects.Play(atlas.Clip("puff"), default, delay: float.NaN);
        effects.Update(0.01f);
        r.Draws.Clear();
        effects.Draw(r);
        Assert.AreEqual(1, r.Draws.Count, "a delay that is not a number is none");
    }

    [TestMethod]
    public void A_clip_with_a_duration_per_frame_plays_each_frame_for_its_time_and_a_looping_clip_plays_one_pass()
    {
        using var atlas = Atlas();
        var frames = atlas.Clip("puff").Frames.Take(3).ToArray();
        var timed = new AnimationClip(frames, [0.125f, 0.375f, 0.125f], loop: true);   // sums no step of 1/16 s rounds
        var effects = new SpriteEffects();
        var r = new Recording();
        effects.Play(timed, default);
        var seen = new List<RectF>();
        for (int i = 0; i < 12; i++)
        {
            effects.Update(0.0625f);
            r.Draws.Clear();
            effects.Draw(r);
            if (r.Draws.Count > 0) seen.Add(r.Draws[0].Source);
        }
        Assert.AreEqual(0, effects.Count, "one pass of 0.625 s, although the clip loops");
        Assert.AreEqual(1, seen.Count(s => s == frames[0].Source), "0.0625 s");
        Assert.AreEqual(6, seen.Count(s => s == frames[1].Source), "0.125 to 0.4375 s");
        Assert.AreEqual(2, seen.Count(s => s == frames[2].Source), "0.5 and 0.5625 s");
        Assert.AreEqual(9, seen.Count);
    }

    [TestMethod]
    public void Effects_draw_in_the_order_played_or_at_their_layer_and_depth_in_a_sorted_block()
    {
        using var atlas = Atlas();
        var effects = new SpriteEffects();
        effects.Play(atlas.Clip("puff"), new Vector2(10, 0), layer: 2, depth: 5);
        effects.Play(atlas.Clip("spark"), new Vector2(20, 0), layer: 1, depth: 9);
        effects.Play(atlas.Clip("spark"), new Vector2(30, 0), layer: 2, depth: 1);
        var r = new Recording();
        effects.Draw(r);
        CollectionAssert.AreEqual(new float[] { 7, 18, 28 }, r.Draws.Select(d => d.Position.X).ToArray(), "a plain renderer: the order they were played in");

        r.Draws.Clear();
        var sorted = new SortedRenderer();
        sorted.Begin(r).At(7, 3);
        effects.Draw(sorted);
        Assert.AreEqual(7, sorted.Layer, "the block's layer and depth are as they were");
        Assert.AreEqual(3f, sorted.Depth);
        sorted.End();
        CollectionAssert.AreEqual(new float[] { 18, 28, 7 }, r.Draws.Select(d => d.Position.X).ToArray(), "sorted: layer 1, then layer 2 by depth");
    }

    [TestMethod]
    public void Playing_and_drawing_effects_makes_no_garbage_once_the_pool_is_large_enough()
    {
        using var atlas = Atlas();
        var puff = atlas.Clip("puff");
        var spark = atlas.Clip("spark");
        var effects = new SpriteEffects(capacity: 4);
        var r = new Recording();
        int step = 0;
        void Step()
        {
            step++;
            if (step % 3 == 0) effects.Play(puff, new Vector2(step % 300, 40), delay: 0.05f, flipX: step % 2 == 0);
            if (step % 5 == 0) effects.Play(spark, new Vector2(step % 300, 80), tint: Color.White, framesPerSecond: 30);
            effects.Update(1 / 60f);
            r.Draws.Clear();
            effects.Draw(r);
        }
        AllocationAssert.None(Step, warmup: 300, calls: 600, because: "effects played, stepped and drawn every frame");
        Assert.IsTrue(effects.Count > 4, "the pool grew past its first capacity");
    }

    [TestMethod]
    public void The_state_hash_follows_the_effects()
    {
        using var atlas = Atlas();
        static ulong Hash(SpriteEffects effects)
        {
            var hash = new StateHasher();
            effects.HashState(hash);
            return hash.Value;
        }
        var a = new SpriteEffects();
        var b = new SpriteEffects();
        Assert.AreEqual(Hash(a), Hash(b));
        a.Play(atlas.Clip("puff"), new Vector2(1, 2), delay: 0.1f);
        Assert.AreNotEqual(Hash(a), Hash(b), "an effect playing");
        b.Play(atlas.Clip("puff"), new Vector2(1, 2), delay: 0.1f);
        Assert.AreEqual(Hash(a), Hash(b), "the same effects, the same hash");
        a.Update(1 / 60f);
        Assert.AreNotEqual(Hash(a), Hash(b), "one step apart");
        b.Update(1 / 60f);
        Assert.AreEqual(Hash(a), Hash(b));
    }

    private sealed class TestGame(Scene first) : Game
    {
        public override GameSettings Settings { get; } = new("sprite-effects");
        protected override void Load() => Scenes.Push(first);
    }

    private sealed class Host(AnimationClip clip) : Scene
    {
        public SpriteEffect Played;
        public override void Enter() => Played = Effects.Play(clip, new Vector2(10, 10));
        public override void Update(float dt) { }
        public override void Draw(IRenderer renderer) => Effects.Draw(renderer);
    }

    [TestMethod]
    public void A_scene_steps_its_effects_hashes_them_and_drops_them_when_it_leaves()
    {
        using var atlas = Atlas();
        var scene = new Host(atlas.Clip("puff"));       // 0.4 s
        var game = new TestGame(scene);
        game.InitializeForTest(new HeadlessPlatform());
        Assert.AreEqual(1, scene.Effects.Count);
        var before = game.ComputeStateHash();
        game.StepForTest();
        Assert.AreNotEqual(before, game.ComputeStateHash(), "the effect's time is part of the scene's state: a rule may wait for it");
        Assert.IsTrue(scene.Effects.IsPlaying(scene.Played));
        for (int i = 0; i < 30; i++) game.StepForTest();
        Assert.IsFalse(scene.Effects.Busy, "the scene stepped the effect to its end with no call from the scene's Update");

        var effects = scene.Effects;
        effects.Play(atlas.Clip("puff"), default);
        game.Scenes.Pop();
        game.StepForTest();
        Assert.AreEqual(0, effects.Count, "a scene that leaves takes its effects with it");
    }
}
