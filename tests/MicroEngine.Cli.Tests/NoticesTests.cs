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
    [DataRow("libHarfBuzzSharp.dll")]
    [DataRow("libHarfBuzzSharp.so")]
    [DataRow("libHarfBuzzSharp.dylib")]
    public void A_package_of_a_game_that_shapes_text_carries_the_HarfBuzz_licence(string library)
    {
        CollectionAssert.AreEqual(new[] { "SDL-LICENSE.txt", "HarfBuzz-LICENSE.txt" }, EngineNotices.ForFiles(["Game", "libSDL3.so", library]).ToArray());
        foreach (var kind in new[] { "android", "ios" })
        {
            Assert.AreEqual("HarfBuzz-LICENSE.txt", Hosts.Notices(kind, null, "THIRD-PARTY-NOTICES.md", shaping: true)[^1].Name, kind);
            Assert.IsFalse(Hosts.Notices(kind, null, "THIRD-PARTY-NOTICES.md").Any(n => n.Name == "HarfBuzz-LICENSE.txt"), $"{kind}: only games that shape text");
        }
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
    public void The_runtime_licences_come_from_the_workload_pack_that_is_installed_even_when_the_SDK_is_newer()
    {
        // An SDK newer than the latest workload set (10.0.404 with the 10.403 set) bundles runtime 10.0.15, but the
        // workload installs its runtime packs at 10.0.14.
        string root = Directory.CreateTempSubdirectory("dotnet-root-").FullName;
        try
        {
            string packs = Path.Combine(root, "packs", "Microsoft.NETCore.App.Runtime.Mono.browser-wasm");
            foreach (var version in new[] { "10.0.9", "10.0.14" })
            {
                Directory.CreateDirectory(Path.Combine(packs, version));
                File.WriteAllText(Path.Combine(packs, version, "LICENSE.TXT"), version);
            }
            Assert.AreEqual(Path.Combine(packs, "10.0.14"), Hosts.WorkloadPack(root, "Microsoft.NETCore.App.Runtime.Mono.browser-wasm", "10.0.15"), "the newest pack installed");
            Assert.AreEqual(Path.Combine(packs, "10.0.9"), Hosts.WorkloadPack(root, "Microsoft.NETCore.App.Runtime.Mono.browser-wasm", "10.0.9"), "the SDK's own runtime version when it is installed");
            Assert.AreEqual(Path.Combine(root, "packs", "Missing", "10.0.15"), Hosts.WorkloadPack(root, "Missing", "10.0.15"), "no pack at all: the expected path, for the error message");
        }
        finally { Directory.Delete(root, recursive: true); }
    }

    [TestMethod]
    public void The_licence_files_ship_with_the_tool()
    {
        foreach (var name in new[] { "SDL-LICENSE.txt", "raylib-LICENSE.txt" })
            StringAssert.Contains(File.ReadAllText(Path.Combine(Tool.Home, "Notices", name)), "Permission is granted to anyone to use this software for any purpose", name);
        StringAssert.Contains(File.ReadAllText(Path.Combine(Tool.Home, "Notices", "HarfBuzz-LICENSE.txt")), "\"Old MIT\" license");
    }
}
