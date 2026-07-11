using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Core.Player;

namespace DwarfsCrypt.Presentation.Save
{
    /// <summary>
    /// One save-slot entry in the InitScene select screen. Shows the slot's summary (or "New Game"
    /// when empty) and reports clicks back to <see cref="SaveSelectMenu"/>. Build the visuals as a
    /// prefab (a select Button + label, optional delete Button) and assign them here.
    /// </summary>
    public class SaveSlotButton : MonoBehaviour
    {
        [Tooltip("Pressed to play this slot.")]
        [SerializeField] private Button _selectButton;
        [Tooltip("Shows the slot summary, e.g. \"Slot 1: Thorin (Lv 3)\".")]
        [SerializeField] private TMP_Text _label;
        [Tooltip("Optional: deletes the slot's save. Hidden for empty slots.")]
        [SerializeField] private Button _deleteButton;

        private int _slot;
        private Action<int> _onSelect;
        private Action<int> _onDelete;

        public void Bind(SaveSlotInfo info, Action<int> onSelect, Action<int> onDelete)
        {
            _slot = info.Slot;
            _onSelect = onSelect;
            _onDelete = onDelete;

            if (_label != null)
                _label.text = info.Exists
                    ? $"Slot {info.Slot + 1}: {info.Name} (Lv {info.Level})"
                    : $"Slot {info.Slot + 1}: New Game";

            if (_selectButton != null)
            {
                _selectButton.onClick.RemoveAllListeners();
                _selectButton.onClick.AddListener(() => _onSelect?.Invoke(_slot));
            }

            if (_deleteButton != null)
            {
                _deleteButton.gameObject.SetActive(info.Exists);
                _deleteButton.onClick.RemoveAllListeners();
                _deleteButton.onClick.AddListener(() => _onDelete?.Invoke(_slot));
            }
        }
    }
}
