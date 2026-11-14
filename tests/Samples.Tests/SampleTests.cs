using Examples;
using MicroEngine.Core;
using MicroEngine.Scenes;

[assembly: Parallelize(Scope = ExecutionScope.MethodLevel)]

namespace Samples.Tests;

/// <summary>
/// The focused examples, played headlessly through their public API: what the documentation says they do is what they
/// do. The tutorial's game has its own file, GemGardenTests.cs, which the testing guide quotes whole.
/// </summary>
[TestClass]
public sealed class ExampleTests
{
    [TestMethod]
    public void EveryExampleOpensRunsAndReturnsToTheList()
    {
        var saves = Directory.CreateTempSubdirectory("examples-").FullName;
        var game = new ExamplesGame(saves);
        game.InitializeForTest(new HeadlessPlatform());
        foreach (var (name, _) in ExamplesGame.All)
        {
            game.Input.SetForced("confirm", true);
            game.StepForTest();
            game.StepForTest();
            game.Input.SetForced("confirm", false);
            for (int i = 0; i < 90; i++) game.StepForTest();
            Assert.IsNotInstanceOfType<MenuScene>(game.Scenes.Current, name);
            game.Input.SetForced("pause", true);
            game.StepForTest();
            game.Input.SetForced("pause", false);
            game.StepForTest();
            Assert.IsInstanceOfType<MenuScene>(game.Scenes.Current, $"{name}: pause goes back to the list");
            game.Input.SetForced("down", true);
            game.StepForTest();
            game.Input.SetForced("down", false);
            game.StepForTest();
        }
        Directory.Delete(saves, true);
    }

    [TestMethod]
    public void TheSavingExampleWritesAndReadsItsSlot()
    {
        var saves = Directory.CreateTempSubdirectory("examples-").FullName;
        var game = new ExamplesGame(saves);
        game.InitializeForTest(new HeadlessPlatform());
        game.Scenes.Push(new SaveExample(saves));
        game.StepForTest();
        foreach (var action in new[] { "right", "right", "confirm" })
        {
            game.Input.SetForced(action, true);
            game.StepForTest();
            game.Input.SetForced(action, false);
            game.StepForTest();
        }
        var saved = new SaveStore("MicroEngineExamples", saves).Load<SaveExample.Progress>("example");
        Assert.IsNotNull(saved);
        Assert.AreEqual(2, saved.Coins);
        Assert.HasCount(1, saved.Visited);
        Directory.Delete(saves, true);
    }

    [TestMethod]
    public void TheDeclaredSaveTypesReadAndWriteTheSameSlot()
    {
        var saves = Directory.CreateTempSubdirectory("examples-").FullName;
        var store = new SaveStore("MicroEngineExamples", saves);
        Assert.AreEqual(0, TypedSaves.Load(store).Coins, "no save yet: a new progress");
        // A slot the example wrote without the declared types loads with them.
        Assert.IsTrue(store.Save("example", new SaveExample.Progress { Coins = 2, Visited = ["room 1"] }));
        string written = File.ReadAllText(Path.Combine(saves, "example.json"));
        var loaded = TypedSaves.Load(store);
        Assert.AreEqual(2, loaded.Coins);
        Assert.HasCount(1, loaded.Visited);
        // And saving with them writes the same file, which the example reads back.
        Assert.IsTrue(TypedSaves.Save(store, loaded));
        Assert.AreEqual(written, File.ReadAllText(Path.Combine(saves, "example.json")));
        Assert.AreEqual(2, store.Load<SaveExample.Progress>("example")!.Coins);
        // The other declared types: a versioned profile, and a slot read as JSON for upgrades.
        Assert.IsTrue(store.Save(ProfileSaves.Slot, new Profile { Version = ProfileSaves.CurrentVersion, Gold = 5 }, SaveTypes.Default.Profile));
        Assert.AreEqual(5, store.Load(ProfileSaves.Slot, SaveTypes.Default.JsonObject)!["Gold"]!.GetValue<int>());
        Assert.AreEqual(5, ProfileSaves.Load(store, out var check)!.Gold);
        Assert.AreEqual(SaveCheck.Current, check);
        Directory.Delete(saves, true);
    }
}

[TestClass]
public sealed class ExtendingTests
{
    [TestMethod]
    public void AGameSavesThroughACustomStorage()
    {
        var storage = new MemorySaveStorage();
        var store = new SaveStore(storage);
        Assert.IsTrue(store.Save("slot", new SaveExample.Progress { Coins = 3 }));
        Assert.AreEqual(3, store.Load<SaveExample.Progress>("slot")!.Coins);
        storage.Write("broken", "{ not json");
        Assert.IsNull(store.Load<SaveExample.Progress>("broken"), "unreadable data loads as null");
        Assert.IsTrue(storage.Exists("broken.corrupt"), "and is set aside, not lost");
    }

    [TestMethod]
    public void AGameWithControlsOfItsOwnHasNoDefaultActionAndItsMenusFollowItsNames()
    {
        var game = new OwnControlsGame();
        game.InitializeForTest(new HeadlessPlatform());
        CollectionAssert.AreEquivalent(new[] { "left", "right", "up", "down", "jump", "back", "pause", "fullscreen" }, game.Input.Actions.ToArray(),
            "only its own actions, and the engine's full-screen toggle");
        Assert.AreEqual("jump", game.Input.Names.Confirm);
        Assert.AreEqual("back", game.Input.Names.Cancel);

        var menu = new MenuProbe();
        game.Scenes.Push(menu);
        game.StepForTest();
        game.Input.SetForced("jump", true);
        game.StepForTest();
        Assert.AreEqual(0, menu.Chosen, "a menu confirms with the action named Confirm");
    }

