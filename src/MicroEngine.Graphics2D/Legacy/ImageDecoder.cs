using System.Runtime.InteropServices;
using StbImageSharp;

namespace MicroEngine.Graphics;

/// <summary>
/// Decodes image files in managed code with StbImageSharp, the same on every platform and in headless tests: PNG (every
/// colour type and bit depth, interlaced or not), JPEG, BMP, TGA, GIF (first frame) and PSD (composited). It replaced the
/// engine's own PNG decoder after decoding every PNG of the repository identically, about seven times faster (ASSET-1).
/// </summary>
internal static class ImageDecoder
{
    /// <summary>File extensions <see cref="Decode"/> reads, lower case with the dot.</summary>
    public static readonly IReadOnlySet<string> Extensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".png", ".jpg", ".jpeg", ".bmp", ".tga", ".gif", ".psd" };

    public static PixelImage Decode(ReadOnlySpan<byte> data)
    {
        if (data.Length == 0) throw new InvalidDataException("The file is empty.");
        ImageResult result;
        try { result = ImageResult.FromMemory(data.ToArray(), ColorComponents.RedGreenBlueAlpha); }
        catch (Exception e) when (e is not OutOfMemoryException)
        {
            throw new InvalidDataException($"Not an image the engine can read ({e.Message}). Supported: PNG, JPEG, BMP, TGA, GIF and PSD.", e);
        }
        if (result.Width <= 0 || result.Height <= 0 || result.Data.Length < result.Width * result.Height * 4)
            throw new InvalidDataException("The image has no pixels.");
        // Color is four bytes in R, G, B, A order, exactly StbImageSharp's layout.
        var pixels = MemoryMarshal.Cast<byte, Color>(result.Data.AsSpan(0, result.Width * result.Height * 4)).ToArray();
        return new PixelImage(result.Width, result.Height, pixels);
    }
}
