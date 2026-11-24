// Uses the unit pyramid from Faked Volumetrics. CircleVolume is a linear, centered profile
// derived from the blue circle in the sample's baked volume. Its U coordinate is radial distance.
Shader "DNPDemo/Venus Spotlight Beam"
{
    Properties
    {
        [NoScaleOffset] _VolumeTexture ("Baked Radial Profile", 2D) = "black" {}
        [HDR] _BeamColor ("Beam Color", Color) = (1, 0.96, 0.89, 1)
        _BeamIntensity ("Beam Intensity", Range(0, 1)) = 0.25
        _FadeStart ("End Fade Start", Range(0, 0.99)) = 0.5
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
            "DisableBatching" = "True"
        }

        Pass
        {
            Name "SpotlightBeam"
            Tags { "LightMode" = "SRPDefaultUnlit" }

            Blend One One
            ColorMask RGB
            ZWrite Off
            ZTest LEqual
            // Draw the exit face once per pixel, including when the viewer is inside the volume.
            Cull Front

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_VolumeTexture);
            SAMPLER(sampler_VolumeTexture);

            CBUFFER_START(UnityPerMaterial)
                half4 _BeamColor;
                half _BeamIntensity;
                half _FadeStart;
            CBUFFER_END

            struct Attributes
            {
                float3 positionOS : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionOS : TEXCOORD0;
                float3 originOS : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                output.positionOS = input.positionOS;
                output.positionCS = TransformObjectToHClip(input.positionOS);
                if (IsPerspectiveProjection())
                {
                    output.originOS = TransformWorldToObject(GetCameraPositionWS());
                }
                else
                {
                    // Orthographic scene-view rays are parallel. Start beyond the unit pyramid.
                    float3 rayOS = TransformWorldToObjectDir(GetViewForwardDir());
                    output.originOS = input.positionOS - rayOS * 3.0;
                }
                return output;
            }

            float4 SideDistances(float3 positionOS)
            {
                float halfWidth = 0.5 * positionOS.z;
                return halfWidth + float4(positionOS.xy, -positionOS.xy);
            }

            float3 FindEntry(float3 originOS, float3 exitOS)
            {
                // The visible back face provides the exit. Clip the segment against the four sides
                // and the end cap to find where it first enters the pyramid.
                float4 outsideSides = max(0.0, -SideDistances(originOS));
                float4 insideSides = max(0.0, SideDistances(exitOS));
                float4 sideEntry = outsideSides / max(outsideSides + insideSides, 1e-5);
                float outsideEnd = max(0.0, originOS.z - 1.0);
                float endEntry = outsideEnd / max(outsideEnd + max(0.0, 1.0 - exitOS.z), 1e-5);
                float firstEntry = max(endEntry, max(max(sideEntry.x, sideEntry.y), max(sideEntry.z, sideEntry.w)));
                return lerp(originOS, exitOS, firstEntry);
            }

            float RadialProfileU(float3 entryOS, float3 exitOS)
            {
                // Project onto the end plane as in the volume bake. A rotationally symmetric
                // light needs only the projected line's distance from the center.
                float2 start = entryOS.xy / max(entryOS.z, 1e-4);
                float2 end = exitOS.xy / max(exitOS.z, 1e-4);
                float2 direction = end - start;
                float lengthSquared = dot(direction, direction);
                if (lengthSquared < 1e-8)
                {
                    return saturate(2.0 * length(end));
                }

                float offset = abs(dot(end, float2(direction.y, -direction.x))) * rsqrt(lengthSquared);
                return saturate(2.0 * offset);
            }

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                float3 entryOS = FindEntry(input.originOS, input.positionOS);
                float profileU = RadialProfileU(entryOS, input.positionOS);
                half density = SAMPLE_TEXTURE2D(_VolumeTexture, sampler_VolumeTexture, float2(profileU, 0.5)).r;
                float thickness = saturate(length(input.positionOS - entryOS));
                float midpoint = (entryOS.z + input.positionOS.z) * 0.5;
                float endFade = 1.0 - smoothstep(_FadeStart, 1.0, midpoint);
                return half4(density * _BeamColor.rgb * (_BeamIntensity * thickness * endFade), 0.0);
            }
            ENDHLSL
        }
    }
}
