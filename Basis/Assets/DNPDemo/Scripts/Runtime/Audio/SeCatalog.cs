using System;
using System.Collections.Generic;
using UnityEngine;

namespace DNPDemo.Audio
{
    /// <summary>
    /// The clip and volume of each <see cref="SeEvent"/>. The clips are not in the repository (see README), so any entry
    /// may have no clip; such an event is silent. Generated from the menu "DNP Demo/Setup/Install Sound Effects".
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

        [SerializeField] List<Entry> _entries = new();

        public IReadOnlyList<Entry> Entries => _entries;

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
