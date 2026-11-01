using System.Numerics;
using MicroEngine;
using MicroEngine.Core;
using MicroEngine.Graphics;
using MicroEngine.Scenes;

namespace Examples;

/// <summary>An Animator with a looping walk and a one-shot clip, its speed changed with left and right.</summary>
public sealed class AnimationExample : Scene
{
    #region setup
    private SpriteAtlas _atlas = null!;
    private readonly Animator _animator = new();

    public override void Enter()
    {
        _atlas = new SpriteAtlasBuilder()
            .AddGrid("hero", new ContentFolder().Image("hero.png"), 16, 16, origin: new Vector2(8, 16))
            .Build(Game.Graphics);
        var frames = _atlas.Frames("hero");
        _animator
            .Add("walk", new AnimationClip(frames, framesPerSecond: 8))                   // loops
            .Add("hop", new AnimationClip([frames[1], frames[3], frames[1]], 6, loop: false)); // plays once
        _animator.Play("walk");
    }

    public override void Exit() => _atlas.Dispose();
    #endregion

    #region update
    public override void Update(float dt)
    {
        if (Input.Pressed("pause")) { Game.Scenes.Pop(); return; }
        if (Input.Pressed("confirm")) _animator.Play("hop", restart: true);
        if (_animator.CurrentName == "hop" && _animator.Finished) _animator.Play("walk");
        _animator.Speed = Math.Clamp(_animator.Speed + Input.Axis("left", "right") * dt, 0.25f, 3f);
        _animator.Update(dt);
    }
    #endregion

    public override void Draw(IRenderer r)
    {
        r.DrawPivoted(_animator.Frame, new Vector2(160, 110), scale: new Vector2(4));
        Font.DrawCentered(r, $"{_animator.CurrentName}  SPEED x{_animator.Speed:0.00}", new Vector2(160, 130), Color.White);
        Heading.Draw(r, Font, "ANIMATION", "LEFT/RIGHT: SPEED  CONFIRM: HOP");
    }
}
