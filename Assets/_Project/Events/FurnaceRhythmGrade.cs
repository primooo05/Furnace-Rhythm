namespace FurnaceRhythm.Events
{
    /// <summary>Presentation-facing timing grades published by the behavior layer.</summary>
    public enum FurnaceRhythmGrade
    {
        /// <summary>No grade has been recorded for this beat.</summary>
        None,
        /// <summary>The input landed in the Perfect window.</summary>
        Perfect,
        /// <summary>The input landed in the Good window.</summary>
        Good,
        /// <summary>The input landed outside the target window.</summary>
        Miss
    }

}
