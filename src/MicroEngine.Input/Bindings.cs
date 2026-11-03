using System.Text;
using System.Text.Json;
using MicroEngine.Platform;

namespace MicroEngine.Input;

/// <summary>
/// The action names the engine's own parts read: menus and dialogs (<c>SelectionList</c>, <c>DialogueBox</c>), the
/// achievements screen, the default on-screen controls and prompts. They are defaults the game may rename: build the
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
/// The bindings of one action as data, by name (keys, buttons and axes are their enum names), so saved bindings stay
/// valid when the engine adds keys. Part of <see cref="InputBindingSet"/>.
/// </summary>
public sealed class ActionBindingData
{
    /// <summary>The action.</summary>
    public string Action { get; set; } = "";
    /// <summary>Keys by name ("A", "Space").</summary>
    public List<string> Keys { get; set; } = [];
    /// <summary>Gamepad buttons by name ("A", "RightShoulder").</summary>
    public List<string> Buttons { get; set; } = [];
    /// <summary>A left-stick direction ("Left"...), or "None".</summary>
    public string Stick { get; set; } = "None";
    /// <summary>Axes as "RightTrigger+" or "LeftX-".</summary>
    public List<string> Axes { get; set; } = [];
    /// <summary>Key combinations as "LeftAlt|RightAlt+Enter": the modifiers (any one of them), then the key.</summary>
    public List<string> Chords { get; set; } = [];
}

/// <summary>
/// Every action's bindings as data, for a rebinding screen to save and load: through <see cref="InputMap.SaveBindings"/>
/// and <see cref="InputMap.LoadBindings"/> (the platform's storage), or as a value a game keeps in its own save
/// (<c>SaveStore.Save("bindings", map.ExportBindings())</c>; the classes have public properties).
/// </summary>
public sealed class InputBindingSet
{
    /// <summary>The format version (1).</summary>
    public int Version { get; set; } = 1;
    /// <summary>The actions, in ordinal order of their names.</summary>
    public List<ActionBindingData> Actions { get; set; } = [];

    /// <summary>The set as JSON text.</summary>
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
                Strings(w, "keys", a.Keys);
                Strings(w, "buttons", a.Buttons);
                w.WriteString("stick", a.Stick);
                Strings(w, "axes", a.Axes);
                Strings(w, "chords", a.Chords);
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

    /// <summary>Reads a set written by <see cref="ToJson"/>.</summary>
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
            var b = _bindings[action];
            set.Actions.Add(new ActionBindingData
            {
                Action = action,
                Keys = [.. b.Keys.Select(k => k.ToString())],
                Buttons = [.. b.Buttons.Select(k => k.ToString())],
                Stick = b.Stick.ToString(),
                Axes = [.. b.Axes.Select(a => a.Axis + (a.Positive ? "+" : "-"))],
                Chords = [.. b.Chords.Select(c => string.Join('|', c.Modifiers) + "+" + c.Key)],
            });
        }
        return set;
    }

    /// <summary>
    /// Replaces the bindings of the actions this map has with those in <paramref name="set"/>. Actions the set does not
    /// mention keep theirs; actions the map does not have are ignored (a renamed or removed action does not come back),
    /// and so are names of keys, buttons or axes this version does not know.
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
            foreach (var k in data.Keys) if (Enum.TryParse<Key>(k, out var key) && Enum.IsDefined(key)) b.Keys.Add(key);
            foreach (var k in data.Buttons) if (Enum.TryParse<PadButton>(k, out var button) && Enum.IsDefined(button)) b.Buttons.Add(button);
            if (Enum.TryParse<StickDirection>(data.Stick, out var stick) && Enum.IsDefined(stick)) b.Stick = stick;
            foreach (var a in data.Axes)
                if (a.Length > 1 && a[^1] is '+' or '-' && Enum.TryParse<PadAxis>(a[..^1], out var axis) && Enum.IsDefined(axis))
                    b.Axes.Add((axis, a[^1] == '+'));
            foreach (var c in data.Chords)
            {
                int plus = c.LastIndexOf('+');
                if (plus <= 0 || !Enum.TryParse<Key>(c[(plus + 1)..], out var key) || !Enum.IsDefined(key)) continue;
                var modifiers = c[..plus].Split('|').Select(m => Enum.TryParse<Key>(m, out var k) && Enum.IsDefined(k) ? k : Key.None).Where(k => k != Key.None).ToArray();
                if (modifiers.Length > 0) b.Chords.Add(new KeyChord(key, modifiers));
            }
            _bindings[data.Action] = b;
            replaced++;
        }
        return replaced;
    }

    /// <summary>
    /// Binds the key or gamepad button of <paramref name="press"/> (a key-down or button-down event, such as the one
    /// <see cref="InputSystem.ListenForPress"/> hands over) to <paramref name="action"/>, adding to its bindings; with
    /// <paramref name="replace"/>, the action's keys (for a key) or buttons (for a button) are replaced.
    /// </summary>
    /// <returns>False when the event is neither a key-down nor a button-down.</returns>
    public bool BindFrom(string action, in InputEvent press, bool replace = false)
    {
        var b = Get(action);
        switch (press.Kind)
        {
            case InputEventKind.KeyDown:
                if (replace) b.Keys.Clear();
                if (!b.Keys.Contains(press.Key)) b.Keys.Add(press.Key);
                return true;
            case InputEventKind.ButtonDown:
                if (replace) b.Buttons.Clear();
                if (!b.Buttons.Contains(press.Button)) b.Buttons.Add(press.Button);
                return true;
            default:
                return false;
        }
    }

    /// <summary>Writes every action's bindings to <paramref name="storage"/> (the slot <paramref name="slot"/>), for a rebinding screen.</summary>
    /// <exception cref="IOException">The storage failed.</exception>
    public void SaveBindings(IStorage storage, string slot = BindingsSlot)
    {
        ArgumentNullException.ThrowIfNull(storage);
        storage.Write(slot, ExportBindings().ToJson());
    }

    /// <summary>
    /// Reads bindings saved with <see cref="SaveBindings"/> and applies them (<see cref="ImportBindings"/>). Returns false
    /// and keeps the current bindings when nothing was saved or the data is unreadable (which is set aside, so the next
    /// save does not destroy it).
    /// </summary>
    public bool LoadBindings(IStorage storage, string slot = BindingsSlot)
    {
        ArgumentNullException.ThrowIfNull(storage);
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
