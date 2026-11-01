using System.Numerics;
using MicroEngine.Core;
using MicroEngine.Graphics;
using MicroEngine.Input;
using MicroEngine.Scenes;

namespace MicroEngine.Tests;

/// <summary>The on-screen controls through their public contract: a touch screen in, actions out.</summary>
[TestClass]
public sealed class TouchControlsTests
{
    private static readonly TouchLayout Layout = TouchLayout.Of(
        new TouchDPad { Offset = new(100, 100) },
        new TouchButton("jump", "jump", "A") { Anchor = TouchAnchor.BottomRight, Offset = new(70, 80), Radius = 36 },
        new TouchButton("fire", "fire", "B") { Anchor = TouchAnchor.BottomRight, Offset = new(150, 60), Radius = 30 });

    private sealed class Rig
    {
        public HeadlessTouchScreen Screen { get; } = new(new Vector2(1280, 720)) { TouchFirst = true };
        public VirtualControls Controls { get; }
        public InputMap Map { get; } = InputMap.CreateDefault().Bind("jump", Key.Space).Bind("fire", Key.X);
        public HeadlessInput Devices { get; } = new();

        public Rig(TouchLayout? layout = null)
        {
            Controls = new VirtualControls(layout ?? Layout);
            Map.AddSource(Controls);
            Frame();
        }

        /// <summary>One frame with one step, as the loop runs it.</summary>
        public void Frame()
        {
            Controls.Update(Screen, 1 / 60f, Map.LastDevice);
            Map.Sample(Devices);
        }

        public void Step()
        {
            Map.EndUpdate();
            Frame();
        }

        public Vector2 At(string id, float dx = 0, float dy = 0)
        {
            var (c, r) = Controls.Geometry(id);
            return c + new Vector2(dx, dy) * r;
        }
    }

    [TestMethod]
    public void AButtonIsPressedHeldAndReleasedLikeAKey()
    {
        var rig = new Rig();
        rig.Screen.Down(1, rig.At("jump"));
        rig.Step();
        Assert.IsTrue(rig.Map.Pressed("jump"));
        rig.Step();
        Assert.IsTrue(rig.Map.Down("jump") && !rig.Map.Pressed("jump"), "held");
        rig.Screen.Up(1);
        rig.Step();
        Assert.IsTrue(rig.Map.Released("jump"));
        Assert.AreEqual(InputDevice.Touch, rig.Map.LastDevice);
    }

    [TestMethod]
    public void ATapShorterThanAFrameIsPressedOnce()
    {
        var rig = new Rig();
        rig.Screen.Down(1, rig.At("jump"));
        rig.Screen.Up(1);
        rig.Step();
        Assert.IsTrue(rig.Map.Pressed("jump"), "down and up between two frames still reaches a step");
        rig.Step();
        Assert.IsFalse(rig.Map.Down("jump"), "read once, then gone");
    }

    [TestMethod]
    public void RapidTapsAreEachSeen()
    {
        var rig = new Rig();
        int presses = 0;
        for (int i = 0; i < 6; i++)
        {
            rig.Screen.Down(i, rig.At("jump"));
            rig.Step();
            if (rig.Map.Pressed("jump")) presses++;
            rig.Screen.Up(i);
            rig.Step();
        }
        Assert.AreEqual(6, presses);
    }

    [TestMethod]
    public void TwoThumbsMoveAndJumpAtOnce()
    {
        var rig = new Rig();
        rig.Screen.Down(1, rig.At("dpad", 0.9f, 0));
        rig.Screen.Down(2, rig.At("jump"));
        rig.Step();
        Assert.IsTrue(rig.Map.Down("right") && rig.Map.Pressed("jump"));
        rig.Screen.Up(2);
        rig.Step();
        Assert.IsTrue(rig.Map.Down("right"), "lifting the jump thumb keeps the direction held");
        Assert.IsFalse(rig.Map.Down("jump"));
        rig.Screen.Down(3, rig.At("fire"));
        rig.Step();
        Assert.IsTrue(rig.Map.Down("right") && rig.Map.Down("fire"));
    }

