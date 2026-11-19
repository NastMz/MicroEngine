using System.Numerics;
using MicroEngine.Core;
using MicroEngine.Graphics;
using MicroEngine.Input;
using MicroEngine.UI;

namespace MicroEngine.Tests;

/// <summary>Action prompts follow the device, the bindings and the touch layout; dialogues page and resolve them.</summary>
[TestClass]
public sealed class ActionPromptTests
{
    private sealed class Rig
    {
        public HeadlessTouchScreen Screen { get; } = new(new Vector2(1280, 720));
        public HeadlessInput Devices { get; } = new();
        public InputMap Map { get; } = InputMap.CreateDefault().Bind("attack", [Key.Z, Key.Space], [PadButton.A]);
        public VirtualControls Controls { get; } = new(TouchLayout.Of(
            new TouchDPad { Offset = new(100, 100) },
            new TouchButton("attack", "attack", "SWORD") { Anchor = TouchAnchor.BottomRight, Offset = new(70, 80), Also = ["confirm"] }));
        public ActionPrompts Prompts { get; }

        public Rig()
        {
            Map.AddSource(Controls);
            Prompts = new ActionPrompts(Map, Controls);
            Step();
        }

        public void Step()
        {
            Controls.Update(Screen, 1 / 60f, Map.LastDevice);
            Map.Sample(Devices);
            Map.EndUpdate();
        }

        public void PressKey(Key key) { Devices.Set(key, true); Step(); Devices.Set(key, false); Step(); }
        public void PressPad(PadButton b) { Devices.PadConnected = true; Devices.Set(b, true); Step(); Devices.Set(b, false); Step(); }
        public void Tap(string id)
        {
            var (c, _) = Controls.Geometry(id);
            Screen.Down(1, c); Step(); Screen.Up(1, c); Step();
        }
    }

    [TestMethod]
    public void MouseControlsAreNamedWithTheKeyboardsAndTheRightStickWithTheGamepads()
    {
        var rig = new Rig();
        rig.Map.Bind("order", PointerButton.Secondary)
            .Bind("zoom", Key.Equals, InputControl.WheelUp)
            .Bind("look", InputControl.PointerRight, InputControl.Of(PadStick.Right, InputMap.StickDirection.Right))
            .Bind("back", PointerButton.Back);
        Assert.AreEqual("RMB", rig.Prompts.Text("order"));
        Assert.AreEqual(PromptKind.Pointer, rig.Prompts.Get("order").Kind);
        Assert.AreEqual(InputControl.Of(PointerButton.Secondary), rig.Prompts.Get("order").Pointer);
        Assert.AreEqual("= / Wheel ↑", rig.Prompts.TextAll("zoom"), "keys first, then the mouse");
        Assert.AreEqual("Mouse →", rig.Prompts.Text("look"));
        Assert.AreEqual("Mouse 4", rig.Prompts.Text("back"));
        Assert.AreEqual("Press RMB to order", rig.Prompts.Format("Press {order} to order"));
        Assert.AreEqual("R→", rig.Prompts.Text("look", InputDevice.Gamepad));
        Assert.AreEqual("RMB", rig.Prompts.Text("order", InputDevice.Gamepad), "a pad has no control for it: the desktop's is better than nothing");
        Assert.AreEqual("RMB", rig.Prompts.Text("order", InputDevice.Mouse), "asking for the mouse is asking for the desktop's prompts");

        int version = rig.Prompts.Version;
        rig.Devices.Pointer = new PointerState(true, new Vector2(10, 10), false, true, false);
        rig.Step();
        Assert.AreEqual(InputDevice.Mouse, rig.Map.LastDevice);
        Assert.AreEqual(InputDevice.Keyboard, rig.Prompts.Device, "the keyboard and the mouse are one device for prompts");
        Assert.AreEqual("Z", rig.Prompts.Text("attack"));
        Assert.AreEqual(version, rig.Prompts.Version, "moving from the keys to the mouse changes no prompt");
        rig.Devices.Pointer = default;
        rig.Step();
        rig.PressPad(PadButton.A);
        Assert.AreEqual(InputDevice.Gamepad, rig.Prompts.Device);
        rig.Prompts.Override = InputDevice.Mouse;
        Assert.AreEqual(InputDevice.Keyboard, rig.Prompts.Device);
    }

    [TestMethod]
    public void TheKeyboardIsTheDefaultOnADesktop()
    {
        var rig = new Rig();
        rig.Devices.PadConnected = true;
        rig.Step();
        Assert.AreEqual(InputDevice.Keyboard, rig.Prompts.Device, "a pad left plugged in takes over only when pressed");
        Assert.AreEqual("Press Z to swing", rig.Prompts.Format("Press {attack} to swing"));
    }

    [TestMethod]
    public void PromptsFollowTheDeviceThePlayerLastUsed()
    {
        var rig = new Rig();
        rig.PressPad(PadButton.A);
        Assert.AreEqual(InputDevice.Gamepad, rig.Prompts.Device);
        Assert.AreEqual("Press A to swing", rig.Prompts.Format("Press {attack} to swing"));
        rig.Tap("attack");
        Assert.AreEqual(InputDevice.Touch, rig.Prompts.Device);
        Assert.AreEqual("Press SWORD to swing", rig.Prompts.Format("Press {attack} to swing"));
        rig.PressKey(Key.Space);
        Assert.AreEqual("Press Z to swing", rig.Prompts.Format("Press {attack} to swing"));
    }

    [TestMethod]
    public void VersionChangesWhenTheDeviceChanges()
    {
        var rig = new Rig();
        int v = rig.Prompts.Version;
        Assert.AreEqual(v, rig.Prompts.Version, "stable while nothing changes");
        rig.PressPad(PadButton.A);
        Assert.AreNotEqual(v, rig.Prompts.Version);
    }

