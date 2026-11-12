using System;
using FurnaceRhythm.Domain;
using FurnaceRhythm.Events;
using UnityEngine;
using UnityEngine.InputSystem;

namespace FurnaceRhythm.Behaviors
{
    /// <summary>Forwards player input and frame time to the pure shift rules.</summary>
    [DisallowMultipleComponent]
    public sealed class FurnaceRhythmController : MonoBehaviour
    {
        private FurnaceShift _shift;

        /// <summary>Initializes this controller with validated timing rules.</summary>
        /// <param name="settings">Rules used for the current shift.</param>
        /// <exception cref="InvalidOperationException">The controller was already initialized.</exception>
        /// <exception cref="ArgumentOutOfRangeException">The settings contain invalid values.</exception>
        public void Initialize(FurnaceShiftSettings settings)
        {
            if (_shift != null)
                throw new InvalidOperationException("The Furnace Rhythm controller is already initialized.");

            _shift = new FurnaceShift(settings);
            PublishSnapshot();
        }

        private void Update()
        {
            if (_shift == null)
                return;

            bool actionPressed = ActionPressedThisFrame();
            FurnaceShiftState state = _shift.Snapshot.State;
            if (state == FurnaceShiftState.Ready || state == FurnaceShiftState.ShiftResult)
            {
                if (actionPressed)
                    _shift.PressAction();
            }
            else
            {
                _shift.Advance(Time.unscaledDeltaTime);
                if (state == FurnaceShiftState.Playing && actionPressed)
                    _shift.PressAction();
            }

            PublishSnapshot();
        }

        private void PublishSnapshot()
        {
            FurnaceShiftSnapshot model = _shift.Snapshot;
            FurnaceRhythmEventBus.Publish(new FurnaceRhythmSnapshot(
                MapState(model.State),
                model.PhaseIndex,
                model.OrderIndex,
                model.Progress,
                model.Score,
                model.OrderScore,
                model.CurrentStreak,
                model.BestStreak,
                model.TotalPerfect,
                model.TotalGood,
                model.TotalMiss,
                MapGrade(model.PhaseGrade0),
                MapGrade(model.PhaseGrade1),
                MapGrade(model.PhaseGrade2),
                MapGrade(model.LastGrade),
                model.AwardedPoints,
                model.LastMessage,
                model.OrderPerfect,
                model.OrderGood,
                model.OrderMiss,
                model.OrderQuality,
                model.ShiftRank,
                model.Settings.TargetCenter,
                model.Settings.PerfectWindow,
                model.Settings.GoodWindow));
        }

        private static FurnaceRhythmRunState MapState(FurnaceShiftState state)
        {
            switch (state)
            {
                case FurnaceShiftState.Playing: return FurnaceRhythmRunState.Playing;
                case FurnaceShiftState.Resolving: return FurnaceRhythmRunState.Resolving;
                case FurnaceShiftState.OrderResult: return FurnaceRhythmRunState.OrderResult;
                case FurnaceShiftState.ShiftResult: return FurnaceRhythmRunState.ShiftResult;
                default: return FurnaceRhythmRunState.Ready;
            }
        }

        private static FurnaceRhythmGrade MapGrade(FurnaceTimingGrade grade)
        {
            switch (grade)
            {
                case FurnaceTimingGrade.Perfect: return FurnaceRhythmGrade.Perfect;
                case FurnaceTimingGrade.Good: return FurnaceRhythmGrade.Good;
                case FurnaceTimingGrade.Miss: return FurnaceRhythmGrade.Miss;
                default: return FurnaceRhythmGrade.None;
            }
        }

        private static bool ActionPressedThisFrame()
        {
            Keyboard keyboard = Keyboard.current;
            Mouse mouse = Mouse.current;
            return (keyboard != null && keyboard.spaceKey.wasPressedThisFrame) ||
                   (mouse != null && mouse.leftButton.wasPressedThisFrame);
        }
    }
}
