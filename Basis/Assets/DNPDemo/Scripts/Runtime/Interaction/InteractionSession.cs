using Basis.Network.Core;

namespace DNPDemo.Interaction
{
    /// <summary>
    /// A tool's handle on one part of one step. Once the part stops being current (completed, or the step moved on)
    /// the session is stale: <see cref="IsCurrent"/> turns false and <see cref="Complete"/> / <see cref="SendProgress"/>
    /// do nothing, so a tool that reacts late cannot affect a later part.
    /// </summary>
    public sealed class InteractionSession
    {
        readonly InteractionRunner _runner;

        internal InteractionSession(InteractionRunner runner, Scenario.ScenarioStep step, InteractionPart part, int partIndex, int partCount,
            DemoRole actor, bool isLocalActor)
        {
            _runner = runner;
            Step = step;
            Part = part;
            PartIndex = partIndex;
            PartCount = partCount;
            Actor = actor;
            IsLocalActor = isLocalActor;
        }

        /// <summary>The step <see cref="Part"/> belongs to.</summary>
        public Scenario.ScenarioStep Step { get; }

        public InteractionPart Part { get; }

        /// <summary>Position of <see cref="Part"/> in the step (0-based), for "step n of m" displays.</summary>
        public int PartIndex { get; }

        public int PartCount { get; }

        /// <summary>Role that performs the step.</summary>
        public DemoRole Actor { get; }

        /// <summary>True on the actor's device: the tool takes input and decides completion.</summary>
        public bool IsLocalActor { get; }

        public bool IsCurrent => _runner.IsCurrent(this);

        /// <summary>Actor only: the operation of this part succeeded. Moves on to the next part, or finishes the step.</summary>
        public void Complete() => _runner.Complete(this);

        /// <summary>
        /// Actor only: sends in-progress state to the other devices, which receive it in
        /// <see cref="InteractionTool.OnRemoteProgress"/>. Use Unreliable for frequent updates and a reliable method
        /// for states that must arrive (a confirmed selection, a finished stroke).
        /// </summary>
        public void SendProgress(byte[] payload, DeliveryMethod deliveryMethod) => _runner.SendProgress(this, payload, deliveryMethod);
    }
}
