using System.Buffers.Binary;
using MicroEngine.Graphics;

namespace MicroEngine.Cli;

/// <summary>
/// The game's icon: one square PNG (256, 512 or 1024 pixels) turned into each system's format without an image
/// library. Windows' .ico is written by the build (MicroEngine.targets); macOS' .icns here: both formats can hold the
/// PNG itself.
/// </summary>
public static class Icons
{
    /// <summary>Checks that <paramref name="png"/> is a PNG of an accepted size; returns its side in pixels.</summary>
    public static int Check(string png)
    {
        var bytes = File.ReadAllBytes(png);
        ReadOnlySpan<byte> signature = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];
        if (bytes.Length < 33 || !bytes.AsSpan(0, 8).SequenceEqual(signature) || bytes[12] != 'I' || bytes[13] != 'H' || bytes[14] != 'D' || bytes[15] != 'R')
            throw new CliException($"the icon {png} is not a PNG file.");
        int width = BinaryPrimitives.ReadInt32BigEndian(bytes.AsSpan(16)), height = BinaryPrimitives.ReadInt32BigEndian(bytes.AsSpan(20));
        if (width != height || width is not (256 or 512 or 1024))
            throw new CliException($"the icon {png} is {width}x{height} pixels; it must be square: 256x256, 512x512 or 1024x1024 (1024x1024 is best: iOS asks for that size).");
        return width;
    }

    /// <summary>Writes a macOS .icns holding the PNG as its one image.</summary>
    public static void WriteIcns(string png, string icns)
    {
        int size = Check(png);
        // ic08/ic09/ic10: PNG-compressed 256, 512 and 1024 pixel images.
        string type = size switch { 256 => "ic08", 512 => "ic09", _ => "ic10" };
        var data = File.ReadAllBytes(png);
        using var file = File.Create(icns);
        void Chunk(string kind, int length)
        {
            var header = new byte[8];
            for (int i = 0; i < 4; i++) header[i] = (byte)kind[i];
            BinaryPrimitives.WriteInt32BigEndian(header.AsSpan(4), length);
            file.Write(header);
        }
        Chunk("icns", 8 + 8 + data.Length);
        Chunk(type, 8 + data.Length);
        file.Write(data);
    }

    /// <summary>MicroEngine's own icon, used (with a warning) when a game declares none: phones require one.</summary>
    public static string Default => Path.Combine(Tool.Home, "Hosts", "default-icon.png");

    /// <summary><paramref name="image"/> at <paramref name="size"/> pixels square (<see cref="PixelImage.Resized"/>).</summary>
    public static PixelImage Resize(PixelImage image, int size) => image.Resized(size, size);

    /// <summary>Writes <paramref name="image"/> as a PNG file.</summary>
    public static void WritePng(PixelImage image, string path)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllBytes(path, PngEncoder.Encode(image.ToRgba(), image.Width, image.Height));
    }

    /// <summary>#RRGGBB of a colour (alpha dropped).</summary>
    public static string Hex(Color c) => $"#{c.R:x2}{c.G:x2}{c.B:x2}";

    private static readonly (string Folder, float Scale)[] AndroidDensities = [("mdpi", 1), ("hdpi", 1.5f), ("xhdpi", 2), ("xxhdpi", 3), ("xxxhdpi", 4)];

    /// <summary>
    /// Android launcher icons in <paramref name="res"/>: the legacy square icon at every density (48 dp), and an adaptive
    /// icon (Android 8+) whose foreground is the icon over the 72 dp the launcher shows and whose background is
    /// <paramref name="background"/>, so launchers mask the game's own art instead of shrinking it onto a white plate.
    /// </summary>
    public static void WriteAndroid(string png, string res, Color background)
    {
        Check(png);
        var icon = PixelImage.Load(png);
        foreach (var (folder, scale) in AndroidDensities)
        {
            var legacy = Resize(icon, (int)(48 * scale));
            WritePng(legacy, Path.Combine(res, $"mipmap-{folder}", "appicon.png"));
            // Android 7.1 asks for a round icon; the square one keeps the game's art whole there.
            WritePng(legacy, Path.Combine(res, $"mipmap-{folder}", "appicon_round.png"));
            int layer = (int)(108 * scale), visible = (int)(72 * scale);
            var foreground = new PixelImage(layer, layer).Blit(Resize(icon, visible), (layer - visible) / 2, (layer - visible) / 2);
            WritePng(foreground, Path.Combine(res, $"mipmap-{folder}", "appicon_foreground.png"));
        }
        Directory.CreateDirectory(Path.Combine(res, "mipmap-anydpi-v26"));
        const string adaptive = """
            <?xml version="1.0" encoding="utf-8"?>
            <adaptive-icon xmlns:android="http://schemas.android.com/apk/res/android">
              <background android:drawable="@color/appicon_background" />
              <foreground android:drawable="@mipmap/appicon_foreground" />
            </adaptive-icon>

            """;
        File.WriteAllText(Path.Combine(res, "mipmap-anydpi-v26", "appicon.xml"), adaptive);
        File.WriteAllText(Path.Combine(res, "mipmap-anydpi-v26", "appicon_round.xml"), adaptive);
        Directory.CreateDirectory(Path.Combine(res, "values"));
        File.WriteAllText(Path.Combine(res, "values", "icon.xml"),
            $"<?xml version=\"1.0\" encoding=\"utf-8\"?>\n<resources>\n  <color name=\"appicon_background\">{Hex(background)}</color>\n</resources>\n");
    }
}
