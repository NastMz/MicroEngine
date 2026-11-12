namespace MicroEngine.Cli;

/// <summary>
/// <c>microengine doctor</c>: what this machine can build, and for anything missing, how to install it. It only looks;
/// it installs nothing and prints no secret (signing variables are reported as set or not set).
/// </summary>
public static class Doctor
{
    private sealed record Item(string Name, bool Ok, string Detail);

    /// <summary>The .NET the engine, the template and the tool's hosts target (net10.0).</summary>
    public const int FrameworkMajor = 10;

    /// <summary>
    /// The workload that builds browser packages of a net<paramref name="framework"/>.0 game with a .NET SDK of major
    /// version <paramref name="sdk"/>: <c>wasm-tools</c> serves the SDK's own framework only, so a newer SDK needs the
    /// framework's own (SDK 11 builds net10.0 games with <c>wasm-tools-net10</c>).
    /// </summary>
    public static string WebWorkload(int sdk, int framework = FrameworkMajor) => sdk > framework ? $"wasm-tools-net{framework}" : "wasm-tools";

    /// <summary>The major version of a .NET SDK version (11 for 11.0.100), or 0 when it is not one.</summary>
    public static int Major(string version) => int.TryParse(version.Trim().Split('.')[0], out int major) ? major : 0;

    public static bool Check(string? only)
    {
        PackageTarget? target = only is null ? null : PackageTarget.Find(only);
        string workloads = "";
        string sdk = "";
        try
        {
            // The SDK that builds games here (a global.json can choose it) and the workloads installed for it: each
            // SDK has its own.
            sdk = Tool.Run(Tool.Dotnet, ["--version"], what: "dotnet --version").Trim();
            workloads = Tool.Run(Tool.Dotnet, ["workload", "list"], what: "dotnet workload list");
        }
        catch (CliException) { }
        bool dotnet = Major(sdk) >= FrameworkMajor;
        string web = WebWorkload(Major(sdk));
        bool Workload(string name) => workloads.Split('\n').Any(l => l.TrimStart().StartsWith(name + " ", StringComparison.Ordinal));
        bool mac = OperatingSystem.IsMacOS();
        string? androidSdk = AndroidSdk();
        bool java = Environment.GetEnvironmentVariable("JAVA_HOME") is { Length: > 0 } j ? Directory.Exists(j) : Tool.Exists("java");
        bool Set(string name) => !string.IsNullOrEmpty(Environment.GetEnvironmentVariable(name));

        var groups = new List<(string Target, List<Item> Checks)>
        {
            ("all", [
                new(".NET SDK", dotnet, dotnet ? $"{sdk} (builds .NET {FrameworkMajor} games)"
                    : $"install the .NET {FrameworkMajor} SDK or a newer one from https://dotnet.microsoft.com/download (MICROENGINE_DOTNET can name a dotnet elsewhere)"),
                new("git", Tool.Exists("git"), Tool.Exists("git") ? "build-info records the commit" : "optional: packages then record the commit as unknown"),
            ]),
            ("windows", [new("Windows host", OperatingSystem.IsWindows(), OperatingSystem.IsWindows() ? "can write the executable's icon and version" : "Windows packages are built on Windows")]),
            ("linux", [new("any host", true, "Linux packages are cross-built from any system")]),
            ("macos", [
                new("macOS host", mac, mac ? "codesign available" : "macOS packages are built on a Mac"),
                new("Developer ID signing", Set("MICROENGINE_MACOS_SIGN_IDENTITY"), Set("MICROENGINE_MACOS_SIGN_IDENTITY") ? "MICROENGINE_MACOS_SIGN_IDENTITY is set" : "optional: without it the app is signed ad hoc and macOS asks players to allow it"),
            ]),
            ("android", [
                new("android workload", Workload("android"), Workload("android") ? "installed" : "dotnet workload install android"),
                new("Android SDK", androidSdk is not null, androidSdk ?? "install Android Studio or the command-line tools, then set ANDROID_HOME"),
                new("Java 17+", java, java ? "found" : "install a JDK 17 or later (for example Microsoft Build of OpenJDK) and set JAVA_HOME"),
                new("release signing key", Set("ANDROID_KEYSTORE"), Set("ANDROID_KEYSTORE") ? "ANDROID_KEYSTORE is set" : "needed for packages players install; --debug-key builds a test APK without it"),
            ]),
            ("ios", [
                new("macOS host with Xcode", mac && Directory.Exists("/Applications/Xcode.app") || mac && Tool.Exists("xcodebuild"), mac ? "xcodebuild" : "iOS apps are built on a Mac with Xcode"),
                new("ios workload", Workload("ios"), Workload("ios") ? "installed" : "dotnet workload install ios"),
                new("device signing", Set("MICROENGINE_IOS_CODESIGN_KEY") && Set("MICROENGINE_IOS_PROVISIONING_PROFILE"),
                    "MICROENGINE_IOS_CODESIGN_KEY and MICROENGINE_IOS_PROVISIONING_PROFILE from an Apple Developer account; ios-simulator needs neither"),
            ]),
            ("web", [new($"{web} workload", Workload(web), Workload(web) ? "installed" : $"dotnet workload install {web}")]),
        };
        string? wanted = target?.Name == "ios-simulator" ? "ios" : target?.Name;
        bool ok = true;
        foreach (var (name, checks) in groups.Where(g => wanted is null || g.Target == "all" || g.Target == wanted))
        {
            Console.WriteLine(name == "all" ? "Every target" : $"package {name}");
            foreach (var c in checks)
            {
                // Optional items (signing, git) never fail the check.
                bool optional = c.Name is "git" or "Developer ID signing" or "release signing key" or "device signing";
                Console.WriteLine($"  {(c.Ok ? "ok     " : optional ? "note   " : "missing")} {c.Name}: {c.Detail}");
                if (!c.Ok && !optional && (wanted is not null || name == "all")) ok = false;
            }
        }
        return ok;
    }

    /// <summary>The Android SDK: ANDROID_HOME, ANDROID_SDK_ROOT, or where Android Studio installs it.</summary>
    public static string? AndroidSdk()
    {
        string home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        string?[] candidates =
        [
            Environment.GetEnvironmentVariable("ANDROID_HOME"), Environment.GetEnvironmentVariable("ANDROID_SDK_ROOT"),
            OperatingSystem.IsWindows() ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Android", "Sdk") : null,
            OperatingSystem.IsMacOS() ? Path.Combine(home, "Library", "Android", "sdk") : Path.Combine(home, "Android", "Sdk"),
        ];
        return candidates.FirstOrDefault(c => !string.IsNullOrEmpty(c) && Directory.Exists(Path.Combine(c, "platform-tools")));
    }
}
