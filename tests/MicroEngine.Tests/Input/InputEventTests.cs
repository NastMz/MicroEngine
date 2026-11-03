using System.Numerics;
using MicroEngine.Core;
using MicroEngine.Input;
using MicroEngine.Kernel;
using MicroEngine.Platform;

namespace MicroEngine.Tests.Input;

/// <summary>
/// The input system through its public contract: device events in (a backend's or sent ones), routed through layers,
/// out as the actions and pointers each player reads.
/// </summary>
[TestClass]
public sealed class InputEventTests
{
    private static readonly DeviceId Keyboard = new(1), Pad1 = new(2), Pad2 = new(3), Mouse = new(4);

    private sealed class Backend : IInputBackend
    {
        public List<InputEvent> Queue { get; } = [];
        public void Drain(List<InputEvent> events)
        {
            events.AddRange(Queue);
            Queue.Clear();
        }
    }

    private sealed class Rig
    {
        public Backend Backend { get; } = new();
        public InputSystem Input { get; }
        public InputMap Map => Input.Players[0].Map;

        public Rig(InputMap? map = null)
        {
            Input = new InputSystem(map ?? InputMap.CreateDefault());
            Input.Connect(Backend);
        }

        public Rig Send(params InputEvent[] events)
        {
            Backend.Queue.AddRange(events);
            return this;
        }

        /// <summary>One frame with one update, as the loop runs it.</summary>
        public void Update()
        {
            Input.BeginFrame();
            Input.Sample();
        }

        public void End() => Input.EndUpdate();
    }

    private static InputEvent Pad(DeviceId id, string name = "pad", GamepadStyle style = GamepadStyle.Xbox) =>
        InputEvent.Added(new InputDeviceInfo(id, DeviceKind.Gamepad, name) { Style = style });

    [TestMethod]
    public void A_key_event_presses_and_releases_the_action_it_is_bound_to()
    {
        var rig = new Rig();
        rig.Send(InputEvent.KeyDown(Keyboard, Key.Space)).Update();
        Assert.IsTrue(rig.Map.Pressed("confirm"));
        Assert.AreEqual(InputDevice.Keyboard, rig.Map.LastDevice);
        rig.End();
        rig.Update();
        Assert.IsTrue(rig.Map.Down("confirm") && !rig.Map.Pressed("confirm"), "held");
        rig.End();
        rig.Send(InputEvent.KeyUp(Keyboard, Key.Space)).Update();
        Assert.IsTrue(rig.Map.Released("confirm"));
    }

    [TestMethod]
    public void A_tap_shorter_than_a_frame_lasts_until_an_update_reads_it_however_many_frames_run_first()
    {
        var rig = new Rig();
        rig.Send(InputEvent.KeyDown(Keyboard, Key.Space), InputEvent.KeyUp(Keyboard, Key.Space));
        // A fast display draws many frames with no update in between: the tap must survive all of them.
        for (int frame = 0; frame < 20; frame++) rig.Input.BeginFrame();
        rig.Input.Sample();
        Assert.IsTrue(rig.Map.Pressed("confirm"), "the tap reaches the first update");
        rig.End();
        rig.Update();
        Assert.IsFalse(rig.Map.Down("confirm"), "read once, then gone");
        Assert.IsTrue(rig.Map.Released("confirm"));
    }

    [TestMethod]
    public void Key_repeats_change_nothing_that_is_held()
    {
        var rig = new Rig();
        rig.Send(InputEvent.KeyDown(Keyboard, Key.Space)).Update();
        rig.End();
        rig.Send(InputEvent.KeyDown(Keyboard, Key.Space, repeat: true)).Update();
        Assert.IsTrue(rig.Map.Down("confirm"));
        Assert.IsFalse(rig.Map.Pressed("confirm"), "a repeat is not a new press");
    }

    [TestMethod]
    public void Gamepad_buttons_and_axes_come_from_events_with_the_pad_style()
    {
        var rig = new Rig();
        rig.Send(Pad(Pad1, "DualSense", GamepadStyle.PlayStation), InputEvent.ButtonDown(Pad1, PadButton.A),
            InputEvent.AxisMoved(Pad1, PadAxis.LeftX, 0.9f)).Update();
        Assert.IsTrue(rig.Map.Pressed("confirm"));
        Assert.IsTrue(rig.Map.Down("right"));
        Assert.AreEqual(GamepadStyle.PlayStation, rig.Map.PadStyle);
        Assert.IsTrue(rig.Map.PadConnected);
        Assert.AreEqual(InputDevice.Gamepad, rig.Map.LastDevice);
    }

