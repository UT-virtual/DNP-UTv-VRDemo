// Skybox for EnvironmentController: cross-fades two equirectangular (2:1) panoramas, each with its own exposure and rotation.
// Pass 0 draws the sky. Pass 1 bakes the current blend into an equirectangular snapshot so a retarget starts from what is on screen.
Shader "DNPDemo/Environment Crossfade Panorama"
{
    Properties
    {
        _PanoramaA ("Source Panorama", 2D) = "black" {}
        _PanoramaB ("Target Panorama", 2D) = "black" {}
        _Blend ("Blend", Range(0, 1)) = 0
        _ExposureA ("Source Exposure", Float) = 0
        _ExposureB ("Target Exposure", Float) = 0
        _RotationA ("Source Rotation", Float) = 0
        _RotationB ("Target Rotation", Float) = 0
    }

    CGINCLUDE
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
    ENDCG

    SubShader
    {
        Tags { "Queue" = "Background" "RenderType" = "Background" "PreviewType" = "Skybox" }
        Cull Off
        ZWrite Off

        Pass
        {
            CGPROGRAM
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag

            struct appdata
            {
                float4 vertex : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 position : SV_POSITION;
                float3 direction : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            v2f vert(appdata input)
            {
                v2f output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.position = UnityObjectToClipPos(input.vertex);
                output.direction = input.vertex.xyz;
                return output;
            }

            fixed4 frag(v2f input) : SV_Target
            {
                return fixed4(BlendedSky(input.direction), 1.0);
            }
            ENDCG
        }

        Pass
        {
            CGPROGRAM
            #pragma target 3.0
            #pragma vertex vert_img
            #pragma fragment fragSnapshot

            float3 DirectionFromUv(float2 uv)
            {
                float longitude = (uv.x - 0.5) * 2.0 * UNITY_PI;
                float latitude = (uv.y - 0.5) * UNITY_PI;
                float latitudeCos = cos(latitude);
                return float3(sin(longitude) * latitudeCos, sin(latitude), cos(longitude) * latitudeCos);
            }

            fixed4 fragSnapshot(v2f_img input) : SV_Target
            {
                return fixed4(BlendedSky(DirectionFromUv(input.uv)), 1.0);
            }
            ENDCG
        }
    }
    Fallback Off
}
