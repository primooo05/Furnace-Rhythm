
using TMPro;
using UnityEngine;
using FurnaceRhythm.Config;
using FurnaceRhythm.Events;

namespace FurnaceRhythm.Presentation
{
    /// <summary>
    /// Builds the Furnace Rhythm HUD and renders shift snapshots published by the behavior layer.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class FurnaceRhythmPresenter : MonoBehaviour
    {
        private enum RoundState
        {
            Ready,
            Playing,
            Resolving,
            OrderResult,
            ShiftResult
        }

        private enum TimingGrade
        {
            Perfect,
            Good,
            Miss
        }

        private const int _phaseCount = 3;
        private const int _orderCount = 5;
        private const int _sparkCount = 8;
        private const float _impactDurationSeconds = 0.32f;
        private const float _sparkDurationSeconds = 0.38f;
        private const float _impactRingDurationSeconds = 0.34f;
        private const float _scoreCountUpSeconds = 0.26f;
        private const float _overlayFadeOutSeconds = 0.16f;
        private const float _overlayRevealSeconds = 0.24f;
        private static readonly string[] _phaseNames = { "HEAT", "HAMMER", "QUENCH" };

        [Header("Shift and timing")]
        private Sprite _hotBilletSprite;
        private FurnaceRhythmTuning _tuning;
        private float _targetCenter = 0.68f;
        private float _perfectWindow = 0.045f;
        private float _goodWindow = 0.12f;

        private readonly TimingGrade[] _orderGrades = new TimingGrade[_phaseCount];
        private readonly UnityEngine.UI.Image[] _phaseIndicators = new UnityEngine.UI.Image[_phaseCount];
        private readonly TextMeshProUGUI[] _phaseIndicatorLabels = new TextMeshProUGUI[_phaseCount];
        private readonly UnityEngine.UI.Image[] _orderProgressSegments = new UnityEngine.UI.Image[_orderCount];
        private readonly UnityEngine.UI.Image[] _comboBonusPips = new UnityEngine.UI.Image[5];
        private readonly UnityEngine.UI.Image[] _orderGradeChipImages = new UnityEngine.UI.Image[_phaseCount];
        private readonly TextMeshProUGUI[] _orderGradeChipValueLabels = new TextMeshProUGUI[_phaseCount];
        private readonly UnityEngine.UI.Image[] _sparkImages = new UnityEngine.UI.Image[_sparkCount];
        private readonly RectTransform[] _sparkRects = new RectTransform[_sparkCount];
        private readonly Vector2[] _sparkDirections = new Vector2[_sparkCount];

        private RoundState _state;
        private int _currentPhaseIndex;
        private int _currentOrderIndex;
        private int _score;
        private int _orderScore;
        private int _currentStreak;
        private int _bestStreak;
        private int _totalPerfect;
        private int _totalGood;
        private int _totalMiss;
        private RectTransform _timingTrackRect;
        private RectTransform _markerRect;
        private RectTransform _markerTrailRect;
        private RectTransform _workpieceRect;
        private RectTransform _glowRect;
        private RectTransform _impactRingRect;
        private RectTransform _judgmentRect;
        private UnityEngine.UI.Image _markerImage;
        private UnityEngine.UI.Image _markerTrailImage;
        private UnityEngine.UI.Image _workpieceImage;
        private UnityEngine.UI.Image _glowImage;
        private UnityEngine.UI.Image _impactRingImage;
        private UnityEngine.UI.Image _feedbackFlashImage;
        private UnityEngine.UI.Image _targetBandImage;
        private UnityEngine.UI.Image _perfectBandImage;
        private TextMeshProUGUI _phaseLabel;
        private TextMeshProUGUI _judgmentLabel;
        private TextMeshProUGUI _instructionLabel;
        private TextMeshProUGUI _scoreValueLabel;
        private TextMeshProUGUI _streakValueLabel;
        private TextMeshProUGUI _comboBonusLabel;
        private TextMeshProUGUI _orderValueLabel;
        private TextMeshProUGUI _orderResultHeaderLabel;
        private TextMeshProUGUI _orderResultQualityLabel;
        private TextMeshProUGUI _orderResultScoreLabel;
        private TextMeshProUGUI _orderResultNextLabel;
        private TextMeshProUGUI _shiftResultRankLabel;
        private TextMeshProUGUI _shiftResultScoreLabel;
        private TextMeshProUGUI _shiftResultStatsLabel;
        private TextMeshProUGUI _shiftResultStreakLabel;

        private GameObject _startOverlayRoot;
        private GameObject _orderResultOverlayRoot;
        private GameObject _shiftResultOverlayRoot;
        private RectTransform _startOverlayCardRect;
        private RectTransform _orderResultCardRect;
        private RectTransform _shiftResultCardRect;
        private RectTransform _activeOverlayCardRect;
        private CanvasGroup _activeOverlayCanvasGroup;
        private float _overlayRevealElapsed;
        private CanvasGroup _fadingOverlayCanvasGroup;
        private float _overlayFadeOutElapsed;
        private float _fadingOverlayStartAlpha;

        private Texture2D _glowTexture;
        private Sprite _glowSprite;
        private float _animationClock;
        private float _impactElapsed;
        private float _sparksElapsed;
        private float _judgmentPopElapsed;
        private float _currentTimingProgress;
        private bool _impactAnimationActive;
        private bool _sparkAnimationActive;
        private bool _judgmentPopActive;
        private float _scoreFlashElapsed;
        private int _displayedScore;
        private int _scoreAnimationStart;
        private int _scoreAnimationTarget;
        private float _scoreAnimationElapsed;
        private float _impactRingElapsed;
        private float _feedbackFlashElapsed;
        private float _feedbackFlashDuration;
        private float _feedbackFlashStrength;
        private Color _feedbackFlashColor;
        private Color _impactRingColor;
        private float _impactRingMaxScale;
        private bool _impactRingActive;
        private TimingGrade _impactGrade;
        private int _sparkPhaseIndex;
        private Color _impactColor;
        private Color _sparkColor;
        private readonly Vector2 _workpieceBasePosition = new Vector2(0f, 88f);
        private readonly Vector2 _sparkOriginPosition = new Vector2(0f, 145f);

        private static Sprite _whiteSprite;
        private static Sprite _roundedPanelSprite;
        private static Sprite _impactRingSprite;
        private bool _hasSnapshot;

        private void Awake()
        {
            BuildInterface();
        }

        private void OnEnable()
        {
            FurnaceRhythmEventBus.SnapshotChanged += OnSnapshotChanged;
        }

        private void OnDisable()
        {
            FurnaceRhythmEventBus.SnapshotChanged -= OnSnapshotChanged;
        }

        private void Update()
        {
            float deltaTime = Time.unscaledDeltaTime;
            _animationClock += deltaTime;
            UpdatePresentationAnimation(deltaTime);
        }

        private void OnDestroy()
        {
            if (_glowSprite != null)
                Destroy(_glowSprite);
            if (_glowTexture != null)
                Destroy(_glowTexture);
        }

        /// <summary>Sets the art definition and timing windows before the view is enabled.</summary>
        /// <param name="tuning">Configuration supplying the billet sprite, or null to omit the art definition.</param>
        /// <param name="center">Normalized center of the target band.</param>
        /// <param name="perfect">Half-width of the Perfect band.</param>
        /// <param name="good">Half-width of the Good band.</param>
        /// <exception cref="System.ArgumentOutOfRangeException">A timing window is invalid.</exception>
        public void Configure(FurnaceRhythmTuning tuning, float center, float perfect, float good)
        {
            if (float.IsNaN(center) || float.IsInfinity(center) || center <= 0f || center >= 1f)
                throw new System.ArgumentOutOfRangeException(nameof(center));
            if (float.IsNaN(perfect) || float.IsInfinity(perfect) || perfect <= 0f)
                throw new System.ArgumentOutOfRangeException(nameof(perfect));
            if (float.IsNaN(good) || float.IsInfinity(good) || good <= 0f || perfect > good || center - good < 0f || center + good >= 1f)
                throw new System.ArgumentOutOfRangeException(nameof(good));

            _tuning = tuning;
            _hotBilletSprite = _tuning != null ? _tuning.HotBilletSprite : null;
            _targetCenter = center;
            _perfectWindow = perfect;
            _goodWindow = good;
        }

        private void OnSnapshotChanged(FurnaceRhythmSnapshot snapshot)
        {
            RoundState previousState = _state;
            int previousPhase = _currentPhaseIndex;
            int previousOrder = _currentOrderIndex;
            bool firstSnapshot = !_hasSnapshot;

            _state = MapState(snapshot.State);
            _currentPhaseIndex = Mathf.Clamp(snapshot.PhaseIndex, 0, _phaseCount - 1);
            _currentOrderIndex = Mathf.Clamp(snapshot.OrderIndex, 0, _orderCount - 1);
            _score = snapshot.Score;
            _orderScore = snapshot.OrderScore;
            _currentStreak = snapshot.CurrentStreak;
            _bestStreak = snapshot.BestStreak;
            _totalPerfect = snapshot.TotalPerfect;
            _totalGood = snapshot.TotalGood;
            _totalMiss = snapshot.TotalMiss;
            _currentTimingProgress = snapshot.Progress;
            _targetCenter = snapshot.TargetCenter;
            _perfectWindow = snapshot.PerfectWindow;
            _goodWindow = snapshot.GoodWindow;
            _orderGrades[0] = MapGrade(snapshot.PhaseGrade0);
            _orderGrades[1] = MapGrade(snapshot.PhaseGrade1);
            _orderGrades[2] = MapGrade(snapshot.PhaseGrade2);

            bool stateChanged = firstSnapshot || previousState != _state;
            bool phaseChanged = firstSnapshot || previousPhase != _currentPhaseIndex;
            bool orderChanged = firstSnapshot || previousOrder != _currentOrderIndex;

            if (_state == RoundState.Ready && stateChanged)
                ShowReadyState();
            else if (_state == RoundState.Playing && (stateChanged || phaseChanged || orderChanged))
                ShowTimingBeatState(snapshot, previousState);
            else if (_state == RoundState.Resolving && stateChanged)
                ShowBeatResult(snapshot);
            else if (_state == RoundState.OrderResult && stateChanged)
                ShowOrderResultState(snapshot);
            else if (_state == RoundState.ShiftResult && stateChanged)
                ShowShiftResultState(snapshot);

            if (_timingTrackRect != null)
            {
                float trackWidth = _timingTrackRect.rect.width;
                _markerRect.anchoredPosition = new Vector2((_currentTimingProgress - 0.5f) * trackWidth, 0f);
                _markerTrailRect.anchoredPosition = _markerRect.anchoredPosition;
                if (_state == RoundState.Playing)
                    UpdateTimingCue(_currentTimingProgress);
            }

            UpdateRunStats();
            _hasSnapshot = true;
        }

        private void ShowReadyState()
        {
            _state = RoundState.Ready;
            _currentPhaseIndex = 0;
            _phaseLabel.text = "READY  ·  FIVE-ORDER SHIFT";
            _judgmentLabel.text = "FORGE FIVE WORKPIECES";
            _judgmentLabel.color = FurnaceRhythmPalette.IndustrialCream;
            _instructionLabel.text = "PRESS SPACE OR CLICK TO START";
            _markerRect.anchoredPosition = new Vector2(-_timingTrackRect.rect.width * 0.5f, 0f);
            _markerTrailRect.anchoredPosition = _markerRect.anchoredPosition;
            _markerImage.color = FurnaceRhythmPalette.IndustrialCream;
            UpdateRunStats();
            RefreshPhaseIndicators();
            ShowOverlay(_startOverlayRoot, _startOverlayCardRect);
        }

        private void ShowTimingBeatState(FurnaceRhythmSnapshot snapshot, RoundState previousState)
        {
            if (previousState == RoundState.Ready || previousState == RoundState.ShiftResult)
            {
                _displayedScore = 0;
                _scoreAnimationStart = 0;
                _scoreAnimationTarget = 0;
                _scoreAnimationElapsed = _scoreCountUpSeconds;
                _scoreFlashElapsed = 0f;
                _feedbackFlashElapsed = 0f;
                _impactRingActive = false;
                _impactAnimationActive = false;
                _sparkAnimationActive = false;
                _judgmentPopActive = false;
                if (_scoreValueLabel != null)
                {
                    _scoreValueLabel.text = "00000";
                    _scoreValueLabel.rectTransform.localScale = Vector3.one;
                }
            }

            HideOverlay(_startOverlayRoot);
            HideOverlay(_orderResultOverlayRoot);
            HideOverlay(_shiftResultOverlayRoot);
            _phaseLabel.text = _phaseNames[_currentPhaseIndex] + "  ·  BEAT " + (_currentPhaseIndex + 1) + " / " + _phaseCount;
            _judgmentLabel.text = "WAIT FOR THE CYAN ZONE";
            _judgmentLabel.color = FurnaceRhythmPalette.IndustrialCream;
            _instructionLabel.text = "PRESS SPACE OR CLICK WHEN THE MARKER ENTERS THE CYAN BAND";
            _workpieceImage.color = Color.white;
            _markerImage.color = FurnaceRhythmPalette.IndustrialCream;
            _markerRect.anchoredPosition = new Vector2((snapshot.Progress - 0.5f) * _timingTrackRect.rect.width, 0f);
            _markerTrailRect.anchoredPosition = _markerRect.anchoredPosition;
            UpdateTimingCue(snapshot.Progress);
            UpdateRunStats();
            RefreshPhaseIndicators();
        }

        private void ShowBeatResult(FurnaceRhythmSnapshot snapshot)
        {
            TimingGrade grade = MapGrade(snapshot.LastGrade);
            if (snapshot.AwardedPoints > 0)
                _scoreFlashElapsed = 0.3f;
            TriggerScreenFlash(grade);
            _judgmentLabel.text = snapshot.LastMessage + "  +" + snapshot.AwardedPoints;
            _judgmentLabel.color = GradeColor(grade);
            _markerImage.color = GradeColor(grade);
            _impactGrade = grade;
            _impactColor = GradeColor(grade);
            _impactElapsed = 0f;
            _impactAnimationActive = true;
            _judgmentPopElapsed = 0f;
            _judgmentPopActive = true;
            if (grade != TimingGrade.Miss)
            {
                TriggerSparks(CurrentPhaseAccent());
                TriggerImpactRing(grade);
            }
            RefreshPhaseIndicators();
        }

        private void ShowOrderResultState(FurnaceRhythmSnapshot snapshot)
        {
            _state = RoundState.OrderResult;
            string quality = snapshot.OrderQuality;
            int perfect = snapshot.OrderPerfect;
            int good = snapshot.OrderGood;
            int miss = snapshot.OrderMiss;
            _phaseLabel.text = "WORKPIECE " + (_currentOrderIndex + 1) + " / " + _orderCount + " COMPLETE";
            _judgmentLabel.text = quality + "  ·  " + perfect + " PERFECT  /  " + good + " GOOD  /  " + miss + " MISS";
            _judgmentLabel.color = miss > 0 ? FurnaceRhythmPalette.FactoryCoral : perfect == _phaseCount ? FurnaceRhythmPalette.ElectricCyan : FurnaceRhythmPalette.SafetyOrange;
            _instructionLabel.text = _currentOrderIndex + 1 < _orderCount ? "NEXT WORKPIECE COMING UP" : "SHIFT SUMMARY NEXT";
            _judgmentPopElapsed = 0f;
            _judgmentPopActive = true;
            RefreshPhaseIndicators();
            ShowOrderResultOverlay(quality, perfect, good, miss);
        }

        private void ShowShiftResultState(FurnaceRhythmSnapshot snapshot)
        {
            _state = RoundState.ShiftResult;
            string rank = snapshot.ShiftRank;
            _phaseLabel.text = "SHIFT COMPLETE";
            _judgmentLabel.text = rank + "  ·  " + _score.ToString("D5") + " POINTS";
            _judgmentLabel.color = _totalMiss == 0 ? FurnaceRhythmPalette.ElectricCyan : _totalMiss <= 2 ? FurnaceRhythmPalette.SafetyOrange : FurnaceRhythmPalette.FactoryCoral;
            _instructionLabel.text = "PRESS SPACE OR CLICK TO START A NEW SHIFT";
            _shiftResultRankLabel.text = rank;
            _shiftResultRankLabel.color = _judgmentLabel.color;
            _shiftResultScoreLabel.text = _score.ToString("D5") + "  POINTS";
            _shiftResultStatsLabel.text = "PERFECT  " + _totalPerfect.ToString("D2") + "     GOOD  " + _totalGood.ToString("D2") +
                                         "     MISS  " + _totalMiss.ToString("D2");
            _shiftResultStreakLabel.text = "BEST STREAK  " + _bestStreak.ToString("D2") + "  ·  FIVE WORKPIECES FORGED";
            _judgmentPopElapsed = 0f;
            _judgmentPopActive = true;
            RefreshPhaseIndicators();
            ShowOverlay(_shiftResultOverlayRoot, _shiftResultCardRect);
        }

        private static RoundState MapState(FurnaceRhythmRunState _state)
        {
            switch (_state)
            {
                case FurnaceRhythmRunState.Playing: return RoundState.Playing;
                case FurnaceRhythmRunState.Resolving: return RoundState.Resolving;
                case FurnaceRhythmRunState.OrderResult: return RoundState.OrderResult;
                case FurnaceRhythmRunState.ShiftResult: return RoundState.ShiftResult;
                default: return RoundState.Ready;
            }
        }

        private static TimingGrade MapGrade(FurnaceRhythmGrade grade)
        {
            switch (grade)
            {
                case FurnaceRhythmGrade.Perfect: return TimingGrade.Perfect;
                case FurnaceRhythmGrade.Good: return TimingGrade.Good;
                default: return TimingGrade.Miss;
            }
        }
        private void UpdateRunStats()
        {
            if (_scoreValueLabel == null)
                return;

            if (_score != _scoreAnimationTarget)
            {
                _scoreAnimationStart = _displayedScore;
                _scoreAnimationTarget = _score;
                _scoreAnimationElapsed = 0f;
            }
            _streakValueLabel.text = _currentStreak.ToString("D2");
            _comboBonusLabel.text = "NEXT HIT  +" + (_currentStreak == 0 ? 0 : Mathf.Min(_currentStreak, 5) * 10);
            _orderValueLabel.text = (_currentOrderIndex + 1).ToString("D2") + " / " + _orderCount.ToString("D2");

            int filledBonusPips = Mathf.Min(_currentStreak, _comboBonusPips.Length);
            for (int i = 0; i < _comboBonusPips.Length; i++)
                _comboBonusPips[i].color = i < filledBonusPips
                    ? FurnaceRhythmPalette.SafetyOrange
                    : WithAlpha(FurnaceRhythmPalette.SlateMachinery, 0.86f);

            for (int i = 0; i < _orderProgressSegments.Length; i++)
            {
                if (i < _currentOrderIndex || ((_state == RoundState.OrderResult || _state == RoundState.ShiftResult) && i == _currentOrderIndex))
                    _orderProgressSegments[i].color = FurnaceRhythmPalette.OxidizedTeal;
                else if (i == _currentOrderIndex)
                    _orderProgressSegments[i].color = FurnaceRhythmPalette.ElectricCyan;
                else
                    _orderProgressSegments[i].color = WithAlpha(FurnaceRhythmPalette.SlateMachinery, 0.82f);
            }
        }

        private void ShowOrderResultOverlay(string quality, int perfect, int good, int miss)
        {
            _orderResultHeaderLabel.text = "WORKPIECE  " + (_currentOrderIndex + 1).ToString("D2") + "  /  " + _orderCount.ToString("D2");
            _orderResultQualityLabel.text = quality;
            _orderResultQualityLabel.color = miss > 0 ? FurnaceRhythmPalette.FactoryCoral : perfect == _phaseCount ? FurnaceRhythmPalette.ElectricCyan : FurnaceRhythmPalette.SafetyOrange;
            _orderResultScoreLabel.text = "+" + _orderScore.ToString("D4") + "  POINTS EARNED";
            _orderResultNextLabel.text = _currentOrderIndex + 1 < _orderCount ? "NEXT WORKPIECE COMING UP" : "SHIFT SUMMARY NEXT";

            for (int i = 0; i < _phaseCount; i++)
            {
                TimingGrade grade = _orderGrades[i];
                _orderGradeChipImages[i].color = WithAlpha(GradeColor(grade), 0.22f);
                _orderGradeChipValueLabels[i].text = GradeName(grade);
                _orderGradeChipValueLabels[i].color = GradeColor(grade);
            }

            ShowOverlay(_orderResultOverlayRoot, _orderResultCardRect);
        }

        private void UpdatePresentationAnimation(float deltaTime)
        {
            float pulse = (Mathf.Sin(_animationClock * 3.6f) + 1f) * 0.5f;

            if (_scoreAnimationElapsed < _scoreCountUpSeconds)
            {
                _scoreAnimationElapsed += deltaTime;
                float countProgress = Mathf.Clamp01(_scoreAnimationElapsed / _scoreCountUpSeconds);
                float countEase = 1f - Mathf.Pow(1f - countProgress, 3f);
                _displayedScore = Mathf.RoundToInt(Mathf.Lerp(_scoreAnimationStart, _scoreAnimationTarget, countEase));
            }
            else
            {
                _displayedScore = _scoreAnimationTarget;
            }

            if (_scoreValueLabel != null)
                _scoreValueLabel.text = _displayedScore.ToString("D5");

            if (_scoreFlashElapsed > 0f && _scoreValueLabel != null)
            {
                _scoreFlashElapsed = Mathf.Max(0f, _scoreFlashElapsed - deltaTime);
                float flashProgress = 1f - _scoreFlashElapsed / 0.3f;
                float scale = 1f + Mathf.Sin(flashProgress * Mathf.PI) * 0.12f;
                _scoreValueLabel.rectTransform.localScale = Vector3.one * scale;
            }
            else if (_scoreValueLabel != null)
            {
                _scoreValueLabel.rectTransform.localScale = Vector3.one;
            }

            if (_activeOverlayCanvasGroup != null)
            {
                _overlayRevealElapsed += deltaTime;
                float revealProgress = Mathf.Clamp01(_overlayRevealElapsed / _overlayRevealSeconds);
                _activeOverlayCanvasGroup.alpha = Mathf.SmoothStep(0f, 1f, revealProgress);
                if (_activeOverlayCardRect != null)
                {
                    float outBack = 1f + 2.70158f * Mathf.Pow(revealProgress - 1f, 3f) + 1.70158f * Mathf.Pow(revealProgress - 1f, 2f);
                    _activeOverlayCardRect.localScale = Vector3.one * Mathf.LerpUnclamped(0.9f, 1f, outBack);
                }

                if (revealProgress >= 1f)
                {
                    _activeOverlayCanvasGroup = null;
                    _activeOverlayCardRect = null;
                }
            }
            float impactEnvelope = 0f;
            float impactProgress = 1f;

            if (_impactAnimationActive)
            {
                _impactElapsed += deltaTime;
                impactProgress = Mathf.Clamp01(_impactElapsed / _impactDurationSeconds);
                impactEnvelope = Mathf.Sin(impactProgress * Mathf.PI);
                if (impactProgress >= 1f)
                    _impactAnimationActive = false;
            }

            float judgmentPop = 0f;
            if (_judgmentPopActive)
            {
                _judgmentPopElapsed += deltaTime;
                float popProgress = Mathf.Clamp01(_judgmentPopElapsed / 0.24f);
                judgmentPop = Mathf.Sin(popProgress * Mathf.PI) * 0.16f;
                if (popProgress >= 1f)
                    _judgmentPopActive = false;
            }
            _judgmentRect.localScale = Vector3.one * (1f + judgmentPop);

            float idleBob = Mathf.Sin(_animationClock * 1.65f) * 2.2f;
            float scalePulse = Mathf.Sin(_animationClock * 1.65f) * 0.006f;
            float squash = 0f;
            float impactLift = 0f;
            float impactRotation = 0f;

            if (impactEnvelope > 0f)
            {
                if (_impactGrade == TimingGrade.Miss)
                {
                    impactRotation = Mathf.Sin(_impactElapsed * 68f) * 3.5f * (1f - impactProgress);
                    squash = -0.018f * impactEnvelope;
                }
                else
                {
                    float accuracyStrength = _impactGrade == TimingGrade.Perfect ? 0.105f : 0.065f;
                    float phaseStrength = _currentPhaseIndex == 1 ? 1.25f : 0.7f;
                    squash = accuracyStrength * phaseStrength * impactEnvelope;
                    impactLift = _currentPhaseIndex == 1 ? 5f * impactEnvelope : 2.5f * impactEnvelope;
                    impactRotation = Mathf.Sin(impactProgress * Mathf.PI) * 1.2f;
                }

                _workpieceImage.color = Color.Lerp(Color.white, _impactColor, impactEnvelope * 0.16f);
            }
            else
            {
                _workpieceImage.color = Color.white;
            }

            _workpieceRect.anchoredPosition = _workpieceBasePosition + new Vector2(0f, idleBob + impactLift);
            _workpieceRect.localScale = new Vector3(1f + scalePulse + squash, 1f + scalePulse - squash * 0.7f, 1f);
            _workpieceRect.localRotation = Quaternion.Euler(0f, 0f, impactRotation);

            Color glowColor = CurrentPhaseAccent();
            float glowAlpha;
            if (_state == RoundState.Playing || _state == RoundState.Resolving)
                glowAlpha = 0.065f + pulse * (_currentPhaseIndex == 0 ? 0.12f : 0.07f);
            else if (_state == RoundState.Ready)
                glowAlpha = 0.055f + pulse * 0.035f;
            else
                glowAlpha = 0.045f;

            glowAlpha += impactEnvelope * 0.1f;
            _glowImage.color = WithAlpha(glowColor, glowAlpha);
            float glowScale = 0.96f + pulse * 0.055f + impactEnvelope * 0.07f;
            _glowRect.localScale = new Vector3(glowScale, glowScale, 1f);

            if (_state == RoundState.Playing)
                UpdateTimingCue(_currentTimingProgress);
            else
            {
                _targetBandImage.color = WithAlpha(FurnaceRhythmPalette.ElectricCyan, 0.52f + pulse * 0.1f);
                _perfectBandImage.color = WithAlpha(FurnaceRhythmPalette.IndustrialCream, 0.78f + pulse * 0.12f);
                _markerTrailImage.color = WithAlpha(FurnaceRhythmPalette.ElectricCyan, 0.08f + pulse * 0.05f);
                _markerTrailRect.localScale = Vector3.one;
            }

            for (int i = 0; i < _phaseIndicatorLabels.Length; i++)
                _phaseIndicatorLabels[i].rectTransform.localScale = Vector3.one;

            if (_state == RoundState.Playing && _currentOrderIndex < _orderProgressSegments.Length)
            {
                _orderProgressSegments[_currentOrderIndex].color = WithAlpha(FurnaceRhythmPalette.ElectricCyan, 0.7f + pulse * 0.28f);
                _phaseIndicators[_currentPhaseIndex].color = WithAlpha(CurrentPhaseAccent(), 0.72f + pulse * 0.2f);
                _phaseIndicatorLabels[_currentPhaseIndex].rectTransform.localScale = Vector3.one * (1f + pulse * 0.035f);
            }

            for (int i = 0; i < _comboBonusPips.Length; i++)
            {
                if (i < Mathf.Min(_currentStreak, _comboBonusPips.Length))
                    _comboBonusPips[i].color = WithAlpha(FurnaceRhythmPalette.SafetyOrange, 0.76f + pulse * 0.24f);
            }

            if (_feedbackFlashElapsed > 0f && _feedbackFlashImage != null)
            {
                _feedbackFlashElapsed = Mathf.Max(0f, _feedbackFlashElapsed - deltaTime);
                float flashProgress = 1f - _feedbackFlashElapsed / _feedbackFlashDuration;
                float alpha = (1f - flashProgress) * (1f - flashProgress) * _feedbackFlashStrength;
                _feedbackFlashImage.color = WithAlpha(_feedbackFlashColor, alpha);
            }
            else if (_feedbackFlashImage != null)
            {
                _feedbackFlashImage.color = WithAlpha(_feedbackFlashColor, 0f);
            }

            if (_impactRingActive && _impactRingImage != null)
            {
                _impactRingElapsed += deltaTime;
                float ringProgress = Mathf.Clamp01(_impactRingElapsed / _impactRingDurationSeconds);
                float ringEase = 1f - Mathf.Pow(1f - ringProgress, 3f);
                float ringScale = Mathf.Lerp(0.42f, _impactRingMaxScale, ringEase);
                _impactRingRect.localScale = new Vector3(ringScale, ringScale, 1f);
                _impactRingImage.color = WithAlpha(_impactRingColor, (1f - ringProgress) * 0.82f);
                if (ringProgress >= 1f)
                    _impactRingActive = false;
            }

            if (_fadingOverlayCanvasGroup != null)
            {
                _overlayFadeOutElapsed += deltaTime;
                float fadeProgress = Mathf.Clamp01(_overlayFadeOutElapsed / _overlayFadeOutSeconds);
                _fadingOverlayCanvasGroup.alpha = _fadingOverlayStartAlpha * (1f - Mathf.SmoothStep(0f, 1f, fadeProgress));
                if (fadeProgress >= 1f)
                {
                    _fadingOverlayCanvasGroup.gameObject.SetActive(false);
                    _fadingOverlayCanvasGroup = null;
                }
            }

            if (_sparkAnimationActive)
                UpdateSparks(deltaTime);
        }

        private void TriggerScreenFlash(TimingGrade grade)
        {
            _feedbackFlashColor = GradeColor(grade);
            _feedbackFlashDuration = grade == TimingGrade.Perfect ? 0.14f : grade == TimingGrade.Good ? 0.1f : 0.08f;
            _feedbackFlashStrength = grade == TimingGrade.Perfect ? 0.2f : grade == TimingGrade.Good ? 0.13f : 0.1f;
            _feedbackFlashElapsed = _feedbackFlashDuration;
        }

        private void TriggerImpactRing(TimingGrade grade)
        {
            _impactRingColor = GradeColor(grade);
            _impactRingMaxScale = grade == TimingGrade.Perfect ? 1.75f : 1.38f;
            _impactRingElapsed = 0f;
            _impactRingActive = true;
            _impactRingRect.localScale = Vector3.one * 0.42f;
            _impactRingImage.color = WithAlpha(_impactRingColor, 0.82f);
        }

        private void TriggerSparks(Color color)
        {
            _sparkColor = color;
            _sparkPhaseIndex = _currentPhaseIndex;
            _sparksElapsed = 0f;
            _sparkAnimationActive = true;
            for (int i = 0; i < _sparkImages.Length; i++)
            {
                if (_sparkPhaseIndex == 0)
                {
                    float spread = ((i % 4) - 1.5f) * 0.16f;
                    _sparkDirections[i] = new Vector2(spread, 1f).normalized;
                }
                else if (_sparkPhaseIndex == 2)
                {
                    float spread = Mathf.Sin(i * 1.7f) * 0.7f;
                    _sparkDirections[i] = new Vector2(spread, 1f).normalized;
                }
                else
                {
                    float angle = Mathf.PI * 2f * i / _sparkImages.Length;
                    _sparkDirections[i] = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                }

                _sparkImages[i].color = WithAlpha(_sparkColor, 0.95f);
                _sparkRects[i].anchoredPosition = _sparkOriginPosition + _sparkDirections[i] * 18f;
                _sparkRects[i].localScale = Vector3.one;
            }
        }

        private void UpdateSparks(float deltaTime)
        {
            _sparksElapsed += deltaTime;
            float progress = Mathf.Clamp01(_sparksElapsed / _sparkDurationSeconds);
            float distance = _sparkPhaseIndex == 2 ? Mathf.Lerp(8f, 50f, progress) : Mathf.Lerp(18f, 98f, progress);
            float alpha = (1f - progress) * (1f - progress * 0.2f);
            float scale = _sparkPhaseIndex == 2 ? 0.75f + progress * 0.6f : 1f - progress * 0.45f;

            for (int i = 0; i < _sparkImages.Length; i++)
            {
                Vector2 direction = _sparkDirections[i];
                Vector2 offset = direction * distance;
                if (_sparkPhaseIndex == 2)
                    offset += Vector2.up * (34f * progress);
                _sparkRects[i].anchoredPosition = _sparkOriginPosition + offset;
                _sparkRects[i].localScale = new Vector3(scale, scale, 1f);
                float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f;
                _sparkRects[i].localRotation = Quaternion.Euler(0f, 0f, angle);
                _sparkImages[i].color = WithAlpha(_sparkColor, alpha * 0.95f);
            }

            if (progress >= 1f)
                _sparkAnimationActive = false;
        }

        private Color CurrentPhaseAccent()
        {
            return CurrentPhaseAccent(_currentPhaseIndex);
        }

        private static Color CurrentPhaseAccent(int phaseIndex)
        {
            switch (phaseIndex)
            {
                case 0: return FurnaceRhythmPalette.SafetyOrange;
                case 1: return FurnaceRhythmPalette.IndustrialCream;
                default: return FurnaceRhythmPalette.ElectricCyan;
            }
        }

        private void UpdateTimingCue(float progress)
        {
            float distance = Mathf.Abs(progress - _targetCenter);
            float anticipation = 1f - Mathf.Clamp01(distance / (_goodWindow * 2.6f));
            float pulse = (Mathf.Sin(_animationClock * 8f) + 1f) * 0.5f;
            _targetBandImage.color = WithAlpha(FurnaceRhythmPalette.ElectricCyan, 0.48f + anticipation * 0.2f + pulse * 0.1f);
            _perfectBandImage.color = WithAlpha(FurnaceRhythmPalette.IndustrialCream, 0.7f + anticipation * 0.22f + pulse * 0.08f);
            _markerTrailImage.color = WithAlpha(CurrentPhaseAccent(), 0.12f + anticipation * 0.2f);
            _markerTrailRect.localScale = new Vector3(0.85f + anticipation * 0.32f, 1f, 1f);

            if (distance <= _goodWindow)
                _markerImage.color = FurnaceRhythmPalette.SafetyOrange;
            else if (distance <= _goodWindow * 2f)
                _markerImage.color = FurnaceRhythmPalette.ElectricCyan;
            else
                _markerImage.color = FurnaceRhythmPalette.IndustrialCream;
        }

        private void RefreshPhaseIndicators()
        {
            for (int i = 0; i < _phaseIndicators.Length; i++)
            {
                if (_state == RoundState.OrderResult || _state == RoundState.ShiftResult ||
                    i < _currentPhaseIndex || (_state == RoundState.Resolving && i == _currentPhaseIndex))
                {
                    _phaseIndicators[i].color = WithAlpha(GradeColor(_orderGrades[i]), 0.78f);
                    _phaseIndicatorLabels[i].color = FurnaceRhythmPalette.IndustrialCream;
                }
                else if (i == _currentPhaseIndex && _state == RoundState.Playing)
                {
                    _phaseIndicators[i].color = WithAlpha(CurrentPhaseAccent(), 0.92f);
                    _phaseIndicatorLabels[i].color = FurnaceRhythmPalette.MidnightIndustrial;
                }
                else
                {
                    _phaseIndicators[i].color = WithAlpha(FurnaceRhythmPalette.SlateMachinery, 0.9f);
                    _phaseIndicatorLabels[i].color = FurnaceRhythmPalette.IndustrialCream;
                }
            }
        }

        private void BuildInterface()
        {
            Canvas canvas = CreateCanvas();
            UnityEngine.UI.Image background = CreateImage(canvas.transform, "Midnight Background", FurnaceRhythmPalette.MidnightIndustrial);
            StretchToParent(background.rectTransform);

            UnityEngine.UI.Image topRule = CreateImage(canvas.transform, "Top Cyan Rule", FurnaceRhythmPalette.ElectricCyan);
            SetCenteredRect(topRule.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -2f), new Vector2(1920f, 4f));
            UnityEngine.UI.Image bottomRule = CreateImage(canvas.transform, "Bottom Teal Rule", FurnaceRhythmPalette.OxidizedTeal);
            SetCenteredRect(bottomRule.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 2f), new Vector2(1920f, 4f));

