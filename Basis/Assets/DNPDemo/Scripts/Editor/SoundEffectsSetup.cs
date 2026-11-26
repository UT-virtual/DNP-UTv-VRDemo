using System.Collections.Generic;
using System.IO;
using DNPDemo.Audio;
using UnityEditor;
using UnityEngine;

namespace DNPDemo.Editor
{
    /// <summary>
    /// Sets up the sound effects: the catalog (<see cref="SeCatalog"/>) that maps each <see cref="SeEvent"/> to its file
    /// in <see cref="Folder"/>, the sound cues of the scenario (<see cref="ScenarioDataBuilder.VersionSounds"/>) and the
    /// player in the DemoSystems prefab (<see cref="ScenarioInstaller"/>).
    /// <para>
    /// The mp3 files are not in the repository (README.md「効果音（SE）の配置」). A file that is not placed yet leaves its
    /// reference empty and the event silent; running the menu again after placing it fills the empty references and
    /// leaves the others alone.
    /// </para>
    /// </summary>
    public static class SoundEffectsSetup
    {
        public const string Folder = DNPDemoProjectSetup.Root + "/Audio/SE";
        public const string CatalogPath = DNPDemoProjectSetup.Root + "/Data/SeCatalog.asset";

        /// <summary>File of each event in <see cref="Folder"/>. Keep it in line with the table in README.md.</summary>
        public static readonly IReadOnlyDictionary<SeEvent, string> FileNames = new Dictionary<SeEvent, string>
        {
            [SeEvent.Generate] = "se_generate.mp3",
            [SeEvent.Morph] = "se_morph.mp3",
            [SeEvent.SelectConfirm] = "se_select_confirm.mp3",
            [SeEvent.Snap] = "se_snap.mp3",
            [SeEvent.VoiceStart] = "se_voice_start.mp3",
            [SeEvent.VoiceEnd] = "se_voice_end.mp3",
            [SeEvent.TaskShow] = "se_task_show.mp3",
            [SeEvent.Complete] = "se_complete.mp3",
            [SeEvent.Fail] = "se_fail.mp3",
        };

        [MenuItem("DNP Demo/Setup/Install Sound Effects")]
        public static void InstallFromMenu()
        {
            SeCatalog catalog = CreateOrUpdateCatalog();
            ScenarioDataBuilder.EnsureVersionSoundsInExistingAsset();
            ScenarioInstaller.Install();

            var missing = new List<string>();
            foreach (KeyValuePair<SeEvent, string> pair in FileNames)
            {
                SeCatalog.Entry entry = catalog.Find(pair.Key);
                if (entry == null || entry.Clip == null)
                {
                    missing.Add(ClipPath(pair.Key));
                }
            }
            if (missing.Count > 0)
            {
                Debug.LogWarning($"[DNPDemo] 効果音のファイルが {missing.Count} 件ありません。置いたあとにもう一度実行してください（無いものは無音になります）。\n{string.Join("\n", missing)}", catalog);
            }
            else
            {
                Debug.Log($"[DNPDemo] 効果音を設定しました（{FileNames.Count} 件）。", catalog);
            }
        }

        public static string ClipPath(SeEvent seEvent) => $"{Folder}/{FileNames[seEvent]}";

        /// <summary>The clip of <paramref name="seEvent"/>, or null while its file is not placed.</summary>
        public static AudioClip LoadClip(SeEvent seEvent) =>
            FileNames.ContainsKey(seEvent) ? AssetDatabase.LoadAssetAtPath<AudioClip>(ClipPath(seEvent)) : null;

        /// <summary>
        /// Loads the catalog, or creates it, and gives every event an entry. Empty clips are filled from
        /// <see cref="Folder"/>; clips and volumes already set are kept.
        /// </summary>
        public static SeCatalog CreateOrUpdateCatalog()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<SeCatalog>(CatalogPath);
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<SeCatalog>();
                Directory.CreateDirectory(Path.GetDirectoryName(CatalogPath));
                AssetDatabase.CreateAsset(catalog, CatalogPath);
                Debug.Log($"[DNPDemo] Created {CatalogPath}");
            }
            Undo.RecordObject(catalog, "Update SE Catalog");
            bool changed = false;
            foreach (SeEvent seEvent in FileNames.Keys)
            {
                bool existed = catalog.Find(seEvent) != null;
                SeCatalog.Entry entry = catalog.GetOrAdd(seEvent);
                changed |= !existed;
                if (entry.Clip == null)
                {
                    AudioClip clip = LoadClip(seEvent);
                    if (clip != null)
                    {
                        entry.Clip = clip;
                        changed = true;
                    }
                }
            }
            if (changed)
            {
                EditorUtility.SetDirty(catalog);
                AssetDatabase.SaveAssetIfDirty(catalog);
            }
            return catalog;
        }
    }
}
