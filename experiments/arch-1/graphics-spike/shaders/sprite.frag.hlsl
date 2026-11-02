// 2D sprite fragment shader: texture * vertex colour.
// SDL_GPU resource rules (fragment stage): sampled textures and samplers live in space2 (SPIR-V set 2).
Texture2D<float4> Sprite : register(t0, space2);
SamplerState SpriteSampler : register(s0, space2);

struct Input
{
    float2 TexCoord : TEXCOORD0;
    float4 Color : TEXCOORD1;
};

float4 main(Input input) : SV_Target0
{
    return Sprite.Sample(SpriteSampler, input.TexCoord) * input.Color;
}
