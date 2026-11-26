Shader "DNPDemo/ModelTransition"
{
    Properties
    {
        _BaseMap ("Base Map", 2D) = "white" {}
        _BaseColor ("Base Color", Color) = (1,1,1,1)
        _Dissolve ("Dissolve", Range(0,1)) = 0
        _ScanLine ("Scan Line", Range(0,1)) = 0
        _BoundsMin ("Bounds Min", Vector) = (0,0,0,0)
        _BoundsMax ("Bounds Max", Vector) = (1,1,1,0)
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
            Name "ModelTransition"
            Tags { "LightMode" = "UniversalForward" }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite On
            Cull Back

            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing
            #pragma multi_compile _ DOTS_INSTANCING_ON

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4 _BaseColor;
                float _Dissolve;
                float _ScanLine;
                float3 _BoundsMin;
                float3 _BoundsMax;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float2 uv : TEXCOORD1;
                float3 normalWS : NORMAL;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                float3 size = max(_BoundsMax - _BoundsMin, float3(0.001, 0.001, 0.001));
                float3 normalized = saturate((input.positionWS - _BoundsMin) / size);
                float noise = frac(sin(dot(input.positionWS, float3(12.9898, 78.233, 37.719))) * 43758.5453);
                clip(_Dissolve - noise);

                half4 baseColor = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv) * _BaseColor;
                float scanLineMask = 1.0 - smoothstep(0.0, 0.035, abs(normalized.y - _ScanLine));
                half3 light = half3(0.7, 0.82, 1.0);
                baseColor.rgb = lerp(baseColor.rgb, baseColor.rgb + light * 0.35, scanLineMask);
                return baseColor;
            }
            ENDHLSL
        }
    }
}
