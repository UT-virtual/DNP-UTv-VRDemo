using System;
using System.Net;
using System.Net.Sockets;
using TMPro;
using UnityEngine;

namespace DNPDemo.Lobby
{
    /// <summary>
    /// Numeric keypad for typing the host's IPv4 address in VR, where a system keyboard is awkward.
    /// Keys are the <see cref="IpKeypadKey"/> components found under this object.
    /// </summary>
    public sealed class IpKeypad : MonoBehaviour
    {
        const int MaxLength = 15; // "255.255.255.255"

        [SerializeField] TMP_Text _display;
        [SerializeField] string _placeholder = "例: 192.168.0.10";

        string _value = string.Empty;

        public string Value => _value;

        /// <summary>True when <see cref="Value"/> is a dotted-quad IPv4 address.</summary>
        public bool IsValidIPv4 => IsDottedQuad(_value);

        public event Action<string> ValueChanged;

        void Awake()
        {
            foreach (IpKeypadKey key in GetComponentsInChildren<IpKeypadKey>(true))
            {
                key.Pressed += HandleKey;
            }
            UpdateDisplay();
        }

        public void SetValue(string value)
        {
            _value = Sanitize(value);
            UpdateDisplay();
            ValueChanged?.Invoke(_value);
        }

        public static bool IsDottedQuad(string text)
        {
            return !string.IsNullOrEmpty(text)
                && text.Split('.').Length == 4
                && IPAddress.TryParse(text, out IPAddress address)
                && address.AddressFamily == AddressFamily.InterNetwork;
        }

        void HandleKey(IpKeypadKey key)
        {
            string next = key.Kind switch
            {
                IpKeypadKey.KeyKind.Backspace => _value.Length > 0 ? _value.Substring(0, _value.Length - 1) : _value,
                IpKeypadKey.KeyKind.Clear => string.Empty,
                _ => _value.Length < MaxLength ? _value + key.Character : _value,
            };
            SetValue(next);
        }

        static string Sanitize(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }
            var chars = new System.Text.StringBuilder(MaxLength);
            foreach (char c in value)
            {
                if ((char.IsDigit(c) || c == '.') && chars.Length < MaxLength)
                {
                    chars.Append(c);
                }
            }
            return chars.ToString();
        }

        void UpdateDisplay()
        {
            if (_display != null)
            {
                _display.text = _value.Length > 0 ? _value : _placeholder;
            }
        }
    }
}
