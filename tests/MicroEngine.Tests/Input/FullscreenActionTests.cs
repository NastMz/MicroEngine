using MicroEngine.Core;
using MicroEngine.Input;

namespace MicroEngine.Tests.Input;

/// <summary>
/// The full-screen toggle is an engine action like any other: bound to F11 and Alt+Enter by default, handled by the
/// engine through the platform's display control, renamable, rebindable and removable; the keys themselves reach the
/// game as ordinary key events.
/// </summary>
[TestClass]
public sealed class FullscreenActionTests
{
    private static readonly DeviceId Keyboard = new(1);

    private sealed class Plain(Func<InputMap>? input = null) : Game
    {
        public override GameSettings Settings { get; } = new("fullscreen");
        public List<Key> KeysSeen { get; } = [];
        protected override InputMap CreateInput() => input?.Invoke() ?? base.CreateInput();
        protected override void Load() { }
        public override void Configure(MicroEngine.Kernel.EngineBuilder builder) =>
            builder.Schedule.Add(MicroEngine.Kernel.FramePhase.FixedUpdate, "test.keys", _ =>
            {
                foreach (var e in InputSystem.Events)
                    if (e.Kind == InputEventKind.KeyDown) KeysSeen.Add(e.Key);
            });
    }

    private static (HeadlessPlatform Platform, Plain Game, GameLoop Loop) Start(Func<InputMap>? input = null)
    {
        var platform = new HeadlessPlatform();
        var game = new Plain(input);
        var loop = new GameLoop(game, platform);
        Assert.IsTrue(loop.Start());
        loop.Tick();
        return (platform, game, loop);
    }

    private static void Tap(HeadlessPlatform platform, GameLoop loop, params Key[] keys)
    {
        foreach (var k in keys) platform.Devices.Send(InputEvent.KeyDown(Keyboard, k));
        loop.Tick();
        foreach (var k in keys.Reverse()) platform.Devices.Send(InputEvent.KeyUp(Keyboard, k));
        loop.Tick();
    }

    [TestMethod]
    public void The_default_map_binds_the_fullscreen_action_to_F11_and_Alt_Enter()
    {
        var map = InputMap.CreateDefault();
        var b = map.GetBindings(map.Names.Fullscreen)!;
        Assert.AreEqual("fullscreen", map.Names.Fullscreen);
        CollectionAssert.AreEqual(new[] { Key.F11 }, b.Keys.ToArray());
        Assert.AreEqual(1, b.Chords.Count);
        Assert.AreEqual(Key.Enter, b.Chords[0].Key);
        CollectionAssert.AreEquivalent(new[] { Key.LeftAlt, Key.RightAlt }, b.Chords[0].Modifiers.ToArray());
        Assert.AreEqual(0, b.Buttons.Count, "no gamepad button has a conventional full-screen meaning");
    }

    [TestMethod]
    public void F11_toggles_full_screen_through_the_engine_and_still_reaches_the_game_as_a_key()
    {
        var (platform, game, loop) = Start();
        Tap(platform, loop, Key.F11);
        Assert.AreEqual(1, platform.FullscreenToggles, "the engine asked the platform once");
        CollectionAssert.Contains(game.KeysSeen, Key.F11, "the key is an ordinary key event");
        loop.Finish();
    }

    [TestMethod]
    public void Alt_Enter_toggles_and_Enter_alone_does_not()
    {
        var (platform, _, loop) = Start();
        Tap(platform, loop, Key.Enter);
        Assert.AreEqual(0, platform.FullscreenToggles);
        Tap(platform, loop, Key.RightAlt, Key.Enter);
        Assert.AreEqual(1, platform.FullscreenToggles);
        loop.Finish();
    }

    [TestMethod]
    public void A_game_can_rebind_the_action()
    {
        var (platform, _, loop) = Start(() => InputMap.CreateDefault().Unbind("fullscreen").Bind("fullscreen", Key.F10));
        Tap(platform, loop, Key.F11);
        Assert.AreEqual(0, platform.FullscreenToggles, "F11 is free for the game");
        Tap(platform, loop, Key.F10);
        Assert.AreEqual(1, platform.FullscreenToggles);
        loop.Finish();
    }

    [TestMethod]
    public void An_unbound_action_does_nothing_and_F11_is_just_a_key()
    {
        var (platform, game, loop) = Start(() => InputMap.CreateDefault().Unbind("fullscreen"));
        Tap(platform, loop, Key.F11);
        Tap(platform, loop, Key.LeftAlt, Key.Enter);
        Assert.AreEqual(0, platform.FullscreenToggles);
        CollectionAssert.Contains(game.KeysSeen, Key.F11);
        loop.Finish();
    }

    [TestMethod]
    public void A_map_built_from_scratch_gets_the_default_toggle_and_renamed_names_are_followed()
    {
        var (platform, _, loop) = Start(() => new InputMap().Bind("jump", Key.Space));
        Tap(platform, loop, Key.F11);
        Assert.AreEqual(1, platform.FullscreenToggles, "a game that never mentions it keeps the usual toggle");
        loop.Finish();

        var (renamed, game, loop2) = Start(() => InputMap.CreateDefault(ActionNames.Default with { Fullscreen = "window" }));
        Assert.IsNull(game.Input.GetBindings("fullscreen"), "no second action under the old name");
        Tap(renamed, loop2, Key.F11);
        Assert.AreEqual(1, renamed.FullscreenToggles);
        loop2.Finish();
    }

    [TestMethod]
    public void Unbinding_an_action_the_map_does_not_have_yet_keeps_it_unbound()
    {
        var map = new InputMap().Unbind("fullscreen");
        Assert.IsNotNull(map.GetBindings("fullscreen"), "the action exists, with no binding, so the engine adds none");
        Assert.AreEqual(0, map.GetBindings("fullscreen")!.Keys.Count);
    }

    [TestMethod]
    public void Chords_need_their_modifier_and_survive_a_save_and_load()
    {
        var map = new InputMap().Bind("save", new KeyChord(Key.S, Key.LeftControl, Key.RightControl));
        var keys = new HeadlessInput();
        keys.Set(Key.S, true);
        map.Sample(keys);
        Assert.IsFalse(map.Down("save"), "S alone");
        keys.Set(Key.RightControl, true);
        map.EndUpdate();
        map.Sample(keys);
        Assert.IsTrue(map.Pressed("save"));
        Assert.AreEqual(InputDevice.Keyboard, map.LastDevice);

        var loaded = new InputMap().Bind("save", Key.F2);
        loaded.ImportBindings(InputBindingSet.FromJson(map.ExportBindings().ToJson()));
        var chord = loaded.GetBindings("save")!.Chords.Single();
        Assert.AreEqual(Key.S, chord.Key);
        CollectionAssert.AreEqual(new[] { Key.LeftControl, Key.RightControl }, chord.Modifiers.ToArray());
        Assert.AreEqual(0, loaded.GetBindings("save")!.Keys.Count);
        Assert.AreEqual(1, map.CopyBindings().GetBindings("save")!.Chords.Count);
    }

    [TestMethod]
    public void Prompts_name_a_chord_with_its_modifier()
    {
        var map = InputMap.CreateDefault();
        var prompts = new ActionPrompts(map) { Override = InputDevice.Keyboard };
        Assert.AreEqual("F11 / Alt+Enter", prompts.TextAll("fullscreen"));
    }
}
