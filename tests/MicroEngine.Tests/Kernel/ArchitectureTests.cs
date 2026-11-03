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
        Audio = "MicroEngine.Audio", Scene = "MicroEngine.Scene", Physics = "MicroEngine.Physics", Box2D = "MicroEngine.Physics.Box2D",
        UI = "MicroEngine.UI", Saves = "MicroEngine.Saves", Achievements = "MicroEngine.Achievements", Hosting = "MicroEngine.Hosting",
        Testing = "MicroEngine.Testing", Migration = "MicroEngine.Migration.Entities",
        // Today's assemblies. MicroEngine still holds every module not yet split out of it; it becomes the metapackage.
        Core = "MicroEngine", Sdl = "MicroEngine.Sdl", Raylib = "MicroEngine.Raylib", Desktop = "MicroEngine.Desktop", Cli = "MicroEngine.Cli";

    private static readonly string[] Abstractions = [Kernel, Platform, Graphics, Content];
    private static readonly string[] Modules = [Graphics2D, Text, Input, Audio, Scene, Physics, UI, Saves, Achievements, Hosting];

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
        [Box2D] = [Physics, Scene, Kernel],
        [UI] = [Graphics2D, Text, Input, Graphics, Content, Kernel, Platform],
        [Saves] = [Kernel, Platform],
        [Achievements] = [Kernel, Platform, UI, Graphics2D, Text, Input, Graphics, Content],
        [Hosting] = [Kernel, Platform],
        [Testing] = [.. Abstractions, .. Modules, Core],
        [Migration] = [Scene, Graphics2D, Kernel, Graphics, Content, Platform],
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
