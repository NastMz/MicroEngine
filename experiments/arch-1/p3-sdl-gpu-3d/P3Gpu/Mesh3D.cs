using System.Numerics;
using SDL;
using static P3Gpu.Gpu;
using static SDL.SDL3;

namespace P3Gpu;

/// <summary>
/// The 3D half: a depth-tested, back-face-culled mesh pipeline, one vertex buffer per object, an RGBA8 colour target
/// and a depth target. Records one render pass per call into a caller-owned command buffer.
/// </summary>
public sealed unsafe class Mesh3D : IDisposable
{
    private readonly SDL_GPUDevice* _device;
    private readonly SDL_GPUGraphicsPipeline* _pipeline;
    private readonly SDL_GPUBuffer*[] _vertexBuffers;
    private readonly SDL_GPUBuffer* _indices;
    private readonly SDL_GPUBuffer* _probes;
    private readonly SDL_GPUTexture* _depth;

    public SDL_GPUTexture* Color { get; }
    public SDL_GPUTextureFormat DepthFormat { get; }
    public string DepthSupport { get; }

    public Mesh3D(SDL_GPUDevice* device)
    {
        _device = device;
        (DepthFormat, DepthSupport) = PickDepthFormat(device);
        Color = CreateTarget(device, SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_R8G8B8A8_UNORM,
            SDL_GPUTextureUsageFlags.SDL_GPU_TEXTUREUSAGE_COLOR_TARGET | SDL_GPUTextureUsageFlags.SDL_GPU_TEXTUREUSAGE_SAMPLER, Scene.Width, Scene.Height);
        _depth = CreateTarget(device, DepthFormat, SDL_GPUTextureUsageFlags.SDL_GPU_TEXTUREUSAGE_DEPTH_STENCIL_TARGET, Scene.Width, Scene.Height);

        var vertex = LoadShader(device, "mesh.vert");
        var fragment = LoadShader(device, "color.frag");
        var bufferDescription = new SDL_GPUVertexBufferDescription { slot = 0, pitch = 28, input_rate = SDL_GPUVertexInputRate.SDL_GPU_VERTEXINPUTRATE_VERTEX };
        var attributes = stackalloc SDL_GPUVertexAttribute[2];
        attributes[0] = new SDL_GPUVertexAttribute { location = 0, buffer_slot = 0, format = SDL_GPUVertexElementFormat.SDL_GPU_VERTEXELEMENTFORMAT_FLOAT3, offset = 0 };
        attributes[1] = new SDL_GPUVertexAttribute { location = 1, buffer_slot = 0, format = SDL_GPUVertexElementFormat.SDL_GPU_VERTEXELEMENTFORMAT_FLOAT4, offset = 12 };
        var colorTarget = new SDL_GPUColorTargetDescription { format = SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_R8G8B8A8_UNORM };
        var info = new SDL_GPUGraphicsPipelineCreateInfo
        {
            vertex_shader = vertex,
            fragment_shader = fragment,
            vertex_input_state = new SDL_GPUVertexInputState
            {
                vertex_buffer_descriptions = &bufferDescription, num_vertex_buffers = 1,
                vertex_attributes = attributes, num_vertex_attributes = 2,
            },
            primitive_type = SDL_GPUPrimitiveType.SDL_GPU_PRIMITIVETYPE_TRIANGLELIST,
            rasterizer_state = new SDL_GPURasterizerState
            {
                fill_mode = SDL_GPUFillMode.SDL_GPU_FILLMODE_FILL,
                cull_mode = SDL_GPUCullMode.SDL_GPU_CULLMODE_BACK,
                front_face = SDL_GPUFrontFace.SDL_GPU_FRONTFACE_COUNTER_CLOCKWISE,
                enable_depth_clip = true,
            },
            depth_stencil_state = new SDL_GPUDepthStencilState
            {
                compare_op = SDL_GPUCompareOp.SDL_GPU_COMPAREOP_LESS, enable_depth_test = true, enable_depth_write = true,
            },
            target_info = new SDL_GPUGraphicsPipelineTargetInfo
            {
                color_target_descriptions = &colorTarget, num_color_targets = 1,
                depth_stencil_format = DepthFormat, has_depth_stencil_target = true,
            },
        };
        _pipeline = NotNull(SDL_CreateGPUGraphicsPipeline(device, &info), "SDL_CreateGPUGraphicsPipeline mesh");
        SDL_ReleaseGPUShader(device, vertex); // a pipeline keeps what it needs
        SDL_ReleaseGPUShader(device, fragment);

        _vertexBuffers = new SDL_GPUBuffer*[Scene.Objects.Length];
        for (var i = 0; i < Scene.Objects.Length; i++)
            _vertexBuffers[i] = CreateBuffer<MeshVertex>(device, SDL_GPUBufferUsageFlags.SDL_GPU_BUFFERUSAGE_VERTEX, Scene.Objects[i].Vertices);
        _indices = CreateBuffer<ushort>(device, SDL_GPUBufferUsageFlags.SDL_GPU_BUFFERUSAGE_INDEX, Scene.CubeIndices);
        _probes = CreateBuffer<MeshVertex>(device, SDL_GPUBufferUsageFlags.SDL_GPU_BUFFERUSAGE_VERTEX, Scene.Probes);
    }

