namespace MicroEngine.Loop;

public sealed class PixelImage
{
    public int Width { get; }
    public int Height { get; }
    public IReadOnlyList<Rgba> Pixels { get; }
    public PixelImage(int width, int height, ReadOnlySpan<Rgba> pixels)
    {
        if (width <= 0) throw new ArgumentOutOfRangeException(nameof(width));
        if (height <= 0) throw new ArgumentOutOfRangeException(nameof(height));
        if ((long)width * height != pixels.Length)
            throw new ArgumentException("Pixel count must equal width times height.", nameof(pixels));
        Width = width;
        Height = height;
        Pixels = Array.AsReadOnly(pixels.ToArray());
    }
}
