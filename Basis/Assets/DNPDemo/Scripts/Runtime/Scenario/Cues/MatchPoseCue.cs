using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DNPDemo.Scenario.Cues
{
    /// <summary>
    /// Puts the object bound to <see cref="Key"/> at the world position and rotation of the marker bound to
    /// <see cref="MarkerKey"/>. Persistent: it shows the result of a step in which the pen moved the object, since the
    /// stage returns moved objects to where they started whenever the step changes.
    /// </summary>
    [Serializable]
    public sealed class MatchPoseCue : ScenarioCue
    {
        public string Key;
        public string MarkerKey;

        public MatchPoseCue()
        {
        }

        public MatchPoseCue(string key, string markerKey)
        {
            Key = key;
            MarkerKey = markerKey;
        }

        public override IEnumerator Play(ScenarioCueContext context)
        {
            ApplyInstant(context);
            yield break;
        }

        public override void ApplyInstant(ScenarioCueContext context)
        {
            if (context.Stage.TryGetObject(Key, out GameObject target) && context.Stage.TryGetObject(MarkerKey, out GameObject marker))
            {
                marker.transform.GetPositionAndRotation(out Vector3 position, out Quaternion rotation);
                target.transform.SetPositionAndRotation(position, rotation);
            }
        }

        public override void CollectRequirements(ScenarioRequirements requirements)
        {
            if (!string.IsNullOrWhiteSpace(Key))
            {
                requirements.ObjectKeys.Add(Key);
            }
            if (!string.IsNullOrWhiteSpace(MarkerKey))
            {
                requirements.ObjectKeys.Add(MarkerKey);
            }
        }

        public override void Validate(List<string> problems)
        {
            if (string.IsNullOrWhiteSpace(Key))
            {
                problems.Add("動かすオブジェクトのキーが空です。");
            }
            if (string.IsNullOrWhiteSpace(MarkerKey))
            {
                problems.Add("合わせる先のマーカーのキーが空です。");
            }
        }

        public override string ToString() => $"{Key} を {MarkerKey} の位置に置く";
    }
}
