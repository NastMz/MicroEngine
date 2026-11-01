using System.Numerics;
using MicroEngine;
using MicroEngine.Graphics;
using MicroEngine.Scenes;

namespace GemGarden.Scenes;

/// <summary>Where the game will happen. For now: a green screen and a way back to the title.</summary>
public sealed class PlayScene(MainGame game) : Scene
{
    public override void Update(float dt)
    {
        if (Input.Pressed("cancel"))
            Game.Scenes.Replace(new TitleScene(game), Transition.Fade());
    }

    public override void Draw(IRenderer r)
    {
        r.FillRect(new RectF(0, 0, Game.Width, Game.Height), Color.Hex("38b764"));
        Font.DrawCentered(r, "THE GARDEN", new Vector2(160, 80), Color.White);
        Font.DrawCentered(r, $"{Age:0.0} SECONDS HERE - CANCEL TO GO BACK", new Vector2(160, 100), Color.Hex("1a1c2c"));
    }
}
