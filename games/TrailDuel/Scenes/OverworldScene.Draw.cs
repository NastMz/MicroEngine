using System.Numerics;
using MicroEngine;
using MicroEngine.Graphics;
using MicroEngine.World;
using TrailDuel.Rules;
using TrailDuel.Ui;
using TrailDuel.Visuals;
using TrailDuel.World;

namespace TrailDuel.Scenes;

/// <summary>Overworld rendering: ground, depth-sorted objects and characters per row, effects and HUD.</summary>
internal sealed partial class OverworldScene
{
    private const int Tile = MapDef.TileSize;
    private readonly List<Actor> _rowActors = [];

    // Tile keys built once: formatting them per tile per frame was most of the overworld's garbage.
    private static readonly string[] FlowerKeys = SpriteKeys.Numbered("t:flowers", 2), TallKeys = SpriteKeys.Numbered("t:tall", 2),
        PathKeys = SpriteKeys.Numbered("t:path", 16), LedgeKeys = SpriteKeys.Numbered("t:ledge", 4), CarpetKeys = SpriteKeys.Numbered("t:carpet", 16),
        FloorKeys = SpriteKeys.Numbered("t:floor", 2), GrassKeys = SpriteKeys.Numbered("t:grass", 4), WaterInKeys = SpriteKeys.Numbered("t:waterin", 4),
        TreeKeys = SpriteKeys.Numbered("t:tree", TileArt.TreeVariants), PineKeys = SpriteKeys.Numbered("t:pine", TileArt.PineVariants),
        FenceKeys = SpriteKeys.Numbered("t:fence", 4), TableKeys = SpriteKeys.Numbered("t:table", 4), CounterKeys = SpriteKeys.Numbered("t:counter", 16),
        RustleKeys = SpriteKeys.Numbered("t:rustle", 3);
    private const int WaterFrames = 3;
    private static readonly string[] WaterKeys = WaterKeyTable();
    private MapDef? _mapKeysFor;
    private string _groundKey = "";
    private string[] _buildingKeys = [];

    private static string[] WaterKeyTable()
    {
        var keys = new string[16 * WaterFrames];
        for (int mask = 0; mask < 16; mask++)
            for (int frame = 0; frame < WaterFrames; frame++) keys[mask * WaterFrames + frame] = $"t:water{mask}_{frame}";
        return keys;
    }

    private static string WaterKey(int mask, int frame)
        => (uint)mask < 16 && (uint)frame < WaterFrames ? WaterKeys[mask * WaterFrames + frame] : $"t:water{mask}_{frame}";

    /// <summary>The ground layer and building keys of the current map, rebuilt when the map changes.</summary>
    private void RefreshMapKeys()
    {
        if (_mapKeysFor == _map.Def) return;
        _mapKeysFor = _map.Def;
        _groundKey = "ground:" + _map.Def.Id;
        _buildingKeys = new string[_map.Def.Buildings.Count];
        for (int i = 0; i < _buildingKeys.Length; i++) _buildingKeys[i] = $"bld:{_map.Def.Id}:{i}";
    }

    /// <summary>The camera position drawn this frame, between the start and the end of the scene's latest step.</summary>
    internal Vector2 CameraDrawPosition => Objects.DrawPosition2D(_cameraEntity);

    public override void Draw(IRenderer r)
    {
        // The rules keep moving the camera's Position; drawing shows it where the world interpolates it between steps.
        _camera.DrawPosition = CameraDrawPosition;
        DrawWorld(r);
        DrawHud(r);
    }

