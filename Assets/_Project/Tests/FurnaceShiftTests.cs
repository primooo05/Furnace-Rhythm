using System;
using FurnaceRhythm.Domain;
using NUnit.Framework;

namespace FurnaceRhythm.Tests
{
    /// <summary>Checks shift rules at timing and scoring boundaries without loading Unity.</summary>
    public sealed class FurnaceShiftTests
    {
        /// <summary>Checks that a new shift waits for the first action.</summary>
        [Test]
        public void NewShift_IsReadyWithNoScore()
        {
            FurnaceShift shift = new FurnaceShift(CreateSettings());

            Assert.That(shift.Snapshot.State, Is.EqualTo(FurnaceShiftState.Ready));
            Assert.That(shift.Snapshot.Score, Is.Zero);
            Assert.That(shift.Snapshot.OrderIndex, Is.Zero);
        }

        /// <summary>Checks that zero elapsed time leaves a ready shift unchanged.</summary>
        [Test]
        public void Advance_WithZeroTime_DoesNotChangeState()
        {
            FurnaceShift shift = new FurnaceShift(CreateSettings());

            shift.Advance(0f);

            Assert.That(shift.Snapshot.State, Is.EqualTo(FurnaceShiftState.Ready));
            Assert.That(shift.Snapshot.Progress, Is.Zero);
        }

        /// <summary>Checks that negative or non-finite elapsed time is rejected.</summary>
        /// <param name="deltaSeconds">Elapsed time supplied to the domain service.</param>
        [TestCase(-0.01f)]
        [TestCase(float.NaN)]
        [TestCase(float.PositiveInfinity)]
        public void Advance_WithInvalidTime_Throws(float deltaSeconds)
        {
            FurnaceShift shift = new FurnaceShift(CreateSettings());

            Assert.Throws<ArgumentOutOfRangeException>(() => shift.Advance(deltaSeconds));
        }

        /// <summary>Checks that a hit at the target center receives a Perfect judgment.</summary>
        [Test]
        public void PressAction_AtTargetCenter_AwardsPerfectPoints()
        {
            FurnaceShift shift = new FurnaceShift(CreateSettings());
            shift.PressAction();

            shift.Advance(0.5f);
            shift.PressAction();

            Assert.That(shift.Snapshot.State, Is.EqualTo(FurnaceShiftState.Resolving));
            Assert.That(shift.Snapshot.LastGrade, Is.EqualTo(FurnaceTimingGrade.Perfect));
            Assert.That(shift.Snapshot.AwardedPoints, Is.EqualTo(100));
        }

        /// <summary>Checks that a hit inside the wider timing band receives a Good judgment.</summary>
        [Test]
        public void PressAction_InsideGoodWindow_AwardsGoodPoints()
        {
            FurnaceShift shift = new FurnaceShift(CreateSettings());
            shift.PressAction();

            shift.Advance(0.7f);
            shift.PressAction();

            Assert.That(shift.Snapshot.LastGrade, Is.EqualTo(FurnaceTimingGrade.Good));
            Assert.That(shift.Snapshot.AwardedPoints, Is.EqualTo(60));
            Assert.That(shift.Snapshot.TotalGood, Is.EqualTo(1));
        }

        /// <summary>Checks that a miss resets the streak and lowers the workpiece quality.</summary>
        [Test]
        public void MissedBeat_ResetsStreakAndRequiresRework()
        {
            FurnaceShift shift = new FurnaceShift(CreateSettings());
            shift.PressAction();
            shift.Advance(0.5f);
            shift.PressAction();
            shift.Advance(0.2f);
            shift.Advance(0.8f);
            Assert.That(shift.Snapshot.CurrentStreak, Is.Zero);
            shift.Advance(0.2f);
            shift.Advance(0.5f);
            shift.PressAction();
            shift.Advance(0.2f);

            FurnaceShiftSnapshot result = shift.Snapshot;
            Assert.That(result.State, Is.EqualTo(FurnaceShiftState.OrderResult));
            Assert.That(result.PhaseGrade1, Is.EqualTo(FurnaceTimingGrade.Miss));
            Assert.That(result.OrderMiss, Is.EqualTo(1));
            Assert.That(result.OrderQuality, Is.EqualTo("NEEDS REWORK"));
        }

        /// <summary>Checks that the largest finite frame step resolves safely and cannot exceed the sweep.</summary>
        [Test]
        public void Advance_WithLargestFiniteTime_ResolvesAsOneMiss()
        {
            FurnaceShift shift = new FurnaceShift(CreateSettings());
            shift.PressAction();

            shift.Advance(float.MaxValue);

            Assert.That(shift.Snapshot.State, Is.EqualTo(FurnaceShiftState.Resolving));
            Assert.That(shift.Snapshot.TotalMiss, Is.EqualTo(1));
            Assert.That(shift.Snapshot.Progress, Is.EqualTo(1f));
        }

        /// <summary>Checks the maximum score and capped combo bonus across a complete shift.</summary>
        [Test]
        public void PerfectShift_AwardsMaximumCappedComboScore()
        {
            FurnaceShift shift = new FurnaceShift(CreateSettings());
            CompletePerfectShift(shift);

            FurnaceShiftSnapshot completed = shift.Snapshot;
            Assert.That(completed.State, Is.EqualTo(FurnaceShiftState.ShiftResult));
            Assert.That(completed.TotalPerfect, Is.EqualTo(15));
            Assert.That(completed.Score, Is.EqualTo(2100));
            Assert.That(completed.BestStreak, Is.EqualTo(15));
        }

        /// <summary>Checks that starting again after results clears the previous run.</summary>
        [Test]
        public void PressAction_AfterShiftResult_StartsFreshShift()
        {
            FurnaceShift shift = new FurnaceShift(CreateSettings());
            CompletePerfectShift(shift);

            shift.PressAction();

            Assert.That(shift.Snapshot.State, Is.EqualTo(FurnaceShiftState.Playing));
            Assert.That(shift.Snapshot.Score, Is.Zero);
            Assert.That(shift.Snapshot.BestStreak, Is.Zero);
            Assert.That(shift.Snapshot.OrderIndex, Is.Zero);
        }

        /// <summary>Checks that invalid timing windows are rejected when settings are constructed.</summary>
        /// <param name="beatDuration">Duration of the timing sweep.</param>
        /// <param name="center">Normalized target center.</param>
        /// <param name="perfectWindow">Perfect window half-width.</param>
        /// <param name="goodWindow">Good window half-width.</param>
        [TestCase(0f, 0.5f, 0.1f, 0.25f)]
        [TestCase(1f, 0f, 0.1f, 0.25f)]
        [TestCase(1f, 0.5f, 0.3f, 0.25f)]
        [TestCase(1f, 0.5f, 0.1f, 0.6f)]
        public void Settings_WithInvalidTiming_Throw(float beatDuration, float center, float perfectWindow, float goodWindow)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new FurnaceShiftSettings(
                beatDuration, center, perfectWindow, goodWindow, 0.2f, 0.3f));
        }

        private static FurnaceShiftSettings CreateSettings()
        {
            return new FurnaceShiftSettings(1f, 0.5f, 0.1f, 0.25f, 0.2f, 0.3f);
        }

        private static void CompletePerfectShift(FurnaceShift shift)
        {
            shift.PressAction();
            for (int order = 0; order < 5; order++)
            {
                for (int phase = 0; phase < 3; phase++)
                {
                    shift.Advance(0.5f);
                    shift.PressAction();
                    shift.Advance(0.2f);
                }

                shift.Advance(0.3f);
            }
        }
    }
}
