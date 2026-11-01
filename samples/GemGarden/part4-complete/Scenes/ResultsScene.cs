using System.Numerics;
using MicroEngine;
using MicroEngine.Graphics;
using MicroEngine.Scenes;
using MicroEngine.UI;

namespace GemGarden.Scenes;

/// <summary>The end of a round: how many gems, whether it is a new best, then back to the title.</summary>
public sealed class ResultsScene(MainGame game, int gems, int total, bool newBest) : Scene
{
    private const float InputDelay = 0.6f;   // so a button held at the end of the round does not skip this screen

    public override void Update(float dt)
    {
        if (Age > InputDelay && Input.Pressed("confirm"))
            Game.Scenes.Replace(new TitleScene(game), Transition.Fade());
    }

    public override void Draw(IRenderer r)
    {
        var heading = gems == total ? "EVERY GEM FOUND!" : "THE SUN HAS SET";
        Font.DrawCentered(r, heading, new Vector2(160, 48), Color.Hex("ffcd75"), scale: 2, shadow: Color.Black);
        Font.DrawCentered(r, $"YOU FOUND {gems} OF {total} GEMS", new Vector2(160, 84), Color.White);
        Font.DrawCentered(r, newBest ? "NEW BEST!" : $"BEST {game.BestScore}", new Vector2(160, 100), UiTheme.Default.Accent);
        if (Age > InputDelay) Font.DrawCentered(r, "PRESS CONFIRM", new Vector2(160, 140), UiTheme.Default.Muted);
    }
}
