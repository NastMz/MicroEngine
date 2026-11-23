using MicroEngine.Core;
using MicroEngine.Input;

namespace MicroEngine.Tests.Input;

/// <summary>
/// What saved bindings may do to a map. Loading never leaves an action without a control because the file was
/// written in a format or with controls this version does not know, and a rebinding screen saves and loads the
/// actions it offers, so the menus' and the engine's actions always follow the game's code. An action that was
/// unbound on purpose is saved and loaded as unbound.
/// </summary>
[TestClass]
public sealed class BindingsPersistenceTests
{
    private static readonly string[] Rebindable = ["jump", "fire"];

    private static InputMap Map() => InputMap.CreateDefault()
        .Bind("jump", [Key.Z, Key.Space], [PadButton.A])
        .Bind("fire", [Key.X], [PadButton.X]);

    private static string[] Controls(InputMap map, string action) => [.. map.GetBindings(action)!.Controls.Select(c => c.ToString())];

    /// <summary>What a later release might write: a format number this one does not know, and other property names.</summary>
    private const string LaterFormat = """
        {
          "version": 3,
          "actions": [
            { "action": "confirm", "inputs": [ { "device": "keyboard", "code": "Enter" } ] },
            { "action": "jump", "inputs": [ { "device": "keyboard", "code": "Space" } ] }
          ]
        }
        """;

    [TestMethod]
    public void A_file_of_a_later_format_is_not_applied_and_is_set_aside()
    {
        var error = Assert.ThrowsExactly<FormatException>(() => InputBindingSet.FromJson(LaterFormat));
        StringAssert.Contains(error.Message, "version 3");
        Assert.ThrowsExactly<FormatException>(() => InputBindingSet.FromJson("""{ "version": 0, "actions": [] }"""));

        var storage = new MemorySaveStorage();
        storage.Write(InputMap.BindingsSlot, LaterFormat);
        var map = Map();
        var confirm = Controls(map, "confirm");

        Assert.IsFalse(map.LoadBindings(storage), "nothing was loaded");
        CollectionAssert.AreEqual(confirm, Controls(map, "confirm"), "the action keeps its controls");
        CollectionAssert.AreEqual(new[] { "Key:Z", "Key:Space", "Pad:A" }, Controls(map, "jump"));
        Assert.AreEqual(LaterFormat, storage.ReadSetAside(InputMap.BindingsSlot), "the file is kept aside, so the next save does not destroy it");

        // A set of a later format that reached the game another way (its own save) is not applied either.
        var set = new InputBindingSet { Version = 3, Actions = { new ActionBindingData { Action = "confirm" } } };
        Assert.AreEqual(0, map.ImportBindings(set));
        CollectionAssert.AreEqual(confirm, Controls(map, "confirm"));
    }

    [TestMethod]
    public void An_action_whose_saved_controls_are_all_unknown_keeps_the_controls_it_has()
    {
        var map = Map();
        var confirm = Controls(map, "confirm");
        var set = InputBindingSet.FromJson("""
            {
              "version": 2,
              "actions": [
                { "action": "confirm", "controls": [ "Pedal:Left", "Key:NoSuchKey" ] },
                { "action": "jump", "controls": [ "Pedal:Right", "Key:Q" ] }
              ]
            }
            """);

        Assert.AreEqual(1, map.ImportBindings(set), "only the action that got a control was replaced");
        CollectionAssert.AreEqual(confirm, Controls(map, "confirm"), "nothing it knows: the action is left as it was");
        CollectionAssert.AreEqual(new[] { "Key:Q" }, Controls(map, "jump"), "what it knows is applied, the rest ignored");
    }

    [TestMethod]
    public void An_action_saved_without_controls_is_loaded_unbound()
    {
        var storage = new MemorySaveStorage();
        var saved = Map().Unbind("fullscreen");
        saved.SaveBindings(storage);

        var map = Map();
        Assert.IsTrue(map.LoadBindings(storage));
        Assert.AreEqual(0, map.GetBindings("fullscreen")!.Controls.Count, "unbound on purpose stays unbound");
        Assert.AreNotEqual(0, map.GetBindings("confirm")!.Controls.Count);
    }

    [TestMethod]
    public void A_rebinding_screen_saves_and_loads_only_the_actions_it_offers()
    {
        var storage = new MemorySaveStorage();
        var map = Map();
        map.Unbind("jump").Bind("jump", Key.Q);
        map.SaveBindings(storage, actions: Rebindable);

        var set = InputBindingSet.FromJson(storage.Read(InputMap.BindingsSlot)!);
        CollectionAssert.AreEqual(new[] { "fire", "jump" }, set.Actions.Select(a => a.Action).ToArray(), "the actions asked for, in ordinal order");
        CollectionAssert.AreEqual(new[] { "fire", "jump" }, map.ExportBindings(["jump", "fire", "no-such-action"]).Actions.Select(a => a.Action).ToArray());

        // A file that names more (saved before, or by a build that wrote every action) changes those only.
        storage.Write(InputMap.BindingsSlot, """
            {
              "version": 2,
              "actions": [
                { "action": "confirm", "controls": [] },
                { "action": "pause", "controls": [] },
                { "action": "fire", "controls": [ "Key:X", "Pad:X" ] },
                { "action": "jump", "controls": [ "Key:Q" ] }
              ]
            }
            """);
        var fresh = Map();
        var confirm = Controls(fresh, "confirm");
        Assert.IsTrue(fresh.LoadBindings(storage, actions: Rebindable));
        CollectionAssert.AreEqual(new[] { "Key:Q" }, Controls(fresh, "jump"));
        CollectionAssert.AreEqual(confirm, Controls(fresh, "confirm"), "an action the screen does not offer is not touched");
        Assert.AreNotEqual(0, fresh.GetBindings("pause")!.Controls.Count);

        Assert.AreEqual(1, Map().ImportBindings(set, ["jump"]), "the same choice for a set the game keeps itself");
    }
}
