namespace MicroEngine.Platform;

/// <summary>What a storage knows about one of its slots without reading the save in it.</summary>
/// <param name="Name">The slot's name, as <see cref="IStorage.Read"/> takes it.</param>
/// <param name="Size">
/// How much the slot holds: the file's size in bytes for file storage, the length of the stored text for a storage
/// that keeps text (the browser's). Good for showing and comparing, not for exact accounting.
/// </param>
/// <param name="LastWrite">
/// When the slot was last written, or null when the storage does not know (a slot written by an earlier version of the
/// browser storage, a medium that keeps no times).
/// </param>
public readonly record struct SlotInfo(string Name, long Size, DateTimeOffset? LastWrite);
