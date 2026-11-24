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
            #include "UnityCG.cginc"

            sampler2D _PanoramaA;
            sampler2D _PanoramaB;
            float _Blend;
            float _ExposureA;
            float _ExposureB;
            float _RotationA;
            float _RotationB;

            struct appdata
            {
                float4 vertex : POSITION;
            };

            struct v2f
            {
                float4 position : SV_POSITION;
                float3 direction : TEXCOORD0;
            };

            v2f vert(appdata input)
            {
                v2f output;
                output.position = UnityObjectToClipPos(input.vertex);
                output.direction = input.vertex.xyz;
                return output;
            }

            float2 PanoramaUv(float3 direction, float rotation)
            {
                direction = normalize(direction);
                float longitude = atan2(direction.x, direction.z) + radians(rotation);
                float latitude = asin(clamp(direction.y, -1.0, 1.0));
                return float2(longitude / (2.0 * UNITY_PI) + 0.5, latitude / UNITY_PI + 0.5);
            }

            fixed4 frag(v2f input) : SV_Target
            {
                float3 direction = normalize(input.direction);
                float3 source = tex2D(_PanoramaA, PanoramaUv(direction, _RotationA)).rgb * exp2(_ExposureA);
                float3 target = tex2D(_PanoramaB, PanoramaUv(direction, _RotationB)).rgb * exp2(_ExposureB);
                return fixed4(lerp(source, target, _Blend), 1.0);
            }
            ENDCG
        }

        Pass
        {
            CGPROGRAM
            #pragma target 3.0
            #pragma vertex vert_img
            #pragma fragment fragSnapshot
            #include "UnityCG.cginc"

            sampler2D _PanoramaA;
            sampler2D _PanoramaB;
            float _Blend;
            float _ExposureA;
            float _ExposureB;
            float _RotationA;
            float _RotationB;

            float3 DirectionFromUv(float2 uv)
            {
                float longitude = (uv.x - 0.5) * 2.0 * UNITY_PI;
                float latitude = (uv.y - 0.5) * UNITY_PI;
                float latitudeCos = cos(latitude);
                return float3(sin(longitude) * latitudeCos, sin(latitude), cos(longitude) * latitudeCos);
            }

            float2 PanoramaUv(float3 direction, float rotation)
            {
                direction = normalize(direction);
                float longitude = atan2(direction.x, direction.z) + radians(rotation);
                float latitude = asin(clamp(direction.y, -1.0, 1.0));
                return float2(longitude / (2.0 * UNITY_PI) + 0.5, latitude / UNITY_PI + 0.5);
            }

            fixed4 fragSnapshot(v2f_img input) : SV_Target
            {
                float3 direction = DirectionFromUv(input.uv);
                float3 source = tex2D(_PanoramaA, PanoramaUv(direction, _RotationA)).rgb * exp2(_ExposureA);
                float3 target = tex2D(_PanoramaB, PanoramaUv(direction, _RotationB)).rgb * exp2(_ExposureB);
                return fixed4(lerp(source, target, _Blend), 1.0);
            }
            ENDCG
        }
    }
    Fallback Off
}
