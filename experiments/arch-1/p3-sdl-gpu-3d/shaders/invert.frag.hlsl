// Custom fragment shader for SDL_Renderer's gpu driver (SDL_CreateGPURenderState).
// Its inputs must match the renderer's own vertex shader output: colour then uv (SPIR-V locations 0 and 1).
// On D3D12 the semantics must be TEXCOORD0 and TEXCOORD1, not the COLOR0/TEXCOORD0 written in SDL's HLSL sources:
// SDL's built-in DXIL (built with shadercross) evidently exposes its varyings as TEXCOORD<location>. With COLOR0,
// compiled straight with DXC, D3D12 pipeline creation fails at draw time (E_INVALIDARG), the draw is dropped and
// only SDL_GetError reports it (observed in this experiment).
// The drawn texture is sampler slot 0 in space2; fragment uniforms pushed with
// SDL_SetGPURenderStateFragmentUniforms live in space3.
Texture2D<float4> Source : register(t0, space2);
SamplerState SourceSampler : register(s0, space2);

cbuffer Effect : register(b0, space3)
{
    float4 Amount; // x = 0 keeps the colour, 1 inverts it
};

struct Input
{
    float4 Color : TEXCOORD0;
    float2 TexCoord : TEXCOORD1;
};

float4 main(Input input) : SV_Target0
{
    float4 c = Source.Sample(SourceSampler, input.TexCoord) * input.Color;
    return float4(lerp(c.rgb, 1.0 - c.rgb, Amount.x), c.a);
}
