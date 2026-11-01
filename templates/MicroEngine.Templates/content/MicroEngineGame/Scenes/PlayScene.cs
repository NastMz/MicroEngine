using System.Numerics;
using MicroEngine;
using MicroEngine.Graphics;
using MicroEngine.Scenes;

namespace MyGame.Scenes;

/// <summary>
/// Move the hero with the arrow keys, a gamepad or the on-screen controls, and collect coins. Input is read as actions
/// ("left", "confirm"...), never as keys, so every device works the same; the game's own actions are bound in
/// MainGame.CreateInput.
/// </summary>
public sealed class PlayScene(MainGame game) : Scene
{
    private Vector2 _hero = new(160, 100);
    private Vector2 _coin = new(240, 70);
    private int _coins;

    public override void Update(float dt)
    {
        var move = new Vector2(Input.Axis("left", "right"), Input.Axis("up", "down"));
        if (move.LengthSquared() > 1) move = Vector2.Normalize(move);
        _hero = Vector2.Clamp(_hero + move * 90 * dt, new Vector2(8, 24), new Vector2(312, 172));
        if (Vector2.Distance(_hero, _coin) < 10)
        {
            _coins++;
            Audio.Play("coin");
            _coin = new Vector2(Game.Random.Next(16, 304), Game.Random.Next(32, 164));
        }
    }

    public override void Draw(IRenderer r)
    {
        Font.Draw(r, $"Coins: {_coins}", new Vector2(8, 6), Color.Hex("fff1e8"));
        r.FillCircle(_coin + new Vector2(0, MathX.Wave(Age, 2f, 1.5f)), 4, Color.Hex("ffec27"));
        r.Draw(game.Sprites["hero"], _hero);
        Font.DrawCentered(r, "Arrows, a gamepad or touch to move", new Vector2(160, 168), Color.Hex("c2c3c7"));
    }
}
