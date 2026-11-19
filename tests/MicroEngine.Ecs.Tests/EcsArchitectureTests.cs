using MicroEngine.Core;

namespace MicroEngine.Ecs.Tests;

/// <summary>
/// ADR 0065: the package is the engine plus one library, and games without it do not carry either. The projects'
/// references are checked by the engine's architecture tests; this looks at the compiled assemblies.
/// </summary>
[TestClass]
public sealed class EcsArchitectureTests
{
    [TestMethod]
    public void The_package_references_the_engine_and_the_library_and_no_engine_assembly_references_either()
    {
        var references = typeof(EcsStore).Assembly.GetReferencedAssemblies().Select(a => a.Name!)
            .Where(n => !n.StartsWith("System", StringComparison.Ordinal) && n != "netstandard").ToList();
        CollectionAssert.Contains(references, "Arch");
        Assert.IsTrue(references.All(n => n == "Arch" || n.StartsWith("MicroEngine", StringComparison.Ordinal)), string.Join(", ", references));

        // Every engine assembly built beside the tests, but the package itself.
        var folder = Path.GetDirectoryName(typeof(Game).Assembly.Location)!;
        var engine = Directory.GetFiles(folder, "MicroEngine*.dll")
            .Where(f => Path.GetFileName(f) is not ("MicroEngine.Ecs.dll" or "MicroEngine.Ecs.Tests.dll")).ToList();
        Assert.IsTrue(engine.Count >= 10, string.Join(", ", engine.Select(Path.GetFileName)));
        foreach (string file in engine)
        {
            System.Reflection.AssemblyName[] referenced;
            try { referenced = System.Reflection.Assembly.LoadFrom(file).GetReferencedAssemblies(); }
            catch (BadImageFormatException) { continue; }   // a native library named like the engine
            foreach (var reference in referenced)
                Assert.IsFalse(reference.Name is "Arch" or "MicroEngine.Ecs", $"{Path.GetFileName(file)} -> {reference.Name}");
        }
    }

    [TestMethod]
    public void A_component_the_game_already_named_for_the_library_can_still_be_added()
    {
        // A game that moves to the package may still name its components itself, as the library's guide says.
        Arch.Core.ArrayRegistry.Add<AlreadyNamed>();
        var components = new EcsComponents().Add<AlreadyNamed>();
        Assert.IsTrue(components.Contains(typeof(AlreadyNamed)));
    }

    private struct AlreadyNamed;
}
