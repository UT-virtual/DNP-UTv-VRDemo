using UnityEngine;
using UnityEngine.SceneManagement;

namespace DNPDemo.Presentation
{
    /// <summary>Fades distant geometry to black while the exhibition room is in the active scene.</summary>
    [DisallowMultipleComponent]
    public sealed class ExhibitionDistanceFog : MonoBehaviour
    {
        [SerializeField, Min(0f), Tooltip("Distance from the viewer where the room starts to darken, in metres.")]
        float _startDistance = 2.5f;

        [SerializeField, Min(0f), Tooltip("Distance from the viewer where the room reaches black, in metres.")]
        float _endDistance = 7f;

        (bool Enabled, FogMode Mode, Color Color, float Start, float End)? _previousFog;

        void OnEnable()
        {
            SceneManager.activeSceneChanged += HandleActiveSceneChanged;
            ApplyToScene(SceneManager.GetActiveScene());
        }

        void OnDisable()
        {
            SceneManager.activeSceneChanged -= HandleActiveSceneChanged;
            if (_previousFog is { } previous && gameObject.scene == SceneManager.GetActiveScene())
            {
                RenderSettings.fogMode = previous.Mode;
                RenderSettings.fogColor = previous.Color;
                RenderSettings.fogStartDistance = previous.Start;
                RenderSettings.fogEndDistance = previous.End;
                RenderSettings.fog = previous.Enabled;
            }
            _previousFog = null;
        }

        void HandleActiveSceneChanged(Scene previous, Scene current)
        {
            ApplyToScene(current);
        }

        void ApplyToScene(Scene scene)
        {
            if (scene != gameObject.scene || _previousFog.HasValue)
            {
                return;
            }

            _previousFog = (RenderSettings.fog, RenderSettings.fogMode, RenderSettings.fogColor,
                RenderSettings.fogStartDistance, RenderSettings.fogEndDistance);
            float start = Mathf.Max(0f, _startDistance);
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = Color.black;
            RenderSettings.fogStartDistance = start;
            RenderSettings.fogEndDistance = Mathf.Max(start + 0.01f, _endDistance);
            RenderSettings.fog = true;
        }
    }
}
