// Palette swap: the red channel of the indexed image (slot 0) selects one of 256 colours of the palette (slot 1, a
// 256 x 1 image), multiplied by the vertex colour. Swapping the palette recolours the image without touching it.
// SDL_GPU resource rules (fragment stage): textures and samplers in space2.
Texture2D<float4> Indexed : register(t0, space2);
SamplerState IndexedSampler : register(s0, space2);
Texture2D<float4> Palette : register(t1, space2);
SamplerState PaletteSampler : register(s1, space2);

struct Input
{
    float2 TexCoord : TEXCOORD0;
    float4 Color : TEXCOORD1;
};

float4 main(Input input) : SV_Target0
{
    float index = Indexed.Sample(IndexedSampler, input.TexCoord).r * 255.0;
    float u = (floor(index + 0.5) + 0.5) / 256.0;
    return Palette.Sample(PaletteSampler, float2(u, 0.5)) * input.Color;
}
