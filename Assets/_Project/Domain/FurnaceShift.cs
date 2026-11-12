using System;

namespace FurnaceRhythm.Domain
{
    /// <summary>Owns the rules and progress for one five-workpiece forging shift.</summary>
    public sealed class FurnaceShift
    {
        private const int _phaseCount = 3;
        private const int _orderCount = 5;
        private readonly FurnaceShiftSettings _settings;
        private readonly FurnaceTimingGrade[] _phaseGrades = new FurnaceTimingGrade[_phaseCount];
        private FurnaceShiftState _state = FurnaceShiftState.Ready;
        private int _phaseIndex;
        private int _orderIndex;
        private int _score;
        private int _orderScore;
        private int _currentStreak;
        private int _bestStreak;
        private int _totalPerfect;
        private int _totalGood;
        private int _totalMiss;
        private float _phaseElapsed;
        private float _resolveElapsed;
        private float _orderResultElapsed;
        private FurnaceTimingGrade _lastGrade;
        private int _awardedPoints;
        private string _lastMessage = string.Empty;
        private int _orderPerfect;
        private int _orderGood;
        private int _orderMiss;
        private string _orderQuality = string.Empty;
        private string _shiftRank = string.Empty;

        /// <summary>Creates a shift with validated timing settings.</summary>
        /// <param name="settings">Timing and hold durations used by the shift.</param>
        /// <exception cref="ArgumentOutOfRangeException">The settings contain invalid values.</exception>
        public FurnaceShift(FurnaceShiftSettings settings)
        {
            this._settings = new FurnaceShiftSettings(
                settings.BeatDurationSeconds,
                settings.TargetCenter,
                settings.PerfectWindow,
                settings.GoodWindow,
                settings.JudgmentHoldSeconds,
                settings.OrderResultHoldSeconds);
            ResetPhaseGrades();
        }

        /// <summary>Gets an immutable view of the current shift state.</summary>
        public FurnaceShiftSnapshot Snapshot
        {
            get
            {
                float progress = _state == FurnaceShiftState.Playing || _state == FurnaceShiftState.Resolving
                    ? Clamp01(_phaseElapsed / _settings.BeatDurationSeconds)
                    : 0f;
                return new FurnaceShiftSnapshot(
                    _state, _phaseIndex, _orderIndex, progress, _score, _orderScore, _currentStreak, _bestStreak,
                    _totalPerfect, _totalGood, _totalMiss,
                    _phaseGrades[0], _phaseGrades[1], _phaseGrades[2], _lastGrade, _awardedPoints, _lastMessage,
                    _orderPerfect, _orderGood, _orderMiss, _orderQuality, _shiftRank, _settings);
            }
        }

        /// <summary>Advances timing and result holds by a frame duration.</summary>
        /// <param name="deltaSeconds">Elapsed time; must be finite and non-negative.</param>
        /// <exception cref="ArgumentOutOfRangeException">The elapsed time is negative or non-finite.</exception>
        public void Advance(float deltaSeconds)
        {
            if (!IsFinite(deltaSeconds) || deltaSeconds < 0f)
                throw new ArgumentOutOfRangeException(nameof(deltaSeconds), "Elapsed time must be finite and non-negative.");

            switch (_state)
            {
                case FurnaceShiftState.Playing:
                    _phaseElapsed += deltaSeconds;
                    if (_progress >= _settings.TargetCenter + _settings.GoodWindow)
                        ResolveBeat(FurnaceTimingGrade.Miss, "LATE · MISS");
                    break;
                case FurnaceShiftState.Resolving:
                    _resolveElapsed += deltaSeconds;
                    if (_resolveElapsed >= _settings.JudgmentHoldSeconds)
                        AdvanceAfterJudgment();
                    break;
                case FurnaceShiftState.OrderResult:
                    _orderResultElapsed += deltaSeconds;
                    if (_orderResultElapsed >= _settings.OrderResultHoldSeconds)
                        AdvanceAfterOrderResult();
                    break;
            }
        }

        /// <summary>Starts a shift, restarts a completed shift, or judges the active timing beat.</summary>
        public void PressAction()
        {
            if (_state == FurnaceShiftState.Ready || _state == FurnaceShiftState.ShiftResult)
            {
                StartShift();
                return;
            }

            if (_state != FurnaceShiftState.Playing)
                return;

            float progress = _progress;
            if (progress >= _settings.TargetCenter + _settings.GoodWindow)
            {
                ResolveBeat(FurnaceTimingGrade.Miss, "LATE · MISS");
                return;
            }

            float distance = Math.Abs(progress - _settings.TargetCenter);
            if (distance <= _settings.PerfectWindow)
                ResolveBeat(FurnaceTimingGrade.Perfect, "PERFECT");
            else if (distance <= _settings.GoodWindow)
                ResolveBeat(FurnaceTimingGrade.Good, "GOOD");
            else
                ResolveBeat(FurnaceTimingGrade.Miss, progress < _settings.TargetCenter ? "EARLY · MISS" : "LATE · MISS");
        }

