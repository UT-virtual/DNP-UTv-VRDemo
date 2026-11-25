// Range selection volume (SelectionVolumeView): many overlapping spheres drawn as one translucent shape.
// Each pixel is blended once. The first sphere to reach a pixel sets a stencil bit and the rest skip it, so overlaps
// do not darken and the result does not depend on draw order. The stencil bit (128) is unused by URP forward/forward+
// and by uGUI masks, which take the low bits.
// The whole shape pulses slowly, and each sphere's edge facing away from the viewer is brightened (fresnel). Because the
// sphere that wins a pixel is not always the outermost one, the rim also shows faintly along sphere seams inside the
// shape; keep _RimIntensity low, or 0 to remove it.
// _BaseColor is overwritten per draw by the view (its alpha fades the selection out), so every term scales with its alpha.
Shader "DNPDemo/SelectionVolume"
{
    Properties
    {
        _BaseColor ("Color", Color) = (0.1, 0.85, 0.75, 0.35)
        _RimColor ("Rim Color", Color) = (0.6, 1, 0.95, 1)
        _RimPower ("Rim Power", Range(0.5, 8)) = 3
        _RimIntensity ("Rim Intensity", Range(0, 2)) = 0.35
        _PulseSpeed ("Pulse Speed (Hz)", Range(0, 4)) = 0.5
        _PulseAmount ("Pulse Amount", Range(0, 1)) = 0.2
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
                half4 _RimColor;
                float _RimPower;
                float _RimIntensity;
                float _PulseSpeed;
                float _PulseAmount;
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
                float3 normalWS : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                // The mesh is a unit sphere around its origin, so the position is the normal.
                output.normalWS = TransformObjectToWorldNormal(normalize(input.positionOS.xyz));
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                float3 normalWS = normalize(input.normalWS);
                float3 viewWS = GetWorldSpaceNormalizeViewDir(input.positionWS);
                float rim = pow(1.0 - saturate(dot(normalWS, viewWS)), _RimPower) * _RimIntensity;
                float pulse = 1.0 + _PulseAmount * sin(_Time.y * 6.2831853 * _PulseSpeed);

                half3 color = _BaseColor.rgb + _RimColor.rgb * rim;
                half alpha = saturate(_BaseColor.a * (pulse + rim * _RimColor.a));
                return half4(color, alpha);
            }
            ENDHLSL
        }
    }
}