    private sealed class MenuProbe : Scene
    {
        public int? Chosen { get; private set; }
        public override void Enter() => Widgets.Content = new MicroEngine.UI.Stack()
            .Add(new MicroEngine.UI.Button("one", () => Chosen ??= 0))
            .Add(new MicroEngine.UI.Button("two", () => Chosen ??= 1));
        public override void Update(float dt) { }
        public override void Draw(MicroEngine.Graphics.IRenderer r) { }
    }

    [TestMethod]
    [DoNotParallelize] // redirects the console
    public void AWrappedProfileJoinsTheCatalogAndRunsTheGame()
    {
        var profile = ReportingPlatform.Wrap(HeadlessPlatform.Profile);
        var catalog = new PlatformCatalog().Add(HeadlessPlatform.Profile).Add(profile);
        Assert.AreEqual("headless-reported", profile.Name);
        var output = new StringWriter();
        var previous = Console.Out;
        Console.SetOut(output);
        int exit;
        try { exit = GameLauncher.Run(new ExamplesGame(), ["--platform", "headless-reported", "--frames", "30"], catalog); }
        finally { Console.SetOut(previous); }
        Assert.AreEqual(0, exit);
        StringAssert.Contains(output.ToString(), "started MicroEngine.Core.HeadlessPlatform (without sound)");
        StringAssert.Contains(output.ToString(), "done: 30 updates, 30 frames", "the original platform, its preset included");
    }
}

/// <summary>The layer example (UI guide): a bubble following a character, a banner sliding in and fading out.</summary>
[TestClass]
public sealed class LayerExampleTests
{
    [TestMethod]
    public void TheBubbleStaysOverTheCharacterAsItWalks()
    {
        var (game, platform) = SettingsExampleTests.Start(new LayerExample());
        var scene = (LayerExample)game.Scenes.Current!;
        for (int i = 0; i < 60; i++)
        {
            SettingsExampleTests.Frame(game, platform);
            var head = game.Views.CanvasToUi(scene.Character);
            var b = scene.Bubble.Bounds;
            Assert.IsTrue(Math.Abs(b.Center.X - head.X) <= 2 && b.Bottom <= head.Y, $"step {i}: the bubble {b} sits over the head {head}");
        }
    }

    [TestMethod]
    public void ConfirmSlidesTheBannerInThenItFadesOut()
    {
        var (game, platform) = SettingsExampleTests.Start(new LayerExample());
        var scene = (LayerExample)game.Scenes.Current!;
        Assert.AreEqual(0, scene.Banner.Opacity, "hidden at first");
        SettingsExampleTests.Press(game, platform, "confirm");
        for (int i = 0; i < 30; i++) SettingsExampleTests.Frame(game, platform);
        Assert.AreEqual(1, scene.Banner.Opacity, 1e-3f);
        Assert.AreEqual(System.Numerics.Vector2.Zero, scene.Banner.Offset, "in place");
        var corner = game.Scenes.Current!.Widgets.Size;
        Assert.IsTrue(Math.Abs(scene.Banner.Bounds.Right - (corner.X - 4)) <= 1, "four units from the right edge");
        for (int i = 0; i < 240; i++) SettingsExampleTests.Frame(game, platform);
        Assert.AreEqual(0, scene.Banner.Opacity, 1e-3f, "faded out again");
    }
}

/// <summary>The dialogue example (focused examples): a box of labels revealing pages, then two buttons.</summary>
[TestClass]
public sealed class DialogueExampleTests
{
    [TestMethod]
    public void PagesRevealLetterByLetterContinueWhenLongAndEndInAnAnswer()
    {
        var (game, platform) = SettingsExampleTests.Start(new DialogueExample());
        var scene = (DialogueExample)game.Scenes.Current!;
        Assert.AreEqual("GARDENER", scene.Speaker.Text);
        SettingsExampleTests.Frame(game, platform);
        Assert.IsTrue(scene.Text.VisibleCharacters is > 0 and < 10, $"the page reveals a little at a time ({scene.Text.VisibleCharacters})");
        var size = scene.Dialogue.Bounds;

        var seen = new List<string>();
        for (int boxes = 0; scene.Dialogue.Visible && boxes < 10; boxes++)
        {
            SettingsExampleTests.Press(game, platform, "confirm");             // the rest of the box at once
            Assert.AreEqual(scene.Text.Text.Length, scene.Text.VisibleCharacters);
            Assert.IsTrue(scene.Text.Lines.Count <= 3, "a box holds three lines at most");
            for (int i = 0; i < 20; i++) SettingsExampleTests.Frame(game, platform);   // the arrow blinks
            Assert.AreEqual(size, scene.Dialogue.Bounds, "and keeps its size");
            seen.Add(scene.Text.Text.Replace('\n', ' '));
            SettingsExampleTests.Press(game, platform, "confirm");             // the next box
        }
        Assert.AreEqual(3, seen.Count, "the long first page takes two boxes, the question one");
        Assert.AreEqual("Welcome! The gems only grow at night, and only where the moonlight touches the soil of the old garden " +
            "behind the well. My lantern broke last winter, so I cannot find them on my own any more.", seen[0] + " " + seen[1],
            "the long page continues where the first box stopped, nothing cut");
        Assert.IsTrue(scene.Answers.Visible, "after the last page, the answer");

        SettingsExampleTests.Press(game, platform, "confirm");                 // YES has the focus
        Assert.AreEqual("THANK YOU!", scene.Result.Text);
        Assert.IsFalse(scene.Answers.Visible);
    }
}

