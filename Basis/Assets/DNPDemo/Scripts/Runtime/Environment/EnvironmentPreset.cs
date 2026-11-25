using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace DNPDemo.Environment
{
    /// <summary>
    /// How a preset sets the spot light's activation. Keep leaves it as it is, so lights switched by other cues survive the
    /// change; the remaining spot light settings are applied either way.
    /// </summary>
    public enum SpotLightMode
    {
        Keep,
        On,
        Off,
    }

    [Serializable]
    public struct DirectionalLightSettings
    {
        public Color Color;
        public float Intensity;
        public Vector3 Rotation;
        public LightShadows Shadows;
        public LightShadowResolution ShadowResolution;
        public float ShadowStrength;
        public float ShadowBias;
        public float ShadowNormalBias;
        public float ShadowNearPlane;
    }

    [Serializable]
    public struct SpotLightSettings
    {
        public SpotLightMode Enabled;
        public Color Color;
        public float Intensity;
        public Vector3 Rotation;
        public float Range;
        public float SpotAngle;
        public float InnerSpotAngle;
        public LightShadows Shadows;
        public LightShadowResolution ShadowResolution;
        public float ShadowStrength;
        public float ShadowBias;
        public float ShadowNormalBias;
        public float ShadowNearPlane;
    }

    [Serializable]
    public struct AmbientSettings
    {
        public AmbientMode Mode;
        public Color SkyColor;
        public Color EquatorColor;
        public Color GroundColor;
        public Color FlatColor;
        public float Intensity;
    }

    /// <summary>
    /// Environment reflection applied at runtime. Resolution and bounces are only read when lighting is baked, so they are
    /// left to the scene's Lighting settings.
    /// </summary>
    [Serializable]
    public struct ReflectionSettings
    {
        public DefaultReflectionMode Mode;
        public Texture CustomReflectionTexture;
        public float Intensity;
    }

    /// <summary>
    /// Main light shadow quality written to the active URP asset while the preset is shown. Override off leaves the values
    /// that Basis' shadow quality setting chose; Override on replaces them until a preset without it is applied.
    /// </summary>
    [Serializable]
    public struct ShadowQualitySettings
    {
        public bool Override;
        public int MainLightShadowmapResolution;
        public float ShadowDistance;
        [Range(1, 4)] public int CascadeCount;
    }

    [CreateAssetMenu(menuName = "DNP Demo/Environment Preset", fileName = "EnvironmentPreset")]
    public sealed class EnvironmentPreset : ScriptableObject
    {
        [SerializeField] Texture2D _panorama;
        [SerializeField] float _exposure;
        [SerializeField] float _rotation;
        [SerializeField] bool _exhibitionRoomVisible;
        [SerializeField] DirectionalLightSettings _directionalLight;
        [SerializeField] SpotLightSettings _spotLight;
        [SerializeField] AmbientSettings _ambient;
        [SerializeField] ReflectionSettings _reflection;
        [SerializeField] ShadowQualitySettings _shadowQuality;

        public Texture2D Panorama => _panorama;
        public float Exposure => _exposure;
        public float Rotation => _rotation;
        public bool ExhibitionRoomVisible => _exhibitionRoomVisible;
        public DirectionalLightSettings DirectionalLight => _directionalLight;
        public SpotLightSettings SpotLight => _spotLight;
        public AmbientSettings Ambient => _ambient;
        public ReflectionSettings Reflection => _reflection;
        public ShadowQualitySettings ShadowQuality => _shadowQuality;

        internal void Configure(
            Texture2D panorama,
            float exposure,
            float rotation,
            bool exhibitionRoomVisible,
            DirectionalLightSettings directionalLight,
            SpotLightSettings spotLight,
            AmbientSettings ambient,
            ReflectionSettings reflection,
            ShadowQualitySettings shadowQuality)
        {
            _shadowQuality = shadowQuality;
            _panorama = panorama;
            _exposure = exposure;
            _rotation = rotation;
            _exhibitionRoomVisible = exhibitionRoomVisible;
            _directionalLight = directionalLight;
            _spotLight = spotLight;
            _ambient = ambient;
            _reflection = reflection;
        }
    }
}
