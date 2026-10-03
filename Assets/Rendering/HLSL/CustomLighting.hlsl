#ifndef FADE_CUSTOM_LIGHTING_INCLUDED
#define FADE_CUSTOM_LIGHTING_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

// ------------------------------------------------------------
// Shader variants
// Unity 6.1+
// ------------------------------------------------------------

#ifndef SHADERGRAPH_PREVIEW

#pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
#pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH

#pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
#pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS

// Forward+
#pragma multi_compile _ _CLUSTER_LIGHT_LOOP

#endif


// ============================================================
// MAIN LIGHT
// ============================================================

void MainLight_float(
    float3 PositionWS,
    out float3 Direction,
    out float3 Color,
    out float DistanceAttenuation,
    out float ShadowAttenuation)
{
#ifdef SHADERGRAPH_PREVIEW

    Direction = normalize(float3(0.5, 1.0, 0.5));
    Color = 1.0;
    DistanceAttenuation = 1.0;
    ShadowAttenuation = 1.0;

#else

    float4 shadowCoord = TransformWorldToShadowCoord(PositionWS);

    Light light = GetMainLight(shadowCoord);

    Direction = light.direction;
    Color = light.color;
    DistanceAttenuation = light.distanceAttenuation;
    ShadowAttenuation = light.shadowAttenuation;

#endif
}


// ============================================================
// RAW LIGHT ACCUMULATION
//
// No NdotL.
// Literally:
//     lightColor * attenuation
//
// Very useful for stylized shaders.
// ============================================================

void LightAccumulation_float(
    float3 PositionWS,
    out float3 Lighting)
{
    Lighting = 0.0;

#ifdef SHADERGRAPH_PREVIEW

    Lighting = 1.0;

#else

    // -------------------------
    // Main light
    // -------------------------

    float4 shadowCoord = TransformWorldToShadowCoord(PositionWS);

    Light mainLight = GetMainLight(shadowCoord);

    Lighting +=
        mainLight.color *
        mainLight.distanceAttenuation *
        mainLight.shadowAttenuation;


    // -------------------------
    // Additional lights
    // -------------------------

#if defined(_ADDITIONAL_LIGHTS)

    uint pixelLightCount = GetAdditionalLightsCount();


    // ========================================================
    // Forward+ directional lights
    // ========================================================

#if USE_CLUSTER_LIGHT_LOOP

    UNITY_LOOP
    for (
        uint lightIndex = 0;
        lightIndex < min(URP_FP_DIRECTIONAL_LIGHTS_COUNT, MAX_VISIBLE_LIGHTS);
        ++lightIndex)
    {
        Light light = GetAdditionalLight(
            lightIndex,
            PositionWS,
            half4(1,1,1,1)
        );

        Lighting +=
            light.color *
            light.distanceAttenuation *
            light.shadowAttenuation;
    }

#endif


    // ========================================================
    // Point / Spot / additional lights
    // ========================================================

    InputData inputData = (InputData)0;

    inputData.positionWS = PositionWS;

    float4 screenPos =
        ComputeScreenPos(
            TransformWorldToHClip(PositionWS)
        );

    inputData.normalizedScreenSpaceUV =
        screenPos.xy / screenPos.w;


    LIGHT_LOOP_BEGIN(pixelLightCount)

        Light light = GetAdditionalLight(
            lightIndex,
            PositionWS,
            half4(1,1,1,1)
        );

        Lighting +=
            light.color *
            light.distanceAttenuation *
            light.shadowAttenuation;

    LIGHT_LOOP_END

#endif // _ADDITIONAL_LIGHTS

#endif // SHADERGRAPH_PREVIEW
}


// ============================================================
// LAMBERT ACCUMULATION
//
// Actual surface lighting:
//     light * saturate(N dot L)
// ============================================================

void DiffuseLightAccumulation_float(
    float3 PositionWS,
    float3 NormalWS,
    out float3 Lighting)
{
    Lighting = 0.0;

#ifdef SHADERGRAPH_PREVIEW

    Lighting = saturate(dot(
        normalize(NormalWS),
        normalize(float3(0.5, 1, 0.5))
    ));

#else

    NormalWS = normalize(NormalWS);


    // ========================================================
    // Main light
    // ========================================================

    float4 shadowCoord =
        TransformWorldToShadowCoord(PositionWS);

    Light mainLight =
        GetMainLight(shadowCoord);

    float mainNdotL =
        saturate(dot(NormalWS, mainLight.direction));

    Lighting +=
        mainLight.color *
        mainNdotL *
        mainLight.distanceAttenuation *
        mainLight.shadowAttenuation;


#if defined(_ADDITIONAL_LIGHTS)

    uint pixelLightCount = GetAdditionalLightsCount();

    // ========================================================
    // Forward+ directional lights
    // ========================================================

#if USE_CLUSTER_LIGHT_LOOP

    UNITY_LOOP
    for (
        uint lightIndex = 0;
        lightIndex < min(URP_FP_DIRECTIONAL_LIGHTS_COUNT, MAX_VISIBLE_LIGHTS);
        ++lightIndex)
    {
        Light light = GetAdditionalLight(
            lightIndex,
            PositionWS,
            half4(1,1,1,1)
        );

        float NdotL =
            saturate(dot(NormalWS, light.direction));

        Lighting +=
            light.color *
            NdotL *
            light.distanceAttenuation *
            light.shadowAttenuation;
    }

#endif


    // ========================================================
    // Other additional lights
    // ========================================================

    InputData inputData = (InputData)0;

    inputData.positionWS = PositionWS;

    float4 screenPos =
        ComputeScreenPos(
            TransformWorldToHClip(PositionWS)
        );

    inputData.normalizedScreenSpaceUV =
        screenPos.xy / screenPos.w;


    LIGHT_LOOP_BEGIN(pixelLightCount)

        Light light = GetAdditionalLight(
            lightIndex,
            PositionWS,
            half4(1,1,1,1)
        );

        float NdotL =
            saturate(dot(NormalWS, light.direction));

        Lighting +=
            light.color *
            NdotL *
            light.distanceAttenuation *
            light.shadowAttenuation;

    LIGHT_LOOP_END

#endif

#endif
}

#endif