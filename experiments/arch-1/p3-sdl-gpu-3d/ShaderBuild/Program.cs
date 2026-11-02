// ARCH-1 P3 offline shader build.
//
//   dotnet run --project ShaderBuild -- <shaders dir> <output dir>
//
// For every <name>.<vert|frag>.hlsl it writes <name>.<stage>.dxil (D3D12), .spv (Vulkan), .msl (Metal, generated
// from the SPIR-V by SPIRV-Cross, entry point "main0") and .json (resource counts SDL_CreateGPUShader needs). This is
// the same pipeline SDL_shadercross runs (DXC + SPIRV-Cross), done ahead of time so nothing compiles at run time.
using System.Diagnostics;
using System.Text;
using System.Text.Json;
using Silk.NET.SPIRV.Cross;

if (args.Length != 2)
{
    Console.Error.WriteLine("usage: ShaderBuild <shaders dir> <output dir>");
    return 2;
}

var source = Path.GetFullPath(args[0]);
var output = Path.GetFullPath(args[1]);
Directory.CreateDirectory(output);
var dxc = Path.Combine(AppContext.BaseDirectory, "dxc", "dxc.exe");
Console.WriteLine($"dxc: {Run(dxc, "--version").Trim()}");

var failures = 0;
foreach (var file in Directory.GetFiles(source, "*.hlsl").OrderBy(f => f, StringComparer.Ordinal))
{
    var name = Path.GetFileNameWithoutExtension(file); // e.g. mesh.vert
    var stage = name.EndsWith(".vert", StringComparison.Ordinal) ? "vs" : name.EndsWith(".frag", StringComparison.Ordinal) ? "ps" : null;
    if (stage is null) { Console.Error.WriteLine($"{name}: name must end in .vert or .frag"); failures++; continue; }
    var dxil = Path.Combine(output, name + ".dxil");
    var spv = Path.Combine(output, name + ".spv");
    try
    {
        Run(dxc, $"-T {stage}_6_0 -E main -Fo \"{dxil}\" \"{file}\"");
        Run(dxc, $"-T {stage}_6_0 -E main -spirv -fspv-target-env=vulkan1.0 -Fo \"{spv}\" \"{file}\"");
        var (msl, reflection) = CrossCompile(File.ReadAllBytes(spv));
        File.WriteAllText(Path.Combine(output, name + ".msl"), msl);
        File.WriteAllText(Path.Combine(output, name + ".json"), JsonSerializer.Serialize(reflection) + "\n");
        Console.WriteLine($"{name}: dxil {new FileInfo(dxil).Length} B, spv {new FileInfo(spv).Length} B, msl {msl.Length} B, {JsonSerializer.Serialize(reflection)}");
    }
    catch (Exception e)
    {
        Console.Error.WriteLine($"{name}: {e.Message}");
        failures++;
    }
}
return failures == 0 ? 0 : 1;

static string Run(string exe, string arguments)
{
    var info = new ProcessStartInfo(exe, arguments) { RedirectStandardOutput = true, RedirectStandardError = true };
    using var process = Process.Start(info)!;
    var stdout = process.StandardOutput.ReadToEnd();
    var stderr = process.StandardError.ReadToEnd();
    process.WaitForExit();
    if (process.ExitCode != 0) throw new InvalidOperationException($"dxc {arguments} failed ({process.ExitCode}): {stderr}{stdout}");
    return stdout + stderr;
}

static unsafe (string Msl, ShaderReflection Reflection) CrossCompile(byte[] spirv)
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
        var msl = Marshal(text);

        Resources* resources = null;
        Check(api, context, api.CompilerCreateShaderResources(compiler, &resources), "resources");
        var reflection = new ShaderReflection(
            Samplers: Count(api, context, resources, ResourceType.SeparateImage) + Count(api, context, resources, ResourceType.SampledImage),
            StorageTextures: Count(api, context, resources, ResourceType.StorageImage),
            StorageBuffers: Count(api, context, resources, ResourceType.StorageBuffer),
            UniformBuffers: Count(api, context, resources, ResourceType.UniformBuffer),
            Inputs: Count(api, context, resources, ResourceType.StageInput),
            Outputs: Count(api, context, resources, ResourceType.StageOutput));
        return (msl, reflection);
    }
    finally
    {
        api.ContextDestroy(context);
    }

    static int Count(Cross api, Context* context, Resources* resources, ResourceType type)
    {
        ReflectedResource* list = null;
        nuint count = 0;
        Check(api, context, api.ResourcesGetResourceListForType(resources, type, &list, &count), type.ToString());
        return (int)count;
    }

    static void Check(Cross api, Context* context, Result result, string what)
    {
        if (result == Result.Success) return;
        var detail = context == null ? "" : Marshal(api.ContextGetLastErrorString(context));
        throw new InvalidOperationException($"SPIRV-Cross {what}: {result} {detail}");
    }

    static string Marshal(byte* text)
    {
        var length = 0;
        while (text[length] != 0) length++;
        return Encoding.UTF8.GetString(text, length);
    }
}

/// <summary>What SDL_GPUShaderCreateInfo needs besides the code (shadercross emits the same counts as JSON).</summary>
internal sealed record ShaderReflection(int Samplers, int StorageTextures, int StorageBuffers, int UniformBuffers, int Inputs, int Outputs);
