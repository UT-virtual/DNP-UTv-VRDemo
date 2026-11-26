using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace DNPDemo.Environment
{
    internal static class PipelineAssets
    {
        /// <summary>The URP asset of the active quality level.</summary>
        public static UniversalRenderPipelineAsset Active() =>
            (QualitySettings.renderPipeline ?? GraphicsSettings.defaultRenderPipeline) as UniversalRenderPipelineAsset;
    }

    /// <summary>
    /// Writes values into the active URP asset and puts the previous ones back afterwards. Basis' quality modules
    /// (SMModuleQualityAndQualitySetURP, SMModuleShadowQualityURP) write the same asset whenever the quality or performance
    /// mode changes, so <see cref="Maintain"/> re-applies the override and keeps Basis' newest values as the ones to restore.
    /// </summary>
    /// <remarks>
    /// The URP asset is a project asset. In the Editor a value left on it during Play Mode ends up in the .asset file, so the
    /// owner must call <see cref="Suspend"/> whenever it stops running.
    /// </remarks>
    internal abstract class PipelineAssetOverride<TValues> where TValues : struct
    {
        readonly Func<UniversalRenderPipelineAsset> _assetProvider;
        TValues? _requested;
        UniversalRenderPipelineAsset _overriddenAsset;
        TValues _saved;
        TValues _applied;
        bool _suspended;

        /// <param name="assetProvider">Returns the URP asset currently in use. Tests pass an in-memory asset.</param>
        protected PipelineAssetOverride(Func<UniversalRenderPipelineAsset> assetProvider)
        {
            _assetProvider = assetProvider ?? throw new ArgumentNullException(nameof(assetProvider));
        }

        /// <summary>True while the override values are written on a URP asset.</summary>
        public bool IsOverriding => _overriddenAsset != null;

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
        /// Re-applies the override when something else changed the asset's values or the active asset. Comparing the values
        /// is all it costs while nothing has changed, so it is meant to run every frame.
        /// </summary>
        public void Maintain()
        {
            if (_suspended || !_requested.HasValue)
            {
                return;
            }
            Sync();
        }

        /// <summary>Sets the values to show. Null puts the saved values back.</summary>
        protected void RequestValues(TValues? values)
        {
            _requested = values;
            Sync();
        }

        protected abstract TValues Read(UniversalRenderPipelineAsset asset);

        protected abstract void Write(UniversalRenderPipelineAsset asset, TValues values);

        protected abstract bool Matches(TValues a, TValues b);

        void Sync()
        {
            if (_suspended || !_requested.HasValue)
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
                _saved = Read(asset);
            }
            else
            {
                TValues current = Read(asset);
                if (!Matches(current, _applied))
                {
                    // Basis wrote a new tier over the override; that tier is what the asset returns to afterwards.
                    _saved = current;
                }
            }

            _applied = _requested.Value;
            if (!Matches(Read(asset), _applied))
            {
                Write(asset, _applied);
            }
        }

        /// <summary>
        /// Puts the saved values back while the override is still what the asset holds. A value written after the override
        /// is newer than the saved one and stays; Basis, for one, restores the authored values when Play Mode exits, before
        /// the owner is destroyed.
        /// </summary>
        void Release()
        {
            // A destroyed asset compares equal to null; there is nothing left to restore on it.
            if (_overriddenAsset != null && Matches(Read(_overriddenAsset), _applied))
            {
                Write(_overriddenAsset, _saved);
            }
            _overriddenAsset = null;
        }
    }
}
