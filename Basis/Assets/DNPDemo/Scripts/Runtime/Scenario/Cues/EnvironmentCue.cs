using System;
using System.Collections;
using System.Collections.Generic;
using DNPDemo.Environment;
using UnityEngine;

namespace DNPDemo.Scenario.Cues
{
    /// <summary>Transitions to an environment preset. Persistent: later steps and late joins apply it instantly.</summary>
    [Serializable]
    public sealed class EnvironmentCue : ScenarioCue
    {
        public string EnvironmentKey = "Environment";
        public EnvironmentPreset Preset;
        [Min(0f)] public float Duration = 2f;

        public EnvironmentCue()
        {
        }

        public EnvironmentCue(EnvironmentPreset preset, float duration = 2f)
        {
            Preset = preset;
            Duration = duration;
        }

        public override IEnumerator Play(ScenarioCueContext context)
        {
            if (!TryGetController(context, out EnvironmentController controller))
            {
                yield break;
            }
            controller.TransitionTo(Preset, Duration);
            while (controller.IsTransitioning)
            {
                yield return null;
            }
        }

        public override void ApplyInstant(ScenarioCueContext context)
        {
            if (TryGetController(context, out EnvironmentController controller))
            {
                controller.ApplyInstant(Preset);
            }
        }

        public override void CollectRequirements(ScenarioRequirements requirements)
        {
            if (!string.IsNullOrWhiteSpace(EnvironmentKey))
            {
                requirements.ObjectKeys.Add(EnvironmentKey);
            }
        }

        public override void Validate(List<string> problems)
        {
            if (string.IsNullOrWhiteSpace(EnvironmentKey))
            {
                problems.Add("環境コントローラーのキーが空です。");
            }
            if (Preset == null)
            {
                problems.Add("環境プリセットが未設定です。");
            }
            if (Duration < 0f)
            {
                problems.Add("環境の切り替え時間を負数にはできません。");
            }
        }

        public override string ToString() => $"環境を {(Preset != null ? Preset.name : "未設定")} に切り替え";

        bool TryGetController(ScenarioCueContext context, out EnvironmentController controller)
        {
            controller = null;
            if (!context.Stage.TryGetObject(EnvironmentKey, out GameObject target))
            {
                return false;
            }
            if (target.TryGetComponent(out controller))
            {
                return true;
            }
            Debug.LogWarning($"[DNPDemo] キー「{EnvironmentKey}」のオブジェクトに EnvironmentController がありません。", target);
            return false;
        }
    }
}