            CreateText(canvas.transform, "Brand", "FURNACE RHYTHM  ·  FIVE-ORDER SHIFT", 18f, FurnaceRhythmPalette.ElectricCyan,
                new Vector2(0.5f, 1f), new Vector2(0f, -38f), new Vector2(900f, 34f), FontStyles.Bold);
            CreateText(canvas.transform, "Game Title", "FURNACE RHYTHM", 48f, FurnaceRhythmPalette.IndustrialCream,
                new Vector2(0.5f, 1f), new Vector2(0f, -90f), new Vector2(1000f, 62f), FontStyles.Bold);
            CreateText(canvas.transform, "Subtitle", "FIVE ORDERS  ·  THREE BEATS EACH", 17f, FurnaceRhythmPalette.OxidizedTeal,
                new Vector2(0.5f, 1f), new Vector2(0f, -137f), new Vector2(900f, 34f), FontStyles.Bold);

            _glowImage = CreateImage(canvas.transform, "Forge Glow", WithAlpha(FurnaceRhythmPalette.SafetyOrange, 0.08f));
            _glowSprite = CreateSoftGlowSprite(out _glowTexture);
            _glowImage.sprite = _glowSprite;
            _glowImage.preserveAspect = true;
            _glowRect = _glowImage.rectTransform;
            SetCenteredRect(_glowRect, new Vector2(0.5f, 0.5f), _workpieceBasePosition, new Vector2(780f, 520f));

