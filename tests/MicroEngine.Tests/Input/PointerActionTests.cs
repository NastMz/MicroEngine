using System.Numerics;
using MicroEngine.Input;
using MicroEngine.Kernel;

namespace MicroEngine.Tests.Input;

/// <summary>
/// The pointer as part of actions: mouse buttons, the wheel and pointer motion bound, read, consumed by layers, rebound
/// by listening and saved like keys; the pointer's own edges and motion; the right stick's dead zone and directions.
/// </summary>
[TestClass]
public sealed class PointerActionTests
{
    private static readonly DeviceId Keyboard = new(1), Pad = new(2), Mouse = new(4), Mouse2 = new(5);

    private sealed class Rig(InputMap map)
    {
        public InputSystem Input { get; } = new(map);
        public InputMap Map => Input.Players[0].Map;

        public Rig Update(params InputEvent[] events)
        {
            foreach (var e in events) Input.Send(e);
            Input.BeginFrame();
            Input.Sample();
            return this;
        }

        public Rig End()
        {
            Input.EndUpdate();
            return this;
        }
    }

    private static InputEvent Down(PointerButton button = PointerButton.Primary, float x = 10, float y = 10, DeviceId? device = null) =>
        InputEvent.PointerDown(device ?? Mouse, 0, PointerKind.Mouse, new Vector2(x, y), button);

    private static InputEvent Up(PointerButton button = PointerButton.Primary, float x = 10, float y = 10, DeviceId? device = null) =>
        InputEvent.PointerUp(device ?? Mouse, 0, PointerKind.Mouse, new Vector2(x, y), button);

    private static InputEvent Move(float x, float y) => InputEvent.PointerMove(Mouse, 0, PointerKind.Mouse, new Vector2(x, y));

    private static InputEvent Wheel(float y, float x = 0) => InputEvent.WheelMoved(Mouse, new Vector2(x, y), new Vector2(10, 10));

    [TestMethod]
    public void A_mouse_button_presses_holds_and_releases_the_action_it_is_bound_to()
    {
        var rig = new Rig(new InputMap().Bind("order", PointerButton.Secondary, Key.E));
        rig.Update(Down(PointerButton.Secondary));
        Assert.IsTrue(rig.Map.Pressed("order") && rig.Map.Down("order"));
        Assert.AreEqual(1f, rig.Map.Value("order"));
        Assert.AreEqual(InputDevice.Mouse, rig.Map.LastDevice);
        rig.End().Update();
        Assert.IsTrue(rig.Map.Down("order") && !rig.Map.Pressed("order"), "held");
        rig.End().Update(Up(PointerButton.Secondary));
        Assert.IsTrue(rig.Map.Released("order"));
        rig.End().Update(Down());
        Assert.IsFalse(rig.Map.Down("order"), "another button is another control");
    }

    [TestMethod]
    public void A_click_shorter_than_a_frame_still_presses_its_action_for_one_update()
    {
        var rig = new Rig(new InputMap().Bind("select", PointerButton.Primary));
        rig.Update(Down(), Up());
        Assert.IsTrue(rig.Map.Pressed("select"));
        rig.End().Update();
        Assert.IsTrue(rig.Map.Released("select"));
    }

    [TestMethod]
    public void The_back_and_forward_buttons_are_read_and_bound_like_the_others()
    {
        var rig = new Rig(new InputMap().Bind("previous", PointerButton.Back).Bind("next", PointerButton.Forward));
        rig.Update(Down(PointerButton.Back));
        Assert.IsTrue(rig.Map.Pressed("previous") && !rig.Map.Down("next"));
        Assert.IsTrue(rig.Map.Pointer.Back && rig.Map.Pointer.Down(PointerButton.Back) && !rig.Map.Pointer.Forward);
        rig.End().Update(Up(PointerButton.Back), Down(PointerButton.Forward));
        Assert.IsTrue(rig.Map.Released("previous") && rig.Map.Pressed("next"));
        Assert.IsTrue(rig.Map.Pointer.Forward);
    }

