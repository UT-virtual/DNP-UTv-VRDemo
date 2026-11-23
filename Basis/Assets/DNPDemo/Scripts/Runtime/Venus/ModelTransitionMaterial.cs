using System.Collections.Generic;
using UnityEngine;

namespace DNPDemo.Venus
{
    internal sealed class ModelTransitionMaterial
    {
        readonly Renderer _renderer;
        readonly Material[] _originalMaterials;
        readonly Material[] _transitionMaterials;
        bool _applied;

        ModelTransitionMaterial(Renderer renderer, Material[] transitionMaterials)
        {
            _renderer = renderer;
            _originalMaterials = (Material[])renderer.sharedMaterials.Clone();
            _transitionMaterials = transitionMaterials;
        }

        public static List<ModelTransitionMaterial> Capture(GameObject model, Material transitionTemplate,
            string baseMapPropertyName, string baseColorPropertyName)
        {
            var captures = new List<ModelTransitionMaterial>();
            if (model == null)
            {
                return captures;
            }
            foreach (Renderer renderer in model.GetComponentsInChildren<Renderer>(true))
            {
                captures.Add(Capture(renderer, transitionTemplate, baseMapPropertyName, baseColorPropertyName));
            }
            return captures;
        }

        static ModelTransitionMaterial Capture(Renderer renderer, Material transitionTemplate,
            string baseMapPropertyName, string baseColorPropertyName)
        {
            Material[] originals = renderer.sharedMaterials;
            var transitionMaterials = new Material[originals.Length];
            for (int i = 0; i < originals.Length; i++)
            {
                if (transitionTemplate == null)
                {
                    transitionMaterials[i] = null;
                    continue;
                }
                var clone = new Material(transitionTemplate);
                Material original = originals[i];
                if (original != null)
                {
                    int mapId = Shader.PropertyToID(baseMapPropertyName);
                    if (clone.HasProperty(mapId) && original.HasProperty(mapId))
                    {
                        clone.SetTexture(mapId, original.GetTexture(mapId));
                    }
                    else if (clone.HasProperty("_MainTex") && original.HasProperty("_MainTex"))
                    {
                        clone.SetTexture("_MainTex", original.GetTexture("_MainTex"));
                    }

                    int colorId = Shader.PropertyToID(baseColorPropertyName);
                    if (clone.HasProperty(colorId) && original.HasProperty(colorId))
                    {
                        clone.SetColor(colorId, original.GetColor(colorId));
                    }
                    else if (clone.HasProperty("_Color") && original.HasProperty("_Color"))
                    {
                        clone.SetColor("_Color", original.HasProperty("_Color") ? original.GetColor("_Color") : Color.white);
                    }
                }
                transitionMaterials[i] = clone;
            }
            var capture = new ModelTransitionMaterial(renderer, transitionMaterials);
            capture.Apply();
            return capture;
        }

        void Apply()
        {
            if (_renderer == null)
            {
                return;
            }
            bool hasTransitionMaterial = false;
            foreach (Material material in _transitionMaterials)
            {
                if (material != null)
                {
                    hasTransitionMaterial = true;
                    break;
                }
            }
            if (hasTransitionMaterial)
            {
                _renderer.sharedMaterials = _transitionMaterials;
                _applied = true;
            }
        }

        public void SetProperties(int dissolveId, float dissolve, int scanLineId, float scanLine,
            int boundsMinId, Vector3 boundsMin, int boundsMaxId, Vector3 boundsMax)
        {
            if (!_applied)
            {
                return;
            }
            foreach (Material material in _transitionMaterials)
            {
                if (material == null)
                {
                    continue;
                }
                if (dissolveId >= 0 && material.HasProperty(dissolveId))
                {
                    material.SetFloat(dissolveId, dissolve);
                }
                if (scanLineId >= 0 && material.HasProperty(scanLineId))
                {
                    material.SetFloat(scanLineId, scanLine);
                }
                if (boundsMinId >= 0 && material.HasProperty(boundsMinId))
                {
                    material.SetVector(boundsMinId, boundsMin);
                }
                if (boundsMaxId >= 0 && material.HasProperty(boundsMaxId))
                {
                    material.SetVector(boundsMaxId, boundsMax);
                }
            }
        }

        public void Restore()
        {
            if (_renderer != null)
            {
                _renderer.sharedMaterials = _originalMaterials;
            }
            foreach (Material material in _transitionMaterials)
            {
                if (material != null)
                {
                    if (Application.isPlaying)
                    {
                        Object.Destroy(material);
                    }
                    else
                    {
                        Object.DestroyImmediate(material);
                    }
                }
            }
            _applied = false;
        }
    }
}