    [TestMethod]
    public void TheDPadGivesEightOrFourDirections()
    {
        var rig = new Rig();
        rig.Screen.Down(1, rig.At("dpad", 0.7f, -0.7f));
        rig.Step();
        Assert.IsTrue(rig.Map.Down("right") && rig.Map.Down("up"), "a diagonal presses two directions");
        rig.Screen.Move(1, rig.At("dpad", 0.1f, 0.05f));
        rig.Step();
        Assert.IsFalse(rig.Map.Down("right") || rig.Map.Down("up"), "the centre is a dead zone");

        var four = new Rig(TouchLayout.Of(new TouchDPad { Offset = new(100, 100), Diagonals = false }));
        four.Screen.Down(1, four.At("dpad", 0.8f, -0.6f));
        four.Step();
        Assert.IsTrue(four.Map.Down("right") && !four.Map.Down("up"), "four-way: only the dominant direction");
        Assert.AreEqual(1, four.Map.Value("right"), "a d-pad is digital");
    }

    [TestMethod]
    public void AThumbThatSlidesOffTheDPadKeepsItsDirection()
    {
        var rig = new Rig();
        rig.Screen.Down(1, rig.At("dpad", 0.8f, 0));
        rig.Step();
        rig.Screen.Move(1, rig.At("dpad", 2.5f, 0.3f));
        rig.Step();
        Assert.IsTrue(rig.Map.Down("right"), "the d-pad owns its finger until it lifts");
        rig.Screen.Move(1, rig.At("jump"));
        rig.Step();
        Assert.IsFalse(rig.Map.Down("jump"), "a d-pad finger never presses a button it crosses");
    }

    [TestMethod]
    public void AButtonToleratesSmallSlidesAndReleasesBeyondThem()
    {
        var rig = new Rig();
        rig.Screen.Down(1, rig.At("jump"));
        rig.Step();
        rig.Screen.Move(1, rig.At("jump", 1.5f, 0));
        rig.Step();
        Assert.IsTrue(rig.Map.Down("jump"), "a small slide off the drawn button keeps it held");
        rig.Screen.Move(1, rig.At("jump", 4, -3));
        rig.Step();
        Assert.IsFalse(rig.Map.Down("jump"), "far away it releases (no stuck button)");
        rig.Screen.Move(1, rig.At("jump"));
        rig.Step();
        Assert.IsTrue(rig.Map.Pressed("jump"), "coming back presses it again");
        rig.Screen.Up(1);
        rig.Step();
        Assert.IsFalse(rig.Map.Down("jump"));
        Assert.AreEqual(0, rig.Controls.FingerCount);
    }

    [TestMethod]
    public void AFingerNeverPressesAButtonItDidNotStartOnUnlessSlidingIsOn()
    {
        var rig = new Rig();
        rig.Screen.Down(1, rig.At("jump"));
        rig.Step();
        rig.Screen.Move(1, rig.At("fire"));
        rig.Step();
        Assert.IsFalse(rig.Map.Down("fire"), "no ghost press of a neighbour");

        var sliding = new Rig(Layout with { SlideBetweenButtons = true });
        sliding.Screen.Down(1, sliding.At("jump"));
        sliding.Step();
        sliding.Screen.Move(1, sliding.At("fire"));
        sliding.Step();
        Assert.IsTrue(sliding.Map.Pressed("fire") && !sliding.Map.Down("jump"), "with sliding on, the finger rolls onto the neighbour");
    }

    [TestMethod]
    public void TouchesOutsideEveryControlDoNothing()
    {
        var rig = new Rig();
        rig.Screen.Down(1, new Vector2(640, 200));
        rig.Step();
        Assert.IsFalse(rig.Map.Actions.Any(rig.Map.Down));
        rig.Screen.Move(1, rig.At("jump"));
        rig.Step();
        Assert.IsFalse(rig.Map.Down("jump"), "a finger that started on nothing stays inert");
    }

    [TestMethod]
    public void TheHitAreaIsLargerThanTheDrawnButtonAndOverlapsGoToTheNearest()
    {
        var rig = new Rig();
        var (center, radius) = rig.Controls.Geometry("jump");
        rig.Screen.Down(1, center + new Vector2(0, radius * 1.2f));
        rig.Step();
        Assert.IsTrue(rig.Map.Down("jump"), "just outside the drawing still counts");
        rig.Screen.Up(1);
        rig.Step();
        var (fire, _) = rig.Controls.Geometry("fire");
        var between = center + (fire - center) * 0.6f;
        rig.Screen.Down(2, between);
        rig.Step();
        Assert.IsTrue(rig.Map.Down("fire") ^ rig.Map.Down("jump"), "an ambiguous touch presses exactly one button");
    }