    [TestMethod]
    public void A_wheel_notch_presses_its_action_for_one_update_and_every_notch_is_a_new_press()
    {
        var rig = new Rig(new InputMap().Bind("next", InputControl.WheelUp).Bind("previous", InputControl.WheelDown));
        rig.Update(Wheel(1));
        Assert.IsTrue(rig.Map.Pressed("next") && rig.Map.Down("next"));
        Assert.IsFalse(rig.Map.Down("previous"));
        Assert.AreEqual(InputDevice.Mouse, rig.Map.LastDevice);
        rig.End().Update(Wheel(1));
        Assert.IsTrue(rig.Map.Pressed("next"), "a notch in the very next update is a second press, not a held one");
        rig.End().Update();
        Assert.IsTrue(rig.Map.Released("next") && !rig.Map.Down("next"), "pressed for one update");
        rig.End().Update();
        Assert.IsFalse(rig.Map.Released("next") || rig.Map.Pressed("next"));
        rig.End().Update(Wheel(-1));
        Assert.IsTrue(rig.Map.Pressed("previous") && !rig.Map.Down("next"));
    }

    [TestMethod]
    public void The_wheels_value_is_the_notches_turned_in_the_update_and_a_smooth_wheel_presses_once_per_whole_notch()
    {
        var rig = new Rig(new InputMap().Bind("zoom-in", InputControl.WheelUp).Bind("zoom-out", InputControl.WheelDown)
            .Bind("pan-right", InputControl.WheelRight));
        rig.Update(Wheel(1), Wheel(1), Wheel(1));
        Assert.AreEqual(3f, rig.Map.Value("zoom-in"), "three notches between two updates");
        Assert.AreEqual(3f, rig.Map.AnalogAxis("zoom-out", "zoom-in"));
        rig.End();
        // A touchpad sends fractions of a notch: the value follows it, a press comes once a whole notch has turned.
        int presses = 0;
        for (int i = 0; i < 10; i++)
        {
            rig.Update(Wheel(0.25f));
            Assert.AreEqual(0.25f, rig.Map.Value("zoom-in"), 1e-5f);
            if (rig.Map.Pressed("zoom-in")) presses++;
            rig.End();
        }
        Assert.AreEqual(2, presses, "2.5 notches turned: two presses");
        rig.Update(Wheel(0, 1));
        Assert.IsTrue(rig.Map.Pressed("pan-right") && !rig.Map.Down("zoom-in"), "the horizontal wheel is its own control");
    }

    [TestMethod]
    public void Pointer_motion_is_an_analog_value_scaled_by_the_maps_sensitivity_and_never_a_press()
    {
        var map = new InputMap { PointerSensitivity = 0.5f }
            .Bind("look-left", InputControl.PointerLeft).Bind("look-right", InputControl.PointerRight)
            .Bind("look-up", InputControl.PointerUp).Bind("look-down", InputControl.PointerDown);
        var rig = new Rig(map);
        rig.Update(Move(100, 100));
        Assert.AreEqual(Vector2.Zero, rig.Map.Motion("look-left", "look-right", "look-up", "look-down"), "a pointer that appears has not moved yet");
        rig.End().Update(Move(110, 96));
        Assert.AreEqual(new Vector2(5, -2), rig.Map.Motion("look-left", "look-right", "look-up", "look-down"), "10 right and 4 up, at half sensitivity");
        Assert.AreEqual(5f, rig.Map.Value("look-right"));
        Assert.AreEqual(0f, rig.Map.Value("look-left"));
        Assert.IsFalse(rig.Map.Down("look-right") || rig.Map.Pressed("look-right"));
        rig.End().Update();
        Assert.AreEqual(Vector2.Zero, rig.Map.Motion("look-left", "look-right", "look-up", "look-down"), "motion lasts the update it happened in");
    }

    [TestMethod]
    public void The_pointer_reports_the_edges_of_every_button_and_how_far_it_moved_in_the_update()
    {
        var rig = new Rig(InputMap.CreateDefault());
        rig.Update(Move(20, 20));
        rig.End().Update(Move(26, 21), Move(30, 25), Down(PointerButton.Middle, 30, 25));
        var p = rig.Map.Pointer;
        Assert.AreEqual(new Vector2(10, 5), p.Delta, "the whole way since the previous update");
        Assert.IsTrue(p.Pressed(PointerButton.Middle) && p.Down(PointerButton.Middle) && !p.Released(PointerButton.Middle));
        Assert.IsFalse(p.Pressed(PointerButton.Primary));
        Assert.AreEqual(PointerKind.Mouse, p.Kind);
        rig.End().Update();
        p = rig.Map.Pointer;
        Assert.IsTrue(p.Down(PointerButton.Middle) && !p.Pressed(PointerButton.Middle), "held");
        Assert.AreEqual(Vector2.Zero, p.Delta);
        rig.End().Update(Up(PointerButton.Middle, 30, 25));
        Assert.IsTrue(rig.Map.Pointer.Released(PointerButton.Middle) && !rig.Map.Pointer.Down(PointerButton.Middle));
        rig.End().Update();
        Assert.IsFalse(rig.Map.Pointer.Released(PointerButton.Middle));
    }

