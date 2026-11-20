// A post-processing pass of the game's own: the red and blue of the picture pulled apart towards the edges, as a cheap
// lens does. The picture is the draw's texture, in slot 0.
// SDL_GPU binding rules (fragment stage): textures and samplers in space2, uniform buffers in space3.
Texture2D<float4> Picture : register(t0, space2);
SamplerState PictureSampler : register(s0, space2);

cbuffer Fringe : register(b0, space3)
{
    float Amount;  // how far apart the colours are at the edge, in texture coordinates
};

struct Input
{
    float2 TexCoord : TEXCOORD0;
    float4 Color : TEXCOORD1;
};

float4 main(Input input) : SV_Target0
{
    float2 apart = (input.TexCoord - 0.5) * 2.0 * Amount;
    float4 c = Picture.Sample(PictureSampler, input.TexCoord);
    c.r = Picture.Sample(PictureSampler, input.TexCoord + apart).r;
    c.b = Picture.Sample(PictureSampler, input.TexCoord - apart).b;
    return c * input.Color;
}