    [TestMethod]
    public void Every_pad_connects_and_disconnects_with_its_own_id()
    {
        var rig = new Rig();
        var connected = new List<string>();
        rig.Input.Devices.Connected += d => connected.Add($"+{d.Name}");
        rig.Input.Devices.Disconnected += d => connected.Add($"-{d.Name}");
        rig.Send(Pad(Pad1, "one"), Pad(Pad2, "two")).Update();
        CollectionAssert.AreEqual(new[] { Pad1, Pad2 }, rig.Input.Devices.Devices.Select(d => d.Id).ToArray());
        rig.End();
        rig.Send(InputEvent.Removed(Pad1)).Update();
        CollectionAssert.AreEqual(new[] { Pad2 }, rig.Input.Devices.Devices.Select(d => d.Id).ToArray());
        CollectionAssert.AreEqual(new[] { "+one", "+two", "-one" }, connected);
        Assert.IsTrue(rig.Map.PadConnected, "the second pad is still there");
        Assert.AreEqual(2, rig.Input.Events.Count(e => e.Kind is InputEventKind.DeviceRemoved or InputEventKind.DeviceAdded) + 1,
            "connection changes are events too");
    }

    [TestMethod]
    public void With_no_setup_player_one_hears_every_device()
    {
        var rig = new Rig();
        rig.Send(Pad(Pad1), Pad(Pad2), InputEvent.KeyDown(Keyboard, Key.Left), InputEvent.ButtonDown(Pad2, PadButton.A)).Update();
        Assert.AreEqual(1, rig.Input.Players.Count);
        Assert.IsTrue(rig.Map.Down("left") && rig.Map.Down("confirm"));
        Assert.AreSame(rig.Input.Players[0], rig.Input.Players.OwnerOf(Pad2));
    }

    [TestMethod]
    public void Keyboard_and_pad_of_two_players_do_not_cancel_each_other()
    {
        var rig = new Rig();
        rig.Send(Pad(Pad1)).Update();
        rig.End();
        rig.Input.Players[0].Pair(Keyboard);
        var p2 = rig.Input.Players.Join(Pad1);
        rig.Send(InputEvent.KeyDown(Keyboard, Key.Left), InputEvent.ButtonDown(Pad1, PadButton.DPadRight)).Update();
        Assert.AreEqual(-1, rig.Map.Axis("left", "right"), "player 1 goes left on the keyboard");
        Assert.AreEqual(1, p2.Map.Axis("left", "right"), "player 2 goes right on the pad");
        Assert.AreEqual(InputDevice.Keyboard, rig.Map.LastDevice);
        Assert.AreEqual(InputDevice.Gamepad, p2.Map.LastDevice);
        Assert.IsFalse(rig.Map.PadConnected, "player 1 has no pad");
        Assert.IsTrue(p2.Map.PadConnected);
    }

    [TestMethod]
    public void Once_player_one_has_a_device_free_devices_drive_nobody_and_ask_to_join()
    {
        var rig = new Rig();
        rig.Input.Players[0].Pair(Keyboard);
        var asked = new List<DeviceId>();
        rig.Input.Players.UnpairedPress += (device, e) =>
        {
            asked.Add(device);
            rig.Input.Players.Join(device);
        };
        rig.Send(Pad(Pad2), InputEvent.ButtonDown(Pad2, PadButton.Start)).Update();
        CollectionAssert.AreEqual(new[] { Pad2 }, asked);
        Assert.IsFalse(rig.Map.Down("pause"), "player 1 does not hear the free pad");
        Assert.AreEqual(2, rig.Input.Players.Count);
        Assert.IsTrue(rig.Input.Players[1].Map.Pressed("pause"), "the joining press counts for the new player");
        Assert.AreSame(rig.Input.Players[1], rig.Input.Players.OwnerOf(Pad2));
    }

    [TestMethod]
    public void A_joining_player_gets_a_copy_of_player_ones_bindings_and_settings()
    {
        var first = InputMap.CreateDefault().Bind("dash", Key.LeftShift);
        first.StickMode = StickDirections.Four;
        var players = new Players(first);
        var p2 = players.Join(Pad1);
        Assert.AreNotSame(first, p2.Map);
        CollectionAssert.AreEquivalent(first.Actions.ToArray(), p2.Map.Actions.ToArray());
        Assert.AreEqual(StickDirections.Four, p2.Map.StickMode);
        Assert.AreEqual(0, p2.Map.Sources.Count, "action sources (on-screen controls) stay player 1's");
        Assert.AreEqual(1, p2.Index);
        Assert.ThrowsExactly<ArgumentException>(() => players.Remove(players[0]));
        Assert.IsTrue(players.Remove(p2));
        Assert.AreEqual(1, players.Count);
    }

