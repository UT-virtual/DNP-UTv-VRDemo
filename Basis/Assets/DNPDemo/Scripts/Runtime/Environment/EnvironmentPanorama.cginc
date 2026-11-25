// Sampling shared by the environment sky (EnvironmentCrossfadePanorama.shader) and its snapshot
// (EnvironmentPanoramaSnapshot.shader): two equirectangular (2:1) panoramas, each with its own exposure and rotation.
//
// Direction and UV are related by
//   u = frac(atan2(x, z) / 2pi + rotation / 360 + 0.5),  v = asin(y) / pi + 0.5
// and its inverse DirectionFromUv (rotation 0). EnvironmentPresetTests keeps a C# copy of both to check they invert.
#ifndef DNPDEMO_ENVIRONMENT_PANORAMA_INCLUDED
#define DNPDEMO_ENVIRONMENT_PANORAMA_INCLUDED

#include "UnityCG.cginc"

sampler2D _PanoramaA;
sampler2D _PanoramaB;
float _Blend;
float _ExposureA;
float _ExposureB;
float _RotationA;
float _RotationB;

// The longitude from atan2 wraps from 1 to 0 at the back of the sphere. Plain tex2D would see a full-width UV
// derivative there and pick the smallest mip, drawing a seam line. Compare the derivative of u with that of a copy
// whose wrap is half a turn away, and use the smaller one for the mip selection (Tarini's method).
float3 SamplePanorama(sampler2D panorama, float3 direction, float rotation)
{
    direction = normalize(direction);
    float longitude = atan2(direction.x, direction.z) / (2.0 * UNITY_PI) + rotation / 360.0;
    float latitude = asin(clamp(direction.y, -1.0, 1.0)) / UNITY_PI + 0.5;
    float u = frac(longitude + 0.5);
    float uShifted = frac(longitude) - 0.5;
    float2 dx = float2(ddx(u), ddx(latitude));
    float2 dy = float2(ddy(u), ddy(latitude));
    float dxShifted = ddx(uShifted);
    float dyShifted = ddy(uShifted);
    dx.x = abs(dx.x) <= abs(dxShifted) ? dx.x : dxShifted;
    dy.x = abs(dy.x) <= abs(dyShifted) ? dy.x : dyShifted;
    return tex2Dgrad(panorama, float2(u, latitude), dx, dy).rgb;
}

float3 BlendedSky(float3 direction)
{
    float3 source = SamplePanorama(_PanoramaA, direction, _RotationA) * exp2(_ExposureA);
    float3 target = SamplePanorama(_PanoramaB, direction, _RotationB) * exp2(_ExposureB);
    return lerp(source, target, _Blend);
}

// Inverse of the mapping in SamplePanorama for rotation 0: the direction that an equirectangular texel looks along.
float3 DirectionFromUv(float2 uv)
{
    float longitude = (uv.x - 0.5) * 2.0 * UNITY_PI;
    float latitude = (uv.y - 0.5) * UNITY_PI;
    float latitudeCos = cos(latitude);
    return float3(sin(longitude) * latitudeCos, sin(latitude), cos(longitude) * latitudeCos);
}

#endif
