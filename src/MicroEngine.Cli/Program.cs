using MicroEngine.Cli;

// `microengine`: the SDK's command-line tool (docs/guide/cli.md). Every command drives the .NET SDK and the systems'
// own tools; nothing here depends on MicroEngine's repository or its CI.
return await Cli.RunAsync(args);

namespace MicroEngine.Cli
{
    public static class Cli
    {
        private const string Usage = """
            microengine — create, run, build and package MicroEngine games.

            Usage: microengine <command> [options]

            Commands:
              new <Name>            create a game from the official template (dotnet new microengine-game)
              run                   build and run the game in this folder
              build                 compile the game and check its identity
              package <target>      build a package for players: windows, linux, macos, android, ios, ios-simulator, web
              info                  show the game's identity and what each target needs from it
              doctor [target]       check this machine's tools for every target (or one)
              shaders               compile the game's HLSL shaders ahead of time (Shaders/ to Content/shaders/)
              version               print the tool's version

            Common options:
              --project <path>      the game's .csproj or its folder (default: the .csproj in this folder)
              --verbose             show every command the tool runs and its full output

            'microengine <command> --help' describes a command. Documentation: https://github.com/NastMz/MicroEngine/blob/main/docs/guide/cli.md
            """;

        private static readonly Dictionary<string, string> Help = new()
        {
            ["new"] = """
                Usage: microengine new <Name> [--output DIR] [--id ID] [--title TITLE]

                Creates a game project from the MicroEngine.Templates package (installed with 'dotnet new install' at this
                tool's version when missing). <Name> becomes the project and C# namespace; --id sets ApplicationId
                (default com.example.<name>), --title the name players see.
                """,
            ["run"] = """
                Usage: microengine run [--project P] [-c Debug|Release] [--platform sdl|raylib|sdl-software|headless] [-- game options]

                Builds and runs the game on this computer. Options after -- go to the game (for example -- --scale 4).
                """,
            ["build"] = """
                Usage: microengine build [--project P] [-c Release|Debug]

                Compiles the game. The build checks its identity (ApplicationId, MicroEngineIcon, MicroEngineOrientation)
                and gives the Windows executable its icon.
                """,
            ["package"] = """
                Usage: microengine package <target> [--project P] [--out DIR] [options]

                Targets (where they can be built):
                  windows        Windows x64 ZIP: single-file executable, SDL 3, content, notices     (on Windows)
                  linux          Linux x64 tar.gz, glibc 2.34+, with a .desktop entry and icon        (anywhere)
                  macos          macOS arm64 .app in a ZIP, signed ad hoc or with your Developer ID   (on a Mac)
                  android        APK for arm64-v8a and x86_64, signed with your key                   (Android SDK)
                  ios            .ipa for iPhone and iPad, signed with your Apple certificate        (on a Mac)
                  ios-simulator  .app for Xcode's Simulator only; not installable on a device        (on a Mac)
                  web            static WebAssembly site (folder and ZIP) for any web server          (wasm-tools)

                Options:
                  --out DIR          where packages go (default: dist next to the project)
                  --debug-key        android: sign with this machine's debug key (test builds only)
                  --version-code N   android, ios: the build number stores compare (default: minutes since 2026-01-01 at the commit)
                  --dev              web: a development build that accepts input scripts and URL parameters
                  --raylib-wasm F    web: the raylib.a to link (default: the one in the MicroEngine.Raylib package)
                  --key LABEL        a label recorded in build-info.json (MicroEngine's release pipeline uses content keys)

                Signing reads only environment variables (docs/guide/signing.md): ANDROID_KEYSTORE, ANDROID_KEYSTORE_PASSWORD,
                ANDROID_KEY_ALIAS, ANDROID_KEY_PASSWORD; MICROENGINE_MACOS_SIGN_IDENTITY, MICROENGINE_MACOS_NOTARY_PROFILE;
                MICROENGINE_IOS_CODESIGN_KEY, MICROENGINE_IOS_PROVISIONING_PROFILE.
                """,
            ["info"] = "Usage: microengine info [--project P]\n\nShows the identity the project declares and whether each target can use it.",
            ["doctor"] = "Usage: microengine doctor [target]\n\nChecks the tools each target needs on this machine and says how to install what is missing.",
            ["shaders"] = """
                Usage: microengine shaders [--project P] [--source DIR] [--out DIR] [--check]

                Compiles every *.vert.hlsl and *.frag.hlsl in the game's Shaders folder (or --source) into .shader files in
                Content/shaders (or --out): DXIL for Direct3D 12 and SPIR-V for Vulkan (DXC), MSL for Metal (SPIRV-Cross),
                and the resource counts the device needs. Entry point main, shader model 6.0, SDL_GPU register spaces
                (vertex uniforms in space1; fragment textures and samplers in space2, uniforms in space3). Commit the
                .shader files: games load them on every platform and never compile at run time.

                --check   write nothing; fail (exit code 1) when a .shader file is missing or differs from its source

                Needs Windows: DXC comes from Microsoft's Microsoft.Direct3D.DXC package (its own licence terms), which
                has Windows binaries only. The tool restores that package into the NuGet packages folder on first use;
                MICROENGINE_DXC names another dxc.exe.
                """,
        };

