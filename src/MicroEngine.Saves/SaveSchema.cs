using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization.Metadata;

namespace MicroEngine.Core;

/// <summary>What <see cref="SaveStore.Load{T}(string, SaveSchema{T})"/> found in a slot.</summary>
public enum SaveLoadStatus
{
    /// <summary>The slot holds nothing: start fresh.</summary>
    Missing,
    /// <summary>A save of the current version.</summary>
    Loaded,
    /// <summary>
    /// A save of an older version, brought up to the current one in memory. The slot still holds the older save; the
    /// next save replaces it, after the store has kept a copy of it.
    /// </summary>
    Migrated,
    /// <summary>
    /// A save written by a newer version of the game than this one. It is left as it is, and the store refuses to save
    /// over it.
    /// </summary>
    TooNew,
    /// <summary>
    /// An older save that this build failed to bring up to date: a step threw, or what the steps left does not fit the
    /// saved type (<see cref="SaveLoadResult{T}.Error"/> says which). It is left as it is, and the store refuses to
    /// save over it.
    /// </summary>
    MigrationFailed,
    /// <summary>Not a save: invalid JSON, not a JSON object, a version that is not a whole number from 1, or a save of
    /// the current version that does not fit the saved type. The store has set the data aside; a new save can be written.</summary>
    Corrupt,
    /// <summary>The slot exists but the medium could not read it. The store refuses to save over it.</summary>
    Unreadable,
}

/// <summary>
/// The outcome of loading a versioned save: the value when there is one, and otherwise why there is none.
/// </summary>
/// <typeparam name="T">The saved type.</typeparam>
public readonly struct SaveLoadResult<T> where T : class
{
    internal SaveLoadResult(SaveLoadStatus status, T? value = null, int version = 0, Exception? error = null)
    {
        Status = status;
        Value = value;
        Version = version;
        Error = error;
    }

    /// <summary>What the slot held.</summary>
    public SaveLoadStatus Status { get; }
    /// <summary>The save in its current shape, for <see cref="SaveLoadStatus.Loaded"/> and <see cref="SaveLoadStatus.Migrated"/>; null otherwise.</summary>
    public T? Value { get; }
    /// <summary>
    /// The version the save was written in: the older version for <see cref="SaveLoadStatus.Migrated"/> and
    /// <see cref="SaveLoadStatus.MigrationFailed"/>, the newer one for <see cref="SaveLoadStatus.TooNew"/>, the current
    /// one for <see cref="SaveLoadStatus.Loaded"/>. 0 when no version was read (missing, corrupt, unreadable).
    /// </summary>
    public int Version { get; }
    /// <summary>What went wrong, for <see cref="SaveLoadStatus.Corrupt"/> and <see cref="SaveLoadStatus.MigrationFailed"/> (to log); null otherwise.</summary>
    public Exception? Error { get; }
    /// <summary>
    /// True when the slot holds a save this build could not use and must not replace (<see cref="SaveLoadStatus.TooNew"/>,
    /// <see cref="SaveLoadStatus.MigrationFailed"/>, <see cref="SaveLoadStatus.Unreadable"/>): the store returns false
    /// from every save to that slot until the game deletes it.
    /// </summary>
    public bool Protected => Status is SaveLoadStatus.TooNew or SaveLoadStatus.MigrationFailed or SaveLoadStatus.Unreadable;
}

/// <summary>What every <see cref="SaveSchema{T}"/> shares.</summary>
public static class SaveSchema
{
    /// <summary>
    /// The name of the property that holds a save's version, the first one of the saved JSON object: <c>"$version"</c>.
    /// The store writes and removes it; the saved type never sees it and must not have a property serialized under
    /// this name.
    /// </summary>
    public const string VersionProperty = "$version";

    /// <summary>The version of a save that has no version property: the saves written before the type had a schema.</summary>
    public const int FirstVersion = 1;

    /// <summary>The version a stored text declares, or null when it is not a JSON object with a whole version from 1 (which includes having none).</summary>
    internal static int? DeclaredVersion(string text)
    {
        try
        {
            return JsonNode.Parse(text) is JsonObject save && save[VersionProperty] is JsonValue value
                && value.TryGetValue(out int version) && version >= FirstVersion ? version : null;
        }
        catch (Exception e) when (e is JsonException or ArgumentException)
        {
            return null;
        }
    }
}

