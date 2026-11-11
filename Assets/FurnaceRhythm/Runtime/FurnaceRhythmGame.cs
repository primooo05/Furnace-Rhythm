
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace FurnaceRhythm
{
    /// <summary>
    /// Furnace Rhythm prototype: a five-order shift, three timing beats per workpiece, and a restart.
    /// The Canvas is created at runtime so the first scene stays intentionally small.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class FurnaceRhythmGame : MonoBehaviour
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

        private const int PhaseCount = 3;
        private const int OrderCount = 5;
        private const int SparkCount = 8;
        private const float ImpactDurationSeconds = 0.32f;
        private const float SparkDurationSeconds = 0.38f;
        private const float ImpactRingDurationSeconds = 0.34f;
        private const float ScoreCountUpSeconds = 0.26f;
        private const float OverlayFadeOutSeconds = 0.16f;
        private const float OverlayRevealSeconds = 0.24f;
        private static readonly string[] PhaseNames = { "HEAT", "HAMMER", "QUENCH" };

        [Header("Shift and timing")]
        [SerializeField] private Sprite hotBilletSprite;
        [SerializeField, Min(0.6f)] private float beatDurationSeconds = 1.7f;
        [SerializeField, Range(0.4f, 0.9f)] private float targetCenter = 0.68f;
        [SerializeField, Range(0.01f, 0.12f)] private float perfectWindow = 0.045f;
        [SerializeField, Range(0.06f, 0.24f)] private float goodWindow = 0.12f;
        [SerializeField, Min(0.1f)] private float judgmentHoldSeconds = 0.5f;
        [SerializeField, Min(0.2f)] private float orderResultHoldSeconds = 1.1f;

        private readonly TimingGrade[] orderGrades = new TimingGrade[PhaseCount];
        private readonly UnityEngine.UI.Image[] phaseIndicators = new UnityEngine.UI.Image[PhaseCount];
        private readonly TextMeshProUGUI[] phaseIndicatorLabels = new TextMeshProUGUI[PhaseCount];
        private readonly UnityEngine.UI.Image[] orderProgressSegments = new UnityEngine.UI.Image[OrderCount];
        private readonly UnityEngine.UI.Image[] comboBonusPips = new UnityEngine.UI.Image[5];
        private readonly UnityEngine.UI.Image[] orderGradeChipImages = new UnityEngine.UI.Image[PhaseCount];
        private readonly TextMeshProUGUI[] orderGradeChipValueLabels = new TextMeshProUGUI[PhaseCount];
        private readonly UnityEngine.UI.Image[] sparkImages = new UnityEngine.UI.Image[SparkCount];
        private readonly RectTransform[] sparkRects = new RectTransform[SparkCount];
        private readonly Vector2[] sparkDirections = new Vector2[SparkCount];

        private RoundState state;
        private int currentPhaseIndex;
        private int currentOrderIndex;
        private int score;
        private int orderScore;
        private int currentStreak;
        private int bestStreak;
        private int totalPerfect;
        private int totalGood;
        private int totalMiss;
        private float phaseElapsed;
        private float resolveElapsed;
        private float orderResultElapsed;

        private RectTransform timingTrackRect;
        private RectTransform markerRect;
        private RectTransform markerTrailRect;
        private RectTransform workpieceRect;
        private RectTransform glowRect;
        private RectTransform impactRingRect;
        private RectTransform judgmentRect;
        private UnityEngine.UI.Image markerImage;
        private UnityEngine.UI.Image markerTrailImage;
        private UnityEngine.UI.Image workpieceImage;
        private UnityEngine.UI.Image glowImage;
        private UnityEngine.UI.Image impactRingImage;
        private UnityEngine.UI.Image feedbackFlashImage;
        private UnityEngine.UI.Image targetBandImage;
        private UnityEngine.UI.Image perfectBandImage;
        private TextMeshProUGUI phaseLabel;
        private TextMeshProUGUI judgmentLabel;
        private TextMeshProUGUI instructionLabel;
        private TextMeshProUGUI scoreValueLabel;
        private TextMeshProUGUI streakValueLabel;
        private TextMeshProUGUI comboBonusLabel;
        private TextMeshProUGUI orderValueLabel;
        private TextMeshProUGUI orderResultHeaderLabel;
        private TextMeshProUGUI orderResultQualityLabel;
        private TextMeshProUGUI orderResultScoreLabel;
        private TextMeshProUGUI orderResultNextLabel;
        private TextMeshProUGUI shiftResultRankLabel;
        private TextMeshProUGUI shiftResultScoreLabel;
        private TextMeshProUGUI shiftResultStatsLabel;
        private TextMeshProUGUI shiftResultStreakLabel;

        private GameObject startOverlayRoot;
        private GameObject orderResultOverlayRoot;
        private GameObject shiftResultOverlayRoot;
        private RectTransform startOverlayCardRect;
        private RectTransform orderResultCardRect;
        private RectTransform shiftResultCardRect;
        private RectTransform activeOverlayCardRect;
        private CanvasGroup activeOverlayCanvasGroup;
        private float overlayRevealElapsed;
        private CanvasGroup fadingOverlayCanvasGroup;
        private float overlayFadeOutElapsed;
        private float fadingOverlayStartAlpha;

        private Texture2D glowTexture;
        private Sprite glowSprite;
        private float animationClock;
        private float impactElapsed;
        private float sparksElapsed;
        private float judgmentPopElapsed;
        private float currentTimingProgress;
        private bool impactAnimationActive;
        private bool sparkAnimationActive;
        private bool judgmentPopActive;
        private float scoreFlashElapsed;
        private int displayedScore;
        private int scoreAnimationStart;
        private int scoreAnimationTarget;
        private float scoreAnimationElapsed;
        private float impactRingElapsed;
        private float feedbackFlashElapsed;
        private float feedbackFlashDuration;
        private float feedbackFlashStrength;
        private Color feedbackFlashColor;
        private Color impactRingColor;
        private float impactRingMaxScale;
        private bool impactRingActive;
        private TimingGrade impactGrade;
        private int sparkPhaseIndex;
        private Color impactColor;
        private Color sparkColor;
        private readonly Vector2 workpieceBasePosition = new Vector2(0f, 88f);
        private readonly Vector2 sparkOriginPosition = new Vector2(0f, 145f);

        private static Sprite whiteSprite;
        private static Sprite roundedPanelSprite;
        private static Sprite impactRingSprite;

        private void Awake()
        {
            BuildInterface();
            ShowReadyState();
        }

        private void Update()
        {
            float deltaTime = Time.unscaledDeltaTime;
            animationClock += deltaTime;
            UpdatePresentationAnimation(deltaTime);

            switch (state)
            {
                case RoundState.Ready:
                    if (ActionPressedThisFrame())
                        StartRound();
                    break;

                case RoundState.Playing:
                    UpdateTimingBeat();
                    break;

                case RoundState.Resolving:
                    resolveElapsed += deltaTime;
                    if (resolveElapsed >= judgmentHoldSeconds)
                        AdvanceAfterJudgment();
                    break;

                case RoundState.OrderResult:
                    orderResultElapsed += deltaTime;
                    if (orderResultElapsed >= orderResultHoldSeconds)
                        AdvanceAfterOrderResult();
                    break;

                case RoundState.ShiftResult:
                    if (ActionPressedThisFrame())
                        StartRound();
                    break;
            }
        }

        private void OnDestroy()
        {
            if (glowSprite != null)
                Destroy(glowSprite);
            if (glowTexture != null)
                Destroy(glowTexture);
        }

        /// <summary>Called by the editor scene builder before the scene is saved.</summary>
        public void SetHotBilletSprite(Sprite sprite)
        {
            hotBilletSprite = sprite;
        }

        private void UpdateTimingBeat()
        {
            phaseElapsed += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(phaseElapsed / beatDurationSeconds);
            currentTimingProgress = progress;
            UpdateTimingCue(progress);
            float trackWidth = timingTrackRect.rect.width;
            markerRect.anchoredPosition = new Vector2((progress - 0.5f) * trackWidth, 0f);
            markerTrailRect.anchoredPosition = markerRect.anchoredPosition;

            // The beat resolves as soon as the marker leaves the target band.
            if (progress >= targetCenter + goodWindow)
            {
                ResolveBeat(TimingGrade.Miss, "LATE · MISS");
                return;
            }

            if (!ActionPressedThisFrame())
                return;

            float distance = Mathf.Abs(progress - targetCenter);
            if (distance <= perfectWindow)
                ResolveBeat(TimingGrade.Perfect, "PERFECT");
            else if (distance <= goodWindow)
                ResolveBeat(TimingGrade.Good, "GOOD");
            else
                ResolveBeat(TimingGrade.Miss, progress < targetCenter ? "EARLY · MISS" : "LATE · MISS");
        }

        private void StartRound()
        {
            HideOverlay(startOverlayRoot);
            HideOverlay(orderResultOverlayRoot);
            HideOverlay(shiftResultOverlayRoot);
            score = 0;
            orderScore = 0;
            displayedScore = 0;
            scoreAnimationStart = 0;
            scoreAnimationTarget = 0;
            scoreAnimationElapsed = 0f;
            scoreFlashElapsed = 0f;
            feedbackFlashElapsed = 0f;
            impactRingActive = false;
            if (impactRingImage != null)
                impactRingImage.color = WithAlpha(impactRingColor, 0f);
            if (scoreValueLabel != null)
            {
                scoreValueLabel.text = "00000";
                scoreValueLabel.rectTransform.localScale = Vector3.one;
            }
            currentStreak = 0;
            bestStreak = 0;
            totalPerfect = 0;
            totalGood = 0;
            totalMiss = 0;
            currentOrderIndex = 0;
            impactAnimationActive = false;
            sparkAnimationActive = false;
            judgmentPopActive = false;
            judgmentRect.localScale = Vector3.one;
            BeginOrder();
        }

        private void BeginOrder()
        {
            orderScore = 0;
            for (int i = 0; i < orderGrades.Length; i++)
                orderGrades[i] = TimingGrade.Miss;

            currentPhaseIndex = 0;
            StartTimingBeat();
        }

        private void StartTimingBeat()
        {
            state = RoundState.Playing;
            phaseElapsed = 0f;
            resolveElapsed = 0f;
            currentTimingProgress = 0f;
            markerRect.anchoredPosition = new Vector2(-timingTrackRect.rect.width * 0.5f, 0f);
            markerTrailRect.anchoredPosition = markerRect.anchoredPosition;
            markerImage.color = FurnaceRhythmPalette.IndustrialCream;
            phaseLabel.text = PhaseNames[currentPhaseIndex] + "  ·  BEAT " + (currentPhaseIndex + 1) + " / " + PhaseCount;
            judgmentLabel.text = "WAIT FOR THE CYAN ZONE";
            judgmentLabel.color = FurnaceRhythmPalette.IndustrialCream;
            instructionLabel.text = "PRESS SPACE OR CLICK WHEN THE MARKER ENTERS THE CYAN BAND";
            workpieceImage.color = Color.white;
            UpdateTimingCue(0f);
            UpdateRunStats();
            RefreshPhaseIndicators();
        }

        private void ResolveBeat(TimingGrade grade, string message)
        {
            if (state != RoundState.Playing)
                return;

            orderGrades[currentPhaseIndex] = grade;
            int awardedPoints = AwardPoints(grade);
            if (awardedPoints > 0)
                scoreFlashElapsed = 0.3f;
            TriggerScreenFlash(grade);
            state = RoundState.Resolving;
            resolveElapsed = 0f;
            judgmentLabel.text = message + "  +" + awardedPoints;
            judgmentLabel.color = GradeColor(grade);
            markerImage.color = GradeColor(grade);
            impactGrade = grade;
            impactColor = GradeColor(grade);
            impactElapsed = 0f;
            impactAnimationActive = true;
            judgmentPopElapsed = 0f;
            judgmentPopActive = true;
            if (grade != TimingGrade.Miss)
            {
                TriggerSparks(CurrentPhaseAccent());
                TriggerImpactRing(grade);
            }
            UpdateRunStats();
            RefreshPhaseIndicators();
        }

        private int AwardPoints(TimingGrade grade)
        {
            if (grade == TimingGrade.Miss)
            {
                totalMiss++;
                currentStreak = 0;
                return 0;
            }

            int basePoints;
            if (grade == TimingGrade.Perfect)
            {
                totalPerfect++;
                basePoints = 100;
            }
            else
            {
                totalGood++;
                basePoints = 60;
            }

            currentStreak++;
            bestStreak = Mathf.Max(bestStreak, currentStreak);
            int comboBonus = Mathf.Min(currentStreak - 1, 5) * 10;
            int awardedPoints = basePoints + comboBonus;
            score += awardedPoints;
            orderScore += awardedPoints;
            return awardedPoints;
        }

        private void AdvanceAfterJudgment()
        {
            if (currentPhaseIndex + 1 < PhaseCount)
            {
                currentPhaseIndex++;
                StartTimingBeat();
                return;
            }

            ShowOrderResultState();
        }

        private void ShowReadyState()
        {
            state = RoundState.Ready;
            currentPhaseIndex = 0;
            phaseLabel.text = "READY  ·  FIVE-ORDER SHIFT";
            judgmentLabel.text = "FORGE FIVE WORKPIECES";
            judgmentLabel.color = FurnaceRhythmPalette.IndustrialCream;
            instructionLabel.text = "PRESS SPACE OR CLICK TO START";
            markerRect.anchoredPosition = new Vector2(-timingTrackRect.rect.width * 0.5f, 0f);
            markerTrailRect.anchoredPosition = markerRect.anchoredPosition;
            markerImage.color = FurnaceRhythmPalette.IndustrialCream;
            UpdateRunStats();
            RefreshPhaseIndicators();
            ShowOverlay(startOverlayRoot, startOverlayCardRect);
        }

        private void ShowOrderResultState()
        {
            state = RoundState.OrderResult;
            orderResultElapsed = 0f;
            int perfect = 0;
            int good = 0;
            int miss = 0;

            for (int i = 0; i < orderGrades.Length; i++)
            {
                switch (orderGrades[i])
                {
                    case TimingGrade.Perfect: perfect++; break;
                    case TimingGrade.Good: good++; break;
                    default: miss++; break;
                }
            }

            string quality = miss > 0 ? "NEEDS REWORK" : perfect == PhaseCount ? "MASTERWORK" : "FINE WORK";
            phaseLabel.text = "WORKPIECE " + (currentOrderIndex + 1) + " / " + OrderCount + " COMPLETE";
            judgmentLabel.text = quality + "  ·  " + perfect + " PERFECT  /  " + good + " GOOD  /  " + miss + " MISS";
            judgmentLabel.color = miss > 0 ? FurnaceRhythmPalette.FactoryCoral : perfect == PhaseCount ? FurnaceRhythmPalette.ElectricCyan : FurnaceRhythmPalette.SafetyOrange;
            instructionLabel.text = currentOrderIndex + 1 < OrderCount ? "NEXT WORKPIECE COMING UP" : "SHIFT SUMMARY NEXT";
            judgmentPopElapsed = 0f;
            judgmentPopActive = true;
            UpdateRunStats();
            RefreshPhaseIndicators();
            ShowOrderResultOverlay(quality, perfect, good, miss);
        }

        private void AdvanceAfterOrderResult()
        {
            HideOverlay(orderResultOverlayRoot);
            if (currentOrderIndex + 1 < OrderCount)
            {
                currentOrderIndex++;
                BeginOrder();
                return;
            }

            ShowShiftResultState();
        }

        private void ShowShiftResultState()
        {
            state = RoundState.ShiftResult;
            string rank = totalMiss == 0 && totalPerfect == PhaseCount * OrderCount
                ? "MASTER FORGER"
                : totalMiss <= 2
                    ? "SKILLED SMITH"
                    : totalPerfect + totalGood >= 10
                        ? "STEADY HAND"
                        : "KEEP PRACTICING";

            phaseLabel.text = "SHIFT COMPLETE";
            judgmentLabel.text = rank + "  ·  " + score.ToString("D5") + " POINTS";
            judgmentLabel.color = totalMiss == 0 ? FurnaceRhythmPalette.ElectricCyan : totalMiss <= 2 ? FurnaceRhythmPalette.SafetyOrange : FurnaceRhythmPalette.FactoryCoral;
            instructionLabel.text = "PRESS SPACE OR CLICK TO START A NEW SHIFT";
            UpdateRunStats();
            shiftResultRankLabel.text = rank;
            shiftResultRankLabel.color = totalMiss == 0 ? FurnaceRhythmPalette.ElectricCyan : totalMiss <= 2 ? FurnaceRhythmPalette.SafetyOrange : FurnaceRhythmPalette.FactoryCoral;
            shiftResultScoreLabel.text = score.ToString("D5") + "  POINTS";
            shiftResultStatsLabel.text = "PERFECT  " + totalPerfect.ToString("D2") + "     GOOD  " + totalGood.ToString("D2") +
                                         "     MISS  " + totalMiss.ToString("D2");
            shiftResultStreakLabel.text = "BEST STREAK  " + bestStreak.ToString("D2") + "  ·  FIVE WORKPIECES FORGED";
            judgmentPopElapsed = 0f;
            judgmentPopActive = true;
            RefreshPhaseIndicators();
            ShowOverlay(shiftResultOverlayRoot, shiftResultCardRect);
        }

        private void UpdateRunStats()
        {
            if (scoreValueLabel == null)
                return;

            if (score != scoreAnimationTarget)
            {
                scoreAnimationStart = displayedScore;
                scoreAnimationTarget = score;
                scoreAnimationElapsed = 0f;
            }
            streakValueLabel.text = currentStreak.ToString("D2");
            comboBonusLabel.text = "NEXT HIT  +" + (currentStreak == 0 ? 0 : Mathf.Min(currentStreak, 5) * 10);
            orderValueLabel.text = (currentOrderIndex + 1).ToString("D2") + " / " + OrderCount.ToString("D2");

            int filledBonusPips = Mathf.Min(currentStreak, comboBonusPips.Length);
            for (int i = 0; i < comboBonusPips.Length; i++)
                comboBonusPips[i].color = i < filledBonusPips
                    ? FurnaceRhythmPalette.SafetyOrange
                    : WithAlpha(FurnaceRhythmPalette.SlateMachinery, 0.86f);

            for (int i = 0; i < orderProgressSegments.Length; i++)
            {
                if (i < currentOrderIndex || ((state == RoundState.OrderResult || state == RoundState.ShiftResult) && i == currentOrderIndex))
                    orderProgressSegments[i].color = FurnaceRhythmPalette.OxidizedTeal;
                else if (i == currentOrderIndex)
                    orderProgressSegments[i].color = FurnaceRhythmPalette.ElectricCyan;
                else
                    orderProgressSegments[i].color = WithAlpha(FurnaceRhythmPalette.SlateMachinery, 0.82f);
            }
        }

        private void ShowOrderResultOverlay(string quality, int perfect, int good, int miss)
        {
            orderResultHeaderLabel.text = "WORKPIECE  " + (currentOrderIndex + 1).ToString("D2") + "  /  " + OrderCount.ToString("D2");
            orderResultQualityLabel.text = quality;
            orderResultQualityLabel.color = miss > 0 ? FurnaceRhythmPalette.FactoryCoral : perfect == PhaseCount ? FurnaceRhythmPalette.ElectricCyan : FurnaceRhythmPalette.SafetyOrange;
            orderResultScoreLabel.text = "+" + orderScore.ToString("D4") + "  POINTS EARNED";
            orderResultNextLabel.text = currentOrderIndex + 1 < OrderCount ? "NEXT WORKPIECE COMING UP" : "SHIFT SUMMARY NEXT";

            for (int i = 0; i < PhaseCount; i++)
            {
                TimingGrade grade = orderGrades[i];
                orderGradeChipImages[i].color = WithAlpha(GradeColor(grade), 0.22f);
                orderGradeChipValueLabels[i].text = GradeName(grade);
                orderGradeChipValueLabels[i].color = GradeColor(grade);
            }

            ShowOverlay(orderResultOverlayRoot, orderResultCardRect);
        }

        private void UpdatePresentationAnimation(float deltaTime)
        {
            float pulse = (Mathf.Sin(animationClock * 3.6f) + 1f) * 0.5f;

            if (scoreAnimationElapsed < ScoreCountUpSeconds)
            {
                scoreAnimationElapsed += deltaTime;
                float countProgress = Mathf.Clamp01(scoreAnimationElapsed / ScoreCountUpSeconds);
                float countEase = 1f - Mathf.Pow(1f - countProgress, 3f);
                displayedScore = Mathf.RoundToInt(Mathf.Lerp(scoreAnimationStart, scoreAnimationTarget, countEase));
            }
            else
            {
                displayedScore = scoreAnimationTarget;
            }

            if (scoreValueLabel != null)
                scoreValueLabel.text = displayedScore.ToString("D5");

            if (scoreFlashElapsed > 0f && scoreValueLabel != null)
            {
                scoreFlashElapsed = Mathf.Max(0f, scoreFlashElapsed - deltaTime);
                float flashProgress = 1f - scoreFlashElapsed / 0.3f;
                float scale = 1f + Mathf.Sin(flashProgress * Mathf.PI) * 0.12f;
                scoreValueLabel.rectTransform.localScale = Vector3.one * scale;
            }
            else if (scoreValueLabel != null)
            {
                scoreValueLabel.rectTransform.localScale = Vector3.one;
            }

            if (activeOverlayCanvasGroup != null)
            {
                overlayRevealElapsed += deltaTime;
                float revealProgress = Mathf.Clamp01(overlayRevealElapsed / OverlayRevealSeconds);
                activeOverlayCanvasGroup.alpha = Mathf.SmoothStep(0f, 1f, revealProgress);
                if (activeOverlayCardRect != null)
                {
                    float outBack = 1f + 2.70158f * Mathf.Pow(revealProgress - 1f, 3f) + 1.70158f * Mathf.Pow(revealProgress - 1f, 2f);
                    activeOverlayCardRect.localScale = Vector3.one * Mathf.LerpUnclamped(0.9f, 1f, outBack);
                }

                if (revealProgress >= 1f)
                {
                    activeOverlayCanvasGroup = null;
                    activeOverlayCardRect = null;
                }
            }
            float impactEnvelope = 0f;
            float impactProgress = 1f;

            if (impactAnimationActive)
            {
                impactElapsed += deltaTime;
                impactProgress = Mathf.Clamp01(impactElapsed / ImpactDurationSeconds);
                impactEnvelope = Mathf.Sin(impactProgress * Mathf.PI);
                if (impactProgress >= 1f)
                    impactAnimationActive = false;
            }

            float judgmentPop = 0f;
            if (judgmentPopActive)
            {
                judgmentPopElapsed += deltaTime;
                float popProgress = Mathf.Clamp01(judgmentPopElapsed / 0.24f);
                judgmentPop = Mathf.Sin(popProgress * Mathf.PI) * 0.16f;
                if (popProgress >= 1f)
                    judgmentPopActive = false;
            }
            judgmentRect.localScale = Vector3.one * (1f + judgmentPop);

            float idleBob = Mathf.Sin(animationClock * 1.65f) * 2.2f;
            float scalePulse = Mathf.Sin(animationClock * 1.65f) * 0.006f;
            float squash = 0f;
            float impactLift = 0f;
            float impactRotation = 0f;

            if (impactEnvelope > 0f)
            {
                if (impactGrade == TimingGrade.Miss)
                {
                    impactRotation = Mathf.Sin(impactElapsed * 68f) * 3.5f * (1f - impactProgress);
                    squash = -0.018f * impactEnvelope;
                }
                else
                {
                    float accuracyStrength = impactGrade == TimingGrade.Perfect ? 0.105f : 0.065f;
                    float phaseStrength = currentPhaseIndex == 1 ? 1.25f : 0.7f;
                    squash = accuracyStrength * phaseStrength * impactEnvelope;
                    impactLift = currentPhaseIndex == 1 ? 5f * impactEnvelope : 2.5f * impactEnvelope;
                    impactRotation = Mathf.Sin(impactProgress * Mathf.PI) * 1.2f;
                }

                workpieceImage.color = Color.Lerp(Color.white, impactColor, impactEnvelope * 0.16f);
            }
            else
            {
                workpieceImage.color = Color.white;
            }

            workpieceRect.anchoredPosition = workpieceBasePosition + new Vector2(0f, idleBob + impactLift);
            workpieceRect.localScale = new Vector3(1f + scalePulse + squash, 1f + scalePulse - squash * 0.7f, 1f);
            workpieceRect.localRotation = Quaternion.Euler(0f, 0f, impactRotation);

            Color glowColor = CurrentPhaseAccent();
            float glowAlpha;
            if (state == RoundState.Playing || state == RoundState.Resolving)
                glowAlpha = 0.065f + pulse * (currentPhaseIndex == 0 ? 0.12f : 0.07f);
            else if (state == RoundState.Ready)
                glowAlpha = 0.055f + pulse * 0.035f;
            else
                glowAlpha = 0.045f;

            glowAlpha += impactEnvelope * 0.1f;
            glowImage.color = WithAlpha(glowColor, glowAlpha);
            float glowScale = 0.96f + pulse * 0.055f + impactEnvelope * 0.07f;
            glowRect.localScale = new Vector3(glowScale, glowScale, 1f);

            if (state == RoundState.Playing)
                UpdateTimingCue(currentTimingProgress);
            else
            {
                targetBandImage.color = WithAlpha(FurnaceRhythmPalette.ElectricCyan, 0.52f + pulse * 0.1f);
                perfectBandImage.color = WithAlpha(FurnaceRhythmPalette.IndustrialCream, 0.78f + pulse * 0.12f);
                markerTrailImage.color = WithAlpha(FurnaceRhythmPalette.ElectricCyan, 0.08f + pulse * 0.05f);
                markerTrailRect.localScale = Vector3.one;
            }

            for (int i = 0; i < phaseIndicatorLabels.Length; i++)
                phaseIndicatorLabels[i].rectTransform.localScale = Vector3.one;

            if (state == RoundState.Playing && currentOrderIndex < orderProgressSegments.Length)
            {
                orderProgressSegments[currentOrderIndex].color = WithAlpha(FurnaceRhythmPalette.ElectricCyan, 0.7f + pulse * 0.28f);
                phaseIndicators[currentPhaseIndex].color = WithAlpha(CurrentPhaseAccent(), 0.72f + pulse * 0.2f);
                phaseIndicatorLabels[currentPhaseIndex].rectTransform.localScale = Vector3.one * (1f + pulse * 0.035f);
            }

            for (int i = 0; i < comboBonusPips.Length; i++)
            {
                if (i < Mathf.Min(currentStreak, comboBonusPips.Length))
                    comboBonusPips[i].color = WithAlpha(FurnaceRhythmPalette.SafetyOrange, 0.76f + pulse * 0.24f);
            }

            if (feedbackFlashElapsed > 0f && feedbackFlashImage != null)
            {
                feedbackFlashElapsed = Mathf.Max(0f, feedbackFlashElapsed - deltaTime);
                float flashProgress = 1f - feedbackFlashElapsed / feedbackFlashDuration;
                float alpha = (1f - flashProgress) * (1f - flashProgress) * feedbackFlashStrength;
                feedbackFlashImage.color = WithAlpha(feedbackFlashColor, alpha);
            }
            else if (feedbackFlashImage != null)
            {
                feedbackFlashImage.color = WithAlpha(feedbackFlashColor, 0f);
            }

            if (impactRingActive && impactRingImage != null)
            {
                impactRingElapsed += deltaTime;
                float ringProgress = Mathf.Clamp01(impactRingElapsed / ImpactRingDurationSeconds);
                float ringEase = 1f - Mathf.Pow(1f - ringProgress, 3f);
                float ringScale = Mathf.Lerp(0.42f, impactRingMaxScale, ringEase);
                impactRingRect.localScale = new Vector3(ringScale, ringScale, 1f);
                impactRingImage.color = WithAlpha(impactRingColor, (1f - ringProgress) * 0.82f);
                if (ringProgress >= 1f)
                    impactRingActive = false;
            }

            if (fadingOverlayCanvasGroup != null)
            {
                overlayFadeOutElapsed += deltaTime;
                float fadeProgress = Mathf.Clamp01(overlayFadeOutElapsed / OverlayFadeOutSeconds);
                fadingOverlayCanvasGroup.alpha = fadingOverlayStartAlpha * (1f - Mathf.SmoothStep(0f, 1f, fadeProgress));
                if (fadeProgress >= 1f)
                {
                    fadingOverlayCanvasGroup.gameObject.SetActive(false);
                    fadingOverlayCanvasGroup = null;
                }
            }

            if (sparkAnimationActive)
                UpdateSparks(deltaTime);
        }

        private void TriggerScreenFlash(TimingGrade grade)
        {
            feedbackFlashColor = GradeColor(grade);
            feedbackFlashDuration = grade == TimingGrade.Perfect ? 0.14f : grade == TimingGrade.Good ? 0.1f : 0.08f;
            feedbackFlashStrength = grade == TimingGrade.Perfect ? 0.2f : grade == TimingGrade.Good ? 0.13f : 0.1f;
            feedbackFlashElapsed = feedbackFlashDuration;
        }

        private void TriggerImpactRing(TimingGrade grade)
        {
            impactRingColor = GradeColor(grade);
            impactRingMaxScale = grade == TimingGrade.Perfect ? 1.75f : 1.38f;
            impactRingElapsed = 0f;
            impactRingActive = true;
            impactRingRect.localScale = Vector3.one * 0.42f;
            impactRingImage.color = WithAlpha(impactRingColor, 0.82f);
        }

        private void TriggerSparks(Color color)
        {
            sparkColor = color;
            sparkPhaseIndex = currentPhaseIndex;
            sparksElapsed = 0f;
            sparkAnimationActive = true;
            for (int i = 0; i < sparkImages.Length; i++)
            {
                if (sparkPhaseIndex == 0)
                {
                    float spread = ((i % 4) - 1.5f) * 0.16f;
                    sparkDirections[i] = new Vector2(spread, 1f).normalized;
                }
                else if (sparkPhaseIndex == 2)
                {
                    float spread = Mathf.Sin(i * 1.7f) * 0.7f;
                    sparkDirections[i] = new Vector2(spread, 1f).normalized;
                }
                else
                {
                    float angle = Mathf.PI * 2f * i / sparkImages.Length;
                    sparkDirections[i] = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                }

                sparkImages[i].color = WithAlpha(sparkColor, 0.95f);
                sparkRects[i].anchoredPosition = sparkOriginPosition + sparkDirections[i] * 18f;
                sparkRects[i].localScale = Vector3.one;
            }
        }

        private void UpdateSparks(float deltaTime)
        {
            sparksElapsed += deltaTime;
            float progress = Mathf.Clamp01(sparksElapsed / SparkDurationSeconds);
            float distance = sparkPhaseIndex == 2 ? Mathf.Lerp(8f, 50f, progress) : Mathf.Lerp(18f, 98f, progress);
            float alpha = (1f - progress) * (1f - progress * 0.2f);
            float scale = sparkPhaseIndex == 2 ? 0.75f + progress * 0.6f : 1f - progress * 0.45f;

            for (int i = 0; i < sparkImages.Length; i++)
            {
                Vector2 direction = sparkDirections[i];
                Vector2 offset = direction * distance;
                if (sparkPhaseIndex == 2)
                    offset += Vector2.up * (34f * progress);
                sparkRects[i].anchoredPosition = sparkOriginPosition + offset;
                sparkRects[i].localScale = new Vector3(scale, scale, 1f);
                float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f;
                sparkRects[i].localRotation = Quaternion.Euler(0f, 0f, angle);
                sparkImages[i].color = WithAlpha(sparkColor, alpha * 0.95f);
            }

            if (progress >= 1f)
                sparkAnimationActive = false;
        }

        private Color CurrentPhaseAccent()
        {
            return CurrentPhaseAccent(currentPhaseIndex);
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
            float distance = Mathf.Abs(progress - targetCenter);
            float anticipation = 1f - Mathf.Clamp01(distance / (goodWindow * 2.6f));
            float pulse = (Mathf.Sin(animationClock * 8f) + 1f) * 0.5f;
            targetBandImage.color = WithAlpha(FurnaceRhythmPalette.ElectricCyan, 0.48f + anticipation * 0.2f + pulse * 0.1f);
            perfectBandImage.color = WithAlpha(FurnaceRhythmPalette.IndustrialCream, 0.7f + anticipation * 0.22f + pulse * 0.08f);
            markerTrailImage.color = WithAlpha(CurrentPhaseAccent(), 0.12f + anticipation * 0.2f);
            markerTrailRect.localScale = new Vector3(0.85f + anticipation * 0.32f, 1f, 1f);

            if (distance <= goodWindow)
                markerImage.color = FurnaceRhythmPalette.SafetyOrange;
            else if (distance <= goodWindow * 2f)
                markerImage.color = FurnaceRhythmPalette.ElectricCyan;
            else
                markerImage.color = FurnaceRhythmPalette.IndustrialCream;
        }

        private void RefreshPhaseIndicators()
        {
            for (int i = 0; i < phaseIndicators.Length; i++)
            {
                if (state == RoundState.OrderResult || state == RoundState.ShiftResult ||
                    i < currentPhaseIndex || (state == RoundState.Resolving && i == currentPhaseIndex))
                {
                    phaseIndicators[i].color = WithAlpha(GradeColor(orderGrades[i]), 0.78f);
                    phaseIndicatorLabels[i].color = FurnaceRhythmPalette.IndustrialCream;
                }
                else if (i == currentPhaseIndex && state == RoundState.Playing)
                {
                    phaseIndicators[i].color = WithAlpha(CurrentPhaseAccent(), 0.92f);
                    phaseIndicatorLabels[i].color = FurnaceRhythmPalette.MidnightIndustrial;
                }
                else
                {
                    phaseIndicators[i].color = WithAlpha(FurnaceRhythmPalette.SlateMachinery, 0.9f);
                    phaseIndicatorLabels[i].color = FurnaceRhythmPalette.IndustrialCream;
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

            glowImage = CreateImage(canvas.transform, "Forge Glow", WithAlpha(FurnaceRhythmPalette.SafetyOrange, 0.08f));
            glowSprite = CreateSoftGlowSprite(out glowTexture);
            glowImage.sprite = glowSprite;
            glowImage.preserveAspect = true;
            glowRect = glowImage.rectTransform;
            SetCenteredRect(glowRect, new Vector2(0.5f, 0.5f), workpieceBasePosition, new Vector2(780f, 520f));

            impactRingImage = CreateImage(canvas.transform, "Judgment Impact Ring", WithAlpha(FurnaceRhythmPalette.ElectricCyan, 0f));
            impactRingImage.sprite = GetImpactRingSprite();
            impactRingImage.preserveAspect = true;
            impactRingRect = impactRingImage.rectTransform;
            SetCenteredRect(impactRingRect, new Vector2(0.5f, 0.5f), sparkOriginPosition, new Vector2(190f, 190f));

            workpieceImage = CreateImage(canvas.transform, "Hot Billet Workpiece", Color.white);
            workpieceImage.sprite = hotBilletSprite;
            workpieceImage.preserveAspect = true;
            workpieceImage.color = Color.white;
            SetCenteredRect(workpieceImage.rectTransform, new Vector2(0.5f, 0.5f), workpieceBasePosition, new Vector2(650f, 432f));
            workpieceRect = workpieceImage.rectTransform;
            if (hotBilletSprite == null)
                CreateText(canvas.transform, "Workpiece Fallback", "HOT BILLET", 22f, FurnaceRhythmPalette.SafetyOrange,
                    new Vector2(0.5f, 0.5f), new Vector2(0f, 34f), new Vector2(400f, 44f), FontStyles.Bold);

            for (int i = 0; i < sparkImages.Length; i++)
            {
                float angle = Mathf.PI * 2f * i / sparkImages.Length;
                sparkDirections[i] = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                sparkImages[i] = CreateImage(canvas.transform, "Forge Spark " + (i + 1), WithAlpha(FurnaceRhythmPalette.IndustrialCream, 0f));
                sparkRects[i] = sparkImages[i].rectTransform;
                SetCenteredRect(sparkRects[i], new Vector2(0.5f, 0.5f), sparkOriginPosition, i % 2 == 0 ? new Vector2(7f, 18f) : new Vector2(5f, 13f));
                sparkRects[i].localRotation = Quaternion.Euler(0f, 0f, angle * Mathf.Rad2Deg - 90f);
            }

            BuildScoreHud(canvas.transform);
            BuildOrderProgress(canvas.transform);

            phaseLabel = CreateText(canvas.transform, "Current Phase", string.Empty, 24f, FurnaceRhythmPalette.IndustrialCream,
                new Vector2(0.5f, 0.5f), new Vector2(0f, -194f), new Vector2(980f, 42f), FontStyles.Bold);
            BuildTimingTrack(canvas.transform);
            judgmentLabel = CreateText(canvas.transform, "Timing Judgment", string.Empty, 30f, FurnaceRhythmPalette.IndustrialCream,
                new Vector2(0.5f, 0.5f), new Vector2(0f, -327f), new Vector2(1320f, 52f), FontStyles.Bold);
            judgmentRect = judgmentLabel.rectTransform;

            for (int i = 0; i < phaseIndicators.Length; i++)
            {
                phaseIndicators[i] = CreateImage(canvas.transform, "Beat Indicator " + (i + 1), WithAlpha(FurnaceRhythmPalette.SlateMachinery, 0.9f));
                phaseIndicators[i].sprite = GetRoundedPanelSprite();
                phaseIndicators[i].type = UnityEngine.UI.Image.Type.Sliced;
                SetCenteredRect(phaseIndicators[i].rectTransform, new Vector2(0.5f, 0.5f), new Vector2((i - 1) * 154f, -389f), new Vector2(136f, 34f));
                BuildPhaseGlyph(phaseIndicators[i].transform, i);
                phaseIndicatorLabels[i] = CreateText(phaseIndicators[i].transform, "Phase Name", PhaseNames[i], 13f,
                    FurnaceRhythmPalette.IndustrialCream, new Vector2(0.5f, 0.5f), new Vector2(13f, 0f), new Vector2(96f, 22f), FontStyles.Bold);
            }

            instructionLabel = CreateText(canvas.transform, "Input Instruction", string.Empty, 16f, FurnaceRhythmPalette.IndustrialCream,
                new Vector2(0.5f, 0f), new Vector2(0f, 52f), new Vector2(1500f, 32f), FontStyles.Bold);
            CreateText(canvas.transform, "Input Hint", "SPACE / CLICK  ·  PERFECT +100  ·  GOOD +60  ·  COMBO BONUS CAPS AT +50", 12f, FurnaceRhythmPalette.OxidizedTeal,
                new Vector2(0.5f, 0f), new Vector2(0f, 24f), new Vector2(700f, 24f), FontStyles.Normal);

            feedbackFlashImage = CreateImage(canvas.transform, "Judgment Screen Flash", WithAlpha(FurnaceRhythmPalette.IndustrialCream, 0f));
            StretchToParent(feedbackFlashImage.rectTransform);

            BuildStartOverlay(canvas.transform);
            BuildOrderResultOverlay(canvas.transform);
            BuildShiftResultOverlay(canvas.transform);
        }

        private void BuildScoreHud(Transform parent)
        {
            TextMeshProUGUI unusedDetail;
            BuildScoreCard(parent, "Score Card", "SHIFT SCORE", FurnaceRhythmPalette.ElectricCyan, FurnaceRhythmPalette.ElectricCyan, -370f,
                out scoreValueLabel, out unusedDetail);
            BuildScoreCard(parent, "Combo Card", "CURRENT STREAK", FurnaceRhythmPalette.SafetyOrange, FurnaceRhythmPalette.IndustrialCream, 0f,
                out streakValueLabel, out comboBonusLabel);
            BuildScoreCard(parent, "Order Card", "WORKPIECE", FurnaceRhythmPalette.OxidizedTeal, FurnaceRhythmPalette.IndustrialCream, 370f,
                out orderValueLabel, out unusedDetail);
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
                for (int i = 0; i < comboBonusPips.Length; i++)
                {
                    comboBonusPips[i] = CreateImage(card, "Combo Bonus Pip " + (i + 1),
                        WithAlpha(FurnaceRhythmPalette.SlateMachinery, 0.86f));
                    comboBonusPips[i].sprite = GetRoundedPanelSprite();
                    comboBonusPips[i].type = UnityEngine.UI.Image.Type.Sliced;
                    SetCenteredRect(comboBonusPips[i].rectTransform, new Vector2(0.5f, 0.5f),
                        new Vector2(-53f + i * 22f, -29f), new Vector2(16f, 5f));
                }
            }
        }

        private void BuildOrderProgress(Transform parent)
        {
            CreateText(parent, "Shift Progress Label", "SHIFT PROGRESS", 10f, FurnaceRhythmPalette.OxidizedTeal,
                new Vector2(0.5f, 1f), new Vector2(0f, -267f), new Vector2(320f, 16f), FontStyles.Bold);

            for (int i = 0; i < orderProgressSegments.Length; i++)
            {
                orderProgressSegments[i] = CreateImage(parent, "Workpiece Progress " + (i + 1),
                    WithAlpha(FurnaceRhythmPalette.SlateMachinery, 0.82f));
                orderProgressSegments[i].sprite = GetRoundedPanelSprite();
                orderProgressSegments[i].type = UnityEngine.UI.Image.Type.Sliced;
                SetCenteredRect(orderProgressSegments[i].rectTransform, new Vector2(0.5f, 1f),
                    new Vector2((i - (OrderCount - 1) * 0.5f) * 134f, -291f), new Vector2(118f, 10f));
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
            startOverlayRoot = CreateOverlayRoot(parent, "Shift Start Overlay", 0.34f);
            startOverlayCardRect = CreateRoundedPanel(startOverlayRoot.transform, "Shift Start Card", new Vector2(0.5f, 0.5f),
                new Vector2(0f, -12f), new Vector2(760f, 270f), FurnaceRhythmPalette.ElectricCyan);

            CreateText(startOverlayCardRect, "Start Card Eyebrow", "THE FURNACE IS READY", 13f, FurnaceRhythmPalette.OxidizedTeal,
                new Vector2(0.5f, 0.5f), new Vector2(0f, 86f), new Vector2(640f, 24f), FontStyles.Bold);
            CreateText(startOverlayCardRect, "Start Card Title", "FORGE FIVE WORKPIECES", 32f, FurnaceRhythmPalette.IndustrialCream,
                new Vector2(0.5f, 0.5f), new Vector2(0f, 41f), new Vector2(690f, 46f), FontStyles.Bold);
            CreateText(startOverlayCardRect, "Start Card Phases", "HEAT     ·     HAMMER     ·     QUENCH", 16f, FurnaceRhythmPalette.ElectricCyan,
                new Vector2(0.5f, 0.5f), new Vector2(0f, -9f), new Vector2(660f, 30f), FontStyles.Bold);
            CreateText(startOverlayCardRect, "Start Card Scoring", "PERFECT 100  /  GOOD 60  /  STREAK BONUS UP TO +50", 12f,
                FurnaceRhythmPalette.OxidizedTeal, new Vector2(0.5f, 0.5f), new Vector2(0f, -55f), new Vector2(670f, 22f), FontStyles.Bold);
            CreateText(startOverlayCardRect, "Start Card Prompt", "PRESS SPACE OR CLICK TO START", 15f, FurnaceRhythmPalette.IndustrialCream,
                new Vector2(0.5f, 0.5f), new Vector2(0f, -98f), new Vector2(650f, 26f), FontStyles.Bold);
        }

        private void BuildOrderResultOverlay(Transform parent)
        {
            orderResultOverlayRoot = CreateOverlayRoot(parent, "Workpiece Quality Overlay", 0.72f);
            orderResultCardRect = CreateRoundedPanel(orderResultOverlayRoot.transform, "Workpiece Quality Card", new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(930f, 430f), FurnaceRhythmPalette.SafetyOrange);

            orderResultHeaderLabel = CreateText(orderResultCardRect, "Quality Card Header", "WORKPIECE  01 / 05", 13f,
                FurnaceRhythmPalette.OxidizedTeal, new Vector2(0.5f, 0.5f), new Vector2(0f, 163f), new Vector2(780f, 24f), FontStyles.Bold);
            orderResultQualityLabel = CreateText(orderResultCardRect, "Quality Card Grade", "FINE WORK", 38f,
                FurnaceRhythmPalette.IndustrialCream, new Vector2(0.5f, 0.5f), new Vector2(0f, 105f), new Vector2(820f, 52f), FontStyles.Bold);
            orderResultScoreLabel = CreateText(orderResultCardRect, "Quality Card Points", "+0000  POINTS EARNED", 18f,
                FurnaceRhythmPalette.ElectricCyan, new Vector2(0.5f, 0.5f), new Vector2(0f, 53f), new Vector2(760f, 30f), FontStyles.Bold);

            for (int i = 0; i < PhaseCount; i++)
            {
                UnityEngine.UI.Image chip = CreateImage(orderResultCardRect, "Grade Chip " + PhaseNames[i],
                    WithAlpha(FurnaceRhythmPalette.SlateMachinery, 0.9f));
                chip.sprite = GetRoundedPanelSprite();
                chip.type = UnityEngine.UI.Image.Type.Sliced;
                SetCenteredRect(chip.rectTransform, new Vector2(0.5f, 0.5f), new Vector2((i - 1) * 242f, -40f),
                    new Vector2(218f, 76f));
                orderGradeChipImages[i] = chip;
                CreateText(chip.transform, "Grade Chip Phase", PhaseNames[i], 11f, FurnaceRhythmPalette.OxidizedTeal,
                    new Vector2(0.5f, 0.5f), new Vector2(0f, 15f), new Vector2(192f, 18f), FontStyles.Bold);
                orderGradeChipValueLabels[i] = CreateText(chip.transform, "Grade Chip Result", "MISS", 17f,
                    FurnaceRhythmPalette.IndustrialCream, new Vector2(0.5f, 0.5f), new Vector2(0f, -12f), new Vector2(192f, 24f), FontStyles.Bold);
            }

            orderResultNextLabel = CreateText(orderResultCardRect, "Quality Card Next", "NEXT WORKPIECE COMING UP", 12f,
                FurnaceRhythmPalette.OxidizedTeal, new Vector2(0.5f, 0.5f), new Vector2(0f, -151f), new Vector2(740f, 22f), FontStyles.Bold);
        }

        private void BuildShiftResultOverlay(Transform parent)
        {
            shiftResultOverlayRoot = CreateOverlayRoot(parent, "Shift Summary Overlay", 0.82f);
            shiftResultCardRect = CreateRoundedPanel(shiftResultOverlayRoot.transform, "Shift Summary Card", new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(930f, 470f), FurnaceRhythmPalette.ElectricCyan);

            CreateText(shiftResultCardRect, "Shift Summary Eyebrow", "SHIFT COMPLETE", 13f, FurnaceRhythmPalette.OxidizedTeal,
                new Vector2(0.5f, 0.5f), new Vector2(0f, 175f), new Vector2(780f, 24f), FontStyles.Bold);
            shiftResultRankLabel = CreateText(shiftResultCardRect, "Shift Summary Rank", "SKILLED SMITH", 34f,
                FurnaceRhythmPalette.IndustrialCream, new Vector2(0.5f, 0.5f), new Vector2(0f, 116f), new Vector2(820f, 50f), FontStyles.Bold);
            shiftResultScoreLabel = CreateText(shiftResultCardRect, "Shift Summary Score", "00000  POINTS", 48f,
                FurnaceRhythmPalette.ElectricCyan, new Vector2(0.5f, 0.5f), new Vector2(0f, 47f), new Vector2(800f, 65f), FontStyles.Bold);
            shiftResultStatsLabel = CreateText(shiftResultCardRect, "Shift Summary Grades", "PERFECT  00     GOOD  00     MISS  00", 16f,
                FurnaceRhythmPalette.IndustrialCream, new Vector2(0.5f, 0.5f), new Vector2(0f, -43f), new Vector2(800f, 30f), FontStyles.Bold);
            shiftResultStreakLabel = CreateText(shiftResultCardRect, "Shift Summary Streak", "BEST STREAK  00  ·  FIVE WORKPIECES FORGED", 12f,
                FurnaceRhythmPalette.OxidizedTeal, new Vector2(0.5f, 0.5f), new Vector2(0f, -88f), new Vector2(820f, 24f), FontStyles.Bold);
            CreateText(shiftResultCardRect, "Shift Summary Restart", "PRESS SPACE OR CLICK TO FORGE AGAIN", 14f,
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
            if (fadingOverlayCanvasGroup == canvasGroup)
                fadingOverlayCanvasGroup = null;
            overlay.SetActive(true);
            activeOverlayCanvasGroup = canvasGroup;
            activeOverlayCanvasGroup.alpha = 0f;
            activeOverlayCardRect = card;
            if (activeOverlayCardRect != null)
                activeOverlayCardRect.localScale = Vector3.one * 0.9f;
            overlayRevealElapsed = 0f;
        }

        private void HideOverlay(GameObject overlay)
        {
            if (overlay == null || !overlay.activeSelf)
                return;

            CanvasGroup canvasGroup = overlay.GetComponent<CanvasGroup>();
            if (activeOverlayCanvasGroup == canvasGroup)
            {
                activeOverlayCanvasGroup = null;
                activeOverlayCardRect = null;
            }

            if (fadingOverlayCanvasGroup != null && fadingOverlayCanvasGroup != canvasGroup)
                fadingOverlayCanvasGroup.gameObject.SetActive(false);
            fadingOverlayCanvasGroup = canvasGroup;
            fadingOverlayStartAlpha = canvasGroup.alpha;
            overlayFadeOutElapsed = 0f;
        }

        private void BuildTimingTrack(Transform parent)
        {
            UnityEngine.UI.Image track = CreateImage(parent, "Timing Track", WithAlpha(FurnaceRhythmPalette.SlateMachinery, 0.95f));
            SetCenteredRect(track.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, -260f), new Vector2(1000f, 26f));
            timingTrackRect = track.rectTransform;

            targetBandImage = CreateImage(track.transform, "Cyan Target Band", WithAlpha(FurnaceRhythmPalette.ElectricCyan, 0.72f));
            RectTransform targetRect = targetBandImage.rectTransform;
            targetRect.anchorMin = new Vector2(targetCenter - goodWindow, 0f);
            targetRect.anchorMax = new Vector2(targetCenter + goodWindow, 1f);
            targetRect.pivot = new Vector2(0.5f, 0.5f);
            targetRect.offsetMin = Vector2.zero;
            targetRect.offsetMax = Vector2.zero;

            perfectBandImage = CreateImage(track.transform, "Perfect Target Center", WithAlpha(FurnaceRhythmPalette.IndustrialCream, 0.88f));
            RectTransform perfectRect = perfectBandImage.rectTransform;
            perfectRect.anchorMin = new Vector2(targetCenter - perfectWindow, 0f);
            perfectRect.anchorMax = new Vector2(targetCenter + perfectWindow, 1f);
            perfectRect.pivot = new Vector2(0.5f, 0.5f);
            perfectRect.offsetMin = Vector2.zero;
            perfectRect.offsetMax = Vector2.zero;

            markerTrailImage = CreateImage(track.transform, "Timing Marker Trail", WithAlpha(FurnaceRhythmPalette.ElectricCyan, 0.12f));
            markerTrailImage.sprite = GetRoundedPanelSprite();
            markerTrailImage.type = UnityEngine.UI.Image.Type.Sliced;
            markerTrailRect = markerTrailImage.rectTransform;
            markerTrailRect.anchorMin = new Vector2(0.5f, 0.5f);
            markerTrailRect.anchorMax = new Vector2(0.5f, 0.5f);
            markerTrailRect.pivot = new Vector2(1f, 0.5f);
            markerTrailRect.sizeDelta = new Vector2(58f, 8f);
            markerTrailRect.anchoredPosition = new Vector2(-timingTrackRect.rect.width * 0.5f, 0f);

            UnityEngine.UI.Image marker = CreateImage(track.transform, "Timing Marker", FurnaceRhythmPalette.IndustrialCream);
            markerRect = marker.rectTransform;
            markerImage = marker;
            markerRect.anchorMin = new Vector2(0.5f, 0.5f);
            markerRect.anchorMax = new Vector2(0.5f, 0.5f);
            markerRect.pivot = new Vector2(0.5f, 0.5f);
            markerRect.sizeDelta = new Vector2(10f, 64f);
            markerRect.anchoredPosition = new Vector2(-timingTrackRect.rect.width * 0.5f, 0f);
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
                font = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");

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
            if (whiteSprite == null)
                whiteSprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f));
            return whiteSprite;
        }

        private static Sprite GetRoundedPanelSprite()
        {
            if (roundedPanelSprite != null)
                return roundedPanelSprite;

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
            roundedPanelSprite = Sprite.Create(texture, new Rect(0f, 0f, textureSize, textureSize), new Vector2(0.5f, 0.5f),
                textureSize, 0, SpriteMeshType.FullRect, new Vector4(16f, 16f, 16f, 16f));
            roundedPanelSprite.name = "Furnace Rhythm Rounded Panel Sprite";
            roundedPanelSprite.hideFlags = HideFlags.HideAndDontSave;
            return roundedPanelSprite;
        }

        private static Sprite GetImpactRingSprite()
        {
            if (impactRingSprite != null)
                return impactRingSprite;

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
            impactRingSprite = Sprite.Create(texture, new Rect(0f, 0f, textureSize, textureSize), new Vector2(0.5f, 0.5f), textureSize);
            impactRingSprite.name = "Furnace Rhythm Impact Ring Sprite";
            impactRingSprite.hideFlags = HideFlags.HideAndDontSave;
            return impactRingSprite;
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

        private static bool ActionPressedThisFrame()
        {
            Keyboard keyboard = Keyboard.current;
            Mouse mouse = Mouse.current;
            return (keyboard != null && keyboard.spaceKey.wasPressedThisFrame) ||
                   (mouse != null && mouse.leftButton.wasPressedThisFrame);
        }
    }
}
