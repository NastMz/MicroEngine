using System.Numerics;
using MicroEngine.Content;
using MicroEngine.Core;
using MicroEngine.Graphics;
using MicroEngine.Input;
using MicroEngine.Kernel;
using MicroEngine.Localization;
using MicroEngine.Scenes;
using MicroEngine.UI;

namespace MicroEngine.Tests.Localization;

/// <summary>
/// A game's localization: tables loaded from its content, the first language, variants of assets per language, the
/// log, the state hash, widgets whose text follows the language and prompts named in it.
/// </summary>
[TestClass]
public sealed class GameLocalizationTests
{
    private const string English = """
        {
          "@name": "English",
          "menu": { "play": "Play", "quit": "Quit" },
          "hud.gems": "{count, plural, one {# gem} other {# gems}}",
          "hint.jump": "Press {jump} to jump",
          "name.hint": "Your name",
          "only.english": "English only"
        }
        """;
    private const string Spanish = """
        {
          "@name": "Español",
          "menu": { "play": "Jugar", "quit": "Salir" },
          "hud.gems": "{count, plural, one {# gema} other {# gemas}}",
          "hint.jump": "Pulsa {jump} para saltar",
          "name.hint": "Tu nombre",
          "prompt.Space": "Espacio",
          "prompt.D-pad": "Cruceta"
        }
        """;
    private const string Arabic = """{ "@name": "العربية", "menu": { "play": "العب", "quit": "خروج" } }""";