    [TestMethod]
    public void A_layer_that_takes_a_press_also_takes_its_release_and_gameplay_never_sees_either()
    {
        var rig = new Rig();
        var menu = new RecordingLayer(e => e.Kind == InputEventKind.KeyDown && e.Key == Key.Escape);
        rig.Input.Layers.Add(menu, InputLayerOrder.UI);
        rig.Send(InputEvent.KeyDown(Keyboard, Key.Escape)).Update();
        Assert.IsFalse(rig.Map.Down("pause"), "the menu took Escape");
        rig.End();
        rig.Input.Layers.Remove(menu);
        rig.Send(InputEvent.KeyUp(Keyboard, Key.Escape)).Update();
        Assert.IsFalse(rig.Map.Released("pause"));
        CollectionAssert.AreEqual(new[] { InputEventKind.KeyDown, InputEventKind.KeyUp }, menu.Seen.Select(e => e.Kind).ToArray(),
            "the release goes to the layer that took the press, even after it left");
        Assert.AreEqual(0, rig.Input.Events.Count);
    }

    [TestMethod]
    public void Layers_see_events_from_the_highest_order_down()
    {
        var rig = new Rig();
        var order = new List<string>();
        rig.Input.Layers.Add(new RecordingLayer(_ => { order.Add("game"); return false; }), InputLayerOrder.Game);
        rig.Input.Layers.Add(new RecordingLayer(_ => { order.Add("ui"); return false; }), InputLayerOrder.UI);
        rig.Input.Layers.Add(new RecordingLayer(_ => { order.Add("touch"); return true; }), InputLayerOrder.TouchControls);
        rig.Send(InputEvent.KeyDown(Keyboard, Key.Z)).Update();
        CollectionAssert.AreEqual(new[] { "ui", "touch" }, order, "the touch layer took it, so the game layer never saw it");
        Assert.IsFalse(rig.Map.Down("confirm"));
    }

    [TestMethod]
    public void A_pointer_a_layer_took_stays_the_layers_until_it_lifts()
    {
        var rig = new Rig();
        var button = new RecordingLayer(e => e.Kind == InputEventKind.PointerDown && e.Position.X < 10);
        rig.Input.Layers.Add(button, InputLayerOrder.UI);
        rig.Send(InputEvent.PointerDown(Mouse, 0, PointerKind.Mouse, new(5, 5)), InputEvent.PointerMove(Mouse, 0, PointerKind.Mouse, new(50, 5)),
            InputEvent.PointerUp(Mouse, 0, PointerKind.Mouse, new(50, 5))).Update();
        Assert.AreEqual(3, button.Seen.Count, "the drag and the release belong to the button");
        Assert.IsFalse(rig.Map.Pointer.Primary);
        rig.End();
        rig.Send(InputEvent.PointerMove(Mouse, 0, PointerKind.Mouse, new(60, 5))).Update();
        Assert.AreEqual(4, button.Seen.Count, "after the release, moves pass through the layers again");
        Assert.AreEqual(new Vector2(60, 5), rig.Map.Pointer.Position, "and reach gameplay");
    }

    [TestMethod]
    public void Touches_are_pointers_with_ids_and_a_tap_is_read_once()
    {
        var rig = new Rig();
        rig.Send(InputEvent.PointerDown(DeviceId.TouchScreen, 7, PointerKind.Touch, new(10, 20)),
            InputEvent.PointerDown(DeviceId.TouchScreen, 9, PointerKind.Touch, new(30, 40))).Update();
        CollectionAssert.AreEqual(new long[] { 7, 9 }, rig.Map.Pointers.Select(p => p.Id).ToArray());
        Assert.IsTrue(rig.Map.Pointers.All(p => p.Down && p.Kind == PointerKind.Touch));
        Assert.IsTrue(rig.Map.Pointer is { Present: true, Primary: true }, "the last finger is also the pointer");
        rig.End();
        rig.Send(InputEvent.PointerUp(DeviceId.TouchScreen, 7, PointerKind.Touch, new(10, 20)),
            InputEvent.PointerUp(DeviceId.TouchScreen, 9, PointerKind.Touch, new(30, 40))).Update();
        Assert.AreEqual(0, rig.Map.Pointers.Count, "lifted fingers that were already seen go");
        rig.End();
        rig.Send(InputEvent.PointerDown(DeviceId.TouchScreen, 11, PointerKind.Touch, new(1, 2)),
            InputEvent.PointerUp(DeviceId.TouchScreen, 11, PointerKind.Touch, new(1, 2))).Update();
        Assert.AreEqual(1, rig.Map.Pointers.Count, "a tap within one frame is there for one update");
        Assert.IsTrue(rig.Map.Pointers[0].Down);
        rig.End();
        rig.Update();
        Assert.AreEqual(0, rig.Map.Pointers.Count);
        Assert.IsFalse(rig.Map.Pointer.Present);
    }

