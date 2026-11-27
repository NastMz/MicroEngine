using System.Collections;
using System.Numerics;
using MicroEngine;
using MicroEngine.Graphics;
using MicroEngine.Kernel;
using MicroEngine.Scenes;

namespace Examples;

#region rules
/// <summary>
/// The rules of the show as a plain class: it owns its timers and its machine, and whatever steps it calls Update (the
/// scene below, or a test with no scene and no game).
/// </summary>
public sealed class Show : IStateHashed
{
    public enum Act { Waiting, Dance, Bow, Over }

    private readonly Timers _timers = new();
    private readonly StateMachine<Act> _act = new(Act.Waiting);

    public Show()
    {
        _act.State(Act.Dance)
            .OnEnter(() => _timers.EverySteps(20, () => Sparks++, times: 3))   // on the beat: every 20 updates
            .After(1.5f, Act.Bow);                                             // 90 updates at 60 a second, exactly
        _act.State(Act.Bow)
            .AfterSteps(45, Act.Over);                                         // a length in updates
    }

    public Act Current => _act.Current;
    public int Sparks { get; private set; }

    public void Begin() => _act.Go(Act.Dance);

    public void Update(float dt)
    {
        _timers.Update(dt);
        _act.Update(dt);
    }

    public void HashState(StateHasher hash)
    {
        hash.Add(_timers);
        hash.Add(_act);
        hash.Add(Sparks);
    }
}
#endregion

/// <summary>
/// A cutscene written as one coroutine that waits for its tweens, over rules kept in a plain class with its own timers
/// and state machine, with the dancers found as the children of one entity.
/// </summary>
public sealed class CutsceneExample : Scene
{
    /// <summary>What each dancer is: an attachment of the game's own.</summary>
    public sealed class Dancer(Color color)
    {
        public Color Color { get; } = color;
        /// <summary>How far off the floor, 0 to 1.</summary>
        public float Hop { get; set; }
    }

    private readonly Show _show = new();
    private EntityId _troupe;

    /// <summary>How much of the stage the curtain covers, 1 closed and 0 open.</summary>
    public float Curtain { get; private set; } = 1;
    public bool Finished { get; private set; }
    public Show Show => _show;

    #region group
    public override void Enter()
    {
        // A group is an entity with children: destroying the troupe destroys its dancers.
        _troupe = Objects.Create();
        string[] colors = ["ef7d57", "ffcd75", "73eff7"];
        for (int i = 0; i < colors.Length; i++)
        {
            var dancer = Objects.Create(_troupe);
            Objects.Local(dancer).Position2D = new Vector2(110 + i * 50, 130);
            Objects.Attach(dancer, new Dancer(Color.Hex(colors[i])));
        }
        Coroutines.Start(Play());
    }

    /// <summary>The dancers of the troupe, in the order they joined it; walking them allocates nothing.</summary>
    public ChildAttachments<Dancer> Dancers => Objects.Children<Dancer>(_troupe);
    #endregion

    #region cutscene
    private IEnumerator Play()
    {
        // A tween is something a coroutine waits for: the next line runs in the update the curtain is fully open.
        yield return Tweens.Add(0.5f, t => Curtain = 1 - t, Ease.OutCubic);
        _show.Begin();
        foreach (var dancer in Dancers)                                         // one after another
            yield return Tweens.Add(0.25f, t => dancer.Hop = 4 * t * (1 - t), Ease.Linear);
        yield return Wait.Until(() => _show.Current == Show.Act.Over);
        yield return Wait.Steps(10);                                            // ten updates, whatever the rate
        yield return Tweens.Add(0.5f, t => Curtain = t, Ease.InQuad);
        Finished = true;
    }
    #endregion

    public override void Update(float dt)
    {
        if (Input.Pressed("pause")) { Game.Scenes.Pop(); return; }
        _show.Update(dt);
    }

    public override void HashState(StateHasher hash)
    {
        hash.Add(_show);
        hash.Add(Curtain);
        hash.Add(Finished);
        foreach (var dancer in Dancers) hash.Add(dancer.Hop);
    }

    public override void Draw(IRenderer r)
    {
        r.FillRect(new RectF(60, 40, 200, 100), Color.Hex("1a1c2c"));
        var walk = Dancers.GetEnumerator();
        while (walk.MoveNext())
        {
            var at = Objects.DrawPosition2D(walk.Entity) - new Vector2(0, walk.Current.Hop * 24);
            r.FillCircle(at, 9, walk.Current.Color);
        }
        for (int i = 0; i < _show.Sparks; i++) r.FillCircle(new Vector2(130 + i * 30, 60), 3, Color.White);
        float half = 100 * Curtain;
        r.FillRect(new RectF(60, 40, half, 100), Color.Hex("b13e53"));
        r.FillRect(new RectF(260 - half, 40, half, 100), Color.Hex("b13e53"));
        Font.DrawCentered(r, Finished ? "THE END" : _show.Current.ToString().ToUpperInvariant(), new Vector2(160, 154), Color.White);
        Heading.Draw(r, Font, "A CUTSCENE");
    }
}
