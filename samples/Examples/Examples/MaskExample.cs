using System.Numerics;
using MicroEngine;
using MicroEngine.Graphics;
using MicroEngine.Scenes;
using Dev = MicroEngine.Graphics.Devices;

namespace Examples;

/// <summary>
/// A picture shown only through a mask, with render targets and the blend states every graphics device has: the
/// picture, the mask and the mask's opposite are drawn into targets of their own with the usual drawing calls; the mask
/// multiplies the picture, its opposite multiplies the scene, and the two are added.
/// </summary>
public sealed class MaskExample : Scene
{
    #region setup
    private static readonly RectF Whole = new(0, 0, 320, 180);
    private Renderer2D? _batcher;
    private Dev.RenderTarget? _picture, _mask, _cover;
    private DeviceRenderer? _pictureRenderer, _maskRenderer, _coverRenderer;   // the drawing API, into our targets
    private Vector2 _spot = new(230, 120);
    private float _time;

    public override void Enter()
    {
        if (Game.Graphics is not DeviceGraphics graphics) return;   // a platform that does not draw on a graphics device
        _batcher = new Renderer2D(graphics.Device);
        _picture = graphics.Device.CreateRenderTarget(new Dev.RenderTargetDescription(320, 180));
        _mask = graphics.Device.CreateRenderTarget(new Dev.RenderTargetDescription(320, 180));
        _cover = graphics.Device.CreateRenderTarget(new Dev.RenderTargetDescription(320, 180));
        _pictureRenderer = new DeviceRenderer(_batcher, graphics, _picture);
        _maskRenderer = new DeviceRenderer(_batcher, graphics, _mask);
        _coverRenderer = new DeviceRenderer(_batcher, graphics, _cover);
    }

    public override void Exit()
    {
        _cover?.Dispose();
        _mask?.Dispose();
        _picture?.Dispose();
        _batcher?.Dispose();
    }
    #endregion

    public override void Update(float dt)
    {
        if (Input.Pressed("pause")) { Game.Scenes.Pop(); return; }
        _spot += Input.Vector("left", "right", "up", "down") * 90 * dt;
        _time += dt;
    }

    public override void Draw(IRenderer r)
    {
        r.FillGradient(Whole, Color.Hex("1a1c2c"), Color.Hex("333c57"));

        if (r is DeviceRenderer canvas && _batcher is not null)
        {
            #region mask
            canvas.Flush();   // the scene is in the canvas now

            // The picture: anything the drawing API draws. Here, moving stripes.
            _pictureRenderer!.Begin();
            _pictureRenderer.Clear(Color.Hex("ef7d57"));
            for (int i = -2; i < 12; i++)
                _pictureRenderer.FillRect(new RectF(i * 32 + _time * 24 % 32, 0, 16, 180), Color.Hex(i % 2 == 0 ? "ffcd75" : "a7f070"));
            _pictureRenderer.End();

            // The mask is white where the picture shows and black elsewhere; the cover is its opposite.
            Shapes(_maskRenderer!, Color.Black, Color.White);
            Shapes(_coverRenderer!, Color.White, Color.Black);

            // Picture × mask: the picture where the mask is white, black elsewhere.
            Over(_picture!, _mask!, Dev.BlendState.Multiply);
            // Scene × cover: a black hole in the scene where the picture goes. Then the picture is added into the hole.
            Over(canvas.Target, _cover!, Dev.BlendState.Multiply);
            Over(canvas.Target, _picture!, Dev.BlendState.Additive);
            #endregion
        }
        else Font.DrawCentered(r, "THIS EXAMPLE NEEDS A WINDOW", new Vector2(160, 86), Color.White);

        Heading.Draw(r, Font, "MASKS", "ARROWS: MOVE THE CIRCLE");
    }

    #region helpers
    /// <summary>The mask's shapes, text and a circle, drawn like anything else.</summary>
    private void Shapes(DeviceRenderer renderer, Color background, Color shape)
    {
        renderer.Begin();
        renderer.Clear(background);
        Font.DrawCentered(renderer, "MASK", new Vector2(160, 70), shape, scale: 9);
        renderer.FillCircle(_spot, 26, shape);
        renderer.End();
    }

    /// <summary>Draws the whole of a target's picture over another target with a blend state.</summary>
    private void Over(Dev.RenderTarget target, Dev.RenderTarget source, Dev.BlendState blend)
    {
        _batcher!.Begin(new BatchSettings(target, Blend: blend));
        _batcher.Draw(source.Color!, Whole, Matrix3x2.Identity, ColorF.White);
        _batcher.End();
    }
    #endregion
}
