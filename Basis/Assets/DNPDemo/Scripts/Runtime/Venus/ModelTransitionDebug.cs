using UnityEngine;

namespace DNPDemo.Venus
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(VenusStatue))]
    public sealed class ModelTransitionDebug : MonoBehaviour
    {
        [SerializeField] VenusVersion _targetVersion;
        [SerializeField] VenusStatue _statue;
        [SerializeField] ModelTransition _transition;

        void Reset()
        {
            _statue = GetComponent<VenusStatue>();
            _transition = GetComponent<ModelTransition>();
        }

        [ContextMenu("Normal Transition")]
        void PlayNormalTransition()
        {
            if (_statue == null)
            {
                _statue = GetComponent<VenusStatue>();
            }
            if (_statue != null)
            {
                _statue.Show(_targetVersion, true);
            }
        }

        [ContextMenu("Apply Instant")]
        void ApplyInstant()
        {
            if (_statue == null)
            {
                _statue = GetComponent<VenusStatue>();
            }
            if (_statue != null)
            {
                _statue.Show(_targetVersion, false);
            }
        }

        [ContextMenu("Complete Immediately")]
        void CompleteImmediately()
        {
            if (_transition == null)
            {
                _transition = GetComponent<ModelTransition>();
            }
            if (_transition != null)
            {
                _transition.CompleteImmediately();
            }
        }
    }
}
