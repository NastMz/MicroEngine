#pragma warning disable CS0618 // SelectionList and DialogueBox, deprecated, are tested until they are removed
using System.Numerics;
using MicroEngine.Core;
using MicroEngine.Input;
using MicroEngine.Kernel;

namespace MicroEngine.Tests.Input;

/// <summary>
/// A two-player scenario through the whole loop, driven by a script's device events: player 1 on the keyboard, player 2
/// joining with a gamepad. The two never cancel each other, and the run is reproducible.
/// </summary>
[TestClass]
public sealed class LocalMultiplayerTests
{
    /// <summary>Two squares on one screen: one per player, moved by that player's actions.</summary>
    private sealed class Duo : Game
    {
        public override GameSettings Settings { get; } = new("duo", 160, 90);
        public Vector2[] Positions { get; } = [new(40, 45), new(120, 45)];
        public List<string> Joined { get; } = [];

        protected override void Load()
        {
            var players = InputSystem.Players;
            // Player 1 plays on the keyboard; a press on a free pad joins as player 2.
            players[0].Pair(InputScript.ScriptKeyboard);
            players.UnpairedPress += (device, press) =>
            {
                if (players.Count >= 2 || press.Kind != InputEventKind.ButtonDown) return;
                players.Join(device);
                Joined.Add($"{device} at {UpdateCount}");
            };
            Scenes.Push(new Field(this));
        }

        public override void HashState(StateHasher hash)
        {
            foreach (var p in Positions) hash.Add(p);
        }
    }

    private sealed class Field(Duo game) : Scenes.Scene
    {
        public override void Update(float dt)
        {
            var players = game.InputSystem.Players;
            for (int i = 0; i < players.Count; i++)
                game.Positions[i] += players[i].Map.Vector("left", "right", "up", "down") * 60 * dt;
        }

        public override void Draw(Graphics.IRenderer renderer) { }
    }

    private const string Script = """
        5 key Left down
        10 pad 1 add playstation
        12 pad 1 Start down
        13 pad 1 Start up
        20 pad 1 DPadRight down
        50 pad 1 DPadRight up
        50 key Left up
        60 quit
        """;

    [TestMethod]
    public void A_keyboard_player_and_a_pad_player_move_apart_without_cancelling_each_other()
    {
        var game = new Duo();
        var result = GameRunner.Run(game, new HeadlessPlatform(), new RunOptions { Script = InputScript.Parse(Script) });
        Assert.IsNull(result.Error, result.Error?.ToString());
        CollectionAssert.AreEqual(new[] { $"{InputScript.ScriptPad(1)} at 12" }, game.Joined, "the pad joined with its first press");
        Assert.AreEqual(2, game.InputSystem.Players.Count);
        // Player 1 held Left for 45 updates; player 2 held Right for 30. Each moved only by their own device.
        Assert.AreEqual(40 - 45, game.Positions[0].X, 0.01f);
        Assert.AreEqual(120 + 30, game.Positions[1].X, 0.01f);
        Assert.AreEqual(GamepadStyle.PlayStation, game.InputSystem.Players[1].Map.PadStyle);
        Assert.IsFalse(game.Input.PadConnected, "player 1 has no pad");
    }

    [TestMethod]
    public void The_two_player_run_is_reproducible()
    {
        StateTrace Run()
        {
            var trace = new StateTrace();
            GameRunner.Run(new Duo(), new HeadlessPlatform(), new RunOptions { Script = InputScript.Parse(Script), StateTrace = trace });
            return trace;
        }
        var first = Run();
        var second = Run();
        Assert.IsNull(StateTrace.FirstDivergence(first, second), "the same script gives the same trace");
    }
}

/// <summary>Scripted device events: parsing and routing through the loop.</summary>
[TestClass]
public sealed class ScriptedDeviceTests
{
    private sealed class Probe : Game
    {
        public override GameSettings Settings { get; } = new("probe", 160, 90);
        public List<string> Lines { get; } = [];
        protected override void Load()
        {
            InputSystem.Text.Start();
            Scenes.Push(new Watch(this));
        }
    }

