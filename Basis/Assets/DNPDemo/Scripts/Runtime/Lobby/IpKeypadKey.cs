using System;
using UnityEngine;
using UnityEngine.UI;

namespace DNPDemo.Lobby
{
    /// <summary>One key of <see cref="IpKeypad"/>. Place under the keypad; it is discovered automatically.</summary>
    [RequireComponent(typeof(Button))]
    public sealed class IpKeypadKey : MonoBehaviour
    {
        public enum KeyKind
        {
            Character,
            Backspace,
            Clear,
        }

        [SerializeField] KeyKind _kind = KeyKind.Character;

        [Tooltip("Character appended for KeyKind.Character (a digit or '.').")]
        [SerializeField] string _character = "0";

        public KeyKind Kind => _kind;
        public char Character => string.IsNullOrEmpty(_character) ? '\0' : _character[0];

        public event Action<IpKeypadKey> Pressed;

        void Awake() => GetComponent<Button>().onClick.AddListener(() => Pressed?.Invoke(this));
    }
}