/// <summary>The nine-slice example (UI guide): a menu drawn with images of the game's own.</summary>
[TestClass]
public sealed class SkinExampleTests
{
    [TestMethod]
    public void TheMenuDrawsWithItsImagesAndItsButtonsWork()
    {
        var (game, platform) = SettingsExampleTests.Start(new SkinExample());
        var scene = (SkinExample)game.Scenes.Current!;
        Assert.IsNotNull(scene.Widgets.Theme.Panel.Image, "buttons draw the wooden frame");
        Assert.IsNotNull(scene.Menu.Style?.Image, "the panel draws the parchment");
        Assert.AreNotEqual(scene.Widgets.Theme.Panel.Image!.Value.Source, scene.Menu.Style!.Value.Image!.Value.Source, "two parts of one image");
        SettingsExampleTests.Press(game, platform, "down");
        SettingsExampleTests.Press(game, platform, "confirm");
        Assert.AreEqual("OPTIONS", scene.Chosen);
    }
}

/// <summary>The title menu example (focused examples): a menu of the widget tree with a cursor sound.</summary>
[TestClass]
public sealed class MenuExampleTests
{
    private static readonly MicroEngine.Input.DeviceId Mouse = new(2);

    [TestMethod]
    public void TheCursorSkipsWhatCannotBeChosenCountsItsMovesAndFollowsTheMouse()
    {
        var (game, platform) = SettingsExampleTests.Start(new MenuExample());
        var scene = (MenuExample)game.Scenes.Current!;
        Assert.AreSame(scene.Items[0], scene.Widgets.Focused);
        Assert.AreEqual(0, scene.Moves, "the first focus is not a move");
        SettingsExampleTests.Press(game, platform, "down");
        Assert.AreSame(scene.Items[2], scene.Widgets.Focused, "CONTINUE cannot be chosen yet: the cursor skips it");
        Assert.AreEqual(1, scene.Moves);
        SettingsExampleTests.Press(game, platform, "confirm");
        Assert.AreEqual("OPTIONS WOULD OPEN HERE", scene.Result.Text);

        var first = scene.Items[0].Bounds;
        platform.Devices.Send(MicroEngine.Input.InputEvent.PointerMove(Mouse, 0, MicroEngine.Input.PointerKind.Mouse,
            game.Views.UiToCanvas(new System.Numerics.Vector2(first.X + first.Width / 2, first.Y + first.Height / 2))));
        SettingsExampleTests.Frame(game, platform);
        SettingsExampleTests.Frame(game, platform);
        Assert.AreSame(scene.Items[0], scene.Widgets.Focused, "the mouse over an item moves the cursor there");
        Assert.AreEqual(2, scene.Moves);
    }
}

/// <summary>The achievements example (UI guide): a list built from widgets and a banner per unlock, one at a time.</summary>
[TestClass]
public sealed class AchievementsExampleTests
{
    /// <summary>Every label's text under <paramref name="w"/>, in tree order, each followed by '|'.</summary>
    private static string Text(MicroEngine.UI.Widget w) =>
        (w is MicroEngine.UI.Label l ? l.Text + "|" : "") + string.Concat(w.Children.Select(Text));

    [TestMethod]
    public void ProgressShowsInTheListAndEveryUnlockGetsItsOwnBanner()
    {
        var (game, platform) = SettingsExampleTests.Start(new AchievementsExample());
        var scene = (AchievementsExample)game.Scenes.Current!;
        Assert.AreEqual(0f, scene.Banner.Opacity, "no banner before an unlock");
        Assert.AreEqual("COLLECT A GEM", ((MicroEngine.UI.Button)scene.Widgets.Focused!).Text);

        SettingsExampleTests.Press(game, platform, "confirm");
        Assert.IsTrue(scene.Tracker.IsUnlocked("first"));
        Assert.AreEqual(1, scene.Tracker.Progress("five"));
        for (int i = 0; i < 30; i++) SettingsExampleTests.Frame(game, platform);
        Assert.AreEqual(1f, scene.Banner.Opacity, 0.01f, "the banner slid in");
        StringAssert.Contains(Text(scene.Banner), "FIRST GEM|");
        StringAssert.Contains(Text(scene.Widgets.Content!), "FIRST GEM|UNLOCKED|");
        StringAssert.Contains(Text(scene.Widgets.Content!), "GEM HOARDER|1/5|");

        SettingsExampleTests.Press(game, platform, "right");
        SettingsExampleTests.Press(game, platform, "confirm");   // a second unlock while the first banner shows
        Assert.IsTrue(scene.Tracker.IsUnlocked("reset"));
        StringAssert.Contains(Text(scene.Banner), "FIRST GEM|", "it waits for the first banner to leave");
        for (int i = 0; i < 180; i++) SettingsExampleTests.Frame(game, platform);
        StringAssert.Contains(Text(scene.Banner), "FRESH START|", "then shows in turn");
    }
}

