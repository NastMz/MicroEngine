using System.Buffers.Binary;

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
}