    [TestMethod]
    public void The_wheel_adds_up_within_an_update_and_starts_again_after_it()
    {
        var rig = new Rig();
        rig.Send(InputEvent.PointerMove(Mouse, 0, PointerKind.Mouse, new(3, 4)), InputEvent.WheelMoved(Mouse, new(0, 1)),
            InputEvent.WheelMoved(Mouse, new(0, 1.5f))).Update();
        Assert.AreEqual(new Vector2(0, 2.5f), rig.Map.Pointer.Wheel);
        Assert.AreEqual(new Vector2(3, 4), rig.Map.Pointer.Position);
        rig.End();
        rig.Update();
        Assert.AreEqual(Vector2.Zero, rig.Map.Pointer.Wheel);
        Assert.IsTrue(rig.Map.Pointer.Present, "the mouse stays over the window");
        rig.End();
        rig.Send(new InputEvent { Kind = InputEventKind.PointerLeave, Device = Mouse, PointerKind = PointerKind.Mouse }).Update();
        Assert.IsFalse(rig.Map.Pointer.Present);
    }

    [TestMethod]
    public void Text_arrives_only_while_text_input_is_on_and_the_backend_is_told()
    {
        var rig = new Rig();
        var control = new HeadlessInputBackend();
        rig.Input.Connect(rig.Backend, text: control);
        rig.Send(InputEvent.TextInput(Keyboard, "x")).Update();
        Assert.AreEqual("", rig.Input.Text.Typed, "off: no text");
        rig.End();
        rig.Input.Text.Start(new RectF(10, 20, 2, 8));
        Assert.AreEqual((true, new RectF(10, 20, 2, 8)), control.TextInput);
        rig.Send(InputEvent.Composition(Keyboard, "ni"), InputEvent.TextInput(Keyboard, "日"), InputEvent.TextInput(Keyboard, "本")).Update();
        Assert.AreEqual("日本", rig.Input.Text.Typed);
        Assert.AreEqual("", rig.Input.Text.Composition, "committing ends the composition");
        rig.End();
        rig.Update();
        Assert.AreEqual("", rig.Input.Text.Typed, "each update sees its own text");
        rig.Input.Text.Stop();
        Assert.IsFalse(control.TextInput.Active);
    }

    [TestMethod]
    public void Rumble_reaches_the_pads_a_player_hears_and_only_those()
    {
        var rig = new Rig();
        var backend = new HeadlessInputBackend();
        rig.Input.Connect(rig.Backend, rumble: backend);
        rig.Send(Pad(Pad1), Pad(Pad2)).Update();
        var p2 = rig.Input.Players.Join(Pad2);
        Assert.IsTrue(rig.Input.Rumble(p2, 0.5f, 2f, 0.2));
        CollectionAssert.AreEqual(new[] { (Pad2, 0.5f, 1f, 0.2) }, backend.Rumbles, "clamped to 0..1, only player 2's pad");
        Assert.IsFalse(new InputSystem(InputMap.CreateDefault()).Rumble(Pad1, 1, 1, 1), "no backend, no rumble");
    }

    [TestMethod]
    public void Listening_for_a_press_rebinds_an_action_and_gameplay_never_sees_that_press()
    {
        var rig = new Rig();
        InputEvent? heard = null;
        using (rig.Input.ListenForPress(e => heard = e))
        {
            rig.Send(InputEvent.KeyDown(Keyboard, Key.Q)).Update();
        }
        Assert.IsNotNull(heard);
        Assert.IsTrue(rig.Map.BindFrom("confirm", heard.Value, replace: true));
        Assert.AreEqual(0, rig.Input.Layers.Count, "the listener left after one press");
        CollectionAssert.AreEqual(new[] { Key.Q }, rig.Map.GetBindings("confirm")!.Keys.ToArray());
        Assert.IsFalse(rig.Map.Down("confirm"), "the press that chose the key does not also confirm");
        rig.End();
        rig.Send(InputEvent.KeyUp(Keyboard, Key.Q)).Update();
        Assert.IsFalse(rig.Map.Released("confirm"));
        rig.End();
        rig.Send(InputEvent.KeyDown(Keyboard, Key.Q)).Update();
        Assert.IsTrue(rig.Map.Pressed("confirm"), "the new binding works");
    }

    [TestMethod]
    public void Focus_loss_releases_what_was_held()
    {
        var rig = new Rig();
        rig.Send(InputEvent.KeyDown(Keyboard, Key.Space)).Update();
        rig.End();
        rig.Input.ReleaseAll();
        rig.Update();
        Assert.IsTrue(rig.Map.Released("confirm"));
    }

