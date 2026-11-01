using System.Numerics;
using MicroEngine;
using MicroEngine.Graphics;
using MicroEngine.Scenes;
using MicroEngine.UI;

namespace GemGarden.Scenes;

/// <summary>The title screen: a menu to play or quit, and the best score from the save file.</summary>
public sealed class TitleScene(MainGame game) : Scene
{
    private readonly SelectionList _menu = new("PLAY", "QUIT");

    public override void Enter()
    {
        _menu.Moved += () => Audio.Play("select");
        Audio.PlayMusic("garden");
    }

    public override void Update(float dt)
    {
        switch (_menu.Update(Input))
        {
            case 0:
                Audio.Play("select");
                Game.Scenes.Replace(new PlayScene(game), Transition.Fade());
                break;
            case 1:
                Game.Quit();
                break;
        }
    }

    public override void Draw(IRenderer r)
    {
        Font.DrawCentered(r, "GEM GARDEN", new Vector2(160, 44), Color.Hex("a7f070"), scale: 3, shadow: Color.Hex("257179"));
        r.Draw(game.Sprites["hero0"], new Vector2(160, 86));
        _menu.Draw(r, Font, new Vector2(146, 104), UiTheme.Default, Age);
        Font.DrawCentered(r, $"BEST {game.BestScore}", new Vector2(160, 150), UiTheme.Default.Muted);
    }
}
