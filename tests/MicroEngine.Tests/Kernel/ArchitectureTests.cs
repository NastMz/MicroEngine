#pragma warning disable CS0618 // the deprecated UI types keep their place in the module map until they are removed
using System.Xml.Linq;
using MicroEngine.Core;
using MicroEngine.Kernel;

namespace MicroEngine.Tests.Kernel;

/// <summary>
/// The engine's assemblies may reference each other only along the allowed edges of the target structure (the
/// assemblies, their contents and the arrows between them). Every engine project under <c>src/</c> must be in the
/// table, so a new assembly cannot appear without its edges being decided.
/// </summary>
[TestClass]
public sealed class ArchitectureTests
{
    private const string Kernel = "MicroEngine.Kernel", Platform = "MicroEngine.Platform", Graphics = "MicroEngine.Graphics",
        Content = "MicroEngine.Content", Graphics2D = "MicroEngine.Graphics2D", Text = "MicroEngine.Text", Input = "MicroEngine.Input", InputTouch = "MicroEngine.Input.Touch",
        Audio = "MicroEngine.Audio", Scene = "MicroEngine.Scene", Physics = "MicroEngine.Physics", PhysicsTiles = "MicroEngine.Physics.Tiles", Box2D = "MicroEngine.Physics.Box2D",
        TextShaping = "MicroEngine.Text.Shaping", UI = "MicroEngine.UI", Saves = "MicroEngine.Saves", Achievements = "MicroEngine.Achievements", Hosting = "MicroEngine.Hosting",
        Testing = "MicroEngine.Testing",
        // The transitional entity adapter of the demo migrations, deleted: no project may reference it again.
        Migration = "MicroEngine.Migration.Entities",
        // Today's assemblies. MicroEngine still holds every module not yet split out of it; it becomes the metapackage.
        Core = "MicroEngine", Sdl = "MicroEngine.Sdl", Raylib = "MicroEngine.Raylib", Desktop = "MicroEngine.Desktop", Cli = "MicroEngine.Cli";

    private static readonly string[] Abstractions = [Kernel, Platform, Graphics, Content];
    private static readonly string[] Modules = [Graphics2D, Text, Input, InputTouch, Audio, Scene, Physics, PhysicsTiles, UI, Saves, Achievements, Hosting];

    /// <summary>Each engine assembly and the engine assemblies it may reference.</summary>
    internal static readonly Dictionary<string, string[]> Allowed = new()
    {
        [Kernel] = [],
        [Platform] = [Kernel],
        [Graphics] = [Kernel, Platform],
        [Content] = [Kernel, Platform],
        [Graphics2D] = [Graphics, Content, Kernel, Platform],
        [Text] = [Graphics2D, Graphics, Content, Kernel, Platform],
        [Input] = [Kernel, Platform, Graphics2D, Text, Graphics, Content],
        // The on-screen controls draw (2D graphics) and may label with text.
        [InputTouch] = [Input, Graphics2D, Text, Graphics, Content, Kernel, Platform],
        [Audio] = [Kernel, Platform, Content],
        [Scene] = [Kernel],
        [Physics] = [Scene, Kernel],
        [PhysicsTiles] = [Physics, Scene, Kernel],
        [Box2D] = [Physics, Scene, Kernel],
        // Shaped text (an optional package): fonts drawn through the 2D layer. Its package depends on MicroEngine, which
        // carries the text assembly, so the project references MicroEngine.
        [TextShaping] = [.. Abstractions, Graphics2D, Text, Core],
        [UI] = [Graphics2D, Text, Input, Graphics, Content, Kernel, Platform],
        [Saves] = [Kernel, Platform],
        // Progress persists through the save store (ADR 0006).
        [Achievements] = [Kernel, Platform, Saves, UI, Graphics2D, Text, Input, Graphics, Content],
        [Hosting] = [Kernel, Platform],
        [Testing] = [.. Abstractions, .. Modules, Core],
        // Transitional until the modules leave it: the defaults, presets and Game façade over every module.
        [Core] = [.. Abstractions, .. Modules],
        // Backends implement the abstractions; until those leave MicroEngine, they reach them through it.
        // The SDL backend also offers the tier-1 device (sdl-gpu) next to SDL_Renderer.
        [Sdl] = [.. Abstractions, Core, "MicroEngine.Graphics.SdlGpu"],
        [Raylib] = [.. Abstractions, Core],
        ["MicroEngine.Backend.Sdl"] = [.. Abstractions],
        ["MicroEngine.Backend.Raylib"] = [.. Abstractions],
        // The tier-1 graphics device: the graphics contract on SDL_GPU, nothing else of the engine.
        ["MicroEngine.Graphics.SdlGpu"] = [Graphics, Kernel, Platform],
        // The 3D path: the graphics contract, content (the mesh asset kind) and the scene world it draws.
        ["MicroEngine.Graphics3D"] = [Graphics, Content, Scene, Kernel, Platform],
        [Desktop] = [Core, Sdl, Raylib, Hosting, Kernel, Platform],
        [Cli] = [Core, Kernel],
    };

