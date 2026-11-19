using MicroEngine.Graphics.Devices;

namespace MicroEngine.Cli.Tests;

/// <summary>
/// `microengine shaders`: the repository's own shaders (the SDL_GPU device's built-ins, the conformance suite's and the
/// samples') are committed as compiled .shader files, and compiling their HLSL again with this tool gives the same
/// content; a compilation error names the file; off Windows the command refuses with a message that says why.
/// </summary>
[TestClass]
public sealed class ShadersTests
{
    private static string Repository()
    {
        for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir is not null; dir = dir.Parent)
            if (File.Exists(Path.Combine(dir.FullName, "MicroEngine.slnx"))) return dir.FullName;
        throw new InvalidOperationException("MicroEngine.slnx not found above the test output");
    }

    /// <summary>Every folder of HLSL shaders in the repository and where its compiled files live.</summary>
    public static IEnumerable<object[]> ShaderFolders()
    {
        var root = Repository();
        foreach (var folder in Directory.GetDirectories(root, "Shaders", SearchOption.AllDirectories))
        {
            if (folder.Contains($"{Path.DirectorySeparatorChar}experiments{Path.DirectorySeparatorChar}") || folder.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}")
                || folder.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}") || !Directory.EnumerateFiles(folder, "*.hlsl").Any()) continue;
            var compiled = Path.Combine(folder, "compiled");
            yield return [Path.GetRelativePath(root, folder), Directory.Exists(compiled) ? Path.GetRelativePath(root, compiled) : Path.GetRelativePath(root, Path.Combine(folder, "..", "Content", "shaders"))];
        }
    }

    [TestMethod]
    public void TheRepositoryHasShaderFoldersToCheck()
    {
        var folders = ShaderFolders().Select(f => (string)f[0]).ToList();
        Assert.IsTrue(folders.Any(f => f.Contains("MicroEngine.Graphics.SdlGpu")), string.Join(", ", folders));
        Assert.IsTrue(folders.Any(f => f.Contains("Shared")), string.Join(", ", folders));
    }

    [TestMethod]
    [OSCondition(OperatingSystems.Windows)]
    [DynamicData(nameof(ShaderFolders))]
    public void CommittedShaders_AreWhatTheirHlslCompilesTo(string source, string output)
    {
        var root = Repository();
        var result = Shaders.Compile(Path.Combine(root, source), Path.Combine(root, output), check: true);
        Assert.IsEmpty(result.Written, $"out of date (run microengine shaders --source {source} --out {output}): {string.Join(", ", result.Written)}");
        Assert.IsNotEmpty(result.Unchanged);
        foreach (var file in result.Unchanged)
        {
            var shader = CompiledShader.Parse(File.ReadAllBytes(file));
            Assert.AreEqual(ShaderFormat.Dxil | ShaderFormat.SpirV | ShaderFormat.Msl, shader.Formats, file);
            Assert.IsTrue(shader.TryGetCode(ShaderFormat.Msl, out var msl) && msl.EntryPoint == "main0", "SPIRV-Cross names the MSL entry point main0");
        }
    }

    [TestMethod]
    [OSCondition(OperatingSystems.Windows)]
    public void Compile_ReflectsResourceCountsAndStage()
    {
        var root = Repository();
        var palette = Shaders.CompileFile(Path.Combine(root, "tests", "Shared", "Graphics", "Shaders", "palette.frag.hlsl"));
        Assert.AreEqual(ShaderStage.Fragment, palette.Stage);
        Assert.AreEqual(new ShaderResources(Samplers: 2), palette.Resources);
        var grayscale = Shaders.CompileFile(Path.Combine(root, "tests", "Shared", "Graphics", "Shaders", "grayscale.frag.hlsl"));
        Assert.AreEqual(new ShaderResources(Samplers: 1, UniformBuffers: 1), grayscale.Resources);
        var vertex = Shaders.CompileFile(Path.Combine(root, "tests", "Shared", "Graphics", "Shaders", "pixel.vert.hlsl"));
        Assert.AreEqual(ShaderStage.Vertex, vertex.Stage);
        Assert.AreEqual(new ShaderResources(UniformBuffers: 1), vertex.Resources);
    }

    [TestMethod]
    [OSCondition(OperatingSystems.Windows)]
    public void Compile_ReflectsTheNamesPlacesAndSizesOfEveryParameter()
    {
        var scratch = Directory.CreateTempSubdirectory("microengine-shaders-test-");
        try
        {
            // A block whose HLSL packing differs from std140: Direction (a float3) follows Time in the same 16 bytes.
            string file = Path.Combine(scratch.FullName, "named.frag.hlsl");
            File.WriteAllText(file, """
                Texture2D<float4> Scene : register(t0, space2);
                SamplerState SceneSampler : register(s0, space2);
                Texture2D<float4> Glow : register(t1, space2);
                SamplerState GlowSampler : register(s1, space2);
                cbuffer Look : register(b0, space3)
                {
                    float4 Tint;
                    float Time;
                    float3 Direction;
                    float2 Step;
                    float4x4 Colours;
                };
                struct Input { float2 TexCoord : TEXCOORD0; float4 Color : TEXCOORD1; };
                float4 main(Input input) : SV_Target0
                {
                    float4 c = Scene.Sample(SceneSampler, input.TexCoord + Step * Time) + Glow.Sample(GlowSampler, input.TexCoord) * Direction.x;
                    return mul(Colours, c) * Tint * input.Color;
                }

                """);
            var reflection = Shaders.CompileFile(file).Reflection;
            Assert.IsNotNull(reflection);
            Assert.HasCount(1, reflection.Blocks);
            var block = reflection.Blocks[0];
            Assert.AreEqual(("Look", 0, 112), (block.Name, block.Slot, block.Size), "the block's size is a multiple of 16, as Direct3D lays it out");
            CollectionAssert.AreEqual(new[]
            {
                new ShaderUniform("Tint", 0, 16), new ShaderUniform("Time", 16, 4), new ShaderUniform("Direction", 20, 12),
                new ShaderUniform("Step", 32, 8), new ShaderUniform("Colours", 48, 64),
            }, block.Uniforms.ToArray(), string.Join(", ", block.Uniforms));
            CollectionAssert.AreEqual(new[] { new ShaderTextureSlot("Scene", 0), new ShaderTextureSlot("Glow", 1) }, reflection.Textures.ToArray());
        }
        finally { scratch.Delete(recursive: true); }
    }

    [TestMethod]
    [OSCondition(OperatingSystems.Windows)]
    public void Compile_AnHlslError_FailsNamingTheFileAndWritesNothing()
    {
        var scratch = Directory.CreateTempSubdirectory("microengine-shaders-test-");
        try
        {
            File.WriteAllText(Path.Combine(scratch.FullName, "broken.frag.hlsl"), "float4 main() : SV_Target0 { return undefinedThing; }\n");
            var output = Path.Combine(scratch.FullName, "out");
            var e = Assert.ThrowsExactly<CliException>(() => Shaders.Compile(scratch.FullName, output, check: false));
            Assert.Contains("broken.frag.hlsl", e.Message);
            Assert.Contains("undefinedThing", e.Message);
            Assert.IsFalse(Directory.Exists(output) && Directory.EnumerateFiles(output).Any(), "nothing written");
        }
        finally { scratch.Delete(recursive: true); }
    }

    [TestMethod]
    [OSCondition(ConditionMode.Exclude, OperatingSystems.Windows)]
    public void Compile_OffWindows_RefusesAndSaysWhy()
    {
        var e = Assert.ThrowsExactly<CliException>(() => Shaders.Compile(".", "out", check: true));
        Assert.AreEqual(3, e.ExitCode);
        Assert.Contains("Windows", e.Message);
        Assert.Contains("Microsoft.Direct3D.DXC", e.Message);
    }
}
