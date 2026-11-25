// Scan line on its own (ScanLine.hlsl): an additive overlay that draws only the thin band running from the top of
// _BoundsMin.._BoundsMax (world space) to the bottom. Put it as an extra material on an object to scan it. The statue's
// version change uses ModelTransition.shader, which combines this with the dissolve.
Shader "DNPDemo/ScanLine"
{
    Properties
    {
        [HDR] _ScanColor ("Scan Color", Color) = (1, 1, 1, 1)
        _Speed ("Speed (passes per second)", Range(0, 5)) = 1.5
        _Width ("Width", Range(0.001, 0.2)) = 0.015
        _Intensity ("Intensity", Range(0, 4)) = 0.8
        _BoundsMin ("Bounds Min", Vector) = (0, 0, 0, 0)
        _BoundsMax ("Bounds Max", Vector) = (1, 2, 1, 0)
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
        }

        Pass
        {
            Name "ScanLine"
            Tags { "LightMode" = "UniversalForward" }

            Blend One One
            ZWrite Off
            ZTest LEqual
            Cull Back

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "ScanLine.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _ScanColor;
                float _Speed;
                float _Width;
                float _Intensity;
                float3 _BoundsMin;
                float3 _BoundsMax;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                float height = ScanLineHeight(input.positionWS, _BoundsMin, _BoundsMax);
                float band = ScanLineBand(height, _Time.y, _Speed, _Width);
                return half4(_ScanColor.rgb * (band * _Intensity), 0.0);
            }
            ENDHLSL
        }
    }
}
