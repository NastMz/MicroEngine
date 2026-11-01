using System.Numerics;
using MicroEngine;
using MicroEngine.Graphics;
using MicroEngine.Scenes;
using MicroEngine.UI;

namespace Examples;

/// <summary>A world larger than the screen: the camera follows a marker, stays inside the world, shakes, and converts
/// between world and screen coordinates.</summary>
public sealed class CameraExample : Scene
{
    #region setup
    private static readonly RectF World = new(0, 0, 960, 540);   // three screens wide, three high
    private readonly Camera2D _camera = new(320, 180)
    {
        Bounds = World,                       // never shows outside the world
        DeadZone = new Vector2(40, 24),       // the target moves freely in the middle before the camera follows
        FollowSharpness = 6,                  // higher catches up faster; 0 snaps
    };
    private Vector2 _target = new(160, 90);
    #endregion

    #region update
    public override void Update(float dt)
    {
        if (Input.Pressed("pause")) { Game.Scenes.Pop(); return; }
        _target += Input.Vector("left", "right", "up", "down") * 140 * dt;
        _target = Vector2.Clamp(_target, World.Position, World.Position + World.Size);
        if (Input.Pressed("confirm")) _camera.Shake(0.6f);   // trauma 0..1, fades by itself
        _camera.Follow(_target, dt);
        _camera.Update(dt);                                   // advances the shake
    }
    #endregion

    public override void Draw(IRenderer r)
    {
        #region draw
        r.PushCamera(_camera);                                // from here on, positions are world pixels
        for (int y = 0; y < World.Height; y += 30)
        for (int x = 0; x < World.Width; x += 30)
            if (_camera.IsVisible(new RectF(x, y, 2, 2)))     // skip what is off screen
                r.FillRect(new RectF(x, y, 2, 2), Color.Hex("566c86"));
        r.DrawRect(World, Color.Hex("ef7d57"), 2);
        r.FillCircle(_target, 5, Color.Hex("ffcd75"));
        r.PopCamera();                                        // back to screen pixels for the HUD

        var screen = _camera.WorldToScreen(_target);
        Font.Draw(r, $"WORLD {_target.X:0},{_target.Y:0}  SCREEN {screen.X:0},{screen.Y:0}", new Vector2(4, 168), UiTheme.Default.Text);
        if (Input.Pointer.Present)
        {
            var world = _camera.ScreenToWorld(Input.Pointer.Position);
            Font.Draw(r, $"MOUSE IN WORLD {world.X:0},{world.Y:0}", new Vector2(4, 156), UiTheme.Default.Muted);
        }
        #endregion
        Heading.Draw(r, Font, "CAMERA", "CONFIRM: SHAKE");
    }
}
