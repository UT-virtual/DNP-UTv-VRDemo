using System.Collections.Generic;
using System.IO;
using DNPDemo.Editor;
using DNPDemo.Interaction;
using DNPDemo.Interaction.History;
using DNPDemo.Interaction.Voice;
using DNPDemo.Menu;
using DNPDemo.Operator;
using DNPDemo.Scenario;
using DNPDemo.TaskHud;
using NUnit.Framework;
using TMPro;
using UnityEditor;

namespace DNPDemo.Tests
{
    /// <summary>
    /// The UI font is a static SDF generated from a character set file. Characters added to the file appear only after
    /// the SDF is regenerated (select the font asset, Update Atlas Texture, Generate Font Atlas, Save).
    /// </summary>
    public class UiFontTests
    {
        const string FontFolder = DNPDemoProjectSetup.Root + "/UI/Fonts";
        const string FontAssetPath = FontFolder + "/NotoSansJP-Regular SDF.asset";
        const string CharacterSetPath = FontFolder + "/JapaneseCharacterSet-3353.txt";

        [Test]
        public void SdfHasEveryCharacterOfTheCharacterSet()
        {
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontAssetPath);
            Assert.IsNotNull(font, FontAssetPath);

            string characters = File.ReadAllText(CharacterSetPath).Replace("\r", string.Empty).Replace("\n", string.Empty);
            font.HasCharacters(characters, out List<char> missing);

            Assert.IsEmpty(missing,
                $"{FontAssetPath} に文字セットの文字がありません（{new string(missing.ToArray())}）。SDF を作り直してください。");
        }

        [Test]
        public void TaskHudMarksAreInTheCharacterSet()
        {
            string characters = File.ReadAllText(CharacterSetPath);
            foreach (string mark in new[] { TaskHudView.DoneMark, TaskHudView.CurrentMark, TaskHudView.PendingMark })
            {
                StringAssert.Contains(mark, characters);
            }
        }

        [Test]
        public void HistoryPanelTextsAreInTheCharacterSet()
        {
            AssertInCharacterSet(new[]
            {
                HistoryMenuProvider.MenuTitle,
                HistoryPanel.RestoreLabel,
                HistoryPanel.EmptyMessage,
                HistoryTool.FormatRestored(null),
                ScenarioDataBuilder.HistoryInstruction("Ver 1.0"),
                "0123456789 / Ver.",
            });
        }

        [Test]
        public void VoiceCommandTextsAreInTheCharacterSet()
        {
            var texts = new List<string>
            {
                VoiceCommandTool.ListeningStatus,
                VoiceCommandTool.RetryStatus,
                VoiceCommandTool.FormatResult(string.Empty),
                MicrophoneThresholdControl.FormatLevelLine(0f),
                MicrophoneThresholdControl.FormatLevelLine(0.01f),
                MicrophoneThresholdControl.FormatDeviceLine(string.Empty),
                "マイクを切り替える",
            };
            foreach (ScenarioStep step in ScenarioDataBuilder.CreateDefaultSteps())
            {
                foreach (InteractionPart part in step.Interaction ?? new List<InteractionPart>())
                {
                    if (part is VoicePart voice)
                    {
                        texts.Add(voice.Transcript);
                    }
                }
            }

            AssertInCharacterSet(texts);
        }

        [Test]
        public void HintTextsAreInTheCharacterSet()
        {
            var texts = new List<string> { TaskHudContent.HintPrefix, ScenarioDataBuilder.HistoryHint("Ver 1.0") };
            AddHintTexts(texts, ScenarioDataBuilder.CreateDefaultSteps());
            var data = AssetDatabase.LoadAssetAtPath<ScenarioData>(ScenarioDataBuilder.AssetPath);
            Assert.IsNotNull(data, ScenarioDataBuilder.AssetPath);
            AddHintTexts(texts, data.Steps);

            AssertInCharacterSet(texts);
        }

        static void AddHintTexts(List<string> texts, IReadOnlyList<ScenarioStep> steps)
        {
            foreach (ScenarioStep step in steps)
            {
                foreach (InteractionPart part in step?.Interaction ?? new List<InteractionPart>())
                {
                    if (!string.IsNullOrEmpty(part?.Hint?.Text))
                    {
                        texts.Add(part.Hint.Text);
                    }
                }
            }
        }

        static void AssertInCharacterSet(IEnumerable<string> texts)
        {
            string characters = File.ReadAllText(CharacterSetPath);
            var missing = new HashSet<char>();
            foreach (string text in texts)
            {
                foreach (char c in text)
                {
                    if (!char.IsWhiteSpace(c) && characters.IndexOf(c) < 0)
                    {
                        missing.Add(c);
                    }
                }
            }
            Assert.IsEmpty(missing, $"文字セットにない文字があります（{string.Concat(missing)}）。文字セットに加えて SDF を作り直してください。");
        }
    }
}
