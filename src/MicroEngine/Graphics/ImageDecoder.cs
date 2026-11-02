using System.Runtime.InteropServices;
using StbImageSharp;

namespace MicroEngine.Graphics;

/// <summary>
/// Decodes image files in managed code, the same on every platform and in headless tests. PNGs go through the engine's
/// own decoder (the common case, and the fastest); JPEG, BMP, TGA, GIF (first frame), PSD (composited) and the PNG
/// variants the engine's decoder does not read (16-bit channels, interlacing) go through StbImageSharp.
/// </summary>
internal static class ImageDecoder
{
    /// <summary>File extensions <see cref="Decode"/> reads, lower case with the dot.</summary>
    public static readonly IReadOnlySet<string> Extensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".png", ".jpg", ".jpeg", ".bmp", ".tga", ".gif", ".psd" };

    public static PixelImage Decode(ReadOnlySpan<byte> data)
    {
        if (IsPng(data))
        {
            try { return PngDecoder.Decode(data); }
            catch (NotSupportedException) { /* 16-bit or interlaced: StbImageSharp reads those. */ }
        }
        return DecodeWithStb(data);
    }

    private static bool IsPng(ReadOnlySpan<byte> data) => data.Length >= 8 && data[0] == 0x89 && data[1] == (byte)'P' && data[2] == (byte)'N' && data[3] == (byte)'G';

    private static PixelImage DecodeWithStb(ReadOnlySpan<byte> data)
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
