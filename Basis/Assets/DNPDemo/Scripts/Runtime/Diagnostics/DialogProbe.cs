using System;
using System.Collections.Generic;
using Basis.BasisUI;
using Basis.Scripts.Device_Management;
using DNPDemo.Lobby;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DNPDemo.Diagnostics
{
    /// <summary>
    /// Logs every dialog and permission prompt a visitor could see between launch and the lobby, so the ones to
    /// suppress can be found on a freshly installed Quest. Runs only in development builds and the editor.
    /// Every line starts with <see cref="Tag"/> and the seconds since launch:
    /// <c>adb logcat -s Unity | grep DialogProbe</c>.
    /// <list type="bullet">
    /// <item>Basis panels (<see cref="BasisMenuPanel"/>, including dialogues and prompts) when they open and close.
    /// Basis has no creation hook for panels, so active panels are polled.</item>
    /// <item>The Basis main menu opening and closing; prompts open it before showing themselves.</item>
    /// <item>Popups diverted into the Basis notification list instead of being shown.</item>
    /// <item>Focus and pause changes. Android permission dialogs and Quest system screens take focus away from the
    /// app, so they show up here even though Unity cannot see them.</item>
    /// <item>Every permission the APK requests and whether it is granted, at launch and after each focus return.</item>
    /// <item>Milestones: scene loads, Basis initialization, and the lobby becoming visible.</item>
    /// </list>
    /// </summary>
    public sealed class DialogProbe : MonoBehaviour
    {
        const string Tag = "[DialogProbe]";
        const float PollInterval = 0.25f;
        const int MaxTextLength = 120;

        // Each open panel, keyed by its entity ID, with its title and the time it was first seen.
        readonly Dictionary<EntityId, OpenPanel> _openPanels = new();
        readonly HashSet<EntityId> _stillOpen = new();
        readonly List<EntityId> _closed = new();
        readonly Dictionary<Guid, BasisNotificationStatus> _notificationStatus = new();
        Dictionary<string, bool> _permissions;

        float _nextPoll;
        bool _mainMenuOpen;
        bool _lobbySeen;

        readonly struct OpenPanel
        {
            public readonly string Title;
            public readonly float OpenedAt;

            public OpenPanel(string title, float openedAt)
            {
                Title = title;
                OpenedAt = openedAt;
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Install()
        {
            if (!Debug.isDebugBuild)
            {
                return;
            }
            var go = new GameObject(nameof(DialogProbe));
            DontDestroyOnLoad(go);
            go.AddComponent<DialogProbe>();
        }

        void Awake()
        {
            Log($"started (Unity {Application.unityVersion}, {SystemInfo.deviceModel}, {Application.version})");
            SceneManager.sceneLoaded += OnSceneLoaded;
            BasisDeviceManagement.OnInitializationCompleted += OnBasisInitialized;
            BasisNotificationCenter.Changed += OnNotificationsChanged;
            LogPermissions();
        }

        void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            BasisDeviceManagement.OnInitializationCompleted -= OnBasisInitialized;
            BasisNotificationCenter.Changed -= OnNotificationsChanged;
        }

        void Update()
        {
            if (Time.unscaledTime < _nextPoll)
            {
                return;
            }
            _nextPoll = Time.unscaledTime + PollInterval;

            PollMainMenu();
            PollPanels();
            PollLobby();
        }

        void OnApplicationFocus(bool hasFocus)
        {
            Log(hasFocus ? "focus regained" : "focus lost (a system dialog or screen may be in front)");
            if (hasFocus)
            {
                LogPermissions();
            }
        }

        void OnApplicationPause(bool paused) => Log(paused ? "paused" : "resumed");

        static void OnSceneLoaded(Scene scene, LoadSceneMode mode) => Log($"scene loaded: {scene.name} ({mode})");

        static void OnBasisInitialized() => Log("Basis initialization completed");

        void PollMainMenu()
        {
            bool open = BasisMainMenu.Instance != null;
            if (open != _mainMenuOpen)
            {
                _mainMenuOpen = open;
                Log(open ? "main menu opened" : "main menu closed");
            }
        }

        void PollPanels()
        {
            _stillOpen.Clear();
            foreach (BasisMenuPanel panel in FindObjectsByType<BasisMenuPanel>())
            {
                EntityId id = panel.GetEntityId();
                _stillOpen.Add(id);
                if (_openPanels.ContainsKey(id))
                {
                    continue;
                }
                string title = TitleOf(panel);
                _openPanels.Add(id, new OpenPanel(title, Time.realtimeSinceStartup));
                Log($"panel opened: {panel.GetType().Name} \"{title}\" layer={panel.Layer} "
                    + $"text=\"{Shorten(DescriptionOf(panel))}\" path={PathOf(panel.transform)}");
            }

            _closed.Clear();
            foreach (KeyValuePair<EntityId, OpenPanel> entry in _openPanels)
            {
                if (!_stillOpen.Contains(entry.Key))
                {
                    _closed.Add(entry.Key);
                }
            }
            foreach (EntityId id in _closed)
            {
                OpenPanel panel = _openPanels[id];
                _openPanels.Remove(id);
                Log($"panel closed: \"{panel.Title}\" after {Time.realtimeSinceStartup - panel.OpenedAt:F1}s");
            }
        }

        void PollLobby()
        {
            if (_lobbySeen)
            {
                return;
            }
            LobbyPanel lobby = FindAnyObjectByType<LobbyPanel>();
            if (lobby != null && lobby.isActiveAndEnabled)
            {
                _lobbySeen = true;
                Log("lobby visible");
            }
        }

        void OnNotificationsChanged()
        {
            foreach (BasisNotification notification in BasisNotificationCenter.All)
            {
                bool known = _notificationStatus.TryGetValue(notification.Id, out BasisNotificationStatus previous);
                if (known && previous == notification.Status)
                {
                    continue;
                }
                _notificationStatus[notification.Id] = notification.Status;
                string change = known ? $"{previous} -> {notification.Status}" : notification.Status.ToString();
                Log($"notification {change}: \"{notification.Title}\" text=\"{Shorten(notification.Description)}\"");
            }
        }

        /// <summary>
        /// Logs every permission the APK requests on the first call, and only the ones whose state changed after that.
        /// </summary>
        void LogPermissions()
        {
            Dictionary<string, bool> current = ReadPermissions();
            if (current == null)
            {
                return;
            }
            foreach (KeyValuePair<string, bool> permission in current)
            {
                bool changed = _permissions == null
                    || !_permissions.TryGetValue(permission.Key, out bool before)
                    || before != permission.Value;
                if (changed)
                {
                    Log($"permission {(permission.Value ? "granted" : "denied")}: {permission.Key}");
                }
            }
            _permissions = current;
        }

        /// <summary>
        /// Returns each permission declared in the installed APK's manifest (including ones Unity and plugins add at
        /// build time) and whether it is currently granted. Null outside Android or when the query fails.
        /// </summary>
        static Dictionary<string, bool> ReadPermissions()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            const int GetPermissions = 0x00001000; // PackageManager.GET_PERMISSIONS
            const int PermissionGranted = 0; // PackageManager.PERMISSION_GRANTED
            try
            {
                using var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
                using AndroidJavaObject activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
                using AndroidJavaObject packageManager = activity.Call<AndroidJavaObject>("getPackageManager");
                string packageName = activity.Call<string>("getPackageName");
                using AndroidJavaObject info =
                    packageManager.Call<AndroidJavaObject>("getPackageInfo", packageName, GetPermissions);
                string[] requested = info.Get<string[]>("requestedPermissions") ?? Array.Empty<string>();

                var result = new Dictionary<string, bool>();
                foreach (string permission in requested)
                {
                    result[permission] = activity.Call<int>("checkSelfPermission", permission) == PermissionGranted;
                }
                return result;
            }
            catch (Exception e)
            {
                Log($"could not read permissions: {e.Message}");
                return null;
            }
#else
            return null;
#endif
        }

        static string TitleOf(BasisMenuPanel panel)
        {
            if (panel is BasisMenuDialoguePanel dialogue && !string.IsNullOrEmpty(dialogue.Title))
            {
                return dialogue.Title;
            }
            return string.IsNullOrEmpty(panel.Data.Title) ? panel.gameObject.name : panel.Data.Title;
        }

        static string DescriptionOf(BasisMenuPanel panel)
        {
            if (panel is BasisMenuDialoguePanel dialogue && !string.IsNullOrEmpty(dialogue.Description))
            {
                return dialogue.Description;
            }
            return panel.Descriptor != null ? panel.Descriptor.Description : null;
        }

        static string PathOf(Transform transform)
        {
            string path = transform.name;
            for (Transform parent = transform.parent; parent != null; parent = parent.parent)
            {
                path = parent.name + "/" + path;
            }
            return path;
        }

        static string Shorten(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return string.Empty;
            }
            text = text.Replace('\n', ' ').Replace('\r', ' ');
            return text.Length <= MaxTextLength ? text : text.Substring(0, MaxTextLength) + "...";
        }

        static void Log(string message) => Debug.Log($"{Tag} t={Time.realtimeSinceStartup:F2}s {message}");
    }
}
