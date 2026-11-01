using System.Numerics;
using MicroEngine;
using MicroEngine.Graphics;
using MicroEngine.Scenes;
using MicroEngine.UI;

namespace GemGarden.Scenes;

/// <summary>An overlay over the frozen round: resume it or go back to the title.</summary>
public sealed class PauseScene(MainGame game) : Scene
{
    private readonly SelectionList _menu = new("RESUME", "QUIT TO TITLE");

    /// <summary>The play scene is drawn first, below this one, and stops updating while this one is on top.</summary>
    public override bool IsOverlay => true;

    public override void Enter()
    {
        _menu.Moved += () => Audio.Play("select");
        Audio.PauseMusic(true);
    }

    public override void Exit() => Audio.PauseMusic(false);

    public override void Update(float dt)
    {
        if (Input.Pressed("pause") || Input.Pressed("cancel"))
        {
            Game.Scenes.Pop();
            return;
        }
        switch (_menu.Update(Input))
        {
            case 0:
                Game.Scenes.Pop();
                break;
            case 1:
                Game.Scenes.Reset(new TitleScene(game), Transition.Fade());
                break;
        }
    }

    public override void Draw(IRenderer r)
    {
        r.FillRect(new RectF(0, 0, Game.Width, Game.Height), Color.Black.WithAlpha(0.5f));
        r.Panel(new RectF(96, 58, 128, 62), UiTheme.Default.Panel);
        Font.DrawCentered(r, "PAUSED", new Vector2(160, 70), UiTheme.Default.Accent);
        _menu.Draw(r, Font, new Vector2(116, 84), UiTheme.Default, Age);
    }
}
