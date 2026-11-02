using System.Numerics;
using MeadowRun.Art;
using MeadowRun.Content;
using MeadowRun.Gameplay;
using MeadowRun.Gameplay.Actors;
using MicroEngine;
using MicroEngine.Graphics;
using MicroEngine.World;

namespace MeadowRun.Scenes;

/// <summary>
/// Draws a <see cref="Stage"/>: parallax backdrop, tiles with autotiling and bump offsets,
/// actors in layers, Pip with squash and stretch, particles, a biome light pass and popups.
/// Holds only presentation state (squash, blink), never gameplay state.
/// </summary>
internal sealed class StageView(Assets art, BitmapFont font, LightMap lights)
{
    private Vector2 _squash = Vector2.One;
    private float _runFrame;
    private string _sky = "";
    private IReadOnlyList<BackdropLayer> _layers = [];
    private string[] _decos = [];
    private Biome _biome;
    private readonly StageMotion _motion = new();
    private readonly List<Actor> _actors = [];
    // Sprite names looked up per tile every frame, built once instead of concatenated each time.
    private static readonly string[] QuestionNames = Names("question", 5), LavaTopNames = Names("lava_top", 4), LavaBodyNames = Names("lava_body", 2);
    private static readonly string[] BackWallNames = Names("backwall", Assets.BackWallVariants);
    private static readonly (string Name, int Dx, int Dy)[] WallFrames = [("wallframe1", 0, -1), ("wallframe2", 1, 0), ("wallframe4", 0, 1), ("wallframe8", -1, 0)];

    private static string[] Names(string prefix, int count) => [.. Enumerable.Range(0, count).Select(i => prefix + i)];

    public void Bind(Stage stage)
    {
        _biome = stage.Level.Biome;
        _sky = "sky_" + Assets.Key(_biome);
        _layers = BackdropArt.Layers(_biome);
        _decos = DecoArt.For(_biome);
    }

    /// <summary>Reads the player's events after each simulation step to drive squash/stretch.</summary>
    public void Observe(Stage stage, float dt)
    {
        var p = stage.Player;
        if ((p.Events & PlayerEvents.Jumped) != 0) _squash = new Vector2(0.72f, 1.3f);
        if ((p.Events & PlayerEvents.Landed) != 0) _squash = new Vector2(1 + 0.35f * MathF.Max(0.4f, p.LandImpact), 1 - 0.3f * MathF.Max(0.4f, p.LandImpact));
        _squash = MathX.Damp(_squash, Vector2.One, 14, dt);
        float speed = MathF.Abs(p.Velocity.X);
        _runFrame += dt * (6 + speed * 0.075f);
    }

    /// <summary>Call just before every stage update, so frames drawn until the next one can place things between.</summary>
    public void Remember(Stage stage) => _motion.Remember(stage);

    /// <summary>Draws the stage <paramref name="blend"/> of the way from the previous update to the current one (1: the current state).</summary>
    public void Draw(IRenderer r, Stage stage, float time, float blend = 1)
    {
        _motion.Place(stage, blend);
        try { DrawStage(r, stage, time); }
        finally { _motion.Restore(stage); }
    }

    private void DrawStage(IRenderer r, Stage stage, float time)
    {
        // One pass over the actors per frame; the layers and the light pass reuse it.
        _actors.Clear();
        foreach (var a in stage.Actors) _actors.Add(a);
        var cam = stage.Camera;
        DrawBackdrop(r, stage, time);
        r.PushCamera(cam);
        DrawTiles(r, stage, time, background: true);
        DrawDecorations(r, stage);
        var platforms = stage.Platforms;
        for (int i = 0; i < platforms.Count; i++) if (cam.IsVisible(platforms[i].Bounds, 16)) platforms[i].Render(r, art, time);
        DrawActors(r, stage, time, layer: 0);
        DrawTiles(r, stage, time, background: false);
        DrawActors(r, stage, time, layer: 1);
        DrawActors(r, stage, time, layer: 2);
        DrawPlayer(r, stage, time);
        DrawActors(r, stage, time, layer: 3);
        stage.Particles.Draw(r);
        r.PopCamera();

        DrawLighting(r, stage, time);

        r.PushCamera(cam);
        stage.Glints.Draw(r);
        var popups = stage.Popups;
        for (int i = 0; i < popups.Count; i++)
        {
            var popup = popups[i];
            var c = popup.Color.WithAlpha(popup.Age < 0.6f ? 1f : 1 - (popup.Age - 0.6f) / 0.3f);
            var size = font.Measure(popup.Text);
            font.DrawOutlined(r, popup.Text, new Vector2(MathF.Round(popup.Position.X - size.X / 2), MathF.Round(popup.Position.Y)), c, Pal.Ink.WithAlpha(c.A));
        }
        r.PopCamera();
    }

