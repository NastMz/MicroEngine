namespace MicroEngine.Cli.Tests;

/// <summary>Every package that carries SDL's (or raylib's) binaries carries that library's licence file.</summary>
[TestClass]
public sealed class NoticesTests
{
    [TestMethod]
    [DataRow("windows", "NeonRift.exe", "SDL3.dll", "raylib.dll")]
    [DataRow("linux", "NeonRift", "libSDL3.so", "libraylib.so")]
    [DataRow("macos", "NeonRift", "libSDL3.dylib", "libraylib.dylib")]
    public void A_desktop_package_carries_the_licence_of_each_native_library(string os, string exe, string sdl, string raylib)
    {
        CollectionAssert.AreEqual(new[] { "SDL-LICENSE.txt", "raylib-LICENSE.txt" }, EngineNotices.ForFiles([exe, sdl, raylib, "Content/a.png"]).ToArray(), os);
        CollectionAssert.AreEqual(new[] { "SDL-LICENSE.txt" }, EngineNotices.ForFiles([exe, sdl]).ToArray(), os);
        Assert.AreEqual(0, EngineNotices.ForFiles([exe]).Count, os);
    }

    [TestMethod]
    [DataRow("android")]
    [DataRow("ios")]
    public void A_phone_package_carries_the_SDL_licence(string kind)
    {
        var notices = Hosts.Notices(kind, licence: null, thirdParty: "THIRD-PARTY-NOTICES.md");
        var sdl = notices.Single(n => n.Name == "SDL-LICENSE.txt");
        Assert.AreEqual(Path.Combine(Tool.Home, "Notices", "SDL-LICENSE.txt"), sdl.File);
        CollectionAssert.AreEqual(new[] { "THIRD-PARTY-NOTICES.txt", "microengine-LICENSE.txt", "SDL-LICENSE.txt" }, notices.Select(n => n.Name).ToArray());
    }

    [TestMethod]
    public void The_licence_files_ship_with_the_tool()
    {
        foreach (var name in new[] { "SDL-LICENSE.txt", "raylib-LICENSE.txt" })
            StringAssert.Contains(File.ReadAllText(Path.Combine(Tool.Home, "Notices", name)), "Permission is granted to anyone to use this software for any purpose", name);
    }
}
