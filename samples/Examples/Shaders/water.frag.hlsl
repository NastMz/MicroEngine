// Water: the picture above the surface, mirrored about it and pushed sideways by a sine wave that travels with time,
// then mixed with the water's colour. The quad covers the water; its texture coordinates are its own place in the
// picture, so Surface (the v coordinate of the water line) is all the shader needs to find the mirror image.
// SDL_GPU binding rules (fragment stage): textures and samplers in space2, uniform buffers in space3.
Texture2D<float4> Scene : register(t0, space2);
SamplerState SceneSampler : register(s0, space2);

cbuffer Water : register(b0, space3)
{
    float4 Tint;      // the water's colour; its alpha is how much of it covers the reflection
    float Time;       // seconds
    float Amplitude;  // sideways push, in texture coordinates
    float Frequency;  // waves per unit of v
    float Speed;      // radians per second
    float Surface;    // v of the water line
};

struct Input
{
    float2 TexCoord : TEXCOORD0;
    float4 Color : TEXCOORD1;
};

float4 main(Input input) : SV_Target0
{
    float depth = input.TexCoord.y - Surface;
    float wave = sin(depth * Frequency * 6.2831853 + Time * Speed) * Amplitude * saturate(depth * 8.0);
    float4 mirrored = Scene.Sample(SceneSampler, float2(input.TexCoord.x + wave, Surface - depth));
    return float4(lerp(mirrored.rgb, Tint.rgb, Tint.a), 1.0) * input.Color;
}
