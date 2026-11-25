namespace DNPDemo.Audio
{
    /// <summary>
    /// Moments that have a sound effect. <see cref="SeCatalog"/> maps each to a clip. The values are serialized in the
    /// catalog asset, so a new event gets a new value and existing values never change.
    /// </summary>
    public enum SeEvent
    {
        /// <summary>A part is added to the statue (played by a SoundCue in the scenario).</summary>
        Generate = 0,

        /// <summary>The statue is reshaped (played by a SoundCue in the scenario).</summary>
        Morph = 1,

        /// <summary>A selection was accepted.</summary>
        SelectConfirm = 2,

        /// <summary>A moved object snapped into place.</summary>
        Snap = 3,

        /// <summary>The voice command started listening.</summary>
        VoiceStart = 4,

        /// <summary>The voice command was accepted when the trigger was released.</summary>
        VoiceEnd = 5,

        /// <summary>A new interaction part began, so the task HUD shows a new task.</summary>
        TaskShow = 6,

        /// <summary>The last part of a step was done.</summary>
        Complete = 7,

        /// <summary>An attempt was rejected and has to be redone.</summary>
        Fail = 8,
    }
}
