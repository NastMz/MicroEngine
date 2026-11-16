// The last pass of the chain: the picture plus its blurred bright parts, darkened towards the corners and on every
// other line. Two textures: the picture in slot 0 (the draw's texture), the glow in slot 1.
// SDL_GPU binding rules (fragment stage): textures and samplers in space2, uniform buffers in space3.
Texture2D<float4> Scene : register(t0, space2);
SamplerState SceneSampler : register(s0, space2);
Texture2D<float4> Glow : register(t1, space2);
SamplerState GlowSampler : register(s1, space2);

cbuffer Look : register(b0, space3)
{
    float GlowStrength;  // how much of the glow is added
    float Vignette;      // 0 none, 1 black corners
    float Scanlines;     // 0 none, 1 every other line black
    float Lines;         // the picture's height in pixels
};

struct Input
{
    float2 TexCoord : TEXCOORD0;
    float4 Color : TEXCOORD1;
};

float4 main(Input input) : SV_Target0
{
    float3 c = Scene.Sample(SceneSampler, input.TexCoord).rgb + Glow.Sample(GlowSampler, input.TexCoord).rgb * GlowStrength;
    float2 fromCentre = input.TexCoord - 0.5;
    c *= 1.0 - Vignette * saturate(dot(fromCentre, fromCentre) * 2.0);
    c *= 1.0 - Scanlines * step(0.5, frac(input.TexCoord.y * Lines * 0.5));
    return float4(saturate(c), 1.0) * input.Color;
}
