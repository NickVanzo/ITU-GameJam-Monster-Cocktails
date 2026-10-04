#ifndef FADE_CUSTOM_LIGHTING_INCLUDED
#define FADE_CUSTOM_LIGHTING_INCLUDED

// ============================================================================
// MAIN LIGHT
// ============================================================================

void MainLight_float(
    float3 PositionWS,
    out float3 Direction,
    out float3 Color,
    out float DistanceAttenuation,
    out float ShadowAttenuation
)
{
#ifdef SHADERGRAPH_PREVIEW

    Direction = normalize(float3(0.5, 1.0, 0.5));
    Color = float3(1.0, 1.0, 1.0);
    DistanceAttenuation = 1.0;
    ShadowAttenuation = 1.0;

#else

    float4 shadowCoord = TransformWorldToShadowCoord(PositionWS);

    Light light = GetMainLight( shadowCoord, PositionWS, half4(1.0, 1.0, 1.0, 1.0));
    Direction = light.direction;
    Color = light.color;
    DistanceAttenuation = light.distanceAttenuation;
    ShadowAttenuation = light.shadowAttenuation;

#endif
}


// ============================================================================
// RAW LIGHT ACCUMULATION
//
// Accumulates:
//
//      LightColor
//      * DistanceAttenuation
//      * ShadowAttenuation
//
// No normal / NdotL contribution.
// Useful for fully custom stylized lighting.
// ============================================================================

void LightAccumulation_float(
    float3 PositionWS,
    out float3 Lighting
)
{
#ifdef SHADERGRAPH_PREVIEW

    Lighting = float3(1.0, 1.0, 1.0);

#else

    Lighting = float3(0.0, 0.0, 0.0);

    half4 shadowMask = half4(1.0, 1.0, 1.0, 1.0);

    // ========================================================================
    // MAIN LIGHT
    // ========================================================================

    float4 shadowCoord = TransformWorldToShadowCoord(PositionWS);

    Light mainLight = GetMainLight(
        shadowCoord,
        PositionWS,
        shadowMask
    );

    Lighting +=
        mainLight.color *
        mainLight.distanceAttenuation *
        mainLight.shadowAttenuation;


    // ========================================================================
    // ADDITIONAL LIGHT SETUP
    // ========================================================================

    uint pixelLightCount = GetAdditionalLightsCount();

    // ========================================================================
    // FORWARD+ / CLUSTERED ADDITIONAL DIRECTIONAL LIGHTS
    // ========================================================================

#if USE_CLUSTER_LIGHT_LOOP

    UNITY_LOOP
    for (
        uint lightIndex = 0;
        lightIndex < min(URP_FP_DIRECTIONAL_LIGHTS_COUNT, MAX_VISIBLE_LIGHTS);
        ++lightIndex
    )
    {
        CLUSTER_LIGHT_LOOP_SUBTRACTIVE_LIGHT_CHECK

        Light light = GetAdditionalLight( lightIndex, PositionWS, shadowMask);
        Lighting += light.color * light.distanceAttenuation * light.shadowAttenuation;
    }

#endif

    // ========================================================================
    // INPUT DATA REQUIRED BY LIGHT_LOOP_BEGIN
    //
    // In Forward mode this is mostly irrelevant.
    // In Forward+/Clustered mode the macro uses:
    //
    //      inputData.positionWS
    //      inputData.normalizedScreenSpaceUV
    //
    // to find the cluster containing this fragment.
    // ========================================================================

    InputData inputData = (InputData)0;
    inputData.positionWS = PositionWS;
    float4 screenPos = ComputeScreenPos( TransformWorldToHClip(PositionWS) );
    inputData.normalizedScreenSpaceUV = screenPos.xy / screenPos.w;


    // ========================================================================
    // POINT / SPOT / OTHER ADDITIONAL LIGHTS
    // ========================================================================

    LIGHT_LOOP_BEGIN(pixelLightCount)

        Light light = GetAdditionalLight(
            lightIndex,
            PositionWS,
            shadowMask
        );

        Lighting +=
            light.color *
            light.distanceAttenuation *
            light.shadowAttenuation;

    LIGHT_LOOP_END

#endif
}


