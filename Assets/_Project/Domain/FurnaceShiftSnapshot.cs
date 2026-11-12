using System;

namespace FurnaceRhythm.Domain
{
    /// <summary>Read-only values describing the current shift state.</summary>
    public readonly struct FurnaceShiftSnapshot
    {
        internal FurnaceShiftSnapshot(
            FurnaceShiftState state,
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
            FurnaceTimingGrade phaseGrade0,
            FurnaceTimingGrade phaseGrade1,
            FurnaceTimingGrade phaseGrade2,
            FurnaceTimingGrade lastGrade,
            int awardedPoints,
            string lastMessage,
            int orderPerfect,
            int orderGood,
            int orderMiss,
            string orderQuality,
            string shiftRank,
            FurnaceShiftSettings settings)
        {
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
            LastMessage = lastMessage;
            OrderPerfect = orderPerfect;
            OrderGood = orderGood;
            OrderMiss = orderMiss;
            OrderQuality = orderQuality;
            ShiftRank = shiftRank;
            Settings = settings;
        }

        /// <summary>Gets the current shift state.</summary>
        public FurnaceShiftState State { get; }
        /// <summary>Gets the zero-based Heat, Hammer, or Quench phase index.</summary>
        public int PhaseIndex { get; }
        /// <summary>Gets the zero-based workpiece index.</summary>
        public int OrderIndex { get; }
        /// <summary>Gets the current marker progress from zero to one.</summary>
        public float Progress { get; }
        /// <summary>Gets the total score for the shift.</summary>
        public int Score { get; }
        /// <summary>Gets the score earned on the current workpiece.</summary>
        public int OrderScore { get; }
        /// <summary>Gets the current consecutive-hit streak.</summary>
        public int CurrentStreak { get; }
        /// <summary>Gets the longest streak in the shift.</summary>
        public int BestStreak { get; }
        /// <summary>Gets the number of Perfect judgments in the shift.</summary>
        public int TotalPerfect { get; }
        /// <summary>Gets the number of Good judgments in the shift.</summary>
        public int TotalGood { get; }
        /// <summary>Gets the number of Miss judgments in the shift.</summary>
        public int TotalMiss { get; }
        /// <summary>Gets the grade recorded for Heat.</summary>
        public FurnaceTimingGrade PhaseGrade0 { get; }
        /// <summary>Gets the grade recorded for Hammer.</summary>
        public FurnaceTimingGrade PhaseGrade1 { get; }
        /// <summary>Gets the grade recorded for Quench.</summary>
        public FurnaceTimingGrade PhaseGrade2 { get; }
        /// <summary>Gets the most recent beat grade.</summary>
        public FurnaceTimingGrade LastGrade { get; }
        /// <summary>Gets the points awarded by the most recent beat.</summary>
        public int AwardedPoints { get; }
        /// <summary>Gets the most recent judgment message.</summary>
        public string LastMessage { get; }
        /// <summary>Gets the Perfect count for the current workpiece.</summary>
        public int OrderPerfect { get; }
        /// <summary>Gets the Good count for the current workpiece.</summary>
        public int OrderGood { get; }
        /// <summary>Gets the Miss count for the current workpiece.</summary>
        public int OrderMiss { get; }
        /// <summary>Gets the quality label for the current workpiece result.</summary>
        public string OrderQuality { get; }
        /// <summary>Gets the rank label for the completed shift.</summary>
        public string ShiftRank { get; }
        /// <summary>Gets the timing settings used to calculate this snapshot.</summary>
        public FurnaceShiftSettings Settings { get; }
    }

}
