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
        Content = "MicroEngine.Content", Graphics2D = "MicroEngine.Graphics2D", Text = "MicroEngine.Text", Input = "MicroEngine.Input",
        Audio = "MicroEngine.Audio", Scene = "MicroEngine.Scene", Physics = "MicroEngine.Physics", PhysicsTiles = "MicroEngine.Physics.Tiles", Box2D = "MicroEngine.Physics.Box2D",
        UI = "MicroEngine.UI", Saves = "MicroEngine.Saves", Achievements = "MicroEngine.Achievements", Hosting = "MicroEngine.Hosting",
        Testing = "MicroEngine.Testing", Migration = "MicroEngine.Migration.Entities",
        // Today's assemblies. MicroEngine still holds every module not yet split out of it; it becomes the metapackage.
        Core = "MicroEngine", Sdl = "MicroEngine.Sdl", Raylib = "MicroEngine.Raylib", Desktop = "MicroEngine.Desktop", Cli = "MicroEngine.Cli";

    private static readonly string[] Abstractions = [Kernel, Platform, Graphics, Content];
    private static readonly string[] Modules = [Graphics2D, Text, Input, Audio, Scene, Physics, PhysicsTiles, UI, Saves, Achievements, Hosting];

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
        [Audio] = [Kernel, Platform, Content],
        [Scene] = [Kernel],
        [Physics] = [Scene, Kernel],
        [PhysicsTiles] = [Physics, Scene, Kernel],
        [Box2D] = [Physics, Scene, Kernel],
        [UI] = [Graphics2D, Text, Input, Graphics, Content, Kernel, Platform],
        [Saves] = [Kernel, Platform],
        // Progress persists through the save store (ADR 0006).
        [Achievements] = [Kernel, Platform, Saves, UI, Graphics2D, Text, Input, Graphics, Content],
        [Hosting] = [Kernel, Platform],
        [Testing] = [.. Abstractions, .. Modules, Core],
        // Transitional, deleted at the end of stage 5: the legacy Draw(IRenderer) needs the IRenderer facade, still in the core.
        [Migration] = [Scene, Graphics2D, Kernel, Graphics, Content, Platform, Core],
        // Transitional until the modules leave it: the defaults, presets and Game façade over every module.
        [Core] = [.. Abstractions, .. Modules],
        // Backends implement the abstractions; until those leave MicroEngine, they reach them through it.
        [Sdl] = [.. Abstractions, Core],
        [Raylib] = [.. Abstractions, Core],
        ["MicroEngine.Backend.Sdl"] = [.. Abstractions],
        ["MicroEngine.Backend.Raylib"] = [.. Abstractions],
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

    /// <summary>The projects that may reference the transitional adapter: the demos that still use it, and its own tests.</summary>
    private static readonly string[] MigrationConsumers = ["games/CourtyardKey/CourtyardKey.csproj", "games/MeadowRun/MeadowRun.csproj",
        "games/NeonRift/NeonRift.csproj", "tests/MicroEngine.Migration.Entities.Tests/MicroEngine.Migration.Entities.Tests.csproj"];

    [TestMethod]
    public void Only_the_demos_reference_the_transitional_entity_adapter()
    {
        var root = Repository();
        var offenders = new List<string>();
        foreach (var project in Directory.GetFiles(root, "*.csproj", SearchOption.AllDirectories))
        {
            string relative = Path.GetRelativePath(root, project).Replace('\\', '/');
            if (relative.Split('/').Any(part => part is "bin" or "obj" or "experiments" or "node_modules")) continue;
            bool references = XDocument.Load(project).Descendants("ProjectReference")
                .Any(r => Path.GetFileNameWithoutExtension(r.Attribute("Include")!.Value.Replace('\\', '/')) == Migration);
            if (references && !MigrationConsumers.Contains(relative)) offenders.Add(relative);
        }
        Assert.AreEqual(0, offenders.Count, "Only the demos may use MicroEngine.Migration.Entities: " + string.Join(", ", offenders));
        var packable = XDocument.Load(Path.Combine(root, "src", Migration, $"{Migration}.csproj")).Descendants("IsPackable").Select(e => e.Value).ToList();
        CollectionAssert.AreEqual(new[] { "false" }, packable, "the adapter is never packaged");
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
        Assert.AreEqual(typeof(MicroEngine.World.MoveResult), typeof(Game).Assembly.GetType("MicroEngine.World.MoveResult"));
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