    [TestMethod]
    public void TheJoystickIsAnalogWithADeadZoneAndDiagonals()
    {
        var layout = TouchLayout.Of(new TouchStick { Placement = StickPlacement.Fixed, Offset = new(100, 100), DeadZone = 0.2f });
        var rig = new Rig(layout);
        rig.Screen.Down(1, rig.At("stick", 0.1f, 0));
        rig.Step();
        Assert.AreEqual(Vector2.Zero, rig.Map.Vector("left", "right", "up", "down"), "inside the dead zone");
        rig.Screen.Move(1, rig.At("stick", 0.6f, 0));
        rig.Step();
        var half = rig.Map.Vector("left", "right", "up", "down");
        Assert.AreEqual(0.5f, half.X, 0.02f, "rescaled beyond the dead zone");
        Assert.IsTrue(rig.Map.Down("right"));
        rig.Screen.Move(1, rig.At("stick", 3, 3));
        rig.Step();
        var full = rig.Map.Vector("left", "right", "up", "down");
        Assert.AreEqual(1, full.Length(), 0.01f, "clamped to full deflection");
        Assert.AreEqual(full.X, full.Y, 0.01f, "a true diagonal");
        Assert.IsTrue(rig.Map.Down("right") && rig.Map.Down("down"));
        Assert.AreEqual(0.707f, rig.Map.Value("right"), 0.02f);
    }

    [TestMethod]
    public void AFloatingJoystickStartsUnderTheThumbAndFollowsIt()
    {
        var layout = TouchLayout.Of(new TouchStick { Offset = new(100, 100) });
        var rig = new Rig(layout);
        var start = new Vector2(200, 600);
        rig.Screen.Down(1, start);
        rig.Step();
        Assert.AreEqual(Vector2.Zero, rig.Controls.ValueOf("stick"), "landing on the zone does not move");
        var (_, r) = rig.Controls.Geometry("stick");
        rig.Screen.Move(1, start + new Vector2(r * 3, 0));
        rig.Step();
        Assert.IsTrue(rig.Map.Down("right"));
        rig.Screen.Move(1, start + new Vector2(r * 2.6f, 0));
        rig.Step();
        Assert.IsTrue(rig.Map.Down("right") && !rig.Map.Down("left"), "the base followed the thumb, so easing back a little is not a reversal");
        rig.Screen.Down(2, new Vector2(1100, 100));
        rig.Step();
        Assert.IsTrue(rig.Map.Down("right"), "a touch outside the zone does not take the stick");
    }

    [TestMethod]
    public void ToggleButtonsSwitchOnEachTap()
    {
        var rig = new Rig(TouchLayout.Of(new TouchButton("lock", "lock", "L") { Mode = TouchButtonMode.Toggle, Offset = new(80, 80) }));
        rig.Screen.Down(1, rig.At("lock"));
        rig.Screen.Up(1);
        rig.Step();
        rig.Step();
        Assert.IsTrue(rig.Map.Down("lock"), "on after one tap, an action no key binds");
        rig.Screen.Down(2, rig.At("lock"));
        rig.Screen.Up(2);
        rig.Step();
        rig.Step();
        Assert.IsFalse(rig.Map.Down("lock"));
    }

    [TestMethod]
    public void CancelledFingersReleaseTheirControls()
    {
        var rig = new Rig();
        rig.Screen.Down(1, rig.At("dpad", 1, 0));
        rig.Screen.Down(2, rig.At("jump"));
        rig.Step();
        rig.Screen.Cancel(1);
        rig.Screen.Cancel(2);
        rig.Step();
        Assert.IsFalse(rig.Map.Down("right") || rig.Map.Down("jump"));
    }

