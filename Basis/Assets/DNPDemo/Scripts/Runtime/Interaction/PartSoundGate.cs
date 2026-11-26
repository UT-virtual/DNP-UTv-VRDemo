using DNPDemo.Audio;

namespace DNPDemo.Interaction
{
    /// <summary>
    /// Decides the sounds of the part changes in <see cref="InteractionRunner"/>, so that one operation plays one sound
    /// on every device.
    /// <list type="bullet">
    /// <item>A completed part plays <see cref="CompletionSound"/>: <see cref="SeEvent.Complete"/> for the step's last
    /// part, otherwise the operation's own sound. The actor and the other devices use the same rule, so they hear the
    /// same sound.</item>
    /// <item>A part that begins plays <see cref="SeEvent.TaskShow"/> once (<see cref="TryAnnounce"/>). The first part of
    /// a step always does; a later part stays quiet when the part before it ended with a completion sound
    /// (<see cref="MarkCompletedWithSound"/>), because that sound already marks the change.</item>
    /// </list>
    /// Parts are identified by (sequence, step, part) as in <see cref="InteractionProgress"/>.
    /// </summary>
    internal sealed class PartSoundGate
    {
        ushort _announcedSequence;
        int _announcedStep = -1;
        int _announcedPart = -1;

        ushort _soundedSequence;
        int _soundedStep = -1;
        int _soundedPart = -1;

        /// <summary>The sound of completing a part: <see cref="SeEvent.Complete"/> for the last one, else <paramref name="feedback"/>.</summary>
        public static SeEvent? CompletionSound(int partIndex, int partCount, SeEvent? feedback) =>
            partIndex >= partCount - 1 ? SeEvent.Complete : feedback;

        /// <summary>Records that the part ended with a completion sound, so the next part's task sound is skipped.</summary>
        public void MarkCompletedWithSound(ushort sequence, int step, int part)
        {
            _soundedSequence = sequence;
            _soundedStep = step;
            _soundedPart = part;
        }

        /// <summary>
        /// True when <see cref="SeEvent.TaskShow"/> should play for the part that begins now. Each part is decided once:
        /// a restart of the same part (the local role changed) returns false.
        /// </summary>
        public bool TryAnnounce(ushort sequence, int step, int part)
        {
            if (_announcedSequence == sequence && _announcedStep == step && _announcedPart == part)
            {
                return false;
            }
            _announcedSequence = sequence;
            _announcedStep = step;
            _announcedPart = part;
            bool previousSounded = part > 0 && _soundedSequence == sequence && _soundedStep == step && _soundedPart == part - 1;
            return !previousSounded;
        }
    }
}
