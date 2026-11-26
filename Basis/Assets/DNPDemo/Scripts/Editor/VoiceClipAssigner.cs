using System.Collections.Generic;
using System.Text;
using DNPDemo.Scenario;
using DNPDemo.Scenario.Cues;
using UnityEditor;
using UnityEngine;

namespace DNPDemo.Editor
{
    /// <summary>
    /// Assigns the recorded voice clips to the <see cref="LineCue"/>s of the scenario asset. The n-th line in play order
    /// (steps, then their cues) gets the clip named <c>{n:00}.wav</c> from <see cref="VoiceFolder"/>. Nothing is changed when the
    /// number of lines or clips does not match, so a revised script never ends up with voices shifted onto the wrong lines.
    /// </summary>
    public static class VoiceClipAssigner
    {
        /// <summary>Folder of the current voice set. One clip per line, numbered from 1 in play order.</summary>
        public const string VoiceFolder = DNPDemoProjectSetup.Root + "/Audio/Voice/SS_voice_2.2";

        /// <summary>Number of lines the voice set was recorded for.</summary>
        public const int ExpectedLineCount = 31;

        const string StepsProperty = "_steps";
        const string CuesProperty = nameof(ScenarioStep.Cues);
        const string VoiceProperty = nameof(LineCue.Voice);
        const string DialogTitle = "Assign Voice Clips";

        public static string ClipPath(int lineNumber) => $"{VoiceFolder}/{lineNumber:00}.wav";

        [MenuItem("DNP Demo/Setup/Assign Voice Clips")]
        public static void AssignFromMenu()
        {
            var data = AssetDatabase.LoadAssetAtPath<ScenarioData>(ScenarioDataBuilder.AssetPath);
            if (data == null)
            {
                Fail($"{ScenarioDataBuilder.AssetPath} が見つかりません。");
                return;
            }

            var serialized = new SerializedObject(data);
            List<LineSlot> lines = CollectLines(serialized);
            if (lines.Count != ExpectedLineCount)
            {
                Fail($"台詞（LineCue）が {lines.Count} 件あります。音声は {ExpectedLineCount} 件分です。何も変更していません。");
                return;
            }

            var clips = new AudioClip[lines.Count];
            var missing = new List<string>();
            for (int i = 0; i < lines.Count; i++)
            {
                string path = ClipPath(i + 1);
                clips[i] = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
                if (clips[i] == null)
                {
                    missing.Add(path);
                }
            }
            if (missing.Count > 0)
            {
                Fail($"音声が見つかりません。何も変更していません。\n{string.Join("\n", missing)}");
                return;
            }

            int changed = 0;
            var report = new StringBuilder();
            report.AppendLine($"[DNPDemo] {ScenarioDataBuilder.AssetPath} に音声を割り当てました（{lines.Count} 件）。");
            for (int i = 0; i < lines.Count; i++)
            {
                SerializedProperty voice = lines[i].Voice;
                if (voice.objectReferenceValue != clips[i])
                {
                    voice.objectReferenceValue = clips[i];
                    changed++;
                }
                report.AppendLine($"{i + 1:00}\t{lines[i].StepId}\t{lines[i].Speaker}\t{lines[i].TextHead}\t{clips[i].name}");
            }

            // ApplyModifiedProperties registers the change with Undo.
            serialized.ApplyModifiedProperties();
            AssetDatabase.SaveAssetIfDirty(data);
            report.Insert(0, $"変更 {changed} 件 / ");
            Debug.Log(report.ToString(), data);
        }

        /// <summary>The voice property of every line cue, in play order.</summary>
        static List<LineSlot> CollectLines(SerializedObject serialized)
        {
            var lines = new List<LineSlot>();
            SerializedProperty steps = serialized.FindProperty(StepsProperty);
            for (int s = 0; s < steps.arraySize; s++)
            {
                SerializedProperty step = steps.GetArrayElementAtIndex(s);
                SerializedProperty cues = step.FindPropertyRelative(CuesProperty);
                string stepId = step.FindPropertyRelative(nameof(ScenarioStep.Id)).stringValue;
                for (int c = 0; c < cues.arraySize; c++)
                {
                    SerializedProperty cue = cues.GetArrayElementAtIndex(c);
                    if (cue.managedReferenceValue is LineCue line)
                    {
                        lines.Add(new LineSlot(stepId, line, cue.FindPropertyRelative(VoiceProperty)));
                    }
                }
            }
            return lines;
        }

        static void Fail(string message)
        {
            Debug.LogError($"[DNPDemo] {message}");
            EditorUtility.DisplayDialog(DialogTitle, message, "OK");
        }

        readonly struct LineSlot
        {
            const int TextHeadLength = 12;

            public readonly string StepId;
            public readonly string Speaker;
            public readonly string TextHead;
            public readonly SerializedProperty Voice;

            public LineSlot(string stepId, LineCue line, SerializedProperty voice)
            {
                StepId = stepId;
                Speaker = line.Speaker == ScenarioSpeaker.Narrator ? "ナレーター" : line.Speaker.ToDisplayName();
                string text = (line.Text ?? string.Empty).Replace("\n", " ");
                TextHead = text.Length > TextHeadLength ? text.Substring(0, TextHeadLength) + "…" : text;
                Voice = voice;
            }
        }
    }
}