    [TestMethod]
    public void RotatingTheScreenLaysTheControlsOutAgainAndReleasesHeldFingers()
    {
        var rig = new Rig();
        rig.Screen.Down(1, rig.At("jump"));
        rig.Step();
        var landscape = rig.Controls.Geometry("jump").Center;
        rig.Screen.Size = new Vector2(720, 1280);
        rig.Step();
        Assert.IsFalse(rig.Map.Down("jump"), "a finger held across a rotation is released");
        var portrait = rig.Controls.Geometry("jump").Center;
        Assert.AreNotEqual(landscape, portrait);
        Assert.IsTrue(portrait.X < 720 && portrait.Y > 640, "still bottom right on the portrait screen");
        rig.Screen.Up(1);
        rig.Screen.Down(2, portrait);
        rig.Step();
        Assert.IsTrue(rig.Map.Pressed("jump"));
    }

    [TestMethod]
    public void ControlsStayInsideTheSafeArea()
    {
        var rig = new Rig();
        rig.Screen.SafeArea = new RectF(90, 0, 1280 - 180, 690); // notches on both sides, a home bar below
        rig.Step();
        foreach (var id in new[] { "dpad", "jump", "fire" })
        {
            var (c, r) = rig.Controls.Geometry(id);
            Assert.IsTrue(c.X - r >= 90 && c.X + r <= 1190 && c.Y + r <= 690, $"{id} is inside the safe area");
        }
    }

    [TestMethod]
    public void ControlsKeepTheirPhysicalSizeAndShrinkOnlyToFit()
    {
        var phone = new Rig();
        phone.Screen.Size = new Vector2(2400, 1080);
        phone.Screen.PixelsPerDp = 2.75f;
        phone.Step();
        Assert.AreEqual(36 * 2.75f, phone.Controls.Geometry("jump").Radius, 0.01f, "36 dp at 2.75 px/dp");

        var small = new Rig();
        small.Screen.Size = new Vector2(480, 270);
        small.Screen.PixelsPerDp = 2;
        small.Step();
        var (dpad, dr) = small.Controls.Geometry("dpad");
        var (jump, jr) = small.Controls.Geometry("jump");
        Assert.IsTrue(dpad.Y - dr * 1.3f >= 270 * 0.5f - 1, "a tiny viewport shrinks the controls into its lower half");
        Assert.IsTrue(dpad.X + dr * 1.3f < jump.X - jr * 1.3f, "left and right thumbs never overlap");

        foreach (var size in new[] { new Vector2(2532, 1170), new Vector2(1024, 768), new Vector2(640, 360), new Vector2(1170, 2532) })
        {
            var rig = new Rig();
            rig.Screen.Size = size;
            rig.Screen.PixelsPerDp = 3;
            rig.Step();
            var (a, ar) = rig.Controls.Geometry("dpad");
            var (b, br) = rig.Controls.Geometry("fire");
            Assert.IsTrue(Vector2.Distance(a, b) > ar + br, $"no overlap at {size}");
            Assert.IsTrue(a.X - ar >= 0 && b.X + br <= size.X && a.Y + ar <= size.Y, $"on screen at {size}");
        }
    }

    [TestMethod]
    public void AutoVisibilityFollowsTheDeviceInUse()
    {
        var rig = new Rig();
        rig.Screen.TouchFirst = false;
        rig.Step();
        Assert.IsFalse(rig.Controls.Shown, "a desktop shows no controls before a touch");
        rig.Screen.Down(1, rig.At("jump"));
        rig.Step();
        Assert.IsTrue(rig.Controls.Shown, "the first touch shows them, and it already counts");
        Assert.IsTrue(rig.Map.Down("jump"));
        rig.Screen.Up(1);
        rig.Step();
        rig.Devices.Set(Key.Space, true);
        rig.Step();
        rig.Step();
        Assert.IsFalse(rig.Controls.Shown, "playing on the keyboard hides them");
        rig.Devices.Set(Key.Space, false);
        rig.Screen.Down(2, rig.At("fire"));
        rig.Step();
        Assert.IsTrue(rig.Controls.Shown && rig.Map.Down("fire"), "the next touch brings them back");

        var never = new Rig(Layout with { Visibility = TouchVisibility.Never });
        never.Screen.Down(1, never.At("jump"));
        never.Step();
        Assert.IsFalse(never.Controls.Shown || never.Map.Down("jump"));
    }

