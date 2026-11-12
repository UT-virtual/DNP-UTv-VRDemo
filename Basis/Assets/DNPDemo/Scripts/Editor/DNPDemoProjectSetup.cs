using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace DNPDemo.Editor
{
    /// <summary>
    /// Project-level setup for the DNP x UTv demo: player settings and build scene order.
    /// Run from the menu, or headless: Unity.exe -batchmode -projectPath Basis -executeMethod DNPDemo.Editor.DNPDemoProjectSetup.RunAll -quit
    /// </summary>
    public static class DNPDemoProjectSetup
    {
        public const string Root = "Assets/DNPDemo";
        public const string MainScenePath = Root + "/Scenes/DNPDemo_Main.unity";
        const string InitScenePath = "Packages/com.basis.framework/Scenes/initialization.unity";

        public const string ProductName = "DNP UTv VR Demo";
        public const string CompanyName = "DNP UTv";
        public const string AndroidAppId = "com.dnputv.vrdemo";

        [MenuItem("DNP Demo/Setup/Run All")]
        public static void RunAll()
        {
            ApplyPlayerSettings();
            ApplyBuildScenes();
            AssetDatabase.SaveAssets();
            Debug.Log("[DNPDemo] Setup complete.");
        }

        [MenuItem("DNP Demo/Setup/Apply Player Settings")]
        public static void ApplyPlayerSettings()
        {
            PlayerSettings.productName = ProductName;
            PlayerSettings.companyName = CompanyName;
            PlayerSettings.SetApplicationIdentifier(UnityEditor.Build.NamedBuildTarget.Android, AndroidAppId);
            DemoBuildVersionPolicy.Apply();
        }

        [MenuItem("DNP Demo/Setup/Apply Build Scenes")]
        public static void ApplyBuildScenes()
        {
            // The demo scene boots directly as the world (it carries its own BasisScene).
            // initialization stays in the list but disabled so it can be switched back for debugging.
            var scenes = new List<EditorBuildSettingsScene>
            {
                new EditorBuildSettingsScene(MainScenePath, true),
            };
            foreach (EditorBuildSettingsScene existing in EditorBuildSettings.scenes)
            {
                if (existing.path == MainScenePath)
                {
                    continue;
                }
                scenes.Add(new EditorBuildSettingsScene(existing.path, existing.path != InitScenePath && existing.enabled));
            }
            EditorBuildSettings.scenes = scenes.ToArray();
        }
    }
}
