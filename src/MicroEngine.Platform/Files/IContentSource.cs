namespace MicroEngine.Platform;

/// <summary>
/// Where a game's content files come from: a folder next to the executable, a phone's unpacked package, later an
/// archive or a browser's fetch. Paths are relative to the source with '/' separators and match the stored name's
/// letter case exactly on every system.
/// </summary>
public interface IContentSource
{
    /// <summary>Where the files are (a folder's absolute path, or a description), for diagnostics.</summary>
    string Location { get; }
    /// <summary>Whether a file exists at <paramref name="path"/>. False for a missing file and for a path outside the source.</summary>
    bool Exists(string path);
    /// <summary>Opens the file at <paramref name="path"/> for reading; the caller disposes the stream.</summary>
    /// <exception cref="ArgumentException">The path leaves the source.</exception>
    /// <exception cref="FileNotFoundException">No such file, or its name is spelled with another letter case.</exception>
    Stream Open(string path);
    /// <summary>The files directly inside <paramref name="folder"/> ("" for the top), as paths relative to the source, in ordinal order.</summary>
    /// <exception cref="DirectoryNotFoundException">The folder does not exist.</exception>
    IReadOnlyList<string> Files(string folder);
}
