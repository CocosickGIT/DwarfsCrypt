using DwarfsCrypt.Domain.Items;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DwarfsCrypt.Presentation.Windows
{
    /// <summary>
    /// A single row on the reward screen: item icon + "Name xN" text.
    /// Put this on the reward row prefab and wire its Icon/Label refs in the inspector.
    /// Icons are loaded the same way the inventory does (Resources.Load by IconPath).
    /// </summary>
    public class RewardItemRow : MonoBehaviour
    {
        // Shown in the icon box when an item has no sprite yet (placeholder, not transparent).
        private static readonly Color PlaceholderColor = new Color(0.35f, 0.35f, 0.35f, 1f);

        [SerializeField] private Image _icon;
        [SerializeField] private TMP_Text _label;

        public void Set(ItemData data, int quantity)
        {
            if (_icon != null)
            {
                var sprite = data != null && !string.IsNullOrEmpty(data.IconPath)
                    ? Resources.Load<Sprite>(data.IconPath)
                    : null;
                _icon.sprite = sprite;
                // With no sprite a UI Image draws a solid quad — show a visible gray placeholder box.
                _icon.color = sprite != null ? Color.white : PlaceholderColor;
            }

            if (_label != null)
            {
                string name = data != null ? data.Name : "?";
                _label.text = quantity > 1 ? $"{name} x{quantity}" : name;
            }
        }
    }
}