    [TestMethod]
    public void ATouchFirstScreenStartsWithTouchPrompts()
    {
        var rig = new Rig();
        rig.Screen.TouchFirst = true;
        rig.Step();
        Assert.AreEqual(InputDevice.Touch, rig.Prompts.Device);
        Assert.AreEqual("SWORD", rig.Prompts.Text("confirm"), "a button that also presses the action names it");
        Assert.AreEqual("←", rig.Prompts.Text("left"));
        Assert.AreEqual("D-pad", rig.Prompts.Directions());
    }

    [TestMethod]
    public void RemappingChangesThePrompt()
    {
        var rig = new Rig();
        rig.Map.Unbind("attack").Bind("attack", [Key.L], [PadButton.X]);
        Assert.AreEqual("L", rig.Prompts.Text("attack"));
        Assert.AreEqual("X", rig.Prompts.Text("attack", InputDevice.Gamepad));
    }

    [TestMethod]
    public void GamepadFamiliesNameTheirButtons()
    {
        var rig = new Rig();
        rig.Devices.PadStyle = GamepadStyle.PlayStation;
        rig.PressPad(PadButton.A);
        Assert.AreEqual("×", rig.Prompts.Text("attack"));
        Assert.AreEqual("○", rig.Prompts.Text("cancel"));
        rig.Devices.PadStyle = GamepadStyle.Nintendo;
        rig.Step();
        Assert.AreEqual("B", rig.Prompts.Text("attack"), "Nintendo pads print B where Xbox pads print A");
        Assert.AreEqual(GamepadStyle.Xbox, ActionPrompts.StyleFromName("Xbox Wireless Controller"));
        Assert.AreEqual(GamepadStyle.PlayStation, ActionPrompts.StyleFromName("054c-0ce6-Wireless Controller"));
    }

    [TestMethod]
    public void HelpScreensListEveryBinding()
    {
        var rig = new Rig();
        Assert.AreEqual("Z / Space", rig.Prompts.TextAll("attack"));
        Assert.AreEqual("↑←↓→", rig.Prompts.Directions());
        Assert.AreEqual("↑←↓→ / WASD", rig.Prompts.Directions(separator: " / "), "help screens list every key set");
        Assert.AreEqual("D-pad", rig.Prompts.Directions(device: InputDevice.Gamepad));
        Assert.AreEqual("Press ↑←↓→", rig.Prompts.Format("Press {move}"));
    }

    [TestMethod]
    public void LabelsUnknownNamesAndBracesAreKept()
    {
        var rig = new Rig();
        rig.Prompts.SetLabel("attack", InputDevice.Touch, "the sword button");
        Assert.AreEqual("the sword button", rig.Prompts.Text("attack", InputDevice.Touch));
        Assert.AreEqual("Z", rig.Prompts.Text("attack", InputDevice.Keyboard));
        Assert.AreEqual("{nothing} and {attack}", rig.Prompts.Format("{nothing} and {{attack}}"));
        Assert.AreEqual("[Z]", rig.Prompts.Format("{attack}", p => $"[{p.Text}]"));
        rig.Prompts.Override = InputDevice.Gamepad;
        Assert.AreEqual("A", rig.Prompts.Text("attack"));
    }

    [TestMethod]
    public void ScriptsChooseTheDevicePromptsAreShownFor()
    {
        var rig = new Rig();
        var script = InputScript.Parse("10 prompts gamepad playstation\n20 prompts touch\n30 prompts auto");
        script.ApplyPrompts(10, rig.Prompts);
        Assert.AreEqual("×", rig.Prompts.Text("attack"));
        script.ApplyPrompts(20, rig.Prompts);
        Assert.AreEqual("SWORD", rig.Prompts.Text("attack"));
        script.ApplyPrompts(30, rig.Prompts);
        Assert.AreEqual("Z", rig.Prompts.Text("attack"));
        Assert.ThrowsExactly<FormatException>(() => InputScript.Parse("10 prompts joystick"));
    }

    [TestMethod]
    public void AnActionNoControlPressesFallsBackSensibly()
    {
        var rig = new Rig();
        rig.Map.Bind("map", Key.M);
        Assert.AreEqual("M", rig.Prompts.Text("map", InputDevice.Gamepad), "a desktop pad player still has the keyboard");
        Assert.AreEqual("MAP", rig.Prompts.Text("map", InputDevice.Touch), "a touch player has no keys");
    }

    [TestMethod]
    public void PromptGlyphsMeasureAndDrawOneLine()
    {
        var rig = new Rig();
        var font = BitmapFont.CreateDefault(null);
        int plain = font.MeasureWidth("Press Z");
        int glyphs = PromptText.Measure(font, rig.Prompts, "Press {attack}");
        Assert.IsTrue(glyphs > plain, "a key cap is wider than its letter");
        rig.PressPad(PadButton.A);
        Assert.AreEqual(font.MeasureWidth("Press ") + font.Spacing + 9 + 2 - font.Spacing, PromptText.Measure(font, rig.Prompts, "Press {attack}"));
    }

    [TestMethod]
    public void AWordWiderThanTheLineIsBroken()
    {
        var font = Font();
        var lines = font.Wrap("A Supercalifragilisticexpialidocious name", 40);
        Assert.IsTrue(lines.All(l => font.MeasureWidth(l) <= 40), string.Join("|", lines));
        Assert.AreEqual("ASupercalifragilisticexpialidociousname", string.Concat(lines).Replace(" ", ""), "no character lost");
        Assert.AreEqual("short words stay whole", string.Join(" ", font.Wrap("short words stay whole", 60)), "words that fit are never cut");
    }

    private static BitmapFont Font() => BitmapFont.CreateDefault(new HeadlessGraphicsDevice());
}