    [TestMethod]
    public void A_pointer_event_that_carries_its_own_motion_moves_the_pointer_by_it_even_when_the_position_stays()
    {
        // A locked cursor does not move: the backend reports how far the mouse did.
        var rig = new Rig(new InputMap().Bind("look-right", InputControl.PointerRight));
        rig.Update(Move(50, 50));
        rig.End().Update(InputEvent.PointerMove(Mouse, 0, PointerKind.Mouse, new Vector2(50, 50), delta: new Vector2(7, -3)),
            InputEvent.PointerMove(Mouse, 0, PointerKind.Mouse, new Vector2(50, 50), delta: new Vector2(2, 0)));
        Assert.AreEqual(new Vector2(9, -3), rig.Map.Pointer.Delta);
        Assert.AreEqual(new Vector2(50, 50), rig.Map.Pointer.Position);
        Assert.AreEqual(9f, rig.Map.Value("look-right"));
    }

    private sealed class Panel(RectF area) : IInputLayer
    {
        public bool Handle(in InputEvent e) =>
            e.Kind is InputEventKind.PointerDown or InputEventKind.Wheel && area.Contains(e.Position);
    }

    [TestMethod]
    public void A_click_or_a_wheel_turn_a_layer_took_presses_no_action()
    {
        var rig = new Rig(new InputMap().Bind("select", PointerButton.Primary).Bind("order", PointerButton.Secondary).Bind("zoom-in", InputControl.WheelUp));
        rig.Input.Layers.Add(new Panel(new RectF(0, 0, 50, 50)), InputLayerOrder.UI);
        rig.Update(Down(PointerButton.Primary, 10, 10), InputEvent.WheelMoved(Mouse, new Vector2(0, 1), new Vector2(10, 10)));
        Assert.IsFalse(rig.Map.Down("select") || rig.Map.Down("zoom-in"), "the panel took them");
        Assert.AreEqual(0f, rig.Map.Value("zoom-in"));
        rig.End().Update(Up(PointerButton.Primary, 10, 10));
        Assert.IsFalse(rig.Map.Released("select"));
        rig.End().Update(Down(PointerButton.Primary, 200, 100), InputEvent.WheelMoved(Mouse, new Vector2(0, 1), new Vector2(200, 100)));
        Assert.IsTrue(rig.Map.Pressed("select") && rig.Map.Pressed("zoom-in"), "outside the panel they are the game's");
    }

    [TestMethod]
    public void A_secondary_click_a_layer_took_does_not_take_the_next_primary_click_with_it()
    {
        var rig = new Rig(new InputMap().Bind("select", PointerButton.Primary).Bind("order", PointerButton.Secondary));
        rig.Input.Layers.Add(new Panel(new RectF(0, 0, 50, 50)), InputLayerOrder.UI);
        rig.Update(Down(PointerButton.Secondary, 10, 10));
        Assert.IsFalse(rig.Map.Down("order"));
        rig.End().Update(Up(PointerButton.Secondary, 10, 10));
        Assert.IsFalse(rig.Map.Released("order"), "the release belongs to the layer that took the press");
        rig.End().Update(Down(PointerButton.Primary, 200, 100));
        Assert.IsTrue(rig.Map.Pressed("select"), "a later click outside the panel is the game's");
        rig.End().Update(Down(PointerButton.Secondary, 200, 100));
        Assert.IsTrue(rig.Map.Pressed("order") && rig.Map.Down("select"), "two buttons held at once, each its own press");
    }

    [TestMethod]
    public void An_action_pressed_by_a_mouse_button_is_consumed_like_any_other()
    {
        var rig = new Rig(new InputMap().Bind("select", PointerButton.Primary));
        rig.Update(Down());
        Assert.IsTrue(rig.Map.Consume("select"));
        Assert.IsFalse(rig.Map.Down("select") || rig.Map.Pressed("select"));
        rig.End().Update(Up());
        Assert.IsFalse(rig.Map.Released("select"));
    }

