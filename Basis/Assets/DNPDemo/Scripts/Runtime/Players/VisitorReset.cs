using DNPDemo.Pen;
using DNPDemo.Scenario;
using UnityEngine;

namespace DNPDemo.Players
{
    /// <summary>
    /// Gets this device ready for the next visitor whenever the run enters the lobby step (the run starting, or the
    /// operator sending it back after the closing): the local player goes back to the spawn point of their role, and the
    /// pen of their role is let go, put back at its starting position, and returned to the default mode.
    /// <para>
    /// The world itself needs nothing here: it follows the step index (<see cref="ScenarioPlayer"/>). This covers what the
    /// visitors carry around and the step index does not decide.
    /// </para>
    /// </summary>
    public sealed class VisitorReset : MonoBehaviour
    {
        [SerializeField] ScenarioDirector _director;
        [SerializeField] DemoSpawnService _spawn;
        [SerializeField] SpatialPenController _pen;

        bool _hasReset;
        ushort _resetSequence;

        void OnEnable()
        {
            if (_director == null || _spawn == null || _pen == null)
            {
                Debug.LogError("[DNPDemo] VisitorReset needs ScenarioDirector, DemoSpawnService and SpatialPenController references.", this);
                enabled = false;
                return;
            }
            _director.StepChanged += HandleStepChanged;
        }

        void OnDisable()
        {
            if (_director != null)
            {
                _director.StepChanged -= HandleStepChanged;
            }
        }

        void HandleStepChanged()
        {
            if (!_director.IsAtLobby || (_hasReset && _resetSequence == _director.Sequence))
            {
                return;
            }
            _hasReset = true;
            _resetSequence = _director.Sequence;
            _spawn.ReturnToRoleSpawn();
            _pen.ResetForNextVisitor();
        }
    }
}
