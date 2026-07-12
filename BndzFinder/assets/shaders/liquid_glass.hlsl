// Liquid glass dock backdrop — D3D11 pixel shader
// Distortion + refraction + edge highlight for premium macOS-style dock glass

cbuffer LiquidGlassParams : register(b0)
{
    float distortion;
    float refraction;
    float edgeHighlight;
    float saturation;
    float noiseScale;
    float time;
    float2 resolution;
};

Texture2D backgroundTex : register(t0);
SamplerState samplerState : register(s0);

float hash(float2 p)
{
    return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453);
}

float noise(float2 p)
{
    float2 i = floor(p);
    float2 f = frac(p);
    float a = hash(i);
    float b = hash(i + float2(1, 0));
    float c = hash(i + float2(0, 1));
    float d = hash(i + float2(1, 1));
    float2 u = f * f * (3.0 - 2.0 * f);
    return lerp(lerp(a, b, u.x), lerp(c, d, u.x), u.y);
}

float4 main(float4 position : SV_POSITION, float2 uv : TEXCOORD0) : SV_TARGET
{
    float2 centered = uv - 0.5;
    float dist = length(centered);

    float edge = smoothstep(0.38, 0.5, dist);
    float2 offset = centered * distortion * (1.0 - dist) * 0.04;
    offset += float2(noise(uv * noiseScale + time * 0.1), noise(uv * noiseScale + 13.7)) * refraction * 0.01;

    float4 color = backgroundTex.Sample(samplerState, uv + offset);
    float lum = dot(color.rgb, float3(0.299, 0.587, 0.114));
    color.rgb = lerp(float3(lum, lum, lum), color.rgb, saturation);

    float highlight = (1.0 - edge) * edgeHighlight * 0.15;
    color.rgb += highlight;

    color.a *= 1.0 - edge * 0.3;
    return color;
}
