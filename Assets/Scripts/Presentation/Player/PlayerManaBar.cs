using TMPro;
using UnityEngine;
using UnityEngine.UI; // Slider

namespace DwarfsCrypt.Presentation.Player
{
    /// <summary>
    /// Drives an on-screen mana bar from the spawned player's mana, mirroring
    /// <see cref="PlayerHealthBar"/>. Build the bar visuals yourself (a Slider and/or a
    /// label) and assign them here. The component finds the <see cref="PlayerController"/>
    /// at runtime, binds to its <see cref="PlayerController.ManaChanged"/> event, and keeps
    /// the slider in sync: maxMana drives the slider's maxValue, currentMana drives its value.
    /// </summary>
    public class PlayerManaBar : MonoBehaviour
    {
        [Tooltip("Slider whose maxValue is set to maxMana and value to currentMana (minValue is forced to 0).")]
        [SerializeField] private Slider _slider;
        [Tooltip("Optional label, e.g. \"35 / 80\".")]
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

            _player.ManaChanged += OnManaChanged;
            OnManaChanged(_player.CurrentMana, _player.MaxMana);
        }

        private void Unbind()
        {
            if (_player == null) return;
            _player.ManaChanged -= OnManaChanged;
            _player = null;
        }

        private void OnManaChanged(float currentMana, float maxMana)
        {
            if (_slider != null)
            {
                _slider.minValue = 0f;
                _slider.maxValue = maxMana;
                _slider.value = Mathf.Clamp(currentMana, 0f, maxMana);
            }

            if (_label != null)
                _label.text = $"{Mathf.FloorToInt(currentMana)} / {Mathf.CeilToInt(maxMana)}";
        }
    }
}