// ============================================================================
// DIFFUSE LIGHT ACCUMULATION
//
// Standard Lambert:
//
//      saturate(dot(N, L))
//
// Includes:
//
//      Main directional light
//      Additional directional lights
//      Point lights
//      Spot lights
//      Distance attenuation
//      Shadows
// ============================================================================

void DiffuseLightAccumulation_float(
    float3 PositionWS,
    float3 NormalWS,
    out float3 Lighting
)
{
#ifdef SHADERGRAPH_PREVIEW

    float3 N = normalize(NormalWS);
    float3 L = normalize(float3(0.5, 1.0, 0.5));

    float NdotL = saturate(dot(N, L));

    Lighting = float3(NdotL, NdotL, NdotL);

#else

    Lighting = float3(0.0, 0.0, 0.0);

    NormalWS = normalize(NormalWS);

    half4 shadowMask = half4(1.0, 1.0, 1.0, 1.0);
    float4 shadowCoord = TransformWorldToShadowCoord(PositionWS);

    Light mainLight = GetMainLight( shadowCoord, PositionWS, shadowMask);

    float mainNdotL = saturate(dot( NormalWS, mainLight.direction));
    Lighting += mainLight.color * mainNdotL * mainLight.distanceAttenuation * mainLight.shadowAttenuation;

    uint pixelLightCount = GetAdditionalLightsCount();

#if USE_CLUSTER_LIGHT_LOOP

    UNITY_LOOP
    for (
        uint lightIndex = 0;
        lightIndex < min(URP_FP_DIRECTIONAL_LIGHTS_COUNT, MAX_VISIBLE_LIGHTS);
        ++lightIndex
    )
    {
        CLUSTER_LIGHT_LOOP_SUBTRACTIVE_LIGHT_CHECK

        Light light = GetAdditionalLight( lightIndex, PositionWS, shadowMask );
        float NdotL = saturate(dot(NormalWS, light.direction));
        Lighting += light.color * NdotL * light.distanceAttenuation * light.shadowAttenuation;
    }

#endif

    float4 screenPos = ComputeScreenPos( TransformWorldToHClip(PositionWS));

    InputData inputData = (InputData)0;
    inputData.positionWS = PositionWS;
    inputData.normalizedScreenSpaceUV = screenPos.xy / screenPos.w;

    LIGHT_LOOP_BEGIN(pixelLightCount)
        Light light = GetAdditionalLight(lightIndex, PositionWS, shadowMask);
        float NdotL = saturate(dot( NormalWS, light.direction ));
        Lighting += light.color * NdotL * light.distanceAttenuation * light.shadowAttenuation;
    LIGHT_LOOP_END

#endif
}

void AdditionalLightAccumulation_float(
    float3 PositionWS,
    out float3 Lighting
)
{
#ifdef SHADERGRAPH_PREVIEW
    Lighting = float3(0.25, 0.25, 0.25);
#else

    Lighting = float3(0.0, 0.0, 0.0);
    half4 shadowMask = half4(1.0, 1.0, 1.0, 1.0);
    uint pixelLightCount = GetAdditionalLightsCount();

#if USE_CLUSTER_LIGHT_LOOP

    UNITY_LOOP
    for (
        uint lightIndex = 0;
        lightIndex < min(URP_FP_DIRECTIONAL_LIGHTS_COUNT, MAX_VISIBLE_LIGHTS);
        ++lightIndex
    )
    {
        CLUSTER_LIGHT_LOOP_SUBTRACTIVE_LIGHT_CHECK
        Light light = GetAdditionalLight( lightIndex, PositionWS, shadowMask );
        Lighting += light.color * light.distanceAttenuation * light.shadowAttenuation;
    }

#endif

    InputData inputData = (InputData)0;
    inputData.positionWS = PositionWS;

    float4 screenPos = ComputeScreenPos( TransformWorldToHClip(PositionWS));
    inputData.normalizedScreenSpaceUV = screenPos.xy / screenPos.w;

    LIGHT_LOOP_BEGIN(pixelLightCount)
        Light light = GetAdditionalLight( lightIndex, PositionWS, shadowMask);
        Lighting += light.color * light.distanceAttenuation * light.shadowAttenuation;
    LIGHT_LOOP_END

#endif
}