    [TestMethod]
    public void HiddenControlsIgnoreTouchesAndReleaseTheirFinger()
    {
        var rig = new Rig();
        rig.Screen.Down(1, rig.At("fire"));
        rig.Step();
        rig.Controls.SetVisible("fire", false);
        rig.Step();
        Assert.IsFalse(rig.Map.Down("fire"));
        rig.Screen.Down(2, rig.At("fire"));
        rig.Step();
        Assert.IsFalse(rig.Map.Down("fire"));
    }

    [TestMethod]
    public void PressesVibrateWhereTheDeviceCan()
    {
        var rig = new Rig();
        rig.Screen.Down(1, rig.At("jump"));
        rig.Screen.Down(2, rig.At("dpad", 1, 0));
        rig.Step();
        Assert.AreEqual(2, rig.Screen.Vibrations);
        rig.Screen.Move(2, rig.At("dpad", 0, 1));
        rig.Step();
        Assert.AreEqual(3, rig.Screen.Vibrations, "a d-pad direction change ticks");

        var quiet = new Rig(Layout with { Haptics = false });
        quiet.Screen.Down(1, quiet.At("jump"));
        quiet.Step();
        Assert.AreEqual(0, quiet.Screen.Vibrations);
    }

    [TestMethod]
    public void APressedControlLooksDifferentInShapeNotOnlyColour()
    {
        var rig = new Rig();
        rig.Screen.Overlay = rig.Controls;
        rig.Step();
        var idle = rig.Screen.DrawOverlay().Shapes.Where(s => Vector2.Distance(s.Center, rig.At("jump")) < 1 && s.Kind == "circle").ToList();
        rig.Screen.Down(1, rig.At("jump"));
        rig.Step();
        var pressed = rig.Screen.DrawOverlay().Shapes.Where(s => Vector2.Distance(s.Center, rig.At("jump")) < 1 && s.Kind == "circle").ToList();
        Assert.IsTrue(pressed[0].Size < idle[0].Size, "drawn smaller when pressed in");
        Assert.IsTrue(pressed[0].Color.A > idle[0].Color.A * 2, "and much more opaque");
        Assert.IsTrue(rig.Screen.DrawOverlay().Shapes.Any(s => s.Kind == "text" && s.Text == "A"), "labelled");
    }

    [TestMethod]
    public void TheJoystickDrawingShowsDirectionAndIntensity()
    {
        var rig = new Rig(TouchLayout.Of(new TouchStick { Placement = StickPlacement.Fixed, Offset = new(100, 100) }));
        rig.Screen.Overlay = rig.Controls;
        var (c, r) = rig.Controls.Geometry("stick");
        rig.Screen.Down(1, c + new Vector2(r * 0.5f, 0));
        rig.Step();
        var weak = rig.Screen.DrawOverlay().Shapes;
        rig.Screen.Move(1, c + new Vector2(r, 0));
        rig.Step();
        var strong = rig.Screen.DrawOverlay().Shapes;
        var knobWeak = weak.Where(s => s.Kind == "circle").Last();
        var knobStrong = strong.Where(s => s.Kind == "circle").Last();
        Assert.IsTrue(knobStrong.Center.X > knobWeak.Center.X, "the knob follows the thumb");
        var arrowWeak = weak.Single(s => s.Kind == "triangle");
        var arrowStrong = strong.Single(s => s.Kind == "triangle");
        Assert.IsTrue(arrowStrong.Center.X > c.X && arrowStrong.Color.A > arrowWeak.Color.A, "a rim arrow points the way, stronger with intensity");
    }

    [TestMethod]
    public void TouchCoexistsWithKeyboardAndGamepad()
    {
        var rig = new Rig();
        rig.Devices.Set(Key.Space, true);
        rig.Screen.Down(1, rig.At("dpad", 1, 0));
        rig.Step();
        Assert.IsTrue(rig.Map.Down("jump") && rig.Map.Down("right"), "keyboard jump with touch direction");
        rig.Devices.Set(Key.Space, false);
        rig.Screen.Up(1);
        rig.Devices.SetLeftStick(new Vector2(-1, 0));
        rig.Screen.Down(2, rig.At("jump"));
        rig.Step();
        Assert.IsTrue(rig.Map.Down("left") && rig.Map.Down("jump"), "gamepad stick with touch jump");
        Assert.IsFalse(rig.Map.Down("right"));
    }

