using FurnaceRhythm.Behaviors;
using FurnaceRhythm.Config;
using FurnaceRhythm.Domain;
using FurnaceRhythm.Presentation;
using UnityEngine;

namespace FurnaceRhythm
{
    /// <summary>Wires the shift controller and its presentation view for the sample scene.</summary>
    [DisallowMultipleComponent]
    public sealed class FurnaceRhythmGame : MonoBehaviour
    {
        [Header("Shift configuration")]
        [SerializeField] private FurnaceRhythmTuning tuning;
        [SerializeField] private Transform presentationParent;

        private void Awake()
        {
            FurnaceShiftSettings settings = tuning != null
                ? new FurnaceShiftSettings(
                    tuning.BeatDurationSeconds,
                    tuning.TargetCenter,
                    tuning.PerfectWindow,
                    tuning.GoodWindow,
                    tuning.JudgmentHoldSeconds,
                    tuning.OrderResultHoldSeconds)
                : FurnaceShiftSettings.Default;

            GameObject presentationObject = new GameObject("Furnace Rhythm Presentation");
            presentationObject.transform.SetParent(presentationParent != null ? presentationParent : transform, false);
            presentationObject.SetActive(false);
            FurnaceRhythmPresenter presenter = presentationObject.AddComponent<FurnaceRhythmPresenter>();
            presenter.Configure(tuning, settings.TargetCenter, settings.PerfectWindow, settings.GoodWindow);
            presentationObject.SetActive(true);

            FurnaceRhythmController controller = GetComponent<FurnaceRhythmController>();
            if (controller == null)
                controller = gameObject.AddComponent<FurnaceRhythmController>();
            controller.Initialize(settings);
        }

        /// <summary>Assigns the timing and art configuration before the scene is saved.</summary>
        /// <param name="shiftTuning">Configuration asset, or null to use domain defaults.</param>
        public void SetTuning(FurnaceRhythmTuning shiftTuning)
        {
            tuning = shiftTuning;
        }

        /// <summary>Assigns the scene object that organizes runtime presentation objects.</summary>
        /// <param name="parent">The UI hierarchy transform, or null to parent under this object.</param>
        public void SetPresentationParent(Transform parent)
        {
            presentationParent = parent;
        }
    }
}
