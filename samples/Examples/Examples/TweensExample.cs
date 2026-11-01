using System.Collections;
using System.Numerics;
using MicroEngine;
using MicroEngine.Graphics;
using MicroEngine.Scenes;

namespace Examples;

/// <summary>Tweens animate a value over time with an easing curve; a coroutine runs a sequence of steps over many updates.</summary>
public sealed class TweensExample : Scene
{
    private float _x = 40, _scale = 1;
    private string _caption = "";

    #region tween
    private void Slide()
    {
        float from = _x, to = _x < 160 ? 280 : 40;
        // Every update until the duration ends: apply(eased progress 0..1). Then done.
        Tweens.Add(0.8f, t => _x = MathX.Lerp(from, to, t), Ease.OutBounce, done: () => _caption = "LANDED");
    }
    #endregion

    #region coroutine
    private IEnumerator Countdown()
    {
        foreach (var word in new[] { "THREE", "TWO", "ONE" })
        {
            _caption = word;
            Tweens.Add(0.3f, t => _scale = 1 + (1 - t));           // a little pop on each word
            yield return Wait.Seconds(0.6f);                        // simulated seconds, not frames
        }
        _caption = "PRESS CONFIRM";
        yield return Wait.Until(() => Input.Pressed("confirm"));    // checked every update
        _caption = "GO!";
        Slide();
    }
    #endregion

    public override void Enter() => Coroutines.Start(Countdown());   // stopped automatically when the scene leaves

    public override void Update(float dt)
    {
        if (Input.Pressed("pause")) { Game.Scenes.Pop(); return; }
        if (Input.Pressed("action")) Slide();
    }

    public override void Draw(IRenderer r)
    {
        r.FillCircle(new Vector2(_x, 110), 10, Color.Hex("ffcd75"));
        Font.DrawCentered(r, _caption, new Vector2(160, 60), Color.White, scale: 2 * _scale);
        Heading.Draw(r, Font, "TWEENS AND COROUTINES", "ACTION: SLIDE");
    }
}