    private void DrawBackdrop(IRenderer r, Stage stage, float time)
    {
        r.Draw(art[_sky], Vector2.Zero);
        var cam = stage.Camera;
        float maxY = MathF.Max(0, stage.Level.PixelHeight - 180);
        for (int i = 0; i < _layers.Count; i++)
        {
            var layer = _layers[i];
            var s = art[layer.Sprite];
            float scroll = cam.Position.X * layer.FactorX + time * layer.Drift;
            float x = -(scroll % s.Width);
            if (x > 0) x -= s.Width;
            float y = MathF.Round(layer.Bottom - s.Height + (maxY - cam.Position.Y) * layer.FactorY);
            if (layer.Additive) r.SetBlendMode(BlendMode.Additive);
            for (float px = MathF.Round(x); px < 320; px += s.Width) r.Draw(s, new Vector2(px, y));
            if (layer.Additive) r.SetBlendMode(BlendMode.Alpha);
        }
    }

    private void DrawTiles(IRenderer r, Stage stage, float time, bool background)
    {
        var level = stage.Level;
        var view = stage.Camera.View;
        int x0 = Math.Max(0, (int)(view.X / 16) - 1), x1 = Math.Min(level.Width - 1, (int)(view.Right / 16) + 1);
        int y0 = Math.Max(0, (int)(view.Y / 16) - 1), y1 = Math.Min(level.Height - 1, (int)(view.Bottom / 16) + 1);
        var tiles = level.Tiles;
        float qt = time % 1.4f;
        int questionFrame = qt < 0.4f ? (int)(qt / 0.1f) : 0;
        int lavaFrame = (int)(time * 5) % 4;
        for (int y = y0; y <= y1; y++)
        for (int x = x0; x <= x1; x++)
        {
            int id = tiles[x, y];
            // Cells that held entity markers inside a walled interior still get their back wall.
            if (background && id is Tile.Empty or Tile.HiddenLife && tiles[x - 1, y] == Tile.BackWall && tiles[x + 1, y] == Tile.BackWall) id = Tile.BackWall;
            if (id == Tile.Empty || id == Tile.HiddenLife) continue;
            if (background != (id == Tile.BackWall)) continue;
            Sprite? sprite = id switch
            {
                Tile.Ground => art.Ground(_biome, Autotile.Mask4(tiles, x, y, t => t == Tile.Ground), (int)(MicroEngine.Graphics.Art.Hash(x, y, 5) * Assets.GroundVariants)),
                Tile.CoinBlock or Tile.PowerBlock or Tile.LifeBlock => art[QuestionNames[questionFrame]],
                Tile.Lava => art[tiles[x, y - 1] == Tile.Lava ? LavaBodyNames[lavaFrame % 2] : LavaTopNames[lavaFrame]],
                _ => art.TileSprite(_biome, id),
            };
            if (sprite is null) continue;
            if (id == Tile.BackWall)
            {
                // Mostly plain stone, with an occasional pale, cracked or mossy block; framed where it meets an opening.
                float h = MicroEngine.Graphics.Art.Hash(x, y, 17);
                var at = new Vector2(x * 16, y * 16);
                r.Draw(art[BackWallNames[h < 0.7f ? 0 : 1 + (int)((h - 0.7f) / 0.1f)]], at);
                foreach (var (frame, dx, dy) in WallFrames)
                    if (OpenInterior(tiles, x + dx, y + dy)) r.Draw(art[frame], at);
                continue;
            }
            float bump = stage.BumpOffset(x, y);
            r.Draw(sprite, new Vector2(x * 16, y * 16 + MathF.Round(bump)));
        }
    }

    /// <summary>An empty cell that is not filled with back wall (a window or doorway onto the sky).</summary>
    private static bool OpenInterior(MicroEngine.World.TileLayer tiles, int x, int y)
    {
        if (!tiles.InBounds(x, y) || tiles[x, y] is not (Tile.Empty or Tile.HiddenLife)) return false;
        bool filled = tiles.InBounds(x - 1, y) && tiles.InBounds(x + 1, y) && tiles[x - 1, y] == Tile.BackWall && tiles[x + 1, y] == Tile.BackWall;
        return !filled;
    }

    private void DrawDecorations(IRenderer r, Stage stage)
    {
        var view = stage.Camera.View.Inflate(16);
        var tiles = stage.Level.Tiles;
        var decorations = stage.Level.Decorations;
        for (int i = 0; i < decorations.Count; i++)
        {
            var (deco, x, y) = decorations[i];
            if (x * 16 < view.Left || x * 16 > view.Right || y * 16 < view.Top - 16 || y * 16 > view.Bottom) continue;
            // Decorations vanish if the ground under them was removed.
            if (tiles[x, y + 1] != Tile.Ground || tiles[x, y] != Tile.Empty) continue;
            r.Draw(art[_decos[deco]], new Vector2(x * 16 + 8, y * 16 + 16));
        }
    }

