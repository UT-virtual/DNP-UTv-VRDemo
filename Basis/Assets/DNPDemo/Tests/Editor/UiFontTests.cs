using System.Collections.Generic;
using System.IO;
using DNPDemo.Editor;
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
    }
}