    /// <summary>Clears colour and depth, draws the probes and the cubes (near first) with a camera at <paramref name="angle"/>.</summary>
    public void Record(SDL_GPUCommandBuffer* cmd, float angle) => Record(cmd, angle, Scene.Clear, drawScene: true);

    public void Record(SDL_GPUCommandBuffer* cmd, float angle, Vector4 clear, bool drawScene, bool cycle = false)
    {
        var colorInfo = new SDL_GPUColorTargetInfo
        {
            texture = Color,
            cycle = cycle,
            clear_color = new SDL_FColor { r = clear.X, g = clear.Y, b = clear.Z, a = clear.W },
            load_op = SDL_GPULoadOp.SDL_GPU_LOADOP_CLEAR, store_op = SDL_GPUStoreOp.SDL_GPU_STOREOP_STORE,
        };
        var depthInfo = new SDL_GPUDepthStencilTargetInfo
        {
            texture = _depth, clear_depth = 1, load_op = SDL_GPULoadOp.SDL_GPU_LOADOP_CLEAR, store_op = SDL_GPUStoreOp.SDL_GPU_STOREOP_DONT_CARE,
            stencil_load_op = SDL_GPULoadOp.SDL_GPU_LOADOP_DONT_CARE, stencil_store_op = SDL_GPUStoreOp.SDL_GPU_STOREOP_DONT_CARE,
        };
        var pass = SDL_BeginGPURenderPass(cmd, &colorInfo, 1, &depthInfo);
        if (drawScene)
        {
            SDL_BindGPUGraphicsPipeline(pass, _pipeline);
            var identity = Matrix4x4.Identity;
            SDL_PushGPUVertexUniformData(cmd, 0, (IntPtr)(&identity), 64);
            var probes = new SDL_GPUBufferBinding { buffer = _probes };
            SDL_BindGPUVertexBuffers(pass, 0, &probes, 1);
            SDL_DrawGPUPrimitives(pass, (uint)Scene.Probes.Length, 1, 0, 0);

            var viewProjection = Scene.View(angle) * Scene.Projection;
            var indices = new SDL_GPUBufferBinding { buffer = _indices };
            SDL_BindGPUIndexBuffer(pass, &indices, SDL_GPUIndexElementSize.SDL_GPU_INDEXELEMENTSIZE_16BIT);
            for (var i = 0; i < Scene.Objects.Length; i++)
            {
                var mvp = Scene.Objects[i].World * viewProjection; // row vectors: world, then view, then projection
                SDL_PushGPUVertexUniformData(cmd, 0, (IntPtr)(&mvp), 64);
                var binding = new SDL_GPUBufferBinding { buffer = _vertexBuffers[i] };
                SDL_BindGPUVertexBuffers(pass, 0, &binding, 1);
                SDL_DrawGPUIndexedPrimitives(pass, (uint)Scene.CubeIndices.Length, 1, 0, 0, 0);
            }
        }
        SDL_EndGPURenderPass(pass);
    }

    public void Dispose()
    {
        foreach (var b in _vertexBuffers) SDL_ReleaseGPUBuffer(_device, b);
        SDL_ReleaseGPUBuffer(_device, _indices);
        SDL_ReleaseGPUBuffer(_device, _probes);
        SDL_ReleaseGPUTexture(_device, _depth);
        SDL_ReleaseGPUTexture(_device, Color);
        SDL_ReleaseGPUGraphicsPipeline(_device, _pipeline);
    }
}
