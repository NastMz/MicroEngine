using System.Numerics;
using CourtyardKey.Content;
using CourtyardKey.Entities;
using CourtyardKey.Visuals;
using MicroEngine;
using MicroEngine.Graphics;

namespace CourtyardKey.Scenes;

/// <summary>Rendering: tiles, Y-sorted actors, particles, darkness, HUD and fades.</summary>
public sealed partial class PlayScene
{
    private static readonly Comparison<Actor> ByDepth = (a, b) => a.Depth.CompareTo(b.Depth);
    private Func<int, int, bool>? _platePressed;
    private static readonly Func<int, int, bool> NoPlates = (_, _) => false;

    private Vector2 _previousCamera;
    private bool _hasPreviousCamera;
    private long _rememberedUpdate = -1;

    /// <summary>
    /// How far this frame is between the previous update and the current one (<see cref="Game.FrameBlend"/>), or 1
    /// when this scene did not run the latest update (paused, under a dialogue or a transition), so a frozen room
    /// does not rock between its last two updates.
    /// </summary>
    public float DrawBlend => Game.UpdateCount == _rememberedUpdate ? Game.FrameBlend : 1;

    /// <summary>The camera position drawn at <paramref name="blend"/> between the previous update and the current one.</summary>
    public Vector2 CameraDrawPositionAt(float blend) => Actor.Blend(_hasPreviousCamera ? _previousCamera : Camera.Position, Camera.Position, blend);

    /// <summary>Called first in every update: the positions every moving thing is drawn from until the next one.</summary>
    private void RememberDrawPositions()
    {
        _rememberedUpdate = Game.UpdateCount;
        _previousCamera = Camera.Position;
        _hasPreviousCamera = true;
        Hero.RememberPosition();
        // Actors drawn last frame; one spawned since is drawn where it is until the next update.
        foreach (var a in _drawList) a.RememberPosition();
    }

    /// <summary>After a jump (scene entry, checkpoint, area change) the camera and hero are drawn where they are.</summary>
    private void SnapDrawPositions()
    {
        _hasPreviousCamera = false;
        Hero.SnapDrawPosition();
    }

    public override void Draw(IRenderer r)
    {
        var camera = Camera.Position;
        Camera.Position = CameraDrawPositionAt(DrawBlend);
        try { DrawWorld(r); }
        finally { Camera.Position = camera; }
    }

    private void DrawWorld(IRenderer r)
    {
        _platePressed ??= PlatePressed;
        r.PushCamera(Camera);
        if (_scrollFrom is not null) _tiles.Draw(r, _scrollFrom, Age, NoPlates);
        _tiles.Draw(r, Room, Age, _platePressed);

        _drawList.Clear();
        if (_leaving.Count > 0)
            foreach (var a in _leaving) _drawList.Add(a);
        foreach (var a in _actors) _drawList.Add(a);
        _drawList.Add(Hero);
        _drawList.Sort(ByDepth);
        foreach (var a in _drawList) a.DrawShadow(r);
        foreach (var a in _drawList) a.Draw(r);
        Particles.Draw(r, DrawBlend);
        Ambient.Draw(r);
        r.PopCamera();

        Light.Clear();
        float darkness = Room.Def.Darkness;
        if (_scrollFrom is not null) darkness = MathX.Lerp(_scrollFrom.Def.Darkness, darkness, Math.Clamp(_scrollTime / ScrollTime, 0, 1));
        Light.Darkness = darkness;
        if (darkness > 0.01f)
            foreach (var a in _drawList) a.EmitLight(Light);
        // Halos fade with the darkness so torches read as light, not as stickers, in lit rooms.
        Light.GlowIntensity = 0.3f * darkness + 0.06f;
        if (darkness > 0.01f) Light.Draw(r, -Camera.RenderOffset, WorldView);

        r.PushCamera(Camera);
        Glows.Draw(r);
        if (PresentedItem != Item.None) DrawPresentedItem(r);
        r.PopCamera();

        _hud.Draw(r);
        if (_fade > 0) r.FillRect(new RectF(0, 0, r.Width, r.Height), _fadeColor.WithAlpha(_fade));
    }

    private void DrawPresentedItem(IRenderer r)
    {
        var sprite = Art[ItemSprite(PresentedItem)];
        var at = Hero.Position + new Vector2(0, -24);
        r.SetBlendMode(BlendMode.Additive);
        float pulse = 0.85f + MathF.Sin(Age * 8) * 0.15f;
        r.DrawPivoted(Art["glow_m"], at, 0, new Vector2(pulse), Pal.Gold[3].WithAlpha(0.55f));
        r.SetBlendMode(BlendMode.Alpha);
        r.DrawPivoted(sprite, at);
    }

    public static string ItemSprite(Item item) => item switch
    {
        Item.Sword => "sword",
        Item.Boomerang => "boomerang",
        Item.SmallKey => "key_small",
        Item.BossKey => "key_boss",
        Item.HeartContainer => "heart_container",
        Item.Gems => "gem_blue",
        Item.CourtyardKey => "courtyard_key",
        _ => "gem_green",
    };
}
