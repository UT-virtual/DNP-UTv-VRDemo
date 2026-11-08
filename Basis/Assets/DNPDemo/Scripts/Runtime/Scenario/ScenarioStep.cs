using System;
using System.Collections.Generic;
using DNPDemo.Scenario.Cues;
using UnityEngine;

namespace DNPDemo.Scenario
{
    /// <summary>
    /// One step of the scenario. Entering a step plays its cues in order; <see cref="Completion"/> decides
    /// what moves it on. The lasting effects of every earlier step are re-applied on entry, so a step looks
    /// the same whether it was reached by playing through, by an operator jump, or by joining late.
    /// </summary>
    [Serializable]
    public sealed class ScenarioStep
    {
        [Tooltip("Stable identifier. Logs and future references use it, so keep it unchanged when steps are reordered.")]
        public string Id;

        [Tooltip("Name shown on the operator panel.")]
        public string Title;

        [Tooltip("Role whose operation this step waits for. None for steps that progress on their own.")]
        public DemoRole Actor;

        public StepCompletion Completion;

        [Tooltip("Instruction shown to the actor while the step waits for their operation.")]
        [TextArea(2, 5)]
        public string TaskText;

        [SerializeReference, SubclassSelector]
        public List<ScenarioCue> Cues = new();

        /// <summary>True when a completion report from a device playing <paramref name="role"/> may finish this step.</summary>
        public bool AcceptsInteractionFrom(DemoRole role) =>
            Completion == StepCompletion.Interaction && role != DemoRole.None && role == Actor;
    }
}
