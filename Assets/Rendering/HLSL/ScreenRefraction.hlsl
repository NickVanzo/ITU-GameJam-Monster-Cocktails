#ifndef SCREEN_REFRACTION_INCLUDED
#define SCREEN_REFRACTION_INCLUDED

void ScreenRefraction_float(float3 PositionWS, float3 NormalWS, float IOR, float Thickness, out float2 Offset)
{
    float3 viewRay = normalize(PositionWS - _WorldSpaceCameraPos);
    float3 bent = refract(viewRay, normalize(NormalWS), 1.0 / max(IOR, 0.0001));
    float4 from = TransformWorldToHClip(PositionWS);
    float4 to = TransformWorldToHClip(PositionWS + bent * Thickness);
    Offset = (to.xy / to.w - from.xy / from.w) * float2(0.5, 0.5 * _ProjectionParams.x);
}

#endif
