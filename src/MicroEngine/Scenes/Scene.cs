using System.Numerics;
using MicroEngine.Audio;
using MicroEngine.Core;
using MicroEngine.Graphics;
using MicroEngine.Input;
using MicroEngine.Kernel;

namespace MicroEngine.Scenes;

/// <summary>
/// A screen of the game (title, level, battle, pause menu...). Scenes live on a stack:
/// an overlay scene (dialogue, pause) draws on top of the scene below and can freeze it.
/// Each scene owns its coroutines and tweens, which stop when it leaves the stack.
/// </summary>
/// <example>
/// <code>
/// public sealed class TitleScene : Scene
/// {
///     public override void Update(float dt)
///     {
///         if (Input.Pressed("confirm")) Game.Scenes.Replace(new LevelScene(), Transition.Fade());
///     }
///
///     public override void Draw(IRenderer renderer) =>
///         Font.DrawCentered(renderer, "PRESS START", new Vector2(Game.Width / 2f, Game.Height / 2f), Color.White);
/// }
/// </code>
/// </example>
public abstract class Scene : IStateHashed
{
    private Game? _game;
    /// <summary>The game this scene belongs to. Set when the scene is pushed; throws <see cref="InvalidOperationException"/> before that (for example in the scene's constructor).</summary>
    public Game Game => _game ?? throw new InvalidOperationException("Scene is not attached to a game.");
    /// <summary>The game's input actions (<see cref="Core.Game.Input"/>).</summary>
    public InputMap Input => Game.Input;
    /// <summary>The game's audio mixer (<see cref="Core.Game.Audio"/>).</summary>
    public AudioMixer Audio => Game.Audio;
    /// <summary>The game's built-in font (<see cref="Core.Game.Font"/>).</summary>
    public BitmapFont Font => Game.Font;
    /// <summary>The game's content files (<see cref="Core.Game.Assets"/>).</summary>
    public Core.AssetCache Assets => Game.Assets;
    /// <summary>Coroutines owned by this scene. They advance before <see cref="Update"/> while the scene is updated and stop when it leaves the stack.</summary>
    public CoroutineRunner Coroutines { get; } = new();
    /// <summary>Tweens owned by this scene. They advance before <see cref="Update"/> while the scene is updated and are cleared when it leaves the stack.</summary>
    public Tweener Tweens { get; } = new();
    /// <summary>Seconds this scene has been updated.</summary>
    public float Age { get; private set; }

    /// <summary>When true, the scene below is drawn first.</summary>
    public virtual bool IsOverlay => false;
    /// <summary>When true (default), scenes below an overlay are not updated.</summary>
    public virtual bool BlocksUpdateBelow => true;

    private SceneWorld? _world;
    /// <summary>
    /// The scene's objects (<see cref="IWorld"/>), created the first time it is read. While the scene is updated the
    /// engine steps it with the scene: each step starts before the scene's coroutines, tweens and <see cref="Update"/>
    /// (which then receives the step on the world's <see cref="IWorld.Clock"/>) and ends after them, releasing what was
    /// destroyed. Before <see cref="Draw"/> the engine sets its <see cref="IWorld.Blend"/>: the frame's blend when the
    /// scene was updated in the latest step, 1 when it was not (frozen under an overlay, in a transition), so drawing
    /// through <see cref="IWorld.DrawPosition2D"/> and <see cref="IWorld.WorldMatrix"/> is smooth while it plays and
    /// still while it is frozen. Its state is part of the scene's state hash.
    /// </summary>
    public IWorld Objects => _world ??= new SceneWorld();
    /// <summary>Whether <see cref="Objects"/> was created (a scene that never reads it has none and pays nothing).</summary>
    internal bool HasWorld => _world is not null;

    internal Action<object?>? ResultHandler;
    internal bool Finishing;
    internal bool HasResult;
    internal object? Result;

    internal void Attach(Game game) => _game = game;