        public static async Task<int> RunAsync(string[] args)
        {
            if (args.Length == 0 || args[0] is "help" or "--help" or "-h" or "-?")
            {
                Console.WriteLine(args.Length > 1 && Help.TryGetValue(args[1], out var h) ? h : Usage);
                return 0;
            }
            if (args[0] is "version" or "--version")
            {
                Console.WriteLine(Tool.Version);
                return 0;
            }
            string command = args[0];
            var rest = args.Skip(1).ToList();
            if (rest.Contains("--help") || rest.Contains("-h"))
            {
                Console.WriteLine(Help.TryGetValue(command, out var h) ? h : Usage);
                return Help.ContainsKey(command) ? 0 : 2;
            }
            try
            {
                var options = Options.Parse(rest, "verbose", "debug-key", "dev", "check");
                Tool.Verbose = options.Has("verbose");
                return command switch
                {
                    "new" => New(options),
                    "run" => Run(options),
                    "build" => Build(options),
                    "package" => await Package(options),
                    "info" => Info(options),
                    "doctor" => Doctor.Check(options.Positional.FirstOrDefault()) ? 0 : 1,
                    "shaders" => CompileShaders(options),
                    _ => throw new CliException($"unknown command '{command}'. Run 'microengine --help' for the list.", 2),
                };
            }
            catch (CliException e)
            {
                Console.Error.WriteLine($"error: {e.Message}");
                return e.ExitCode;
            }
        }

        private static int New(Options o)
        {
            string name = o.Positional.FirstOrDefault() ?? throw new CliException("give the game a name: microengine new <Name>.", 2);
            if (!System.Text.RegularExpressions.Regex.IsMatch(name, "^[A-Za-z][A-Za-z0-9]*$"))
                throw new CliException($"'{name}' is not a valid game name: use letters and digits, starting with a letter (it becomes the project, the executable and the C# namespace).", 2);
            string output = o.Get("output") ?? name;
            string id = o.Get("id") ?? $"com.example.{name.ToLowerInvariant()}";
            if (!GameProject.IsValidId(id)) throw new CliException($"'{id}' is not a valid application id (for example com.example.{name.ToLowerInvariant()}).", 2);
            string? title = o.Get("title");
            o.RejectUnknown("new");
            if (Directory.Exists(output) && Directory.EnumerateFileSystemEntries(output).Any()) throw new CliException($"{Path.GetFullPath(output)} already exists and is not empty.", 2);
            // `dotnet new list` fails (exit code 103) when no template matches.
            bool installed;
            try { installed = Tool.Run(Tool.Dotnet, ["new", "list", "microengine-game"], what: "dotnet new list").Contains("microengine-game", StringComparison.Ordinal); }
            catch (CliException) { installed = false; }
            if (!installed)
            {
                Console.WriteLine($"Installing the MicroEngine.Templates {Tool.Version} template package...");
                Tool.Run(Tool.Dotnet, ["new", "install", $"MicroEngine.Templates::{Tool.Version}"], what: "dotnet new install MicroEngine.Templates");
            }
            var args = new List<string> { "new", "microengine-game", "-n", name, "-o", output, "--app-id", id };
            if (title is not null) args.AddRange(["--title", title]);
            Tool.Run(Tool.Dotnet, args, what: "dotnet new microengine-game");
            Console.WriteLine($"Created {name} in {Path.GetFullPath(output)}.\nNext:\n  cd {output}\n  microengine run");
            return 0;
        }

        private static int Run(Options o)
        {
            string project = GameProject.Locate(o.Get("project"));
            string configuration = o.Get("configuration") ?? "Debug";
            string? platform = o.Get("platform");
            o.RejectUnknown("run");
            var args = new List<string> { "run", "--project", project, "-c", configuration, "--" };
            if (platform is not null) args.AddRange(["--platform", platform]);
            args.AddRange(o.PassThrough);
            return Tool.Attach(Tool.Dotnet, args);
        }

        private static int Build(Options o)
        {
            string project = GameProject.Locate(o.Get("project"));
            string configuration = o.Get("configuration") ?? "Release";
            o.RejectUnknown("build");
            var game = GameProject.Load(project, configuration);
            game.CheckIcon();
            Tool.Run(Tool.Dotnet, ["build", project, "-c", configuration, "-nologo"], game.Directory, "dotnet build");
            Console.WriteLine($"Built {game.Title} {game.Version} ({configuration}): {GameProject.Load(project, configuration).TargetDir}");
            return 0;
        }