    [TestMethod]
    public void ScriptedTouchesUseTheSameHitTesting()
    {
        var rig = new Rig();
        rig.Controls.ScriptTouch(1, "dpad", new Vector2(1, 0));
        rig.Controls.ScriptTouch(2, "jump", default);
        rig.Step();
        Assert.IsTrue(rig.Map.Down("right") && rig.Map.Pressed("jump"));
        rig.Controls.ScriptLift(2);
        rig.Controls.ScriptTouch(1, "dpad", new Vector2(-1, 0));
        rig.Step();
        Assert.IsTrue(rig.Map.Down("left") && !rig.Map.Down("jump"));
        rig.Controls.ScriptTouchAt(3, new Vector2(0.5f, 0.2f));
        rig.Step();
        Assert.AreEqual(2, rig.Controls.FingerCount, "the middle of the screen is touched, but presses nothing");
    }

    [TestMethod]
    public void SwitchingLayoutsReleasesFingers()
    {
        var rig = new Rig();
        rig.Screen.Down(1, rig.At("jump"));
        rig.Step();
        rig.Controls.Layout = TouchLayout.CreateDefault();
        rig.Step();
        Assert.IsFalse(rig.Map.Down("jump"));
        Assert.IsTrue(rig.Controls.Actions.Contains("confirm"));
    }

    [TestMethod]
    public void TheFrameAvoidsNotchesAndMovesUpInPortrait()
    {
        var landscape = FrameLayout.Fit(new Vector2(2400, 1080), new RectF(120, 0, 2160, 1080), 320, 180);
        Assert.AreEqual(new RectF(240, 0, 1920, 1080), landscape, "6× fits inside the safe area, centred in it");
        var portrait = FrameLayout.Fit(new Vector2(1080, 2400), new RectF(0, 100, 1080, 2250), 320, 180);
        Assert.AreEqual(100, portrait.Y, "top of the safe area, leaving room below for the controls");
        Assert.AreEqual(new Vector2(160, 90), FrameLayout.ToCanvas(landscape.Center, landscape, 320, 180));
        var phone = FrameLayout.Fit(new Vector2(915, 412), new RectF(0, 0, 915, 412), 480, 270);
        Assert.AreEqual(412, phone.Height, 0.5f, "a 1× frame would leave a third of a phone browser empty: scale fractionally");
        var desktop = FrameLayout.Fit(new Vector2(1000, 560), new RectF(0, 0, 1000, 560), 320, 180);
        Assert.AreEqual(540, desktop.Height, "3× of a possible 3.1×: whole pixels win");
    }

    private static bool Covers(RectF frame, (Vector2 Center, float Radius) c)
    {
        float x = Math.Clamp(c.Center.X, frame.X, frame.Right), y = Math.Clamp(c.Center.Y, frame.Y, frame.Bottom);
        return Vector2.Distance(new Vector2(x, y), c.Center) < c.Radius;
    }

    [TestMethod]
    public void OnA16By9ScreenTheFrameShrinksSoNoControlCoversIt()
    {
        var rig = new Rig(Layout with { Controls = [.. Layout.Controls, new TouchButton("pause", "pause", "II") { Anchor = TouchAnchor.TopRight, Offset = new(30, 30), Radius = 20 }] });
        rig.Screen.Size = new Vector2(1920, 1080);
        rig.Screen.PixelsPerDp = 2.75f;
        rig.Step();
        var full = FrameLayout.Fit(rig.Screen.Size, rig.Screen.SafeArea, 320, 180);
        var frame = rig.Controls.Frame(rig.Screen.Size, rig.Screen.SafeArea, 320, 180);
        foreach (var id in new[] { "dpad", "jump", "fire", "pause" })
            Assert.IsFalse(Covers(frame, rig.Controls.Geometry(id)), $"{id} covers the frame {frame}");
        Assert.IsTrue(frame.Width < full.Width && frame.Width >= full.Width * 0.5f, $"{frame} against {full}");
        Assert.AreEqual(0, frame.Y, "moved to the top, so the controls get the space below");
        Assert.AreEqual(1920 / 2f, frame.Center.X, 1, "centred");
        Assert.AreEqual(16 / 9f, frame.Width / frame.Height, 0.01f, "aspect kept");
    }

