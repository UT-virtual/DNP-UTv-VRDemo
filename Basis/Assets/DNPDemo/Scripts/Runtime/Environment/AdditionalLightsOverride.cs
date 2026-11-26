using System;
using UnityEngine.Rendering.Universal;

namespace DNPDemo.Environment
{
    /// <summary>
    /// Keeps the URP asset's additional lights on. Basis' Very Low graphics quality, the default on Quest, turns them off
    /// (SMModuleQualityAndQualitySetURP), which hides the statue's spot light and fill light. Forward+ shades additional
    /// lights per pixel, so that is the mode written. URP's setter is internal, so the value goes through Basis' own
    /// reflection handle.
    /// </summary>
    internal sealed class AdditionalLightsOverride : PipelineAssetOverride<LightRenderingMode>
    {
        public AdditionalLightsOverride(Func<UniversalRenderPipelineAsset> assetProvider) : base(assetProvider)
        {
            BasisUrpQualityFields.EnsureResolved();
        }

        /// <summary>Turns the additional lights on until <see cref="PipelineAssetOverride{TValues}.Suspend"/>.</summary>
        public void Keep() => RequestValues(LightRenderingMode.PerPixel);

        protected override LightRenderingMode Read(UniversalRenderPipelineAsset asset) => asset.additionalLightsRenderingMode;

        protected override void Write(UniversalRenderPipelineAsset asset, LightRenderingMode mode) =>
            BasisUrpQualityFields.AdditionalLightsRenderingMode.Set(asset, mode);

        protected override bool Matches(LightRenderingMode a, LightRenderingMode b) => a == b;
    }
}
