using System.Buffers.Binary;
using MicroEngine.Graphics;

namespace MicroEngine.Tests;

/// <summary>Image files other than plain PNGs decode in managed code (ADR 0025), recognised by content, not by name.</summary>
[TestClass]
public sealed class ImageFormatTests
{
    // An 8×8 JPEG, quality 100, written by GDI+: the left half (220, 30, 30), the right half (30, 30, 220).
    private const string Jpeg = "/9j/4AAQSkZJRgABAQEAYABgAAD/2wBDAAEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQH/2wBDAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQH/wAARCAAIAAgDASIAAhEBAxEB/8QAHwAAAQUBAQEBAQEAAAAAAAAAAAECAwQFBgcICQoL/8QAtRAAAgEDAwIEAwUFBAQAAAF9AQIDAAQRBRIhMUEGE1FhByJxFDKBkaEII0KxwRVS0fAkM2JyggkKFhcYGRolJicoKSo0NTY3ODk6Q0RFRkdISUpTVFVWV1hZWmNkZWZnaGlqc3R1dnd4eXqDhIWGh4iJipKTlJWWl5iZmqKjpKWmp6ipqrKztLW2t7i5usLDxMXGx8jJytLT1NXW19jZ2uHi4+Tl5ufo6erx8vP09fb3+Pn6/8QAHwEAAwEBAQEBAQEBAQAAAAAAAAECAwQFBgcICQoL/8QAtREAAgECBAQDBAcFBAQAAQJ3AAECAxEEBSExBhJBUQdhcRMiMoEIFEKRobHBCSMzUvAVYnLRChYkNOEl8RcYGRomJygpKjU2Nzg5OkNERUZHSElKU1RVVldYWVpjZGVmZ2hpanN0dXZ3eHl6goOEhYaHiImKkpOUlZaXmJmaoqOkpaanqKmqsrO0tba3uLm6wsPExcbHyMnK0tPU1dbX2Nna4uPk5ebn6Onq8vP09fb3+Pn6/9oADAMBAAIRAxEAPwD8K/j/AP8AMpf9x7/3DUUUV/v1+y5/5QU8Df8AvJn/AK+HxBP2T9o1/wApl+Mf/ePf/XV8Dn//2Q==";

    private static void Near(Color expected, Color actual, int tolerance, string what) =>
        Assert.IsTrue(Math.Abs(expected.R - actual.R) <= tolerance && Math.Abs(expected.G - actual.G) <= tolerance && Math.Abs(expected.B - actual.B) <= tolerance,
            $"{what}: {actual} is not within {tolerance} of {expected}");

    [TestMethod]
    public void Jpeg_decodes_to_opaque_pixels_close_to_the_original()
    {
        var image = PixelImage.Decode(Convert.FromBase64String(Jpeg));
        Assert.AreEqual(8, image.Width);
        Assert.AreEqual(8, image.Height);
        Near(new Color(220, 30, 30), image[0, 0], 24, "left");
        Near(new Color(30, 30, 220), image[7, 7], 24, "right");
        Assert.AreEqual(255, image[3, 4].A);
    }

    /// <summary>A 24-bit bottom-up BMP: the classic layout of paint programs.</summary>
    private static byte[] Bmp(int width, int height, Func<int, int, Color> pixel)
    {
        int stride = (width * 3 + 3) & ~3, size = 54 + stride * height;
        var data = new byte[size];
        data[0] = (byte)'B'; data[1] = (byte)'M';
        BinaryPrimitives.WriteInt32LittleEndian(data.AsSpan(2), size);
        BinaryPrimitives.WriteInt32LittleEndian(data.AsSpan(10), 54);
        BinaryPrimitives.WriteInt32LittleEndian(data.AsSpan(14), 40);
        BinaryPrimitives.WriteInt32LittleEndian(data.AsSpan(18), width);
        BinaryPrimitives.WriteInt32LittleEndian(data.AsSpan(22), height);
        BinaryPrimitives.WriteInt16LittleEndian(data.AsSpan(26), 1);
        BinaryPrimitives.WriteInt16LittleEndian(data.AsSpan(28), 24);
        for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
            {
                var c = pixel(x, y);
                int at = 54 + (height - 1 - y) * stride + x * 3;
                (data[at], data[at + 1], data[at + 2]) = (c.B, c.G, c.R);
            }
        return data;
    }

    [TestMethod]
    public void Bmp_decodes_exactly_and_loads_from_a_file_whatever_its_extension()
    {
        var bytes = Bmp(3, 2, (x, y) => new Color((byte)(x * 100), (byte)(y * 200), 7));
        var image = PixelImage.Decode(bytes);
        Assert.AreEqual(new Color(200, 200, 7), image[2, 1]);
        Assert.AreEqual(new Color(0, 0, 7), image[0, 0]);
        string path = Path.Combine(Path.GetTempPath(), $"me-image-{Guid.NewGuid():N}.dat");
        try
        {
            File.WriteAllBytes(path, bytes);
            Assert.AreEqual(new Color(100, 0, 7), PixelImage.Load(path)[1, 0]);
        }
        finally { File.Delete(path); }
    }

    [TestMethod]
    public void Unreadable_data_fails_with_a_message_naming_the_supported_formats()
    {
        var error = Assert.ThrowsExactly<InvalidDataException>(() => PixelImage.Decode([1, 2, 3, 4, 5, 6, 7, 8, 9]));
        StringAssert.Contains(error.Message, "PNG, JPEG");
        Assert.ThrowsExactly<InvalidDataException>(() => PixelImage.Decode([]));
        Assert.ThrowsExactly<InvalidDataException>(() => PixelImage.FromPng(Convert.FromBase64String(Jpeg)), "FromPng still insists on a PNG");
    }
}
