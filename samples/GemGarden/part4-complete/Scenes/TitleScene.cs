using System.Numerics;
using MicroEngine;
using MicroEngine.Graphics;
using MicroEngine.Scenes;
using MicroEngine.UI;

namespace GemGarden.Scenes;

/// <summary>The title screen: a menu to play or quit, and the best score from the save file.</summary>
public sealed class TitleScene(MainGame game) : Scene
{
    public override void Enter()
    {
        Audio.PlayMusic("garden");
        Widgets.Content = new Stack { Spacing = 4 }
            .Add(new Button("PLAY", Play) { Width = 64 })
            .Add(new Button("QUIT", Game.Quit) { Width = 64 });
        Widgets.Offset = new Vector2(0, 30);                           // below the title, from the centre
        Widgets.FocusChanged += change =>
        {
            if (change.From is not null) Audio.Play("select");         // a click when the focus moves
        };
    }

    private void Play()
    {
        Audio.Play("select");
        Game.Scenes.Replace(new PlayScene(game), Transition.Fade());
    }

    public override void Update(float dt) { }

    public override void Draw(IRenderer r)
    {
        Font.DrawCentered(r, "GEM GARDEN", new Vector2(160, 44), Color.Hex("a7f070"), scale: 3, shadow: Color.Hex("257179"));
        r.Draw(game.Sprites["hero0"], new Vector2(160, 80));
        Font.DrawCentered(r, $"BEST {game.BestScore}", new Vector2(160, 164), UiTheme.Default.Muted);
    }
}
