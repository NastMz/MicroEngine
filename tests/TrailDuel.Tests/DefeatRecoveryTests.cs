using System.Numerics;
using MicroEngine;
using MicroEngine.Graphics;
using TrailDuel.Rules;
using TrailDuel.Scenes;
using TrailDuel.Visuals;

namespace TrailDuel.Tests;

/// <summary>Records, in order, what a scene draws so tests can check which sprites and overlays reach the screen.</summary>
internal sealed class RecordingRenderer : IRenderer
{
    public readonly List<(RectF? Source, RectF? Fill, Color Color)> Ops = [];
    public int Width => 320;
    public int Height => 180;
    public void Clear(Color color) { }
    public void Draw(Texture texture, RectF source, Vector2 position, Color tint, in DrawOptions options = default) => Ops.Add((source, null, tint));
    public void DrawStretched(Texture texture, RectF source, RectF destination, Color tint) => Ops.Add((source, null, tint));
    public void FillRect(RectF rect, Color color) => Ops.Add((null, rect, color));
    public void FillCircle(Vector2 center, float radius, Color color) { }
    public void FillTriangle(Vector2 a, Vector2 b, Vector2 c, Color color) { }
    public void DrawLine(Vector2 from, Vector2 to, float thickness, Color color) { }
    public void PushCamera(Camera2D camera) { }
    public void PopCamera() { }
    public void SetBlendMode(BlendMode mode) { }
    public void SetClip(RectF? screenRect) { }

    /// <summary>Index of the last full-screen fill in <paramref name="color"/>'s RGB (a fade), or -1.</summary>
    public int LastFullScreenFill(Color color) => Ops.FindLastIndex(o =>
        o.Fill is { Width: >= 320, Height: >= 180 } && o.Color.R == color.R && o.Color.G == color.G && o.Color.B == color.B && o.Color.A > 0);
}

/// <summary>
/// Losing a battle: the white-out narration must be readable over the dark screen, and the player must
/// come back at the rest point, drawn and free to walk.
/// </summary>
[TestClass]
public sealed class DefeatRecoveryTests
{
    private sealed record Outcome(OverworldScene World, List<string> UnreadableNarration, int NarrationFrames);

    /// <summary>Starts on Route 1 with one creature at 1 HP, walks into the trainer's sight and loses.</summary>
    private static Outcome LoseOnRouteOne(Harness h)
    {
        h.Step(10);
        while (h.State.Party.Count > 1) h.State.Party.RemoveAt(1);
        h.State.Party[0].Hp = 1;
        var world = (OverworldScene)h.Game.Scenes.Current!;
        h.Game.Input.SetForced("up", true);
        for (int guard = 0; !world.Busy && guard < 600; guard++) h.Step();
        h.Game.Input.SetForced("up", false);
        Assert.IsTrue(world.Busy, "The route trainer should spot the player.");

        var unreadable = new List<string>();
        int narration = 0;
        bool sawBattle = false;
        for (int f = 0; f < 8000; f++)
        {
            sawBattle |= h.Game.Scenes.Current is BattleScene;
            bool onWorld = h.Game.Scenes.Current == world && !h.Game.Scenes.InTransition;
            if (sawBattle && onWorld && !world.Busy && !world.Player.Moving) break;
            if (onWorld && world.Text.Active)
            {
                narration++;
                var r = new RecordingRenderer();
                world.Draw(r);
                int fade = r.LastFullScreenFill(Pal.Ink);
                // Anything drawn after the fade is on top of it; the dialogue must be among it.
                if (fade >= 0 && !r.Ops.Skip(fade + 1).Any(o => o.Source is not null))
                    unreadable.Add($"frame {f} on {h.State.MapId}");
            }
            if (f % 12 == 0) h.Press("confirm", 2); else h.Step();
        }
        Assert.IsTrue(sawBattle, "The trainer battle should start.");
        Assert.AreEqual(0, h.State.BattlesWon);
        return new Outcome(world, unreadable, narration);
    }

    [TestMethod]
    public void WhiteOutNarrationIsDrawnOverTheDarkenedScreen()
    {
        using var h = new Harness("route");
        var outcome = LoseOnRouteOne(h);
        Assert.IsTrue(outcome.NarrationFrames > 0, "The white-out should be narrated.");
        Assert.AreEqual(0, outcome.UnreadableNarration.Count, "Dialogue hidden under the fade: " + string.Join(", ", outcome.UnreadableNarration.Take(5)));
    }

    [TestMethod]
    public void AfterDefeatThePlayerIsBackAtTheRestPointVisibleAndControllable()
    {
        using var h = new Harness("route");
        var world = LoseOnRouteOne(h).World;

        Assert.AreEqual("home", h.State.MapId);
        Assert.AreEqual((4, 6), (world.Player.X, world.Player.Y), "The player must stand on the respawn tile.");
        Assert.AreEqual((4, 6), (h.State.X, h.State.Y));
        Assert.AreEqual(Direction.Down, world.Player.Facing);
        Assert.IsFalse(world.Busy);
        Assert.IsTrue(h.State.Party.All(c => c.Hp == c.MaxHp), "The party is healed.");

        // Rendered: the player's sprite is drawn and no fade is left over the room.
        var r = new RecordingRenderer();
        world.Draw(r);
        var sprite = h.Game.Atlas[world.Player.SpriteKey(world.Player.Frame)].Source;
        Assert.IsTrue(r.Ops.Any(o => o.Source == sprite), "The player sprite should be drawn after respawn.");
        Assert.AreEqual(-1, r.LastFullScreenFill(Pal.Ink), "The screen should be fully faded in.");

        // Controllable: one step up moves the player inside the room.
        h.Game.Input.SetForced("up", true);
        for (int i = 0; i < 40 && world.Player.Y == 6; i++) h.Step();
        h.Game.Input.SetForced("up", false);
        h.Step(30);
        Assert.AreEqual((4, 5), (world.Player.X, world.Player.Y));
        Assert.AreEqual("home", h.State.MapId);
    }
}
