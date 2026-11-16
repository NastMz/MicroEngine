// The vertex stage every 2D shader of the examples shares: the batcher's vertices (position in target pixels, texture
// coordinate, premultiplied colour) and one matrix that takes pixels to clip space.
// SDL_GPU binding rules (vertex stage): uniform buffers in space1; inputs use TEXCOORD<location>.
cbuffer Projection : register(b0, space1)
{
    float4x4 PixelsToClip;
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
    output.Position = mul(PixelsToClip, float4(input.Position, 0.0, 1.0));
    return output;
}
