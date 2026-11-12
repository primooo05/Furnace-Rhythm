using System;
using UnityEngine;

namespace FurnaceRhythm.Config
{
    /// <summary>Editable timing and result durations for a Furnace Rhythm shift.</summary>
    [CreateAssetMenu(menuName = "Furnace Rhythm/Shift Tuning", fileName = "FurnaceRhythmTuning")]
    public sealed class FurnaceRhythmTuning : ScriptableObject
    {
        [SerializeField, Min(0.6f)] private float beatDurationSeconds = 1.7f;
        [SerializeField, Range(0.4f, 0.9f)] private float targetCenter = 0.68f;
        [SerializeField, Range(0.01f, 0.12f)] private float perfectWindow = 0.045f;
        [SerializeField, Range(0.06f, 0.24f)] private float goodWindow = 0.12f;
        [SerializeField, Min(0.1f)] private float judgmentHoldSeconds = 0.5f;
        [SerializeField, Min(0.2f)] private float orderResultHoldSeconds = 1.1f;
        [SerializeField] private Sprite hotBilletSprite;

        /// <summary>Gets the duration of each timing sweep in seconds.</summary>
        public float BeatDurationSeconds => beatDurationSeconds;
        /// <summary>Gets the normalized center of the target window.</summary>
        public float TargetCenter => targetCenter;
        /// <summary>Gets the half-width of the Perfect region.</summary>
        public float PerfectWindow => perfectWindow;
        /// <summary>Gets the half-width of the Good region.</summary>
        public float GoodWindow => goodWindow;
        /// <summary>Gets the delay before the next beat begins.</summary>
        public float JudgmentHoldSeconds => judgmentHoldSeconds;
        /// <summary>Gets the delay before the next workpiece begins.</summary>
        public float OrderResultHoldSeconds => orderResultHoldSeconds;
        /// <summary>Gets the billet art shown by the presentation layer.</summary>
        public Sprite HotBilletSprite => hotBilletSprite;

        /// <summary>Assigns the billet art to this configuration asset.</summary>
        /// <param name="sprite">Imported sprite displayed by the presentation layer.</param>
        /// <exception cref="ArgumentNullException">The sprite is null.</exception>
        public void SetHotBilletSprite(Sprite sprite)
        {
            if (sprite == null)
                throw new ArgumentNullException(nameof(sprite));

            hotBilletSprite = sprite;
        }
    }
}
