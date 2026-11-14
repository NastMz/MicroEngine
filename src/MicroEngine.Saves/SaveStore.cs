using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace MicroEngine.Core;

/// <summary>
/// Small JSON persistence for settings and progress. Without a directory or a storage of its own, a store keeps its slots
/// in the platform's default medium (<see cref="DefaultStorage(string)"/>): files under the user's data folder
/// (<see cref="DefaultDirectory"/>), and the page's localStorage in browsers. A composition can hand a game another medium
/// (<c>HostBuilder.Storage</c>); tests pass a temporary directory.
/// Corrupt or missing data yields null instead of crashing the game; corrupt data is set aside
/// ("&lt;slot&gt;.corrupt.json" for files) so the next save cannot destroy it. A slot that exists but could not be
/// read (locked file, blocked browser storage) is never written by this store, because the game is then
/// running with fresh progress that would replace the real one. Failed writes return false instead of
/// throwing, so a full disk or browser storage quota leaves the game running with its progress in memory.
/// </summary>
/// <remarks>
/// A slot is a name such as "settings" or "progress"; with file storage it becomes <c>&lt;slot&gt;.json</c> in
/// <see cref="Directory"/>. Values are serialized with <see cref="System.Text.Json.JsonSerializer"/> (public properties).
/// <para>
/// A slot is a plain name, never a path, and the same names are valid on every system, so a save can move between
/// them: letters, digits, spaces and punctuation such as <c>-</c>, <c>_</c> and <c>.</c> ("profile.v2"). Every call
/// that takes a slot throws <see cref="ArgumentException"/> for a name that is empty or white space, contains
/// <c>/ \ : * ? " &lt; &gt; |</c> or a control character, ends with a dot or a space (which includes "." and ".."),
/// ends with ".corrupt" in any letter case (the name a slot's unreadable data is set aside under), or
/// is a Windows device name ("CON", "PRN", "AUX", "NUL", "COM1", "LPT1" and the like, with or without an extension).
/// </para>
/// <para>
/// <see cref="Save{T}(string, T)"/> and <see cref="Load{T}(string)"/> find the properties of the saved type by reflection.
/// A game published trimmed or with NativeAOT has no reflection to serialize with: it declares its saved types in a
/// <see cref="System.Text.Json.Serialization.JsonSerializerContext"/> and passes the generated metadata to
/// <see cref="Save{T}(string, T, JsonTypeInfo{T})"/> and <see cref="Load{T}(string, JsonTypeInfo{T})"/>. Both pairs
/// write the same text and read each other's slots.
/// </para>
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
    private const string ReflectionMessage = "Serializes the saved type with reflection, which trimmed and NativeAOT applications do not have. Declare the type in a JsonSerializerContext and pass its JsonTypeInfo<T> to the overload that takes one.";
    private static readonly JsonSerializerOptions Options = new() { WriteIndented = true };
    // The text of a slot is the store's choice, not the metadata's: indented, like the slots written without metadata.
    private static readonly JsonWriterOptions WriterOptions = new() { Indented = true };
    private readonly Platform.IStorage _storage;
    private readonly HashSet<string> _unreadable = new(StringComparer.Ordinal);

    /// <summary>The save folder for file storage; otherwise a description of where saves live.</summary>
    public string Directory => _storage.Location;

    /// <summary>Creates a store for a game.</summary>
    /// <param name="gameId">The game's id, which names its save folder; must not be empty.</param>
    /// <param name="directory">The folder for the slot files; null uses the platform's default medium (<see cref="DefaultStorage(string)"/>).</param>
    /// <exception cref="ArgumentException"><paramref name="gameId"/> is null, empty or white space, or, when it names the save folder, a path that leaves the data folder (<see cref="DefaultDirectory"/>).</exception>
    public SaveStore(string gameId, string? directory = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(gameId);
        _storage = directory is null ? DefaultStorage(gameId) : new FileSaveStorage(directory);
    }

    /// <summary>
    /// The platform's default save medium for a game: the page's localStorage in browsers
    /// (<see cref="Platform.BrowserStorage"/>, keys <c>microengine/&lt;gameId&gt;/&lt;slot&gt;</c>), files in
    /// <see cref="DefaultDirectory"/> everywhere else. A rule of the platform, not a setting: a host that wants another
    /// medium composes it (<c>HostBuilder.Storage</c>) and the game passes it to
    /// <see cref="SaveStore(Platform.IStorage)"/>.
    /// </summary>
    /// <exception cref="ArgumentException"><paramref name="gameId"/> is null, empty or white space, or, outside browsers, a path that leaves the data folder (<see cref="DefaultDirectory"/>).</exception>
    public static Platform.IStorage DefaultStorage(string gameId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(gameId);
        return OperatingSystem.IsBrowser() ? new Platform.BrowserStorage(gameId) : new FileSaveStorage(DefaultDirectory(gameId));
    }

    /// <summary>
    /// The save folder used when no directory is given (outside browsers):
    /// <c>%LOCALAPPDATA%\MicroEngine\&lt;gameId&gt;</c> on Windows, <c>~/Library/Application Support/MicroEngine/&lt;gameId&gt;</c>
    /// on macOS and iOS (inside the app's container), the app's private files folder (<c>files/MicroEngine/&lt;gameId&gt;</c>)
    /// on Android, and <c>$XDG_DATA_HOME/MicroEngine/&lt;gameId&gt;</c> (by default <c>~/.local/share</c>) on Linux and
    /// other Unix systems. Always absolute, also when the folder does not exist yet: the first save creates it.
    /// </summary>
    /// <exception cref="ArgumentException">
    /// <paramref name="gameId"/> is null, empty or white space, or is a path that leaves the data folder: one that starts
    /// with <c>/</c> or <c>\</c>, starts with a drive (<c>C:</c>), or has a part made only of dots and spaces (<c>.</c>, <c>..</c>, <c>...</c>).
    /// </exception>
    public static string DefaultDirectory(string gameId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(gameId);
        if (LeavesTheDataFolder(gameId))
            throw new ArgumentException($"\"{gameId}\" is not a valid game id: it is a path that leaves the save folder. A game id is a name such as \"com.example.mygame\".", nameof(gameId));
        return Path.Combine(UserDataFolder(), "MicroEngine", gameId);
    }

    /// <summary>
    /// Rooted, with a drive, or with a part made only of dots and spaces ("." and ".." among them, and the ones Windows
    /// drops, such as "..."), by the rules of every system at once.
    /// </summary>
    private static bool LeavesTheDataFolder(string gameId)
    {
        if (gameId[0] is '/' or '\\') return true;
        if (gameId.Length > 1 && gameId[1] == ':' && char.IsAsciiLetter(gameId[0])) return true;
        foreach (var segment in gameId.Split('/', '\\'))
            if (segment.Length > 0 && segment.AsSpan().Trim(". ").IsEmpty) return true;
        return false;
    }

    private static string UserDataFolder()
    {
        if (OperatingSystem.IsWindows()) return Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        // Without DoNotVerify, .NET answers "" when the folder does not exist yet (a new account, a container, CI),
        // and the saves would land in the working directory.
        if (OperatingSystem.IsMacOS()) return Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData, Environment.SpecialFolderOption.DoNotVerify);
        // A phone app's home is its own container: iOS keeps app data in Library/Application Support, Android in the
        // app's files folder (its home). The same folders the phone hosts used before they stopped setting XDG_DATA_HOME.
        string home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile, Environment.SpecialFolderOption.DoNotVerify);
        if ((OperatingSystem.IsIOS() || OperatingSystem.IsTvOS()) && home.Length > 0) return Path.Combine(home, "Library", "Application Support");
        if (OperatingSystem.IsAndroid() && home.Length > 0) return home;
        return XdgDataHome(Environment.GetEnvironmentVariable("XDG_DATA_HOME"), home)
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

    /// <summary>Creates a store over any storage of the platform contract (<see cref="Platform.IStorage"/>).</summary>
    /// <exception cref="ArgumentNullException"><paramref name="storage"/> is null.</exception>
    public SaveStore(Platform.IStorage storage) => _storage = storage ?? throw new ArgumentNullException(nameof(storage));

    /// <summary>Reads and deserializes a slot, finding the properties of <typeparamref name="T"/> by reflection.</summary>
    /// <typeparam name="T">The saved type.</typeparam>
    /// <param name="slot">The slot name.</param>
    /// <returns>
    /// The saved value, or null when the slot does not exist, holds invalid JSON (the data is set aside) or could not be read
    /// (the slot is then protected from <see cref="Save{T}(string, T)"/> for the life of this store).
    /// </returns>
    /// <exception cref="InvalidOperationException">The application was published trimmed or with NativeAOT; use <see cref="Load{T}(string, JsonTypeInfo{T})"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="slot"/> is not a valid slot name (see the class remarks).</exception>
    [RequiresUnreferencedCode(ReflectionMessage)]
    [RequiresDynamicCode(ReflectionMessage)]
    public T? Load<T>(string slot) where T : class => Read(SlotName.Checked(slot), static text => JsonSerializer.Deserialize<T>(text, Options));

    /// <summary>
    /// Reads and deserializes a slot with the game's own metadata for <typeparamref name="T"/>, without reflection: the
    /// way to load in a game published trimmed or with NativeAOT. It reads the slots <see cref="Save{T}(string, T)"/> wrote.
    /// </summary>
    /// <typeparam name="T">The saved type.</typeparam>
    /// <param name="slot">The slot name.</param>
    /// <param name="typeInfo">The metadata of <typeparamref name="T"/>, from the game's <see cref="System.Text.Json.Serialization.JsonSerializerContext"/> (<c>MySaves.Default.Progress</c>).</param>
    /// <returns>
    /// The saved value, or null when the slot does not exist, holds invalid JSON (the data is set aside) or could not be read
    /// (the slot is then protected from saving for the life of this store).
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="typeInfo"/> is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="slot"/> is not a valid slot name (see the class remarks).</exception>
    public T? Load<T>(string slot, JsonTypeInfo<T> typeInfo) where T : class
    {
        SlotName.Check(slot);
        ArgumentNullException.ThrowIfNull(typeInfo);
        return Read(slot, text => JsonSerializer.Deserialize(text, typeInfo));
    }

    private T? Read<T>(string slot, Func<string, T?> deserialize) where T : class
    {
        try
        {
            if (_storage.Read(slot) is not { } text) return null;
            var value = deserialize(text);
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
    /// Writes the whole slot at once, so a crash never leaves a half-written save, finding the properties of
    /// <typeparamref name="T"/> by reflection. Returns false when the medium refused the write, or when the slot could
    /// not be read earlier (see the class remarks).
    /// </summary>
    /// <exception cref="InvalidOperationException">The application was published trimmed or with NativeAOT; use <see cref="Save{T}(string, T, JsonTypeInfo{T})"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="slot"/> is not a valid slot name (see the class remarks).</exception>
    [RequiresUnreferencedCode(ReflectionMessage)]
    [RequiresDynamicCode(ReflectionMessage)]
    public bool Save<T>(string slot, T data)
    {
        SlotName.Check(slot);
        if (_unreadable.Contains(slot)) return false;
        return Write(slot, JsonSerializer.Serialize(data, Options));
    }

    /// <summary>
    /// Writes the whole slot at once with the game's own metadata for <typeparamref name="T"/>, without reflection: the
    /// way to save in a game published trimmed or with NativeAOT. The text is the same indented JSON
    /// <see cref="Save{T}(string, T)"/> writes, so either call reads the other's slots. Returns false when the medium
    /// refused the write, or when the slot could not be read earlier (see the class remarks).
    /// </summary>
    /// <param name="slot">The slot name.</param>
    /// <param name="data">The value to save.</param>
    /// <param name="typeInfo">The metadata of <typeparamref name="T"/>, from the game's <see cref="System.Text.Json.Serialization.JsonSerializerContext"/> (<c>MySaves.Default.Progress</c>).</param>
    /// <exception cref="ArgumentNullException"><paramref name="typeInfo"/> is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="slot"/> is not a valid slot name (see the class remarks).</exception>
    public bool Save<T>(string slot, T data, JsonTypeInfo<T> typeInfo)
    {
        SlotName.Check(slot);
        ArgumentNullException.ThrowIfNull(typeInfo);
        if (_unreadable.Contains(slot)) return false;
        var buffer = new ArrayBufferWriter<byte>();
        using (var writer = new Utf8JsonWriter(buffer, WriterOptions)) JsonSerializer.Serialize(writer, data, typeInfo);
        return Write(slot, Encoding.UTF8.GetString(buffer.WrittenSpan));
    }

    private bool Write(string slot, string text)
    {
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
    /// <exception cref="ArgumentException"><paramref name="slot"/> is not a valid slot name (see the class remarks).</exception>
    public bool Exists(string slot) => _storage.Exists(SlotName.Checked(slot));
    /// <summary>
    /// Removes the slot's data; does nothing when it does not exist. A slot that could not be read can be saved again
    /// after it is deleted: deleting is the game's explicit choice to start over.
    /// </summary>
    /// <exception cref="ArgumentException"><paramref name="slot"/> is not a valid slot name (see the class remarks).</exception>
    public void Delete(string slot)
    {
        SlotName.Check(slot);
        _storage.Delete(slot);
        _unreadable.Remove(slot);
    }
}

/// <summary>
/// Where a <see cref="SaveStore"/> keeps its slots: the platform's storage contract
/// (<see cref="Platform.IStorage"/>: named texts that are written whole, failures as <see cref="IOException"/> or
/// <see cref="UnauthorizedAccessException"/>) under the name saves have always used.
/// </summary>
public interface ISaveStorage : Platform.IStorage;

/// <summary>
/// The rule for slot names, shared by <see cref="SaveStore"/> and <see cref="FileSaveStorage"/>: a slot is a plain name
/// that every supported system can store as a file name, so it never leaves the save folder and a save can be copied
/// from one system to another.
/// </summary>
internal static class SlotName
{
    /// <summary>The device names Windows reserves, with or without an extension.</summary>
    private static readonly HashSet<string> Devices = new(StringComparer.OrdinalIgnoreCase)
    {
        "CON", "PRN", "AUX", "NUL", "CONIN$", "CONOUT$",
        "COM0", "COM1", "COM2", "COM3", "COM4", "COM5", "COM6", "COM7", "COM8", "COM9", "COM¹", "COM²", "COM³",
        "LPT0", "LPT1", "LPT2", "LPT3", "LPT4", "LPT5", "LPT6", "LPT7", "LPT8", "LPT9", "LPT¹", "LPT²", "LPT³",
    };

    /// <summary>What a slot's unreadable data is set aside under, after the slot's name; no slot may end with it.</summary>
    public const string SetAsideSuffix = ".corrupt";

    /// <summary>Returns <paramref name="slot"/> when it is a valid slot name; throws otherwise.</summary>
    public static string Checked(string slot)
    {
        Check(slot);
        return slot;
    }

    /// <exception cref="ArgumentNullException"><paramref name="slot"/> is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="slot"/> is not a plain, portable file name.</exception>
    public static void Check(string slot)
    {
        ArgumentNullException.ThrowIfNull(slot, nameof(slot));
        if (Problem(slot) is { } problem)
            throw new ArgumentException($"\"{slot}\" is not a valid slot name: {problem}. A slot is a plain name such as \"progress\" or \"profile.v2\", not a path.", nameof(slot));
    }

    private static string? Problem(string slot)
    {
        if (string.IsNullOrWhiteSpace(slot)) return "it is empty";
        for (int i = 0; i < slot.Length; i++)
        {
            char c = slot[i];
            if (c is '/' or '\\') return "it contains a path separator";
            if (c < ' ' || c is ':' or '*' or '?' or '"' or '<' or '>' or '|') return "it contains a character that file names cannot hold";
            if (char.IsHighSurrogate(c) && i + 1 < slot.Length && char.IsLowSurrogate(slot[i + 1])) i++;
            else if (char.IsSurrogate(c)) return "it contains half of a character";
        }
        if (slot[^1] is '.' or ' ') return "it ends with a dot or a space";
        // "<slot>.corrupt" is where unreadable data of <slot> is set aside: as a slot it would be replaced by that copy.
        if (slot.EndsWith(SetAsideSuffix, StringComparison.OrdinalIgnoreCase)) return "it ends with \".corrupt\", the name unreadable data is set aside under";
        int dot = slot.IndexOf('.');
        if (Devices.Contains((dot < 0 ? slot : slot[..dot]).TrimEnd(' '))) return "it is a device name on Windows";
        return null;
    }
}

/// <summary>
/// One JSON file per slot in a directory (desktop and tests). A slot is a plain name (see <see cref="SaveStore"/>):
/// every member that takes one throws <see cref="ArgumentException"/> for a name that is not, so no slot reaches a file
/// outside the directory.
/// </summary>
public sealed class FileSaveStorage(string directory) : ISaveStorage
{
    /// <summary>The directory that holds the slot files.</summary>
    public string Location { get; } = directory ?? throw new ArgumentNullException(nameof(directory));

    private string PathFor(string slot) => Path.Combine(Location, SlotName.Checked(slot) + ".json");

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
        try { File.Move(path, Path.ChangeExtension(path, SlotName.SetAsideSuffix + ".json"), overwrite: true); }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException) { /* Stays in place; callers that check Exists will not overwrite it. */ }
    }
}
