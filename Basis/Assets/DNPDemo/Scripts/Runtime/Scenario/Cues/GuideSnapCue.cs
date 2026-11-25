using System;
using System.Collections;
using System.Collections.Generic;
using DNPDemo.Venus;
using UnityEngine;

namespace DNPDemo.Scenario.Cues
{
    /// <summary>
    /// Plays the <see cref="GuideSnapEffect"/> on the object bound to <see cref="Key"/> and waits until its line has
    /// reached the end of the guide, so that a following <see cref="StatueVersionCue"/> changes the statue as the line
    /// arrives. Transient: a device that jumps or joins later only sees the version.
    /// </summary>
    [Serializable]
    public sealed class GuideSnapCue : ScenarioCue
    {
        public const float DefaultTravelSeconds = 0.8f;

        public string Key;

        [Min(0.01f)] public float TravelSeconds = DefaultTravelSeconds;

        public GuideSnapCue()
        {
        }

        public GuideSnapCue(string key, float travelSeconds = DefaultTravelSeconds)
        {
            Key = key;
            TravelSeconds = travelSeconds;
        }

        public override IEnumerator Play(ScenarioCueContext context)
        {
            if (!context.Stage.TryGetObject(Key, out GameObject target))
            {
                yield break;
            }
            if (!target.TryGetComponent(out GuideSnapEffect effect))
            {
                Debug.LogWarning($"[DNPDemo] キー「{Key}」のオブジェクトに GuideSnapEffect がありません。", target);
                yield break;
            }
            effect.Play(TravelSeconds);
            float timeoutSeconds = TravelSeconds + 1f;
            float elapsedSeconds = 0f;
            while (effect.IsPlaying && elapsedSeconds < timeoutSeconds)
            {
                yield return null;
                elapsedSeconds += Time.deltaTime;
            }
        }

        public override void CollectRequirements(ScenarioRequirements requirements)
        {
            if (!string.IsNullOrWhiteSpace(Key))
            {
                requirements.ObjectKeys.Add(Key);
            }
        }

        public override void Validate(List<string> problems)
        {
            if (string.IsNullOrWhiteSpace(Key))
            {
                problems.Add("ガイドのキーが空です。");
            }
            if (!(TravelSeconds > 0f))
            {
                problems.Add("ガイドの演出の時間は 0 より大きくしてください。");
            }
        }

        public override string ToString() => $"{Key} に沿って腕をスナップ（{TravelSeconds:0.##} 秒）";
    }
}
