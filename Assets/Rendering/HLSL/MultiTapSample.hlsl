#ifndef MULTI_TAP_SAMPLE_INCLUDED
#define MULTI_TAP_SAMPLE_INCLUDED

#define MULTI_TAP_MAX_SAMPLES 64
#define MULTI_TAP_R2 float2(0.7548776662, 0.5698402910)
#define MULTI_TAP_TWIST 0.7698003589

float3 MultiTapSpectrum(float t)
{
    return saturate(1.0 - abs(t - float3(1.0, 3.0, 5.0) / 6.0) * 6.0);
}

float2 MultiTapDisk(int i)
{
    float2 u = frac(0.5 + i * MULTI_TAP_R2);
    float angle = u.y * TWO_PI;
    return float2(cos(angle), sin(angle)) * sqrt(u.x);
}

void MultiTapSample_float(
    UnityTexture2D Texture,
    UnitySamplerState Sampler,
    float2 UV,
    float2 Offset,
    float2 Spread,
    float Twist,
    float2 Radius,
    float Dispersion,
    float Samples,
    out float4 RGBA
)
{
    int count = clamp((int)Samples, 1, MULTI_TAP_MAX_SAMPLES);
    float dispersion = count > 2 ? saturate(Dispersion) : 0.0;
    float2 dx = ddx(UV);
    float2 dy = ddy(UV);
    float4 sum = 0.0;
    float4 weightSum = 0.0;
    float2 curl = float2(-Spread.y, Spread.x) * Twist * MULTI_TAP_TWIST;

    UNITY_LOOP
    for (int i = 0; i < count; i++)
    {
        float t = (i + 0.5) / count;
        float a = t * 2.0 - 1.0;
        float2 uv = UV + Offset + Spread * a + curl * cos(PI * a) + MultiTapDisk(i) * Radius;
        float4 weight = float4(lerp(1.0, MultiTapSpectrum(t), dispersion), 1.0);
        sum += SAMPLE_TEXTURE2D_GRAD(Texture.tex, Sampler.samplerstate, uv, dx, dy) * weight;
        weightSum += weight;
    }

    RGBA = sum / weightSum;
}

#endif
