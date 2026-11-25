// Dissolve on its own (Dissolve.hlsl): unlit base map and colour that burns away from the noise as _Cutoff goes from 0
// (shown) to 1 (hidden), with a glowing edge. The statue's version change uses ModelTransition.shader, which combines this
// with the scan line; this one is for objects that only dissolve.
Shader "DNPDemo/Dissolve"
{
    Properties
    {
        _BaseMap ("Base Map", 2D) = "white" {}
        _BaseColor ("Base Color", Color) = (1, 1, 1, 1)
        _Cutoff ("Cutoff", Range(0, 1)) = 0
        [NoScaleOffset] _NoiseMap ("Noise Map", 2D) = "gray" {}
        _NoiseScale ("Noise Scale (per metre)", Range(0.1, 40)) = 6
        _EdgeWidth ("Edge Width", Range(0, 0.3)) = 0.06
        [HDR] _EdgeColor ("Edge Color", Color) = (1, 0.541, 0, 1)
        _EdgeIntensity ("Edge Intensity", Range(0, 8)) = 2
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "TransparentCutout"
            "Queue" = "AlphaTest"
            "IgnoreProjector" = "True"
        }

        Pass
        {
            Name "Dissolve"
            Tags { "LightMode" = "UniversalForward" }

            ZWrite On
            Cull Back

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Dissolve.hlsl"

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);
            TEXTURE2D(_NoiseMap);
            SAMPLER(sampler_NoiseMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4 _BaseColor;
                float _Cutoff;
                float _NoiseScale;
                float _EdgeWidth;
                half4 _EdgeColor;
                float _EdgeIntensity;
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
                float noise = DissolveNoise(TEXTURE2D_ARGS(_NoiseMap, sampler_NoiseMap), input.positionWS, normalize(input.normalWS), _NoiseScale);
                float edge = ApplyDissolve(noise, _Cutoff, _EdgeWidth);
                half4 color = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv) * _BaseColor;
                color.rgb += _EdgeColor.rgb * (_EdgeIntensity * edge);
                return half4(color.rgb, 1.0);
            }
            ENDHLSL
        }
    }
}
