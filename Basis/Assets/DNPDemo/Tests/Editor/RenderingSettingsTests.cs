using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace DNPDemo.Tests
{
    public class RenderingSettingsTests
    {
        /// <summary>
        /// In Forward, Unity assigns additional lights per object, and that assignment drops the statue's spot light for
        /// some camera positions and directions, leaving the statue, its pedestal and the floor unlit. Forward+ picks the
        /// lights per screen tile instead. See docs/BASIS_PATCHES.md.
        /// </summary>
        [Test]
        public void RenderersThatDrawAdditionalLightsUseForwardPlus()
        {
            for (int level = 0; level < QualitySettings.names.Length; level++)
            {
                if (QualitySettings.GetRenderPipelineAssetAt(level) is not UniversalRenderPipelineAsset asset ||
                    asset.additionalLightsRenderingMode == LightRenderingMode.Disabled)
                {
                    continue;
                }
                foreach (ScriptableRendererData data in asset.rendererDataList)
                {
                    if (data is UniversalRendererData renderer)
                    {
                        Assert.AreEqual(RenderingMode.ForwardPlus, renderer.renderingMode,
                            $"Quality level {QualitySettings.names[level]} draws additional lights with {renderer.name} in {renderer.renderingMode}.");
                    }
                }
            }
        }
    }
}