    private static string Content(Action<string>? more = null)
    {
        string root = Path.Combine(Path.GetTempPath(), "microengine-loc-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(root, "text"));
        Directory.CreateDirectory(Path.Combine(root, "ui"));
        File.WriteAllText(Path.Combine(root, "text", "en.strings.json"), English);
        File.WriteAllText(Path.Combine(root, "text", "es.strings.json"), Spanish);
        File.WriteAllText(Path.Combine(root, "text", "ar.strings.json"), Arabic);
        File.WriteAllText(Path.Combine(root, "text", "notes.txt"), "not a table");
        foreach (var image in new[] { "title.png", "title.es.png", "flag.png", "flag.ar.png", "flag.es-MX.png" })
            File.WriteAllBytes(Path.Combine(root, "ui", image), [1]);
        more?.Invoke(root);
        return root;
    }

    private sealed class Lines : ILog
    {
        public readonly List<string> Written = [];
        public void Write(LogLevel level, string category, string message) => Written.Add($"{level} {category}: {message}");
    }

    private sealed class SystemSays(params string[] languages) : ISystemLanguages
    {
        public IReadOnlyList<string> PreferredLanguages { get; } = languages;
    }

    private sealed class TextGame(string root, Action<TextGame>? load = null, string[]? system = null, Scene? scene = null) : Game
    {
        public Lines Lines { get; } = new();
        public override GameSettings Settings { get; } = new("text", 320, 180, 1);
        protected override string? ContentRoot => root;
        public override void Configure(EngineBuilder builder)
        {
            builder.Services.Replace<ILog>(Lines);
            if (system is not null) builder.Capabilities.Add<ISystemLanguages>(new SystemSays(system));
        }
        protected override void Load()
        {
            if (load is null) Localization.Load("text"); else load(this);
            if (scene is not null) Scenes.Push(scene);
        }
    }

    private static (TextGame Game, HeadlessPlatform Platform) Start(Action<TextGame>? load = null, string[]? system = null, Scene? scene = null, Action<string>? content = null)
    {
        var game = new TextGame(Content(content), load, system, scene);
        var platform = HeadlessPlatform.For(game.Settings, rasterize: false);
        game.InitializeForTest(platform);
        return (game, platform);
    }

    private static void Frame(Game game, HeadlessPlatform platform)
    {
        game.StepForTest();
        game.Render(platform.BeginFrame());
        platform.EndFrame();
    }

    [TestMethod]
    public void AFolderOfTablesGivesTheGameItsLanguages()
    {
        var (game, _) = Start();
        var text = game.Localization;
        CollectionAssert.AreEqual(new[] { "ar", "en", "es" }, text.Languages.Select(l => l.Code).ToArray());
        CollectionAssert.AreEqual(new[] { "العربية", "English", "Español" }, text.Languages.Select(l => l.Name).ToArray());
        Assert.AreEqual("en", text.Language, "a scripted run does not follow the machine's language");
        Assert.AreEqual("Play", text["menu.play"]);
        text.Language = "es";
        Assert.AreEqual("3 gemas", text.Format("hud.gems", ("count", 3)));
        Assert.AreSame(text, game.Services.Get<GameLocalization>());
    }

    [TestMethod]
    public void TheFirstLanguageIsTheSystemsWhenTheGameHasIt()
    {
        var (spanish, _) = Start(system: ["es-AR", "en-US"]);
        Assert.AreEqual("es", spanish.Localization.Language);
        Assert.IsFalse(spanish.Localization.LanguageChosen, "the system's language is a default, not a choice");

        var (french, _) = Start(system: ["fr-FR"]);
        Assert.AreEqual("en", french.Localization.Language, "a language the game lacks leaves the fallback language");

        // The player's saved choice wins, whenever it is applied.
        var (chosen, _) = Start(system: ["es"], load: g =>
        {
            g.Localization.TrySetLanguage("ar");
            g.Localization.Load("text");
        });
        Assert.AreEqual("ar", chosen.Localization.Language);
        Assert.IsTrue(chosen.Localization.RightToLeft);
    }

    private sealed class RealTimeGame(string root) : Game
    {
        public override GameSettings Settings { get; } = new("text", 320, 180, 1);
        protected override string? ContentRoot => root;
        public override void Configure(EngineBuilder builder) => builder.Configure<TimeSettings>(time => time with { Mode = TimeMode.RealTime });
        protected override void Load() => Localization.Load("text");
    }

    [TestMethod]
    public void AGameOnTheWallClockStartsInTheRuntimesLanguageWhereThePlatformDoesNotSay()
    {
        var before = System.Globalization.CultureInfo.CurrentUICulture;
        try
        {
            System.Globalization.CultureInfo.CurrentUICulture = new System.Globalization.CultureInfo("es-MX");
            var game = new RealTimeGame(Content());
            game.InitializeForTest(HeadlessPlatform.For(game.Settings, rasterize: false));
            CollectionAssert.AreEqual(new[] { "es-MX" }, game.Localization.SystemLanguages.ToArray());
            Assert.AreEqual("es", game.Localization.Language);

            // A build without culture data (browsers, phones, packaged games) has the invariant culture: nothing to follow.
            System.Globalization.CultureInfo.CurrentUICulture = System.Globalization.CultureInfo.InvariantCulture;
            var invariant = new RealTimeGame(Content());
            invariant.InitializeForTest(HeadlessPlatform.For(invariant.Settings, rasterize: false));
            Assert.AreEqual(0, invariant.Localization.SystemLanguages.Count);
            Assert.AreEqual("en", invariant.Localization.Language);
        }
        finally { System.Globalization.CultureInfo.CurrentUICulture = before; }
    }

    /// <summary>A platform that says which languages the player prefers, as the SDL platform does.</summary>
    private sealed class SpeakingPlatform(HeadlessPlatform inner, params string[] languages) : IPlatform
    {
        private readonly SystemSays _says = new(languages);
        public IGraphicsDevice Graphics => inner.Graphics;
        public MicroEngine.Audio.IAudioDevice Audio => inner.Audio;
        public void ToggleFullscreen() => inner.ToggleFullscreen();
        public IInputSource Input => inner.Input;
        public IRenderer BeginFrame() => inner.BeginFrame();
        public void EndFrame() => inner.EndFrame();
        public void SaveScreenshot(string path) => inner.SaveScreenshot(path);
        public Color Letterbox { get => inner.Letterbox; set => inner.Letterbox = value; }
        public MicroEngine.Platform.IMonotonicClock Clock => ((MicroEngine.Platform.IPlatformHost)inner).Clock;
        public bool PumpEvents(MicroEngine.Platform.IEngineEventSink sink) => inner.PumpEvents(sink);
        public MicroEngine.Platform.ISurface? MainSurface => ((MicroEngine.Platform.IPlatformHost)inner).MainSurface;
        public bool CanBlock => inner.CanBlock;
        public bool TryGetCapability<T>([System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out T? capability) where T : class
        {
            if (typeof(T) == typeof(ISystemLanguages)) { capability = (T)(object)_says; return true; }
            return inner.TryGetCapability(out capability);
        }
        public void Dispose() => inner.Dispose();
    }

    [TestMethod]
    public void ThePlatformsLanguagesAreFollowedOnTheWallClockAndNeverInADeterministicRun()
    {
        var real = new RealTimeGame(Content());
        real.InitializeForTest(new SpeakingPlatform(HeadlessPlatform.For(real.Settings, rasterize: false), "es-CO", "en"));
        Assert.AreEqual("es", real.Localization.Language, "a game a player runs starts in the player's language");

        var scripted = new TextGame(Content());
        scripted.InitializeForTest(new SpeakingPlatform(HeadlessPlatform.For(scripted.Settings, rasterize: false), "es-CO", "en"));
        Assert.AreEqual(0, scripted.Localization.SystemLanguages.Count);
        Assert.AreEqual("en", scripted.Localization.Language, "a scripted run is the same on every machine");
    }

    [TestMethod]
    public void OneFileOrACsvLoadsToo()
    {
        var (game, _) = Start(g =>
        {
            g.Localization.Load("text/es.strings.json");
            g.Localization.Load("more/all.strings.csv");
        }, content: root =>
        {
            Directory.CreateDirectory(Path.Combine(root, "more"));
            File.WriteAllText(Path.Combine(root, "more", "all.strings.csv"), "key,es,fr\nextra,Otro,Autre\n");
        });
        CollectionAssert.AreEqual(new[] { "es", "fr" }, game.Localization.Languages.Select(l => l.Code).ToArray());
        Assert.AreEqual("es", game.Localization.Language, "without English the first table is the fallback language");
        Assert.AreEqual("Otro", game.Localization["extra"]);
        Assert.AreEqual("Jugar", game.Localization["menu.play"]);
    }

    [TestMethod]
    public void ABrokenOrMissingTableFailsNamingTheFile()
    {
        var broken = Assert.ThrowsExactly<AssetLoadException>(() => Start(content: root =>
            File.WriteAllText(Path.Combine(root, "text", "fr.strings.json"), "{\n  \"a\": 3\n}")));
        Assert.AreEqual("text/fr.strings.json", broken.AssetPath);
        StringAssert.Contains(broken.Message, "line 2");

        var empty = Assert.ThrowsExactly<AssetLoadException>(() => Start(g => g.Localization.Load("ui")));
        StringAssert.Contains(empty.Message, ".strings.json");
        Assert.ThrowsExactly<AssetLoadException>(() => Start(g => g.Localization.Load("nowhere")));
        Assert.ThrowsExactly<AssetLoadException>(() => Start(g => g.Localization.Load("text/notes.txt")));
    }

    [TestMethod]
    public void AnAssetHasVariantsPerLanguage()
    {
        var (game, _) = Start();
        var text = game.Localization;
        Assert.AreEqual("ui/title.png", text.Path("ui/title.png"));
        text.Language = "es";
        Assert.AreEqual("ui/title.es.png", text.Path("ui/title.png"));
        Assert.AreSame(text.Path("ui/title.png"), text.Path("ui/title.png"), "asked again, the same string: nothing is allocated while drawing");
        Assert.AreEqual("ui/flag.png", text.Path("ui/flag.png"), "no variant for this language: the file itself");
        text.Language = "ar";
        Assert.AreEqual("ui/flag.ar.png", text.Path("ui/flag.png"));
        Assert.AreEqual("ui/title.png", text.Path("ui\\title.png"));
        CollectionAssert.AreEqual(new[] { "ui/flag.png", "ui/flag.ar.png" }, text.Variants("ui/flag.png").ToArray(), "the file and its variants for the game's languages, to preload");
        Assert.AreEqual("ui/none.png", text.Path("ui/none.png"), "a file that does not exist is for the loader to refuse");
    }

    [TestMethod]
    public void WhatIsMissingIsWrittenToTheLogOnce()
    {
        var (game, _) = Start();
        game.Localization.Language = "es";
        Assert.AreEqual("menu.options", game.Localization["menu.options"]);
        Assert.AreEqual("menu.options", game.Localization["menu.options"]);
        Assert.AreEqual("English only", game.Localization["only.english"]);
        var lines = game.Lines.Written.Where(l => l.Contains("localization")).ToList();
        Assert.AreEqual(2, lines.Count, string.Join("\n", lines));
        StringAssert.StartsWith(lines[0], "Warning localization: ");
        StringAssert.Contains(lines[0], "menu.options");
        StringAssert.Contains(lines[1], "only.english");
    }

    [TestMethod]
    public void TheLanguageIsPartOfTheStateHashOnceAGameHasTables()
    {
        var (plain, _) = Start(_ => { });
        Assert.IsFalse(plain.ComputeStateHash(sections: true).Sections.Any(s => s.Name == "engine.localization"), "a game without tables hashes as before");

        var (game, _) = Start();
        var english = game.ComputeStateHash(sections: true).Sections.Single(s => s.Name == "engine.localization");
        game.Localization.Language = "es";
        var spanish = game.ComputeStateHash(sections: true).Sections.Single(s => s.Name == "engine.localization");
        Assert.AreNotEqual(english.Hash, spanish.Hash);
    }

    private sealed class MenuScene : Scene
    {
        public readonly Label Title = new(new LocalizedText("menu.play"));
        public readonly Label Gems = new(new LocalizedText("hud.gems", ("count", 1)));
        public readonly Button Quit = new(new LocalizedText("menu.quit"));
        public readonly TextField Name = new() { Placeholder = "x", LocalizedPlaceholder = new LocalizedText("name.hint") };
        public readonly Label Plain = new("menu.play");

        public override void Enter() =>
            Widgets.Content = new Stack { Align = Align.Stretch, Width = 200 }.Add(Title).Add(Gems).Add(Quit).Add(Name).Add(Plain);

        public override void Update(float dt) { }
        public override void Draw(IRenderer renderer) { }
    }

    [TestMethod]
    public void WidgetsHoldingAKeyFollowTheLanguage()
    {
        var scene = new MenuScene();
        var (game, platform) = Start(scene: scene);
        Frame(game, platform);
        Assert.AreEqual("Play", scene.Title.Text);
        Assert.AreEqual("1 gem", scene.Gems.Text);
        Assert.AreEqual("Quit", scene.Quit.Text);
        Assert.AreEqual("Your name", scene.Name.Placeholder);
        Assert.AreEqual("menu.play", scene.Plain.Text, "plain text is never looked up");
        float english = scene.Quit.Bounds.Width;

        game.Localization.Language = "es";
        scene.Gems.Localized = new LocalizedText("hud.gems", ("count", 5));
        Frame(game, platform);
        Assert.AreEqual("Jugar", scene.Title.Text);
        Assert.AreEqual("5 gemas", scene.Gems.Text);
        Assert.AreEqual("Salir", scene.Quit.Text);
        Assert.AreEqual("Tu nombre", scene.Name.Placeholder);
        CollectionAssert.AreEqual(new[] { "Jugar" }, scene.Title.Lines.ToArray(), "the label is laid out again");
        Assert.AreEqual(english, scene.Quit.Bounds.Width, "the stack keeps its width");

        // Plain text set afterwards takes the key's place.
        scene.Title.Text = "Fixed";
        game.Localization.Language = "en";
        Frame(game, platform);
        Assert.AreEqual("Fixed", scene.Title.Text);
        Assert.IsNull(scene.Title.Localized);
    }

    [TestMethod]
    public void LabelsStartFromTheRightInARightToLeftLanguage()
    {
        var scene = new MenuScene();
        var (game, platform) = Start(scene: scene, load: g =>
        {
            g.Localization.Load("text");
            g.Localization.Language = "ar";
        });
        // The built-in font draws these letters as '?': the line is measured all the same.
        Frame(game, platform);
        var label = scene.Title;
        float width = game.Font.MeasureWidth(label.Text);
        Assert.IsTrue(width < label.Bounds.Width, "the stack is wider than the word");
        Assert.AreEqual(label.Bounds.Right - width, label.LineStart(0), 0.01f);
        game.Localization.Language = "en";
        Frame(game, platform);
        Assert.AreEqual(label.Bounds.X, label.LineStart(0), 0.01f);
    }

    [TestMethod]
    public void PromptsNameKeysAndControlsInTheLanguage()
    {
        var (game, _) = Start();
        game.Input.Bind("jump", [Key.Space], [PadButton.A]);
        var prompts = game.Prompts;
        Assert.AreEqual("Press Space to jump", prompts.Format(game.Localization["hint.jump"]));
        int version = prompts.Version;

        game.Localization.Language = "es";
        Assert.AreNotEqual(version, prompts.Version, "text laid out with prompts is laid out again");
        Assert.AreEqual("Pulsa Espacio para saltar", prompts.Format(game.Localization["hint.jump"]));
        Assert.AreEqual("Espacio", prompts.KeyText(Key.Space));
        Assert.AreEqual("Enter", prompts.KeyText(Key.Enter), "a name the table does not translate stays");
        Assert.AreEqual("Cruceta", prompts.Directions(device: InputDevice.Gamepad));
        Assert.AreEqual("Z", prompts.KeyText(Key.Z));
        Assert.AreEqual(0, game.Localization.MissingKeys.Count, "asking for a prompt's name reports nothing");
    }
}
