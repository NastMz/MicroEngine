using System.Runtime.InteropServices;
using System.Text.Json;
using SDL;
using static SDL.SDL3;

namespace P3Gpu;

public sealed record ShaderReflection(int Samplers, int StorageTextures, int StorageBuffers, int UniformBuffers, int Inputs, int Outputs);

/// <summary>Thin SDL_GPU helpers: shaders from offline blobs, uploads through transfer buffers, readback with a fence.</summary>
public static unsafe class Gpu
{
    public const SDL_GPUShaderFormat AllFormats =
        SDL_GPUShaderFormat.SDL_GPU_SHADERFORMAT_SPIRV | SDL_GPUShaderFormat.SDL_GPU_SHADERFORMAT_DXIL | SDL_GPUShaderFormat.SDL_GPU_SHADERFORMAT_MSL;

    public static void Ok(bool ok, string what)
    {
        if (!ok) throw new InvalidOperationException($"{what}: {SDL_GetError()}");
    }

    public static T* NotNull<T>(T* pointer, string what) where T : unmanaged
        => pointer != null ? pointer : throw new InvalidOperationException($"{what}: {SDL_GetError()}");

    /// <summary>Loads <c>shaders/name.{dxil|spv|msl}</c> for the device's backend; resource counts come from the JSON reflection.</summary>
    public static SDL_GPUShader* LoadShader(SDL_GPUDevice* device, string name)
    {
        var driver = SDL_GetGPUDeviceDriver(device);
        var (extension, format, entry) = driver switch
        {
            "direct3d12" => ("dxil", SDL_GPUShaderFormat.SDL_GPU_SHADERFORMAT_DXIL, "main"),
            "vulkan" => ("spv", SDL_GPUShaderFormat.SDL_GPU_SHADERFORMAT_SPIRV, "main"),
            "metal" => ("msl", SDL_GPUShaderFormat.SDL_GPU_SHADERFORMAT_MSL, "main0"),
            _ => throw new NotSupportedException($"no shader blobs for driver {driver}"),
        };
        var directory = Path.Combine(AppContext.BaseDirectory, "shaders");
        var code = File.ReadAllBytes(Path.Combine(directory, $"{name}.{extension}"));
        if (format == SDL_GPUShaderFormat.SDL_GPU_SHADERFORMAT_MSL) code = [.. code, 0]; // MSL source is NUL-terminated
        var reflection = JsonSerializer.Deserialize<ShaderReflection>(File.ReadAllText(Path.Combine(directory, $"{name}.json")))!;
        var entryBytes = System.Text.Encoding.UTF8.GetBytes(entry + "\0");
        fixed (byte* codePointer = code)
        fixed (byte* entryPointer = entryBytes)
        {
            var info = new SDL_GPUShaderCreateInfo
            {
                code_size = (nuint)code.Length,
                code = codePointer,
                entrypoint = entryPointer,
                format = format,
                stage = name.EndsWith(".vert", StringComparison.Ordinal) ? SDL_GPUShaderStage.SDL_GPU_SHADERSTAGE_VERTEX : SDL_GPUShaderStage.SDL_GPU_SHADERSTAGE_FRAGMENT,
                num_samplers = (uint)reflection.Samplers,
                num_storage_textures = (uint)reflection.StorageTextures,
                num_storage_buffers = (uint)reflection.StorageBuffers,
                num_uniform_buffers = (uint)reflection.UniformBuffers,
            };
            return NotNull(SDL_CreateGPUShader(device, &info), $"SDL_CreateGPUShader {name}.{extension}");
        }
    }

