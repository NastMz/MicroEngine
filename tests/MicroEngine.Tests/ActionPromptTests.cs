#pragma warning disable CS0618 // DialogueBox, deprecated, is tested until it is removed
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
    public void ALongPageContinuesInsteadOfBeingCut()
    {
        var font = Font();
        var map = new InputMap().Bind("confirm", Key.Z);
        var box = new DialogueBox { Lines = 2 };
        string page = "one two three four five six seven eight nine ten eleven twelve thirteen fourteen fifteen";
        int total = font.Wrap(page, 100 - 16).Count;
        Assert.IsTrue(total > 2, "the page needs more than two lines at this width");
        box.Show("Elder", page);
        var parts = new List<string>();
        for (int i = 0; i < 20 && box.Active; i++)
        {
            Draw(box, font, 100);
            Assert.IsTrue(box.VisibleLines.Count <= 2);
            parts.Add(string.Join(" ", box.VisibleLines));
            Confirm(box, map);  // reveal the rest of the part
            Confirm(box, map);  // continue
        }
        Assert.IsFalse(box.Active, "the box closes after the last part");
        Assert.AreEqual((total + 1) / 2, parts.Count, "one confirm per two lines");
        Assert.AreEqual(page, string.Join(" ", parts), "every word shown exactly once: " + string.Join("|", parts));
    }

    [TestMethod]
    public void DialoguePagesShowEveryLineOnceAndResolvePrompts()
    {
        var rig = new Rig();
        var font = Font();
        var box = new DialogueBox { Lines = 1, Prompts = rig.Prompts };
        box.Show(null, "Press {attack} to swing it at bushes and pots");
        var seen = new List<string>();
        for (int i = 0; i < 20 && box.Active; i++)
        {
            Draw(box, font, 80);
            Assert.IsTrue(font.MeasureWidth(box.VisibleLines.Single()) <= 80 - 16, "no line is wider than the box");
            seen.Add(box.VisibleLines.Single());
            Confirm(box, rig.Map);
            Confirm(box, rig.Map);
        }
        Assert.IsFalse(box.Active);
        Assert.AreEqual("Press Z to swing it at bushes and pots", string.Join(" ", seen));
    }

    [TestMethod]
    public void ADeviceChangeMidPageUpdatesThePrompt()
    {
        var rig = new Rig();
        var font = Font();
        var box = new DialogueBox { Prompts = rig.Prompts };
        box.Show(null, "Press {attack} to swing.");
        Draw(box, font);
        Assert.AreEqual("Press Z to swing.", box.Text);
        rig.PressPad(PadButton.A);
        Draw(box, font);
        Assert.AreEqual("Press A to swing.", box.Text);
        Assert.AreEqual("Press A to swing.", string.Join(" ", box.VisibleLines));
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

    private static void Draw(DialogueBox box, BitmapFont font, int width = 300) => box.Draw(new HeadlessRenderer(320, 180), font, new RectF(0, 0, width, 40));

    private static void Confirm(DialogueBox box, InputMap map)
    {
        map.SetForced("confirm", true); map.Sample(new HeadlessInput()); box.Update(0, map); map.EndUpdate();
        map.SetForced("confirm", false); map.Sample(new HeadlessInput()); map.EndUpdate();
    }
}
