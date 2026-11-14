using System.Numerics;
using MicroEngine;
using MicroEngine.Graphics;
using MicroEngine.Scenes;
using Dev = MicroEngine.Graphics.Devices;

namespace Examples;

/// <summary>
/// A picture shown only through a mask, with render targets and one blend state: the picture and the mask are drawn
/// into targets of their own with the usual drawing calls, the mask's alpha is multiplied into the picture, and the
/// result is drawn over the scene.
/// </summary>
public sealed class MaskExample : Scene
{
    #region setup
    /// <summary>Keeps of the destination only what the source covers: destination × source alpha, colour and alpha alike.</summary>
    private static readonly Dev.BlendState KeepWhereCovered = new(Dev.BlendFactor.Zero, Dev.BlendFactor.SrcAlpha, Dev.BlendOp.Add,
        Dev.BlendFactor.Zero, Dev.BlendFactor.SrcAlpha, Dev.BlendOp.Add);
    private static readonly RectF Whole = new(0, 0, 320, 180);
    private Renderer2D? _batcher;
    private Dev.RenderTarget? _picture, _mask;
    private DeviceRenderer? _pictureRenderer, _maskRenderer;   // the drawing API, into our targets
    private Vector2 _spot = new(230, 120);
    private float _time;

    public override void Enter()
    {
        // The blend state is not one of the four every device has: ask before using it.
        if (Game.Graphics is not DeviceGraphics graphics || !graphics.Device.Capabilities.Has(Dev.GraphicsFeatures.CustomBlend)) return;
        _batcher = new Renderer2D(graphics.Device);
        _picture = graphics.Device.CreateRenderTarget(new Dev.RenderTargetDescription(320, 180));
        _mask = graphics.Device.CreateRenderTarget(new Dev.RenderTargetDescription(320, 180));
        _pictureRenderer = new DeviceRenderer(_batcher, graphics, _picture);
        _maskRenderer = new DeviceRenderer(_batcher, graphics, _mask);
    }

    public override void Exit()
    {
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
            canvas.Flush();

            // The picture: anything the drawing API draws. Here, moving stripes.
            _pictureRenderer!.Begin();
            _pictureRenderer.Clear(Color.Hex("ef7d57"));
            for (int i = -2; i < 12; i++)
                _pictureRenderer.FillRect(new RectF(i * 32 + _time * 24 % 32, 0, 16, 180), Color.Hex(i % 2 == 0 ? "ffcd75" : "a7f070"));
            _pictureRenderer.End();

            // The mask: opaque where the picture shows. Text and a circle, drawn like anything else.
            _maskRenderer!.Begin();
            _maskRenderer.Clear(Color.Transparent);
            Font.DrawCentered(_maskRenderer, "MASK", new Vector2(160, 70), Color.White, scale: 9);
            _maskRenderer.FillCircle(_spot, 26, Color.White);
            _maskRenderer.End();

            // Multiply the mask's alpha into the picture, then draw what is left over the scene.
            _batcher.Begin(new BatchSettings(_picture!, Blend: KeepWhereCovered));
            _batcher.Draw(_mask!.Color!, Whole, Matrix3x2.Identity, ColorF.White);
            _batcher.End();
            _batcher.Begin(new BatchSettings(canvas.Target));
            _batcher.Draw(_picture!.Color!, Whole, Matrix3x2.Identity, ColorF.White);
            _batcher.End();
            #endregion
        }
        else Font.DrawCentered(r, "THIS EXAMPLE NEEDS A WINDOW", new Vector2(160, 86), Color.White);

        Heading.Draw(r, Font, "MASKS", "ARROWS: MOVE THE CIRCLE");
    }
}