    public static SDL_GPUBuffer* CreateBuffer<T>(SDL_GPUDevice* device, SDL_GPUBufferUsageFlags usage, ReadOnlySpan<T> data) where T : unmanaged
    {
        var bytes = MemoryMarshal.AsBytes(data);
        var info = new SDL_GPUBufferCreateInfo { usage = usage, size = (uint)bytes.Length };
        var buffer = NotNull(SDL_CreateGPUBuffer(device, &info), "SDL_CreateGPUBuffer");
        var transfer = Upload(device, bytes);
        var cmd = NotNull(SDL_AcquireGPUCommandBuffer(device), "SDL_AcquireGPUCommandBuffer");
        var copy = SDL_BeginGPUCopyPass(cmd);
        var source = new SDL_GPUTransferBufferLocation { transfer_buffer = transfer, offset = 0 };
        var destination = new SDL_GPUBufferRegion { buffer = buffer, offset = 0, size = (uint)bytes.Length };
        SDL_UploadToGPUBuffer(copy, &source, &destination, false);
        SDL_EndGPUCopyPass(copy);
        Ok(SDL_SubmitGPUCommandBuffer(cmd), "SDL_SubmitGPUCommandBuffer");
        SDL_ReleaseGPUTransferBuffer(device, transfer); // released when the GPU is done with it
        return buffer;
    }

    public static SDL_GPUTexture* CreateTexture(SDL_GPUDevice* device, int width, int height, ReadOnlySpan<byte> rgba)
    {
        var info = new SDL_GPUTextureCreateInfo
        {
            type = SDL_GPUTextureType.SDL_GPU_TEXTURETYPE_2D,
            format = SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_R8G8B8A8_UNORM,
            usage = SDL_GPUTextureUsageFlags.SDL_GPU_TEXTUREUSAGE_SAMPLER,
            width = (uint)width, height = (uint)height, layer_count_or_depth = 1, num_levels = 1,
        };
        var texture = NotNull(SDL_CreateGPUTexture(device, &info), "SDL_CreateGPUTexture");
        var transfer = Upload(device, rgba);
        var cmd = NotNull(SDL_AcquireGPUCommandBuffer(device), "SDL_AcquireGPUCommandBuffer");
        var copy = SDL_BeginGPUCopyPass(cmd);
        var source = new SDL_GPUTextureTransferInfo { transfer_buffer = transfer, offset = 0 };
        var destination = new SDL_GPUTextureRegion { texture = texture, w = (uint)width, h = (uint)height, d = 1 };
        SDL_UploadToGPUTexture(copy, &source, &destination, false);
        SDL_EndGPUCopyPass(copy);
        Ok(SDL_SubmitGPUCommandBuffer(cmd), "SDL_SubmitGPUCommandBuffer");
        SDL_ReleaseGPUTransferBuffer(device, transfer);
        return texture;
    }

    public static SDL_GPUTexture* CreateTarget(SDL_GPUDevice* device, SDL_GPUTextureFormat format, SDL_GPUTextureUsageFlags usage, int width, int height)
    {
        var info = new SDL_GPUTextureCreateInfo
        {
            type = SDL_GPUTextureType.SDL_GPU_TEXTURETYPE_2D, format = format, usage = usage,
            width = (uint)width, height = (uint)height, layer_count_or_depth = 1, num_levels = 1,
        };
        return NotNull(SDL_CreateGPUTexture(device, &info), $"SDL_CreateGPUTexture {format}");
    }

    /// <summary>First depth format the device can render to, plus the support of each candidate (for the report).</summary>
    public static (SDL_GPUTextureFormat Chosen, string Support) PickDepthFormat(SDL_GPUDevice* device)
    {
        SDL_GPUTextureFormat[] candidates =
        [
            SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_D24_UNORM, SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_D32_FLOAT,
            SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_D16_UNORM, SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_D24_UNORM_S8_UINT,
            SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_D32_FLOAT_S8_UINT,
        ];
        var support = candidates.Select(f => (Format: f, Ok: (bool)SDL_GPUTextureSupportsFormat(device, f,
            SDL_GPUTextureType.SDL_GPU_TEXTURETYPE_2D, SDL_GPUTextureUsageFlags.SDL_GPU_TEXTUREUSAGE_DEPTH_STENCIL_TARGET))).ToArray();
        var chosen = support.First(s => s.Ok).Format;
        return (chosen, string.Join(", ", support.Select(s => $"{s.Format.ToString().Replace("SDL_GPU_TEXTUREFORMAT_", "")}={(s.Ok ? "yes" : "no")}")));
    }