/// <summary>
/// How a saved type changes between releases of a game: the version this build writes and one step for each earlier
/// version, which turns a save of that version into the next. <see cref="SaveStore.Load{T}(string, SaveSchema{T})"/>
/// runs the steps a save needs, in order, and <see cref="SaveStore.Save{T}(string, T, SaveSchema{T})"/> stamps the
/// current version.
/// </summary>
/// <remarks>
/// <para>
/// Versions are whole numbers from 1. A save with no version (written before the type had a schema, or by
/// <see cref="SaveStore.Save{T}(string, T)"/>) is version 1, so a game adopts a schema at version 1 with no steps and
/// its players' saves keep loading. A release that renames, removes or reinterprets a saved property raises the
/// version by one and adds the step from the previous one; adding a property needs neither.
/// </para>
/// <para>
/// A step receives the save as a <see cref="JsonObject"/>, without its version property, and changes it in place. It
/// is an ordinary method: test it by itself with a JSON literal. Never edit a step that has shipped; saves of that
/// version are on players' machines. A step that throws, for any reason, leaves the slot as it is
/// (<see cref="SaveLoadStatus.MigrationFailed"/>).
/// </para>
/// <para>
/// The saved value must serialize to a JSON object. With <see cref="SaveSchema{T}(int, JsonTypeInfo{T})"/> nothing
/// here uses reflection: steps work on JSON nodes and the type is read and written with the game's metadata, so it
/// works in games published trimmed or with NativeAOT. A schema has no state besides its declaration: build it once
/// and keep it in a static field.
/// </para>
/// </remarks>
/// <typeparam name="T">The saved type.</typeparam>
/// <example>
/// <code>
/// public static readonly SaveSchema&lt;Profile&gt; Schema = new SaveSchema&lt;Profile&gt;(3, MySaves.Default.Profile)
///     .Migrate(1, save =&gt; save["Lives"] = 3)                                             // 1 -&gt; 2: lives arrived
///     .Migrate(2, save =&gt; { save["Gold"] = save["Coins"]?.DeepClone(); save.Remove("Coins"); }); // 2 -&gt; 3: a rename
///
/// var loaded = Saves.Load("profile", Schema);
/// var profile = loaded.Value ?? new Profile();
/// Saves.Save("profile", profile, Schema);
/// </code>
/// </example>
public sealed class SaveSchema<T> where T : class
{
    private const string ReflectionMessage = "Serializes the saved type with reflection, which trimmed and NativeAOT applications do not have. Declare the type in a JsonSerializerContext and pass its JsonTypeInfo<T> to the constructor that takes one.";
    private readonly Func<JsonObject, T?> _read;
    private readonly Func<T, JsonNode?> _write;
    private readonly Action<JsonObject>?[] _steps; // _steps[v] turns version v into v + 1; index 0 is unused
    private bool _complete;

    /// <summary>A schema that reads and writes <typeparamref name="T"/> with the game's own metadata, without reflection.</summary>
    /// <param name="version">The version this build writes: 1 for a type that has never changed.</param>
    /// <param name="typeInfo">The metadata of <typeparamref name="T"/>, from the game's <see cref="System.Text.Json.Serialization.JsonSerializerContext"/> (<c>MySaves.Default.Profile</c>).</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="version"/> is below 1.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="typeInfo"/> is null.</exception>
    public SaveSchema(int version, JsonTypeInfo<T> typeInfo) : this(version, Reader(typeInfo), Writer(typeInfo))
    {
    }

    /// <summary>
    /// A schema that finds the properties of <typeparamref name="T"/> by reflection, like
    /// <see cref="SaveStore.Save{T}(string, T)"/>: for games that are not published trimmed or with NativeAOT.
    /// </summary>
    /// <param name="version">The version this build writes: 1 for a type that has never changed.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="version"/> is below 1.</exception>
    [RequiresUnreferencedCode(ReflectionMessage)]
    [RequiresDynamicCode(ReflectionMessage)]
    public SaveSchema(int version) : this(version, static save => save.Deserialize<T>(SaveStore.Options), static value => JsonSerializer.SerializeToNode(value, SaveStore.Options))
    {
    }

