// 2D sprite vertex shader: pixel-space position, uv, colour; one orthographic matrix.
cbuffer Projection : register(b0, space1)
{
    float4x4 ViewProjection;
};

struct Input
{
    float2 Position : TEXCOORD0;
    float2 TexCoord : TEXCOORD1;
    float4 Color : TEXCOORD2;
};

struct Output
{
    float2 TexCoord : TEXCOORD0;
    float4 Color : TEXCOORD1;
    float4 Position : SV_Position;
};

Output main(Input input)
{
    Output output;
    output.TexCoord = input.TexCoord;
    output.Color = input.Color;
    output.Position = mul(ViewProjection, float4(input.Position, 0.0, 1.0));
    return output;
}