/// <summary>The text entry example (UI guide): a text field, its Enter, and copy and paste buttons on the clipboard.</summary>
[TestClass]
public sealed class TextEntryExampleTests
{
    private static readonly MicroEngine.Input.DeviceId Keyboard = new(1);

    [TestMethod]
    public void TypingSubmitsAndTheButtonsCopyAndPasteTheWholeField()
    {
        var (game, platform) = SettingsExampleTests.Start(new TextEntryExample());
        var scene = (TextEntryExample)game.Scenes.Current!;
        Assert.AreSame(scene.Field, scene.Widgets.Focused, "the field starts with the focus");

        platform.Devices.Send(MicroEngine.Input.InputEvent.TextInput(Keyboard, "Ada"));
        SettingsExampleTests.Frame(game, platform);
        platform.Devices.Send(MicroEngine.Input.InputEvent.KeyDown(Keyboard, MicroEngine.Input.Key.Enter));
        SettingsExampleTests.Frame(game, platform);
        Assert.AreEqual("HELLO, ADA", scene.Status.Text);

        scene.Widgets.Focus(scene.CopyButton);
        SettingsExampleTests.Press(game, platform, "confirm");
        Assert.AreEqual("Ada", platform.Clipboard.Text);
        Assert.AreEqual("COPIED", scene.Status.Text);

        platform.Clipboard.Text = "Grace";
        scene.Widgets.Focus(scene.PasteButton);
        SettingsExampleTests.Press(game, platform, "confirm");
        Assert.AreEqual("Grace", scene.Field.Text, "paste replaces the whole field");

        platform.Clipboard.Text = null;
        SettingsExampleTests.Press(game, platform, "confirm");
        Assert.AreEqual(("Grace", "NOTHING TO PASTE"), (scene.Field.Text, scene.Status.Text));
    }
}

/// <summary>The shaping example (text guide): Arabic and Hebrew from fallbacks of one shaped font, served to widgets too.</summary>
[TestClass]
public sealed class ShapingExampleTests
{
    [TestMethod]
    public void ArabicAndHebrewComeFromTheFallbacksAndRunRightToLeft()
    {
        var (game, platform) = SettingsExampleTests.Start(new ShapingExample());
        var scene = (ShapingExample)game.Scenes.Current!;
        var arabic = scene.Text.Shape("مرحبا بالعالم");
        Assert.IsTrue(arabic.Where(g => g.Advance > 0 && g.Cluster != 5).All(g => g.Face == 1 && g.Glyph != 0), "Arabic letters from Noto Sans Arabic, none missing");
        Assert.IsTrue(arabic[0].Cluster > arabic[^1].Cluster, "the last letter is drawn first, at the left");
        Assert.IsTrue(scene.Text.Shape("שלום עולם").Where(g => g.Cluster != 4).All(g => g.Face == 2 && g.Glyph != 0), "Hebrew from Noto Sans Hebrew");
        Assert.IsTrue(scene.Text.Shape("Hello").All(g => g.Face == 0), "Latin from Inter");
        Assert.IsInstanceOfType<MicroEngine.Graphics.ShapedFontSize>(scene.Widgets.Font, "the widgets use the shaped font");
        CollectionAssert.AreEqual(new[] { false, true, true, true, false }, ShapingExample.Lines.Select(scene.Text.IsRightToLeft).ToArray(), "each line aligned to the side it starts from");
        var mixed = scene.Text.Shape(ShapingExample.Lines[3]).Select(g => g.Cluster).Distinct().ToList();
        Assert.IsTrue(mixed.IndexOf(6) < mixed.IndexOf(8), "Bob reads left to right inside the Arabic line");
        game.Scenes.Pop();
        SettingsExampleTests.Frame(game, platform);
        Assert.ThrowsExactly<ObjectDisposedException>(() => scene.Text.Shape("x"), "leaving the example releases its font");
    }
}

/// <summary>The inventory example (UI guide): slots in a grid, the description following the focus.</summary>
[TestClass]
public sealed class InventoryExampleTests
{
    private static readonly MicroEngine.Input.DeviceId Mouse = new(2);

    [TestMethod]
    public void TheArrowsMoveByRowsAndColumnsAndConfirmUsesTheItem()
    {
        var (game, platform) = SettingsExampleTests.Start(new InventoryExample());
        var scene = game.Scenes.Current!;
        var slots = scene.Widgets.AllWidgets.OfType<ItemSlot>().ToArray();
        Assert.AreSame(slots[0], scene.Widgets.Focused);
        SettingsExampleTests.Press(game, platform, "down");
        Assert.AreSame(slots[4], scene.Widgets.Focused, "down is the slot below: four columns");
        var about = scene.Widgets.AllWidgets.OfType<MicroEngine.UI.Label>().Single();
        Assert.AreEqual("BREAKS CRACKED WALLS", about.Text, "the description follows the focus");
        SettingsExampleTests.Press(game, platform, "confirm");
        Assert.AreEqual(4, slots[4].Count);
        Assert.AreEqual("USED BOMB, 4 LEFT", about.Text);
    }

