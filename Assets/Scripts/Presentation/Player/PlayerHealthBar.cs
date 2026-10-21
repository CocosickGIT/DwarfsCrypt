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

        // Bind via PlayerController's spawn/despawn events instead of scanning the scene every frame.
        private void OnEnable()
        {
            PlayerController.Spawned += Bind;
            PlayerController.Despawned += HandleDespawned;

            // The player may already exist (bar enabled after spawn) — bind to it now.
            if (PlayerController.Current != null)
                Bind(PlayerController.Current);
        }

        private void OnDisable()
        {
            PlayerController.Spawned -= Bind;
            PlayerController.Despawned -= HandleDespawned;
            Unbind();
        }

        private void Bind(PlayerController player)
        {
            if (player == null || _player == player) return;
            Unbind();

            _player = player;
            _player.HealthChanged += OnHealthChanged;
            OnHealthChanged(_player.CurrentHp, _player.MaxHp);
        }

        private void HandleDespawned(PlayerController player)
        {
            if (_player == player) Unbind();
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
