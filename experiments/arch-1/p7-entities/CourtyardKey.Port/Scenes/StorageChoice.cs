using Arch1.Scene;

namespace CourtyardKey.Scenes;

/// <summary>
/// P7 measurement switch: the attachment storage the port's world uses. <c>P7_STORAGE</c> (DenseStable,
/// DenseSwapRemove, PerEntityDictionary) picks it for a whole process; tests set <see cref="Override"/> per thread.
/// </summary>
public static class StorageChoice
{
    [ThreadStatic] private static AttachmentStorage? _override;

    /// <summary>The storage for worlds created on this thread from now on (null: the environment's or DenseStable).</summary>
    public static AttachmentStorage? Override { get => _override; set => _override = value; }

    /// <summary>The storage a new play scene's world uses.</summary>
    public static AttachmentStorage Current =>
        _override ?? (Enum.TryParse<AttachmentStorage>(Environment.GetEnvironmentVariable("P7_STORAGE"), out var s) ? s : AttachmentStorage.DenseStable);
}