    [TestMethod]
    public void AClickOnASlotUsesItsItem()
    {
        var (game, platform) = SettingsExampleTests.Start(new InventoryExample());
        var key = game.Scenes.Current!.Widgets.AllWidgets.OfType<ItemSlot>().Single(s => s.Name == "KEY");
        var at = game.Views.UiToCanvas(key.Bounds.Center);
        platform.Devices.Send(MicroEngine.Input.InputEvent.PointerDown(Mouse, 0, MicroEngine.Input.PointerKind.Mouse, at));
        platform.Devices.Send(MicroEngine.Input.InputEvent.PointerUp(Mouse, 0, MicroEngine.Input.PointerKind.Mouse, at));
        SettingsExampleTests.Frame(game, platform);
        Assert.AreEqual(0, key.Count);
    }
}

/// <summary>The long list example (UI guide): thirty buttons in a scrolling view.</summary>
[TestClass]
public sealed class LongListExampleTests
{
    private static readonly MicroEngine.Input.DeviceId Mouse = new(2);

    [TestMethod]
    public void TheFocusGoesToTheLastLevelAndTheViewFollowsIt()
    {
        var (game, platform) = SettingsExampleTests.Start(new LongListExample());
        var scene = (LongListExample)game.Scenes.Current!;
        Assert.IsGreaterThan(0, scene.List.MaxScroll, "thirty levels do not fit");
        SettingsExampleTests.Press(game, platform, "up");                 // wraps from the first level to the last
        SettingsExampleTests.Press(game, platform, "confirm");
        Assert.AreEqual("LEVEL 30", scene.Picked);
        Assert.AreEqual(scene.List.MaxScroll, scene.List.Scroll, "the view scrolled to the end to show it");
    }

    [TestMethod]
    public void TheWheelScrollsAndAClickPicksTheLevelUnderThePointer()
    {
        var (game, platform) = SettingsExampleTests.Start(new LongListExample());
        var scene = (LongListExample)game.Scenes.Current!;
        var inside = new System.Numerics.Vector2(scene.List.Bounds.X + 20, scene.List.Bounds.Y + 20);
        platform.Devices.Send(MicroEngine.Input.InputEvent.WheelMoved(Mouse, new System.Numerics.Vector2(0, -2), game.Views.UiToCanvas(inside)));
        SettingsExampleTests.Frame(game, platform);
        Assert.IsGreaterThan(0, scene.List.Scroll);

        SettingsExampleTests.Frame(game, platform);                         // laid out at the new offset
        var level = scene.Widgets.AllWidgets.OfType<MicroEngine.UI.Button>().First(b => scene.List.Bounds.Contains(b.Bounds.Center));
        Assert.AreNotEqual("LEVEL 01", level.Text, "the first level scrolled away");
        var at = game.Views.UiToCanvas(level.Bounds.Center);
        platform.Devices.Send(MicroEngine.Input.InputEvent.PointerDown(Mouse, 0, MicroEngine.Input.PointerKind.Mouse, at));
        platform.Devices.Send(MicroEngine.Input.InputEvent.PointerUp(Mouse, 0, MicroEngine.Input.PointerKind.Mouse, at));
        SettingsExampleTests.Frame(game, platform);
        Assert.AreEqual(level.Text, scene.Picked);
    }
}

/// <summary>The settings screen example (UI guide): a slider, a toggle and a value selector built on widget primitives.</summary>
[TestClass]
public sealed class SettingsExampleTests
{
    private static readonly MicroEngine.Input.DeviceId Mouse = new(2);

    private sealed class NoEvents : MicroEngine.Platform.IEngineEventSink
    {
        public void Resized(MicroEngine.Kernel.SurfaceResized size) { }
        public void FocusChanged(bool focused) { }
        public void Suspending() { }
        public void Resumed() { }
        public void QuitRequested() { }
    }

    private static (ExamplesGame Game, HeadlessPlatform Platform, SettingsExample Scene) Open()
    {
        var (game, platform) = Start(new SettingsExample());
        return (game, platform, (SettingsExample)game.Scenes.Current!);
    }

    /// <summary>The examples game on a 960×540 headless window with <paramref name="scene"/> on top, its UI laid out.</summary>
    internal static (ExamplesGame Game, HeadlessPlatform Platform) Start(Scene scene)
    {
        var game = new ExamplesGame(Directory.CreateTempSubdirectory("examples-").FullName);
        var platform = HeadlessPlatform.For(game.Settings, rasterize: false);
        platform.Resize(960, 540);
        platform.PumpEvents(new NoEvents());
        game.InitializeForTest(platform);
        game.Scenes.Push(scene);
        Frame(game, platform);            // the push applies at the step
        // Headless tests do not draw, and drawing is what gives the UI its size.
        scene.Widgets.Size = game.Views.CanvasToUi(new System.Numerics.Vector2(game.Settings.VirtualWidth, game.Settings.VirtualHeight));
        Frame(game, platform);
        return (game, platform);
    }

    internal static void Frame(ExamplesGame game, HeadlessPlatform platform)
    {
        game.StepForTest();
    }

    internal static void Press(ExamplesGame game, HeadlessPlatform platform, string action)
    {
        game.Input.SetForced(action, true);
        Frame(game, platform);
        game.Input.SetForced(action, false);
        Frame(game, platform);
    }

    private static T Find<T>(SettingsExample scene, int index = 0) where T : MicroEngine.UI.Widget =>
        scene.Widgets.AllWidgets.OfType<T>().ElementAt(index);