            _impactRingImage = CreateImage(canvas.transform, "Judgment Impact Ring", WithAlpha(FurnaceRhythmPalette.ElectricCyan, 0f));
            _impactRingImage.sprite = GetImpactRingSprite();
            _impactRingImage.preserveAspect = true;
            _impactRingRect = _impactRingImage.rectTransform;
            SetCenteredRect(_impactRingRect, new Vector2(0.5f, 0.5f), _sparkOriginPosition, new Vector2(190f, 190f));

            _workpieceImage = CreateImage(canvas.transform, "Hot Billet Workpiece", Color.white);
            _workpieceImage.sprite = _hotBilletSprite;
            _workpieceImage.preserveAspect = true;
            _workpieceImage.color = Color.white;
            SetCenteredRect(_workpieceImage.rectTransform, new Vector2(0.5f, 0.5f), _workpieceBasePosition, new Vector2(650f, 432f));
            _workpieceRect = _workpieceImage.rectTransform;
            if (_hotBilletSprite == null)
                CreateText(canvas.transform, "Workpiece Fallback", "HOT BILLET", 22f, FurnaceRhythmPalette.SafetyOrange,
                    new Vector2(0.5f, 0.5f), new Vector2(0f, 34f), new Vector2(400f, 44f), FontStyles.Bold);

