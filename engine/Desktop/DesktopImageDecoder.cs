using System.Buffers.Binary;
using MicroEngine.Loop;
using Raylib_cs;
namespace MicroEngine.Desktop;

/// <summary>Optional CPU PNG decoder for the Desktop adapter; returns no native ownership.</summary>
public static class DesktopImageDecoder
{
    public static PixelImage Decode(byte[] encodedPng)
    {
        ArgumentNullException.ThrowIfNull(encodedPng);
        ReadOnlySpan<byte> bytes = encodedPng;
        ReadOnlySpan<byte> signature = [137, 80, 78, 71, 13, 10, 26, 10];
        if (bytes.Length < 33 || !bytes[..8].SequenceEqual(signature)
            || BinaryPrimitives.ReadUInt32BigEndian(bytes.Slice(8, 4)) != 13 || !bytes.Slice(12, 4).SequenceEqual("IHDR"u8))
            throw new InvalidDataException("A PNG signature and IHDR are required.");
        uint width = BinaryPrimitives.ReadUInt32BigEndian(bytes.Slice(16, 4));
        uint height = BinaryPrimitives.ReadUInt32BigEndian(bytes.Slice(20, 4));
        if (width is 0 or > 8192 || height is 0 or > 8192 || (ulong)width * height > 16777216)
            throw new InvalidDataException("PNG dimensions exceed the CPU decode budget.");
        var image = Raylib.LoadImageFromMemory(".png", encodedPng);
        try
        {
            if (!Raylib.IsImageValid(image) || image.Width != width || image.Height != height)
                throw new InvalidDataException("PNG decoding failed or dimensions changed.");
            var pixels = new Rgba[checked(image.Width * image.Height)];
            for (int y = 0; y < image.Height; y++)
            for (int x = 0; x < image.Width; x++)
            {
                var color = Raylib.GetImageColor(image, x, y);
                pixels[y * image.Width + x] = new(color.R, color.G, color.B, color.A);
            }
            return new(image.Width, image.Height, pixels);
        }
        finally { if (image.Width > 0) Raylib.UnloadImage(image); }
    }
}