    /// <summary>Called when the scene is pushed onto the stack, after <see cref="Game"/> is set and input was reset.</summary>
    public virtual void Enter() { }
    /// <summary>Called when the scene leaves the stack (popped, replaced or cleared), after its coroutines and tweens stopped.</summary>
    public virtual void Exit() { }
    /// <summary>Called when a scene pushed above this one is popped.</summary>
    public virtual void Resume() { }
    /// <summary>
    /// A short description of the scene's state for development probes and tests (the web host's trace, for example
    /// "x=120 y=88 grounded"); null by default. Never shown to players.
    /// </summary>
    public virtual string? Describe() => null;
    /// <summary>
    /// The actions this scene needs from the on-screen touch controls while it is on top, or null for all of them (the
    /// default). A dialogue returns <c>["confirm", "cancel"]</c>: the movement control disappears from over its text and
    /// the buttons that confirm and cancel stay. Read every frame, so a scene may change it with its own state.
    /// </summary>
    public virtual IReadOnlyCollection<string>? TouchActions => null;
    /// <summary>
    /// Adds the scene's simulation state to the per-step state hash (<see cref="Core.Game.ComputeStateHash"/>): the
    /// fields that decide what happens next (positions, velocities, timers, health, the state of its own generators), in
    /// a fixed order. Leave out what <see cref="Draw"/> writes (text and layout caches, positions held for blending,
    /// particles that are only drawn): the hash must be the same whether or not a frame was drawn. The engine adds the
    /// scene's type, <see cref="Age"/>, coroutines and tweens itself, in the scene's section
    /// (<c>scene[index]:Type</c>). The default adds nothing.
    /// </summary>
    /// <param name="hash">The hasher of the step; a section started here lasts until the engine starts the next one.</param>
    public virtual void HashState(StateHasher hash) { }

    /// <summary>Advances the scene by one fixed step. Not called while a blocking overlay is above, nor during the covering half of a transition.</summary>
    /// <param name="dt">
    /// Step length in seconds: <c>1 / UpdateRate</c>, or, once the scene has <see cref="Objects"/>, the step on the
    /// world's clock (the same at scale 1; scaled by <see cref="WorldClock.Scale"/>; 0 while it is frozen).
    /// </param>
    public abstract void Update(float dt);
    /// <summary>Draws the scene once per frame into the virtual canvas. Scenes below an overlay are drawn first.</summary>
    /// <param name="renderer">The renderer of the current frame.</param>
    public abstract void Draw(IRenderer renderer);

    /// <summary>The engine's part of the scene's state, then the scene's own (<see cref="HashState"/>).</summary>
    internal void HashAll(StateHasher hash)
    {
        hash.Add(Age);
        Coroutines.HashState(hash);
        Tweens.HashState(hash);
        _world?.HashState(hash);
        HashState(hash);
    }

    internal void Tick(float dt)
    {
        if (_world is not { } world)
        {
            Age += dt;
            Coroutines.Update(dt);
            Tweens.Update(dt);
            Update(dt);
            return;
        }
        float step = world.BeginStep(Game.UpdateCount, dt);
        Age += dt;
        Coroutines.Update(dt);
        Tweens.Update(dt);
        Update(step);
        world.EndStep();
    }

    internal void BeginDraw() => _world?.BeginDraw(Game.UpdateCount, Game.FrameBlend);
}

/// <summary>
/// A scene that ends by returning a value to whoever pushed it (a dialogue's choice, a shop's purchase, a menu's
/// answer): push it with <see cref="SceneManager.Push{TResult}(Scene{TResult}, Action{TResult}, Transition?)"/> and
/// end it with <see cref="Finish"/>.
/// </summary>
/// <typeparam name="TResult">What the scene returns.</typeparam>
public abstract class Scene<TResult> : Scene
{
    /// <summary>
    /// Pops this scene, deferred like <see cref="SceneManager.Pop"/>, and hands <paramref name="result"/> to the callback
    /// it was pushed with, after its <see cref="Scene.Exit"/> and before the scene below receives
    /// <see cref="Scene.Resume"/>. Only the first call counts; nothing happens if the scene is no longer on top when the
    /// change applies. A scene removed any other way (popped, replaced, cleared) returns nothing.
    /// </summary>
    /// <param name="result">The value handed to the callback.</param>
    /// <param name="transition">An optional transition for the pop.</param>
    protected void Finish(TResult result, Transition? transition = null) => Game.Scenes.Finish(this, result, transition);
}

/// <summary>Manages the scene stack and the transitions between scenes.</summary>
public sealed class SceneManager
{
    private readonly Game _game;
    private readonly List<Scene> _stack = [];
    private readonly Queue<Action> _pending = new();
    private readonly Queue<Action> _afterSwap = new();
    private Transition? _transition;
    private Action? _transitionSwap;
    private bool _swapped;

