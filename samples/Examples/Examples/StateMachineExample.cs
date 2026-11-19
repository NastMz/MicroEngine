using System.Numerics;
using MicroEngine;
using MicroEngine.Graphics;
using MicroEngine.Kernel;
using MicroEngine.Scenes;

namespace Examples;

/// <summary>A guard written as a state machine: it patrols, chases what comes near, winds up and strikes, and a
/// stomp stuns it whatever it was doing. Move with the arrows; ACTION stomps.</summary>
public sealed class StateMachineExample : Scene
{
    #region states
    private enum Guard { Patrol, Chase, WindUp, Strike, Stunned, Awake }
    private enum Blow { Stomp }

    private readonly StateMachine<Guard, Blow> _guard = new(Guard.Patrol);
    #endregion

    private Vector2 _guardAt = new(240, 120), _playerAt = new(60, 120);
    private float _patrolDirection = -1, _flash;
    private float Distance => Vector2.Distance(_guardAt, _playerAt);

    #region declare
    public override void Enter()
    {
        // What every waking state shares: a stomp interrupts any of them.
        _guard.State(Guard.Awake)
            .On(Blow.Stomp, Guard.Stunned);

        _guard.State(Guard.Patrol).SubstateOf(Guard.Awake)
            .OnUpdate(dt => Walk(_patrolDirection, 30, dt))
            .When(() => Distance < 90, Guard.Chase);

        _guard.State(Guard.Chase).SubstateOf(Guard.Awake)
            .OnEnter(() => Audio.TryPlay("select", 0.4f))
            .OnUpdate(dt => Walk(MathF.Sign(_playerAt.X - _guardAt.X), 55, dt))
            .When(() => Distance < 22, Guard.WindUp)         // declared first: checked first
            .When(() => Distance > 130, Guard.Patrol);

        _guard.State(Guard.WindUp).SubstateOf(Guard.Awake)
            .After(0.35f, Guard.Strike);                      // the machine keeps the time in the state

        _guard.State(Guard.Strike).SubstateOf(Guard.Awake)
            .OnEnter(() => _flash = 1)
            .OnExit(() => _flash = 0)                         // runs however the state ends, a stomp included
            .After(0.2f, Guard.Chase);

        _guard.State(Guard.Stunned)                           // not a part of Awake: a second stomp is not taken
            .After(1.2f, Guard.Patrol);
    }
    #endregion

    #region step
    public override void Update(float dt)
    {
        if (Input.Pressed("pause")) { Game.Scenes.Pop(); return; }
        _playerAt.X = Math.Clamp(_playerAt.X + Input.Axis("left", "right") * 90 * dt, 12, 308);
        if (Input.Pressed("action") && Distance < 40) _guard.Fire(Blow.Stomp);
        _guard.Update(dt);   // the state's update, then at most one way out
    }

    // One line puts the machine's state and its time in the scene's state hash.
    public override void HashState(StateHasher hash)
    {
        hash.Add(_guard);
        hash.Add(_guardAt);
        hash.Add(_playerAt);
        hash.Add(_patrolDirection);
    }
    #endregion

    private void Walk(float direction, float speed, float dt)
    {
        _guardAt.X += direction * speed * dt;
        if (_guardAt.X < 40) { _guardAt.X = 40; _patrolDirection = 1; }
        if (_guardAt.X > 280) { _guardAt.X = 280; _patrolDirection = -1; }
    }

    public override void Draw(IRenderer r)
    {
        r.FillRect(new RectF(0, 132, r.Width, 48), Color.Hex("257179"));
        var body = _guard.Current switch
        {
            Guard.Stunned => Color.Hex("94b0c2"),
            Guard.WindUp => Color.Hex("ffcd75"),
            Guard.Strike => Color.Hex("b13e53"),
            Guard.Chase => Color.Hex("ef7d57"),
            _ => Color.Hex("a7f070"),
        };
        r.FillRect(new RectF(_guardAt.X - 8, _guardAt.Y - 16 + (_guard.Current == Guard.Stunned ? 8 : 0), 16, 28), body);
        if (_flash > 0) r.FillCircle(_guardAt + new Vector2(MathF.Sign(_playerAt.X - _guardAt.X) * 16, 0), 9, Color.White);
        r.FillCircle(_playerAt, 7, Color.Hex("41a6f6"));
        string time = _guard.Time.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture);
        Font.DrawCentered(r, $"{_guard.Current} {time}".ToUpperInvariant(), new Vector2(160, 60), Color.White);
        Heading.Draw(r, Font, "A STATE MACHINE", "ACTION: STOMP");
    }
}
