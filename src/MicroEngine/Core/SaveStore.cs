using System.Text.Json;

namespace MicroEngine.Core;

/// <summary>
/// Small JSON persistence for settings and progress. Files live under the user's data folder
/// (<see cref="DefaultDirectory"/>) unless a directory is supplied (tests use a temporary one) or a launcher
/// chose another medium through <see cref="DefaultStorage"/> (the browser keeps saves in localStorage).
/// Corrupt or missing data yields null instead of crashing the game; corrupt data is set aside
/// ("&lt;slot&gt;.corrupt.json" for files) so the next save cannot destroy it. A slot that exists but could not be
/// read (locked file, blocked browser storage) is never written by this store, because the game is then
/// running with fresh progress that would replace the real one. Failed writes return false instead of
/// throwing, so a full disk or browser storage quota leaves the game running with its progress in memory.
/// </summary>
/// <remarks>
/// A slot is a name such as "settings" or "progress"; with file storage it becomes <c>&lt;slot&gt;.json</c> in
/// <see cref="Directory"/>. Values are serialized with <see cref="System.Text.Json.JsonSerializer"/> (public properties).
/// </remarks>
/// <example>
/// <code>
/// public sealed class Progress { public int Level { get; set; } = 1; }
///
/// var store = new SaveStore("com.example.mygame");
/// var progress = store.Load&lt;Progress&gt;("progress") ?? new Progress();
/// progress.Level++;
/// store.Save("progress", progress);
/// </code>
/// </example>
public sealed class SaveStore
{
    private static readonly JsonSerializerOptions Options = new() { WriteIndented = true };
    private readonly ISaveStorage _storage;
    private readonly HashSet<string> _unreadable = new(StringComparer.Ordinal);

    /// <summary>
    /// Storage for stores created without a directory, chosen by the launcher before the game is built
    /// (null: files under local application data). Called with the game id.
    /// </summary>
    public static Func<string, ISaveStorage>? DefaultStorage { get; set; }

    /// <summary>The save folder for file storage; otherwise a description of where saves live.</summary>
    public string Directory => _storage.Location;

    /// <summary>Creates a store for a game.</summary>
    /// <param name="gameId">The game's id, which names its save folder; must not be empty.</param>
    /// <param name="directory">The folder for the slot files; null uses <see cref="DefaultStorage"/> when a launcher set it, otherwise <see cref="DefaultDirectory"/>.</param>
    /// <exception cref="ArgumentException"><paramref name="gameId"/> is null, empty or white space.</exception>
    public SaveStore(string gameId, string? directory = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(gameId);
        _storage = directory is null && DefaultStorage is { } create
            ? create(gameId)
            : new FileSaveStorage(directory ?? DefaultDirectory(gameId));
    }

    /// <summary>
    /// The save folder used when neither a directory nor <see cref="DefaultStorage"/> is given:
    /// <c>%LOCALAPPDATA%\MicroEngine\&lt;gameId&gt;</c> on Windows, <c>~/Library/Application Support/MicroEngine/&lt;gameId&gt;</c>
    /// on macOS, and <c>$XDG_DATA_HOME/MicroEngine/&lt;gameId&gt;</c> (by default <c>~/.local/share</c>) on Linux and other
    /// Unix systems. Always absolute, also when the folder does not exist yet: the first save creates it.
    /// </summary>
    public static string DefaultDirectory(string gameId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(gameId);
        return Path.Combine(UserDataFolder(), "MicroEngine", gameId);
    }

