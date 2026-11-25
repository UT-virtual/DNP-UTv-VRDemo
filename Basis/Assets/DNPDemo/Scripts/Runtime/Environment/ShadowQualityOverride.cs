using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace DNPDemo.Environment
{
    /// <summary>
    /// Writes a preset's main light shadow quality into the active URP asset and puts the previous values back afterwards.
    /// Basis' shadow quality setting (SMModuleShadowQualityURP) writes the same fields whenever the quality or performance
    /// mode changes, so <see cref="Maintain"/> re-applies the override and keeps Basis' newest values as the ones to restore.
    /// </summary>
    /// <remarks>
    /// The URP asset is a project asset. In the Editor a value left on it during Play Mode ends up in the .asset file, so the
    /// owner must call <see cref="Suspend"/> whenever it stops running.
    /// </remarks>
    internal sealed class ShadowQualityOverride
    {
        struct Values
        {
            public int Resolution;
            public float Distance;
            public int Cascades;

            public static Values Read(UniversalRenderPipelineAsset asset) => new()
            {
                Resolution = asset.mainLightShadowmapResolution,
                Distance = asset.shadowDistance,
                Cascades = asset.shadowCascadeCount,
            };

            public static Values From(ShadowQualitySettings settings) => new()
            {
                Resolution = settings.MainLightShadowmapResolution,
                Distance = Mathf.Max(0f, settings.ShadowDistance),
                Cascades = Mathf.Clamp(settings.CascadeCount, 1, 4),
            };

            public void Write(UniversalRenderPipelineAsset asset)
            {
                asset.mainLightShadowmapResolution = Resolution;
                asset.shadowDistance = Distance;
                asset.shadowCascadeCount = Cascades;
            }

            public bool Matches(Values other) =>
                Resolution == other.Resolution && Mathf.Approximately(Distance, other.Distance) && Cascades == other.Cascades;
        }

        readonly Func<UniversalRenderPipelineAsset> _assetProvider;
        ShadowQualitySettings _requested;
        UniversalRenderPipelineAsset _overriddenAsset;
        Values _saved;
        Values _applied;
        bool _suspended;

        /// <param name="assetProvider">Returns the URP asset currently in use. Tests pass an in-memory asset.</param>
        public ShadowQualityOverride(Func<UniversalRenderPipelineAsset> assetProvider)
        {
            _assetProvider = assetProvider ?? throw new ArgumentNullException(nameof(assetProvider));
        }

        /// <summary>The URP asset of the active quality level.</summary>
        public static UniversalRenderPipelineAsset ActiveAsset() =>
            (QualitySettings.renderPipeline ?? GraphicsSettings.defaultRenderPipeline) as UniversalRenderPipelineAsset;

        /// <summary>The settings last passed to <see cref="Request"/>, whether or not they are on the asset right now.</summary>
        public ShadowQualitySettings Requested => _requested;

        /// <summary>True while the override values are written on a URP asset.</summary>
        public bool IsOverriding => _overriddenAsset != null;

        /// <summary>Sets the settings to show. A request without Override puts the saved values back.</summary>
        public void Request(ShadowQualitySettings settings)
        {
            _requested = settings;
            Sync();
        }

        /// <summary>Puts the saved values back and stops overriding until <see cref="Resume"/>. The request is kept.</summary>
        public void Suspend()
        {
            _suspended = true;
            Release();
        }

        /// <summary>Re-applies the kept request after <see cref="Suspend"/>.</summary>
        public void Resume()
        {
            _suspended = false;
            Sync();
        }

        /// <summary>
        /// Re-applies the override when something else changed the asset's values or the active asset. Comparing three
        /// fields is all it costs while nothing has changed, so it is meant to run every frame.
        /// </summary>
        public void Maintain()
        {
            if (_suspended || !_requested.Override)
            {
                return;
            }
            Sync();
        }

        void Sync()
        {
            if (_suspended || !_requested.Override)
            {
                Release();
                return;
            }

            UniversalRenderPipelineAsset asset = _assetProvider();
            if (asset == null)
            {
                Release();
                return;
            }
            if (asset != _overriddenAsset)
            {
                // First activation, or the quality level switched to another URP asset: the old asset gets its values back
                // and the new one's values become the ones to restore.
                Release();
                _overriddenAsset = asset;
                _saved = Values.Read(asset);
            }
            else
            {
                Values current = Values.Read(asset);
                if (!current.Matches(_applied))
                {
                    // Basis wrote a new tier over the override; that tier is what the asset returns to afterwards.
                    _saved = current;
                }
            }

            _applied = Values.From(_requested);
            if (!Values.Read(asset).Matches(_applied))
            {
                _applied.Write(asset);
            }
        }

        void Release()
        {
            // A destroyed asset compares equal to null; there is nothing left to restore on it.
            if (_overriddenAsset != null)
            {
                _saved.Write(_overriddenAsset);
            }
            _overriddenAsset = null;
        }
    }
}