    [TestMethod]
    public void TheKeyboardMovesThroughTheRowsAndChangesEachControl()
    {
        var (game, platform, scene) = Open();
        float music = game.Audio.MusicVolume;
        Assert.AreSame(Find<Slider>(scene), scene.Widgets.Focused, "the first slider has the focus");
        Press(game, platform, "left");
        Assert.AreEqual(music - 0.1f, game.Audio.MusicVolume, 1e-4f, "left lowers the music, and does not move the focus");
        Assert.AreSame(Find<Slider>(scene), scene.Widgets.Focused);

        Press(game, platform, "down");
        Press(game, platform, "down");
        Assert.AreSame(Find<Toggle>(scene), scene.Widgets.Focused);
        Press(game, platform, "confirm");
        Assert.IsFalse(Find<Toggle>(scene).On);

        Press(game, platform, "down");
        Press(game, platform, "right");
        Assert.AreEqual("HARD", Find<Choice>(scene).Value);
        Press(game, platform, "right");
        Assert.AreEqual("EASY", Find<Choice>(scene).Value, "the selector wraps");
    }

    [TestMethod]
    public void DraggingTheSliderSetsItFromThePointerEvenPastItsEnd()
    {
        var (game, platform, scene) = Open();
        var effects = Find<Slider>(scene, 1);
        var b = effects.Bounds;
        var at = (float fraction) => game.Views.UiToCanvas(new System.Numerics.Vector2(b.X + b.Width * fraction, b.Y + b.Height / 2));
        platform.Devices.Send(MicroEngine.Input.InputEvent.PointerDown(Mouse, 0, MicroEngine.Input.PointerKind.Mouse, at(0.25f)));
        Frame(game, platform);
        Assert.AreEqual(0.25f, game.Audio.SfxVolume, 0.02f);
        Assert.AreSame(effects, scene.Widgets.Focused, "a press focuses the slider");

        platform.Devices.Send(MicroEngine.Input.InputEvent.PointerMove(Mouse, 0, MicroEngine.Input.PointerKind.Mouse, at(3f)));
        Frame(game, platform);
        Assert.AreEqual(1f, game.Audio.SfxVolume, "dragged past the end: full");
        platform.Devices.Send(MicroEngine.Input.InputEvent.PointerUp(Mouse, 0, MicroEngine.Input.PointerKind.Mouse, at(3f)));
        Frame(game, platform);
        platform.Devices.Send(MicroEngine.Input.InputEvent.PointerMove(Mouse, 0, MicroEngine.Input.PointerKind.Mouse, at(0f)));
        Frame(game, platform);
        Assert.AreEqual(1f, game.Audio.SfxVolume, "after the release, moves no longer drag");
    }

    [TestMethod]
    public void AClickSwitchesTheToggleAndCancelLeavesTheScreen()
    {
        var (game, platform, scene) = Open();
        var toggle = Find<Toggle>(scene);
        var c = game.Views.UiToCanvas(new System.Numerics.Vector2(toggle.Bounds.X + 2, toggle.Bounds.Y + 2));
        platform.Devices.Send(MicroEngine.Input.InputEvent.PointerDown(Mouse, 0, MicroEngine.Input.PointerKind.Mouse, c));
        platform.Devices.Send(MicroEngine.Input.InputEvent.PointerUp(Mouse, 0, MicroEngine.Input.PointerKind.Mouse, c));
        Frame(game, platform);
        Assert.IsFalse(toggle.On);

        Press(game, platform, "cancel");
        Assert.IsNotInstanceOfType<SettingsExample>(game.Scenes.Current);
    }
}

/// <summary>The controls screen example (input guide): rebinding, swapping a taken control, resetting and saving.</summary>
[TestClass]
public sealed class RebindExampleTests
{
    private static readonly MicroEngine.Input.DeviceId Keyboard = new(1), Pad = new(2);

    private static MicroEngine.Input.InputMap Defaults()
    {
        var map = MicroEngine.Input.InputMap.CreateDefault();
        RebindExample.BindDefaults(map);
        return map;
    }

    private static string[] Keys(MicroEngine.Input.InputMap map, string action) => [.. map.GetBindings(action)!.Keys.Select(k => k.ToString())];
    private static string[] Buttons(MicroEngine.Input.InputMap map, string action) => [.. map.GetBindings(action)!.Buttons.Select(b => b.ToString())];

    [TestMethod]
    public void AFreeKeyReplacesTheActionsKeysAndLeavesTheOthersAlone()
    {
        var map = Defaults();
        RebindExample.Assign(map, "jump", MicroEngine.Input.InputEvent.KeyDown(Keyboard, MicroEngine.Input.Key.Q));
        CollectionAssert.AreEqual(new[] { "Q" }, Keys(map, "jump"));
        CollectionAssert.AreEqual(new[] { "A" }, Buttons(map, "jump"), "a key replaces keys, not buttons");
        CollectionAssert.AreEqual(new[] { "X" }, Keys(map, "fire"));
    }

    [TestMethod]
    public void AKeyAnotherGameplayActionUsesIsSwapped()
    {
        var map = Defaults();
        RebindExample.Assign(map, "jump", MicroEngine.Input.InputEvent.KeyDown(Keyboard, MicroEngine.Input.Key.X));
        CollectionAssert.AreEqual(new[] { "X" }, Keys(map, "jump"));
        CollectionAssert.AreEqual(new[] { "Z", "Space" }, Keys(map, "fire"), "fire gets the keys jump gave up");
        CollectionAssert.Contains(Keys(map, "cancel"), "X", "menu actions keep their keys");
    }

