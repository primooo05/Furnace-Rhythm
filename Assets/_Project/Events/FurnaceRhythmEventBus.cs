using System;

namespace FurnaceRhythm.Events
{
    /// <summary>Provides the event boundary between game behavior and presentation.</summary>
    public static class FurnaceRhythmEventBus
    {
        /// <summary>Raised when the behavior layer publishes a shift snapshot.</summary>
        public static event Action<FurnaceRhythmSnapshot> SnapshotChanged;

        /// <summary>Publishes the current shift snapshot to active listeners.</summary>
        /// <param name="snapshot">Values to display.</param>
        /// <exception cref="ArgumentOutOfRangeException">The snapshot is uninitialized.</exception>
        public static void Publish(FurnaceRhythmSnapshot snapshot)
        {
            if (snapshot.TargetCenter <= 0f)
                throw new ArgumentOutOfRangeException(nameof(snapshot), "The snapshot must be initialized before publication.");

            SnapshotChanged?.Invoke(snapshot);
        }
    }
}