        private float _progress => Clamp01(_phaseElapsed / _settings.BeatDurationSeconds);

        private void StartShift()
        {
            _score = 0;
            _orderScore = 0;
            _currentStreak = 0;
            _bestStreak = 0;
            _totalPerfect = 0;
            _totalGood = 0;
            _totalMiss = 0;
            _orderIndex = 0;
            _shiftRank = string.Empty;
            BeginOrder();
        }

        private void BeginOrder()
        {
            _orderScore = 0;
            ResetPhaseGrades();
            _phaseIndex = 0;
            _orderPerfect = 0;
            _orderGood = 0;
            _orderMiss = 0;
            _orderQuality = string.Empty;
            BeginBeat();
        }

        private void BeginBeat()
        {
            _state = FurnaceShiftState.Playing;
            _phaseElapsed = 0f;
            _resolveElapsed = 0f;
            _lastGrade = FurnaceTimingGrade.None;
            _awardedPoints = 0;
            _lastMessage = string.Empty;
        }

        private void ResolveBeat(FurnaceTimingGrade grade, string message)
        {
            if (_state != FurnaceShiftState.Playing)
                return;

            _phaseGrades[_phaseIndex] = grade;
            _awardedPoints = AwardPoints(grade);
            _lastGrade = grade;
            _lastMessage = message;
            _state = FurnaceShiftState.Resolving;
            _resolveElapsed = 0f;
        }

        private int AwardPoints(FurnaceTimingGrade grade)
        {
            if (grade == FurnaceTimingGrade.Miss)
            {
                _totalMiss = checked(_totalMiss + 1);
                _currentStreak = 0;
                return 0;
            }

            int basePoints;
            if (grade == FurnaceTimingGrade.Perfect)
            {
                _totalPerfect = checked(_totalPerfect + 1);
                basePoints = 100;
            }
            else
            {
                _totalGood = checked(_totalGood + 1);
                basePoints = 60;
            }

            _currentStreak = checked(_currentStreak + 1);
            _bestStreak = Math.Max(_bestStreak, _currentStreak);
            int comboBonus = Math.Min(_currentStreak - 1, 5) * 10;
            int points = checked(basePoints + comboBonus);
            _score = checked(_score + points);
            _orderScore = checked(_orderScore + points);
            return points;
        }

        private void AdvanceAfterJudgment()
        {
            if (_phaseIndex + 1 < _phaseCount)
            {
                _phaseIndex = checked(_phaseIndex + 1);
                BeginBeat();
                return;
            }

            _orderPerfect = 0;
            _orderGood = 0;
            _orderMiss = 0;
            for (int index = 0; index < _phaseGrades.Length; index++)
            {
                switch (_phaseGrades[index])
                {
                    case FurnaceTimingGrade.Perfect: _orderPerfect = checked(_orderPerfect + 1); break;
                    case FurnaceTimingGrade.Good: _orderGood = checked(_orderGood + 1); break;
                    default: _orderMiss = checked(_orderMiss + 1); break;
                }
            }

            _orderQuality = _orderMiss > 0 ? "NEEDS REWORK" : _orderPerfect == _phaseCount ? "MASTERWORK" : "FINE WORK";
            _state = FurnaceShiftState.OrderResult;
            _orderResultElapsed = 0f;
        }

        private void AdvanceAfterOrderResult()
        {
            if (_orderIndex + 1 < _orderCount)
            {
                _orderIndex = checked(_orderIndex + 1);
                BeginOrder();
                return;
            }

            _shiftRank = _totalMiss == 0 && _totalPerfect == _phaseCount * _orderCount
                ? "MASTER FORGER"
                : _totalMiss <= 2
                    ? "SKILLED SMITH"
                    : _totalPerfect + _totalGood >= 10
                        ? "STEADY HAND"
                        : "KEEP PRACTICING";
            _state = FurnaceShiftState.ShiftResult;
        }

        private void ResetPhaseGrades()
        {
            for (int index = 0; index < _phaseGrades.Length; index++)
                _phaseGrades[index] = FurnaceTimingGrade.Miss;
        }

        private static float Clamp01(float value)
        {
            return value < 0f ? 0f : value > 1f ? 1f : value;
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }
    }
}