    [TestMethod]
    public void Each_player_reads_the_mouse_paired_with_them()
    {
        var rig = new Rig(new InputMap().Bind("select", PointerButton.Primary));
        rig.Input.Players[0].Pair(Mouse);
        var second = rig.Input.Players.Join(Mouse2);
        rig.Update(Down(device: Mouse2));
        Assert.IsTrue(second.Map.Pressed("select"));
        Assert.IsFalse(rig.Map.Down("select"), "player 1 does not hear player 2's mouse");
    }

    [TestMethod]
    public void A_finger_on_the_screen_presses_what_the_primary_button_is_bound_to_as_a_touch()
    {
        var rig = new Rig(new InputMap().Bind("select", PointerButton.Primary));
        rig.Update(InputEvent.PointerDown(DeviceId.TouchScreen, 7, PointerKind.Touch, new Vector2(30, 40)));
        Assert.IsTrue(rig.Map.Pressed("select"));
        Assert.AreEqual(InputDevice.Touch, rig.Map.LastDevice);
        Assert.AreEqual(PointerKind.Touch, rig.Map.Pointer.Kind);
    }

    [TestMethod]
    public void Listening_for_any_press_hears_a_mouse_button_binds_it_and_gameplay_never_sees_that_click()
    {
        var rig = new Rig(new InputMap().Bind("fire", Key.X).Bind("select", PointerButton.Secondary));
        InputEvent? heard = null;
        using (rig.Input.ListenForPress(e => heard = e, PressKinds.All))
        {
            rig.Update(Move(10, 10));
            Assert.IsNull(heard, "moving the mouse is not a press");
            rig.End().Update(Down(PointerButton.Secondary));
        }
        Assert.AreEqual(PointerButton.Secondary, heard?.PointerButton);
        Assert.AreEqual(InputControl.Of(PointerButton.Secondary), InputControl.FromPress(heard!.Value));
        Assert.IsFalse(rig.Map.Down("select"), "the click that chose the button does not also select");
        Assert.IsTrue(rig.Map.BindFrom("fire", heard.Value));
        CollectionAssert.AreEqual(new InputControl[] { Key.X, PointerButton.Secondary }, rig.Map.GetBindings("fire")!.Controls.ToArray());
        rig.End().Update(Up(PointerButton.Secondary));
        Assert.IsFalse(rig.Map.Released("fire") || rig.Map.Released("select"), "nor does its release");
        rig.End().Update(Down(PointerButton.Secondary));
        Assert.IsTrue(rig.Map.Pressed("fire"), "the next click is the new binding");
    }

    [TestMethod]
    public void Listening_for_any_press_hears_the_wheel_and_replacing_swaps_the_mouse_controls_only()
    {
        var rig = new Rig(new InputMap().Bind("next", Key.Tab, PointerButton.Forward, PadButton.RightShoulder));
        InputEvent? heard = null;
        using (rig.Input.ListenForPress(e => heard = e, PressKinds.All)) rig.Update(Wheel(-1));
        Assert.AreEqual(InputControl.WheelDown, InputControl.FromPress(heard!.Value));
        Assert.AreEqual(0f, rig.Map.Pointer.Wheel.Y, "the turn went to the listener");
        Assert.IsTrue(rig.Map.BindFrom("next", heard.Value, replace: true));
        CollectionAssert.AreEqual(new InputControl[] { Key.Tab, PadButton.RightShoulder, InputControl.WheelDown }, rig.Map.GetBindings("next")!.Controls.ToArray(),
            "the key and the gamepad button stay; the mouse button gave way to the wheel");
        Assert.IsFalse(rig.Map.BindFrom("next", InputEvent.PointerDown(DeviceId.TouchScreen, 1, PointerKind.Touch, default)), "a touch is not a control to bind");
        Assert.IsFalse(rig.Map.BindFrom("next", Move(1, 1)));
    }

