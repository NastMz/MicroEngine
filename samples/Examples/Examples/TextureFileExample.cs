using System.Numerics;
using MicroEngine;
using MicroEngine.Graphics;
using MicroEngine.Scenes;
using Dev = MicroEngine.Graphics.Devices;

namespace Examples;

/// <summary>
/// A texture made ahead of time. <c>microengine textures</c> turned <c>Content/pictures/emblem.png</c> into
/// <c>Content/textures/emblem.ktx2</c>: the same picture as BC7 blocks, a quarter of the memory, with nothing to decode
/// when it loads. A device that samples BC7 draws that file; every other device, and a platform without a graphics
/// device, draws the image, which the game ships too.
/// </summary>
public sealed class TextureFileExample : Scene
{
    private static readonly RectF Whole = new(0, 0, 320, 180);
    private static readonly Vector2 Place = new(96, 34);

    #region load
    private Renderer2D? _batcher;
    private Dev.Texture? _ready;   // the .ktx2 file, on a device that samples its format
    private Texture? _image;       // the image file, everywhere else

    public override void Enter()
    {
        if (Game.Graphics is DeviceGraphics graphics)
        {
            var file = Dev.TextureFile.Parse(Assets.Bytes("textures/emblem.ktx2"));
            if (file.CanCreate(graphics.Device.Capabilities))
            {
                _ready = file.Create(graphics.Device);   // the file's bytes go to the device as they are
                _batcher = new Renderer2D(graphics.Device);
            }
        }
        if (_ready is null) _image = Game.Graphics.CreateTexture(Assets.Image("pictures/emblem.png"), TextureFilter.Linear);
    }

    public override void Exit()   // the scene made both textures, so it releases them
    {
        _batcher?.Dispose();
        _ready?.Dispose();
        _image?.Dispose();
    }
    #endregion

    /// <summary>Whether the texture on screen came from the file made ahead of time (the device samples its format).</summary>
    public bool DrawsTheTextureFile => _ready is not null;

    public override void Update(float dt)
    {
        if (Input.Pressed("pause")) Game.Scenes.Pop();
    }

    public override void Draw(IRenderer r)
    {
        r.FillGradient(Whole, Color.Hex("1a1c2c"), Color.Hex("333c57"));

        #region draw
        if (r is DeviceRenderer canvas && _ready is not null)
        {
            canvas.Flush();   // the background is in the canvas now; the texture lands over it
            _batcher!.Begin(new BatchSettings(canvas.Target, Sampler: Dev.SamplerDescription.LinearClamp));
            _batcher.Draw(_ready, new RectF(0, 0, _ready.Width, _ready.Height), Matrix3x2.CreateScale(0.5f) * Matrix3x2.CreateTranslation(Place), ColorF.White);
            _batcher.End();
        }
        else r.Draw(_image!, _image!.Bounds, Place, Color.White, new DrawOptions(Scale: new Vector2(0.5f)));
        #endregion

        string source = _ready is not null ? $"EMBLEM.KTX2: {_ready.Format.ToString().ToUpperInvariant()}, {MemoryOf(_ready) / 1024} KB ON THE DEVICE" : $"EMBLEM.PNG: RGBA, {_image!.SizeInBytes / 1024} KB ON THE DEVICE";
        Font.DrawCentered(r, source, new Vector2(160, 166), Color.Hex("94b0c2"));
        Heading.Draw(r, Font, "A TEXTURE MADE AHEAD OF TIME", "");
    }

    private static long MemoryOf(Dev.Texture texture) => Dev.TextureFormats.ByteCount(texture.Format, texture.Width, texture.Height);
}
