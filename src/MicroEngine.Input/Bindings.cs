using System.Text;
using System.Text.Json;
using MicroEngine.Platform;

namespace MicroEngine.Input;

/// <summary>
/// The action names the engine's own parts read: the widget tree's focus and confirm (<c>UiRoot</c>), the default
/// on-screen controls and prompts. They are defaults the game may rename: build the
/// map with <see cref="InputMap.CreateDefault(ActionNames?)"/> or set <see cref="InputMap.Names"/>, and every engine
/// part follows.
/// </summary>
/// <example>
/// <code>
/// // The game calls its confirm action "accept"; engine menus follow.
/// protected override InputMap CreateInput() =&gt; InputMap.CreateDefault(ActionNames.Default with { Confirm = "accept" });
/// </code>
/// </example>
public sealed record ActionNames
{
    /// <summary>left, right, up, down, confirm, cancel, action and pause.</summary>
    public static ActionNames Default { get; } = new();
    /// <summary>Moves or selects left ("left").</summary>
    public string Left { get; init; } = "left";
    /// <summary>Moves or selects right ("right").</summary>
    public string Right { get; init; } = "right";
    /// <summary>Moves or selects up ("up").</summary>
    public string Up { get; init; } = "up";
    /// <summary>Moves or selects down ("down").</summary>
    public string Down { get; init; } = "down";
    /// <summary>Accepts a choice or advances a dialogue ("confirm").</summary>
    public string Confirm { get; init; } = "confirm";
    /// <summary>Goes back ("cancel").</summary>
    public string Cancel { get; init; } = "cancel";
    /// <summary>The game's secondary action ("action").</summary>
    public string Action { get; init; } = "action";
    /// <summary>Pauses ("pause").</summary>
    public string Pause { get; init; } = "pause";
    /// <summary>
    /// Switches between a window and full screen ("fullscreen"), handled by the engine itself where the platform can
    /// change its window; F11 and Alt+Enter by default (<see cref="InputMap.BindFullscreenDefaults"/>). Unbind it to free
    /// those keys, rebind it to move the toggle.
    /// </summary>
    public string Fullscreen { get; init; } = "fullscreen";
}

/// <summary>
/// The bindings of one action as data: its controls as text (<see cref="InputControl.ToString"/>), by enum name, so
/// saved bindings stay valid when the engine adds keys. Part of <see cref="InputBindingSet"/>.
/// </summary>
/// <remarks>
/// <see cref="Keys"/>, <see cref="Buttons"/>, <see cref="Stick"/>, <see cref="Axes"/> and <see cref="Chords"/> are the
/// first format (version 1), which had one list per kind of control and no place for the mouse. They are still read,
/// so files saved by earlier versions load; <see cref="InputMap.ExportBindings"/> fills <see cref="Controls"/> only.
/// </remarks>
public sealed class ActionBindingData
{
    /// <summary>The action.</summary>
    public string Action { get; set; } = "";
    /// <summary>
    /// Every control bound to the action, as text: "Key:Space", "Pad:A", "Mouse:Secondary", "Wheel:Up",
    /// "Stick:Right.Up", "Axis:RightTrigger+", "Chord:LeftAlt|RightAlt+Enter", "Motion:Left".
    /// </summary>
    public List<string> Controls { get; set; } = [];
    /// <summary>Version 1: keys by name ("A", "Space").</summary>
    public List<string> Keys { get; set; } = [];
    /// <summary>Version 1: gamepad buttons by name ("A", "RightShoulder").</summary>
    public List<string> Buttons { get; set; } = [];
    /// <summary>Version 1: a left-stick direction ("Left"...), or "None".</summary>
    public string Stick { get; set; } = "None";
    /// <summary>Version 1: axes as "RightTrigger+" or "LeftX-".</summary>
    public List<string> Axes { get; set; } = [];
    /// <summary>Version 1: key combinations as "LeftAlt|RightAlt+Enter": the modifiers (any one of them), then the key.</summary>
    public List<string> Chords { get; set; } = [];
}

/// <summary>
/// Every action's bindings as data, for a rebinding screen to save and load: through <see cref="InputMap.SaveBindings"/>
/// and <see cref="InputMap.LoadBindings"/> (the platform's storage), or as a value a game keeps in its own save
/// (<c>SaveStore.Save("bindings", map.ExportBindings())</c>; the classes have public properties).
/// </summary>
public sealed class InputBindingSet
{
    /// <summary>The current format version: each action has one list of controls.</summary>
    public const int CurrentVersion = 2;

