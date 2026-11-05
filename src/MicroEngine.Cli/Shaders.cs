using System.Diagnostics;
using System.Text;
using MicroEngine.Graphics.Devices;
using Silk.NET.SPIRV.Cross;

namespace MicroEngine.Cli;

/// <summary>
/// `microengine shaders`: compiles HLSL ahead of time into the <see cref="CompiledShader"/> files games ship. One HLSL
/// source per stage (<c>name.vert.hlsl</c>, <c>name.frag.hlsl</c>, entry point <c>main</c>, shader model 6.0, SDL_GPU's
/// register spaces) becomes <c>name.vert.shader</c> with DXIL and SPIR-V from DXC and MSL from SPIRV-Cross, plus the
/// resource counts SPIRV-Cross reflects. DXC comes from the Microsoft.Direct3D.DXC package, which carries Windows
/// binaries only, so compiling needs Windows; the files it writes load on every platform.
/// </summary>
public static class Shaders
{
    /// <summary>What compiling one folder did.</summary>
    /// <param name="Written">Files written (or, with check, files that would change).</param>
    /// <param name="Unchanged">Files whose bytes were already identical.</param>
    public readonly record struct BuildResult(IReadOnlyList<string> Written, IReadOnlyList<string> Unchanged);

    /// <summary>The DXC version the tool runs, e.g. "dxcompiler.dll: 1.8 - 1.8.2505.32".</summary>
    public static string DxcVersion => Dxc("--version").Trim();

    /// <summary>
    /// Compiles every <c>*.vert.hlsl</c> and <c>*.frag.hlsl</c> directly in <paramref name="source"/> into
    /// <paramref name="output"/>. With <paramref name="check"/> nothing is written and <see cref="BuildResult.Written"/> lists the
    /// files that are missing or differ.
    /// </summary>
    /// <exception cref="CliException">Not on Windows, no HLSL file, or a compilation error (the message is DXC's).</exception>
    public static BuildResult Compile(string source, string output, bool check)
    {
        if (!OperatingSystem.IsWindows())
            throw new CliException("compiling shaders needs Windows: DXC comes from the Microsoft.Direct3D.DXC package, which carries Windows binaries only. Compile on Windows and commit the .shader files; games load them on every platform.", 3);
        if (!Directory.Exists(source)) throw new CliException($"no shader folder {source}.", 2);
        var files = Directory.GetFiles(source, "*.hlsl").Where(f => StageOf(f) is not null).Order(StringComparer.Ordinal).ToList();
        if (files.Count == 0) throw new CliException($"no *.vert.hlsl or *.frag.hlsl file in {source}.", 2);
        var written = new List<string>();
        var unchanged = new List<string>();
        if (!check) Directory.CreateDirectory(output);
        foreach (var file in files)
        {
            var shader = CompileFile(file);
            string target = Path.Combine(output, Path.GetFileNameWithoutExtension(file) + CompiledShader.FileExtension);
            byte[] bytes = shader.ToBytes();
            if (Same(target, bytes)) { unchanged.Add(target); continue; }
            if (!check) File.WriteAllBytes(target, bytes);
            written.Add(target);
        }
        return new BuildResult(written, unchanged);
    }

