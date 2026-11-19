// One direction of a blur, nine samples wide. With a threshold it keeps only what is brighter than it, so the first
// pass (across) picks the bright parts of the picture and the second (down, threshold 0) finishes the blur.
// SDL_GPU binding rules (fragment stage): textures and samplers in space2, uniform buffers in space3.
Texture2D<float4> Source : register(t0, space2);
SamplerState SourceSampler : register(s0, space2);

cbuffer Blur : register(b0, space3)
{
    float2 Step;      // one sample's distance in texture coordinates, along the blur's direction
    float Threshold;  // 0 keeps everything
};

struct Input
{
    float2 TexCoord : TEXCOORD0;
    float4 Color : TEXCOORD1;
};

static const float Weights[5] = { 0.227027, 0.1945946, 0.1216216, 0.054054, 0.016216 };

float3 Bright(float2 uv)
{
    float3 c = Source.Sample(SourceSampler, uv).rgb;
    return max(c - Threshold, 0.0) / (1.0 - Threshold);
}

float4 main(Input input) : SV_Target0
{
    float3 sum = Bright(input.TexCoord) * Weights[0];
    for (int i = 1; i < 5; i++)
        sum += (Bright(input.TexCoord + Step * i) + Bright(input.TexCoord - Step * i)) * Weights[i];
    return float4(sum, 1.0) * input.Color;
}