    [TestMethod]
    public void Events_of_an_unannounced_device_announce_it()
    {
        var rig = new Rig();
        rig.Send(InputEvent.ButtonDown(new DeviceId(42), PadButton.A)).Update();
        Assert.AreEqual(DeviceKind.Gamepad, rig.Input.Devices.Find(new DeviceId(42))!.Kind);
        Assert.IsTrue(rig.Map.Pressed("confirm"));
    }

    [TestMethod]
    public void Each_event_no_layer_took_is_seen_by_exactly_one_update()
    {
        var rig = new Rig();
        rig.Send(InputEvent.KeyDown(Keyboard, Key.A));
        rig.Input.BeginFrame();
        rig.Send(InputEvent.KeyUp(Keyboard, Key.A));
        rig.Input.BeginFrame();
        rig.Input.Sample();
        CollectionAssert.AreEqual(new[] { InputEventKind.KeyDown, InputEventKind.KeyUp }, rig.Input.Events.Select(e => e.Kind).ToArray(),
            "frames without an update keep their events for the next update");
        rig.End();
        rig.Input.Sample();
        Assert.AreEqual(0, rig.Input.Events.Count, "a second update in the same frame sees none");
    }

    [TestMethod]
    public void A_single_player_without_pairings_adds_nothing_to_the_state_hash()
    {
        var rig = new Rig();
        var hash = new StateHasher();
        rig.Input.HashState(hash);
        ulong empty = new StateHasher().Value;
        Assert.AreEqual(empty, hash.Value);
        rig.Send(Pad(Pad1)).Update();
        rig.Input.Players.Join(Pad1);
        hash.Reset();
        rig.Input.HashState(hash);
        ulong joined = hash.Value;
        Assert.AreNotEqual(empty, joined);
        rig.End();
        rig.Send(InputEvent.ButtonDown(Pad1, PadButton.A)).Update();
        hash.Reset();
        rig.Input.HashState(hash);
        Assert.AreNotEqual(joined, hash.Value, "player 2's actions are simulation state");
    }

    private sealed class PolledBackend : BufferedInputBackend
    {
        public void Press(Key key, bool down) => Emit(down ? InputEvent.KeyDown(Keyboard, key) : InputEvent.KeyUp(Keyboard, key));
        public void Plug(DeviceId pad) => Emit(Pad(pad));
        public void Button(DeviceId pad, bool down) => Emit(down ? InputEvent.ButtonDown(pad, PadButton.A) : InputEvent.ButtonUp(pad, PadButton.A));
        public void Blur() => ReleaseHeld(5);
    }

    [TestMethod]
    public void A_buffered_backend_queues_events_and_answers_polled_questions_from_them()
    {
        var backend = new PolledBackend();
        backend.BeginFrame();
        backend.Press(Key.Space, true);
        backend.Press(Key.Space, false);
        Assert.IsTrue(backend.IsKeyDown(Key.Space), "a tap reads as held in its frame");
        backend.BeginFrame();
        Assert.IsFalse(backend.IsKeyDown(Key.Space), "and only in its frame");
        backend.Plug(Pad1);
        backend.Button(Pad1, true);
        backend.Press(Key.Left, true);
        Assert.IsTrue(backend.PadConnected && backend.IsPadDown(PadButton.A));
        backend.Blur();
        backend.BeginFrame();
        Assert.IsFalse(backend.IsKeyDown(Key.Left) || backend.IsPadDown(PadButton.A), "losing the focus releases everything");
        var drained = new List<InputEvent>();
        backend.Drain(drained);
        CollectionAssert.AreEqual(new[]
        {
            InputEventKind.KeyDown, InputEventKind.KeyUp, InputEventKind.DeviceAdded, InputEventKind.ButtonDown, InputEventKind.KeyDown,
            InputEventKind.KeyUp, InputEventKind.ButtonUp,
        }, drained.Select(e => e.Kind).ToArray());
        Assert.IsTrue(drained.TakeLast(2).All(e => e.Time == 5));
        backend.Drain(drained);
        Assert.AreEqual(7, drained.Count, "a drain moves the events out");
        for (int i = 0; i < 10_000; i++) backend.Press(Key.Z, i % 2 == 0);
        drained.Clear();
        backend.Drain(drained);
        Assert.IsTrue(drained.Count <= 4096, "nobody draining keeps a bounded backlog, the newest events");
        Assert.AreEqual(InputEventKind.KeyUp, drained[^1].Kind);
    }