    private sealed class Watch(Probe game) : Scenes.Scene
    {
        public override void Update(float dt)
        {
            long u = game.UpdateCount;
            if (Input.Pressed("confirm")) game.Lines.Add($"{u} confirm {Input.LastDevice}");
            if (Input.Pointer.Wheel != Vector2.Zero) game.Lines.Add(FormattableString.Invariant($"{u} wheel {Input.Pointer.Wheel.Y}"));
            if (Input.Pointer.Primary) game.Lines.Add(FormattableString.Invariant($"{u} click {Input.Pointer.Position.X},{Input.Pointer.Position.Y}"));
            if (game.InputSystem.Text.Typed.Length > 0) game.Lines.Add($"{u} text {game.InputSystem.Text.Typed}");
            if (Input.Value("right") > 0) game.Lines.Add(FormattableString.Invariant($"{u} right {Input.Value("right"):0.00}"));
        }

        public override void Draw(Graphics.IRenderer renderer) { }
    }

    [TestMethod]
    public void Script_device_commands_reach_the_game_through_the_input_layers()
    {
        var game = new Probe();
        var script = InputScript.Parse("""
            3 keytap Z
            6 pad 2 add nintendo
            7 pad 2 A down
            8 pad 2 A up
            10 pad 2 LeftX 1
            11 pad 2 LeftX 0
            15 mouse 20,30 down
            16 mouse 20,30 up
            20 wheel -1
            25 text Ana Lía
            30 quit
            """);
        GameRunner.Run(game, new HeadlessPlatform(), new RunOptions { Script = script });
        CollectionAssert.AreEqual(new[]
        {
            "4 confirm Keyboard", "8 confirm Gamepad", "11 right 1.00", "16 click 20,30", "21 wheel -1", "26 text Ana Lía",
        }, game.Lines);
    }

    [TestMethod]
    public void A_layer_sees_scripted_device_events_before_gameplay()
    {
        var game = new Probe();
        var taken = new List<InputEventKind>();
        var layer = new Taker(taken);
        var platform = new HeadlessPlatform();
        var loop = new GameLoop(game, platform, new RunOptions { Script = InputScript.Parse("2 keytap Z\n4 quit") });
        Assert.IsTrue(loop.Start());
        game.InputSystem.Layers.Add(layer, InputLayerOrder.UI);
        while (loop.Tick()) { }
        loop.Finish();
        CollectionAssert.AreEqual(new[] { InputEventKind.KeyDown, InputEventKind.KeyUp }, taken.ToArray());
        Assert.IsFalse(game.Lines.Any(l => l.Contains("confirm")), "the layer took the press");
    }

    private sealed class Taker(List<InputEventKind> taken) : IInputLayer
    {
        public bool Handle(in InputEvent e)
        {
            if (e.Kind is not (InputEventKind.KeyDown or InputEventKind.KeyUp)) return false;
            taken.Add(e.Kind);
            return true;
        }
    }

    [TestMethod]
    public void Malformed_device_commands_name_their_line()
    {
        foreach (var line in new[] { "5 key Space sideways", "5 key Nope down", "5 pad 0 add", "5 pad 1 Jump down", "5 mouse 1 down", "5 text" })
        {
            var e = Assert.ThrowsExactly<FormatException>(() => InputScript.Parse("1 press confirm\n" + line), line);
            StringAssert.Contains(e.Message, "line 2", line);
        }
    }
}

/// <summary>Engine widgets take the presses they act on, so the scene below an overlay does not act on them too.</summary>
[TestClass]
public sealed class WidgetConsumptionTests
{
    [TestMethod]
    public void A_menu_that_chooses_and_a_dialogue_that_advances_consume_their_confirm()
    {
        var map = InputMap.CreateDefault();
        var keys = new HeadlessInput();
        keys.Set(Key.Z, true);
        map.Sample(keys);
        var menu = new UI.SelectionList("a", "b");
        Assert.AreEqual(0, menu.Update(map));
        Assert.IsFalse(map.Pressed("confirm"), "the scene under the menu does not also confirm");

        var map2 = InputMap.CreateDefault();
        map2.Sample(keys);
        var box = new UI.DialogueBox();
        box.Show(null, "hello", "world");
        Assert.IsTrue(box.Update(0.01f, map2));
        Assert.IsFalse(map2.Pressed("confirm"));

        var map3 = InputMap.CreateDefault();
        map3.Sample(keys);
        new UI.DialogueBox().Update(0.01f, map3);
        Assert.IsTrue(map3.Pressed("confirm"), "a closed box takes nothing");
    }
}
