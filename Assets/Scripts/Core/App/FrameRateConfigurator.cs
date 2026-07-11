using UnityEngine;

namespace Core.App
{
    /// <summary>
    /// Forces the target frame rate at startup to the display's native refresh rate. Without this,
    /// Unity caps Android/iOS builds at 30 FPS by default. Runs automatically (no scene wiring)
    /// before the first scene loads, so high-refresh (90/120 Hz) screens run at full rate.
    ///
    /// Note: <see cref="Application.targetFrameRate"/> is ignored while VSync is on, and several of
    /// the project's quality levels ship with <c>vSyncCount: 1</c>, so we disable VSync here too —
    /// otherwise the frame rate would silently fall back to the display refresh / VSync interval.
    /// </summary>
    public static class FrameRateConfigurator
    {
        /// <summary>Used only if the display refresh rate can't be read (reports 0).</summary>
        private const int FallbackFps = 60;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Apply()
        {
            QualitySettings.vSyncCount = 0; // required, or targetFrameRate is ignored

            int refreshHz = Mathf.RoundToInt((float)Screen.currentResolution.refreshRateRatio.value);
            Application.targetFrameRate = refreshHz > 0 ? refreshHz : FallbackFps;
        }
    }
}
