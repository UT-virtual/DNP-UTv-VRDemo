using System;
using System.Collections;

namespace DNPDemo.Scenario.Cues
{
    /// <summary>Shows or hides the scene object bound to <see cref="Key"/> (<see cref="ScenarioObjectBinding"/>). Persistent.</summary>
    [Serializable]
    public sealed class SetObjectActiveCue : ScenarioCue
    {
        public string Key;
        public bool Active = true;

        public SetObjectActiveCue()
        {
        }

        public SetObjectActiveCue(string key, bool active)
        {
            Key = key;
            Active = active;
        }

        public override IEnumerator Play(ScenarioCueContext context)
        {
            ApplyInstant(context);
            yield break;
        }

        public override void ApplyInstant(ScenarioCueContext context) => context.Stage.SetObjectActive(Key, Active);

        public override void CollectRequirements(ScenarioRequirements requirements)
        {
            if (!string.IsNullOrEmpty(Key))
            {
                requirements.ObjectKeys.Add(Key);
            }
        }

        public override string ToString() => $"{Key} を{(Active ? "表示" : "非表示")}";
    }
}