    /// <summary>
    /// The format version: 2 (<see cref="CurrentVersion"/>) for a set this version wrote; 1 for a file of the first
    /// format, which is read the same way.
    /// </summary>
    public int Version { get; set; } = CurrentVersion;
    /// <summary>The actions, in ordinal order of their names.</summary>
    public List<ActionBindingData> Actions { get; set; } = [];

    /// <summary>
    /// The set as JSON text. Written by hand with a JSON writer, never by reflection, so it works in trimmed and
    /// NativeAOT builds.
    /// </summary>
    public string ToJson()
    {
        using var stream = new MemoryStream();
        using (var w = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = true }))
        {
            w.WriteStartObject();
            w.WriteNumber("version", Version);
            w.WriteStartArray("actions");
            foreach (var a in Actions)
            {
                w.WriteStartObject();
                w.WriteString("action", a.Action);
                Strings(w, "controls", a.Controls);
                // The first format's lists are written only for a set that still carries them.
                if (a.Keys.Count > 0) Strings(w, "keys", a.Keys);
                if (a.Buttons.Count > 0) Strings(w, "buttons", a.Buttons);
                if (a.Stick is { Length: > 0 } and not "None") w.WriteString("stick", a.Stick);
                if (a.Axes.Count > 0) Strings(w, "axes", a.Axes);
                if (a.Chords.Count > 0) Strings(w, "chords", a.Chords);
                w.WriteEndObject();
            }
            w.WriteEndArray();
            w.WriteEndObject();
        }
        return Encoding.UTF8.GetString(stream.ToArray());

        static void Strings(Utf8JsonWriter w, string name, List<string> values)
        {
            w.WriteStartArray(name);
            foreach (var v in values) w.WriteStringValue(v);
            w.WriteEndArray();
        }
    }

    /// <summary>Reads a set written by <see cref="ToJson"/>, of this format or of the first one.</summary>
    /// <exception cref="FormatException">The text is not a binding set.</exception>
    public static InputBindingSet FromJson(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            var set = new InputBindingSet { Version = root.GetProperty("version").GetInt32() };
            foreach (var a in root.GetProperty("actions").EnumerateArray())
                set.Actions.Add(new ActionBindingData
                {
                    Action = a.GetProperty("action").GetString() ?? "",
                    Controls = Strings(a, "controls"),
                    Keys = Strings(a, "keys"),
                    Buttons = Strings(a, "buttons"),
                    Stick = a.TryGetProperty("stick", out var stick) ? stick.GetString() ?? "None" : "None",
                    Axes = Strings(a, "axes"),
                    Chords = Strings(a, "chords"),
                });
            return set;
        }
        catch (Exception e) when (e is JsonException or KeyNotFoundException or InvalidOperationException)
        {
            throw new FormatException("The text is not a set of input bindings: " + e.Message, e);
        }

        static List<string> Strings(JsonElement e, string name) =>
            e.TryGetProperty(name, out var array) ? [.. array.EnumerateArray().Select(v => v.GetString() ?? "")] : [];
    }
}

public sealed partial class InputMap
{
    /// <summary>The slot <see cref="SaveBindings"/> and <see cref="LoadBindings"/> use unless told otherwise.</summary>
    public const string BindingsSlot = "input-bindings";

    /// <summary>Every action's bindings as data (see <see cref="InputBindingSet"/>), in ordinal order of the names.</summary>
    public InputBindingSet ExportBindings()
    {
        var set = new InputBindingSet();
        foreach (var action in _bindings.Keys.Order(StringComparer.Ordinal))
        {
            var data = new ActionBindingData { Action = action };
            foreach (var c in _bindings[action].Controls) data.Controls.Add(c.ToString());
            set.Actions.Add(data);
        }
        return set;
    }

