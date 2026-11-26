// Pen lines (StrokeView). Each line is a LineRenderer whose texture coordinate runs from 0 at the first point to 1 at the
// last one (Texture Mode = Stretch), so the end being drawn glows slightly. Opaque: a line that fades out narrows instead.
Shader "DNPDemo/StrokeLine"
{
    Properties
    {
        _BaseColor ("Color", Color) = (0, 0.898, 1, 1)
        [HDR] _TipColor ("Tip Color", Color) = (1, 1, 1, 1)
        _TipLength ("Tip Length (fraction of the line)", Range(0, 0.5)) = 0.06
        _TipIntensity ("Tip Intensity", Range(0, 4)) = 0.8
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Opaque"
            "Queue" = "Geometry"
            "IgnoreProjector" = "True"
        }

        Pass
        {
            Name "StrokeLine"
            Tags { "LightMode" = "UniversalForward" }

            ZWrite On
            Cull Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                half4 _TipColor;
                float _TipLength;
                float _TipIntensity;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                half4 color : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                half4 color : COLOR;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                output.color = input.color;
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                float tip = smoothstep(1.0 - max(_TipLength, 1e-4), 1.0, input.uv.x);
                half3 color = _BaseColor.rgb * input.color.rgb + _TipColor.rgb * (_TipIntensity * tip);
                return half4(color, 1.0);
            }
            ENDHLSL
        }
    }
}
