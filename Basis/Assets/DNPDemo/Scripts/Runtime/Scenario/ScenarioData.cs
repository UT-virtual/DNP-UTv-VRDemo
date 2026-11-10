using System.Collections.Generic;
using UnityEngine;

namespace DNPDemo.Scenario
{
    /// <summary>
    /// The whole scenario as data. Revisions of the script are made by editing this asset;
    /// the initial asset is generated from the menu "DNP Demo/Create/Scenario Data".
    /// </summary>
    [CreateAssetMenu(menuName = "DNP Demo/Scenario Data", fileName = "ScenarioData")]
    public sealed class ScenarioData : ScriptableObject
    {
        /// <summary>Step indices travel as 16-bit values on the network.</summary>
        public const int MaxStepCount = ushort.MaxValue;

        [SerializeField] LineTiming _lineTiming = new();
        [SerializeField] List<ScenarioStep> _steps = new();

        public LineTiming LineTiming => _lineTiming;
        public IReadOnlyList<ScenarioStep> Steps => _steps;

        internal void SetSteps(IEnumerable<ScenarioStep> steps)
        {
            _steps = new List<ScenarioStep>(steps);
        }

        /// <summary>The speakers and object keys the cues of every step address.</summary>
        public ScenarioRequirements CollectRequirements()
        {
            var requirements = new ScenarioRequirements();
            foreach (ScenarioStep step in _steps)
            {
                if (step?.Cues == null)
                {
                    continue;
                }
                foreach (Cues.ScenarioCue cue in step.Cues)
                {
                    cue?.CollectRequirements(requirements);
                }
            }
            return requirements;
        }

        /// <summary>Problems that would break a run. Empty when the data is usable.</summary>
        public List<string> Validate() => Validate(_steps);

        public static List<string> Validate(IReadOnlyList<ScenarioStep> steps)
        {
            var problems = new List<string>();
            if (steps == null || steps.Count == 0)
            {
                problems.Add("ステップがありません。");
                return problems;
            }
            if (steps.Count > MaxStepCount)
            {
                problems.Add($"ステップ数が上限（{MaxStepCount}）を超えています。");
            }
            var ids = new HashSet<string>();
            for (int i = 0; i < steps.Count; i++)
            {
                ScenarioStep step = steps[i];
                string label = $"ステップ {i}";
                if (step == null)
                {
                    problems.Add($"{label} が空です。");
                    continue;
                }
                if (string.IsNullOrWhiteSpace(step.Id))
                {
                    problems.Add($"{label} の Id が空です。");
                }
                else if (!ids.Add(step.Id))
                {
                    problems.Add($"{label} の Id「{step.Id}」が重複しています。");
                }
                if (step.Completion == StepCompletion.Interaction && step.Actor == DemoRole.None)
                {
                    problems.Add($"{label}（{step.Id}）は操作待ちですが、担当ロールがありません。");
                }
                if (step.Cues != null)
                {
                    for (int c = 0; c < step.Cues.Count; c++)
                    {
                        if (step.Cues[c] == null)
                        {
                            problems.Add($"{label}（{step.Id}）のキュー {c} の種類が未設定です。");
                        }
                    }
                }
            }
            return problems;
        }
    }
}
