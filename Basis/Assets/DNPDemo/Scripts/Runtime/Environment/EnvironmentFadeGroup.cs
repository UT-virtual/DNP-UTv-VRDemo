using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace DNPDemo.Environment
{
    /// <summary>
    /// Renderers that an environment preset fades in and out, such as the exhibition room. The renderers and their transparent
    /// materials are listed on the prefab (EnvironmentPresetBuilder generates them), so the transparent shader variants are
    /// part of the build.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class EnvironmentFadeGroup : MonoBehaviour
    {
        static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        static readonly int ZWriteId = Shader.PropertyToID("_ZWrite");

        /// <summary>A renderer and the transparent counterparts of its materials, slot for slot.</summary>
        [Serializable]
        public struct Entry
        {
            public Renderer Renderer;
            public Material[] FadeMaterials;
        }

        /// <summary>
        /// How the renderers are drawn. Transparent materials do not sort correctly against the group's own faces or other
        /// transparent objects, so they are only used while the group is partially visible.
        /// </summary>
        enum Display
        {
            /// <summary>Fully visible with the renderers' own opaque materials and shadow settings.</summary>
            Opaque,
            /// <summary>Partially visible with the transparent materials. Casts no shadows, as a fading wall should not darken the scene.</summary>
            Fading,
            /// <summary>Not drawn at all. Colliders are left untouched.</summary>
            Hidden,
        }

        sealed class Target
        {
            public Renderer Renderer;
            public bool Enabled;
            public ShadowCastingMode ShadowCasting;
            public Material[] OpaqueMaterials;
            public Material[] FadeMaterials;
        }

        sealed class FadeMaterial
        {
            public Material Instance;
            public Color BaseColor;
        }

        [SerializeField] Entry[] _entries = Array.Empty<Entry>();

        Target[] _targets;
        readonly Dictionary<Material, FadeMaterial> _fadeMaterials = new();
        Display _display = Display.Opaque;
        float _alpha = 1f;

        /// <summary>Visibility from 0 (hidden) to 1 (opaque).</summary>
        public float Alpha => _alpha;

        internal int EntryCount => _entries.Length;

        internal Entry GetEntry(int index) => _entries[index];

        /// <summary>Replaces the listed renderers. Called by the prefab builder and by tests before the group is first shown.</summary>
        internal void SetEntries(Entry[] entries)
        {
            if (_targets != null)
            {
                throw new InvalidOperationException("The fade entries must be set before the group is first faded.");
            }
            _entries = entries ?? throw new ArgumentNullException(nameof(entries));
        }

        public void SetAlpha(float alpha)
        {
            alpha = Mathf.Clamp01(alpha);
            EnsureTargets();
            _alpha = alpha;
            Display display = alpha >= 1f ? Display.Opaque : alpha <= 0f ? Display.Hidden : Display.Fading;
            if (display == Display.Fading)
            {
                foreach (FadeMaterial fade in _fadeMaterials.Values)
                {
                    Color color = fade.BaseColor;
                    color.a *= alpha;
                    fade.Instance.SetColor(BaseColorId, color);
                }
            }
            if (display == _display)
            {
                return;
            }
            _display = display;
            foreach (Target target in _targets)
            {
                ApplyDisplay(target, display);
            }
        }

        /// <summary>Records the renderers as authored on the first fade; they are restored to this state when fully visible.</summary>
        void EnsureTargets()
        {
            if (_targets != null)
            {
                return;
            }
            _targets = new Target[_entries.Length];
            for (int i = 0; i < _entries.Length; i++)
            {
                Entry entry = _entries[i];
                Material[] opaque = entry.Renderer != null ? entry.Renderer.sharedMaterials : Array.Empty<Material>();
                Material[] fade = entry.FadeMaterials ?? Array.Empty<Material>();
                if (entry.Renderer == null || fade.Length != opaque.Length)
                {
                    Debug.LogError($"[DNPDemo] {name} のフェード対象 {i} 番の Renderer かマテリアルの数が正しくありません。" +
                        "DNP Demo/Setup/Build Environment Presets で作り直してください。", this);
                    fade = opaque;
                }
                else
                {
                    fade = Array.ConvertAll(fade, InstantiateFadeMaterial);
                }
                _targets[i] = new Target
                {
                    Renderer = entry.Renderer,
                    Enabled = entry.Renderer != null && entry.Renderer.enabled,
                    ShadowCasting = entry.Renderer != null ? entry.Renderer.shadowCastingMode : ShadowCastingMode.Off,
                    OpaqueMaterials = opaque,
                    FadeMaterials = fade,
                };
            }
        }

        /// <summary>
        /// One runtime copy per fade asset, shared by the renderers that use it. URP's material validation turns depth writing
        /// off for transparent Lit assets, but the fading group keeps writing depth so its own faces, including the back faces
        /// of double-sided materials, still occlude each other. A copy keeps the asset's keywords, so it uses the built variant.
        /// </summary>
        Material InstantiateFadeMaterial(Material asset)
        {
            if (asset == null)
            {
                return null;
            }
            if (!_fadeMaterials.TryGetValue(asset, out FadeMaterial fade))
            {
                var instance = new Material(asset) { name = $"{asset.name} (Runtime)", hideFlags = HideFlags.DontSave };
                instance.SetFloat(ZWriteId, 1f);
                fade = new FadeMaterial
                {
                    Instance = instance,
                    BaseColor = instance.HasProperty(BaseColorId) ? instance.GetColor(BaseColorId) : Color.white,
                };
                _fadeMaterials.Add(asset, fade);
            }
            return fade.Instance;
        }

        static void ApplyDisplay(Target target, Display display)
        {
            Renderer renderer = target.Renderer;
            if (renderer == null)
            {
                return;
            }
            switch (display)
            {
                case Display.Opaque:
                    renderer.sharedMaterials = target.OpaqueMaterials;
                    renderer.shadowCastingMode = target.ShadowCasting;
                    renderer.enabled = target.Enabled;
                    break;
                case Display.Fading:
                    renderer.sharedMaterials = target.FadeMaterials;
                    renderer.shadowCastingMode = ShadowCastingMode.Off;
                    renderer.enabled = target.Enabled;
                    break;
                case Display.Hidden:
                    renderer.enabled = false;
                    break;
            }
        }

        /// <summary>Puts the renderers back on their own materials before the copies they may be drawing with are destroyed.</summary>
        void OnDestroy()
        {
            if (_targets != null)
            {
                foreach (Target target in _targets)
                {
                    ApplyDisplay(target, Display.Opaque);
                }
            }
            foreach (FadeMaterial fade in _fadeMaterials.Values)
            {
                if (Application.isPlaying)
                {
                    Destroy(fade.Instance);
                }
                else
                {
                    DestroyImmediate(fade.Instance);
                }
            }
            _fadeMaterials.Clear();
        }
    }
}
