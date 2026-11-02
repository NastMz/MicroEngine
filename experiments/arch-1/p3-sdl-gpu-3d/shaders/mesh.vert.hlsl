// Mesh vertex shader: position + colour, one matrix.
// SDL_GPU resource rules (vertex stage): uniform buffers live in space1 (SPIR-V descriptor set 1);
// vertex attributes use the TEXCOORD<location> semantic on D3D12.
cbuffer Transform : register(b0, space1)
{
    // Uploaded straight from System.Numerics.Matrix4x4 memory (row-major, row vectors).
    // HLSL's default column_major packing reads it transposed, so mul(M, v) here equals v * M in C#.
    float4x4 WorldViewProjection;
};

struct Input
{
    float3 Position : TEXCOORD0;
    float4 Color : TEXCOORD1;
};

struct Output
{
    float4 Color : TEXCOORD0;
    float4 Position : SV_Position;
};

Output main(Input input)
{
    Output output;
    output.Color = input.Color;
    output.Position = mul(WorldViewProjection, float4(input.Position, 1.0));
    return output;
}
