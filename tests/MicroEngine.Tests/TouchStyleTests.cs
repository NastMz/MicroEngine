using System.Numerics;
using MicroEngine.Core;
using MicroEngine.Graphics;
using MicroEngine.Input;

namespace MicroEngine.Tests;

/// <summary>Touch control styles and skins change how controls look, never how they are touched.</summary>
[TestClass]
public sealed class TouchStyleTests
{
    private sealed class Rig
    {
        public HeadlessTouchScreen Screen { get; } = new(new Vector2(1280, 720)) { TouchFirst = true };
        public VirtualControls Controls { get; }
        public InputMap Map { get; } = InputMap.CreateDefault().Bind("jump", Key.Space);

        public Rig(TouchLayout layout)
        {
            Controls = new VirtualControls(layout);
            Map.AddSource(Controls);
            Screen.Overlay = Controls;
            Step();
        }

        public void Step()
        {
            Controls.Update(Screen, 1 / 60f, Map.LastDevice);
            Map.Sample(new HeadlessInput());
            Map.EndUpdate();
        }

        public List<RecordingOverlayCanvas.Shape> Near(string id) =>
            Screen.DrawOverlay().Shapes.Where(s => Vector2.Distance(s.Center, Controls.Geometry(id).Center) <= Controls.Geometry(id).Radius * 1.5f).ToList();
    }

    private static TouchControl[] Controls() =>
    [
        new TouchDPad { Offset = new(100, 100) },
        new TouchButton("jump", "jump", "A") { Anchor = TouchAnchor.BottomRight, Offset = new(70, 80), Radius = 36 },
    ];

    [TestMethod]
    public void AStyleChangesTheDrawingButNotTheHitArea()
    {
        var plain = new Rig(TouchLayout.Of(Controls()));
        var styled = new Rig(TouchLayout.Of(Controls()) with
        {
            ButtonStyle = new TouchStyle { Shape = TouchShape.RoundedSquare, Fill = Color.Hex("ff000080"), Outline = Color.Hex("00ff00"), OutlineWidth = 3 },
        });
        Assert.AreEqual(plain.Controls.Geometry("jump"), styled.Controls.Geometry("jump"), "same place and size");
        // A touch just inside the plain hit area presses the styled control too.
        var (c, r) = styled.Controls.Geometry("jump");
        styled.Screen.Down(1, c + new Vector2(r * 1.25f, 0));
        styled.Step();
        Assert.IsTrue(styled.Map.Down("jump"));

        var shapes = new Rig(TouchLayout.Of(Controls()) with { ButtonStyle = new TouchStyle { Shape = TouchShape.RoundedSquare, Fill = Color.Hex("ff000080") } }).Near("jump");
        Assert.IsFalse(shapes.Any(s => s.Kind is "circle" or "ring"), "a rounded square has no discs");
        Assert.IsTrue(shapes.Any(s => s.Kind == "rect" && s.Color.R == 255 && s.Color.G == 0), "filled with the style's colour");
        Assert.IsTrue(shapes.Any(s => s.Kind == "text" && s.Text == "A"), "the label stays");
    }

    [TestMethod]
    public void APerControlStyleWinsOverTheKindStyle()
    {
        var layout = TouchLayout.Of(
            new TouchButton("jump", "jump", "A") { Anchor = TouchAnchor.BottomRight, Offset = new(70, 80), Style = new TouchStyle { Label = Color.Hex("123456") } },
            new TouchButton("fire", "fire", "B") { Anchor = TouchAnchor.BottomRight, Offset = new(170, 80) }) with
        { ButtonStyle = new TouchStyle { Label = Color.Hex("abcdef") } };
        var rig = new Rig(layout);
        var texts = rig.Screen.DrawOverlay().Shapes.Where(s => s.Kind == "text").ToDictionary(s => s.Text!, s => s.Color);
        Assert.AreEqual((byte)0x12, texts["A"].R);
        Assert.AreEqual((byte)0xab, texts["B"].R);
    }

    [TestMethod]
    public void ImagesReplaceFillAndOutlineAndShowThePress()
    {
        var idle = new PixelImage(16, 16);
        var down = new PixelImage(16, 16);
        var rig = new Rig(TouchLayout.Of(Controls()) with { ButtonStyle = new TouchStyle { Image = idle, PressedImage = down } });
        var shapes = rig.Near("jump");
        Assert.AreEqual(1, shapes.Count(s => s.Kind == "image"));
        Assert.IsFalse(shapes.Any(s => s.Kind is "circle" or "ring"));
        Assert.AreEqual(rig.Controls.Geometry("jump").Radius * 2, shapes.Single(s => s.Kind == "image").Size, 0.01f, "covers the control");
        var (c, _) = rig.Controls.Geometry("jump");
        rig.Screen.Down(1, c);
        rig.Step();
        Assert.IsTrue(rig.Near("jump").Single(s => s.Kind == "image").Size < rig.Controls.Geometry("jump").Radius * 2, "pressed in");
    }

    [TestMethod]
    public void TheDefaultLookIsUnchanged()
    {
        // The default style draws what the controls drew before styles existed: halo ring, fill disc, outline ring, label.
        var rig = new Rig(TouchLayout.Of(Controls()));
        var kinds = rig.Near("jump").Select(s => s.Kind).ToList();
        CollectionAssert.AreEqual(new[] { "ring", "circle", "ring", "text" }, kinds);
    }

    private sealed class CountingSkin : ITouchSkin
    {
        public List<TouchControlView> Views { get; } = [];
        public void Draw(IOverlayCanvas canvas, in TouchControlView view)
        {
            Views.Add(view);
            if (view.Control is TouchDPad) DefaultTouchSkin.Instance.Draw(canvas, view);
        }
    }

    [TestMethod]
    public void ASkinSeesEachControlsStateAndCanDelegate()
    {
        var skin = new CountingSkin();
        var rig = new Rig(TouchLayout.Of(Controls()) with { Skin = skin });
        var (c, r) = rig.Controls.Geometry("dpad");
        rig.Screen.Down(1, c + new Vector2(r * 0.8f, 0));
        rig.Step();
        var shapes = rig.Screen.DrawOverlay().Shapes;
        var pad = skin.Views.Last(v => v.Control.Id == "dpad");
        Assert.IsTrue(pad.Pressed && pad.Held && pad.Directions.Contains(InputMap.StickDirection.Right));
        Assert.IsTrue(shapes.Any(s => s.Kind == "triangle"), "the d-pad was delegated to the default skin");
        Assert.IsFalse(shapes.Any(s => s.Kind == "text" && s.Text == "A"), "the button was left to the custom skin, which drew nothing");
    }

    [TestMethod]
    public void RoundedShapesNeverOverlapThemselves()
    {
        var canvas = new RecordingOverlayCanvas();
        OverlayShapes.FillRoundedRect(canvas, new RectF(0, 0, 40, 20), 6, Color.White);
        // Four corner fans of eight triangles plus three rectangles: area adds up to the rounded rectangle's.
        float rects = canvas.Shapes.Where(s => s.Kind == "rect").Count();
        Assert.AreEqual(3, rects);
        Assert.AreEqual(32, canvas.Shapes.Count(s => s.Kind == "triangle"));
    }
}