    internal SceneManager(Game game) => _game = game;

    /// <summary>The top scene, or null when the stack is empty.</summary>
    public Scene? Current => _stack.Count > 0 ? _stack[^1] : null;
    /// <summary>Number of scenes on the stack.</summary>
    public int Count => _stack.Count;
    /// <summary>Whether a transition is playing.</summary>
    public bool InTransition => _transition is not null;
    /// <summary>The scenes from bottom (index 0) to top.</summary>
    public IReadOnlyList<Scene> Stack => _stack;

    /// <summary>
    /// Puts <paramref name="scene"/> on top of the stack. The change is deferred: it applies before the next update (or
    /// after <see cref="Core.Game"/>'s <c>Load</c>), or halfway through <paramref name="transition"/> when one is given.
    /// </summary>
    /// <remarks>
    /// Changes keep the order they were requested in. A change requested while a transition plays drops its own
    /// transition; until the playing transition has swapped its scenes, it waits and applies right after the swap.
    /// </remarks>
    public void Push(Scene scene, Transition? transition = null) => Schedule(() => PushNow(scene), transition);
    /// <summary>
    /// Pushes a scene that returns a value, deferred like <see cref="Push(Scene, Transition?)"/>: when it calls
    /// <see cref="Scene{TResult}.Finish"/>, it is popped and <paramref name="onResult"/> receives the value (after the
    /// scene's <see cref="Scene.Exit"/>, before the scene below receives <see cref="Scene.Resume"/>).
    /// </summary>
    public void Push<TResult>(Scene<TResult> scene, Action<TResult> onResult, Transition? transition = null)
    {
        ArgumentNullException.ThrowIfNull(scene);
        ArgumentNullException.ThrowIfNull(onResult);
        Schedule(() =>
        {
            PushNow(scene);
            scene.ResultHandler = value => onResult((TResult)value!);
        }, transition);
    }

    internal void Finish(Scene scene, object? result, Transition? transition)
    {
        if (scene.Finishing) return;
        scene.Finishing = true;
        Schedule(() =>
        {
            if (!ReferenceEquals(Current, scene)) return;
            scene.Result = result;
            scene.HasResult = true;
            PopNow();
        }, transition);
    }
    /// <summary>Removes the top scene, deferred like <see cref="Push"/>. The scene below receives <see cref="Scene.Resume"/>. Does nothing on an empty stack.</summary>
    public void Pop(Transition? transition = null) => Schedule(PopNow, transition);
    /// <summary>Replaces the top scene.</summary>
    public void Replace(Scene scene, Transition? transition = null) => Schedule(() => { PopNow(); PushNow(scene); }, transition);
    /// <summary>Clears the stack and starts over with <paramref name="scene"/>.</summary>
    public void Reset(Scene scene, Transition? transition = null) => Schedule(() => { while (_stack.Count > 0) PopNow(); PushNow(scene); }, transition);

    private void Schedule(Action change, Transition? transition)
    {
        // A transition that has not swapped yet owns the next change; later requests follow it.
        if (_transition is not null && !_swapped) { _afterSwap.Enqueue(change); return; }
        if (transition is null) { _pending.Enqueue(change); return; }
        if (_transition is not null) { _pending.Enqueue(change); return; }
        _transition = transition;
        _transitionSwap = change;
        _swapped = false;
    }

    private void PushNow(Scene scene)
    {
        ArgumentNullException.ThrowIfNull(scene);
        scene.Attach(_game);
        scene.ResultHandler = null;
        scene.Finishing = scene.HasResult = false;
        scene.Result = null;
        _stack.Add(scene);
        _game.Input.Reset();
        scene.Enter();
    }

    private void PopNow()
    {
        if (_stack.Count == 0) return;
        var top = _stack[^1];
        _stack.RemoveAt(_stack.Count - 1);
        top.Coroutines.StopAll();
        top.Tweens.Clear();
        top.Exit();
        var handler = top.HasResult ? top.ResultHandler : null;
        object? result = top.Result;
        top.ResultHandler = null;
        top.HasResult = false;
        top.Result = null;
        handler?.Invoke(result);
        _game.Input.Reset();
        Current?.Resume();
    }

