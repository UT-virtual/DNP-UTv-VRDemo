using System;
using UnityEngine;

namespace DNPDemo.Interaction
{
    /// <summary>
    /// Base class of the pen tools (selection, stroke, manipulate, voice, history panel). <see cref="InteractionRunner"/>
    /// drives every tool through the same lifecycle on every device, the actor's and the observer's:
    /// <list type="number">
    /// <item><see cref="Begin"/> when a part the tool <see cref="CanRun"/> becomes current. On the actor's device
    /// (<see cref="InteractionSession.IsLocalActor"/>) the tool takes input; elsewhere it only shows the actor's progress.</item>
    /// <item>The actor's tool reports in-progress state with <see cref="InteractionSession.SendProgress"/>; the others
    /// receive it in <see cref="OnRemoteProgress"/>. It calls <see cref="InteractionSession.Complete"/> when the
    /// operation succeeds.</item>
    /// <item><see cref="End"/> when the part is no longer current (the next part began, or the step finished).
    /// Input stops, but what the tool shows stays, so a selection remains visible during the following voice command.</item>
    /// <item><see cref="Discard"/> when the scenario moves to another step. Everything the tool shows or holds is removed;
    /// the lasting result is shown by the step's cues instead.</item>
    /// </list>
    /// </summary>
    public abstract class InteractionTool : MonoBehaviour
    {
        /// <summary>True when this tool runs parts like <paramref name="part"/>.</summary>
        public abstract bool CanRun(InteractionPart part);

        /// <summary>The part of <paramref name="session"/> became current. Reset per-part state here.</summary>
        public abstract void Begin(InteractionSession session);

        /// <summary>The part is no longer current. Stop taking input; keep what is shown.</summary>
        public virtual void End()
        {
        }

        /// <summary>The step moved on. Remove everything this tool shows or holds.</summary>
        public abstract void Discard();

        /// <summary>The actor's tool sent in-progress state for the current part (observer devices only).</summary>
        public virtual void OnRemoteProgress(InteractionSession session, ArraySegment<byte> payload)
        {
        }
    }
}