    private void DrawWorld(IRenderer r)
    {
        RefreshMapKeys();
        var view = _camera.VisibleBounds;
        int x0 = Math.Max(0, (int)MathF.Floor(view.X / Tile) - 1), x1 = Math.Min(_map.Width - 1, (int)MathF.Ceiling(view.Right / Tile) + 1);
        int y0 = Math.Max(0, (int)MathF.Floor(view.Y / Tile) - 1), y1 = Math.Min(_map.Height - 1, (int)MathF.Ceiling(view.Bottom / Tile) + 2);

        r.PushCamera(_camera);
        for (int y = y0; y <= y1; y++)
        for (int x = x0; x <= x1; x++)
            DrawGround(r, x, y);
        r.Draw(_game.Sprite(_groundKey), Vector2.Zero);

        for (int y = y0; y <= y1; y++)
        {
            for (int x = x0; x <= x1; x++) DrawObject(r, x, y);
            foreach (var item in _map.Def.Items) if (item.Y == y && !State.Has(item.Flag)) r.Draw(_game.Sprite("itemball"), new Vector2(item.X * Tile, item.Y * Tile + MathF.Round(MathX.Wave(Age + item.X, 0.8f, 1))));
            for (int i = 0; i < _map.Def.Buildings.Count; i++)
            {
                var b = _map.Def.Buildings[i];
                if (b.Y + b.H - 1 == y) r.Draw(_game.Sprite(_buildingKeys[i]), new Vector2(b.X * Tile, b.Y * Tile - BuildingArt.RoofOverhang));
            }
            _rowActors.Clear();
            foreach (var npc in _map.Npcs) if (npc.Visible && npc.DrawRow == y) _rowActors.Add(npc);
            if (_player.DrawRow == y) _rowActors.Add(_player);
            _rowActors.Sort((a, b) => a.PixelPosition.Y.CompareTo(b.PixelPosition.Y));
            foreach (var a in _rowActors) DrawActor(r, a);
        }

        foreach (var (rx, ry, time) in _rustles)
        {
            int frame = Math.Min(2, (int)((Age - time) / 0.15f));
            r.Draw(_game.Sprite(SpriteKeys.Pick(RustleKeys, "t:rustle", frame)), new Vector2(rx * Tile, ry * Tile));
        }
        _dust.Draw(r);
        if (_healGlow >= 0) DrawHealGlow(r);
        if (_bubbleOn is { } bubbleActor)
        {
            float pop = _bubbleTime < 0.12f ? _bubbleTime / 0.12f : 1;
            var p = bubbleActor.DrawPosition + new Vector2(2, -20 + (1 - pop) * 6);
            r.Draw(_game.Sprite("bubble"), new Vector2(MathF.Round(p.X), MathF.Round(p.Y)));
        }
        r.PopCamera();
    }

    private void DrawGround(IRenderer r, int x, int y)
    {
        var kind = _map.KindAt(x, y);
        var pos = new Vector2(x * Tile, y * Tile);
        string? key = kind switch
        {
            TileKind.Flowers => SpriteKeys.Pick(FlowerKeys, "t:flowers", (int)(Age * 1.6f + x * 0.37f + y * 0.21f) % 2),
            TileKind.TallGrass => SpriteKeys.Pick(TallKeys, "t:tall", (int)(Age * 0.9f + (x + y) * 0.5f) % 2),
            TileKind.Path => SpriteKeys.Pick(PathKeys, "t:path", AutotileMask(x, y)),
            TileKind.Water => WaterKey(AutotileMask(x, y), (int)(Age * 2.2f) % 3),
            TileKind.Ledge => SpriteKeys.Pick(LedgeKeys, "t:ledge", (_map.KindAt(x - 1, y) == TileKind.Ledge ? 1 : 0) | (_map.KindAt(x + 1, y) == TileKind.Ledge ? 2 : 0)),
            TileKind.Wall => _map.KindAt(x, y + 1) == TileKind.Wall ? "t:walltop" : (x % 4 == 2 && !_map.Def.Id.Equals("hall") ? "t:wallwin" : "t:wall"),
            TileKind.Mat => "t:mat",
            TileKind.Carpet => SpriteKeys.Pick(CarpetKeys, "t:carpet", AutotileMask(x, y)),
            TileKind.Rug => "t:rug",
            TileKind.Void => null,
            _ => _map.Def.Id is "hall" or "shrine" ? "t:stone" : _map.Def.Indoor ? SpriteKeys.Pick(FloorKeys, "t:floor", (x + y) % 2) : SpriteKeys.Pick(GrassKeys, "t:grass", (int)(Art.Hash(x, y, 31) * 4)),
        };
        if (key is not null) r.Draw(_game.Sprite(key), pos);
        if (kind == TileKind.Water)
        {
            // Concave corners: both edge neighbours are water but the diagonal is land.
            bool W(int tx, int ty) => _map.KindAt(tx, ty) == TileKind.Water || !Layer.InBounds(tx, ty);
            for (int c = 0; c < 4; c++)
            {
                int dx = c % 2 == 0 ? -1 : 1, dy = c < 2 ? -1 : 1;
                if (W(x + dx, y) && W(x, y + dy) && !W(x + dx, y + dy)) r.Draw(_game.Sprite(WaterInKeys[c]), pos);
            }
        }
    }

    private const int DoorMarker = 255;
    private TileLayer? _layer;
    private int[] _masks = [];
    private MapDef? _layerFor;

    /// <summary>The map as a tile-id layer (TileKind values) for autotiling; building doors count as path.</summary>
    private TileLayer Layer
    {
        get
        {
            if (_layer is not null && _layerFor == _map.Def) return _layer;
            var layer = new TileLayer(_map.Width, _map.Height);
            for (int y = 0; y < _map.Height; y++)
            for (int x = 0; x < _map.Width; x++)
                layer[x, y] = (int)_map.KindAt(x, y);
            foreach (var b in _map.Def.Buildings) layer[b.X + b.DoorX, b.Y + b.H - 1] = DoorMarker;
            _layerFor = _map.Def;
            _layer = layer;
            _masks = AutotileMasks(layer);
            return layer;
        }
    }