    /// <summary>
    /// Replaces the bindings of the actions this map has with those in <paramref name="set"/>. Actions the set does not
    /// mention keep theirs; actions the map does not have are ignored (a renamed or removed action does not come back),
    /// and so are controls this version does not know. A set of the first format (one list per kind of control) is read
    /// the same way.
    /// </summary>
    /// <returns>The number of actions whose bindings were replaced.</returns>
    public int ImportBindings(InputBindingSet set)
    {
        ArgumentNullException.ThrowIfNull(set);
        int replaced = 0;
        foreach (var data in set.Actions)
        {
            if (!_bindings.ContainsKey(data.Action)) continue;
            var b = new Binding();
            foreach (var k in data.Keys) if (InputControl.TryParse("Key:" + k, out var key)) b.Add(key);
            foreach (var k in data.Buttons) if (InputControl.TryParse("Pad:" + k, out var button)) b.Add(button);
            if (InputControl.TryParse("Stick:Left." + data.Stick, out var stick)) b.Add(stick);
            foreach (var a in data.Axes) if (InputControl.TryParse("Axis:" + a, out var axis)) b.Add(axis);
            foreach (var c in data.Chords) if (InputControl.TryParse("Chord:" + c, out var chord)) b.Add(chord);
            foreach (var c in data.Controls) if (InputControl.TryParse(c, out var control)) b.Add(control);
            _bindings[data.Action] = b;
            replaced++;
        }
        _usesChanged = true;
        return replaced;
    }

    /// <summary>
    /// Binds the control of <paramref name="press"/> (a key-down, a gamepad button-down, a mouse button-down or a wheel
    /// turn, such as the one <see cref="InputSystem.ListenForPress"/> hands over) to <paramref name="action"/>, adding
    /// to its bindings. With <paramref name="replace"/>, the new control takes the place of the action's controls of
    /// the same sort: its keys for a key, its gamepad buttons for a button, its mouse buttons and wheel directions for
    /// a mouse button or the wheel.
    /// </summary>
    /// <returns>False when the event is none of those presses (<see cref="InputControl.FromPress"/>).</returns>
    public bool BindFrom(string action, in InputEvent press, bool replace = false)
    {
        if (InputControl.FromPress(press) is not { } control) return false;
        var b = Get(action);
        if (replace) b.Controls.RemoveAll(c => Sort(c) == Sort(control));
        b.Add(control);
        _usesChanged = true;
        return true;

        // What "the same sort" is for a replacement.
        static int Sort(InputControl c) => c.Kind switch
        {
            InputControlKind.PointerButton or InputControlKind.Wheel => 100,
            _ => (int)c.Kind,
        };
    }

    /// <summary>Writes every action's bindings to <paramref name="storage"/> (the slot <paramref name="slot"/>), for a rebinding screen.</summary>
    /// <remarks>
    /// The slot is handed to the storage as given, and the storage decides which names it can hold. The file storage
    /// takes plain names only (<c>"input-bindings"</c>, <c>"bindings.player2"</c>), and a plain name is the same file
    /// or key on every system.
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="storage"/> or <paramref name="slot"/> is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="slot"/> is empty or white space, or is a name the storage cannot hold (for the file storage, anything that is not a plain name).</exception>
    /// <exception cref="IOException">The storage failed.</exception>
    /// <exception cref="UnauthorizedAccessException">The storage refused access.</exception>
    public void SaveBindings(IStorage storage, string slot = BindingsSlot)
    {
        ArgumentNullException.ThrowIfNull(storage);
        ArgumentException.ThrowIfNullOrWhiteSpace(slot);
        storage.Write(slot, ExportBindings().ToJson());
    }

    /// <summary>
    /// Reads bindings saved with <see cref="SaveBindings"/> and applies them (<see cref="ImportBindings"/>). Returns false
    /// and keeps the current bindings when nothing was saved or the data is unreadable (which is set aside, so the next
    /// save does not destroy it). A storage that cannot be read throws instead, and the current bindings stay.
    /// </summary>
    /// <exception cref="ArgumentNullException"><paramref name="storage"/> or <paramref name="slot"/> is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="slot"/> is empty or white space, or is a name the storage cannot hold (for the file storage, anything that is not a plain name).</exception>
    /// <exception cref="IOException">The storage failed.</exception>
    /// <exception cref="UnauthorizedAccessException">The storage refused access.</exception>
    public bool LoadBindings(IStorage storage, string slot = BindingsSlot)
    {
        ArgumentNullException.ThrowIfNull(storage);
        ArgumentException.ThrowIfNullOrWhiteSpace(slot);
        if (storage.Read(slot) is not { } text) return false;
        InputBindingSet set;
        try { set = InputBindingSet.FromJson(text); }
        catch (FormatException)
        {
            storage.SetAside(slot);
            return false;
        }
        ImportBindings(set);
        return true;
    }
}
