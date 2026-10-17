using TurnExplorer.Model;

namespace TurnExplorer.Storage;

public static class CheckpointFile
{
    /// <summary>Publishes a new file; never replaces an existing destination.</summary>
    public static void CreateNew(string path, TurnCheckpoint checkpoint)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        var bytes = TurnCheckpointCodec.Encode(checkpoint);
        var destination = Path.GetFullPath(path);
        var temporary = Path.Combine(Path.GetDirectoryName(destination)!, ".checkpoint-" + Guid.NewGuid().ToString("N") + ".tmp");
        var ownsTemporary = false;
        try
        {
            var stream = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None);
            ownsTemporary = true;
            try { stream.Write(bytes); }
            catch (Exception writeError)
            {
                try { stream.Dispose(); }
                catch (Exception closeError)
                {
                    throw new AggregateException("Checkpoint write and close failed.", writeError, closeError);
                }
                throw;
            }
            stream.Dispose();
            File.Move(temporary, destination, overwrite: false);
            ownsTemporary = false;
        }
        catch (Exception primary)
        {
            if (ownsTemporary)
            {
                try { File.Delete(temporary); }
                catch (Exception cleanup)
                {
                    throw new AggregateException("Checkpoint publication and cleanup failed.", primary, cleanup);
                }
            }
            throw;
        }
    }

    /// <summary>Reads at most 25 bytes, then validates the exact CP-1 payload.</summary>
    public static TurnCheckpoint Read(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        var bytes = new byte[25];
        var count = 0;
        while (count < bytes.Length)
        {
            var read = stream.Read(bytes, count, bytes.Length - count);
            if (read == 0) break;
            count += read;
        }
        return TurnCheckpointCodec.Decode(bytes.AsSpan(0, count).ToArray());
    }
}
