using System.Numerics;
using MicroEngine.Audio;
using MicroEngine.Core;
using MicroEngine.Graphics;
using MicroEngine.Input;

namespace MicroEngine.Scenes;

/// <summary>
/// A screen of the game (title, level, battle, pause menu...). Scenes live on a stack:
/// an overlay scene (dialogue, pause) draws on top of the scene below and can freeze it.
/// Each scene owns its coroutines and tweens, which stop when it leaves the stack.
/// </summary>
public abstract class Scene
{
    private Game? _game;
    public Game Game => _game ?? throw new InvalidOperationException("Scene is not attached to a game.");
    public InputMap Input => Game.Input;
    public AudioMixer Audio => Game.Audio;
    public BitmapFont Font => Game.Font;
    public CoroutineRunner Coroutines { get; } = new();
    public Tweener Tweens { get; } = new();
    /// <summary>Seconds this scene has been updated.</summary>
    public float Age { get; private set; }

    /// <summary>When true, the scene below is drawn first.</summary>
    public virtual bool IsOverlay => false;
    /// <summary>When true (default), scenes below an overlay are not updated.</summary>
    public virtual bool BlocksUpdateBelow => true;

    internal void Attach(Game game) => _game = game;

    public virtual void Enter() { }
    public virtual void Exit() { }
    /// <summary>Called when a scene pushed above this one is popped.</summary>
    public virtual void Resume() { }
    /// <summary>
    /// A short description of the scene's state for development probes and tests (the web host's trace, for example
    /// "x=120 y=88 grounded"); null by default. Never shown to players.
    /// </summary>
    public virtual string? Describe() => null;
    public abstract void Update(float dt);
    public abstract void Draw(IRenderer renderer);

    internal void Tick(float dt)
    {
        Age += dt;
        Coroutines.Update(dt);
        Tweens.Update(dt);
        Update(dt);
    }
}

/// <summary>Manages the scene stack and the transitions between scenes.</summary>
public sealed class SceneManager
{
    private readonly Game _game;
    private readonly List<Scene> _stack = [];
    private readonly Queue<Action> _pending = new();
    private Transition? _transition;
    private Action? _transitionSwap;
    private bool _swapped;

    internal SceneManager(Game game) => _game = game;

    public Scene? Current => _stack.Count > 0 ? _stack[^1] : null;
    public int Count => _stack.Count;
    public bool InTransition => _transition is not null;
    public IReadOnlyList<Scene> Stack => _stack;

    public void Push(Scene scene, Transition? transition = null) => Schedule(() => PushNow(scene), transition);
    public void Pop(Transition? transition = null) => Schedule(PopNow, transition);
    /// <summary>Replaces the top scene.</summary>
    public void Replace(Scene scene, Transition? transition = null) => Schedule(() => { PopNow(); PushNow(scene); }, transition);
    /// <summary>Clears the stack and starts over with <paramref name="scene"/>.</summary>
    public void Reset(Scene scene, Transition? transition = null) => Schedule(() => { while (_stack.Count > 0) PopNow(); PushNow(scene); }, transition);

    private void Schedule(Action change, Transition? transition)
    {
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
            }
            if (t.Finished) _transition = null;
            // Scenes keep animating during the "in" half so the new scene is alive when revealed.
            if (!_swapped) return;
        }
        int first = _stack.Count - 1;
        while (first > 0 && _stack[first].IsOverlay && !_stack[first].BlocksUpdateBelow) first--;
        for (int i = first; i < _stack.Count; i++) _stack[i].Tick(dt);
        ApplyPending();
    }

    internal void Draw(IRenderer renderer)
    {
        int first = _stack.Count - 1;
        while (first > 0 && _stack[first].IsOverlay) first--;
        for (int i = Math.Max(first, 0); i < _stack.Count; i++) _stack[i].Draw(renderer);
        _transition?.Draw(renderer);
    }

    internal void Clear()
    {
        while (_stack.Count > 0) PopNow();
        _transition = null;
        _pending.Clear();
    }
}

/// <summary>
/// A full-screen effect that covers the old scene (progress 0..0.5), swaps scenes at 0.5 and
/// reveals the new one (0.5..1). Subclasses draw a cover amount between 0 (clear) and 1 (covered).
/// </summary>
public abstract class Transition(float duration)
{
    public float Duration { get; } = Math.Max(duration, 0.02f);
    public float Elapsed { get; private set; }
    public float Progress => Math.Clamp(Elapsed / Duration, 0, 1);
    public bool Finished => Elapsed >= Duration;
    /// <summary>0 = nothing covered, 1 = fully covered.</summary>
    public float Cover => Progress < 0.5f ? Ease.InOutQuad(Progress * 2) : Ease.InOutQuad((1 - Progress) * 2);
    internal void Advance(float dt) => Elapsed += dt;
    public abstract void Draw(IRenderer renderer);

    public static Transition Fade(float duration = 0.5f, Color? color = null) => new FadeTransition(duration, color ?? Color.Black);
    public static Transition Iris(Vector2 center, float duration = 0.8f, Color? color = null) => new IrisTransition(duration, center, color ?? Color.Black);
    public static Transition Diamonds(float duration = 0.7f, Color? color = null) => new DiamondTransition(duration, color ?? Color.Black);
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
