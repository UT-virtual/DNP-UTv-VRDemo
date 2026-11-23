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

        static readonly int MainTexId = Shader.PropertyToID("_MainTex");
        static readonly int ColorId = Shader.PropertyToID("_Color");

        /// <summary>
        /// Swaps every renderer under <paramref name="model"/> to clones of <paramref name="transitionTemplate"/>
        /// that carry over each original material's base map and color, and returns the captures to restore later.
        /// </summary>
        public static List<ModelTransitionMaterial> Capture(GameObject model, Material transitionTemplate,
            string baseMapPropertyName, string baseColorPropertyName)
        {
            var captures = new List<ModelTransitionMaterial>();
            if (model == null || transitionTemplate == null)
            {
                return captures;
            }
            int baseMapId = Shader.PropertyToID(baseMapPropertyName);
            int baseColorId = Shader.PropertyToID(baseColorPropertyName);
            foreach (Renderer renderer in model.GetComponentsInChildren<Renderer>(true))
            {
                Material[] originals = renderer.sharedMaterials;
                var transitionMaterials = new Material[originals.Length];
                for (int i = 0; i < originals.Length; i++)
                {
                    transitionMaterials[i] = CreateTransitionMaterial(transitionTemplate, originals[i], baseMapId, baseColorId);
                }
                var capture = new ModelTransitionMaterial(renderer, transitionMaterials);
                capture.Apply();
                captures.Add(capture);
            }
            return captures;
        }

        static Material CreateTransitionMaterial(Material template, Material original, int baseMapId, int baseColorId)
        {
            var clone = new Material(template);
            if (original == null)
            {
                return clone;
            }
            if (!CopyTexture(clone, original, baseMapId))
            {
                CopyTexture(clone, original, MainTexId);
            }
            if (!CopyColor(clone, original, baseColorId))
            {
                CopyColor(clone, original, ColorId);
            }
            return clone;
        }

        static bool CopyTexture(Material to, Material from, int id)
        {
            if (!to.HasProperty(id) || !from.HasProperty(id))
            {
                return false;
            }
            to.SetTexture(id, from.GetTexture(id));
            return true;
        }

        static bool CopyColor(Material to, Material from, int id)
        {
            if (!to.HasProperty(id) || !from.HasProperty(id))
            {
                return false;
            }
            to.SetColor(id, from.GetColor(id));
            return true;
        }

        void Apply()
        {
            if (_renderer == null || _transitionMaterials.Length == 0)
            {
                return;
            }
            _renderer.sharedMaterials = _transitionMaterials;
            _applied = true;
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
                if (material.HasProperty(dissolveId))
                {
                    material.SetFloat(dissolveId, dissolve);
                }
                if (material.HasProperty(scanLineId))
                {
                    material.SetFloat(scanLineId, scanLine);
                }
                if (material.HasProperty(boundsMinId))
                {
                    material.SetVector(boundsMinId, boundsMin);
                }
                if (material.HasProperty(boundsMaxId))
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
