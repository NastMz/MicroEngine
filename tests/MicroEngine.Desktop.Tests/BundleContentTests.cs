namespace MicroEngine.Desktop.Tests;

/// <summary>Where a packaged desktop game finds its content files (packaging/README.md): no window is opened.</summary>
[TestClass]
public sealed class BundleContentTests
{
    private string _root = "";

    [TestInitialize]
    public void CreateRoot() => _root = Directory.CreateTempSubdirectory("microengine-bundle-").FullName;

    [TestCleanup]
    public void DeleteRoot() => Directory.Delete(_root, recursive: true);

    [TestMethod]
    public void A_macOS_bundle_reads_content_from_its_resources()
    {
        var macos = Directory.CreateDirectory(Path.Combine(_root, "Game.app", "Contents", "MacOS")).FullName;
        var content = Directory.CreateDirectory(Path.Combine(_root, "Game.app", "Contents", "Resources", "Content")).FullName;
        Assert.AreEqual(content, DesktopLauncher.BundleContentRoot(macos + Path.DirectorySeparatorChar));
    }

    [TestMethod]
    public void A_Content_folder_next_to_the_executable_still_wins()
    {
        var macos = Directory.CreateDirectory(Path.Combine(_root, "Game.app", "Contents", "MacOS")).FullName;
        Directory.CreateDirectory(Path.Combine(macos, "Content"));
        Directory.CreateDirectory(Path.Combine(_root, "Game.app", "Contents", "Resources", "Content"));
        Assert.IsNull(DesktopLauncher.BundleContentRoot(macos));
    }

    [TestMethod]
    public void Other_folders_keep_the_default()
    {
        Directory.CreateDirectory(Path.Combine(_root, "Content"));
        Assert.IsNull(DesktopLauncher.BundleContentRoot(_root));
        var macos = Directory.CreateDirectory(Path.Combine(_root, "Contents", "MacOS")).FullName;
        Assert.IsNull(DesktopLauncher.BundleContentRoot(macos), "no Resources/Content: nothing to point at");
    }
}
