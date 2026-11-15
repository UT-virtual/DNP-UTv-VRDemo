using System;
using System.Collections.Generic;
using DNPDemo.Scenario;
using UnityEngine;

namespace DNPDemo.Interaction.Stroke
{
    /// <summary>What a stroke part asks the actor to draw.</summary>
    public enum StrokeGoal
    {
        /// <summary>One line that runs from the start zone to the end zone, in either direction.</summary>
        Guide = 0,
        /// <summary>A free drawing of any number of lines whose overall size reaches a minimum.</summary>
        Sketch = 1,
    }

    /// <summary>
    /// Drawing in the air: while the trigger is held the pen's tip leaves a line. <see cref="StrokeTool"/> judges each
    /// line when the trigger is released (see <see cref="StrokeJudge"/>):
    /// <list type="bullet">
    /// <item><see cref="StrokeGoal.Guide"/>: the line must reach both <see cref="StartZoneKey"/> and
    /// <see cref="EndZoneKey"/>. A line that does not fades out and the actor tries again.</item>
    /// <item><see cref="StrokeGoal.Sketch"/>: the remaining lines together must span <see cref="MinExtent"/>. Lines that
    /// fall short stay, so the drawing can be continued; the eraser at the pen's butt removes lines.</item>
    /// </list>
    /// </summary>
    [Serializable]
    public sealed class StrokePart : InteractionPart
    {
        public const float DefaultMinExtent = 0.15f;

        public StrokeGoal Goal;

        [Tooltip("Guide: key (ScenarioObjectBinding) of the zone the line starts from. The object carries a DetectionZoneGroup or a DetectionZone.")]
        public string StartZoneKey;

        [Tooltip("Guide: key of the zone the line must reach.")]
        public string EndZoneKey;

        [Tooltip("Sketch: the longest side of the box around all lines must be at least this long, in metres.")]
        [Min(0.01f)] public float MinExtent = DefaultMinExtent;

        public StrokePart()
        {
        }

        public static StrokePart Guide(string instruction, string startZoneKey, string endZoneKey) => new()
        {
            Instruction = instruction,
            Goal = StrokeGoal.Guide,
            StartZoneKey = startZoneKey,
            EndZoneKey = endZoneKey,
        };

        public static StrokePart Sketch(string instruction, float minExtent = DefaultMinExtent) => new()
        {
            Instruction = instruction,
            Goal = StrokeGoal.Sketch,
            MinExtent = minExtent,
        };

        public override PenMode Mode => PenMode.Draw;

        public override void CollectRequirements(ScenarioRequirements requirements)
        {
            if (Goal != StrokeGoal.Guide)
            {
                return;
            }
            if (!string.IsNullOrWhiteSpace(StartZoneKey))
            {
                requirements.ObjectKeys.Add(StartZoneKey);
            }
            if (!string.IsNullOrWhiteSpace(EndZoneKey))
            {
                requirements.ObjectKeys.Add(EndZoneKey);
            }
        }

        public override void Validate(List<string> problems)
        {
            switch (Goal)
            {
                case StrokeGoal.Guide:
                    if (string.IsNullOrWhiteSpace(StartZoneKey))
                    {
                        problems.Add("ガイドの始点ゾーンのキーが空です。");
                    }
                    if (string.IsNullOrWhiteSpace(EndZoneKey))
                    {
                        problems.Add("ガイドの終点ゾーンのキーが空です。");
                    }
                    else if (EndZoneKey == StartZoneKey)
                    {
                        problems.Add($"ガイドの始点と終点が同じゾーン「{EndZoneKey}」です。");
                    }
                    break;
                case StrokeGoal.Sketch:
                    if (!(MinExtent > 0f))
                    {
                        problems.Add("線画の最小サイズは 0 より大きくしてください。");
                    }
                    break;
                default:
                    problems.Add($"線画の種類 {(int)Goal} は定義されていません。");
                    break;
            }
        }

        public override string ToString() => Goal switch
        {
            StrokeGoal.Guide => $"ガイド（{Label(StartZoneKey)} → {Label(EndZoneKey)}）{Instruction}",
            StrokeGoal.Sketch => $"線画（{MinExtent:0.##}m 以上）{Instruction}",
            _ => $"線画（不明な種類）{Instruction}",
        };

        static string Label(string key) => string.IsNullOrWhiteSpace(key) ? "未設定" : key;
    }
}
