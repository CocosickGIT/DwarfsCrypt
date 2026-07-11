using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace DwarfsCrypt.Presentation.Diagnostics
{
    /// <summary>
    /// On-screen frame-rate overlay. Self-bootstrapping: a single persistent instance is created
    /// automatically at startup (no scene wiring), builds its own screen-space canvas + TMP label,
    /// and survives scene loads. Shows smoothed FPS and frame time, colour-coded by performance.
    ///
    /// Toggle visibility at runtime with the F3 key on desktop, or call <see cref="SetVisible"/>
    /// from a button for mobile.
    /// </summary>
    public class FpsDisplay : MonoBehaviour
    {
        // How often the displayed value refreshes, in unscaled seconds. The FPS shown is the average
        // over this window, so it stays readable instead of flickering every frame.
        private const float SampleInterval = 0.5f;

        // Colour thresholds (frames per second).
        private const float GoodFps = 55f; // >= this -> green
        private const float OkFps = 30f;   // >= this -> yellow, below -> red

        private static readonly Color GoodColor = new(0.45f, 1f, 0.45f);
        private static readonly Color OkColor = new(1f, 0.85f, 0.3f);
        private static readonly Color BadColor = new(1f, 0.4f, 0.4f);

        private TMP_Text _label;
        private int _frames;
        private float _elapsed;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        /// <summary>
        /// Spawns the overlay once the game starts, before the first scene's Awake runs.
        /// Compiled only in the editor and development builds, so release builds never show it
        /// nor pay any overhead.
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Bootstrap()
        {
            var go = new GameObject("FpsDisplay");
            DontDestroyOnLoad(go);
            go.AddComponent<FpsDisplay>();
        }
#endif

        private void Awake()
        {
            BuildUi();
        }

        private void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard != null && keyboard.f3Key.wasPressedThisFrame)
                _label.transform.parent.gameObject.SetActive(!_label.transform.parent.gameObject.activeSelf);

            _frames++;
            _elapsed += Time.unscaledDeltaTime;
            if (_elapsed < SampleInterval) return;

            float fps = _frames / _elapsed;
            float ms = 1000f / Mathf.Max(fps, 0.0001f);
            _label.text = $"{fps:0} FPS  ({ms:0.0} ms)";
            _label.color = fps >= GoodFps ? GoodColor : fps >= OkFps ? OkColor : BadColor;

            _frames = 0;
            _elapsed = 0f;
        }

        /// <summary>Show or hide the overlay (e.g. wired to a settings toggle on mobile).</summary>
        public void SetVisible(bool visible) =>
            _label.transform.parent.gameObject.SetActive(visible);

        private void BuildUi()
        {
            // Dedicated high-sorting overlay canvas so the counter draws above every other UI.
            // No GraphicRaycaster: the label is non-interactive and must not eat HUD touches/clicks.
            var canvasGo = new GameObject("FpsCanvas",
                typeof(Canvas), typeof(UnityEngine.UI.CanvasScaler));
            canvasGo.transform.SetParent(transform, false);

            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = short.MaxValue; // always on top

            var scaler = canvasGo.GetComponent<UnityEngine.UI.CanvasScaler>();
            scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 1f; // match height (landscape-locked game)

            var textGo = new GameObject("FpsLabel", typeof(TextMeshProUGUI));
            textGo.transform.SetParent(canvasGo.transform, false);

            _label = textGo.GetComponent<TextMeshProUGUI>();
            _label.fontSize = 34f;
            _label.alignment = TextAlignmentOptions.TopRight;
            _label.raycastTarget = false;
            _label.text = "-- FPS";

            // Anchor to the top-right corner with a small inset from the screen edge.
            var rect = _label.rectTransform;
            rect.anchorMin = new Vector2(1f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(1f, 1f);
            rect.anchoredPosition = new Vector2(-24f, -16f);
            rect.sizeDelta = new Vector2(320f, 48f);
        }
    }
}