    [TestMethod]
    public void AButtonAnotherGameplayActionUsesIsSwapped()
    {
        var map = Defaults();
        RebindExample.Assign(map, "fire", MicroEngine.Input.InputEvent.ButtonDown(Pad, MicroEngine.Input.PadButton.A));
        CollectionAssert.AreEqual(new[] { "A" }, Buttons(map, "fire"));
        CollectionAssert.AreEqual(new[] { "X" }, Buttons(map, "jump"));
        CollectionAssert.AreEqual(new[] { "X" }, Keys(map, "fire"), "a button replaces buttons, not keys");
    }

    [TestMethod]
    public void TheActionsOwnControlChangesNothing()
    {
        var map = Defaults();
        RebindExample.Assign(map, "jump", MicroEngine.Input.InputEvent.KeyDown(Keyboard, MicroEngine.Input.Key.Space));
        CollectionAssert.AreEqual(new[] { "Z", "Space" }, Keys(map, "jump"));
    }

    [TestMethod]
    public void TheScreenRebindsWithTheNextPressSavesItAndEscapeKeepsTheBinding()
    {
        var saves = Directory.CreateTempSubdirectory("examples-").FullName;
        var platform = new HeadlessPlatform();
        var game = new ExamplesGame(saves);
        game.InitializeForTest(platform);
        game.Scenes.Push(new RebindExample(saves));
        game.StepForTest();

        void Choose()
        {
            game.Input.SetForced("confirm", true);
            game.StepForTest();
            game.Input.SetForced("confirm", false);
            game.StepForTest();
        }
        void Press(MicroEngine.Input.Key key)
        {
            platform.Devices.Send(MicroEngine.Input.InputEvent.KeyDown(Keyboard, key));
            game.StepForTest();
            platform.Devices.Send(MicroEngine.Input.InputEvent.KeyUp(Keyboard, key));
            game.StepForTest();
        }

        Choose();                        // "jump", the first entry
        Press(MicroEngine.Input.Key.Q);
        CollectionAssert.AreEqual(new[] { "Q" }, Keys(game.Input, "jump"));
        Assert.IsFalse(game.Input.Down("jump"), "the press that chose the key is not also a jump");

        Choose();
        Press(MicroEngine.Input.Key.Escape);
        CollectionAssert.AreEqual(new[] { "Q" }, Keys(game.Input, "jump"), "escape keeps the binding");
        Assert.IsInstanceOfType<RebindExample>(game.Scenes.Current, "and does not leave the screen");

        var reloaded = Defaults();
        Assert.IsTrue(reloaded.LoadBindings(new FileSaveStorage(saves)));
        CollectionAssert.AreEqual(new[] { "Q" }, Keys(reloaded, "jump"), "the change was saved");
        Directory.Delete(saves, true);
    }
}

/// <summary>The save-versioning example (persistence guide): old saves upgrade, newer ones are never overwritten.</summary>
[TestClass]
public sealed class SaveVersionTests
{
    private static (MemorySaveStorage Storage, SaveStore Store) With(string? profileJson)
    {
        var storage = new MemorySaveStorage();
        if (profileJson is not null) storage.Write(ProfileSaves.Slot, profileJson);
        return (storage, new SaveStore(storage));
    }

    [TestMethod]
    public void ASaveFromBeforeVersionsUpgradesThroughEveryStepAndKeepsTheOriginal()
    {
        const string original = """{ "Coins": 7 }""";
        var (storage, store) = With(original);
        var profile = ProfileSaves.Load(store, out var check);
        Assert.AreEqual(SaveCheck.Upgraded, check);
        Assert.IsNotNull(profile);
        Assert.AreEqual(7, profile.Gold, "1 -> 2 renamed Coins to Gold");
        Assert.AreEqual(3, profile.Lives, "0 -> 1 added lives");
        Assert.AreEqual(ProfileSaves.CurrentVersion, profile.Version);
        Assert.AreEqual(7, store.Load<System.Text.Json.Nodes.JsonObject>("profile.v0")!["Coins"]!.GetValue<int>(), "the original is backed up");
        Assert.AreEqual(original, storage.Read(ProfileSaves.Slot), "loading alone never rewrites the slot");

        Assert.IsTrue(ProfileSaves.Save(store, profile));
        Assert.AreEqual(SaveCheck.Current, (ProfileSaves.Load(store, out check), check).check, "saved again, it is current");
    }

    [TestMethod]
    public void ASaveFromAnIntermediateVersionRunsOnlyTheRemainingSteps()
    {
        var (_, store) = With("""{ "Version": 1, "Coins": 5, "Lives": 1 }""");
        var profile = ProfileSaves.Load(store, out var check);
        Assert.AreEqual(SaveCheck.Upgraded, check);
        Assert.AreEqual(5, profile!.Gold);
        Assert.AreEqual(1, profile.Lives, "the 0 -> 1 step did not run again");
    }

    [TestMethod]
    public void ACurrentSaveLoadsWithoutABackup()
    {
        var (storage, store) = With($$"""{ "Version": {{ProfileSaves.CurrentVersion}}, "Gold": 9, "Lives": 2 }""");
        var profile = ProfileSaves.Load(store, out var check);
        Assert.AreEqual(SaveCheck.Current, check);
        Assert.AreEqual(9, profile!.Gold);
        Assert.IsFalse(storage.Exists($"profile.v{ProfileSaves.CurrentVersion}"));
    }