    private static int LayerOf(Actor a) => a switch
    {
        Checkpoint or GoalPole or LightProp or Spring or FireBar => 0,
        Coin or PowerItem or PopCoin or Sunstone => 1,
        SeedShot or Shard => 3,
        _ => 2,
    };

    private void DrawActors(IRenderer r, Stage stage, float time, int layer)
    {
        var cam = stage.Camera;
        foreach (var a in _actors)
        {
            if (LayerOf(a) != layer) continue;
            if (a is not (FireBar or GoalPole or Grumbold) && !cam.IsVisible(a.Bounds, 24)) continue;
            a.Render(r, art, time);
        }
    }

    private void DrawPlayer(IRenderer r, Stage stage, float time)
    {
        var p = stage.Player;
        if (p.Invulnerable > 0 && stage.Phase == StagePhase.Playing && (int)(time * 20) % 2 == 0) return;
        string clip = ClipFor(stage);
        var frames = art.Pip(p.Form, clip);
        int index = clip switch
        {
            "run" => (int)_runFrame % frames.Count,
            "idle" => (int)(time * 1.6f) % frames.Count,
            "victory" => (int)(time * 4) % frames.Count,
            _ => 0,
        };
        var sprite = frames[index];
        var scale = _squash;
        if (!p.Grounded && stage.Phase == StagePhase.Playing)
        {
            float stretch = Math.Clamp(MathF.Abs(p.Velocity.Y) / 1800f, 0, 0.12f);
            scale *= new Vector2(1 - stretch, 1 + stretch);
        }
        var feet = new Vector2(MathF.Round(p.Feet.X), MathF.Round(p.Feet.Y));
        bool flip = p.Facing < 0;
        if (clip == "skid") flip = !flip;
        r.Draw(sprite.Texture, sprite.Source, feet - sprite.Origin * scale, Color.White, new DrawOptions(FlipX: flip, Scale: scale));
    }

    private string ClipFor(Stage stage)
    {
        var p = stage.Player;
        switch (stage.Phase)
        {
            case StagePhase.Dying: return "hurt";
            case StagePhase.FlagSlide: return "fall";
            case StagePhase.Tally:
            case StagePhase.Victory: return "victory";
        }
        if (stage.ThrowPose > 0) return "throw";
        if (!p.Grounded) return p.Velocity.Y < 0 ? "jump" : "fall";
        if (p.Skidding) return "skid";
        if (MathF.Abs(p.Velocity.X) > 6) return "run";
        if (p.Controllable && stage.Phase == StagePhase.Playing && p.Velocity.X == 0 && _crouch) return "crouch";
        // Blink briefly every few seconds while idle.
        return stage.Clock % 3.2f < 0.12f ? "blink" : "idle";
    }

    private bool _crouch;
    public void SetCrouch(bool down) => _crouch = down;

    /// <summary>Biome light pass: caves and castle are darkened (multiply) and glows are added on top.</summary>
    private const float GlowRadius = 24;

    private void DrawLighting(IRenderer r, Stage stage, float time)
    {
        if (_biome is not (Biome.Caves or Biome.Castle)) return;
        // A flat colour grade (darkness 1, no brightening lights) plus additive halos.
        lights.Clear();
        lights.Shade = _biome == Biome.Caves ? Color.Hex("9c94c8") : Color.Hex("e0ccd0");
        // Pip carries a faint glow of his own so he never gets lost in the dark.
        var color = stage.Player.Form == PipForm.Ember ? Pal.Flame : _biome == Biome.Caves ? Pal.Teal : Pal.Amber;
        lights.Add(stage.Player.Center, GlowRadius, 0, color.WithAlpha(0.35f));
        foreach (var a in _actors)
        {
            if (a is LightProp or Checkpoint { Lit: true } or SeedShot or FireBar or Sunstone && stage.Camera.IsVisible(a.Bounds, 40))
            {
                var c = a is LightProp && _biome == Biome.Caves ? (a.Position.X % 32 < 16 ? Pal.Violet : Pal.Teal) : Pal.Amber;
                lights.Add(a is FireBar bar ? bar.Pivot : a.Center, GlowRadius, 0, c.WithAlpha(0.3f + MathX.Wave(time + a.Position.X * 0.01f, 1.1f, 0.06f)));
            }
        }
        lights.Draw(r, stage.Camera);
        if (_biome == Biome.Castle)
        {
            r.SetBlendMode(BlendMode.Additive);
            r.FillGradient(new RectF(0, 140, 320, 40), Pal.Wine.WithAlpha(0f), Pal.Rust.WithAlpha(0.18f), 5);
            r.SetBlendMode(BlendMode.Alpha);
        }
    }
}
