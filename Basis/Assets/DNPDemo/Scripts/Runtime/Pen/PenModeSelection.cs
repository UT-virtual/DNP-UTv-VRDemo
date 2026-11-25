using System;
using DNPDemo.Interaction;
using UnityEngine;

namespace DNPDemo.Pen
{
    /// <summary>
    /// The mode the local player picked for their pen, on the main menu or with the pen's mode button
    /// (<see cref="SelectNext"/>). The menu entries are registered before any scene loads and cannot reference scene
    /// objects, so the menu writes here and the pen reads from here.
    /// </summary>
    public static class PenModeSelection
    {
        /// <summary>Modes offered on the menu, left to right. The mode button steps through them in this order.</summary>
        public static readonly PenMode[] Selectable = { PenMode.Select, PenMode.Manipulate, PenMode.Voice, PenMode.Draw };

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

        /// <summary>Switches to the mode after <see cref="Current"/> on the menu, wrapping from the last to the first.</summary>
        public static void SelectNext() => Select(Next(Current));

        /// <summary>The mode after <paramref name="mode"/> on the menu. A mode not on the menu is followed by the first one.</summary>
        public static PenMode Next(PenMode mode)
        {
            int index = Array.IndexOf(Selectable, mode);
            return Selectable[(index + 1) % Selectable.Length];
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