    [TestMethod]
    public void OnAWidePhoneTheBarsHoldMostOfTheControlsAndTheFrameShrinksLittle()
    {
        var rig = new Rig();
        rig.Screen.Size = new Vector2(2400, 1080);
        rig.Screen.PixelsPerDp = 2.75f;
        rig.Step();
        var full = FrameLayout.Fit(rig.Screen.Size, rig.Screen.SafeArea, 320, 180);
        var frame = rig.Controls.Frame(rig.Screen.Size, rig.Screen.SafeArea, 320, 180);
        foreach (var id in new[] { "dpad", "jump", "fire" }) Assert.IsFalse(Covers(frame, rig.Controls.Geometry(id)), id);
        Assert.IsTrue(frame.Width >= full.Width * 0.75f, $"20:9: {frame} against {full}");

        var tablet = new Rig();
        tablet.Screen.Size = new Vector2(1024, 768);
        tablet.Step();
        Assert.AreEqual(FrameLayout.Fit(tablet.Screen.Size, tablet.Screen.SafeArea, 320, 180).Size, tablet.Controls.Frame(tablet.Screen.Size, tablet.Screen.SafeArea, 320, 180).Size,
            "4:3: the frame at the top leaves room below for the controls at full size");
    }

    [TestMethod]
    public void HiddenControlsLeaveTheFrameAsLargeAsItCanBe()
    {
        var rig = new Rig();
        rig.Screen.TouchFirst = false;
        rig.Step();
        Assert.IsFalse(rig.Controls.Shown);
        Assert.AreEqual(FrameLayout.Fit(new Vector2(1280, 720), new RectF(0, 0, 1280, 720), 320, 180), rig.Controls.Frame(new Vector2(1280, 720), new RectF(0, 0, 1280, 720), 320, 180));
        var off = new Rig();
        off.Controls.KeepFrameClear = false;
        Assert.AreEqual(FrameLayout.Fit(off.Screen.Size, off.Screen.SafeArea, 320, 180), off.Controls.Frame(off.Screen.Size, off.Screen.SafeArea, 320, 180));
    }

    [TestMethod]
    public void TheContextShowsOnlyTheControlsItsActionsNeed()
    {
        var rig = new Rig();
        rig.Screen.Down(1, rig.At("dpad", 1, 0));
        rig.Step();
        Assert.IsTrue(rig.Map.Down("right"));
        var frameBefore = rig.Controls.Frame(rig.Screen.Size, rig.Screen.SafeArea, 320, 180);
        rig.Controls.SetContext(["jump"]);
        rig.Step();
        Assert.IsFalse(rig.Controls.IsVisible("dpad") || rig.Controls.IsVisible("fire"), "a dialogue hides what it does not use");
        Assert.IsTrue(rig.Controls.IsVisible("jump"));
        Assert.IsFalse(rig.Map.Down("right"), "the hidden d-pad released its finger");
        Assert.AreEqual(frameBefore, rig.Controls.Frame(rig.Screen.Size, rig.Screen.SafeArea, 320, 180), "the game does not change size with the context");
        rig.Screen.Down(2, rig.At("jump"));
        rig.Step();
        Assert.IsTrue(rig.Map.Down("jump"));
        rig.Controls.SetContext(null);
        rig.Step();
        Assert.IsTrue(rig.Controls.IsVisible("dpad"));
        rig.Controls.SetVisible("fire", false);
        rig.Controls.SetContext(["fire"]);
        Assert.IsFalse(rig.Controls.IsVisible("fire"), "a control the game hid stays hidden whatever the context");
    }

    private sealed class DialogueGame : Game
    {
        public override GameSettings Settings { get; } = new("dialogue");
        protected override TouchLayout CreateTouchLayout() => Layout;
        protected override void Load() => Scenes.Push(new Field());
        private sealed class Field : Scene
        {
            public override void Update(float dt) { if (Input.Pressed("confirm")) Game.Scenes.Push(new Talk()); }
            public override void Draw(IRenderer renderer) { }
        }
        private sealed class Talk : Scene
        {
            public override IReadOnlyCollection<string>? TouchActions { get; } = ["jump"];
            public override bool IsOverlay => true;
            public override void Update(float dt) { if (Input.Pressed("cancel")) Game.Scenes.Pop(); }
            public override void Draw(IRenderer renderer) { }
        }
    }

