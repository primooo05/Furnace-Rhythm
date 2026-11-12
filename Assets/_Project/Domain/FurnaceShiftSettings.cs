using System;

namespace FurnaceRhythm.Domain
{
    /// <summary>Immutable timing and presentation durations used by a shift.</summary>
    public readonly struct FurnaceShiftSettings
    {
        /// <summary>Creates validated timing settings for a shift.</summary>
        /// <param name="beatDurationSeconds">Duration of each timing sweep; must be positive.</param>
        /// <param name="targetCenter">Target position between zero and one.</param>
        /// <param name="perfectWindow">Half-width of the Perfect region.</param>
        /// <param name="goodWindow">Half-width of the Good region and automatic miss point.</param>
        /// <param name="judgmentHoldSeconds">How long a beat result remains before advancing.</param>
        /// <param name="orderResultHoldSeconds">How long a workpiece result remains before advancing.</param>
        /// <exception cref="ArgumentOutOfRangeException">A timing value is outside its valid range.</exception>
        public FurnaceShiftSettings(
            float beatDurationSeconds,
            float targetCenter,
            float perfectWindow,
            float goodWindow,
            float judgmentHoldSeconds,
            float orderResultHoldSeconds)
        {
            ValidatePositive(beatDurationSeconds, nameof(beatDurationSeconds));
            ValidatePositive(perfectWindow, nameof(perfectWindow));
            ValidatePositive(goodWindow, nameof(goodWindow));
            ValidatePositive(judgmentHoldSeconds, nameof(judgmentHoldSeconds));
            ValidatePositive(orderResultHoldSeconds, nameof(orderResultHoldSeconds));

            if (!IsFinite(targetCenter) || targetCenter <= 0f || targetCenter >= 1f)
                throw new ArgumentOutOfRangeException(nameof(targetCenter), "The target center must be between zero and one.");
            if (perfectWindow > goodWindow)
                throw new ArgumentOutOfRangeException(nameof(perfectWindow), "The Perfect window cannot be wider than the Good window.");
            if (targetCenter - goodWindow < 0f || targetCenter + goodWindow >= 1f)
                throw new ArgumentOutOfRangeException(nameof(goodWindow), "The Good window must fit within the timing sweep.");

            BeatDurationSeconds = beatDurationSeconds;
            TargetCenter = targetCenter;
            PerfectWindow = perfectWindow;
            GoodWindow = goodWindow;
            JudgmentHoldSeconds = judgmentHoldSeconds;
            OrderResultHoldSeconds = orderResultHoldSeconds;
        }

        /// <summary>Gets the default timing used by the sample shift.</summary>
        public static FurnaceShiftSettings Default => new FurnaceShiftSettings(1.7f, 0.68f, 0.045f, 0.12f, 0.5f, 1.1f);

        /// <summary>Gets the duration of a timing sweep in seconds.</summary>
        public float BeatDurationSeconds { get; }
        /// <summary>Gets the normalized center of the target window.</summary>
        public float TargetCenter { get; }
        /// <summary>Gets the half-width of the Perfect window.</summary>
        public float PerfectWindow { get; }
        /// <summary>Gets the half-width of the Good window.</summary>
        public float GoodWindow { get; }
        /// <summary>Gets the delay before the next beat begins.</summary>
        public float JudgmentHoldSeconds { get; }
        /// <summary>Gets the delay before the next workpiece begins.</summary>
        public float OrderResultHoldSeconds { get; }

        private static void ValidatePositive(float value, string parameterName)
        {
            if (!IsFinite(value) || value <= 0f)
                throw new ArgumentOutOfRangeException(parameterName, "The value must be finite and greater than zero.");
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }
    }

}