    [TestMethod]
    public void Keys_map_to_usb_hid_usages_both_ways()
    {
        Assert.AreEqual(4, KeyUsages.Usage(Key.A));
        Assert.AreEqual(39, KeyUsages.Usage(Key.D0));
        Assert.AreEqual(Key.Keypad0, KeyUsages.FromHidUsage(98));
        Assert.AreEqual(Key.F13, KeyUsages.FromHidUsage(104));
        Assert.AreEqual(Key.None, KeyUsages.FromHidUsage(102), "a usage the engine does not name");
        foreach (var key in KeyUsages.All) Assert.AreEqual(key, KeyUsages.FromHidUsage(KeyUsages.Usage(key)), key.ToString());
    }

    private sealed class RecordingLayer(Func<InputEvent, bool> take) : IInputLayer
    {
        public List<InputEvent> Seen { get; } = [];
        public bool Handle(in InputEvent e)
        {
            Seen.Add(e);
            return take(e);
        }
    }
}

/// <summary>The input system inside a running game: the platform's backend, polled sources and the touch screen.</summary>
[TestClass]
public sealed class GameInputTests
{
    private sealed class PlainGame : Game
    {
        public override GameSettings Settings { get; } = new("input");
        public List<string> Lines { get; } = [];
        protected override void Load() => Scenes.Push(new LogScene(this));
    }

    private sealed class LogScene(PlainGame game) : Scenes.Scene
    {
        public override void Update(float dt)
        {
            foreach (var p in Input.Pointers) game.Lines.Add($"pointer {p.Id} {p.Position.X:0},{p.Position.Y:0} {(p.Down ? "down" : "up")}");
            if (Input.Pressed("jump")) game.Lines.Add("jump");
            if (Input.Pressed("confirm")) game.Lines.Add("confirm");
        }

        public override void Draw(Graphics.IRenderer renderer) { }
    }

    [TestMethod]
    public void The_platforms_backend_events_and_hand_set_keys_both_reach_player_one()
    {
        var platform = new HeadlessPlatform();
        var game = new PlainGame();
        var loop = new GameLoop(game, platform);
        Assert.IsTrue(loop.Start());
        platform.Devices.Send(InputEvent.KeyDown(new DeviceId(1), Key.Z));
        loop.Tick();
        Assert.IsTrue(game.Lines.Contains("confirm"), "a backend event");
        platform.Devices.Send(InputEvent.KeyUp(new DeviceId(1), Key.Z));
        loop.Tick();
        platform.Keys.Set(Key.Space, true);
        game.Lines.Clear();
        loop.Tick();
        Assert.IsTrue(game.Lines.Contains("confirm"), "a polled source is one more device");
        Assert.AreEqual(1, game.InputSystem.PolledSources.Count);
        loop.Finish();
    }

    [TestMethod]
    public void A_finger_on_a_control_presses_it_and_a_finger_elsewhere_is_a_gameplay_pointer_in_canvas_pixels()
    {
        var platform = new HeadlessPlatform();
        var game = new TouchLayoutGame();
        var loop = new GameLoop(game, platform);
        Assert.IsTrue(loop.Start());
        loop.Tick();
        var screen = platform.TouchScreen;
        // The screen is 4x the 320x180 canvas with no controls in the way of the frame: its middle is the canvas's.
        screen.Down(1, game.Touch.Geometry("jump").Center);
        screen.Down(2, new Vector2(640, 360));
        loop.Tick();
        Assert.IsTrue(game.Lines.Contains("jump"), string.Join(", ", game.Lines));
        var pointers = game.Lines.Where(l => l.StartsWith("pointer")).ToList();
        Assert.AreEqual(1, pointers.Count, "only the finger no control took reaches gameplay: " + string.Join(", ", pointers));
        StringAssert.StartsWith(pointers[0], "pointer 2 ");
        var frame = game.Touch.LastFrame;
        var expected = FrameLayout.ToCanvas(new Vector2(640, 360), frame, 320, 180);
        Assert.AreEqual($"pointer 2 {expected.X:0},{expected.Y:0} down", pointers[0]);
        loop.Finish();
    }

    [TestMethod]
    public void A_ui_layer_above_the_touch_controls_takes_a_finger_before_they_do()
    {
        var platform = new HeadlessPlatform();
        var game = new TouchLayoutGame();
        var loop = new GameLoop(game, platform);
        Assert.IsTrue(loop.Start());
        loop.Tick();
        game.InputSystem.Layers.Add(new TakeEverything(), InputLayerOrder.UI);
        platform.TouchScreen.Down(1, game.Touch.Geometry("jump").Center);
        game.Lines.Clear();
        loop.Tick();
        Assert.AreEqual(0, game.Lines.Count, "neither the control nor gameplay saw the finger: " + string.Join(", ", game.Lines));
        Assert.IsFalse(game.Touch.IsPressed("jump"));
        loop.Finish();
    }

