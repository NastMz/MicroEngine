using System.Numerics;
using MicroEngine.Input;
using MicroEngine.Kernel;

namespace MicroEngine.Tests.Input;

/// <summary>
/// What games written before pointer controls existed rely on: saved bindings in the first format still load, the
/// per-update state hash of a map that binds no pointer control does not change, and listening for a press hears keys
/// and gamepad buttons only unless asked for more.
/// </summary>
[TestClass]
public sealed class BindingCompatibilityTests
{
    private static readonly DeviceId Keyboard = new(1), Pad = new(2), Mouse = new(4);

    /// <summary>A file written by <c>SaveBindings</c> of the first format, kept as text.</summary>
    private const string Version1 = """
        {
          "version": 1,
          "actions": [
            {
              "action": "confirm",
              "keys": [ "Q", "Keypad5" ],
              "buttons": [ "RightPaddle1" ],
              "stick": "None",
              "axes": [ "RightTrigger+" ],
              "chords": [ "LeftControl|RightControl+S" ]
            },
            {
              "action": "left",
              "keys": [ "H" ],
              "buttons": [ "DPadLeft" ],
              "stick": "Left",
              "axes": [ "RightX-" ],
              "chords": []
            }
          ]
        }
        """;

    [TestMethod]
    public void A_saved_file_of_the_first_format_loads_every_key_button_stick_axis_and_chord()
    {
        var map = InputMap.CreateDefault();
        Assert.AreEqual(2, map.ImportBindings(InputBindingSet.FromJson(Version1)));

        var confirm = map.GetBindings("confirm")!;
        CollectionAssert.AreEqual(new[] { Key.Q, Key.Keypad5 }, confirm.Keys.ToArray());
        CollectionAssert.AreEqual(new[] { PadButton.RightPaddle1 }, confirm.Buttons.ToArray());
        Assert.AreEqual(InputMap.StickDirection.None, confirm.Stick);
        CollectionAssert.AreEqual(new[] { (PadAxis.RightTrigger, true) }, confirm.Axes.ToArray());
        var chord = confirm.Chords.Single();
        Assert.AreEqual(Key.S, chord.Key);
        CollectionAssert.AreEqual(new[] { Key.LeftControl, Key.RightControl }, chord.Modifiers.ToArray());

        var left = map.GetBindings("left")!;
        CollectionAssert.AreEqual(new[] { Key.H }, left.Keys.ToArray());
        CollectionAssert.AreEqual(new[] { PadButton.DPadLeft }, left.Buttons.ToArray());
        Assert.AreEqual(InputMap.StickDirection.Left, left.Stick);
        CollectionAssert.AreEqual(new[] { (PadAxis.RightX, false) }, left.Axes.ToArray());
        CollectionAssert.AreEqual(InputMap.CreateDefault().GetBindings("right")!.Keys.ToArray(), map.GetBindings("right")!.Keys.ToArray(), "actions the file does not mention keep theirs");
    }

    [TestMethod]
    public void The_state_hash_of_a_map_that_binds_no_pointer_control_is_what_it_always_was()
    {
        // The hashes below were recorded before pointer controls existed, from this exact sequence: keys, a gamepad with
        // its stick, the mouse moving and clicking, and the wheel.
        var input = new InputSystem(InputMap.CreateDefault().BindAxis("action", PadAxis.RightTrigger));
        var map = input.Players[0].Map;
        var hashes = new List<ulong>();
        void Step(params InputEvent[] events)
        {
            foreach (var e in events) input.Send(e);
            input.BeginFrame();
            input.Sample();
            var hash = new StateHasher();
            map.HashState(hash);
            input.HashState(hash);
            hashes.Add(hash.Value);
            input.EndUpdate();
        }

        Step();
        Step(InputEvent.KeyDown(Keyboard, Key.Space), InputEvent.KeyDown(Keyboard, Key.Left));
        Step(InputEvent.KeyUp(Keyboard, Key.Space));
        Step(InputEvent.Added(new InputDeviceInfo(Pad, DeviceKind.Gamepad, "pad") { Style = GamepadStyle.PlayStation }),
            InputEvent.AxisMoved(Pad, PadAxis.LeftX, 0.9f), InputEvent.AxisMoved(Pad, PadAxis.RightTrigger, 0.7f), InputEvent.ButtonDown(Pad, PadButton.A));
        Step(InputEvent.PointerMove(Mouse, 0, PointerKind.Mouse, new Vector2(40, 30)));
        Step(InputEvent.PointerDown(Mouse, 0, PointerKind.Mouse, new Vector2(42, 31)), InputEvent.PointerDown(Mouse, 0, PointerKind.Mouse, new Vector2(42, 31), PointerButton.Secondary));
        Step(InputEvent.PointerUp(Mouse, 0, PointerKind.Mouse, new Vector2(50, 31)), InputEvent.WheelMoved(Mouse, new Vector2(0, 1), new Vector2(50, 31)));
        Step(InputEvent.PointerUp(Mouse, 0, PointerKind.Mouse, new Vector2(50, 31), PointerButton.Secondary), InputEvent.KeyUp(Keyboard, Key.Left));
        Step();

        CollectionAssert.AreEqual(Recorded, hashes, string.Join(", ", hashes.Select(h => $"0x{h:X16}UL")));
    }

    private static readonly ulong[] Recorded =
    [
        0x5CF0F8858D06D600UL, 0xAA684E8B4FBD9AF3UL, 0x0D4EF45DEE1DE542UL, 0x6041E9F5069BD61CUL, 0xE7D5EF0D6E4F331CUL,
        0x2D36E6189F7E08C9UL, 0x260B040AE953F3A0UL, 0x6243350078D34761UL, 0x05F4B6AAD521CF89UL,
    ];

    [TestMethod]
    public void Listening_for_a_press_without_saying_more_hears_keys_and_gamepad_buttons_and_leaves_the_mouse_to_the_game()
    {
        var input = new InputSystem(InputMap.CreateDefault());
        InputEvent? heard = null;
        using var listening = input.ListenForPress(e => heard = e);
        input.Send(InputEvent.PointerDown(Mouse, 0, PointerKind.Mouse, new Vector2(10, 10)));
        input.Send(InputEvent.WheelMoved(Mouse, new Vector2(0, 1), new Vector2(10, 10)));
        input.BeginFrame();
        input.Sample();
        Assert.IsNull(heard, "a click on the rebinding screen's own buttons is not a binding");
        Assert.IsTrue(input.Players[0].Map.Pointer.Primary, "the click reached the game");
        input.EndUpdate();
        input.Send(InputEvent.ButtonDown(Pad, PadButton.Y));
        input.BeginFrame();
        Assert.AreEqual(PadButton.Y, heard?.Button);
    }

    [TestMethod]
    public void An_axis_bound_on_the_right_stick_keeps_its_own_dead_zone_per_axis()
    {
        var input = new InputSystem(new InputMap().BindAxis("aim-right", PadAxis.RightX));
        var map = input.Players[0].Map;
        input.Send(InputEvent.AxisMoved(Pad, PadAxis.RightX, 0.55f));
        input.Send(InputEvent.AxisMoved(Pad, PadAxis.RightY, 0.9f));
        input.BeginFrame();
        input.Sample();
        Assert.AreEqual(0.5f, map.Value("aim-right"), 1e-5f, "0.55 beyond the axis dead zone of 0.1, whatever the other axis does");
        Assert.IsTrue(map.Down("aim-right"));
    }
}
