using System.Numerics;
using MicroEngine;
using MicroEngine.Graphics;
using MicroEngine.Kernel;
using MicroEngine.Scenes;

namespace Examples;

/// <summary>A scene system is a part of a scene the engine runs for it: this one keeps the time of day, and the scene's
/// timers drop a star every half second of the night. The scene never steps, hashes or stops either of them.</summary>
public sealed class SystemsExample : Scene
{
    #region system
    /// <summary>The time of day, shared by whatever in the scene wants to read it.</summary>
    public sealed class DayCycle : SceneSystem
    {
        /// <summary>0 at dawn, 0.5 at dusk, back to 0 after a full day.</summary>
        public float Time { get; private set; }
        public float DayLength { get; init; } = 4f;
        public bool IsNight => Time >= 0.5f;

        // Called by the engine before the scene's Update, with the same step.
        protected override void Update(float dt) => Time = (Time + dt / DayLength) % 1f;

        // The engine adds it to the scene's state hash: a deterministic run covers the system with no code in the scene.
        public override void HashState(StateHasher hash) => hash.Add(Time);
    }
    #endregion

    private readonly List<Vector2> _stars = [];
    private DayCycle _day = null!;

    #region use
    public override void Enter()
    {
        _day = Systems.Add(new DayCycle());          // started now, stepped every update, stopped when the scene leaves
        Timers.Every(0.5f, () =>                     // the scene's time: it slows and freezes with the world's clock
        {
            if (_day.IsNight) _stars.Add(new Vector2(Game.Random.Next(20, 300), Game.Random.Next(30, 110)));
            else _stars.Clear();
        });
    }

    public override void Update(float dt)
    {
        if (Input.Pressed("pause")) Game.Scenes.Pop();
    }
    #endregion

    public override void HashState(StateHasher hash) => hash.Add(_stars.Count);

    public override void Draw(IRenderer r)
    {
        float light = 0.5f + 0.5f * MathF.Cos(_day.Time * MathF.Tau);
        r.FillRect(new RectF(0, 0, r.Width, r.Height), Color.Lerp(Color.Hex("1a1c2c"), Color.Hex("73eff7"), light));
        foreach (var star in _stars) r.FillCircle(star, 1.5f, Color.White);
        r.FillRect(new RectF(0, 130, r.Width, 50), Color.Lerp(Color.Hex("257179"), Color.Hex("38b764"), light));
        Heading.Draw(r, Font, "A SCENE SYSTEM AND TIMERS", _day.IsNight ? "NIGHT" : "DAY");
    }
}
