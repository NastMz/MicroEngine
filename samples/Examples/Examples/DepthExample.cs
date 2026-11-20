using System.Numerics;
using MicroEngine;
using MicroEngine.Core;
using MicroEngine.Graphics;
using MicroEngine.Scenes;

namespace Examples;

/// <summary>
/// Draw order that does not depend on the order of the calls: a walker among trees, each drawn at the depth of its
/// feet, so the walker goes behind the trees below it and in front of those above; shadows are a layer under all of
/// them and the marker over the walker a layer above. Confirm draws the same calls unsorted, to compare.
/// </summary>
public sealed class DepthExample : Scene
{
    private static readonly Vector2[] Trees =
    [
        new(60, 70), new(110, 120), new(150, 84), new(200, 140), new(236, 96), new(280, 62), new(92, 160), new(262, 158),
    ];

    private readonly SortedRenderer _sorted = new();
    private SpriteAtlas _atlas = null!;
    private bool _unsorted;

    /// <summary>Where the walker's feet are, in canvas pixels.</summary>
    public Vector2 Walker { get; private set; } = new(150, 92);
    /// <summary>Whether the draws are sent in the order of the calls instead of sorted.</summary>
    public bool Unsorted => _unsorted;

    public override void Enter()
    {
        var sheet = new ContentFolder().Image("hero.png");
        _atlas = new SpriteAtlasBuilder().AddGrid("hero", sheet, 16, 16, origin: new Vector2(8, 16)).Build(Game.Graphics);
    }

    public override void Exit() => _atlas.Dispose();

    public override void Update(float dt)
    {
        if (Input.Pressed("pause")) { Game.Scenes.Pop(); return; }
        if (Input.Pressed("confirm")) _unsorted = !_unsorted;
        var move = Input.Vector("left", "right", "up", "down");
        Walker = Vector2.Clamp(Walker + move * 60 * dt, new Vector2(12, 34), new Vector2(308, 172));
    }

    public override void Draw(IRenderer r)
    {
        r.FillRect(new RectF(0, 0, 320, 180), Color.Hex("38b764"));
        if (_unsorted) DrawWorld(r, null);
        else
        {
            #region sorted
            var world = _sorted.Begin(r);      // a SortedRenderer the scene keeps: draws wait until End
            DrawWorld(world, world);
            _sorted.End();                     // sent by layer, then depth, then the order of the calls
            #endregion
        }
        Heading.Draw(r, Font, "DRAW ORDER", _unsorted ? "CONFIRM: SORT" : "CONFIRM: CALL ORDER");
    }

    #region world
    /// <summary>The same calls for both pictures; <paramref name="order"/> is null when nothing sorts them.</summary>
    private void DrawWorld(IRenderer r, SortedRenderer? order)
    {
        // The walker first and the trees after it: in call order every tree would cover the walker.
        order?.At(layer: 0, depth: Walker.Y);                       // the depth of an object on the ground: its feet
        r.Draw(_atlas["hero0"], Walker);
        foreach (var tree in Trees)
        {
            order?.At(layer: -1);                                   // shadows: under everything that stands
            r.FillCircle(tree + new Vector2(0, 1), 9, Color.Black.WithAlpha(0.25f));
            order?.At(layer: 0, depth: tree.Y);
            r.FillRect(new RectF(tree.X - 2, tree.Y - 14, 4, 14), Color.Hex("5d275d"));
            r.FillCircle(tree - new Vector2(0, 22), 13, Color.Hex("257179"));
            r.FillCircle(tree - new Vector2(4, 26), 5, Color.Hex("38b764"));
        }
        order?.At(layer: 1);                                        // over everything, wherever the walker is
        r.FillTriangle(Walker - new Vector2(3, 24), Walker - new Vector2(-3, 24), Walker - new Vector2(0, 19), Color.Hex("ffcd75"));
    }
    #endregion
}