    /// <summary>
    /// The autotile mask of the tile at (<paramref name="x"/>, <paramref name="y"/>): which neighbours join its path,
    /// water, carpet or counter. Maps never change while shown, so the masks are worked out once per map.
    /// </summary>
    private int AutotileMask(int x, int y)
    {
        _ = Layer; // rebuilds the layer and its masks when the map changed
        return _masks[y * _map.Width + x];
    }

    private int[] AutotileMasks(TileLayer layer)
    {
        var masks = new int[_map.Width * _map.Height];
        for (int y = 0; y < _map.Height; y++)
        for (int x = 0; x < _map.Width; x++)
            masks[y * _map.Width + x] = _map.KindAt(x, y) switch
            {
                TileKind.Path => Autotile.Mask4(layer, x, y, id => id == (int)TileKind.Path || id == DoorMarker, outsideMatches: true),
                TileKind.Water => Autotile.Mask4(layer, x, y, id => id == (int)TileKind.Water, outsideMatches: true),
                TileKind.Carpet => Autotile.Mask4(layer, x, y, id => id == (int)TileKind.Carpet || id == (int)TileKind.Mat, outsideMatches: false),
                TileKind.Counter => Autotile.Mask4(layer, x, y, id => id == (int)TileKind.Counter, outsideMatches: false),
                _ => 0,
            };
        return masks;
    }

    private void DrawObject(IRenderer r, int x, int y)
    {
        var kind = _map.KindAt(x, y);
        var pos = new Vector2(x * Tile, y * Tile);
        string? key = kind switch
        {
            TileKind.Tree => SpriteKeys.Pick(TreeKeys, "t:tree", (int)(Art.Hash(x, y, 61) * TileArt.TreeVariants)),
            TileKind.Pine => SpriteKeys.Pick(PineKeys, "t:pine", (int)(Art.Hash(x, y, 62) * TileArt.PineVariants)),
            TileKind.Rock => "t:rock",
            TileKind.Bush => "t:bush",
            TileKind.Fence => SpriteKeys.Pick(FenceKeys, "t:fence", (_map.KindAt(x - 1, y) == TileKind.Fence ? 1 : 0) | (_map.KindAt(x + 1, y) == TileKind.Fence ? 2 : 0)),
            TileKind.Bookshelf => "t:bookshelf",
            TileKind.Bed => _map.KindAt(x, y - 1) == TileKind.Bed ? "t:bedbottom" : "t:bedtop",
            TileKind.Table or TileKind.Pedestal => SpriteKeys.Pick(TableKeys, "t:table", (IsTable(x - 1, y) ? 1 : 0) | (IsTable(x + 1, y) ? 2 : 0)),
            TileKind.Plant => "t:plant",
            TileKind.Counter => SpriteKeys.Pick(CounterKeys, "t:counter", AutotileMask(x, y)),
            TileKind.Tv => "t:tv",
            TileKind.Kitchen => "t:kitchen",
            TileKind.Altar => "t:altar",
            TileKind.Shelf => "t:shelf",
            TileKind.Machine => "t:machine",
            TileKind.Statue => "t:statue",
            _ => null,
        };
        if (key is not null) r.Draw(_game.Sprite(key), pos);
        if (_map.SignAt(x, y) is not null) r.Draw(_game.Sprite("t:sign"), pos);
        if (kind == TileKind.Pedestal && _map.Def.Id == "lab") DrawStarterOrb(r, x, y);
    }

    private bool IsTable(int x, int y) => _map.KindAt(x, y) is TileKind.Table or TileKind.Pedestal;

    private void DrawStarterOrb(IRenderer r, int x, int y)
    {
        int index = x - 4;
        if (index < 0 || index >= SpeciesDex.Starters.Length) return;
        string id = SpeciesDex.Starters[index];
        if (State.Has("starter") && (id == State.StarterId || id == GameState.RivalStarterFor(State.StarterId))) return;
        float bob = _previewSpecies == id ? MathF.Round(MathX.Wave(Age, 2, 1.5f)) - 1 : 0;
        r.Draw(_game.Sprite("orbsmall"), new Vector2(x * Tile + 8, y * Tile + 6 + bob));
    }

    private void DrawActor(IRenderer r, Actor a)
    {
        var p = a.DrawPosition;
        r.Draw(_game.Sprite("t:shadow"), p + new Vector2(0, 1));
        int frame = a.Frame;
        if (a == _player && !a.Moving && _bumpTimer > 0 && !Busy && AnyDirectionHeld()) frame = a.BumpFrame(Age);
        var sprite = _game.Sprite(a.SpriteKey(frame));
        var s = a.DrawSpritePosition;
        r.Draw(sprite, new Vector2(MathF.Round(s.X), MathF.Round(s.Y)));
        // Tall grass hides the feet of anyone standing in it.
        if (!a.Hopping && _map.KindAt(a.X, a.Y) == TileKind.TallGrass && !a.Moving)
            r.Draw(_game.Sprite("t:tallover"), new Vector2(a.X * Tile, a.Y * Tile));
    }