    private static string UserDataFolder()
    {
        if (OperatingSystem.IsWindows()) return Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        // Without DoNotVerify, .NET answers "" when the folder does not exist yet (a new account, a container, CI),
        // and the saves would land in the working directory.
        if (OperatingSystem.IsMacOS()) return Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData, Environment.SpecialFolderOption.DoNotVerify);
        return XdgDataHome(Environment.GetEnvironmentVariable("XDG_DATA_HOME"),
                   Environment.GetFolderPath(Environment.SpecialFolder.UserProfile, Environment.SpecialFolderOption.DoNotVerify))
            ?? Path.GetTempPath(); // no home folder at all: saves last only as long as the temporary folder
    }

    /// <summary>
    /// The XDG base directory rule for user data: <c>$XDG_DATA_HOME</c> when it is an absolute path (a relative value is
    /// invalid and ignored), otherwise <c>&lt;home&gt;/.local/share</c>; null when there is no absolute home either.
    /// </summary>
    internal static string? XdgDataHome(string? xdgDataHome, string? home)
    {
        if (xdgDataHome is { Length: > 1 } && xdgDataHome[0] == '/') return xdgDataHome.TrimEnd('/');
        if (home is { Length: > 0 } && home[0] == '/') return home.TrimEnd('/') + "/.local/share";
        return null;
    }

    /// <summary>Creates a store over a custom storage medium.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="storage"/> is null.</exception>
    public SaveStore(ISaveStorage storage) => _storage = storage ?? throw new ArgumentNullException(nameof(storage));

    /// <summary>Reads and deserializes a slot.</summary>
    /// <typeparam name="T">The saved type.</typeparam>
    /// <param name="slot">The slot name.</param>
    /// <returns>
    /// The saved value, or null when the slot does not exist, holds invalid JSON (the data is set aside) or could not be read
    /// (the slot is then protected from <see cref="Save"/> for the life of this store).
    /// </returns>
    public T? Load<T>(string slot) where T : class
    {
        try
        {
            if (_storage.Read(slot) is not { } text) return null;
            var value = JsonSerializer.Deserialize<T>(text, Options);
            if (value is null) _storage.SetAside(slot);
            return value;
        }
        catch (JsonException)
        {
            _storage.SetAside(slot);
            return null;
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            _unreadable.Add(slot);
            return null;
        }
    }

    /// <summary>
    /// Writes the whole slot at once, so a crash never leaves a half-written save. Returns false when the
    /// medium refused the write, or when the slot could not be read earlier (see the class remarks).
    /// </summary>
    public bool Save<T>(string slot, T data)
    {
        if (_unreadable.Contains(slot)) return false;
        var text = JsonSerializer.Serialize(data, Options);
        try
        {
            _storage.Write(slot, text);
            return true;
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            return false;
        }
    }

    /// <summary>Whether the slot has saved data.</summary>
    public bool Exists(string slot) => _storage.Exists(slot);
    /// <summary>
    /// Removes the slot's data; does nothing when it does not exist. A slot that could not be read can be saved again
    /// after it is deleted: deleting is the game's explicit choice to start over.
    /// </summary>
    public void Delete(string slot)
    {
        _storage.Delete(slot);
        _unreadable.Remove(slot);
    }
}

/// <summary>
/// Where a <see cref="SaveStore"/> keeps its slots: named texts that are written whole. Failures surface
/// as <see cref="IOException"/> or <see cref="UnauthorizedAccessException"/>.
/// </summary>
public interface ISaveStorage
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
    /// <summary>Moves unreadable data out of the slot, keeping a copy; best effort, never throws.</summary>
    void SetAside(string slot);
}

/// <summary>One JSON file per slot in a directory (desktop and tests).</summary>
public sealed class FileSaveStorage(string directory) : ISaveStorage
{
    /// <summary>The directory that holds the slot files.</summary>
    public string Location { get; } = directory ?? throw new ArgumentNullException(nameof(directory));

    private string PathFor(string slot) => Path.Combine(Location, slot + ".json");

    /// <inheritdoc/>
    public string? Read(string slot)
    {
        var path = PathFor(slot);
        return File.Exists(path) ? File.ReadAllText(path) : null;
    }

    /// <summary>Writes atomically (temp file + move).</summary>
    public void Write(string slot, string text)
    {
        System.IO.Directory.CreateDirectory(Location);
        var path = PathFor(slot);
        var temp = path + ".tmp";
        File.WriteAllText(temp, text);
        File.Move(temp, path, overwrite: true);
    }

    /// <inheritdoc/>
    public bool Exists(string slot) => File.Exists(PathFor(slot));
    /// <inheritdoc/>
    public void Delete(string slot) { if (File.Exists(PathFor(slot))) File.Delete(PathFor(slot)); }

    /// <summary>Renames <c>&lt;slot&gt;.json</c> to <c>&lt;slot&gt;.corrupt.json</c>, replacing an earlier copy; leaves the file in place if that fails.</summary>
    public void SetAside(string slot)
    {
        var path = PathFor(slot);
        try { File.Move(path, Path.ChangeExtension(path, ".corrupt.json"), overwrite: true); }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException) { /* Stays in place; callers that check Exists will not overwrite it. */ }
    }
}