            for (int i = 0; i < _sparkImages.Length; i++)
            {
                float angle = Mathf.PI * 2f * i / _sparkImages.Length;
                _sparkDirections[i] = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                _sparkImages[i] = CreateImage(canvas.transform, "Forge Spark " + (i + 1), WithAlpha(FurnaceRhythmPalette.IndustrialCream, 0f));
                _sparkRects[i] = _sparkImages[i].rectTransform;
                SetCenteredRect(_sparkRects[i], new Vector2(0.5f, 0.5f), _sparkOriginPosition, i % 2 == 0 ? new Vector2(7f, 18f) : new Vector2(5f, 13f));
                _sparkRects[i].localRotation = Quaternion.Euler(0f, 0f, angle * Mathf.Rad2Deg - 90f);
            }

            BuildScoreHud(canvas.transform);
            BuildOrderProgress(canvas.transform);

            _phaseLabel = CreateText(canvas.transform, "Current Phase", string.Empty, 24f, FurnaceRhythmPalette.IndustrialCream,
                new Vector2(0.5f, 0.5f), new Vector2(0f, -194f), new Vector2(980f, 42f), FontStyles.Bold);
            BuildTimingTrack(canvas.transform);
            _judgmentLabel = CreateText(canvas.transform, "Timing Judgment", string.Empty, 30f, FurnaceRhythmPalette.IndustrialCream,
                new Vector2(0.5f, 0.5f), new Vector2(0f, -327f), new Vector2(1320f, 52f), FontStyles.Bold);
            _judgmentRect = _judgmentLabel.rectTransform;