    internal void ApplyPending()
    {
        while (_pending.Count > 0) _pending.Dequeue()();
    }

    internal void Update(float dt)
    {
        ApplyPending();
        if (_transition is { } t)
        {
            t.Advance(dt);
            if (!_swapped && t.Progress >= 0.5f)
            {
                _swapped = true;
                _transitionSwap?.Invoke();
                _transitionSwap = null;
                while (_afterSwap.Count > 0) _afterSwap.Dequeue()();
            }
            if (t.Finished) _transition = null;
            // Scenes keep animating during the "in" half so the new scene is alive when revealed.
            if (!_swapped) return;
        }
        int first = Math.Max(_stack.Count - 1, 0); // an empty stack updates nothing
        while (first > 0 && _stack[first].IsOverlay && !_stack[first].BlocksUpdateBelow) first--;
        for (int i = first; i < _stack.Count; i++) _stack[i].Tick(dt);
        ApplyPending();
    }

    /// <summary>
    /// The stack's state (section <c>engine.scenes</c>: the scenes' types, the changes waiting and the transition), then one
    /// section per scene from the bottom (<c>scene[index]:Type</c>).
    /// </summary>
    internal void HashState(StateHasher hash)
    {
        hash.Section("engine.scenes");
        hash.Add(_stack.Count);
        foreach (var scene in _stack) hash.Add(scene.GetType().FullName);
        hash.Add(_pending.Count);
        hash.Add(_afterSwap.Count);
        hash.Add(_transition is not null);
        if (_transition is { } t)
        {
            hash.Add(t.GetType().FullName);
            hash.Add(t.Duration);
            hash.Add(t.Elapsed);
            hash.Add(_swapped);
        }
        for (int i = 0; i < _stack.Count; i++)
        {
            var scene = _stack[i];
            hash.Section($"scene[{i}]:{scene.GetType().Name}");
            scene.HashAll(hash);
        }
    }

    internal void Draw(IRenderer renderer)
    {
        int first = _stack.Count - 1;
        while (first > 0 && _stack[first].IsOverlay) first--;
        for (int i = Math.Max(first, 0); i < _stack.Count; i++)
        {
            _stack[i].BeginDraw();
            _stack[i].Draw(renderer);
        }
        _transition?.DrawAt(renderer, _game.FrameBlend);
    }

    internal void Clear()
    {
        while (_stack.Count > 0) PopNow();
        _transition = null;
        _pending.Clear();
        _afterSwap.Clear();
    }
}

/// <summary>
/// A full-screen effect that covers the old scene (progress 0..0.5), swaps scenes at 0.5 and
/// reveals the new one (0.5..1). Subclasses draw a cover amount between 0 (clear) and 1 (covered).
/// </summary>
public abstract class Transition(float duration)
{
    /// <summary>Length of the transition in seconds (at least 0.02).</summary>
    public float Duration { get; } = Math.Max(duration, 0.02f);
    /// <summary>Seconds played so far.</summary>
    public float Elapsed { get; private set; }
    /// <summary>Elapsed time as a fraction of <see cref="Duration"/>, from 0 to 1. The scenes swap at 0.5.</summary>
    public float Progress => Math.Clamp(Elapsed / Duration, 0, 1);
    /// <summary>Whether the transition has played to the end.</summary>
    public bool Finished => Elapsed >= Duration;
    /// <summary>
    /// 0 = nothing covered, 1 = fully covered. While drawing, it follows the frame between the last two updates
    /// (<see cref="Core.Game.FrameBlend"/>), so the effect moves evenly on displays of any rate.
    /// </summary>
    public float Cover
    {
        get
        {
            float progress = _blend >= 1 ? Progress : Math.Clamp(float.Lerp(_previous, Elapsed, _blend) / Duration, 0, 1);
            return progress < 0.5f ? Ease.InOutQuad(progress * 2) : Ease.InOutQuad((1 - progress) * 2);
        }
    }

    private float _previous, _blend = 1;

    internal void Advance(float dt)
    {
        _previous = Elapsed;
        Elapsed += dt;
    }

