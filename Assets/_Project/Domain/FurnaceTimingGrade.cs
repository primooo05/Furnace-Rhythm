namespace FurnaceRhythm.Domain
{
    /// <summary>The timing quality assigned to a single beat.</summary>
    public enum FurnaceTimingGrade
    {
        /// <summary>No judgment has been made for this beat yet.</summary>
        None,
        /// <summary>The input landed inside the narrow target window.</summary>
        Perfect,
        /// <summary>The input landed inside the wider target window.</summary>
        Good,
        /// <summary>The input landed outside the target window or was missed.</summary>
        Miss
    }

}