    [TestMethod]
    public void Saved_bindings_keep_mouse_buttons_the_wheel_pointer_motion_and_both_sticks_in_the_second_format()
    {
        var map = new InputMap()
            .Bind("select", PointerButton.Primary, Key.Enter, PadButton.A)
            .Bind("zoom-in", InputControl.WheelUp, new KeyChord(Key.Equals, Key.LeftControl, Key.RightControl))
            .Bind("look-right", InputControl.PointerRight, InputControl.Of(PadStick.Right, InputMap.StickDirection.Right))
            .Bind("back", PointerButton.Back)
            .BindAxis("fire", PadAxis.RightTrigger)
            .Bind("left", [Key.A], [], InputMap.StickDirection.Left);
        var set = map.ExportBindings();
        Assert.AreEqual(2, set.Version);
        CollectionAssert.AreEqual(new[] { "Key:Enter", "Pad:A", "Mouse:Primary" }, set.Actions.Single(a => a.Action == "select").Controls);
        string json = set.ToJson();
        StringAssert.Contains(json, "\"version\": 2");
        StringAssert.Contains(json, "\"Wheel:Up\"");
        Assert.IsFalse(json.Contains("\"keys\""), "the first format's lists are not written");

        var loaded = new InputMap().Bind("select", Key.Q).Bind("zoom-in", Key.Q).Bind("look-right", Key.Q).Bind("back", Key.Q).Bind("fire", Key.Q).Bind("left", Key.Q);
        Assert.AreEqual(6, loaded.ImportBindings(InputBindingSet.FromJson(json)));
        foreach (var action in map.Actions)
            CollectionAssert.AreEqual(map.GetBindings(action)!.Controls.ToArray(), loaded.GetBindings(action)!.Controls.ToArray(), action);
        Assert.AreEqual(InputMap.StickDirection.Left, loaded.GetBindings("left")!.Stick);
        CollectionAssert.AreEqual(new[] { (PadAxis.RightTrigger, true) }, loaded.GetBindings("fire")!.Axes.ToArray());
    }

    [TestMethod]
    public void A_saved_control_this_version_does_not_know_is_skipped_and_the_others_load()
    {
        const string json = """
            { "version": 2, "actions": [ { "action": "select", "controls": [ "Mouse:Secondary", "Mouse:Sixth", "Pedal:Left", "Wheel:Up", "Key:7", "nonsense" ] } ] }
            """;
        var map = new InputMap().Bind("select", Key.Q);
        Assert.AreEqual(1, map.ImportBindings(InputBindingSet.FromJson(json)));
        CollectionAssert.AreEqual(new InputControl[] { PointerButton.Secondary, InputControl.WheelUp }, map.GetBindings("select")!.Controls.ToArray());
    }

    [TestMethod]
    public void A_control_is_the_same_as_text_and_back()
    {
        InputControl[] controls =
        [
            Key.Space, new KeyChord(Key.Enter, Key.LeftAlt, Key.RightAlt), PadButton.RightShoulder, PointerButton.Middle, InputControl.WheelLeft,
            InputControl.Of(PadStick.Right, InputMap.StickDirection.Up), InputControl.Of(PadAxis.LeftTrigger), InputControl.Of(PadAxis.RightX, positive: false),
            InputControl.PointerDown,
        ];
        string[] texts = ["Key:Space", "Chord:LeftAlt|RightAlt+Enter", "Pad:RightShoulder", "Mouse:Middle", "Wheel:Left", "Stick:Right.Up", "Axis:LeftTrigger+", "Axis:RightX-", "Motion:Down"];
        CollectionAssert.AreEqual(texts, controls.Select(c => c.ToString()).ToArray());
        foreach (var (control, text) in controls.Zip(texts))
        {
            Assert.IsTrue(InputControl.TryParse(text, out var parsed), text);
            Assert.AreEqual(control, parsed, text);
        }
        Assert.AreEqual(InputDevice.Mouse, InputControl.WheelUp.Device);
        Assert.AreEqual(InputDevice.Keyboard, InputControl.Of(Key.A).Device);
        Assert.AreEqual(InputDevice.Gamepad, InputControl.Of(PadAxis.LeftX).Device);
        Assert.AreEqual("RMB", PointerNames.Of(PointerButton.Secondary));
        Assert.AreEqual("Mouse 4", PointerNames.Of(PointerButton.Back));
        Assert.AreEqual("Wheel ↑", PointerNames.Of(InputControl.WheelUp));
        Assert.IsNull(PointerNames.Of(Key.A));
    }