    private bool AnyDirectionHeld() => Input.Down("up") || Input.Down("down") || Input.Down("left") || Input.Down("right");

    /// <summary>The shrine's altar columns, where the heal glow rises.</summary>
    private static readonly int[] AltarColumns = [3, 7];
    // Effect colours parsed once instead of every frame.
    private static readonly Color HealGlowColor = Color.Hex("f8a0c0").WithAlpha(0.35f), CanopyShade = Color.Hex("b4ccb4"),
        LightShaft = Color.Hex("fff4c0").WithAlpha(0.012f);

    private void DrawHealGlow(IRenderer r)
    {
        // Light rises from the altars while the jingle plays.
        float t = _healGlow;
        r.SetBlendMode(BlendMode.Additive);
        foreach (int ax in AltarColumns)
        {
            var c = new Vector2(ax * Tile + 8, 2 * Tile - 2);
            float pulse = 0.5f + 0.5f * MathF.Sin(t * 9);
            r.FillCircle(c, 6 + pulse * 4, HealGlowColor);
            for (int i = 0; i < 4; i++)
            {
                float rise = (t * 30 + i * 9) % 30;
                r.FillRect(new RectF(c.X - 4 + i * 2.5f, c.Y + 6 - rise, 1, 2), Pal.White.WithAlpha(0.8f * (1 - rise / 30)));
            }
        }
        r.SetBlendMode(BlendMode.Alpha);
    }

    private void DrawHud(IRenderer r)
    {
        if (_map.Def.Backdrop == Backdrop.Forest)
        {
            // Canopy shade and slow-drifting shafts of light give the wood its own mood.
            r.SetBlendMode(BlendMode.Multiply);
            r.FillRect(TrailDuelGame.Canvas, CanopyShade);
            r.SetBlendMode(BlendMode.Additive);
            for (int i = 0; i < 3; i++)
            {
                float x = (i * 130 + Age * 4) % 420 - 80;
                for (int s = 0; s < 12; s++)
                    r.FillTriangle(new Vector2(x + s * 2, 0), new Vector2(x + s * 2 + 30, 0), new Vector2(x + s * 2 + 70, TrailDuelGame.CanvasHeight), LightShaft);
            }
            r.SetBlendMode(BlendMode.Alpha);
        }
        if (_bannerTime is >= 0 and < 2.6f)
        {
            float slide = _bannerTime < 0.3f ? 1 - _bannerTime / 0.3f : _bannerTime > 2.2f ? (_bannerTime - 2.2f) / 0.4f : 0;
            int w = Font.MeasureWidth(_bannerText) + 20;
            var rect = new RectF(6, 6 - slide * 30, w, 18);
            UiKit.Box(r, rect);
            UiKit.Text(r, Font, _bannerText, rect.X + 10, rect.Y + 5);
        }
        if (_previewSpecies is { } species) DrawStarterPreview(r, species);
        // The fade darkens the world, not the dialogue: narration over a dark screen must stay readable.
        if (_fade > 0) r.FillRect(TrailDuelGame.Canvas, Pal.Ink.WithAlpha(_fade));
        _text.Draw(r);
        _choice.Draw(r, new Vector2(316, 130));
        if (_flash) r.FillRect(TrailDuelGame.Canvas, Pal.White.WithAlpha(0.85f));
    }

    private void DrawStarterPreview(IRenderer r, string species)
    {
        var data = SpeciesDex.Get(species);
        var rect = new RectF(98, 18, 124, 108);
        UiKit.Box(r, rect);
        r.FillGradient(new RectF(rect.X + 4, rect.Y + 4, rect.Width - 8, 70), Pal.Sky.WithAlpha(0.6f), Pal.Cream, 6);
        r.Draw(_game.Sprite("platform:enemy"), new Vector2(160, 72));
        float bob = MathF.Round(MathX.Wave(Age, 1.2f, 1.5f));
        r.Draw(_game.Sprite(SpriteKeys.Front[species]), new Vector2(160, 74 + bob));
        UiKit.Text(r, Font, data.Name, rect.X + 10, rect.Y + 82);
        int bw = UiKit.TypeBadgeWidth(Font, data.Type);
        UiKit.TypeBadge(r, Font, data.Type, rect.Right - bw - 10, rect.Y + 81);
        UiKit.Text(r, Font, "Lv. 5", rect.X + 10, rect.Y + 93, UiKit.Muted);
    }
}
