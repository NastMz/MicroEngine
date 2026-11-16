using MicroEngine.Core;

namespace MicroEngine.Navigation.Tests;

/// <summary>ADR 0056: navigation is managed code over the engine only, and games without it do not carry it.</summary>
[TestClass]
public sealed class NavigationArchitectureTests
{
    [TestMethod]
    public void The_package_references_only_the_engine_and_the_engine_never_references_it()
    {
        var references = typeof(PathFinder).Assembly.GetReferencedAssemblies().Select(a => a.Name!)
            .Where(n => !n.StartsWith("System", StringComparison.Ordinal) && n != "netstandard").ToList();
        Assert.IsTrue(references.All(n => n.StartsWith("MicroEngine", StringComparison.Ordinal)), string.Join(", ", references));
        Assert.IsFalse(typeof(Game).Assembly.GetReferencedAssemblies().Any(a => a.Name == "MicroEngine.Navigation"));
    }
}