    private static string Repository()
    {
        for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir is not null; dir = dir.Parent)
            if (File.Exists(Path.Combine(dir.FullName, "MicroEngine.slnx"))) return dir.FullName;
        throw new InvalidOperationException("The repository root (MicroEngine.slnx) was not found above the test output.");
    }

    [TestMethod]
    public void Every_engine_project_is_in_the_table_and_references_only_allowed_assemblies()
    {
        var src = Path.Combine(Repository(), "src");
        var projects = Directory.GetFiles(src, "*.csproj", SearchOption.AllDirectories)
            .Where(p => !p.Contains($"{Path.DirectorySeparatorChar}Hosts{Path.DirectorySeparatorChar}") && !p.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}"))
            .ToList();
        Assert.IsTrue(projects.Count >= 6, string.Join(", ", projects));
        var violations = new List<string>();
        foreach (var project in projects)
        {
            string name = Path.GetFileNameWithoutExtension(project);
            if (!Allowed.TryGetValue(name, out var allowed))
            {
                violations.Add($"{name} is not in the architecture table: decide its allowed references first");
                continue;
            }
            foreach (var reference in XDocument.Load(project).Descendants("ProjectReference"))
            {
                string target = Path.GetFileNameWithoutExtension(reference.Attribute("Include")!.Value.Replace('\\', '/'));
                if (!allowed.Contains(target)) violations.Add($"{name} -> {target}");
            }
        }
        Assert.AreEqual(0, violations.Count, "Forbidden references: " + string.Join("; ", violations));
    }

    [TestMethod]
    public void No_project_references_the_removed_entity_adapter()
    {
        var root = Repository();
        Assert.IsFalse(Directory.Exists(Path.Combine(root, "src", Migration)), "the transitional entity adapter is deleted");
        var offenders = new List<string>();
        foreach (var project in Directory.GetFiles(root, "*.csproj", SearchOption.AllDirectories))
        {
            string relative = Path.GetRelativePath(root, project).Replace('\\', '/');
            // Hidden folders (.git, local tool copies of the repository such as .claude/worktrees) are not the repository.
            if (relative.Split('/').Any(part => part is "bin" or "obj" or "experiments" or "node_modules" || part.StartsWith('.'))) continue;
            if (Path.GetFileNameWithoutExtension(project) == Migration
                || XDocument.Load(project).Descendants("ProjectReference")
                    .Any(r => Path.GetFileNameWithoutExtension(r.Attribute("Include")!.Value.Replace('\\', '/')) == Migration))
                offenders.Add(relative);
        }
        Assert.AreEqual(0, offenders.Count, "MicroEngine.Migration.Entities is gone; nothing may reference it: " + string.Join(", ", offenders));
        Assert.IsFalse(File.ReadAllText(Path.Combine(root, "MicroEngine.slnx")).Contains(Migration, StringComparison.Ordinal), "the solution lists no adapter project");
    }

    [TestMethod]
    public void The_core_no_longer_carries_the_legacy_entities()
    {
        Assert.IsNull(typeof(Game).Assembly.GetType("MicroEngine.World.Entity"));
        Assert.IsNull(typeof(Game).Assembly.GetType("MicroEngine.World.EntityList`1"));
    }

    [TestMethod]
    public void The_compiled_kernel_references_no_engine_assembly()
    {
        var engine = typeof(EngineBuilder).Assembly.GetReferencedAssemblies().Where(a => a.Name!.StartsWith("MicroEngine", StringComparison.Ordinal)).Select(a => a.Name).ToList();
        Assert.AreEqual(0, engine.Count, string.Join(", ", engine));
        Assert.AreEqual(Kernel, typeof(EngineBuilder).Assembly.GetName().Name);
        Assert.AreEqual(Kernel, typeof(Color).Assembly.GetName().Name, "the math and colour types live in the kernel");
        Assert.AreEqual(Kernel, typeof(ColorF).Assembly.GetName().Name);
    }

    [TestMethod]
    public void The_compiled_platform_contracts_reference_only_the_kernel()
    {
        var platform = typeof(MicroEngine.Platform.IPlatformHost).Assembly;
        Assert.AreEqual(Platform, platform.GetName().Name);
        Assert.AreEqual(Platform, typeof(MicroEngine.Platform.ISurface).Assembly.GetName().Name, "the surface is a platform contract");
        Assert.AreEqual(Platform, typeof(MicroEngine.Platform.IStorage).Assembly.GetName().Name);
        Assert.AreEqual(Platform, typeof(MicroEngine.Platform.IContentSource).Assembly.GetName().Name);
        var engine = platform.GetReferencedAssemblies().Select(a => a.Name!).Where(n => n.StartsWith("MicroEngine", StringComparison.Ordinal)).ToList();
        CollectionAssert.AreEqual(new[] { Kernel }, engine, string.Join(", ", engine));
        CollectionAssert.Contains(typeof(Game).Assembly.GetReferencedAssemblies().Select(a => a.Name).ToList(), Platform, "the core is built on the platform contracts");
    }

    [TestMethod]
    public void The_compiled_content_references_only_the_kernel_and_the_platform_contracts()
    {
        var content = typeof(MicroEngine.Content.AssetStore).Assembly;
        Assert.AreEqual(Content, content.GetName().Name);
        Assert.AreEqual(Content, typeof(MicroEngine.Content.Asset<>).Assembly.GetName().Name, "typed handles are content's");
        Assert.AreEqual(Content, typeof(AssetLoadException).Assembly.GetName().Name, "the load failure belongs with the loaders");
        Assert.AreEqual(Content, typeof(MicroEngine.Content.ZipContentSource).Assembly.GetName().Name);
        var engine = content.GetReferencedAssemblies().Select(a => a.Name!).Where(n => n.StartsWith("MicroEngine", StringComparison.Ordinal)).Order(StringComparer.Ordinal).ToList();
        CollectionAssert.AreEqual(new[] { Kernel, Platform }, engine, string.Join(", ", engine));
        CollectionAssert.Contains(typeof(Game).Assembly.GetReferencedAssemblies().Select(a => a.Name).ToList(), Content, "the asset cache is built on the store");
        Assert.AreEqual(typeof(AssetLoadException), typeof(Game).Assembly.GetType("MicroEngine.Core.AssetLoadException"), "code compiled against an earlier MicroEngine.dll still finds it");
    }

    [TestMethod]
    public void The_compiled_audio_references_only_the_kernel_the_platform_contracts_and_content()
    {
        var audio = typeof(MicroEngine.Audio.AudioEngine).Assembly;
        Assert.AreEqual(Audio, audio.GetName().Name);
        Assert.AreEqual(Audio, typeof(MicroEngine.Audio.AudioMixer).Assembly.GetName().Name, "the game's sound façade is audio's");
        Assert.AreEqual(Platform, typeof(MicroEngine.Platform.IAudioSink).Assembly.GetName().Name, "the PCM sink is a platform contract");
        var engine = audio.GetReferencedAssemblies().Select(a => a.Name!).Where(n => n.StartsWith("MicroEngine", StringComparison.Ordinal)).Order(StringComparer.Ordinal).ToList();
        Assert.IsTrue(engine.All(Allowed[Audio].Contains), string.Join(", ", engine));
        CollectionAssert.Contains(engine, Platform);
        Assert.AreEqual(typeof(MicroEngine.Audio.AudioClip), typeof(Game).Assembly.GetType("MicroEngine.Audio.AudioClip"), "code compiled against an earlier MicroEngine.dll still finds it");
    }

    [TestMethod]
    public void The_compiled_scene_world_references_only_the_kernel()
    {
        var scene = typeof(MicroEngine.Scenes.SceneWorld).Assembly;
        Assert.AreEqual(Scene, scene.GetName().Name);
        Assert.AreEqual(Scene, typeof(MicroEngine.Scenes.IWorld).Assembly.GetName().Name, "the world contract is the scene module's");
        Assert.AreEqual(Scene, typeof(MicroEngine.Scenes.EntityId).Assembly.GetName().Name);
        Assert.AreEqual(Scene, typeof(MicroEngine.Scenes.Transform).Assembly.GetName().Name);
        var engine = scene.GetReferencedAssemblies().Select(a => a.Name!).Where(n => n.StartsWith("MicroEngine", StringComparison.Ordinal)).ToList();
        CollectionAssert.AreEqual(new[] { Kernel }, engine, string.Join(", ", engine));
        Assert.AreEqual(Core, typeof(MicroEngine.Scenes.Scene).Assembly.GetName().Name, "the scene stack stays in the core until it leaves it");
    }

    [TestMethod]
    public void The_compiled_physics_contract_references_only_the_scene_world_and_the_kernel_and_the_tile_backend_only_the_contract()
    {
        var physics = typeof(MicroEngine.Physics.IPhysicsWorld2D).Assembly;
        Assert.AreEqual(Physics, physics.GetName().Name);
        Assert.AreEqual(Physics, typeof(MicroEngine.Physics.PhysicsCapabilityException).Assembly.GetName().Name, "the typed capability error is the contract's");
        Assert.AreEqual(Physics, typeof(MicroEngine.Physics.PhysicsModule).Assembly.GetName().Name, "stepping and transform sync are the contract's module");
        var engine = physics.GetReferencedAssemblies().Select(a => a.Name!).Where(n => n.StartsWith("MicroEngine", StringComparison.Ordinal)).Order(StringComparer.Ordinal).ToList();
        CollectionAssert.AreEqual(new[] { Kernel, Scene }, engine, string.Join(", ", engine));

        var tiles = typeof(MicroEngine.Physics.Tiles.TilePhysicsWorld2D).Assembly;
        Assert.AreEqual(PhysicsTiles, tiles.GetName().Name);
        Assert.AreEqual(PhysicsTiles, typeof(MicroEngine.World.TileGrid).Assembly.GetName().Name, "the tile types live with the tile backend");
        Assert.AreEqual(PhysicsTiles, typeof(MicroEngine.World.TileMotion).Assembly.GetName().Name);
        var tileRefs = tiles.GetReferencedAssemblies().Select(a => a.Name!).Where(n => n.StartsWith("MicroEngine", StringComparison.Ordinal)).ToList();
        Assert.IsTrue(tileRefs.All(Allowed[PhysicsTiles].Contains), string.Join(", ", tileRefs));
        CollectionAssert.Contains(tileRefs, Physics);
        Assert.AreEqual(typeof(MicroEngine.World.TileGrid), typeof(Game).Assembly.GetType("MicroEngine.World.TileGrid"), "code compiled against an earlier MicroEngine.dll still finds it");
        Assert.AreEqual(PhysicsTiles, typeof(MicroEngine.World.TileMoveResult).Assembly.GetName().Name);
        Assert.AreEqual(Core, typeof(MicroEngine.World.TileRenderer).Assembly.GetName().Name, "drawing tiles needs the renderer, which stays in the core");
    }

    [TestMethod]
    public void The_compiled_hosting_references_only_the_kernel_and_the_platform_contracts()
    {
        var hosting = typeof(MicroEngine.Hosting.HostBuilder).Assembly;
        Assert.AreEqual(Hosting, hosting.GetName().Name);
        Assert.AreEqual(Hosting, typeof(MicroEngine.Hosting.LaunchRequest).Assembly.GetName().Name, "the launcher model is hosting's");
        var engine = hosting.GetReferencedAssemblies().Select(a => a.Name!).Where(n => n.StartsWith("MicroEngine", StringComparison.Ordinal)).Order(StringComparer.Ordinal).ToList();
        CollectionAssert.AreEqual(new[] { Kernel, Platform }, engine, string.Join(", ", engine));
        CollectionAssert.Contains(typeof(Game).Assembly.GetReferencedAssemblies().Select(a => a.Name).ToList(), Hosting, "every host composes through hosting");
    }

    [TestMethod]
    public void The_compiled_saves_reference_only_the_kernel_and_the_platform_contracts()
    {
        var saves = typeof(SaveStore).Assembly;
        Assert.AreEqual(Saves, saves.GetName().Name);
        Assert.AreEqual(Saves, typeof(FileSaveStorage).Assembly.GetName().Name, "the file medium is the saves'");
        Assert.AreEqual(Saves, typeof(MicroEngine.Saves.SavesModule).Assembly.GetName().Name);
        var engine = saves.GetReferencedAssemblies().Select(a => a.Name!).Where(n => n.StartsWith("MicroEngine", StringComparison.Ordinal)).Order(StringComparer.Ordinal).ToList();
        CollectionAssert.AreEqual(new[] { Kernel, Platform }, engine, string.Join(", ", engine));
        // Code compiled against an earlier MicroEngine.dll still finds the save types there.
        var forwarded = typeof(Game).Assembly.GetForwardedTypes();
        CollectionAssert.IsSubsetOf(new[] { typeof(SaveStore), typeof(ISaveStorage), typeof(FileSaveStorage) }, forwarded);
    }

    [TestMethod]
    public void The_compiled_touch_controls_reference_only_their_allowed_assemblies()
    {
        var touch = typeof(MicroEngine.Input.VirtualControls).Assembly;
        Assert.AreEqual(InputTouch, touch.GetName().Name);
        var moved = new[]
        {
            typeof(MicroEngine.Input.TouchLayout), typeof(MicroEngine.Input.TouchStyle), typeof(MicroEngine.Input.ITouchSkin),
            typeof(MicroEngine.Input.ITouchScreen), typeof(MicroEngine.Input.TouchEvent), typeof(MicroEngine.Input.IOverlayCanvas),
            typeof(MicroEngine.Input.ITouchOverlay), typeof(MicroEngine.Input.FrameLayout),
        };
        foreach (var type in moved) Assert.AreEqual(InputTouch, type.Assembly.GetName().Name, type.Name);
        var engine = touch.GetReferencedAssemblies().Select(a => a.Name!).Where(n => n.StartsWith("MicroEngine", StringComparison.Ordinal)).ToList();
        Assert.IsTrue(engine.All(Allowed[InputTouch].Contains), string.Join(", ", engine));
        CollectionAssert.DoesNotContain(engine, Core);
        // Code compiled against an earlier MicroEngine.dll still finds the touch types there.
        CollectionAssert.IsSubsetOf(moved.Append(typeof(MicroEngine.Input.VirtualControls)).ToArray(), typeof(Game).Assembly.GetForwardedTypes());
    }

    [TestMethod]
    public void The_compiled_achievements_reference_only_the_kernel_the_platform_saves_and_2D_graphics()
    {
        var achievements = typeof(AchievementTracker).Assembly;
        Assert.AreEqual(Achievements, achievements.GetName().Name);
        foreach (var type in new[] { typeof(Achievement), typeof(AchievementStatus), typeof(AchievementSaveData), typeof(AchievementRecord), typeof(MicroEngine.Achievements.AchievementsModule) })
            Assert.AreEqual(Achievements, type.Assembly.GetName().Name, type.Name);
        var engine = achievements.GetReferencedAssemblies().Select(a => a.Name!).Where(n => n.StartsWith("MicroEngine", StringComparison.Ordinal)).ToList();
        Assert.IsTrue(engine.All(Allowed[Achievements].Contains), string.Join(", ", engine));
        CollectionAssert.Contains(engine, Saves, "achievements persist through the save store");
        CollectionAssert.DoesNotContain(engine, Core);
        // Code compiled against an earlier MicroEngine.dll still finds the achievement types there.
        CollectionAssert.IsSubsetOf(new[] { typeof(Achievement), typeof(AchievementStatus), typeof(AchievementSaveData), typeof(AchievementRecord), typeof(AchievementTracker) },
            typeof(Game).Assembly.GetForwardedTypes());
    }

    [TestMethod]
    public void The_compiled_graphics_contract_references_only_the_kernel_and_the_platform_contracts()
    {
        var graphics = typeof(MicroEngine.Graphics.Devices.IGraphicsDevice).Assembly;
        Assert.AreEqual(Graphics, graphics.GetName().Name);
        Assert.AreEqual(Graphics, typeof(MicroEngine.Graphics.Devices.GraphicsDeviceBase).Assembly.GetName().Name, "core validation is the contract's");
        Assert.AreEqual(Graphics, typeof(MicroEngine.Graphics.Devices.ReferenceGraphicsDevice).Assembly.GetName().Name, "the reference device lives with the contract");
        var engine = graphics.GetReferencedAssemblies().Select(a => a.Name!).Where(n => n.StartsWith("MicroEngine", StringComparison.Ordinal)).Order(StringComparer.Ordinal).ToList();
        CollectionAssert.AreEqual(new[] { Kernel, Platform }, engine, string.Join(", ", engine));
    }

    [TestMethod]
    public void The_compiled_2D_layer_references_only_the_graphics_contract_the_kernel_and_the_platform()
    {
        var graphics2D = typeof(MicroEngine.Graphics.Renderer2D).Assembly;
        Assert.AreEqual(Graphics2D, graphics2D.GetName().Name);
        Assert.AreEqual(Graphics2D, typeof(MicroEngine.Graphics.Camera2D).Assembly.GetName().Name, "the 2D camera lives in the 2D layer");
        var engine = graphics2D.GetReferencedAssemblies().Select(a => a.Name!).Where(n => n.StartsWith("MicroEngine", StringComparison.Ordinal)).ToList();
        Assert.IsTrue(engine.All(Allowed[Graphics2D].Contains), string.Join(", ", engine));
        CollectionAssert.Contains(engine, Graphics, "the batcher is built on the device contract");
        CollectionAssert.Contains(typeof(Game).Assembly.GetReferencedAssemblies().Select(a => a.Name).ToList(), Graphics2D, "the IRenderer façade is built on the batcher");
    }

    [TestMethod]
    public void The_compiled_shaping_module_references_only_text_the_2D_layer_and_the_contracts_below_them()
    {
        var shaping = typeof(MicroEngine.Graphics.ShapedFont).Assembly;
        Assert.AreEqual(TextShaping, shaping.GetName().Name);
        var engine = shaping.GetReferencedAssemblies().Select(a => a.Name!).Where(n => n.StartsWith("MicroEngine", StringComparison.Ordinal)).ToList();
        Assert.IsTrue(engine.All(new[] { Text, Graphics2D, Graphics, Content, Kernel, Platform }.Contains), string.Join(", ", engine));
        Assert.IsTrue(shaping.GetReferencedAssemblies().Any(a => a.Name == "HarfBuzzSharp"), "HarfBuzz follows the shaping module");
        Assert.IsFalse(typeof(Game).Assembly.GetReferencedAssemblies().Concat(typeof(MicroEngine.Graphics.BitmapFont).Assembly.GetReferencedAssemblies())
            .Any(a => a.Name == "HarfBuzzSharp"), "games without shaping do not carry HarfBuzz");
    }

    [TestMethod]
    public void The_compiled_text_references_only_the_2D_layer_the_graphics_contract_content_the_kernel_and_the_platform()
    {
        var text = typeof(MicroEngine.Graphics.BitmapFont).Assembly;
        Assert.AreEqual(Text, text.GetName().Name);
        var fonts = new[] { typeof(MicroEngine.Graphics.IFont), typeof(MicroEngine.Graphics.FontExtensions), typeof(MicroEngine.Graphics.ScalableFont),
            typeof(MicroEngine.Graphics.SizedFont), typeof(MicroEngine.Graphics.FontOptions) };
        foreach (var type in fonts) Assert.AreEqual(Text, type.Assembly.GetName().Name, $"{type.Name} is text's");
        var engine = text.GetReferencedAssemblies().Select(a => a.Name!).Where(n => n.StartsWith("MicroEngine", StringComparison.Ordinal)).ToList();
        Assert.IsTrue(engine.All(Allowed[Text].Contains), string.Join(", ", engine));
        CollectionAssert.Contains(engine, Graphics2D, "fonts draw through the 2D layer");
        CollectionAssert.Contains(engine, Content, "the font loader is registered with content");
        Assert.IsTrue(text.GetReferencedAssemblies().Any(a => a.Name == "FontStashSharp"), "the scalable-font dependency follows text");
        Assert.IsFalse(typeof(Game).Assembly.GetReferencedAssemblies().Any(a => a.Name == "FontStashSharp"), "the core no longer rasterizes fonts");
        // Code compiled against an earlier MicroEngine.dll still finds the font types there.
        CollectionAssert.IsSubsetOf(fonts.Append(typeof(MicroEngine.Graphics.BitmapFont)).ToArray(), typeof(Game).Assembly.GetForwardedTypes());
    }

    [TestMethod]
    public void The_compiled_UI_references_only_the_2D_layer_text_input_the_graphics_contract_content_the_kernel_and_the_platform()
    {
        var ui = typeof(MicroEngine.UI.UiRoot).Assembly;
        Assert.AreEqual(UI, ui.GetName().Name);
        var moved = new[] { typeof(MicroEngine.UI.Typewriter), typeof(MicroEngine.UI.SelectionList), typeof(MicroEngine.UI.UiTheme) };
        var widgets = new[] { typeof(MicroEngine.UI.Widget), typeof(MicroEngine.UI.Button), typeof(MicroEngine.UI.Label), typeof(MicroEngine.UI.Panel),
            typeof(MicroEngine.UI.Stack), typeof(MicroEngine.UI.TextField), typeof(MicroEngine.UI.Toasts), typeof(MicroEngine.UI.UiActions) };
        foreach (var type in moved.Concat(widgets)) Assert.AreEqual(UI, type.Assembly.GetName().Name, $"{type.Name} is the UI's");
        var engine = ui.GetReferencedAssemblies().Select(a => a.Name!).Where(n => n.StartsWith("MicroEngine", StringComparison.Ordinal)).ToList();
        Assert.IsTrue(engine.All(Allowed[UI].Contains), string.Join(", ", engine));
        CollectionAssert.Contains(engine, Input, "widgets read actions and take events as an input layer");
        CollectionAssert.Contains(engine, Text, "widgets draw text");
        // Code compiled against an earlier MicroEngine.dll still finds the moved widgets there.
        CollectionAssert.IsSubsetOf(moved, typeof(Game).Assembly.GetForwardedTypes());
        CollectionAssert.Contains(typeof(Game).Assembly.GetReferencedAssemblies().Select(a => a.Name).ToList(), UI, "the façade hosts the widgets in scenes");
    }

    [TestMethod]
    public void The_compiled_input_references_only_the_kernel_and_the_platform_contracts()
    {
        var input = typeof(MicroEngine.Input.InputMap).Assembly;
        Assert.AreEqual(Input, input.GetName().Name);
        Assert.AreEqual(Input, typeof(MicroEngine.Input.InputSystem).Assembly.GetName().Name, "devices, players and layers are input's");
        Assert.AreEqual(Input, typeof(MicroEngine.Input.IInputBackend).Assembly.GetName().Name);
        var engine = input.GetReferencedAssemblies().Select(a => a.Name!).Where(n => n.StartsWith("MicroEngine", StringComparison.Ordinal)).Order(StringComparer.Ordinal).ToList();
        CollectionAssert.AreEqual(new[] { Kernel, Platform }, engine, string.Join(", ", engine));
        CollectionAssert.Contains(typeof(Game).Assembly.GetReferencedAssemblies().Select(a => a.Name).ToList(), Input, "the core reads input through it");
    }

    [TestMethod]
    public void The_compiled_core_references_only_allowed_assemblies()
    {
        var engine = typeof(Game).Assembly.GetReferencedAssemblies().Select(a => a.Name!).Where(n => n.StartsWith("MicroEngine", StringComparison.Ordinal)).ToList();
        CollectionAssert.Contains(engine, Kernel);
        Assert.IsTrue(engine.All(Allowed[Core].Contains), string.Join(", ", engine));
    }

    [TestMethod]
    public void The_table_only_allows_edges_between_assemblies_it_knows()
    {
        var unknown = Allowed.SelectMany(e => e.Value.Where(t => !Allowed.ContainsKey(t)).Select(t => $"{e.Key} -> {t}")).ToList();
        Assert.AreEqual(0, unknown.Count, string.Join("; ", unknown));
        Assert.AreEqual(0, Allowed[Kernel].Length, "the kernel references nothing");
    }
}
