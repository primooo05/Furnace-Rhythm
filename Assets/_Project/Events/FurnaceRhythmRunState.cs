namespace FurnaceRhythm.Events
{
    /// <summary>Presentation-facing shift states published by the behavior layer.</summary>
    public enum FurnaceRhythmRunState
    {
        /// <summary>The shift is waiting for input.</summary>
        Ready,
        /// <summary>A timing beat is active.</summary>
        Playing,
        /// <summary>A beat judgment is being shown.</summary>
        Resolving,
        /// <summary>A workpiece result is being shown.</summary>
        OrderResult,
        /// <summary>The final shift result is being shown.</summary>
        ShiftResult
    }

}