    [TestMethod]
    public void Prompts_name_keys_as_the_players_layout_prints_them()
    {
        var platform = new HeadlessPlatform();
        // An AZERTY layout: the keys in W and A's places print Z and Q.
        platform.Devices.Labels[Key.W] = "Z";
        platform.Devices.Labels[Key.A] = "Q";
        var game = new PlainGame();
        game.InitializeForTest(platform);
        game.Prompts.Override = InputDevice.Keyboard;
        Assert.AreEqual("↑←↓→ / ZQSD", game.Prompts.Directions(separator: " / "));
        Assert.AreEqual("Q", game.Prompts.KeyText(Key.A));
        Assert.AreEqual("Space", game.Prompts.KeyText(Key.Space), "keys the layout does not label keep the engine's name");
        Assert.AreEqual("Num 5", ActionPrompts.KeyName(Key.Keypad5));
        Assert.AreEqual("-", ActionPrompts.KeyName(Key.Minus));
    }

    private sealed class TakeEverything : IInputLayer
    {
        public bool Handle(in InputEvent e) => true;
    }

    private sealed class TouchLayoutGame : Game
    {
        public override GameSettings Settings { get; } = new("touch");
        public List<string> Lines { get; } = [];
        protected override InputMap CreateInput() => InputMap.CreateDefault().Bind("jump", Key.Space);
        protected override TouchLayout CreateTouchLayout() => TouchLayout.Of(
            new TouchButton("jump", "jump", "A") { Anchor = TouchAnchor.BottomRight, Offset = new(70, 80), Radius = 36 });
        protected override void Load() => Scenes.Push(new TouchLogScene(this));
    }

    private sealed class TouchLogScene(TouchLayoutGame game) : Scenes.Scene
    {
        public override void Update(float dt)
        {
            foreach (var p in Input.Pointers) game.Lines.Add($"pointer {p.Id} {p.Position.X:0},{p.Position.Y:0} {(p.Down ? "down" : "up")}");
            if (Input.Pressed("jump")) game.Lines.Add("jump");
        }

        public override void Draw(Graphics.IRenderer renderer) { }
    }
}

/// <summary>Action consumption, engine action names and saved bindings of <see cref="InputMap"/>.</summary>
[TestClass]
public sealed class InputMapFeatureTests
{
    [TestMethod]
    public void A_consumed_action_reads_as_up_for_everyone_until_it_is_let_go()
    {
        var map = InputMap.CreateDefault();
        var keys = new HeadlessInput();
        keys.Set(Key.Z, true);
        map.Sample(keys);
        Assert.IsTrue(map.Pressed("confirm"), "the menu reads it first");
        Assert.IsTrue(map.Consume("confirm"));
        Assert.IsFalse(map.Pressed("confirm") || map.Down("confirm"), "the scene below does not");
        Assert.AreEqual(0, map.Value("confirm"));
        map.EndUpdate();
        map.Sample(keys);
        Assert.IsFalse(map.Down("confirm"), "still held: still taken");
        keys.Set(Key.Z, false);
        map.EndUpdate();
        map.Sample(keys);
        Assert.IsFalse(map.Released("confirm"), "its release is hidden too");
        keys.Set(Key.Z, true);
        map.EndUpdate();
        map.Sample(keys);
        Assert.IsTrue(map.Pressed("confirm"), "the next press is new");
        Assert.IsFalse(map.Consume("cancel"), "an action that is up cannot be consumed");
    }

    [TestMethod]
    public void The_engines_action_names_can_be_renamed()
    {
        var names = ActionNames.Default with { Confirm = "accept", Left = "west" };
        var map = InputMap.CreateDefault(names);
        Assert.AreSame(names, map.Names);
        Assert.IsNotNull(map.GetBindings("accept"));
        Assert.IsNull(map.GetBindings("confirm"));
        CollectionAssert.Contains(map.GetBindings("west")!.Keys.ToArray(), Key.A);
        Assert.AreEqual("confirm", InputMap.CreateDefault().Names.Confirm);
    }

    [TestMethod]
    public void Engine_widgets_controls_and_prompts_follow_renamed_actions()
    {
        var names = ActionNames.Default with { Confirm = "accept", Up = "north", Down = "south" };
        var map = InputMap.CreateDefault(names);
        var keys = new HeadlessInput();
        var list = new UI.SelectionList("a", "b");
        keys.Set(Key.S, true);
        map.Sample(keys);
        Assert.IsNull(list.Update(map));
        Assert.AreEqual(1, list.Index, "the renamed down action moves the cursor");
        keys.Set(Key.S, false);
        keys.Set(Key.Z, true);
        map.EndUpdate();
        map.Sample(keys);
        Assert.AreEqual(1, list.Update(map), "the renamed confirm action chooses");

        var layout = TouchLayout.CreateDefault(names);
        CollectionAssert.Contains(layout.Controls.OfType<TouchButton>().Select(b => b.Action).ToArray(), "accept");
        Assert.AreEqual("north", layout.Controls.OfType<TouchDPad>().Single().Up);
        Assert.AreEqual("confirm", layout.Controls.OfType<TouchButton>().First().Id, "control ids keep the default names");

        var prompts = new ActionPrompts(map);
        Assert.AreEqual(prompts.Directions(map.Names.Left, map.Names.Right, "north", "south"), prompts.Directions());
    }

