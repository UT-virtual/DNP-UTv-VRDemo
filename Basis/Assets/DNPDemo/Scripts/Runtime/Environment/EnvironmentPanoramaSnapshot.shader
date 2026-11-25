// Bakes the environment sky's current blend into an equirectangular texture. EnvironmentController blits it with a copy
// of the sky material's properties, so a retarget mid-transition starts from what is on screen.
Shader "Hidden/DNPDemo/Environment Panorama Snapshot"
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
        Cull Off
        ZWrite Off
        ZTest Always

        Pass
        {
            CGPROGRAM
            #pragma target 3.0
            #pragma vertex vert_img
            #pragma fragment frag
            #include "EnvironmentPanorama.cginc"

            fixed4 frag(v2f_img input) : SV_Target
            {
                return fixed4(BlendedSky(DirectionFromUv(input.uv)), 1.0);
            }
            ENDCG
        }
    }
    Fallback Off
}
