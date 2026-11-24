using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace DNPDemo.Environment
{
    [Serializable]
    public struct DirectionalLightSettings
    {
        public Color Color;
        public float Intensity;
        public Vector3 Rotation;
        public LightShadows Shadows;
        public float ShadowStrength;
        public float ShadowBias;
        public float ShadowNormalBias;
        public float ShadowNearPlane;
    }

    /// <summary>
    /// How the statue spotlight looks in this environment. Where it points and whether it is on belong to the Lighting
    /// prefab and the scenario cues, so a preset only changes its colour, brightness and shadows.
    /// </summary>
    [Serializable]
    public struct SpotLightSettings
    {
        public Color Color;
        public float Intensity;
        public LightShadows Shadows;
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
    /// The default reflection. Changing the skybox at runtime does not re-render the skybox reflection, so an environment
    /// that needs its own reflections supplies a custom cubemap.
    /// </summary>
    [Serializable]
    public struct ReflectionSettings
    {
        public DefaultReflectionMode Mode;
        public Texture CustomReflectionTexture;
        public float Intensity;
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

        public Texture2D Panorama => _panorama;
        public float Exposure => _exposure;
        public float Rotation => _rotation;
        public bool ExhibitionRoomVisible => _exhibitionRoomVisible;
        public DirectionalLightSettings DirectionalLight => _directionalLight;
        public SpotLightSettings SpotLight => _spotLight;
        public AmbientSettings Ambient => _ambient;
        public ReflectionSettings Reflection => _reflection;

        internal void Configure(
            Texture2D panorama,
            float exposure,
            float rotation,
            bool exhibitionRoomVisible,
            DirectionalLightSettings directionalLight,
            SpotLightSettings spotLight,
            AmbientSettings ambient,
            ReflectionSettings reflection)
        {
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
