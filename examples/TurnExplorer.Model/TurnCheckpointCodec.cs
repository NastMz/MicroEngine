using System.Buffers.Binary;
using RoomExplorer.Model;

namespace TurnExplorer.Model;

public static class TurnCheckpointCodec
{
    public static byte[] Encode(TurnCheckpoint checkpoint)
    {
        _ = TurnSession.Restore(checkpoint);
        var data = new byte[24];
        Write(data, 0, checkpoint.Version);
        Write(data, 4, (int)checkpoint.Room);
        Write(data, 8, BitConverter.SingleToInt32Bits(checkpoint.X));
        Write(data, 12, BitConverter.SingleToInt32Bits(checkpoint.Y));
        Write(data, 16, (checkpoint.MarkerActivated ? 1 : 0) | (checkpoint.MenuOpen ? 2 : 0)
            | (checkpoint.SelectedOption == MenuOption.Return ? 4 : 0) | (checkpoint.EncounterCompleted ? 8 : 0));
        Write(data, 20, checkpoint.TurnPhase);
        return data;
    }

    public static TurnCheckpoint Decode(byte[] data)
    {
        ArgumentNullException.ThrowIfNull(data);
        if (data.Length != 24) throw new FormatException("Checkpoint must contain exactly 24 bytes.");
        var flags = Read(data, 16);
        var version = Read(data, 0);
        if ((flags & ~(version == 2 ? 15 : 7)) != 0) throw new FormatException("Checkpoint contains unsupported flags.");
        var checkpoint = new TurnCheckpoint(version, (Room)Read(data, 4),
            BitConverter.Int32BitsToSingle(Read(data, 8)), BitConverter.Int32BitsToSingle(Read(data, 12)),
            (flags & 1) != 0, (flags & 2) != 0, (flags & 4) != 0 ? MenuOption.Return : MenuOption.Advance,
            Read(data, 20), (flags & 8) != 0);
        try { _ = TurnSession.Restore(checkpoint); }
        catch (ArgumentException error) { throw new FormatException("Checkpoint contains invalid state.", error); }
        return checkpoint;
    }

    private static void Write(byte[] data, int offset, int value)
        => BinaryPrimitives.WriteInt32LittleEndian(data.AsSpan(offset, 4), value);
    private static int Read(byte[] data, int offset)
        => BinaryPrimitives.ReadInt32LittleEndian(data.AsSpan(offset, 4));
}
