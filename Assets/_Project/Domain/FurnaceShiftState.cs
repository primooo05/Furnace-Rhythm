namespace FurnaceRhythm.Domain
{
    /// <summary>Identifies the current stage of a five-workpiece shift.</summary>
    public enum FurnaceShiftState
    {
        /// <summary>The shift is waiting for the player to start.</summary>
        Ready,
        /// <summary>A timing beat is moving toward its target.</summary>
        Playing,
        /// <summary>The last beat judgment is being held on screen.</summary>
        Resolving,
        /// <summary>A completed workpiece result is being shown.</summary>
        OrderResult,
        /// <summary>The shift result is being shown.</summary>
        ShiftResult
    }

}