    [TestMethod]
    public void A_game_that_renames_its_actions_gets_a_default_touch_layout_that_presses_them()
    {
        var game = new RenamedGame();
        game.InitializeForTest(new HeadlessPlatform());
        CollectionAssert.Contains(game.Touch.Actions.ToArray(), "accept");
        CollectionAssert.DoesNotContain(game.Touch.Actions.ToArray(), "confirm");
    }

    private sealed class RenamedGame : Game
    {
        public override GameSettings Settings { get; } = new("renamed");
        protected override InputMap CreateInput() => InputMap.CreateDefault(ActionNames.Default with { Confirm = "accept" });
        protected override void Load() { }
    }

    [TestMethod]
    public void Bindings_survive_a_save_and_load_and_only_touch_the_actions_the_map_has()
    {
        var storage = new MemoryStorage();
        var saved = InputMap.CreateDefault().Unbind("confirm").Bind("confirm", [Key.Q, Key.Keypad5], [PadButton.RightPaddle1])
            .BindAxis("confirm", PadAxis.RightTrigger).Bind("gone", Key.G);
        saved.SaveBindings(storage);

        var loaded = InputMap.CreateDefault();
        Assert.IsTrue(loaded.LoadBindings(storage));
        var b = loaded.GetBindings("confirm")!;
        CollectionAssert.AreEqual(new[] { Key.Q, Key.Keypad5 }, b.Keys.ToArray());
        CollectionAssert.AreEqual(new[] { PadButton.RightPaddle1 }, b.Buttons.ToArray());
        CollectionAssert.AreEqual(new[] { (PadAxis.RightTrigger, true) }, b.Axes.ToArray());
        Assert.IsNull(loaded.GetBindings("gone"), "an action the game no longer has does not come back");
        CollectionAssert.AreEqual(InputMap.CreateDefault().GetBindings("left")!.Keys.ToArray(), loaded.GetBindings("left")!.Keys.ToArray());
    }

    [TestMethod]
    public void Missing_or_unreadable_bindings_keep_the_defaults_and_unreadable_ones_are_set_aside()
    {
        var storage = new MemoryStorage();
        var map = InputMap.CreateDefault();
        Assert.IsFalse(map.LoadBindings(storage), "nothing saved");
        storage.Write(InputMap.BindingsSlot, "{ not json");
        Assert.IsFalse(map.LoadBindings(storage));
        Assert.IsTrue(storage.SetAsideSlots.Contains(InputMap.BindingsSlot));
        CollectionAssert.AreEqual(new[] { Key.Z, Key.Space, Key.Enter, Key.J }, map.GetBindings("confirm")!.Keys.ToArray());
    }

    [TestMethod]
    public void Saved_names_this_version_does_not_know_are_skipped()
    {
        var set = new InputBindingSet { Actions = [new ActionBindingData { Action = "confirm", Keys = ["Q", "NoSuchKey"], Axes = ["Nope+", "LeftX-"] }] };
        var map = InputMap.CreateDefault();
        Assert.AreEqual(1, map.ImportBindings(InputBindingSet.FromJson(set.ToJson())));
        var b = map.GetBindings("confirm")!;
        CollectionAssert.AreEqual(new[] { Key.Q }, b.Keys.ToArray());
        CollectionAssert.AreEqual(new[] { (PadAxis.LeftX, false) }, b.Axes.ToArray());
        Assert.ThrowsExactly<FormatException>(() => InputBindingSet.FromJson("[]"));
    }

    private sealed class MemoryStorage : IStorage
    {
        private readonly Dictionary<string, string> _slots = [];
        public HashSet<string> SetAsideSlots { get; } = [];
        public string Location => "memory";
        public string? Read(string slot) => _slots.GetValueOrDefault(slot);
        public void Write(string slot, string text) => _slots[slot] = text;
        public bool Exists(string slot) => _slots.ContainsKey(slot);
        public void Delete(string slot) => _slots.Remove(slot);
        public void SetAside(string slot)
        {
            SetAsideSlots.Add(slot);
            _slots.Remove(slot);
        }
    }
}