        private static int Info(Options o)
        {
            var game = GameProject.Load(GameProject.Locate(o.Get("project")));
            o.RejectUnknown("info");
            string? iconProblem = null;
            try { game.CheckIcon(); } catch (CliException e) { iconProblem = e.Message; }
            Console.WriteLine($"""
                Project      {game.Path}
                Title        {game.Title}
                Id           {game.ApplicationId ?? "(none: ApplicationId is needed for macos, android, ios)"}
                Version      {game.Version}
                Description  {(game.Description.Length > 0 ? game.Description : "(none)")}
                Icon         {(game.Icon is null ? "(none: MicroEngineIcon)" : iconProblem ?? game.Icon)}
                Orientation  {game.Orientation}
                Entry point  {game.DescribeEntryPoint()}
                Content      {game.ContentDir ?? "(no Content folder)"}
                Licence      {game.LicenseFile ?? "(none)"}
                Engine       MicroEngine {game.EngineVersion}, .NET {game.DotnetVersion}
                """);
            if (game.ApplicationId is { } id && !GameProject.IsValidId(id)) Console.WriteLine($"Problem      ApplicationId '{id}' is not valid.");
            return iconProblem is null ? 0 : 1;
        }

        private static int CompileShaders(Options o)
        {
            string? sourceOption = o.Get("source"), outOption = o.Get("out"), projectOption = o.Get("project");
            bool check = o.Has("check");
            o.RejectUnknown("shaders");
            string source, output;
            if (sourceOption is not null && outOption is not null && projectOption is null) (source, output) = (Path.GetFullPath(sourceOption), Path.GetFullPath(outOption));
            else
            {
                string directory = Path.GetDirectoryName(GameProject.Locate(projectOption))!;
                source = Path.GetFullPath(sourceOption ?? Path.Combine(directory, "Shaders"));
                output = Path.GetFullPath(outOption ?? Path.Combine(directory, "Content", "shaders"));
            }
            var result = Shaders.Compile(source, output, check);
            foreach (var file in result.Written) Console.WriteLine(check ? $"out of date: {file}" : $"wrote {file}");
            Console.WriteLine($"shaders: {result.Written.Count} {(check ? "out of date" : "written")}, {result.Unchanged.Count} unchanged ({Shaders.DxcVersion.Split('\n')[0].Trim()})");
            return check && result.Written.Count > 0 ? 1 : 0;
        }

        private static async Task<int> Package(Options o)
        {
            var target = PackageTarget.Find(o.Positional.FirstOrDefault() ?? throw new CliException("choose a target: microengine package <windows|linux|macos|android|ios|ios-simulator|web>.", 2));
            var game = GameProject.Load(GameProject.Locate(o.Get("project")));
            var request = new PackageRequest(game, target, Path.GetFullPath(o.Get("out") ?? Path.Combine(game.Directory, "dist")))
            {
                Key = o.Get("key") ?? "local",
                DebugKey = o.Has("debug-key"),
                Dev = o.Has("dev"),
                RaylibWasm = o.Get("raylib-wasm") is { } r ? Path.GetFullPath(r) : null,
            };
            string? code = o.Get("version-code");
            o.RejectUnknown("package");
            int versionCode = code is null ? VersionCode(game.Directory)
                : int.TryParse(code, out var n) && n is > 0 and <= 2100000000 ? n : throw new CliException("--version-code must be a whole number from 1 to 2100000000.", 2);
            string file = target.Name switch
            {
                "android" => await Hosts.Android(request, versionCode),
                "ios" or "ios-simulator" => await Hosts.Ios(request, versionCode),
                "web" => await Hosts.Web(request),
                _ => await Packaging.Desktop(request),
            };
            Console.WriteLine($"package: {file}");
            return 0;
        }

        /// <summary>
        /// The build number Android and Apple compare to accept an update: minutes from 2026-01-01 to the commit being
        /// built (or to now outside Git), so every later build installs over an earlier one.
        /// </summary>
        public static int VersionCode(string dir)
        {
            long seconds;
            try { seconds = long.Parse(Tool.Run("git", ["show", "-s", "--format=%ct", "HEAD"], dir, "git").Trim(), System.Globalization.CultureInfo.InvariantCulture); }
            catch (Exception e) when (e is CliException or FormatException) { seconds = DateTimeOffset.UtcNow.ToUnixTimeSeconds(); }
            long minutes = (seconds - new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero).ToUnixTimeSeconds()) / 60;
            return (int)Math.Clamp(minutes, 1, 2100000000);
        }
    }
}
