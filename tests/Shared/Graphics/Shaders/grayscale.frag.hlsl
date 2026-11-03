// Full-screen effect: the scene (slot 0) desaturated towards its luma (Rec. 601 weights) by Amount, a fragment uniform.
// Colours are premultiplied, so the luma of the premultiplied colour is the premultiplied luma: alpha is kept.
// SDL_GPU resource rules (fragment stage): textures and samplers in space2, uniform buffers in space3.
Texture2D<float4> Scene : register(t0, space2);
SamplerState SceneSampler : register(s0, space2);

cbuffer Effect : register(b0, space3)
{
    float Amount;
    float3 Padding;
};

struct Input
{
    float2 TexCoord : TEXCOORD0;
    float4 Color : TEXCOORD1;
};

float4 main(Input input) : SV_Target0
{
    float4 c = Scene.Sample(SceneSampler, input.TexCoord);
    float luma = dot(c.rgb, float3(0.299, 0.587, 0.114));
    return float4(lerp(c.rgb, luma.xxx, Amount), c.a) * input.Color;
}
