using System.Numerics;
using MicroEngine.Input;

namespace MicroEngine.Tests;

[TestClass]
public sealed class TouchPadTests
{
    private const float Aspect = 16f / 9f;

    private static Vector2 CenterOf(TouchPad pad, PadButton button) => pad.Controls(Aspect).Single(c => c.Button == button).Center;

    [TestMethod]
    public void AFingerOnAControlPressesItsButtonUntilLifted()
    {
        var pad = new TouchPad();
        Assert.IsFalse(pad.Visible);
        pad.Set(1, CenterOf(pad, PadButton.A), Aspect);
        Assert.IsTrue(pad.Visible, "the controls appear after the first touch");
        Assert.IsTrue(pad.IsDown(PadButton.A));
        Assert.IsFalse(pad.IsDown(PadButton.B));
        pad.Release(1);
        Assert.IsFalse(pad.IsDown(PadButton.A));
    }

    [TestMethod]
    public void FingersAreIndependentAndCanSlideBetweenControls()
    {
        var pad = new TouchPad();
        pad.Set(1, CenterOf(pad, PadButton.DPadRight), Aspect);
        pad.Set(2, CenterOf(pad, PadButton.A), Aspect);
        Assert.IsTrue(pad.IsDown(PadButton.DPadRight) && pad.IsDown(PadButton.A), "move and jump at once");
        pad.Set(1, CenterOf(pad, PadButton.DPadLeft), Aspect);
        Assert.IsFalse(pad.IsDown(PadButton.DPadRight));
        Assert.IsTrue(pad.IsDown(PadButton.DPadLeft));
        pad.Set(1, new Vector2(0.5f, 0.5f), Aspect);
        Assert.IsFalse(pad.IsDown(PadButton.DPadLeft), "the middle of the screen is no control");
        pad.ReleaseAll();
        Assert.IsFalse(pad.IsDown(PadButton.A));
    }

    [TestMethod]
    public void ControlsStayOnScreenAndApartAtPhoneAndTabletAspects()
    {
        foreach (float aspect in new[] { 4f / 3f, 16f / 9f, 19.5f / 9f })
        {
            var controls = new TouchPad().Controls(aspect).ToList();
            CollectionAssert.AreEquivalent(new[] { PadButton.DPadLeft, PadButton.DPadRight, PadButton.DPadUp, PadButton.DPadDown,
                PadButton.A, PadButton.B, PadButton.X, PadButton.RightShoulder, PadButton.Start }, controls.Select(c => c.Button).ToArray());
            foreach (var c in controls)
            {
                Assert.IsTrue(c.Center.X - c.Radius / aspect >= 0 && c.Center.X + c.Radius / aspect <= 1, $"{c.Button} leaves the screen at {aspect}");
                Assert.IsTrue(c.Center.Y - c.Radius >= 0 && c.Center.Y + c.Radius <= 1, $"{c.Button} leaves the screen at {aspect}");
            }
            foreach (var a in controls)
                foreach (var b in controls.Where(b => b.Button != a.Button))
                    Assert.IsTrue(Vector2.Distance(a.Center * new Vector2(aspect, 1), b.Center * new Vector2(aspect, 1)) >= a.Radius + b.Radius - 0.06f, $"{a.Button} overlaps {b.Button}");
        }
    }
}
