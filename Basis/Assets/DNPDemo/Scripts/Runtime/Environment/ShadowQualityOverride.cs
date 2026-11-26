using System;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace DNPDemo.Environment
{
    /// <summary>
    /// Writes a preset's main light shadow quality into the active URP asset. Basis' shadow quality setting
    /// (SMModuleShadowQualityURP) writes the same fields, which <see cref="PipelineAssetOverride{TValues}"/> takes care of.
    /// </summary>
    internal sealed class ShadowQualityOverride : PipelineAssetOverride<ShadowQualityOverride.Values>
    {
        internal struct Values
        {
            public int Resolution;
            public float Distance;
            public int Cascades;

            public static Values From(ShadowQualitySettings settings) => new()
            {
                Resolution = settings.MainLightShadowmapResolution,
                Distance = Mathf.Max(0f, settings.ShadowDistance),
                Cascades = Mathf.Clamp(settings.CascadeCount, 1, 4),
            };
        }

        ShadowQualitySettings _requested;

        public ShadowQualityOverride(Func<UniversalRenderPipelineAsset> assetProvider) : base(assetProvider)
        {
        }

        /// <summary>The settings last passed to <see cref="Request"/>, whether or not they are on the asset right now.</summary>
        public ShadowQualitySettings Requested => _requested;

        /// <summary>Sets the settings to show. A request without Override puts the saved values back.</summary>
        public void Request(ShadowQualitySettings settings)
        {
            _requested = settings;
            RequestValues(settings.Override ? Values.From(settings) : null);
        }

        protected override Values Read(UniversalRenderPipelineAsset asset) => new()
        {
            Resolution = asset.mainLightShadowmapResolution,
            Distance = asset.shadowDistance,
            Cascades = asset.shadowCascadeCount,
        };

        protected override void Write(UniversalRenderPipelineAsset asset, Values values)
        {
            asset.mainLightShadowmapResolution = values.Resolution;
            asset.shadowDistance = values.Distance;
            asset.shadowCascadeCount = values.Cascades;
        }

        protected override bool Matches(Values a, Values b) =>
            a.Resolution == b.Resolution && Mathf.Approximately(a.Distance, b.Distance) && a.Cascades == b.Cascades;
    }
}
