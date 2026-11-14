namespace MicroEngine.Platform;

/// <summary>
/// Where a game keeps what it saves: named slots of text that are written whole (files on desktops and phones, the
/// page's localStorage in browsers, a cloud service). Failures surface as <see cref="IOException"/> or
/// <see cref="UnauthorizedAccessException"/>.
/// </summary>
/// <remarks>
/// Which names a slot can have is the storage's to say, because only the storage knows what a name becomes: a file
/// storage takes plain file names and keeps every slot inside its folder, a key-value storage may take any text. A
/// storage refuses a name it cannot hold with <see cref="ArgumentException"/> from every member that takes a slot;
/// that is a mistake in the calling code, not a failure of the medium. A plain name (letters, digits, <c>-</c>,
/// <c>_</c>, a dot inside the name) is valid in every storage the engine provides.
/// </remarks>
public interface IStorage
{
    /// <summary>The save folder, or a description of the medium for diagnostics.</summary>
    string Location { get; }
    /// <summary>The slot's text, or null when it does not exist.</summary>
    string? Read(string slot);
    /// <summary>Replaces the slot atomically.</summary>
    void Write(string slot, string text);
    /// <summary>Whether the slot has data.</summary>
    bool Exists(string slot);
    /// <summary>Removes the slot; does nothing when it does not exist.</summary>
    void Delete(string slot);
    /// <summary>Moves unreadable data out of the slot, keeping a copy; best effort, never throws for a failure of the medium.</summary>
    void SetAside(string slot);
}