    [TestMethod]
    public void One_control_is_unbound_from_an_action_and_the_actions_a_control_presses_are_found()
    {
        var map = new InputMap().Bind("select", PointerButton.Primary, Key.Enter).Bind("fire", PointerButton.Primary).Bind("jump", Key.Space);
        CollectionAssert.AreEquivalent(new[] { "select", "fire" }, map.ActionsBoundTo(PointerButton.Primary).ToArray());
        Assert.IsTrue(map.Unbind("fire", PointerButton.Primary));
        Assert.IsFalse(map.Unbind("fire", PointerButton.Primary), "it was already gone");
        CollectionAssert.AreEqual(new[] { "select" }, map.ActionsBoundTo(PointerButton.Primary).ToArray());
        CollectionAssert.AreEqual(new InputControl[] { Key.Enter, PointerButton.Primary }, map.GetBindings("select")!.Controls.ToArray());
        Assert.IsNotNull(map.GetBindings("fire"), "the action stays, unbound");
        map.Bind("select", Key.Enter);
        Assert.AreEqual(2, map.GetBindings("select")!.Controls.Count, "a control an action already has is not added twice");
    }

    [TestMethod]
    public void The_right_stick_has_a_radial_dead_zone_and_presses_the_directions_bound_to_it()
    {
        var map = new InputMap()
            .BindStick("aim-left", PadStick.Right, InputMap.StickDirection.Left).BindStick("aim-right", PadStick.Right, InputMap.StickDirection.Right)
            .BindStick("aim-up", PadStick.Right, InputMap.StickDirection.Up).BindStick("aim-down", PadStick.Right, InputMap.StickDirection.Down)
            .Bind("left", [], [], InputMap.StickDirection.Left);
        Assert.AreEqual(0.2f, map.RightStickDeadZone);
        var rig = new Rig(map);
        rig.Update(InputEvent.AxisMoved(Pad, PadAxis.RightX, 0.15f), InputEvent.AxisMoved(Pad, PadAxis.RightY, 0.1f));
        Assert.AreEqual(Vector2.Zero, rig.Map.Vector("aim-left", "aim-right", "aim-up", "aim-down"), "inside the dead zone the stick is centred");
        rig.End().Update(InputEvent.AxisMoved(Pad, PadAxis.RightX, 0.6f), InputEvent.AxisMoved(Pad, PadAxis.RightY, 0f));
        Assert.AreEqual(0.5f, rig.Map.Value("aim-right"), 1e-5f, "0.6 beyond a radial dead zone of 0.2");
        Assert.IsTrue(rig.Map.Pressed("aim-right") && !rig.Map.Down("aim-left"));
        Assert.IsFalse(rig.Map.Down("left"), "the left stick's actions do not follow the right stick");
        Assert.AreEqual(InputDevice.Gamepad, rig.Map.LastDevice);
        rig.End().Update(InputEvent.AxisMoved(Pad, PadAxis.RightX, 0f), InputEvent.AxisMoved(Pad, PadAxis.RightY, -1f));
        Assert.IsTrue(rig.Map.Pressed("aim-up") && rig.Map.Released("aim-right"));

        map.RightStickDeadZone = 0.5f;
        rig.End().Update(InputEvent.AxisMoved(Pad, PadAxis.RightY, -0.45f));
        Assert.AreEqual(0f, rig.Map.Value("aim-up"), "its dead zone is its own");
        var copy = map.CopyBindings();
        Assert.AreEqual(0.5f, copy.RightStickDeadZone);
        CollectionAssert.AreEqual(map.GetBindings("aim-up")!.Controls.ToArray(), copy.GetBindings("aim-up")!.Controls.ToArray());
    }

    [TestMethod]
    public void A_map_with_pointer_controls_adds_the_pointers_new_state_to_its_hash()
    {
        static ulong Hash(InputMap map, params InputEvent[] events)
        {
            var rig = new Rig(map);
            rig.Update(Move(10, 10)).End().Update(events);
            var hash = new StateHasher();
            rig.Map.HashState(hash);
            return hash.Value;
        }
        InputMap Bound() => new InputMap().Bind("look-right", InputControl.PointerRight);
        Assert.AreNotEqual(Hash(Bound(), InputEvent.PointerMove(Mouse, 0, PointerKind.Mouse, new Vector2(10, 10), delta: new Vector2(3, 0))),
            Hash(Bound(), InputEvent.PointerMove(Mouse, 0, PointerKind.Mouse, new Vector2(10, 10), delta: new Vector2(4, 0))), "motion the position does not show");
        Assert.AreNotEqual(Hash(new InputMap(), Down(PointerButton.Back)), Hash(new InputMap()), "a held back button is state even with nothing bound to it");
    }
}
