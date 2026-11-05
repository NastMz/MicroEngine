using System.Numerics;
using MicroEngine;
using MicroEngine.Core;
using MicroEngine.Graphics;
using MicroEngine.Graphics3D;
using MicroEngine.Hosting;
using MicroEngine.Scenes;
using Dev = MicroEngine.Graphics.Devices;

namespace Slice3D;

/// <summary>
/// The slice as a game: 960 × 540, smooth presentation, a 30 Hz simulation (so the engine's interpolation between
/// steps is visible), and the tier-1 graphics device. Its composition is the one every host calls.
/// </summary>
public sealed class Slice3DGame : Game
{
    /// <summary>The game's composition: the game itself (the launcher's <c>--graphics</c> can still name another device).</summary>
    public static void Configure(HostBuilder host) => host.UseGame(() => new Slice3DGame());

    /// <inheritdoc/>
    public override GameSettings Settings { get; } = new("MicroEngine 3D slice", 960, 540, 1)
    {
        Presentation = Presentation.HighResolution2D,
        UpdateRate = 30,
        Background = new Color(24, 28, 40),
        // The 3D path needs custom shaders and depth: SDL's tier-1 device. Where SDL_GPU cannot start, the launcher
        // says why instead of running the game without 3D.
        Graphics = new GraphicsSettings { Device = "sdl-gpu" },
    };

    /// <inheritdoc/>
    protected override void Load()
    {
        Assets.Loaders.AddMeshes();
        Scenes.Push(new SliceScene());
    }
}

/// <summary>
/// A turntable carrying four crates, one of which carries a small crate on an arm (three levels), on a floor, in the
/// scene's world: the engine steps it and draws it at the frame's blend. Drawn as 3D into the canvas, a 2D HUD over it,
/// and a label in the UI view at the window's resolution.
/// </summary>
public sealed class SliceScene : Scene
{
    private readonly Camera3D _camera = new() { Position = new Vector3(0, 3.2f, 7.5f), Target = new Vector3(0, 0.4f, 0), FieldOfView = MathF.PI / 3.2f };
    private Renderer3D? _renderer;
    private GpuMesh? _crate;
    private EntityId _turntable, _carrier, _arm;
    private float _turn, _spin, _orbit;
    private long _steps;

    /// <summary>The renderer's statistics of the last frame, for tests.</summary>
    public Renderer3DStatistics Statistics => _renderer?.Statistics ?? default;

    /// <inheritdoc/>
    public override void Enter()
    {
        // The engine's textures live on a device of the contract; the 3D path draws on the same device.
        var device = Game.Graphics is DeviceGraphics graphics ? graphics.Device
            : throw new InvalidOperationException("the 3D slice needs a platform drawing on a graphics device of the contract (SDL with --graphics sdl-gpu)");
        _renderer = new Renderer3D(device) { Ambient = 0.3f, LightDirection = new Vector3(-0.5f, 1, 0.7f) };
        var mesh = Assets.Store.Get(Assets.Scope.Load<MeshData>(MeshAssets.Kind, "crate.glb"));
        _crate = _renderer.Upload(mesh);

        _turntable = Objects.Create();
        var tints = new[] { new ColorF(1, 0.55f, 0.35f, 1), new ColorF(0.45f, 0.8f, 1, 1), new ColorF(0.7f, 1, 0.5f, 1), new ColorF(1, 0.9f, 0.45f, 1) };
        for (int i = 0; i < 4; i++)
        {
            var box = Objects.Create(_turntable);
            float a = i * MathF.PI / 2;
            ref var t = ref Objects.Local(box);
            t.Position = new Vector3(2.2f * MathF.Cos(a), -0.05f, 2.2f * MathF.Sin(a));
            t.Scale = new Vector3(0.9f);
            Objects.Attach(box, new MeshRenderer(_crate) { Tint = tints[i] });
            if (i == 0) _carrier = box;
        }
        _arm = Objects.Create(_carrier);
        Objects.Local(_arm).Position = new Vector3(0, 1.1f, 0);
        var small = Objects.Create(_arm);
        ref var s = ref Objects.Local(small);
        s.Position = new Vector3(0.9f, 0, 0);
        s.Scale = new Vector3(0.35f);
        Objects.Attach(small, new MeshRenderer(_crate));
        var floor = Objects.Create();
        ref var f = ref Objects.Local(floor);
        f.Position = new Vector3(0, -0.6f, 0);
        f.Scale = new Vector3(7, 0.2f, 7);
        Objects.Attach(floor, new MeshRenderer(_crate) { Tint = new ColorF(0.35f, 0.35f, 0.4f, 1) });
    }

    /// <inheritdoc/>
    public override void Exit()
    {
        _crate?.Dispose();
        _renderer?.Dispose();
    }

    /// <summary>One engine step: the turntable, the carrier and the arm turn at their own rates.</summary>
    public override void Update(float dt)
    {
        _steps++;
        _turn += dt * 0.6f;
        _spin += dt * 1.7f;
        _orbit += dt * 3.1f;
        Objects.Local(_turntable).Rotation = Quaternion.CreateFromAxisAngle(Vector3.UnitY, _turn);
        Objects.Local(_carrier).Rotation = Quaternion.CreateFromAxisAngle(Vector3.UnitY, _spin);
        Objects.Local(_arm).Rotation = Quaternion.CreateFromAxisAngle(Vector3.UnitY, _orbit);
        if (Input.Pressed("cancel")) Game.Quit();
    }

    /// <summary>The world in 3D, then the HUD in 2D over it, in the same canvas.</summary>
    public override void Draw(IRenderer r)
    {
        if (r is DeviceRenderer canvas && _renderer is not null)
        {
            // The frame's clear (the game's background) and anything drawn so far reach the device first; the 3D pass
            // lands after them, and the 2D below lands over it.
            canvas.Flush();
            _renderer.Begin(canvas.Target, _camera);
            _renderer.DrawWorld(Objects);
            _renderer.End();
        }
        // A translucent panel with one bar per step of the last 30 (one second) and the frame's blend as a bar.
        r.FillRect(new RectF(12, 12, 196, 34), new Color(0, 0, 0, 140));
        for (int i = 0; i < 30; i++)
            r.FillRect(new RectF(20 + i * 6, 18, 4, 12), i <= _steps % 30 ? new Color(100, 217, 255) : new Color(51, 51, 64));
        r.FillRect(new RectF(20, 36, 180 * Game.FrameBlend + 2, 4), new Color(255, 204, 77));
    }

    /// <summary>The UI view: a label at the window's resolution.</summary>
    public override void DrawUi(IRenderer ui)
    {
        string label = $"3D slice  {_renderer?.Statistics.Meshes ?? 0} meshes  {_renderer?.Statistics.Triangles ?? 0} triangles";
        Font.DrawShadowed(ui, label, new Vector2(16, ui.Height - 24), new Color(244, 241, 222), new Color(0, 0, 0), 2);
    }
}
