using System.Numerics;
using MicroEngine.Core;
using MicroEngine.Input;

namespace MicroEngine.Tests;

/// <summary>Analog values, dead zones, stick sectors, remapping, action sources and device tracking of <see cref="InputMap"/>.</summary>
[TestClass]
public sealed class AnalogInputTests
{
    [TestMethod]
    public void KeysAreDigitalAndDiagonalsAreNormalised()
    {
        var map = InputMap.CreateDefault();
        var keys = new HeadlessInput();
        keys.Set(Key.Right, true);
        keys.Set(Key.Down, true);
        map.Sample(keys);
        Assert.AreEqual(1, map.Value("right"));
        var v = map.Vector("left", "right", "up", "down");
        Assert.AreEqual(1, v.Length(), 1e-4f);
        Assert.AreEqual(v.X, v.Y, 1e-4f);
        Assert.AreEqual(1, map.Axis("left", "right"), "the digital axis is unchanged");
    }

    [TestMethod]
    public void TheStickHasARadialDeadZoneAndAnalogValues()
    {
        var map = InputMap.CreateDefault();
        var pad = new HeadlessInput { PadConnected = true };
        pad.SetLeftStick(new Vector2(0.15f, 0.1f));
        map.Sample(pad);
        Assert.AreEqual(Vector2.Zero, map.Vector("left", "right", "up", "down"), "resting drift is ignored");
        Assert.IsFalse(map.Down("right"));
        pad.SetLeftStick(new Vector2(0.4f, 0));
        map.Sample(pad);
        Assert.AreEqual(0.25f, map.Value("right"), 0.01f, "rescaled beyond the dead zone");
        Assert.IsFalse(map.Down("right"), "a light push walks without pressing the digital action");
        pad.SetLeftStick(new Vector2(0.9f, 0));
        map.Sample(pad);
        Assert.IsTrue(map.Down("right"));
        Assert.AreEqual(InputDevice.Gamepad, map.LastDevice);
        Assert.IsTrue(map.PadConnected);
    }

    [TestMethod]
    public void StickDiagonalsUseSectorsAndFourWayKeepsTheDominantAxis()
    {
        var map = InputMap.CreateDefault();
        var pad = new HeadlessInput();
        pad.SetLeftStick(Vector2.Normalize(new Vector2(1, -1)));
        map.Sample(pad);
        Assert.IsTrue(map.Down("right") && map.Down("up"), "45° presses both");
        pad.SetLeftStick(Vector2.Normalize(new Vector2(1, -0.3f)));
        map.EndUpdate();
        map.Sample(pad);
        Assert.IsTrue(map.Down("right") && !map.Down("up"), "17° is still straight right");

        var grid = InputMap.CreateDefault();
        grid.StickMode = StickDirections.Four;
        pad.SetLeftStick(Vector2.Normalize(new Vector2(0.8f, -0.7f)));
        grid.Sample(pad);
        Assert.IsTrue(grid.Down("right") && !grid.Down("up"), "four-way: one direction at a time");
    }

    [TestMethod]
    public void TriggersAndTheRightStickCanBeBound()
    {
        var map = new InputMap().BindAxis("fire", PadAxis.RightTrigger).BindAxis("aim-left", PadAxis.RightX, positive: false);
        var pad = new HeadlessInput();
        pad.Set(PadAxis.RightTrigger, 0.3f);
        pad.Set(PadAxis.RightX, -0.8f);
        map.Sample(pad);
        Assert.IsFalse(map.Down("fire"));
        Assert.AreEqual(0.22f, map.Value("fire"), 0.01f);
        Assert.IsTrue(map.Down("aim-left"));
        pad.Set(PadAxis.RightTrigger, 1);
        map.Sample(pad);
        Assert.IsTrue(map.Pressed("fire"));
    }

    [TestMethod]
    public void ActionsCanBeRebound()
    {
        var map = InputMap.CreateDefault();
        var keys = new HeadlessInput();
        map.Unbind("confirm").Bind("confirm", Key.E);
        CollectionAssert.AreEqual(new[] { Key.E }, map.GetBindings("confirm")!.Keys.ToArray());
        keys.Set(Key.Z, true);
        map.Sample(keys);
        Assert.IsFalse(map.Down("confirm"), "the old key no longer confirms");
        keys.Set(Key.E, true);
        map.Sample(keys);
        Assert.IsTrue(map.Down("confirm"));
        Assert.IsNull(map.GetBindings("nothing"));
    }

    private sealed class Switch : IActionSource
    {
        public bool On;
        public bool IsDown(string action) => On && action == "confirm";
        public InputDevice Device => InputDevice.Other;
    }

    [TestMethod]
    public void CustomActionSourcesPressActionsLikeDevices()
    {
        var map = InputMap.CreateDefault();
        var accessibility = new Switch();
        map.AddSource(accessibility);
        var keys = new HeadlessInput();
        map.Sample(keys);
        Assert.IsFalse(map.Down("confirm"));
        accessibility.On = true;
        map.Sample(keys);
        Assert.IsTrue(map.Pressed("confirm"));
        Assert.AreEqual(InputDevice.Other, map.LastDevice);
        Assert.IsTrue(map.RemoveSource(accessibility));
        map.EndUpdate();
        map.Sample(keys);
        Assert.IsTrue(map.Released("confirm"));
    }

    [TestMethod]
    public void LastDeviceFollowsTheMostRecentPress()
    {
        var map = InputMap.CreateDefault();
        var devices = new HeadlessInput();
        devices.Set(Key.Z, true);
        map.Sample(devices);
        Assert.AreEqual(InputDevice.Keyboard, map.LastDevice);
        devices.Set(Key.Z, false);
        devices.Set(PadButton.Start, true);
        map.Sample(devices);
        Assert.AreEqual(InputDevice.Gamepad, map.LastDevice);
    }

    [TestMethod]
    public void AGamepadUnpluggedMidPressReleasesItsActions()
    {
        var map = InputMap.CreateDefault();
        var pad = new HeadlessInput { PadConnected = true };
        pad.Set(PadButton.A, true);
        map.Sample(pad);
        Assert.IsTrue(map.Down("confirm"));
        // A disconnected pad reports nothing, as SDL's and raylib's sources do.
        pad.Set(PadButton.A, false);
        pad.PadConnected = false;
        map.EndUpdate();
        map.Sample(pad);
        Assert.IsTrue(map.Released("confirm"));
        Assert.IsFalse(map.PadConnected);
    }

    [TestMethod]
    public void ComposedInputMergesAxesAndThePointer()
    {
        var a = new HeadlessInput();
        var b = new HeadlessInput { PadConnected = true, Pointer = new PointerState(true, new Vector2(10, 20), true, false, false) };
        a.Set(PadAxis.RightTrigger, 0.2f);
        b.Set(PadAxis.RightTrigger, 0.9f);
        IInputSource both = new CompositeInput([a, b]);
        Assert.AreEqual(0.9f, both.GetPadAxis(PadAxis.RightTrigger));
        Assert.IsTrue(both.PadConnected);
        Assert.AreEqual(new Vector2(10, 20), both.Pointer.Position);
    }
}
