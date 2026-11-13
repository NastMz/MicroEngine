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
    public void The_runtime_licences_come_from_the_runtime_pack_MSBuild_resolved_not_from_the_SDK_version()
    {
        // What SDK 11.0.100 (which bundles runtime 11.0.0) prints for a net10.0 Android host: the pack it uses is 10.0.15,
        // and other frameworks' packs come first.
        const string json = """
            {
              "Items": {
                "ResolvedRuntimePack": [
                  { "Identity": "Microsoft.Android.Runtime.36.android", "FrameworkName": "Microsoft.Android", "NuGetPackageVersion": "36.1.118", "PackageDirectory": "/dotnet/packs/Microsoft.Android.Runtime.36.android/36.1.118" },
                  { "Identity": "Microsoft.NETCore.App.Runtime.Mono.android-arm64", "FrameworkName": "Microsoft.NETCore.App", "NuGetPackageVersion": "10.0.15", "PackageDirectory": "/dotnet/packs/Microsoft.NETCore.App.Runtime.Mono.android-arm64/10.0.15" }
                ]
              }
            }
            """;
        Assert.AreEqual(new DotnetRuntime("/dotnet/packs/Microsoft.NETCore.App.Runtime.Mono.android-arm64/10.0.15", "10.0.15"), Packaging.ParseRuntimePack(json));
        Assert.IsNull(Packaging.ParseRuntimePack("""{ "Items": { "ResolvedRuntimePack": [] } }"""), "no runtime pack resolved (a project restored for another runtime)");
        Assert.IsNull(Packaging.ParseRuntimePack("""{ "Items": {} }"""));
        Assert.ThrowsExactly<CliException>(() => Packaging.ParseRuntimePack("warning: not JSON"));
    }

    [TestMethod]
    public void A_runtime_pack_without_its_licence_files_is_an_error_that_names_the_folder()
    {
        string dir = Directory.CreateTempSubdirectory("runtime-pack-").FullName;
        try
        {
            var pack = new DotnetRuntime(dir, "10.0.15");
            File.WriteAllText(Path.Combine(dir, "LICENSE.TXT"), "MIT");
            Assert.AreEqual(Path.Combine(dir, "LICENSE.TXT"), pack.Licence);
            var error = Assert.ThrowsExactly<CliException>(() => pack.Notices);
            StringAssert.Contains(error.Message, Path.Combine(dir, "THIRD-PARTY-NOTICES.TXT"));
            StringAssert.Contains(error.Message, "10.0.15");
        }
        finally { Directory.Delete(dir, recursive: true); }
    }

    [TestMethod]
    public void The_licence_files_ship_with_the_tool()
    {
        foreach (var name in new[] { "SDL-LICENSE.txt", "raylib-LICENSE.txt" })
            StringAssert.Contains(File.ReadAllText(Path.Combine(Tool.Home, "Notices", name)), "Permission is granted to anyone to use this software for any purpose", name);
        StringAssert.Contains(File.ReadAllText(Path.Combine(Tool.Home, "Notices", "HarfBuzz-LICENSE.txt")), "\"Old MIT\" license");
    }
}
