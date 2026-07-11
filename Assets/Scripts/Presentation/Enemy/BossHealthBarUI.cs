using TMPro;
using UnityEngine;
using UnityEngine.UI; // Slider

namespace DwarfsCrypt.Presentation.Enemy
{
    /// <summary>
    /// Drives an on-screen boss HP bar, mirroring <see cref="Player.PlayerHealthBar"/>:
    /// build the visuals yourself (a Slider, an HP label, a name label). The bar is shown
    /// while any boss is aggroed on the player (chasing or attacking) and hidden when the
    /// boss dies or gives up the chase. If several bosses are aggroed at once, the one
    /// with the lowest HP fraction is shown.
    ///
    /// The inspector references are optional: anything left empty is auto-found in this
    /// object's children (first Slider, first label under it, its fillRect image), so the
    /// component can simply sit on the bar root. Visibility is driven through a
    /// CanvasGroup's alpha — never SetActive — so the component keeps updating while the
    /// bar is hidden even when it lives on the hidden object itself.
    /// </summary>
    public class BossHealthBarUI : MonoBehaviour
    {
        [Tooltip("Root of the bar visuals; hidden/shown via CanvasGroup alpha. If empty, the slider's GameObject (or this one) is used.")]
        [SerializeField] private GameObject _barRoot;
        [Tooltip("Slider whose maxValue is set to maxHp and value to currentHp. If empty, the first Slider in children is used.")]
        [SerializeField] private Slider _slider;
        [Tooltip("Label like \"450 / 900\". If empty, the first TMP text under the slider is used.")]
        [SerializeField] private TMP_Text _hpLabel;
        [Tooltip("Optional label showing the boss's name from its character JSON. Not auto-found.")]
        [SerializeField] private TMP_Text _nameLabel;

        [Header("Enrage tint (optional)")]
        [Tooltip("The slider's Fill image; tinted while the boss is enraged. If empty, taken from the slider's Fill Rect.")]
        [SerializeField] private Image _fillImage;
        [SerializeField] private Color _fillColor = new Color(0.75f, 0.15f, 0.12f, 1f);
        [SerializeField] private Color _enragedFillColor = new Color(1f, 0.45f, 0.05f, 1f);

        private CanvasGroup _canvasGroup;
        private BossController _boss;

        private void Awake()
        {
            if (_slider == null)
                _slider = GetComponentInChildren<Slider>(true);

            if (_hpLabel == null && _slider != null)
                _hpLabel = _slider.GetComponentInChildren<TMP_Text>(true);

            if (_fillImage == null && _slider != null && _slider.fillRect != null)
                _fillImage = _slider.fillRect.GetComponent<Image>();

            GameObject root = _barRoot != null ? _barRoot
                : _slider != null ? _slider.gameObject
                : gameObject;

            _canvasGroup = root.GetComponent<CanvasGroup>();
            if (_canvasGroup == null)
                _canvasGroup = root.AddComponent<CanvasGroup>();
            _canvasGroup.interactable = false;
            _canvasGroup.blocksRaycasts = false;

            // A disabled Slider component (easy leftover when duplicating bar prefabs)
            // would silently skip visual updates.
            if (_slider != null)
                _slider.enabled = true;

            SetVisible(false);
        }

        private void Update()
        {
            BossController boss = PickBoss();
            if (boss == null)
            {
                _boss = null;
                SetVisible(false);
                return;
            }

            if (boss != _boss)
            {
                _boss = boss;
                if (_nameLabel != null)
                    _nameLabel.text = _boss.Character.Character.Name;
            }

            float max = _boss.Character.MaxHp;
            float cur = Mathf.Clamp(_boss.Character.CurrentHp, 0f, max);

            if (_slider != null)
            {
                _slider.minValue = 0f;
                _slider.maxValue = max;
                _slider.value = cur;
            }

            if (_hpLabel != null)
                _hpLabel.text = $"{Mathf.CeilToInt(cur)} / {Mathf.CeilToInt(max)}";

            if (_fillImage != null)
                _fillImage.color = _boss.IsEnraged ? _enragedFillColor : _fillColor;

            SetVisible(true);
        }

        private void SetVisible(bool visible)
        {
            _canvasGroup.alpha = visible ? 1f : 0f;

            // If the root is a different object that someone left inactive, activate it on
            // show; hiding is always alpha-only so this component's Update keeps running.
            if (visible && !_canvasGroup.gameObject.activeSelf)
                _canvasGroup.gameObject.SetActive(true);
        }

        // Among live, aggroed bosses prefer the one closest to death — in a multi-boss
        // fight that's the one the player is actually working on.
        private BossController PickBoss()
        {
            BossController best = null;
            float bestFraction = float.MaxValue;

            foreach (BossController boss in BossController.ActiveBosses)
            {
                if (boss == null || boss.Character == null || boss.Character.Character == null) continue;
                if (boss.Character.IsDead || !boss.IsAggroed) continue;

                float max = boss.Character.MaxHp;
                float fraction = max > 0f ? boss.Character.CurrentHp / max : 0f;
                if (fraction < bestFraction)
                {
                    bestFraction = fraction;
                    best = boss;
                }
            }
            return best;
        }
    }
}
