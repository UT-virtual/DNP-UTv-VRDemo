using System;
using System.Collections;
using System.Collections.Generic;
using DNPDemo.Scenario.Cues;
using UnityEngine;

namespace DNPDemo.Scenario
{
    /// <summary>
    /// Plays the current step on this device. On every step change it cuts the running playback, resets the
    /// stage to its baseline, applies the lasting effects of all earlier steps instantly, then plays the current
    /// step's cues from the start. Every device therefore shows the same world for the same step index,
    /// whether it played through, jumped, or joined late.
    /// </summary>
    public sealed class ScenarioPlayer : MonoBehaviour
    {
        [SerializeField] ScenarioDirector _director;
        [SerializeField] ScenarioStage _stage;

        Coroutine _playback;
        ScenarioCueContext _context;

        void OnEnable()
        {
            if (_director == null || _stage == null)
            {
                Debug.LogError("[DNPDemo] ScenarioPlayer needs ScenarioDirector and ScenarioStage references.", this);
                enabled = false;
                return;
            }
            _director.StepChanged += Rebuild;
            Rebuild();
        }

        void OnDisable()
        {
            if (_director != null)
            {
                _director.StepChanged -= Rebuild;
            }
            StopPlayback();
        }

        void Rebuild()
        {
            StopPlayback();
            _stage.ResetToBaseline();
            if (!_director.IsRunning || _director.Scenario == null)
            {
                return;
            }
            _context ??= new ScenarioCueContext(_stage, _director.Scenario);
            IReadOnlyList<ScenarioStep> steps = _director.Scenario.Steps;
            int stepIndex = _director.StepIndex;
            for (int i = 0; i < stepIndex; i++)
            {
                ApplyInstant(steps[i]);
            }
            _playback = StartCoroutine(PlayStep(steps[stepIndex], _director.Sequence, stepIndex));
        }

        void ApplyInstant(ScenarioStep step)
        {
            if (step?.Cues == null)
            {
                return;
            }
            foreach (ScenarioCue cue in step.Cues)
            {
                if (cue == null)
                {
                    continue;
                }
                try
                {
                    cue.ApplyInstant(_context);
                }
                catch (Exception exception)
                {
                    Debug.LogException(exception, this);
                }
            }
        }

        IEnumerator PlayStep(ScenarioStep step, ushort sequence, int stepIndex)
        {
            if (step.Cues != null)
            {
                foreach (ScenarioCue cue in step.Cues)
                {
                    if (cue == null)
                    {
                        continue;
                    }
                    IEnumerator routine;
                    try
                    {
                        routine = cue.Play(_context);
                    }
                    catch (Exception exception)
                    {
                        Debug.LogException(exception, this);
                        continue;
                    }
                    // Driven here rather than yielded as a nested coroutine, so that stopping this coroutine
                    // stops the cue too and a failing cue is skipped instead of stalling the step.
                    var stack = new Stack<IEnumerator>();
                    stack.Push(routine);
                    while (stack.Count > 0)
                    {
                        bool hasNext;
                        try
                        {
                            hasNext = stack.Peek().MoveNext();
                        }
                        catch (Exception exception)
                        {
                            Debug.LogException(exception, this);
                            break;
                        }
                        if (!hasNext)
                        {
                            stack.Pop();
                            continue;
                        }
                        object current = stack.Peek().Current;
                        if (current is IEnumerator nested)
                        {
                            stack.Push(nested);
                            continue;
                        }
                        yield return current;
                    }
                }
            }
            _playback = null;
            _director.NotifyCuesFinished(sequence, stepIndex);
        }

        void StopPlayback()
        {
            if (_playback != null)
            {
                StopCoroutine(_playback);
                _playback = null;
            }
            if (_stage != null)
            {
                _stage.StopTransient();
            }
        }
    }
}
