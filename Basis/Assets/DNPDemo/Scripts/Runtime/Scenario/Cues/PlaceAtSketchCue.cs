using System;
using System.Collections;
using System.Collections.Generic;
using DNPDemo.Interaction.Stroke;
using DNPDemo.Presentation;
using UnityEngine;

namespace DNPDemo.Scenario.Cues
{
    /// <summary>
    /// Shows the object bound to <see cref="Key"/> where the sketch of the step <see cref="SketchStepId"/> was drawn: at
    /// the centre of the box around its lines (<see cref="StrokeTool.LastResult"/>, read through the tool's binding
    /// <see cref="StrokesKey"/>), turned like the marker <see cref="MarkerKey"/>. The position is kept within
    /// <see cref="MaxOffset"/> of the marker, so a sketch drawn far away still leaves the object near the statue. Without
    /// that sketch (the operator jumped past it, or this device joined after it was drawn) the object appears at the
    /// marker. Persistent; when it plays, the object grows in with its <see cref="ScaleInEffect"/>, if it has one.
    /// <para>
    /// Every device takes the position from its own copy of the lines. Each device that saw the sketch holds the same
    /// lines, so they agree; a device that joined after the sketch puts the object at the marker instead.
    /// </para>
    /// </summary>
    [Serializable]
    public sealed class PlaceAtSketchCue : ScenarioCue
    {
        public const float DefaultMaxOffset = 0.8f;
        public const float DefaultAppearSeconds = 0.5f;

        [Tooltip("Key of the object to show.")]
        public string Key;

        [Tooltip("Key of the marker: the position without a sketch, the centre of the allowed area, and the rotation.")]
        public string MarkerKey;

        [Tooltip("Id of the step whose sketch places the object. Lines from any other step are ignored.")]
        public string SketchStepId;

        [Tooltip("Key of the StrokeTool that keeps the last sketch.")]
        public string StrokesKey = StrokeTool.BindingKey;

        [Tooltip("Farthest the object may appear from the marker, in metres.")]
        [Min(0f)] public float MaxOffset = DefaultMaxOffset;

        [Tooltip("Seconds the object takes to grow in when the cue plays. 0 shows it at once.")]
        [Min(0f)] public float AppearSeconds = DefaultAppearSeconds;

        public PlaceAtSketchCue()
        {
        }

        public PlaceAtSketchCue(string key, string markerKey, string sketchStepId, float maxOffset = DefaultMaxOffset)
        {
            Key = key;
            MarkerKey = markerKey;
            SketchStepId = sketchStepId;
            MaxOffset = maxOffset;
        }

        /// <summary>
        /// Where the object goes: the centre of <paramref name="sketch"/>'s lines, moved towards <paramref name="marker"/>
        /// until it is within <paramref name="maxOffset"/> of it, or the marker itself when the sketch is missing, empty,
        /// or was drawn in another step than <paramref name="sketchStepId"/>.
        /// </summary>
        public static Vector3 ResolvePosition(StrokeResult sketch, string sketchStepId, Vector3 marker, float maxOffset)
        {
            if (sketch == null || sketch.Step == null || sketch.Step.Id != sketchStepId || !sketch.TryGetBounds(out Bounds bounds))
            {
                return marker;
            }
            return marker + Vector3.ClampMagnitude(bounds.center - marker, Mathf.Max(0f, maxOffset));
        }

        public override IEnumerator Play(ScenarioCueContext context)
        {
            if (Place(context, out GameObject target) && target.TryGetComponent(out ScaleInEffect effect))
            {
                effect.Play(AppearSeconds);
            }
            yield break;
        }

        public override void ApplyInstant(ScenarioCueContext context) => Place(context, out _);

        bool Place(ScenarioCueContext context, out GameObject target)
        {
            if (!context.Stage.TryGetObject(Key, out target) || !context.Stage.TryGetObject(MarkerKey, out GameObject marker))
            {
                return false;
            }
            marker.transform.GetPositionAndRotation(out Vector3 markerPosition, out Quaternion rotation);
            Vector3 position = ResolvePosition(FindSketch(context), SketchStepId, markerPosition, MaxOffset);
            target.transform.SetPositionAndRotation(position, rotation);
            target.SetActive(true);
            return true;
        }

        StrokeResult FindSketch(ScenarioCueContext context)
        {
            if (!context.Stage.TryGetObject(StrokesKey, out GameObject tool))
            {
                return null;
            }
            if (!tool.TryGetComponent(out StrokeTool strokes))
            {
                Debug.LogWarning($"[DNPDemo] キー「{StrokesKey}」のオブジェクトに StrokeTool がありません。", tool);
                return null;
            }
            return strokes.LastResult;
        }

        public override void CollectRequirements(ScenarioRequirements requirements)
        {
            foreach (string key in new[] { Key, MarkerKey, StrokesKey })
            {
                if (!string.IsNullOrWhiteSpace(key))
                {
                    requirements.ObjectKeys.Add(key);
                }
            }
        }

        public override void Validate(List<string> problems)
        {
            if (string.IsNullOrWhiteSpace(Key))
            {
                problems.Add("出すオブジェクトのキーが空です。");
            }
            if (string.IsNullOrWhiteSpace(MarkerKey))
            {
                problems.Add("置き場所のマーカーのキーが空です。");
            }
            if (string.IsNullOrWhiteSpace(SketchStepId))
            {
                problems.Add("線画を描くステップの Id が空です。");
            }
            if (string.IsNullOrWhiteSpace(StrokesKey))
            {
                problems.Add("線画を保持する StrokeTool のキーが空です。");
            }
        }

        public override string ToString() => $"{Key} を {SketchStepId} の線画の位置に出す（{MarkerKey} から {MaxOffset:0.##}m 以内）";
    }
}
