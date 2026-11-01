using System.Numerics;
using MicroEngine;
using MicroEngine.Graphics;
using MicroEngine.Scenes;

namespace GemGarden.Scenes;

/// <summary>The title screen: the game's name until the player presses confirm.</summary>
public sealed class TitleScene(MainGame game) : Scene
{
    #region music
    public override void Enter() => Audio.PlayMusic("garden");
    #endregion

    public override void Update(float dt)
    {
        if (Input.Pressed("confirm"))
            Game.Scenes.Replace(new PlayScene(game), Transition.Fade());
    }

    public override void Draw(IRenderer r)
    {
        Font.DrawCentered(r, "GEM GARDEN", new Vector2(160, 60), Color.Hex("a7f070"), scale: 3, shadow: Color.Hex("257179"));
        Font.DrawCentered(r, "PRESS CONFIRM", new Vector2(160, 120), Color.White);
    }
}
