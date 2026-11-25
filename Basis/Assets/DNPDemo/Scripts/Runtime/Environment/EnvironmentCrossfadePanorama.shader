// Skybox for EnvironmentController: cross-fades two equirectangular (2:1) panoramas, each with its own exposure and rotation.
// Unity draws every pass of the skybox material, so this shader has exactly one pass. The snapshot that lets a retarget
// start from what is on screen lives in EnvironmentPanoramaSnapshot.shader.
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
            #include "EnvironmentPanorama.cginc"

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
    }
    Fallback Off
}
