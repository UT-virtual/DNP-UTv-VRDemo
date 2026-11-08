using System;
using System.Collections;

namespace DNPDemo.Scenario.Cues
{
    /// <summary>Shows or hides a scene object registered in <see cref="ScenarioStage"/> under <see cref="Key"/>. Persistent.</summary>
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

        public override string ToString() => $"{Key} を{(Active ? "表示" : "非表示")}";
    }
}