    [TestMethod]
    public void NoSaveIsMissing()
    {
        var (_, store) = With(null);
        Assert.IsNull(ProfileSaves.Load(store, out var check));
        Assert.AreEqual(SaveCheck.Missing, check);
    }

    [TestMethod]
    [DataRow("""{ "Version": 99, "Gold": 1 }""", DisplayName = "written by a newer build")]
    [DataRow("""{ "Version": "two", "Gold": 1 }""", DisplayName = "a version that is not a number")]
    [DataRow("""{ "Version": 2, "Gold": "lots" }""", DisplayName = "a field of the wrong type")]
    public void ASaveThisBuildCannotReadIsProtectedAndLeftAsItIs(string json)
    {
        var (storage, store) = With(json);
        Assert.IsNull(ProfileSaves.Load(store, out var check));
        Assert.AreEqual(SaveCheck.Protected, check);
        Assert.AreEqual(json, storage.Read(ProfileSaves.Slot));
    }

    [TestMethod]
    public void WithoutABackupAnOldSaveIsNotUpgraded()
    {
        var storage = new BackupRefusingStorage();
        storage.Write(ProfileSaves.Slot, """{ "Coins": 4 }""");
        Assert.IsNull(ProfileSaves.Load(new SaveStore(storage), out var check));
        Assert.AreEqual(SaveCheck.Protected, check);
    }

    /// <summary>Memory slots that refuse every write except the profile itself, as a full disk would refuse the backup.</summary>
    private sealed class BackupRefusingStorage : ISaveStorage
    {
        private readonly MemorySaveStorage _slots = new();
        public string Location => "memory";
        public string? Read(string slot) => _slots.Read(slot);
        public void Write(string slot, string text)
        {
            if (slot != ProfileSaves.Slot) throw new IOException("disk full");
            _slots.Write(slot, text);
        }
        public bool Exists(string slot) => _slots.Exists(slot);
        public void Delete(string slot) => _slots.Delete(slot);
        public void SetAside(string slot) => _slots.SetAside(slot);
    }
}

[TestClass]
public sealed class PlatformerExampleTests
{
    private static void Hold(Game game, string action, int steps)
    {
        game.Input.SetForced(action, true);
        for (int i = 0; i < steps; i++) game.StepForTest();
        game.Input.SetForced(action, false);
        game.StepForTest();
    }

    [TestMethod]
    public void TheLevelPlacesItsFourCoinsAndKeepsThoseThePlayerDoesNotTouch()
    {
        var game = new ExamplesGame(Path.GetTempPath());
        game.InitializeForTest(new HeadlessPlatform());
        var scene = new PlatformerExample();
        game.Scenes.Push(scene);
        for (int i = 0; i < 60; i++) game.StepForTest();

        Assert.AreEqual(4, scene.CoinsLeft);
        Assert.AreEqual(0, scene.Collected);
    }

    [TestMethod]
    public void JumpsUpThroughAOneWayPlatformLandsOnItAndDropsBack()
    {
        var game = new ExamplesGame(Path.GetTempPath());
        game.InitializeForTest(new HeadlessPlatform());
        var scene = new PlatformerExample();
        game.Scenes.Push(scene);
        for (int i = 0; i < 60; i++) game.StepForTest();
        Assert.AreEqual(160, scene.Player.Bottom, "lands on the solid floor");

        Hold(game, "right", 30);                     // under the one-way ledge (x 64..112, top 112)
        Hold(game, "confirm", 2);
        for (int i = 0; i < 60; i++) game.StepForTest();
        Assert.AreEqual(112, scene.Player.Bottom, "jumped up through the ledge and stands on it");

        game.Input.SetForced("down", true);
        Hold(game, "confirm", 2);
        game.Input.SetForced("down", false);
        for (int i = 0; i < 60; i++) game.StepForTest();
        Assert.AreEqual(160, scene.Player.Bottom, "down + jump drops through it to the floor");
    }
}

[TestClass]
public sealed class DiagnosticsExampleTests
{
    [TestMethod]
    public void TheToggleTurnsOnMeasuringAndDrawingAndLeavingTheExampleTurnsThemOff()
    {
        var game = new ExamplesGame(Path.GetTempPath());
        game.InitializeForTest(new HeadlessPlatform());
        var scene = new DiagnosticsExample();
        game.Scenes.Push(scene);
        for (int i = 0; i < 60; i++) game.StepForTest();
        Assert.AreEqual(153, scene.PlayerPosition.Y, 1e-3f, "the player's box rests on the floor (top 160)");
        Assert.IsFalse(scene.Showing);
        Assert.AreEqual(0, game.DebugDraw.Count, "nothing is queued while the debug drawing is off");

        game.Input.SetForced("diagnostics", true);
        game.StepForTest();
        game.Input.SetForced("diagnostics", false);
        Assert.IsTrue(game.Diagnostics.Enabled && game.DebugDraw.Enabled);
        Assert.IsTrue(game.DebugDraw.Count > 0, "the physics view and the velocity line are queued");

        game.Scenes.Pop();
        game.StepForTest();
        Assert.IsFalse(game.Diagnostics.Enabled || game.DebugDraw.Enabled, "leaving the example turns both off");
    }
}
