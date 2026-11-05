// The basic material's vertex stage: model-space position, normal and texture coordinate (MeshVertex, 32 bytes) to
// clip space with one world-view-projection matrix; the normal goes to world space with the world matrix (uniform scale
// assumed; it is renormalised per fragment). Matrices come straight from System.Numerics: mul(M, v) here is v * M in C#.
// SDL_GPU resource rules (vertex stage): uniform buffers in space1; inputs use TEXCOORD<location>.
cbuffer Transform : register(b0, space1)
{
    float4x4 WorldViewProjection;
    float4x4 World;
};

struct Input
{
    float3 Position : TEXCOORD0;
    float3 Normal : TEXCOORD1;
    float2 TexCoord : TEXCOORD2;
};

struct Output
{
    float3 Normal : TEXCOORD0;
    float2 TexCoord : TEXCOORD1;
    float4 Position : SV_Position;
};

Output main(Input input)
{
    Output output;
    output.Normal = mul(World, float4(input.Normal, 0.0)).xyz;
    output.TexCoord = input.TexCoord;
    output.Position = mul(WorldViewProjection, float4(input.Position, 1.0));
    return output;
}