void AdditionalDiffuseLightAccumulation_float(
    float3 PositionWS,
    float3 NormalWS,
    out float3 Lighting
)
{
#ifdef SHADERGRAPH_PREVIEW
    Lighting = float3(0.25, 0.25, 0.25);
#else
    Lighting = float3(0.0, 0.0, 0.0);
    NormalWS = normalize(NormalWS);

    half4 shadowMask =  half4(1.0, 1.0, 1.0, 1.0);

    uint pixelLightCount = GetAdditionalLightsCount();

#if USE_CLUSTER_LIGHT_LOOP

    UNITY_LOOP
    for (
        uint lightIndex = 0;
        lightIndex < min(URP_FP_DIRECTIONAL_LIGHTS_COUNT, MAX_VISIBLE_LIGHTS);
        ++lightIndex
    )
    {
        CLUSTER_LIGHT_LOOP_SUBTRACTIVE_LIGHT_CHECK

        Light light = GetAdditionalLight(lightIndex, PositionWS, shadowMask);
        float NdotL = saturate(dot(NormalWS,light.direction));
        Lighting += light.color *NdotL *light.distanceAttenuation *light.shadowAttenuation;
    }

#endif

    InputData inputData = (InputData)0;
    inputData.positionWS = PositionWS;

    float4 screenPos = ComputeScreenPos(TransformWorldToHClip(PositionWS));
    inputData.normalizedScreenSpaceUV = screenPos.xy / screenPos.w;

    LIGHT_LOOP_BEGIN(pixelLightCount)

        Light light = GetAdditionalLight(lightIndex,PositionWS,shadowMask);
        float NdotL = saturate(dot(NormalWS,light.direction));
        Lighting += light.color * NdotL * light.distanceAttenuation * light.shadowAttenuation;

    LIGHT_LOOP_END

#endif
}

void AmbientLight_float(
    float3 NormalWS,
    out float3 Ambient
)
{
#ifdef SHADERGRAPH_PREVIEW
    Ambient = float3(0.15, 0.15, 0.15);
#else
    Ambient = SampleSH(normalize(NormalWS));
#endif
}

float Bayer2(float2 p)
{
    p = floor(p);
    return frac(p.x * 0.5 + p.y * p.y * 0.75);
}

float Bayer4(float2 p) { return Bayer2(p * 0.5) * 0.25 + Bayer2(p); }
float Bayer8(float2 p) { return Bayer4(p * 0.5) * 0.25 + Bayer2(p); }

void BayerDither_float(float2 PixelPosition, out float Noise)
{
    Noise = Bayer4(PixelPosition) + 0.5 / 16.0;
}

void BayerDither8_float(float2 PixelPosition, out float Noise)
{
    Noise = Bayer8(PixelPosition) + 0.5 / 64.0;
}

void ShadeDithered_float(float3 Albedo, float3 Lit, float3 ShadowColor, float Steps, float3 Emissiveness, float Noise, float DitherWidth, out float3 Color)
{
    float  lum  = dot(Lit, float3(0.2126, 0.7152, 0.0722));
    float  x    = lum * Steps + (Noise - 0.5) * DitherWidth;   // jitter the band threshold per pixel
    float  band = clamp(floor(x), 0.0, Steps) / Steps;         // 0 = in shadow, 1 = fully lit
    float3 lit  = Lit * (band / max(min(lum, 1.0), 1e-4));     // banded, keeps the light's hue
    Color = Albedo * (ShadowColor + lit) + Emissiveness;
}

void Shade_float(float3 Albedo, float3 Lit, float3 ShadowColor, float Steps, float3 Emissiveness, out float3 Color)
{
    ShadeDithered_float(Albedo, Lit, ShadowColor, Steps, Emissiveness, 0.5, 0.0, Color);
}

#endif