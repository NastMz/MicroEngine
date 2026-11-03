// The basic material's fragment stage: base colour texture (premultiplied) times the base colour (premultiplied factor
// times tint), shaded by one directional light: Ambient + (1 - Ambient) * max(0, dot(N, L)). A zero normal (a mesh
// without normals) is drawn unshaded. Output is opaque.
// SDL_GPU resource rules (fragment stage): textures and samplers in space2, uniform buffers in space3.
Texture2D<float4> BaseColorTexture : register(t0, space2);
SamplerState BaseColorSampler : register(s0, space2);

cbuffer Material : register(b0, space3)
{
    float4 BaseColor;
    float3 LightDirection; // unit vector from the surface towards the light, world space
    float Ambient;
};

struct Input
{
    float3 Normal : TEXCOORD0;
    float2 TexCoord : TEXCOORD1;
};

float4 main(Input input) : SV_Target0
{
    float4 color = BaseColorTexture.Sample(BaseColorSampler, input.TexCoord) * BaseColor;
    float lengthSquared = dot(input.Normal, input.Normal);
    float light = 1.0;
    if (lengthSquared > 0.0)
        light = Ambient + (1.0 - Ambient) * max(0.0, dot(input.Normal * rsqrt(lengthSquared), LightDirection));
    return float4(color.rgb * light, 1.0);
}
