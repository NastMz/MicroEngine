using System.Numerics;
using MicroEngine;
using MicroEngine.Graphics;
using MicroEngine.Scenes;

namespace Examples;

/// <summary>A particle fountain and bursts, under a light map that darkens the scene around two lights.</summary>
public sealed class ParticlesExample : Scene
{
    #region setup
    private static readonly ParticleSpec Spark = new()
    {
        StartColor = Color.Hex("ffcd75"), EndColor = Color.Hex("b13e5300"),   // fades to transparent
        Speed = (40, 90), Angle = (-MathF.PI * 0.75f, -MathF.PI * 0.25f),      // upwards (y grows down)
        Life = (0.6f, 1.2f), Gravity = 120, Size = (2, 1), Round = true,
    };
    private static readonly ParticleSpec Burst = Spark with { Angle = (0, MathF.Tau), Gravity = 0, Drag = 2, Speed = (60, 140) };

    private readonly ParticleSystem _particles = new(capacity: 400) { Additive = true };   // glows add up
    private LightMap _light = null!;
    private Vector2 _lamp = new(160, 100);

    public override void Enter() => _light = new LightMap(Game.Graphics) { Darkness = 0.85f, Shade = Color.Hex("101028") };
    public override void Exit() => _light.Dispose();
    #endregion

    #region update
    public override void Update(float dt)
    {
        if (Input.Pressed("pause")) { Game.Scenes.Pop(); return; }
        _lamp += Input.Vector("left", "right", "up", "down") * 100 * dt;
        _particles.Emit(new Vector2(80, 150), Spark, count: 2);
        if (Input.Pressed("confirm")) _particles.Emit(_lamp, Burst, count: 60);
        _particles.Update(dt);
    }
    #endregion

    public override void Draw(IRenderer r)
    {
        #region draw
        r.FillGradient(new RectF(0, 0, 320, 180), Color.Hex("3b5dc9"), Color.Hex("29366f"));
        r.FillRect(new RectF(0, 150, 320, 30), Color.Hex("257179"));
        _particles.Draw(r);

        // Lights: rebuilt every frame, drawn over the finished scene.
        _light.Clear();
        _light.Add(new Vector2(80, 140), radius: 50, brightness: 1, glow: Color.Hex("ffcd75"));
        _light.Add(_lamp, radius: 40, brightness: 0.9f, glow: Color.Hex("73eff7"));
        _light.Draw(r, Vector2.Zero, new RectF(0, 0, 320, 180));   // no camera: world and screen are the same
        #endregion

        Heading.Draw(r, Font, $"PARTICLES AND LIGHT ({_particles.Count})", "CONFIRM: BURST");
    }
}