    public static SDL_GPUSampler* PointClamp(SDL_GPUDevice* device)
    {
        var info = new SDL_GPUSamplerCreateInfo
        {
            min_filter = SDL_GPUFilter.SDL_GPU_FILTER_NEAREST, mag_filter = SDL_GPUFilter.SDL_GPU_FILTER_NEAREST,
            mipmap_mode = SDL_GPUSamplerMipmapMode.SDL_GPU_SAMPLERMIPMAPMODE_NEAREST,
            address_mode_u = SDL_GPUSamplerAddressMode.SDL_GPU_SAMPLERADDRESSMODE_CLAMP_TO_EDGE,
            address_mode_v = SDL_GPUSamplerAddressMode.SDL_GPU_SAMPLERADDRESSMODE_CLAMP_TO_EDGE,
            address_mode_w = SDL_GPUSamplerAddressMode.SDL_GPU_SAMPLERADDRESSMODE_CLAMP_TO_EDGE,
        };
        return NotNull(SDL_CreateGPUSampler(device, &info), "SDL_CreateGPUSampler");
    }

    /// <summary>Records a download of an RGBA8 texture into a new transfer buffer (caller submits, waits, then calls <see cref="Read"/>).</summary>
    public static SDL_GPUTransferBuffer* RecordDownload(SDL_GPUDevice* device, SDL_GPUCommandBuffer* cmd, SDL_GPUTexture* texture, int width, int height)
    {
        var info = new SDL_GPUTransferBufferCreateInfo { usage = SDL_GPUTransferBufferUsage.SDL_GPU_TRANSFERBUFFERUSAGE_DOWNLOAD, size = (uint)(width * height * 4) };
        var transfer = NotNull(SDL_CreateGPUTransferBuffer(device, &info), "SDL_CreateGPUTransferBuffer");
        var copy = SDL_BeginGPUCopyPass(cmd);
        var source = new SDL_GPUTextureRegion { texture = texture, w = (uint)width, h = (uint)height, d = 1 };
        var destination = new SDL_GPUTextureTransferInfo { transfer_buffer = transfer, offset = 0 };
        SDL_DownloadFromGPUTexture(copy, &source, &destination);
        SDL_EndGPUCopyPass(copy);
        return transfer;
    }

    public static Image Read(SDL_GPUDevice* device, SDL_GPUTransferBuffer* transfer, int width, int height)
    {
        var bytes = new byte[width * height * 4];
        var mapped = SDL_MapGPUTransferBuffer(device, transfer, false);
        if (mapped == IntPtr.Zero) throw new InvalidOperationException($"SDL_MapGPUTransferBuffer: {SDL_GetError()}");
        Marshal.Copy(mapped, bytes, 0, bytes.Length);
        SDL_UnmapGPUTransferBuffer(device, transfer);
        SDL_ReleaseGPUTransferBuffer(device, transfer);
        return new Image(width, height, bytes);
    }

    public static void SavePng(Image image, string path)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        fixed (byte* pixels = image.Rgba)
        {
            var surface = NotNull(SDL_CreateSurfaceFrom(image.Width, image.Height, SDL_PixelFormat.SDL_PIXELFORMAT_ABGR8888, (IntPtr)pixels, image.Width * 4), "SDL_CreateSurfaceFrom");
            Ok(SDL_SavePNG(surface, path), "SDL_SavePNG");
            SDL_DestroySurface(surface);
        }
    }

    private static SDL_GPUTransferBuffer* Upload(SDL_GPUDevice* device, ReadOnlySpan<byte> bytes)
    {
        var info = new SDL_GPUTransferBufferCreateInfo { usage = SDL_GPUTransferBufferUsage.SDL_GPU_TRANSFERBUFFERUSAGE_UPLOAD, size = (uint)bytes.Length };
        var transfer = NotNull(SDL_CreateGPUTransferBuffer(device, &info), "SDL_CreateGPUTransferBuffer");
        var mapped = SDL_MapGPUTransferBuffer(device, transfer, false);
        if (mapped == IntPtr.Zero) throw new InvalidOperationException($"SDL_MapGPUTransferBuffer: {SDL_GetError()}");
        bytes.CopyTo(new Span<byte>((void*)mapped, bytes.Length));
        SDL_UnmapGPUTransferBuffer(device, transfer);
        return transfer;
    }
}