            for (int i = 0; i < _phaseIndicators.Length; i++)
            {
                _phaseIndicators[i] = CreateImage(canvas.transform, "Beat Indicator " + (i + 1), WithAlpha(FurnaceRhythmPalette.SlateMachinery, 0.9f));
                _phaseIndicators[i].sprite = GetRoundedPanelSprite();
                _phaseIndicators[i].type = UnityEngine.UI.Image.Type.Sliced;
                SetCenteredRect(_phaseIndicators[i].rectTransform, new Vector2(0.5f, 0.5f), new Vector2((i - 1) * 154f, -389f), new Vector2(136f, 34f));
                BuildPhaseGlyph(_phaseIndicators[i].transform, i);
                _phaseIndicatorLabels[i] = CreateText(_phaseIndicators[i].transform, "Phase Name", _phaseNames[i], 13f,
                    FurnaceRhythmPalette.IndustrialCream, new Vector2(0.5f, 0.5f), new Vector2(13f, 0f), new Vector2(96f, 22f), FontStyles.Bold);
            }

            _instructionLabel = CreateText(canvas.transform, "Input Instruction", string.Empty, 16f, FurnaceRhythmPalette.IndustrialCream,
                new Vector2(0.5f, 0f), new Vector2(0f, 52f), new Vector2(1500f, 32f), FontStyles.Bold);
            CreateText(canvas.transform, "Input Hint", "SPACE / CLICK  ·  PERFECT +100  ·  GOOD +60  ·  COMBO BONUS CAPS AT +50", 12f, FurnaceRhythmPalette.OxidizedTeal,
                new Vector2(0.5f, 0f), new Vector2(0f, 24f), new Vector2(700f, 24f), FontStyles.Normal);

            _feedbackFlashImage = CreateImage(canvas.transform, "Judgment Screen Flash", WithAlpha(FurnaceRhythmPalette.IndustrialCream, 0f));
            StretchToParent(_feedbackFlashImage.rectTransform);