    [TestMethod]
    public void TheTopScenesTouchActionsApplyEveryFrame()
    {
        var game = new DialogueGame();
        var platform = new HeadlessPlatform();
        game.InitializeForTest(platform);
        game.StepForTest();
        Assert.IsTrue(game.Touch.IsVisible("dpad"));
        game.Input.SetForced("confirm", true);
        game.StepForTest();
        game.Input.SetForced("confirm", false);
        game.StepForTest();
        Assert.IsFalse(game.Touch.IsVisible("dpad"), "the dialogue on top hides the d-pad");
        game.Input.SetForced("cancel", true);
        game.StepForTest();
        game.Input.SetForced("cancel", false);
        game.StepForTest();
        Assert.IsTrue(game.Touch.IsVisible("dpad"), "back after the dialogue closes");
    }

    // Whole-loop behaviour: the platform's touch screen, focus loss and suspension.

    private sealed class TouchGame : Game
    {
        public override GameSettings Settings { get; } = new("touch");
        public List<string> Log { get; } = [];
        protected override TouchLayout CreateTouchLayout() => Layout;
        protected override InputMap CreateInput() => InputMap.CreateDefault().Bind("jump", Key.Space);
        protected override void Load() => Scenes.Push(new Probe(this));

        private sealed class Probe(TouchGame game) : Scene
        {
            public override void Update(float dt)
            {
                if (Input.Pressed("jump")) game.Log.Add($"jump {Game.UpdateCount}");
                if (Input.Down("right")) game.Log.Add($"right {Game.UpdateCount}");
            }
            public override void Draw(IRenderer renderer) { }
        }
    }

    private sealed class SuspendingPlatform : IPlatform
    {
        public HeadlessPlatform Inner { get; } = new();
        public bool Suspended { get; set; }
        public ITouchScreen Touch => Inner.Touch;
        public IGraphicsDevice Graphics => Inner.Graphics;
        public Audio.IAudioDevice Audio => Inner.Audio;
        public IInputSource Input => Inner.Input;
        public bool CloseRequested => false;
        public double Now => Inner.Now;
        public bool RealTime => false;
        public void PollEvents() => Inner.PollEvents();
        public IRenderer BeginFrame() => Inner.BeginFrame();
        public void EndFrame() { }
        public void SaveScreenshot(string path) { }
        public void ToggleFullscreen() { }
        public void Dispose() { }
    }

    [TestMethod]
    public void TheLoopFeedsTouchesToTheGameAndSuspensionReleasesThem()
    {
        var platform = new SuspendingPlatform();
        var game = new TouchGame();
        var loop = new GameLoop(game, platform);
        Assert.IsTrue(loop.Start());
        Assert.AreSame(game.Touch, platform.Inner.TouchScreen.Overlay, "the game's controls are drawn on the platform's screen");
        loop.Tick();
        var screen = platform.Inner.TouchScreen;
        screen.Down(1, game.Touch.Geometry("dpad").Center + new Vector2(game.Touch.Geometry("dpad").Radius, 0));
        screen.Down(2, game.Touch.Geometry("jump").Center);
        loop.Tick();
        loop.Tick();
        Assert.IsTrue(game.Log.Contains("jump 2") && game.Log.Contains("right 2") && game.Log.Contains("right 3"), string.Join(", ", game.Log));
        platform.Suspended = true;
        loop.Tick();
        platform.Suspended = false;
        game.Log.Clear();
        loop.Tick();
        loop.Tick();
        Assert.AreEqual(0, game.Log.Count, "fingers held when the game left are released: nothing stays pressed after resuming");
        loop.Finish();
    }

    [TestMethod]
    public void AScriptTouchesTheControlsThroughTheLoop()
    {
        var game = new TouchGame();
        var script = InputScript.Parse("""
            5 touch 1 dpad 1 0
            6 tap 2 jump
            20 lift 1
            30 quit
            """);
        GameRunner.Run(game, new HeadlessPlatform(), new RunOptions { Script = script });
        Assert.IsTrue(game.Log.Contains("jump 7"), string.Join(", ", game.Log));
        Assert.AreEqual(15, game.Log.Count(l => l.StartsWith("right")), "right held for the 15 updates between touch and lift");
    }
}