    /// <summary>Compiles one HLSL file (its name ends in <c>.vert.hlsl</c> or <c>.frag.hlsl</c>).</summary>
    public static CompiledShader CompileFile(string file)
    {
        var stage = StageOf(file) ?? throw new CliException($"{Path.GetFileName(file)}: a shader's name ends in .vert.hlsl or .frag.hlsl.", 2);
        string profile = stage == ShaderStage.Vertex ? "vs_6_0" : "ps_6_0";
        string scratch = Path.Combine(Path.GetTempPath(), "microengine-shaders-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(scratch);
        try
        {
            string dxil = Path.Combine(scratch, "out.dxil"), spirv = Path.Combine(scratch, "out.spv");
            // The source is passed by its file name from its own folder, so no machine path ends up in the output.
            string directory = Path.GetDirectoryName(Path.GetFullPath(file))!, name = Path.GetFileName(file);
            Dxc($"-T {profile} -E main -Qstrip_debug -Fo \"{dxil}\" \"{name}\"", directory);
            Dxc($"-T {profile} -E main -spirv -fspv-target-env=vulkan1.0 -Fo \"{spirv}\" \"{name}\"", directory);
            var spirvBytes = File.ReadAllBytes(spirv);
            var (msl, resources) = CrossCompile(spirvBytes);
            var code = new Dictionary<ShaderFormat, ShaderCode>
            {
                [ShaderFormat.Dxil] = new(File.ReadAllBytes(dxil), "main"),
                [ShaderFormat.SpirV] = new(spirvBytes, "main"),
                [ShaderFormat.Msl] = new(Encoding.UTF8.GetBytes(msl), "main0"),
            };
            return new CompiledShader(stage, resources, code, name);
        }
        finally { Directory.Delete(scratch, recursive: true); }
    }

    /// <summary>
    /// Whether <paramref name="file"/> holds this compilation: compared through its parsed content, so a checkout that
    /// changed the JSON's line endings (Git's autocrlf) still counts as up to date.
    /// </summary>
    private static bool Same(string file, byte[] compiled)
    {
        if (!File.Exists(file)) return false;
        try { return CompiledShader.Parse(File.ReadAllBytes(file)).ToBytes().AsSpan().SequenceEqual(compiled); }
        catch (InvalidDataException) { return false; }
    }

    private static ShaderStage? StageOf(string file) => Path.GetFileName(file) switch
    {
        var n when n.EndsWith(".vert.hlsl", StringComparison.OrdinalIgnoreCase) => ShaderStage.Vertex,
        var n when n.EndsWith(".frag.hlsl", StringComparison.OrdinalIgnoreCase) => ShaderStage.Fragment,
        _ => null,
    };

    /// <summary>The Microsoft.Direct3D.DXC version the tool uses (DXC 1.8, May 2025).</summary>
    public const string DxcPackageVersion = "1.8.2505.32";

    private static string? _dxc;

    /// <summary>
    /// Where dxc.exe is: <c>MICROENGINE_DXC</c> when set, otherwise the Microsoft.Direct3D.DXC package of
    /// <see cref="DxcPackageVersion"/> in the NuGet packages folder, restored there first (by <c>dotnet restore</c> of a
    /// throwaway project) when it is missing. The tool never ships DXC itself.
    /// </summary>
    public static string LocateDxc()
    {
        if (_dxc is not null) return _dxc;
        if (Environment.GetEnvironmentVariable("MICROENGINE_DXC") is { Length: > 0 } custom)
            return _dxc = File.Exists(custom) ? custom : throw new CliException($"MICROENGINE_DXC names {custom}, which does not exist.", 3);
        string arch = System.Runtime.InteropServices.RuntimeInformation.OSArchitecture == System.Runtime.InteropServices.Architecture.Arm64 ? "arm64" : "x64";
        string packages = NuGetPackagesFolder();
        string exe = Path.Combine(packages, "microsoft.direct3d.dxc", DxcPackageVersion, "build", "native", "bin", arch, "dxc.exe");
        if (!File.Exists(exe))
        {
            Console.WriteLine($"Restoring Microsoft.Direct3D.DXC {DxcPackageVersion} (Microsoft's shader compiler, under its own licence terms) into {packages}...");
            string scratch = Path.Combine(Path.GetTempPath(), "microengine-dxc-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(scratch);
            try
            {
                File.WriteAllText(Path.Combine(scratch, "Dxc.csproj"), $"""
                    <Project Sdk="Microsoft.NET.Sdk">
                      <PropertyGroup><TargetFramework>netstandard2.0</TargetFramework></PropertyGroup>
                      <ItemGroup><PackageReference Include="Microsoft.Direct3D.DXC" Version="[{DxcPackageVersion}]" ExcludeAssets="all" /></ItemGroup>
                    </Project>
                    """);
                Tool.Run(Tool.Dotnet, ["restore", "--nologo"], scratch, "dotnet restore (Microsoft.Direct3D.DXC)");
            }
            finally { Directory.Delete(scratch, recursive: true); }
            if (!File.Exists(exe)) throw new CliException($"Microsoft.Direct3D.DXC {DxcPackageVersion} was restored but {exe} is missing.", 3);
        }
        return _dxc = exe;
    }

    private static string NuGetPackagesFolder()
    {
        if (Environment.GetEnvironmentVariable("NUGET_PACKAGES") is { Length: > 0 } configured) return configured;
        var line = Tool.Run(Tool.Dotnet, ["nuget", "locals", "global-packages", "--list"], what: "dotnet nuget locals").Trim();
        int colon = line.IndexOf(':');
        return colon >= 0 ? line[(colon + 1)..].Trim() : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".nuget", "packages");
    }

    private static string Dxc(string arguments, string? directory = null)
    {
        string exe = LocateDxc();
        var info = new ProcessStartInfo(exe, arguments) { RedirectStandardOutput = true, RedirectStandardError = true, WorkingDirectory = directory ?? Environment.CurrentDirectory };
        using var process = Process.Start(info) ?? throw new CliException("DXC could not start.", 3);
        var stdout = process.StandardOutput.ReadToEndAsync();
        var stderr = process.StandardError.ReadToEnd();
        process.WaitForExit();
        if (process.ExitCode != 0) throw new CliException($"dxc {arguments}:\n{stderr}{stdout.Result}".TrimEnd(), 1);
        return stdout.Result + stderr;
    }

    private static unsafe (string Msl, ShaderResources Resources) CrossCompile(byte[] spirv)
    {
        var api = Cross.GetApi();
        Context* context = null;
        Check(api, null, api.ContextCreate(&context), "context");
        try
        {
            ParsedIr* ir = null;
            fixed (byte* words = spirv)
                Check(api, context, api.ContextParseSpirv(context, (uint*)words, (nuint)(spirv.Length / 4), &ir), "parse");
            Compiler* compiler = null;
            Check(api, context, api.ContextCreateCompiler(context, Backend.Msl, ir, CaptureMode.TakeOwnership, &compiler), "compiler");
            CompilerOptions* options = null;
            Check(api, context, api.CompilerCreateCompilerOptions(compiler, &options), "options");
            Check(api, context, api.CompilerOptionsSetUint(options, CompilerOption.MslVersion, 2 * 10000 + 1 * 100), "msl version");
            Check(api, context, api.CompilerInstallCompilerOptions(compiler, options), "install options");
            byte* text = null;
            Check(api, context, api.CompilerCompile(compiler, &text), "compile");
            var msl = Utf8(text);

            Resources* resources = null;
            Check(api, context, api.CompilerCreateShaderResources(compiler, &resources), "resources");
            var counts = new ShaderResources(
                Samplers: Count(api, context, resources, ResourceType.SeparateImage) + Count(api, context, resources, ResourceType.SampledImage),
                UniformBuffers: Count(api, context, resources, ResourceType.UniformBuffer),
                StorageTextures: Count(api, context, resources, ResourceType.StorageImage),
                StorageBuffers: Count(api, context, resources, ResourceType.StorageBuffer));
            return (msl, counts);
        }
        finally { api.ContextDestroy(context); }

        static int Count(Cross api, Context* context, Resources* resources, ResourceType type)
        {
            ReflectedResource* list = null;
            nuint count = 0;
            Check(api, context, api.ResourcesGetResourceListForType(resources, type, &list, &count), type.ToString());
            return (int)count;
        }

        static void Check(Cross api, Context* context, Silk.NET.SPIRV.Cross.Result result, string what)
        {
            if (result == Silk.NET.SPIRV.Cross.Result.Success) return;
            var detail = context == null ? "" : Utf8(api.ContextGetLastErrorString(context));
            throw new CliException($"SPIRV-Cross {what}: {result} {detail}", 1);
        }

        static string Utf8(byte* text)
        {
            var length = 0;
            while (text[length] != 0) length++;
            return Encoding.UTF8.GetString(text, length);
        }
    }
}
