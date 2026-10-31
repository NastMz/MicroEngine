using System.Numerics;

namespace MicroEngine.Graphics;

/// <summary>A GPU (or headless) texture owned by the active graphics device.</summary>
public abstract class Texture : IDisposable
{
    public int Width { get; }
    public int Height { get; }
    public bool IsDisposed { get; private set; }
    protected Texture(int width, int height) => (Width, Height) = (width, height);
    public RectF Bounds => new(0, 0, Width, Height);
    public void Dispose()
    {
        if (IsDisposed) return;
        IsDisposed = true;
        Release();
        GC.SuppressFinalize(this);
    }
    protected abstract void Release();
}

public enum BlendMode { Alpha, Additive, Multiply }

/// <summary>Per-draw sprite options. Origin is in source pixels and is also the rotation pivot.</summary>
public readonly record struct DrawOptions(
    bool FlipX = false,
    bool FlipY = false,
    float Rotation = 0f,
    Vector2 Origin = default,
    Vector2? Scale = null)
{
    public static readonly DrawOptions None = new();
}

/// <summary>
/// Immediate-mode 2D drawing in virtual pixels. Coordinates go through the camera
/// pushed with <see cref="PushCamera"/>; with no camera they are screen space.
/// </summary>
public interface IRenderer
{
    int Width { get; }
    int Height { get; }
    void Clear(Color color);
    void Draw(Texture texture, RectF source, Vector2 position, Color tint, in DrawOptions options = default);
    void DrawStretched(Texture texture, RectF source, RectF destination, Color tint);
    void FillRect(RectF rect, Color color);
    void FillCircle(Vector2 center, float radius, Color color);
    void FillTriangle(Vector2 a, Vector2 b, Vector2 c, Color color);
    void DrawLine(Vector2 from, Vector2 to, float thickness, Color color);
    void PushCamera(Camera2D camera);
    void PopCamera();
    void SetBlendMode(BlendMode mode);
    /// <summary>Restricts drawing to a screen-space rectangle; pass null to remove.</summary>
    void SetClip(RectF? screenRect);
}

/// <summary>Creates device resources. Separated so headless tests can provide fakes.</summary>
public interface IGraphicsDevice
{
    Texture CreateTexture(PixelImage image);
}
