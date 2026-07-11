using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DwarfsCrypt.Presentation.Player
{
    /// <summary>
    /// Buff-icon style cooldown overlay for a single HUD ability button (dash / skills).
    /// When the ability is used, the icon dims to the cooldown tint and a Filled overlay
    /// image sweeps its fillAmount from 0 → 1 over the cooldown duration; when it reaches 1
    /// the button returns to the ready look. Purely cosmetic — the real cooldown gating still
    /// lives in PlayerController / SkillCaster, this just mirrors its duration.
    /// </summary>
    [DisallowMultipleComponent]
    public class CooldownButtonView : MonoBehaviour
    {
        [Header("Visuals")]
        [Tooltip("Overlay Image set to Image Type = Filled (e.g. Radial360). Its fillAmount animates 0 → 1 as the ability recharges.")]
        [SerializeField] private Image _fillImage;
        [Tooltip("The ability icon whose color is tinted while recharging.")]
        [SerializeField] private Image _iconImage;
        [Tooltip("Optional button greyed-out (non-interactable) while recharging.")]
        [SerializeField] private Button _button;
        [Tooltip("Optional label showing the remaining cooldown seconds; hidden when ready.")]
        [SerializeField] private TMP_Text _countdownLabel;

        [Header("Colors")]
        [Tooltip("Icon tint when the ability is ready.")]
        [SerializeField] private Color _readyColor = Color.white;
        [Tooltip("Icon tint while the ability is on cooldown.")]
        [SerializeField] private Color _cooldownColor = new Color(0.4f, 0.4f, 0.4f, 1f);

        [Header("Countdown text")]
        [Tooltip("Show one decimal (e.g. \"2.4\") under this threshold, whole seconds above it.")]
        [SerializeField] private float _decimalThreshold = 1f;

        private Coroutine _routine;

        private void Awake() => SetReadyLook();

        /// <summary>Start the cooldown sweep over <paramref name="duration"/> seconds.</summary>
        public void Play(float duration)
        {
            if (!isActiveAndEnabled || duration <= 0f)
            {
                SetReadyLook();
                return;
            }

            if (_routine != null) StopCoroutine(_routine);
            _routine = StartCoroutine(CooldownRoutine(duration));
        }

        private IEnumerator CooldownRoutine(float duration)
        {
            if (_iconImage != null) _iconImage.color = _cooldownColor;
            if (_button != null) _button.interactable = false;
            if (_fillImage != null) _fillImage.fillAmount = 0f;
            if (_countdownLabel != null) _countdownLabel.gameObject.SetActive(true);

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                if (_fillImage != null)
                    _fillImage.fillAmount = Mathf.Clamp01(elapsed / duration); // 0 → 1

                if (_countdownLabel != null)
                {
                    float remaining = Mathf.Max(0f, duration - elapsed);
                    _countdownLabel.text = remaining > _decimalThreshold
                        ? Mathf.CeilToInt(remaining).ToString()
                        : remaining.ToString("0.0");
                }

                yield return null;
            }

            SetReadyLook();
            _routine = null;
        }

        private void SetReadyLook()
        {
            if (_fillImage != null) _fillImage.fillAmount = 1f;
            if (_iconImage != null) _iconImage.color = _readyColor;
            if (_button != null) _button.interactable = true;
            if (_countdownLabel != null) _countdownLabel.gameObject.SetActive(false);
        }
    }
}