    /// <summary>Draws the effect <paramref name="blend"/> of the way from the previous update to the last.</summary>
    internal void DrawAt(IRenderer renderer, float blend)
    {
        _blend = blend;
        try { Draw(renderer); }
        finally { _blend = 1; }
    }
    /// <summary>Draws the effect over the scenes, using <see cref="Cover"/>.</summary>
    public abstract void Draw(IRenderer renderer);

    /// <summary>Fades to <paramref name="color"/> (black by default) and back.</summary>
    /// <param name="duration">Total length in seconds.</param>
    /// <param name="color">Cover colour; null for black.</param>
    public static Transition Fade(float duration = 0.5f, Color? color = null) => new FadeTransition(duration, color ?? Color.Black);
    /// <summary>A circle closes on <paramref name="center"/> (virtual pixels) and opens again.</summary>
    /// <param name="center">Centre of the circle in virtual pixels.</param>
    /// <param name="duration">Total length in seconds.</param>
    /// <param name="color">Cover colour; null for black.</param>
    public static Transition Iris(Vector2 center, float duration = 0.8f, Color? color = null) => new IrisTransition(duration, center, color ?? Color.Black);
    /// <summary>Growing diamonds sweep diagonally across the screen and shrink away.</summary>
    /// <param name="duration">Total length in seconds.</param>
    /// <param name="color">Cover colour; null for black.</param>
    public static Transition Diamonds(float duration = 0.7f, Color? color = null) => new DiamondTransition(duration, color ?? Color.Black);
    /// <summary>A panel slides in from the left, then leaves to the right.</summary>
    /// <param name="duration">Total length in seconds.</param>
    /// <param name="color">Cover colour; null for black.</param>
    public static Transition Wipe(float duration = 0.6f, Color? color = null) => new WipeTransition(duration, color ?? Color.Black);
}

internal sealed class FadeTransition(float duration, Color color) : Transition(duration)
{
    public override void Draw(IRenderer r) => r.FillRect(new RectF(0, 0, r.Width, r.Height), color.WithAlpha(Cover));
}

/// <summary>Classic closing circle. Drawn as horizontal spans outside the circle, so no stencil is needed.</summary>
internal sealed class IrisTransition(float duration, Vector2 center, Color color) : Transition(duration)
{
    public override void Draw(IRenderer r)
    {
        float maxRadius = MathF.Sqrt(r.Width * r.Width + r.Height * r.Height);
        float radius = maxRadius * (1 - Cover);
        if (radius <= 0.5f) { r.FillRect(new RectF(0, 0, r.Width, r.Height), color); return; }
        for (int y = 0; y < r.Height; y += 2)
        {
            float dy = y + 1 - center.Y;
            float half = radius * radius - dy * dy;
            if (half <= 0) { r.FillRect(new RectF(0, y, r.Width, 2), color); continue; }
            float w = MathF.Sqrt(half);
            float x0 = MathF.Round(center.X - w), x1 = MathF.Round(center.X + w);
            if (x0 > 0) r.FillRect(new RectF(0, y, x0, 2), color);
            if (x1 < r.Width) r.FillRect(new RectF(x1, y, r.Width - x1, 2), color);
        }
    }
}

/// <summary>A grid of growing diamonds sweeping diagonally across the screen.</summary>
internal sealed class DiamondTransition(float duration, Color color) : Transition(duration)
{
    private const int Cell = 16;
    public override void Draw(IRenderer r)
    {
        int cols = r.Width / Cell + 2, rows = r.Height / Cell + 2;
        float span = cols + rows;
        for (int y = 0; y < rows; y++)
        for (int x = 0; x < cols; x++)
        {
            float local = Math.Clamp(Cover * 2f - (x + y) / span, 0, 1);
            if (local <= 0) continue;
            float size = local * Cell;
            var c = new Vector2(x * Cell, y * Cell);
            r.FillTriangle(c + new Vector2(0, -size), c + new Vector2(-size, 0), c + new Vector2(size, 0), color);
            r.FillTriangle(c + new Vector2(-size, 0), c + new Vector2(0, size), c + new Vector2(size, 0), color);
        }
    }
}

internal sealed class WipeTransition(float duration, Color color) : Transition(duration)
{
    public override void Draw(IRenderer r)
    {
        float w = r.Width * Cover;
        if (Progress < 0.5f) r.FillRect(new RectF(0, 0, w, r.Height), color);
        else r.FillRect(new RectF(r.Width - w, 0, w, r.Height), color);
    }
}
