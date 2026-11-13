using System.Numerics;
using MicroEngine;
using MicroEngine.Graphics;
using MicroEngine.Scenes;
using MicroEngine.Tiled;
using MicroEngine.World;

namespace TiledLevel;

/// <summary>
/// The level of <see cref="TiledLevelGame.Map"/>, played: the hero runs and jumps over the objects whose
/// <c>bodyType</c> property the level designer set, and collects the objects of class <c>coin</c>.
/// </summary>
public sealed class LevelScene(TiledLevelGame game) : Scene
{
    public const float Gravity = 2600, RunSpeed = 420, JumpSpeed = 1250, MaxFall = 1600;
    // The hero's sprite is 128 × 160; it collides with a narrower box at its feet.
    public static readonly Vector2 HeroBox = new(64, 140);

    private readonly List<RectF> _solids = [];
    private readonly List<TiledObject> _coins = [];
    // The map has no tile layers: everything solid is an object, so the grid is empty and open at its edges.
    private readonly TileGrid _noTiles = new(new TileLayer(1, 1), 32, new Dictionary<int, TileFlags>()) { OutsideFlags = TileFlags.None };
    private TiledObject _heroSprite = null!;
    private RectF _hero;
    private Vector2 _velocity;
    private bool _facingLeft;

    /// <summary>The hero's collision box, in map pixels.</summary>
    public RectF Hero => _hero;
    /// <summary>The solid rectangles read from the map.</summary>
    public IReadOnlyList<RectF> Solids => _solids;
    /// <summary>How many coins the map holds and how many are left.</summary>
    public int CoinsTotal { get; private set; }
    public int CoinsLeft => _coins.Count;
    /// <summary>Whether the hero stood on something after the last step.</summary>
    public bool OnGround { get; private set; }

    public override void Enter()
    {
        var map = game.Map;
        #region level
        // The map is data: this game decides what its classes and properties mean.
        foreach (var o in map.Objects)
        {
            if (o.Properties.String("bodyType") is "static" or "dynamic") _solids.Add(o.TurnedBounds);
            if (o.Class == "coin") _coins.Add(o);
            if (o.Class == "hero")
            {
                _heroSprite = o;
                // The box stands where the sprite's feet are: a tile object's position is its bottom-left corner.
                _hero = new RectF(o.Position.X + (o.Size.X - HeroBox.X) / 2, o.Position.Y - HeroBox.Y, HeroBox.X, HeroBox.Y);
            }
        }
        #endregion
        CoinsTotal = _coins.Count;
        WorldCamera = Game.CreateCamera();
        WorldCamera.Bounds = new RectF(0, 0, map.PixelWidth, map.PixelHeight);
        WorldCamera.CenterOn(_hero.Center);
    }

    public override void Update(float dt)
    {
        float run = Input.Axis("left", "right");
        if (run != 0) _facingLeft = run < 0;
        _velocity.X = run * RunSpeed;
        _velocity.Y = MathF.Min(_velocity.Y + Gravity * dt, MaxFall);
        if (OnGround && Input.Pressed("confirm")) _velocity.Y = -JumpSpeed;
        var move = TileMotion.Move(_hero, _velocity * dt, _noTiles, _solids);
        _hero = move.Bounds;
        if (move.HitFloor || move.HitCeiling) _velocity.Y = 0;
        OnGround = TileMotion.IsOnGround(_hero, _noTiles, _solids);
        _coins.RemoveAll(c => c.TurnedBounds.Intersects(_hero));
        WorldCamera!.CenterOn(_hero.Center);
    }

    public override void Draw(IRenderer r)
    {
        #region draw
        // Layer by layer, as Tiled shows them, each shifted by its parallax. The "game" layer's objects move, so the
        // scene draws them where the game has them instead of where the map placed them.
        var map = game.Map;
        foreach (var layer in map.ObjectLayers)
        {
            var shift = map.ParallaxShift(layer, WorldCamera!.Center);
            if (layer.Name != "game") game.Sprites.Draw(r, layer, WorldCamera, Age, shift);
            else
            {
                foreach (var coin in _coins) game.Sprites.Draw(r, coin, Age);
                var feet = new Vector2(_hero.Center.X - _heroSprite.Size.X / 2, _hero.Bottom);
                game.Sprites.Draw(r, _heroSprite with { Position = feet, Orientation = _facingLeft ? TileOrientation.FlipX : TileOrientation.None });
            }
        }
        #endregion
    }

    public override void DrawUi(IRenderer ui) =>
        Font.Draw(ui, $"COINS {CoinsTotal - CoinsLeft}/{CoinsTotal}   MOVE: ARROWS   JUMP: Z", new Vector2(16, 16), Color.White, 2);
}