            BuildStartOverlay(canvas.transform);
            BuildOrderResultOverlay(canvas.transform);
            BuildShiftResultOverlay(canvas.transform);
        }

        private void BuildScoreHud(Transform parent)
        {
            TextMeshProUGUI unusedDetail;
            BuildScoreCard(parent, "Score Card", "SHIFT SCORE", FurnaceRhythmPalette.ElectricCyan, FurnaceRhythmPalette.ElectricCyan, -370f,
                out _scoreValueLabel, out unusedDetail);
            BuildScoreCard(parent, "Combo Card", "CURRENT STREAK", FurnaceRhythmPalette.SafetyOrange, FurnaceRhythmPalette.IndustrialCream, 0f,
                out _streakValueLabel, out _comboBonusLabel);
            BuildScoreCard(parent, "Order Card", "WORKPIECE", FurnaceRhythmPalette.OxidizedTeal, FurnaceRhythmPalette.IndustrialCream, 370f,
                out _orderValueLabel, out unusedDetail);
        }

        private void BuildScoreCard(
            Transform parent,
            string name,
            string caption,
            Color accent,
            Color valueColor,
            float horizontalPosition,
            out TextMeshProUGUI valueLabel,
            out TextMeshProUGUI detailLabel)
        {
            RectTransform card = CreateRoundedPanel(parent, name, new Vector2(0.5f, 1f),
                new Vector2(horizontalPosition, -204f), new Vector2(330f, 76f), accent);
            CreateText(card, name + " Caption", caption, 11f, FurnaceRhythmPalette.OxidizedTeal,
                new Vector2(0.5f, 0.5f), new Vector2(0f, 19f), new Vector2(286f, 18f), FontStyles.Bold);
            valueLabel = CreateText(card, name + " Value", "00000", 29f, valueColor,
                new Vector2(0.5f, 0.5f), new Vector2(0f, -9f), new Vector2(286f, 36f), FontStyles.Bold);
            detailLabel = null;
            if (name == "Combo Card")
            {
                valueLabel.rectTransform.anchoredPosition = new Vector2(-56f, -8f);
                valueLabel.rectTransform.sizeDelta = new Vector2(140f, 36f);
                detailLabel = CreateText(card, name + " Bonus", "NEXT HIT  +0", 11f, FurnaceRhythmPalette.SafetyOrange,
                    new Vector2(0.5f, 0.5f), new Vector2(77f, -8f), new Vector2(128f, 22f), FontStyles.Bold);
                for (int i = 0; i < _comboBonusPips.Length; i++)
                {
                    _comboBonusPips[i] = CreateImage(card, "Combo Bonus Pip " + (i + 1),
                        WithAlpha(FurnaceRhythmPalette.SlateMachinery, 0.86f));
                    _comboBonusPips[i].sprite = GetRoundedPanelSprite();
                    _comboBonusPips[i].type = UnityEngine.UI.Image.Type.Sliced;
                    SetCenteredRect(_comboBonusPips[i].rectTransform, new Vector2(0.5f, 0.5f),
                        new Vector2(-53f + i * 22f, -29f), new Vector2(16f, 5f));
                }
            }
        }

        private void BuildOrderProgress(Transform parent)
        {
            CreateText(parent, "Shift Progress Label", "SHIFT PROGRESS", 10f, FurnaceRhythmPalette.OxidizedTeal,
                new Vector2(0.5f, 1f), new Vector2(0f, -267f), new Vector2(320f, 16f), FontStyles.Bold);

            for (int i = 0; i < _orderProgressSegments.Length; i++)
            {
                _orderProgressSegments[i] = CreateImage(parent, "Workpiece Progress " + (i + 1),
                    WithAlpha(FurnaceRhythmPalette.SlateMachinery, 0.82f));
                _orderProgressSegments[i].sprite = GetRoundedPanelSprite();
                _orderProgressSegments[i].type = UnityEngine.UI.Image.Type.Sliced;
                SetCenteredRect(_orderProgressSegments[i].rectTransform, new Vector2(0.5f, 1f),
                    new Vector2((i - (_orderCount - 1) * 0.5f) * 134f, -291f), new Vector2(118f, 10f));
            }
        }

        private void BuildPhaseGlyph(Transform parent, int phaseIndex)
        {
            Color glyphColor = CurrentPhaseAccent(phaseIndex);
            if (phaseIndex == 0)
            {
                for (int i = 0; i < 3; i++)
                {
                    UnityEngine.UI.Image flame = CreateImage(parent, "Heat Glyph Spark " + i, glyphColor);
                    SetCenteredRect(flame.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(-47f + i * 7f, 0f),
                        new Vector2(4f, i == 1 ? 18f : 13f));
                    flame.rectTransform.localRotation = Quaternion.Euler(0f, 0f, (i - 1) * -16f);
                }
            }
            else if (phaseIndex == 1)
            {
                UnityEngine.UI.Image hammerHead = CreateImage(parent, "Hammer Glyph Head", glyphColor);
                SetCenteredRect(hammerHead.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(-45f, 5f), new Vector2(20f, 6f));
                UnityEngine.UI.Image hammerHandle = CreateImage(parent, "Hammer Glyph Handle", glyphColor);
                SetCenteredRect(hammerHandle.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(-42f, -3f), new Vector2(5f, 15f));
                hammerHandle.rectTransform.localRotation = Quaternion.Euler(0f, 0f, -18f);
            }
            else
            {
                UnityEngine.UI.Image waterDrop = CreateImage(parent, "Quench Glyph Drop", glyphColor);
                SetCenteredRect(waterDrop.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(-47f, 3f), new Vector2(11f, 11f));
                waterDrop.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 45f);
                for (int i = 0; i < 2; i++)
                {
                    UnityEngine.UI.Image steamLine = CreateImage(parent, "Quench Glyph Steam " + i, FurnaceRhythmPalette.OxidizedTeal);
                    SetCenteredRect(steamLine.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(-51f + i * 8f, -7f),
                        new Vector2(3f, 7f));
                    steamLine.rectTransform.localRotation = Quaternion.Euler(0f, 0f, (i == 0 ? -1f : 1f) * 15f);
                }
            }
        }

        private void BuildStartOverlay(Transform parent)
        {
            _startOverlayRoot = CreateOverlayRoot(parent, "Shift Start Overlay", 0.34f);
            _startOverlayCardRect = CreateRoundedPanel(_startOverlayRoot.transform, "Shift Start Card", new Vector2(0.5f, 0.5f),
                new Vector2(0f, -12f), new Vector2(760f, 270f), FurnaceRhythmPalette.ElectricCyan);

            CreateText(_startOverlayCardRect, "Start Card Eyebrow", "THE FURNACE IS READY", 13f, FurnaceRhythmPalette.OxidizedTeal,
                new Vector2(0.5f, 0.5f), new Vector2(0f, 86f), new Vector2(640f, 24f), FontStyles.Bold);
            CreateText(_startOverlayCardRect, "Start Card Title", "FORGE FIVE WORKPIECES", 32f, FurnaceRhythmPalette.IndustrialCream,
                new Vector2(0.5f, 0.5f), new Vector2(0f, 41f), new Vector2(690f, 46f), FontStyles.Bold);
            CreateText(_startOverlayCardRect, "Start Card Phases", "HEAT     ·     HAMMER     ·     QUENCH", 16f, FurnaceRhythmPalette.ElectricCyan,
                new Vector2(0.5f, 0.5f), new Vector2(0f, -9f), new Vector2(660f, 30f), FontStyles.Bold);
            CreateText(_startOverlayCardRect, "Start Card Scoring", "PERFECT 100  /  GOOD 60  /  STREAK BONUS UP TO +50", 12f,
                FurnaceRhythmPalette.OxidizedTeal, new Vector2(0.5f, 0.5f), new Vector2(0f, -55f), new Vector2(670f, 22f), FontStyles.Bold);
            CreateText(_startOverlayCardRect, "Start Card Prompt", "PRESS SPACE OR CLICK TO START", 15f, FurnaceRhythmPalette.IndustrialCream,
                new Vector2(0.5f, 0.5f), new Vector2(0f, -98f), new Vector2(650f, 26f), FontStyles.Bold);
        }

        private void BuildOrderResultOverlay(Transform parent)
        {
            _orderResultOverlayRoot = CreateOverlayRoot(parent, "Workpiece Quality Overlay", 0.72f);
            _orderResultCardRect = CreateRoundedPanel(_orderResultOverlayRoot.transform, "Workpiece Quality Card", new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(930f, 430f), FurnaceRhythmPalette.SafetyOrange);

            _orderResultHeaderLabel = CreateText(_orderResultCardRect, "Quality Card Header", "WORKPIECE  01 / 05", 13f,
                FurnaceRhythmPalette.OxidizedTeal, new Vector2(0.5f, 0.5f), new Vector2(0f, 163f), new Vector2(780f, 24f), FontStyles.Bold);
            _orderResultQualityLabel = CreateText(_orderResultCardRect, "Quality Card Grade", "FINE WORK", 38f,
                FurnaceRhythmPalette.IndustrialCream, new Vector2(0.5f, 0.5f), new Vector2(0f, 105f), new Vector2(820f, 52f), FontStyles.Bold);
            _orderResultScoreLabel = CreateText(_orderResultCardRect, "Quality Card Points", "+0000  POINTS EARNED", 18f,
                FurnaceRhythmPalette.ElectricCyan, new Vector2(0.5f, 0.5f), new Vector2(0f, 53f), new Vector2(760f, 30f), FontStyles.Bold);

            for (int i = 0; i < _phaseCount; i++)
            {
                UnityEngine.UI.Image chip = CreateImage(_orderResultCardRect, "Grade Chip " + _phaseNames[i],
                    WithAlpha(FurnaceRhythmPalette.SlateMachinery, 0.9f));
                chip.sprite = GetRoundedPanelSprite();
                chip.type = UnityEngine.UI.Image.Type.Sliced;
                SetCenteredRect(chip.rectTransform, new Vector2(0.5f, 0.5f), new Vector2((i - 1) * 242f, -40f),
                    new Vector2(218f, 76f));
                _orderGradeChipImages[i] = chip;
                CreateText(chip.transform, "Grade Chip Phase", _phaseNames[i], 11f, FurnaceRhythmPalette.OxidizedTeal,
                    new Vector2(0.5f, 0.5f), new Vector2(0f, 15f), new Vector2(192f, 18f), FontStyles.Bold);
                _orderGradeChipValueLabels[i] = CreateText(chip.transform, "Grade Chip Result", "MISS", 17f,
                    FurnaceRhythmPalette.IndustrialCream, new Vector2(0.5f, 0.5f), new Vector2(0f, -12f), new Vector2(192f, 24f), FontStyles.Bold);
            }

            _orderResultNextLabel = CreateText(_orderResultCardRect, "Quality Card Next", "NEXT WORKPIECE COMING UP", 12f,
                FurnaceRhythmPalette.OxidizedTeal, new Vector2(0.5f, 0.5f), new Vector2(0f, -151f), new Vector2(740f, 22f), FontStyles.Bold);
        }

        private void BuildShiftResultOverlay(Transform parent)
        {
            _shiftResultOverlayRoot = CreateOverlayRoot(parent, "Shift Summary Overlay", 0.82f);
            _shiftResultCardRect = CreateRoundedPanel(_shiftResultOverlayRoot.transform, "Shift Summary Card", new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(930f, 470f), FurnaceRhythmPalette.ElectricCyan);

            CreateText(_shiftResultCardRect, "Shift Summary Eyebrow", "SHIFT COMPLETE", 13f, FurnaceRhythmPalette.OxidizedTeal,
                new Vector2(0.5f, 0.5f), new Vector2(0f, 175f), new Vector2(780f, 24f), FontStyles.Bold);
            _shiftResultRankLabel = CreateText(_shiftResultCardRect, "Shift Summary Rank", "SKILLED SMITH", 34f,
                FurnaceRhythmPalette.IndustrialCream, new Vector2(0.5f, 0.5f), new Vector2(0f, 116f), new Vector2(820f, 50f), FontStyles.Bold);
            _shiftResultScoreLabel = CreateText(_shiftResultCardRect, "Shift Summary Score", "00000  POINTS", 48f,
                FurnaceRhythmPalette.ElectricCyan, new Vector2(0.5f, 0.5f), new Vector2(0f, 47f), new Vector2(800f, 65f), FontStyles.Bold);
            _shiftResultStatsLabel = CreateText(_shiftResultCardRect, "Shift Summary Grades", "PERFECT  00     GOOD  00     MISS  00", 16f,
                FurnaceRhythmPalette.IndustrialCream, new Vector2(0.5f, 0.5f), new Vector2(0f, -43f), new Vector2(800f, 30f), FontStyles.Bold);
            _shiftResultStreakLabel = CreateText(_shiftResultCardRect, "Shift Summary Streak", "BEST STREAK  00  ·  FIVE WORKPIECES FORGED", 12f,
                FurnaceRhythmPalette.OxidizedTeal, new Vector2(0.5f, 0.5f), new Vector2(0f, -88f), new Vector2(820f, 24f), FontStyles.Bold);
            CreateText(_shiftResultCardRect, "Shift Summary Restart", "PRESS SPACE OR CLICK TO FORGE AGAIN", 14f,
                FurnaceRhythmPalette.IndustrialCream, new Vector2(0.5f, 0.5f), new Vector2(0f, -173f), new Vector2(800f, 28f), FontStyles.Bold);
        }

        private GameObject CreateOverlayRoot(Transform parent, string name, float scrimAlpha)
        {
            GameObject overlay = new GameObject(name, typeof(RectTransform), typeof(CanvasGroup));
            overlay.transform.SetParent(parent, false);
            StretchToParent(overlay.GetComponent<RectTransform>());

            UnityEngine.UI.Image scrim = CreateImage(overlay.transform, name + " Scrim",
                WithAlpha(FurnaceRhythmPalette.MidnightIndustrial, scrimAlpha));
            StretchToParent(scrim.rectTransform);
            overlay.SetActive(false);
            return overlay;
        }

        private static RectTransform CreateRoundedPanel(
            Transform parent,
            string name,
            Vector2 anchor,
            Vector2 position,
            Vector2 dimensions,
            Color accent)
        {
            UnityEngine.UI.Image frame = CreateImage(parent, name + " Frame", FurnaceRhythmPalette.SlateMachinery);
            frame.sprite = GetRoundedPanelSprite();
            frame.type = UnityEngine.UI.Image.Type.Sliced;
            SetCenteredRect(frame.rectTransform, anchor, position, dimensions);

            UnityEngine.UI.Image surface = CreateImage(frame.transform, name + " Surface", WithAlpha(FurnaceRhythmPalette.MidnightIndustrial, 0.97f));
            surface.sprite = GetRoundedPanelSprite();
            surface.type = UnityEngine.UI.Image.Type.Sliced;
            StretchToParentInset(surface.rectTransform, 3f);

            UnityEngine.UI.Image accentLine = CreateImage(surface.transform, name + " Accent", accent);
            RectTransform accentRect = accentLine.rectTransform;
            accentRect.anchorMin = new Vector2(0f, 1f);
            accentRect.anchorMax = new Vector2(1f, 1f);
            accentRect.pivot = new Vector2(0.5f, 0.5f);
            accentRect.anchoredPosition = new Vector2(0f, -7f);
            accentRect.sizeDelta = new Vector2(-26f, 2f);
            return frame.rectTransform;
        }

        private void ShowOverlay(GameObject overlay, RectTransform card)
        {
            if (overlay == null)
                return;

            CanvasGroup canvasGroup = overlay.GetComponent<CanvasGroup>();
            if (_fadingOverlayCanvasGroup == canvasGroup)
                _fadingOverlayCanvasGroup = null;
            overlay.SetActive(true);
            _activeOverlayCanvasGroup = canvasGroup;
            _activeOverlayCanvasGroup.alpha = 0f;
            _activeOverlayCardRect = card;
            if (_activeOverlayCardRect != null)
                _activeOverlayCardRect.localScale = Vector3.one * 0.9f;
            _overlayRevealElapsed = 0f;
        }

        private void HideOverlay(GameObject overlay)
        {
            if (overlay == null || !overlay.activeSelf)
                return;

            CanvasGroup canvasGroup = overlay.GetComponent<CanvasGroup>();
            if (_activeOverlayCanvasGroup == canvasGroup)
            {
                _activeOverlayCanvasGroup = null;
                _activeOverlayCardRect = null;
            }

            if (_fadingOverlayCanvasGroup != null && _fadingOverlayCanvasGroup != canvasGroup)
                _fadingOverlayCanvasGroup.gameObject.SetActive(false);
            _fadingOverlayCanvasGroup = canvasGroup;
            _fadingOverlayStartAlpha = canvasGroup.alpha;
            _overlayFadeOutElapsed = 0f;
        }

        private void BuildTimingTrack(Transform parent)
        {
            UnityEngine.UI.Image track = CreateImage(parent, "Timing Track", WithAlpha(FurnaceRhythmPalette.SlateMachinery, 0.95f));
            SetCenteredRect(track.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, -260f), new Vector2(1000f, 26f));
            _timingTrackRect = track.rectTransform;

            _targetBandImage = CreateImage(track.transform, "Cyan Target Band", WithAlpha(FurnaceRhythmPalette.ElectricCyan, 0.72f));
            RectTransform targetRect = _targetBandImage.rectTransform;
            targetRect.anchorMin = new Vector2(_targetCenter - _goodWindow, 0f);
            targetRect.anchorMax = new Vector2(_targetCenter + _goodWindow, 1f);
            targetRect.pivot = new Vector2(0.5f, 0.5f);
            targetRect.offsetMin = Vector2.zero;
            targetRect.offsetMax = Vector2.zero;

            _perfectBandImage = CreateImage(track.transform, "Perfect Target Center", WithAlpha(FurnaceRhythmPalette.IndustrialCream, 0.88f));
            RectTransform perfectRect = _perfectBandImage.rectTransform;
            perfectRect.anchorMin = new Vector2(_targetCenter - _perfectWindow, 0f);
            perfectRect.anchorMax = new Vector2(_targetCenter + _perfectWindow, 1f);
            perfectRect.pivot = new Vector2(0.5f, 0.5f);
            perfectRect.offsetMin = Vector2.zero;
            perfectRect.offsetMax = Vector2.zero;

            _markerTrailImage = CreateImage(track.transform, "Timing Marker Trail", WithAlpha(FurnaceRhythmPalette.ElectricCyan, 0.12f));
            _markerTrailImage.sprite = GetRoundedPanelSprite();
            _markerTrailImage.type = UnityEngine.UI.Image.Type.Sliced;
            _markerTrailRect = _markerTrailImage.rectTransform;
            _markerTrailRect.anchorMin = new Vector2(0.5f, 0.5f);
            _markerTrailRect.anchorMax = new Vector2(0.5f, 0.5f);
            _markerTrailRect.pivot = new Vector2(1f, 0.5f);
            _markerTrailRect.sizeDelta = new Vector2(58f, 8f);
            _markerTrailRect.anchoredPosition = new Vector2(-_timingTrackRect.rect.width * 0.5f, 0f);

            UnityEngine.UI.Image marker = CreateImage(track.transform, "Timing Marker", FurnaceRhythmPalette.IndustrialCream);
            _markerRect = marker.rectTransform;
            _markerImage = marker;
            _markerRect.anchorMin = new Vector2(0.5f, 0.5f);
            _markerRect.anchorMax = new Vector2(0.5f, 0.5f);
            _markerRect.pivot = new Vector2(0.5f, 0.5f);
            _markerRect.sizeDelta = new Vector2(10f, 64f);
            _markerRect.anchoredPosition = new Vector2(-_timingTrackRect.rect.width * 0.5f, 0f);
        }

        private static Canvas CreateCanvas()
        {
            GameObject canvasObject = new GameObject("Furnace Rhythm Canvas", typeof(RectTransform), typeof(Canvas),
                typeof(UnityEngine.UI.CanvasScaler), typeof(UnityEngine.UI.GraphicRaycaster));
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            UnityEngine.UI.CanvasScaler scaler = canvasObject.GetComponent<UnityEngine.UI.CanvasScaler>();
            scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            return canvas;
        }

        private static UnityEngine.UI.Image CreateImage(Transform parent, string name, Color color)
        {
            GameObject imageObject = new GameObject(name, typeof(RectTransform), typeof(UnityEngine.UI.Image));
            imageObject.transform.SetParent(parent, false);

            UnityEngine.UI.Image image = imageObject.GetComponent<UnityEngine.UI.Image>();
            image.sprite = GetWhiteSprite();
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        private static Sprite CreateSoftGlowSprite(out Texture2D texture)
        {
            const int textureSize = 128;
            texture = new Texture2D(textureSize, textureSize, TextureFormat.RGBA32, false);
            texture.name = "Furnace Rhythm Soft Glow";
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Bilinear;
            texture.hideFlags = HideFlags.HideAndDontSave;

            Color[] pixels = new Color[textureSize * textureSize];
            for (int y = 0; y < textureSize; y++)
            {
                for (int x = 0; x < textureSize; x++)
                {
                    float dx = (x / (textureSize - 1f) - 0.5f) * 2f;
                    float dy = (y / (textureSize - 1f) - 0.5f) * 2f;
                    float radius = Mathf.Sqrt(dx * dx + dy * dy);
                    float falloff = Mathf.Pow(Mathf.Clamp01(1f - radius), 2.6f);
                    pixels[y * textureSize + x] = new Color(1f, 1f, 1f, falloff);
                }
            }

            texture.SetPixels(pixels);
            texture.Apply(false, false);
            Sprite sprite = Sprite.Create(texture, new Rect(0f, 0f, textureSize, textureSize), new Vector2(0.5f, 0.5f), 100f);
            sprite.name = "Furnace Rhythm Soft Glow Sprite";
            sprite.hideFlags = HideFlags.HideAndDontSave;
            return sprite;
        }

        private static TextMeshProUGUI CreateText(
            Transform parent,
            string name,
            string value,
            float size,
            Color color,
            Vector2 anchor,
            Vector2 position,
            Vector2 dimensions,
            FontStyles fontStyle)
        {
            GameObject textObject = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            textObject.transform.SetParent(parent, false);

            TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
            TMP_FontAsset font = TMP_Settings.defaultFontAsset;
            if (font == null)
                font = TMP_Settings.defaultFontAsset;

            text.font = font;
            text.fontSize = size;
            text.fontStyle = fontStyle;
            text.color = color;
            text.text = value;
            text.alignment = TextAlignmentOptions.Center;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.raycastTarget = false;
            SetCenteredRect(text.rectTransform, anchor, position, dimensions);
            return text;
        }

        private static void SetCenteredRect(RectTransform rect, Vector2 anchor, Vector2 position, Vector2 dimensions)
        {
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = dimensions;
            rect.localScale = Vector3.one;
        }

        private static void StretchToParent(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.localScale = Vector3.one;
        }

        private static void StretchToParentInset(RectTransform rect, float inset)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = new Vector2(inset, inset);
            rect.offsetMax = new Vector2(-inset, -inset);
            rect.localScale = Vector3.one;
        }

        private static Sprite GetWhiteSprite()
        {
            if (_whiteSprite == null)
                _whiteSprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f));
            return _whiteSprite;
        }

        private static Sprite GetRoundedPanelSprite()
        {
            if (_roundedPanelSprite != null)
                return _roundedPanelSprite;

            const int textureSize = 64;
            const float cornerRadius = 12f;
            Texture2D texture = new Texture2D(textureSize, textureSize, TextureFormat.RGBA32, false);
            texture.name = "Furnace Rhythm Rounded Panel Skin";
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Bilinear;
            texture.hideFlags = HideFlags.HideAndDontSave;

            Color[] pixels = new Color[textureSize * textureSize];
            for (int y = 0; y < textureSize; y++)
            {
                for (int x = 0; x < textureSize; x++)
                {
                    float nearestX = Mathf.Clamp(x + 0.5f, cornerRadius, textureSize - cornerRadius);
                    float nearestY = Mathf.Clamp(y + 0.5f, cornerRadius, textureSize - cornerRadius);
                    float distance = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(nearestX, nearestY));
                    float alpha = Mathf.Clamp01(cornerRadius + 0.5f - distance);
                    pixels[y * textureSize + x] = new Color(1f, 1f, 1f, alpha);
                }
            }

            texture.SetPixels(pixels);
            texture.Apply(false, false);
            _roundedPanelSprite = Sprite.Create(texture, new Rect(0f, 0f, textureSize, textureSize), new Vector2(0.5f, 0.5f),
                textureSize, 0, SpriteMeshType.FullRect, new Vector4(16f, 16f, 16f, 16f));
            _roundedPanelSprite.name = "Furnace Rhythm Rounded Panel Sprite";
            _roundedPanelSprite.hideFlags = HideFlags.HideAndDontSave;
            return _roundedPanelSprite;
        }

        private static Sprite GetImpactRingSprite()
        {
            if (_impactRingSprite != null)
                return _impactRingSprite;

            const int textureSize = 64;
            const float ringRadius = 0.72f;
            Texture2D texture = new Texture2D(textureSize, textureSize, TextureFormat.RGBA32, false);
            texture.name = "Furnace Rhythm Impact Ring Skin";
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Bilinear;
            texture.hideFlags = HideFlags.HideAndDontSave;

            Color[] pixels = new Color[textureSize * textureSize];
            for (int y = 0; y < textureSize; y++)
            {
                for (int x = 0; x < textureSize; x++)
                {
                    float dx = ((x + 0.5f) / textureSize - 0.5f) * 2f;
                    float dy = ((y + 0.5f) / textureSize - 0.5f) * 2f;
                    float distanceToRing = Mathf.Abs(Mathf.Sqrt(dx * dx + dy * dy) - ringRadius);
                    float coverage = 1f - Mathf.Clamp01((distanceToRing - 0.025f) / 0.055f);
                    float alpha = Mathf.SmoothStep(0f, 1f, coverage);
                    pixels[y * textureSize + x] = new Color(1f, 1f, 1f, alpha);
                }
            }

            texture.SetPixels(pixels);
            texture.Apply(false, false);
            _impactRingSprite = Sprite.Create(texture, new Rect(0f, 0f, textureSize, textureSize), new Vector2(0.5f, 0.5f), textureSize);
            _impactRingSprite.name = "Furnace Rhythm Impact Ring Sprite";
            _impactRingSprite.hideFlags = HideFlags.HideAndDontSave;
            return _impactRingSprite;
        }

        private static Color GradeColor(TimingGrade grade)
        {
            switch (grade)
            {
                case TimingGrade.Perfect: return FurnaceRhythmPalette.ElectricCyan;
                case TimingGrade.Good: return FurnaceRhythmPalette.SafetyOrange;
                default: return FurnaceRhythmPalette.FactoryCoral;
            }
        }

        private static string GradeName(TimingGrade grade)
        {
            switch (grade)
            {
                case TimingGrade.Perfect: return "PERFECT";
                case TimingGrade.Good: return "GOOD";
                default: return "MISS";
            }
        }

        private static Color WithAlpha(Color color, float alpha)
        {
            color.a = alpha;
            return color;
        }

    }
}
