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
    float4 _PanoramaA_TexelSize;
    float4 _PanoramaB_TexelSize;
    float _Blend;
    float _ExposureA;
    float _ExposureB;
    float _RotationA;
    float _RotationB;

    // The longitude wraps from 1 back to 0 across one pixel, where screen-space derivatives would pick the smallest mip
    // and draw a seam, so the panorama is always read at mip 0. The latitude is kept half a texel inside the image so
    // bilinear filtering at the poles does not blend in the opposite edge.
    float3 SamplePanorama(sampler2D panorama, float4 texelSize, float3 direction, float rotation, float exposure)
    {
        direction = normalize(direction);
        float longitude = atan2(direction.x, direction.z) + radians(rotation);
        float latitude = asin(clamp(direction.y, -1.0, 1.0));
        float2 uv = float2(longitude / (2.0 * UNITY_PI) + 0.5, latitude / UNITY_PI + 0.5);
        float halfTexel = 0.5 * texelSize.y;
        uv.y = clamp(uv.y, halfTexel, 1.0 - halfTexel);
        return tex2Dlod(panorama, float4(uv, 0.0, 0.0)).rgb * exp2(exposure);
    }

    float4 Crossfade(float3 direction)
    {
        float3 source = SamplePanorama(_PanoramaA, _PanoramaA_TexelSize, direction, _RotationA, _ExposureA);
        float3 target = SamplePanorama(_PanoramaB, _PanoramaB_TexelSize, direction, _RotationB, _ExposureB);
        return float4(lerp(source, target, _Blend), 1.0);
    }
    ENDCG

    SubShader
    {
        Tags { "Queue" = "Background" "RenderType" = "Background" "PreviewType" = "Skybox" }
        Cull Off
        ZWrite Off

        // Pass 0: the skybox.
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

            // The stereo macros route each instance to its eye under single-pass instanced XR rendering.
            v2f vert(appdata input)
            {
                v2f output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.position = UnityObjectToClipPos(input.vertex);
                output.direction = input.vertex.xyz;
                return output;
            }

            float4 frag(v2f input) : SV_Target
            {
                return Crossfade(input.direction);
            }
            ENDCG
        }

        // Pass 1: renders the current blend into an equirectangular snapshot (EnvironmentController.CaptureCurrentSky).
        Pass
        {
            CGPROGRAM
            #pragma target 3.0
            #pragma vertex vert_img
            #pragma fragment fragSnapshot

            float4 fragSnapshot(v2f_img input) : SV_Target
            {
                float longitude = (input.uv.x - 0.5) * 2.0 * UNITY_PI;
                float latitude = (input.uv.y - 0.5) * UNITY_PI;
                float latitudeCos = cos(latitude);
                return Crossfade(float3(sin(longitude) * latitudeCos, sin(latitude), cos(longitude) * latitudeCos));
            }
            ENDCG
        }
    }
    Fallback Off
}
