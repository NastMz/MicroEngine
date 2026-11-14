using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using MicroEngine.Core;

namespace Examples;

#region types
/// <summary>
/// The game's saved types, declared once. The compiler writes how to read and write each of them and the types they
/// hold, so saving needs no reflection: what a game published trimmed or with NativeAOT requires.
/// </summary>
[JsonSerializable(typeof(SaveExample.Progress))]
[JsonSerializable(typeof(Profile))]
[JsonSerializable(typeof(JsonObject))]   // a slot read as JSON, to upgrade old saves
public partial class SaveTypes : JsonSerializerContext;
#endregion

/// <summary>The save example's slot, saved and loaded with the metadata in <see cref="SaveTypes"/>.</summary>
public static class TypedSaves
{
    #region use
    public static SaveExample.Progress Load(SaveStore store) =>
        store.Load("example", SaveTypes.Default.Progress) ?? new SaveExample.Progress();   // null: no save yet, or unreadable

    public static bool Save(SaveStore store, SaveExample.Progress progress) =>
        store.Save("example", progress, SaveTypes.Default.Progress);                        // false: the medium refused
    #endregion
}