    private SaveSchema(int version, Func<JsonObject, T?> read, Func<T, JsonNode?> write)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(version, SaveSchema.FirstVersion);
        Version = version;
        _steps = new Action<JsonObject>?[version];
        _read = read;
        _write = write;
    }

    private static Func<JsonObject, T?> Reader(JsonTypeInfo<T> typeInfo)
    {
        ArgumentNullException.ThrowIfNull(typeInfo);
        return save => save.Deserialize(typeInfo);
    }

    private static Func<T, JsonNode?> Writer(JsonTypeInfo<T> typeInfo) => value => JsonSerializer.SerializeToNode(value, typeInfo);

    /// <summary>The version this build writes.</summary>
    public int Version { get; }

    /// <summary>
    /// Declares the step that turns a save of version <paramref name="from"/> into version <paramref name="from"/> + 1.
    /// Every version from 1 to <see cref="Version"/> - 1 needs one; a schema with a step missing throws
    /// <see cref="InvalidOperationException"/> when it is used.
    /// </summary>
    /// <param name="from">The version the step receives.</param>
    /// <param name="step">Changes the save in place. The version property is not in it.</param>
    /// <returns>This schema, for chaining.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="from"/> is below 1, or is not below <see cref="Version"/>.</exception>
    /// <exception cref="ArgumentException">The version already has a step.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="step"/> is null.</exception>
    public SaveSchema<T> Migrate(int from, Action<JsonObject> step)
    {
        ArgumentNullException.ThrowIfNull(step);
        ArgumentOutOfRangeException.ThrowIfLessThan(from, SaveSchema.FirstVersion);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(from, Version);
        if (_steps[from] is not null) throw new ArgumentException($"Version {from} already has a step: a schema has one step per version.", nameof(from));
        _steps[from] = step;
        return this;
    }

    /// <summary>
    /// Reads a save from its text: checks its version, runs the steps it needs and builds the value. It touches no
    /// storage, so it is also how a test loads the text an earlier release wrote. It never returns
    /// <see cref="SaveLoadStatus.Missing"/> or <see cref="SaveLoadStatus.Unreadable"/>, which are the storage's.
    /// </summary>
    /// <exception cref="ArgumentNullException"><paramref name="text"/> is null.</exception>
    /// <exception cref="InvalidOperationException">A version below <see cref="Version"/> has no step.</exception>
    public SaveLoadResult<T> Read(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        CheckComplete();
        JsonObject save;
        int version = SaveSchema.FirstVersion;
        try
        {
            if (JsonNode.Parse(text) is not JsonObject parsed)
                return new(SaveLoadStatus.Corrupt, error: new JsonException("A save is a JSON object."));
            save = parsed;
            if (save[SaveSchema.VersionProperty] is { } declared)
            {
                if (declared is not JsonValue value || !value.TryGetValue(out version) || version < SaveSchema.FirstVersion)
                    return new(SaveLoadStatus.Corrupt, error: new JsonException($"\"{SaveSchema.VersionProperty}\" is not a whole number from {SaveSchema.FirstVersion}."));
                save.Remove(SaveSchema.VersionProperty);
            }
        }
        // ArgumentException: a property name that appears twice, which a JSON object in memory cannot hold.
        catch (Exception e) when (e is JsonException or ArgumentException)
        {
            return new(SaveLoadStatus.Corrupt, error: e);
        }
        if (version > Version) return new(SaveLoadStatus.TooNew, version: version);

        bool older = version < Version;
        try
        {
            for (int v = version; v < Version; v++) _steps[v]!(save);
        }
        // Whatever a step throws, it throws on data that came from a player's machine: the save could not be brought up
        // to date, and the game goes on without it.
        catch (Exception e)
        {
            return new(SaveLoadStatus.MigrationFailed, version: version, error: e);
        }
        try
        {
            if (_read(save) is { } loaded) return new(older ? SaveLoadStatus.Migrated : SaveLoadStatus.Loaded, loaded, version);
            return new(older ? SaveLoadStatus.MigrationFailed : SaveLoadStatus.Corrupt, version: older ? version : 0, error: new JsonException("The save is null."));
        }
        catch (JsonException e)
        {
            return new(older ? SaveLoadStatus.MigrationFailed : SaveLoadStatus.Corrupt, version: older ? version : 0, error: e);
        }
    }

    /// <summary>
    /// The text of a save: the value as indented JSON, as <see cref="SaveStore.Save{T}(string, T)"/> writes it, with the
    /// current version as its first property.
    /// </summary>
    /// <exception cref="ArgumentNullException"><paramref name="value"/> is null.</exception>
    /// <exception cref="InvalidOperationException">
    /// A version below <see cref="Version"/> has no step; <typeparamref name="T"/> does not serialize to a JSON object;
    /// or it has a property serialized as <see cref="SaveSchema.VersionProperty"/>.
    /// </exception>
    public string Write(T value)
    {
        ArgumentNullException.ThrowIfNull(value);
        CheckComplete();
        if (_write(value) is not JsonObject save)
            throw new InvalidOperationException($"A versioned save is a JSON object, and {typeof(T).Name} does not serialize to one. Save a class whose properties hold the data.");
        if (save.ContainsKey(SaveSchema.VersionProperty))
            throw new InvalidOperationException($"{typeof(T).Name} has a property serialized as \"{SaveSchema.VersionProperty}\", the name the store keeps a save's version under. Rename the property.");
        save.Insert(0, SaveSchema.VersionProperty, Version);
        var buffer = new ArrayBufferWriter<byte>();
        using (var writer = new Utf8JsonWriter(buffer, SaveStore.WriterOptions)) save.WriteTo(writer);
        return Encoding.UTF8.GetString(buffer.WrittenSpan);
    }

    internal void CheckComplete()
    {
        if (_complete) return;
        for (int v = SaveSchema.FirstVersion; v < Version; v++)
            if (_steps[v] is null)
                throw new InvalidOperationException($"The schema of {typeof(T).Name} is at version {Version} and has no step from version {v}: declare it with Migrate({v}, ...). Every version below the current one needs its step, or saves of that version could not be loaded.");
        _complete = true;
    }
}
