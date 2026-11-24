using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace DNPDemo.Environment
{
    /// <summary>
    /// Fades every renderer under a root together, leaving colliders alone. Fully shown, the renderers keep their own
    /// opaque materials; fully hidden, they are disabled so they neither draw nor cast shadows. Transparent copies of the
    /// materials are used only in between, because a transparent wall writes no depth and sorts against other
    /// transparent objects.
    /// </summary>
    internal sealed class RendererFadeGroup : IDisposable
    {
        static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        static readonly int LegacyColorId = Shader.PropertyToID("_Color");
        static readonly int SurfaceId = Shader.PropertyToID("_Surface");

        sealed class Entry
        {
            public Renderer Renderer;
            public bool InitiallyEnabled;
            public Material[] OriginalMaterials;
            public Material[] FadeMaterials;
            public Color[] BaseColors;
            public int[] ColorPropertyIds;
        }

        readonly List<Entry> _entries = new();
        readonly Object _logContext;
        bool _fadeMaterialsCreated;
        bool _reportedUnsupportedMaterial;

        public RendererFadeGroup(GameObject root, Object logContext)
        {
            _logContext = logContext;
            foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                _entries.Add(new Entry
                {
                    Renderer = renderer,
                    InitiallyEnabled = renderer.enabled,
                    OriginalMaterials = renderer.sharedMaterials,
                });
            }
        }

        public float Alpha { get; private set; } = 1f;

        public void SetAlpha(float alpha)
        {
            Alpha = Mathf.Clamp01(alpha);
            bool partial = Alpha > 0f && Alpha < 1f;
            if (partial)
            {
                EnsureFadeMaterials();
            }
            foreach (Entry entry in _entries)
            {
                if (entry.Renderer == null)
                {
                    continue;
                }
                entry.Renderer.enabled = entry.InitiallyEnabled && Alpha > 0f;
                if (!partial)
                {
                    entry.Renderer.sharedMaterials = entry.OriginalMaterials;
                    continue;
                }
                for (int i = 0; i < entry.FadeMaterials.Length; i++)
                {
                    Material fade = entry.FadeMaterials[i];
                    if (fade == null || fade == entry.OriginalMaterials[i])
                    {
                        continue;
                    }
                    Color color = entry.BaseColors[i];
                    color.a *= Alpha;
                    fade.SetColor(entry.ColorPropertyIds[i], color);
                }
                entry.Renderer.sharedMaterials = entry.FadeMaterials;
            }
        }

        /// <summary>Puts the renderers back as they were found and destroys the transparent copies.</summary>
        public void Dispose()
        {
            foreach (Entry entry in _entries)
            {
                if (entry.Renderer != null)
                {
                    entry.Renderer.sharedMaterials = entry.OriginalMaterials;
                    entry.Renderer.enabled = entry.InitiallyEnabled;
                }
                if (entry.FadeMaterials == null)
                {
                    continue;
                }
                for (int i = 0; i < entry.FadeMaterials.Length; i++)
                {
                    Material fade = entry.FadeMaterials[i];
                    if (fade != null && fade != entry.OriginalMaterials[i])
                    {
                        DestroyRuntimeObject(fade);
                    }
                }
            }
            _entries.Clear();
        }

        void EnsureFadeMaterials()
        {
            if (_fadeMaterialsCreated)
            {
                return;
            }
            _fadeMaterialsCreated = true;
            foreach (Entry entry in _entries)
            {
                Material[] originals = entry.OriginalMaterials;
                entry.FadeMaterials = new Material[originals.Length];
                entry.BaseColors = new Color[originals.Length];
                entry.ColorPropertyIds = new int[originals.Length];
                for (int i = 0; i < originals.Length; i++)
                {
                    Material original = originals[i];
                    entry.FadeMaterials[i] = original;
                    if (original == null)
                    {
                        continue;
                    }
                    int colorProperty = original.HasProperty(BaseColorId) ? BaseColorId
                        : original.HasProperty(LegacyColorId) ? LegacyColorId
                        : 0;
                    if (colorProperty == 0 || !original.HasProperty(SurfaceId))
                    {
                        // Stays opaque while fading and disappears at the end of the fade.
                        if (!_reportedUnsupportedMaterial)
                        {
                            Debug.LogWarning($"[DNPDemo] マテリアル「{original.name}」は URP Lit ではないため、半透明にならずにフェードの最後で消えます。", entry.Renderer);
                            _reportedUnsupportedMaterial = true;
                        }
                        continue;
                    }
                    var fade = new Material(original) { name = original.name + " (Fade)", hideFlags = HideFlags.DontSave };
                    MakeTransparent(fade);
                    entry.FadeMaterials[i] = fade;
                    entry.BaseColors[i] = original.GetColor(colorProperty);
                    entry.ColorPropertyIds[i] = colorProperty;
                }
            }
        }

        static void MakeTransparent(Material material)
        {
            material.SetFloat(SurfaceId, 1f);
            material.SetFloat("_Blend", 0f);
            material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            material.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            material.SetFloat("_SrcBlendAlpha", (float)BlendMode.One);
            material.SetFloat("_DstBlendAlpha", (float)BlendMode.OneMinusSrcAlpha);
            material.SetFloat("_ZWrite", 0f);
            material.SetOverrideTag("RenderType", "Transparent");
            material.renderQueue = (int)RenderQueue.Transparent;
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.DisableKeyword("_ALPHATEST_ON");
        }

        static void DestroyRuntimeObject(Object target)
        {
            if (Application.isPlaying)
            {
                Object.Destroy(target);
            }
            else
            {
                Object.DestroyImmediate(target);
            }
        }
    }
}
