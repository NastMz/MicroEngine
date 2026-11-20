// Colour grading of a premultiplied picture: brightness, contrast around mid grey, saturation and a tint, in that
// order, on the straight colour; the alpha is kept.
// SDL_GPU binding rules (fragment stage): textures and samplers in space2, uniform buffers in space3.
Texture2D<float4> Source : register(t0, space2);
SamplerState SourceSampler : register(s0, space2);

cbuffer Grade : register(b0, space3)
{
    float4 Tint;        // multiplies the colour; white changes nothing
    float Brightness;   // 1 changes nothing
    float Contrast;     // 1 changes nothing
    float Saturation;   // 1 changes nothing, 0 is grey
};

struct Input
{
    float2 TexCoord : TEXCOORD0;
    float4 Color : TEXCOORD1;
};

float4 main(Input input) : SV_Target0
{
    float4 c = Source.Sample(SourceSampler, input.TexCoord);
    float3 rgb = c.a > 0.0 ? c.rgb / c.a : float3(0.0, 0.0, 0.0);
    rgb *= Brightness;
    rgb = (rgb - 0.5) * Contrast + 0.5;
    float grey = dot(rgb, float3(0.2126, 0.7152, 0.0722));
    rgb = lerp(float3(grey, grey, grey), rgb, Saturation);
    rgb = saturate(rgb * Tint.rgb);
    return float4(rgb * c.a, c.a) * input.Color;
}
