using System.Numerics;
using MicroEngine;
using MicroEngine.Core;
using Dev = MicroEngine.Graphics.Devices;

namespace Examples;

/// <summary>
/// One quad drawn with a custom pipeline, written against the graphics device alone: shaders, a pipeline for one blend
/// state and one target format, a vertex and an index buffer, the pixel-to-clip matrix, the uniforms and the textures.
/// </summary>
internal sealed class RawShaderPass : IDisposable
{
    private readonly Dev.IGraphicsDevice _device;
    private readonly Dev.Shader _vertex, _fragment;
    private readonly Dev.Pipeline _pipeline;
    private readonly Dev.GpuBuffer _vertices, _indices;

    public RawShaderPass(Dev.IGraphicsDevice device, AssetCache assets, string fragment, Dev.BlendState blend)
    {
        _device = device;
        _vertex = Dev.CompiledShader.Parse(assets.Bytes("shaders/sprite.vert.shader")).Create(device);
        _fragment = Dev.CompiledShader.Parse(assets.Bytes($"shaders/{fragment}.frag.shader")).Create(device);
        _pipeline = device.CreatePipeline(new Dev.PipelineDescription(_vertex, _fragment, Dev.VertexLayout.Vertex2D, blend,
            Dev.DepthState.None, new Dev.RasterState(), Dev.TextureFormat.Rgba8Unorm, null));
        _vertices = device.CreateBuffer(new Dev.BufferDescription(4 * Dev.Vertex2D.SizeInBytes, Dev.BufferUsage.Vertex));
        _indices = device.CreateBuffer(new Dev.BufferDescription(8 * sizeof(ushort), Dev.BufferUsage.Index));
    }

    /// <summary>Draws <paramref name="uv"/> (texture coordinates) of the textures over <paramref name="area"/> of the target.</summary>
    public void Draw<T>(Dev.RenderTarget target, RectF area, RectF uv, in T uniforms, params Dev.Texture[] textures) where T : unmanaged
    {
        var list = _device.BeginCommands();
        list.UpdateBuffer<Dev.Vertex2D>(_vertices, 0,
        [
            new(new Vector2(area.X, area.Y), new Vector2(uv.X, uv.Y), ColorF.White),
            new(new Vector2(area.X + area.Width, area.Y), new Vector2(uv.X + uv.Width, uv.Y), ColorF.White),
            new(new Vector2(area.X + area.Width, area.Y + area.Height), new Vector2(uv.X + uv.Width, uv.Y + uv.Height), ColorF.White),
            new(new Vector2(area.X, area.Y + area.Height), new Vector2(uv.X, uv.Y + uv.Height), ColorF.White),
        ]);
        list.UpdateBuffer<ushort>(_indices, 0, [0, 1, 2, 0, 2, 3, 0, 0]);
        list.BeginPass(new Dev.PassDescription(new Dev.ColorAttachment(target)));
        list.SetPipeline(_pipeline);
        list.SetUniforms(Dev.ShaderStage.Vertex, 0, Matrix4x4.CreateOrthographicOffCenter(0, target.Width, target.Height, 0, 0, 1));
        list.SetUniforms(Dev.ShaderStage.Fragment, 0, uniforms);
        for (int slot = 0; slot < textures.Length; slot++)
            list.SetTexture(slot, textures[slot], _device.GetSampler(Dev.SamplerDescription.LinearClamp));
        list.SetVertices(_vertices);
        list.SetIndices(_indices);
        list.DrawIndexed(6);
        list.EndPass();
        _device.Submit(list);
    }

    public void Dispose()
    {
        _indices.Dispose();
        _vertices.Dispose();
        _pipeline.Dispose();
        _fragment.Dispose();
        _vertex.Dispose();
    }
}
