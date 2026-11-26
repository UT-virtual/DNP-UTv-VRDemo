// Version change of the statue (ModelTransition). During a transition every renderer of the old and the new model is
// swapped to a copy of this material carrying the original base map and colour. The old model dissolves away while the
// new one dissolves in (Dissolve.hlsl), and a scan line runs down both (ScanLine.hlsl).
// Driven by ModelTransition every frame: _Cutoff (0 shown .. 1 hidden), _ScanFade (0..1, fades the scan line in and out
// over the transition) and _BoundsMin / _BoundsMax (the box the scan line runs through). The rest are for tuning.
Shader "DNPDemo/ModelTransition"
{
    Properties
    {
        _BaseMap ("Base Map", 2D) = "white" {}
        _BaseColor ("Base Color", Color) = (1, 1, 1, 1)

        [Header(Dissolve)]
        _Cutoff ("Cutoff (driven)", Range(0, 1)) = 0
        [NoScaleOffset] _NoiseMap ("Noise Map", 2D) = "gray" {}
        _NoiseScale ("Noise Scale (per metre)", Range(0.1, 40)) = 6
        _EdgeWidth ("Edge Width", Range(0, 0.3)) = 0.06
        [HDR] _EdgeColor ("Edge Color", Color) = (1, 0.541, 0, 1)
        _EdgeIntensity ("Edge Intensity", Range(0, 8)) = 2

        [Header(Scan Line)]
        [HDR] _ScanColor ("Scan Color", Color) = (1, 1, 1, 1)
        _Speed ("Speed (passes per second)", Range(0, 5)) = 1.5
        _Width ("Width", Range(0.001, 0.2)) = 0.015
        _Intensity ("Intensity", Range(0, 4)) = 0.8
        _ScanFade ("Scan Fade (driven)", Range(0, 1)) = 0
        _BoundsMin ("Bounds Min (driven)", Vector) = (0, 0, 0, 0)
        _BoundsMax ("Bounds Max (driven)", Vector) = (1, 1, 1, 0)
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
            #include "Dissolve.hlsl"
            #include "ScanLine.hlsl"

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
                half4 _ScanColor;
                float _Speed;
                float _Width;
                float _Intensity;
                float _ScanFade;
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

                float3 normalWS = normalize(input.normalWS);
                float noise = DissolveNoise(TEXTURE2D_ARGS(_NoiseMap, sampler_NoiseMap), input.positionWS, normalWS, _NoiseScale);
                float edge = ApplyDissolve(noise, _Cutoff, _EdgeWidth);

                half4 color = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv) * _BaseColor;
                color.rgb += _EdgeColor.rgb * (_EdgeIntensity * edge);

                float height = ScanLineHeight(input.positionWS, _BoundsMin, _BoundsMax);
                float band = ScanLineBand(height, _Time.y, _Speed, _Width);
                color.rgb += _ScanColor.rgb * (band * _Intensity * _ScanFade);
                return color;
            }
            ENDHLSL
        }
    }
}
