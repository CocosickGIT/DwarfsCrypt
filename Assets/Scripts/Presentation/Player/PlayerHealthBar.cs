using TMPro;
using UnityEngine;
using UnityEngine.UI; // Slider

namespace DwarfsCrypt.Presentation.Player
{
    /// <summary>
    /// Drives an on-screen HP bar from the spawned player's health. Build the bar visuals
    /// yourself (a Slider and/or a label) and assign them here. The component finds the
    /// <see cref="PlayerController"/> at runtime, binds to its
    /// <see cref="PlayerController.HealthChanged"/> event, and keeps the slider in sync:
    /// maxHp drives the slider's maxValue, currentHp drives its value.
    /// </summary>
    public class PlayerHealthBar : MonoBehaviour
    {
        [Tooltip("Slider whose maxValue is set to maxHp and value to currentHp (minValue is forced to 0).")]
        [SerializeField] private Slider _slider;
        [Tooltip("Optional label, e.g. \"80 / 100\".")]
        [SerializeField] private TMP_Text _label;

        private PlayerController _player;

        private void OnDisable() => Unbind();

        private void Update()
        {
            // The player is spawned at runtime; keep trying until found, and rebind if it despawns.
            if (_player == null)
                TryBind();
        }

        private void TryBind()
        {
            _player = FindFirstObjectByType<PlayerController>();
            if (_player == null) return;

            _player.HealthChanged += OnHealthChanged;
            OnHealthChanged(_player.CurrentHp, _player.MaxHp);
        }

        private void Unbind()
        {
            if (_player == null) return;
            _player.HealthChanged -= OnHealthChanged;
            _player = null;
        }

        private void OnHealthChanged(float currentHp, float maxHp)
        {
            if (_slider != null)
            {
                _slider.minValue = 0f;
                _slider.maxValue = maxHp;
                _slider.value = Mathf.Clamp(currentHp, 0f, maxHp);
            }

            if (_label != null)
                _label.text = $"{Mathf.CeilToInt(currentHp)} / {Mathf.CeilToInt(maxHp)}";
        }
    }
}
