using System;
using System.Collections.Generic;
using UnityEngine;

namespace DNPDemo.Audio
{
    /// <summary>
    /// The clip and volume of each <see cref="SeEvent"/>, and the master volume of every sound effect. The clips are not
    /// in the repository (see README), so any entry may have no clip; such an event is silent. Generated from the menu
    /// "DNP Demo/Setup/Install Sound Effects".
    /// <para>
    /// The master volume (<see cref="MasterVolume"/>) is the one place to set how loud the effects are: the interaction
    /// effects (<see cref="SePlayer"/>) and the scenario's sound cues (<see cref="DNPDemo.Scenario.ScenarioStage"/>) both
    /// play at it. The per-event volume here and the volume of a sound cue default to 1 and only lower one sound against
    /// the others. Lowering the effects while a line plays is separate (<see cref="SeDucking"/>).
    /// </para>
    /// </summary>
    [CreateAssetMenu(menuName = "DNP Demo/SE Catalog", fileName = "SeCatalog")]
    public sealed class SeCatalog : ScriptableObject
    {
        [Serializable]
        public sealed class Entry
        {
            public SeEvent Event;
            public AudioClip Clip;
            [Range(0f, 1f)] public float Volume = 1f;
        }

        /// <summary>
        /// Master volume a new catalog gets, and the one used without a catalog. The clips are normalized on import, so
        /// at full volume the effects drown the voice lines.
        /// </summary>
        public const float DefaultMasterVolume = 0.3f;

        [Tooltip("Volume of every sound effect (interaction effects and the scenario's sound cues).")]
        [SerializeField, Range(0f, 1f)] float _masterVolume = DefaultMasterVolume;

        [SerializeField] List<Entry> _entries = new();

        public float MasterVolume => _masterVolume;

        public IReadOnlyList<Entry> Entries => _entries;

        /// <summary>The master volume of <paramref name="catalog"/>, or <see cref="DefaultMasterVolume"/> without one.</summary>
        public static float MasterVolumeOf(SeCatalog catalog) => catalog != null ? catalog.MasterVolume : DefaultMasterVolume;

        /// <summary>The clip of <paramref name="seEvent"/>. False when the event has no entry or its clip is missing.</summary>
        public bool TryGet(SeEvent seEvent, out AudioClip clip, out float volume)
        {
            Entry entry = Find(seEvent);
            if (entry != null && entry.Clip != null)
            {
                clip = entry.Clip;
                volume = entry.Volume;
                return true;
            }
            clip = null;
            volume = 0f;
            return false;
        }

        /// <summary>The entry of <paramref name="seEvent"/>, or null.</summary>
        public Entry Find(SeEvent seEvent)
        {
            foreach (Entry entry in _entries)
            {
                if (entry != null && entry.Event == seEvent)
                {
                    return entry;
                }
            }
            return null;
        }

        /// <summary>The entry of <paramref name="seEvent"/>, added when missing. For the editor builder and tests.</summary>
        internal Entry GetOrAdd(SeEvent seEvent)
        {
            Entry entry = Find(seEvent);
            if (entry == null)
            {
                entry = new Entry { Event = seEvent };
                _entries.Add(entry);
            }
            return entry;
        }
    }
}
