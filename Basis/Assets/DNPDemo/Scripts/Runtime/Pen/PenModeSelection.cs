using System;
using DNPDemo.Interaction;
using UnityEngine;

namespace DNPDemo.Pen
{
    /// <summary>
    /// The mode the local player picked for their pen on the main menu. The menu entries are registered before any
    /// scene loads and cannot reference scene objects, so the menu writes here and the pen reads from here.
    /// </summary>
    public static class PenModeSelection
    {
        /// <summary>Modes offered on the menu, left to right.</summary>
        public static readonly PenMode[] Selectable = { PenMode.Select, PenMode.Voice, PenMode.Draw };

        public const PenMode Default = PenMode.Select;

        public static PenMode Current { get; private set; } = Default;

        /// <summary>Raised after <see cref="Current"/> changes.</summary>
        public static event Action<PenMode> Changed;

        public static bool IsSelectable(PenMode mode) => Array.IndexOf(Selectable, mode) >= 0;

        /// <summary>Switches the pen to <paramref name="mode"/>. Modes not offered on the menu are ignored.</summary>
        public static void Select(PenMode mode)
        {
            if (!IsSelectable(mode) || mode == Current)
            {
                return;
            }
            Current = mode;
            Changed?.Invoke(mode);
        }

        // Play mode can start without a domain reload, so the static state is reset explicitly.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        internal static void ResetToDefault()
        {
            Current = Default;
            Changed = null;
        }
    }
}
