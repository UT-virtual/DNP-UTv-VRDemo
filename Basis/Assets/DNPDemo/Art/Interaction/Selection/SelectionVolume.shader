// Range selection volume (SelectionVolumeView): many overlapping spheres drawn as one translucent shape.
// Each pixel is blended once. The first sphere to reach a pixel sets a stencil bit and the rest skip it, so overlaps
// do not darken and the result does not depend on draw order. The colour is flat for the same reason: which sphere
// wins a pixel is not the front-most one.
// The stencil bit (128) is unused by URP forward/forward+ and by uGUI masks, which take the low bits.
Shader "DNPDemo/SelectionVolume"
{
    Properties
    {
        _BaseColor ("Color", Color) = (1, 0, 0, 0.3)
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
            Name "SelectionVolume"
            Tags { "LightMode" = "UniversalForward" }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            ZTest LEqual
            Cull Back

            Stencil
            {
                Ref 128
                ReadMask 128
                WriteMask 128
                Comp NotEqual
                Pass Replace
            }

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                return _BaseColor;
            }
            ENDHLSL
        }
    }
}
