using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DwarfsCrypt.Presentation.Boosters
{
    /// <summary>
    /// One buff slot in the on-screen buff bar. The radial fill is part of the base prefab (its own
    /// sprite/Radial-360 setup stays as designed — only its fillAmount is animated), while the icon
    /// sprite comes from the buff itself (supplied by the shrine's <c>BoosterController</c>). So this
    /// is a single generic base prefab reused for every buff; <see cref="BuffBar"/> spawns and binds
    /// one per buff, swapping in the right icon at runtime.
    /// </summary>
    public class BuffIcon : MonoBehaviour
    {
        [Tooltip("Image showing the buff's icon (sprite is set from the buff at runtime).")]
        [SerializeField] private Image _iconImage;
        [Tooltip("Prefab's own Image Type = Filled / Fill Method = Radial 360 image; " +
                 "only its fillAmount is driven 1->0 over the duration (sprite kept from the prefab).")]
        [SerializeField] private Image _radialFill;
        [Tooltip("Optional label showing whole seconds remaining.")]
        [SerializeField] private TMP_Text _timerLabel;

        private ActiveBuff _buff;

        public ActiveBuff Buff => _buff;

        public void Bind(ActiveBuff buff)
        {
            _buff = buff;

            // Icon comes from the buff; the radial fill keeps the prefab's own sprite.
            if (_iconImage != null && buff.Icon != null)
                _iconImage.sprite = buff.Icon;

            Refresh();
        }

        private void Update()
        {
            if (_buff != null) Refresh();
        }

        private void Refresh()
        {
            if (_radialFill != null)
                _radialFill.fillAmount = _buff.Normalized;
            if (_timerLabel != null)
                _timerLabel.text = Mathf.CeilToInt(_buff.Remaining).ToString();
        }
    }
}
