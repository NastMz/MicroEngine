// A picture as a cathode-ray tube shows it: the glass bulges (the picture's edges curve inwards and what falls outside
// is black), every line of the picture is darker between its neighbours, and the phosphor's red, green and blue
// stripes show across it.
// SDL_GPU binding rules (fragment stage): textures and samplers in space2, uniform buffers in space3.
Texture2D<float4> Source : register(t0, space2);
SamplerState SourceSampler : register(s0, space2);

cbuffer Crt : register(b0, space3)
{
    float Curvature;  // 0 flat
    float Scanlines;  // 0 none, 1 black between lines
    float Lines;      // lines of the picture
    float Mask;       // 0 none, 1 full stripes
    float Columns;    // stripe triads across the picture
};

struct Input
{
    float2 TexCoord : TEXCOORD0;
    float4 Color : TEXCOORD1;
};

float4 main(Input input) : SV_Target0
{
    float2 centred = input.TexCoord * 2.0 - 1.0;
    centred += centred * (centred.yx * centred.yx) * Curvature;
    float2 uv = centred * 0.5 + 0.5;
    if (uv.x < 0.0 || uv.x > 1.0 || uv.y < 0.0 || uv.y > 1.0) return float4(0.0, 0.0, 0.0, 1.0) * input.Color;
    float4 c = Source.Sample(SourceSampler, uv);
    float within = 0.5 - 0.5 * cos(uv.y * Lines * 6.2831853);   // 1 in the middle of a line, 0 between two
    c.rgb *= 1.0 - Scanlines * (1.0 - within);
    float t = frac(uv.x * Columns) * 3.0;
    float3 stripes = float3(t < 1.0 ? 1.0 : 0.0, (t >= 1.0 && t < 2.0) ? 1.0 : 0.0, t >= 2.0 ? 1.0 : 0.0);
    c.rgb *= lerp(float3(1.0, 1.0, 1.0), 0.55 + 0.9 * stripes, Mask);
    return float4(saturate(c.rgb), c.a) * input.Color;
}
