using System;

namespace FurnaceRhythm.Events
{
    /// <summary>Immutable event data used by HUD and effects listeners.</summary>
    public readonly struct FurnaceRhythmSnapshot
    {
        /// <summary>Creates an event payload containing the current shift display values.</summary>
        /// <param name="state">Current shift stage.</param>
        /// <param name="phaseIndex">Zero-based timing phase.</param>
        /// <param name="orderIndex">Zero-based workpiece index.</param>
        /// <param name="progress">Timing marker position from zero to one.</param>
        /// <param name="score">Shift score.</param>
        /// <param name="orderScore">Current workpiece score.</param>
        /// <param name="currentStreak">Current consecutive-hit streak.</param>
        /// <param name="bestStreak">Longest streak in the shift.</param>
        /// <param name="totalPerfect">Total Perfect judgments.</param>
        /// <param name="totalGood">Total Good judgments.</param>
        /// <param name="totalMiss">Total Miss judgments.</param>
        /// <param name="phaseGrade0">Heat grade.</param>
        /// <param name="phaseGrade1">Hammer grade.</param>
        /// <param name="phaseGrade2">Quench grade.</param>
        /// <param name="lastGrade">Most recent beat grade.</param>
        /// <param name="awardedPoints">Points from the most recent beat.</param>
        /// <param name="lastMessage">Judgment message shown in the HUD.</param>
        /// <param name="orderPerfect">Perfect count for the current workpiece.</param>
        /// <param name="orderGood">Good count for the current workpiece.</param>
        /// <param name="orderMiss">Miss count for the current workpiece.</param>
        /// <param name="orderQuality">Quality label for the current workpiece result.</param>
        /// <param name="shiftRank">Rank label for the completed shift.</param>
        /// <param name="targetCenter">Normalized center of the target band.</param>
        /// <param name="perfectWindow">Half-width of the Perfect band.</param>
        /// <param name="goodWindow">Half-width of the Good band.</param>
        /// <exception cref="ArgumentOutOfRangeException">An index, counter, grade, or timing value is invalid.</exception>
        public FurnaceRhythmSnapshot(
            FurnaceRhythmRunState state,
            int phaseIndex,
            int orderIndex,
            float progress,
            int score,
            int orderScore,
            int currentStreak,
            int bestStreak,
            int totalPerfect,
            int totalGood,
            int totalMiss,
            FurnaceRhythmGrade phaseGrade0,
            FurnaceRhythmGrade phaseGrade1,
            FurnaceRhythmGrade phaseGrade2,
            FurnaceRhythmGrade lastGrade,
            int awardedPoints,
            string lastMessage,
            int orderPerfect,
            int orderGood,
            int orderMiss,
            string orderQuality,
            string shiftRank,
            float targetCenter,
            float perfectWindow,
            float goodWindow)
        {
            if (!Enum.IsDefined(typeof(FurnaceRhythmRunState), state))
                throw new ArgumentOutOfRangeException(nameof(state));
            if (phaseIndex < 0 || phaseIndex >= 3)
                throw new ArgumentOutOfRangeException(nameof(phaseIndex));
            if (orderIndex < 0 || orderIndex >= 5)
                throw new ArgumentOutOfRangeException(nameof(orderIndex));
            if (!IsFinite(progress) || progress < 0f || progress > 1f)
                throw new ArgumentOutOfRangeException(nameof(progress));
            if (score < 0 || orderScore < 0 || currentStreak < 0 || bestStreak < 0 ||
                totalPerfect < 0 || totalGood < 0 || totalMiss < 0 || awardedPoints < 0 || awardedPoints > 150 ||
                orderPerfect < 0 || orderGood < 0 || orderMiss < 0)
                throw new ArgumentOutOfRangeException(nameof(score), "Shift counters and points must be non-negative and within valid ranges.");
            if (!Enum.IsDefined(typeof(FurnaceRhythmGrade), phaseGrade0) ||
                !Enum.IsDefined(typeof(FurnaceRhythmGrade), phaseGrade1) ||
                !Enum.IsDefined(typeof(FurnaceRhythmGrade), phaseGrade2) ||
                !Enum.IsDefined(typeof(FurnaceRhythmGrade), lastGrade))
                throw new ArgumentOutOfRangeException(nameof(lastGrade));
            if (!IsFinite(targetCenter) || !IsFinite(perfectWindow) || !IsFinite(goodWindow) ||
                targetCenter <= 0f || targetCenter >= 1f || perfectWindow <= 0f || goodWindow <= 0f ||
                perfectWindow > goodWindow || targetCenter - goodWindow < 0f || targetCenter + goodWindow >= 1f)
                throw new ArgumentOutOfRangeException(nameof(targetCenter), "Timing windows must be finite and fit within the sweep.");

            State = state;
            PhaseIndex = phaseIndex;
            OrderIndex = orderIndex;
            Progress = progress;
            Score = score;
            OrderScore = orderScore;
            CurrentStreak = currentStreak;
            BestStreak = bestStreak;
            TotalPerfect = totalPerfect;
            TotalGood = totalGood;
            TotalMiss = totalMiss;
            PhaseGrade0 = phaseGrade0;
            PhaseGrade1 = phaseGrade1;
            PhaseGrade2 = phaseGrade2;
            LastGrade = lastGrade;
            AwardedPoints = awardedPoints;
            LastMessage = lastMessage ?? string.Empty;
            OrderPerfect = orderPerfect;
            OrderGood = orderGood;
            OrderMiss = orderMiss;
            OrderQuality = orderQuality ?? string.Empty;
            ShiftRank = shiftRank ?? string.Empty;
            TargetCenter = targetCenter;
            PerfectWindow = perfectWindow;
            GoodWindow = goodWindow;
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }

        /// <summary>Gets the shift stage.</summary>
        public FurnaceRhythmRunState State { get; }
        /// <summary>Gets the zero-based timing phase.</summary>
        public int PhaseIndex { get; }
        /// <summary>Gets the zero-based workpiece index.</summary>
        public int OrderIndex { get; }
        /// <summary>Gets timing marker progress.</summary>
        public float Progress { get; }
        /// <summary>Gets the shift score.</summary>
        public int Score { get; }
        /// <summary>Gets the current workpiece score.</summary>
        public int OrderScore { get; }
        /// <summary>Gets the current streak.</summary>
        public int CurrentStreak { get; }
        /// <summary>Gets the best streak.</summary>
        public int BestStreak { get; }
        /// <summary>Gets the total Perfect count.</summary>
        public int TotalPerfect { get; }
        /// <summary>Gets the total Good count.</summary>
        public int TotalGood { get; }
        /// <summary>Gets the total Miss count.</summary>
        public int TotalMiss { get; }
        /// <summary>Gets the Heat grade.</summary>
        public FurnaceRhythmGrade PhaseGrade0 { get; }
        /// <summary>Gets the Hammer grade.</summary>
        public FurnaceRhythmGrade PhaseGrade1 { get; }
        /// <summary>Gets the Quench grade.</summary>
        public FurnaceRhythmGrade PhaseGrade2 { get; }
        /// <summary>Gets the most recent grade.</summary>
        public FurnaceRhythmGrade LastGrade { get; }
        /// <summary>Gets the points awarded on the most recent beat.</summary>
        public int AwardedPoints { get; }
        /// <summary>Gets the most recent judgment message.</summary>
        public string LastMessage { get; }
        /// <summary>Gets the workpiece Perfect count.</summary>
        public int OrderPerfect { get; }
        /// <summary>Gets the workpiece Good count.</summary>
        public int OrderGood { get; }
        /// <summary>Gets the workpiece Miss count.</summary>
        public int OrderMiss { get; }
        /// <summary>Gets the workpiece quality label.</summary>
        public string OrderQuality { get; }
        /// <summary>Gets the completed shift rank label.</summary>
        public string ShiftRank { get; }
        /// <summary>Gets the normalized target center.</summary>
        public float TargetCenter { get; }
        /// <summary>Gets the Perfect window half-width.</summary>
        public float PerfectWindow { get; }
        /// <summary>Gets the Good window half-width.</summary>
        public float GoodWindow { get; }
    }

}
