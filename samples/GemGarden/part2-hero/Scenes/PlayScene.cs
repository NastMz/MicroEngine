using System.Numerics;
using MicroEngine;
using MicroEngine.Graphics;
using MicroEngine.Scenes;
using GemGarden.World;

namespace GemGarden.Scenes;

/// <summary>The hero on a lawn: move with the arrows, a gamepad or the touch joystick; hold run to go faster.</summary>
public sealed class PlayScene : Scene
{
    #region fields
    private readonly MainGame _game;
    private readonly Player _player;

    public PlayScene(MainGame game)
    {
        _game = game;
        _player = new Player(game.Sprites, new Vector2(160, 100));
    }
    #endregion

    #region update
    public override void Update(float dt)
    {
        if (Input.Pressed("pause"))
        {
            Game.Scenes.Replace(new TitleScene(_game), Transition.Fade());
            return;
        }
        _player.Update(dt, Input, new RectF(0, 0, Game.Width, Game.Height));
    }
    #endregion

    #region draw
    public override void Draw(IRenderer r)
    {
        // The same 16x16 grass sprite, drawn over the whole screen.
        var grass = _game.Sprites["grass"];
        for (int y = 0; y < Game.Height; y += grass.Height)
        for (int x = 0; x < Game.Width; x += grass.Width)
            r.Draw(grass, new Vector2(x, y));

        _player.Draw(r);
        Font.DrawShadowed(r, "MOVE: ARROWS, WASD, STICK   RUN: SHIFT, X, B", new Vector2(4, 4), Color.White, Color.Black);
    }
    #endregion
}
