// Writes a mesh to depth only (no colour). Drawn over the history panel after the preview model, so objects behind the
// panel that draw later (transparent ones) are hidden by it.
// See DNPDemo.Interaction.History.HistoryRenderQueues for the order it is drawn in.
Shader "DNPDemo/HistoryPanelDepth"
{
    SubShader
    {
        Tags { "Queue"="Geometry+470" "IgnoreProjector"="True" "RenderType"="Opaque" }

        Cull Off
        ZWrite On
        ZTest LEqual
        ColorMask 0

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            v2f vert(appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.pos = UnityObjectToClipPos(v.vertex);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                return 0;
            }
            ENDCG
        }
    }
}
